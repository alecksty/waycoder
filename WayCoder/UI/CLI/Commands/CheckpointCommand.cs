using WayCoder.UI.Tui.Screens;

namespace WayCoder.UI.Cli.Commands;

public class CheckpointCommand : SlashCommand
{
    public override string Name => "/checkpoint";
    public override string Description => L.Pick("创建检查点", "Create a checkpoint");

    public override async Task ExecuteAsync(string args, ChatScreen screen)
    {
        var label = string.IsNullOrEmpty(args) ? L.Pick("手动创建", "manual") : args;
        var cp = await CheckpointManager.CreateAsync(label);
        screen.AddSystemMsg(cp != null ? L.Pick($"📌 检查点已创建: #{cp.Id}", $"📌 Checkpoint created: #{cp.Id}") : L.Pick("检查点创建失败", "Failed to create the checkpoint"));
    }
}
