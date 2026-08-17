using WayCoder.UI.Tui.Screens;

namespace WayCoder.UI.Cli.Commands;

public class HistoryCommand : SlashCommand
{
    public override string Name => "/history";
    public override string Description => "搜索对话历史";
    public override string? Usage => "/history [关键词]";

    public override Task ExecuteAsync(string args, ChatScreen screen)
    {
        var agent = ProgramContext.Agent;
        if (agent == null) { screen.AddSystemMsg("Agent 未初始化"); return Task.CompletedTask; }

        if (string.IsNullOrEmpty(args))
        {
            screen.AddSystemMsg("用法: /history <关键词>  在对话历史中搜索");
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
            screen.AddSystemMsg($"未找到包含 \"{args}\" 的消息");
        else
        {
            var header = $"🔍 搜索 \"{args}\" ({results.Count} 条):";
            screen.AddSystemMsg(header + "\n" + string.Join("\n", results.Take(15)));
        }
        return Task.CompletedTask;
    }
}
