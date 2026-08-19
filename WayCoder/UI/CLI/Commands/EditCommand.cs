using WayCoder.UI.Shared;
using WayCoder.UI.Tui;
using WayCoder.UI.TUI.Base;
using WayCoder.UI.Tui.Screens;

namespace WayCoder.UI.Cli.Commands;

public class EditCommand : SlashCommand
{
    public override string Name => "/edit";
    public override string Description => "终端源码编辑器";
    public override string? Usage => "/edit [文件路径] [--readonly|-r]  (--readonly=只读查看，禁止修改)";

    public override Task ExecuteAsync(string args, ChatScreen screen)
    {
        // 解析只读标志：/edit <文件> --readonly 或 -r（只读查看，禁止修改文件）
        var readOnly = args.Contains("--readonly", StringComparison.OrdinalIgnoreCase)
                    || args.Contains("-r", StringComparison.OrdinalIgnoreCase)
                    || WorkModeManager.CurrentMode == WorkMode.Plan; // Plan 模式默认只读（只读分析）
        var file = args
            .Replace("--readonly", "", StringComparison.OrdinalIgnoreCase)
            .Replace("-r", "", StringComparison.OrdinalIgnoreCase)
            .Trim();

        TuiManager.Instance.PushScreen(string.IsNullOrEmpty(file)
            ? new EditorScreen(readOnly: readOnly)
            : new EditorScreen(file, readOnly));
        return Task.CompletedTask;
    }
}
