using WayCoder.UI.TUI.Base;
using WayCoder.UI.Tui.Screens;

namespace WayCoder.UI.Cli.Commands;

/// <summary>
/// /diag —— 手动采集当前状态 / 死机现场快照到 logs/freeze_*.txt。
/// 复用在 FreezeCapture.DumpNow（与看门狗冻结触发同一套代码）。
/// 用途：任务变慢但还没死时主动留快照基线；死机恢复后跑一次对比；
/// 也便于用户脱离调试环境时把现场文件发给开发者。
/// </summary>
public class DiagCommand : SlashCommand
{
    public override string Name => "/diag";
    public override string Description => "手动采集当前状态/死机现场快照到 logs/";

    public override Task ExecuteAsync(string args, ChatScreen screen)
    {
        var path = FreezeCapture.DumpNow("手动 /diag", TuiManager.UiLoopActivity, 0);
        screen.AddSystemMsg(string.IsNullOrEmpty(path)
            ? "⚠ 状态采集失败（详见 logs/error_*.log）"
            : $"📋 状态快照已写入: {path}");
        return Task.CompletedTask;
    }
}
