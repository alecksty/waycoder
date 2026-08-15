namespace WayCoder.Infra;

/// <summary>系统字体条目。</summary>
public sealed record FontEntry(string Family, string Path);

/// <summary>
/// 跨平台系统字体搜索：macOS / Windows / Linux 常见字体目录，递归枚举 .ttf/.otf。
/// 零反射、零依赖、AOT 安全。
/// </summary>
public static class FontFinder
{
    /// <summary>默认首选族名（中文优先），找不到再取任意字体。</summary>
    public static readonly string[] PreferredFamilies =
    {
        "PingFang SC", "Microsoft YaHei", "SimHei", "Noto Sans CJK SC", "WenQuanYi Micro Hei",
        "DejaVu Sans", "Arial", "Helvetica", "Segoe UI",
    };

    public static List<FontEntry> Find()
    {
        var dirs = new List<string>();
        var home = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
        if (OperatingSystem.IsWindows())
        {
            dirs.Add(@"C:\Windows\Fonts");
            var local = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            if (!string.IsNullOrEmpty(local)) dirs.Add(Path.Combine(local, "Microsoft", "Windows", "Fonts"));
        }
        else if (OperatingSystem.IsMacOS())
        {
            dirs.Add("/System/Library/Fonts");
            dirs.Add("/Library/Fonts");
            if (!string.IsNullOrEmpty(home)) dirs.Add(Path.Combine(home, "Library", "Fonts"));
        }
        else // Linux / 其它
        {
            dirs.Add("/usr/share/fonts");
            dirs.Add("/usr/local/share/fonts");
            if (!string.IsNullOrEmpty(home))
            {
                dirs.Add(Path.Combine(home, ".local", "share", "fonts"));
                dirs.Add(Path.Combine(home, ".fonts"));
            }
        }

        var result = new List<FontEntry>();
        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var dir in dirs)
        {
            if (string.IsNullOrEmpty(dir) || !Directory.Exists(dir)) continue;
            try
            {
                foreach (var file in Directory.EnumerateFiles(dir, "*", SearchOption.AllDirectories))
                {
                    var ext = Path.GetExtension(file).ToLowerInvariant();
                    if (ext is not (".ttf" or ".otf")) continue;
                    if (seen.Contains(file)) continue;
                    seen.Add(file);
                    result.Add(new FontEntry(Path.GetFileNameWithoutExtension(file), file));
                }
            }
            catch
            {
                // 目录无权限/遍历失败：忽略
            }
        }
        return result;
    }

    /// <summary>族名归一化：小写 + 仅保留字母数字（忽略空格/连字符）。</summary>
    public static string Normalize(string s)
    {
        var sb = new System.Text.StringBuilder(s.Length);
        foreach (char ch in s.ToLowerInvariant())
            if (char.IsLetterOrDigit(ch)) sb.Append(ch);
        return sb.ToString();
    }
}
