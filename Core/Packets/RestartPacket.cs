using MemoryPack;

namespace Core;

[MemoryPackable]
public sealed partial class RestartPacket : PlayPacket
{
    public RestartPacket() : base(CommandKey.Restart)
    {
    }
}

[MemoryPackable]
public sealed partial class RestartRespPacket : PlayRespPacket
{
    public RestartRespPacket() : base(CommandKey.RestartAck)
    {
    }
}