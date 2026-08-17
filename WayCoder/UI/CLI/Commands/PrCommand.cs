using WayCoder.Tools;
using WayCoder.UI.Tui.Screens;

namespace WayCoder.UI.Cli.Commands;

public class PrCommand : SlashCommand
{
    public override string Name => "/pr";
    public override string Description => "创建 Pull Request";
    public override string? Usage => "/pr [标题]";

    public override async Task ExecuteAsync(string args, ChatScreen screen)
    {
        var tool = new GitPRTool();
        var dict = new Dictionary<string, object?>();
        if (!string.IsNullOrEmpty(args)) dict["title"] = args;
        var result = await tool.ExecuteAsync(dict);
        screen.AddSystemMsg(result);
    }
}
