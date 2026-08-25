using WayCoder.UI.Tui.Screens;

namespace WayCoder.UI.Cli.Commands;

public class PermCommand : SlashCommand
{
    public override string Name => "/permissions";
    public override string[] Aliases => ["/perm"];
    public override string Description => "沙箱边界管理（off/project/network-off/hard，独立于权限）";
    public override string? Usage => "/perm [off|project|network-off|hard|suggest|auto-edit|full-auto]";

    public override Task ExecuteAsync(string args, ChatScreen screen)
    {
        if (string.IsNullOrEmpty(args))
            screen.AddSystemMsg($"当前沙箱边界: {SandboxManager.Level}\n" +
                "  off           无边界\n  project       仅项目内写入\n  network-off   关闭网络\n  hard          仅项目内写 + 关网络\n" +
                "  (兼容旧值: suggest→off, auto-edit→project, full-auto→hard)");
        else
        {
            SandboxManager.SetLevel(args);
            screen.AddSystemMsg($"沙箱边界已切换: {SandboxManager.Level}\n（边界独立于权限；/permit 管确认）");
        }
        return Task.CompletedTask;
    }
}
