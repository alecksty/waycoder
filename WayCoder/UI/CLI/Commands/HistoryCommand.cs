using WayCoder.UI.Tui.Screens;

namespace WayCoder.UI.Cli.Commands;

public class HistoryCommand : SlashCommand
{
    public override string Name => "/history";
    public override string Description => L.Pick("搜索对话历史", "Search the conversation history");
    public override string? Usage => L.Pick("/history [关键词]", "/history [keyword]");

    public override Task ExecuteAsync(string args, ChatScreen screen)
    {
        var agent = ProgramContext.Agent;
        if (agent == null) { screen.AddSystemMsg(L.Pick("Agent 未初始化", "Agent not initialized")); return Task.CompletedTask; }

        if (string.IsNullOrEmpty(args))
        {
            screen.AddSystemMsg(L.Pick("用法: /history <关键词>  在对话历史中搜索", "Usage: /history <keyword>  searches the conversation history"));
            return Task.CompletedTask;
        }

        var results = new List<string>();
        int idx = 0;
        foreach (var msg in agent.SnapshotMessages())
        {
            var content = msg["content"]?.AsString() ?? "";
            if (content.Contains(args, StringComparison.OrdinalIgnoreCase))
            {
                var preview = content.Length > 80 ? ContextManager.TruncateByRunes(content, 80) + "..." : content;
                results.Add($"  [{idx}] {preview}");
            }
            idx++;
        }

        if (results.Count == 0)
            screen.AddSystemMsg(L.Pick($"未找到包含 \"{args}\" 的消息", $"No messages containing \"{args}\" were found"));
        else
        {
            var header = L.Pick($"🔍 搜索 \"{args}\" ({results.Count} 条):", $"🔍 Search \"{args}\" ({results.Count} matches):");
            screen.AddSystemMsg(header + "\n" + string.Join("\n", results.Take(15)));
        }
        return Task.CompletedTask;
    }
}
