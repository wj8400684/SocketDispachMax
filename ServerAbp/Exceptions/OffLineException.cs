namespace Server;

public sealed class OffLineException : Exception
{
    public OffLineException(string message)
        : base(message)
    {
    }
}