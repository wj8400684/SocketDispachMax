using System.Buffers;
using SuperSocket.ProtoBase;

namespace Core;

/// <summary>
/// | bodyLength | body |
/// | header | cmd | body |
/// </summary>
public sealed class PlayPipeLineFilter : FixedHeaderPipelineFilter<PlayPacket>
{
    private const byte HeaderLength = 2;

    public PlayPipeLineFilter()
        : base(HeaderLength)
    {
        PacketFactoryPool.Inilizetion();
    }

    protected override PlayPacket DecodePackage(ref ReadOnlySequence<byte> buffer)
    {
        var reader = new SequenceReader<byte>(buffer.Slice(HeaderLength));

        //读取 command
        reader.TryRead(out var command);

        var factory = PacketFactoryPool.Get(command);

        if (factory == null)
            throw new ProtocolException($"命令：{command}未注册");

        return factory.Decode(ref reader);
    }

    protected override int GetBodyLengthFromHeader(ref ReadOnlySequence<byte> buffer)
    {
        var reader = new SequenceReader<byte>(buffer);

        reader.TryReadLittleEndian(out short bodyLength);

        return bodyLength;
    }
}