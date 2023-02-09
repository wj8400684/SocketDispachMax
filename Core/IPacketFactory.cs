using System;
using System.Buffers;
namespace Core;

internal interface IPacketFactory
{
    PlayPacket Decode(ref SequenceReader<byte> body);
}
