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
                // 流式 token 含 «» 中间格式（LLM 在推理段首尾注入 «dim»/«/»），
                // 必须解码成 ANSI 效果 —— 否则终端里直接显示出转义标记本身
                onToken: t => Console.Write(SpectreToAnsi(t)),
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
        var inputMgr = TuiManager.Instance.Input; // 共享输入管理器（RenderWait 复用，统一 paste 解析）

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
        // 状态栏左侧写模型名，不写品牌名 —— 品牌在顶栏标题和上面的欢迎横幅里已经有了，
        // 这里再来一遍就是第三遍；而且 /model 切换后本来就会把这里改成模型名，启动态跟着一致
        slot0.StatusLeft = _config.Model;
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
        // 回调在后台线程触发（Agent 请求权限），UI 部分投递到 UI 线程，绝不直接碰控件
        PermissionManager.PermissionPromptStarted += toolName =>
        {
            screen.PostToUI(() =>
            {
                screen.SlotStates[screen.ActiveSlotIndex] = SlotState.WaitingPerm;
                screen.OnPermissionWaiting(toolName);
            });
            DesktopNotifier.NotifyPermissionWaiting(toolName); // 非 UI 操作，后台直接做
        };
        PermissionManager.PermissionPromptResolved += _ =>
        {
            screen.PostToUI(() =>
            {
                if (screen.SlotStates[screen.ActiveSlotIndex] == SlotState.WaitingPerm)
                    screen.SlotStates[screen.ActiveSlotIndex] = SlotState.Working;
                screen.OnPermissionResolved();
            });
        };

        // 工作模式变更信号 → 同步当前槽位持久模式 + Agent 实例模式 + 状态栏显示
        // （覆盖 Shift+Tab 循环切换、/mode 命令等 UI 线程发起的变更来源；
        //   后台槽位批准计划走 Agent.OnWorkModeChanged，不经过此全局事件，避免污染活跃槽位）
        WorkModeManager.ModeChanged += mode =>
        {
            screen.PostToUI(() =>
            {
                _slots[_activeSlot].WorkMode = mode;
                if (_slots[_activeSlot].Agent != null)
                {
                    _slots[_activeSlot].Agent!.WorkMode = mode;
                    // 统一刷新工具集 + 系统提示词（修 P0-1：/mode、Shift+Tab 等入口此前不刷新）
                    _slots[_activeSlot].Agent!.ReapplyToolFilter();
                }
                screen.StatusBar.CurrentWorkMode = mode;
            });
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
                    screen.PostToUI(() => screen.AddSystemMsg(msg)); // ContinueWith 在线程池：投递 UI 线程
            }
            catch { /* REPL 已退出时静默忽略 */ }
        });

        // 注入 ChatScreen 回调
        screen.OnCycleModel = () => CycleModel(screen);
        screen.OnShowHelp = () => ShowHelpInChat(screen);
        screen.OnSearchHistory = query => SearchHistory(query, screen);
        screen.OnOpenSessions = () => OpenSessions(screen);
        screen.OnReasoningEffort = () => PickReasoningEffort(screen);
        screen.OnOpenDiff = () =>
        {
            // Ctrl+D → /diff：匹配并同步执行（DiffCommand 无 await，RenderWait 自驱渲染）
            var (cmd, args) = SlashCommandRegistry.Match("/diff");
            if (cmd != null) cmd.ExecuteAsync(args, screen).GetAwaiter().GetResult();
        };
        // Ctrl+Shift+P → 命令面板（对齐 Claude Code quickOpen / OpenCode）
        screen.OnOpenCommandPalette = () =>
        {
            var cmds = WayCoder.UI.Tui.CommandPalette.BuildDefaultCommands(screen);
            if (cmds.Count > 0) WayCoder.UI.Tui.CommandPalette.Show(cmds);
        };

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
            screen.CurrentSessionId = _currentSessionIds[_activeSlot]; // 侧边栏会话区标记「当前」
            screen.PumpUIQueue(); // 消费后台投递的 UI 操作（Agent 流式 / 权限回调等）
            mgr.Render();

            // 处理 ChatScreen 提交的消息（Enter 键 → async LLM 调用）。
            // 输入排队机制：Agent 忙碌时不打断 —— 普通对话留在队列（TryPeek 不取走），
            // 等当前槽位 Agent 批次完成后由本循环继续取指令；斜杠命令即时处理不受限。
            bool slotBusy = _slots[_activeSlot].IsBusy;
            while (screen.PendingSubmissions.TryPeek(out var peek))
            {
                // "\x1b" 特殊标记：退出请求
                if (peek == "\x1b")
                {
                    screen.PendingSubmissions.TryDequeue(out _);
                    AutoSaveSession();
                    _watchMode?.Dispose();
                    mgr.Exit();
                    Environment.Exit(0);
                }
                // 队头是普通对话且 Agent 忙 → 不取走（排队等待）：状态显示在动态栏（⏳排队N），不弹聊天区
                if (slotBusy && !peek.StartsWith('/'))
                {
                    break;
                }
                if (!screen.PendingSubmissions.TryDequeue(out var submission))
                    break;
                mgr.Render();
                await ProcessUserInput(submission, screen);
                slotBusy = _slots[_activeSlot].IsBusy; // 处理普通对话会启动任务 → 下一轮跳过后续队列
            }
            // 动态栏排队显示：每轮按队列实际待处理指令数更新
            // （Agent 忙时队列有等待指令 → 显示 ⏳排队N；空闲处理完 → 0）
            int queueCount = screen.PendingSubmissions.Count(q => !q.StartsWith('/') && q != "\x1b");
            screen.SetQueuedCount(queueCount);

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

            // Shift+Tab 是独立事件类型（Unix 下是 ESC[Z），KeyInfo 可能是空的 ——
            // 下发给窗口前补成真键，否则窗口收到一个空 ConsoleKeyInfo
            if (ev.Type == InputType.ShiftTab && key.Key != ConsoleKey.Tab)
                key = new ConsoleKeyInfo('\t', ConsoleKey.Tab, shift: true, alt: false, control: false);

            // ── 键位作用域总闸（规则见 TuiKeyScope）──
            // 栈顶有窗口/对话框时键盘归它：除系统键（仅 Ctrl+C）外，REPL 这一层一律不得截胡。
            // 没有这道闸，后台线程弹确认框时主循环仍在读键，下面那几组「系统级」分支就会穿透对话框 ——
            // 按 F1 在对话框底下切槽位、还先 PopScreen 把屏幕栈拆掉，Ctrl+Q 直接退进程。
            if (mgr.ActiveScreen?.FocusedWindow != null && !TuiKeyScope.IsSystemKey(key))
            {
                mgr.OnKey(key);
                mgr.Render();
                continue;
            }

            // 系统级：Ctrl+C 设置退出标志，走正常清理路径
            if (key.Key == ConsoleKey.C && ctrl)
            {
                _exitRequested = true;
                continue;
            }

            // 以下全是窗口键：能走到这里就说明栈顶没有窗口（被上面的总闸拦掉了），
            // 也就是焦点在 ChatScreen 上。对话框开着时它们一律不生效。

            // 窗口键：Ctrl+Q 紧急退出（强制保存所有槽位 + 恢复终端）
            if (key.Key == ConsoleKey.Q && ctrl)
            {
                PanicExit("用户 Ctrl+Q 紧急退出");
            }

            // 窗口键：切换工作模式（Build → Plan → Chat）
            // 判定见 InputEvent.IsModeSwitchKey —— Unix 的 ESC[Z / Windows 的 Tab+Shift / 通用 Ctrl+K
            // 槽位/Agent 模式同步 + 工具集刷新 + 状态栏由 ModeChanged 处理器统一完成
            if (InputEvent.IsModeSwitchKey(ev))
            {
                var newMode = WorkModeManager.CycleNext();
                screen.AddSystemMsg($"工作模式: {WorkModeManager.Format(newMode)}（Shift+Tab / Ctrl+K 切换）");
                mgr.Render();
                continue;
            }

            // 窗口键：F1~F10 切换 Agent 槽位。
            // 注意 ChatScreen.HandleGlobalShortcut 里还有一份（只切 UI，不绑 Agent），
            // 两份重复：这里先命中就 continue，那份只在对话框自带收键循环里才有机会跑。
            // 合并需要给 ChatScreen 接一个回调来做 Agent 侧绑定，属独立重构，暂不动。
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

            // 窗口键：Ctrl+P 循环切换权限模式（问答→自动→智能→畅通）
            if (key.Key == ConsoleKey.P && ctrl)
            {
                PermissionManager.CycleMode();
                screen.AddSystemMsg($"权限模式: {PermissionManager.FormatMode()}（Ctrl+P 循环切换）");
                RefreshActiveSlotTools(); // 权限模式可影响工具集（YOLO 换 YoloToolAllowList），切换后刷新
                mgr.Render();
                continue;
            }

            // 窗口键：Ctrl+E 循环切换经济模式（关闭→自动→开启→极致）
            if (key.Key == ConsoleKey.E && ctrl)
            {
                var eco = _config.CycleEconomy();
                var name = eco switch
                {
                    EconomyMode.On => "省钱",
                    EconomyMode.Auto => "自动",
                    EconomyMode.Extreme => "极致",
                    _ => "关闭",
                };
                screen.AddSystemMsg($"经济模式: {name}（Ctrl+E 循环切换）");
                mgr.Render();
                continue;
            }

            // 窗口键：Ctrl+X 交换当前槽位的大小模型
            if (key.Key == ConsoleKey.X && ctrl)
            {
                // 交换 = 大小模型整套（模型 id + 服务商 + 网关 + KEY）整体互换：
                // 新大模型继承原小模型的服务商/网关/Key，新小模型继承原大模型的。
                // key 是服务商级（ApiKeyStore），交换服务商后新模型自动取对应 store key。
                var slotCfg = AgentSlotConfig.Get(_activeSlot);
                if (!slotCfg.UseGlobal)
                {
                    var oldLarge = slotCfg.LargeModel;
                    var smallModel = slotCfg.SmallModel ?? _config.SmallModel;
                    var smallInfo = ModelCatalog.Find(smallModel);
                    var smallProvider = smallInfo?.ProviderId ?? _config.SmallProvider;
                    var smallGw = smallInfo?.DefaultBaseUrl
                        ?? (ModelCatalog.Providers.TryGetValue(smallProvider, out var p) ? p.DefaultBaseUrl : null);
                    AgentSlotConfig.Set(_activeSlot, new AgentSlotConfig.SlotConfig
                    {
                        UseGlobal = false,
                        LargeModel = smallModel,                 // 新大模型 = 原小模型
                        SmallModel = oldLarge,                   // 新小模型 = 原大模型
                        BaseUrl = smallGw,                       // 原小模型网关
                        ApiKeyProviderId = smallProvider,        // 原小模型服务商
                        ApiKey = ApiKeyStore.Get(smallProvider) ?? "", // 原小模型 key
                    });
                }
                else
                {
                    (_config.Model, _config.SmallModel) = (_config.SmallModel, _config.Model);
                    (_config.Provider, _config.SmallProvider) = (_config.SmallProvider, _config.Provider);
                    // 新大模型（原小模型）网关：模型目录默认
                    var newLargeInfo = ModelCatalog.Find(_config.Model);
                    if (newLargeInfo?.DefaultBaseUrl != null)
                        _config.BaseUrl = newLargeInfo.DefaultBaseUrl;
                    // 新大模型 key = 新服务商（原 SmallProvider）在 store 的 key（key 是服务商级）
                    _config.ApiKey = ApiKeyStore.Get(_config.Provider) ?? "";
                    _config.SaveToEnvFile();
                }

                var lg = AgentSlotConfig.ResolveLargeModel(AgentSlotConfig.Get(_activeSlot), _activeSlot);
                var sm = AgentSlotConfig.ResolveSmallModel(AgentSlotConfig.Get(_activeSlot), _activeSlot);

                // 运行时真正生效（对齐 CycleModel）：只改配置/模型栏是「仅仅交换了显示」，
                // RunSlotAgentAsync 用的是 agent.LlmClient（Agent 持有的实例）——
                // 必须更新当前槽位 Agent 的 LLM + 上下文窗口，实际请求才会走新模型
                var curAgent = _slots[_activeSlot].Agent;
                if (curAgent?.LlmClient != null)
                {
                    curAgent.LlmClient.Model = lg;
                    curAgent.LlmClient.SmallModel = sm;
                }
                curAgent?.UpdateContextWindow(ModelCatalog.ResolveContextWindow(lg, _config.MaxContextTokens));
                if (slotCfg.UseGlobal && _llm != null)
                {
                    _llm.Model = _config.Model;
                    _llm.SmallModel = _config.SmallModel;
                }
                var slotSwap = _slots[_activeSlot];
                if (slotSwap != null)
                {
                    // 同步槽位 LLM 实例与 Last* 记录，避免 GetSlotLlm 下次误判模型变化而重建
                    if (slotSwap.LlmClient != null)
                    {
                        slotSwap.LlmClient.Model = lg;
                        slotSwap.LlmClient.SmallModel = sm;
                    }
                    slotSwap.LastLargeModel = lg;
                    slotSwap.LastSmallModel = sm;
                }

                screen.AddSystemMsg($"🔄 大小模型交换 → 大:{lg} · 小:{sm}");
                mgr.Render();
                continue;
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
        InAgentRenderLoop = true; // 对话框（diff 等）据此区分 Agent 场景：外层渲染，RenderWait 只等
        try
        {
        var agentTask = Task.Run(async () =>
        {
            await _agent!.ChatAsync(_currentUserInput!,
                onToken: tok =>
                {
                    // 后台回调只投递，不直接改控件树（UI 线程 PumpUIQueue 消费，杜绝并发遍历崩溃）
                    screen_!.PostToUI(() =>
                    {
                        screen_!.Running = false;
                        screen_!.OnToolFinished(); // token 开始流入 = 回到思考状态
                        screen_!.EnsureAgentStreaming();
                        screen_!.AppendToken(tok);
                    });
                },
                onTool: (name, brief) =>
                {
                    string briefCopy = brief;
                    screen_!.PostToUI(() =>
                    {
                        screen_!.FinishAgentMsg();
                        // 完整 brief 交给 AddToolProgress 按聊天区宽度截取 —— 提前砍 57 字符会把
                        // bash 命令/文件路径的参数截得没法看；动态栏那份仍截短（一行小空间）
                        screen_!.AddToolProgress(name, briefCopy);
                        screen_!.OnToolStarted(name, briefCopy.Length > 40 ? ContextManager.TruncateByRunes(briefCopy, 37) + "..." : briefCopy);
                        // 不立刻 StartAgentMsg，等 onToolOutput 流式输出完毕再懒启动
                    });
                    // 每 3 次工具调用自动保存（文件 IO，非 UI 操作，留在后台线程）
                    if (++_toolCallCount % 3 == 0)
                        AutoSaveSession();
                },
                onToolOutput: line =>
                {
                    screen_!.PostToUI(() =>
                    {
                        screen_!.AppendToLast(line + "\n");
                        screen_!.OnToolFinished(); // 流式输出 = 工具已执行完毕，回到思考
                    });
                },
                cancellationToken: cts.Token);
        });

        var mgr = TuiManager.Instance;
        var inputMgr = TuiManager.Instance.Input; // 共享 InputManager：统一 paste/CSI/鼠标解析
        while (!agentTask.IsCompleted)
        {
            screen_?.PumpUIQueue(); // 消费后台投递的 UI 操作（Agent 流式回调）
            try { mgr.Render(); }
            catch (Exception ex)
            {
                // 控件渲染异常不崩溃：记日志 + 强制全刷新重试（某控件 OnRender 偶发异常）
                ErrorLog.Error("UI.Render", $"渲染异常（已兜底继续）: {ex.GetType().Name}: {ex.Message}");
                WayCoder.UI.TUI.Base.TuiManager.RequestFullRefresh();
            }
            var ev = inputMgr.ReadInput(30);
            if (ev.Type == InputType.Timeout) continue;

            // 鼠标：Agent 忙时对话框按钮点击（diff/权限框）→ 路由窗口（原 Console.ReadKey 只读键盘，按钮点不了）
            if (ev.Type == InputType.Mouse)
            {
                if (TuiManager.MouseEnabled) mgr.HandleMouse(ev);
                continue;
            }
            // 粘贴 → bracketed paste 路由 ChatScreen
            if (ev.Type == InputType.Paste)
            {
                if (!string.IsNullOrEmpty(ev.PasteText)) screen_?.HandleBracketedPaste(ev.PasteText);
                continue;
            }
            if (ev.Type == InputType.Resize) { mgr.OnResize(); continue; }

            var key = ev.KeyInfo;
            // Shift+Tab 是独立事件（Unix 下是 ESC[Z，KeyInfo 可能空），补成真键
            if (ev.Type == InputType.ShiftTab && key.Key != ConsoleKey.Tab)
                key = new ConsoleKeyInfo('\t', ConsoleKey.Tab, shift: true, alt: false, control: false);

            // 键位作用域：栈顶有窗口/对话框时键盘归它（同 REPL 主循环的总闸）。
            // 没有这道闸，/loop 这类走本循环的路径会在 diff/权限对话框开着时把
            // Y/N/A/Q 之类按键当无用键吃掉 —— 就是「对话框快捷键按不了」。
            if (mgr.ActiveScreen?.FocusedWindow != null && !TuiKeyScope.IsSystemKey(key))
            {
                mgr.OnKey(key);
                continue;
            }
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
            else
            {
                // Agent 忙：普通键路由给 ChatScreen —— 输入框继续可编辑，Enter 提交进
                // PendingSubmissions 队列，Agent 空闲后由 REPL 主循环逐个处理（排队等响应，界面不卡死）
                mgr.OnKey(key);
            }
        }
        await agentTask; // 传播异常
        }
        finally { InAgentRenderLoop = false; }
    }

    /// <summary>是否处于 Agent 渲染循环（RunAgentWithRenderLoop 活跃）。
    /// 对话框（DiffPreview 等）据此区分：Agent 场景外层循环负责渲染+路由，RenderWait 只等待；
    /// 命令场景（/diff）无外层循环，RenderWait 自己渲染+读键。</summary>
    internal static volatile bool InAgentRenderLoop;

    /// <summary>
    /// 后台任务 + UI 渲染循环：长命令（如 /test all 同步跑 20-30s 自测）此前直接 await 在 UI 线程
    /// 阻塞主循环 → 界面卡死无法输入。本方法把任务丢后台线程跑，UI 线程保持渲染 + 读键路由
    /// （普通键可继续打字排队，对话框正常路由），任务完成返回结果。
    /// </summary>
    internal static async Task<T> RunWithUiLoop<T>(Func<T> background, ChatScreen screen)
    {
        var task = Task.Run(background);
        var mgr = TuiManager.Instance;
        var inputMgr = TuiManager.Instance.Input;
        while (!task.IsCompleted)
        {
            screen.PumpUIQueue();
            mgr.Render();
            var ev = inputMgr.ReadInput(30);
            if (ev.Type == InputType.Timeout) continue;
            if (ev.Type == InputType.Mouse && TuiManager.MouseEnabled) { mgr.HandleMouse(ev); continue; }
            if (ev.Type == InputType.Paste) { if (!string.IsNullOrEmpty(ev.PasteText)) screen.HandleBracketedPaste(ev.PasteText); continue; }
            if (ev.Type == InputType.Resize) { mgr.OnResize(); continue; }
            var key = ev.KeyInfo;
            // 键位作用域：栈顶有窗口/对话框时键盘归它
            if (mgr.ActiveScreen?.FocusedWindow != null && !TuiKeyScope.IsSystemKey(key)) { mgr.OnKey(key); continue; }
            // 命令执行期：普通键路由给 ChatScreen（输入框可编辑，Enter 提交排队）
            mgr.OnKey(key);
        }
        return await task;
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
            // 复用须校验 LlmClient.Model 未被回退残留污染（RunSlotAgentAsync 回退会改写 llm.Model）
            if (slot.LlmClient != null
                && slot.LastLargeModel == _llm!.Model
                && slot.LlmClient.Model == _llm.Model)
                return slot.LlmClient;
            var clone = _llm!.Clone();
            clone.ModelOverride = null; // 槽位从无覆盖状态起步，不继承瞬时小模型覆盖
            slot.LlmClient = clone;
            slot.LastLargeModel = _llm.Model;
            slot.LastSmallModel = _llm.SmallModel;
            return clone;
        }

        // 独立配置槽位：统一解析模型/key/baseUrl（槽位优先 → .env 回退 → 写回槽位），
        // 保证与状态栏（ResolveLargeModel）同源、与实际请求一致。
        var (largeModel, apiKey, baseUrl, _) = AgentSlotConfig.ResolveEffectiveModel(slotIdx);
        var smallModel = AgentSlotConfig.ResolveSmallModel(slotCfg, slotIdx);

        // 模型/key 未变 → 复用已有 LLM；LlmClient.Model 被回退残留污染（≠解析值）时重建
        if (slot.LlmClient != null
            && slot.LastLargeModel == largeModel
            && slot.LastSmallModel == smallModel
            && slot.LlmClient.Model == largeModel)
            return slot.LlmClient;

        // 创建新的 LLM（使用槽位专属 API Key 和 BaseUrl）
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
        agent.ReapplyToolFilter(); // 绑定槽位模式后按档位重建工具集/提示词（补「持久模式为 Chat/Plan 的槽位不刷新」缺口）
        agent.OnWorkModeChanged = mode =>
        {
            _slots[slotIdx].WorkMode = mode;
            agent.ReapplyToolFilter(); // 计划审批门批准回 Build 等内部切模式后刷新工具集/提示词
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

        // /loop 与 /plan 会直接启动 ChatAsync：若当前槽位 Agent 正在后台运行，
        // 不检查 IsBusy 会造成同一 Agent/LLM 并发 ChatAsync（推理缓冲/模型覆盖竞态、输出错乱）。
        if (userInput.StartsWith("/loop "))
        {
            if (_slots[_activeSlot].IsBusy)
            {
                screen.AddSystemMsg("⚠ 当前槽位 Agent 正在运行中，/loop 无法启动（请先 Esc 中断或等待完成）");
                return;
            }
            await RunLoopAsync(userInput[6..].Trim(), screen);
            return;
        }

        if (userInput == "/plan")
        {
            if (_slots[_activeSlot].IsBusy)
            {
                screen.AddSystemMsg("⚠ 当前槽位 Agent 正在运行中，/plan 无法启动（请先 Esc 中断或等待完成）");
                return;
            }
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
            // 排队：不打断当前任务 —— 指令入队，等 Agent 当前批次完成后由主循环取指令自动执行
            screen.PendingSubmissions.Enqueue(userInput);
            screen.AddSystemMsg("⏳ Agent 忙碌中 — 指令已排队，当前批次完成后自动执行");
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

        // 输出路由：活跃槽位投递到 UI 线程（PostToUI 队列），非活跃槽位缓冲到槽位。
        // 后台 Agent 线程绝不直接改 ChatScreen 控件树 —— 那会与 UI 线程的
        // FindFocused/OnRender 遍历 Children 竞态崩溃（Collection was modified）。
        // 整个"判定+投递"在 Sync 锁内原子完成。
        void Route(Action<ChatScreen> live, Action<AgentSlot> buffered)
        {
            lock (slot.Sync)
            {
                var active = TuiManager.Instance.ActiveScreen as ChatScreen;
                if (_activeSlot == slotIdx && active != null)
                    active.PostToUI(() => live(active)); // 投递：UI 线程 PumpUIQueue 消费
                else
                    buffered(slot);
            }
        }

        // 与 ChatScreen.AddToolProgress 一致的渲染头（非活跃缓冲也保持相同样式）
        static string ToolLabel(string name, string brief)
            => $"  {WayCoder.UI.Tui.ToolRenderers.ToolRendererFactory.Get(name).FormatHeader(brief)}";

        // 回退链首项用槽位实际模型（llm.Model），而非全局 _config.Model——
        // 否则槽位模型与 .env 不一致时，首项/失败消息会显示错误模型（如 mimo-v2.5）
        var modelStack = BuildFallbackChain(llm.Model);
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
                        cs => { cs.FinishAgentMsg(); cs.AddToolProgress(name, brief); cs.OnToolStarted(name, brief.Length > 40 ? ContextManager.TruncateByRunes(brief, 37) + "..." : brief); },
                        s => { s.BufferedFinishStream(); s.BufferedAddMsg("tool", ToolLabel(name, brief), indent: 1); }),
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
                llm.LargeTotalTokens, llm.SmallTotalTokens, // 分大小模型上下文用量
                llm.EstimatedCost ?? llm.TaskCost, // 累计费用优先，回退本轮费用
                ContextManager.EstimateTokens(agent.SnapshotMessages()), agent.Context.MaxTokens,
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
