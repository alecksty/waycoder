
namespace WayCoder;

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
/// 单个配置属性的完整定义 —— Schema 驱动 FromEnv / SettingSchema / SaveToEnvFile。
/// AOT 安全：Getter/Setter 用委托不用反射。
/// </summary>
record ConfigProp(
    string Key,                        // 属性名 "Model"
    string EnvVar,                     // "WAYCODER_MODEL"
    string? OldEnvVar,                 // "CORECODER_MODEL"（旧名兼容，null = 无）
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
/// 配置 - 环境变量和默认值。
///
/// 新增配置项只需在 _schema 列表中加一行，SettingSchema/FromEnv/SaveToEnvFile 全部自动推导。
/// 环境变量优先读取 WAYCODER_*（新名），回退到 CORECODER_*（旧名，兼容 v0.16.2 及之前版本）。
/// </summary>
public class Config
{
    // ════════════════════════════════════════════════════════════
    // 属性声明（保持原有类型和默认值，全项目兼容）
    // ════════════════════════════════════════════════════════════

    public string Model { get; set; } = "deepseek-v4-flash";
    public string SmallModel { get; set; } = "deepseek-v4-flash";
    public string ApiKey { get; set; } = "";
    public string? BaseUrl { get; set; }
    public int MaxTokens { get; set; } = 4096;
    public float Temperature { get; set; } = 0.0f;
    public int MaxContextTokens { get; set; } = 128_000;
    public string Provider { get; set; } = "openai";
    public double? MaxBudgetUsd { get; set; }
    public bool AutoGitCommit { get; set; } = false;
    public bool WatchMode { get; set; } = false;
    public bool PromptCaching { get; set; } = true;
    public string SandboxLevel { get; set; } = "suggest";
    public bool EditorLint { get; set; } = true;
    public bool DiffPreview { get; set; } = false;
    public int ToolTimeoutSec { get; set; } = 120;
    public int LintTimeoutSec { get; set; } = 60;
    public int SubAgentMaxDepth { get; set; } = 3;
    public int MemoryRelevanceTopN { get; set; } = 5;
    public bool EmbeddingEnabled { get; set; } = false;
    public string EmbeddingModel { get; set; } = "text-embedding-3-small";
    public int EmbeddingDimensions { get; set; } = 0;
    public bool TeamMemoryEnabled { get; set; } = false;
    public bool TeamMemoryAutoSync { get; set; } = true;
    public string ThemePreset { get; set; } = "default";

    // 界面主题
    public string BorderStyle { get; set; } = "rounded";
    public string BorderColor { get; set; } = "36";
    public string AccentColor { get; set; } = "36";
    public string ColorScheme { get; set; } = "default";

    // ════════════════════════════════════════════════════════════
    // 单一 Schema 定义（新增配置项只加这里一行）
    // ════════════════════════════════════════════════════════════

    static readonly ConfigProp[] _schema;

    static Config()
    {
        _schema = [
            // ── 模型 ──
            P("Model",        "WAYCODER_MODEL",           "CORECODER_MODEL",
              "大模型 (复杂任务)", "🤖 模型", "架构/重构/调试/多文件",
              "select", ["deepseek-v4-pro","gpt-5.4","gpt-5.5","deepseek-v4-flash","gpt-4o","gpt-4o-mini"], 0,
              c => c.Model, (c, v) => c.Model = v, "deepseek-v4-flash"),

            P("SmallModel",   "WAYCODER_SMALL_MODEL",     "CORECODER_SMALL_MODEL",
              "小模型 (简单任务)", "🤖 模型", "补全/摘要/压缩 (便宜快速)",
              "select", ["deepseek-v4-flash","gpt-5.4-mini","gpt-4o-mini","deepseek-v4-pro"], 1,
              c => c.SmallModel, (c, v) => c.SmallModel = v, "deepseek-v4-flash"),

            P("BaseUrl",      "WAYCODER_BASE_URL",        "CORECODER_BASE_URL",
              "API 地址", "🤖 模型", "API 端点 URL",
              "text", null, 2,
              c => c.BaseUrl ?? "", (c, v) => c.BaseUrl = string.IsNullOrEmpty(v) ? null : v,
              skipIfEmpty: true),

            P("ApiKey",       "WAYCODER_API_KEY",         "CORECODER_API_KEY",
              "API 密钥", "🤖 模型", "API 密钥 (已隐藏)",
              "secret", null, 3,
              c => c.ApiKey, (c, v) => c.ApiKey = v, "", skipIfEmpty: true),

            // ── 参数 ──
            P("MaxTokens",        "WAYCODER_MAX_TOKENS",        "CORECODER_MAX_TOKENS",
              "最大 Token", "⚙ 参数", "每次请求最大 Token 数",
              "number", null, 0,
              c => c.MaxTokens.ToString(), (c, v) => c.MaxTokens = int.Parse(v), "4096"),

            P("Temperature",      "WAYCODER_TEMPERATURE",       "CORECODER_TEMPERATURE",
              "温度", "⚙ 参数", "0=精确 1=创意",
              "number", null, 1,
              c => c.Temperature.ToString("F1"), (c, v) => c.Temperature = float.Parse(v), "0.0"),

            P("MaxContextTokens", "WAYCODER_MAX_CONTEXT",       "CORECODER_MAX_CONTEXT",
              "上下文窗口", "⚙ 参数", "上下文窗口大小",
              "number", null, 2,
              c => c.MaxContextTokens.ToString(), (c, v) => c.MaxContextTokens = int.Parse(v), "128000"),

            P("ToolTimeoutSec",   "WAYCODER_TOOL_TIMEOUT",      "CORECODER_TOOL_TIMEOUT",
              "工具超时 (秒)", "⚙ 参数", "Bash 等工具执行超时，默认 120 秒",
              "number", null, 3,
              c => c.ToolTimeoutSec.ToString(), (c, v) => c.ToolTimeoutSec = int.Parse(v), "120"),

            P("LintTimeoutSec",   "WAYCODER_LINT_TIMEOUT",      "CORECODER_LINT_TIMEOUT",
              "Lint 超时 (秒)", "⚙ 参数", "Lint 检查超时，默认 60 秒（大项目可调大）",
              "number", null, 4,
              c => c.LintTimeoutSec.ToString(), (c, v) => c.LintTimeoutSec = int.Parse(v), "60"),

            P("SubAgentMaxDepth", "WAYCODER_SUBAGENT_DEPTH",    "CORECODER_SUBAGENT_DEPTH",
              "子智能体深度", "🤖 模型", "子智能体最大递归层数，1=单层 5=最深",
              "number", null, 4,
              c => c.SubAgentMaxDepth.ToString(),
              (c, v) => c.SubAgentMaxDepth = Math.Clamp(int.Parse(v), 1, 5), "3"),

            // ── 预算 ──
            P("MaxBudgetUsd",     "WAYCODER_MAX_BUDGET_USD",    "CORECODER_MAX_BUDGET_USD",
              "预算上限 ($)", "💰 预算", "超支自动停止，留空=无限制",
              "number", null, 0,
              c => c.MaxBudgetUsd?.ToString("F2") ?? "",
              (c, v) => c.MaxBudgetUsd = string.IsNullOrEmpty(v) ? null : double.Parse(v),
              skipIfEmpty: true),

            // ── 系统 ──
            P("Provider",         "WAYCODER_PROVIDER",          "CORECODER_PROVIDER",
              "提供商", "🔧 系统", "API 提供商 (openai/deepseek/...)",
              "text", null, 0,
              c => c.Provider, (c, v) => c.Provider = v, "openai"),

            P("AutoGitCommit",    "WAYCODER_AUTO_COMMIT",       "CORECODER_AUTO_COMMIT",
              "Git 自动提交", "🔧 系统", "工具执行后自动 git commit",
              "select", ["false","true"], 1,
              c => c.AutoGitCommit.ToString().ToLowerInvariant(),
              (c, v) => c.AutoGitCommit = bool.Parse(v), "false"),

            P("WatchMode",        "WAYCODER_WATCH",             "CORECODER_WATCH",
              "Watch 模式", "🔧 系统", "监听外部编辑器 AI! 注释自动触发 Agent",
              "select", ["false","true"], 2,
              c => c.WatchMode.ToString().ToLowerInvariant(),
              (c, v) => c.WatchMode = bool.Parse(v), "false"),

            P("PromptCaching",    "WAYCODER_PROMPT_CACHE",      "CORECODER_PROMPT_CACHE",
              "Prompt 缓存", "🔧 系统", "追踪系统提示词重复发送，/stats 展示节省",
              "select", ["false","true"], 3,
              c => c.PromptCaching.ToString().ToLowerInvariant(),
              (c, v) => c.PromptCaching = bool.Parse(v), "true"),

            P("SandboxLevel",     "WAYCODER_SANDBOX_LEVEL",     "CORECODER_SANDBOX_LEVEL",
              "沙箱级别", "🔧 系统", "suggest=确认 auto-edit=编自动 full-auto=全自动沙箱",
              "select", ["suggest","auto-edit","full-auto"], 4,
              c => c.SandboxLevel, (c, v) => c.SandboxLevel = v, "suggest"),

            P("EditorLint",       "WAYCODER_EDITOR_LINT",       "CORECODER_EDITOR_LINT",
              "编辑器 Lint", "🔧 系统", "保存时自动运行 lint 检查并标注错误行",
              "select", ["false","true"], 5,
              c => c.EditorLint.ToString().ToLowerInvariant(),
              (c, v) => c.EditorLint = bool.Parse(v), "true"),

            P("DiffPreview",      "WAYCODER_DIFF_PREVIEW",      "CORECODER_DIFF_PREVIEW",
              "Diff 预览", "🔧 系统", "写文件前展示差异并逐 hunk 确认（非交互模式自动跳过）",
              "select", ["false","true"], 6,
              c => c.DiffPreview.ToString().ToLowerInvariant(),
              (c, v) => c.DiffPreview = bool.Parse(v), "false"),

            P("MemoryRelevanceTopN", "WAYCODER_MEMORY_TOPN",    "CORECODER_MEMORY_TOPN",
              "记忆注入条数", "🔧 系统", "每次注入的最相关记忆数，0=关闭语义匹配",
              "number", null, 6,
              c => c.MemoryRelevanceTopN.ToString(),
              (c, v) => c.MemoryRelevanceTopN = Math.Clamp(int.Parse(v), 0, 20), "5"),

            P("EmbeddingEnabled",  "WAYCODER_EMBEDDING",       "CORECODER_EMBEDDING",
              "向量嵌入", "🔧 系统", "启用语义向量嵌入搜索（需 API 支持 /v1/embeddings）",
              "select", ["false","true"], 7,
              c => c.EmbeddingEnabled.ToString().ToLowerInvariant(),
              (c, v) => c.EmbeddingEnabled = bool.Parse(v), "false"),

            P("EmbeddingModel",    "WAYCODER_EMBEDDING_MODEL", "CORECODER_EMBEDDING_MODEL",
              "嵌入模型", "🔧 系统", "向量嵌入模型名称",
              "text", null, 8,
              c => c.EmbeddingModel, (c, v) => c.EmbeddingModel = v, "text-embedding-3-small"),

            P("EmbeddingDimensions", "WAYCODER_EMBEDDING_DIMS", "CORECODER_EMBEDDING_DIMS",
              "嵌入维度", "🔧 系统", "向量维度（0=模型默认，如 text-embedding-3-small=1536）",
              "number", null, 9,
              c => c.EmbeddingDimensions.ToString(),
              (c, v) => c.EmbeddingDimensions = Math.Clamp(int.Parse(v), 0, 4096), "0"),

            P("TeamMemoryEnabled", "WAYCODER_TEAM_MEMORY",     "CORECODER_TEAM_MEMORY",
              "团队记忆共享", "🔧 系统", "通过 git 同步 .waycoder/memory/ 共享记忆（需仓库支持）",
              "select", ["false","true"], 10,
              c => c.TeamMemoryEnabled.ToString().ToLowerInvariant(),
              (c, v) => c.TeamMemoryEnabled = bool.Parse(v), "false"),

            P("TeamMemoryAutoSync", "WAYCODER_TEAM_AUTO_SYNC", "CORECODER_TEAM_AUTO_SYNC",
              "启动自动同步", "🔧 系统", "启动时自动 git pull 拉取团队共享记忆",
              "select", ["false","true"], 11,
              c => c.TeamMemoryAutoSync.ToString().ToLowerInvariant(),
              (c, v) => c.TeamMemoryAutoSync = bool.Parse(v), "true"),

            // ── 界面 ──
            P("ThemePreset",      "WAYCODER_THEME",             "CORECODER_THEME",
              "界面主题", "🎨 界面", "预设配色方案，选中即生效",
              "select", ["default","ocean","forest","sunset","midnight","mono"], 4,
              c => c.ThemePreset, (c, v) => c.ThemePreset = v, "default"),

            P("ColorScheme",      "WAYCODER_COLOR_SCHEME",      "CORECODER_COLOR_SCHEME",
              "配色方案", "🎨 界面", "预设配色 (覆盖下方颜色设置)",
              "select", ["default","ocean","forest","sunset","mono","cyberpunk"], 0,
              c => c.ColorScheme, (c, v) => { c.ColorScheme = v; ApplyColorScheme(c, v); }, "default"),

            P("BorderStyle",      "WAYCODER_BORDER_STYLE",      "CORECODER_BORDER_STYLE",
              "边框类型", "🎨 界面", "对话框和面板的边框样式",
              "select", ["rounded","single","double","bold"], 1,
              c => c.BorderStyle, (c, v) => c.BorderStyle = v, "rounded"),

            P("BorderColor",      "WAYCODER_BORDER_COLOR",      "CORECODER_BORDER_COLOR",
              "边框颜色", "🎨 界面", "ANSI 色号: 36=青 32=绿 33=黄 35=紫 34=蓝 37=白",
              "select", ["36","32","33","35","34","37"], 2,
              c => c.BorderColor, (c, v) => c.BorderColor = v, "36"),

            P("AccentColor",      "WAYCODER_ACCENT_COLOR",      "CORECODER_ACCENT_COLOR",
              "强调色", "🎨 界面", "标题和选中高亮的颜色",
              "select", ["36","32","33","35","34","37"], 3,
              c => c.AccentColor, (c, v) => c.AccentColor = v, "36"),
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

    /// <summary>
    /// 从环境变量加载配置。也支持从当前目录向上查找到 home 目录的 .env 文件。
    /// </summary>
    public static Config FromEnv()
    {
        LoadDotEnv();

        var config = new Config();

        // Schema 驱动的批量加载
        foreach (var p in _schema)
        {
            var val = Env(p.EnvVar, p.OldEnvVar);
            if (val != null) p.Setter(config, val);
        }

        // 特殊处理：ApiKey 多路回退
        if (string.IsNullOrEmpty(config.ApiKey))
        {
            config.ApiKey = Environment.GetEnvironmentVariable("OPENAI_API_KEY")
                ?? Environment.GetEnvironmentVariable("DEEPSEEK_API_KEY")
                ?? Environment.GetEnvironmentVariable("ANTHROPIC_API_KEY")
                ?? Environment.GetEnvironmentVariable("API_KEY")
                ?? "";
        }

        // 特殊处理：BaseUrl 多路回退
        if (string.IsNullOrEmpty(config.BaseUrl))
        {
            config.BaseUrl = Environment.GetEnvironmentVariable("OPENAI_BASE_URL");
        }

        return config;
    }

    // ════════════════════════════════════════════════════════════
    // 设置界面元数据（从 Schema 自动生成）
    // ════════════════════════════════════════════════════════════

    public static List<SettingDef> SettingSchema() =>
        _schema.Select(p => new SettingDef(
            p.Key, p.Label, p.Category, p.Desc,
            p.Type, p.Options, p.EnvVar, p.Order
        )).ToList();

    // ════════════════════════════════════════════════════════════
    // 保存到 .env 文件（从 Schema 自动生成）
    // ════════════════════════════════════════════════════════════

    public void SaveToEnvFile()
    {
        var envPath = FindEnvFile() ?? Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".waycoder", ".env");
        var dir = Path.GetDirectoryName(envPath);
        if (dir != null) Directory.CreateDirectory(dir);
        var lines = File.Exists(envPath) ? File.ReadAllLines(envPath).ToList() : [];

        foreach (var p in _schema)
        {
            var val = p.Getter(this);
            if (p.SkipIfEmpty && string.IsNullOrEmpty(val)) continue;
            if (p.DefaultStr != null && val == p.DefaultStr) continue;
            ApplyOrAppend(lines, p.EnvVar, val);
        }

        File.WriteAllLines(envPath, lines);
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

                var eqIdx = trimmed.IndexOf('=');
                if (eqIdx <= 0 || eqIdx >= trimmed.Length - 1) continue;

                var key = trimmed[..eqIdx].Trim();
                var value = trimmed[(eqIdx + 1)..].Trim();

                if ((value.StartsWith('"') && value.EndsWith('"'))
                    || (value.StartsWith('\'') && value.EndsWith('\'')))
                    value = value[1..^1];

                if (Environment.GetEnvironmentVariable(key) == null)
                    Environment.SetEnvironmentVariable(key, value);
            }
        }
        catch { /* 静默跳过无法读取的 .env 文件 */ }
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
