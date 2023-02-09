using MemoryPack;

namespace Core;

[MemoryPackable]
public sealed partial class AuthorizePacket : PlayPacketWithIdentifier
{
    public ulong UserId { get; set; }

    public AuthorizePacket() 
        : base(CommandKey.Authorize)
    {
  
    }
}

[MemoryPackable]
public sealed partial class AuthorizeRespPacket : PlayPacketRespWithIdentifier
{
    public AuthorizeRespPacket() 
        : base(CommandKey.AuthorizeAck)
    {
    }
}