using System.Text;
using WayCoder.Tools;
using WayCoder.UI.Shared;
using WayCoder.UI.Tui;
using WayCoder.UI.Shared.Terminal;
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
            screen.AddSystemMsg("用法: /history <关键词> 或 Ctrl+R 交互搜索");
            return;
        }

        var results = new List<(int Index, string Role, string Preview)>();
        for (int i = 0; i < _agent!.Messages.Count; i++)
        {
            var msg = _agent.Messages[i];
            var role = msg["role"]?.AsString() ?? "";
            var content = msg["content"]?.AsString() ?? "";
            if (content.Contains(keyword, StringComparison.OrdinalIgnoreCase))
            {
                var idx = content.IndexOf(keyword, StringComparison.OrdinalIgnoreCase);
                var start = Math.Max(0, idx - 40);
                var len = Math.Min(120, content.Length - start);
                var preview = content.Substring(start, len);
                if (start > 0) preview = "..." + preview;
                if (start + len < content.Length) preview += "...";
                results.Add((i + 1, role, preview.Replace("\n", " ")));
            }
        }

        if (results.Count == 0)
        {
            screen.AddSystemMsg($"未找到包含 \"{keyword}\" 的消息");
            return;
        }

        screen.AddSystemMsg($"🔍 \"{keyword}\" — {results.Count} 条结果:");
        foreach (var (idx, role, preview) in results.Take(15))
        {
            var roleIcon = role switch { "user" => "👤", "assistant" => "🤖", "tool" => "🔧", _ => "  " };
            screen.AddSystemMsg($"  #{idx} {roleIcon} {preview}");
        }

        if (results.Count > 15)
            screen.AddSystemMsg($"  ... 还有 {results.Count - 15} 条结果");
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
            screen.AddSystemMsg("用法: /loop [最大轮次] 提示词");
            return;
        }

        screen.AddSystemMsg($"🔁 /loop 开始 (最多 {maxIter} 轮)");
        var startTime = DateTime.UtcNow;

        for (int iter = 1; iter <= maxIter; iter++)
        {
            screen.AddSystemMsg($"\n── 第 {iter}/{maxIter} 轮 ──");
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
                screen.AddSystemMsg("⚠ /loop 已中断");
                break;
            }
            catch (Exception ex)
            {
                screen.FinishAgentMsg();
                screen.AddSystemMsg($"  ⚠ 第 {iter} 轮出错: {ex.Message}");
                ErrorLog.Error("Program.Loop", $"/loop 第 {iter} 轮异常: {ex.Message}", ex);
                if (iter == maxIter) break;
                await Task.Delay(1000);
                continue;
            }

            // 检查最近一条 assistant 消息是否含成功标记
            var lastAssistant = _agent!.Messages.LastOrDefault(m =>
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
                screen.AddSystemMsg($"  💡 条件达成！{iter} 轮 / {elapsed:F1}s");
                return;
            }

            // 注入继续指令
            prompt = $"上一轮结果未满足条件，请继续尝试。上次输出摘要：{lastContent[..Math.Min(lastContent.Length, 200)]}";
        }

        screen.AddSystemMsg($"⏰ 已达上限 {maxIter} 轮，/loop 结束");
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

        Console.WriteLine("WayCoder 项目初始化");
        Console.WriteLine($"目录: {cwd}");
        Console.WriteLine();

        if (!Directory.Exists(waycoderDir))
        {
            Directory.CreateDirectory(waycoderDir);
            Console.WriteLine($"✅ 创建 .waycoder/");
        }
        else
        {
            Console.WriteLine("⏭ .waycoder/ 已存在");
        }

        // mcp_servers.json 模板
        var mcpPath = Path.Combine(waycoderDir, "mcp_servers.json");
        if (!File.Exists(mcpPath))
        {
            var mcpTemplate = @"[
  {
    ""_comment"": ""MCP 服务器配置示例。name=工具名前缀, command=启动命令, args=参数, env=环境变量(可选)"",
    ""name"": ""filesystem"",
    ""command"": ""npx"",
    ""args"": [""-y"", ""@modelcontextprotocol/server-filesystem"", "".""],
    ""env"": {}
  }
]
";
            File.WriteAllText(mcpPath, mcpTemplate, Encoding.UTF8);
            Console.WriteLine("✅ 创建 mcp_servers.json (MCP 服务器配置)");
        }
        else Console.WriteLine("⏭ mcp_servers.json 已存在");

        // prompt.md 模板
        var promptPath = Path.Combine(waycoderDir, "prompt.md");
        if (!File.Exists(promptPath))
        {
            var promptTemplate = @"# 项目提示词

<!-- 在此文件中编写项目专属的 AI 指令。WayCoder 会自动将其注入系统提示词。 -->

## 项目概述
<!-- 简要描述你的项目 -->

## 编码规范
<!-- 代码风格、命名约定等 -->

## 注意事项
<!-- AI 需要特别注意的事项 -->
";
            File.WriteAllText(promptPath, promptTemplate, Encoding.UTF8);
            Console.WriteLine("✅ 创建 prompt.md (项目提示词模板)");
        }
        else Console.WriteLine("⏭ prompt.md 已存在");

        // memory.md (如果不存在则创建空文件)
        var memoryPath = Path.Combine(waycoderDir, "memory.md");
        if (!File.Exists(memoryPath))
        {
            File.WriteAllText(memoryPath, "# 项目记忆\n\n", Encoding.UTF8);
            Console.WriteLine("✅ 创建 memory.md (项目记忆)");
        }

        Console.WriteLine();
        Console.WriteLine("初始化完成！现在可以运行 waycoder 开始编码。");
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
        screen.ChatMessages.Add(new ChatMsg { Role = "user", Content = "对比模型价格和功能" });
        screen.ChatMessages.Add(new ChatMsg
        {
            Role = "agent", Content = @"### 价格对比

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
deepseek 性价比最高。"
        });
        screen.StatusLeft = "大:deepseek-v4-flash";
        mgr.Render();
        Console.WriteLine("\n===END===");

        // 建议面板截图验证
        screen.AddSystemMsg("建议列表：/reset /resume /restart-agent /restore-checkpoint");
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
            Console.WriteLine("（没有已保存的会话）");
            Console.WriteLine("会话在正常退出或输入 /save 时自动保存。");
            return;
        }

        Console.WriteLine($"📋 已保存的会话 ({sessions.Count} 个)");
        Console.WriteLine(new string('─', 60));
        Console.WriteLine($"{"会话名",-24} {"消息数",-8} {"模型",-16} {"保存时间"}");
        Console.WriteLine(new string('─', 60));
        foreach (var s in sessions)
        {
            var name = s.Id.Length > 22 ? s.Id[..19] + "..." : s.Id;
            var msgCount = s.MessageCount.ToString();
            var model = s.Model?.Length > 14 ? s.Model[..11] + "..." : (s.Model ?? "?");
            Console.WriteLine($"{name,-24} {msgCount,-8} {model,-16} {s.SavedAt}");
        }
        Console.WriteLine(new string('─', 60));
        Console.WriteLine("恢复: waycoder -c <会话名>  或  waycoder -c (恢复最近)");
    }

    private static void ShowUsage()
    {
        MarkupLine("«bold yellow»WayCoder (道码)«/» — 中文版易用编程智能体");
        Console.WriteLine();
        MarkupLine("«bold»使用方法:«/» «cyan»waycoder [选项]«/»");
        Console.WriteLine();
        MarkupLine("  «bold»选项:«/»");
        // 从参数注册表自动生成（排除内部/开发参数）
        foreach (var line in Arguments.CliArgRegistry.HelpText(2, 36).Split('\n'))
        {
            if (!string.IsNullOrWhiteSpace(line))
                Console.WriteLine(line);
        }
        Console.WriteLine();
        MarkupLine("  «bold»示例:«/»");
        MarkupLine("  «dim»$«/» waycoder                                     «dim»# 交互式 REPL«/»");
        MarkupLine("  «dim»$«/» waycoder «cyan»-p«/» «green»\"列出当前目录\"«/»               «dim»# 一次性模式«/»");
        MarkupLine("  «dim»$«/» waycoder «cyan»-m«/» deepseek-v4-pro             «dim»# 指定模型«/»");
        MarkupLine("  «dim»$«/» waycoder «cyan»-t«/»                              «dim»# 运行自测«/»");
        MarkupLine("  «dim»$«/» echo «green»\"列出目录\"«/» «dim»|«/» waycoder                   «dim»# 管道模式«/»");
    }

    /// <summary>Ctrl+M 打开模型选择对话框</summary>
    /// <summary>应用模型到指定槽位（供 CycleModel 调用）</summary>
    internal static void ApplyModel(string modelId, bool isLarge, int slot)
    {
        if (slot == -1)
        {
            if (isLarge) _config.Model = modelId; else _config.SmallModel = modelId;
            _config.SaveToEnvFile();
        }
        else if (slot == -2)
        {
            AgentSlotConfig.SetUniform(new AgentSlotConfig.SlotConfig
            { UseGlobal = false, LargeModel = isLarge ? modelId : null, SmallModel = isLarge ? null : modelId });
            if (isLarge) _config.Model = modelId; else _config.SmallModel = modelId;
            _config.SaveToEnvFile();
        }
        else if (slot is >= 0 and < 10)
        {
            var e = AgentSlotConfig.Get(slot);
            AgentSlotConfig.Set(slot, new AgentSlotConfig.SlotConfig
            {
                UseGlobal = false,
                LargeModel = isLarge ? modelId : e.LargeModel,
                SmallModel = isLarge ? e.SmallModel : modelId,
                BaseUrl = e.BaseUrl, ApiKeyProviderId = e.ApiKeyProviderId, ApiKey = e.ApiKey,
            });
        }
    }

    private static void CycleModel(ChatScreen screen)
    {
        var result = ModelPicker.Show(currentSlot: ActiveSlotIndex);
        if (result != null)
        {
            // 需要先输入 API Key
            if (result.NeedsApiKey && !string.IsNullOrEmpty(result.ProviderId))
            {
                var key = UxHelper.Secret(
                    $"🔑 输入 {result.ProviderId} 的 API Key（输入不可见，Enter 确认）:");
                if (!string.IsNullOrWhiteSpace(key))
                {
                    ApiKeyStore.Set(result.ProviderId, key);
                    screen.AddSystemMsg($"🔑 API Key 已保存: {result.ProviderId}");
                }
                else
                {
                    screen.AddSystemMsg("❌ 未输入 API Key，已取消");
                    return;
                }
            }

            // 应用模型到配置
            ApplyModel(result.ModelId, result.IsLarge, result.TargetSlot);

            var modelName = result.ModelId;
            _llm!.Model = _config.Model;
            _llm.SmallModel = _config.SmallModel;
            if (result.IsLarge)
                _agent?.UpdateContextWindow(ModelCatalog.ResolveContextWindow(_config.Model, _config.MaxContextTokens));

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
                screen.AddSystemMsg($"🔄 全部槽位 → {(result.IsLarge ? "大模型" : "小模型")} {modelName}");
            }
            else if (result.TargetSlot == -1) // 默认模型
            {
                screen.AddSystemMsg($"🔄 默认 {(result.IsLarge ? "大模型" : "小模型")} → {modelName}");
            }
            else // 指定槽位
            {
                int idx = result.TargetSlot;
                if (idx >= 0 && idx < AgentSlot.Count)
                {
                    var slot = _slots[idx];
                    slot.LastLargeModel = null; // 强制下次使用重新创建 LLM
                    slot.LastSmallModel = null;
                    screen.AddSystemMsg($"🔄 F{idx + 1} 槽位 → {(result.IsLarge ? "大模型" : "小模型")} {modelName}");
                }
            }

            screen.StatusLeft = _config.Model;
            TuiManager.RequestFullRefresh();
        }
    }

    /// <summary>Ctrl+S 打开会话管理对话框</summary>
    private static void OpenSessions(ChatScreen screen)
    {
        var result = SessionPicker.Show(currentSessionId: _currentSessionId);
        if (result == null) return;

        switch (result.Action)
        {
            case "switch":
                if (result.SessionId != _currentSessionId)
                {
                    AutoSaveSession();
                    var loaded = SessionManager.LoadSession(result.SessionId);
                    if (loaded != null)
                    {
                        var (messages, model) = loaded.Value;
                        _currentSessionId = result.SessionId;
                        _config.Model = model;
                        _llm!.Model = model;
                        _agent!.Messages.Clear();
                        _agent.Messages.AddRange(messages);
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
                        screen.StatusLeft = $"{_config.Model}";
                        screen.AddSystemMsg($"📂 已切换到会话: {result.SessionId}");
                    }
                }
                break;
            case "rename":
                screen.AddSystemMsg($"✏ 会话已重命名: {result.SessionId} → {result.NewName}");
                break;
            case "delete":
                SessionManager.DeleteSession(result.SessionId);
                if (result.SessionId == _currentSessionId)
                {
                    _currentSessionId = SessionManager.CreateNewSessionId();
                    _agent!.Messages.Clear();
                    screen.ClearChat();
                    screen.AddSystemMsg("🗑 当前会话已删除，已创建新会话");
                }
                else
                {
                    screen.AddSystemMsg($"🗑 会话已删除: {result.SessionId}");
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
                screen.AddSystemMsg("🧠 推理深度 → 已清除（使用模型默认）");
            else
                screen.AddSystemMsg($"🧠 推理深度 → {result.Level}");
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

        var choice = UxHelper.Select("命令面板 ↑↓ 选择 Enter 执行 Esc 取消", commands);
        if (choice == null) return "";

        // 对于带参数的命令，截取命令名
        var spaceIdx = choice.IndexOf(' ');
        return spaceIdx > 0 ? choice[..spaceIdx] : choice;
    }

    private static async Task<string> RunShellOnceAsync()
    {
        var needRestore = TuiManager.Instance.IsActive;
        if (needRestore) TuiManager.Instance.Exit();
        try
        {
        var cmd = UxHelper.Ask("! 命令");
        if (string.IsNullOrWhiteSpace(cmd)) return "";

        try
        {
            var result = await new Tools.BashTool().ExecuteAsync(
                new Dictionary<string, object?> { ["command"] = cmd });
            Console.WriteLine(result);
        }
        catch (Exception ex)
        {
            ErrorLog.Error("Program.ShellCmd", $"Shell 命令执行异常: {ex.Message}", ex);
            UxHelper.Error("Shell 错误", ex.Message);
        }

        return ""; // 不回传给 Agent
        }
        finally
        {
            if (needRestore) { TuiManager.Instance.Enter(); TuiManager.Instance.Render(); }
        }
    }

    private static async Task PlanModeAsync()
    {
        var needRestore = TuiManager.Instance.IsActive;
        if (needRestore) TuiManager.Instance.Exit();
        try
        {
        MarkupLine("«bold cyan»📋 计划模式«/» — 只读分析，Agent 先规划再执行");
        MarkupLine("«dim»输入你的需求，Agent 会先分析并列出执行计划«/»");
        Console.WriteLine();

        var userInput = TuiChatInput.ReadInput();
        if (string.IsNullOrWhiteSpace(userInput)) return;

        // 使用 PlanMode 结构化系统提示词（含项目上下文、仓库地图）
        var planPrompt = PlanMode.GetPlanSystemPrompt() +
            $"\n\n# 用户需求\n\n{userInput}\n\n请按上述格式输出你的分析和执行计划。";

        using var cts = new CancellationTokenSource();
        try
        {
            await ChatWithStatusAsync(planPrompt, cts.Token);
            Console.WriteLine();

            // 计划输出后询问是否执行
            Console.WriteLine();
            MarkupLine("«bold yellow»是否执行此计划？«/»");
            MarkupLine("«dim»  y = 执行  |  n = 放弃  |  输入修改意见«/»");
            var confirm = TuiChatInput.ReadInput();
            if (!string.IsNullOrWhiteSpace(confirm) && PlanMode.IsApproval(confirm))
            {
                Console.WriteLine();
                MarkupLine("«bold green»▶ 执行模式«/»");
                var execPrompt = $"按照之前制定的计划，逐步执行以下需求：\n\n{userInput}";
                await ChatWithStatusAsync(execPrompt, cts.Token);
                Console.WriteLine();
            }
            else if (!string.IsNullOrWhiteSpace(confirm))
            {
                if (TuiManager.Instance.ActiveScreen is ChatScreen cs)
                    cs.AddSystemMsg($"📋 计划待修改：{confirm}");
            }
        }
        catch (Exception ex)
        {
            ErrorLog.Error("Program.PlanMode", $"计划模式异常: {ex.Message}", ex);
            UxHelper.Error("错误", ex.Message);
        }
        }
        finally
        {
            if (needRestore) { TuiManager.Instance.Enter(); TuiManager.Instance.Render(); }
        }
    }
}
