using System.Collections.ObjectModel;
using System.Text;
using CoreCoderSharp.Tools;
using Terminal.Gui;

namespace CoreCoderSharp.UI;

/// <summary>
/// Terminal.Gui v2 版 REPL — 默认 TUI，替代手写 ANSI 转义码。
/// 使用 --tui-v1 可回退到旧版 ScreenManager TUI。
/// </summary>
public class TerminalGuiRepl
{
    // === 依赖 ===
    private readonly Agent _agent;
    private readonly LLM _llm;
    private readonly Config _config;

    // === 视图 ===
    private Window _mainWin = null!;
    private TextView _chatView = null!;
    private TextView _inputView = null!;
    private Terminal.Gui.Label _tokenLabel = null!;
    private StatusBar _statusBar = null!;
    private FrameView _sidePanel = null!;
    private TabView _sideTabs = null!;
    private ListView _todoList = null!;
    private ListView _filesList = null!;
    private ListView _locksList = null!;
    private ListView _mcpList = null!;

    // === 状态 ===
    private readonly List<string> _inputHistory = [];
    private int _historyIdx = -1;
    private CancellationTokenSource? _currentCts;
    private int _streamingLineStart = -1;
    private bool _panelVisible;
    private (List<JsonObject> Messages, string Model)? _pendingRestore;

    public TerminalGuiRepl(Agent agent, LLM llm, Config config)
    {
        _agent = agent;
        _llm = llm;
        _config = config;
    }

    // ================================================================
    // 入口
    // ================================================================

    public void Run()
    {
        Application.Init();
        BuildUI();
        ShowWelcome();
        TryAutoResume();

        Application.Run(_mainWin, OnError);

        // 退出时自动保存
        AutoSaveSession();
        Application.Shutdown();
    }

    /// <summary>尝试恢复上次自动保存的会话。</summary>
    private void TryAutoResume()
    {
        try
        {
            var auto = SessionManager.LoadSession("_auto");
            if (auto == null) return;
            var count = auto.Value.Messages.Count;
            AppendSystem($"💾 发现上次会话 ({count} 条消息)。输入 /resume 恢复，或忽略此消息开始新会话。");
            _pendingRestore = auto;
        }
        catch { /* 恢复失败不影响启动 */ }
    }

    /// <summary>退出时自动保存会话。</summary>
    private void AutoSaveSession()
    {
        try
        {
            if (_agent.Messages.Count == 0) return;
            var hasUser = _agent.Messages.Any(m => (string?)m["role"] == "user");
            if (!hasUser) return;
            SessionManager.SaveSession(_agent.Messages, _config.Model, "_auto");
        }
        catch { /* 静默失败 */ }
    }

    private static bool OnError(Exception ex)
    {
        Console.Error.WriteLine($"Terminal.Gui error: {ex.Message}");
        return true; // continue running
    }

    // ================================================================
    // UI 构建
    // ================================================================

    private void BuildUI()
    {
        // 全局配色：所有视图默认黑底白字
        var scheme = new ColorScheme
        {
            Normal = new Terminal.Gui.Attribute(Color.White, Color.Black),
            Focus = new Terminal.Gui.Attribute(Color.BrightYellow, Color.DarkGray),
            HotNormal = new Terminal.Gui.Attribute(Color.Cyan, Color.Black),
            HotFocus = new Terminal.Gui.Attribute(Color.BrightCyan, Color.DarkGray),
            Disabled = new Terminal.Gui.Attribute(Color.Gray, Color.Black),
        };

        // 聊天区专用：黑底白字
        var chatScheme = new ColorScheme
        {
            Normal = new Terminal.Gui.Attribute(Color.White, Color.Black),
            Focus = new Terminal.Gui.Attribute(Color.White, Color.Black),
            HotNormal = new Terminal.Gui.Attribute(Color.Cyan, Color.Black),
            HotFocus = new Terminal.Gui.Attribute(Color.BrightYellow, Color.DarkGray),
            Disabled = new Terminal.Gui.Attribute(Color.Gray, Color.Black),
        };

        _mainWin = new Window
        {
            Title = $"WayCoder 道码 v0.17.3 — {_config.Model}",
            X = 0, Y = 0,
            Width = Dim.Fill(),
            Height = Dim.Fill(),
            ColorScheme = scheme,
        };

        // --- 聊天区 ---
        _chatView = new TextView
        {
            X = 0, Y = 0,
            Width = Dim.Fill(),
            Height = Dim.Fill() - 5,
            ReadOnly = true,
            WordWrap = true,
            ColorScheme = chatScheme,
        };

        // --- 输入区 ---
        _inputView = new TextView
        {
            X = 0,
            Y = Pos.AnchorEnd(4),
            Width = Dim.Fill(),
            Height = 3,
            ColorScheme = scheme,
        };
        _inputView.KeyDown += OnInputKeyDown;
        _inputView.SetFocus();

        // --- Token 标签 ---
        _tokenLabel = new Terminal.Gui.Label
        {
            X = 0,
            Y = Pos.AnchorEnd(1),
            Width = Dim.Fill(),
            Height = 1,
            Text = " 就绪",
            ColorScheme = scheme,
        };

        // --- 状态栏 ---
        _statusBar = new StatusBar(new Shortcut[]
        {
            new(Key.F1, "帮助", () => ShowHelp(), ""),
            new(Key.F2, "面板", () => ToggleSidePanel(), ""),
            new(Key.F5, "设置", () => SettingsPage.Show(), ""),
            new(Key.F6, "编辑", () => _ = Editor.PickAndRunAsync(), ""),
            new(Key.F10, "退出", () => Application.RequestStop(), ""),
        });

        // --- 侧边面板 (默认隐藏) ---
        _sidePanel = new FrameView
        {
            Title = "面板",
            X = Pos.AnchorEnd(32),
            Y = 0,
            Width = 32,
            Height = Dim.Fill() - 5,
            Visible = false,
            BorderStyle = LineStyle.Rounded,
        };

        _sideTabs = new TabView
        {
            X = 0, Y = 0,
            Width = Dim.Fill(),
            Height = Dim.Fill(),
        };

        // 任务 Tab
        _todoList = new ListView { X = 0, Y = 0, Width = Dim.Fill(), Height = Dim.Fill() };
        var taskTab = new Tab { Text = "任务" };
        taskTab.Add(_todoList);
        _sideTabs.AddTab(taskTab, false);

        // 文件 Tab
        _filesList = new ListView { X = 0, Y = 0, Width = Dim.Fill(), Height = Dim.Fill() };
        var fileTab = new Tab { Text = "文件" };
        fileTab.Add(_filesList);
        _sideTabs.AddTab(fileTab, false);

        // 锁 Tab
        _locksList = new ListView { X = 0, Y = 0, Width = Dim.Fill(), Height = Dim.Fill() };
        var lockTab = new Tab { Text = "锁" };
        lockTab.Add(_locksList);
        _sideTabs.AddTab(lockTab, false);

        // MCP Tab
        _mcpList = new ListView { X = 0, Y = 0, Width = Dim.Fill(), Height = Dim.Fill() };
        var mcpTab = new Tab { Text = "MCP" };
        mcpTab.Add(_mcpList);
        _sideTabs.AddTab(mcpTab, false);

        _sidePanel.Add(_sideTabs);

        _mainWin.Add(_chatView, _inputView, _tokenLabel);
        // SidePanel 叠加在右侧，StatusBar 钉在底部
        _mainWin.Add(_sidePanel);
        _mainWin.Add(_statusBar);
    }

    // ================================================================
    // 欢迎屏
    // ================================================================

    private void ShowWelcome()
    {
        AppendLine("██╗    ██╗ █████╗ ██╗   ██╗");
        AppendLine("██║    ██║██╔══██╗╚██╗ ██╔╝");
        AppendLine("██║ █╗ ██║███████║ ╚████╔╝ ");
        AppendLine("██║███╗██║██╔══██║  ╚██╔╝  ");
        AppendLine("╚███╔███╔╝██║  ██║   ██║   ");
        AppendLine(" ╚══╝╚══╝ ╚═╝  ╚═╝   ╚═╝   ");
        AppendLine("WayCoder 道码 · 中文版易用编程智能体 · v0.17.3");
        AppendLine("深圳市探索智能科技有限公司");
        AppendLine($"大模型: {_config.Model} · 小模型: {_config.SmallModel}  · /help 帮助");

        var branch = DetectGitBranch();
        if (branch != null)
            AppendLine($"Git 分支: {branch}");

        UpdateTokenDisplay();
    }

    // ================================================================
    // 聊天输出
    // ================================================================

    private void AppendLine(string text)
    {
        _chatView.Text += text + "\n";
        _chatView.MoveEnd();
    }

    private void AppendUser(string text) => AppendLine("❯ " + text);
    private void AppendSystem(string text)
    {
        foreach (var line in text.Split('\n'))
            AppendLine("  " + line);
    }

    private void AppendTool(string text) => AppendLine("  🔧 " + text);

    private void StartAssistantStream()
    {
        _streamingLineStart = _chatView.Text.Length;
    }

    private void AppendStreamToken(string token)
    {
        if (_streamingLineStart < 0)
            StartAssistantStream();
        _chatView.Text += token;
        _chatView.MoveEnd();
    }

    private void FinishAssistantStream()
    {
        _chatView.Text += "\n";
        _streamingLineStart = -1;
        _chatView.MoveEnd();
    }

    // ================================================================
    // 键盘输入处理
    // ================================================================

    private async void OnInputKeyDown(object? sender, Key key)
    {
        // Enter (no Ctrl) → 发送
        if (key == Key.Enter && !key.IsCtrl)
        {
            key.Handled = true;
            await SendInputAsync();
            return;
        }

        // Ctrl+Enter → 插入换行
        if (key == Key.Enter && key.IsCtrl)
        {
            key.Handled = true;
            _inputView.Text += "\n";
            _inputView.MoveEnd();
            _inputView.SetNeedsDraw();
            return;
        }

        // Escape: 输入为空→退出REPL, 输入非空→清空
        if (key == Key.Esc)
        {
            var text = _inputView.Text?.ToString() ?? "";
            if (string.IsNullOrWhiteSpace(text))
                return; // let TextView handle
            key.Handled = true;
            _inputView.Text = "";
            _inputView.SetNeedsDraw();
            return;
        }

        // Up/Down → 输入历史导航 (仅单行输入时)
        if (key == Key.CursorUp || key == Key.CursorDown)
        {
            var text = _inputView.Text?.ToString() ?? "";
            if (!text.Contains('\n') && _inputHistory.Count > 0)
            {
                key.Handled = true;
                if (key == Key.CursorUp)
                    NavigateHistoryUp();
                else
                    NavigateHistoryDown();
                return;
            }
            // 多行输入时让 TextView 处理光标移动
        }

        // Ctrl+R → 搜索历史
        if (key.KeyCode == KeyCode.R && key.IsCtrl)
        {
            key.Handled = true;
            SearchHistory();
            return;
        }

        // Ctrl+M → 切换模型
        if (key.KeyCode == KeyCode.M && key.IsCtrl)
        {
            key.Handled = true;
            CycleModel();
            return;
        }

        // F2 → 侧边面板
        if (key == Key.F2)
        {
            key.Handled = true;
            ToggleSidePanel();
            return;
        }

        // PageUp/PageDown → 滚动聊天区
        if (key == Key.PageUp) { ChatScrollUp(5); key.Handled = true; return; }
        if (key == Key.PageDown) { ChatScrollDown(5); key.Handled = true; return; }

        // Ctrl+Home/End → 跳到顶部/底部
        if (key.KeyCode == KeyCode.Home && key.IsCtrl) { ChatScrollTop(); key.Handled = true; return; }
        if (key.KeyCode == KeyCode.End && key.IsCtrl) { ChatScrollBottom(); key.Handled = true; return; }
    }

    private void NavigateHistoryUp()
    {
        if (_historyIdx == -1)
            _historyIdx = _inputHistory.Count - 1;
        else if (_historyIdx > 0)
            _historyIdx--;
        _inputView.Text = _inputHistory[_historyIdx];
        _inputView.MoveEnd();
        _inputView.SetNeedsDraw();
    }

    private void NavigateHistoryDown()
    {
        if (_historyIdx < 0) return;
        _historyIdx++;
        if (_historyIdx >= _inputHistory.Count)
        {
            _historyIdx = -1;
            _inputView.Text = "";
        }
        else
        {
            _inputView.Text = _inputHistory[_historyIdx];
            _inputView.MoveEnd();
        }
        _inputView.SetNeedsDraw();
    }

    private void ChatScrollUp(int lines)
    {
        var row = _chatView.CursorPosition.Y - lines;
        _chatView.ScrollTo(Math.Max(0, row), false);
    }

    private void ChatScrollDown(int lines)
    {
        var row = _chatView.CursorPosition.Y + lines;
        _chatView.ScrollTo(row, false);
    }

    private void ChatScrollTop()
    {
        _chatView.ScrollTo(0, false);
    }

    private void ChatScrollBottom()
    {
        _chatView.MoveEnd();
    }

    private async Task SendInputAsync()
    {
        var text = _inputView.Text?.ToString()?.TrimEnd() ?? "";
        if (string.IsNullOrWhiteSpace(text))
        {
            _inputView.Text = "";
            _inputView.SetNeedsDraw();
            return;
        }

        // 输入历史
        if (_inputHistory.Count == 0 || _inputHistory[^1] != text)
            _inputHistory.Add(text);
        if (_inputHistory.Count > 200) _inputHistory.RemoveAt(0);
        _historyIdx = -1;

        AppendUser(text);
        _inputView.Text = "";
        _inputView.SetNeedsDraw();

        await ProcessInputAsync(text);
    }

    // ================================================================
    // 命令处理
    // ================================================================

    private async Task ProcessInputAsync(string input)
    {
        // 全角规范化
        input = input.Replace('／', '/').Replace('！', '!').Replace('＃', '#');

        // 命令别名
        input = input switch
        {
            "/c" => "/compact", "/m" => "/model", "/r" => "/reset",
            "/h" => "/help", "/t" => "/tokens", "/d" => "/diff",
            "/s" => "/save", "/q" => "quit",
            _ => input,
        };

        // 斜杠命令拼写纠错
        if (input.StartsWith('/'))
        {
            var corrected = Program.SuggestCommand(input);
            if (corrected != null && corrected != input)
            {
                AppendSystem($"💡 命令 [{input}] 未识别，已纠正为 [{corrected}]");
                input = corrected;
            }
        }

        var lower = input.ToLowerInvariant();

        // 退出
        if (lower is "quit" or "exit" or "/quit" or "/exit")
        {
            Application.RequestStop();
            return;
        }

        // 内置命令
        switch (lower)
        {
            case "/help": ShowHelp(); return;
            case "/reset": _agent.Reset(); AppendSystem("♻ 对话已重置"); return;
            case "/tokens": ShowTokens(); return;
            case "/stats": ShowStats(); return;
            case "/model": AppendSystem($"当前模型: {_config.Model}"); return;
        }

        if (input == "/compact") { await CompactAsync(); return; }
        if (input == "/save") { SaveSession(); return; }
        if (input == "/diff") { ShowDiff(); return; }
        if (input == "/perm" || input == "/permissions") { AppendSystem($"权限模式: {SandboxManager.Level}"); return; }
        if (input.StartsWith("/perm ")) { SandboxManager.SetLevel(input[6..].Trim()); AppendSystem($"沙箱级别: {SandboxManager.Level}"); return; }
        if (input.StartsWith("/model ")) { SwitchModel(input); return; }
        if (input == "/sessions") { ShowSessions(); return; }
        if (input.StartsWith("/load ")) { LoadSession(input[6..].Trim()); return; }
        if (input == "/plan") { await PlanModeAsync(); return; }
        if (input.StartsWith("/search ")) { await RunSearchAsync(input[8..].Trim()); return; }
        if (input is "/settings" or "/config") { SettingsPage.Show(); return; }
        if (input == "/about") { ShowAbout(); return; }
        if (input == "/resume") { await ResumeSession(); return; }
        if (input == "/repomap") { RepoMapGenerator.Invalidate(); AppendSystem(RepoMapGenerator.Generate()); return; }
        if (input == "/checkpoint") { var r = await CheckpointManager.CreateAsync("手动"); AppendSystem(r != null ? $"✔ 检查点 #{r.Id}" : "✘ 失败"); return; }
        if (input == "/checkpoints") { AppendSystem(CheckpointManager.ListCheckpoints()); return; }
        if (input == "/undo") { AppendSystem(await CheckpointManager.UndoAsync(null)); return; }
        if (input.StartsWith("/undo ") && int.TryParse(input[6..], out var cpid)) { AppendSystem(await CheckpointManager.UndoAsync(cpid)); return; }
        if (input == "/recent") { ShowRecent(); return; }
        if (input == "/export") { ExportConversation(); return; }
        if (input == "/git-status") { await RunGitAsync("status"); return; }
        if (input == "/git-log") { await RunGitAsync("log --oneline -20"); return; }
        if (input == "/git-diff") { await RunGitAsync("diff"); return; }
        if (input == "/review") { await RunReviewAsync(); return; }
        if (input == "/lint") { await RunLintAsync(); return; }
        if (input.StartsWith("/pr")) { await RunPRAsync(input); return; }
        if (input.StartsWith("/edit ")) { await Editor.RunAsync(input[6..].Trim()); _mainWin.SetNeedsDraw(); return; }
        if (input == "/edit") { await Editor.PickAndRunAsync(); _mainWin.SetNeedsDraw(); return; }
        if (input.StartsWith("/loop ")) { await RunLoopAsync(input[6..].Trim()); return; }
        if (input == "/history") { AppendSystem("用法: /history <关键词> 或 Ctrl+R"); return; }
        if (input.StartsWith("/history ")) { SearchHistory(input[9..].Trim()); return; }
        if (input.StartsWith("/test")) { RunModuleTest(input); return; }
        if (input == "/watch") { AppendSystem("Watch 模式暂不支持 Terminal.Gui, 请用 --tui-v1"); return; }

        // 自定义命令
        if (input.StartsWith('/'))
        {
            var cmdName = input[1..].Split(' ')[0];
            if (CustomCommands.Commands.TryGetValue(cmdName, out var cmd))
            {
                await RunBashAsync(cmd.Content);
                return;
            }
        }

        // 调用 Agent
        await CallAgentAsync(input);
    }

    // ================================================================
    // Agent 调用
    // ================================================================

    private async Task CallAgentAsync(string input)
    {
        _currentCts = new CancellationTokenSource();
        var modelStack = BuildFallbackChain();
        var startTime = DateTime.UtcNow;
        var completed = false;

        for (int attempt = 0; attempt < modelStack.Length; attempt++)
        {
            var model = modelStack[attempt];
            if (attempt > 0)
            {
                _llm.Model = model;
                _config.Model = model;
                AppendSystem($"🔄 自动回退到: {model}");
            }

            try
            {
                StartAssistantStream();
                await _agent.ChatAsync(input,
                    onToken: tok => Application.Invoke(() => AppendStreamToken(tok)),
                    onTool: (name, brief) => Application.Invoke(() =>
                    {
                        FinishAssistantStream();
                        AppendTool($"{name}({Truncate(brief, 60)})");
                        StartAssistantStream();
                    }),
                    cancellationToken: _currentCts.Token);
                FinishAssistantStream();
                completed = true;
                break;
            }
            catch (OperationCanceledException)
            {
                FinishAssistantStream();
                AppendSystem(_currentCts.IsCancellationRequested ? "⚠ 已中断" : "⏰ 超时");
                break;
            }
            catch (Exception ex) when (attempt < modelStack.Length - 1)
            {
                FinishAssistantStream();
                AppendSystem($"⚠ {model} 失败: {ex.Message}");
            }
            catch (Exception ex)
            {
                FinishAssistantStream();
                AppendSystem($"✘ 错误: {ex.Message}");
            }
        }

        if (completed)
        {
            AppendSystem($"✅ 完成 ({(DateTime.UtcNow - startTime).TotalSeconds:F1}s)");
            Console.Write('\a');
        }

        var modified = EditFileTool.ChangedFiles;
        if (modified.Count > 0)
            AppendSystem($"📝 已修改 {modified.Count} 个文件 (/diff 查看 /undo 撤销)");
        UpdateTokenDisplay();
    }

    private string[] BuildFallbackChain()
    {
        var primary = _config.Model;
        var fallbacks = new List<string> { primary };
        foreach (var fb in new[] { "deepseek-v4-flash", "gpt-5.4-mini", "deepseek-v4-pro", "gpt-5.4" })
            if (fb != primary && !fallbacks.Contains(fb)) fallbacks.Add(fb);
        return fallbacks.ToArray();
    }

    private static string Truncate(string s, int max) =>
        s.Length <= max ? s : s[..(max - 3)] + "...";

    // ================================================================
    // Token 显示
    // ================================================================

    private void UpdateTokenDisplay()
    {
        var p = _llm.TotalPromptTokens;
        var c = _llm.TotalCompletionTokens;
        var cost = _llm.EstimatedCost;
        var contextUsed = ContextManager.EstimateTokens(_agent.Messages);

        var parts = new List<string> { $"↑{FmtK(p)} ↓{FmtK(c)}" };
        if (cost.HasValue) parts.Add($"${cost.Value:F4}");
        if (_llm.LastLatencyMs > 0)
            parts.Add($"{_llm.LastLatencyMs / 1000:F1}s {_llm.LastTokensPerSec:F0}t/s");
        if (_config.MaxContextTokens > 0)
            parts.Add($"上下文 {contextUsed * 100 / _config.MaxContextTokens}%");

        _tokenLabel.Text = "  " + string.Join(" · ", parts);
    }

    private static string FmtK(int n) => n >= 1000 ? $"{n / 1000.0:F1}k" : n.ToString();

    // ================================================================
    // 命令实现
    // ================================================================

    private void ShowHelp()
    {
        var h = @"╔══════ 帮助 (Terminal.Gui 版) ══════╗
║  /help        帮助         ║
║  /reset       清空对话     ║
║  /model [m]   切换模型     ║
║  /tokens      用量统计     ║
║  /stats       详细统计     ║
║  /compact     压缩上下文   ║
║  /diff        修改文件     ║
║  /save        保存会话     ║
║  /search <q>  网页搜索     ║
║  /plan        计划模式     ║
║  /edit [f]    源码编辑器   ║
║  /loop N P    循环执行     ║
║  /pr [标题]   创建 PR      ║
║  /review      代码审查     ║
║  /lint        Lint 检查    ║
║  /git-status  Git 状态     ║
║  /checkpoint  检查点       ║
║  /undo        回退操作     ║
║  /export      导出对话     ║
║  Ctrl+M       切换模型     ║
║  Ctrl+R       搜索历史     ║
║  F2           侧边面板     ║
║  F5           设置界面     ║
║  F10          退出         ║
╚════════════════════════════╝";
        AppendSystem(h);
    }

    private void ShowTokens()
    {
        var p = _llm.TotalPromptTokens; var c = _llm.TotalCompletionTokens;
        AppendSystem($"Token: {p:N0} 入 + {c:N0} 出 = {p + c:N0} 总计 · 请求 {_llm.TotalRequests} 次");
    }

    private void ShowStats()
    {
        var sb = new StringBuilder();
        sb.AppendLine($"模型: {_config.Model} (大) / {_config.SmallModel} (小)");
        sb.AppendLine($"Token: {_llm.TotalPromptTokens:N0} 入 + {_llm.TotalCompletionTokens:N0} 出 = {_llm.TotalPromptTokens + _llm.TotalCompletionTokens:N0}");
        if (_llm.EstimatedCost is { } cost) sb.AppendLine($"花费: ${cost:F4}");
        sb.AppendLine($"请求: {_llm.TotalRequests} 次 · 消息: {_agent.Messages.Count} 条");
        if (_llm.LastLatencyMs > 0)
            sb.AppendLine($"延迟: {_llm.LastLatencyMs / 1000:F1}s · 速度: {_llm.LastTokensPerSec:F0} tok/s");
        AppendSystem(sb.ToString().TrimEnd());
    }

    private async Task CompactAsync()
    {
        var before = ContextManager.EstimateTokens(_agent.Messages);
        await _agent.Context.MaybeCompressAsync(_agent.Messages, _llm);
        var after = ContextManager.EstimateTokens(_agent.Messages);
        var pct = before > 0 ? (int)((before - after) * 100.0 / before) : 0;
        AppendSystem($"✔ 压缩: {before:N0} → {after:N0} ({pct}%)");
    }

    private void SaveSession()
    {
        var sid = SessionManager.SaveSession(_agent.Messages, _config.Model);
        AppendSystem($"✔ 已保存: {sid} (恢复: waycoder -r {sid})");
    }

    private void ShowDiff()
    {
        var files = EditFileTool.ChangedFiles;
        if (files.Count == 0) { AppendSystem("未修改任何文件"); return; }
        foreach (var f in files)
            AppendSystem($"  📄 {Path.GetRelativePath(Directory.GetCurrentDirectory(), f)}");
    }

    private void SwitchModel(string input)
    {
        var m = input[7..].Trim();
        var known = new[] { "deepseek-v4-flash", "deepseek-v4-pro", "gpt-5.4-mini", "gpt-5.4", "gpt-5.5" };
        var match = known.FirstOrDefault(k => k.StartsWith(m, StringComparison.OrdinalIgnoreCase));
        if (match != null) m = match;
        _llm.Model = m; _config.Model = m;
        _mainWin.Title = $"WayCoder 道码 v0.17.3 — {m}";
        AppendSystem($"✅ 大模型: {m}");
    }

    private void CycleModel()
    {
        var models = new[] { "deepseek-v4-flash", "deepseek-v4-pro", "gpt-5.4-mini", "gpt-5.4" };
        var cur = _config.Model;
        var next = models[(Array.IndexOf(models, cur) + 1) % models.Length];
        _llm.Model = next; _config.Model = next;
        _mainWin.Title = $"WayCoder 道码 v0.17.3 — {next}";
        AppendSystem($"🔄 大模型 → {next}");
    }

    private void ShowSessions()
    {
        var sessions = SessionManager.ListSessions();
        if (sessions.Count == 0) { AppendSystem("没有已保存的会话"); return; }
        foreach (var s in sessions)
            AppendSystem($"  📁 {s.Id} [{s.Model}] {s.SavedAt}");
    }

    private void LoadSession(string id)
    {
        var loaded = SessionManager.LoadSession(id);
        if (loaded == null) { AppendSystem($"❌ 会话不存在: {id}"); return; }
        _agent.Messages = loaded.Value.Messages;
        _llm.Model = loaded.Value.Model; _config.Model = loaded.Value.Model;
        _mainWin.Title = $"WayCoder 道码 v0.17.3 — {loaded.Value.Model}";
        AppendSystem($"✅ 已加载: {id} ({loaded.Value.Messages.Count} 条)");
    }

    private async Task ResumeSession()
    {
        if (_pendingRestore == null)
        {
            // 回退：尝试直接加载
            var auto = SessionManager.LoadSession("_auto");
            if (auto == null) { AppendSystem("没有可恢复的会话"); return; }
            _pendingRestore = auto;
        }

        var (msgs, model) = _pendingRestore.Value;
        _agent.Messages = msgs;
        _llm.Model = model; _config.Model = model;
        _mainWin.Title = $"WayCoder 道码 v0.17.3 — {model}";
        _pendingRestore = null;
        AppendSystem($"✅ 已恢复 {msgs.Count} 条消息 (模型: {model})");
    }

    private async Task PlanModeAsync()
    {
        var prompt = _inputView.Text?.ToString()?.TrimEnd() ?? "";
        _inputView.Text = "";
        if (string.IsNullOrWhiteSpace(prompt))
        { AppendSystem("📋 请输入需求"); return; }
        AppendUser(prompt);
        await CallAgentAsync($"分析需求，列出计划再执行：\n\n{prompt}");
    }

    private async Task RunSearchAsync(string query)
    {
        AppendSystem($"🔍 {query}");
        var result = await new WebSearchTool().ExecuteAsync(new() { ["query"] = query });
        AppendSystem(result);
    }

    private async Task RunGitAsync(string cmd)
    {
        var result = await new GitTool().ExecuteAsync(new() { ["command"] = cmd });
        AppendSystem(result);
    }

    private async Task RunBashAsync(string cmd)
    {
        var result = await new BashTool().ExecuteAsync(new() { ["command"] = cmd });
        AppendSystem(result);
    }

    private async Task RunReviewAsync()
    {
        var prompt = ReviewMode.BuildReviewPrompt();
        if (prompt.StartsWith("（")) { AppendSystem(prompt); return; }
        AppendSystem("🔍 代码审查...");
        await CallAgentAsync(prompt);
    }

    private async Task RunLintAsync()
    {
        AppendSystem("🔍 Lint...");
        var result = await new LintTool().ExecuteAsync(new());
        AppendSystem(result);
    }

    private async Task RunPRAsync(string input)
    {
        var parts = input.Split(' ', 2);
        var title = parts.Length > 1 ? parts[1].Trim() : "";
        var prTool = new GitPRTool();
        if (string.IsNullOrEmpty(title))
            AppendSystem(await prTool.ExecuteAsync(new() { ["action"] = "url" }));
        else
            AppendSystem(await prTool.ExecuteAsync(new() {
                ["action"] = "create", ["title"] = title,
                ["description"] = "🤖 Generated with WayCoder/道码"
            }));
    }

    private async Task RunLoopAsync(string args)
    {
        int maxIter = 10; var prompt = args;
        var sp = prompt.IndexOf(' ');
        if (sp > 0 && int.TryParse(prompt[..sp], out var n) && n > 0 && n <= 50)
        { maxIter = n; prompt = prompt[(sp + 1)..]; }
        if (string.IsNullOrWhiteSpace(prompt)) { AppendSystem("用法: /loop [轮次] 提示词"); return; }

        AppendSystem($"🔁 /loop 最多 {maxIter} 轮");
        for (int i = 1; i <= maxIter; i++)
        {
            AppendSystem($"── 第 {i}/{maxIter} 轮 ──");
            try
            {
                await CallAgentAsync(prompt);
                var last = _agent.Messages.FindLast(m => (string?)m["role"] == "assistant");
                var content = last?["content"]?.GetValue<string>() ?? "";
                var markers = new[] { "SUCCESS", "成功", "✅", "PASS", "0 errors", "编译成功" };
                if (markers.Any(m => content.Contains(m, StringComparison.OrdinalIgnoreCase)))
                { AppendSystem($"✅ 条件达成！{i} 轮"); return; }
                prompt = $"继续：{content[..Math.Min(content.Length, 200)]}";
            }
            catch { break; }
        }
        AppendSystem($"⏰ 已达上限 {maxIter} 轮");
    }

    private void RunModuleTest(string input)
    {
        var module = input.Length > 5 ? input[5..].Trim() : "all";
        AppendSystem(SelfTest.RunModule(module));
    }

    private void SearchHistory(string? keyword = null)
    {
        keyword ??= "";
        if (string.IsNullOrWhiteSpace(keyword)) { AppendSystem("用法: /history <关键词>"); return; }

        var results = new List<string>();
        for (int i = 0; i < _agent.Messages.Count; i++)
        {
            var content = (_agent.Messages[i]["content"]?.GetValue<string>()) ?? "";
            if (content.Contains(keyword, StringComparison.OrdinalIgnoreCase))
            {
                var idx = Math.Max(0, content.IndexOf(keyword, StringComparison.OrdinalIgnoreCase) - 40);
                var len = Math.Min(120, content.Length - idx);
                var preview = (idx > 0 ? "..." : "") + content.Substring(idx, len) + (idx + len < content.Length ? "..." : "");
                var role = (_agent.Messages[i]["role"]?.GetValue<string>()) ?? "";
                results.Add($"  #{i + 1} {(role == "user" ? "👤" : "🤖")} {preview.Replace("\n", " ")}");
            }
        }

        if (results.Count == 0) { AppendSystem($"未找到 \"{keyword}\""); return; }
        AppendSystem($"🔍 \"{keyword}\" — {results.Count} 条:");
        foreach (var r in results.Take(15)) AppendSystem(r);
    }

    private void ShowAbout()
    {
        AppendSystem("WayCoder 道码 · 中文版易用编程智能体 v0.17.3");
        AppendSystem("C# / .NET 10 · AOT 编译 · Terminal.Gui 版");
        AppendSystem("深圳市探索智能科技有限公司");
    }

    private void ShowRecent()
    {
        var files = EditFileTool.ChangedFiles;
        if (files.Count == 0) { AppendSystem("暂无最近文件"); return; }
        foreach (var f in files)
            AppendSystem($"  📄 {Path.GetRelativePath(Directory.GetCurrentDirectory(), f)}");
    }

    private void ExportConversation()
    {
        try
        {
            var dir = Path.Combine(Directory.GetCurrentDirectory(), ".corecoder");
            if (!Directory.Exists(dir)) Directory.CreateDirectory(dir);
            var path = Path.Combine(dir, $"export_{DateTime.Now:yyyyMMdd_HHmmss}.md");
            var sb = new StringBuilder();
            sb.AppendLine($"# WayCoder 对话导出\n模型: {_config.Model}\n时间: {DateTime.Now:yyyy-MM-dd HH:mm:ss}\n");
            foreach (var msg in _agent.Messages)
            {
                var role = (string?)msg["role"] ?? "";
                var content = (string?)msg["content"] ?? "";
                switch (role)
                {
                    case "user": sb.AppendLine($"### 👤 User\n\n{content}\n"); break;
                    case "assistant": if (content.Length > 0) sb.AppendLine($"### 🤖\n\n{content}\n"); break;
                    case "tool": sb.AppendLine($"### 🔧\n\n```\n{content[..Math.Min(content.Length, 2000)]}\n```\n"); break;
                }
            }
            File.WriteAllText(path, sb.ToString(), Encoding.UTF8);
            AppendSystem($"✅ 已导出: .corecoder/{Path.GetFileName(path)} ({new FileInfo(path).Length / 1024}KB)");
        }
        catch (Exception ex) { AppendSystem($"❌ 导出失败: {ex.Message}"); }
    }

    // ================================================================
    // 侧边面板
    // ================================================================

    private void ToggleSidePanel()
    {
        _panelVisible = !_panelVisible;
        if (_panelVisible)
        {
            RefreshPanelData();
            _sidePanel.Visible = true;
        }
        else
        {
            _sidePanel.Visible = false;
        }
        _sidePanel.SetNeedsDraw();
    }

    /// <summary>刷新侧边面板数据：Todo / 修改文件 / 文件锁 / MCP。</summary>
    private void RefreshPanelData()
    {
        // 任务列表
        var todos = TodoTool.Items;
        var todoItems = todos.Count == 0
            ? new List<string> { "（暂无任务）" }
            : todos.Select(t => $"{(t.Status == "completed" ? "✅" : t.Status == "in_progress" ? "🔄" : "⏳")} {t.Title}").ToList();
        _todoList.SetSource<string>(new ObservableCollection<string>(todoItems));

        // 修改文件列表
        var files = EditFileTool.ChangedFiles;
        var fileItems = files.Count == 0
            ? new List<string> { "（暂无修改）" }
            : files.Select(f => Path.GetRelativePath(Directory.GetCurrentDirectory(), f)).ToList();
        _filesList.SetSource<string>(new ObservableCollection<string>(fileItems));

        // 文件锁列表
        var locks = FileLockManager.GetAllLocks();
        var lockItems = locks.Count == 0
            ? new List<string> { "（无活跃锁）" }
            : locks.Select(l => $"{Path.GetFileName(l.FilePath)} ({l.AgentId})").ToList();
        _locksList.SetSource<string>(new ObservableCollection<string>(lockItems));

        // MCP 服务器
        var mcpInfo = McpManager.Info ?? "未配置";
        var mcpItems = mcpInfo.Split('\n', StringSplitOptions.RemoveEmptyEntries)
            .Select(l => l.Trim())
            .Where(l => l.Length > 0)
            .ToList();
        if (mcpItems.Count == 0) mcpItems.Add("未配置 MCP 服务器");
        _mcpList.SetSource<string>(new ObservableCollection<string>(mcpItems));
    }

    // ================================================================
    // 工具
    // ================================================================

    private static string? DetectGitBranch()
    {
        try
        {
            var hp = Path.Combine(Directory.GetCurrentDirectory(), ".git", "HEAD");
            if (!File.Exists(hp)) return null;
            var h = File.ReadAllText(hp).Trim();
            return h.StartsWith("ref: refs/heads/") ? h["ref: refs/heads/".Length..]
                : h.Length >= 7 ? h[..7] : h;
        }
        catch { return null; }
    }
}
