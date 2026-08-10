using WayCoder.UI.TuiScreens;

namespace WayCoder.Commands;

public class ExportCommand : SlashCommand
{
    public override string Name => "/export";
    public override string Description => "导出对话为 Markdown";

    public override Task ExecuteAsync(string args, ChatScreen screen)
    {
        var agent = ProgramContext.Agent;
        if (agent == null) { screen.AddSystemMsg("Agent 未初始化"); return Task.CompletedTask; }

        var dir = Global.WriteConfigPath(Environment.CurrentDirectory);
        Directory.CreateDirectory(dir);
        var filename = $"export_{DateTime.Now:yyyyMMdd_HHmmss}.md";
        var path = Path.Combine(dir, filename);

        var sb = new System.Text.StringBuilder();
        sb.AppendLine($"# WayCoder 对话导出");
        sb.AppendLine($"> {DateTime.Now:yyyy-MM-dd HH:mm}");
        sb.AppendLine();
        foreach (var msg in agent.Messages)
        {
            var role = msg["role"]?.GetValue<string>() ?? "?";
            var content = msg["content"]?.GetValue<string>() ?? "";
            sb.AppendLine($"## {role}");
            sb.AppendLine(content);
            sb.AppendLine();
        }

        File.WriteAllText(path, sb.ToString(), System.Text.Encoding.UTF8);
        screen.AddSystemMsg($"📄 已导出: {filename}");
        return Task.CompletedTask;
    }
}
