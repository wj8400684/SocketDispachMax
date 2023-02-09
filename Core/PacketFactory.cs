using MemoryPack;
using System.Buffers;
using SuperSocket.ProtoBase;

namespace Core;

internal class PacketFactory<TPacket> : IPacketFactory
    where TPacket : PlayPacket
{
    public PlayPacket Decode(ref SequenceReader<byte> reader)
    {
        var result = MemoryPackSerializer.Deserialize<TPacket>(reader.UnreadSequence);

        if (result == null)
            throw new ProtocolException($"反序列化数据失败！字节长度：{reader.UnreadSequence.Length}");
      
        return result;
    }
}
