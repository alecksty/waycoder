using System.Text;
using WayCoder.Tools;
using WayCoder.UI;
using WayCoder.Terminal;
using WayCoder.UI.TuiScreens;

namespace WayCoder;

/// <summary>
/// 入口 + CLI + REPL —— 面向用户的终端界面。
/// </summary>
public class Program
{
    private static Config _config = new();
    private static LLM? _llm;
    private static Agent? _agent;
    private static readonly AgentSlot[] _slots = new AgentSlot[AgentSlot.Count];
    private static int _activeSlot; // 当前活跃槽位索引（F1 对应 0）

    /// <summary>当前活跃槽位索引（供外部命令访问）</summary>
    public static int ActiveSlotIndex => _activeSlot;

    /// <summary>所有槽位数组（供外部命令访问）</summary>
    public static AgentSlot[] GetSlots() => _slots;
    private static WatchMode? _watchMode;
    private static volatile bool _agentBusy;
    private static volatile bool _exitRequested;
    private static int _lastStreamW, _lastStreamH;
    private static CancellationTokenSource? _agentCts;
    private static (List<JsonObject> Messages, string Model)? _pendingRestore;

    /// <summary>Watch 模式线程安全提示队列</summary>
    private static readonly System.Collections.Concurrent.ConcurrentQueue<string> _pendingWatchPrompts = new();

    public static async Task<int> Main(string[] args)
    {
        Console.OutputEncoding = Encoding.UTF8;

        // 全局异常处理：恢复终端 + 保存会话
        AppDomain.CurrentDomain.UnhandledException += (_, e) =>
        {
            try { Tty.ExitAltScreen(); } catch { }
            try { AutoSaveException(e.ExceptionObject as Exception); } catch { }
        };
        TaskScheduler.UnobservedTaskException += (_, e) =>
        {
            try { AutoSaveException(e.Exception); } catch { }
            e.SetObserved();
        };

        // 注册 + 解析 CLI 参数（重复名称自动报错）
        Arguments.BuiltinArgs.RegisterAll();
        var (parsed, exitCode) = Arguments.CliArgRegistry.Parse(args);
        if (exitCode.HasValue) return exitCode.Value;

        // 读取值参数
        string? model = Arguments.CliArgRegistry.Get(parsed, "model");
        string? baseUrl = Arguments.CliArgRegistry.Get(parsed, "base-url");
        string? apiKey = Arguments.CliArgRegistry.Get(parsed, "api-key");
        string? prompt = Arguments.CliArgRegistry.Get(parsed, "prompt");
        string? resumeId = Arguments.CliArgRegistry.Get(parsed, "resume");
        double? maxBudget = null;
        var budgetStr = Arguments.CliArgRegistry.Get(parsed, "max-budget-usd");
        if (budgetStr != null && double.TryParse(budgetStr, out var b)) maxBudget = b;

        bool yoloMode = Arguments.CliArgRegistry.Has(parsed, "yolo");
        bool watchMode = Arguments.CliArgRegistry.Has(parsed, "watch");

        if (Arguments.CliArgRegistry.Has(parsed, "version"))
        {
            Console.WriteLine(Global.AppNameVersion);
            return 0;
        }

        if (Arguments.CliArgRegistry.Has(parsed, "help"))
        {
            ShowUsage();
            return 0;
        }

        // 项目初始化向导
        if (Arguments.CliArgRegistry.Has(parsed, "init"))
        {
            RunInit();
            return 0;
        }

        // 标准输入管道模式：echo "prompt" | waycoder
        if (prompt == null && Console.IsInputRedirected)
        {
            var stdinText = Console.In.ReadToEnd().Trim();
            // 只有当 stdin 真正有内容时才作为管道输入
            if (!string.IsNullOrEmpty(stdinText))
            {
                prompt = stdinText;
                // 管道输入非交互，自动开启 yolo 模式
                if (!yoloMode) yoloMode = true;
            }
        }

        // -p 一次性模式：非交互，自动放行权限
        if (prompt != null)
            yoloMode = true;

        _config = Config.FromEnv();
        // 加载主题（优先 theme.json，回退配置项）
        if (ThemeConfig.Instance.BorderStyle == "single" && ThemeConfig.Instance.BorderColor == 36)
            ThemeConfig.ApplyPreset(_config.ThemePreset);
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
            MarkupLine("«bold red»╔══════════════════════════════╗«/»");
            MarkupLine("«bold red»║  API 密钥未设置！           ║«/»");
            MarkupLine("«bold red»╚══════════════════════════════╝«/»");
            Console.WriteLine();
            Console.WriteLine("请设置以下环境变量之一:");
            Console.WriteLine("  WAYCODER_API_KEY");
            Console.WriteLine("  DEEPSEEK_API_KEY");
            Console.WriteLine("  OPENAI_API_KEY");
            Console.WriteLine("  ANTHROPIC_API_KEY");
            Console.WriteLine("  API_KEY");
            Console.WriteLine();
            Console.WriteLine("或者在项目根目录创建 .env 文件:");
            Console.WriteLine("  WAYCODER_API_KEY=sk-你的密钥");
            return 1;
        }

        _llm = new LLM(_config.Model, _config.ApiKey, _config.BaseUrl,
            _config.MaxTokens, _config.Temperature);

        // 语义记忆：向量嵌入初始化
        EmbeddingStore.LlmClient = _llm;
        EmbeddingStore.Enabled = _config.EmbeddingEnabled;
        EmbeddingStore.EmbeddingModel = _config.EmbeddingModel;

        _agent = new Agent(_llm, maxContextTokens: _config.MaxContextTokens,
            maxBudgetUsd: _config.MaxBudgetUsd, autoCommit: _config.AutoGitCommit);
        ProgramContext.Agent = _agent;
        _slots[0] = new AgentSlot { Agent = _agent }; // 槽位 0 持有主 Agent

        // --yolo / -p / 管道输入: 非交互模式下跳过所有权限确认
        if (yoloMode)
        {
            SandboxManager.SetLevel("full-auto");
            PermissionManager.CurrentMode = PermissionManager.Mode.Yolo;
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

        // 团队知识库共享：启动时自动拉取远程共享记忆
        SharedMemoryManager.Enabled = _config.TeamMemoryEnabled;
        if (_config.TeamMemoryEnabled && _config.TeamMemoryAutoSync)
        {
            try
            {
                var pullResult = await SharedMemoryManager.PullSharedAsync();
                if (pullResult.Success && (pullResult.NewFiles.Count > 0 || pullResult.UpdatedFiles.Count > 0))
                {
                    Console.WriteLine($"📥 团队记忆同步: {pullResult.Message}");
                }
            }
            catch (Exception ex)
            {
                DebugLog.Log("Program", $"团队记忆自动同步失败: {ex.Message}");
            }
        }

        // 加载自定义斜杠命令、hooks、MCP 服务器和检查点
        CustomCommands.Load();
        SlashCommandRegistry.RegisterAll();
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
                if (model == null)
                {
                    _llm.Model = loaded.Value.Model;
                    _config.Model = loaded.Value.Model;
                }

                MarkupLine($"«green»✔ 已恢复会话:«/» «cyan»{E(resumeId)}«/» «dim»(模型: {E(_llm.Model)})«/»");
            }
            else
            {
                MarkupLine($"«red»✘ 会话 '{E(resumeId)}' 未找到«/»");
                return 1;
            }
        }

        if (!string.IsNullOrEmpty(prompt))
            await RunOnceAsync(prompt);
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

        try
        {
            MarkupLine($"«dim»🤖 {E(prompt)}«/»");
            await ChatWithStatusAsync(prompt, cts.Token);
            Console.WriteLine();
        }
        catch (OperationCanceledException)
        {
            if (cts.IsCancellationRequested)
            {
                MarkupLine("\n«orange3»⚠ 已中断«/»");
                Environment.Exit(130);
            }
            else
            {
                UxHelper.Error("请求超时", "服务器 60s 未响应，请检查网络或 API 配置");
                Environment.Exit(1);
            }
        }
        catch (Exception ex)
        {
            UxHelper.Error("错误", ex.Message);
            Environment.Exit(1);
        }
    }

    // ========================================================================
    // 交互式 REPL
    // ========================================================================

    private static async Task RunReplAsync()
    {
        var mgr = TuiManager.Instance;
        var screen = new ChatScreen();
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
        using var inputMgr = new UI.InputManager();
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
        slot0.StatusLeft = Global.AppFullName;
        slot0.HasWelcome = true;
        _llm!.SmallModel = _config.SmallModel;

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

        // 权限确认框信号 → 槽位状态栏标记"等待权限"
        PermissionManager.PermissionPromptStarted += _ =>
            screen.SlotStates[screen.ActiveSlotIndex] = SlotState.WaitingPerm;
        PermissionManager.PermissionPromptResolved += _ =>
        {
            if (screen.SlotStates[screen.ActiveSlotIndex] == SlotState.WaitingPerm)
                screen.SlotStates[screen.ActiveSlotIndex] = SlotState.Working;
        };

        // Watch 模式 — 监听外部编辑器文件变更
        if (_config.WatchMode)
        {
            StartWatchMode(screen);
            screen.AddSystemMsg("👁 Watch 模式已启动 — 在文件中写 AI! 注释自动触发 Agent");
        }

        // 尝试恢复上次会话
        TryRestoreSession(screen);

        // 注入 ChatScreen 回调
        screen.OnCycleModel = () => CycleModel(screen);
        screen.OnShowHelp = () => ShowHelpInChat(screen);
        screen.OnSearchHistory = query => SearchHistory(query, screen);

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
                screen.AddSystemMsg($"👁 Watch: {watchPrompt[..Math.Min(watchPrompt.Length, 80)]}");
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

            // 鼠标 — 暂不开启，待后续稳定后通过 WAYCODER_MOUSE=1 启用
            if (ev.Type == InputType.Mouse)
            {
                // if (Environment.GetEnvironmentVariable("WAYCODER_MOUSE") != "1") continue;
                // mgr.HandleMouse(ev);
                // mgr.Render();
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

            var auto = SessionManager.LoadSession("_auto");
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
                if (slot?.Agent?.Messages == null || slot.Agent.Messages.Count == 0) continue;
                var hasUser = slot.Agent.Messages.Any(m => (string?)m["role"] == "user");
                if (!hasUser) continue;
                var slotSuffix = i == 0 ? "_auto" : $"_auto_slot{i}";
                try { SessionManager.SaveSession(slot.Agent.Messages, _config.Model, slotSuffix); } catch { }
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
                if (slot?.Agent?.Messages == null || slot.Agent.Messages.Count == 0) continue;
                var slotSuffix = $"_auto_slot{i}";
                try { SessionManager.SaveSession(slot.Agent.Messages, _config.Model, slotSuffix); } catch { }
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
                    screen_!.EnsureAgentStreaming();
                    screen_!.AppendToken(tok);
                },
                onTool: (name, brief) =>
                {
                    screen_!.FinishAgentMsg();
                    screen_!.AddToolProgress(name, brief.Length > 60 ? brief[..57] + "..." : brief);
                    // 不立刻 StartAgentMsg，等 onToolOutput 流式输出完毕再懒启动
                    // 每 3 次工具调用自动保存
                    if (++_toolCallCount % 3 == 0)
                        AutoSaveSession();
                },
                onToolOutput: line =>
                {
                    screen_!.AppendToLast(line + "\n");
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
        if (slotCfg.UseGlobal) return _llm!;

        var slot = _slots[slotIdx];
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

    private static void SwitchAgentSlot(int idx, ChatScreen screen)
    {
        if (idx < 0 || idx >= AgentSlot.Count || idx == _activeSlot) return;
        if (_agentBusy)
        {
            screen.AddSystemMsg("⚠ Agent 正在运行，请等待完成后再切换槽位");
            return;
        }

        // 保存当前槽位状态
        _slots[_activeSlot].SaveFrom(screen);

        // 懒创建目标槽位 Agent
        _activeSlot = idx;
        StructuredMemory.CurrentSlotIndex = idx;
        var slot = _slots[idx];
        var slotLlm = GetSlotLlm(idx);
        if (slot.Agent == null)
        {
            slot.Agent = new Agent(slotLlm, maxContextTokens: _config.MaxContextTokens,
                maxBudgetUsd: _config.MaxBudgetUsd, autoCommit: _config.AutoGitCommit);
        }
        else
        {
            // 更新已存在的 Agent 的 LLM（模型可能已变更）
            slot.Agent.LlmClient.Model = AgentSlotConfig.ResolveLargeModel(AgentSlotConfig.Get(idx), idx);
            slot.Agent.LlmClient.SmallModel = AgentSlotConfig.ResolveSmallModel(AgentSlotConfig.Get(idx), idx);
        }

        _agent = slot.Agent;
        ProgramContext.Agent = slot.Agent;

        // 重绑子智能体父引用（所有 Agent 共享 AgentTool 实例）
        foreach (var t in _agent.Tools)
        {
            if (t is AgentTool agentTool) agentTool.ParentAgent = _agent;
        }

        // 首次激活显示欢迎提示
        if (!slot.HasWelcome)
        {
            slot.HasWelcome = true;
            slot.ChatMessages.Add(new ChatMsg
            {
                Role = "system",
                Content = $"🤖 Agent 槽位 F{idx + 1} — 独立会话，Ctrl+E编辑器 Ctrl+T设置 Ctrl+H帮助 Ctrl+B面板 Ctrl+Q退出",
            });
        }

        slot.RestoreTo(screen);
        screen.ActiveSlotIndex = idx;

        // 恢复目标槽位的工作模式
        WorkModeManager.CurrentMode = slot.WorkMode;
        screen.StatusBar.CurrentWorkMode = slot.WorkMode;

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

        if (userInput == "/resume" && _pendingRestore != null)
        {
            var (msgs, model) = _pendingRestore.Value;
            _agent!.Messages.Clear();
            _agent.Messages.AddRange(msgs);
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

        // 调用 Agent (支持自动回退)
        using var cts = new CancellationTokenSource();
        _agentCts = cts;
        _agentBusy = true;
        screen.SlotStates[screen.ActiveSlotIndex] = SlotState.Working;
        try
        {
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
                    screen.StatusLeft = model;
                    screen.AddSystemMsg($"🔄 自动回退到: {model}");
                    screen.StartAgentMsg();
                }

                try
                {
                    screen.Running = true;
                    screen.StartAgentMsg();
                    screen.Render();

                    // 后台执行 Agent（主线程保持渲染 + 响应热键）
                    _currentUserInput = userInput;
                    screen_ = screen;
                    _toolCallCount = 0;
                    await RunAgentWithRenderLoop(cts);

                    screen.Running = false;
                    screen.FinishAgentMsg();
                    completed = true;
                    break; // 成功
                }
                catch (OperationCanceledException)
                {
                    screen.Running = false;
                    screen.FinishAgentMsg();
                    var cancelled = cts.IsCancellationRequested;
                    screen.AddSystemMsg(cancelled
                        ? "⚠ 已中断"
                        : "⏰ 服务器 60s 未响应");
                    if (!cancelled)
                        screen.SlotStates[screen.ActiveSlotIndex] = SlotState.Error;
                    break;
                }
                catch (Exception ex) when (attempt < modelStack.Length - 1)
                {
                    screen.Running = false;
                    screen.FinishAgentMsg();
                    screen.AddSystemMsg($"  ⚠ {model} 失败: {ex.Message}");
                    // 继续回退链
                }
                catch (Exception ex)
                {
                    screen.Running = false;
                    screen.FinishAgentMsg();
                    screen.AddSystemMsg($"  💔 所有模型均失败: {ex.Message}");
                    screen.SlotStates[screen.ActiveSlotIndex] = SlotState.Error;
                }
            }

            // 完成通知
            if (completed)
            {
                var elapsed = (DateTime.UtcNow - startTime).TotalSeconds;
                screen.AddSystemMsg($"  💡 完成 ({elapsed:F1}s)");
            }

            // 文件修改确认 + 最近文件跟踪
            var modified = EditFileTool.ChangedFiles;
            if (modified.Count > 0)
            {
                screen.AddSystemMsg($"📝 已修改 {modified.Count} 个文件 (/diff 查看 /undo 撤销 /recent 最近)");
                foreach (var f in modified)
                {
                    if (!screen.RecentFiles.Contains(f))
                    {
                        screen.RecentFiles.Add(f);
                        if (screen.RecentFiles.Count > 50) screen.RecentFiles.RemoveAt(0);
                    }
                }
            }

            // 更新右下角 token 显示 + 性能
            screen.UpdateTokenDisplayFull(
                _llm!.TotalPromptTokens, _llm.TotalCompletionTokens,
                _llm.EstimatedCost,
                ContextManager.EstimateTokens(_agent!.Messages), _config.MaxContextTokens,
                _llm.LastLatencyMs, _llm.LastTokensPerSec);
            screen.Render();
        }
        finally
        {
            _agentBusy = false;
            _agentCts = null;
            if (screen.SlotStates[screen.ActiveSlotIndex] != SlotState.Error)
                screen.SlotStates[screen.ActiveSlotIndex] = SlotState.Idle;
            screen.Render();
        }

        // 忙时 Ctrl+C 已由系统级 Ctrl+C 处理（直接强制退出），无需额外确认
    }

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
        screen.AddSystemMsg("快捷键: F1-F10槽位 Shift+Tab切模式 Esc中断 Ctrl+E编辑器 Ctrl+T设置 Ctrl+R搜索 Ctrl+M模型 Ctrl+P提示 Ctrl+B侧栏 Ctrl+H帮助 Ctrl+Q退出 PgUp/PgDn翻页 Ctrl+Home/End首尾 ↑↓历史 Ctrl+V粘贴 Ctrl+Shift+F1/F2主题 · 命令: /help /model /tokens /compact /diff /save /resume /history /sessions");
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
                if (iter == maxIter) break;
                await Task.Delay(1000);
                continue;
            }

            // 检查最近一条 assistant 消息是否含成功标记
            var lastAssistant = _agent.Messages.LastOrDefault(m =>
                m["role"]?.GetValue<string>() == "assistant");
            var lastContent = lastAssistant?["content"]?.GetValue<string>() ?? "";

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

    /// <summary>Ctrl+M 循环切换大模型</summary>
    private static void CycleModel(ChatScreen screen)
    {
        var models = new[] { "deepseek-v4-flash", "deepseek-v4-pro", "gpt-5.4-mini", "gpt-5.4" };
        var cur = _config.Model;
        var idx = Array.IndexOf(models, cur);
        var next = models[(idx + 1) % models.Length];
        _llm!.Model = next;
        _config.Model = next;
        screen.StatusLeft = $"{_config.Model}";
        screen.AddSystemMsg($"🔄 大模型 → {next} (Ctrl+M 继续切换)");
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
            UxHelper.Error("错误", ex.Message);
        }
        }
        finally
        {
            if (needRestore) { TuiManager.Instance.Enter(); TuiManager.Instance.Render(); }
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

    // 辅助方法: 安全的控制台输出 + 状态动画
    // ========================================================================

    /// <summary>转义用户内容中的 [ ] 标记字符</summary>
    private static string E(string? text) => TuiHelper.Esc(text);

    /// <summary>输出带标记的行（转换 Spectre 标记为 ANSI）</summary>
    private static void MarkupLine(string markup) => Console.WriteLine(SpectreToAnsi(markup));

    /// <summary>将类 Spectre 风格标记（使用 «» 符号）转换为 ANSI 转义码（通过 AnsiText 封装层）</summary>
    private static string SpectreToAnsi(string markup)
    {
        return markup
            .Replace("«/»", AnsiTty.SgrReset)
            .Replace("«dim»", AnsiTty.SgrDim)
            .Replace("«bold»", AnsiTty.SgrBold)
            .Replace("«cyan»", AnsiTty.FgCode(TuiColors.Cyan))
            .Replace("«green»", AnsiTty.FgCode(TuiColors.Green))
            .Replace("«yellow»", AnsiTty.FgCode(TuiColors.Yellow))
            .Replace("«red»", AnsiTty.FgCode(TuiColors.Red))
            .Replace("«orange3»", AnsiTty.FgCode(TuiColors.Yellow))
            .Replace("«grey»", AnsiTty.FgCode(TuiColors.Grey))
            .Replace("«bold yellow»", AnsiTty.FgCode(TuiColors.Yellow))
            .Replace("«bold cyan»", AnsiTty.FgCode(TuiColors.Cyan))
            .Replace("«bold red»", AnsiTty.FgCode(TuiColors.Red))
            .Replace("«bold green»", AnsiTty.FgCode(TuiColors.Green))
            .Replace("«bold orange3»", AnsiTty.FgCode(TuiColors.Yellow));
    }

    /// <summary>
    /// 带旋转动画 + 超时提示的 ChatAsync 包装器。
    /// 等待 LLM 时显示 "⠋ 思考中..." 旋转动画，网络卡顿无响应时有进度提示。
    /// </summary>
    private static async Task<string> ChatWithStatusAsync(
        string userInput,
        CancellationToken ct,
        Action<bool>? setStreamed = null)
    {
        // ANSI 控制序列（通过 AnsiText 封装层）
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

                    // 清行 + 回行首 + 动画帧（直接写 stdout）
                    Console.Write($"\r{AnsiTty.ClearToEnd}  {AnsiTty.SgrDim}{status}");
                    await Console.Out.FlushAsync(token);
                    i++;
                    try
                    {
                        await Task.Delay(120, token);
                    }
                    catch (OperationCanceledException)
                    {
                        break;
                    }
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
                MarkupLine($"  «dim»⚙ {E(name)}({E(shortBrief)})«/»");
            },
            onToolOutput: line =>
            {
                // 管道模式：逐行输出 bash 结果到控制台
                Console.WriteLine($"  «dim»│ {E(line)}«/»");
            },
            cancellationToken: ct);

        // 清除最后一轮动画
        StopSpinner();

        return response;
    }
}