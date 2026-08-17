using WayCoder.UI.Tui.Screens;

namespace WayCoder.UI.Cli.Commands;

public class PermCommand : SlashCommand
{
    public override string Name => "/permissions";
    public override string[] Aliases => ["/perm"];
    public override string Description => "权限管理";
    public override string? Usage => "/perm [suggest|auto-edit|full-auto]";

    public override Task ExecuteAsync(string args, ChatScreen screen)
    {
        if (string.IsNullOrEmpty(args))
            screen.AddSystemMsg($"当前权限: {SandboxManager.Level}\n可选: suggest, auto-edit, full-auto");
        else
        {
            SandboxManager.SetLevel(args);
            screen.AddSystemMsg($"沙箱级别已切换: {SandboxManager.Level}");
        }
        return Task.CompletedTask;
    }
}
