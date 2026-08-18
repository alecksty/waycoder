using System.Text;
using WayCoder.Tools;
using WayCoder.UI.Shared;
using WayCoder.UI.Tui;
using WayCoder.UI.Shared.Terminal;
using WayCoder.UI.TUI.Base;
using WayCoder.UI.Tui.Controls;
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
    // 交互式 REPL
    // ========================================================================

    /// <summary>CLI 文本界面（--cli）：逐行交互，Agent 纯文本回复，exit/quit 退出（非全屏 TUI）。</summary>
    private static async Task RunCliReplAsync(string? editFile)
    {
        var agent = _agent;
        if (agent == null) { Console.Error.WriteLine("Agent 未初始化"); return; }
        Console.WriteLine("WayCoder 道码 · CLI 模式（--cli）— 输入消息，exit/quit 退出");
        while (true)
        {
            Console.Write("» ");
            var line = Console.In.ReadLine();
            if (line == null) break; // EOF
            var input = line.Trim();
            if (string.IsNullOrWhiteSpace(input)) continue;
            if (input.Equals("exit", StringComparison.OrdinalIgnoreCase) ||
                input.Equals("quit", StringComparison.OrdinalIgnoreCase)) break;
            await ProcessTextInput(agent, input);
        }
    }

    /// <summary>管道模式：echo "任务" | waycoder 逐行读 stdin 交给 Agent，纯文本输出，EOF 退出。</summary>
    private static async Task RunPipeModeAsync()
    {
        var agent = _agent;
        if (agent == null) { Console.Error.WriteLine("Agent 未初始化"); return; }
        string? line;
        while ((line = Console.In.ReadLine()) != null)
        {
            var input = line.Trim();
            if (string.IsNullOrWhiteSpace(input)) continue;
            await ProcessTextInput(agent, input);
        }
    }

    /// <summary>纯文本聊天处理（管道/CLI 界面复用）：显示输入 → Agent 流式回复 → 工具调用行 → 异常兜底。</summary>
    private static async Task ProcessTextInput(Agent agent, string input)
    {
        Console.WriteLine();
        Console.WriteLine($"[2m🤖 {input}[0m");
        try
        {
            await agent.ChatAsync(input,
                onToken: t => Console.Write(t),
                onTool: (name, brief) => Console.WriteLine($"\n[90m🔧 [{name}] {brief}[0m"));
            Console.WriteLine();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"\n[31m[✘ 错误][0m {ex.Message}");
        }
    }

    private static async Task RunReplAsync(string? editFile = null)
    {
        // 非交互（管道/重定向）：echo "任务" | waycoder 读 stdin 执行后退出，不启动全屏 TUI
        if (Console.IsInputRedirected)
        {
            await RunPipeModeAsync();
            return;
        }

        var mgr = TuiManager.Instance;
        // 标记版界面（.tui 声明式布局）为默认；chat.tui 加载失败时兜底手写 ChatScreen
        ChatScreen screen;
        try { screen = new MarkupChatScreen(); }
        catch (Exception ex) { Console.Error.WriteLine($"[Markup fallback→ChatScreen] {ex.Message}"); screen = new ChatScreen(); }
        screen.ChatDisplayStyle = _config.ChatDisplayStyle;
        mgr.Enter();
        mgr.PushScreen(screen);
        screen.SyncTheme();
        screen.RefreshTheme();

        // Ctrl+C 设置退出标志，走正常清理路径（AutoSaveSession + mgr.Exit）
        Console.CancelKeyPress += (_, e) =>
        {
            e.Cancel = true;
            _exitRequested = true;
        };

        // 输入管理器：拦截键盘 + 鼠标 + resize 即时重绘
        using var inputMgr = new InputManager();
        inputMgr.Init();

        // 初始化 10 个槽位（槽位 0 已在 Main 中持有主 Agent）
        for (int i = 0; i < AgentSlot.Count; i++) _slots[i] ??= new AgentSlot();
        if (_slots[0].Agent == null) _slots[0].Agent = _agent;
        _activeSlot = 0;
        StructuredMemory.CurrentSlotIndex = 0;
        var slot0 = _slots[0];

        // 启动欢迎屏 — ASCII Logo 注入槽位 0（多行合并避免 ItemSpacing 空行）
        var logo = string.Join("\n",
            "",
            "",
            "",
            "██╗    ██╗ █████╗ ██╗   ██╗ ██████╗ ██████╗ ██████╗ ███████╗██████╗ ",
            "██║    ██║██╔══██╗╚██╗ ██╔╝██╔════╝██╔═══██╗██╔══██╗██╔════╝██╔══██╗",
            "██║ █╗ ██║███████║ ╚████╔╝ ██║     ██║   ██║██║  ██║█████╗  ██████╔╝",
            "██║███╗██║██╔══██║  ╚██╔╝  ██║     ██║   ██║██║  ██║██╔══╝  ██╔══██╗",
            "╚███╔███╔╝██║  ██║   ██║   ╚██████╗╚██████╔╝██████╔╝███████╗██║  ██║",
            " ╚══╝╚══╝ ╚═╝  ╚═╝   ╚═╝    ╚═════╝ ╚═════╝ ╚═════╝ ╚══════╝╚═╝  ╚═╝"
        );
        slot0.ChatMessages.Add(new ChatMsg { Role = "banner", Content = logo, Centered = true });
        slot0.ChatMessages.Add(new ChatMsg { Role = "system", Content = $"{Global.AppFullName} · {Global.Version}", Centered = true });
        slot0.ChatMessages.Add(new ChatMsg { Role = "system", Content = "深圳市探索智能科技有限公司", Centered = true });
        slot0.ChatMessages.Add(new ChatMsg { Role = "system", Content = $"大模型: {_config.Model} · 小模型: {_config.SmallModel}  ·  /help 帮助", Centered = true });
        // 快捷键表输出到首条对话（每个槽位首次激活也会显示，见 SlotWelcome）
        slot0.ChatMessages.Add(new ChatMsg { Role = "system", Content = TuiKeybindHelp.GetHelpText() });
        slot0.StatusLeft = Global.AppFullName;
        slot0.HasWelcome = true;
        _llm!.SmallModel = _config.SmallModel;

        // 检测 API Key 配置：未配 key 时冒泡提示
        bool hasGlobalKey = !string.IsNullOrEmpty(_config.ApiKey);
        var storeKeys = ApiKeyStore.ListAll();
        var keyCount = storeKeys.Count(kv => !string.IsNullOrEmpty(kv.Value));
        var currentProvider = ModelCatalog.All
            .FirstOrDefault(m => m.Id == _config.Model)?.ProviderId;
        bool hasCurrentKey = hasGlobalKey
            || (currentProvider != null && ApiKeyStore.Has(currentProvider));
        if (!hasGlobalKey && keyCount == 0 || !hasCurrentKey)
        {
            var hint = !hasGlobalKey && keyCount == 0
                ? "⚠ 未检测到任何 API Key，请按 Ctrl+M 打开模型选择框，选择模型后回车输入 Key"
                : $"⚠ 当前模型 {_config.Model} 未配置 API Key，按 Ctrl+M 选择模型并输入 Key";
            slot0.ChatMessages.Add(new ChatMsg { Role = "system",
                Content = $"«bold yellow»🔑 {hint}«/»" });
        }

        // 检测 git 分支
        var branch = DetectGitBranch();
        if (branch != null)
        {
            slot0.StatusLeft += $" ·  {branch}";
            slot0.GitBranch = branch;
        }

        // 槽位 0 状态灌入屏幕（欢迎屏即首屏）
        slot0.RestoreTo(screen);
        screen.ActiveSlotIndex = 0;

        // 权限确认框信号 → 槽位状态栏标记"等待权限" + 桌面通知
        PermissionManager.PermissionPromptStarted += toolName =>
        {
            screen.SlotStates[screen.ActiveSlotIndex] = SlotState.WaitingPerm;
            screen.OnPermissionWaiting(toolName);
            DesktopNotifier.NotifyPermissionWaiting(toolName);
        };
        PermissionManager.PermissionPromptResolved += _ =>
        {
            if (screen.SlotStates[screen.ActiveSlotIndex] == SlotState.WaitingPerm)
                screen.SlotStates[screen.ActiveSlotIndex] = SlotState.Working;
            screen.OnPermissionResolved();
        };

        // 工作模式变更信号 → 同步当前槽位持久模式 + Agent 实例模式 + 状态栏显示
        // （覆盖 Shift+Tab 循环切换、/mode 命令等 UI 线程发起的变更来源；
        //   后台槽位批准计划走 Agent.OnWorkModeChanged，不经过此全局事件，避免污染活跃槽位）
        WorkModeManager.ModeChanged += mode =>
        {
            _slots[_activeSlot].WorkMode = mode;
            if (_slots[_activeSlot].Agent != null)
                _slots[_activeSlot].Agent!.WorkMode = mode;
            screen.StatusBar.CurrentWorkMode = mode;
        };

        // Watch 模式 — 监听外部编辑器文件变更
        if (_config.WatchMode)
        {
            StartWatchMode(screen);
            screen.AddSystemMsg("👁 Watch 模式已启动 — 在文件中写 AI! 注释自动触发 Agent");
        }

        // 尝试恢复上次会话
        TryRestoreSession(screen);

        // 启动后台版本检查（异步、静默，有新版本才提示；不阻塞主循环）
        _ = UpdateChecker.CheckAsync().ContinueWith(t =>
        {
            try
            {
                var msg = t.IsCompletedSuccessfully ? t.Result : "";
                if (msg.StartsWith("🆕", StringComparison.Ordinal))
                    screen.AddSystemMsg(msg);
            }
            catch { /* REPL 已退出时静默忽略 */ }
        });

        // 注入 ChatScreen 回调
        screen.OnCycleModel = () => CycleModel(screen);
        screen.OnShowHelp = () => ShowHelpInChat(screen);
        screen.OnSearchHistory = query => SearchHistory(query, screen);
        screen.OnOpenSessions = () => OpenSessions(screen);
        screen.OnReasoningEffort = () => PickReasoningEffort(screen);

        // ── 自动投递命令行槽位任务队列（-p1 ~ -p0，同一槽位可排队）──
        foreach (var (slotIdx, tasks) in _pendingSlotQueues.OrderBy(kv => kv.Key))
        {
            // 切换到目标槽位
            if (slotIdx != _activeSlot)
            {
                SwitchAgentSlot(slotIdx, screen);
                mgr.Render();
            }

            // 确保目标槽位有 Agent
            if (_slots[slotIdx].Agent == null)
            {
                var slotLlm = GetSlotLlm(slotIdx);
                _slots[slotIdx].Agent = new Agent(slotLlm, maxContextTokens: ModelCatalog.ResolveContextWindow(slotLlm.Model, _config.MaxContextTokens),
                    maxBudgetUsd: _config.MaxBudgetUsd, autoCommit: _config.AutoGitCommit);
                _agent = _slots[slotIdx].Agent;
                ProgramContext.Agent = _agent;
            }

            var prefix = string.IsNullOrWhiteSpace(_pendingSlotPrefix)
                ? "" : _pendingSlotPrefix + " ";

            screen.AddSystemMsg($"📨 槽位 F{slotIdx + 1} 收到 {tasks.Count} 个任务（来自命令行）");
            mgr.Render();

            foreach (var task in tasks)
            {
                var fullPrompt = prefix + task;
                screen.AddSystemMsg($"  → {fullPrompt.Truncate(80)}");
                mgr.Render();
                await ProcessUserInput(fullPrompt, screen);
            }
        }
        _pendingSlotQueues.Clear();
        _pendingSlotPrefix = "";

        // 启动时执行一次 OnResize，确保动态布局计算正确
        mgr.OnResize();
        mgr.Render();

        // --edit <文件>：启动后直接进入编辑器打开指定文件（Esc/Ctrl+Q 退出回到聊天界面）
        if (!string.IsNullOrWhiteSpace(editFile))
            mgr.PushScreen(new EditorScreen(editFile));

        var running = true;
        while (running && !_exitRequested)
        {
            mgr.Render();

            // 处理 ChatScreen 提交的消息（Enter 键 → async LLM 调用）
            while (screen.PendingSubmissions.TryDequeue(out var submission))
            {
                // "\x1b" 特殊标记：退出请求
                if (submission == "\x1b")
                {
                    AutoSaveSession();
                    _watchMode?.Dispose();
                    mgr.Exit();
                    Environment.Exit(0);
                }

                mgr.Render();
                await ProcessUserInput(submission, screen);
            }

            // 检查 Watch 模式待处理提示
            while (_pendingWatchPrompts.TryDequeue(out var watchPrompt))
            {
                screen.AddSystemMsg($"👁 Watch: {ContextManager.TruncateByRunes(watchPrompt, 80)}");
                mgr.Render();
                await ProcessUserInput(watchPrompt, screen);
            }

            var ev = inputMgr.ReadInput(50);

            // Resize — 通知全控件树重新布局 + 全屏刷新
            if (ev.Type == InputType.Resize)
            {
                mgr.OnResize();
                mgr.Render();
                continue;
            }

            // 超时 — 继续轮询
            if (ev.Type == InputType.Timeout) continue;

            // 鼠标 — 路由给 TuiManager → 活跃屏幕 → 控件树（仅在启用鼠标时）
            if (ev.Type == InputType.Mouse)
            {
                if (TuiManager.MouseEnabled) mgr.HandleMouse(ev);
                mgr.Render();
                continue;
            }

            // 粘贴 — bracketed paste 自动检测
            if (ev.Type == InputType.Paste)
            {
                if (!string.IsNullOrEmpty(ev.PasteText))
                    screen.HandleBracketedPaste(ev.PasteText);
                mgr.Render();
                continue;
            }

            // 按键
            var key = ev.KeyInfo;
            bool ctrl = key.Modifiers.HasFlag(ConsoleModifiers.Control);

            // 系统级：Ctrl+C 设置退出标志，走正常清理路径
            if (key.Key == ConsoleKey.C && ctrl)
            {
                _exitRequested = true;
                continue;
            }

            // 系统级：Ctrl+Q 紧急退出（强制保存所有槽位 + 恢复终端）
            if (key.Key == ConsoleKey.Q && ctrl)
            {
                PanicExit("用户 Ctrl+Q 紧急退出");
            }

            // 系统级：Shift+Tab 切换工作模式（Build → Plan → Review → Auto）
            if (ev.Type == InputType.ShiftTab)
            {
                var newMode = WorkModeManager.CycleNext();
                _slots[_activeSlot].WorkMode = newMode;
                screen.StatusBar.CurrentWorkMode = newMode;
                screen.AddSystemMsg($"工作模式: {WorkModeManager.Format(newMode)}（Shift+Tab 切换）");
                mgr.Render();
                continue;
            }

            // 系统级：F1~F10 切换 Agent 槽位
            if (key.Key >= ConsoleKey.F1 && key.Key <= ConsoleKey.F10)
            {
                int slotIdx = key.Key - ConsoleKey.F1;
                // 先确保回到 ChatScreen（弹出其他 screen 直至根）
                while (mgr.ActiveScreen != screen && mgr.ActiveScreen != null)
                    mgr.PopScreen();
                SwitchAgentSlot(slotIdx, screen);
                mgr.Render();
                continue;
            }

            // 活跃槽位 Agent 运行中的热键：Esc 中断 / Ctrl+Z 优雅暂停（空闲时 Esc/Ctrl+Z 正常下发）
            if (_slots[_activeSlot].IsBusy)
            {
                if (key.Key == ConsoleKey.Escape)
                {
                    // 原子摘除 CTS，与后台 finally 的 Dispose 对齐（只有一个能取到非 null，取到者负责释放），
                    // 消除「读到非空 → 后台 Dispose → Cancel 抛 ObjectDisposedException」竞态。
                    var cts = Interlocked.Exchange(ref _slots[_activeSlot].Cts, null);
                    if (cts != null)
                    {
                        try { cts.Cancel(); } catch { }
                        cts.Dispose();
                    }
                    screen.AddSystemMsg("⚠ 已请求中断当前槽位的 Agent");
                    mgr.Render();
                    continue;
                }
                if (key.Key == ConsoleKey.Z && ctrl)
                {
                    _slots[_activeSlot].Agent!.PauseRequested = true;
                    screen.AddSystemMsg("⏸ 已请求暂停 — 当前批次完成后自动提交并停机（再按 Esc 立即中断）");
                    mgr.Render();
                    continue;
                }
            }

            // 其余全部下发到活跃 Screen → Window → 控件冒泡
            mgr.OnKey(key);
            mgr.Render();
        }

        AutoSaveSession();
        _watchMode?.Dispose();
        mgr.Exit();
    }

    /// <summary>启动时检测上次自动保存的会话 + 崩溃恢复标记。</summary>
    private static void TryRestoreSession(ChatScreen screen)
    {
        try
        {
            // 检测崩溃恢复标记
            var crashFile = Path.Combine(Global.GlobalConfigPath("sessions"), ".crash_recovery");
            if (File.Exists(crashFile))
            {
                var crashInfo = File.ReadAllText(crashFile).Trim();
                screen.AddSystemMsg($"⚠ 检测到上次异常退出 ({crashInfo.Split('\n')[0]})。输入 /resume 恢复工作。");
                try { File.Delete(crashFile); } catch { }
            }

            var auto = SessionManager.LoadSession("_auto", 0)
                       ?? SessionManager.LoadSession("_auto"); // 旧版本存全局，回退兼容
            if (auto == null) return;

            var count = auto.Value.Messages.Count;
            screen.AddSystemMsg($"💾 发现上次会话 ({count} 条消息)。输入 /resume 恢复，或忽略此消息开始新会话。");
            _pendingRestore = auto;
        }
        catch
        {
            /* 恢复失败不影响启动 */
        }
    }

    /// <summary>启动 Watch 模式 — 监听文件变更中的 AI! / AI? 注释。</summary>
    private static void StartWatchMode(ChatScreen screen)
    {
        try
        {
            var dir = Directory.GetCurrentDirectory();
            _watchMode = new WatchMode(dir, prompt => { _pendingWatchPrompts.Enqueue(prompt); });
            _watchMode.Start();
        }
        catch (Exception ex)
        {
            screen.AddSystemMsg($"  ⚠ Watch 模式启动失败: {ex.Message}");
            DebugLog.Log("watch", $"启动失败: {ex.Message}");
            ErrorLog.Error("Program.WatchMode", $"Watch 模式启动失败: {ex.Message}", ex);
        }
    }

    /// <summary>切换 Watch 模式开关。</summary>
    private static void ToggleWatchMode(ChatScreen screen)
    {
        if (_watchMode != null)
        {
            _watchMode.Dispose();
            _watchMode = null;
            _config.WatchMode = false;
            screen.AddSystemMsg("👁 Watch 模式已关闭");
        }
        else
        {
            _config.WatchMode = true;
            StartWatchMode(screen);
            if (_watchMode != null)
                screen.AddSystemMsg("👁 Watch 模式已启动 — 在文件中写 AI! 注释自动触发 Agent");
        }
    }

    /// <summary>退出时自动保存会话，下次启动可恢复。并行模式下保存所有非空槽位。</summary>
    private static void AutoSaveSession()
    {
        try
        {
            HooksManager.RunSessionEnd("exit");
            HooksManager.ClearSessionHooks();

            var saved = 0;
            for (int i = 0; i < AgentSlot.Count; i++)
            {
                var slot = _slots[i];
                var slotMsgs = slot?.Agent?.SnapshotMessages();
                if (slotMsgs == null || slotMsgs.Count == 0) continue;
                // 只保存有实际对话的会话（至少一条用户消息）
                var hasUser = slotMsgs.Any(m =>
                    m["role"]?.AsString() == "user");
                if (!hasUser) continue;
                SessionManager.SaveSession(slotMsgs, _config.Model, "_auto", i);
                saved++;
            }
            if (saved > 0)
                DebugLog.Log("session", $"会话已自动保存 ({saved} 个槽位)");
        }
        catch (Exception ex)
        {
            DebugLog.Log("session", $"自动保存失败: {ex.Message}");
            ErrorLog.Warning("Program.AutoSave", $"会话自动保存失败: {ex.Message}", ex);
        }
    }

    /// <summary>异常崩溃时紧急保存所有槽位</summary>
    private static void AutoSaveException(Exception? ex)
    {
        try
        {
            if (ex != null)
                DebugLog.Log("crash", $"异常: {ex.GetType().Name}: {ex.Message}\n{ex.StackTrace}");
            // 保存所有非空槽位
            for (int i = 0; i < AgentSlot.Count; i++)
            {
                var slot = _slots[i];
                var slotMsgs = slot?.Agent?.SnapshotMessages();
                if (slotMsgs == null || slotMsgs.Count == 0) continue;
                var hasUser = slotMsgs.Any(m => m["role"]?.AsString() == "user");
                if (!hasUser) continue;
                try { SessionManager.SaveSession(slotMsgs, _config.Model, "_auto", i); } catch { }
            }
            // 写入崩溃标记文件
            var crashFile = Path.Combine(Global.GlobalConfigPath("sessions"), ".crash_recovery");
            File.WriteAllText(crashFile, $"{DateTime.Now:yyyy-MM-dd HH:mm:ss}\n{ex?.GetType().Name}: {ex?.Message}");
        }
        catch { /* 崩溃恢复本身不应再抛异常 */ }
    }

    /// <summary>紧急退出：保存数据 + 恢复终端 + 退出进程</summary>
    private static void PanicExit(string reason)
    {
        try
        {
            DebugLog.Log("panic", $"紧急退出: {reason}");
            AutoSaveSession();
            // 也保存其他槽位
            for (int i = 1; i < AgentSlot.Count; i++)
            {
                var slot = _slots[i];
                var slotMsgs = slot?.Agent?.SnapshotMessages();
                if (slotMsgs == null || slotMsgs.Count == 0) continue;
                try { SessionManager.SaveSession(slotMsgs, _config.Model, "_auto", i); } catch { }
            }
        }
        catch { }
        finally
        {
            try { Tty.ExitAltScreen(); } catch { }
            Environment.Exit(1);
        }
    }

    /// <summary>在 Agent 后台执行期间保持 TUI 渲染 + 响应热键（Esc 取消 / Ctrl+Q 紧急退出）</summary>
    /// <returns>Agent 的 ChatAsync 任务，调用方需 await 以获取异常</returns>
    private static async Task RunAgentWithRenderLoop(CancellationTokenSource cts)
    {
        var agentTask = Task.Run(async () =>
        {
            await _agent!.ChatAsync(_currentUserInput!,
                onToken: tok =>
                {
                    screen_!.Running = false;
                    screen_!.OnToolFinished(); // token 开始流入 = 回到思考状态
                    screen_!.EnsureAgentStreaming();
                    screen_!.AppendToken(tok);
                },
                onTool: (name, brief) =>
                {
                    screen_!.FinishAgentMsg();
                    screen_!.AddToolProgress(name, brief.Length > 60 ? ContextManager.TruncateByRunes(brief, 57) + "..." : brief);
                    screen_!.OnToolStarted(name, brief.Length > 40 ? ContextManager.TruncateByRunes(brief, 37) + "..." : brief);
                    // 不立刻 StartAgentMsg，等 onToolOutput 流式输出完毕再懒启动
                    // 每 3 次工具调用自动保存
                    if (++_toolCallCount % 3 == 0)
                        AutoSaveSession();
                },
                onToolOutput: line =>
                {
                    screen_!.AppendToLast(line + "\n");
                    screen_!.OnToolFinished(); // 流式输出 = 工具已执行完毕，回到思考
                },
                cancellationToken: cts.Token);
        });

        var mgr = TuiManager.Instance;
        while (!agentTask.IsCompleted)
        {
            mgr.Render();
            if (Console.KeyAvailable)
            {
                var key = Console.ReadKey(intercept: true);
                if (key.Key == ConsoleKey.Escape)
                    cts.Cancel();
                else if (key.Key == ConsoleKey.Z && key.Modifiers.HasFlag(ConsoleModifiers.Control))
                {
                    // Ctrl+Z 优雅暂停：置位标志，Agent 在当前批次完成后的下一轮边界停机
                    _agent!.PauseRequested = true;
                    screen_!.AddSystemMsg("⏸ 已请求暂停 — 当前批次完成后自动提交并停机（再按 Esc 立即中断）");
                }
                else if (key.Key == ConsoleKey.Q && key.Modifiers.HasFlag(ConsoleModifiers.Control))
                {
                    cts.Cancel();
                    PanicExit("Agent 运行中 Ctrl+Q 紧急退出");
                }
            }
            else
            {
                await Task.Delay(30);
            }
        }
        await agentTask; // 传播异常
    }

    // 当前正在处理的用户输入 + 屏幕引用（供 RunAgentWithRenderLoop 使用）
    private static string? _currentUserInput;
    private static ChatScreen? screen_;
    private static int _toolCallCount;

    /// <summary>
    /// 切换 Agent 槽位（F1-F10）。保存当前槽位 UI 状态，懒创建目标槽位 Agent，
    /// 恢复目标槽位状态到屏幕。Agent 运行中禁止切换。
    /// </summary>
    /// <summary>根据槽位配置获取或创建对应的 LLM 客户端</summary>
    private static LLM GetSlotLlm(int slotIdx)
    {
        var slotCfg = AgentSlotConfig.Get(slotIdx);
        var slot = _slots[slotIdx];

        // 使用全局模型/密钥的槽位：每个槽位持有独立 LLM 克隆实例，而非共享 _llm。
        // 根因修复「10 个 agent 不能真正并行」——共享 _llm 时并发 ChatAsync 会竞态读写
        // ModelOverride/_reasoningBuffer/_reasoningShown 等非线程安全实例字段。
        if (slotCfg.UseGlobal)
        {
            if (slot.LlmClient != null && slot.LastLargeModel == _llm!.Model)
                return slot.LlmClient;
            var clone = _llm!.Clone();
            clone.ModelOverride = null; // 槽位从无覆盖状态起步，不继承瞬时小模型覆盖
            slot.LlmClient = clone;
            slot.LastLargeModel = _llm.Model;
            slot.LastSmallModel = _llm.SmallModel;
            return clone;
        }

        var largeModel = AgentSlotConfig.ResolveLargeModel(slotCfg, slotIdx);
        var smallModel = AgentSlotConfig.ResolveSmallModel(slotCfg, slotIdx);

        // 模型未变 → 复用已有 LLM
        if (slot.LlmClient != null
            && slot.LastLargeModel == largeModel
            && slot.LastSmallModel == smallModel)
            return slot.LlmClient;

        // 创建新的 LLM（使用槽位专属 API Key 和 BaseUrl）
        var apiKey = AgentSlotConfig.ResolveApiKey(slotCfg);
        var baseUrl = AgentSlotConfig.ResolveBaseUrl(slotCfg, largeModel);
        var llm = new LLM(largeModel, apiKey, baseUrl,
            _config.MaxTokens, _config.Temperature)
        {
            SmallModel = smallModel,
        };

        slot.LlmClient = llm;
        slot.LastLargeModel = largeModel;
        slot.LastSmallModel = smallModel;

        return llm;
    }

    /// <summary>
    /// 绑定槽位 Agent 的工作模式：把槽位持久模式灌入 Agent 实例，并接线回调，
    /// 使 Agent 内部切换模式（如计划审批门批准后自动切回建造模式）能通知到正确槽位——
    /// 而非依赖全局 ModeChanged 事件（后台槽位批准时会污染活跃槽位）。
    /// </summary>
    private static void WireSlotWorkMode(int slotIdx, Agent agent, ChatScreen screen)
    {
        var slot = _slots[slotIdx];
        agent.WorkMode = slot.WorkMode;
        agent.OnWorkModeChanged = mode =>
        {
            _slots[slotIdx].WorkMode = mode;
            // 仅当该槽位是活跃槽位时才同步全局镜像与状态栏（后台线程安全：枚举赋值原子）
            if (slotIdx == _activeSlot)
            {
                WorkModeManager.CurrentMode = mode;
                screen.StatusBar.CurrentWorkMode = mode;
            }
        };
    }

    private static void SwitchAgentSlot(int idx, ChatScreen screen)
    {
        if (idx < 0 || idx >= AgentSlot.Count || idx == _activeSlot) return;

        var oldSlot = _slots[_activeSlot];
        var newSlot = _slots[idx];

        // 序列化旧槽位：与旧槽位后台线程的"检查活跃+写屏"互斥，避免切换瞬间丢 token。
        // 必须先 SaveFrom 快照再改 _activeSlot（两者在同一锁内原子完成）。
        lock (oldSlot.Sync)
        {
            oldSlot.SaveFrom(screen);
            _activeSlot = idx;
            StructuredMemory.CurrentSlotIndex = idx;
        }

        // 序列化新槽位：与新槽位后台线程的缓冲写入互斥，确保 RestoreTo 读到一致快照。
        lock (newSlot.Sync)
        {
            var slot = newSlot;
            var slotLlm = GetSlotLlm(idx);
            if (slot.Agent == null)
            {
                slot.Agent = new Agent(slotLlm, maxContextTokens: ModelCatalog.ResolveContextWindow(slotLlm.Model, _config.MaxContextTokens),
                    maxBudgetUsd: _config.MaxBudgetUsd, autoCommit: _config.AutoGitCommit);
            }
            else
            {
                // 更新已存在的 Agent 的 LLM（模型可能已变更）
                var large = AgentSlotConfig.ResolveLargeModel(AgentSlotConfig.Get(idx), idx);
                slot.Agent.LlmClient.Model = large;
                slot.Agent.LlmClient.SmallModel = AgentSlotConfig.ResolveSmallModel(AgentSlotConfig.Get(idx), idx);
                slot.Agent.UpdateContextWindow(ModelCatalog.ResolveContextWindow(large, _config.MaxContextTokens));
            }
            slot.Agent.AgentId = $"F{idx + 1}"; // 槽位标识，供文件锁跨槽位冲突检测

            _agent = slot.Agent;
            ProgramContext.Agent = slot.Agent;

            // 重绑子智能体父引用（所有 Agent 共享 AgentTool 实例）
            foreach (var t in _agent.Tools)
            {
                if (t is AgentTool agentTool) agentTool.ParentAgent = _agent;
            }

            // 绑定槽位工作模式（灌入实例 + 接线回调）
            WireSlotWorkMode(idx, _agent, screen);

            // 首次激活显示欢迎提示
            if (!slot.HasWelcome)
            {
                slot.HasWelcome = true;
                slot.ChatMessages.Add(new ChatMsg
                {
                    Role = "system",
                    Content = $"🤖 Agent 槽位 F{idx + 1} — 独立会话\n\n{TuiKeybindHelp.GetHelpText()}",
                });
            }

            slot.RestoreTo(screen);
            screen.ActiveSlotIndex = idx;

            // 恢复目标槽位的工作模式
            WorkModeManager.CurrentMode = slot.WorkMode;
            screen.StatusBar.CurrentWorkMode = slot.WorkMode;
        }

        screen.Render();
    }

    /// <summary>处理用户输入：内置命令或 Agent 调用</summary>
    private static async Task ProcessUserInput(string userInput, ChatScreen screen)
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
                screen.AddSystemMsg($"💡 命令 [{userInput}] 未识别，已纠正为 [{corrected}]");
                userInput = corrected;
            }
        }

        var lower = userInput.ToLowerInvariant();

        // 退出
        if (lower is "quit" or "exit" or "/quit" or "/exit")
        {
            AutoSaveSession();
            _watchMode?.Dispose();
            TuiManager.Instance.Exit();
            Environment.Exit(0);
        }

        // 触发提示 (已通过建议面板处理，但保留备用)
        if (userInput == "/")
        {
            screen.SetInput(ShowCommandPalette());
            screen.Render();
            return;
        }

        if (userInput == "!")
        {
            await RunShellOnceAsync();
            return;
        }

        // 需要 Program 私有状态的特殊命令（在注册表分发之前）
        if (userInput == "/watch")
        {
            ToggleWatchMode(screen);
            return;
        }

        if ((userInput == "/resume" || userInput == "/continue") && _pendingRestore != null)
        {
            var (msgs, model) = _pendingRestore.Value;
            _agent!.ReplaceMessages(msgs);
            _pendingRestore = null;
            screen.AddSystemMsg($"✔ 已恢复 {msgs.Count} 条消息 (模型: {model})");
            return;
        }

        if (userInput.StartsWith("/loop "))
        {
            await RunLoopAsync(userInput[6..].Trim(), screen);
            return;
        }

        if (userInput == "/plan")
        {
            screen.AddSystemMsg("📋 计划模式");
            await PlanModeAsync();
            return;
        }

        if (userInput == "/pause")
        {
            screen.AddSystemMsg("⏸ 暂停请在 Agent 运行时按 Ctrl+Z（当前批次完成后优雅停机并提交）。Esc 为立即中断。");
            return;
        }

        // 统一斜杠命令分发（SlashCommandRegistry）
        var (cmd, args) = SlashCommandRegistry.Match(userInput);
        if (cmd != null)
        {
            await cmd.ExecuteAsync(args, screen);
            return;
        }

        // 自定义命令 (来自 .waycoder/commands/*.md)
        var customCmdName = userInput.TrimStart('/').Split(' ')[0].ToLowerInvariant();
        if (CustomCommands.Commands.ContainsKey(customCmdName))
        {
            var (content, _) = CustomCommands.Execute(customCmdName,
                userInput.Contains(' ') ? userInput[(userInput.IndexOf(' ') + 1)..] : "", _agent!);
            screen.AddSystemMsg(content);
            return;
        }

        // 调用 Agent（后台并行执行：不阻塞主循环，支持多槽位同时运行）
        StartSlotTask(_activeSlot, userInput, screen);
    }

    /// <summary>
    /// 启动槽位后台 Agent 任务（不阻塞主循环，实现多槽位并行执行）。
    /// 该槽位若已有任务在跑则拒绝；否则懒创建 Agent 后在后台线程运行。
    /// </summary>
    private static void StartSlotTask(int slotIdx, string userInput, ChatScreen screen)
    {
        var slot = _slots[slotIdx];
        if (slot.IsBusy)
        {
            screen.AddSystemMsg("⚠ 当前槽位的 Agent 仍在运行，请等待完成或按 Esc 中断后再提交新任务");
            return;
        }

        // 懒创建 Agent（槽位首次使用）
        if (slot.Agent == null)
        {
            var slotLlm = GetSlotLlm(slotIdx);
            slot.Agent = new Agent(slotLlm, maxContextTokens: ModelCatalog.ResolveContextWindow(slotLlm.Model, _config.MaxContextTokens),
                maxBudgetUsd: _config.MaxBudgetUsd, autoCommit: _config.AutoGitCommit);
        }
        slot.Agent.AgentId = $"F{slotIdx + 1}"; // 槽位标识，供文件锁跨槽位冲突检测

        _agent = slot.Agent;
        ProgramContext.Agent = slot.Agent;

        // 重绑子智能体父引用（所有 Agent 共享 AgentTool 实例）
        foreach (var t in _agent.Tools)
        {
            if (t is AgentTool agentTool) agentTool.ParentAgent = _agent;
        }

        // 绑定槽位工作模式（灌入实例 + 接线回调）
        WireSlotWorkMode(slotIdx, _agent, screen);

        slot.IsBusy = true;
        slot.Cts = new CancellationTokenSource();
        var ct = slot.Cts.Token;
        screen.SlotStates[slotIdx] = SlotState.Working;

        // 记录任务开始时的累计花费，用于任务完成后展示单次花费
        slot.Agent.LlmClient.SnapshotTaskCost();

        var capturedScreen = screen;
        _slotTasks[slotIdx] = Task.Run(async () =>
        {
            // 绑定本槽位记忆目录：AsyncLocal 在该任务 async 链内生效，
            // 主线程后续切槽位不影响本任务读到的槽位值
            StructuredMemory.CurrentSlotIndex = slotIdx;
            try
            {
                await RunSlotAgentAsync(slotIdx, userInput, capturedScreen, ct);
            }
            catch (Exception ex)
            {
                DebugLog.Log("slot", $"槽位 F{slotIdx + 1} 后台任务异常: {ex.Message}");
            }
            finally
            {
                // 必须先摘除 Cts 再置 IsBusy=false：若反过来，二者之间 UI 线程看到 IsBusy=false
                // 会启动新任务写入新的 Cts，此处的 Exchange 会把新任务的 Cts 摘走并 Dispose，
                // 导致新任务无法被 Esc 中断（读到 null 即 no-op）。
                // 原子摘除并释放 CancellationTokenSource，避免泄漏；Esc 中断路径读到 null 即 no-op，杜绝 dispose 竞态
                Interlocked.Exchange(ref slot.Cts, null)?.Dispose();
                slot.IsBusy = false;
                if (capturedScreen.SlotStates[slotIdx] != SlotState.Error)
                    capturedScreen.SlotStates[slotIdx] = SlotState.Idle;
            }
        });
    }

    /// <summary>
    /// 槽位后台 Agent 主执行体。输出按"槽位是否活跃"路由：
    /// 活跃 → 实时写屏（复用 ChatScreen 流式方法）；非活跃 → 缓冲到槽位 ChatMessages，
    /// 切换回时由 RestoreTo 展示。路由决策与 SwitchAgentSlot 的切换共享槽位 Sync 锁，避免丢 token。
    /// </summary>
    private static async Task RunSlotAgentAsync(int slotIdx, string userInput, ChatScreen screen, CancellationToken ct)
    {
        var slot = _slots[slotIdx];
        var agent = slot.Agent!;
        var llm = agent.LlmClient;

        // 输出路由：活跃槽位实时写屏，非活跃槽位缓冲到槽位。整个"判定+写入"在 Sync 锁内原子完成。
        void Route(Action<ChatScreen> live, Action<AgentSlot> buffered)
        {
            lock (slot.Sync)
            {
                var active = TuiManager.Instance.ActiveScreen as ChatScreen;
                if (_activeSlot == slotIdx && active != null)
                    live(active);
                else
                    buffered(slot);
            }
        }

        // 与 ChatScreen.AddToolProgress 一致的渲染头（非活跃缓冲也保持相同样式）
        static string ToolLabel(string name, string brief)
            => $"  {WayCoder.UI.Tui.ToolRenderers.ToolRendererFactory.Get(name).FormatHeader(brief)}";

        var modelStack = BuildFallbackChain();
        var startTime = DateTime.UtcNow;
        var completed = false;

        for (int attempt = 0; attempt < modelStack.Length; attempt++)
        {
            var model = modelStack[attempt];
            if (attempt > 0)
            {
                // 仅改槽位专属 LLM，避免并发槽位间对全局 _config.Model 的竞态
                llm.Model = model;
                Route(cs => { cs.StatusLeft = model; cs.AddSystemMsg($"🔄 自动回退到: {model}"); cs.StartAgentMsg(); },
                      s => { s.StatusLeft = model; s.BufferedAddMsg("system", $"🔄 自动回退到: {model}"); s.BufferedStartStream(); });
            }

            try
            {
                Route(cs => { cs.Running = true; cs.StartAgentMsg(); },
                      s => { s.BufferedStartStream(); });

                await agent.ChatAsync(userInput,
                    onToken: tok => Route(
                        cs => { cs.Running = false; cs.OnToolFinished(); cs.EnsureAgentStreaming(); cs.AppendToken(tok); },
                        s => s.BufferedAppendToken(tok)),
                    onTool: (name, brief) => Route(
                        cs => { cs.FinishAgentMsg(); cs.AddToolProgress(name, brief.Length > 60 ? ContextManager.TruncateByRunes(brief, 57) + "..." : brief); cs.OnToolStarted(name, brief.Length > 40 ? ContextManager.TruncateByRunes(brief, 37) + "..." : brief); },
                        s => { s.BufferedFinishStream(); s.BufferedAddMsg("tool", ToolLabel(name, brief.Length > 60 ? ContextManager.TruncateByRunes(brief, 57) + "..." : brief), indent: 1); }),
                    onToolOutput: line => Route(
                        cs => { cs.AppendToLast(line + "\n"); cs.OnToolFinished(); },
                        s => s.BufferedAppendToLast(line + "\n")),
                    cancellationToken: ct);

                Route(cs => { cs.Running = false; cs.FinishAgentMsg(); },
                      s => s.BufferedFinishStream());
                completed = true;
                break; // 成功
            }
            catch (OperationCanceledException)
            {
                var cancelled = ct.IsCancellationRequested;
                if (!cancelled)
                    ErrorLog.Error("Program.REPL", $"LLM 请求超时（{Config.Instance.LlmHttpTimeoutSec}s）");
                var cancelMsg = cancelled ? "⚠ 已中断" : $"⏰ 服务器 {Config.Instance.LlmHttpTimeoutSec}s 未响应";
                Route(cs => { cs.Running = false; cs.FinishAgentMsg(); cs.AddSystemMsg(cancelMsg); },
                      s => { s.BufferedFinishStream(); s.BufferedAddMsg("system", cancelMsg); });
                if (!cancelled)
                    screen.SlotStates[slotIdx] = SlotState.Error;
                break;
            }
            catch (Exception ex) when (attempt < modelStack.Length - 1)
            {
                Route(cs => { cs.Running = false; cs.FinishAgentMsg(); cs.AddSystemMsg($"  ⚠ {model} 失败: {ex.Message}"); },
                      s => { s.BufferedFinishStream(); s.BufferedAddMsg("system", $"  ⚠ {model} 失败: {ex.Message}"); });
                ErrorLog.Warning("Program.REPL", $"模型 {model} 失败，尝试回退: {ex.Message}", ex);
                // 继续回退链
            }
            catch (Exception ex)
            {
                Route(cs => { cs.Running = false; cs.FinishAgentMsg(); cs.AddSystemMsg($"  💔 所有模型均失败: {ex.Message}"); },
                      s => { s.BufferedFinishStream(); s.BufferedAddMsg("system", $"  💔 所有模型均失败: {ex.Message}"); });
                screen.SlotStates[slotIdx] = SlotState.Error;
                ErrorLog.Error("Program.REPL", $"所有模型均失败: {ex.Message}", ex);
            }
        }

        // 完成通知
        if (completed)
        {
            var elapsed = (DateTime.UtcNow - startTime).TotalSeconds;
            var costMsg = FormatTaskCost(llm);
            Route(cs => cs.AddSystemMsg($"  ✅ 完成 · 耗时 {elapsed:F1}s · {costMsg}"),
                  s => s.BufferedAddMsg("system", $"  ✅ 完成 · 耗时 {elapsed:F1}s · {costMsg}"));
            DesktopNotifier.NotifyAgentFinished();
        }

        // 文件修改确认 + 最近文件跟踪
        var modified = EditFileTool.ChangedFiles;
        if (modified.Count > 0)
        {
            Route(cs =>
            {
                cs.AddSystemMsg($"📝 已修改 {modified.Count} 个文件 (/diff 查看 /undo 撤销 /recent 最近)");
                foreach (var f in modified)
                    if (!cs.RecentFiles.Contains(f)) { cs.RecentFiles.Add(f); if (cs.RecentFiles.Count > 50) cs.RecentFiles.RemoveAt(0); }
            }, s =>
            {
                s.BufferedAddMsg("system", $"📝 已修改 {modified.Count} 个文件 (/diff 查看 /undo 撤销 /recent 最近)");
                foreach (var f in modified)
                    if (!s.RecentFiles.Contains(f)) { s.RecentFiles.Add(f); if (s.RecentFiles.Count > 50) s.RecentFiles.RemoveAt(0); }
            });
        }

        // 更新右下角 token 显示 + 性能（仅活跃槽位，缓冲槽位切回时由 RestoreTo 重建）
        // 渲染交给主循环 50ms 轮询（MarkDirty 已置脏），避免后台线程与 UI 线程并发 Render。
        Route(cs =>
        {
            cs.UpdateTokenDisplayFull(
                llm.TotalPromptTokens, llm.TotalCompletionTokens,
                llm.EstimatedCost,
                ContextManager.EstimateTokens(agent.SnapshotMessages()), _config.MaxContextTokens,
                llm.LastLatencyMs, llm.LastTokensPerSec);
        }, _ => { });
    }

    /// <summary>
    /// 格式化单次任务的花费信息。包含输入/输出 token 数和美元成本。
    /// </summary>
    private static string FormatTaskCost(LLM llm)
    {
        var input = llm.TaskPromptTokens;
        var output = llm.TaskCompletionTokens;
        var cost = llm.TaskCost;

        var sb = new System.Text.StringBuilder();
        sb.Append($"📊 {input}+{output}={input + output} 词元");

        if (cost.HasValue)
        {
            var rmb = cost.Value * 7.25; // USD → RMB
            if (rmb < 0.01)
                sb.Append($" · 💰 ¥{rmb:F4}");
            else if (rmb < 1.0)
                sb.Append($" · 💰 ¥{rmb:F3}");
            else
                sb.Append($" · 💰 ¥{rmb:F2}");
        }
        else
        {
            sb.Append(" · 💰 未知定价");
        }

        return sb.ToString();
    }
}
