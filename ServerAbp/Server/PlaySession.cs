using System.Net;
using Core;
using SuperSocket;
using SuperSocket.Channel;
using SuperSocket.ProtoBase;
using SuperSocket.Server;

namespace Server;

public sealed class PlaySession : AppSession
{
    private CancellationTokenSource _connectionToken = null!;
    private readonly IPackageEncoder<PlayPacket> _encoder;
    private readonly IAsyncSessionContainer _sessionContainer;

    public PlaySession(IPackageEncoder<PlayPacket> encoder, IAsyncSessionContainer sessionContainer)
    {
        _encoder = encoder;
        _sessionContainer = sessionContainer;
    }

    internal ulong UserId { get; set; }

    internal bool IsAdmin { get; set; }

    internal string? Name { get; set; }

    internal string Address => ((IPEndPoint)RemoteEndPoint).Address.ToString();

    protected override ValueTask OnSessionConnectedAsync()
    {
        _connectionToken = new CancellationTokenSource();

        return base.OnSessionConnectedAsync();
    }

    protected override async ValueTask OnSessionClosedAsync(CloseEventArgs e)
    {
        _connectionToken.Cancel();
        _connectionToken.Dispose();

        //发送离线线封包至管理员
        await SendPacketToAdminAsync(new ClientOfflinePacket
        {
            Name = Name,
            UserId = UserId,
            Address = Address,
        });
    }

    internal async ValueTask<List<ClientDetails>> GetClientDetailsAsync()
    {
        var list = new List<ClientDetails>();

        var sessions = await _sessionContainer.GetSessionsAsync<PlaySession>(s => !s.IsAdmin);

        list.AddRange(sessions.Select(s => new ClientDetails
        {
            Online = true,
            Name = s.Name,
            UserId = s.UserId,
            Address = s.Address,
        }));

        return list;
    }

    internal ValueTask SendPacketAsync(PlayPacket packet)
    {
        ThrowIfChannelClosed();

        return Channel.SendAsync(_encoder, packet);
    }

    internal async ValueTask SendPacketToAdminAsync(PlayPacket packet)
    {
        //找到管理员session
        await SendPacketAsync(packet, session => session.IsAdmin);
    }

    internal async ValueTask<bool> SendPacketAsync(PlayPacket packet, Predicate<PlaySession> filter)
    {
        var sessions = await _sessionContainer.GetSessionsAsync(filter);

        if (sessions == null || !sessions.Any())
            return false;

        foreach (var session in sessions)
        {
            await session.SendPacketAsync(packet);
        }

        return true;
    }

    private void ThrowIfChannelClosed()
    {
        if (!Channel.IsClosed)
            return;

        if (!_connectionToken.IsCancellationRequested)
            _connectionToken.Cancel();

        throw new SessionClosedException();
    }
}

public sealed class SessionClosedException : Exception
{
    public SessionClosedException()
    {
    }

    public SessionClosedException(string? message)
        : base(message)
    {
    }
}