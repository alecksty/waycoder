using WayCoder.UI.Tui.Screens;

namespace WayCoder.UI.Cli.Commands;

public class TimelineCommand : SlashCommand
{
    public override string Name => "/timeline";
    public override string Description => "回滚时间线（改坏可回滚）";

    public override Task ExecuteAsync(string args, ChatScreen screen)
    {
        var tl = CheckpointManager.ListTimeline();
        screen.AddSystemMsg("📜 **回滚时间线**\n" + tl +
            "\n\n回退到某检查点：`/undo <id>` · 回退单个文件：`/undo <id> <文件路径>`");
        return Task.CompletedTask;
    }
}
