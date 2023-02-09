using System.Data;
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

public sealed class CommandHandle
{
    private readonly MethodInfo _method;
    private readonly bool _isTaskGenericType;
    private readonly object? _delegateTarget;
    private readonly Func<object?, object[], Task> _handler;
    private readonly IEasyClient<PlayPacket, PlayPacket> _client;

    public CommandHandle(MethodInfo method, IEasyClient<PlayPacket, PlayPacket> client, object? delegateTarget = null)
    {
        //获取该方法返回类型是否为 Task<T> 
        var isTaskGenericType = method.ReturnType.IsGenericType &&
                                method.ReturnType.GetGenericTypeDefinition() == typeof(Task<>);

        _method = method;
        _client = client;
        _delegateTarget = delegateTarget;
        _handler = CreateInvoker(method);
        _isTaskGenericType = isTaskGenericType;
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
        if (_isTaskGenericType)
        {
            var result = await resultTask.ToTask<PlayPacket>();

            await _client.SendAsync(result);
        }
        else
        {
            await resultTask;
        }
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
}

public sealed class PlayClient
{
    private Timer _heartTimer;
    private readonly PacketDispatcher _packetDispatcher = new();
    private readonly PacketIdentifierProvider _packetIdentifierProvider = new();

    private readonly IEasyClient<PlayPacket, PlayPacket> _client =
        new IOCPTcpEasyClient<PlayPacket, PlayPacket>(new PlayPipeLineFilter(), new PlayPacketEncode());

    private readonly Dictionary<CommandKey, CommandHandle> _handlers = new(20);

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

        OnHeartBeat(null);
    }

    public ValueTask<LoginRespPacket> LoginAsync(string username, string password)
    {
        return SendAndReceivePacketAsync<LoginRespPacket>(new LoginPacket
        {
            Username = username,
            Password = password,
        });
    }

    public ValueTask<OrderRespPacket> AddOrderAsync(CancellationToken cancellationToken)
    {
        var contentPacket = new OrderPacket
        {
            Content = "sssssssssssss",
        };

        return SendAndReceiveForwardPacketAsync<OrderRespPacket>(new ForwardPacket()
        {
            IsSingle = true,
            Body = contentPacket.Encode(),
            TargetId = 1554106598,
            BodyCommandKey = contentPacket.Key,
        }, cancellationToken);
    }

    public ValueTask<RestartRespPacket> RestartOrderAsync()
    {
        var contentPacket = new RestartPacket
        {
            
        };

        var body = contentPacket.Encode();

        return SendAndReceiveForwardPacketAsync<RestartRespPacket>(new ForwardPacket()
        {
            Body = contentPacket.Encode(),
            TargetId = 1554106598,
            BodyCommandKey = contentPacket.Key,
        }, CancellationToken.None);
    }

    public ValueTask<RefreshRespPacket> RefreshOrderAsync()
    {
        var contentPacket = new RefreshPacket
        {
        };

        return SendAndReceiveForwardPacketAsync<RefreshRespPacket>(new ForwardPacket()
        {
            Body = contentPacket.Encode(),
            TargetId = 1554106598,
            BodyCommandKey = contentPacket.Key,
        }, CancellationToken.None);
    }

    public PlayClient RegisterHandle<TPacket>(Func<TPacket, Task> handler)
        where TPacket : PlayPacket
    {
        var command = PlayPacket.GetCommandKey<TPacket>();

        var commandHandle = new CommandHandle(handler.Method, _client, handler.Target);

        if (!_handlers.TryAdd(command, commandHandle))
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

    /// <summary>
    /// 发送并且等待转发的封包
    /// </summary>
    /// <param name="packet"></param>
    /// <param name="cancellationToken"></param>
    /// <typeparam name="TRespPacket"></typeparam>
    /// <returns></returns>
    /// <exception cref="Exception"></exception>
    private async ValueTask<TRespPacket> SendAndReceiveForwardPacketAsync<TRespPacket>(
        ForwardPacket packet,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        packet.Identifier = _packetIdentifierProvider.GetNextPacketIdentifier();

        using var packetAwaitable = _packetDispatcher.AddAwaitable<ForwardRespPacket>(packet.Identifier);

        try
        {
            await _client.SendAsync(packet);
        }
        catch (Exception e)
        {
            packetAwaitable.Fail(e);
            Console.Error.WriteLine($"发送封包抛出一个异常 key：{packet.Key}");
        }

        ForwardRespPacket forwardRespPacket;

        try
        {
            forwardRespPacket = await packetAwaitable.WaitAsync(cancellationToken);
        }
        catch (Exception e)
        {
            if (e is TimeoutException)
                Console.Error.WriteLine($"等待封包调度超时 key：{packet.Key}");

            throw;
        }

        if (!forwardRespPacket.SuccessFul)
            throw new Exception($"转发命令失败：{forwardRespPacket.ErrorMessage}");

        return forwardRespPacket.DecodeBody<TRespPacket>();
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
            timer.Change(20 * 1000, 20 * 1000);
        }
    }

    void OnClosed(object? sender, EventArgs e)
    {
        Console.WriteLine("断开连接");
    }

    ValueTask OnPackageHandler(IEasyClient<PlayPacket>? client, PlayPacket packet)
    {
        Console.WriteLine($"key：{packet.Key}");

        switch (packet.Key)
        {
            case CommandKey.LoginAck: //设置调度器
            case CommandKey.ForwardAck: //设置调度器
            case CommandKey.AuthorizeAck: //设置调度器
                var packetWithIdentifier = (PlayPacketWithIdentifier)packet;
                _packetDispatcher.TryDispatch(packetWithIdentifier);
                return ValueTask.CompletedTask;
            case CommandKey.HeartBeatAck: //无需处理
                return ValueTask.CompletedTask;
            default:
                break;
        }

        if (!_handlers.TryGetValue(packet.Key, out var commandHandle))
            return ValueTask.CompletedTask;

        Task.Run(() => commandHandle.InvokeAsync(packet));

        return ValueTask.CompletedTask;
    }
}