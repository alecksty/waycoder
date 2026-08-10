using WayCoder.Tools;
using WayCoder.UI.TuiScreens;

namespace WayCoder.Commands;

public class SearchCommand : SlashCommand
{
    public override string Name => "/search";
    public override string Description => "网页搜索";
    public override string? Usage => "/search <关键词>";

    public override async Task ExecuteAsync(string args, ChatScreen screen)
    {
        if (string.IsNullOrEmpty(args))
        {
            screen.AddSystemMsg("用法: /search <关键词>");
            return;
        }
        var tool = new WebSearchTool();
        var result = await tool.ExecuteAsync(new Dictionary<string, object?> { ["query"] = args });
        screen.AddSystemMsg($"🔍 搜索结果:\n{result}");
    }
}
