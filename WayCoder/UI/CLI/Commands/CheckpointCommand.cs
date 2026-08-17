using WayCoder.UI.Tui.Screens;

namespace WayCoder.UI.Cli.Commands;

public class CheckpointCommand : SlashCommand
{
    public override string Name => "/checkpoint";
    public override string Description => "创建检查点";

    public override async Task ExecuteAsync(string args, ChatScreen screen)
    {
        var label = string.IsNullOrEmpty(args) ? "手动创建" : args;
        var cp = await CheckpointManager.CreateAsync(label);
        screen.AddSystemMsg(cp != null ? $"📌 检查点已创建: #{cp.Id}" : "检查点创建失败");
    }
}
