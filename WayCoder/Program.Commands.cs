using System.Text;
using WayCoder.Tools;
using WayCoder.UI.Shared;
using WayCoder.UI.Tui;
using WayCoder.UI.Shared.Terminal;
using WayCoder.UI.TUI.Base;
using WayCoder.UI.TUI.Custom;
using WayCoder.UI.Tui.Screens;
using WayCoder.UI.Web;
using Arguments = WayCoder.UI.Cli.Arguments;

namespace WayCoder;

/// <summary>
/// 入口 + CLI + REPL —— 面向用户的终端界面。
/// </summary>
public partial class Program
{

    // ========================================================================
    // 斜杠命令拼写纠错
    // ========================================================================

    /// <summary>已知斜杠命令名（不含参数），用于拼写纠错。——仅主名，不含短别名。</summary>
    internal static string[] KnownCommands =>
        SlashCommandRegistry.Commands.Select(c => c.Name).ToArray();

    /// <summary>Damerau-Levenshtein 编辑距离（支持字符换位）。</summary>
    internal static int Levenshtein(string a, string b)
    {
        var dp = new int[a.Length + 1, b.Length + 1];
        for (int i = 0; i <= a.Length; i++) dp[i, 0] = i;
        for (int j = 0; j <= b.Length; j++) dp[0, j] = j;
        for (int i = 1; i <= a.Length; i++)
        {
            for (int j = 1; j <= b.Length; j++)
            {
                var cost = a[i - 1] == b[j - 1] ? 0 : 1;
                dp[i, j] = Math.Min(
                    Math.Min(dp[i - 1, j] + 1, dp[i, j - 1] + 1),
                    dp[i - 1, j - 1] + cost);
                // 换位检测: "eu" ↔ "ue" 距离=1
                if (i > 1 && j > 1 && a[i - 1] == b[j - 2] && a[i - 2] == b[j - 1])
                    dp[i, j] = Math.Min(dp[i, j], dp[i - 2, j - 2] + cost);
            }
        }

        return dp[a.Length, b.Length];
    }

    /// <summary>
    /// 斜杠命令拼写纠错。输入不是已知命令时，返回编辑距离最近（≤2）的命令名并保留参数；
    /// 否则返回 null。短命令（命令名 &lt;5 字符）仅接受距离 1，避免 /ls→/pr 这类误判。
    /// </summary>
    internal static string? SuggestCommand(string input)
    {
        if (!input.StartsWith('/')) return null;
        var spaceIdx = input.IndexOf(' ');
        var cmd = spaceIdx > 0 ? input[..spaceIdx] : input;
        if (KnownCommands.Contains(cmd, StringComparer.OrdinalIgnoreCase)) return null;

        string? best = null;
        var bestDist = int.MaxValue;
        foreach (var known in KnownCommands)
        {
            var dist = Levenshtein(cmd, known);
            if (dist < bestDist)
            {
                bestDist = dist;
                best = known;
            }
        }

        if (best == null || bestDist == 0 || bestDist > 2) return null;
        // 短命令只接受距离 1（如 /hel→/help），避免 /ls→/pr 误判
        if (bestDist > 1 && cmd.Length < 5) return null;
        return spaceIdx > 0 ? best + input[spaceIdx..] : best;
    }

    // ---- 内置命令的聊天内联版本 ----
    /// <summary>Tab 键智能补全文件路径。返回 true 表示已处理。</summary>
    private static bool TabCompletePath(ChatScreen screen)
    {
        try
        {
            // 获取当前输入的"词"（光标前的连续非空白字符）
            var text = screen.GetInputText();
            var cursorPos = screen.InputArea.CursorCol; // 光标在当前行的位置
            if (cursorPos == 0) return false;

            // 从光标位置向前找到词的开始
            var lineText = screen.InputArea.Lines[screen.InputArea.CursorRow];
            var wordStart = cursorPos - 1;
            while (wordStart >= 0 && !char.IsWhiteSpace(lineText[wordStart]))
                wordStart--;
            wordStart++;

            var partial = lineText[wordStart..cursorPos];
            if (partial.Length == 0) return false;

            // 检测是否像文件路径（包含 / \ . 或以这些开头）
            if (!partial.Contains('/') && !partial.Contains('\\') && !partial.StartsWith('.') && !partial.StartsWith('/'))
                return false;

            // 解析路径
            var cwd = Directory.GetCurrentDirectory();
            string dir, prefix;
            var fullPath = Path.Combine(cwd, partial);
            var lastSep = partial.LastIndexOfAny(['/', '\\']);
            if (lastSep >= 0)
            {
                dir = Path.Combine(cwd, partial[..lastSep]);
                prefix = partial[(lastSep + 1)..];
            }
            else
            {
                dir = cwd;
                prefix = partial;
            }

            if (!Directory.Exists(dir)) return false;

            // 查找匹配的文件/目录
            var matches = Directory.GetFileSystemEntries(dir)
                .Select(p => Path.GetFileName(p))
                .Where(n => n.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
                .ToList();

            if (matches.Count == 0) return false;

            if (matches.Count == 1)
            {
                // 唯一匹配：补全
                var completion = matches[0];
                var fullMatch = Path.Combine(dir, completion);
                if (Directory.Exists(fullMatch)) completion += Path.DirectorySeparatorChar;
                // 替换到行中
                var before = lineText[..wordStart];
                var after = lineText[cursorPos..];
                screen.InputArea.Lines[screen.InputArea.CursorRow] = before + completion + after;
                screen.InputArea.CursorCol = wordStart + completion.Length;
                return true;
            }
            else
            {
                // 多个匹配：找最长公共前缀
                var lcp = FindLongestCommonPrefix(matches);
                if (lcp.Length > prefix.Length)
                {
                    var before = lineText[..wordStart];
                    var after = lineText[cursorPos..];
                    screen.InputArea.Lines[screen.InputArea.CursorRow] = before + lcp + after;
                    screen.InputArea.CursorCol = wordStart + lcp.Length;
                }

                // 显示匹配列表
                screen.AddSystemMsg("📁 " + string.Join("  ", matches.Take(20)));
                return true;
            }
        }
        catch
        {
            return false;
        }
    }

    private static string FindLongestCommonPrefix(List<string> strings)
    {
        if (strings.Count == 0) return "";
        var prefix = strings[0];
        foreach (var s in strings.Skip(1))
        {
            while (!s.StartsWith(prefix, StringComparison.OrdinalIgnoreCase) && prefix.Length > 0)
                prefix = prefix[..^1];
            if (prefix.Length == 0) break;
        }

        return prefix;
    }

    /// <summary>检测当前目录的 git 分支名。</summary>
    private static string? DetectGitBranch()
    {
        try
        {
            var headPath = Path.Combine(Directory.GetCurrentDirectory(), ".git", "HEAD");
            if (!File.Exists(headPath)) return null;
            var head = File.ReadAllText(headPath).Trim();
            if (head.StartsWith("ref: refs/heads/"))
                return head["ref: refs/heads/".Length..];
            return head.Length >= 7 ? head[..7] : head; // detached HEAD
        }
        catch
        {
            return null;
        }
    }

    private static void ShowHelpInChat(ChatScreen screen)
    {
        // 弹出控件化快捷键速查面板（对标 Crush 帮助窗），替代旧的一大段系统消息
        WayCoder.UI.Tui.Controls.TuiKeybindHelp.Show();
    }

    /// <summary>搜索对话历史中的关键词。</summary>
    private static void SearchHistory(string input, ChatScreen screen)
    {
        var keyword = input.Length > 9 ? input[9..].Trim() : "";
        if (string.IsNullOrWhiteSpace(keyword))
        {
            screen.AddSystemMsg(L.Pick("用法: /history <关键词> 或 Ctrl+F 交互搜索", "Usage: /history <keyword>, or Ctrl+F to search interactively"));
            return;
        }

        var results = new List<(int Index, string Role, string Preview)>();
        var msgs = _agent!.SnapshotMessages();
        for (int i = 0; i < msgs.Count; i++)
        {
            var msg = msgs[i];
            var role = msg["role"]?.AsString() ?? "";
            var content = msg["content"]?.AsString() ?? "";
            if (content.Contains(keyword, StringComparison.OrdinalIgnoreCase))
            {
                results.Add((i + 1, role, BuildHistoryPreview(content, keyword).Replace("\n", " ")));
            }
        }

        if (results.Count == 0)
        {
            screen.AddSystemMsg(L.Pick($"未找到包含 \"{keyword}\" 的消息", $"No messages containing \"{keyword}\" were found"));
            return;
        }

        screen.AddSystemMsg(L.Pick($"🔍 \"{keyword}\" — {results.Count} 条结果:", $"🔍 \"{keyword}\" — {results.Count} result(s):"));
        foreach (var (idx, role, preview) in results.Take(15))
        {
            var roleIcon = role switch { "user" => "👤", "assistant" => "🤖", "tool" => "🔧", _ => "  " };
            screen.AddSystemMsg($"  #{idx} {roleIcon} {preview}");
        }

        if (results.Count > 15)
            screen.AddSystemMsg(L.Pick($"  ... 还有 {results.Count - 15} 条结果", $"  ... and {results.Count - 15} more"));
    }

    /// <summary>提取历史消息关键词预览（前 40 + 120 码元窗口），代理对对齐不切半（否则渲染成 U+FFFD）。</summary>
    internal static string BuildHistoryPreview(string content, string keyword)
    {
        var idx = content.IndexOf(keyword, StringComparison.OrdinalIgnoreCase);
        var start = Math.Max(0, idx - 40);
        var end = Math.Min(content.Length, start + 120);
        while (start > 0 && char.IsLowSurrogate(content[start])) start--;
        while (end < content.Length && char.IsLowSurrogate(content[end])) end++;
        var preview = content.Substring(start, end - start);
        if (start > 0) preview = "..." + preview;
        if (end < content.Length) preview += "...";
        return preview;
    }

    // ========================================================================
    // /loop — 循环执行直到条件达成
    // ========================================================================

    /// <summary>
    /// /loop [最大轮次] 提示词 — 重复执行 Agent，直到输出含成功标记或达到上限。
    /// </summary>
    private static async Task RunLoopAsync(string args, ChatScreen screen)
    {
        int maxIter = 10;
        var prompt = args;

        // 解析可选的最大轮次：/loop 5 修复所有编译错误
        var spaceIdx = prompt.IndexOf(' ');
        if (spaceIdx > 0 && int.TryParse(prompt[..spaceIdx], out var n) && n > 0 && n <= 50)
        {
            maxIter = n;
            prompt = prompt[(spaceIdx + 1)..].Trim();
        }

        if (string.IsNullOrWhiteSpace(prompt))
        {
            screen.AddSystemMsg(L.Pick("用法: /loop [最大轮次] 提示词", "Usage: /loop [max iterations] prompt"));
            return;
        }

        screen.AddSystemMsg(L.Pick($"🔁 /loop 开始 (最多 {maxIter} 轮)", $"🔁 /loop started (up to {maxIter} iterations)"));
        var startTime = DateTime.UtcNow;

        for (int iter = 1; iter <= maxIter; iter++)
        {
            screen.AddSystemMsg(L.Pick($"\n── 第 {iter}/{maxIter} 轮 ──", $"\n── Iteration {iter}/{maxIter} ──"));
            screen.StatusLeft = $"loop {iter}/{maxIter}";

            using var cts = new CancellationTokenSource();

            try
            {
                screen.Running = true;
                screen.StartAgentMsg();
                screen.Render();

                // 后台执行 Agent（主线程保持渲染 + 响应热键）
                _currentUserInput = prompt;
                screen_ = screen;
                _toolCallCount = 0;
                await RunAgentWithRenderLoop(cts);

                screen.Running = false;
                screen.FinishAgentMsg();
            }
            catch (OperationCanceledException)
            {
                screen.Running = false;
                screen.FinishAgentMsg();
                screen.AddSystemMsg(L.Pick("⚠ /loop 已中断", "⚠ /loop interrupted"));
                break;
            }
            catch (Exception ex)
            {
                screen.FinishAgentMsg();
                screen.AddSystemMsg(L.Pick($"  ⚠ 第 {iter} 轮出错: {ex.Message}", $"  ⚠ Iteration {iter} failed: {ex.Message}"));
                ErrorLog.Error("Program.Loop", $"/loop 第 {iter} 轮异常: {ex.Message}", ex);
                if (iter == maxIter) break;
                await Task.Delay(1000);
                continue;
            }

            // 检查最近一条 assistant 消息是否含成功标记
            var lastAssistant = _agent!.SnapshotMessages().LastOrDefault(m =>
                m["role"]?.AsString() == "assistant");
            var lastContent = lastAssistant?["content"]?.AsString() ?? "";

            var successMarkers = new[]
            {
                "SUCCESS", "成功", "✅", "PASS", "通过",
                "所有测试通过", "0 errors", "0 个错误", "编译成功", "构建成功"
            };
            var isSuccess = successMarkers.Any(m =>
                lastContent.Contains(m, StringComparison.OrdinalIgnoreCase));

            if (isSuccess)
            {
                var elapsed = (DateTime.UtcNow - startTime).TotalSeconds;
                screen.AddSystemMsg(L.Pick($"  💡 条件达成！{iter} 轮 / {elapsed:F1}s", $"  💡 Condition met! {iter} iteration(s) / {elapsed:F1}s"));
                return;
            }

            // 注入继续指令
            prompt = L.Pick($"上一轮结果未满足条件，请继续尝试。上次输出摘要：{ContextManager.TruncateByRunes(lastContent, 200)}",
                $"The previous round did not meet the condition; please keep trying. Summary of the last output: {ContextManager.TruncateByRunes(lastContent, 200)}");
        }

        screen.AddSystemMsg(L.Pick($"⏰ 已达上限 {maxIter} 轮，/loop 结束", $"⏰ Reached the {maxIter}-iteration limit, /loop finished"));
    }

    // ========================================================================
    // /test — 分模块测试
    // ========================================================================

    /// <summary>
    /// <summary>项目初始化向导：创建 .waycoder/ 配置目录和模板文件。</summary>
    private static void RunInit()
    {
        var cwd = Directory.GetCurrentDirectory();
        var waycoderDir = Path.Combine(cwd, ".waycoder");

        Console.WriteLine(L.Pick("WayCoder 项目初始化", "WayCoder project init"));
        Console.WriteLine(L.Pick($"目录: {cwd}", $"Directory: {cwd}"));
        Console.WriteLine();

        if (!Directory.Exists(waycoderDir))
        {
            Directory.CreateDirectory(waycoderDir);
            Console.WriteLine(L.Pick($"✅ 创建 .waycoder/", $"✅ Created .waycoder/"));
        }
        else
        {
            Console.WriteLine(L.Pick("⏭ .waycoder/ 已存在", "⏭ .waycoder/ already exists"));
        }

        // mcp_servers.json 模板
        var mcpPath = Path.Combine(waycoderDir, "mcp_servers.json");
        if (!File.Exists(mcpPath))
        {
            var mcpTemplate = L.Pick(@"[
  {
    ""_comment"": ""MCP 服务器配置示例。name=工具名前缀, command=启动命令, args=参数, env=环境变量(可选)"",
    ""name"": ""filesystem"",
    ""command"": ""npx"",
    ""args"": [""-y"", ""@modelcontextprotocol/server-filesystem"", "".""],
    ""env"": {}
  }
]
", @"[
  {
    ""_comment"": ""Example MCP server config. name=tool-name prefix, command=launch command, args=arguments, env=environment variables (optional)"",
    ""name"": ""filesystem"",
    ""command"": ""npx"",
    ""args"": [""-y"", ""@modelcontextprotocol/server-filesystem"", "".""],
    ""env"": {}
  }
]
");
            // 原子写 + 无 BOM：这份文件用户会手编、外部工具（jq / python json.load）也会解析，
            // 而 Encoding.UTF8 静态实例是**带 BOM** 的，凭空加 BOM 会让它们报错
            Global.WriteAllTextAtomic(mcpPath, mcpTemplate);
            Console.WriteLine(L.Pick("✅ 创建 mcp_servers.json (MCP 服务器配置)", $"✅ Created mcp_servers.json (MCP server config)"));
        }
        else Console.WriteLine(L.Pick("⏭ mcp_servers.json 已存在", "⏭ mcp_servers.json already exists"));

        // prompt.md 模板
        var promptPath = Path.Combine(waycoderDir, "prompt.md");
        if (!File.Exists(promptPath))
        {
            var promptTemplate = L.Pick(@"# 项目提示词

<!-- 在此文件中编写项目专属的 AI 指令。WayCoder 会自动将其注入系统提示词。 -->

## 项目概述
<!-- 简要描述你的项目 -->

## 编码规范
<!-- 代码风格、命名约定等 -->

## 注意事项
<!-- AI 需要特别注意的事项 -->
", @"# Project prompt

<!-- Put project-specific AI instructions in this file. WayCoder injects them into the system prompt automatically. -->

## Project overview
<!-- Briefly describe your project -->

## Coding conventions
<!-- Code style, naming conventions, and so on -->

## Notes
<!-- Anything the AI needs to be especially careful about -->
");
            File.WriteAllText(promptPath, promptTemplate, Encoding.UTF8);
            Console.WriteLine(L.Pick("✅ 创建 prompt.md (项目提示词模板)", $"✅ Created prompt.md (project prompt template)"));
        }
        else Console.WriteLine(L.Pick("⏭ prompt.md 已存在", "⏭ prompt.md already exists"));

        // memory.md (如果不存在则创建空文件)
        var memoryPath = Path.Combine(waycoderDir, "memory.md");
        if (!File.Exists(memoryPath))
        {
            File.WriteAllText(memoryPath, L.Pick("# 项目记忆\n\n", "# Project memory\n\n"), Encoding.UTF8);
            Console.WriteLine(L.Pick("✅ 创建 memory.md (项目记忆)", $"✅ Created memory.md (project memory)"));
        }

        Console.WriteLine();
        Console.WriteLine(L.Pick("初始化完成！现在可以运行 waycoder 开始编码。", "Initialization complete! Run waycoder to start coding."));
    }

    /// <summary>对比各模式 SystemPrompt + 工具 schema 大小（极致省钱效果验证）。</summary>
    internal static void RunSyspromptSize()
    {
        Console.OutputEncoding = System.Text.Encoding.UTF8;
        var all = WayCoder.Tools.ToolRegistry.AllTools;
        var wl = new[] { "read_file", "write_file", "edit_file", "bash", "web_search" };
        var wlTools = all.Where(t => wl.Contains(t.Name, StringComparer.OrdinalIgnoreCase)).ToList();
        Console.WriteLine(L.Pick($"工具数: 全 {all.Count} vs 白名单 {wlTools.Count}（read/write/edit/bash/web_search）",
            $"Tool count: {all.Count} total vs {wlTools.Count} allowlisted (read/write/edit/bash/web_search)"));

        void M(string label, List<WayCoder.Tools.ITool> tools)
        {
            var sp = SystemPrompt.Generate(tools);
            int schemaChars = tools.Sum(t => t.Schema().ToJson().Length);
            int total = sp.Length + schemaChars;
            Console.WriteLine(L.Pick($"{label,-26} SP={sp.Length,6}字符  schema={schemaChars,6}字符  合计={total,7}  ≈{total / 3,5} tok(估)",
                $"{label,-26} SP={sp.Length,6} chars  schema={schemaChars,6} chars  total={total,7}  ≈{total / 3,5} tok(est)"));
        }

        var saved = Config.Instance.EconomyMode;
        try
        {
            // 4 个提示词档位 × 对应工具精简档位：省钱关=全量，开=去重复，开的越大越精简
            var modes = new (string Label, EconomyMode Mode)[]
            {
                (L.Pick("Off 全量", "Off full"), EconomyMode.Off),
                (L.Pick("Auto 去bash冗余", "Auto drop bash redundancy"), EconomyMode.Auto),
                (L.Pick("On 去搜索编辑冗余", "On drop search/edit redundancy"), EconomyMode.On),
                (L.Pick("Extreme 核心集", "Extreme core set"), EconomyMode.Extreme),
            };
            Console.WriteLine(L.Pick("── 提示词档位 × 工具精简档位 ──", "── Prompt tier × tool trim tier ──"));
            foreach (var (label, mode) in modes)
            {
                Config.Instance.EconomyMode = mode;
                var trimmed = Agent.TrimToolsForEconomy(all, mode);
                M(L.Pick($"{label} 工具{trimmed.Count}", $"{label} tools {trimmed.Count}"), trimmed);
            }
            Console.WriteLine(L.Pick("── 对照：白名单固定 5 工具，仅提示词档位变化 ──", "── Control: 5-tool allowlist fixed, only the prompt tier varies ──"));
            foreach (var (label, mode) in modes)
            {
                Config.Instance.EconomyMode = mode;
                M(L.Pick($"{label} 白名单5", $"{label} allowlist-5"), wlTools);
            }
            // 工作模式对照：Chat=0 工具 0 提示词；Plan=只读白名单 + GeneratePlan（体系提示词少量）
            var planTools = all.Where(t => WorkModeManager.PlanReadOnlyTools.Contains(t.Name)).ToList();
            var planSp = SystemPrompt.GeneratePlan(planTools);
            int planSchema = planTools.Sum(t => t.Schema().ToJson().Length);
            Console.WriteLine(L.Pick("── 对照：工作模式（Chat / Plan）──", "── Control: work modes (Chat / Plan) ──"));
            Console.WriteLine(L.Pick($"Chat 聊天    工具0                  SP=      0字符  schema=       0字符  合计=        0  ≈     0 tok(估)  (仅用户/助手消息)",
                $"Chat         tools 0               SP=      0 chars  schema=       0 chars  total=        0  ≈     0 tok(est)  (user/assistant messages only)"));
            Console.WriteLine(L.Pick($"Plan 规划    工具{planTools.Count,2}                SP={planSp.Length,6}字符  schema={planSchema,6}字符  合计={planSp.Length + planSchema,7}  ≈{(planSp.Length + planSchema) / 3,5} tok(估)  (只读白名单 + GeneratePlan)",
                $"Plan        tools {planTools.Count,-2}               SP={planSp.Length,6} chars  schema={planSchema,6} chars  total={planSp.Length + planSchema,7}  ≈{(planSp.Length + planSchema) / 3,5} tok(est)  (read-only allowlist + GeneratePlan)"));
        }
        finally { Config.Instance.EconomyMode = saved; }
    }

    /// <summary>截图模式：TUI 控件截图验证</summary>
    internal static void RunScreenshot()
    {
        Console.OutputEncoding = System.Text.Encoding.UTF8;

        // 使用新 TUI 架构进行截图
        var mgr = TuiManager.Instance;
        var screen = new ChatScreen();
        screen.ChatDisplayStyle = _config.ChatDisplayStyle;
        mgr.Enter();
        mgr.PushScreen(screen);

        // 添加测试消息
        screen.ChatMessages.Add(new ChatMsg { Role = "system", Content = Global.AppNameVersion });
        screen.ChatMessages.Add(new ChatMsg { Role = "user", Content = L.Pick("对比模型价格和功能", "Compare model pricing and features") });
        screen.ChatMessages.Add(new ChatMsg
        {
            Role = "assistant", Content = L.Pick(@"### 价格对比

| 模型 | 输入/1M | 输出/1M | 上下文 |
|------|---------|---------|--------|
| deepseek-v4-flash | $0.14 | $0.28 | 128K |
| gpt-5.4-mini | $0.075 | $0.15 | 200K |

### 功能清单

- 代码生成
  - C# / .NET 项目
  - Python 脚本
  - 前端 React/Vue
- 代码审查
  - Diff 级别审查
  - 安全漏洞扫描
deepseek 性价比最高。", @"### Price comparison

| Model | Input/1M | Output/1M | Context |
|------|---------|---------|--------|
| deepseek-v4-flash | $0.14 | $0.28 | 128K |
| gpt-5.4-mini | $0.075 | $0.15 | 200K |

### Feature list

- Code generation
  - C# / .NET projects
  - Python scripts
  - Front-end React/Vue
- Code review
  - Diff-level review
  - Security vulnerability scanning
deepseek offers the best value for money.")
        });
        screen.StatusLeft = L.Pick("大:deepseek-v4-flash", "large:deepseek-v4-flash");
        mgr.Render();
        Console.WriteLine("\n===END===");

        // 建议面板截图验证
        screen.AddSystemMsg(L.Pick("建议列表：/reset /resume /restart-agent /restore-checkpoint", "Suggestions: /reset /resume /restart-agent /restore-checkpoint"));
        screen.SetInput("/res");
        screen.Suggestions = new List<string>
        {
            "/reset", "/resume", "/restart-agent", "/restore-checkpoint",
            "/reset-all-config", "/reset-cache", "/restart-service",
            "/restore-session", "/reset-password", "/resize-window",
        };
        screen.SuggestIndex = 1;
        screen.SuggestActive = true;
        screen.StatusLeft = "deepseek-v4-flash";
        screen.UpdateSuggestions(screen.Suggestions, screen.SuggestIndex);

        // 截图1: 建议顶部
        mgr.Render();
        Console.WriteLine("\n===END===");

        // 截图2: 建议中间
        screen.SuggestIndex = 6;
        screen.UpdateSuggestions(screen.Suggestions, screen.SuggestIndex);
        mgr.Render();
        Console.WriteLine("\n===END===");

        screen.SuggestActive = false;
        mgr.Exit();
    }

    /// <summary>列出所有已保存的会话（--session-list）</summary>
    private static void ShowSessionList()
    {
        var sessions = SessionManager.ListSessions(50);
        if (sessions.Count == 0)
        {
            Console.WriteLine(L.Pick("（没有已保存的会话）", "(no saved sessions)"));
            Console.WriteLine(L.Pick("会话在正常退出或输入 /save 时自动保存。", "Sessions are saved automatically on a clean exit, or when you run /save."));
            return;
        }

        Console.WriteLine(L.Pick($"📋 已保存的会话 ({sessions.Count} 个)", $"📋 Saved sessions ({sessions.Count})"));
        Console.WriteLine(new string('─', 60));
        // 表头提成局部量：`$"{"会话名",-24}"` 这种嵌套引号连台账扫描器都认不出来（它按「引号即字面量边界」走），
        // 而那正是「已经翻过的文件里还留着中文」的隐蔽形态
        var hName = L.Pick("会话名", "Session");
        var hCount = L.Pick("消息数", "Messages");
        var hModel = L.Pick("模型", "Model");
        var hSaved = L.Pick("保存时间", "Saved at");
        Console.WriteLine($"{hName,-24} {hCount,-8} {hModel,-16} {hSaved}");
        Console.WriteLine(new string('─', 60));
        foreach (var s in sessions)
        {
            var name = s.Id.Length > 22 ? s.Id[..19] + "..." : s.Id;
            var msgCount = s.MessageCount.ToString();
            var model = s.Model?.Length > 14 ? s.Model[..11] + "..." : (s.Model ?? "?");
            Console.WriteLine($"{name,-24} {msgCount,-8} {model,-16} {s.SavedAt}");
        }
        Console.WriteLine(new string('─', 60));
        Console.WriteLine(L.Pick("恢复: waycoder -c <会话名>  或  waycoder -c (恢复最近)", "Resume: waycoder -c <session name>  or  waycoder -c (resume the most recent)"));
    }

    private static void ShowUsage()
    {
        // ── 顶部横幅：线框边框（┌─┐│），标题/公司名居中，版本右对齐 ──
        const int bannerW = 44;
        string Mid(string s)
        {
            var sw = WayCoder.UI.Shared.Terminal.AnsiString.DisplayWidth(s);
            var pad = Math.Max(0, bannerW - 2 - sw);
            var left = pad / 2;
            return "│" + new string(' ', left) + s + new string(' ', pad - left) + "│";
        }
        Console.WriteLine("┌" + new string('─', bannerW - 2) + "┐");
        // 标题用 ASCII 连字符「-」替代「—」（U+2014）：后者 CharWidth 按 2 列算但部分终端显示 1 列，
        // 导致居中后右框线差 1 列不对齐
        Console.WriteLine(Mid(L.Pick("WayCoder (道码) - 中文编程智能体", "WayCoder (道码) - coding agent")));
        Console.WriteLine(Mid(Global.Company));
        Console.WriteLine("└" + new string('─', bannerW - 2) + "┘");
        var verLine = L.Pick($"版本: {Global.Version}", $"Version: {Global.Version}");
        Console.WriteLine(new string(' ', Math.Max(0, bannerW - WayCoder.UI.Shared.Terminal.AnsiString.DisplayWidth(verLine))) + verLine);
        Console.WriteLine();
        MarkupLine(L.Pick("«bold»使用方法:«/» «cyan»waycoder [选项]«/»", "«bold»Usage:«/» «cyan»waycoder [options]«/»"));
        Console.WriteLine();
        MarkupLine(L.Pick("  «bold»选项:«/»", "  «bold»Options:«/»"));
        // 从参数注册表自动生成（排除内部/开发参数，按分类分组显示；分类标题含标记需 MarkupLine 渲染）
        // 空行也输出：分类标题上方的分隔空行需保留
        foreach (var line in Arguments.CliArgRegistry.HelpText(2, 36).Split('\n'))
        {
            MarkupLine(line);
        }
        Console.WriteLine();
        MarkupLine(L.Pick("  «bold»示例:«/»", "  «bold»Examples:«/»"));
        // 示例命令 + 注释纵向对齐（注释列固定，CJK 感知）
        const int exNoteCol = 36;
        void Ex(string plain, string markup, string note)
        {
            var line = "  «dim»$«/» " + markup;
            var w = WayCoder.UI.Shared.Terminal.AnsiString.DisplayWidth("  $ " + plain);
            if (w < exNoteCol) line += new string(' ', exNoteCol - w);
            line += $"«dim»# {note}«/»";
            MarkupLine(line);
        }
        Ex("waycoder", "waycoder", L.Pick("交互式 REPL", "interactive REPL"));
        Ex("waycoder --web", "waycoder «cyan»--web«/»", L.Pick("浏览器网页模式", "browser UI mode"));
        Ex(L.Pick("waycoder -p \"列出当前目录\"", "waycoder -p \"list the current directory\""), L.Pick("waycoder «cyan»-p«/» «green»\"列出当前目录\"«/»", "waycoder «cyan»-p«/» «green»\"list the current directory\"«/»"), L.Pick("一次性模式", "one-shot mode"));
        Ex("waycoder -m deepseek-v4-pro", "waycoder «cyan»-m«/» deepseek-v4-pro", L.Pick("指定模型", "specify the model"));
        Ex("waycoder -t", "waycoder «cyan»-t«/»", L.Pick("运行自测", "run the self-test"));
        Ex(L.Pick("echo \"列出目录\" | waycoder", "echo \"list the directory\" | waycoder"), L.Pick("echo «green»\"列出目录\"«/» «dim»|«/» waycoder", "echo «green»\"list the directory\"«/» «dim»|«/» waycoder"), L.Pick("管道模式", "pipe mode"));
        Console.WriteLine();
        MarkupLine(L.Pick("  «bold green»💡 强烈推荐使用 Web 模式：waycoder --web«/»", "  «bold green»💡 The Web UI is recommended: waycoder --web«/»"));
    }

    /// <summary>Ctrl+M 打开模型选择对话框</summary>
    private static void CycleModel(ChatScreen screen)
    {
        var result = ModelPicker.Show(currentSlot: ActiveSlotIndex);
        if (result != null)
        {
            // 需要先输入 API Key
            if (result.NeedsApiKey && !string.IsNullOrEmpty(result.ProviderId))
            {
                var key = UxHelper.Secret(
                    L.Pick($"🔑 输入 {result.ProviderId} 的 API Key（输入不可见，Enter 确认）:",
                    $"🔑 Enter the API Key for {result.ProviderId} (input hidden, Enter to confirm):"));
                if (!string.IsNullOrWhiteSpace(key))
                {
                    ApiKeyStore.Set(result.ProviderId, key);
                    screen.AddSystemMsg(L.Pick($"🔑 API Key 已保存: {result.ProviderId}", $"🔑 API Key saved: {result.ProviderId}"));
                }
                else
                {
                    screen.AddSystemMsg(L.Pick("❌ 未输入 API Key，已取消", "❌ No API Key entered, cancelled"));
                    return;
                }
            }

            // 应用模型到配置（携带所选模型的网关地址 + 服务商，地址不同=不同服务商）。
            // 走 ModelPicker.Apply —— 此前这里另有一份逐字重复的 Program.ApplyModel，
            // 且它**漏了「运行时生效」尾段**，CycleModel 只好在下面自己补一遍。
            ModelPicker.Apply(result.ModelId, result.IsLarge, result.TargetSlot, result.BaseUrl, result.ProviderId);

            var modelName = result.ModelId;
            // 全局 _llm 镜像（ModelPicker.Apply 的尾段管的是 ProgramContext.Agent，
            // 槽位 UseGlobal 时用的才是这个全局实例，两边都要跟上）
            _llm!.Model = _config.Model;
            _llm.SmallModel = _config.SmallModel;
            if (result.IsLarge)
                _agent?.UpdateContextWindow(ModelCatalog.ResolveContextWindow(_config.Model, _config.MaxContextTokens));

            // 大小模型显示名提成局部量：文案与判据同源，也给台账扫描器一个无嵌套引号的实参
            var whichModel = result.IsLarge ? L.Pick("大模型", "large model") : L.Pick("小模型", "small model");
            // 更新受影响的槽位 LLM
            if (result.TargetSlot == -2) // 全部槽位
            {
                for (int i = 0; i < AgentSlot.Count; i++)
                {
                    var s = _slots[i];
                    if (s.LlmClient != null)
                    {
                        s.LlmClient.Model = _config.Model;
                        s.LlmClient.SmallModel = _config.SmallModel;
                    }
                }
                screen.AddSystemMsg(L.Pick($"🔄 全部槽位 → {whichModel} {modelName}", $"🔄 All slots → {whichModel} {modelName}"));
            }
            else if (result.TargetSlot == -1) // 默认模型
            {
                screen.AddSystemMsg(L.Pick($"🔄 默认 {whichModel} → {modelName}", $"🔄 Default {whichModel} → {modelName}"));
            }
            else // 指定槽位
            {
                int idx = result.TargetSlot;
                if (idx >= 0 && idx < AgentSlot.Count)
                {
                    var slot = _slots[idx];
                    slot.LastLargeModel = null; // 强制下次使用重新创建 LLM
                    slot.LastSmallModel = null;
                    screen.AddSystemMsg(L.Pick($"🔄 F{idx + 1} 槽位 → {whichModel} {modelName}", $"🔄 Slot F{idx + 1} → {whichModel} {modelName}"));
                }
            }

            screen.RefreshModelStatus(); // 统一 (provider)model 格式 + 标脏刷新
            TuiManager.RequestFullRefresh();
        }
    }

    /// <summary>Ctrl+S 打开会话管理对话框（按当前槽位隔离会话记录）</summary>
    private static void OpenSessions(ChatScreen screen)
    {
        var slot = ActiveSlotIndex;
        var result = SessionPicker.Show(currentSessionId: _currentSessionIds[slot], slot: slot);
        if (result == null) return;

        switch (result.Action)
        {
            case "switch":
                if (result.SessionId != _currentSessionIds[slot])
                {
                    AutoSaveSession();
                    var loaded = SessionManager.LoadSession(result.SessionId, slot);
                    if (loaded != null)
                    {
                        var (messages, model) = loaded.Value;
                        _currentSessionIds[slot] = result.SessionId;
                        _agent!.LlmClient.Model = model;
                        _agent.ReplaceMessages(messages);
                        // 重建 ChatScreen 消息列表
                        screen.ClearChat();
                        foreach (var msg in messages)
                        {
                            var role = msg["role"]?.AsString() ?? "";
                            var content = msg["content"]?.AsString() ?? "";
                            if (role == "user") screen.AddMessage(content, "user");
                            else if (role == "assistant") screen.AddMessage(content, "assistant");
                            else if (role == "tool") screen.AddMessage(content, "tool", indent: 1);
                        }
                        // 会话恢复的模型可能与当前 cfg.Model 不同：不同 → 运行态回滚通道；相同 → 当前主通道
                        screen.StatusLeft = ConnectionConfig.FormatModelChannel(
                            string.Equals(model, Config.Instance.Model, StringComparison.OrdinalIgnoreCase)
                                ? ConnectionConfig.CurrentMainChannel() : "rollback",
                            Config.Instance.Provider, model);
                        screen.AddSystemMsg(L.Pick($"📂 已切换到会话: {result.SessionId}", $"📂 Switched to session: {result.SessionId}"));
                    }
                }
                break;
            case "rename":
                screen.AddSystemMsg(L.Pick($"✏ 会话已重命名: {result.SessionId} → {result.NewName}", $"✏ Session renamed: {result.SessionId} → {result.NewName}"));
                break;
            case "delete":
                SessionManager.DeleteSession(result.SessionId, slot);
                if (result.SessionId == _currentSessionIds[slot])
                {
                    _currentSessionIds[slot] = SessionManager.CreateNewSessionId();
                    _agent!.ClearMessages();
                    screen.ClearChat();
                    screen.AddSystemMsg(L.Pick("🗑 当前会话已删除，已创建新会话", "🗑 The current session was deleted and a new one was created"));
                }
                else
                {
                    screen.AddSystemMsg(L.Pick($"🗑 会话已删除: {result.SessionId}", $"🗑 Session deleted: {result.SessionId}"));
                }
                break;
        }
    }

    /// <summary>Ctrl+G 打开推理深度选择对话框</summary>
    private static void PickReasoningEffort(ChatScreen screen)
    {
        var result = ReasoningPicker.Show(
            currentLevel: _config.ReasoningEffort,
            modelName: _config.Model);
        if (result != null)
        {
            if (string.IsNullOrEmpty(result.Level))
                screen.AddSystemMsg(L.Pick("🧠 推理深度 → 已清除（使用模型默认）", "🧠 Reasoning effort → cleared (using the model default)"));
            else
                screen.AddSystemMsg(L.Pick($"🧠 推理深度 → {result.Level}", $"🧠 Reasoning effort → {result.Level}"));
        }
    }

    /// <summary>/ 触发：弹出命令面板，用方向键选择，回车执行</summary>
    private static string ShowCommandPalette()
    {
        var commands = new List<string>();

        // 从注册表生成命令列表（优先显示 Usage，其次 Name）
        foreach (var cmd in SlashCommandRegistry.Commands)
            commands.Add(cmd.Usage ?? cmd.Name);
        commands.Add("quit");

        // 追加自定义命令
        foreach (var (name, _) in CustomCommands.Commands)
            commands.Add($"/{name}");

        var choice = UxHelper.Select(L.Pick("命令面板 ↑↓ 选择 Enter 执行 Esc 取消", "Command palette: ↑↓ to choose, Enter to run, Esc to cancel"), commands);
        if (choice == null) return "";

        // 对于带参数的命令，截取命令名
        var spaceIdx = choice.IndexOf(' ');
        return spaceIdx > 0 ? choice[..spaceIdx] : choice;
    }

    /// <summary>
    /// `!` shell 直通执行：裸 `!` 弹提示输入命令，`!cmd` 直接执行 cmd。
    /// 走 ExecuteUserShellAsync（跳过 Agent 黑名单，仅留绝对红线——红色边框即危险提示）。
    /// 输出**加到聊天内容**（tool 角色纯文本，防 markdown 误渲染），过长截取最后 500 行。
    /// **后台执行**：不阻塞 REPL 主循环 —— 慢命令（git/npm/build）不再冻结 TUI（主循环冻结看门狗
    /// 曾反复报 PendingSubmissions 冻结 ~3s = `!cmd` 内联 await 子进程所致）。
    /// </summary>
    private static void RunShellCommandAsync(string? command, ChatScreen screen)
    {
        var cmd = command;
        if (string.IsNullOrWhiteSpace(cmd))
        {
            // 裸 !：控制台提示输入命令（Ask 需先退出 TUI）
            var needRestore = TuiManager.Instance.IsActive;
            if (needRestore) TuiManager.Instance.Exit();
            try { cmd = UxHelper.Ask(L.Pick("! 命令", "! command")); }
            finally { if (needRestore) { TuiManager.Instance.Enter(); TuiManager.Instance.Render(); } }
            if (string.IsNullOrWhiteSpace(cmd)) return;
        }

        var capturedCmd = cmd;
        var capturedScreen = screen;
        _ = Task.Run(async () =>
        {
            try
            {
                // 先投递「执行中」提示：慢命令也有即时反馈（输出完成后追加结果）
                capturedScreen.PostToUI(() => capturedScreen.AddSystemMsg(L.Pick($"⏳ 执行: $ {capturedCmd}", $"⏳ Running: $ {capturedCmd}")));
                var result = await new Tools.BashTool().ExecuteUserShellAsync(capturedCmd);
                capturedScreen.PostToUI(() =>
                    // 与 agent bash 工具路径一致：shell 输出走等宽竖线控制台块（带 ┃ gutter 与角色色）
                    capturedScreen.AddMessage($"$ {capturedCmd}\n{TailLines(result, 500)}", "tool", shellBlock: true));
            }
            catch (Exception ex)
            {
                ErrorLog.Error("Program.ShellCmd", $"Shell 命令执行异常: {ex.Message}", ex);
                capturedScreen.PostToUI(() => capturedScreen.AddSystemMsg(L.Pick($"⚠ Shell 错误: {ex.Message}", $"⚠ Shell error: {ex.Message}")));
            }
        });
    }

    /// <summary>保留字符串**最后** maxLines 行（超出丢头部；少则原样）。纯逻辑便于自测。</summary>
    internal static string TailLines(string s, int maxLines)
    {
        if (string.IsNullOrEmpty(s) || maxLines <= 0) return s ?? "";
        var lines = s.Replace("\r\n", "\n").Split('\n');
        if (lines.Length <= maxLines) return s;
        return string.Join('\n', lines.TakeLast(maxLines)) + L.Pick($"\n…（共 {lines.Length} 行，仅显示最后 {maxLines} 行）", $"\n... (of {lines.Length} lines, showing only the last {maxLines})");
    }

    private static async Task PlanModeAsync()
    {
        var needRestore = TuiManager.Instance.IsActive;
        if (needRestore) TuiManager.Instance.Exit();
        try
        {
        MarkupLine(L.Pick("«bold cyan»📋 计划模式«/» — 只读分析，Agent 先规划再执行", "«bold cyan»📋 Plan mode«/» — read-only analysis; the agent plans before acting"));
        MarkupLine(L.Pick("«dim»输入你的需求，Agent 会先分析并列出执行计划«/»", "«dim»Type your request; the agent will analyze it and lay out an execution plan«/»"));
        Console.WriteLine();

        var userInput = TuiChatInput.ReadInput();
        if (string.IsNullOrWhiteSpace(userInput)) return;

        // 使用 PlanMode 结构化系统提示词（含项目上下文、仓库地图）
        var planPrompt = PlanMode.GetPlanSystemPrompt() +
            L.Pick($"\n\n# 用户需求\n\n{userInput}\n\n请按上述格式输出你的分析和执行计划。",
                $"\n\n# User request\n\n{userInput}\n\nOutput your analysis and execution plan in the format described above.");

        using var cts = new CancellationTokenSource();
        try
        {
            await ChatWithStatusAsync(planPrompt, cts.Token);
            Console.WriteLine();

            // 计划输出后询问是否执行
            Console.WriteLine();
            MarkupLine(L.Pick("«bold yellow»是否执行此计划？«/»", "«bold yellow»Execute this plan?«/»"));
            MarkupLine(L.Pick("«dim»  y = 执行  |  n = 放弃  |  输入修改意见«/»", "«dim»  y = execute  |  n = discard  |  or type your revisions«/»"));
            var confirm = TuiChatInput.ReadInput();
            if (!string.IsNullOrWhiteSpace(confirm) && PlanMode.IsApproval(confirm))
            {
                Console.WriteLine();
                MarkupLine(L.Pick("«bold green»▶ 执行模式«/»", "«bold green»▶ Execution mode«/»"));
                var execPrompt = L.Pick($"按照之前制定的计划，逐步执行以下需求：\n\n{userInput}",
                    $"Following the plan drawn up earlier, carry out the request below step by step:\n\n{userInput}");
                await ChatWithStatusAsync(execPrompt, cts.Token);
                Console.WriteLine();
            }
            else if (!string.IsNullOrWhiteSpace(confirm))
            {
                if (TuiManager.Instance.ActiveScreen is ChatScreen cs)
                    cs.AddSystemMsg(L.Pick($"📋 计划待修改：{confirm}", $"📋 Plan pending revision: {confirm}"));
            }
        }
        catch (Exception ex)
        {
            ErrorLog.Error("Program.PlanMode", $"计划模式异常: {ex.Message}", ex);
            UxHelper.Error(L.Pick("错误", "Error"), ex.Message);
        }
        }
        finally
        {
            if (needRestore) { TuiManager.Instance.Enter(); TuiManager.Instance.Render(); }
        }
    }
}
