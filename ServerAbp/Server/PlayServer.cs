using Core;
using Microsoft.Extensions.Options;
using SuperSocket;
using SuperSocket.Server;

namespace Server;

public sealed class PlayServer : SuperSocketService<PlayPacket>
{
    public PlayServer(IServiceProvider serviceProvider, IOptions<ServerOptions> serverOptions)
        : base(serviceProvider, serverOptions)
    {
    }
}