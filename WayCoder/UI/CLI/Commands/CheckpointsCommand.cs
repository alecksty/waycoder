using WayCoder.UI.Tui.Screens;

namespace WayCoder.UI.Cli.Commands;

public class CheckpointsCommand : SlashCommand
{
    public override string Name => "/checkpoints";
    public override string Description => "列出检查点";

    public override Task ExecuteAsync(string args, ChatScreen screen)
    {
        var cps = CheckpointManager.ListCheckpoints();
        screen.AddSystemMsg("📌 **检查点列表**\n" + cps);
        return Task.CompletedTask;
    }
}
