namespace Core;

public enum CommandKey : byte
{
    None,
    Authorize,
    AuthorizeAck,
    Login,
    LoginAck,
    AddOrder,
    AddOrderAck,
    Refresh,
    RefreshAck,
    Restart,
    RestartAck,
    HeartBeat,
    HeartBeatAck,
    Forward,
    ForwardAck,
    ClientOffline,
    ClientOnline
}