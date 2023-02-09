using System.Buffers;
using System.Linq.Expressions;
using System.Net;
using System.Net.Sockets;
using System.Reflection;
using System.Security.Authentication;
using Core;
using MemoryPack;
using SuperSocket.Client;
using SuperSocket.IOCPEasyClient;

namespace Work;

public readonly struct HandlerReplyArgs
{
    public HandlerReplyArgs(PlayPacket packet)
    {
        Packet = packet;
        ForwardPacket = null;
    }

    public HandlerReplyArgs(PlayPacket packet, ForwardPacket forwardPacket)
    {
        Packet = packet;
        ForwardPacket = forwardPacket;
    }

    public readonly PlayPacket Packet { get; }

    public readonly ForwardPacket? ForwardPacket { get; }
}

public sealed class CommandHandle
{
    private readonly MethodInfo _method;
    private readonly bool _isTaskGenericType;
    private readonly object? _delegateTarget;
    private readonly Type? _parameterType;
    private readonly Func<object?, object[], Task> _handler;
    private readonly Func<HandlerReplyArgs, ValueTask>? _handlerReply;

    public CommandHandle(MethodInfo method, Func<HandlerReplyArgs, ValueTask>? handlerReply = null,
        object? delegateTarget =
            null)
    {
        //获取该方法返回类型是否为 Task<T> 
        var isTaskGenericType = method.ReturnType.IsGenericType &&
                                method.ReturnType.GetGenericTypeDefinition() == typeof(Task<>);

        _method = method;
        _handlerReply = handlerReply;
        _delegateTarget = delegateTarget;
        _handler = CreateInvoker(method);
        _isTaskGenericType = isTaskGenericType;

        var parameters = method.GetParameters();

        if (parameters.Length > 0)
            _parameterType = parameters.First().ParameterType;
    }

    public async Task InvokeAsync(PlayPacket packet)
    {
        Task resultTask;

        try
        {
            resultTask = _handler.Invoke(_delegateTarget, new[] { packet });
        }
        catch (Exception ex)
        {
            throw new Exception($"执行{_method.Name}抛出一个异常", ex);
        }

        //如果该方法返回的类型是 task<> 范型类型 则需要回复服务器信息
        if (!_isTaskGenericType)
            await resultTask;
        else
        {
            var result = await resultTask.ToTask<PlayPacket>();

            await HandReplyAsync(new HandlerReplyArgs(result));
        }
    }

    public async Task InvokeAsync(ForwardPacket packet)
    {
        //获取该方法的返回范型参数
        var parameterType = _parameterType;

        ArgumentNullException.ThrowIfNull(parameterType, $"{_method.Name} 执行方法必须具有返回参数");

        //反序列化
        var bodyPacket = packet.DecodeBody(parameterType);

        Task resultTask;

        try
        {
            resultTask = _handler.Invoke(_delegateTarget, new[] { bodyPacket });
        }
        catch (Exception ex)
        {
            throw new Exception($"执行{_method.Name}抛出一个异常", ex);
        }

        var result = await resultTask.ToTask<PlayPacket>();

        await HandReplyAsync(new HandlerReplyArgs(result, packet));
    }

    private Func<object?, object[], Task> CreateInvoker(MethodInfo methodInfo)
    {
        const string instanceName = "instance";
        const string parametersName = "parameters";

        var instance = Expression.Parameter(typeof(object), instanceName);

        var parameters = Expression.Parameter(typeof(object[]), parametersName);

        var instanceCast = methodInfo.IsStatic ? null : Expression.Convert(instance, methodInfo.DeclaringType!);

        var parametersCast = methodInfo.GetParameters().Select((p, i) =>
        {
            var parameter = Expression.ArrayIndex(parameters, Expression.Constant(i));
            return Expression.Convert(parameter, p.ParameterType);
        });

        var body = Expression.Call(instanceCast, methodInfo, parametersCast);

        var bodyCast = Expression.Convert(body, typeof(Task));

        return Expression.Lambda<Func<object?, object[], Task>>(bodyCast, instance, parameters).Compile();
    }

    private async ValueTask HandReplyAsync(HandlerReplyArgs handlerReplyArgs)
    {
        var handler = _handlerReply;

        if (handler != null)
            await handler.Invoke(handlerReplyArgs);
    }
}

public sealed class PlayClient
{
    private Timer _heartTimer;
    private readonly PacketDispatcher _packetDispatcher = new();
    private readonly PacketIdentifierProvider _packetIdentifierProvider = new();

    private readonly IEasyClient<PlayPacket, PlayPacket> _client =
        new IOCPTcpEasyClient<PlayPacket, PlayPacket>(new PlayPipeLineFilter(), new PlayPacketEncode());

    private readonly Dictionary<CommandKey, CommandHandle> _commandHandlePool = new(20);

    public PlayClient()
    {
        _client.Closed += OnClosed;
        _client.PackageHandler += OnPackageHandler;
        //_client.Security = GetSecurityOptions();
        _heartTimer = new Timer(OnHeartBeat, null, Timeout.Infinite, Timeout.Infinite);
    }

    public async ValueTask StartAsync()
    {
        Console.WriteLine("连接成功");

        var connected = await _client.ConnectAsync(new DnsEndPoint("127.0.0.1", 4040, AddressFamily.InterNetwork));

        Console.WriteLine($"连接结果: {connected}");

        _client.StartReceive();
    }

    public async ValueTask<AuthorizeRespPacket> AuthorizeAsync(ulong userId)
    {
        var result = await SendAndReceivePacketAsync<AuthorizeRespPacket>(new AuthorizePacket
        {
            UserId = userId,
        });

        if (result.SuccessFul)
            OnHeartBeat(null);

        return result;
    }

    /// <summary>
    /// 注册一个需要回复执行者
    /// </summary>
    /// <param name="handler"></param>
    /// <typeparam name="TPacket"></typeparam>
    /// <typeparam name="TRespPacket"></typeparam>
    /// <returns></returns>
    /// <exception cref="Exception"></exception>
    public PlayClient RegisterHandle<TPacket, TRespPacket>(Func<TPacket, Task<TRespPacket>> handler)
        where TPacket : PlayPacket
        where TRespPacket : PlayPacket
    {
        //获取封包标记的 command
        var command = PlayPacket.GetCommandKey<TPacket>();

        //实例化命令执行器
        var commandHandle = new CommandHandle(handler.Method, OnHandlerReplyAsync, handler.Target);

        //添加到命令池
        if (!_commandHandlePool.TryAdd(command, commandHandle))
            throw new Exception($"{command}该命令已经注册");

        return this;
    }

    private SecurityOptions GetSecurityOptions()
    {
        return new SecurityOptions
        {
            TargetHost = "supersocket",
            EnabledSslProtocols = SslProtocols.Tls12 | SslProtocols.Tls13,
            RemoteCertificateValidationCallback = (sender, certificate, chain, sslPolicyErrors) => true
        };
    }

    private ValueTask<TRespPacket> SendAndReceivePacketAsync<TRespPacket>(
        PlayPacketWithIdentifier packet)
        where TRespPacket : PlayPacketWithIdentifier
    {
        return SendAndReceivePacketAsync<TRespPacket>(packet, CancellationToken.None);
    }

    private async ValueTask<TRespPacket> SendAndReceivePacketAsync<TRespPacket>(
        PlayPacketWithIdentifier packet,
        CancellationToken cancellationToken)
        where TRespPacket : PlayPacketWithIdentifier
    {
        cancellationToken.ThrowIfCancellationRequested();

        packet.Identifier = _packetIdentifierProvider.GetNextPacketIdentifier();

        using var packetAwaitable = _packetDispatcher.AddAwaitable<TRespPacket>(packet.Identifier);

        try
        {
            await _client.SendAsync(packet);
        }
        catch (Exception e)
        {
            packetAwaitable.Fail(e);
            Console.Error.WriteLine($"发送封包抛出一个异常 key：{packet.Key}");
        }

        try
        {
            return await packetAwaitable.WaitAsync(cancellationToken);
        }
        catch (Exception e)
        {
            if (e is TimeoutException)
                Console.Error.WriteLine($"等待封包调度超时 key：{packet.Key}");

            throw;
        }
    }

    async void OnHeartBeat(object? state)
    {
        var timer = _heartTimer;

        try
        {
            timer.Change(Timeout.Infinite, Timeout.Infinite);

            await _client.SendAsync(new HeartBeatPacket
            {
                ClientTimestamp = DateTime.Now.ToTimeStamp13(),
            });
        }
        finally
        {
            timer.Change(200 * 1000, 200 * 1000);
        }
    }

    void OnClosed(object? sender, EventArgs e)
    {
        Console.WriteLine("断开连接");
    }

    async ValueTask OnHandlerReplyAsync(HandlerReplyArgs args)
    {
        var forwardPacket = args.ForwardPacket;

        if (forwardPacket == null)
        {
            await _client.SendAsync(args.Packet);
        }
        else
        {
            await _client.SendAsync(new ForwardRespPacket
            {
                //表示转发完成
                SuccessFul = true,
                Body = args.Packet.Encode(),
                Identifier = forwardPacket.Identifier,
                TargetId = forwardPacket.TargetId,
            });
        }
    }

    ValueTask OnPackageHandler(IEasyClient<PlayPacket>? client, PlayPacket packet)
    {
        Console.WriteLine($"key：{packet.Key}");

        switch (packet.Key)
        {
            case CommandKey.LoginAck:
            case CommandKey.AuthorizeAck: //以上两个命令设置封包调度器内容
                _packetDispatcher.TryDispatch((PlayPacketWithIdentifier)packet);
                return ValueTask.CompletedTask;
            case CommandKey.HeartBeatAck: //无需处理
                return ValueTask.CompletedTask;
            default:
                break;
        }

        CommandHandle? commandHandle;

        //不是转发命令则直接执行
        if (packet.Key != CommandKey.Forward)
        {
            if (_commandHandlePool.TryGetValue(packet.Key, out commandHandle))
                Task.Run(() => commandHandle.InvokeAsync(packet));

            return ValueTask.CompletedTask;
        }

        //获取转发的封包
        var forwardPacket = (ForwardPacket)packet;

        //获取注册的命令执行器
        if (!_commandHandlePool.TryGetValue(forwardPacket.BodyCommandKey, out commandHandle))
            return ValueTask.CompletedTask;

        //调用外部方法
        Task.Run(() => commandHandle.InvokeAsync(forwardPacket));

        return ValueTask.CompletedTask;
    }
}