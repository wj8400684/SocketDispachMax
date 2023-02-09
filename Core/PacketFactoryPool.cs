namespace Core;

internal class PacketFactoryPool
{
    private static IPacketFactory[]? _packetFactorys;

    public static void Inilizetion()
    {
        if (_packetFactorys != null)
            return;

        var commands = PlayPacket.GetCommands();

        _packetFactorys = new IPacketFactory[commands.Count + 1];

        //获取 PacketFactory <> 范型 type
        var type = typeof(PacketFactory<>);

        foreach (var command in commands)
        {
            var genericType = type.MakeGenericType(command.Key);

            if (Activator.CreateInstance(genericType) is not IPacketFactory packetFactory)
                continue;

            _packetFactorys[(int)command.Value] = packetFactory;
        }
    }

    public static IPacketFactory? Get(byte key)
    {
        ArgumentNullException.ThrowIfNull(_packetFactorys);

        return _packetFactorys[key];
    }
}
