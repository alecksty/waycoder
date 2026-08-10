using WayCoder.Tools;
using WayCoder.UI.TuiScreens;

namespace WayCoder.Commands;

public class GitDiffCommand : SlashCommand
{
    public override string Name => "/git-diff";
    public override string Description => "Git 差异";

    public override async Task ExecuteAsync(string args, ChatScreen screen)
    {
        var tool = new GitTool();
        var result = await tool.ExecuteAsync(new Dictionary<string, object?> { ["command"] = "diff" });
        screen.AddSystemMsg(result);
    }
}
