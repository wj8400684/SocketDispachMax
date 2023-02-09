using Core;

namespace Server;

/// <summary>
/// 转发回复
/// 目标客户端回复的命令转发至管理员
/// clintWorkResponse => server => clientAdminResponse
/// </summary>
[PlayCommand(CommandKey.ForwardAck)]
public sealed class ForwardAck : PlayAsyncCommand<ForwardRespPacket>
{
    protected override async ValueTask ExecuteAsync(PlaySession session, ForwardRespPacket packet)
    {
        //转发至客户端
        var result = await session.SendPacketAsync(packet,s => s.UserId == packet.TargetId);

        if (result)
            return;
        
        session.LogWarning($"{packet.TargetId} 管理客户端不在线");
    }
}