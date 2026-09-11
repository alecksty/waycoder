using System.Text;

namespace WayCoder.Infra;

/// <summary>
/// Claude Code 会话（JSONL）解析 —— **唯一实现**。
///
/// 此前两处各写一份：<c>Infra/ContextBridge.ParseClaude</c>（把外部会话导入当前上下文，
/// 还要取工具调用与摘要）与 <c>Infra/ImportHelper.ParseClaudeJsonl</c>（导入成 WayCoder 消息），
/// 规则相同却各写各的，连文本提取都有两份（<c>ExtractText</c> / <c>ExtractClaudeText</c>）。
/// 外部格式一旦变化（新增块类型、改字段名），就要同时改两处 —— 漏一处就是
/// 「同一个会话文件，两个入口读出不同内容」。
///
/// 这里只负责「JSONL → 记录序列」的解析；两个调用方各取所需（一个要工具调用与摘要，
/// 一个只要正文），差异留在调用侧。
/// </summary>
public static class ClaudeSessionParser
{
    /// <summary>
    /// 一条解析结果。<see cref="Kind"/> 取 <c>user</c> / <c>assistant</c> / <c>tool</c> / <c>summary</c>；
    /// 仅 <c>tool</c> 会带 <see cref="ToolName"/> / <see cref="ToolInput"/>。
    /// </summary>
    public readonly record struct Entry(string Kind, string Text, string? ToolName = null, JNode? ToolInput = null);

    /// <summary>
    /// 逐行解析 JSONL；跳过侧链消息（<c>isSidechain</c> = 子智能体）、无法解析的行，
    /// 以及无正文的空记录。文件不存在/读取失败返回已解析部分（不抛）。
    /// </summary>
    public static List<Entry> Parse(string file)
    {
        var list = new List<Entry>();
        try
        {
            foreach (var raw in File.ReadLines(file, Encoding.UTF8))
            {
                var node = Json.Parse(raw);
                if (node == null) continue;
                var type = node.GetString("type");
                if (type == null) continue;

                switch (type)
                {
                    case "user":
                        if (node.GetBool("isSidechain")) continue; // 侧链 = 子智能体消息
                        if (ExtractText(node["message"]?["content"]) is { } utext && !string.IsNullOrWhiteSpace(utext))
                            list.Add(new Entry("user", utext));
                        break;

                    case "assistant":
                        var content = node["message"]?["content"];
                        if (content == null) break;
                        if (content.Kind == JKind.Array)
                        {
                            foreach (var block in content.Items)
                            {
                                var bt = block?.GetString("type");
                                if (bt == "text")
                                {
                                    if (block!.GetString("text") is { } t && !string.IsNullOrWhiteSpace(t))
                                        list.Add(new Entry("assistant", t));
                                }
                                else if (bt == "tool_use")
                                {
                                    var name = block!.GetString("name") ?? "工具";
                                    list.Add(new Entry("tool", name, name, block["input"]));
                                }
                            }
                        }
                        else if (ExtractText(content) is { } atext && !string.IsNullOrWhiteSpace(atext))
                        {
                            list.Add(new Entry("assistant", atext));
                        }
                        break;

                    case "summary":
                        if (node.GetString("summary") is { } summary && !string.IsNullOrWhiteSpace(summary))
                            list.Add(new Entry("summary", summary));
                        break;
                }
            }
        }
        catch { /* 读一半/编码异常：返回已解析部分 */ }
        return list;
    }

    /// <summary>
    /// 提取 Claude content 的纯文本：字符串直接返回；<c>[{type:"text",text}]</c> 数组则拼接其中的
    /// text 块（多块用换行连接）；其它形态退回 <c>AsString()</c>。无文本返回 null。
    /// </summary>
    public static string? ExtractText(JNode? content)
    {
        if (content == null) return null;
        if (content.Kind == JKind.String) return content.AsString();
        if (content.Kind == JKind.Array)
        {
            var parts = new List<string>();
            foreach (var block in content.Items)
                if (block?.GetString("type") == "text" && block.GetString("text") is { } t)
                    parts.Add(t);
            return parts.Count > 0 ? string.Join("\n", parts) : null;
        }
        return content.AsString();
    }
}
