using System.Buffers;
using System.Buffers.Binary;
using SuperSocket.ProtoBase;

namespace Core;

public sealed class PlayPacketEncode : IPackageEncoder<PlayPacket>
{
    private const int HeaderSize = sizeof(short);

    public int Encode(IBufferWriter<byte> writer, PlayPacket pack)
    {
        #region 获取头部字节缓冲区

        var headSpan = writer.GetSpan(HeaderSize);
        writer.Advance(HeaderSize);

        #endregion

        #region 写入 command

        var length = writer.WriteLittleEndian((byte)pack.Key);

        #endregion

        #region 写入内容

        length += pack.Encode(writer);

        #endregion

        #region 写入 body 的长度

        BinaryPrimitives.WriteInt16LittleEndian(headSpan, (short)length);

        #endregion

        return HeaderSize + length;

        var body = pack.Encode();

        writer.WriteLittleEndian((short)body.Length); // body 的长度
        writer.Write(body); // 内容

        return HeaderSize + body.Length;
    }
}