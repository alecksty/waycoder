using WayCoder.UI.Tui.Screens;

namespace WayCoder.UI.Cli.Commands;

/// <summary>
/// /exit — 退出 WayCoder（/quit / /退出）。设退出标志，走 REPL 正常清理路径（保存会话 + 退出全屏）。
/// </summary>
public class ExitCommand : SlashCommand
{
    public override string Name => "/exit";
    public override string[] Aliases => ["/quit", "/退出"];
    public override string Description => "退出 WayCoder";
    public override string? Usage => "/exit";

    public override Task ExecuteAsync(string args, ChatScreen screen)
    {
        screen.AddSystemMsg("👋 再见，正在保存并退出…");
        Program.RequestExit();
        return Task.CompletedTask;
    }
}
