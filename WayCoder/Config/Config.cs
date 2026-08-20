
namespace WayCoder;

/// <summary>
/// 设置项元数据 —— 供设置界面自动生成布局。
/// </summary>
public record SettingDef(
    string Key, string Label, string Category, string Desc,
    string Type = "text",          // text | number | select | secret | toggle
    string[]? Options = null,      // select 类型的可选项
    string EnvVar = "",
    int Order = 0,
    string Default = ""   // 默认值（设置界面「复位默认」用）
);

/// <summary>
/// 单个配置属性的完整定义 —— Schema 驱动 FromEnv / SettingSchema / SaveToEnvFile。
/// AOT 安全：Getter/Setter 用委托不用反射。
/// </summary>
record ConfigProp(
    string Key,                        // 属性名 "Model"
    string EnvVar,                     // "WAYCODER_MODEL"
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
/// 配置 - 环境变量和默认值。单例模式，所有模块通过 Config.Instance 统一读取。
///
/// 新增配置项只需在 _schema 列表中加一行，SettingSchema/FromEnv/SaveToEnvFile 全部自动推导。
/// 环境变量读取 WAYCODER_* 前缀。
/// API Key 不保存到 .env 文件（密钥独立管理）。
/// </summary>
public class Config
{
    // ════════════════════════════════════════════════════════════
    // 单例
    // ════════════════════════════════════════════════════════════

    /// <summary>全局配置单例（首次访问时初始化）</summary>
    public static Config Instance
    {
        get
        {
            if (_instance == null)
            {
                LoadDotEnv();
                _instance = new Config();
                // Schema 驱动的批量加载
                foreach (var p in _schema)
                {
                    var val = Env(p.EnvVar, p.OldEnvVar);
                    if (!string.IsNullOrEmpty(val))
                    {
                        try { p.Setter(_instance, val); }
                        catch { /* 非法值（如 WAYCODER_MAX_TOKENS=abc）忽略，保留默认值，避免启动崩溃 */ }
                    }
                }
                // 特殊处理：ApiKey 解析 —— 一个服务商一个 key，key 跟服务商走（不跟模型走）。
                // 优先级：全局 JSON（按当前服务商）> .env WAYCODER_API_KEY > 各家 API_KEY 环境变量。
                var providerKey = ApiKeyStore.Get(_instance.Provider) ?? ApiKeyStore.ForModel(_instance.Model);
                if (!string.IsNullOrWhiteSpace(providerKey))
                {
                    _instance.ApiKey = providerKey;
                }
                else if (string.IsNullOrEmpty(_instance.ApiKey))
                {
                    _instance.ApiKey = Environment.GetEnvironmentVariable("OPENAI_API_KEY")
                        ?? Environment.GetEnvironmentVariable("DEEPSEEK_API_KEY")
                        ?? Environment.GetEnvironmentVariable("ANTHROPIC_API_KEY")
                        ?? Environment.GetEnvironmentVariable("API_KEY")
                        ?? "";
                }
                // 特殊处理：BaseUrl 多路回退
                if (string.IsNullOrEmpty(_instance.BaseUrl))
                {
                    _instance.BaseUrl = Environment.GetEnvironmentVariable("OPENAI_BASE_URL");
                }
            }
            return _instance;
        }
    }
    private static Config? _instance;

    /// <summary>.env 写文件锁：Web 设置面板可能并发 POST 多项设置，串行化读改写防止文件锁冲突（IOException）。</summary>
    private static readonly object SaveLock = new();

    /// <summary>重新加载配置（读取最新的环境变量和 .env 文件）</summary>
    public static void Reload() { _instance = null; }

    /// <summary>从环境变量加载配置（兼容旧调用，返回单例）</summary>
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
    public double? MaxBudgetUsd { get; set; }
    public bool AutoGitCommit { get; set; } = false;
    public bool WatchMode { get; set; } = false;
    public bool PromptCaching { get; set; } = true;
    public string SandboxLevel { get; set; } = "suggest";
    public bool EditorLint { get; set; } = true;
    public string EditorIndent { get; set; } = "tab";   // "tab"=制表符 / "space"=4 空格
    public bool DiffPreview { get; set; } = false;
    public bool WriteContentView { get; set; } = true;
    public bool MouseEnabled { get; set; } = true;
    /// <summary>聊天区显示消息上限：超过后自动丢弃最旧消息（Agent 会话仍在、会话文件持久化，仅裁剪显示层保持流畅）</summary>
    public int MaxChatMessages { get; set; } = 1000;
    /// <summary>聊天代码块预览行数上限：超过后保留头尾、中间折叠省略</summary>
    public int MaxCodePreviewLines { get; set; } = 500;
    public bool DesktopNotifications { get; set; } = false;
    public int ToolTimeoutSec { get; set; } = 120;
    public string AllowedTools { get; set; } = "";     // 逗号分隔白名单，空=全部允许
    public string DisabledTools { get; set; } = "";    // 逗号分隔黑名单
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

    // ── LLM 连接 ──
    public int LlmHttpTimeoutSec { get; set; } = 300;
    public int LlmMaxRetries { get; set; } = 5;
    public int LlmConnectionTimeoutSec { get; set; } = 300;
    public int LlmRateLimitMaxWaitSec { get; set; } = 120;

    // ── 回退链 ──
    public string FallbackChain { get; set; } = "deepseek-v4-pro,deepseek-v4-flash,qwen-turbo,glm-4-flash";

    // ── 文件锁 ──
    public int FileLockTimeoutSec { get; set; } = 30;

    // ── 超时参数（集中管理，所有超时均可通过环境变量/设置界面修改） ──
    public int BackgroundTaskTimeoutSec { get; set; } = 600;
    public int AutoTestTimeoutSec { get; set; } = 30;
    public int AutoTestDebounceSec { get; set; } = 60;
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

    // ════════════════════════════════════════════════════════════
    // 单一 Schema 定义（新增配置项只加这里一行）
    // ════════════════════════════════════════════════════════════

    static readonly ConfigProp[] _schema;

    static Config()
    {
        _schema = [
            // ── 模型 ──
            P("Model",        "WAYCODER_MODEL",           null,
              "大模型 (复杂任务)", "🤖 模型", "架构/重构/调试/多文件",
              "select", ["deepseek-chat","deepseek-v4-pro","gpt-5.4","gpt-5.5","deepseek-v4-flash","gpt-4o","gpt-4o-mini"], 0,
              c => c.Model, (c, v) => c.Model = v, "deepseek-v4-flash"),

            P("SmallModel",   "WAYCODER_SMALL_MODEL",     null,
              "小模型 (简单任务)", "🤖 模型", "补全/摘要/压缩 (便宜快速)",
              "select", ["deepseek-chat","deepseek-v4-flash","gpt-5.4-mini","gpt-4o-mini","deepseek-v4-pro"], 1,
              c => c.SmallModel, (c, v) => c.SmallModel = v, "deepseek-v4-flash"),

            P("SmallProvider","WAYCODER_SMALL_PROVIDER",  null,
              "小模型服务商", "🤖 模型", "小模型所属服务商 (deepseek/qwen/openai/...)",
              "text", null, 2,
              c => c.SmallProvider, (c, v) => c.SmallProvider = v, "deepseek"),

            P("BaseUrl",      "WAYCODER_BASE_URL",        null,
              "API 地址", "🤖 模型", "API 端点 URL",
              "text", null, 3,
              c => c.BaseUrl ?? "", (c, v) => c.BaseUrl = string.IsNullOrEmpty(v) ? null : v,
              skipIfEmpty: true),

            P("ApiKey",       "WAYCODER_API_KEY",         null,
              "API 密钥", "🤖 模型", "API 密钥 (已隐藏)",
              "secret", null, 4,
              c => c.ApiKey, (c, v) => c.ApiKey = v, "", skipIfEmpty: true),

            P("ReasoningEffort", "WAYCODER_REASONING_EFFORT", null,
              "推理深度", "🤖 模型", "推理模型的思考深度 (minimal/low/medium/high/max)，空=默认",
              "select", ["","minimal","low","medium","high","max"], 5,
              c => c.ReasoningEffort, (c, v) => c.ReasoningEffort = v, "", skipIfEmpty: true),

            P("WhisperModel", "WAYCODER_WHISPER_MODEL", null,
              "转录模型", "🎙 语音", "Whisper 转录模型（OpenAI 默认 whisper-1；Groq 可用 whisper-large-v3）",
              "text", null, 0,
              c => c.WhisperModel, (c, v) => c.WhisperModel = v, "whisper-1"),

            P("WhisperBaseUrl", "WAYCODER_WHISPER_BASE_URL", null,
              "转录 API 地址", "🎙 语音", "Whisper 转录 API 根地址（空=默认 https://api.openai.com）",
              "text", null, 1,
              c => c.WhisperBaseUrl ?? "", (c, v) => c.WhisperBaseUrl = string.IsNullOrEmpty(v) ? null : v,
              skipIfEmpty: true),

            P("WhisperApiKey", "WAYCODER_WHISPER_API_KEY", null,
              "转录 API Key", "🎙 语音", "Whisper 转录 API Key（空=回退到主 API Key）",
              "secret", null, 2,
              c => c.WhisperApiKey, (c, v) => c.WhisperApiKey = v, "", skipIfEmpty: true),

            // ── 参数 ──
            P("MaxTokens",        "WAYCODER_MAX_TOKENS",        null,
              "最大 Token", "⚙ 参数", "每次请求最大 Token 数",
              "number", null, 0,
              c => c.MaxTokens.ToString(), (c, v) => c.MaxTokens = Math.Clamp(int.Parse(v), 512, 65536), "32768"),

            P("Temperature",      "WAYCODER_TEMPERATURE",       null,
              "温度", "⚙ 参数", "0=精确 1=创意",
              "number", null, 1,
              c => c.Temperature.ToString("F1"), (c, v) => c.Temperature = float.Parse(v, System.Globalization.CultureInfo.InvariantCulture), "0.1"),

            P("MaxContextTokens", "WAYCODER_MAX_CONTEXT",       null,
              "上下文窗口", "⚙ 参数", "上下文窗口大小（未知模型兜底，默认 128K）",
              "number", null, 2,
              c => c.MaxContextTokens.ToString(), (c, v) => c.MaxContextTokens = int.Parse(v), "131072"),

            P("ToolTimeoutSec",   "WAYCODER_TOOL_TIMEOUT",      null,
              "工具超时 (秒)", "⚙ 参数", "Bash 等工具执行超时，默认 120 秒",
              "number", null, 3,
              c => c.ToolTimeoutSec.ToString(), (c, v) => c.ToolTimeoutSec = int.Parse(v), "120"),

            P("AllowedTools",    "WAYCODER_ALLOWED_TOOLS",     null,
              "工具白名单", "🔒 安全", "逗号分隔的工具名列表，仅允许这些工具可用（空=全部允许）",
              "text", null, 4,
              c => c.AllowedTools, (c, v) => c.AllowedTools = v, ""),

            P("DisabledTools",   "WAYCODER_DISABLED_TOOLS",    null,
              "工具黑名单", "🔒 安全", "逗号分隔的工具名列表，禁止这些工具（空=不禁用）",
              "text", null, 5,
              c => c.DisabledTools, (c, v) => c.DisabledTools = v, ""),

            P("LintTimeoutSec",   "WAYCODER_LINT_TIMEOUT",      null,
              "Lint 超时 (秒)", "⚙ 参数", "Lint 检查超时，默认 60 秒（大项目可调大）",
              "number", null, 4,
              c => c.LintTimeoutSec.ToString(), (c, v) => c.LintTimeoutSec = int.Parse(v), "60"),

            P("SubAgentMaxDepth", "WAYCODER_SUBAGENT_DEPTH",    null,
              "子智能体深度", "🤖 模型", "子智能体最大递归层数，1=单层 5=最深",
              "number", null, 4,
              c => c.SubAgentMaxDepth.ToString(),
              (c, v) => c.SubAgentMaxDepth = Math.Clamp(int.Parse(v), 1, 5), "3"),

            P("SubAgentMaxParallel", "WAYCODER_SUBAGENT_MAX_PARALLEL", null,
              "子智能体并行数", "🤖 模型", "并行子任务数量上限",
              "number", null, 5,
              c => c.SubAgentMaxParallel.ToString(),
              (c, v) => c.SubAgentMaxParallel = Math.Clamp(int.Parse(v), 1, 10), "4"),

            P("SubAgentOutputMaxChars", "WAYCODER_SUBAGENT_OUTPUT_MAX_CHARS", null,
              "子智能体输出上限", "🤖 模型", "子智能体输出截断阈值（字符数），0=不截断",
              "number", null, 6,
              c => c.SubAgentOutputMaxChars.ToString(),
              (c, v) => c.SubAgentOutputMaxChars = Math.Max(0, int.Parse(v)), "5000"),

            P("SubAgentMaxRounds", "WAYCODER_SUBAGENT_MAX_ROUNDS", null,
              "子智能体轮次上限", "🤖 模型", "子智能体顶层最大工具调用轮次（每深一层减 5，下限 5）",
              "number", null, 7,
              c => c.SubAgentMaxRounds.ToString(),
              (c, v) => c.SubAgentMaxRounds = Math.Clamp(int.Parse(v), 5, 100), "20"),

            P("SubAgentParallelTotalMaxChars", "WAYCODER_SUBAGENT_PARALLEL_TOTAL_MAX_CHARS", null,
              "并行子智能体总输出上限", "🤖 模型", "并行子智能体聚合结果的总字符上限，0=不限制",
              "number", null, 8,
              c => c.SubAgentParallelTotalMaxChars.ToString(),
              (c, v) => c.SubAgentParallelTotalMaxChars = Math.Max(0, int.Parse(v)), "15000"),

            P("MaxRounds",     "WAYCODER_MAX_ROUNDS",         null,
              "最大对话轮次", "⚙ 参数", "每轮对话最大工具调用次数",
              "number", null, 5,
              c => c.MaxRounds.ToString(),
              (c, v) => c.MaxRounds = Math.Clamp(int.Parse(v), 5, 500), "50"),

            P("BashOutputMaxChars", "WAYCODER_BASH_OUTPUT_MAX_CHARS", null,
              "Bash 输出上限", "⚙ 参数", "Bash 输出截断阈值（字符数），0=不截断",
              "number", null, 6,
              c => c.BashOutputMaxChars.ToString(),
              (c, v) => c.BashOutputMaxChars = Math.Max(0, int.Parse(v)), "50000"),

            P("LlmHttpTimeoutSec", "WAYCODER_LLM_HTTP_TIMEOUT_SEC", null,
              "LLM 请求超时 (秒)", "⚙ 参数", "单次 HTTP 请求超时",
              "number", null, 7,
              c => c.LlmHttpTimeoutSec.ToString(),
              (c, v) => c.LlmHttpTimeoutSec = Math.Clamp(int.Parse(v), 10, 3600), "300"),

            P("LlmMaxRetries",    "WAYCODER_LLM_MAX_RETRIES",   null,
              "LLM 最大重试", "⚙ 参数", "HTTP 失败最大重试次数",
              "number", null, 8,
              c => c.LlmMaxRetries.ToString(),
              (c, v) => c.LlmMaxRetries = Math.Clamp(int.Parse(v), 0, 10), "5"),

            P("LlmConnectionTimeoutSec", "WAYCODER_LLM_CONNECTION_TIMEOUT_SEC", null,
              "LLM 连接超时 (秒)", "⚙ 参数", "HTTP 连接总超时",
              "number", null, 9,
              c => c.LlmConnectionTimeoutSec.ToString(),
              (c, v) => c.LlmConnectionTimeoutSec = Math.Clamp(int.Parse(v), 10, 3600), "300"),

            P("LlmRateLimitMaxWaitSec", "WAYCODER_LLM_RATE_LIMIT_MAX_WAIT_SEC", null,
              "LLM 限速最大等待 (秒)", "⚙ 参数", "429 限速后最大等待时间",
              "number", null, 10,
              c => c.LlmRateLimitMaxWaitSec.ToString(),
              (c, v) => c.LlmRateLimitMaxWaitSec = Math.Clamp(int.Parse(v), 10, 600), "120"),

            // ── 超时参数（集中管理） ──
            P("BackgroundTaskTimeoutSec", "WAYCODER_BG_TASK_TIMEOUT_SEC", null,
              "后台任务超时 (秒)", "⏱ 超时", "后台 Shell 任务最大运行时间",
              "number", null, 11,
              c => c.BackgroundTaskTimeoutSec.ToString(),
              (c, v) => c.BackgroundTaskTimeoutSec = Math.Clamp(int.Parse(v), 30, 3600), "600"),

            P("AutoTestTimeoutSec", "WAYCODER_AUTO_TEST_TIMEOUT_SEC", null,
              "自动测试超时 (秒)", "⏱ 超时", "Agent 自动跑测试的超时时间",
              "number", null, 12,
              c => c.AutoTestTimeoutSec.ToString(),
              (c, v) => c.AutoTestTimeoutSec = Math.Clamp(int.Parse(v), 5, 300), "30"),

            P("AutoTestDebounceSec", "WAYCODER_AUTO_TEST_DEBOUNCE_SEC", null,
              "自动测试防抖 (秒)", "⏱ 超时", "同项目自动测试最小间隔",
              "number", null, 13,
              c => c.AutoTestDebounceSec.ToString(),
              (c, v) => c.AutoTestDebounceSec = Math.Clamp(int.Parse(v), 10, 600), "60"),

            P("GitTimeoutSec", "WAYCODER_GIT_TIMEOUT_SEC", null,
              "Git 操作超时 (秒)", "⏱ 超时", "Git 命令执行超时",
              "number", null, 14,
              c => c.GitTimeoutSec.ToString(),
              (c, v) => c.GitTimeoutSec = Math.Clamp(int.Parse(v), 5, 120), "15"),

            P("KillTimeoutSec", "WAYCODER_KILL_TIMEOUT_SEC", null,
              "Kill 命令超时 (秒)", "⏱ 超时", "进程终止等待超时",
              "number", null, 15,
              c => c.KillTimeoutSec.ToString(),
              (c, v) => c.KillTimeoutSec = Math.Clamp(int.Parse(v), 3, 60), "10"),

            P("DownloadTimeoutSec", "WAYCODER_DOWNLOAD_TIMEOUT_SEC", null,
              "下载超时 (秒)", "⏱ 超时", "HTTP 下载默认超时",
              "number", null, 16,
              c => c.DownloadTimeoutSec.ToString(),
              (c, v) => c.DownloadTimeoutSec = Math.Clamp(int.Parse(v), 5, 600), "60"),

            P("HookTimeoutSec", "WAYCODER_HOOK_TIMEOUT_SEC", null,
              "Hook 超时 (秒)", "⏱ 超时", "事件钩子脚本执行超时",
              "number", null, 17,
              c => c.HookTimeoutSec.ToString(),
              (c, v) => c.HookTimeoutSec = Math.Clamp(int.Parse(v), 2, 120), "10"),

            P("AskUserTimeoutSec", "WAYCODER_ASK_USER_TIMEOUT_SEC", null,
              "用户等待超时 (秒)", "⏱ 超时", "弹窗问用户的最长等待时间",
              "number", null, 18,
              c => c.AskUserTimeoutSec.ToString(),
              (c, v) => c.AskUserTimeoutSec = Math.Clamp(int.Parse(v), 10, 600), "120"),

            P("RegexTimeoutSec", "WAYCODER_REGEX_TIMEOUT_SEC", null,
              "正则超时 (秒)", "⏱ 超时", "正则匹配超时保护",
              "number", null, 19,
              c => c.RegexTimeoutSec.ToString(),
              (c, v) => c.RegexTimeoutSec = Math.Clamp(int.Parse(v), 1, 30), "5"),

            P("FetchTimeoutSec", "WAYCODER_FETCH_TIMEOUT_SEC", null,
              "网页抓取超时 (秒)", "⏱ 超时", "URL 内容抓取超时",
              "number", null, 20,
              c => c.FetchTimeoutSec.ToString(),
              (c, v) => c.FetchTimeoutSec = Math.Clamp(int.Parse(v), 5, 120), "30"),

            P("ContextSnipRatio", "WAYCODER_CTX_SNIP_RATIO",   null,
              "上下文裁剪比例 (%)", "⚙ 参数", "工具输出裁剪触发比例",
              "number", null, 11,
              c => c.ContextSnipRatio.ToString(),
              (c, v) => c.ContextSnipRatio = Math.Clamp(int.Parse(v), 10, 80), "50"),

            P("ContextSummarizeRatio", "WAYCODER_CTX_SUMMARIZE_RATIO", null,
              "上下文摘要比例 (%)", "⚙ 参数", "LLM 摘要触发比例",
              "number", null, 12,
              c => c.ContextSummarizeRatio.ToString(),
              (c, v) => c.ContextSummarizeRatio = Math.Clamp(int.Parse(v), 20, 90), "70"),

            P("ContextCollapseRatio", "WAYCODER_CTX_COLLAPSE_RATIO", null,
              "上下文折叠比例 (%)", "⚙ 参数", "硬折叠触发比例",
              "number", null, 13,
              c => c.ContextCollapseRatio.ToString(),
              (c, v) => c.ContextCollapseRatio = Math.Clamp(int.Parse(v), 30, 99), "90"),

            P("ContextWindowLargeThreshold", "WAYCODER_CTX_LARGE_THRESHOLD", null,
              "大窗口阈值 (tokens)", "⚙ 参数", "超过此值视为大上下文窗口，用固定 buffer",
              "number", null, 14,
              c => c.ContextWindowLargeThreshold.ToString(),
              (c, v) => c.ContextWindowLargeThreshold = Math.Clamp(int.Parse(v), 50000, 1_000_000), "200000"),

            P("ContextWindowLargeBuffer", "WAYCODER_CTX_LARGE_BUFFER", null,
              "大窗口缓冲 (tokens)", "⚙ 参数", "大窗口剩余低于此值触发自动摘要",
              "number", null, 15,
              c => c.ContextWindowLargeBuffer.ToString(),
              (c, v) => c.ContextWindowLargeBuffer = Math.Clamp(int.Parse(v), 5000, 100_000), "20000"),

            P("ContextWindowSmallRatio", "WAYCODER_CTX_SMALL_RATIO", null,
              "小窗口摘要比例", "⚙ 参数", "小窗口剩余比例低于此值触发自动摘要 (0.1-0.5)",
              "number", null, 16,
              c => c.ContextWindowSmallRatio.ToString("F2"),
              (c, v) => c.ContextWindowSmallRatio = Math.Clamp(double.Parse(v, System.Globalization.CultureInfo.InvariantCulture), 0.1, 0.5), "0.2"),

            P("AutoContinueAfterSummarize", "WAYCODER_AUTO_CONTINUE", null,
              "自动继续", "⚙ 参数", "摘要后自动注入继续提示（Crush 风格）",
              "select", ["false","true"], 17,
              c => c.AutoContinueAfterSummarize.ToString().ToLowerInvariant(),
              (c, v) => c.AutoContinueAfterSummarize = bool.Parse(v), "true"),

            P("MaxAutoRequeue", "WAYCODER_MAX_REQUEUE", null,
              "自动续跑次数", "⚙ 参数", "撞 MaxRounds 上限后自动压缩+续跑的次数（0=关闭）",
              "number", null, 18,
              c => c.MaxAutoRequeue.ToString(), (c, v) => c.MaxAutoRequeue = Math.Clamp(int.Parse(v), 0, 20), "3"),

            P("EconomyMode", "WAYCODER_ECONOMY", null,
              "省 Token 模式", "💰 计费", "关=完整 / 开=精简+更早压缩 / 自动=按复杂度调节 / 极致=尽量不注入",
              "select", ["off","auto","on","extreme"], 20,
              c => c.EconomyMode.ToString().ToLowerInvariant(),
              (c, v) => c.EconomyMode = v.ToLowerInvariant() switch
              {
                  "auto" => EconomyMode.Auto,
                  "on" => EconomyMode.On,
                  "extreme" => EconomyMode.Extreme,
                  _ => EconomyMode.Off,
              }, "off"),

            P("EconomyPriority", "WAYCODER_ECONOMY_PRIORITY", null,
              "自动模式优先级", "💰 计费", "自动模式下收紧策略：质量优先/均衡/费用优先",
              "select", ["quality","balanced","cost"], 21,
              c => c.EconomyPriority.ToString().ToLowerInvariant(),
              (c, v) => c.EconomyPriority = v.ToLowerInvariant() switch
              {
                  "balanced" => EconomyPriority.Balanced,
                  "cost" => EconomyPriority.Cost,
                  _ => EconomyPriority.Quality,
              }, "quality"),
            P("EconomySnipRatio", "WAYCODER_ECONOMY_SNIP_RATIO", null,
              "裁剪阈值 %", "💰 计费", "省 token 模式：达到该上下文占比即裁剪工具输出",
              "number", null, 22,
              c => c.EconomySnipRatio.ToString(), (c, v) => c.EconomySnipRatio = Math.Clamp(int.Parse(v), 10, 60), "35"),
            P("EconomySummarizeRatio", "WAYCODER_ECONOMY_SUMMARIZE_RATIO", null,
              "摘要阈值 %", "💰 计费", "省 token 模式：达到该占比即 LLM 摘要旧对话",
              "number", null, 23,
              c => c.EconomySummarizeRatio.ToString(), (c, v) => c.EconomySummarizeRatio = Math.Clamp(int.Parse(v), 30, 80), "55"),
            P("EconomyCollapseRatio", "WAYCODER_ECONOMY_COLLAPSE_RATIO", null,
              "硬折叠阈值 %", "💰 计费", "省 token 模式：达到该占比即硬折叠上下文",
              "number", null, 24,
              c => c.EconomyCollapseRatio.ToString(), (c, v) => c.EconomyCollapseRatio = Math.Clamp(int.Parse(v), 50, 95), "75"),
            P("EconomySnipChars", "WAYCODER_ECONOMY_SNIP_CHARS", null,
              "工具输出裁剪字符", "💰 计费", "省 token 模式：单条工具输出超过即截断（保留首尾）",
              "number", null, 25,
              c => c.EconomySnipChars.ToString(), (c, v) => c.EconomySnipChars = Math.Clamp(int.Parse(v), 200, 8000), "2000"),
            P("EconomyMaxTokens", "WAYCODER_ECONOMY_MAX_TOKENS", null,
              "单次输出上限", "💰 计费", "省 token 模式：单次请求 max_tokens 上限",
              "number", null, 26,
              c => c.EconomyMaxTokens.ToString(), (c, v) => c.EconomyMaxTokens = Math.Clamp(int.Parse(v), 512, 32768), "8192"),
            P("EconomyComplexRounds", "WAYCODER_ECONOMY_COMPLEX_ROUNDS", null,
              "复杂任务判定轮数", "💰 计费", "自动模式：任务达到此轮数视为完全复杂（保质量）",
              "number", null, 27,
              c => c.EconomyComplexRounds.ToString(), (c, v) => c.EconomyComplexRounds = Math.Clamp(int.Parse(v), 5, 100), "30"),
            P("SnipCharsNormal", "WAYCODER_SNIP_CHARS_NORMAL", null,
              "工具输出裁剪(正常)", "💰 计费", "正常模式：单条工具输出超过即截断（保留首尾）",
              "number", null, 28,
              c => c.SnipCharsNormal.ToString(), (c, v) => c.SnipCharsNormal = Math.Clamp(int.Parse(v), 200, 16000), "4000"),
            P("TinyWindow", "WAYCODER_TINY_WINDOW", null,
              "Tiny 窗口", "💰 计费", "Tiny 模式实际上下文窗口（--tiny 指定）",
              "number", null, 29,
              c => c.TinyWindow.ToString(), (c, v) => c.TinyWindow = Math.Clamp(int.Parse(v), 1024, 262144), "4096"),

            P("FallbackChain", "WAYCODER_FALLBACK_CHAIN",     null,
              "回退模型链", "🤖 模型", "逗号分隔的备选模型列表",
              "text", null, 7,
              c => c.FallbackChain, (c, v) => c.FallbackChain = v,
              "deepseek-v4-flash,deepseek-v4-pro,gemini-2.0-flash,qwen-turbo,glm-4-flash,gpt-5.4-mini"),

            P("FallbackMaxBudget", "WAYCODER_FALLBACK_MAX_BUDGET", null,
              "回退预算 ($)", "💰 预算", "回退链最大花费，null=无限制",
              "number", null, 0,
              c => c.FallbackMaxBudget?.ToString("F2") ?? "",
              (c, v) => c.FallbackMaxBudget = string.IsNullOrEmpty(v) ? null : double.Parse(v, System.Globalization.CultureInfo.InvariantCulture),
              skipIfEmpty: true),

            // ── 预算 ──
            P("MaxBudgetUsd",     "WAYCODER_MAX_BUDGET_USD",    null,
              "预算上限 ($)", "💰 预算", "超支自动停止，留空=无限制",
              "number", null, 0,
              c => c.MaxBudgetUsd?.ToString("F2") ?? "",
              (c, v) => c.MaxBudgetUsd = string.IsNullOrEmpty(v) ? null : double.Parse(v, System.Globalization.CultureInfo.InvariantCulture),
              skipIfEmpty: true),

            // ── 系统 ──
            P("Provider",         "WAYCODER_PROVIDER",          null,
              "提供商", "🔧 系统", "API 提供商 (openai/deepseek/...)",
              "text", null, 0,
              c => c.Provider, (c, v) => c.Provider = v, "openai"),

            P("AutoGitCommit",    "WAYCODER_AUTO_COMMIT",       null,
              "Git 自动提交", "🔧 系统", "工具执行后自动 git commit",
              "select", ["false","true"], 1,
              c => c.AutoGitCommit.ToString().ToLowerInvariant(),
              (c, v) => c.AutoGitCommit = bool.Parse(v), "false"),

            P("WatchMode",        "WAYCODER_WATCH",             null,
              "Watch 模式", "🔧 系统", "监听外部编辑器 AI! 注释自动触发 Agent",
              "select", ["false","true"], 2,
              c => c.WatchMode.ToString().ToLowerInvariant(),
              (c, v) => c.WatchMode = bool.Parse(v), "false"),

            P("WatchExtensions",  "WAYCODER_WATCH_EXTENSIONS",  null,
              "Watch 扩展名", "🔧 系统", "监听的源文件扩展名（逗号分隔，默认 .cs .fs .py .js .ts .go .rs）",
              "text", null, 6,
              c => c.WatchExtensions,
              (c, v) => c.WatchExtensions = v, ".cs,.fs,.py,.js,.ts,.go,.rs"),

            P("WatchIgnoreDirs",  "WAYCODER_WATCH_IGNORE_DIRS",null,
              "Watch 忽略目录", "🔧 系统", "不监听的目录名（逗号分隔，默认 obj,bin,node_modules,.git）",
              "text", null, 7,
              c => c.WatchIgnoreDirs,
              (c, v) => c.WatchIgnoreDirs = v, "obj,bin,node_modules,.git"),

            P("PromptCaching",    "WAYCODER_PROMPT_CACHE",      null,
              "Prompt 缓存", "🔧 系统", "追踪系统提示词重复发送，/stats 展示节省",
              "select", ["false","true"], 3,
              c => c.PromptCaching.ToString().ToLowerInvariant(),
              (c, v) => c.PromptCaching = bool.Parse(v), "true"),

            P("SandboxLevel",     "WAYCODER_SANDBOX_LEVEL",     null,
              "沙箱级别", "🔧 系统", "suggest=确认 auto-edit=编自动 full-auto=全自动沙箱",
              "select", ["suggest","auto-edit","full-auto"], 4,
              c => c.SandboxLevel, (c, v) => c.SandboxLevel = v, "suggest"),

            P("EditorIndent",     "WAYCODER_EDITOR_INDENT",     null,
              "编辑器缩进", "🔧 系统", "Tab 键插入制表符(\\t)或 4 个空格",
              "select", ["tab","space"], 4,
              c => c.EditorIndent, (c, v) => c.EditorIndent = v, "tab"),

            P("EditorLint",       "WAYCODER_EDITOR_LINT",       null,
              "编辑器 Lint", "🔧 系统", "保存时自动运行 lint 检查并标注错误行",
              "select", ["false","true"], 5,
              c => c.EditorLint.ToString().ToLowerInvariant(),
              (c, v) => c.EditorLint = bool.Parse(v), "true"),

            P("DiffPreview",      "WAYCODER_DIFF_PREVIEW",      null,
              "Diff 预览", "🔧 系统", "写文件前展示差异并逐 hunk 确认（非交互模式自动跳过）",
              "select", ["false","true"], 6,
              c => c.DiffPreview.ToString().ToLowerInvariant(),
              (c, v) => c.DiffPreview = bool.Parse(v), "false"),

            P("WriteContentView", "WAYCODER_WRITE_CONTENT_VIEW", null,
              "写入内容展示", "🔧 系统", "write_file/edit_file/multiedit 完成后在聊天区内联展示写入内容（diff 格式：行号+标记；非交互模式自动跳过）",
              "select", ["false","true"], 8,
              c => c.WriteContentView.ToString().ToLowerInvariant(),
              (c, v) => c.WriteContentView = bool.Parse(v), "true"),

            P("MouseEnabled",      "WAYCODER_MOUSE",            null,
              "鼠标支持", "🔧 系统", "启用终端鼠标（点击/滚动/移动；终端不支持或误触时关闭）",
              "select", ["false","true"], 8,
              c => c.MouseEnabled.ToString().ToLowerInvariant(),
              (c, v) => c.MouseEnabled = bool.Parse(v), "true"),

            P("MaxChatMessages",   "WAYCODER_MAX_CHAT_MESSAGES", null,
              "聊天显示上限", "🔧 系统", "聊天区显示消息上限（100~10000），超过自动丢最旧（会话仍在、文件持久化，仅显示层裁剪保流畅）",
              "number", null, 8,
              c => c.MaxChatMessages.ToString(),
              (c, v) => c.MaxChatMessages = Math.Clamp(int.Parse(v), 100, 10_000), "1000"),

            P("MaxCodePreviewLines","WAYCODER_MAX_CODE_LINES",  null,
              "代码预览行数", "🔧 系统", "聊天代码块预览行数上限（10~1000），超过保留头尾中间折叠省略",
              "number", null, 8,
              c => c.MaxCodePreviewLines.ToString(),
              (c, v) => c.MaxCodePreviewLines = Math.Clamp(int.Parse(v), 10, 1000), "500"),

            P("DesktopNotifications", "WAYCODER_ENABLE_NOTIFICATIONS", null,
              "桌面通知", "🔧 系统", "Agent 完成/权限等待时发送桌面通知（默认关闭）",
              "select", ["false","true"], 7,
              c => c.DesktopNotifications.ToString().ToLowerInvariant(),
              (c, v) => c.DesktopNotifications = bool.Parse(v), "false"),

            P("MemoryRelevanceTopN", "WAYCODER_MEMORY_TOPN",    null,
              "记忆注入条数", "🔧 系统", "每次注入的最相关记忆数，0=关闭语义匹配",
              "number", null, 6,
              c => c.MemoryRelevanceTopN.ToString(),
              (c, v) => c.MemoryRelevanceTopN = Math.Clamp(int.Parse(v), 0, 20), "5"),

            P("EmbeddingEnabled",  "WAYCODER_EMBEDDING",       null,
              "向量嵌入", "🔧 系统", "启用语义向量嵌入搜索（需 API 支持 /v1/embeddings）",
              "select", ["false","true"], 7,
              c => c.EmbeddingEnabled.ToString().ToLowerInvariant(),
              (c, v) => c.EmbeddingEnabled = bool.Parse(v), "false"),

            P("EmbeddingModel",    "WAYCODER_EMBEDDING_MODEL", null,
              "嵌入模型", "🔧 系统", "向量嵌入模型名称",
              "text", null, 8,
              c => c.EmbeddingModel, (c, v) => c.EmbeddingModel = v, "text-embedding-3-small"),

            P("EmbeddingDimensions", "WAYCODER_EMBEDDING_DIMS", null,
              "嵌入维度", "🔧 系统", "向量维度（0=模型默认，如 text-embedding-3-small=1536）",
              "number", null, 9,
              c => c.EmbeddingDimensions.ToString(),
              (c, v) => c.EmbeddingDimensions = Math.Clamp(int.Parse(v), 0, 4096), "0"),

            P("TeamMemoryEnabled", "WAYCODER_TEAM_MEMORY",     null,
              "团队记忆共享", "🔧 系统", "通过 git 同步 .waycoder/memory/ 共享记忆（需仓库支持）",
              "select", ["false","true"], 10,
              c => c.TeamMemoryEnabled.ToString().ToLowerInvariant(),
              (c, v) => c.TeamMemoryEnabled = bool.Parse(v), "false"),

            P("TeamMemoryAutoSync", "WAYCODER_TEAM_AUTO_SYNC", null,
              "启动自动同步", "🔧 系统", "启动时自动 git pull 拉取团队共享记忆",
              "select", ["false","true"], 11,
              c => c.TeamMemoryAutoSync.ToString().ToLowerInvariant(),
              (c, v) => c.TeamMemoryAutoSync = bool.Parse(v), "true"),

            P("SandboxMaxMemoryMb", "WAYCODER_SANDBOX_MAX_MEMORY_MB", null,
              "沙箱最大内存 (MB)", "🔧 系统", "子进程最大内存，超限自动 kill",
              "number", null, 12,
              c => c.SandboxMaxMemoryMb.ToString(),
              (c, v) => c.SandboxMaxMemoryMb = Math.Clamp(int.Parse(v), 64, 65536), "1024"),

            P("SandboxMaxCpuSeconds", "WAYCODER_SANDBOX_MAX_CPU_SEC", null,
              "沙箱最大 CPU (秒)", "🔧 系统", "子进程最大 CPU 时间，超限自动 kill",
              "number", null, 13,
              c => c.SandboxMaxCpuSeconds.ToString(),
              (c, v) => c.SandboxMaxCpuSeconds = Math.Clamp(int.Parse(v), 5, 86400), "300"),

            P("SandboxAllowNetwork", "WAYCODER_SANDBOX_ALLOW_NETWORK", null,
              "沙箱网络", "🔧 系统", "允许沙箱子进程访问网络",
              "select", ["false","true"], 14,
              c => c.SandboxAllowNetwork.ToString().ToLowerInvariant(),
              (c, v) => c.SandboxAllowNetwork = bool.Parse(v), "false"),

            P("FileLockTimeoutSec", "WAYCODER_FILE_LOCK_TIMEOUT_SEC", null,
              "文件锁超时 (秒)", "🔧 系统", "防多 Agent 并发写冲突的锁超时",
              "number", null, 15,
              c => c.FileLockTimeoutSec.ToString(),
              (c, v) => c.FileLockTimeoutSec = Math.Clamp(int.Parse(v), 5, 600), "30"),

            // ── 界面 ──
            P("GuiTheme",         "WAYCODER_GUI_THEME",         null,
              "GUI 主题", "🎨 界面", "GUI 版深/浅色主题",
              "select", ["dark", "light"], 4,
              c => c.GuiTheme, (c, v) => c.GuiTheme = v, "dark"),

            P("ThemePreset",      "WAYCODER_THEME",             null,
              "界面主题", "🎨 界面", "预设配色方案，选中即生效",
              "select", WayCoder.UI.Tui.TuiTheme.PresetNames, 4,
              c => c.ThemePreset, (c, v) => c.ThemePreset = v, "黄金甲"),

            P("ColorScheme",      "WAYCODER_COLOR_SCHEME",      null,
              "配色方案", "🎨 界面", "预设配色 (覆盖下方颜色设置)",
              "select", ["default","ocean","forest","sunset","mono","cyberpunk"], 0,
              c => c.ColorScheme, (c, v) => { c.ColorScheme = v; ApplyColorScheme(c, v); }, "default"),

            P("BorderStyle",      "WAYCODER_BORDER_STYLE",      null,
              "边框类型", "🎨 界面", "对话框和面板的边框样式",
              "select", ["rounded","single","double","bold"], 1,
              c => c.BorderStyle, (c, v) => c.BorderStyle = v, "rounded"),

            P("BorderColor",      "WAYCODER_BORDER_COLOR",      null,
              "边框颜色", "🎨 界面", "ANSI 色号: 36=青 32=绿 33=黄 35=紫 34=蓝 37=白",
              "select", ["36","32","33","35","34","37"], 2,
              c => c.BorderColor, (c, v) => c.BorderColor = v, "36"),

            P("AccentColor",      "WAYCODER_ACCENT_COLOR",      null,
              "强调色", "🎨 界面", "标题和选中高亮的颜色",
              "select", ["36","32","33","35","34","37"], 3,
              c => c.AccentColor, (c, v) => c.AccentColor = v, "36"),

            P("ChatDisplayStyle", "WAYCODER_CHAT_STYLE",        null,
              "聊天显示风格", "🎨 界面", "detailed=全显示 auto=智能简洁=极简（隐藏工具详情）",
              "select", ["auto","detailed","concise"], 5,
              c => c.ChatDisplayStyle, (c, v) => c.ChatDisplayStyle = v, "auto"),

            P("MarkupUi", "WAYCODER_MARKUP_UI",                  null,
              "标记界面（实验）", "🎨 界面", "用 .tui 声明式标记重建主聊天界面（实验性，测试通过后翻默认）",
              "select", ["false","true"], 6,
              c => c.MarkupUi.ToString().ToLowerInvariant(),
              (c, v) => c.MarkupUi = bool.Parse(v), "false"),
        ];
    }

    // ════════════════════════════════════════════════════════════
    // Schema 便捷构造器
    // ════════════════════════════════════════════════════════════

    static ConfigProp P(string key, string envVar, string? oldEnvVar,
        string label, string category, string desc,
        string type, string[]? options, int order,
        Func<Config, string> get, Action<Config, string> set,
        string? defaultStr = null, bool skipIfEmpty = false)
        => new(key, envVar, oldEnvVar, label, category, desc, type, options, order, get, set, defaultStr, skipIfEmpty);

    // ════════════════════════════════════════════════════════════
    // 环境变量读取
    // ════════════════════════════════════════════════════════════

    static string? Env(string newName, string? oldName) =>
        Environment.GetEnvironmentVariable(newName)
        ?? (oldName != null ? Environment.GetEnvironmentVariable(oldName) : null);

    // ════════════════════════════════════════════════════════════
    // 设置界面元数据（从 Schema 自动生成）
    // ════════════════════════════════════════════════════════════

    public static List<SettingDef> SettingSchema() =>
        _schema.Select(p => new SettingDef(
            p.Key, p.Label, p.Category, p.Desc,
            p.Type, p.Options, p.EnvVar, p.Order,
            p.DefaultStr ?? ""
        )).ToList();

    // ════════════════════════════════════════════════════════════
    // 命令行读写（/config 命令共用，避免重复 switch）
    // ════════════════════════════════════════════════════════════

    /// <summary>按 Key 或 EnvVar 查找配置项（忽略大小写）。未知返回 null。</summary>
    internal static ConfigProp? FindProp(string key)
    {
        if (string.IsNullOrWhiteSpace(key)) return null;
        return _schema.FirstOrDefault(p =>
            string.Equals(p.Key, key, StringComparison.OrdinalIgnoreCase) ||
            string.Equals(p.EnvVar, key, StringComparison.OrdinalIgnoreCase));
    }

    /// <summary>读取当前值（字符串形式，走 Schema Getter）。未知返回 null。</summary>
    public static string? GetPropValue(string key) => FindProp(key)?.Getter(Instance);

    /// <summary>
    /// 设置配置值（走 Schema Setter，自动解析/钳制）。
    /// 成功返回 true；失败返回 false 并给出 error（select 类型含可选项提示）。
    /// </summary>
    public static bool TrySetPropValue(string key, string value, out string? error)
    {
        var p = FindProp(key);
        if (p == null)
        {
            error = $"未知设置项「{key}」。用 /config list 查看全部设置项。";
            return false;
        }

        // select 类型：校验可选项（忽略大小写）
        if (p.Type == "select" && p.Options is { Length: > 0 })
        {
            if (!p.Options.Contains(value, StringComparer.OrdinalIgnoreCase))
            {
                error = $"「{p.Label}」可选值: {string.Join(" / ", p.Options)}";
                return false;
            }
        }

        try
        {
            p.Setter(Instance, value);
            error = null;
            return true;
        }
        catch (Exception ex)
        {
            error = $"「{p.Label}」设置失败: {ex.Message}";
            return false;
        }
    }

    // ════════════════════════════════════════════════════════════
    // 保存到 .env 文件（从 Schema 自动生成）
    // ════════════════════════════════════════════════════════════

    public void SaveToEnvFile()
    {
        lock (SaveLock)
        {
            var envPath = FindEnvFile() ?? Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".waycoder", ".env");
            var dir = Path.GetDirectoryName(envPath);
            if (dir != null) Directory.CreateDirectory(dir);
            var lines = File.Exists(envPath) ? File.ReadAllLines(envPath).ToList() : [];

            foreach (var p in _schema)
            {
                // API Key 不写入 .env：密钥独立管理，走全局 ~/.waycoder/api_keys.json（一个服务商一个 key）
                if (p.Type == "secret") continue;
                var val = p.Getter(this);
                if (p.SkipIfEmpty && string.IsNullOrEmpty(val)) continue;
                if (p.DefaultStr != null && val == p.DefaultStr) continue;
                ApplyOrAppend(lines, p.EnvVar, val);
            }

            File.WriteAllLines(envPath, lines);
        }
    }

    // ════════════════════════════════════════════════════════════
    // 辅助方法
    // ════════════════════════════════════════════════════════════

    public static void ApplyColorScheme(Config config, string scheme)
    {
        switch (scheme.ToLowerInvariant())
        {
            case "ocean":    config.BorderColor = "34"; config.AccentColor = "34"; break;
            case "forest":   config.BorderColor = "32"; config.AccentColor = "32"; break;
            case "sunset":   config.BorderColor = "33"; config.AccentColor = "33"; break;
            case "mono":     config.BorderColor = "37"; config.AccentColor = "37"; break;
            case "cyberpunk": config.BorderColor = "35"; config.AccentColor = "36"; break;
            default:         config.BorderColor = "36"; config.AccentColor = "36"; break;
        }
        config.ColorScheme = scheme;
    }

    private static void ApplyOrAppend(List<string> lines, string key, string value)
    {
        for (int i = 0; i < lines.Count; i++)
        {
            if (lines[i].TrimStart().StartsWith(key + "=", StringComparison.OrdinalIgnoreCase))
            { lines[i] = key + "=" + value; return; }
        }
        lines.Add(key + "=" + value);
    }

    private static void LoadDotEnv()
    {
        var envPath = FindEnvFile();
        if (envPath == null) return;

        try
        {
            foreach (var line in File.ReadAllLines(envPath))
            {
                var trimmed = line.Trim();
                if (string.IsNullOrEmpty(trimmed) || trimmed.StartsWith('#')) continue;

                // 兼容 `export KEY=value` 写法的 .env（shell 脚本风格）——否则 KEY 会带上前缀字面量
                if (trimmed.StartsWith("export ", StringComparison.OrdinalIgnoreCase))
                    trimmed = trimmed["export ".Length..].TrimStart();

                var eqIdx = trimmed.IndexOf('=');
                if (eqIdx <= 0 || eqIdx >= trimmed.Length - 1) continue;

                var key = trimmed[..eqIdx].Trim();
                var value = trimmed[(eqIdx + 1)..].Trim();

                // 剥离引号外的行内注释（`KEY=value # 注释` 常见 shell 风格）——
                // 否则 value 带注释原文（如 `"deepseek-v4-flash" # 主模型` 整个当值），模型名非法
                int commentIdx = FindUnquotedComment(value);
                if (commentIdx >= 0) value = value[..commentIdx].TrimEnd();

                if ((value.StartsWith('"') && value.EndsWith('"'))
                    || (value.StartsWith('\'') && value.EndsWith('\'')))
                    value = value[1..^1];

                if (Environment.GetEnvironmentVariable(key) == null)
                    Environment.SetEnvironmentVariable(key, value);
            }
        }
        catch { /* 静默跳过无法读取的 .env 文件 */ }
    }

    /// <summary>定位 value 中引号外的行内注释起始（# 且前面是空白），无则返回 -1。</summary>
    private static int FindUnquotedComment(string value)
    {
        bool inSingle = false, inDouble = false;
        for (int i = 0; i < value.Length; i++)
        {
            char c = value[i];
            if (c == '\'' && !inDouble) inSingle = !inSingle;
            else if (c == '"' && !inSingle) inDouble = !inDouble;
            else if (c == '#' && !inSingle && !inDouble)
            {
                if (i == 0 || char.IsWhiteSpace(value[i - 1])) return i;
            }
        }
        return -1;
    }

    private static string? FindEnvFile()
    {
        var current = Directory.GetCurrentDirectory();
        var home = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);

        while (true)
        {
            var candidate = Path.Combine(current, ".env");
            if (File.Exists(candidate)) return candidate;
            if (current == home || current == Path.GetPathRoot(current) || string.IsNullOrEmpty(current))
                break;
            current = Path.GetDirectoryName(current)!;
        }
        return null;
    }
}
