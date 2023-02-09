namespace Core;

public interface IPacketAwaitable : IDisposable
{
    void Complete(PlayPacket packet);

    void Fail(Exception exception);

    void Cancel();
}