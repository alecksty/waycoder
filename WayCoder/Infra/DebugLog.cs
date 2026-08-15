namespace WayCoder;

/// <summary>
/// 调试日志：记录 Agent 与 LLM 之间的所有通信内容。
/// 通过 --debug 或 /debug-on 开启，/debug-off 关闭。
/// 日志写入 logs/ 目录，按会话日期时间命名。
/// </summary>
public static class DebugLog
{
    private static readonly Lock _lock = new();
    private static string? _logDir;
    private static string? _sessionFile;
    private static int _roundCount;

    /// <summary>调试模式是否启用</summary>
    public static bool Enabled => _logDir != null;

    /// <summary>
    /// 开启调试日志。logs/ 目录自动创建。
    /// </summary>
    public static void Enable(string? baseDir = null)
    {
        lock (_lock)
        {
            if (_logDir != null) return; // 已开启

            var root = baseDir ?? Directory.GetCurrentDirectory();
            _logDir = Path.Combine(root, "logs");
            Directory.CreateDirectory(_logDir);

            var timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
            _sessionFile = Path.Combine(_logDir, $"session_{timestamp}.log");

            _roundCount = 0;
        }

        // 写会话头
        Write("header", $"""
                          ╔══════════════════════════════════════╗
                          ║  WayCoder Debug Log                 ║
                          ║  {DateTime.Now:yyyy-MM-dd HH:mm:ss}                    ║
                          ╚══════════════════════════════════════╝
                          """);
    }

    /// <summary>
    /// 关闭调试日志。
    /// </summary>
    public static void Disable()
    {
        lock (_lock)
        {
            if (_logDir != null)
            {
                Write("footer", "── 调试日志结束 ──");
            }
            _logDir = null;
            _sessionFile = null;
        }
    }

    /// <summary>
    /// 记录一个日志条目。自动递增轮次计数。
    /// </summary>
    public static void Log(string tag, string content, bool incrementRound = false)
    {
        if (!Enabled) return;

        lock (_lock)
        {
            if (incrementRound) _roundCount++;
            Write(tag, content);
        }
    }

    /// <summary>
    /// 记录发送给 LLM 的消息列表。
    /// </summary>
    public static void LogRequest(List<JNode> messages, List<JNode> tools)
    {
        if (!Enabled) return;

        lock (_lock)
        {
            _roundCount++;
            var sb = new System.Text.StringBuilder();
            sb.AppendLine($"Round #{_roundCount} → SEND to LLM");
            sb.AppendLine($"Messages: {messages.Count}, Tools: {tools.Count}");
            sb.AppendLine(new string('─', 60));

            for (int i = 0; i < messages.Count; i++)
            {
                var m = messages[i];
                var role = m["role"]?.AsString() ?? "?";
                var content = m["content"]?.AsString() ?? "";

                if (role == "system")
                {
                    sb.AppendLine($"[{i}] role=system ({content.Length} chars)");
                }
                else if (content?.Length > 500)
                {
                    sb.AppendLine($"[{i}] role={role} ({content.Length} chars)");
                    sb.AppendLine(content[..500] + "...");
                }
                else
                {
                    sb.AppendLine($"[{i}] role={role}: {content}");
                }

                // 工具调用
                if (m["tool_calls"] != null)
                {
                    var tcStr = m["tool_calls"]!.ToJson();
                    if (tcStr.Length > 1000)
                        sb.AppendLine($"  tool_calls: {tcStr[..1000]}...");
                    else
                        sb.AppendLine($"  tool_calls: {tcStr}");
                }
            }

            // 工具定义
            sb.AppendLine(new string('─', 60));
            sb.AppendLine("Tools:");
            foreach (var t in tools)
            {
                var name = t["function"]?["name"]?.AsString() ?? "?";
                sb.AppendLine($"  - {name}");
            }

            sb.AppendLine(new string('═', 60));
            WriteRaw(sb.ToString());
        }
    }

    /// <summary>
    /// 记录从 LLM 收到的响应。
    /// </summary>
    public static void LogResponse(string content, List<ToolCall> toolCalls, int promptTokens, int completionTokens)
    {
        if (!Enabled) return;

        lock (_lock)
        {
            var sb = new System.Text.StringBuilder();
            sb.AppendLine($"Round #{_roundCount} ← RECV from LLM");
            sb.AppendLine($"Tokens: {promptTokens} prompt + {completionTokens} completion");
            sb.AppendLine(new string('─', 60));

            if (!string.IsNullOrEmpty(content))
            {
                sb.AppendLine($"Content ({content.Length} chars):");
                sb.AppendLine(content);
            }

            if (toolCalls.Count > 0)
            {
                sb.AppendLine($"Tool Calls: {toolCalls.Count}");
                foreach (var tc in toolCalls)
                {
                    var argsStr = JsonHelper.SerializeArgs(tc.Arguments);
                    if (argsStr.Length > 500)
                        argsStr = argsStr[..500] + "...";
                    sb.AppendLine($"  - {tc.Name}({argsStr})");
                }
            }

            sb.AppendLine(new string('═', 60));
            WriteRaw(sb.ToString());
        }
    }

    /// <summary>
    /// 记录工具执行结果。
    /// </summary>
    public static void LogToolResult(string toolName, string result)
    {
        if (!Enabled) return;

        lock (_lock)
        {
            var sb = new System.Text.StringBuilder();
            sb.AppendLine($"  [{toolName}] result:");
            if (result.Length > 1000)
                sb.AppendLine(result[..1000] + $"\n  ... ({result.Length} chars total)");
            else
                sb.AppendLine(result);
            WriteRaw(sb.ToString());
        }
    }

    private static void Write(string tag, string content)
    {
        if (_sessionFile == null) return;
        try
        {
            var entry = $"[{DateTime.Now:HH:mm:ss.fff}] [{tag}]\n{content}\n";
            File.AppendAllText(_sessionFile, entry, System.Text.Encoding.UTF8);
        }
        catch { /* 日志写入失败不阻塞主流程 */ }
    }

    private static void WriteRaw(string content)
    {
        if (_sessionFile == null) return;
        try
        {
            File.AppendAllText(_sessionFile, content, System.Text.Encoding.UTF8);
        }
        catch { /* 日志写入失败不阻塞主流程 */ }
    }
}
