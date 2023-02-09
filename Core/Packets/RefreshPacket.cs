using MemoryPack;

namespace Core;

[MemoryPackable]
public sealed partial class RefreshPacket : PlayPacket
{
    public RefreshPacket() : base(CommandKey.Refresh)
    {
    }
}

[MemoryPackable]
public sealed partial class RefreshRespPacket : PlayRespPacket
{
    public RefreshRespPacket() 
        : base(CommandKey.RefreshAck)
    {
    }
}