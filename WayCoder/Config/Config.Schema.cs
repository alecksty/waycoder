namespace WayCoder;

public partial class Config
{
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

            P("ReasoningEffort", null,                        null,
              "推理深度", "🤖 模型", "推理模型的思考深度 (minimal/low/medium/high/max)，空=默认",
              "select", ["","minimal","low","medium","high","max"], 5,
              c => c.ReasoningEffort, (c, v) => c.ReasoningEffort = v, "", skipIfEmpty: true),

            P("WhisperModel", "WAYCODER_WHISPER_MODEL", null,
              "转录模型", "🎙️ 语音", "Whisper 转录模型（OpenAI 默认 whisper-1；Groq 可用 whisper-large-v3）",
              "text", null, 0,
              c => c.WhisperModel, (c, v) => c.WhisperModel = v, "whisper-1"),

            P("WhisperBaseUrl", "WAYCODER_WHISPER_BASE_URL", null,
              "转录 API 地址", "🎙️ 语音", "Whisper 转录 API 根地址（空=默认 https://api.openai.com）",
              "text", null, 1,
              c => c.WhisperBaseUrl ?? "", (c, v) => c.WhisperBaseUrl = string.IsNullOrEmpty(v) ? null : v,
              skipIfEmpty: true),

            P("WhisperApiKey", "WAYCODER_WHISPER_API_KEY", null,
              "转录 API Key", "🎙️ 语音", "Whisper 转录 API Key（空=回退到主 API Key）",
              "secret", null, 2,
              c => c.WhisperApiKey, (c, v) => c.WhisperApiKey = v, "", skipIfEmpty: true),

            // ── 参数 ──
            P("MaxTokens",        null,                         null,
              "最大 Token", "⚙️ 参数", "每次请求最大 Token 数",
              "number", null, 0,
              c => c.MaxTokens.ToString(), (c, v) => c.MaxTokens = Math.Clamp(int.Parse(v), 512, 65536), "32768"),

            P("Temperature",      null,                         null,
              "温度", "⚙️ 参数", "0=精确 1=创意",
              "number", null, 1,
              c => c.Temperature.ToString("F1"), (c, v) => c.Temperature = float.Parse(v, System.Globalization.CultureInfo.InvariantCulture), "0.1"),

            P("MaxContextTokens", null,                         null,
              "上下文窗口", "⚙️ 参数", "上下文窗口大小（未知模型兜底，默认 128K）",
              "number", null, 2,
              c => c.MaxContextTokens.ToString(), (c, v) => c.MaxContextTokens = int.Parse(v), "131072"),

            P("ToolTimeoutSec",   null,                         null,
              "工具超时 (秒)", "⚙️ 参数", "Bash 等工具执行超时，默认 120 秒",
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

            P("PlanToolAllowList", null,                  null,
              "计划模式工具集", "🔒 安全", "计划(Plan)模式允许的工具（空=全部；危险工具不放行）",
              "text", null, 6,
              c => c.PlanToolAllowList, (c, v) => c.PlanToolAllowList = v, ""),

            P("BuildToolAllowList", null,                   null,
              "建造模式工具集", "🔒 安全", "建造(Build)模式允许的工具（空=全部）",
              "text", null, 7,
              c => c.BuildToolAllowList, (c, v) => c.BuildToolAllowList = v, ""),

            P("YoloToolAllowList", null,                  null,
              "YOLO模式工具集", "🔒 安全", "YOLO 模式允许的工具（空=全部）",
              "text", null, 8,
              c => c.YoloToolAllowList, (c, v) => c.YoloToolAllowList = v, ""),

            P("LintTimeoutSec",   null,                         null,
              "Lint 超时 (秒)", "⚙️ 参数", "Lint 检查超时，默认 60 秒（大项目可调大）",
              "number", null, 4,
              c => c.LintTimeoutSec.ToString(), (c, v) => c.LintTimeoutSec = int.Parse(v), "60"),

            P("SubAgentMaxDepth", null,                         null,
              "子智能体深度", "🤖 模型", "子智能体最大递归层数，1=单层 5=最深",
              "number", null, 4,
              c => c.SubAgentMaxDepth.ToString(),
              (c, v) => c.SubAgentMaxDepth = Math.Clamp(int.Parse(v), 1, 5), "3"),

            P("SubAgentMaxParallel", null,                             null,
              "子智能体并行数", "🤖 模型", "并行子任务数量上限",
              "number", null, 5,
              c => c.SubAgentMaxParallel.ToString(),
              (c, v) => c.SubAgentMaxParallel = Math.Clamp(int.Parse(v), 1, 10), "4"),

            P("SubAgentOutputMaxChars", null,                                 null,
              "子智能体输出上限", "🤖 模型", "子智能体输出截断阈值（字符数），0=不截断",
              "number", null, 6,
              c => c.SubAgentOutputMaxChars.ToString(),
              (c, v) => c.SubAgentOutputMaxChars = Math.Max(0, int.Parse(v)), "5000"),

            P("SubAgentMaxRounds", null,                           null,
              "子智能体轮次上限", "🤖 模型", "子智能体顶层最大工具调用轮次（每深一层减 5，下限 5）",
              "number", null, 7,
              c => c.SubAgentMaxRounds.ToString(),
              (c, v) => c.SubAgentMaxRounds = Math.Clamp(int.Parse(v), 5, 100), "20"),

            P("SubAgentParallelTotalMaxChars", null,                                         null,
              "并行子智能体总输出上限", "🤖 模型", "并行子智能体聚合结果的总字符上限，0=不限制",
              "number", null, 8,
              c => c.SubAgentParallelTotalMaxChars.ToString(),
              (c, v) => c.SubAgentParallelTotalMaxChars = Math.Max(0, int.Parse(v)), "15000"),

            P("SubAgentRetryCount", null,                            null,
              "子智能体重试次数", "🤖 模型", "子智能体失败（返回错误）时的自动重试次数，0=不重试",
              "number", null, 9,
              c => c.SubAgentRetryCount.ToString(),
              (c, v) => c.SubAgentRetryCount = Math.Clamp(int.Parse(v), 0, 5), "1"),

            P("SubAgentMaxTotalTasks", null,                                null,
              "子智能体总任务上限", "🤖 模型", "子智能体 tasks 数组硬上限（超出并行数的部分自动分批串行，总数超此上限报错）",
              "number", null, 10,
              c => c.SubAgentMaxTotalTasks.ToString(),
              (c, v) => c.SubAgentMaxTotalTasks = Math.Clamp(int.Parse(v), 1, 1000), "100"),

            P("MaxRounds",     null,                          null,
              "最大对话轮次", "⚙️ 参数", "每轮对话最大工具调用次数",
              "number", null, 5,
              c => c.MaxRounds.ToString(),
              (c, v) => c.MaxRounds = Math.Clamp(int.Parse(v), 5, 500), "50"),

            P("BashOutputMaxChars", null,                             null,
              "Bash 输出上限", "⚙️ 参数", "Bash 输出截断阈值（字符数），0=不截断",
              "number", null, 6,
              c => c.BashOutputMaxChars.ToString(),
              (c, v) => c.BashOutputMaxChars = Math.Max(0, int.Parse(v)), "50000"),

            P("LlmHttpTimeoutSec", null,                            null,
              "LLM 请求超时 (秒)", "⚙️ 参数", "单次 HTTP 请求超时",
              "number", null, 7,
              c => c.LlmHttpTimeoutSec.ToString(),
              (c, v) => c.LlmHttpTimeoutSec = Math.Clamp(int.Parse(v), 10, 3600), "300"),

            P("LlmMaxRetries",    null,                         null,
              "LLM 最大重试", "⚙️ 参数", "HTTP 失败最大重试次数",
              "number", null, 8,
              c => c.LlmMaxRetries.ToString(),
              (c, v) => c.LlmMaxRetries = Math.Clamp(int.Parse(v), 0, 10), "5"),

            P("LlmConnectionTimeoutSec", null,                                  null,
              "LLM 连接超时 (秒)", "⚙️ 参数", "HTTP 连接总超时",
              "number", null, 9,
              c => c.LlmConnectionTimeoutSec.ToString(),
              (c, v) => c.LlmConnectionTimeoutSec = Math.Clamp(int.Parse(v), 10, 3600), "300"),

            P("LlmRateLimitMaxWaitSec", null,                                   null,
              "LLM 限速最大等待 (秒)", "⚙️ 参数", "429 限速后最大等待时间",
              "number", null, 10,
              c => c.LlmRateLimitMaxWaitSec.ToString(),
              (c, v) => c.LlmRateLimitMaxWaitSec = Math.Clamp(int.Parse(v), 10, 600), "120"),

            // ── 超时参数（集中管理） ──
            P("BackgroundTaskTimeoutSec", null,                           null,
              "后台任务超时 (秒)", "⏱️ 超时", "后台 Shell 任务最大运行时间",
              "number", null, 11,
              c => c.BackgroundTaskTimeoutSec.ToString(),
              (c, v) => c.BackgroundTaskTimeoutSec = Math.Clamp(int.Parse(v), 30, 3600), "600"),

            P("AutoTestTimeoutSec", null,                             null,
              "自动测试超时 (秒)", "⏱️ 超时", "Agent 自动跑测试的超时时间",
              "number", null, 12,
              c => c.AutoTestTimeoutSec.ToString(),
              (c, v) => c.AutoTestTimeoutSec = Math.Clamp(int.Parse(v), 5, 300), "30"),

            P("AutoTestDebounceSec", null,                              null,
              "自动测试防抖 (秒)", "⏱️ 超时", "同项目自动测试最小间隔",
              "number", null, 13,
              c => c.AutoTestDebounceSec.ToString(),
              (c, v) => c.AutoTestDebounceSec = Math.Clamp(int.Parse(v), 10, 600), "60"),

            P("TestCommand", null,                    null,
              "指定测试命令", "🧪 测试", "测试驱动修复：非空时优先用它而非自动探测，测试失败会硬绿判定直到通过",
              "text", null, 14,
              c => c.TestCommand, (c, v) => c.TestCommand = v, ""),

            P("VerifyBeforeDone", null,                          null,
              "修完必验证", "🧪 测试", "声明完成前若本轮改过源码但未跑过验证，强制收尾验证一次（防假修好了）",
              "toggle", null, 15,
              c => c.VerifyBeforeDone.ToString().ToLowerInvariant(),
              (c, v) => c.VerifyBeforeDone = bool.Parse(v), "true"),

            P("GitTimeoutSec", null,                       null,
              "Git 操作超时 (秒)", "⏱️ 超时", "Git 命令执行超时",
              "number", null, 14,
              c => c.GitTimeoutSec.ToString(),
              (c, v) => c.GitTimeoutSec = Math.Clamp(int.Parse(v), 5, 120), "15"),

            P("KillTimeoutSec", null,                        null,
              "Kill 命令超时 (秒)", "⏱️ 超时", "进程终止等待超时",
              "number", null, 15,
              c => c.KillTimeoutSec.ToString(),
              (c, v) => c.KillTimeoutSec = Math.Clamp(int.Parse(v), 3, 60), "10"),

            P("DownloadTimeoutSec", null,                            null,
              "下载超时 (秒)", "⏱️ 超时", "HTTP 下载默认超时",
              "number", null, 16,
              c => c.DownloadTimeoutSec.ToString(),
              (c, v) => c.DownloadTimeoutSec = Math.Clamp(int.Parse(v), 5, 600), "60"),

            P("HookTimeoutSec", null,                        null,
              "Hook 超时 (秒)", "⏱️ 超时", "事件钩子脚本执行超时",
              "number", null, 17,
              c => c.HookTimeoutSec.ToString(),
              (c, v) => c.HookTimeoutSec = Math.Clamp(int.Parse(v), 2, 120), "10"),

            P("AskUserTimeoutSec", null,                            null,
              "用户等待超时 (秒)", "⏱️ 超时", "弹窗问用户的最长等待时间",
              "number", null, 18,
              c => c.AskUserTimeoutSec.ToString(),
              (c, v) => c.AskUserTimeoutSec = Math.Clamp(int.Parse(v), 10, 600), "120"),

            P("RegexTimeoutSec", null,                         null,
              "正则超时 (秒)", "⏱️ 超时", "正则匹配超时保护",
              "number", null, 19,
              c => c.RegexTimeoutSec.ToString(),
              (c, v) => c.RegexTimeoutSec = Math.Clamp(int.Parse(v), 1, 30), "5"),

            P("FetchTimeoutSec", null,                         null,
              "网页抓取超时 (秒)", "⏱️ 超时", "URL 内容抓取超时",
              "number", null, 20,
              c => c.FetchTimeoutSec.ToString(),
              (c, v) => c.FetchTimeoutSec = Math.Clamp(int.Parse(v), 5, 120), "30"),

            P("ContextSnipRatio", null,                        null,
              "上下文裁剪比例 (%)", "⚙️ 参数", "工具输出裁剪触发比例",
              "number", null, 11,
              c => c.ContextSnipRatio.ToString(),
              (c, v) => c.ContextSnipRatio = Math.Clamp(int.Parse(v), 10, 80), "50"),

            P("ContextSummarizeRatio", null,                           null,
              "上下文摘要比例 (%)", "⚙️ 参数", "LLM 摘要触发比例",
              "number", null, 12,
              c => c.ContextSummarizeRatio.ToString(),
              (c, v) => c.ContextSummarizeRatio = Math.Clamp(int.Parse(v), 20, 90), "70"),

            P("ContextCollapseRatio", null,                          null,
              "上下文折叠比例 (%)", "⚙️ 参数", "硬折叠触发比例",
              "number", null, 13,
              c => c.ContextCollapseRatio.ToString(),
              (c, v) => c.ContextCollapseRatio = Math.Clamp(int.Parse(v), 30, 99), "90"),

            P("ContextWindowLargeThreshold", null,                           null,
              "大窗口阈值 (tokens)", "⚙️ 参数", "超过此值视为大上下文窗口，用固定 buffer",
              "number", null, 14,
              c => c.ContextWindowLargeThreshold.ToString(),
              (c, v) => c.ContextWindowLargeThreshold = Math.Clamp(int.Parse(v), 50000, 1_000_000), "200000"),

            P("ContextWindowLargeBuffer", null,                        null,
              "大窗口缓冲 (tokens)", "⚙️ 参数", "大窗口剩余低于此值触发自动摘要",
              "number", null, 15,
              c => c.ContextWindowLargeBuffer.ToString(),
              (c, v) => c.ContextWindowLargeBuffer = Math.Clamp(int.Parse(v), 5000, 100_000), "20000"),

            P("ContextWindowSmallRatio", null,                       null,
              "小窗口摘要比例", "⚙️ 参数", "小窗口剩余比例低于此值触发自动摘要 (0.1-0.5)",
              "number", null, 16,
              c => c.ContextWindowSmallRatio.ToString("F2"),
              (c, v) => c.ContextWindowSmallRatio = Math.Clamp(double.Parse(v, System.Globalization.CultureInfo.InvariantCulture), 0.1, 0.5), "0.2"),

            P("AutoContinueAfterSummarize", null,                     null,
              "自动继续", "⚙️ 参数", "摘要后自动注入继续提示（Crush 风格）",
              "select", ["false","true"], 17,
              c => c.AutoContinueAfterSummarize.ToString().ToLowerInvariant(),
              (c, v) => c.AutoContinueAfterSummarize = bool.Parse(v), "true"),

            P("MaxAutoRequeue", null,                   null,
              "自动续跑次数", "⚙️ 参数", "撞 MaxRounds 上限后自动压缩+续跑的次数（0=关闭）",
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

            P("EconomyPriority", null,                        null,
              "自动模式优先级", "💰 计费", "自动模式下收紧策略：质量优先/均衡/费用优先",
              "select", ["quality","balanced","cost"], 21,
              c => c.EconomyPriority.ToString().ToLowerInvariant(),
              (c, v) => c.EconomyPriority = v.ToLowerInvariant() switch
              {
                  "balanced" => EconomyPriority.Balanced,
                  "cost" => EconomyPriority.Cost,
                  _ => EconomyPriority.Quality,
              }, "quality"),
            P("EconomySnipRatio", null,                          null,
              "裁剪阈值 %", "💰 计费", "省 token 模式：达到该上下文占比即裁剪工具输出",
              "number", null, 22,
              c => c.EconomySnipRatio.ToString(), (c, v) => c.EconomySnipRatio = Math.Clamp(int.Parse(v), 10, 60), "35"),
            P("EconomySummarizeRatio", null,                               null,
              "摘要阈值 %", "💰 计费", "省 token 模式：达到该占比即 LLM 摘要旧对话",
              "number", null, 23,
              c => c.EconomySummarizeRatio.ToString(), (c, v) => c.EconomySummarizeRatio = Math.Clamp(int.Parse(v), 30, 80), "55"),
            P("EconomyCollapseRatio", null,                              null,
              "硬折叠阈值 %", "💰 计费", "省 token 模式：达到该占比即硬折叠上下文",
              "number", null, 24,
              c => c.EconomyCollapseRatio.ToString(), (c, v) => c.EconomyCollapseRatio = Math.Clamp(int.Parse(v), 50, 95), "75"),
            P("EconomySnipChars", null,                          null,
              "工具输出裁剪字符", "💰 计费", "省 token 模式：单条工具输出超过即截断（保留首尾）",
              "number", null, 25,
              c => c.EconomySnipChars.ToString(), (c, v) => c.EconomySnipChars = Math.Clamp(int.Parse(v), 200, 8000), "2000"),
            P("EconomyMaxTokens", null,                          null,
              "单次输出上限", "💰 计费", "省 token 模式：单次请求 max_tokens 上限",
              "number", null, 26,
              c => c.EconomyMaxTokens.ToString(), (c, v) => c.EconomyMaxTokens = Math.Clamp(int.Parse(v), 512, 32768), "8192"),
            P("EconomyComplexRounds", null,                              null,
              "复杂任务判定轮数", "💰 计费", "自动模式：任务达到此轮数视为完全复杂（保质量）",
              "number", null, 27,
              c => c.EconomyComplexRounds.ToString(), (c, v) => c.EconomyComplexRounds = Math.Clamp(int.Parse(v), 5, 100), "30"),
            P("SnipCharsNormal", null,                         null,
              "工具输出裁剪(正常)", "💰 计费", "正常模式：单条工具输出超过即截断（保留首尾）",
              "number", null, 28,
              c => c.SnipCharsNormal.ToString(), (c, v) => c.SnipCharsNormal = Math.Clamp(int.Parse(v), 200, 16000), "4000"),
            P("TinyWindow", null,                   null,
              "Tiny 窗口", "💰 计费", "Tiny 模式实际上下文窗口（--tiny 指定）",
              "number", null, 29,
              c => c.TinyWindow.ToString(), (c, v) => c.TinyWindow = Math.Clamp(int.Parse(v), 1024, 262144), "4096"),

            P("FallbackEnabled", null,                         null,
              "回退链开关", "🤖 模型", "模型失败时按回退链自动切换备选 connect（默认关：只用当前模型，失败即停）",
              "select", ["false","true"], 6,
              c => c.FallbackEnabled.ToString().ToLowerInvariant(),
              (c, v) => c.FallbackEnabled = bool.TryParse(v, out var b) && b, "false"),

            P("FallbackChain", null,                          null,
              "回退模型链", "🤖 模型", "逗号分隔的备选模型列表",
              "text", null, 7,
              c => c.FallbackChain, (c, v) => c.FallbackChain = v,
              "deepseek-v4-flash,deepseek-v4-pro,gemini-2.0-flash,qwen-turbo,glm-4-flash,gpt-5.4-mini"),

            P("FallbackMaxBudget", null,                           null,
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

            P("BudgetWarnPercent",null,                         null,
              "预算预警阈值 (%)", "💰 预算", "花费达到预算此百分比时发出一次提醒（0=关闭）",
              "number", null, 1,
              c => c.BudgetWarnPercent.ToString("F0"),
              (c, v) => c.BudgetWarnPercent = Math.Clamp(double.Parse(v, System.Globalization.CultureInfo.InvariantCulture), 0, 100), "80"),

            // ── 系统 ──
            P("Provider",         "WAYCODER_PROVIDER",          null,
              "提供商", "🔧 系统", "API 提供商 (openai/deepseek/...)",
              "text", null, 0,
              c => c.Provider, (c, v) => c.Provider = v, "openai"),

            P("AutoGitCommit",    null,                         null,
              "Git 自动提交", "🔧 系统", "工具执行后自动 git commit",
              "select", ["false","true"], 1,
              c => c.AutoGitCommit.ToString().ToLowerInvariant(),
              (c, v) => c.AutoGitCommit = bool.Parse(v), "false"),

            P("AutoCheckpoint",   null,                         null,
              "写前自动快照", "🔧 系统", "每轮对话首次写文件前自动创建文件备份检查点（改坏可 /timeline 回滚）",
              "select", ["true","false"], 2,
              c => c.AutoCheckpoint.ToString().ToLowerInvariant(),
              (c, v) => c.AutoCheckpoint = bool.Parse(v), "true"),

            P("CheckpointMax", null,                      null,
              "检查点保留上限", "🔧 系统", "超过上限自动删除最旧检查点（防磁盘无限增长）",
              "number", null, 3,
              c => c.CheckpointMax.ToString(),
              (c, v) => c.CheckpointMax = Math.Clamp(int.Parse(v), 1, 1000), "50"),

            P("UpdateEnabled",    null,                         null,
              "更新开关", "🔧 系统", "内网/离线部署：关闭后 /update、--update 不做网络请求",
              "select", ["true","false"], 3,
              c => c.UpdateEnabled.ToString().ToLowerInvariant(),
              (c, v) => c.UpdateEnabled = bool.Parse(v), "true"),

            P("OllamaNumCtx",     null,                         null,
              "Ollama num_ctx", "🔧 系统", "本地 Ollama 显式上下文窗口（0=自动探测不发送）",
              "number", null, 4,
              c => c.OllamaNumCtx.ToString(),
              (c, v) => c.OllamaNumCtx = Math.Max(0, int.Parse(v)), "0"),

            P("WatchMode",        null,                         null,
              "Watch 模式", "🔧 系统", "监听外部编辑器 AI! 注释自动触发 Agent",
              "select", ["false","true"], 2,
              c => c.WatchMode.ToString().ToLowerInvariant(),
              (c, v) => c.WatchMode = bool.Parse(v), "false"),

            P("WatchExtensions",  null,                         null,
              "Watch 扩展名", "🔧 系统", "监听的源文件扩展名（逗号分隔，默认 .cs .fs .py .js .ts .go .rs）",
              "text", null, 6,
              c => c.WatchExtensions,
              (c, v) => c.WatchExtensions = v, ".cs,.fs,.py,.js,.ts,.go,.rs"),

            P("WatchIgnoreDirs",  null,                        null,
              "Watch 忽略目录", "🔧 系统", "不监听的目录名（逗号分隔，默认 obj,bin,node_modules,.git）",
              "text", null, 7,
              c => c.WatchIgnoreDirs,
              (c, v) => c.WatchIgnoreDirs = v, "obj,bin,node_modules,.git"),

            P("PromptCaching",    null,                         null,
              "Prompt 缓存", "🔧 系统", "追踪系统提示词重复发送，/stats 展示节省",
              "select", ["false","true"], 3,
              c => c.PromptCaching.ToString().ToLowerInvariant(),
              (c, v) => c.PromptCaching = bool.Parse(v), "true"),

            P("SandboxLevel",     null,                         null,
              "沙箱级别", "🔧 系统", "suggest=确认 auto-edit=编自动 full-auto=全自动沙箱",
              "select", ["suggest","auto-edit","full-auto"], 4,
              c => c.SandboxLevel, (c, v) => c.SandboxLevel = v, "suggest"),

            P("EditorIndent",     null,                         null,
              "编辑器缩进", "🔧 系统", "Tab 键插入制表符(\\t)或 4 个空格",
              "select", ["tab","space"], 4,
              c => c.EditorIndent, (c, v) => c.EditorIndent = v, "tab"),

            P("EditorLint",       null,                         null,
              "编辑器 Lint", "🔧 系统", "保存时自动运行 lint 检查并标注错误行",
              "select", ["false","true"], 5,
              c => c.EditorLint.ToString().ToLowerInvariant(),
              (c, v) => c.EditorLint = bool.Parse(v), "true"),

            P("DiffPreview",      null,                         null,
              "Diff 预览", "🔧 系统", "写文件前展示差异并逐 hunk 确认（非交互模式自动跳过）",
              "select", ["false","true"], 6,
              c => c.DiffPreview.ToString().ToLowerInvariant(),
              (c, v) => c.DiffPreview = bool.Parse(v), "false"),

            P("WriteContentView", null,                          null,
              "写入内容展示", "🔧 系统", "write_file/edit_file/multiedit 完成后在聊天区内联展示写入内容（diff 格式：行号+标记；非交互模式自动跳过）",
              "select", ["false","true"], 8,
              c => c.WriteContentView.ToString().ToLowerInvariant(),
              (c, v) => c.WriteContentView = bool.Parse(v), "true"),

            P("MouseEnabled",      "WAYCODER_MOUSE",            null,
              "鼠标支持", "🔧 系统", "启用终端鼠标（点击/滚动/移动；终端不支持或误触时关闭）",
              "select", ["false","true"], 8,
              c => c.MouseEnabled.ToString().ToLowerInvariant(),
              (c, v) => c.MouseEnabled = bool.Parse(v), "true"),

            P("MaxChatMessages",   null,                         null,
              "聊天显示上限", "🔧 系统", "聊天区显示消息上限（100~10000），超过自动丢最旧（会话仍在、文件持久化，仅显示层裁剪保流畅）",
              "number", null, 8,
              c => c.MaxChatMessages.ToString(),
              (c, v) => c.MaxChatMessages = Math.Clamp(int.Parse(v), 100, 10_000), "1000"),

            P("MaxCodePreviewLines",null,                       null,
              "代码预览行数", "🔧 系统", "聊天代码块预览行数上限（10~1000），超过保留头尾中间折叠省略",
              "number", null, 8,
              c => c.MaxCodePreviewLines.ToString(),
              (c, v) => c.MaxCodePreviewLines = Math.Clamp(int.Parse(v), 10, 1000), "500"),

            P("DesktopNotifications", null,                            null,
              "桌面通知", "🔧 系统", "Agent 完成/权限等待时发送桌面通知（默认关闭）",
              "select", ["false","true"], 7,
              c => c.DesktopNotifications.ToString().ToLowerInvariant(),
              (c, v) => c.DesktopNotifications = bool.Parse(v), "false"),

            P("MemoryRelevanceTopN", null,                      null,
              "记忆注入条数", "🔧 系统", "每次注入的最相关记忆数，0=关闭语义匹配",
              "number", null, 6,
              c => c.MemoryRelevanceTopN.ToString(),
              (c, v) => c.MemoryRelevanceTopN = Math.Clamp(int.Parse(v), 0, 20), "5"),

            P("EmbeddingEnabled",  null,                       null,
              "向量嵌入", "🔧 系统", "启用语义向量嵌入搜索（需 API 支持 /v1/embeddings）",
              "select", ["false","true"], 7,
              c => c.EmbeddingEnabled.ToString().ToLowerInvariant(),
              (c, v) => c.EmbeddingEnabled = bool.Parse(v), "false"),

            P("EmbeddingModel",    null,                       null,
              "嵌入模型", "🔧 系统", "向量嵌入模型名称",
              "text", null, 8,
              c => c.EmbeddingModel, (c, v) => c.EmbeddingModel = v, "text-embedding-3-small"),

            P("EmbeddingDimensions", null,                      null,
              "嵌入维度", "🔧 系统", "向量维度（0=模型默认，如 text-embedding-3-small=1536）",
              "number", null, 9,
              c => c.EmbeddingDimensions.ToString(),
              (c, v) => c.EmbeddingDimensions = Math.Clamp(int.Parse(v), 0, 4096), "0"),

            P("TeamMemoryEnabled", null,                       null,
              "团队记忆共享", "🔧 系统", "通过 git 同步 .waycoder/memory/ 共享记忆（需仓库支持）",
              "select", ["false","true"], 10,
              c => c.TeamMemoryEnabled.ToString().ToLowerInvariant(),
              (c, v) => c.TeamMemoryEnabled = bool.Parse(v), "false"),

            P("TeamMemoryAutoSync", null,                      null,
              "启动自动同步", "🔧 系统", "启动时自动 git pull 拉取团队共享记忆",
              "select", ["false","true"], 11,
              c => c.TeamMemoryAutoSync.ToString().ToLowerInvariant(),
              (c, v) => c.TeamMemoryAutoSync = bool.Parse(v), "true"),

            P("TeachModeEnabled", null,                  null,
              "教学模式", "🧠 学习", "AI 不只执行，还逐处解释为什么 + 结束时提问巩固（覆盖极简输出规则）",
              "select", ["false","true"], 12,
              c => c.TeachModeEnabled.ToString().ToLowerInvariant(),
              (c, v) => c.TeachModeEnabled = bool.Parse(v), "false"),

            P("RetroOnExitEnabled", null,                     null,
              "退出自动复盘", "🧠 学习", "会话退出时自动复盘并提炼经验入知识库（需配置模型）",
              "select", ["false","true"], 13,
              c => c.RetroOnExitEnabled.ToString().ToLowerInvariant(),
              (c, v) => c.RetroOnExitEnabled = bool.Parse(v), "false"),

            P("SandboxMaxMemoryMb", null,                             null,
              "沙箱最大内存 (MB)", "🔧 系统", "子进程最大内存，超限自动 kill",
              "number", null, 12,
              c => c.SandboxMaxMemoryMb.ToString(),
              (c, v) => c.SandboxMaxMemoryMb = Math.Clamp(int.Parse(v), 64, 65536), "1024"),

            P("SandboxMaxCpuSeconds", null,                           null,
              "沙箱最大 CPU (秒)", "🔧 系统", "子进程最大 CPU 时间，超限自动 kill",
              "number", null, 13,
              c => c.SandboxMaxCpuSeconds.ToString(),
              (c, v) => c.SandboxMaxCpuSeconds = Math.Clamp(int.Parse(v), 5, 86400), "300"),

            P("SandboxAllowNetwork", null,                             null,
              "沙箱网络", "🔧 系统", "允许沙箱子进程访问网络",
              "select", ["false","true"], 14,
              c => c.SandboxAllowNetwork.ToString().ToLowerInvariant(),
              (c, v) => c.SandboxAllowNetwork = bool.Parse(v), "false"),

            P("SandboxMode", null,                    null,
              "沙箱边界", "🔧 系统", "边界轴（独立于权限）：off 无边界 / project 仅项目内写 / network-off 禁网络 / hard 仅项目内写+禁网络",
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
              "文件锁超时 (秒)", "🔧 系统", "防多 Agent 并发写冲突的锁超时",
              "number", null, 15,
              c => c.FileLockTimeoutSec.ToString(),
              (c, v) => c.FileLockTimeoutSec = Math.Clamp(int.Parse(v), 5, 600), "30"),

            // ── 界面 ──
            P("GuiTheme",         null,                         null,
              "GUI 主题", "🎨 界面", "GUI 版深/浅色主题",
              "select", ["dark", "light"], 4,
              c => c.GuiTheme, (c, v) => c.GuiTheme = v, "dark"),

            P("ThemePreset",      null,                         null,
              "界面主题", "🎨 界面", "预设配色方案，选中即生效",
              "select", WayCoder.UI.Tui.TuiTheme.PresetNames, 4,
              c => c.ThemePreset, (c, v) => c.ThemePreset = v, "黄金甲"),

            P("ColorScheme",      null,                         null,
              "配色方案", "🎨 界面", "预设配色 (覆盖下方颜色设置)",
              "select", ["default","ocean","forest","sunset","mono","cyberpunk"], 0,
              c => c.ColorScheme, (c, v) => { c.ColorScheme = v; ApplyColorScheme(c, v); }, "default"),

            P("BorderStyle",      null,                         null,
              "边框类型", "🎨 界面", "对话框和面板的边框样式",
              "select", ["rounded","single","double","bold"], 1,
              c => c.BorderStyle, (c, v) => c.BorderStyle = v, "rounded"),

            P("BorderColor",      null,                         null,
              "边框颜色", "🎨 界面", "ANSI 色号: 36=青 32=绿 33=黄 35=紫 34=蓝 37=白",
              "select", ["36","32","33","35","34","37"], 2,
              c => c.BorderColor, (c, v) => c.BorderColor = v, "36"),

            P("AccentColor",      null,                         null,
              "强调色", "🎨 界面", "标题和选中高亮的颜色",
              "select", ["36","32","33","35","34","37"], 3,
              c => c.AccentColor, (c, v) => c.AccentColor = v, "36"),

            P("ChatDisplayStyle", null,                         null,
              "聊天显示风格", "🎨 界面", "detailed=全显示 auto=智能简洁=极简（隐藏工具详情）",
              "select", ["auto","detailed","concise"], 5,
              c => c.ChatDisplayStyle, (c, v) => c.ChatDisplayStyle = v, "auto"),

            P("MarkupUi", null,                                  null,
              "标记界面（实验）", "🎨 界面", "用 .tui 声明式标记重建主聊天界面（实验性，测试通过后翻默认）",
              "select", ["false","true"], 6,
              c => c.MarkupUi.ToString().ToLowerInvariant(),
              (c, v) => c.MarkupUi = bool.Parse(v), "false"),
        ];
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
}
