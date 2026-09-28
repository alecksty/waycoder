using WayCoder.UI.Tui.Screens;

namespace WayCoder.UI.Cli.Commands;

public class TimelineCommand : SlashCommand
{
    public override string Name => "/timeline";
    public override string Description => L.Pick("回滚时间线（改坏可回滚）", "Rollback timeline (recover from a bad change)");

    public override Task ExecuteAsync(string args, ChatScreen screen)
    {
        var tl = CheckpointManager.ListTimeline();
        screen.AddSystemMsg(L.Pick("📜 **回滚时间线**\n", "📜 **Rollback timeline**\n") + tl +
            L.Pick("\n\n回退到某检查点：`/undo <id>` · 回退单个文件：`/undo <id> <文件路径>`", "\n\nRevert to a checkpoint: `/undo <id>` · Revert a single file: `/undo <id> <file path>`"));
        return Task.CompletedTask;
    }
}
