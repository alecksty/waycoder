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
        var home = WayCoder.Global.Home;
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
        else if (OperatingSystem.IsAndroid())
        {
            // ⚠ **Android 必须单列一支**：`OperatingSystem.IsLinux()` 在 Android 上返回 **false**，
            // 于是它原来掉进下面的 Linux 分支、只找 `/usr/share/fonts` —— 那个目录在 Android 上
            // 根本不存在 ⇒ **一个系统字体都找不到**，画布里的中文全渲染成豆腐块（实测）。
            dirs.Add("/system/fonts");
            dirs.Add("/system/fonts/noto");       // 部分 ROM 把 Noto 单独放一层
            if (!string.IsNullOrEmpty(home))
            {
                dirs.Add(Path.Combine(home, "fonts"));      // 随包带进来的字体
                dirs.Add(Path.Combine(home, "vml", "fonts"));
            }
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
                    // `.ttc` 是**字体集合**（Android 的中日韩字体基本都是它，如 NotoSansCJK-Regular.ttc）
                    // —— 原来只认 .ttf/.otf，等于在 Android 上把唯一带中文字形的那几个全跳过了。
                    // 解析侧按"取集合里第一个字体"处理（见 TrueTypeFont）。
                    if (ext is not (".ttf" or ".otf" or ".ttc")) continue;
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
