using Core;
using SuperSocket.Command;

namespace Server;

public sealed class PlayCommandAttribute : CommandAttribute
{
    public PlayCommandAttribute(CommandKey key)
    {
        Key = (byte)key;
    }
}