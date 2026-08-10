using CoreCoderSharp.Tools;
using CoreCoderSharp.UI.TuiScreens;

namespace CoreCoderSharp.Commands;

public class GitLogCommand : SlashCommand
{
    public override string Name => "/git-log";
    public override string Description => "Git 日志";

    public override async Task ExecuteAsync(string args, ChatScreen screen)
    {
        var tool = new GitTool();
        var result = await tool.ExecuteAsync(new Dictionary<string, object?> { ["command"] = "log -10" });
        screen.AddSystemMsg(result);
    }
}
