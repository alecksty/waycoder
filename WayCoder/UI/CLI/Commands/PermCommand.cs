using WayCoder.UI.Tui.Screens;

namespace WayCoder.UI.Cli.Commands;

public class PermCommand : SlashCommand
{
    public override string Name => "/permissions";
    public override string[] Aliases => ["/perm"];
    public override string Description => L.Pick("沙箱边界管理（off/project/network-off/hard，独立于权限）", "Sandbox boundary management (off/project/network-off/hard, independent of permission)");
    public override string? Usage => "/perm [off|project|network-off|hard|suggest|auto-edit|full-auto]";

    public override Task ExecuteAsync(string args, ChatScreen screen)
    {
        if (string.IsNullOrEmpty(args))
            screen.AddSystemMsg(L.Pick($"当前沙箱边界: {SandboxManager.Level}\n", $"Current sandbox boundary: {SandboxManager.Level}\n") +
                L.Pick("  off           无边界\n  project       仅项目内写入\n  network-off   关闭网络\n  hard          仅项目内写 + 关网络\n", "  off           no boundary\n  project       writes inside the project only\n  network-off   network disabled\n  hard          writes inside the project only + network disabled\n") +
                L.Pick("  (兼容旧值: suggest→off, auto-edit→project, full-auto→hard)", "  (legacy values accepted: suggest→off, auto-edit→project, full-auto→hard)"));
        else
        {
            SandboxManager.SetLevel(args);
            screen.AddSystemMsg(L.Pick($"沙箱边界已切换: {SandboxManager.Level}\n（边界独立于权限；/permit 管确认）", $"Sandbox boundary switched: {SandboxManager.Level}\n(the boundary is independent of permission; /permit controls confirmation)"));
        }
        return Task.CompletedTask;
    }
}
