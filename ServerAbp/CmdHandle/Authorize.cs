using Core;

namespace Server;

/// <summary>
/// 客户端授权认证
/// </summary>
[PlayCommand(CommandKey.Authorize)]
public sealed class Authorize : PlayAsyncCommand<AuthorizePacket, AuthorizeRespPacket>
{
    protected override async ValueTask<AuthorizeRespPacket?> ExecuteAsync(PlaySession session, AuthorizePacket packet)
    {
        session.LogInformation($"{packet.UserId}：客户端授权");

        session.UserId = packet.UserId;

        //发送上线封包至管理员
        await session.SendPacketToAdminAsync(new ClientOnlinePacket
        {
            Name = session.Name,
            UserId = packet.UserId,
            Address = session.Address,
        });

        return new AuthorizeRespPacket
        {
            SuccessFul = true,
            Identifier = packet.Identifier,
        };
    }
}