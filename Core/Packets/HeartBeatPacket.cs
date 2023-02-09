using MemoryPack;

namespace Core;

[MemoryPackable]
public sealed partial class HeartBeatPacket : PlayPacket
{
    public long ClientTimestamp { get; set; }
    
    public HeartBeatPacket() : base(CommandKey.HeartBeat)
    {
    }
}

[MemoryPackable]
public sealed partial class HeartBeatRespPacket : PlayPacket
{
    public long ServerTimestamp { get; set; }
    
    public HeartBeatRespPacket() 
        : base(CommandKey.HeartBeatAck)
    {
    }
}