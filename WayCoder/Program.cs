using System.Text;
using WayCoder.Tools;
using WayCoder.UI.Shared;
using WayCoder.UI.Tui;
using WayCoder.UI.Shared.Terminal;
using WayCoder.UI.TUI.Base;
using WayCoder.UI.Tui.Screens;
using WayCoder.UI.Web;
using Arguments = WayCoder.UI.Cli.Arguments;

namespace WayCoder;

/// <summary>
/// 入口 + CLI + REPL —— 面向用户的终端界面。
/// </summary>
public partial class Program
{
    private static Config _config = new();

    /// <summary>--tui-chat 参数置位：强制用 .tui 标记版聊天界面（等价 WAYCODER_MARKUP_UI=1）</summary>
    public static bool MarkupChatOverride;

    private static LLM? _llm;
    private static Agent? _agent;
    private static readonly AgentSlot[] _slots = new AgentSlot[AgentSlot.Count];
    private static volatile int _activeSlot; // 当前活跃槽位索引（F1 对应 0）。volatile：主线程写、后台槽位/命令线程读，保证可见性

    /// <summary>待投递的槽位任务队列：槽位索引 → 任务列表（同一槽位多次 -pN 可排队）</summary>
    private static readonly Dictionary<int, List<string>> _pendingSlotQueues = [];

    /// <summary>所有槽位任务的共享前缀（-pa 传入）</summary>
    private static string _pendingSlotPrefix = "";

    /// <summary>
    /// `--json` / `--output-format json`：stdout 只允许出现 <see cref="JsonResult"/> 对象（IDE 桥接契约）。
    ///
    /// 存成静态字段而非只在 Main 里当局部变量：命令行槽位任务（`-p1`~`-p0`）走的是 `RunReplAsync`
    /// 那条路（此时 `prompt == null`），Main 里那个 `if (jsonMode) return await RunOnceJsonAsync(prompt)`
    /// 覆盖不到 —— 判不出来的话 `waycoder --json -p1 "修复 bug" | jq -r .answer` 会拿到
    /// 不可解析的 ANSI 文本外加退出码 0。
    /// </summary>
    private static bool _jsonMode;

    /// <summary>当前活跃槽位索引（供外部命令访问）</summary>
    public static int ActiveSlotIndex => _activeSlot;

    /// <summary>所有槽位数组（供外部命令访问）</summary>
    public static AgentSlot[] GetSlots() => _slots;

    /// <summary>
    /// 刷新活跃槽位 Agent 的工具集与系统提示词（模式/权限切换后调用）。
    /// 供 /mode、/permit、Ctrl+P 等入口统一调用，修 P0-1「其余入口不刷新工具集」。
    /// </summary>
    public static void RefreshActiveSlotTools()
    {
        var slots = _slots;
        if (slots == null) return;
        if (_activeSlot < 0 || _activeSlot >= slots.Length) return;
        slots[_activeSlot].Agent?.ReapplyToolFilter();
    }

    /// <summary>
    /// 应用 --permission-mode（Claude Code 对齐）：plan→行为轴 Plan，
    /// acceptEdits→边界轴 auto-edit，bypassPermissions→full-auto。未知值返回 false。
    /// </summary>
    internal static bool ApplyPermissionMode(string mode)
    {
        switch (mode.Trim().ToLowerInvariant())
        {
            case "plan":
                WorkModeManager.SetMode(WorkMode.Plan);
                return true;
            case "acceptedits":
                PermissionManager.SetMode("auto"); // 确认轴：自动接受编辑
                return true;
            case "bypasspermissions":
                PermissionManager.CurrentMode = PermissionManager.Mode.Yolo; // 确认轴：不打断（边界独立，不动沙箱）
                return true;
            case "default":
                return true;
            default:
                Console.WriteLine($"⚠ 未知 --permission-mode: {mode}（支持 default / acceptEdits / plan / bypassPermissions）");
                return false;
        }
    }

    private static WatchMode? _watchMode;
    private static volatile bool _exitRequested;

    /// <summary>请求退出（/exit、/quit 斜杠命令调用）：设退出标志，REPL 主循环走正常清理路径退出。</summary>
    public static void RequestExit() => _exitRequested = true;
    private static readonly Task?[] _slotTasks = new Task?[AgentSlot.Count];
    private static (List<JNode> Messages, string Model)? _pendingRestore;

    /// <summary>待恢复的自动保存会话（/resume 用；TryRestoreSession 启动时填充）。</summary>
    public static (List<JNode> Messages, string Model)? PendingRestore => _pendingRestore;

    /// <summary>恢复后清空待恢复会话。</summary>
    public static void ClearPendingRestore() => _pendingRestore = null;

    /// <summary>一次性/管道模式 POSIX 信号注册（保持引用防 GC 回收，Windows 下为 null）。</summary>
    private static System.Runtime.InteropServices.PosixSignalRegistration? _sigintReg;
    private static System.Runtime.InteropServices.PosixSignalRegistration? _sigtermReg;
    // 非 Windows：Ctrl+Z 默认发 SIGTSTP（终端会挂起整个进程）——注册处理把「优雅暂停」落到信号上，
    // 同时 ctx.Cancel=true 取消默认挂起。Windows 无此信号，Ctrl+Z 仍走 ReadKey 键路径。
    private static System.Runtime.InteropServices.PosixSignalRegistration? _sigtstpReg;

    /// <summary>各槽位当前会话 ID（用于 SessionPicker 标记），按槽位隔离</summary>
    private static readonly string[] _currentSessionIds = InitCurrentSessionIds();

    private static string[] InitCurrentSessionIds()
    {
        var arr = new string[AgentSlot.Count];
        for (int i = 0; i < arr.Length; i++) arr[i] = "_auto";
        return arr;
    }

    /// <summary>Watch 模式线程安全提示队列</summary>
    private static readonly System.Collections.Concurrent.ConcurrentQueue<string> _pendingWatchPrompts = new();

    public static async Task<int> Main(string[] args)
    {
        Console.OutputEncoding = Encoding.UTF8;

        // 错误日志系统（自动追踪所有错误，写入 logs/error_YYYYMMDD.log）
        ErrorLog.Initialize(catchAllExceptions: true);

        // 全局异常处理：恢复终端 + 保存会话 + ErrorLog
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

        // 退出时清理孤儿进程：持久 shell 会话 / 后台任务进程（避免 cmd/bash 及其内长命令残留）
        AppDomain.CurrentDomain.ProcessExit += (_, _) =>
        {
            try { PersistentShellManager.ShutdownAll(); } catch { }
            try { BackgroundTaskManager.ShutdownAll(); } catch { }
        };

        // 注册 + 解析 CLI 参数（重复名称自动报错）
        Arguments.BuiltinArgs.RegisterAll();
        var (parsed, exitCode) = Arguments.CliArgRegistry.Parse(args);
        if (exitCode.HasValue) return exitCode.Value;

        // --no-color / -q 提前生效：ShowUsage（-h）等输出早于 _config 初始化，须在此设置
        if (Arguments.CliArgRegistry.Has(parsed, "no-color"))
            WayCoder.UI.Shared.Terminal.AnsiTty.Enabled = false;
        Config.Instance.QuietMode = Arguments.CliArgRegistry.Has(parsed, "quiet");

        // --permit <tiny/chat/ack/auto/smart/yolo>：启动权限模式（问答ACK/自动AUTO/智能SMART/畅通YOLO）；
        // tiny/chat 是纯聊天别名 → 切工作模式 Chat（0 工具 0 提示词），而非落到权限枚举
        if (Arguments.CliArgRegistry.Get(parsed, "permit") is string permitMode)
        {
            if (PermissionManager.IsChatModeAlias(permitMode))
                WorkModeManager.SetMode(WorkMode.Chat);
            else
                PermissionManager.SetMode(permitMode);
        }

        // --mode <build|plan|chat>：启动工作模式（行为轴）。在 --permit 之后处理，显式指定时覆盖聊天别名。
        // 与 /mode 斜杠命令同源解析；主/一次性 Agent 在 Main 后续按全局 CurrentMode 同步（_agent.WorkMode = ...）。
        if (Arguments.CliArgRegistry.Get(parsed, "mode") is string modeName)
        {
            var target = modeName.ToLowerInvariant() switch
            {
                "build" or "建造" or "b" => WorkMode.Build,
                "plan" or "计划" or "p" => WorkMode.Plan,
                "chat" or "聊天" or "c" => WorkMode.Chat,
                _ => (WorkMode?)null,
            };
            if (target != null)
                WorkModeManager.SetMode(target.Value);
            else
                Console.Error.WriteLine($"⚠ 未知工作模式 '{modeName}'（可用: build|plan|chat）。");
        }

        // 读取值参数
        string? model = Arguments.CliArgRegistry.Get(parsed, "model");
        string? baseUrl = Arguments.CliArgRegistry.Get(parsed, "base-url");
        string? apiKey = Arguments.CliArgRegistry.Get(parsed, "api-key");
        string? prompt = Arguments.CliArgRegistry.Get(parsed, "prompt");
        string? resumeId = Arguments.CliArgRegistry.Get(parsed, "resume")
            ?? Arguments.CliArgRegistry.Get(parsed, "session"); // --session <id>（OpenCode/Claude Code）等同 --resume <id>
        string? editFile = Arguments.CliArgRegistry.Get(parsed, "edit");

        // 共享前缀（-pa "前缀" → 拼到每个 -pN 任务前面）
        _pendingSlotPrefix = Arguments.CliArgRegistry.Get(parsed, "prompt-all") ?? "";

        // 收集槽位专项任务队列（-p1 ~ -p9, -p0=F10，同一槽位多次可排队）
        for (int n = 0; n <= 9; n++)
        {
            var values = Arguments.CliArgRegistry.GetAll(parsed, $"slot-prompt-{n}");
            if (values == null || values.Count == 0) continue;

            var slotIdx = n == 0 ? 9 : n - 1; // -p1→0, -p2→1, ..., -p0→9
            if (!_pendingSlotQueues.ContainsKey(slotIdx))
                _pendingSlotQueues[slotIdx] = [];

            foreach (var v in values)
            {
                if (!string.IsNullOrWhiteSpace(v))
                    _pendingSlotQueues[slotIdx].Add(v);
            }
        }

        // -p1~-p0 槽位任务 → 强制进入 REPL 交互模式（而非一次性模式）
        if (_pendingSlotQueues.Count > 0 && prompt == null)
            prompt = null; // 保持 null，走 REPL 分支
        double? maxBudget = null;
        var budgetStr = Arguments.CliArgRegistry.Get(parsed, "max-budget-usd");
        if (budgetStr != null && double.TryParse(budgetStr, System.Globalization.NumberStyles.Float,
                System.Globalization.CultureInfo.InvariantCulture, out var b)) maxBudget = b;
        var requeueStr = Arguments.CliArgRegistry.Get(parsed, "max-requeue");
        int? maxRequeue = null;
        if (requeueStr != null && int.TryParse(requeueStr, out var rq))
            maxRequeue = Math.Clamp(rq, 0, 20);

        bool yoloMode = Arguments.CliArgRegistry.Has(parsed, "yolo");
        bool watchMode = Arguments.CliArgRegistry.Has(parsed, "watch");
        bool tinyMode = Arguments.CliArgRegistry.Has(parsed, "tiny");
        string? tinyWindowSpec = Arguments.CliArgRegistry.Get(parsed, "tiny");
        bool economyMode = Arguments.CliArgRegistry.Has(parsed, "economy");
        string? economySpec = Arguments.CliArgRegistry.Get(parsed, "economy");
        // --output-format（Claude Code）/ --format（OpenCode）：json|stream-json 等同 --json
        var outFormat = Arguments.CliArgRegistry.Get(parsed, "output-format");
        bool jsonMode = _jsonMode = Arguments.CliArgRegistry.Has(parsed, "json") || outFormat is "json" or "stream-json";

        // --permission-mode bypassPermissions（Claude Code）→ yolo
        string? permissionMode = Arguments.CliArgRegistry.Get(parsed, "permission-mode");
        if (string.Equals(permissionMode, "bypassPermissions", StringComparison.OrdinalIgnoreCase))
            yoloMode = true;
        bool webMode = Arguments.CliArgRegistry.Has(parsed, "web");
        string? webPortSpec = Arguments.CliArgRegistry.Get(parsed, "web");
        bool tuiMode = Arguments.CliArgRegistry.Has(parsed, "tui");
        bool cliMode = Arguments.CliArgRegistry.Has(parsed, "cli");

        if (Arguments.CliArgRegistry.Has(parsed, "version"))
        {
            // -v/--version：单行固定格式，方便其他软件正则抓取版本号
            Console.WriteLine($"{Global.AppName} ({Global.AppNameCN}) 版本:{Global.Version.TrimStart('v', 'V')}");
            return 0;
        }

        if (Arguments.CliArgRegistry.Has(parsed, "session-list"))
        {
            ShowSessionList();
            return 0;
        }

        if (Arguments.CliArgRegistry.Has(parsed, "help"))
        {
            ShowUsage();
            return 0;
        }

        // 一次性自动升级（--update）：检查并自替换，幂等（已最新则提示后退出）
        if (Arguments.CliArgRegistry.Has(parsed, "update"))
        {
            if (!Config.Instance.UpdateEnabled)
            {
                Console.WriteLine("🔒 更新已禁用（内网/离线模式）。设置 WAYCODER_UPDATE_ENABLED=true 或 /config 打开「更新开关」后重试。");
                return 0;
            }
            var updateResult = await UpdateChecker.SelfUpdateAsync();
            Console.WriteLine(updateResult);
            return updateResult.StartsWith("✅", StringComparison.Ordinal) ? 0 : 1;
        }

        // 项目初始化向导
        if (Arguments.CliArgRegistry.Has(parsed, "init"))
        {
            RunInit();
            return 0;
        }

        // 标准输入管道模式：echo "prompt" | waycoder（槽位任务优先，不抢 stdin）
        if (prompt == null && _pendingSlotQueues.Count == 0 && Console.IsInputRedirected)
        {
            var stdinText = ReadRedirectedPrompt();
            // 只有当 stdin 真正有内容时才作为管道输入
            if (!string.IsNullOrEmpty(stdinText))
            {
                prompt = stdinText;
                // 管道输入非交互，自动开启 yolo 模式
                if (!yoloMode) yoloMode = true;
            }
        }

        // -p 一次性模式：非交互，自动放行权限
        // -p1~-p0 槽位任务也自动放行
        if (prompt != null || _pendingSlotQueues.Count > 0)
            yoloMode = true;

        _config = Config.FromEnv();

        // ── 死机现场采集：注入当前状态快照提供者（FreezeCapture 从心跳线程/看门狗读取） ──
        // 冻结时主线程可能正持锁 → 锁获取一律 Monitor.TryEnter + 短超时，失败标「⚠ 主线程持锁」。
        FreezeCapture.LiveStateProvider = () =>
        {
            var s = new FreezeCapture.LiveState();
            try
            {
                s.ActiveSlot = _activeSlot;
                var agent = _agent;
                if (agent != null)
                {
                    s.ActiveAgentId = agent.AgentId;
                    s.WorkMode = agent.WorkMode.ToString();
                    s.Model = agent.LlmClient.EffectiveModel;
                    s.SmallModel = agent.LlmClient.SmallModel;
                    s.Round = agent.CurrentRound;
                    s.CurrentTool = agent.CurrentToolName;
                    s.CurrentToolBrief = agent.CurrentToolBrief;
                    s.TotalRequests = agent.LlmClient.TotalRequests;
                    s.TotalPromptTokens = agent.LlmClient.TotalPromptTokens;
                    s.TotalCompletionTokens = agent.LlmClient.TotalCompletionTokens;
                    s.TaskCost = agent.LlmClient.TaskCost ?? 0;
                    s.LastLatencyMs = agent.LlmClient.LastLatencyMs;
                    s.MaxTokens = agent.Context.MaxTokens;
                    s.LastPromptTokens = agent.Context.LastPromptTokens;
                    s.IsCompressing = ContextManager.IsCompressing;

                    // 消息数：TryEnter 防主线程持锁卡死采集线程
                    if (Monitor.TryEnter(agent.MessagesLock, 50))
                    {
                        try { s.MessageCount = agent.MessageCount; }
                        finally { Monitor.Exit(agent.MessagesLock); }
                    }
                }
                for (int i = 0; i < _slots.Length; i++)
                {
                    if (_slots[i] == null) continue;
                    s.SlotBusy[i] = _slots[i].IsBusy;
                    s.SlotAgentIds[i] = _slots[i].Agent?.AgentId ?? "";
                }
            }
            catch { /* 采集路径绝不抛，缺字段就用默认值 */ }
            return s;
        };

        if (MarkupChatOverride) _config.MarkupUi = true; // --tui-chat 强制走标记版界面
        // 加载主题配色：theme.json 记住的 preset 优先，回退 .env ThemePreset（首次启动无 theme.json）
        ThemeConfig.ApplyPreset(ThemeConfig.Instance.PresetKey ?? _config.ThemePreset);

        // ── 竞品 / 增强参数消费 ──
        if (Arguments.CliArgRegistry.Get(parsed, "max-turns") is string mtStr && int.TryParse(mtStr, out var maxTurns))
            _config.MaxRounds = Math.Clamp(maxTurns, 5, 500);
        if (Arguments.CliArgRegistry.Has(parsed, "auto-commit"))
        {
            var ac = Arguments.CliArgRegistry.Get(parsed, "auto-commit");
            _config.AutoGitCommit = ac == null
                || ac.Equals("on", StringComparison.OrdinalIgnoreCase)
                || ac.Equals("true", StringComparison.OrdinalIgnoreCase);
        }
        if (Arguments.CliArgRegistry.Get(parsed, "mcp-config") is string mcpCfgPath)
            WayCoder.Tools.McpManager.ConfigPathOverride = mcpCfgPath;
        if (Arguments.CliArgRegistry.Get(parsed, "theme") is string themeName)
            Config.ApplyColorScheme(_config, themeName);
        // ── --model / --base-url / --api-key：本次启动「强制连接」三元组 ──
        // 语义：给了哪几项就覆盖哪几项，**只作用于本次进程**，绝不改用户配置。
        // 需要一个不提权的临时连接（换套 baseUrl/key/model 试一下）就用这三个参数；
        // 要**永久**保存请用 /model、/connect、/provider —— 那些才是"改配置"的入口。
        //
        // 为什么要 PersistDisabled：--model 会走 ApplyModelChoice → SetActiveConnect，
        // 那条路默认会写出 connections.json + config.json + .env 三个文件（见 SetActiveConnect 尾部），
        // 与 --model 自身「本次会话，不持久化」的说明矛盾，也让一次命令行启动变成不可逆的配置变更。
        // 置位后内存状态照常更新（运行时镜像一致，压缩用的小模型/回退链读到的都是新连接），只是不落盘。
        //
        // 顺序依赖：base-url 必须在 ApplyModelChoice 之后落到 _config 上，否则会被 connect 推导出的
        // 地址覆盖。这里靠"先 ApplyModelChoice、后显式赋值"保证，别再把这几个赋值散到别处。
        if (model != null || baseUrl != null || apiKey != null)
        {
            var savedPersistDisabled = Global.PersistDisabled;
            Global.PersistDisabled = true;
            try
            {
                if (model != null)
                {
                    var catInfo = ModelCatalog.Find(model);
                    ConnectionConfig.ApplyModelChoice(catInfo?.ProviderId ?? _config.Provider, model,
                        isLarge: true, out _, catInfo?.DefaultBaseUrl);
                }
                if (baseUrl != null) _config.BaseUrl = baseUrl;
                if (apiKey != null) _config.ApiKey = apiKey; // 仅本次会话；不写 api_keys.json
            }
            finally { Global.PersistDisabled = savedPersistDisabled; }
        }
        if (maxBudget != null) _config.MaxBudgetUsd = maxBudget;
        if (maxRequeue != null) _config.MaxAutoRequeue = maxRequeue.Value;
        if (watchMode) _config.WatchMode = true;

        // 竞品参数对齐：工具白/黑名单（Claude Code --allowedTools / --disallowedTools）、
        // 系统提示词追加（--system-prompt / --append-system-prompt）。均在 agent 创建前设置。
        var cliAllowed = Arguments.CliArgRegistry.GetAll(parsed, "allowed-tools");
        if (cliAllowed != null)
            _config.AllowedTools = string.Join(",", cliAllowed);
        var cliDisabled = Arguments.CliArgRegistry.GetAll(parsed, "disallowed-tools");
        if (cliDisabled != null)
            _config.DisabledTools = string.Join(",", cliDisabled);
        var cliSysPrompt = Arguments.CliArgRegistry.Get(parsed, "system-prompt");
        if (cliSysPrompt != null)
            _config.ExtraSystemPrompt = cliSysPrompt;
        if (economyMode)
            _config.EconomyMode = (economySpec?.ToLowerInvariant()) switch
            {
                "auto" => EconomyMode.Auto,
                "off" => EconomyMode.Off,
                "extreme" => EconomyMode.Extreme,
                _ => EconomyMode.On,
            };

        // --permission-mode plan/acceptEdits/bypassPermissions（Claude Code 对齐）。
        // default 不覆盖 config.json 中已持久化的边界轴级别，其余模式在 agent 创建前应用。
        bool permissionModeApplied = false;
        if (!string.IsNullOrWhiteSpace(permissionMode)
            && !permissionMode.Equals("default", StringComparison.OrdinalIgnoreCase))
        {
            permissionModeApplied = ApplyPermissionMode(permissionMode);
        }

        // 从模型目录自动设置 base URL（两层架构：provider 唯一地址优先，模型默认地址兜底）
        if (_config.BaseUrl == null)
        {
            var catInfo = ModelCatalog.Find(_config.Model);
            var catBaseUrl = catInfo != null
                && ModelCatalog.Providers.TryGetValue(catInfo.ProviderId, out var cp)
                && !string.IsNullOrEmpty(cp.DefaultBaseUrl)
                ? cp.DefaultBaseUrl : catInfo?.DefaultBaseUrl;
            if (catBaseUrl != null)
                _config.BaseUrl = catBaseUrl;
        }

        // Tiny 模式：仅显式 --tiny 启用（压测 / 本地小模型省 token），不再按模型窗口自动进入
        if (tinyMode)
        {
            _config.TinyMode = true;
            _config.TinyWindow = ModelCatalog.ResolveTinyWindow(tinyWindowSpec, _config.Model, _config.BaseUrl);
        }

        // Local/Ollama 模型不需要 API key
        var isLocalModel = _config.Model.Contains("ollama", StringComparison.OrdinalIgnoreCase)
            || (_config.BaseUrl?.Contains("localhost") == true)
            || (_config.BaseUrl?.Contains("127.0.0.1") == true);

        if (string.IsNullOrEmpty(_config.ApiKey) && !isLocalModel)
        {
            // 首次无 API key 也允许启动——不退出。进入软件后经 /model（ModelPicker 选模型提示输入 key）、
            // /provider（设Key/清Key）、或 /model keys set <供应商> <key> 在软件内设置，无需先退出配环境变量。
            // 仅打印一行温和提示（不阻塞、不进红框），引导到可设置处。
            MarkupLine("«bold yellow»⚠ 当前未设置 API Key：已直接启动，可用 /model（选择模型时输入）、/provider（设Key）或 /model keys set <供应商> <key> 补设«/»");
            Console.WriteLine();
        }

        // 批量任务引擎：多仓库并行处理（每个任务在独立克隆副本中隔离执行，无需构建 LLM/Agent）
        if (HasBatch(parsed))
            return await RunBatchAsync(parsed);

        _llm = new LLM(_config.Model, _config.ApiKey, _config.BaseUrl,
            _config.MaxTokens, _config.Temperature);

        // 语义记忆：向量嵌入初始化
        EmbeddingStore.LlmClient = _llm;
        EmbeddingStore.Enabled = _config.EmbeddingEnabled;
        EmbeddingStore.EmbeddingModel = _config.EmbeddingModel;

        _agent = new Agent(_llm, maxContextTokens: ModelCatalog.ResolveContextWindow(_config.Model, _config.MaxContextTokens),
            maxBudgetUsd: _config.MaxBudgetUsd, autoCommit: _config.AutoGitCommit);
        _agent.AgentId = "F1"; // 主 Agent = 槽位 1，供文件锁跨槽位冲突检测
        ProgramContext.Agent = _agent;
        ProgramContext.LLM = _llm; // 修复全局 LLM 未挂载的遗漏（/tokens /stats /init 等命令取用）
        _slots[0] = new AgentSlot { Agent = _agent }; // 槽位 0 持有主 Agent

        // 主/一次性 Agent 沿用全局工作模式（--permit tiny/chat → Chat 等）；非 Build 时刷新工具集与提示词
        _agent.WorkMode = WorkModeManager.CurrentMode;
        if (_agent.WorkMode != WorkMode.Build)
            _agent.ReapplyToolFilter();

        // 边界轴：始终从配置初始化沙箱模式（独立于权限轴；--permission-mode 已设边界时不覆盖）
        if (!permissionModeApplied)
            SandboxManager.Mode = _config.SandboxMode;
        // 确认轴：--yolo / -p / 管道输入 非交互跳过确认（只改权限，不动边界）
        if (yoloMode)
            PermissionManager.CurrentMode = PermissionManager.Mode.Yolo;

        // 同步 PromptCache 设置
        PromptCache.Enabled = _config.PromptCaching;

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
            ErrorLog.Warning("Program", $"团队记忆自动同步失败: {ex.Message}", ex);
            }
        }

        // 加载自定义斜杠命令、hooks、MCP 服务器和检查点
        CustomCommands.Load();
        SlashCommandRegistry.RegisterAll();
        HooksManager.Init();
        HooksManager.RunSessionStart("startup");
        McpManager.Init();
        CheckpointManager.LoadFromDisk();
        CheckpointManager.AutoCheckpoint = Config.Instance.AutoCheckpoint;

        // 恢复会话（-c/--continue/--resume/--session）
        var hasResumeFlag = Arguments.CliArgRegistry.Has(parsed, "resume")
            || Arguments.CliArgRegistry.Has(parsed, "session");
        if (hasResumeFlag)
        {
            // 无参数时：优先 _auto，回退最新会话（一次性模式 = 主槽位 0，旧版本存全局则回退）
            if (string.IsNullOrEmpty(resumeId))
            {
                var autoLoaded = SessionManager.LoadSession("_auto", 0)
                                 ?? SessionManager.LoadSession("_auto");
                if (autoLoaded != null)
                {
                    resumeId = "_auto";
                }
                else
                {
                    // 找最新保存的会话
                    var sessions = SessionManager.ListSessions(1, 0, 0);
                    if (sessions.Count == 0)
                        sessions = SessionManager.ListSessions(1);
                    if (sessions.Count > 0)
                        resumeId = sessions[0].Id;
                }

                if (resumeId == null)
                {
                    MarkupLine("«yellow»⚠ 没有找到可恢复的会话«/»");
                    return 1;
                }
            }

            HooksManager.RunSessionStart("resume");
            var loaded = SessionManager.LoadSession(resumeId, 0)
                         ?? SessionManager.LoadSession(resumeId); // 旧版本存全局，回退
            if (loaded != null)
            {
                _agent.ReplaceMessages(loaded.Value.Messages);
                if (model == null && !string.IsNullOrEmpty(loaded.Value.Model))
                {
                    _llm.Model = loaded.Value.Model;
                    _config.Model = loaded.Value.Model;
                    // 会话模型只做内存镜像（不落盘）；标记使后续任何配置保存的 Reconcile 跳过它，
                    // 避免把会话模型误收敛为 state 默认（Phase 2 确认：不污染持久配置）
                    _config.SessionModelMirror = true;
                }

                MarkupLine($"«green»✔ 已恢复会话:«/» «cyan»{E(resumeId)}«/» «dim»({loaded.Value.Messages.Count} 条消息, 模型: {E(_llm.Model)})«/»");
            }
            else
            {
                MarkupLine($"«red»✘ 会话 '{E(resumeId)}' 未找到«/»");
                MarkupLine("«dim»可用 /sessions 命令查看所有已保存会话«/»");
                return 1;
            }
        }

        // 浏览器聊天界面（--web [端口]，默认 9527）
        if (webMode)
        {
            int webPort = 9527;
            var portFromEnv = Environment.GetEnvironmentVariable("WAYCODER_WEB_PORT");
            if (!string.IsNullOrEmpty(webPortSpec) && int.TryParse(webPortSpec, out var wp) && wp > 0 && wp < 65536)
                webPort = wp;
            else if (!string.IsNullOrEmpty(portFromEnv) && int.TryParse(portFromEnv, out var we) && we > 0 && we < 65536)
                webPort = we;
            await RunWebAsync(webPort);
            return 0;
        }

        // CLI 文本界面（--cli，非全屏逐行交互；--tui 显式时优先默认 TUI）
        if (cliMode && !tuiMode)
        {
            await RunCliReplAsync(editFile);
            return 0;
        }

        // --tui 显式且带 -p：提示词投递到槽位 0，走全屏 REPL（复用槽位自动投递机制），
        // 而非 RunOnceAsync 一次性 spinner 模式——便于观察任务完成阶段的 TUI 渲染/死机。
        if (tuiMode && !jsonMode && !string.IsNullOrEmpty(prompt))
        {
            if (!_pendingSlotQueues.TryGetValue(0, out var list))
                _pendingSlotQueues[0] = list = [];
            list.Add(prompt);
            prompt = null; // 交给 REPL 自动投递
        }

        if (!string.IsNullOrEmpty(prompt))
        {
            if (jsonMode)
                return await RunOnceJsonAsync(prompt);
            await RunOnceAsync(prompt);
        }
        else
            return await RunReplAsync(editFile); // 界面起不来（无控制台）时透传非 0 退出码

        return 0;
    }

    // ========================================================================
    // 一次性模式
    // ========================================================================

    /// <summary>
    /// 首字节等待上限：管道已建但迟迟没有内容时，超过这个时间就放弃「它在喂提示词」的判断。
    ///
    /// 取值是在两种错误之间取衡：**短了**会把写得慢的真提示词当噪声丢掉（任务不执行），
    /// **长了**会让「启动器递过来一个不写不关的管道」这种环境白等。丢任务更糟，所以偏长；
    /// 而且放弃时不再静默（见下方 stderr 提示），用户至少知道发生了什么。
    /// 常见情况（`< NUL`、`< 空文件`、`: | waycoder`）走的是 EOF 那条路，**立即返回，不等**。
    /// </summary>
    private const int StdinFirstByteTimeoutMs = 5000;

    /// <summary>
    /// 读被重定向的 stdin 作为一次性提示词。
    ///
    /// 不能直接 `Console.In.ReadToEnd()`：它要等 EOF，而「被别的程序拉起」恰恰是父进程给了管道
    /// 却既不写也不关（Node/Electron `spawn` 的默认 stdio、IDE 集成、双击启动器）——那样会永远
    /// 卡在这一行：没有窗口、没有任何提示、也没有退出码，比「静默退出 0」更难查。
    ///
    /// 判据是「**读到第一个字节** 或 **读到 EOF**，谁先到算谁」：
    /// - 先到 EOF（空管道 / 立刻关闭的管道）→ 立即返回空串，不白等一个时限；
    /// - 先到首字节 → 确认在用管道喂提示词，**此后不再设限**，一路阻塞到 EOF ——
    ///   慢生产方（分批 echo、`curl` 慢、脚本 sleep 后才写）的内容不会被截断；
    /// - 两者都没到 → 认定这不是喂提示词的管道，放弃并**在 stderr 说明**（不静默）。
    ///
    /// 时限**无条件**生效，不看「有没有界面可回退」：Unix 上 stdin 被重定向时
    /// `CanUseFullScreen()` 恒 false（`/dev/tty` 没进 raw mode），若因「回退不了就死等」跳过时限，
    /// 就正好保留了这次要消灭的那个 Unix 挂死。放弃后由调用方走它自己的路：有画布→开界面，
    /// 没画布→报错 + 退出码 1。
    ///
    /// 实现上把「读」放到后台线程、时限加在**等待侧**：`Console.OpenStandardInput()` 拿到的不是
    /// overlapped 句柄，`ReadAsync(..., token)` 取消不了已经发出的同步读（token 只在读之前被检查），
    /// 写在读那一侧的超时根本不会到点。读线程是后台线程，放弃后它阻塞着既不挡进程退出、
    /// 也不会偷偷把内容塞给谁。两个事件**故意不 Dispose**：放弃后那条线程可能仍在跑，
    /// 对已释放的 `ManualResetEventSlim` 调 `Set()` 会抛，而异常出在线程的 finally 里
    /// 就是未捕获异常 → 进程直接挂（比它要解决的问题更严重）。
    /// </summary>
    private static string ReadRedirectedPrompt()
    {
        var firstByte = new ManualResetEventSlim(false);
        var finished = new ManualResetEventSlim(false);
        string text = "";
        var reader = new Thread(() =>
        {
            try { text = ReadAllStdin(firstByte); }
            catch { /* 读失败当作没有提示词 */ }
            try { finished.Set(); } catch { /* 事件不可用不影响主线程已返回的结果 */ }
        }) { IsBackground = true, Name = "waycoder-stdin-prompt" };
        reader.Start();

        int signaled = WaitHandle.WaitAny(
            [firstByte.WaitHandle, finished.WaitHandle], StdinFirstByteTimeoutMs);

        if (signaled == WaitHandle.WaitTimeout)
        {
            Console.Error.WriteLine(
                $"⚠ 标准输入是管道，但 {StdinFirstByteTimeoutMs / 1000} 秒内既没有内容也没有结束，按「没有提示词」继续。");
            Console.Error.WriteLine("  若确实要用管道喂提示词，请改用 -p \"任务\" 显式指定（或先让生产方写出内容）。");
            return "";
        }

        finished.Wait(); // 已确认有内容（或已 EOF）→ 等它读干净（已有内容时不再设限）
        return text;
    }

    /// <summary>把重定向的 stdin 读干净（阻塞到 EOF）。读到第一个字节时置 <paramref name="firstByte"/>。
    /// 按 `Console.InputEncoding` + BOM 探测解码，与原来 `Console.In`（StreamReader 同一套参数）一致，
    /// 免得中文 / UTF-16 管道输入的解读方式被这次改动改变。</summary>
    private static string ReadAllStdin(ManualResetEventSlim firstByte)
    {
        var stdin = Console.OpenStandardInput();
        using var buf = new MemoryStream();
        var chunk = new byte[8192];
        while (true)
        {
            int n;
            try { n = stdin.Read(chunk, 0, chunk.Length); }
            catch (IOException) { break; } // 管道异常（写入方消失等）
            if (n <= 0) break;             // EOF：写入方关闭，提示词结束
            buf.Write(chunk, 0, n);
            firstByte.Set();
        }

        using var sr = new StreamReader(new MemoryStream(buf.ToArray()),
            Console.InputEncoding, detectEncodingFromByteOrderMarks: true);
        return sr.ReadToEnd().Trim();
    }

    /// <summary>
    /// 一次性/管道模式下注册 SIGINT/SIGTERM 处理：中断时先保存会话再退出，
    /// 保证断点续传（auto.json）在管道模式下依然生效（Windows 无 POSIX 信号，跳过）。
    /// 引用保存在静态字段，防止被 GC 回收导致信号处理失效。
    /// </summary>
    private static void RegisterOnceModeSignalHandlers()
    {
        if (OperatingSystem.IsWindows()) return;
        try
        {
            _sigintReg = System.Runtime.InteropServices.PosixSignalRegistration.Create(
                System.Runtime.InteropServices.PosixSignal.SIGINT, ctx =>
                {
                    ctx.Cancel = true;
                    AutoSaveSession();
                    Environment.Exit(130);
                });
            _sigtermReg = System.Runtime.InteropServices.PosixSignalRegistration.Create(
                System.Runtime.InteropServices.PosixSignal.SIGTERM, ctx =>
                {
                    ctx.Cancel = true;
                    AutoSaveSession();
                    Environment.Exit(143);
                });
        }
        catch
        {
            // 某些平台不支持 POSIX 信号注册，静默跳过（回退到默认终止行为）
        }
    }

    private static async Task RunOnceAsync(string prompt)
    {
        using var cts = new CancellationTokenSource();
        RegisterOnceModeSignalHandlers();

        try
        {
            MarkupLine($"«dim»🤖 {E(prompt)}«/»");
            await ChatWithStatusAsync(prompt, cts.Token);
            Console.WriteLine();
            AutoSaveSession();
        }
        catch (OperationCanceledException)
        {
            if (cts.IsCancellationRequested)
            {
                MarkupLine("\n«orange3»⚠ 已中断«/»");
                AutoSaveSession();
                Environment.Exit(130);
            }
            else
            {
                ErrorLog.Error("Program.RunOnce", $"LLM 请求超时（{Config.Instance.LlmHttpTimeoutSec}s）");
                UxHelper.Error("请求超时", $"服务器 {Config.Instance.LlmHttpTimeoutSec}s 未响应，请检查网络或 API 配置");
                AutoSaveSession();
                Environment.Exit(1);
            }
        }
        catch (Exception ex)
        {
            ErrorLog.Fatal("Program.RunOnce", $"一次性模式崩溃: {ex.Message}", ex);
            UxHelper.Error("错误", ex.Message);
            AutoSaveSession();
            Environment.Exit(1);
        }
    }

    // ========================================================================
    // 浏览器聊天界面（--web）
    // ========================================================================

    private static async Task RunWebAsync(int port)
    {
        // web 无终端权限弹框 → 强制 yolo（服务仅绑定 127.0.0.1，风险可控）
        // 边界轴：web 模式边界也来自配置（不强制 full-auto）；确认轴：浏览器交互模型需自主放行
        SandboxManager.Mode = _config.SandboxMode;
        PermissionManager.CurrentMode = PermissionManager.Mode.Yolo;
        // web 无终端 → diff 预览走浏览器弹窗，强制开启
        Config.Instance.DiffPreview = true;

        var web = new WebChatServer(_agent!, port);
        web.Start();
        var url = $"http://127.0.0.1:{web.Port}";
        MarkupLine($"«green»🌐 浏览器聊天界面已启动:«/» «cyan»{E(url)}«/»");
        if (Environment.GetEnvironmentVariable("WAYCODER_WEB_NO_OPEN") != "1")
            OpenBrowser(web.Port);
        MarkupLine("«dim»按 Ctrl+C 退出（自动保存会话）«/»");

        // 阻塞等待 Ctrl+C
        var tcs = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
        ConsoleCancelEventHandler handler = (_, e) => { e.Cancel = true; tcs.TrySetResult(true); };
        Console.CancelKeyPress += handler;
        try { await tcs.Task; }
        finally { Console.CancelKeyPress -= handler; }

        web.Stop();
        AutoSaveSession();
    }

    private static void OpenBrowser(int port)
    {
        var url = $"http://127.0.0.1:{port}";
        try
        {
            if (OperatingSystem.IsWindows())
                System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo("cmd", $"/c start {url}") { UseShellExecute = false });
            else if (OperatingSystem.IsMacOS())
                System.Diagnostics.Process.Start("open", url);
            else
                System.Diagnostics.Process.Start("xdg-open", url);
        }
        catch { /* 打开浏览器失败不影响服务 */ }
    }

    // ========================================================================
    // 一次性模式（JSON 输出）—— IDE / 脚本桥接
    // ========================================================================

    /// <summary>
    /// 一次性模式 + --json：静默执行 Agent（不流式输出），
    /// stdout 只打印一个结构化 JSON 结果，返回退出码（0 成功 / 1 失败）。
    /// </summary>
    private static async Task<int> RunOnceJsonAsync(string prompt)
    {
        using var cts = new CancellationTokenSource();
        RegisterOnceModeSignalHandlers();

        bool ok = await RunOneJsonCoreAsync(_agent!, _llm!, prompt, cts, "Program.RunOnceJson");

        // JSON 模式也保存会话（AutoSaveSession 只写盘/日志，不污染 stdout）
        AutoSaveSession();
        return ok ? 0 : 1;
    }

    /// <summary>
    /// 把一个提示词交给指定 Agent 跑完，往 stdout 写**一行** <see cref="JsonResult"/>（`--json` 桥接契约：
    /// 一次任务一个对象），返回是否成功。
    ///
    /// 抽出来是因为「一次性 <c>-p</c>」与「命令行槽位任务 <c>-p1</c>~<c>-p0</c>」都要用它 ——
    /// 后者此前走纯文本路径，`waycoder --json -p1 "修复 bug" | jq -r .answer` 会拿到不可解析的
    /// ANSI 文本（见 Program.Repl.cs 的无界面分支）。日志标签分开只为定位来源。
    /// </summary>
    private static async Task<bool> RunOneJsonCoreAsync(Agent agent, LLM llm, string prompt,
        CancellationTokenSource cts, string logTag)
    {
        var sw = System.Diagnostics.Stopwatch.StartNew();
        string? answer = null;
        bool success = false;
        string? error = null;

        try
        {
            llm.SnapshotTaskCost();
            answer = await agent.ChatAsync(prompt, cancellationToken: cts.Token);
            success = true;
        }
        catch (OperationCanceledException)
        {
            error = cts.IsCancellationRequested ? "已中断" : $"LLM 请求超时（{Config.Instance.LlmHttpTimeoutSec}s）";
        }
        catch (Exception ex)
        {
            ErrorLog.Error(logTag, $"一次性模式崩溃: {ex.Message}", ex);
            error = ex.Message;
        }
        finally
        {
            sw.Stop();
        }

        Console.WriteLine(JsonResult.Build(
            success: success,
            answer: answer ?? "",
            error: error,
            model: llm.Model,
            promptTokens: llm.TaskPromptTokens,
            completionTokens: llm.TaskCompletionTokens,
            costUsd: llm.TaskCost,
            durationMs: sw.ElapsedMilliseconds,
            changedFiles: EditFileTool.ChangedFiles).ToJson());
        return success;
    }

    // ========================================================================
    // 批量任务引擎
    // ========================================================================

    /// <summary>是否触发了批量任务（--batch 或 --batch-repo）。</summary>
    private static bool HasBatch(Dictionary<string, List<string>> parsed)
    {
        return Arguments.CliArgRegistry.Has(parsed, "batch")
            || Arguments.CliArgRegistry.Has(parsed, "batch-repo");
    }

    /// <summary>运行批量任务引擎：解析清单 → 多仓库并行 → 打印聚合报告。</summary>
    private static async Task<int> RunBatchAsync(Dictionary<string, List<string>> parsed)
    {
        BatchSpec? spec;
        string? error;

        var batchArg = Arguments.CliArgRegistry.Get(parsed, "batch");
        if (batchArg != null)
        {
            // 可能是内联 JSON 或文件路径
            var trimmed = batchArg.Trim();
            string json;
            if (trimmed.StartsWith("{", StringComparison.Ordinal))
            {
                json = trimmed;
            }
            else if (File.Exists(trimmed))
            {
                json = await File.ReadAllTextAsync(trimmed);
            }
            else
            {
                MarkupLine($"«red»✘ 批量任务文件不存在: {E(trimmed)}«/»");
                return 1;
            }
            spec = BatchSpec.Parse(json, out error);
            if (spec == null)
            {
                MarkupLine($"«red»✘ 批量任务解析失败: {E(error)}«/»");
                return 1;
            }
        }
        else
        {
            var repos = Arguments.CliArgRegistry.GetAll(parsed, "batch-repo") ?? new List<string>();
            var task = Arguments.CliArgRegistry.Get(parsed, "batch-task") ?? "";
            if (repos.Count == 0 || string.IsNullOrWhiteSpace(task))
            {
                MarkupLine("«red»✘ --batch-repo 至少需要一个仓库，且必须提供 --batch-task 共享任务«/»");
                return 1;
            }
            spec = BatchSpec.FromRepos(repos, task);
        }

        // --batch-keep 覆盖 keepResults
        if (Arguments.CliArgRegistry.Has(parsed, "batch-keep"))
            spec!.KeepResults = true;

        Console.WriteLine();
        MarkupLine("«bold cyan»🚀 WayCoder 批量任务引擎«/»");
        MarkupLine($"«dim»任务数: {spec!.Jobs.Count} · 并行度: {spec.MaxParallel} · 超时: {spec.TimeoutSec}s · 保留副本: {(spec.KeepResults ? "是" : "否")}«/»");
        Console.WriteLine();

        var report = await BatchRunner.RunAsync(spec, log: line => Console.WriteLine(line));

        Console.WriteLine();
        Console.WriteLine(report.ToMarkdown());
        MarkupLine(report.Failed == 0
            ? $"«bold green»✅ 批量任务全部成功 ({report.Succeeded}/{report.Total})«/»"
            : $"«bold red»❌ 批量任务完成：成功 {report.Succeeded} / 失败 {report.Failed}«/»");

        return report.Failed == 0 ? 0 : 1;
    }
}
