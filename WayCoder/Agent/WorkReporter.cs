namespace WayCoder;

/// <summary>
/// 工作总结报告生成器 —— 在 Agent 完成一轮对话后自动生成结构化摘要。
///
/// 报告内容：
/// - 完成的任务清单（已创建/修改/删除的文件、已执行的命令）
/// - 未完成事项
/// - 潜在问题与建议
///
/// 格式：Markdown 文本，可直接展示或嵌入导出。
/// </summary>
public static class WorkReporter
{
    /// <summary>
    /// 生成工作总结报告。
    /// </summary>
    /// <param name="messages">本轮的 assistant + tool 消息列表</param>
    /// <param name="startedAt">本轮开始时间</param>
    /// <returns>Markdown 格式的报告</returns>
    public static string Generate(List<JsonObject>? messages, DateTime? startedAt = null)
    {
        if (messages == null || messages.Count == 0)
            return "_本轮无对话历史。_";

        var sb = new System.Text.StringBuilder();
        var elapsed = startedAt.HasValue ? DateTime.UtcNow - startedAt.Value : (TimeSpan?)null;

        // ── 头部 ──
        sb.AppendLine("# 📊 工作总结");
        sb.AppendLine();
        if (elapsed.HasValue)
            sb.AppendLine($"**耗时**：{FormatDuration(elapsed.Value)}  |  **消息数**：{messages.Count}  |  **时间**：{DateTime.Now:HH:mm:ss}");
        else
            sb.AppendLine($"**消息数**：{messages.Count}  |  **时间**：{DateTime.Now:HH:mm:ss}");
        sb.AppendLine();

        // ── 统计 ──
        var stats = CollectStats(messages);
        if (stats.TotalActions > 0)
        {
            sb.AppendLine("## 📈 活动统计");
            sb.AppendLine();
            sb.AppendLine("| 类别 | 数量 |");
            sb.AppendLine("|------|------|");
            if (stats.FilesCreated > 0) sb.AppendLine($"| 📝 创建文件 | {stats.FilesCreated} |");
            if (stats.FilesModified > 0) sb.AppendLine($"| ✏ 修改文件 | {stats.FilesModified} |");
            if (stats.FilesDeleted > 0) sb.AppendLine($"| 🗑 删除文件 | {stats.FilesDeleted} |");
            if (stats.FilesRead > 0) sb.AppendLine($"| 📖 读取文件 | {stats.FilesRead} |");
            if (stats.BashRuns > 0) sb.AppendLine($"| ⚙ 执行命令 | {stats.BashRuns} |");
            if (stats.Searches > 0) sb.AppendLine($"| 🔍 搜索操作 | {stats.Searches} |");
            if (stats.Errors > 0) sb.AppendLine($"| ❌ 错误 | {stats.Errors} |");
            sb.AppendLine();
        }

        // ── 工具调用详情 ──
        var toolCalls = ExtractToolCalls(messages);
        if (toolCalls.Count > 0)
        {
            sb.AppendLine("## 🔧 工具调用");
            sb.AppendLine();
            foreach (var tc in toolCalls.Take(30)) // 最多 30 条
            {
                sb.AppendLine($"- **{tc.Tool}**：{tc.Summary}");
            }
            if (toolCalls.Count > 30)
                sb.AppendLine($"- _... 还有 {toolCalls.Count - 30} 条调用_");
            sb.AppendLine();
        }

        // ── 文件变更清单 ──
        var changedFiles = ExtractChangedFiles(messages);
        if (changedFiles.Count > 0)
        {
            sb.AppendLine("## 📁 涉及文件");
            sb.AppendLine();
            foreach (var (path, action) in changedFiles)
            {
                var emoji = action switch
                {
                    "创建" or "create" => "📝",
                    "修改" or "edit" => "✏",
                    "删除" or "delete" => "🗑",
                    "读取" or "read" => "📖",
                    _ => "•",
                };
                sb.AppendLine($"- {emoji} `{path}` _{action}_");
            }
            sb.AppendLine();
        }

        // ── 任务进度 ──
        var progress = TaskProgress.GetSummary();
        if (!string.IsNullOrEmpty(progress) && progress != "⏳ 就绪")
        {
            sb.AppendLine("## 📋 任务进度");
            sb.AppendLine();
            sb.AppendLine(progress);
            sb.AppendLine();
        }

        sb.AppendLine("---");
        sb.AppendLine($"_由 WayCoder WorkReporter 自动生成 · {DateTime.Now:yyyy-MM-dd HH:mm:ss}_");

        return sb.ToString();
    }

    // ── 统计 ──

    private static WorkStats CollectStats(List<JsonObject> messages)
    {
        var stats = new WorkStats();
        foreach (var m in messages)
        {
            var role = m["role"]?.GetValue<string>() ?? "";
            if (role != "assistant") continue;

            var content = m["content"]?.GetValue<string>() ?? "";
            var toolCalls = m["tool_calls"]?.AsArray();
            if (toolCalls == null) continue;

            foreach (var tc in toolCalls)
            {
                stats.TotalActions++;
                var func = tc?["function"];
                var toolName = func?["name"]?.GetValue<string>() ?? "";
                var args = func?["arguments"]?.GetValue<string>() ?? "";

                switch (toolName)
                {
                    case "write_file": stats.FilesCreated++; break;
                    case "edit_file": stats.FilesModified++; break;
                    case "rm": stats.FilesDeleted++; break;
                    case "read_file": stats.FilesRead++; break;
                    case "bash": stats.BashRuns++; break;
                    case "grep" or "glob" or "ls": stats.Searches++; break;
                }
            }

            // 检测内容中的错误标记
            if (content.Contains("[ERROR]") || content.Contains("编译失败") || content.Contains("error CS"))
                stats.Errors++;
        }
        return stats;
    }

    private static List<(string Tool, string Summary)> ExtractToolCalls(List<JsonObject> messages)
    {
        var calls = new List<(string, string)>();
        foreach (var m in messages)
        {
            var role = m["role"]?.GetValue<string>() ?? "";
            if (role != "assistant") continue;

            var toolCalls = m["tool_calls"]?.AsArray();
            if (toolCalls == null) continue;

            foreach (var tc in toolCalls)
            {
                var func = tc?["function"];
                var toolName = func?["name"]?.GetValue<string>() ?? "?";
                var args = func?["arguments"]?.GetValue<string>() ?? "";

                var summary = SummarizeArgs(toolName, args);
                calls.Add((toolName, summary));
            }
        }
        return calls;
    }

    private static List<(string Path, string Action)> ExtractChangedFiles(List<JsonObject> messages)
    {
        var seen = new HashSet<string>();
        var files = new List<(string, string)>();

        foreach (var m in messages)
        {
            var toolCalls = m["tool_calls"]?.AsArray();
            if (toolCalls == null) continue;

            foreach (var tc in toolCalls)
            {
                var func = tc?["function"];
                var toolName = func?["name"]?.GetValue<string>() ?? "";
                var args = func?["arguments"]?.GetValue<string>() ?? "";

                var (path, action) = toolName switch
                {
                    "write_file" => (ExtractArg(args, "file_path"), "创建"),
                    "edit_file" => (ExtractArg(args, "file_path"), "修改"),
                    "rm" => (ExtractArg(args, "file_path"), "删除"),
                    "read_file" => (ExtractArg(args, "file_path"), "读取"),
                    "mv" => (ExtractArg(args, "file_path"), "移动"),
                    "cp" => (ExtractArg(args, "file_path"), "复制"),
                    _ => (null, null),
                };

                if (path != null && !seen.Contains(path))
                {
                    seen.Add(path);
                    files.Add((path, action!));
                }
            }
        }
        return files;
    }

    // ── 参数解析辅助 ──

    private static string SummarizeArgs(string toolName, string args)
    {
        return toolName switch
        {
            "bash" => ExtractArg(args, "command") ?? args.Truncate(60),
            "write_file" => $"→ {ExtractArg(args, "file_path") ?? "?"}",
            "edit_file" => $"→ {ExtractArg(args, "file_path") ?? "?"}",
            "read_file" => $"← {ExtractArg(args, "file_path") ?? "?"}",
            "grep" => $"🔍 {ExtractArg(args, "pattern") ?? "?"}",
            "glob" => $"🔍 {ExtractArg(args, "pattern") ?? "?"}",
            "rm" => $"🗑 {ExtractArg(args, "file_path") ?? "?"}",
            "agent" => $"🤖 {ExtractArg(args, "description") ?? ExtractArg(args, "prompt")?.Truncate(50) ?? "?"}",
            "web_search" => $"🌐 {ExtractArg(args, "query")?.Truncate(40) ?? "?"}",
            _ => args.Truncate(60),
        };
    }

    private static string? ExtractArg(string args, string key)
    {
        try
        {
            // 简单 JSON 字段提取（避免分配 JsonDocument）
            var search = $"\"{key}\"";
            var idx = args.IndexOf(search, StringComparison.Ordinal);
            if (idx < 0) return null;

            idx += search.Length;
            // 跳过冒号和空白
            while (idx < args.Length && (args[idx] == ':' || args[idx] == ' ' || args[idx] == '\t'))
                idx++;
            if (idx >= args.Length) return null;

            // 读字符串值
            if (args[idx] == '"')
            {
                idx++;
                var start = idx;
                while (idx < args.Length)
                {
                    if (args[idx] == '"' && args[idx - 1] != '\\')
                        break;
                    idx++;
                }
                var val = args[start..idx];
                // 反转义
                return val.Replace("\\\"", "\"").Replace("\\\\", "\\").Replace("\\n", " ");
            }
            return null;
        }
        catch { return null; }
    }

    private static string FormatDuration(TimeSpan d) =>
        d.TotalHours >= 1 ? $"{d.TotalHours:F1}h" :
        d.TotalMinutes >= 1 ? $"{d.TotalMinutes:F0}m{d.Seconds}s" :
        $"{d.Seconds}s";

    private struct WorkStats
    {
        public int TotalActions;
        public int FilesCreated;
        public int FilesModified;
        public int FilesDeleted;
        public int FilesRead;
        public int BashRuns;
        public int Searches;
        public int Errors;
    }
}

/// <summary>字符串截断扩展</summary>
internal static class StringExtensions
{
    public static string Truncate(this string s, int maxLen) =>
        s.Length <= maxLen ? s : s[..(maxLen - 1)] + "…";
}
