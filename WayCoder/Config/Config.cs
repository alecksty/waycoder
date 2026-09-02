
namespace WayCoder;

/// <summary>
/// 设置项元数据 —— 供设置界面自动生成布局。
/// </summary>
public record SettingDef(
    string Key, string Label, string Category, string Desc,
    string Type = "text",          // text | number | select | secret | toggle
    string[]? Options = null,      // select 类型的可选项
    string? EnvVar = null,
    int Order = 0,
    string Default = ""   // 默认值（设置界面「复位默认」用）
);

/// <summary>
/// 单个配置属性的完整定义 —— Schema 驱动 FromEnv / SettingSchema / SaveToEnvFile。
/// AOT 安全：Getter/Setter 用委托不用反射。
/// </summary>
record ConfigProp(
    string Key,                        // 属性名 "Model"
    string? EnvVar,                    // "WAYCODER_MODEL"；null = 仅 config.json（精简环境变量后大部分置 null）
    string? OldEnvVar,                 // null（旧名兼容，null = 无）
    string Label,                      // "大模型 (复杂任务)"
    string Category,                   // "🤖 模型"
    string Desc,                       // "架构/重构/调试/多文件"
    string Type,                       // text | number | select | secret | toggle
    string[]? Options,                 // select 下拉选项
    int Order,                         // 分类内排序
    Func<Config, string> Getter,       // Config → 环境变量值（字符串）
    Action<Config, string> Setter,     // (Config, 环境变量值) → 设置属性
    string? DefaultStr = null,         // 默认值（保存时跳过相等的情况）
    bool SkipIfEmpty = false           // 值为空字符串时跳过保存
);

/// <summary>
/// 省 Token 模式三态：
///   Off  (关)  — 完整提示词 + 正常压缩阈值（默认）
///   On   (开)  — 精简提示词 + 激进压缩阈值 + 输出上限
///   Auto (自动) — 保持完整提示词，压缩阈值按「任务轮数复杂度」动态插值（简单省、复杂保质量）
/// </summary>
public enum EconomyMode
{
    Off,
    Auto,
    On,
    /// <summary>极致：提示词尽量不注入、上下文尽量少给（比 On 更激进）</summary>
    Extreme,
}

/// <summary>沙箱边界模式（独立于权限确认轴）：管「能碰什么」——可写范围 + 网络。</summary>
public enum SandboxMode
{
    /// <summary>无边界（保持现状）</summary>
    Off,
    /// <summary>可写范围仅项目根；网络开</summary>
    ProjectWrite,
    /// <summary>可写任意（除敏感）；网络关</summary>
    NetworkOff,
    /// <summary>仅项目根 + 网络关（最严，bash 进程沙箱化）</summary>
    Hard,
}

/// <summary>
/// 省 Token 自动模式的优先级偏好（仅 Auto 生效）：
///   Quality  (质量优先) — 复杂任务几乎不省，简单任务才省（默认，先保质量再谈费用）
///   Balanced (均衡)     — 始终保留一定省钱力度
///   Cost     (费用优先) — 尽量省，弱化复杂度影响
/// </summary>
public enum EconomyPriority
{
    Quality,
    Balanced,
    Cost,
}

/// <summary>
/// 配置 - 单例模式，所有模块通过 Config.Instance 统一读取。
/// config.json 是唯一权威源；默认不使用环境变量（含 .env 文件）。
/// 仅首次启动（无 config.json）时从 .env + 环境变量读取并导入固化到 config.json，
/// 此后环境变量不再被引用。删除 config.json 即回到首次启动状态（可重新导入环境变量）。
///
/// 新增配置项只需在 _schema 列表中加一行，SettingSchema/FromEnv/SaveToEnvFile 全部自动推导。
/// 环境变量只保留引导级（约 14 个：服务商/模型/密钥/经济/鼠标/预算/工具白黑名单/Whisper），
/// 其余属性 EnvVar 置 null = 仅走 config.json（对齐竞品：环境变量越少越好）。
/// API Key 不保存到 config.json / .env 文件（密钥独立管理，走 api_keys.json；首次启动从环境变量导入）。
/// </summary>
public partial class Config
{
    // ════════════════════════════════════════════════════════════
    // 单例
    // ════════════════════════════════════════════════════════════

    /// <summary>全局配置单例（首次访问时线程安全地完整初始化后发布）</summary>
    public static Config Instance => _instance.Value;

    /// <summary>
    /// 懒加载单例：完整初始化（加载 .env / schema / config.json / ApiKey 解析 / 迁移 / 同步）
    /// 全部完成后才由 Lazy 发布，避免并发首次访问读到半初始化的 Config（原实现 _instance 在初始化中途赋值，
    /// 另一线程可能读到缺失 config.json 覆盖与 ApiKey 的实例）。
    /// </summary>
    private static Lazy<Config> _instance = new(CreateInstance);

    private static Config CreateInstance()
    {
        var cfg = new Config();
        var configExists = File.Exists(ConfigJsonPath);
        string? providerKey = null;

        if (configExists)
        {
            // 默认路径：config.json 是唯一权威源 —— 不读 .env、不读环境变量
            //（环境变量仅在首次启动导入过，之后不再被引用）。
            cfg.LoadConfigJson();

            // API Key 只从 api_keys.json 解析（环境变量 key 首次启动时已导入 api_keys.json）
            providerKey = ApiKeyStore.Get(cfg.Provider) ?? ApiKeyStore.ForModel(cfg.Model);
            if (!string.IsNullOrWhiteSpace(providerKey))
                cfg.ApiKey = providerKey;
            // BaseUrl 已由 LoadConfigJson 载入，无环境变量兜底
        }
        else
        {
            // 首次启动（无 config.json）：从 .env + 环境变量读取，并把结果导入固化到配置文件。
            LoadDotEnv();
            foreach (var p in _schema)
            {
                var val = Env(p.EnvVar, p.OldEnvVar);
                if (!string.IsNullOrEmpty(val))
                {
                    try { p.Setter(cfg, val); }
                    catch { /* 非法值（如 WAYCODER_MAX_TOKENS=abc）忽略，保留默认值，避免启动崩溃 */ }
                }
            }

            // 环境变量 API Key → api_keys.json（只补空不覆盖）：
            // 供应商专属变量（DEEPSEEK_API_KEY 等）由 ImportFromEnvironment 处理；
            // 通用 WAYCODER_API_KEY（schema 已写入 cfg.ApiKey）导入到当前服务商条目。
            ApiKeyStore.ImportFromEnvironment();
            if (string.IsNullOrEmpty(ApiKeyStore.Get(cfg.Provider))
                && !string.IsNullOrWhiteSpace(cfg.ApiKey)
                && ApiKeyStore.IsValidApiKey(cfg.ApiKey))
            {
                ApiKeyStore.Set(cfg.Provider, cfg.ApiKey);
            }

            // ApiKey 解析：api_keys.json（刚导入）优先，其余环境变量兜底（仅首次）
            providerKey = ApiKeyStore.Get(cfg.Provider) ?? ApiKeyStore.ForModel(cfg.Model);
            if (!string.IsNullOrWhiteSpace(providerKey))
            {
                cfg.ApiKey = providerKey;
            }
            else if (string.IsNullOrEmpty(cfg.ApiKey))
            {
                // json 为空时才用环境变量 key（默认优先 api_keys.json，env 只补空不覆盖）
                cfg.ApiKey = ApiKeyStore.EnvKey(cfg.Provider)
                    ?? Environment.GetEnvironmentVariable("API_KEY")
                    ?? "";
            }

            // BaseUrl 环境变量兜底（仅首次）
            if (string.IsNullOrEmpty(cfg.BaseUrl))
            {
                cfg.BaseUrl = Environment.GetEnvironmentVariable("OPENAI_BASE_URL");
            }

            // 导入：把当前值固化为 config.json（此后 config.json 为权威源，环境变量不再被读取）。
            // 首次启动固定生成 config.json（即使无环境变量），锁定「config.json 存在」状态。
            cfg.SaveToConfigJson();
            // 已有 .env 才精简为 5 项引导配置；全新安装不凭空创建 .env
            if (FindEnvFile() != null) cfg.SaveMinimalDotEnv();
        }

        // 每次启动：全局 config.json 有更新则同步一份到项目本地，防止意外损坏
        cfg.SyncConfigJsonToLocal();

        return cfg;
    }

    /// <summary>.env 写文件锁：Web 设置面板可能并发 POST 多项设置，串行化读改写防止文件锁冲突（IOException）。</summary>
    private static readonly object SaveLock = new();

    /// <summary>重新加载配置（读取最新的环境变量和 .env 文件）</summary>
    public static void Reload() => _instance = new Lazy<Config>(CreateInstance);

    /// <summary>加载配置（兼容旧调用，返回单例；config.json 为权威源，环境变量仅首次启动导入）</summary>
    public static Config FromEnv() => Instance;
    // ════════════════════════════════════════════════════════════
    // 属性声明（保持原有类型和默认值，全项目兼容）
    // ════════════════════════════════════════════════════════════

    public string Model { get; set; } = "deepseek-v4-flash";
    public string SmallModel { get; set; } = "deepseek-v4-flash";
    public string ApiKey { get; set; } = "";
    public string? BaseUrl { get; set; }
    public int MaxTokens { get; set; } = 32768;
    public float Temperature { get; set; } = 0.1f;
    public int MaxContextTokens { get; set; } = 128_000;
    public string Provider { get; set; } = "openai";
    public string SmallProvider { get; set; } = "deepseek";
    /// <summary>切换免费模型前记住的模型（/free-restore / --model restore 恢复，跨会话持久化）。</summary>
    public string? FreePrevProvider { get; set; }
    public string? FreePrevModel { get; set; }
    public string? FreePrevBaseUrl { get; set; }
    public double? MaxBudgetUsd { get; set; }
    /// <summary>预算预警阈值（%）：花费达到预算此百分比时发出一次提醒（0=关闭预警）。</summary>
    public double BudgetWarnPercent { get; set; } = 80.0;
    public bool AutoGitCommit { get; set; } = false;
    /// <summary>写文件前自动快照（改坏可回滚）：每轮对话首次写文件前自动创建文件备份检查点。</summary>
    public bool AutoCheckpoint { get; set; } = true;
    /// <summary>更新开关（内网/离线部署）：关闭后 /update 与 --update 不做任何网络请求，直接提示已禁用。</summary>
    public bool UpdateEnabled { get; set; } = true;
    /// <summary>Ollama 显式 num_ctx（上下文窗口大小，0=自动探测不发送）。内网本地模型可强制指定。</summary>
    public int OllamaNumCtx { get; set; } = 0;
    public bool WatchMode { get; set; } = false;
    public bool PromptCaching { get; set; } = true;
    public string SandboxLevel { get; set; } = "suggest";
    public bool EditorLint { get; set; } = true;
    public string EditorIndent { get; set; } = "tab";   // "tab"=制表符 / "space"=4 空格
    public bool DiffPreview { get; set; } = false;
    public bool WriteContentView { get; set; } = true;
    public bool MouseEnabled { get; set; } = true;
    /// <summary>静默模式运行标志（-q/--quiet，非持久化运行时开关）</summary>
    public bool QuietMode { get; set; }
    /// <summary>聊天区显示消息上限：超过后自动丢弃最旧消息（Agent 会话仍在、会话文件持久化，仅裁剪显示层保持流畅）</summary>
    public int MaxChatMessages { get; set; } = 1000;
    /// <summary>聊天区显示消息总 token 上限：超过后自动丢弃最旧消息（按单条估算 token 累计）。
    /// 防止单条工具输出巨大时，即使条数未超限，显示层总内容仍过大导致渲染卡死。</summary>
    public int MaxChatTokens { get; set; } = 200_000;
    /// <summary>聊天代码块预览行数上限：超过后保留头尾、中间折叠省略</summary>
    public int MaxCodePreviewLines { get; set; } = 500;
    public bool DesktopNotifications { get; set; } = false;
    public int ToolTimeoutSec { get; set; } = 120;
    public string AllowedTools { get; set; } = "";     // 逗号分隔白名单，空=全部允许
    public string DisabledTools { get; set; } = "";    // 逗号分隔黑名单
    public string PlanToolAllowList { get; set; } = "";   // 计划模式工具白名单（空=全部，危险工具默认不放行）
    public string BuildToolAllowList { get; set; } = "";  // 建造模式工具白名单（空=全部）
    public string YoloToolAllowList { get; set; } = "";   // YOLO 模式工具白名单（空=全部）
    /// <summary>追加到系统提示词的自定义文本（--system-prompt / --append-system-prompt）</summary>
    public string ExtraSystemPrompt { get; set; } = "";
    public int LintTimeoutSec { get; set; } = 60;
    public int SubAgentMaxDepth { get; set; } = 3;
    public int BashOutputMaxChars { get; set; } = 50_000;
    public string WatchExtensions { get; set; } = "";
    public string WatchIgnoreDirs { get; set; } = "";
    public double? FallbackMaxBudget { get; set; } = 5.0;
    public int MemoryRelevanceTopN { get; set; } = 5;
    public bool EmbeddingEnabled { get; set; } = false;
    public string EmbeddingModel { get; set; } = "text-embedding-3-small";
    public int EmbeddingDimensions { get; set; } = 0;
    public bool TeamMemoryEnabled { get; set; } = false;
    public bool TeamMemoryAutoSync { get; set; } = true;
    /// <summary>教学模式：AI 不只执行，还讲解为什么 + 结束时提问巩固。</summary>
    public bool TeachModeEnabled { get; set; } = false;
    /// <summary>会话退出时自动复盘并提炼经验入知识库。</summary>
    public bool RetroOnExitEnabled { get; set; } = false;
    public string ThemePreset { get; set; } = "黄金甲";
    /// <summary>GUI 版深/浅主题（dark/light）</summary>
    public string GuiTheme { get; set; } = "dark";

    // ── 推理深度 ──
    /// <summary>推理深度级别（minimal/low/medium/high/max），空字符串=不设置（模型默认）</summary>
    public string ReasoningEffort { get; set; } = "";

    // ── 语音转录（transcribe 工具）──
    /// <summary>Whisper 转录模型，默认 whisper-1</summary>
    public string WhisperModel { get; set; } = "whisper-1";
    /// <summary>Whisper 转录 API 地址，空=默认 https://api.openai.com</summary>
    public string? WhisperBaseUrl { get; set; }
    /// <summary>Whisper 转录 API Key，空=回退到主 ApiKey</summary>
    public string WhisperApiKey { get; set; } = "";

    // 界面主题
    public string BorderStyle { get; set; } = "rounded";
    public string BorderColor { get; set; } = "36";
    public string AccentColor { get; set; } = "36";
    public string ColorScheme { get; set; } = "default";
    public string ChatDisplayStyle { get; set; } = "auto";

    /// <summary>用 .tui 声明式标记版聊天界面（实验性，默认关闭；测试通过后翻默认）</summary>
    public bool MarkupUi { get; set; } = false;

    // ── 沙箱 ──
    public int SandboxMaxMemoryMb { get; set; } = 1024;
    public int SandboxMaxCpuSeconds { get; set; } = 300;
    public bool SandboxAllowNetwork { get; set; } = false;

    // ── Agent/SubAgent ──
    public int MaxRounds { get; set; } = 50;
    public int SubAgentMaxParallel { get; set; } = 4;
    public int SubAgentOutputMaxChars { get; set; } = 5000;
    // 子智能体第 0 层（顶层）最大工具调用轮次，每深一层减 5，下限 5。
    // 默认 20（复杂模块如 DataStructures/Mathematics 单次派发可写更多代码，减少反复重派）。
    public int SubAgentMaxRounds { get; set; } = 20;
    // 并行子智能体聚合结果的总字符上限（0=不限制）。单个子智能体输出各截断到
    // SubAgentOutputMaxChars，但并行 N 个累加仍可能撑爆主智能体上下文，故再设总上限。
    public int SubAgentParallelTotalMaxChars { get; set; } = 15000;
    // 子智能体失败（返回错误）时的自动重试次数。默认 1：首次失败后换方法重试一次，仍失败则返回错误。
    public int SubAgentRetryCount { get; set; } = 1;
    // 子智能体 tasks 数组的硬上限（防 LLM 生成海量任务失控）。超出 SubAgentMaxParallel 的部分自动分批串行，
    // 但总数一旦超过此上限直接报错（保护资源）。默认 100。
    public int SubAgentMaxTotalTasks { get; set; } = 100;

    // ── LLM 连接 ──
    public int LlmHttpTimeoutSec { get; set; } = 300;
    public int LlmMaxRetries { get; set; } = 5;
    public int LlmConnectionTimeoutSec { get; set; } = 300;
    public int LlmRateLimitMaxWaitSec { get; set; } = 120;

    // ── 回退链 ──
    /// <summary>回退链开关：默认关。开=模型失败时按 connect 链自动回退；关=只用当前模型，失败即停。</summary>
    public bool FallbackEnabled { get; set; } = false;
    public string FallbackChain { get; set; } = "deepseek-v4-pro,deepseek-v4-flash,qwen-turbo,glm-4-flash";

    // ── 文件锁 ──
    public int FileLockTimeoutSec { get; set; } = 30;

    // ── 超时参数（集中管理，所有超时均可通过环境变量/设置界面修改） ──
    public int BackgroundTaskTimeoutSec { get; set; } = 600;
    public int AutoTestTimeoutSec { get; set; } = 30;
    public int AutoTestDebounceSec { get; set; } = 60;
    /// <summary>指定测试命令（测试驱动修复）：非空时优先用它而非自动探测；测试失败会硬绿判定直到通过。</summary>
    public string TestCommand { get; set; } = "";
    /// <summary>修完必验证：声明完成前若本轮改过源码但未跑过验证（build/test），强制收尾验证一次，防「假修好了」。</summary>
    public bool VerifyBeforeDone { get; set; } = true;
    public int GitTimeoutSec { get; set; } = 15;
    public int KillTimeoutSec { get; set; } = 10;
    public int DownloadTimeoutSec { get; set; } = 60;
    public int HookTimeoutSec { get; set; } = 10;
    public int AskUserTimeoutSec { get; set; } = 120;
    public int RegexTimeoutSec { get; set; } = 5;
    public int FetchTimeoutSec { get; set; } = 30;

    // ── 上下文压缩 ──
    public int ContextSnipRatio { get; set; } = 50;
    public int ContextSummarizeRatio { get; set; } = 70;
    public int ContextCollapseRatio { get; set; } = 90;

    // Crush-style: 基于真实 token 数的自动摘要阈值
    // 大窗口 (>200K) 用固定 buffer，小窗口用比例
    public int ContextWindowLargeThreshold { get; set; } = 200_000;
    public int ContextWindowLargeBuffer { get; set; } = 20_000;
    public double ContextWindowSmallRatio { get; set; } = 0.2;

    // 自动摘要后是否注入继续提示（Crush 风格 auto-requeue）
    public bool AutoContinueAfterSummarize { get; set; } = true;

    // 撞 MaxRounds 上限后自动压缩 + 续跑的次数（0=关闭自动续跑）
    public int MaxAutoRequeue { get; set; } = 3;

    // Tiny 模式：小窗口 + 精简系统提示词（省 token / 压力测试 / 本地小模型）
    public bool TinyMode { get; set; } = false;

    /// <summary>Tiny 模式默认上下文窗口（4K，探测失败时的兜底）</summary>
    public const int TinyContextWindow = 4096;

    /// <summary>Tiny 模式实际上下文窗口（--tiny 8k 指定，或 --tiny 自动探测覆盖）</summary>
    public int TinyWindow { get; set; } = TinyContextWindow;

    // 省 token 模式：三态（关/自动/开）。保持正常窗口，从提示词/压缩/输出上限综合降 token
    public EconomyMode EconomyMode { get; set; } = EconomyMode.Off;

    /// <summary>循环切换到下一个经济模式（关闭→自动→开启→极致→关闭）。返回新模式。</summary>
    public EconomyMode CycleEconomy()
    {
        EconomyMode = EconomyMode switch
        {
            EconomyMode.Off => EconomyMode.Auto,
            EconomyMode.Auto => EconomyMode.On,
            EconomyMode.On => EconomyMode.Extreme,
            _ => EconomyMode.Off,
        };
        return EconomyMode;
    }

    /// <summary>沙箱边界模式（独立于权限确认轴）：Off / ProjectWrite / NetworkOff / Hard。</summary>
    public SandboxMode SandboxMode { get; set; } = SandboxMode.Off;

    /// <summary>检查点保留上限（超过自动删除最旧）。</summary>
    public int CheckpointMax { get; set; } = 50;

    /// <summary>循环切换到下一个沙箱模式（关闭→项目写→网络关→严格→关闭）。返回新模式。</summary>
    public SandboxMode CycleSandbox()
    {
        SandboxMode = SandboxMode switch
        {
            SandboxMode.Off => SandboxMode.ProjectWrite,
            SandboxMode.ProjectWrite => SandboxMode.NetworkOff,
            SandboxMode.NetworkOff => SandboxMode.Hard,
            _ => SandboxMode.Off,
        };
        return SandboxMode;
    }

    /// <summary>自动模式优先级（仅 Auto 生效）：质量优先（默认）/均衡/费用优先</summary>
    public EconomyPriority EconomyPriority { get; set; } = EconomyPriority.Quality;

    /// <summary>省 token 模式：snip 裁剪比例（设置界面可调）</summary>
    public int EconomySnipRatio { get; set; } = 35;
    /// <summary>省 token 模式：LLM 摘要比例（设置界面可调）</summary>
    public int EconomySummarizeRatio { get; set; } = 55;
    /// <summary>省 token 模式：硬折叠比例（设置界面可调）</summary>
    public int EconomyCollapseRatio { get; set; } = 75;
    /// <summary>省 token 模式：工具输出单条裁剪字符阈值（设置界面可调）</summary>
    public int EconomySnipChars { get; set; } = 2000;
    /// <summary>省 token 模式：单次输出 token 上限（设置界面可调）</summary>
    public int EconomyMaxTokens { get; set; } = 8192;
    /// <summary>正常模式：工具输出单条裁剪字符阈值（设置界面可调，对照 EconomySnipChars）</summary>
    public int SnipCharsNormal { get; set; } = 4000;
    /// <summary>自动模式：任务达到此轮数视为「完全复杂」（收紧系数降到最低，保质量）</summary>
    public int EconomyComplexRounds { get; set; } = 30;
}
