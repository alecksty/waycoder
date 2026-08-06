
namespace CoreCoderSharp;

/// <summary>
/// 配置 - 环境变量和默认值。
/// </summary>
public class Config
{
    public string Model { get; set; } = "deepseek-v4-flash";
    public string ApiKey { get; set; } = "";
    public string? BaseUrl { get; set; }
    public int MaxTokens { get; set; } = 4096;
    public float Temperature { get; set; } = 0.0f;
    public int MaxContextTokens { get; set; } = 128_000;
    public string Provider { get; set; } = "openai";

    /// <summary>最大预算（美元），null 表示无限制</summary>
    public double? MaxBudgetUsd { get; set; }

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
}
