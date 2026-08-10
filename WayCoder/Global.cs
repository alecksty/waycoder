namespace WayCoder;

/// <summary>
/// 全局常量 —— 应用名称、版本号、开发者信息等，全项目统一引用。
/// </summary>
public static class Global
{
    // ── 应用 ──
    /// <summary>应用品牌名（英文）</summary>
    public const string AppName = "WayCoder";
    /// <summary>应用中文名</summary>
    public const string AppNameCN = "道码";
    /// <summary>应用全称</summary>
    public const string AppFullName = "WayCoder 道码 · 中文版易用编程智能体";
    /// <summary>版本号</summary>
    public const string Version = "v0.25.8";
    /// <summary>应用名 + 版本号</summary>
    public static string AppNameVersion => $"{AppName} {Version} ({AppNameCN})";

    // ── 公司 / 开发者 ──
    /// <summary>公司名称</summary>
    public const string Company = "施探宇";
    /// <summary>开发者</summary>
    public const string Developer = "施探宇 (aleck)";
    /// <summary>开发者邮箱</summary>
    public const string Email = "aleckstygit@outlook.com";
    /// <summary>联系电话</summary>
    public const string Phone = "+86 138-xxxx-xxxx";
    /// <summary>地址</summary>
    public const string Address = "中国 · 天津";

    // ── 仓库 ──
    /// <summary>Git 仓库地址</summary>
    public const string RepoUrl = "https://gitee.com/aleckstygit/my-coder";
    /// <summary>开源协议</summary>
    public const string License = "MIT";

    // ── 配置目录 ──
    /// <summary>当前配置目录名</summary>
    public const string ConfigDirName = ".waycoder";
    /// <summary>旧配置目录名（向后兼容只读）</summary>
    public const string LegacyConfigDirName = ".corecoder";

    /// <summary>搜索顺序：新目录优先，旧目录回退</summary>
    public static string[] ConfigDirSearchOrder => [ConfigDirName, LegacyConfigDirName];

    /// <summary>全局配置路径（~/。waycoder/...）</summary>
    public static string GlobalConfigPath(params string[] segments)
    {
        var parts = new string[segments.Length + 1];
        parts[0] = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ConfigDirName);
        Array.Copy(segments, 0, parts, 1, segments.Length);
        return Path.Combine(parts);
    }

    /// <summary>写配置路径：始终返回 .waycoder/ 下路径</summary>
    public static string WriteConfigPath(string cwd, params string[] segments)
    {
        var parts = new string[segments.Length + 1];
        parts[0] = Path.Combine(cwd, ConfigDirName);
        Array.Copy(segments, 0, parts, 1, segments.Length);
        return Path.Combine(parts);
    }

    /// <summary>读配置路径：先试 .waycoder/，回退 .corecoder/，都不存在返回 .waycoder/ 路径</summary>
    public static string ReadConfigPath(string cwd, params string[] segments)
    {
        foreach (var dirName in ConfigDirSearchOrder)
        {
            var parts = new string[segments.Length + 1];
            parts[0] = Path.Combine(cwd, dirName);
            Array.Copy(segments, 0, parts, 1, segments.Length);
            var path = Path.Combine(parts);
            // 检查目录是否存在（对于文件路径，检查父目录）
            var parent = segments.Length > 0 ? Path.GetDirectoryName(path) : path;
            if (parent != null && Directory.Exists(parent)) return path;
        }
        // 都不存在，返回新目录路径
        var fallbackParts = new string[segments.Length + 1];
        fallbackParts[0] = Path.Combine(cwd, ConfigDirName);
        Array.Copy(segments, 0, fallbackParts, 1, segments.Length);
        return Path.Combine(fallbackParts);
    }
}
