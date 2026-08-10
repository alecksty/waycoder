using WayCoder.Tools;
using WayCoder.UI.TuiScreens;

namespace WayCoder.Commands;

public class GitStatusCommand : SlashCommand
{
    public override string Name => "/git-status";
    public override string Description => "Git 状态";

    public override async Task ExecuteAsync(string args, ChatScreen screen)
    {
        var tool = new GitTool();
        var result = await tool.ExecuteAsync(new Dictionary<string, object?> { ["command"] = "status" });
        screen.AddSystemMsg(result);
    }
}
