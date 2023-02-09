using SuperSocket.Command;

namespace Server;

/// <summary>
/// 管理员命令过滤器
/// 只允许管理员session通过命令
/// </summary>
public sealed class PlayAdminCommandFilter : CommandFilterAttribute
{
    /// <summary>
    /// 命令准备执行中
    /// </summary>
    /// <param name="commandContext"></param>
    /// <returns></returns>
    public override bool OnCommandExecuting(CommandExecutingContext commandContext)
    {
        var session = (PlaySession)commandContext.Session;

        return session.IsAdmin;
    }

    /// <summary>
    /// 命令执行完毕
    /// </summary>
    /// <param name="commandContext"></param>
    public override void OnCommandExecuted(CommandExecutingContext commandContext)
    {
    }
}