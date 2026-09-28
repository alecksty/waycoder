using WayCoder.Tools;
using WayCoder.UI.Tui.Screens;

namespace WayCoder.UI.Cli.Commands;

public class SearchCommand : SlashCommand
{
    public override string Name => "/search";
    public override string Description => L.Pick("网页搜索", "Web search");
    public override string? Usage => L.Pick("/search <关键词>", "/search <keywords>");

    public override async Task ExecuteAsync(string args, ChatScreen screen)
    {
        if (string.IsNullOrEmpty(args))
        {
            screen.AddSystemMsg(L.Pick("用法: /search <关键词>", "Usage: /search <keywords>"));
            return;
        }
        var tool = new WebSearchTool();
        var result = await tool.ExecuteAsync(new Dictionary<string, object?> { ["query"] = args });
        screen.AddSystemMsg(L.Pick($"🔍 搜索结果:\n{result}", $"🔍 Search results:\n{result}"));
    }
}
