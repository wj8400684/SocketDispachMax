using MemoryPack;

namespace Core;

/// <summary>
/// 客户端上线封包
/// </summary>
[MemoryPackable]
public sealed partial class ClientOfflinePacket : PlayPacket
{
    public ulong UserId { get; set; }
    
    public string? Name { get; set; }
    
    public string? Address { get; set; }
    
    public ClientOfflinePacket() 
        : base(CommandKey.ClientOffline)
    {
    }
}

/// <summary>
/// 客户端离线封包
/// </summary>
[MemoryPackable]
public sealed partial class ClientOnlinePacket : PlayPacket
{
    public ulong UserId { get; set; }
    
    public string? Name { get; set; }
    
    public string? Address { get; set; }
    
    public ClientOnlinePacket() 
        : base(CommandKey.ClientOnline)
    {
    }
}