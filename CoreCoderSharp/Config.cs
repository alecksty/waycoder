
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
