using Core;

namespace Server;

/// <summary>
/// 数据转发
/// 把管理员发送的命令转发到指定的客户端
/// adminClientRequest => server => clientWorkRequest
/// </summary>
[PlayAdminCommandFilter]
[PlayCommand(CommandKey.Forward)]
public sealed class Forward : PlayAsyncCommand<ForwardPacket, ForwardRespPacket>
{
    protected override async ValueTask<ForwardRespPacket?> ExecuteAsync(PlaySession session, ForwardPacket packet)
    {
        session.LogInformation($"{session.UserId}：管理员转发命令至：{packet.TargetId}");

        var targetId = packet.TargetId;
        packet.TargetId = session.UserId;
        
        if (packet.IsSingle)
        {
            //发送到指定客户端
            if (await session.SendPacketAsync(packet, filter => filter.UserId == targetId))
                return null;
        }
        else
        {
            //群发
            await session.SendPacketAsync(packet, filter => !filter.IsAdmin);
            return null;
        }

        session.LogWarning($"{packet.TargetId} 目标客户端不在线");

        return new ForwardRespPacket
        {
            TargetId = packet.TargetId,
            Identifier = packet.Identifier,
            ErrorMessage = "目标客户端不在线",
        };
    }
}