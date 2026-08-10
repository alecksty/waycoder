
namespace CoreCoderSharp;

/// <summary>
/// 设置项元数据 —— 供设置界面自动生成布局。
/// </summary>
public record SettingDef(
    string Key, string Label, string Category, string Desc,
    string Type = "text",          // text | number | select | secret | toggle
    string[]? Options = null,      // select 类型的可选项
    string EnvVar = "",
    int Order = 0
);

/// <summary>
/// 配置 - 环境变量和默认值。
/// 环境变量优先读取 WAYCODER_*（新名），
/// 回退到 CORECODER_*（旧名，兼容 v0.16.2 及之前版本）。
/// </summary>
public class Config
{
    public string Model { get; set; } = "deepseek-v4-flash";       // 大模型
    public string SmallModel { get; set; } = "deepseek-v4-flash";  // 小模型 (便宜快速)
    public string ApiKey { get; set; } = "";
    public string? BaseUrl { get; set; }
    public int MaxTokens { get; set; } = 4096;
    public float Temperature { get; set; } = 0.0f;
    public int MaxContextTokens { get; set; } = 128_000;
    public string Provider { get; set; } = "openai";

    /// <summary>最大预算（美元），null 表示无限制</summary>
    public double? MaxBudgetUsd { get; set; }

    /// <summary>每次工具执行后自动 git commit（默认关闭）</summary>
    public bool AutoGitCommit { get; set; } = false;

    /// <summary>Watch 模式 — 监听外部编辑器文件变更，自动处理 AI! / AI? 注释</summary>
    public bool WatchMode { get; set; } = false;

    /// <summary>Prompt 缓存 — 追踪系统提示词/工具定义的重复发送，/stats 展示节省量</summary>
    public bool PromptCaching { get; set; } = true;

    /// <summary>沙箱级别: suggest | auto-edit | full-auto</summary>
    public string SandboxLevel { get; set; } = "suggest";

    /// <summary>编辑器保存后自动 lint 检查（默认开启）</summary>
    public bool EditorLint { get; set; } = true;

    /// <summary>Diff 预览 — 写文件前展示差异并逐 hunk 确认（默认关闭）</summary>
    public bool DiffPreview { get; set; } = false;

    /// <summary>工具执行超时（秒），默认 120</summary>
    public int ToolTimeoutSec { get; set; } = 120;

    /// <summary>Lint 检查超时（秒），默认 60</summary>
    public int LintTimeoutSec { get; set; } = 60;

    /// <summary>子智能体最大递归深度，默认 3（1=单层，范围 1-5）</summary>
    public int SubAgentMaxDepth { get; set; } = 3;

    /// <summary>记忆相关性注入条数，默认 5（0=关闭语义匹配，回退全量加载）</summary>
    public int MemoryRelevanceTopN { get; set; } = 5;

    /// <summary>向量嵌入功能开关（默认关闭，需 API 支持 /v1/embeddings）</summary>
    public bool EmbeddingEnabled { get; set; } = false;
    /// <summary>嵌入模型名称</summary>
    public string EmbeddingModel { get; set; } = "text-embedding-3-small";
    /// <summary>嵌入向量维度（0=模型默认）</summary>
    public int EmbeddingDimensions { get; set; } = 0;

    /// <summary>团队知识库共享开关（默认关闭，需 git 仓库）</summary>
    public bool TeamMemoryEnabled { get; set; } = false;
    /// <summary>启动时自动拉取共享记忆</summary>
    public bool TeamMemoryAutoSync { get; set; } = true;

    /// <summary>界面主题预设</summary>
    public string ThemePreset { get; set; } = "default";

    // ---- 界面主题 ----
    /// <summary>边框类型: single | double | rounded | bold</summary>
    public string BorderStyle { get; set; } = "rounded";
    /// <summary>边框颜色 (ANSI 色号): 36=青 32=绿 33=黄 35=紫 34=蓝 37=白</summary>
    public string BorderColor { get; set; } = "36";
    /// <summary>标题/强调色 (ANSI 色号)</summary>
    public string AccentColor { get; set; } = "36";
    /// <summary>预设配色方案: default | ocean | forest | sunset | mono | cyberpunk</summary>
    public string ColorScheme { get; set; } = "default";

    /// <summary>读取环境变量：新名优先，旧名回退（向后兼容）</summary>
    private static string? Env(string newName, string oldName) =>
        Environment.GetEnvironmentVariable(newName)
        ?? Environment.GetEnvironmentVariable(oldName);

    /// <summary>读取环境变量 + 通用回退链（如 API Key 同时查 OPENAI_API_KEY）</summary>
    private static string? EnvMulti(string newName, string oldName, params string[] more) =>
        Env(newName, oldName) ?? more.Select(Environment.GetEnvironmentVariable).FirstOrDefault(v => v != null);

    /// <summary>
    /// 从环境变量加载配置。也支持从当前目录向上查找到 home 目录的 .env 文件。
    /// </summary>
    public static Config FromEnv()
    {
        LoadDotEnv();

        var config = new Config();

        var envModel = Env("WAYCODER_MODEL", "CORECODER_MODEL");
        if (envModel != null) config.Model = envModel;

        var envSmallModel = Env("WAYCODER_SMALL_MODEL", "CORECODER_SMALL_MODEL");
        if (envSmallModel != null) config.SmallModel = envSmallModel;

        var envApiKey = EnvMulti("WAYCODER_API_KEY", "CORECODER_API_KEY",
            "OPENAI_API_KEY", "DEEPSEEK_API_KEY", "ANTHROPIC_API_KEY", "API_KEY");
        if (envApiKey != null) config.ApiKey = envApiKey;

        var envBaseUrl = EnvMulti("WAYCODER_BASE_URL", "CORECODER_BASE_URL", "OPENAI_BASE_URL");
        if (envBaseUrl != null) config.BaseUrl = envBaseUrl;

        if (int.TryParse(Env("WAYCODER_MAX_TOKENS", "CORECODER_MAX_TOKENS"), out var mt))
            config.MaxTokens = mt;
        if (float.TryParse(Env("WAYCODER_TEMPERATURE", "CORECODER_TEMPERATURE"), out var t))
            config.Temperature = t;
        if (int.TryParse(Env("WAYCODER_MAX_CONTEXT", "CORECODER_MAX_CONTEXT"), out var mc))
            config.MaxContextTokens = mc;

        var envProvider = Env("WAYCODER_PROVIDER", "CORECODER_PROVIDER");
        if (envProvider != null) config.Provider = envProvider;

        if (double.TryParse(Env("WAYCODER_MAX_BUDGET_USD", "CORECODER_MAX_BUDGET_USD"), out var budget))
            config.MaxBudgetUsd = budget;

        if (bool.TryParse(Env("WAYCODER_AUTO_COMMIT", "CORECODER_AUTO_COMMIT"), out var ac))
            config.AutoGitCommit = ac;

        if (bool.TryParse(Env("WAYCODER_WATCH", "CORECODER_WATCH"), out var wm))
            config.WatchMode = wm;

        if (bool.TryParse(Env("WAYCODER_PROMPT_CACHE", "CORECODER_PROMPT_CACHE"), out var pc))
            config.PromptCaching = pc;

        var envSandbox = Env("WAYCODER_SANDBOX_LEVEL", "CORECODER_SANDBOX_LEVEL");
        if (envSandbox != null) config.SandboxLevel = envSandbox;

        if (bool.TryParse(Env("WAYCODER_EDITOR_LINT", "CORECODER_EDITOR_LINT"), out var el))
            config.EditorLint = el;

        if (bool.TryParse(Env("WAYCODER_DIFF_PREVIEW", "CORECODER_DIFF_PREVIEW"), out var dp))
            config.DiffPreview = dp;

        if (int.TryParse(Env("WAYCODER_TOOL_TIMEOUT", "CORECODER_TOOL_TIMEOUT"), out var tto))
            config.ToolTimeoutSec = tto;

        if (int.TryParse(Env("WAYCODER_LINT_TIMEOUT", "CORECODER_LINT_TIMEOUT"), out var lto))
            config.LintTimeoutSec = lto;

        if (int.TryParse(Env("WAYCODER_SUBAGENT_DEPTH", "CORECODER_SUBAGENT_DEPTH"), out var sd))
            config.SubAgentMaxDepth = Math.Clamp(sd, 1, 5);

        if (int.TryParse(Env("WAYCODER_MEMORY_TOPN", "CORECODER_MEMORY_TOPN"), out var mtn))
            config.MemoryRelevanceTopN = Math.Clamp(mtn, 0, 20);

        // Embedding 配置
        if (bool.TryParse(Env("WAYCODER_EMBEDDING", "CORECODER_EMBEDDING"), out var emb))
            config.EmbeddingEnabled = emb;
        var embModel = Env("WAYCODER_EMBEDDING_MODEL", "CORECODER_EMBEDDING_MODEL");
        if (embModel != null) config.EmbeddingModel = embModel;
        if (int.TryParse(Env("WAYCODER_EMBEDDING_DIMS", "CORECODER_EMBEDDING_DIMS"), out var embDims))
            config.EmbeddingDimensions = Math.Clamp(embDims, 0, 4096);

        // 团队记忆
        if (bool.TryParse(Env("WAYCODER_TEAM_MEMORY", "CORECODER_TEAM_MEMORY"), out var tm))
            config.TeamMemoryEnabled = tm;
        if (bool.TryParse(Env("WAYCODER_TEAM_AUTO_SYNC", "CORECODER_TEAM_AUTO_SYNC"), out var tas))
            config.TeamMemoryAutoSync = tas;

        var envTheme = Env("WAYCODER_THEME", "CORECODER_THEME");
        if (envTheme != null) config.ThemePreset = envTheme;

        // 主题
        var envBorder = Env("WAYCODER_BORDER_STYLE", "CORECODER_BORDER_STYLE");
        if (envBorder != null) config.BorderStyle = envBorder;
        var envBorderColor = Env("WAYCODER_BORDER_COLOR", "CORECODER_BORDER_COLOR");
        if (envBorderColor != null) config.BorderColor = envBorderColor;
        var envAccent = Env("WAYCODER_ACCENT_COLOR", "CORECODER_ACCENT_COLOR");
        if (envAccent != null) config.AccentColor = envAccent;
        var envScheme = Env("WAYCODER_COLOR_SCHEME", "CORECODER_COLOR_SCHEME");
        if (envScheme != null) { config.ColorScheme = envScheme; ApplyColorScheme(config, envScheme); }

        return config;
    }

    /// <summary>
    /// 从当前目录向上查找到 home 目录，加载 .env 文件中的键值对。
    /// 已有的环境变量不会被覆盖。
    /// </summary>
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

                var eqIdx = trimmed.IndexOf('=');
                if (eqIdx <= 0 || eqIdx >= trimmed.Length - 1) continue;

                var key = trimmed[..eqIdx].Trim();
                var value = trimmed[(eqIdx + 1)..].Trim();

                // 去除引号
                if ((value.StartsWith('"') && value.EndsWith('"'))
                    || (value.StartsWith('\'') && value.EndsWith('\'')))
                {
                    value = value[1..^1];
                }

                // 只在环境变量未设置时才设置
                if (Environment.GetEnvironmentVariable(key) == null)
                {
                    Environment.SetEnvironmentVariable(key, value);
                }
            }
        }
        catch
        {
            // 静默跳过无法读取的 .env 文件
        }
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

    // ================================================================
    // 设置界面元数据 (新增配置项只需加一行)
    // ================================================================

    /// <summary>应用预设配色方案</summary>
    public static void ApplyColorScheme(Config config, string scheme)
    {
        switch (scheme.ToLowerInvariant())
        {
            case "ocean":
                config.BorderColor = "34"; config.AccentColor = "34"; break;
            case "forest":
                config.BorderColor = "32"; config.AccentColor = "32"; break;
            case "sunset":
                config.BorderColor = "33"; config.AccentColor = "33"; break;
            case "mono":
                config.BorderColor = "37"; config.AccentColor = "37"; break;
            case "cyberpunk":
                config.BorderColor = "35"; config.AccentColor = "36"; break;
            default: // "default"
                config.BorderColor = "36"; config.AccentColor = "36"; break;
        }
        config.ColorScheme = scheme;
    }

    /// <summary>所有可配置项的元数据，设置界面自动布局</summary>
    public static List<SettingDef> SettingSchema() =>
    [
        // 模型
        new("Model", "大模型 (复杂任务)", "🤖 模型", "架构/重构/调试/多文件",
            "select", ["deepseek-v4-pro","gpt-5.4","gpt-5.5","deepseek-v4-flash","gpt-4o","gpt-4o-mini"],
            "WAYCODER_MODEL", 0),
        new("SmallModel", "小模型 (简单任务)", "🤖 模型", "补全/摘要/压缩 (便宜快速)",
            "select", ["deepseek-v4-flash","gpt-5.4-mini","gpt-4o-mini","deepseek-v4-pro"],
            "WAYCODER_SMALL_MODEL", 1),
        new("BaseUrl", "API 地址", "🤖 模型", "API 端点 URL",
            "text", null, "WAYCODER_BASE_URL", 2),
        new("ApiKey", "API 密钥", "🤖 模型", "API 密钥 (已隐藏)",
            "secret", null, "WAYCODER_API_KEY", 3),

        // 参数
        new("MaxTokens", "最大 Token", "⚙ 参数", "每次请求最大 Token 数",
            "number", null, "WAYCODER_MAX_TOKENS", 0),
        new("Temperature", "温度", "⚙ 参数", "0=精确 1=创意",
            "number", null, "WAYCODER_TEMPERATURE", 1),
        new("MaxContextTokens", "上下文窗口", "⚙ 参数", "上下文窗口大小",
            "number", null, "WAYCODER_MAX_CONTEXT", 2),

        // 预算
        new("MaxBudgetUsd", "预算上限 ($)", "💰 预算", "超支自动停止，留空=无限制",
            "number", null, "WAYCODER_MAX_BUDGET_USD", 0),

        // 系统
        new("Provider", "提供商", "🔧 系统", "API 提供商 (openai/deepseek/...)",
            "text", null, "WAYCODER_PROVIDER", 0),
        new("AutoGitCommit", "Git 自动提交", "🔧 系统", "工具执行后自动 git commit",
            "select", ["false", "true"], "WAYCODER_AUTO_COMMIT", 1),
        new("WatchMode", "Watch 模式", "🔧 系统", "监听外部编辑器 AI! 注释自动触发 Agent",
            "select", ["false", "true"], "WAYCODER_WATCH", 2),
        new("PromptCaching", "Prompt 缓存", "🔧 系统", "追踪系统提示词重复发送，/stats 展示节省",
            "select", ["false", "true"], "WAYCODER_PROMPT_CACHE", 3),
        new("SandboxLevel", "沙箱级别", "🔧 系统", "suggest=确认 auto-edit=编自动 full-auto=全自动沙箱",
            "select", ["suggest", "auto-edit", "full-auto"], "WAYCODER_SANDBOX_LEVEL", 4),
        new("EditorLint", "编辑器 Lint", "🔧 系统", "保存时自动运行 lint 检查并标注错误行",
            "select", ["false", "true"], "WAYCODER_EDITOR_LINT", 5),
        new("DiffPreview", "Diff 预览", "🔧 系统", "写文件前展示差异并逐 hunk 确认（需非交互模式自动跳过）",
            "select", ["false", "true"], "WAYCODER_DIFF_PREVIEW", 6),
        new("ToolTimeoutSec", "工具超时 (秒)", "⚙ 参数", "Bash 等工具执行超时，默认 120 秒",
            "number", null, "WAYCODER_TOOL_TIMEOUT", 3),
        new("LintTimeoutSec", "Lint 超时 (秒)", "⚙ 参数", "Lint 检查超时，默认 60 秒（大项目可调大）",
            "number", null, "WAYCODER_LINT_TIMEOUT", 4),
        new("SubAgentMaxDepth", "子智能体深度", "🤖 模型", "子智能体最大递归层数，1=单层 5=最深",
            "number", null, "WAYCODER_SUBAGENT_DEPTH", 4),
        new("MemoryRelevanceTopN", "记忆注入条数", "🔧 系统", "每次注入的最相关记忆数，0=关闭语义匹配",
            "number", null, "WAYCODER_MEMORY_TOPN", 6),
        new("EmbeddingEnabled", "向量嵌入", "🔧 系统", "启用语义向量嵌入搜索（需 API 支持 /v1/embeddings）",
            "select", ["false", "true"], "WAYCODER_EMBEDDING", 7),
        new("EmbeddingModel", "嵌入模型", "🔧 系统", "向量嵌入模型名称",
            "text", null, "WAYCODER_EMBEDDING_MODEL", 8),
        new("EmbeddingDimensions", "嵌入维度", "🔧 系统", "向量维度（0=模型默认，如 text-embedding-3-small=1536）",
            "number", null, "WAYCODER_EMBEDDING_DIMS", 9),
        new("TeamMemoryEnabled", "团队记忆共享", "🔧 系统", "通过 git 同步 .corecoder/memory/ 共享记忆（需仓库支持）",
            "select", ["false", "true"], "WAYCODER_TEAM_MEMORY", 10),
        new("TeamMemoryAutoSync", "启动自动同步", "🔧 系统", "启动时自动 git pull 拉取团队共享记忆",
            "select", ["false", "true"], "WAYCODER_TEAM_AUTO_SYNC", 11),
        new("ThemePreset", "界面主题", "🎨 界面", "预设配色方案，选中即生效",
            "select", ["default","ocean","forest","sunset","midnight","mono"],
            "WAYCODER_THEME", 4),

        // 界面主题
        new("ColorScheme", "配色方案", "🎨 界面", "预设配色 (覆盖下方颜色设置)",
            "select", ["default", "ocean", "forest", "sunset", "mono", "cyberpunk"],
            "WAYCODER_COLOR_SCHEME", 0),
        new("BorderStyle", "边框类型", "🎨 界面", "对话框和面板的边框样式",
            "select", ["rounded", "single", "double", "bold"],
            "WAYCODER_BORDER_STYLE", 1),
        new("BorderColor", "边框颜色", "🎨 界面", "ANSI 色号: 36=青 32=绿 33=黄 35=紫 34=蓝 37=白",
            "select", ["36", "32", "33", "35", "34", "37"],
            "WAYCODER_BORDER_COLOR", 2),
        new("AccentColor", "强调色", "🎨 界面", "标题和选中高亮的颜色",
            "select", ["36", "32", "33", "35", "34", "37"],
            "WAYCODER_ACCENT_COLOR", 3),
    ];

    /// <summary>将当前配置写回 .env 文件</summary>
    public void SaveToEnvFile()
    {
        var envPath = FindEnvFile() ?? Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".corecoder", ".env");
        var dir = Path.GetDirectoryName(envPath);
        if (dir != null) Directory.CreateDirectory(dir);
        var lines = File.Exists(envPath) ? File.ReadAllLines(envPath).ToList() : [];
        ApplyOrAppend(lines, "WAYCODER_MODEL", Model);
        ApplyOrAppend(lines, "WAYCODER_SMALL_MODEL", SmallModel);
        if (!string.IsNullOrEmpty(BaseUrl)) ApplyOrAppend(lines, "WAYCODER_BASE_URL", BaseUrl);
        ApplyOrAppend(lines, "WAYCODER_API_KEY", ApiKey);
        ApplyOrAppend(lines, "WAYCODER_MAX_TOKENS", MaxTokens.ToString());
        ApplyOrAppend(lines, "WAYCODER_TEMPERATURE", Temperature.ToString("F1"));
        ApplyOrAppend(lines, "WAYCODER_MAX_CONTEXT", MaxContextTokens.ToString());
        if (MaxBudgetUsd.HasValue) ApplyOrAppend(lines, "WAYCODER_MAX_BUDGET_USD", MaxBudgetUsd.Value.ToString("F2"));
        ApplyOrAppend(lines, "WAYCODER_PROVIDER", Provider);
        ApplyOrAppend(lines, "WAYCODER_AUTO_COMMIT", AutoGitCommit.ToString().ToLowerInvariant());
        ApplyOrAppend(lines, "WAYCODER_WATCH", WatchMode.ToString().ToLowerInvariant());
        ApplyOrAppend(lines, "WAYCODER_PROMPT_CACHE", PromptCaching.ToString().ToLowerInvariant());
        ApplyOrAppend(lines, "WAYCODER_SANDBOX_LEVEL", SandboxLevel);
        ApplyOrAppend(lines, "WAYCODER_EDITOR_LINT", EditorLint.ToString().ToLowerInvariant());
        ApplyOrAppend(lines, "WAYCODER_TOOL_TIMEOUT", ToolTimeoutSec.ToString());
        ApplyOrAppend(lines, "WAYCODER_LINT_TIMEOUT", LintTimeoutSec.ToString());
        ApplyOrAppend(lines, "WAYCODER_SUBAGENT_DEPTH", SubAgentMaxDepth.ToString());
        ApplyOrAppend(lines, "WAYCODER_MEMORY_TOPN", MemoryRelevanceTopN.ToString());
        ApplyOrAppend(lines, "WAYCODER_EMBEDDING", EmbeddingEnabled.ToString().ToLowerInvariant());
        ApplyOrAppend(lines, "WAYCODER_EMBEDDING_MODEL", EmbeddingModel);
        ApplyOrAppend(lines, "WAYCODER_EMBEDDING_DIMS", EmbeddingDimensions.ToString());
        ApplyOrAppend(lines, "WAYCODER_TEAM_MEMORY", TeamMemoryEnabled.ToString().ToLowerInvariant());
        ApplyOrAppend(lines, "WAYCODER_TEAM_AUTO_SYNC", TeamMemoryAutoSync.ToString().ToLowerInvariant());
        ApplyOrAppend(lines, "WAYCODER_THEME", ThemePreset);
        ApplyOrAppend(lines, "WAYCODER_BORDER_STYLE", BorderStyle);
        ApplyOrAppend(lines, "WAYCODER_BORDER_COLOR", BorderColor);
        ApplyOrAppend(lines, "WAYCODER_ACCENT_COLOR", AccentColor);
        ApplyOrAppend(lines, "WAYCODER_COLOR_SCHEME", ColorScheme);
        File.WriteAllLines(envPath, lines);
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

}
