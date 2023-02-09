using Core;
using Microsoft.Extensions.Logging;

namespace Server;

[PlayCommand(CommandKey.Login)]
public sealed class Login : PlayAsyncCommand<LoginPacket, LoginRespPacket>
{
    protected override async ValueTask<LoginRespPacket?> ExecuteAsync(PlaySession session, LoginPacket packet)
    {
        session.LogInformation($"20424666：管理员登陆");

        session.UserId = 20424666;
        session.IsAdmin = true;

        var details = await session.GetClientDetailsAsync();

        return new LoginRespPacket
        {
            SuccessFul = true,
            Id = 20422666,
            Data = details,
            Identifier = packet.Identifier,
            Token = Guid.NewGuid().ToString("N")
        };
    }
}