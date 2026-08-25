using System.Text;

namespace WayCoder;

/// <summary>
/// 全局常量 —— 应用名称、版本号、开发者信息等，全项目统一引用。
/// </summary>
public static class Global
{
    // ── 文件编码 ──
    /// <summary>UTF-8 无 BOM 编码（源文件写入默认；.NET 的 <see cref="Encoding.UTF8"/> 静态实例带 BOM，Python/严格解析器会拒绝）</summary>
    public static readonly Encoding Utf8NoBom = new UTF8Encoding(false);

    /// <summary>写文本文件：原文件带 BOM 则保留，否则写无 BOM UTF-8（write_file/编辑器/批量工具统一走这里，避免源文件被强加 BOM）。</summary>
    public static void WriteAllTextPreserveBom(string path, string content)
        => File.WriteAllText(path, content, FileStartsWithUtf8Bom(path) ? Encoding.UTF8 : Utf8NoBom);

    /// <summary>探测文件是否以 UTF-8 BOM（EF BB BF）开头。</summary>
    static bool FileStartsWithUtf8Bom(string path)
    {
        try
        {
            if (!File.Exists(path)) return false;
            using var fs = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
            var b = new byte[3];
            if (fs.Read(b, 0, 3) < 3) return false;
            return b[0] == 0xEF && b[1] == 0xBB && b[2] == 0xBF;
        }
        catch { return false; }
    }

    // ── 应用 ──
    /// <summary>应用品牌名（英文）</summary>
    public const string AppName = "WayCoder";
    /// <summary>应用中文名</summary>
    public const string AppNameCN = "道码";
    /// <summary>应用全称</summary>
    public const string AppFullName = "WayCoder 道码·通用编程智能体";
    /// <summary>版本号</summary>
    public const string Version = "v0.96.7";
    /// <summary>应用名 + 版本号</summary>
    public static string AppNameVersion => $"{AppName} {Version} ({AppNameCN})";

    // ── 公司 / 开发者 ──
    /// <summary>公司名称</summary>
    public const string Company = "深圳市探索智能科技有限公司";
    /// <summary>开发者（作者）</summary>
    public const string Developer = "施探宇 (aleck)";
    /// <summary>开发者邮箱</summary>
    public const string Email = "alecksty@163.com";
    /// <summary>联系电话</summary>
    public const string Phone = "+86 186-8039-9436";
    /// <summary>地址</summary>
    public const string Address = "中国 · 深圳";

    // ── 仓库 ──
    /// <summary>Git 仓库地址</summary>
    public const string RepoUrl = "https://gitee.com/aleckstygit/my-coder";
    /// <summary>开源协议</summary>
    public const string License = "MIT";

    // ── 配置目录 ──
    /// <summary>当前配置目录名</summary>
    public const string ConfigDirName = ".waycoder";

    /// <summary>测试/嵌入式场景可覆写的用户主目录；默认取系统用户目录。</summary>
    public static string? HomeOverride { get; set; }

    /// <summary>当前生效的用户主目录。</summary>
    public static string Home =>
        HomeOverride ?? Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);

    /// <summary>搜索顺序：新目录优先，旧目录回退</summary>
    public static string[] ConfigDirSearchOrder => [ConfigDirName];

    /// <summary>全局配置路径（~/waycoder/...）</summary>
    public static string GlobalConfigPath(params string[] segments)
    {
        var parts = new string[segments.Length + 1];
        parts[0] = Path.Combine(Home, ConfigDirName);
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

    /// <summary>全局读配置路径：~/.waycoder/... 优先，回退 ~/.corecoder/...</summary>
    public static string GlobalReadConfigPath(params string[] segments)
    {
        return ReadConfigPath(Home, segments);
    }

    /// <summary>从 cwd 向上查找存在的配置目录，返回目录名（.waycoder / .corecoder），都不存在返回 null</summary>
    public static string? FindExistingConfigDir(string cwd)
    {
        var dir = cwd;
        while (dir != null)
        {
            foreach (var dirName in ConfigDirSearchOrder)
            {
                if (Directory.Exists(Path.Combine(dir, dirName)))
                    return dirName;
            }
            var parent = Path.GetDirectoryName(dir);
            if (parent == dir) break;
            dir = parent;
        }
        return null;
    }

    /// <summary>从 cwd 向上查找配置文件（如 mcp_servers.json），返回完整路径，未找到返回 null</summary>
    public static string? FindConfigFileInTree(string cwd, string relativePath)
    {
        var dir = cwd;
        while (dir != null)
        {
            foreach (var dirName in ConfigDirSearchOrder)
            {
                var candidate = Path.Combine(dir, dirName, relativePath);
                if (File.Exists(candidate)) return candidate;
            }
            var parent = Path.GetDirectoryName(dir);
            if (parent == dir) break;
            dir = parent;
        }
        return null;
    }
}
