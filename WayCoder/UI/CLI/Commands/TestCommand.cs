#if WAYCODER_TEST
using WayCoder.UI.Tui.Screens;

namespace WayCoder.UI.Cli.Commands;

/// <summary>
/// /test — 统一自测命令（仅 Debug/开发版编译，Release 不含 test 指令）
/// /test              → 显示帮助
/// /test all|tui|...  → SelfTest 模块测试
/// /test perm|toast   → TUI 控件演示
/// </summary>
public class TestCommand : SlashCommand
{
    public override string Name => "/test";
    public override string Description => L.Pick("运行自测或 TUI 演示", "Run the self-test suite or TUI demos");
    public override string? Usage => L.Pick("/test [all|tui|tools|…|dialog[ 名字]|perm|toast|menu]", "/test [all|tui|tools|…|dialog[ name]|perm|toast|menu]");

    public override async Task ExecuteAsync(string args, ChatScreen screen)
    {
        args = args.Trim().ToLowerInvariant();

        // 对话框巡检：/test dialog 挨个弹一遍，/test dialog <名字> 只弹一个
        if (args is "dialog" or "对话框" || args.StartsWith("dialog ", StringComparison.Ordinal))
        {
            DialogWalk.Run(screen, args.StartsWith("dialog ", StringComparison.Ordinal) ? args[7..] : "");
            return;
        }

        // UI 演示
        switch (args)
        {
            case "perm" or "权限框":
                screen.ShowPermissionDialog("bash",
                    "rm -rf /tmp/build",
                    "command: rm -rf /tmp/build\ncwd: /home/user/project",
                    isDangerous: true);
                return;

            case "toast" or "提示框":
                screen.ShowToast(L.Pick("✅ 操作已完成 (2s 自动消失)", "✅ Operation completed (disappears in 2s)"), 2000);
                return;

            case "menu" or "菜单":
                screen.ShowMenu(L.Pick("测试菜单", "Test menu"), [L.Pick("选项 A", "Option A"), L.Pick("选项 B", "Option B"), L.Pick("选项 C", "Option C")]);
                return;

            case "help" or "":
                screen.AddMessage(
                    L.Pick("/test <模块>:\n", "/test <module>:\n") +
                    L.Pick("  自测模块: all, tools, ui, git, config, memory, agent, review, mcp, system\n", "  Self-test modules: all, tools, ui, git, config, memory, agent, review, mcp, system\n") +
                    L.Pick("  对话框巡检: dialog（21 个挨个弹一遍）, dialog <名字>（只弹一个）\n", "  Dialog walk: dialog (opens all 21 one by one), dialog <name> (opens just one)\n") +
                    L.Pick("    名字: ", "    Names: ") + string.Join(", ", DialogWalk.Targets) + "\n" +
                    L.Pick("  TUI 演示: perm(权限框), toast(提示框), menu(菜单)", "  TUI demos: perm (permission dialog), toast (toast), menu (menu)"),
                    "tool");
                return;
        }

        // 自测模块：后台线程跑（/test all 同步 20-30s 会阻塞 UI 主循环 → 界面卡死），
        // UI 线程保持渲染 + 读键（可继续打字/操作），完成后结果回写聊天
        var module = args.Length == 0 ? "all" : args;
        var result = await Program.RunWithUiLoop(() => SelfTest.RunToChat(module), screen);
        screen.AddMessage(result, "tool");
    }
}
#endif // WAYCODER_TEST
