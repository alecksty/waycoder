using System.Text;
using CoreCoderSharp.Tools;
using CoreCoderSharp.UI;
using Spectre.Console;

namespace CoreCoderSharp;

/// <summary>
/// 入口 + CLI + REPL —— 面向用户的终端界面。
/// </summary>
public class Program
{
    private static Config _config = new();
    private static LLM? _llm;
    private static Agent? _agent;
    private static WatchMode? _watchMode;
    private static (List<JsonObject> Messages, string Model)? _pendingRestore;
    private static readonly List<string> _inputHistory = [];
    private static int _historyIdx = -1;
    /// <summary>Watch 模式线程安全提示队列</summary>
    private static readonly System.Collections.Concurrent.ConcurrentQueue<string> _pendingWatchPrompts = new();

    public static async Task<int> Main(string[] args)
    {
        Console.OutputEncoding = Encoding.UTF8;

        // 手动解析 CLI 参数
        string? model = null, baseUrl = null, apiKey = null, prompt = null, resumeId = null;
        double? maxBudget = null;
        bool showVersion = false, yoloMode = false, initMode = false, watchMode = false, tuiV2 = false;

        for (int i = 0; i < args.Length; i++)
        {
            switch (args[i])
            {
                case "-m" or "--model" when i + 1 < args.Length: model = args[++i]; break;
                case "--base-url" when i + 1 < args.Length: baseUrl = args[++i]; break;
                case "--api-key" when i + 1 < args.Length: apiKey = args[++i]; break;
                case "-p" or "--prompt" when i + 1 < args.Length: prompt = args[++i]; break;
                case "-r" or "--resume" when i + 1 < args.Length: resumeId = args[++i]; break;
                case "-v" or "--version": showVersion = true; break;
                case "-t" or "--test": SelfTest.Run(); return 0;
                case "--debug": DebugLog.Enable(); break;
                case "--yolo": yoloMode = true; break;
                case "--init": initMode = true; break;
                case "-w" or "--watch": watchMode = true; break;
#if TERMINAL_GUI
                case "--tui-v2": tuiV2 = true; break;
#endif
                case "--max-budget-usd" when i + 1 < args.Length:
                    if (double.TryParse(args[++i], out var b)) maxBudget = b; break;
                case "-h" or "--help": ShowUsage(); return 0;
            }
        }

        if (showVersion) { Console.WriteLine("WayCoder v0.17.3 (道码)"); return 0; }

        // 项目初始化向导
        if (initMode) { RunInit(); return 0; }

        // 标准输入管道模式：echo "prompt" | waycoder
        if (prompt == null && Console.IsInputRedirected)
        {
            prompt = Console.In.ReadToEnd().Trim();
            // 管道输入非交互，自动开启 yolo 模式
            if (!yoloMode) yoloMode = true;
        }

        _config = Config.FromEnv();
        if (model != null) _config.Model = model;
        if (baseUrl != null) _config.BaseUrl = baseUrl;
        if (apiKey != null) _config.ApiKey = apiKey;
        if (maxBudget != null) _config.MaxBudgetUsd = maxBudget;
        if (watchMode) _config.WatchMode = true;

        // DeepSeek 模型自动设置 base URL
        if (_config.BaseUrl == null && _config.Model.StartsWith("deepseek"))
            _config.BaseUrl = "https://api.deepseek.com";

        if (string.IsNullOrEmpty(_config.ApiKey))
        {
            MarkupLine("[bold red]╔══════════════════════════════╗[/]");
            MarkupLine("[bold red]║  API 密钥未设置！           ║[/]");
            MarkupLine("[bold red]╚══════════════════════════════╝[/]");
            Console.WriteLine();
            Console.WriteLine("请设置以下环境变量之一:");
            Console.WriteLine("  WAYCODER_API_KEY");
            Console.WriteLine("  DEEPSEEK_API_KEY");
            Console.WriteLine("  OPENAI_API_KEY");
            Console.WriteLine("  ANTHROPIC_API_KEY");
            Console.WriteLine("  API_KEY");
            Console.WriteLine();
            Console.WriteLine("或者在项目根目录创建 .env 文件:");
            Console.WriteLine("  CORECODER_API_KEY=sk-你的密钥");
            return 1;
        }

        _llm = new LLM(_config.Model, _config.ApiKey, _config.BaseUrl,
            _config.MaxTokens, _config.Temperature);
        _agent = new Agent(_llm, maxContextTokens: _config.MaxContextTokens,
            maxBudgetUsd: _config.MaxBudgetUsd, autoCommit: _config.AutoGitCommit);

        // --yolo: 一次性模式下跳过所有权限确认
        if (yoloMode)
        {
            SandboxManager.SetLevel("full-auto");
        }
        else
        {
            // 从配置初始化沙箱级别
            SandboxManager.SetLevel(_config.SandboxLevel);
            // 同步 PromptCache 设置
            PromptCache.Enabled = _config.PromptCaching;
        }

        // 设置沙箱允许的目录（项目根目录）
        SandboxManager.AllowedDirectory = Directory.GetCurrentDirectory();

        // 加载自定义斜杠命令、hooks、MCP 服务器和检查点
        CustomCommands.Load();
        HooksManager.Init();
        McpManager.Init();
        CheckpointManager.LoadFromDisk();

        // 恢复会话
        if (resumeId != null)
        {
            var loaded = SessionManager.LoadSession(resumeId);
            if (loaded != null)
            {
                _agent.Messages = loaded.Value.Messages;
                if (model == null) { _llm.Model = loaded.Value.Model; _config.Model = loaded.Value.Model; }
                MarkupLine($"[green]✔ 已恢复会话:[/] [cyan]{E(resumeId)}[/] [dim](模型: {E(_llm.Model)})[/]");
            }
            else
            {
                MarkupLine($"[red]✘ 会话 '{E(resumeId)}' 未找到[/]");
                return 1;
            }
        }

        if (prompt != null)
            await RunOnceAsync(prompt);
#if TERMINAL_GUI
        else if (tuiV2)
            await RunReplV2Async();
#endif
        else
            await RunReplAsync();

        return 0;
    }

    // ========================================================================
    // 一次性模式
    // ========================================================================

    private static async Task RunOnceAsync(string prompt)
    {
        using var cts = new CancellationTokenSource();
        Console.CancelKeyPress += (_, e) => { e.Cancel = true; cts.Cancel(); };

        try
        {
            MarkupLine($"[dim]🤖 {E(prompt)}[/]");
            await ChatWithStatusAsync(prompt, cts.Token);
            Console.WriteLine();
        }
        catch (OperationCanceledException)
        {
            if (cts.IsCancellationRequested)
            {
                MarkupLine("\n[orange3]⚠ 已中断[/]");
                Environment.Exit(130);
            }
            else
            {
                TuiBox.Error("请求超时", "服务器 60s 未响应，请检查网络或 API 配置");
                Environment.Exit(1);
            }
        }
        catch (Exception ex)
        {
            TuiBox.Error("错误", ex.Message);
            Environment.Exit(1);
        }
    }

    // ========================================================================
    // 交互式 REPL (Terminal.Gui v2 版)
    // ========================================================================

#if TERMINAL_GUI
    private static async Task RunReplV2Async()
    {
        _llm!.SmallModel = _config.SmallModel;

        // 尝试恢复上次自动保存的会话
        try
        {
            var auto = SessionManager.LoadSession("_auto");
            if (auto != null) _pendingRestore = auto;
        }
        catch { }

        var v2Repl = new UI.TerminalGuiRepl(_agent!, _llm!, _config);
        await Task.Run(() => v2Repl.Run());

        AutoSaveSession();
    }
#endif

    // ========================================================================
    // 交互式 REPL
    // ========================================================================

    private static async Task RunReplAsync()
    {
        var sm = ScreenManager.Instance;
        sm.Enter();
        sm.RefreshTheme();
        sm.ChatMessages.Clear();
        sm.InputLines.Clear();
        sm.InputLines.Add(new StringBuilder());

        // 启动欢迎屏 — ASCII Logo 注入聊天区
        var logo = new[]
        {
            "██╗    ██╗ █████╗ ██╗   ██╗",
            "██║    ██║██╔══██╗╚██╗ ██╔╝",
            "██║ █╗ ██║███████║ ╚████╔╝ ",
            "██║███╗██║██╔══██║  ╚██╔╝  ",
            "╚███╔███╔╝██║  ██║   ██║   ",
            " ╚══╝╚══╝ ╚═╝  ╚═╝   ╚═╝   ",
        };
        foreach (var line in logo)
            sm.ChatMessages.Add(new ScreenManager.ChatMsg { Role = "system", Content = line });
        sm.AddSystemMsg("WayCoder 道码 · 中文版易用编程智能体 · v0.17.3");
        sm.AddSystemMsg("深圳市探索智能科技有限公司");
        sm.AddSystemMsg($"大模型: {_config.Model} · 小模型: {_config.SmallModel}  ·  /help 帮助");
        sm.StatusLeft = $"大:{_config.Model} 小:{_config.SmallModel}";
        _llm!.SmallModel = _config.SmallModel;

        // 检测 git 分支
        var branch = DetectGitBranch();
        if (branch != null)
        {
            sm.StatusLeft += $" |  {branch}";
            sm.GitBranch = branch;
        }

        // Watch 模式 — 监听外部编辑器文件变更
        if (_config.WatchMode)
        {
            StartWatchMode(sm);
            sm.AddSystemMsg("👁 Watch 模式已启动 — 在文件中写 AI! 注释自动触发 Agent");
        }

        // 尝试恢复上次会话
        TryRestoreSession(sm);

        // Ctrl+C 优雅退出（触发 finally 中的自动保存）
        var exitRequested = false;
        Console.CancelKeyPress += (_, e) => { e.Cancel = true; exitRequested = true; };

        var running = true;
        while (running && !exitRequested)
        {
            sm.Render();

            // 检查 Watch 模式待处理提示
            while (_pendingWatchPrompts.TryDequeue(out var watchPrompt))
            {
                sm.AddSystemMsg($"👁 Watch: {watchPrompt[..Math.Min(watchPrompt.Length, 80)]}");
                sm.Render();
                await ProcessUserInput(watchPrompt, sm);
            }

            var key = Console.ReadKey(intercept: true);
            bool ctrl = key.Modifiers.HasFlag(ConsoleModifiers.Control);
            bool shift = key.Modifiers.HasFlag(ConsoleModifiers.Shift);

            // 终端 resize 检测
            var (tw, th) = (Console.WindowWidth, Console.WindowHeight);
            if (tw != sm.TW || th != sm.TH) { sm.Render(); continue; }

            // ---- 建议模式 ----
            if (sm.SuggestActive)
            {
                switch (key.Key)
                {
                    case ConsoleKey.Escape: sm.SuggestActive = false; continue;
                    case ConsoleKey.UpArrow: if (sm.SuggestIdx > 0) sm.SuggestIdx--; continue;
                    case ConsoleKey.DownArrow: if (sm.SuggestIdx < sm.Suggestions.Count - 1) sm.SuggestIdx++; continue;
                    case ConsoleKey.Enter: case ConsoleKey.Tab:
                        sm.AcceptSuggestion(); sm.UpdateSuggestions(); continue;
                    case ConsoleKey.Backspace: sm.InputBackspace(); sm.UpdateSuggestions(); break;
                    case ConsoleKey.LeftArrow: case ConsoleKey.RightArrow:
                        sm.SuggestActive = false; break;
                    default: break;
                }
                if (key.Key is ConsoleKey.UpArrow or ConsoleKey.DownArrow or ConsoleKey.Enter or ConsoleKey.Tab or ConsoleKey.Escape)
                    continue;
            }

            sm.SyncTodos(); // 每帧同步 Todo 数据

            switch (key.Key)
            {
                case ConsoleKey.Enter when !ctrl && !shift:
                    var input = sm.GetInputText();
                    if (string.IsNullOrWhiteSpace(input)) continue;
                    sm.AddUserMsg(input);
                    // 保存到输入历史（去重相邻重复）
                    if (_inputHistory.Count == 0 || _inputHistory[^1] != input)
                        _inputHistory.Add(input);
                    if (_inputHistory.Count > 200) _inputHistory.RemoveAt(0);
                    _historyIdx = -1;
                    sm.SetInput("");
                    sm.Render();
                    await ProcessUserInput(input, sm);
                    break;

case ConsoleKey.F2:
                    sm.ActivePanel = sm.ActivePanel switch
                    {
                        ScreenManager.PanelTab.Off => ScreenManager.PanelTab.Todo,
                        ScreenManager.PanelTab.Todo => ScreenManager.PanelTab.Files,
                        ScreenManager.PanelTab.Files => ScreenManager.PanelTab.Locks,
                        ScreenManager.PanelTab.Locks => ScreenManager.PanelTab.MCP,
                        _ => ScreenManager.PanelTab.Off,
                    };
                    if (sm.ActivePanel == ScreenManager.PanelTab.Files)
                        sm.ModifiedFiles = EditFileTool.ChangedFiles.ToList();
                    break;

                case ConsoleKey.F5:
                    SettingsPage.Show();
                    break;

                case ConsoleKey.R when ctrl:
                    sm.Exit();
                    var query = TuiPrompt.Ask("搜索对话历史");
                    sm.Enter();
                    if (!string.IsNullOrWhiteSpace(query))
                        SearchHistory("/history " + query, sm);
                    break;

                case ConsoleKey.M when ctrl:
                    // 循环切换大模型
                    CycleModel(sm);
                    break;

                case ConsoleKey.F1:
                    ShowHelpInChat(sm);
                    break;

                case ConsoleKey.F10:
                    AutoSaveSession();
                    _watchMode?.Dispose();
                    sm.Exit();
                    Environment.Exit(0);
                    break;

                // ---- 聊天区滚动 ----
                case ConsoleKey.PageUp: sm.ChatScrollUp(Math.Max(1, (Console.WindowHeight - 10) / 2)); break;
                case ConsoleKey.PageDown: sm.ChatScrollDown(Math.Max(1, (Console.WindowHeight - 10) / 2)); break;
                case ConsoleKey.Home when ctrl: sm.ChatScrollTop(); break;
                case ConsoleKey.End when ctrl: sm.ChatScrollBottom(); break;
                case ConsoleKey.UpArrow when ctrl: sm.ChatScrollUp(3); break;
                case ConsoleKey.DownArrow when ctrl: sm.ChatScrollDown(3); break;

                case ConsoleKey.Enter when ctrl || shift:
                    sm.InputNewLine();
                    sm.UpdateSuggestions();
                    break;
                case ConsoleKey.Escape when string.IsNullOrEmpty(sm.GetInputText()):
                    running = false;
                    break;

                case ConsoleKey.Backspace when ctrl: sm.InputDeleteWordLeft(); sm.UpdateSuggestions(); break;
                case ConsoleKey.Backspace: sm.InputBackspace(); sm.UpdateSuggestions(); break;
                case ConsoleKey.Delete when ctrl: sm.InputDeleteWordRight(); sm.UpdateSuggestions(); break;
                case ConsoleKey.Delete: sm.InputDelete(); sm.UpdateSuggestions(); break;
                case ConsoleKey.LeftArrow when ctrl: sm.InputWordLeft(); break;
                case ConsoleKey.LeftArrow: sm.InputMoveLeft(); break;
                case ConsoleKey.RightArrow when ctrl: sm.InputWordRight(); break;
                case ConsoleKey.RightArrow: sm.InputMoveRight(); break;
                case ConsoleKey.UpArrow:
                    if (sm.InputLines.Count == 1)
                    {
                        // 单行输入 + 空内容: 滚动聊天区
                        if (string.IsNullOrEmpty(sm.GetInputText()))
                        { sm.ChatScrollUp(3); break; }
                        // 单行输入：浏览历史
                        if (_inputHistory.Count > 0)
                        {
                            if (_historyIdx == -1) _historyIdx = _inputHistory.Count - 1;
                            else if (_historyIdx > 0) _historyIdx--;
                            sm.SetInput(_inputHistory[_historyIdx]);
                        }
                    }
                    else sm.InputMoveUp();
                    break;
                case ConsoleKey.DownArrow:
                    if (sm.InputLines.Count == 1)
                    {
                        // 单行输入 + 空内容: 滚动聊天区
                        if (string.IsNullOrEmpty(sm.GetInputText()))
                        { sm.ChatScrollDown(3); break; }
                        if (_historyIdx >= 0)
                        {
                            _historyIdx++;
                            sm.SetInput(_historyIdx < _inputHistory.Count
                                ? _inputHistory[_historyIdx] : "");
                            if (_historyIdx >= _inputHistory.Count) _historyIdx = -1;
                        }
                    }
                    else sm.InputMoveDown();
                    break;
                case ConsoleKey.Home: sm.InputCx = 0; break;
                case ConsoleKey.End: sm.InputCx = sm.InputLines[sm.InputCy].Length; break;
                case ConsoleKey.Tab:
                    // 文件路径智能补全：如果当前词像路径则补全，否则插入空格
                    if (!TabCompletePath(sm))
                    {
                        for (int t = 0; t < 4; t++) sm.InputInsert(' ');
                    }
                    sm.UpdateSuggestions();
                    break;

                default:
                    if (key.KeyChar >= ' ')
                    {
                        sm.InputInsert(key.KeyChar);
                        sm.UpdateSuggestions();
                    }
                    break;
            }
        }

        AutoSaveSession();
        _watchMode?.Dispose();
        sm.Exit();
    }

    /// <summary>启动时检测上次自动保存的会话，提示用户恢复。</summary>
    private static void TryRestoreSession(ScreenManager sm)
    {
        try
        {
            var auto = SessionManager.LoadSession("_auto");
            if (auto == null) return;

            var count = auto.Value.Messages.Count;
            sm.AddSystemMsg($"💾 发现上次会话 ({count} 条消息)。输入 /resume 恢复，或忽略此消息开始新会话。");
            _pendingRestore = auto;
        }
        catch { /* 恢复失败不影响启动 */ }
    }

    /// <summary>启动 Watch 模式 — 监听文件变更中的 AI! / AI? 注释。</summary>
    private static void StartWatchMode(ScreenManager sm)
    {
        try
        {
            var dir = Directory.GetCurrentDirectory();
            _watchMode = new WatchMode(dir, prompt =>
            {
                _pendingWatchPrompts.Enqueue(prompt);
            });
            _watchMode.Start();
        }
        catch (Exception ex)
        {
            sm.AddSystemMsg($"⚠ Watch 模式启动失败: {ex.Message}");
            DebugLog.Log("watch", $"启动失败: {ex.Message}");
        }
    }

    /// <summary>切换 Watch 模式开关。</summary>
    private static void ToggleWatchMode(ScreenManager sm)
    {
        if (_watchMode != null)
        {
            _watchMode.Dispose();
            _watchMode = null;
            _config.WatchMode = false;
            sm.AddSystemMsg("👁 Watch 模式已关闭");
        }
        else
        {
            _config.WatchMode = true;
            StartWatchMode(sm);
            if (_watchMode != null)
                sm.AddSystemMsg("👁 Watch 模式已启动 — 在文件中写 AI! 注释自动触发 Agent");
        }
    }

    /// <summary>退出时自动保存会话，下次启动可恢复。</summary>
    private static void AutoSaveSession()
    {
        try
        {
            if (_agent == null || _agent.Messages.Count == 0) return;
            // 只保存有实际对话的会话（至少一条用户消息）
            var hasUser = _agent.Messages.Any(m =>
                (string?)m["role"] == "user");
            if (!hasUser) return;
            SessionManager.SaveSession(_agent.Messages, _config.Model, "_auto");
            DebugLog.Log("session", "会话已自动保存");
        }
        catch (Exception ex)
        {
            DebugLog.Log("session", $"自动保存失败: {ex.Message}");
        }
    }

    /// <summary>处理用户输入：内置命令或 Agent 调用</summary>
    private static async Task ProcessUserInput(string userInput, ScreenManager sm)
    {
        // 全角规范化
        userInput = userInput
            .Replace('／', '/').Replace('！', '!').Replace('＃', '#');

        // 命令别名（快捷方式）
        userInput = userInput switch
        {
            "/c" => "/compact",
            "/m" => "/model",
            "/r" => "/reset",
            "/h" => "/help",
            "/t" => "/tokens",
            "/d" => "/diff",
            "/s" => "/save",
            "/q" => "quit",
            _ => userInput,
        };

        // 斜杠命令拼写纠错：/rsume → /resume（编辑距离 ≤ 2 自动纠正）
        if (userInput.StartsWith('/'))
        {
            var corrected = SuggestCommand(userInput);
            if (corrected != null && corrected != userInput)
            {
                sm.AddSystemMsg($"💡 命令 [{userInput}] 未识别，已纠正为 [{corrected}]");
                userInput = corrected;
            }
        }

        var lower = userInput.ToLowerInvariant();

        // 退出
        if (lower is "quit" or "exit" or "/quit" or "/exit")
        {
            AutoSaveSession();
            _watchMode?.Dispose();
            sm.Exit();
            Environment.Exit(0);
        }

        // 触发提示 (已通过建议面板处理，但保留备用)
        if (userInput == "/") { sm.SetInput(ShowCommandPalette()); sm.Render(); return; }
        if (userInput == "!") { await RunShellOnceAsync(); return; }

        // 内置命令
        if (userInput == "/help") { ShowHelpInChat(sm); return; }
        if (userInput == "/reset") { _agent!.Reset(); sm.AddSystemMsg("♻ 对话已重置"); return; }
        if (userInput == "/tokens") { ShowTokensInChat(sm); return; }
        if (userInput == "/stats") { ShowStatsInChat(sm); return; }
        if (userInput == "/watch") { ToggleWatchMode(sm); return; }
        if (userInput == "/model") { sm.AddSystemMsg($"当前模型: {_config.Model}"); return; }
        if (userInput.StartsWith("/model ")) { SwitchModelInline(userInput, sm); return; }
        if (userInput == "/compact") { await CompactAsync(); sm.AddSystemMsg("✔ 上下文已压缩"); return; }
        if (userInput == "/save") { SaveSessionInteractive(sm); return; }
        if (userInput == "/permissions" || userInput == "/perm") { ShowPermStatusInChat(sm); return; }
        if (userInput.StartsWith("/perm ")) { SandboxManager.SetLevel(userInput[6..].Trim()); sm.AddSystemMsg($"沙箱级别已切换: {SandboxManager.Level}"); return; }
        if (userInput == "/sessions") { ShowSessionBrowser(sm); return; }
        if (userInput.StartsWith("/load ")) { LoadSessionInteractive(userInput[6..].Trim(), sm); return; }
        if (userInput == "/diff") { ShowDiffInChat(sm); return; }
        if (userInput == "/plan") { sm.AddSystemMsg("📋 计划模式"); await PlanModeAsync(); return; }
        if (userInput.StartsWith("/search ")) { await RunSearchAsync(userInput[8..].Trim()); return; }
        if (userInput == "/edit") { await Editor.PickAndRunAsync(); sm.Render(); return; }
        if (userInput.StartsWith("/edit ")) { await Editor.RunAsync(userInput[6..].Trim()); sm.Render(); return; }
        if (userInput is "/settings" or "/config") { SettingsPage.Show(); return; }
        if (userInput == "/about") { ScreenManager.ShowAbout(); return; }
        if (userInput.StartsWith("/history")) { SearchHistory(userInput, sm); return; }
        if (userInput == "/export") { ExportConversation(sm); return; }
        if (userInput == "/recent") { ShowRecentFiles(sm); return; }
        if (userInput == "/resume" && _pendingRestore != null)
        {
            var (msgs, model) = _pendingRestore.Value;
            _agent!.Messages.Clear();
            _agent.Messages.AddRange(msgs);
            _pendingRestore = null;
            sm.AddSystemMsg($"✔ 已恢复 {msgs.Count} 条消息 (模型: {model})");
            return;
        }
        if (userInput.StartsWith("/loop "))
        { await RunLoopAsync(userInput[6..].Trim(), sm); return; }
        if (userInput.StartsWith("/test"))
        { RunModuleTest(userInput, sm); return; }

        // 调用 Agent (支持自动回退)
        using var cts = new CancellationTokenSource();
        Console.CancelKeyPress += (_, e) => { e.Cancel = true; cts.Cancel(); };
        var modelStack = BuildFallbackChain();
        var startTime = DateTime.UtcNow;
        var completed = false;

        for (int attempt = 0; attempt < modelStack.Length; attempt++)
        {
            var model = modelStack[attempt];
            if (attempt > 0)
            {
                _llm!.Model = model;
                _config.Model = model;
                sm.StatusLeft = model;
                sm.AddSystemMsg($"🔄 自动回退到: {model}");
                sm.StartAgentMsg();
            }

            try
            {
                sm.StartAgentMsg();
                sm.Render();

                await _agent!.ChatAsync(userInput,
                    onToken: tok =>
                    {
                        sm.AppendToken(tok);
                        sm.Render();
                    },
                    onTool: (name, brief) =>
                    {
                        sm.FinishAgentMsg();
                        sm.AddToolMsg(name, brief.Length > 60 ? brief[..57] + "..." : brief);
                        sm.StartAgentMsg();
                        sm.Render();
                    },
                    cancellationToken: cts.Token);

                sm.FinishAgentMsg();
                completed = true;
                break; // 成功
            }
            catch (OperationCanceledException)
            {
                sm.FinishAgentMsg();
                sm.AddSystemMsg(cts.IsCancellationRequested
                    ? "⚠ 已中断" : "⏰ 服务器 60s 未响应");
                break;
            }
            catch (Exception ex) when (attempt < modelStack.Length - 1)
            {
                sm.FinishAgentMsg();
                sm.AddSystemMsg($"⚠ {model} 失败: {ex.Message}");
                // 继续回退链
            }
            catch (Exception ex)
            {
                sm.FinishAgentMsg();
                sm.AddSystemMsg($"✘ 所有模型均失败: {ex.Message}");
            }
        }

        // 完成通知：耗时 + 终端响铃
        if (completed)
        {
            var elapsed = (DateTime.UtcNow - startTime).TotalSeconds;
            sm.AddSystemMsg($"✅ 完成 ({elapsed:F1}s)");
            Console.Write('\a'); // 终端响铃
        }

        // 文件修改确认 + 最近文件跟踪
        var modified = EditFileTool.ChangedFiles;
        if (modified.Count > 0)
        {
            sm.AddSystemMsg($"📝 已修改 {modified.Count} 个文件 (/diff 查看 /undo 撤销 /recent 最近)");
            foreach (var f in modified)
            {
                if (!sm.RecentFiles.Contains(f))
                {
                    sm.RecentFiles.Add(f);
                    if (sm.RecentFiles.Count > 50) sm.RecentFiles.RemoveAt(0);
                }
            }
        }

        // 更新右下角 token 显示 + 性能
        sm.UpdateTokenDisplay(
            _llm!.TotalPromptTokens, _llm.TotalCompletionTokens,
            _llm.EstimatedCost,
            ContextManager.EstimateTokens(_agent!.Messages), _config.MaxContextTokens,
            _llm.LastLatencyMs, _llm.LastTokensPerSec);
        sm.Render();
    }

    // ========================================================================
    // 斜杠命令拼写纠错
    // ========================================================================

    /// <summary>已知斜杠命令名（不含参数），用于拼写纠错。</summary>
    internal static readonly string[] KnownCommands =
    [
        "/help", "/reset", "/model", "/tokens", "/stats", "/watch", "/compact",
        "/save", "/permissions", "/perm", "/sessions", "/load", "/diff", "/plan",
        "/search", "/edit", "/settings", "/config", "/about", "/history", "/export",
        "/recent", "/resume", "/loop", "/test", "/debug-on", "/debug-off", "/todo",
        "/git-status", "/git-log", "/git-diff", "/review", "/lint", "/checkpoint",
        "/undo", "/checkpoints", "/repomap", "/pr",
    ];

    /// <summary>Levenshtein 编辑距离（字符级）。</summary>
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
            if (dist < bestDist) { bestDist = dist; best = known; }
        }

        if (best == null || bestDist == 0 || bestDist > 2) return null;
        // 短命令只接受距离 1（如 /hel→/help），避免 /ls→/pr 误判
        if (bestDist > 1 && cmd.Length < 5) return null;
        return spaceIdx > 0 ? best + input[spaceIdx..] : best;
    }

    // ---- 内置命令的聊天内联版本 ----
    /// <summary>Tab 键智能补全文件路径。返回 true 表示已处理。</summary>
    private static bool TabCompletePath(ScreenManager sm)
    {
        try
        {
            // 获取当前输入的"词"（光标前的连续非空白字符）
            var text = sm.GetInputText();
            var cursorPos = sm.InputCx; // 光标在当前行的位置
            if (cursorPos == 0) return false;

            // 从光标位置向前找到词的开始
            var lineText = sm.InputLines[sm.InputCy].ToString();
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
                sm.InputLines[sm.InputCy] = new StringBuilder(before + completion + after);
                sm.InputCx = wordStart + completion.Length;
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
                    sm.InputLines[sm.InputCy] = new StringBuilder(before + lcp + after);
                    sm.InputCx = wordStart + lcp.Length;
                }
                // 显示匹配列表
                sm.AddSystemMsg("📁 " + string.Join("  ", matches.Take(20)));
                return true;
            }
        }
        catch { return false; }
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

    /// <summary>显示最近访问/修改的文件。</summary>
    private static void ShowRecentFiles(ScreenManager sm)
    {
        var all = new HashSet<string>(EditFileTool.ChangedFiles);
        foreach (var f in sm.RecentFiles) all.Add(f);

        if (all.Count == 0)
        {
            sm.AddSystemMsg("（暂无最近文件）");
            return;
        }

        var sorted = all.OrderByDescending(f =>
        {
            try { return File.GetLastWriteTime(f); }
            catch { return DateTime.MinValue; }
        }).Take(15).ToList();

        sm.AddSystemMsg($"📁 最近文件 ({sorted.Count}):");
        foreach (var f in sorted)
        {
            var relative = Path.GetRelativePath(Directory.GetCurrentDirectory(), f);
            var icon = File.Exists(f) ? "📄" : "⚠";
            sm.AddSystemMsg($"  {icon} {relative}");
        }
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
        catch { return null; }
    }

    private static void ShowHelpInChat(ScreenManager sm)
    {
        sm.AddSystemMsg("帮助: /help /reset /model /tokens /compact /diff /save /resume /history /export /sessions ... Ctrl+R搜索 Ctrl+M切换模型 ↑↓历史");
    }
    private static void ShowTokensInChat(ScreenManager sm)
    {
        var p = _llm!.TotalPromptTokens; var c = _llm!.TotalCompletionTokens;
        var latency = _llm.LastLatencyMs;
        var tps = _llm.LastTokensPerSec;
        var info = $"Token: {p:N0} 输入 + {c:N0} 输出 = {(p + c):N0} 总计";
        if (latency > 0)
            info += $" | 上次: {latency / 1000:F1}s, {tps:F0} tok/s | 请求: {_llm.TotalRequests} 次";
        sm.AddSystemMsg(info);
    }

    private static void ShowStatsInChat(ScreenManager sm)
    {
        var sb = new System.Text.StringBuilder();
        sb.AppendLine("╔══════════ 用量统计 ══════════╗");

        var p = _llm!.TotalPromptTokens;
        var c = _llm!.TotalCompletionTokens;
        var cost = _llm.EstimatedCost;
        var latency = _llm.LastLatencyMs;
        var tps = _llm.LastTokensPerSec;

        sb.AppendLine($"║ 模型:    {_config.Model,-22} ║");
        sb.AppendLine($"║ 大模型:  {_config.Model,-22} ║");
        sb.AppendLine($"║ 小模型:  {_config.SmallModel,-22} ║");
        sb.AppendLine($"║ ─────────────────────────── ║");
        sb.AppendLine($"║ 输入:    {p,10:N0} tokens    ║");
        sb.AppendLine($"║ 输出:    {c,10:N0} tokens    ║");
        sb.AppendLine($"║ 总计:    {p + c,10:N0} tokens    ║");
        if (cost.HasValue)
            sb.AppendLine($"║ 花费:    ${cost.Value,10:F4}          ║");
        sb.AppendLine($"║ 请求:    {_llm.TotalRequests,10} 次        ║");
        if (latency > 0)
        {
            sb.AppendLine($"║ 延迟:    {latency / 1000,10:F1} s        ║");
            sb.AppendLine($"║ 速度:    {tps,10:F0} tok/s     ║");
        }
        sb.AppendLine($"║ ─────────────────────────── ║");
        sb.AppendLine($"║ 消息:    {_agent!.Messages.Count,10} 条        ║");
        sb.AppendLine($"║ 轮次:    {(int)(_agent.Messages.Count / 2.0),10}          ║");
        sb.AppendLine($"║ 会话:    {SessionManager.ListSessions().Count,10} 个        ║");
        sb.AppendLine($"║ 权限:    {PermissionManager.CurrentMode,-22} ║");
        sb.AppendLine("╚══════════════════════════════╝");

        sm.AddSystemMsg(sb.ToString());
    }

    private static void SaveSessionInChat(ScreenManager sm)
    {
        var sid = SessionManager.SaveSession(_agent!.Messages, _config.Model);
        sm.AddSystemMsg($"✔ 会话已保存: {sid}");
    }
    private static void SwitchModelInline(string input, ScreenManager sm)
    {
        var m = input[7..].Trim();
        if (string.IsNullOrEmpty(m)) return;

        var known = new[] { "deepseek-v4-flash","deepseek-v4-pro","gpt-5.4-mini","gpt-5.4","gpt-5.5","gpt-4o","gpt-4o-mini" };

        // /model big <name> 或 /model small <name>
        bool isSmall = m.StartsWith("small ", StringComparison.OrdinalIgnoreCase);
        bool isBig = m.StartsWith("big ", StringComparison.OrdinalIgnoreCase);
        if (isSmall || isBig) m = m[(m.IndexOf(' ') + 1)..];

        var match = known.FirstOrDefault(k => k.StartsWith(m, StringComparison.OrdinalIgnoreCase));
        if (match != null) m = match;

        if (isSmall)
        {
            _config.SmallModel = m;
            _llm!.SmallModel = m;
        }
        else
        {
            _llm!.Model = m;
            _config.Model = m;
        }

        sm.StatusLeft = $"大:{_config.Model} 小:{_config.SmallModel}";
        var label = isSmall ? "小模型" : "大模型";
        sm.AddSystemMsg($"✅ {label}已切换: {m}");
    }

    private static void SaveSessionInteractive(ScreenManager sm)
    {
        var name = sm.ShowMenu("保存会话 — 输入名称", ["💾 自动命名", "📝 自定义名称..."]);
        string? sid = null;
        if (name == 1)
        {
            sm.Exit();
            var input = TuiPrompt.Ask("会话名称");
            sm.Enter();
            if (!string.IsNullOrWhiteSpace(input)) sid = input.Trim();
        }
        if (sid == null)
            sid = SessionManager.SaveSession(_agent!.Messages, _config.Model);
        else
            sid = SessionManager.SaveSession(_agent!.Messages, _config.Model, sid);
        sm.AddSystemMsg($"✅ 会话已保存: {sid}");
    }

    private static void ShowSessionBrowser(ScreenManager sm)
    {
        var sessions = SessionManager.ListSessions();
        if (sessions.Count == 0) { sm.AddSystemMsg("没有已保存的会话"); return; }

        var choices = new List<string>();
        foreach (var s in sessions)
            choices.Add($"📁 {s.Id}  [{s.Model}]  {s.SavedAt}");
        choices.Add("🗑 删除会话...");

        var idx = sm.ShowMenu($"会话列表 ({sessions.Count})", choices);
        if (idx < 0) return;

        if (idx == sessions.Count) // 删除
        {
            var delChoices = sessions.Select(s => $"{s.Id}  [{s.Model}]").ToList();
            var delIdx = sm.ShowMenu("选择要删除的会话", delChoices);
            if (delIdx >= 0)
            {
                SessionManager.DeleteSession(sessions[delIdx].Id);
                sm.AddSystemMsg($"✅ 已删除: {sessions[delIdx].Id}");
            }
        }
        else
        {
            LoadSessionInteractive(sessions[idx].Id, sm);
        }
    }

    private static void LoadSessionInteractive(string id, ScreenManager sm)
    {
        var loaded = SessionManager.LoadSession(id);
        if (loaded == null) { sm.AddSystemMsg($"❌ 会话不存在: {id}"); return; }
        _agent!.Messages = loaded.Value.Messages;
        _llm!.Model = loaded.Value.Model;
        _config.Model = loaded.Value.Model;
        sm.StatusLeft = loaded.Value.Model;
        sm.ChatMessages.Clear();
        foreach (var msg in loaded.Value.Messages)
        {
            var role = msg["role"]?.GetValue<string>() ?? "";
            var content = msg["content"]?.GetValue<string>() ?? "";
            if (role == "user") sm.AddUserMsg(content);
            else if (role == "tool") sm.AddToolMsg("tool", content[..Math.Min(content.Length, 40)]);
            else if (role == "assistant") sm.ChatMessages.Add(new ScreenManager.ChatMsg { Role = "agent", Content = content });
        }
        sm.AddSystemMsg($"✅ 已加载会话: {id} (模型: {loaded.Value.Model})");
    }

    private static void ShowPermStatusInChat(ScreenManager sm)
    {
        var mode = PermissionManager.CurrentMode.ToString();
        sm.AddSystemMsg($"权限模式: {mode} (危险工具需确认: bash/write/edit/agent/kill/rm)");
    }
    private static void ShowDiffInChat(ScreenManager sm)
    {
        var files = EditFileTool.ChangedFiles;
        if (files.Count == 0) sm.AddSystemMsg("未修改任何文件");
        else foreach (var f in files) sm.AddSystemMsg($"  {f}");
    }

    /// <summary>搜索对话历史中的关键词。</summary>
    private static void SearchHistory(string input, ScreenManager sm)
    {
        var keyword = input.Length > 9 ? input[9..].Trim() : "";
        if (string.IsNullOrWhiteSpace(keyword))
        {
            sm.AddSystemMsg("用法: /history <关键词> 或 Ctrl+R 交互搜索");
            return;
        }

        var results = new List<(int Index, string Role, string Preview)>();
        for (int i = 0; i < _agent!.Messages.Count; i++)
        {
            var msg = _agent.Messages[i];
            var role = msg["role"]?.GetValue<string>() ?? "";
            var content = msg["content"]?.GetValue<string>() ?? "";
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
            sm.AddSystemMsg($"未找到包含 \"{keyword}\" 的消息");
            return;
        }

        sm.AddSystemMsg($"🔍 \"{keyword}\" — {results.Count} 条结果:");
        foreach (var (idx, role, preview) in results.Take(15))
        {
            var roleIcon = role switch { "user" => "👤", "assistant" => "🤖", "tool" => "🔧", _ => "  " };
            sm.AddSystemMsg($"  #{idx} {roleIcon} {preview}");
        }
        if (results.Count > 15)
            sm.AddSystemMsg($"  ... 还有 {results.Count - 15} 条结果");
    }

    /// <summary>导出当前对话为 Markdown 文件。</summary>
    private static void ExportConversation(ScreenManager sm)
    {
        try
        {
            var dir = Path.Combine(Directory.GetCurrentDirectory(), ".corecoder");
            if (!Directory.Exists(dir)) Directory.CreateDirectory(dir);
            var filename = $"export_{DateTime.Now:yyyyMMdd_HHmmss}.md";
            var path = Path.Combine(dir, filename);

            var sb = new System.Text.StringBuilder();
            sb.AppendLine($"# WayCoder 对话导出");
            sb.AppendLine($"- 模型: {_config.Model}");
            sb.AppendLine($"- 时间: {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
            sb.AppendLine($"- 消息数: {_agent!.Messages.Count}");
            sb.AppendLine();
            sb.AppendLine("---");
            sb.AppendLine();

            foreach (var msg in _agent.Messages)
            {
                var role = msg["role"]?.GetValue<string>() ?? "";
                var content = msg["content"]?.GetValue<string>() ?? "";

                switch (role)
                {
                    case "user":
                        sb.AppendLine($"### 👤 User\n\n{content}\n");
                        break;
                    case "assistant":
                        if (!string.IsNullOrEmpty(content))
                            sb.AppendLine($"### 🤖 Assistant\n\n{content}\n");
                        break;
                    case "tool":
                        // 截断很长的工具输出
                        var toolContent = content.Length > 2000
                            ? content[..2000] + $"\n\n...（共 {content.Length} 字符）"
                            : content;
                        sb.AppendLine($"### 🔧 Tool\n\n```\n{toolContent}\n```\n");
                        break;
                }
            }

            File.WriteAllText(path, sb.ToString(), Encoding.UTF8);
            var size = new FileInfo(path).Length;
            sm.AddSystemMsg($"✅ 已导出: .corecoder/{filename} ({size / 1024}KB)");
        }
        catch (Exception ex)
        {
            sm.AddSystemMsg($"❌ 导出失败: {ex.Message}");
        }
    }

    // ========================================================================
    // /loop — 循环执行直到条件达成
    // ========================================================================

    /// <summary>
    /// /loop [最大轮次] 提示词 — 重复执行 Agent，直到输出含成功标记或达到上限。
    /// </summary>
    private static async Task RunLoopAsync(string args, ScreenManager sm)
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
            sm.AddSystemMsg("用法: /loop [最大轮次] 提示词");
            return;
        }

        sm.AddSystemMsg($"🔁 /loop 开始 (最多 {maxIter} 轮)");
        var startTime = DateTime.UtcNow;

        for (int iter = 1; iter <= maxIter; iter++)
        {
            sm.AddSystemMsg($"\n── 第 {iter}/{maxIter} 轮 ──");
            sm.StatusLeft = $"loop {iter}/{maxIter}";

            using var cts = new CancellationTokenSource();

            try
            {
                sm.StartAgentMsg();
                sm.Render();

                await _agent!.ChatAsync(prompt,
                    onToken: tok => { sm.AppendToken(tok); sm.Render(); },
                    onTool: (name, brief) =>
                    {
                        sm.FinishAgentMsg();
                        sm.AddToolMsg(name, brief.Length > 60 ? brief[..57] + "..." : brief);
                        sm.StartAgentMsg();
                        sm.Render();
                    },
                    cancellationToken: cts.Token);

                sm.FinishAgentMsg();
            }
            catch (OperationCanceledException)
            {
                sm.FinishAgentMsg();
                sm.AddSystemMsg("⚠ /loop 已中断");
                break;
            }
            catch (Exception ex)
            {
                sm.FinishAgentMsg();
                sm.AddSystemMsg($"⚠ 第 {iter} 轮出错: {ex.Message}");
                if (iter == maxIter) break;
                await Task.Delay(1000);
                continue;
            }

            // 检查最近一条 assistant 消息是否含成功标记
            var lastAssistant = _agent.Messages.LastOrDefault(m =>
                m["role"]?.GetValue<string>() == "assistant");
            var lastContent = lastAssistant?["content"]?.GetValue<string>() ?? "";

            var successMarkers = new[] { "SUCCESS", "成功", "✅", "PASS", "通过",
                "所有测试通过", "0 errors", "0 个错误", "编译成功", "构建成功" };
            var isSuccess = successMarkers.Any(m =>
                lastContent.Contains(m, StringComparison.OrdinalIgnoreCase));

            if (isSuccess)
            {
                var elapsed = (DateTime.UtcNow - startTime).TotalSeconds;
                sm.AddSystemMsg($"✅ 条件达成！{iter} 轮 / {elapsed:F1}s");
                Console.Write('\a');
                return;
            }

            // 注入继续指令
            prompt = $"上一轮结果未满足条件，请继续尝试。上次输出摘要：{lastContent[..Math.Min(lastContent.Length, 200)]}";
        }

        sm.AddSystemMsg($"⏰ 已达上限 {maxIter} 轮，/loop 结束");
    }

    // ========================================================================
    // /test — 分模块测试
    // ========================================================================

    /// <summary>
    /// /test <模块> — 运行特定模块的自测。
    /// 模块: all | tools | ui | git | config | memory | agent | review | mcp
    /// </summary>
    private static void RunModuleTest(string input, ScreenManager sm)
    {
        var module = input.Length > 5 ? input[5..].Trim() : "all";
        if (string.IsNullOrWhiteSpace(module)) module = "all";

        sm.AddSystemMsg($"🧪 开始测试模块: {module}");

        try
        {
            var results = SelfTest.RunModule(module);
            sm.AddSystemMsg(results);
        }
        catch (Exception ex)
        {
            sm.AddSystemMsg($"❌ 测试异常: {ex.Message}");
        }
    }

    // ========================================================================
    // 命令实现
    // ========================================================================

    /// <summary>项目初始化向导：创建 .corecoder/ 配置目录和模板文件。</summary>
    private static void RunInit()
    {
        var cwd = Directory.GetCurrentDirectory();
        var corecoderDir = Path.Combine(cwd, ".corecoder");

        Console.WriteLine("WayCoder 项目初始化");
        Console.WriteLine($"目录: {cwd}");
        Console.WriteLine();

        if (!Directory.Exists(corecoderDir))
        {
            Directory.CreateDirectory(corecoderDir);
            Console.WriteLine($"✅ 创建 .corecoder/");
        }
        else
        {
            Console.WriteLine("⏭ .corecoder/ 已存在");
        }

        // mcp_servers.json 模板
        var mcpPath = Path.Combine(corecoderDir, "mcp_servers.json");
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
        var promptPath = Path.Combine(corecoderDir, "prompt.md");
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
        var memoryPath = Path.Combine(corecoderDir, "memory.md");
        if (!File.Exists(memoryPath))
        {
            File.WriteAllText(memoryPath, "# 项目记忆\n\n", Encoding.UTF8);
            Console.WriteLine("✅ 创建 memory.md (项目记忆)");
        }

        Console.WriteLine();
        Console.WriteLine("初始化完成！现在可以运行 waycoder 开始编码。");
    }

    private static void ShowUsage()
    {
        MarkupLine("[bold yellow]WayCoder (道码)[/] — 中文版易用编程智能体");
        Console.WriteLine();
        MarkupLine("[bold]使用方法:[/] [cyan]waycoder [[选项]][/]");
        Console.WriteLine();
        MarkupLine("  [bold]选项:[/]");
        MarkupLine("  [cyan]-m, --model[/] <名称>   模型名称 (默认: deepseek-v4-flash)");
        MarkupLine("  [cyan]--base-url[/] <URL>     API 基础 URL");
        MarkupLine("  [cyan]--api-key[/] <密钥>     API 密钥");
        MarkupLine("  [cyan]-p, --prompt[/] <文本>  一次性提示词 (非交互模式)");
        MarkupLine("  [cyan]-r, --resume[/] <ID>    恢复已保存的会话");
        MarkupLine("  [cyan]-v, --version[/]        显示版本信息");
        MarkupLine("  [cyan]--init[/]              初始化项目 (.waycoder/ 配置目录)");
        MarkupLine("  [cyan]-t, --test[/]           运行自测");
        MarkupLine("  [cyan]--debug[/]              开启调试日志 (记录到 logs/ 目录)");
        MarkupLine("  [cyan]--yolo[/]              跳过所有权限确认 (非交互模式必备)");
        MarkupLine("  [cyan]--max-budget-usd[/] <金额> 费用上限（美元），超支自动停止");
#if TERMINAL_GUI
        MarkupLine("  [cyan]--tui-v2[/]             Terminal.Gui 实验性 TUI (仅 Debug)");
#endif
        MarkupLine("  [cyan]-h, --help[/]           显示此帮助");
        Console.WriteLine();
        MarkupLine("  [bold]示例:[/]");
        MarkupLine("  [dim]$[/] waycoder                                     [dim]# 交互式 REPL[/]");
        MarkupLine("  [dim]$[/] waycoder [cyan]-p[/] [green]\"列出当前目录\"[/]               [dim]# 一次性模式[/]");
        MarkupLine("  [dim]$[/] waycoder [cyan]-m[/] deepseek-v4-pro             [dim]# 指定模型[/]");
        MarkupLine("  [dim]$[/] waycoder [cyan]-t[/]                              [dim]# 运行自测[/]");
        MarkupLine("  [dim]$[/] echo [green]\"列出目录\"[/] [dim]|[/] waycoder                   [dim]# 管道模式[/]");
    }

    private static void ShowHelp()
    {
        var table = new TuiTable("命令");
        table.AddColumn("命令");
        table.AddColumn("说明");

        // 内置命令
        table.AddRow("/help", "显示此帮助");
        table.AddRow("/reset", "清空对话历史");
        table.AddRow("/model", "显示当前模型");
        table.AddMarkupRow($"[{TuiColors.AccentMarkup}]/model[/] [dim]&lt;名称&gt;[/]", "切换模型");
        table.AddRow("/tokens", "显示 Token 用量");
        table.AddRow("/compact", "压缩上下文");
        table.AddRow("/diff", "修改文件列表");
        table.AddRow("/save", "保存会话");
        table.AddRow("/resume", "恢复上次自动保存的会话");
        table.AddRow("/history", "搜索对话历史 (Ctrl+R)");
        table.AddRow("/export", "导出对话为 Markdown 文件");
        table.AddRow("/sessions", "会话管理 (浏览/加载/删除)");
        table.AddMarkupRow($"[{TuiColors.AccentMarkup}]/load[/] [dim]&lt;ID&gt;[/]", "加载指定会话");
        table.AddRow("/debug-on / -off", "开启/关闭调试日志");
        table.AddRow("/permissions", "权限管理");
        table.AddMarkupRow($"[{TuiColors.AccentMarkup}]/perm[/] [dim]&lt;suggest|auto-edit|full-auto&gt;[/]", "设置沙箱级别");
        table.AddRow("/plan", "计划模式");
        table.AddRow("/todo", "查看任务列表");
        table.AddRow("/git-status", "Git 状态");
        table.AddRow("/git-log", "Git 日志");
        table.AddRow("/git-diff", "Git 差异");
        table.AddRow("/review", "代码审查");
        table.AddRow("/lint", "运行 lint 检查");
        table.AddMarkupRow($"[{TuiColors.AccentMarkup}]/search[/] [dim]&lt;关键词&gt;[/]", "网页搜索");
        table.AddRow("/checkpoint", "创建检查点");
        table.AddMarkupRow($"[{TuiColors.AccentMarkup}]/undo[/] [dim][[编号]][/]", "回退检查点");
        table.AddRow("/checkpoints", "列出检查点");
        table.AddRow("/repomap", "刷新仓库地图");
        table.AddMarkupRow($"[{TuiColors.AccentMarkup}]/pr[/] [dim][[标题]][/]", "创建 Pull Request");
        table.AddMarkupRow($"[{TuiColors.AccentMarkup}]/edit[/] [dim][[文件]][/]", "终端源码编辑器");
        table.AddRow("/settings", "设置界面");
        table.AddRow("quit", "退出");

        // 自定义命令
        if (CustomCommands.Commands.Count > 0)
        {
            foreach (var (name, cmd) in CustomCommands.Commands)
            {
                var desc = cmd.Description.Length > 20
                    ? cmd.Description[..17] + "..."
                    : cmd.Description;
                table.AddRow($"/{name}", desc);
            }
        }

        table.Render();
    }

    private static void ShowTokens()
    {
        var p = _llm!.TotalPromptTokens;
        var c = _llm!.TotalCompletionTokens;
        var total = p + c;
        var cost = _llm.EstimatedCost;

        var content = new StringBuilder();
        content.AppendLine($"[{TuiColors.AccentMarkup}]{p:N0}[/] 输入 " +
            $"+ [{TuiColors.AccentMarkup}]{c:N0}[/] 输出 " +
            $"= [bold {TuiColors.SuccessMarkup}]{total:N0}[/] 总计");
        if (cost != null)
            content.Append($"约 [dim]${cost:F4}[/]");

        TuiBox.Info("Token 用量", content.ToString().TrimEnd());
    }

    private static void SwitchModel(string input)
    {
        var newModel = input[7..].Trim();
        if (!string.IsNullOrEmpty(newModel))
        {
            _llm!.Model = newModel;
            _config.Model = newModel;
            MarkupLine($"[green]✔ 已切换到:[/] [bold cyan]{E(newModel)}[/]");
        }
    }

    /// <summary>Ctrl+M 循环切换大模型</summary>
    private static void CycleModel(ScreenManager sm)
    {
        var models = new[] { "deepseek-v4-flash", "deepseek-v4-pro", "gpt-5.4-mini", "gpt-5.4" };
        var cur = _config.Model;
        var idx = Array.IndexOf(models, cur);
        var next = models[(idx + 1) % models.Length];
        _llm!.Model = next;
        _config.Model = next;
        sm.StatusLeft = $"大:{_config.Model} 小:{_config.SmallModel}";
        sm.AddSystemMsg($"🔄 大模型 → {next} (Ctrl+M 继续切换)");
    }

    private static async Task CompactAsync()
    {
        var before = ContextManager.EstimateTokens(_agent!.Messages);
        var maxTokens = _agent.Context.MaxTokens;
        AnsiConsole.MarkupLine($"[dim]压缩前: {before:N0} / {maxTokens:N0} tokens[/]");

        var lastLayer = 0;
        var compressed = await _agent.Context.MaybeCompressAsync(_agent.Messages, _llm,
            onProgress: (layer, msg) =>
        {
            if (layer != lastLayer)
            {
                AnsiConsole.MarkupLine($"[cyan]▶ 第 {layer} 层:[/] [dim]{E(msg)}[/]");
                lastLayer = layer;
            }
        });

        var after = ContextManager.EstimateTokens(_agent.Messages);
        var pct = before > 0 ? (int)((before - after) * 100.0 / before) : 0;
        if (compressed)
        {
            AnsiConsole.MarkupLine(
                $"[green]✔ 已压缩:[/] {before:N0} → [cyan]{after:N0}[/] tokens " +
                $"([green]{pct}%[/] 释放, [dim]{_agent.Messages.Count} 条消息[/])");
            var barPct = after * 100.0 / maxTokens;
            AnsiConsole.MarkupLine($"  [dim]上下文使用率:[/] {BoxBuffer.MiniBar(barPct, 10)}");
        }
        else
            AnsiConsole.MarkupLine($"[dim]无需压缩 ({before:N0} tokens, {_agent.Messages.Count} 条消息)[/]");
    }

    private static void SaveSession()
    {
        var sid = SessionManager.SaveSession(_agent!.Messages, _config.Model);
        MarkupLine($"[green]✔ 会话已保存:[/] [cyan]{E(sid)}[/]");
        MarkupLine($"[dim]恢复命令: waycoder -r {E(sid)}[/]");
    }

    private static void ShowDiff()
    {
        var files = EditFileTool.ChangedFiles;
        if (files.Count == 0)
        {
            MarkupLine("[dim]未修改任何文件[/]");
            return;
        }

        var table = new TuiTable($"修改的文件 ({files.Count} 个)");
        table.AddColumn("文件路径");
        foreach (var f in files.OrderBy(f => f))
            table.AddRow(f);
        table.Render();
    }

    // ========================================================================
    // 输入触发式智能提示
    // ========================================================================

    /// <summary>/ 触发：弹出命令面板，用方向键选择，回车执行</summary>
    private static string ShowCommandPalette()
    {
        var commands = new List<string>
        {
            "/help", "/reset", "/model", "/model <名称>", "/tokens",
            "/compact", "/diff", "/save", "/resume", "/sessions",
            "/debug-on", "/debug-off", "/permissions", "/perm <suggest|auto-edit|full-auto>",
            "/plan", "/todo", "/git-status", "/git-log", "/git-diff",
            "/review", "/lint", "/search <关键词>",
            "/checkpoint", "/undo [编号]", "/checkpoints",
            "/repomap", "/pr [标题]", "/edit [文件]", "/settings", "quit",
        };

        // 追加自定义命令
        foreach (var (name, _) in CustomCommands.Commands)
            commands.Add($"/{name}");

        var choice = TuiList.Select("命令面板 ↑↓ 选择 Enter 执行 Esc 取消", commands);
        if (choice == null) return "";

        // 对于带参数的命令，截取命令名
        var spaceIdx = choice.IndexOf(' ');
        return spaceIdx > 0 ? choice[..spaceIdx] : choice;
    }

    /// <summary>! 触发：输入 Shell 命令并立即执行</summary>
    private static async Task<string> RunShellOnceAsync()
    {
        var cmd = TuiPrompt.Ask("! 命令");
        if (string.IsNullOrWhiteSpace(cmd)) return "";

        try
        {
            var result = await new Tools.BashTool().ExecuteAsync(
                new Dictionary<string, object?> { ["command"] = cmd });
            Console.WriteLine(result);
        }
        catch (Exception ex)
        {
            TuiBox.Error("Shell 错误", ex.Message);
        }
        return ""; // 不回传给 Agent
    }

    /// <summary># 触发：显示工程文件列表，辅助输入</summary>
    private static async Task<string> ShowFileHintAsync()
    {
        // 快速显示目录结构
        try
        {
            var treeResult = await new Tools.TreeTool().ExecuteAsync(
                new Dictionary<string, object?> { ["depth"] = 2, ["max"] = 30 });
            // 只显示前 20 行
            var lines = treeResult.Split('\n').Take(20);
            AnsiConsole.MarkupLine($"[{TuiColors.DimMarkup}]── 工程文件 (前 20 行) ──[/]");
            foreach (var line in lines)
                Console.WriteLine($"  [{TuiColors.DimMarkup}]{TuiHelper.Esc(line)}[/]");
            Console.WriteLine();
        }
        catch { /* 静默失败 */ }

        var file = TuiPrompt.Ask("# 文件");
        return string.IsNullOrWhiteSpace(file) ? "" : $"#{file}";
    }

    private static void ShowSessions()
    {
        var sessions = SessionManager.ListSessions();
        if (sessions.Count == 0)
        {
            MarkupLine("[dim]没有已保存的会话[/]");
            return;
        }

        var table = new TuiTable($"已保存的会话 ({sessions.Count} 个)");
        table.AddColumn("ID", 12);
        table.AddColumn("模型", 20);
        table.AddColumn("保存时间", 20);
        table.AddColumn("预览");

        foreach (var s in sessions)
            table.AddRow(s.Id, s.Model, s.SavedAt, s.Preview);

        table.Render();
    }

    // ========================================================================
    // 计划模式 / Todo / Git 命令
    // ========================================================================

    private static async Task PlanModeAsync()
    {
        MarkupLine("[bold cyan]📋 计划模式[/] — Agent 将先规划再执行");
        MarkupLine("[dim]输入你的需求，Agent 会先分析并列出执行计划[/]");
        Console.WriteLine();

        var userInput = TuiInput.ReadInput();
        if (string.IsNullOrWhiteSpace(userInput)) return;

        var planPrompt = $"请分析以下需求，先列出详细的执行计划（分步骤、涉及的文件、可能的风险），再逐步执行。\n\n需求：{userInput}\n\n请先输出计划，然后逐步执行。";

        using var cts = new CancellationTokenSource();
        try
        {
            await ChatWithStatusAsync(planPrompt, cts.Token);
            Console.WriteLine();
        }
        catch (Exception ex)
        {
            TuiBox.Error("错误", ex.Message);
        }
    }

    private static void ShowTodo()
    {
        var items = TodoTool.Items;
        if (items.Count == 0)
        {
            MarkupLine("[dim]（暂无任务）[/]");
            return;
        }

        var completed = items.Count(i => i.Status == "completed");
        var table = new TuiTable($"任务列表 ({completed}/{items.Count} 完成)");
        table.AddColumn("#", 5);
        table.AddColumn("状态", 14);
        table.AddColumn("标题");

        var statusMarkup = new Dictionary<string, string>
        {
            ["completed"] = $"[{TuiColors.SuccessMarkup}]✅ 已完成[/]",
            ["in_progress"] = $"[{TuiColors.AccentMarkup}]🔄 进行中[/]",
            ["pending"] = $"[{TuiColors.WarnMarkup}]⏳ 待处理[/]",
            ["cancelled"] = $"[{TuiColors.DimMarkup}]❌ 已取消[/]",
        };

        foreach (var item in items.OrderBy(i => i.Id))
        {
            var status = statusMarkup.GetValueOrDefault(item.Status, $"❓ {item.Status}");
            table.AddMarkupRow($"#{item.Id}", status, TuiHelper.Esc(item.Title));
        }

        table.Render();
    }

    private static async Task RunReviewAsync()
    {
        var reviewPrompt = ReviewMode.BuildReviewPrompt();
        if (reviewPrompt.StartsWith("（没有修改过"))
        {
            MarkupLine($"[dim]{E(reviewPrompt)}[/]");
            return;
        }

        MarkupLine("[bold cyan]🔍 代码审查中...[/]");
        using var cts = new CancellationTokenSource();
        try
        {
            await ChatWithStatusAsync(reviewPrompt, cts.Token);
            Console.WriteLine();
        }
        catch (Exception ex)
        {
            TuiBox.Error("审查出错", ex.Message);
        }
    }

    private static async Task RunGitAsync(string command)
    {
        MarkupLine($"[dim]git {E(command)}[/]");
        var result = await new Tools.GitTool().ExecuteAsync(new() { ["command"] = command });
        Console.WriteLine(result);
    }

    // ========================================================================
    // Lint / Search / Checkpoint 命令 (v0.6.1+)
    // ========================================================================

    private static async Task RunLintAsync()
    {
        MarkupLine("[bold cyan]🔍 Lint 检查中...[/]");
        var lintTool = new Tools.LintTool();
        var result = await lintTool.ExecuteAsync(new Dictionary<string, object?>());
        Console.WriteLine(result);
    }

    private static async Task RunSearchAsync(string query)
    {
        if (string.IsNullOrWhiteSpace(query))
        {
            MarkupLine("[orange3]用法: /search <关键词>[/]");
            return;
        }
        MarkupLine($"[bold cyan]🔍 搜索: {E(query)}[/]");
        var searchTool = new Tools.WebSearchTool();
        var result = await searchTool.ExecuteAsync(new Dictionary<string, object?> { ["query"] = query });
        Console.WriteLine(result);
    }

    private static async Task CreateCheckpointAsync()
    {
        MarkupLine("[bold cyan]📦 创建检查点...[/]");
        var cp = await CheckpointManager.CreateAsync("手动创建");
        if (cp != null)
            MarkupLine($"[green]✔ 检查点 #{cp.Id} 已创建[/] [dim]({cp.Type})[/]");
        else
            MarkupLine("[red]✘ 检查点创建失败[/]");
    }

    private static async Task UndoCheckpointAsync(string input)
    {
        var parts = input.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        int? id = null;
        if (parts.Length > 1 && int.TryParse(parts[1], out var parsed))
            id = parsed;

        MarkupLine("[bold orange3]⏪ 回退中...[/]");
        var result = await CheckpointManager.UndoAsync(id);
        Console.WriteLine(result);
    }

    private static void ShowCheckpoints()
    {
        MarkupLine("[bold]检查点列表:[/]");
        Console.WriteLine(CheckpointManager.ListCheckpoints());
    }

    private static void ShowRepoMap()
    {
        MarkupLine("[bold]仓库地图 (刷新中...)[/]");
        RepoMapGenerator.Invalidate();
        var map = RepoMapGenerator.Generate();
        Console.WriteLine(map);
    }

    private static async Task RunPRAsync(string input)
    {
        var parts = input.Split(' ', 2);
        var title = parts.Length > 1 ? parts[1].Trim() : "";
        if (string.IsNullOrEmpty(title))
        {
            // 仅显示 PR 链接
            var prTool = new GitPRTool();
            var result = await prTool.ExecuteAsync(new Dictionary<string, object?> { ["action"] = "url" });
            Console.WriteLine(result);
            return;
        }
        else
        {
            var prTool = new GitPRTool();
            var result = await prTool.ExecuteAsync(new Dictionary<string, object?>
            {
                ["action"] = "create",
                ["title"] = title,
                ["description"] = $"🤖 Generated with [WayCoder/道码](https://github.com/alecksty/waycoder)"
            });
            Console.WriteLine(result);
        }
    }

    // ========================================================================
    /// <summary>构建模型回退链：当前模型 + 备选模型</summary>
    private static string[] BuildFallbackChain()
    {
        var primary = _config.Model;
        var fallbacks = new List<string> { primary };
        foreach (var fb in new[] { "deepseek-v4-flash", "gpt-5.4-mini", "deepseek-v4-pro", "gpt-5.4" })
            if (fb != primary && !fallbacks.Contains(fb))
                fallbacks.Add(fb);
        return fallbacks.ToArray();
    }

    // 辅助方法: 安全的 Spectre.Console 输出 + 状态动画
    // ========================================================================

    /// <summary>转义用户内容中的 Spectre 标记字符</summary>
    private static string E(string? text) => Markup.Escape(text ?? "");

    /// <summary>输出带标记的行（纯静态标记，无动态内容）</summary>
    private static void MarkupLine(string markup) => AnsiConsole.MarkupLine(markup);

    /// <summary>输出带标记的文本（不换行）</summary>
    private static void M(string markup) => AnsiConsole.Markup(markup);

    /// <summary>
    /// 带旋转动画 + 超时提示的 ChatAsync 包装器。
    /// 等待 LLM 时显示 "⠋ 思考中..." 旋转动画，网络卡顿无响应时有进度提示。
    /// </summary>
    private static async Task<string> ChatWithStatusAsync(
        string userInput,
        CancellationToken ct,
        Action<bool>? setStreamed = null)
    {
        // ANSI 控制序列 (显式 ESC 字节, 兼容所有终端)
        const string DimOn = "[2m";
        const string DimOff = "[0m";
        const string ClearLine = "[2K";
        var spinnerFrames = new[] { "⠋", "⠙", "⠹", "⠸", "⠼", "⠴", "⠦", "⠧", "⠇", "⠏" };
        var spinnerActive = false;
        var startTime = DateTime.UtcNow;
        CancellationTokenSource? spinnerCts = null;

        void StartSpinner()
        {
            if (spinnerActive) return;
            spinnerActive = true;
            startTime = DateTime.UtcNow;
            spinnerCts = new CancellationTokenSource();
            var token = spinnerCts.Token;
            _ = Task.Run(async () =>
            {
                var i = 0;
                while (!token.IsCancellationRequested)
                {
                    var elapsed = (DateTime.UtcNow - startTime).TotalSeconds;
                    var frame = spinnerFrames[i % spinnerFrames.Length];
                    string status;
                    if (elapsed > 60)
                        status = $"{frame} 响应缓慢, 请耐心等待... ({elapsed:F0}s)";
                    else if (elapsed > 30)
                        status = $"{frame} 等待响应中... ({elapsed:F0}s)";
                    else if (elapsed > 15)
                        status = $"{frame} 思考中... ({elapsed:F0}s)";
                    else
                        status = $"{frame} 思考中...";

                    // 清行 + 回行首 + 动画帧 (直接写 stdout，绕过 Spectre 管线)
                    System.Console.Write($"\r{ClearLine}  {DimOn}{status}{DimOff}");
                    System.Console.Out.Flush();
                    i++;
                    try { await Task.Delay(120, token); }
                    catch (OperationCanceledException) { break; }
                }
            }, token);
        }

        void StopSpinner()
        {
            if (!spinnerActive) return;
            spinnerActive = false;
            spinnerCts?.Cancel();
            Console.Write("\r" + new string(' ', 60) + "\r");
            Console.Out.Flush();
        }

        // 初始动画
        StartSpinner();

        var response = await _agent!.ChatAsync(userInput,
            onToken: tok =>
            {
                StopSpinner();
                Console.Write(tok);
                if (setStreamed != null) setStreamed(true);
            },
            onTool: (name, brief) =>
            {
                StopSpinner();
                Console.WriteLine(); // 结束上一行流式输出
                var shortBrief = brief.Length > 60 ? brief[..57] + "..." : brief;
                AnsiConsole.MarkupLine($"  [dim]🔧 {E(name)}({E(shortBrief)})[/]");
                StartSpinner(); // 下一轮 LLM 等待
            },
            cancellationToken: ct);

        // 清除最后一轮动画
        StopSpinner();

        return response;
    }
}
