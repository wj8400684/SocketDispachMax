using MemoryPack;

namespace Core;

[MemoryPackable]
public sealed partial class LoginPacket : PlayPacketWithIdentifier
{
    public string Username { get; set; } = null!;

    public string Password { get; set; } = null!;
    
    public LoginPacket() : base(CommandKey.Login)
    {
    }
}

[MemoryPackable]
public sealed partial class LoginRespPacket : PlayPacketRespWithIdentifier
{
    public ulong Id { get; set; }

    public string? Token { get; set; }

    public List<ClientDetails>? Data { get; set; }
    
    public LoginRespPacket() : base(CommandKey.LoginAck)
    {
        
    }
}

