namespace WayCoder.Tools;

/// <summary>
/// 对话导出工具 —— 将 Agent 对话历史导出为 Markdown / JSON / HTML。
/// 在 ToolRegistry 注册后，Agent 可在用户请求时调用。
/// </summary>
public class ExportTool : ITool
{
    public string Name => "export_chat";

    public string Description => "将当前 Agent 对话历史导出为文件。支持 Markdown（按角色分段）、JSON（原始消息列表）、HTML（带样式的网页）。默认导出到当前目录。";

    public JNode Parameters => JNode.Object()
        .Set("type", "object")
        .Set("properties", JNode.Object()
            .Set("format", JNode.Object()
                .Set("type", "string")
                .Set("enum", JNode.Array().Add("md").Add("json").Add("html"))
                .Set("description", "导出格式: md(Markdown), json(JSON数组), html(网页)"))
            .Set("output_path", JNode.Object()
                .Set("type", "string")
                .Set("description", "输出文件路径（可选，默认 chat_export_{timestamp}.{format}）")))
        .Set("required", JNode.Array().Add("format"));

    /// <summary>消息历史引用（由 Agent 在构造后注入）</summary>
    public List<JNode>? Messages { get; set; }

    public Task<string> ExecuteAsync(Dictionary<string, object?> arguments)
    {
        var format = arguments.GetValueOrDefault("format")?.ToString() ?? "md";
        var outputPath = arguments.GetValueOrDefault("output_path")?.ToString();

        if (Messages == null || Messages.Count == 0)
            return Task.FromResult("错误：没有可导出的对话历史");

        try
        {
            var timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
            var content = format switch
            {
                "json" => ExportJson(),
                "html" => ExportHtml(),
                _ => ExportMarkdown(),
            };

            outputPath ??= $"chat_export_{timestamp}.{format switch
            {
                "json" => "json",
                "html" => "html",
                _ => "md",
            }}";

            File.WriteAllText(outputPath, content);
            var size = new FileInfo(outputPath).Length;
            return Task.FromResult($"✅ 已导出 {Messages.Count} 条消息到 {outputPath} ({FormatSize(size)})");
        }
        catch (Exception ex)
        {
            return Task.FromResult($"导出失败：{ex.Message}");
        }
    }

    private string ExportMarkdown()
    {
        var sb = new System.Text.StringBuilder();
        sb.AppendLine("# WayCoder 对话导出");
        sb.AppendLine($"- 导出时间：{DateTime.Now:yyyy-MM-dd HH:mm:ss}");
        sb.AppendLine($"- 消息数：{Messages!.Count}");
        sb.AppendLine();
        sb.AppendLine("---");
        sb.AppendLine();

        foreach (var m in Messages!)
        {
            var role = m["role"]?.AsString() ?? "?";
            var content = m["content"]?.AsString() ?? "";
            var icon = role switch
            {
                "user" => "👤", "assistant" => "🤖", "system" => "⚙",
                "tool" => "🔧", _ => "❓",
            };

            sb.AppendLine($"### {icon} {RoleDisplayName(role)}");
            sb.AppendLine();

            if (role == "tool")
            {
                var toolId = m["tool_call_id"]?.AsString() ?? "";
                sb.AppendLine($"_工具调用 ID: {toolId}_");
                sb.AppendLine();
                sb.AppendLine("```");
                sb.AppendLine(TruncateContent(content, 3000));
                sb.AppendLine("```");
            }
            else
            {
                sb.AppendLine(TruncateContent(content, 5000));
            }
            sb.AppendLine();
            sb.AppendLine("---");
            sb.AppendLine();
        }
        return sb.ToString();
    }

    private string ExportJson()
    {
        var arr = JNode.Array();
        foreach (var m in Messages!)
        {
            var obj = JNode.Object();
            obj.Set("role", m["role"]?.Clone() ?? JNode.Null());
            obj.Set("content", m["content"]?.Clone() ?? JNode.Null());
            if (m["tool_call_id"] != null)
                obj.Set("tool_call_id", m["tool_call_id"]!.Clone() ?? JNode.Null());
            arr.Add(obj);
        }
        return arr.ToJson();
    }

    private string ExportHtml()
    {
        var sb = new System.Text.StringBuilder();
        sb.AppendLine("<!DOCTYPE html><html><head><meta charset=\"UTF-8\">");
        sb.AppendLine("<title>WayCoder 对话</title>");
        sb.AppendLine("<style>");
        sb.AppendLine("body{font-family:system-ui,sans-serif;max-width:900px;margin:0 auto;padding:20px;background:#1a1a2e;color:#e0e0e0;}");
        sb.AppendLine(".user{border-left:3px solid #4ecdc4;padding:10px 20px;margin:10px 0;background:#16213e;}");
        sb.AppendLine(".assistant{border-left:3px solid #6c5ce7;padding:10px 20px;margin:10px 0;background:#1a1a2e;}");
        sb.AppendLine(".tool{border-left:3px solid #fdcb6e;padding:10px 20px;margin:10px 0;background:#2d2d2d;font-family:monospace;font-size:0.9em;white-space:pre-wrap;}");
        sb.AppendLine(".system{border-left:3px solid #636e72;padding:10px 20px;margin:10px 0;background:#2d3436;font-style:italic;}");
        sb.AppendLine("pre{background:#0f0f23;padding:10px;border-radius:4px;overflow-x:auto;}");
        sb.AppendLine("code{font-family:'Fira Code',monospace;}");
        sb.AppendLine("</style></head><body>");
        sb.AppendLine($"<h1>🦀 WayCoder 对话</h1><p>{DateTime.Now:yyyy-MM-dd HH:mm} | {Messages!.Count} 条消息</p>");

        foreach (var m in Messages!)
        {
            var role = m["role"]?.AsString() ?? "?";
            var content = m["content"]?.AsString() ?? "";
            var escaped = System.Net.WebUtility.HtmlEncode(content);
            sb.AppendLine($"<div class=\"{role}\"><strong>{RoleDisplayName(role)}</strong><br>{escaped}</div>");
        }

        sb.AppendLine("</body></html>");
        return sb.ToString();
    }

    private static string TruncateContent(string content, int maxLen)
        => content.Length <= maxLen ? content : content[..maxLen] + "\n\n... (已截断)";

    /// <summary>角色英文标识 → 中文显示名（导出文件里角色标题中文化）。</summary>
    private static string RoleDisplayName(string role) => role switch
    {
        "user" => "用户",
        "assistant" => "智能体",
        "system" => "系统",
        "tool" => "工具",
        _ => role,
    };

    private static string FormatSize(long bytes) => bytes switch
    {
        < 1024 => $"{bytes} B",
        < 1024 * 1024 => $"{bytes / 1024.0:F1} KB",
        _ => $"{bytes / (1024.0 * 1024):F1} MB",
    };
}
