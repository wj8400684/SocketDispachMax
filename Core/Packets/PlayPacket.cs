using System.Buffers;
using System.Data;
using MemoryPack;
using SuperSocket.ProtoBase;

namespace Core;

public abstract class PlayPacket : IKeyedPackageInfo<CommandKey>
{
    private readonly Type _type;
    private static readonly Dictionary<Type, CommandKey> CommandTypes = new();

    #region command inilizetion

    internal static void LoadAllCommand()
    {
        var packets = typeof(PlayPacket).Assembly.GetTypes() //获取当前类库下所有类型
                                                 .Where(t => typeof(PlayPacket).IsAssignableFrom(t)) //获取间接或直接继承t的所有类型
                                                 .Where(t => !t.IsAbstract && t.IsClass) //获取非抽象类 排除接口继承
                                                 .Select(t => (PlayPacket?)Activator.CreateInstance(t)); //创造实例，并返回结果（项目需求，可删除）

        using var enumerator = packets.GetEnumerator();
        while (enumerator.MoveNext())
        {
            if (enumerator.Current != null)
                CommandTypes.TryAdd(enumerator.Current.GetType(), enumerator.Current.Key);
        }
    }

    public static CommandKey GetCommandKey<TPacket>()
    {
        var type = typeof(TPacket);

        if (!CommandTypes.TryGetValue(type, out var key))
            throw new Exception($"{type.Name} 未继承PlayPacket");

        return key;
    }

    public static List<KeyValuePair<Type, CommandKey>> GetCommands()
    {
        return CommandTypes.ToList();
    }

    static PlayPacket()
    {
        LoadAllCommand();
    }

    #endregion

    protected PlayPacket(CommandKey key)
    {
        Key = key;
        _type = GetType();
    }

    [MemoryPackIgnore]
    public CommandKey Key { get; set; }

    public virtual byte[] Encode()
    {
        return MemoryPackSerializer.Serialize(_type, this);
    }

    public virtual int Encode(IBufferWriter<byte> bufWriter)
    {
        using var state = MemoryPackWriterOptionalStatePool.Rent(MemoryPackSerializerOptions.Utf8);
        var writer = new MemoryPackWriter<IBufferWriter<byte>>(ref bufWriter, state);
        writer.WriteValue(_type, this);
        var writtenCount = writer.WrittenCount;
        writer.Flush();

        return writtenCount;
    }
}

public abstract class PlayRespPacket : PlayPacket
{
    public string? ErrorMessage { get; set; }

    public bool SuccessFul { get; set; }

    protected PlayRespPacket(CommandKey key)
        : base(key)
    {
    }
}

public abstract class PlayPacketWithIdentifier : PlayPacket
{
    public ulong Identifier { get; set; }

    protected PlayPacketWithIdentifier(CommandKey key) : base(key)
    {
    }
}

public abstract class PlayPacketRespWithIdentifier : PlayPacketWithIdentifier
{
    public string? ErrorMessage { get; set; }

    public bool SuccessFul { get; set; }

    protected PlayPacketRespWithIdentifier(CommandKey key) : base(key)
    {
    }
}