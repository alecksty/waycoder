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
    public static string Generate(List<JNode>? messages, DateTime? startedAt = null)
    {
        if (messages == null || messages.Count == 0)
            return L.Pick("_本轮无对话历史。_", "_No conversation history for this round._");

        var sb = new System.Text.StringBuilder();
        var elapsed = startedAt.HasValue ? DateTime.UtcNow - startedAt.Value : (TimeSpan?)null;

        // ── 头部 ──
        sb.AppendLine(L.Pick("# 📊 工作总结", "# 📊 Work summary"));
        sb.AppendLine();
        if (elapsed.HasValue)
            sb.AppendLine(L.Pick($"**耗时**：{FormatDuration(elapsed.Value)}  |  **消息数**：{messages.Count}  |  **时间**：{DateTime.Now:HH:mm:ss}",
                $"**Elapsed**: {FormatDuration(elapsed.Value)}  |  **Messages**: {messages.Count}  |  **Time**: {DateTime.Now:HH:mm:ss}"));
        else
            sb.AppendLine(L.Pick($"**消息数**：{messages.Count}  |  **时间**：{DateTime.Now:HH:mm:ss}",
                $"**Messages**: {messages.Count}  |  **Time**: {DateTime.Now:HH:mm:ss}"));
        sb.AppendLine();

        // ── 统计 ──
        var stats = CollectStats(messages);
        if (stats.TotalActions > 0)
        {
            sb.AppendLine(L.Pick("## 📈 活动统计", "## 📈 Activity"));
            sb.AppendLine();
            sb.AppendLine(L.Pick("| 类别 | 数量 |", "| Category | Count |"));
            sb.AppendLine("|------|------|");
            if (stats.FilesCreated > 0) sb.AppendLine(L.Pick($"| 📝 创建文件 | {stats.FilesCreated} |", $"| 📝 Files created | {stats.FilesCreated} |"));
            if (stats.FilesModified > 0) sb.AppendLine(L.Pick($"| ✏ 修改文件 | {stats.FilesModified} |", $"| ✏ Files modified | {stats.FilesModified} |"));
            if (stats.FilesDeleted > 0) sb.AppendLine(L.Pick($"| 🗑️ 删除文件 | {stats.FilesDeleted} |", $"| 🗑️ Files deleted | {stats.FilesDeleted} |"));
            if (stats.FilesRead > 0) sb.AppendLine(L.Pick($"| 📖 读取文件 | {stats.FilesRead} |", $"| 📖 Files read | {stats.FilesRead} |"));
            if (stats.BashRuns > 0) sb.AppendLine(L.Pick($"| ⚙️ 执行命令 | {stats.BashRuns} |", $"| ⚙️ Commands run | {stats.BashRuns} |"));
            if (stats.Searches > 0) sb.AppendLine(L.Pick($"| 🔍 搜索操作 | {stats.Searches} |", $"| 🔍 Searches | {stats.Searches} |"));
            if (stats.Errors > 0) sb.AppendLine(L.Pick($"| ❌ 错误 | {stats.Errors} |", $"| ❌ Errors | {stats.Errors} |"));
            sb.AppendLine();
        }

        // ── 工具调用详情 ──
        var toolCalls = ExtractToolCalls(messages);
        if (toolCalls.Count > 0)
        {
            sb.AppendLine(L.Pick("## 🔧 工具调用", "## 🔧 Tool calls"));
            sb.AppendLine();
            var toolSep = L.Pick("：", ": ");
            foreach (var tc in toolCalls.Take(30)) // 最多 30 条
            {
                sb.AppendLine($"- **{tc.Tool}**{toolSep}{tc.Summary}");
            }
            if (toolCalls.Count > 30)
                sb.AppendLine(L.Pick($"- _... 还有 {toolCalls.Count - 30} 条调用_",
                    $"  - _... {toolCalls.Count - 30} more call(s)_"));
            sb.AppendLine();
        }

        // ── 文件变更清单 ──
        var changedFiles = ExtractChangedFiles(messages);
        if (changedFiles.Count > 0)
        {
            sb.AppendLine(L.Pick("## 📁 涉及文件", "## 📁 Files touched"));
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
        // ⚠ 判据用 `HasProgress`（数据），**不是**拿 `GetSummary()` 的文案去比 ——
        //   此前写的是 `progress != "⏳ 就绪"`，而 GetSummary 从不返回那个串 ⇒ 恒真
        //   ⇒ 每轮都塞一段「## 📋 任务进度 / （尚无进度记录）」。见 TaskProgress.HasProgress。
        var progress = TaskProgress.GetSummary();
        if (TaskProgress.HasProgress)
        {
            sb.AppendLine(L.Pick("## 📋 任务进度", "## 📋 Task progress"));
            sb.AppendLine();
            sb.AppendLine(progress);
            sb.AppendLine();
        }

        sb.AppendLine("---");
        sb.AppendLine(L.Pick($"_由 WayCoder WorkReporter 自动生成 · {DateTime.Now:yyyy-MM-dd HH:mm:ss}_",
            $"_Generated automatically by WayCoder WorkReporter · {DateTime.Now:yyyy-MM-dd HH:mm:ss}_"));

        return sb.ToString();
    }

    // ── 统计 ──

    private static WorkStats CollectStats(List<JNode> messages)
    {
        var stats = new WorkStats();
        foreach (var m in messages)
        {
            var role = m["role"]?.AsString() ?? "";
            if (role != "assistant") continue;

            var content = m["content"]?.AsString() ?? "";
            var toolCalls = m["tool_calls"];
            if (toolCalls == null) continue;

            foreach (var tc in toolCalls.Items)
            {
                stats.TotalActions++;
                var func = tc["function"];
                var toolName = func?["name"]?.AsString() ?? "";
                var args = func?["arguments"]?.AsString() ?? "";

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

            // 检测内容中的错误标记。
            //
            // ⚠ **中英都要认**（`Lang.cs` 的公理 A2：机器可读标记永远双语识别，与界面语言无关）。
            //   `编译失败` 这一条来自 `MauiVml` 的编译失败前缀，而那句文案**会随界面语言变**
            //   （英文界面下是 `⚠️ Compile failed: `）—— 只认中文的后果是：英文会话里
            //   工作汇报（`.waycoder/reports/latest.md`）的错误数**静默少算**，
            //   不报错、不留痕，只是数字变小。这条是 2026-09-28 双语化时差点漏掉的。
            //   ⚠ 以后凡"拿文案当判据"的地方，都要按 A2 把两侧都列上。
            if (content.Contains("[ERROR]") || content.Contains("error CS")
                || content.Contains("编译失败") || content.Contains("Compile failed"))
                stats.Errors++;
        }
        return stats;
    }

    private static List<(string Tool, string Summary)> ExtractToolCalls(List<JNode> messages)
    {
        var calls = new List<(string, string)>();
        foreach (var m in messages)
        {
            var role = m["role"]?.AsString() ?? "";
            if (role != "assistant") continue;

            var toolCalls = m["tool_calls"];
            if (toolCalls == null) continue;

            foreach (var tc in toolCalls.Items)
            {
                var func = tc["function"];
                var toolName = func?["name"]?.AsString() ?? "?";
                var args = func?["arguments"]?.AsString() ?? "";

                var summary = SummarizeArgs(toolName, args);
                calls.Add((toolName, summary));
            }
        }
        return calls;
    }

    private static List<(string Path, string Action)> ExtractChangedFiles(List<JNode> messages)
    {
        var seen = new HashSet<string>();
        var files = new List<(string, string)>();

        foreach (var m in messages)
        {
            var toolCalls = m["tool_calls"];
            if (toolCalls == null) continue;

            foreach (var tc in toolCalls.Items)
            {
                var func = tc["function"];
                var toolName = func?["name"]?.AsString() ?? "";
                var args = func?["arguments"]?.AsString() ?? "";

                var (path, action) = toolName switch
                {
                    // ⚠ 这些标签既**显示**（`_{action}_`）又被上面的 emoji 表**匹配**
                    //   （`"创建" or "create" => 📝` …，那张表本来就是中英双认 ⇒ 改这里不必改那里）。
                    "write_file" => (ExtractArg(args, "file_path"), L.Pick("创建", "create")),
                    "edit_file" => (ExtractArg(args, "file_path"), L.Pick("修改", "edit")),
                    "rm" => (ExtractArg(args, "file_path"), L.Pick("删除", "delete")),
                    "read_file" => (ExtractArg(args, "file_path"), L.Pick("读取", "read")),
                    "mv" => (ExtractArg(args, "file_path"), L.Pick("移动", "move")),
                    "cp" => (ExtractArg(args, "file_path"), L.Pick("复制", "copy")),
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
                    if (args[idx] == '"')
                    {
                        // 统计前导反斜杠：偶数个 → 真结束符；奇数个 → 引号被转义（如 "C:\\dir\\" 的尾引号）。
                        // 此前只看前一个字符，遇到 \\" 误判为被转义、遇到 \" 误判为结束，摘要截错后续 JSON。
                        int backslashes = 0;
                        for (int j = idx - 1; j >= 0 && args[j] == '\\'; j--)
                            backslashes++;
                        if (backslashes % 2 == 0)
                            break;
                    }
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
    public static string Truncate(this string s, int maxLen)
    {
        if (maxLen <= 0) return "";
        if (s.Length <= maxLen) return s;
        return ContextManager.TruncateByRunes(s, maxLen - 1) + "…";
    }
}
