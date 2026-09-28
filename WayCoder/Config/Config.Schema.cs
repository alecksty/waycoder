namespace WayCoder;

public partial class Config
{
    // ════════════════════════════════════════════════════════════
    // 单一 Schema 定义（新增配置项只加这里一行）
    // ════════════════════════════════════════════════════════════

    // ⚠ **刻意不是 `static readonly` 字段**：`Label` / `Category` / `Desc` 三栏是**文案**（要跟语言走），
    //   而 `static readonly` + 静态构造只在**类型初始化那一刻**求值一次 ⇒ 里面的 `L.Pick`
    //   会把语言**冻在首次访问时的值**上（公理 A3）。本仓在 `RepoMapGenerator` 上真踩过这个坑：
    //   进程级缓存把语言冻死 ⇒ 切完语言文案不变，而且只在某些启动顺序下复现。
    //   改成**按语言缓存**的属性：换语言时重建一次，同语言重复访问直接复用同一份。
    static ConfigProp[]? _schemaCache;
    static UiLang _schemaLang;

    // 分类名（显示用）—— 全 Schema 共用，改一处即可。
    // ⚠ 必须是**表达式体属性**而不是 const/static readonly：后者会在类型初始化时求值一次、
    //   把语言冻在那一刻（公理 A3，本仓在 RepoMapGenerator 上真踩过）。
    static string CatSystem   => L.Pick("🔧 系统", "🔧 System");
    static string CatParams   => L.Pick("⚙️ 参数", "⚙️ Parameters");
    static string CatModel    => L.Pick("🤖 模型", "🤖 Models");
    static string CatBilling  => L.Pick("💰 计费", "💰 Billing");
    static string CatBudget   => L.Pick("💰 预算", "💰 Budget");
    static string CatSecurity => L.Pick("🔒 安全", "🔒 Security");
    static string CatSpeech   => L.Pick("🎙️ 语音", "🎙️ Speech");
    static string CatTest     => L.Pick("🧪 测试", "🧪 Testing");
    static string CatLearn    => L.Pick("🧠 学习", "🧠 Learning");
    static string CatUi       => L.Pick("🎨 界面", "🎨 Interface");
    static string CatTimeout  => L.Pick("⏱️ 超时", "⏱️ Timeouts");

    static ConfigProp[] Schema
    {
        get
        {
            if (_schemaCache != null && _schemaLang == L.Current) return _schemaCache;
            _schemaCache = [
            // ── 模型 ──
            P("Model",        "WAYCODER_MODEL",           null,
              L.Pick("大模型 (复杂任务)", "Main model (complex tasks)"),
              CatModel,
              L.Pick("架构/重构/调试/多文件", "Architecture / refactoring / debugging / multi-file"),
              "select", ["deepseek-chat","deepseek-v4-pro","gpt-5.4","gpt-5.5","deepseek-v4-flash","gpt-4o","gpt-4o-mini"], 0,
              c => c.Model, (c, v) => c.Model = v, "deepseek-v4-flash"),

            P("SmallModel",   "WAYCODER_SMALL_MODEL",     null,
              L.Pick("小模型 (简单任务)", "Small model (simple tasks)"),
              CatModel,
              L.Pick("补全/摘要/压缩 (便宜快速)", "Completion / summarization / compression (cheap and fast)"),
              "select", ["deepseek-chat","deepseek-v4-flash","gpt-5.4-mini","gpt-4o-mini","deepseek-v4-pro"], 1,
              c => c.SmallModel, (c, v) => c.SmallModel = v, "deepseek-v4-flash"),

            P("SmallProvider","WAYCODER_SMALL_PROVIDER",  null,
              L.Pick("小模型服务商", "Small model provider"),
              CatModel,
              L.Pick("小模型所属服务商 (deepseek/qwen/openai/...)", "Provider that owns the small model (deepseek/qwen/openai/...)"),
              "text", null, 2,
              c => c.SmallProvider, (c, v) => c.SmallProvider = v, "deepseek"),

            P("BaseUrl",      "WAYCODER_BASE_URL",        null,
              L.Pick("API 地址", "API base URL"),
              CatModel,
              L.Pick("API 端点 URL", "API endpoint URL"),
              "text", null, 3,
              c => c.BaseUrl ?? "", (c, v) => c.BaseUrl = string.IsNullOrEmpty(v) ? null : v,
              skipIfEmpty: true),

            P("ApiKey",       "WAYCODER_API_KEY",         null,
              L.Pick("API 密钥", "API key"),
              CatModel,
              L.Pick("API 密钥 (已隐藏)", "API key (hidden)"),
              "secret", null, 4,
              c => c.ApiKey, (c, v) => c.ApiKey = v, "", skipIfEmpty: true),

            P("ReasoningEffort", null,                        null,
              L.Pick("推理深度", "Reasoning effort"),
              CatModel,
              L.Pick("推理模型的思考深度 (minimal/low/medium/high/max)，空=默认", "Thinking depth for reasoning models (minimal/low/medium/high/max), empty = default"),
              "select", ["","minimal","low","medium","high","max"], 5,
              c => c.ReasoningEffort, (c, v) => c.ReasoningEffort = v, "", skipIfEmpty: true),

            P("WhisperModel", "WAYCODER_WHISPER_MODEL", null,
              L.Pick("转录模型", "Transcription model"),
              CatSpeech,
              L.Pick("Whisper 转录模型（OpenAI 默认 whisper-1；Groq 可用 whisper-large-v3）", "Whisper transcription model (OpenAI defaults to whisper-1; Groq supports whisper-large-v3)"),
              "text", null, 0,
              c => c.WhisperModel, (c, v) => c.WhisperModel = v, "whisper-1"),

            P("WhisperBaseUrl", "WAYCODER_WHISPER_BASE_URL", null,
              L.Pick("转录 API 地址", "Transcription API base URL"),
              CatSpeech,
              L.Pick("Whisper 转录 API 根地址（空=默认 https://api.openai.com）", "Whisper transcription API root URL (empty = default https://api.openai.com)"),
              "text", null, 1,
              c => c.WhisperBaseUrl ?? "", (c, v) => c.WhisperBaseUrl = string.IsNullOrEmpty(v) ? null : v,
              skipIfEmpty: true),

            P("WhisperApiKey", "WAYCODER_WHISPER_API_KEY", null,
              L.Pick("转录 API Key", "Transcription API key"),
              CatSpeech,
              L.Pick("Whisper 转录 API Key（空=回退到主 API Key）", "Whisper transcription API key (empty = fall back to the main API key)"),
              "secret", null, 2,
              c => c.WhisperApiKey, (c, v) => c.WhisperApiKey = v, "", skipIfEmpty: true),

            // ── 参数 ──
            P("MaxTokens",        null,                         null,
              L.Pick("最大 Token", "Max tokens"),
              CatParams,
              L.Pick("每次请求最大 Token 数", "Maximum number of tokens per request"),
              "number", null, 0,
              c => c.MaxTokens.ToString(), (c, v) => c.MaxTokens = Math.Clamp(int.Parse(v), 512, 65536), "32768"),

            P("Temperature",      null,                         null,
              L.Pick("温度", "Temperature"),
              CatParams,
              L.Pick("0=精确 1=创意", "0 = precise, 1 = creative"),
              "number", null, 1,
              c => c.Temperature.ToString("F1"), (c, v) => c.Temperature = float.Parse(v, System.Globalization.CultureInfo.InvariantCulture), "0.1"),

            P("MaxContextTokens", null,                         null,
              L.Pick("上下文窗口", "Context window"),
              CatParams,
              L.Pick("上下文窗口大小（未知模型兜底，默认 128K）", "Context window size (fallback for unknown models, default 128K)"),
              "number", null, 2,
              c => c.MaxContextTokens.ToString(), (c, v) => c.MaxContextTokens = int.Parse(v), "131072"),

            P("ToolTimeoutSec",   null,                         null,
              L.Pick("工具超时 (秒)", "Tool timeout (sec)"),
              CatParams,
              L.Pick("Bash 等工具执行超时，默认 120 秒", "Execution timeout for tools such as Bash, default 120 seconds"),
              "number", null, 3,
              c => c.ToolTimeoutSec.ToString(), (c, v) => c.ToolTimeoutSec = int.Parse(v), "120"),

            P("AllowedTools",    "WAYCODER_ALLOWED_TOOLS",     null,
              L.Pick("工具白名单", "Tool allowlist"),
              CatSecurity,
              L.Pick("逗号分隔的工具名列表，仅允许这些工具可用（空=全部允许）", "Comma-separated tool names; only these tools stay available (empty = all allowed)"),
              "text", null, 4,
              c => c.AllowedTools, (c, v) => c.AllowedTools = v, ""),

            P("DisabledTools",   "WAYCODER_DISABLED_TOOLS",    null,
              L.Pick("工具黑名单", "Tool denylist"),
              CatSecurity,
              L.Pick("逗号分隔的工具名列表，禁止这些工具（空=不禁用）", "Comma-separated tool names to disable (empty = none disabled)"),
              "text", null, 5,
              c => c.DisabledTools, (c, v) => c.DisabledTools = v, ""),

            P("PlanToolAllowList", null,                  null,
              L.Pick("计划模式工具集", "Plan mode toolset"),
              CatSecurity,
              L.Pick("计划(Plan)模式允许的工具（空=全部；危险工具不放行）", "Tools allowed in Plan mode (empty = all; dangerous tools are never allowed)"),
              "text", null, 6,
              c => c.PlanToolAllowList, (c, v) => c.PlanToolAllowList = v, ""),

            P("BuildToolAllowList", null,                   null,
              L.Pick("建造模式工具集", "Build mode toolset"),
              CatSecurity,
              L.Pick("建造(Build)模式允许的工具（空=全部）", "Tools allowed in Build mode (empty = all)"),
              "text", null, 7,
              c => c.BuildToolAllowList, (c, v) => c.BuildToolAllowList = v, ""),

            P("YoloToolAllowList", null,                  null,
              L.Pick("YOLO模式工具集", "YOLO mode toolset"),
              CatSecurity,
              L.Pick("YOLO 模式允许的工具（空=全部）", "Tools allowed in YOLO mode (empty = all)"),
              "text", null, 8,
              c => c.YoloToolAllowList, (c, v) => c.YoloToolAllowList = v, ""),

            P("LintTimeoutSec",   null,                         null,
              L.Pick("Lint 超时 (秒)", "Lint timeout (sec)"),
              CatParams,
              L.Pick("Lint 检查超时，默认 60 秒（大项目可调大）", "Lint check timeout, default 60 seconds (raise it for large projects)"),
              "number", null, 4,
              c => c.LintTimeoutSec.ToString(), (c, v) => c.LintTimeoutSec = int.Parse(v), "60"),

            P("SubAgentMaxDepth", null,                         null,
              L.Pick("子智能体深度", "Sub-agent depth"),
              CatModel,
              L.Pick("子智能体最大递归层数，1=单层 5=最深", "Maximum recursion depth for sub-agents, 1 = single level, 5 = deepest"),
              "number", null, 4,
              c => c.SubAgentMaxDepth.ToString(),
              (c, v) => c.SubAgentMaxDepth = Math.Clamp(int.Parse(v), 1, 5), "3"),

            P("SubAgentMaxParallel", null,                             null,
              L.Pick("子智能体并行数", "Sub-agent parallelism"),
              CatModel,
              L.Pick("并行子任务数量上限", "Maximum number of parallel subtasks"),
              "number", null, 5,
              c => c.SubAgentMaxParallel.ToString(),
              (c, v) => c.SubAgentMaxParallel = Math.Clamp(int.Parse(v), 1, 10), "4"),

            P("SubAgentOutputMaxChars", null,                                 null,
              L.Pick("子智能体输出上限", "Sub-agent output limit"),
              CatModel,
              L.Pick("子智能体输出截断阈值（字符数），0=不截断", "Sub-agent output truncation threshold in characters, 0 = no truncation"),
              "number", null, 6,
              c => c.SubAgentOutputMaxChars.ToString(),
              (c, v) => c.SubAgentOutputMaxChars = Math.Max(0, int.Parse(v)), "5000"),

            P("SubAgentMaxRounds", null,                           null,
              L.Pick("子智能体轮次上限", "Sub-agent round limit"),
              CatModel,
              L.Pick("子智能体顶层最大工具调用轮次（每深一层减 5，下限 5）", "Maximum tool-call rounds at the top level for sub-agents (minus 5 per level deeper, floor 5)"),
              "number", null, 7,
              c => c.SubAgentMaxRounds.ToString(),
              (c, v) => c.SubAgentMaxRounds = Math.Clamp(int.Parse(v), 5, 100), "20"),

            P("SubAgentParallelTotalMaxChars", null,                                         null,
              L.Pick("并行子智能体总输出上限", "Parallel sub-agent total output limit"),
              CatModel,
              L.Pick("并行子智能体聚合结果的总字符上限，0=不限制", "Total character limit for the aggregated results of parallel sub-agents, 0 = unlimited"),
              "number", null, 8,
              c => c.SubAgentParallelTotalMaxChars.ToString(),
              (c, v) => c.SubAgentParallelTotalMaxChars = Math.Max(0, int.Parse(v)), "15000"),

            P("SubAgentRetryCount", null,                            null,
              L.Pick("子智能体重试次数", "Sub-agent retry count"),
              CatModel,
              L.Pick("子智能体失败（返回错误）时的自动重试次数，0=不重试", "Automatic retries when a sub-agent fails (returns an error), 0 = no retry"),
              "number", null, 9,
              c => c.SubAgentRetryCount.ToString(),
              (c, v) => c.SubAgentRetryCount = Math.Clamp(int.Parse(v), 0, 5), "1"),

            P("SubAgentMaxTotalTasks", null,                                null,
              L.Pick("子智能体总任务上限", "Sub-agent total task limit"),
              CatModel,
              L.Pick("子智能体 tasks 数组硬上限（超出并行数的部分自动分批串行，总数超此上限报错）", "Hard limit on the sub-agent tasks array (whatever exceeds the parallelism is batched and run serially; exceeding this total is an error)"),
              "number", null, 10,
              c => c.SubAgentMaxTotalTasks.ToString(),
              (c, v) => c.SubAgentMaxTotalTasks = Math.Clamp(int.Parse(v), 1, 1000), "100"),

            P("MaxRounds",     null,                          null,
              L.Pick("最大对话轮次", "Max conversation rounds"),
              CatParams,
              L.Pick("每轮对话最大工具调用次数", "Maximum tool calls per conversation round"),
              "number", null, 5,
              c => c.MaxRounds.ToString(),
              (c, v) => c.MaxRounds = Math.Clamp(int.Parse(v), 5, 500), "200"),

            P("BashOutputMaxChars", null,                             null,
              L.Pick("Bash 输出上限", "Bash output limit"),
              CatParams,
              L.Pick("Bash 输出截断阈值（字符数），0=不截断", "Bash output truncation threshold in characters, 0 = no truncation"),
              "number", null, 6,
              c => c.BashOutputMaxChars.ToString(),
              (c, v) => c.BashOutputMaxChars = Math.Max(0, int.Parse(v)), "50000"),

            P("LlmHttpTimeoutSec", null,                            null,
              L.Pick("LLM 请求超时 (秒)", "LLM request timeout (sec)"),
              CatParams,
              L.Pick("单次 HTTP 请求超时", "Timeout for a single HTTP request"),
              "number", null, 7,
              c => c.LlmHttpTimeoutSec.ToString(),
              (c, v) => c.LlmHttpTimeoutSec = Math.Clamp(int.Parse(v), 10, 3600), "300"),

            P("LlmMaxRetries",    null,                         null,
              L.Pick("LLM 最大重试", "LLM max retries"),
              CatParams,
              L.Pick("HTTP 失败最大重试次数", "Maximum number of retries after an HTTP failure"),
              "number", null, 8,
              c => c.LlmMaxRetries.ToString(),
              (c, v) => c.LlmMaxRetries = Math.Clamp(int.Parse(v), 0, 10), "5"),

            P("LlmConnectionTimeoutSec", null,                                  null,
              L.Pick("LLM 连接超时 (秒)", "LLM connect timeout (sec)"),
              CatParams,
              L.Pick("HTTP 连接总超时", "Overall HTTP connection timeout"),
              "number", null, 9,
              c => c.LlmConnectionTimeoutSec.ToString(),
              (c, v) => c.LlmConnectionTimeoutSec = Math.Clamp(int.Parse(v), 10, 3600), "300"),

            P("LlmRateLimitMaxWaitSec", null,                                   null,
              L.Pick("LLM 限速最大等待 (秒)", "LLM rate-limit max wait (sec)"),
              CatParams,
              L.Pick("429 限速后最大等待时间", "Maximum wait time after a 429 rate limit"),
              "number", null, 10,
              c => c.LlmRateLimitMaxWaitSec.ToString(),
              (c, v) => c.LlmRateLimitMaxWaitSec = Math.Clamp(int.Parse(v), 10, 600), "120"),

            // ── 超时参数（集中管理） ──
            P("BackgroundTaskTimeoutSec", null,                           null,
              L.Pick("后台任务超时 (秒)", "Background task timeout (sec)"),
              CatTimeout,
              L.Pick("后台 Shell 任务最大运行时间", "Maximum runtime for background shell tasks"),
              "number", null, 11,
              c => c.BackgroundTaskTimeoutSec.ToString(),
              (c, v) => c.BackgroundTaskTimeoutSec = Math.Clamp(int.Parse(v), 30, 3600), "600"),

            P("AutoTestTimeoutSec", null,                             null,
              L.Pick("自动测试超时 (秒)", "Auto-test timeout (sec)"),
              CatTimeout,
              L.Pick("Agent 自动跑测试的超时时间", "Timeout for the agent's automatic test runs"),
              "number", null, 12,
              c => c.AutoTestTimeoutSec.ToString(),
              (c, v) => c.AutoTestTimeoutSec = Math.Clamp(int.Parse(v), 5, 300), "30"),

            P("AutoTestDebounceSec", null,                              null,
              L.Pick("自动测试防抖 (秒)", "Auto-test debounce (sec)"),
              CatTimeout,
              L.Pick("同项目自动测试最小间隔", "Minimum interval between automatic tests in the same project"),
              "number", null, 13,
              c => c.AutoTestDebounceSec.ToString(),
              (c, v) => c.AutoTestDebounceSec = Math.Clamp(int.Parse(v), 10, 600), "60"),

            P("TestCommand", null,                    null,
              L.Pick("指定测试命令", "Explicit test command"),
              CatTest,
              L.Pick("测试驱动修复：非空时优先用它而非自动探测，测试失败会硬绿判定直到通过", "Test-driven repair: when non-empty, prefer it over auto-detection; failing tests keep the run gated until they pass"),
              "text", null, 14,
              c => c.TestCommand, (c, v) => c.TestCommand = v, ""),

            P("VerifyBeforeDone", null,                          null,
              L.Pick("修完必验证", "Always verify after fixing"),
              CatTest,
              L.Pick("声明完成前若本轮改过源码但未跑过验证，强制收尾验证一次（防假修好了）", "Before declaring completion, if source files changed this round without verification, force one final verification pass (guards against a false done)"),
              "toggle", null, 15,
              c => c.VerifyBeforeDone.ToString().ToLowerInvariant(),
              (c, v) => c.VerifyBeforeDone = bool.Parse(v), "true"),

            P("GitTimeoutSec", null,                       null,
              L.Pick("Git 操作超时 (秒)", "Git operation timeout (sec)"),
              CatTimeout,
              L.Pick("Git 命令执行超时", "Git command execution timeout"),
              "number", null, 14,
              c => c.GitTimeoutSec.ToString(),
              (c, v) => c.GitTimeoutSec = Math.Clamp(int.Parse(v), 5, 120), "15"),

            P("KillTimeoutSec", null,                        null,
              L.Pick("Kill 命令超时 (秒)", "Kill command timeout (sec)"),
              CatTimeout,
              L.Pick("进程终止等待超时", "Timeout while waiting for a process to terminate"),
              "number", null, 15,
              c => c.KillTimeoutSec.ToString(),
              (c, v) => c.KillTimeoutSec = Math.Clamp(int.Parse(v), 3, 60), "10"),

            P("DownloadTimeoutSec", null,                            null,
              L.Pick("下载超时 (秒)", "Download timeout (sec)"),
              CatTimeout,
              L.Pick("HTTP 下载默认超时", "Default HTTP download timeout"),
              "number", null, 16,
              c => c.DownloadTimeoutSec.ToString(),
              (c, v) => c.DownloadTimeoutSec = Math.Clamp(int.Parse(v), 5, 600), "60"),

            P("HookTimeoutSec", null,                        null,
              L.Pick("Hook 超时 (秒)", "Hook timeout (sec)"),
              CatTimeout,
              L.Pick("事件钩子脚本执行超时", "Execution timeout for event hook scripts"),
              "number", null, 17,
              c => c.HookTimeoutSec.ToString(),
              (c, v) => c.HookTimeoutSec = Math.Clamp(int.Parse(v), 2, 120), "10"),

            P("AskUserTimeoutSec", null,                            null,
              L.Pick("用户等待超时 (秒)", "User prompt timeout (sec)"),
              CatTimeout,
              L.Pick("弹窗问用户的最长等待时间", "Maximum time to wait for the user to answer a prompt"),
              "number", null, 18,
              c => c.AskUserTimeoutSec.ToString(),
              (c, v) => c.AskUserTimeoutSec = Math.Clamp(int.Parse(v), 10, 600), "120"),

            P("RegexTimeoutSec", null,                         null,
              L.Pick("正则超时 (秒)", "Regex timeout (sec)"),
              CatTimeout,
              L.Pick("正则匹配超时保护", "Timeout protection for regex matching"),
              "number", null, 19,
              c => c.RegexTimeoutSec.ToString(),
              (c, v) => c.RegexTimeoutSec = Math.Clamp(int.Parse(v), 1, 30), "5"),

            P("FetchTimeoutSec", null,                         null,
              L.Pick("网页抓取超时 (秒)", "Fetch timeout (sec)"),
              CatTimeout,
              L.Pick("URL 内容抓取超时", "Timeout for fetching URL content"),
              "number", null, 20,
              c => c.FetchTimeoutSec.ToString(),
              (c, v) => c.FetchTimeoutSec = Math.Clamp(int.Parse(v), 5, 120), "30"),

            P("ContextSnipRatio", null,                        null,
              L.Pick("上下文裁剪比例 (%)", "Context snip ratio (%)"),
              CatParams,
              L.Pick("工具输出裁剪触发比例", "Share of the context that triggers tool-output snipping"),
              "number", null, 11,
              c => c.ContextSnipRatio.ToString(),
              (c, v) => c.ContextSnipRatio = Math.Clamp(int.Parse(v), 10, 80), "50"),

            P("ContextSummarizeRatio", null,                           null,
              L.Pick("上下文摘要比例 (%)", "Context summarize ratio (%)"),
              CatParams,
              L.Pick("LLM 摘要触发比例", "Share of the context that triggers LLM summarization"),
              "number", null, 12,
              c => c.ContextSummarizeRatio.ToString(),
              (c, v) => c.ContextSummarizeRatio = Math.Clamp(int.Parse(v), 20, 90), "70"),

            P("ContextCollapseRatio", null,                          null,
              L.Pick("上下文折叠比例 (%)", "Context collapse ratio (%)"),
              CatParams,
              L.Pick("硬折叠触发比例", "Share of the context that triggers hard collapse"),
              "number", null, 13,
              c => c.ContextCollapseRatio.ToString(),
              (c, v) => c.ContextCollapseRatio = Math.Clamp(int.Parse(v), 30, 99), "90"),

            P("ContextWindowLargeThreshold", null,                           null,
              L.Pick("大窗口阈值 (tokens)", "Large window threshold (tokens)"),
              CatParams,
              L.Pick("超过此值视为大上下文窗口，用固定 buffer", "Above this value the context counts as a large window and uses a fixed buffer"),
              "number", null, 14,
              c => c.ContextWindowLargeThreshold.ToString(),
              (c, v) => c.ContextWindowLargeThreshold = Math.Clamp(int.Parse(v), 50000, 1_000_000), "200000"),

            P("ContextWindowLargeBuffer", null,                        null,
              L.Pick("大窗口缓冲 (tokens)", "Large window buffer (tokens)"),
              CatParams,
              L.Pick("大窗口剩余低于此值触发自动摘要", "Auto-summarize when the remaining large window drops below this value"),
              "number", null, 15,
              c => c.ContextWindowLargeBuffer.ToString(),
              (c, v) => c.ContextWindowLargeBuffer = Math.Clamp(int.Parse(v), 5000, 100_000), "20000"),

            P("ContextWindowSmallRatio", null,                       null,
              L.Pick("小窗口摘要比例", "Small window summarize ratio"),
              CatParams,
              L.Pick("小窗口剩余比例低于此值触发自动摘要 (0.1-0.5)", "Auto-summarize when the remaining small-window ratio drops below this value (0.1-0.5)"),
              "number", null, 16,
              c => c.ContextWindowSmallRatio.ToString("F2"),
              (c, v) => c.ContextWindowSmallRatio = Math.Clamp(double.Parse(v, System.Globalization.CultureInfo.InvariantCulture), 0.1, 0.5), "0.2"),

            P("AutoContinueAfterSummarize", null,                     null,
              L.Pick("自动继续", "Auto continue"),
              CatParams,
              L.Pick("摘要后自动注入继续提示（Crush 风格）", "Automatically inject a continue prompt after summarization (Crush style)"),
              "select", ["false","true"], 17,
              c => c.AutoContinueAfterSummarize.ToString().ToLowerInvariant(),
              (c, v) => c.AutoContinueAfterSummarize = bool.Parse(v), "true"),

            P("MaxAutoRequeue", null,                   null,
              L.Pick("自动续跑次数", "Auto-requeue count"),
              CatParams,
              L.Pick("撞 MaxRounds 上限后自动压缩+续跑的次数（0=关闭）", "How many times to auto-compress and continue after hitting the MaxRounds limit (0 = off)"),
              "number", null, 18,
              c => c.MaxAutoRequeue.ToString(), (c, v) => c.MaxAutoRequeue = Math.Clamp(int.Parse(v), 0, 20), "3"),

            P("EconomyMode", "WAYCODER_ECONOMY", null,
              L.Pick("省 Token 模式", "Token-saving mode"),
              CatBilling,
              L.Pick("关=完整 / 开=精简+更早压缩 / 自动=按复杂度调节 / 极致=尽量不注入", "off = full / on = trimmed + earlier compression / auto = adapt to complexity / extreme = inject as little as possible"),
              "select", ["off","auto","on","extreme"], 20,
              c => c.EconomyMode.ToString().ToLowerInvariant(),
              (c, v) => c.EconomyMode = v.ToLowerInvariant() switch
              {
                  "auto" => EconomyMode.Auto,
                  "on" => EconomyMode.On,
                  "extreme" => EconomyMode.Extreme,
                  _ => EconomyMode.Off,
              }, "off"),

            P("EconomyPriority", null,                        null,
              L.Pick("自动模式优先级", "Auto mode priority"),
              CatBilling,
              L.Pick("自动模式下收紧策略：质量优先/均衡/费用优先", "Tightening strategy in auto mode: quality first / balanced / cost first"),
              "select", ["quality","balanced","cost"], 21,
              c => c.EconomyPriority.ToString().ToLowerInvariant(),
              (c, v) => c.EconomyPriority = v.ToLowerInvariant() switch
              {
                  "balanced" => EconomyPriority.Balanced,
                  "cost" => EconomyPriority.Cost,
                  _ => EconomyPriority.Quality,
              }, "quality"),
            P("EconomySnipRatio", null,                          null,
              L.Pick("裁剪阈值 %", "Snip threshold %"),
              CatBilling,
              L.Pick("省 token 模式：达到该上下文占比即裁剪工具输出", "Token-saving mode: snip tool output once this share of the context is reached"),
              "number", null, 22,
              c => c.EconomySnipRatio.ToString(), (c, v) => c.EconomySnipRatio = Math.Clamp(int.Parse(v), 10, 60), "35"),
            P("EconomySummarizeRatio", null,                               null,
              L.Pick("摘要阈值 %", "Summarize threshold %"),
              CatBilling,
              L.Pick("省 token 模式：达到该占比即 LLM 摘要旧对话", "Token-saving mode: summarize the old conversation with the LLM once this share is reached"),
              "number", null, 23,
              c => c.EconomySummarizeRatio.ToString(), (c, v) => c.EconomySummarizeRatio = Math.Clamp(int.Parse(v), 30, 80), "55"),
            P("EconomyCollapseRatio", null,                              null,
              L.Pick("硬折叠阈值 %", "Collapse threshold %"),
              CatBilling,
              L.Pick("省 token 模式：达到该占比即硬折叠上下文", "Token-saving mode: hard-collapse the context once this share is reached"),
              "number", null, 24,
              c => c.EconomyCollapseRatio.ToString(), (c, v) => c.EconomyCollapseRatio = Math.Clamp(int.Parse(v), 50, 95), "75"),
            P("EconomySnipChars", null,                          null,
              L.Pick("工具输出裁剪字符", "Tool output snip chars"),
              CatBilling,
              L.Pick("省 token 模式：单条工具输出超过即截断（保留首尾）", "Token-saving mode: truncate a single tool output beyond this many characters (keep head and tail)"),
              "number", null, 25,
              c => c.EconomySnipChars.ToString(), (c, v) => c.EconomySnipChars = Math.Clamp(int.Parse(v), 200, 8000), "2000"),
            P("EconomyMaxTokens", null,                          null,
              L.Pick("单次输出上限", "Per-request output limit"),
              CatBilling,
              L.Pick("省 token 模式：单次请求 max_tokens 上限", "Token-saving mode: max_tokens limit for a single request"),
              "number", null, 26,
              c => c.EconomyMaxTokens.ToString(), (c, v) => c.EconomyMaxTokens = Math.Clamp(int.Parse(v), 512, 32768), "8192"),
            P("EconomyComplexRounds", null,                              null,
              L.Pick("复杂任务判定轮数", "Complex-task round threshold"),
              CatBilling,
              L.Pick("自动模式：任务达到此轮数视为完全复杂（保质量）", "Auto mode: a task reaching this many rounds counts as fully complex (quality preserved)"),
              "number", null, 27,
              c => c.EconomyComplexRounds.ToString(), (c, v) => c.EconomyComplexRounds = Math.Clamp(int.Parse(v), 5, 100), "30"),
            P("SnipCharsNormal", null,                         null,
              L.Pick("工具输出裁剪(正常)", "Tool output snip (normal)"),
              CatBilling,
              L.Pick("正常模式：单条工具输出超过即截断（保留首尾）", "Normal mode: truncate a single tool output beyond this many characters (keep head and tail)"),
              "number", null, 28,
              c => c.SnipCharsNormal.ToString(), (c, v) => c.SnipCharsNormal = Math.Clamp(int.Parse(v), 200, 16000), "4000"),
            P("TinyWindow", null,                   null,
              L.Pick("Tiny 窗口", "Tiny window"),
              CatBilling,
              L.Pick("Tiny 模式实际上下文窗口（--tiny 指定）", "Actual context window in Tiny mode (set by --tiny)"),
              "number", null, 29,
              c => c.TinyWindow.ToString(), (c, v) => c.TinyWindow = Math.Clamp(int.Parse(v), 1024, 262144), "4096"),

            P("FallbackEnabled", null,                         null,
              L.Pick("回退链开关", "Fallback chain switch"),
              CatModel,
              L.Pick("模型失败时按回退链自动切换备选 connect（默认关：只用当前模型，失败即停）", "Automatically switch to the next connect in the fallback chain when a model fails (default off: use the current model only and stop on failure)"),
              "select", ["false","true"], 6,
              c => c.FallbackEnabled.ToString().ToLowerInvariant(),
              (c, v) => c.FallbackEnabled = bool.TryParse(v, out var b) && b, "false"),

            P("FallbackChain", null,                          null,
              L.Pick("回退模型链", "Fallback model chain"),
              CatModel,
              L.Pick("逗号分隔的备选模型列表", "Comma-separated list of fallback models"),
              "text", null, 7,
              c => c.FallbackChain, (c, v) => c.FallbackChain = v,
              "deepseek-v4-flash,deepseek-v4-pro,gemini-2.0-flash,qwen-turbo,glm-4-flash,gpt-5.4-mini"),

            P("FallbackMaxBudget", null,                           null,
              L.Pick("回退预算 ($)", "Fallback budget ($)"),
              CatBudget,
              L.Pick("回退链最大花费，null=无限制", "Maximum spend for the fallback chain, null = unlimited"),
              "number", null, 0,
              c => c.FallbackMaxBudget?.ToString("F2") ?? "",
              (c, v) => c.FallbackMaxBudget = string.IsNullOrEmpty(v) ? null : double.Parse(v, System.Globalization.CultureInfo.InvariantCulture),
              skipIfEmpty: true),

            // ── 预算 ──
            P("MaxBudgetUsd",     "WAYCODER_MAX_BUDGET_USD",    null,
              L.Pick("预算上限 ($)", "Budget limit ($)"),
              CatBudget,
              L.Pick("超支自动停止，留空=无限制", "Stop automatically when over budget, leave empty = unlimited"),
              "number", null, 0,
              c => c.MaxBudgetUsd?.ToString("F2") ?? "",
              (c, v) => c.MaxBudgetUsd = string.IsNullOrEmpty(v) ? null : double.Parse(v, System.Globalization.CultureInfo.InvariantCulture),
              skipIfEmpty: true),

            P("BudgetWarnPercent",null,                         null,
              L.Pick("预算预警阈值 (%)", "Budget warning threshold (%)"),
              CatBudget,
              L.Pick("花费达到预算此百分比时发出一次提醒（0=关闭）", "Warn once when spending reaches this percentage of the budget (0 = off)"),
              "number", null, 1,
              c => c.BudgetWarnPercent.ToString("F0"),
              (c, v) => c.BudgetWarnPercent = Math.Clamp(double.Parse(v, System.Globalization.CultureInfo.InvariantCulture), 0, 100), "80"),

            // ── 系统 ──
            P("Provider",         "WAYCODER_PROVIDER",          null,
              L.Pick("提供商", "Provider"),
              CatSystem,
              L.Pick("API 提供商 (openai/deepseek/...)", "API provider (openai/deepseek/...)"),
              "text", null, 0,
              c => c.Provider, (c, v) => c.Provider = v, "openai"),

            P("AutoGitCommit",    null,                         null,
              L.Pick("Git 自动提交", "Auto git commit"),
              CatSystem,
              L.Pick("工具执行后自动 git commit", "Run git commit automatically after tool execution"),
              "select", ["false","true"], 1,
              c => c.AutoGitCommit.ToString().ToLowerInvariant(),
              (c, v) => c.AutoGitCommit = bool.Parse(v), "false"),

            P("AutoCheckpoint",   null,                         null,
              L.Pick("写前自动快照", "Auto snapshot before write"),
              CatSystem,
              L.Pick("每轮对话首次写文件前自动创建文件备份检查点（改坏可 /timeline 回滚）", "Create a file backup checkpoint before the first write of each conversation round (roll back with /timeline if it breaks)"),
              "select", ["true","false"], 2,
              c => c.AutoCheckpoint.ToString().ToLowerInvariant(),
              (c, v) => c.AutoCheckpoint = bool.Parse(v), "true"),

            P("CheckpointMax", null,                      null,
              L.Pick("检查点保留上限", "Checkpoint retention limit"),
              CatSystem,
              L.Pick("超过上限自动删除最旧检查点（防磁盘无限增长）", "Delete the oldest checkpoints beyond this limit (keeps disk usage bounded)"),
              "number", null, 3,
              c => c.CheckpointMax.ToString(),
              (c, v) => c.CheckpointMax = Math.Clamp(int.Parse(v), 1, 1000), "50"),

            P("UpdateEnabled",    null,                         null,
              L.Pick("更新开关", "Update switch"),
              CatSystem,
              L.Pick("内网/离线部署：关闭后 /update、--update 不做网络请求", "Intranet/offline deployments: when off, /update and --update make no network requests"),
              "select", ["true","false"], 3,
              c => c.UpdateEnabled.ToString().ToLowerInvariant(),
              (c, v) => c.UpdateEnabled = bool.Parse(v), "true"),

            P("OllamaNumCtx",     null,                         null,
              L.Pick("Ollama num_ctx", "Ollama num_ctx"),
              CatSystem,
              L.Pick("本地 Ollama 显式上下文窗口（0=自动探测不发送）", "Explicit context window for local Ollama (0 = auto-detect and send nothing)"),
              "number", null, 4,
              c => c.OllamaNumCtx.ToString(),
              (c, v) => c.OllamaNumCtx = Math.Max(0, int.Parse(v)), "0"),

            P("WatchMode",        null,                         null,
              L.Pick("Watch 模式", "Watch mode"),
              CatSystem,
              L.Pick("监听外部编辑器 AI! 注释自动触发 Agent", "Watch for AI! comments from external editors and trigger the agent automatically"),
              "select", ["false","true"], 2,
              c => c.WatchMode.ToString().ToLowerInvariant(),
              (c, v) => c.WatchMode = bool.Parse(v), "false"),

            P("WatchExtensions",  null,                         null,
              L.Pick("Watch 扩展名", "Watch extensions"),
              CatSystem,
              L.Pick("监听的源文件扩展名（逗号分隔，默认 .cs .fs .py .js .ts .go .rs）", "Source file extensions to watch (comma-separated, default .cs .fs .py .js .ts .go .rs)"),
              "text", null, 6,
              c => c.WatchExtensions,
              (c, v) => c.WatchExtensions = v, ".cs,.fs,.py,.js,.ts,.go,.rs"),

            P("WatchIgnoreDirs",  null,                        null,
              L.Pick("Watch 忽略目录", "Watch ignored directories"),
              CatSystem,
              L.Pick("不监听的目录名（逗号分隔，默认 obj,bin,node_modules,.git）", "Directory names not to watch (comma-separated, default obj,bin,node_modules,.git)"),
              "text", null, 7,
              c => c.WatchIgnoreDirs,
              (c, v) => c.WatchIgnoreDirs = v, "obj,bin,node_modules,.git"),

            P("PromptCaching",    null,                         null,
              L.Pick("Prompt 缓存", "Prompt caching"),
              CatSystem,
              L.Pick("追踪系统提示词重复发送，/stats 展示节省", "Track repeated system prompt sends; /stats shows the savings"),
              "select", ["false","true"], 3,
              c => c.PromptCaching.ToString().ToLowerInvariant(),
              (c, v) => c.PromptCaching = bool.Parse(v), "true"),

            P("SandboxLevel",     null,                         null,
              L.Pick("沙箱级别", "Sandbox level"),
              CatSystem,
              L.Pick("suggest=确认 auto-edit=编自动 full-auto=全自动沙箱", "suggest = confirm, auto-edit = auto edit, full-auto = fully automatic sandbox"),
              "select", ["suggest","auto-edit","full-auto"], 4,
              c => c.SandboxLevel, (c, v) => c.SandboxLevel = v, "suggest"),

            P("EditorIndent",     null,                         null,
              L.Pick("编辑器缩进", "Editor indent"),
              CatSystem,
              L.Pick("Tab 键插入制表符(\\t)或 4 个空格", "Tab key inserts a tab character (\\t) or 4 spaces"),
              "select", ["tab","space"], 4,
              c => c.EditorIndent, (c, v) => c.EditorIndent = v, "tab"),

            P("EditorLint",       null,                         null,
              L.Pick("编辑器 Lint", "Editor lint"),
              CatSystem,
              L.Pick("保存时自动运行 lint 检查并标注错误行", "Run lint automatically on save and mark error lines"),
              "select", ["false","true"], 5,
              c => c.EditorLint.ToString().ToLowerInvariant(),
              (c, v) => c.EditorLint = bool.Parse(v), "true"),

            P("DiffPreview",      null,                         null,
              L.Pick("Diff 预览", "Diff preview"),
              CatSystem,
              L.Pick("写文件前展示差异并逐 hunk 确认（非交互模式自动跳过）", "Show the diff and confirm each hunk before writing files (skipped automatically in non-interactive mode)"),
              "select", ["false","true"], 6,
              c => c.DiffPreview.ToString().ToLowerInvariant(),
              (c, v) => c.DiffPreview = bool.Parse(v), "false"),

            P("WriteContentView", null,                          null,
              L.Pick("写入内容展示", "Show written content"),
              CatSystem,
              L.Pick("write_file/edit_file/multiedit 完成后在聊天区内联展示写入内容（diff 格式：行号+标记；非交互模式自动跳过）", "Show the written content inline in the chat after write_file/edit_file/multiedit completes (diff format: line numbers + markers; skipped automatically in non-interactive mode)"),
              "select", ["false","true"], 8,
              c => c.WriteContentView.ToString().ToLowerInvariant(),
              (c, v) => c.WriteContentView = bool.Parse(v), "true"),

            P("MouseEnabled",      "WAYCODER_MOUSE",            null,
              L.Pick("鼠标支持", "Mouse support"),
              CatSystem,
              L.Pick("启用终端鼠标（点击/滚动/移动；终端不支持或误触时关闭）", "Enable terminal mouse (click/scroll/move; turn off when the terminal does not support it or you hit it by accident)"),
              "select", ["false","true"], 8,
              c => c.MouseEnabled.ToString().ToLowerInvariant(),
              (c, v) => c.MouseEnabled = bool.Parse(v), "true"),

            P("MaxChatMessages",   null,                         null,
              L.Pick("聊天显示上限", "Chat message display limit"),
              CatSystem,
              L.Pick("聊天区显示消息上限（100~10000），超过自动丢最旧（会话仍在、文件持久化，仅显示层裁剪保流畅）", "Maximum messages shown in the chat area (100-10000); the oldest are dropped beyond it (the session and its files are still persisted, this only trims the display to keep it smooth)"),
              "number", null, 8,
              c => c.MaxChatMessages.ToString(),
              (c, v) => c.MaxChatMessages = Math.Clamp(int.Parse(v), 100, 10_000), "1000"),

            P("MaxChatLines",      null,                         null,
              L.Pick("聊天显示行数", "Chat line display limit"),
              CatSystem,
              L.Pick("聊天区总行数上限（50~20000，0=不限制），超过自动丢最旧显示项到 80% 低水位（会话仍在、文件持久化，仅显示层裁剪保流畅）", "Maximum total lines in the chat area (50-20000, 0 = unlimited); the oldest display items are dropped down to the 80% low-water mark beyond it (the session and its files are still persisted, this only trims the display to keep it smooth)"),
              "number", null, 8,
              c => c.MaxChatLines.ToString(),
              (c, v) => c.MaxChatLines = Math.Clamp(int.Parse(v), 0, 20_000), "500"),

            P("VmlExportFrame",    null,                         null,
              L.Pick("导出游戏画面", "Export game frame"),
              CatSystem,
              L.Pick("vml 工具跑完图形程序后，把画面导成 PNG 交给 AI 看（AI 看不见画面就只能盲写：「没崩」≠「画对了」）。关闭可省一次渲染+编码，约 70~110ms", "After the vml tool runs a graphics program, export the frame as a PNG for the AI to see (blind without it: not crashing is not the same as drawing correctly). Turning this off saves one render + encode, about 70-110ms"),
              "select", ["true","false"], 8,
              c => c.VmlExportFrame.ToString().ToLowerInvariant(),
              (c, v) => c.VmlExportFrame = bool.Parse(v), "true"),

            P("VmlFrameMaxSide",   null,                         null,
              L.Pick("画面最大边长", "Max frame side"),
              CatSystem,
              L.Pick("导出画面的最大边长像素（128~4096），超过则等比缩小；VML 窗口常规 320×480，正常不会触发", "Maximum side length in pixels of an exported frame (128-4096); larger frames are scaled down proportionally. VML windows are normally 320x480, so this rarely triggers"),
              "number", null, 8,
              c => c.VmlFrameMaxSide.ToString(),
              (c, v) => c.VmlFrameMaxSide = Math.Clamp(int.Parse(v), 128, 4096), "1024"),

            P("MaxCodePreviewLines",null,                       null,
              L.Pick("代码预览行数", "Code preview lines"),
              CatSystem,
              L.Pick("聊天代码块预览行数上限（10~1000），超过保留头尾中间折叠省略", "Maximum preview lines for chat code blocks (10-1000); beyond it the middle is folded away and only the head and tail are kept"),
              "number", null, 8,
              c => c.MaxCodePreviewLines.ToString(),
              (c, v) => c.MaxCodePreviewLines = Math.Clamp(int.Parse(v), 10, 1000), "500"),

            P("DesktopNotifications", null,                            null,
              L.Pick("桌面通知", "Desktop notifications"),
              CatSystem,
              L.Pick("Agent 完成/权限等待时发送桌面通知（默认关闭）", "Send a desktop notification when the agent finishes or waits for permission (off by default)"),
              "select", ["false","true"], 7,
              c => c.DesktopNotifications.ToString().ToLowerInvariant(),
              (c, v) => c.DesktopNotifications = bool.Parse(v), "false"),

            P("MemoryRelevanceTopN", null,                      null,
              L.Pick("记忆注入条数", "Memory injection count"),
              CatSystem,
              L.Pick("每次注入的最相关记忆数，0=关闭语义匹配", "Number of most relevant memories injected each time, 0 = disable semantic matching"),
              "number", null, 6,
              c => c.MemoryRelevanceTopN.ToString(),
              (c, v) => c.MemoryRelevanceTopN = Math.Clamp(int.Parse(v), 0, 20), "5"),

            P("EmbeddingEnabled",  null,                       null,
              L.Pick("向量嵌入", "Vector embeddings"),
              CatSystem,
              L.Pick("启用语义向量嵌入搜索（需 API 支持 /v1/embeddings）", "Enable semantic vector embedding search (requires an API that supports /v1/embeddings)"),
              "select", ["false","true"], 7,
              c => c.EmbeddingEnabled.ToString().ToLowerInvariant(),
              (c, v) => c.EmbeddingEnabled = bool.Parse(v), "false"),

            P("EmbeddingModel",    null,                       null,
              L.Pick("嵌入模型", "Embedding model"),
              CatSystem,
              L.Pick("向量嵌入模型名称", "Name of the vector embedding model"),
              "text", null, 8,
              c => c.EmbeddingModel, (c, v) => c.EmbeddingModel = v, "text-embedding-3-small"),

            P("EmbeddingDimensions", null,                      null,
              L.Pick("嵌入维度", "Embedding dimensions"),
              CatSystem,
              L.Pick("向量维度（0=模型默认，如 text-embedding-3-small=1536）", "Vector dimensions (0 = the model default, e.g. text-embedding-3-small = 1536)"),
              "number", null, 9,
              c => c.EmbeddingDimensions.ToString(),
              (c, v) => c.EmbeddingDimensions = Math.Clamp(int.Parse(v), 0, 4096), "0"),

            P("TeamMemoryEnabled", null,                       null,
              L.Pick("团队记忆共享", "Team memory sharing"),
              CatSystem,
              L.Pick("通过 git 同步 .waycoder/memory/ 共享记忆（需仓库支持）", "Sync shared memory under .waycoder/memory/ through git (requires repository support)"),
              "select", ["false","true"], 10,
              c => c.TeamMemoryEnabled.ToString().ToLowerInvariant(),
              (c, v) => c.TeamMemoryEnabled = bool.Parse(v), "false"),

            P("TeamMemoryAutoSync", null,                      null,
              L.Pick("启动自动同步", "Auto sync on startup"),
              CatSystem,
              L.Pick("启动时自动 git pull 拉取团队共享记忆", "Run git pull on startup to fetch shared team memory"),
              "select", ["false","true"], 11,
              c => c.TeamMemoryAutoSync.ToString().ToLowerInvariant(),
              (c, v) => c.TeamMemoryAutoSync = bool.Parse(v), "true"),

            P("TeachModeEnabled", null,                  null,
              L.Pick("教学模式", "Teaching mode"),
              CatLearn,
              L.Pick("AI 不只执行，还逐处解释为什么 + 结束时提问巩固（覆盖极简输出规则）", "The AI not only executes but explains the why at each step and asks a question at the end to reinforce it (overrides the terse-output rule)"),
              "select", ["false","true"], 12,
              c => c.TeachModeEnabled.ToString().ToLowerInvariant(),
              (c, v) => c.TeachModeEnabled = bool.Parse(v), "false"),

            P("RetroOnExitEnabled", null,                     null,
              L.Pick("退出自动复盘", "Auto retrospective on exit"),
              CatLearn,
              L.Pick("会话退出时自动复盘并提炼经验入知识库（需配置模型）", "On session exit, automatically review the session and distill lessons into the knowledge base (a model must be configured)"),
              "select", ["false","true"], 13,
              c => c.RetroOnExitEnabled.ToString().ToLowerInvariant(),
              (c, v) => c.RetroOnExitEnabled = bool.Parse(v), "false"),

            P("SandboxMaxMemoryMb", null,                             null,
              L.Pick("沙箱最大内存 (MB)", "Sandbox max memory (MB)"),
              CatSystem,
              L.Pick("子进程最大内存，超限自动 kill", "Maximum memory for child processes; they are killed when exceeded"),
              "number", null, 12,
              c => c.SandboxMaxMemoryMb.ToString(),
              (c, v) => c.SandboxMaxMemoryMb = Math.Clamp(int.Parse(v), 64, 65536), "1024"),

            P("SandboxMaxCpuSeconds", null,                           null,
              L.Pick("沙箱最大 CPU (秒)", "Sandbox max CPU (sec)"),
              CatSystem,
              L.Pick("子进程最大 CPU 时间，超限自动 kill", "Maximum CPU time for child processes; they are killed when exceeded"),
              "number", null, 13,
              c => c.SandboxMaxCpuSeconds.ToString(),
              (c, v) => c.SandboxMaxCpuSeconds = Math.Clamp(int.Parse(v), 5, 86400), "300"),

            P("SandboxAllowNetwork", null,                             null,
              L.Pick("沙箱网络", "Sandbox network"),
              CatSystem,
              L.Pick("允许沙箱子进程访问网络", "Allow sandboxed child processes to access the network"),
              "select", ["false","true"], 14,
              c => c.SandboxAllowNetwork.ToString().ToLowerInvariant(),
              (c, v) => c.SandboxAllowNetwork = bool.Parse(v), "false"),

            P("SandboxMode", null,                    null,
              L.Pick("沙箱边界", "Sandbox boundary"),
              CatSystem,
              L.Pick("边界轴（独立于权限）：off 无边界 / project 仅项目内写 / network-off 禁网络 / hard 仅项目内写+禁网络", "Boundary axis (independent of permissions): off = no boundary / project = write inside the project only / network-off = no network / hard = project-only writes plus no network"),
              "select", ["off","project","network-off","hard"], 15,
              c => c.SandboxMode switch
              {
                  WayCoder.SandboxMode.ProjectWrite => "project",
                  WayCoder.SandboxMode.NetworkOff => "network-off",
                  WayCoder.SandboxMode.Hard => "hard",
                  _ => "off",
              },
              (c, v) => c.SandboxMode = v.ToLowerInvariant() switch
              {
                  "project" => WayCoder.SandboxMode.ProjectWrite,
                  "network-off" => WayCoder.SandboxMode.NetworkOff,
                  "hard" => WayCoder.SandboxMode.Hard,
                  _ => WayCoder.SandboxMode.Off,
              }, "off"),

            P("FileLockTimeoutSec", null,                             null,
              L.Pick("文件锁超时 (秒)", "File lock timeout (sec)"),
              CatSystem,
              L.Pick("防多 Agent 并发写冲突的锁超时", "Lock timeout that prevents concurrent write conflicts between agents"),
              "number", null, 15,
              c => c.FileLockTimeoutSec.ToString(),
              (c, v) => c.FileLockTimeoutSec = Math.Clamp(int.Parse(v), 5, 600), "30"),

            // ── 界面 ──
            P("GuiTheme",         null,                         null,
              L.Pick("GUI 主题", "GUI theme"),
              CatUi,
              L.Pick("GUI 版深/浅色主题", "Dark/light theme for the GUI build"),
              "select", ["dark", "light"], 4,
              c => c.GuiTheme, (c, v) => c.GuiTheme = v, "dark"),

            P("ThemePreset",      null,                         null,
              L.Pick("界面主题", "Interface theme"),
              CatUi,
              L.Pick("预设配色方案，选中即生效", "Preset color scheme, applied as soon as it is selected"),
              "select", WayCoder.UI.Tui.TuiTheme.PresetNames, 4,
              c => c.ThemePreset, (c, v) => c.ThemePreset = v, "黄金甲"),

            P("ColorScheme",      null,                         null,
              L.Pick("配色方案", "Color scheme"),
              CatUi,
              L.Pick("预设配色 (覆盖下方颜色设置)", "Preset colors (overrides the color settings below)"),
              "select", ["default","ocean","forest","sunset","mono","cyberpunk"], 0,
              c => c.ColorScheme, (c, v) => { c.ColorScheme = v; ApplyColorScheme(c, v); }, "default"),

            P("BorderStyle",      null,                         null,
              L.Pick("边框类型", "Border style"),
              CatUi,
              L.Pick("对话框和面板的边框样式", "Border style for dialogs and panels"),
              "select", ["rounded","single","double","bold"], 1,
              c => c.BorderStyle, (c, v) => c.BorderStyle = v, "rounded"),

            P("BorderColor",      null,                         null,
              L.Pick("边框颜色", "Border color"),
              CatUi,
              L.Pick("ANSI 色号: 36=青 32=绿 33=黄 35=紫 34=蓝 37=白", "ANSI color code: 36 = cyan, 32 = green, 33 = yellow, 35 = magenta, 34 = blue, 37 = white"),
              "select", ["36","32","33","35","34","37"], 2,
              c => c.BorderColor, (c, v) => c.BorderColor = v, "36"),

            P("AccentColor",      null,                         null,
              L.Pick("强调色", "Accent color"),
              CatUi,
              L.Pick("标题和选中高亮的颜色", "Color for headings and selection highlights"),
              "select", ["36","32","33","35","34","37"], 3,
              c => c.AccentColor, (c, v) => c.AccentColor = v, "36"),

            P("ChatDisplayStyle", null,                         null,
              L.Pick("聊天显示风格", "Chat display style"),
              CatUi,
              L.Pick("detailed=全显示 auto=智能简洁=极简（隐藏工具详情）", "detailed = show everything, auto = smart, concise = minimal (tool details hidden)"),
              "select", ["auto","detailed","concise"], 5,
              c => c.ChatDisplayStyle, (c, v) => c.ChatDisplayStyle = v, "auto"),

            P("MarkupUi", null,                                  null,
              L.Pick("标记界面（实验）", "Markup UI (experimental)"),
              CatUi,
              L.Pick("用 .tui 声明式标记重建主聊天界面（实验性，测试通过后翻默认）", "Rebuild the main chat UI from declarative .tui markup (experimental; becomes the default once the tests pass)"),
              "select", ["false","true"], 6,
              c => c.MarkupUi.ToString().ToLowerInvariant(),
              (c, v) => c.MarkupUi = bool.Parse(v), "false"),
            ];
            _schemaLang = L.Current;
            return _schemaCache;
        }
    }

    // ════════════════════════════════════════════════════════════
    // Schema 便捷构造器
    // ════════════════════════════════════════════════════════════

    static ConfigProp P(string key, string? envVar, string? oldEnvVar,
        string label, string category, string desc,
        string type, string[]? options, int order,
        Func<Config, string> get, Action<Config, string> set,
        string? defaultStr = null, bool skipIfEmpty = false)
        => new(key, envVar, oldEnvVar, label, category, desc, type, options, order, get, set, defaultStr, skipIfEmpty);

    // ════════════════════════════════════════════════════════════
    // 环境变量读取
    // ════════════════════════════════════════════════════════════

    static string? Env(string? newName, string? oldName) =>
        newName != null && Environment.GetEnvironmentVariable(newName) is { } v ? v
        : (oldName != null ? Environment.GetEnvironmentVariable(oldName) : null);

    // ════════════════════════════════════════════════════════════
    // 设置界面元数据（从 Schema 自动生成）
    // ════════════════════════════════════════════════════════════

    public static List<SettingDef> SettingSchema() =>
        Schema.Select(p => new SettingDef(
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
        return Schema.FirstOrDefault(p =>
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
            error = L.Pick($"未知设置项「{key}」。用 /config list 查看全部设置项。",
                           $"Unknown setting \"{key}\". Use /config list to see them all.");
            return false;
        }

        // select 类型：校验可选项（忽略大小写）
        if (p.Type == "select" && p.Options is { Length: > 0 })
        {
            if (!p.Options.Contains(value, StringComparer.OrdinalIgnoreCase))
            {
                error = L.Pick($"「{p.Label}」可选值: {string.Join(" / ", p.Options)}",
                               $"\"{p.Label}\" accepts: {string.Join(" / ", p.Options)}");
                return false;
            }
        }

        try
        {
            // 显式写模型/连接字段（/config set、GUI/Web/MAUI 设置界面、TUI Settings 默认分支）
            // = 用户主动配置，解除会话加载的内存镜像标记（Reconcile 才会把新值收敛回 state）
            if (NonPersistedModelKeys.Contains(p.Key)) Instance.SessionModelMirror = false;
            p.Setter(Instance, value);
            error = null;
            return true;
        }
        catch (Exception ex)
        {
            error = L.Pick($"「{p.Label}」设置失败: {ex.Message}",
                           $"Failed to set \"{p.Label}\": {ex.Message}");
            return false;
        }
    }
}
