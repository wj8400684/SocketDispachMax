using Core;

namespace Server;

[PlayCommand(CommandKey.HeartBeat)]
public sealed class HeartBeat : PlayAsyncCommand<HeartBeatPacket, HeartBeatRespPacket>
{
    protected override ValueTask<HeartBeatRespPacket?> ExecuteAsync(PlaySession session, HeartBeatPacket packet)
    {
        return ValueTask.FromResult<HeartBeatRespPacket?>(new HeartBeatRespPacket
        {
            ServerTimestamp = DateTime.Now.ToTimeStamp13(),
        });
    }
}