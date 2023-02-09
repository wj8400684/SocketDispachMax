using Core;
using Microsoft.Extensions.Logging;
using SuperSocket.Channel;
using SuperSocket.Command;

namespace Server;

public abstract class PlayAsyncCommand<TPacket> : IAsyncCommand<PlaySession, PlayPacket>
    where TPacket : PlayPacket
{
    protected abstract ValueTask ExecuteAsync(PlaySession session, TPacket packet);

    async ValueTask IAsyncCommand<PlaySession, PlayPacket>.ExecuteAsync(PlaySession session, PlayPacket package)
    {
        try
        {
            await ExecuteAsync(session, (TPacket)package);
        }
        catch (SessionClosedException)
        {
        }
        catch (Exception e)
        {
            session.LogError(e, $"执行命令：{package.Key} 抛出一个未知异常，关闭客户端");
            await session.CloseAsync(CloseReason.ApplicationError);
        }
    }
}

public abstract class PlayAsyncCommand<TPacket, TRespPacket> : IAsyncCommand<PlaySession, PlayPacket>
    where TPacket : PlayPacket
    where TRespPacket : PlayPacket
{
    protected abstract ValueTask<TRespPacket?> ExecuteAsync(PlaySession session, TPacket packet);

    public async ValueTask ExecuteAsync(PlaySession session, PlayPacket package)
    {
        TRespPacket? respPacket;

        try
        {
            respPacket = await ExecuteAsync(session, (TPacket)package);
        }
        catch (SessionClosedException)
        {
            return;
        }
        catch (Exception e)
        {
            session.LogError(e, $"执行命令：{package.Key} 抛出一个未知异常，关闭客户端");
            await session.CloseAsync(CloseReason.ApplicationError);
            return;
        }

        if (respPacket != null)
            await session.SendPacketAsync(respPacket);
    }
}