
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

    // ---- 界面主题 ----
    /// <summary>边框类型: single | double | rounded | bold</summary>
    public string BorderStyle { get; set; } = "rounded";
    /// <summary>边框颜色 (ANSI 色号): 36=青 32=绿 33=黄 35=紫 34=蓝 37=白</summary>
    public string BorderColor { get; set; } = "36";
    /// <summary>标题/强调色 (ANSI 色号)</summary>
    public string AccentColor { get; set; } = "36";
    /// <summary>预设配色方案: default | ocean | forest | sunset | mono | cyberpunk</summary>
    public string ColorScheme { get; set; } = "default";

    /// <summary>
    /// 从环境变量加载配置。也支持从当前目录向上查找到 home 目录的 .env 文件。
    /// </summary>
    public static Config FromEnv()
    {
        LoadDotEnv();

        // 先用默认值创建，再逐个用环境变量覆盖
        var config = new Config();

        var envModel = Environment.GetEnvironmentVariable("CORECODER_MODEL");
        if (envModel != null) config.Model = envModel;

        var envSmallModel = Environment.GetEnvironmentVariable("CORECODER_SMALL_MODEL");
        if (envSmallModel != null) config.SmallModel = envSmallModel;

        var envApiKey = Environment.GetEnvironmentVariable("CORECODER_API_KEY")
                        ?? Environment.GetEnvironmentVariable("OPENAI_API_KEY")
                        ?? Environment.GetEnvironmentVariable("DEEPSEEK_API_KEY");
        if (envApiKey != null) config.ApiKey = envApiKey;

        var envBaseUrl = Environment.GetEnvironmentVariable("OPENAI_BASE_URL")
                         ?? Environment.GetEnvironmentVariable("CORECODER_BASE_URL");
        if (envBaseUrl != null) config.BaseUrl = envBaseUrl;

        if (int.TryParse(Environment.GetEnvironmentVariable("CORECODER_MAX_TOKENS"), out var mt))
            config.MaxTokens = mt;
        if (float.TryParse(Environment.GetEnvironmentVariable("CORECODER_TEMPERATURE"), out var t))
            config.Temperature = t;
        if (int.TryParse(Environment.GetEnvironmentVariable("CORECODER_MAX_CONTEXT"), out var mc))
            config.MaxContextTokens = mc;

        var envProvider = Environment.GetEnvironmentVariable("CORECODER_PROVIDER");
        if (envProvider != null) config.Provider = envProvider;

        if (double.TryParse(Environment.GetEnvironmentVariable("CORECODER_MAX_BUDGET_USD"), out var budget))
            config.MaxBudgetUsd = budget;

        if (bool.TryParse(Environment.GetEnvironmentVariable("CORECODER_AUTO_COMMIT"), out var ac))
            config.AutoGitCommit = ac;

        if (bool.TryParse(Environment.GetEnvironmentVariable("CORECODER_WATCH"), out var wm))
            config.WatchMode = wm;

        // 主题
        var envBorder = Environment.GetEnvironmentVariable("CORECODER_BORDER_STYLE");
        if (envBorder != null) config.BorderStyle = envBorder;
        var envBorderColor = Environment.GetEnvironmentVariable("CORECODER_BORDER_COLOR");
        if (envBorderColor != null) config.BorderColor = envBorderColor;
        var envAccent = Environment.GetEnvironmentVariable("CORECODER_ACCENT_COLOR");
        if (envAccent != null) config.AccentColor = envAccent;
        var envScheme = Environment.GetEnvironmentVariable("CORECODER_COLOR_SCHEME");
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
            "CORECODER_MODEL", 0),
        new("SmallModel", "小模型 (简单任务)", "🤖 模型", "补全/摘要/压缩 (便宜快速)",
            "select", ["deepseek-v4-flash","gpt-5.4-mini","gpt-4o-mini","deepseek-v4-pro"],
            "CORECODER_SMALL_MODEL", 1),
        new("BaseUrl", "API 地址", "🤖 模型", "API 端点 URL",
            "text", null, "OPENAI_BASE_URL", 2),
        new("ApiKey", "API 密钥", "🤖 模型", "API 密钥 (已隐藏)",
            "secret", null, "CORECODER_API_KEY", 3),

        // 参数
        new("MaxTokens", "最大 Token", "⚙ 参数", "每次请求最大 Token 数",
            "number", null, "CORECODER_MAX_TOKENS", 0),
        new("Temperature", "温度", "⚙ 参数", "0=精确 1=创意",
            "number", null, "CORECODER_TEMPERATURE", 1),
        new("MaxContextTokens", "上下文窗口", "⚙ 参数", "上下文窗口大小",
            "number", null, "CORECODER_MAX_CONTEXT", 2),

        // 预算
        new("MaxBudgetUsd", "预算上限 ($)", "💰 预算", "超支自动停止，留空=无限制",
            "number", null, "CORECODER_MAX_BUDGET_USD", 0),

        // 系统
        new("Provider", "提供商", "🔧 系统", "API 提供商 (openai/deepseek/...)",
            "text", null, "CORECODER_PROVIDER", 0),
        new("AutoGitCommit", "Git 自动提交", "🔧 系统", "工具执行后自动 git commit",
            "select", ["false", "true"], "CORECODER_AUTO_COMMIT", 1),
        new("WatchMode", "Watch 模式", "🔧 系统", "监听外部编辑器 AI! 注释自动触发 Agent",
            "select", ["false", "true"], "CORECODER_WATCH", 2),

        // 界面主题
        new("ColorScheme", "配色方案", "🎨 界面", "预设配色 (覆盖下方颜色设置)",
            "select", ["default", "ocean", "forest", "sunset", "mono", "cyberpunk"],
            "CORECODER_COLOR_SCHEME", 0),
        new("BorderStyle", "边框类型", "🎨 界面", "对话框和面板的边框样式",
            "select", ["rounded", "single", "double", "bold"],
            "CORECODER_BORDER_STYLE", 1),
        new("BorderColor", "边框颜色", "🎨 界面", "ANSI 色号: 36=青 32=绿 33=黄 35=紫 34=蓝 37=白",
            "select", ["36", "32", "33", "35", "34", "37"],
            "CORECODER_BORDER_COLOR", 2),
        new("AccentColor", "强调色", "🎨 界面", "标题和选中高亮的颜色",
            "select", ["36", "32", "33", "35", "34", "37"],
            "CORECODER_ACCENT_COLOR", 3),
    ];

    /// <summary>将当前配置写回 .env 文件</summary>
    public void SaveToEnvFile()
    {
        var envPath = FindEnvFile() ?? Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".corecoder", ".env");
        var dir = Path.GetDirectoryName(envPath);
        if (dir != null) Directory.CreateDirectory(dir);
        var lines = File.Exists(envPath) ? File.ReadAllLines(envPath).ToList() : [];
        ApplyOrAppend(lines, "CORECODER_MODEL", Model);
        ApplyOrAppend(lines, "CORECODER_SMALL_MODEL", SmallModel);
        if (!string.IsNullOrEmpty(BaseUrl)) ApplyOrAppend(lines, "OPENAI_BASE_URL", BaseUrl);
        ApplyOrAppend(lines, "CORECODER_API_KEY", ApiKey);
        ApplyOrAppend(lines, "CORECODER_MAX_TOKENS", MaxTokens.ToString());
        ApplyOrAppend(lines, "CORECODER_TEMPERATURE", Temperature.ToString("F1"));
        ApplyOrAppend(lines, "CORECODER_MAX_CONTEXT", MaxContextTokens.ToString());
        if (MaxBudgetUsd.HasValue) ApplyOrAppend(lines, "CORECODER_MAX_BUDGET_USD", MaxBudgetUsd.Value.ToString("F2"));
        ApplyOrAppend(lines, "CORECODER_PROVIDER", Provider);
        ApplyOrAppend(lines, "CORECODER_AUTO_COMMIT", AutoGitCommit.ToString().ToLowerInvariant());
        ApplyOrAppend(lines, "CORECODER_WATCH", WatchMode.ToString().ToLowerInvariant());
        ApplyOrAppend(lines, "CORECODER_BORDER_STYLE", BorderStyle);
        ApplyOrAppend(lines, "CORECODER_BORDER_COLOR", BorderColor);
        ApplyOrAppend(lines, "CORECODER_ACCENT_COLOR", AccentColor);
        ApplyOrAppend(lines, "CORECODER_COLOR_SCHEME", ColorScheme);
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
