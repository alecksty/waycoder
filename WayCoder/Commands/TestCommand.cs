using WayCoder.UI.TuiScreens;

namespace WayCoder.Commands;

/// <summary>
/// /test — 统一自测命令（合并旧 RunTestDemo + SelfTest.RunToChat）
/// /test              → 显示帮助
/// /test all|tui|...  → SelfTest 模块测试
/// /test perm|toast   → TUI 控件演示
/// </summary>
public class TestCommand : SlashCommand
{
    public override string Name => "/test";
    public override string Description => "运行自测或 TUI 演示";
    public override string? Usage => "/test [all|tui|tools|git|config|memory|agent|review|mcp|system|perm|toast|menu]";

    public override Task ExecuteAsync(string args, ChatScreen screen)
    {
        args = args.Trim().ToLowerInvariant();

        // UI 演示
        switch (args)
        {
            case "perm" or "权限框":
                screen.ShowInlinePermission("bash",
                    "rm -rf /tmp/build",
                    "command: rm -rf /tmp/build\ncwd: /home/user/project",
                    isDangerous: true);
                return Task.CompletedTask;

            case "toast" or "提示框":
                screen.ShowToast("✅ 操作已完成 (2s 自动消失)", 2000);
                return Task.CompletedTask;

            case "menu" or "菜单":
                screen.ShowMenu("测试菜单", ["选项 A", "选项 B", "选项 C"]);
                return Task.CompletedTask;

            case "help" or "":
                screen.AddMessage(
                    "/test <模块>:\n" +
                    "  自测模块: all, tools, ui, git, config, memory, agent, review, mcp, system\n" +
                    "  TUI 演示: perm(权限框), toast(提示框), menu(菜单)",
                    "tool");
                return Task.CompletedTask;
        }

        // 自测模块
        var result = SelfTest.RunToChat(args.Length == 0 ? "all" : args);
        screen.AddMessage(result, "tool");
        return Task.CompletedTask;
    }
}
