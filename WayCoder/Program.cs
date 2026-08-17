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
    private static Config _config = new();
    private static LLM? _llm;
    private static Agent? _agent;
    private static readonly AgentSlot[] _slots = new AgentSlot[AgentSlot.Count];
    private static volatile int _activeSlot; // 当前活跃槽位索引（F1 对应 0）。volatile：主线程写、后台槽位/命令线程读，保证可见性

    /// <summary>待投递的槽位任务队列：槽位索引 → 任务列表（同一槽位多次 -pN 可排队）</summary>
    private static readonly Dictionary<int, List<string>> _pendingSlotQueues = [];

    /// <summary>所有槽位任务的共享前缀（-pa 传入）</summary>
    private static string _pendingSlotPrefix = "";

    /// <summary>当前活跃槽位索引（供外部命令访问）</summary>
    public static int ActiveSlotIndex => _activeSlot;

    /// <summary>所有槽位数组（供外部命令访问）</summary>
    public static AgentSlot[] GetSlots() => _slots;
    private static WatchMode? _watchMode;
    private static volatile bool _exitRequested;
    private static readonly Task?[] _slotTasks = new Task?[AgentSlot.Count];
    private static (List<JNode> Messages, string Model)? _pendingRestore;

    /// <summary>一次性/管道模式 POSIX 信号注册（保持引用防 GC 回收，Windows 下为 null）。</summary>
    private static System.Runtime.InteropServices.PosixSignalRegistration? _sigintReg;
    private static System.Runtime.InteropServices.PosixSignalRegistration? _sigtermReg;

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
        if (budgetStr != null && double.TryParse(budgetStr, out var b)) maxBudget = b;
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
        bool jsonMode = Arguments.CliArgRegistry.Has(parsed, "json");
        bool webMode = Arguments.CliArgRegistry.Has(parsed, "web");
        string? webPortSpec = Arguments.CliArgRegistry.Get(parsed, "web");

        if (Arguments.CliArgRegistry.Has(parsed, "version"))
        {
            Console.WriteLine(Global.AppNameVersion);
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
        // -p1~-p0 槽位任务也自动放行
        if (prompt != null || _pendingSlotQueues.Count > 0)
            yoloMode = true;

        _config = Config.FromEnv();
        // 加载主题配色：theme.json 记住的 preset 优先，回退 .env ThemePreset（首次启动无 theme.json）
        ThemeConfig.ApplyPreset(ThemeConfig.Instance.PresetKey ?? _config.ThemePreset);
        if (model != null) _config.Model = model;
        if (baseUrl != null) _config.BaseUrl = baseUrl;
        if (apiKey != null)
        {
            _config.ApiKey = apiKey;
            // 命令行配置的 API key 自动保存到全局 ~/.waycoder/api_keys.json（key 跟服务商走，不跟模型走）。
            // 服务商优先级：--model 指定的模型所属服务商 > 当前服务商。
            var keyProvider = model != null
                ? (ModelCatalog.Find(model)?.ProviderId ?? _config.Provider)
                : _config.Provider;
            if (!string.IsNullOrWhiteSpace(keyProvider))
            {
                ApiKeyStore.Set(keyProvider, apiKey);
                _config.Provider = keyProvider;
            }
        }
        if (maxBudget != null) _config.MaxBudgetUsd = maxBudget;
        if (maxRequeue != null) _config.MaxAutoRequeue = maxRequeue.Value;
        if (watchMode) _config.WatchMode = true;
        if (economyMode)
            _config.EconomyMode = (economySpec?.ToLowerInvariant()) switch
            {
                "auto" => EconomyMode.Auto,
                "off" => EconomyMode.Off,
                _ => EconomyMode.On,
            };

        // 从模型目录自动设置 base URL（支持 Ollama/DeepSeek/OpenAI 等所有模型）
        if (_config.BaseUrl == null)
        {
            var catInfo = ModelCatalog.Find(_config.Model);
            if (catInfo?.DefaultBaseUrl != null)
                _config.BaseUrl = catInfo.DefaultBaseUrl;
        }

        // Tiny 模式：显式 --tiny（可指定窗口），或模型窗口 <128K 自动进入（本地小模型）
        if (tinyMode)
        {
            _config.TinyMode = true;
            _config.TinyWindow = ModelCatalog.ResolveTinyWindow(tinyWindowSpec, _config.Model, _config.BaseUrl);
        }
        else
        {
            var probed = ModelCatalog.ProbeModelWindow(_config.Model, _config.BaseUrl, _config.MaxContextTokens);
            if (probed < Config.TinyAutoThreshold)
            {
                _config.TinyMode = true;
                _config.TinyWindow = probed;
            }
        }

        // Local/Ollama 模型不需要 API key
        var isLocalModel = _config.Model.Contains("ollama", StringComparison.OrdinalIgnoreCase)
            || (_config.BaseUrl?.Contains("localhost") == true)
            || (_config.BaseUrl?.Contains("127.0.0.1") == true);

        if (string.IsNullOrEmpty(_config.ApiKey) && !isLocalModel)
        {
            MarkupLine("«bold red»╔══════════════════════════════╗«/»");
            MarkupLine("«bold red»║  API 密钥未设置！           ║«/»");
            MarkupLine("«bold red»╚══════════════════════════════╝«/»");
            Console.WriteLine();
            Console.WriteLine("请设置以下环境变量之一:");
            Console.WriteLine("  WAYCODER_API_KEY");
            Console.WriteLine("  DEEPSEEK_API_KEY");
            Console.WriteLine("  GEMINI_API_KEY (Google 免费层)");
            Console.WriteLine("  OPENAI_API_KEY");
            Console.WriteLine("  ANTHROPIC_API_KEY");
            Console.WriteLine("  DASHSCOPE_API_KEY (阿里千问)");
            Console.WriteLine("  API_KEY");
            Console.WriteLine();
            Console.WriteLine("或者在项目根目录创建 .env 文件:");
            Console.WriteLine("  WAYCODER_API_KEY=sk-你的密钥");
            Console.WriteLine();
            Console.WriteLine("或用全局 JSON 保存多个服务商的 key（一键切换模型/服务商，无需重输）:");
            Console.WriteLine("  waycoder --model key <供应商> <key>   # 如 --model key deepseek sk-xxx");
            Console.WriteLine("  waycoder --model name <模型ID>        # 切换模型，自动匹配对应 key");
            return 1;
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

        // 恢复会话（-c/--continue/--resume）
        var hasResumeFlag = Arguments.CliArgRegistry.Has(parsed, "resume");
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
                if (model == null)
                {
                    _llm.Model = loaded.Value.Model;
                    _config.Model = loaded.Value.Model;
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

        if (!string.IsNullOrEmpty(prompt))
        {
            if (jsonMode)
                return await RunOnceJsonAsync(prompt);
            await RunOnceAsync(prompt);
        }
        else
            await RunReplAsync(editFile);

        return 0;
    }

    // ========================================================================
    // 一次性模式
    // ========================================================================

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
        SandboxManager.SetLevel("full-auto");
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
        var sw = System.Diagnostics.Stopwatch.StartNew();
        string? answer = null;
        bool success = false;
        string? error = null;

        using var cts = new CancellationTokenSource();
        RegisterOnceModeSignalHandlers();
        try
        {
            _llm!.SnapshotTaskCost();
            answer = await _agent!.ChatAsync(prompt, cancellationToken: cts.Token);
            success = true;
        }
        catch (OperationCanceledException)
        {
            error = cts.IsCancellationRequested ? "已中断" : $"LLM 请求超时（{Config.Instance.LlmHttpTimeoutSec}s）";
        }
        catch (Exception ex)
        {
            ErrorLog.Error("Program.RunOnceJson", $"一次性模式崩溃: {ex.Message}", ex);
            error = ex.Message;
        }
        finally
        {
            sw.Stop();
            // JSON 模式也保存会话（AutoSaveSession 只写盘/日志，不污染 stdout）
            AutoSaveSession();
        }

        var result = JsonResult.Build(
            success: success,
            answer: answer ?? "",
            error: error,
            model: _llm!.Model,
            promptTokens: _llm.TaskPromptTokens,
            completionTokens: _llm.TaskCompletionTokens,
            costUsd: _llm.TaskCost,
            durationMs: sw.ElapsedMilliseconds,
            changedFiles: EditFileTool.ChangedFiles);

        Console.WriteLine(result.ToJson());
        return success ? 0 : 1;
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
