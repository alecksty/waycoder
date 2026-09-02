namespace WayCoder;

public partial class Config
{
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
        var home = Global.Home;

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
