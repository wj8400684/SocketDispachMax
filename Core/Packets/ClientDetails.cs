using MemoryPack;

namespace Core;

[MemoryPackable]
public sealed partial class ClientDetails
{
    public ulong UserId { get; set; }
    
    public string? Name { get; set; }

    public string? Address { get; set; }

    public bool Online { get; set; }
}