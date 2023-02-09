using MemoryPack;

namespace Core;

[MemoryPackable]
public sealed partial class ForwardPacket : PlayPacketWithIdentifier
{
    /// <summary>
    /// 目标id
    /// </summary>
    public ulong TargetId { get; set; }

    /// <summary>
    /// 是转发到指定目标id还是该管理员下的所有客户端
    /// </summary>
    public bool IsSingle { get; set; }

    /// <summary>
    /// 子命令
    /// </summary>
    public CommandKey BodyCommandKey { get; set; }

    /// <summary>
    /// 转发内容
    /// </summary>
    public byte[] Body { get; set; } = null!;

    public ForwardPacket()
        : base(CommandKey.Forward)
    {
    }

    /// <summary>
    /// 解码转发的内容
    /// </summary>
    /// <param name="type"></param>
    /// <returns></returns>
    public PlayPacket DecodeBody(Type type)
    {
        var packet = MemoryPackSerializer.Deserialize(type, Body);

        ArgumentNullException.ThrowIfNull(packet,"反序序列化封包不能为null");

        return (PlayPacket)packet;
    }
}

[MemoryPackable]
public sealed partial class ForwardRespPacket : PlayPacketRespWithIdentifier
{
    /// <summary>
    /// 目标id
    /// </summary>
    public ulong TargetId { get; set; }

    /// <summary>
    /// 转发内容
    /// </summary>
    public byte[] Body { get; set; } = null!;

    public ForwardRespPacket()
        : base(CommandKey.ForwardAck)
    {
    }

    /// <summary>
    /// 解码转发的内容
    /// </summary>
    /// <typeparam name="TBody"></typeparam>
    /// <returns></returns>
    public TBody DecodeBody<TBody>()
    {
        var packet = MemoryPackSerializer.Deserialize<TBody>(Body);

        ArgumentNullException.ThrowIfNull(packet);

        return packet;
    }
}