using MemoryPack;

namespace Core;

[MemoryPackable]
public sealed partial class OrderPacket : PlayPacket
{
    public ulong TargetId { get; set; }

    public string Content { get; set; } = null!;
    
    public OrderPacket() : base(CommandKey.AddOrder)
    {

    }
}

[MemoryPackable]
public sealed partial class OrderRespPacket : PlayRespPacket
{
    public OrderRespPacket() : base(CommandKey.AddOrderAck)
    {
    }
}