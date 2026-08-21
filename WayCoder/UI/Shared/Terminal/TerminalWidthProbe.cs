using System.Globalization;
using System.Text;
using WayCoder.UI.Shared;

namespace WayCoder.UI.Shared.Terminal;

/// <summary>
/// 终端字符宽度实测探针 —— 用「+字符*」+ 光标位置查询（CPR, Cursor Position Report）测量
/// 任意字符在<em>当前终端字体</em>下的真实显示列宽。
///
/// 原理：把光标移到行首 → 输出 `+{字符}*` → 发 `\x1b[6n` 查询光标列 → 终端回 `\x1b[{row};{col}R`。
/// `+` 占 1 列、`*` 占 1 列、光标落在 `*` 之后一列，故 col = 3 + 字符宽 → 字符宽 = col - 3。
///
/// 用途：静态宽度表 AnsiString.CharWidth 是手写 Unicode 区间，可能与某终端字体有偏差
/// （emoji 宽窄、某些符号 1/2 列、零宽字符等）。本探针给出「终端真相」，用于校准静态表
/// 或生成运行时实测缓存。自测段与 --width-probe 命令共用。
///
/// 限制：
/// - 仅限真实 TTY（管道/重定向/CI 下 CPR 无响应，CanProbe=false，测量返回 null）
/// - 假定等宽字体（TUI 主场景本就如此）
/// - 逐字符测量有 IO 往返开销，适合校准/调试，不适合渲染热路径
/// </summary>
public static class TerminalWidthProbe
{
    /// <summary>当前进程是否能做终端实测：stdin/stdout 均为真实终端（非管道重定向）且非 CI。</summary>
    public static bool CanProbe
    {
        get
        {
            if (Console.IsInputRedirected || Console.IsOutputRedirected) return false;
            var ci = Environment.GetEnvironmentVariable("CI");
            return string.IsNullOrEmpty(ci);
        }
    }

    /// <summary>
    /// 实测单个字符/字符串的显示列宽。输出 `+{text}*` 后读取光标列：
    /// Windows 用 Console.CursorLeft（原生可读）；Unix/macOS/Linux 用 CPR 光标位置查询
    /// （`\x1b[6n` 回 `\x1b[{row};{col}R`），宽度 = 列 - 3。
    /// 超时（终端不响应）返回 null。测量后清理当前行，不留痕迹。
    /// </summary>
    public static int? MeasureDisplayWidth(string text, int timeoutMs = 500)
    {
        if (!CanProbe) return null;

        // ── Windows：CursorLeft 原生可读当前光标列（0 基）──
        // `+` 占列 0，text 占 1..w，`*` 占列 w+1，光标落在列 w+2 → w = col - 2
        if (OperatingSystem.IsWindows())
        {
            Console.Out.Write("\x1b[1G\x1b[K+");
            Console.Out.Write(text);
            Console.Out.Write("*");
            Console.Out.Flush();
            int col = Console.CursorLeft;
            Console.Out.Write("\x1b[1G\x1b[K");
            Console.Out.Flush();
            return col >= 2 ? col - 2 : null;
        }

        // ── Unix：临时 raw 输入 + CPR 读原始字节 ──
        // 行缓冲（ICANON）会吞掉无换行的 CPR 回复、ECHO 会回显污染，必须 EnterRaw。
        var orig = TerminalRawMode.EnterRaw();
        try
        {
            Console.Out.Write("\x1b[1G\x1b[K+");
            Console.Out.Write(text);
            Console.Out.Write("*\x1b[6n");
            Console.Out.Flush();
            // CPR 返回 1 基列：`+` 占 1，text 占 2..1+w，`*` 占列 2+w，光标落在列 3+w → w = col - 3
            var col = ReadCprColumn(timeoutMs);
            Console.Out.Write("\x1b[1G\x1b[K");
            Console.Out.Flush();
            return col is int c && c >= 3 ? c - 3 : null;
        }
        finally { TerminalRawMode.Restore(orig); }
    }

    // ═══════════════════════════════════════════════════════════════
    //  校准报告（供 --width-probe 命令与自测段共用）
    // ═══════════════════════════════════════════════════════════════

    /// <summary>单个待测字符的描述项：字符、用途标签。</summary>
    public readonly record struct ProbeItem(string Char, string Label);

    /// <summary>一次实测结果：字符、标签、静态表宽、实测宽（null=不可测）、是否一致。</summary>
    public readonly record struct ProbeResult(string Char, string Label, int StaticWidth, int? ActualWidth)
    {
        public bool Consistent => ActualWidth is int a && a == StaticWidth;
    }

    /// <summary>
    /// 代表字符集 —— 覆盖静态表所有特判区段 + 易错类别：
    /// ASCII 窄、CJK/全角宽、符号特判 1 列（✓→/几何/箭头/⌘）、emoji 2 列、韩文、零宽组合符。
    /// </summary>
    public static readonly ProbeItem[] ProbeSet =
    [
        // ── ASCII 窄（静态 1）──
        new("A", "ASCII 大写"),      new("a", "ASCII 小写"),    new("1", "ASCII 数字"),
        new("!", "ASCII 符号"),
        // ── CJK / 全角宽（静态 2）──
        new("中", "汉字"),            new("道", "汉字"),          new("汉", "汉字"),
        new("Ａ", "全角 A"),          new("。", "句号"),          new("、", "顿号"),
        new("「", "日引号开"),        new("」", "日引号闭"),
        new("…", "省略号 U+2026"),    new("—", "破折号 U+2014"),  new("“", "左引号"), new("”", "右引号"),
        // ── 符号特判 1 列（静态 1）──
        new("✓", "对勾 U+2713"),     new("✔", "重对勾 U+2714"),  new("✕", "乘 U+2715"),
        new("✖", "重乘 U+2716"),     new("✗", "叉 U+2717"),      new("✘", "重叉 U+2718"),
        new("←", "左箭头"),          new("→", "右箭头"),        new("↑", "上箭头"),
        new("↔", "双向箭头"),        new("⇄", "交换箭头 U+21C4"),
        new("■", "方块"),            new("□", "空方块"),        new("▲", "上三角"),
        new("△", "空三角"),          new("●", "实心圆"),        new("○", "空圆"),
        new("·", "中点 U+00B7"),     new("•", "项目符 U+2022"),  new("◦", "白项目符 U+25E6"),
        new("⌘", "命令符 U+2318"),
        // ── 媒体控制 + 时钟 emoji（静态 2，23E9-23F3）──
        new("⏱", "计时器 U+23F1"),     new("⏰", "闹钟 U+23F0"),
        new("⏳", "沙漏 U+23F3"),       new("⏩", "快进 U+23E9"),
        // ── 杂项符号与箭头（静态 2，2B00-2BFF）──
        new("⭐", "星 U+2B50"),         new("⬛", "黑大方 U+2B1B"),
        new("⬜", "白大方 U+2B1C"),     new("⭕", "大圆 U+2B55"),
        new("⬆", "上箭头 U+2B06"),
        // ── emoji 宽（静态 2）──
        new("★", "五角星 U+2605"),   new("❤", "红心 U+2764"),    new("☀", "太阳 U+2600"),
        new("⚡", "闪电 U+26A1"),     new("📦", "包裹 U+1F4E6"),   new("🚀", "火箭 U+1F680"),
        new("✅", "白勾 U+2705"),
        // ── 韩文（静态 2）──
        new("가", "韩文音节"),        new("한", "韩文音节"),
        // ── 零宽字符（静态 0，探针应测出 0 列）──
        new("́", "组合重音 U+0301"), new("​", "零宽空格 U+200B"),
    ];

    /// <summary>
    /// 逐个实测 ProbeSet 中全部字符，返回「静态表宽 vs 实测宽」结果表。
    /// 非 TTY 时所有 ActualWidth 为 null（Consistent=false，不可测）。
    /// </summary>
    public static List<ProbeResult> ProbeAll(int timeoutMs = 500)
    {
        var results = new List<ProbeResult>(ProbeSet.Length);
        foreach (var item in ProbeSet)
        {
            var staticWidth = item.Char.EnumerateRunes().All(r => AnsiString.CharWidth(r) == 0)
                ? 0   // 全零宽（如组合符）→ 0
                : item.Char.EnumerateRunes().Sum(r => AnsiString.CharWidth(r));
            var actual = MeasureDisplayWidth(item.Char, timeoutMs);
            results.Add(new ProbeResult(item.Char, item.Label, staticWidth, actual));
        }
        return results;
    }

    /// <summary>实测与静态表不一致（且可测）的项，供校准静态表。</summary>
    public static IEnumerable<ProbeResult> Mismatches(IEnumerable<ProbeResult> results)
        => results.Where(r => r.ActualWidth != null && !r.Consistent);

    /// <summary>
    /// 打印实测报告（--width-probe 命令入口）。
    /// dir 为 null → 测内置 ProbeSet 代表字符，输出全表；
    /// dir 为目录 → 扫描该目录源码文件，实测其中出现的全部非 ASCII 字符，输出不一致项 + 宽度分布汇总。
    /// 非 TTY 时提示并返回 1。返回：不一致项数（0 = 静态表与当前终端完全吻合）。
    /// </summary>
    public static int PrintReport(string? dir = null, int timeoutMs = 500)
    {
        if (!CanProbe)
        {
            Console.WriteLine("无法实测：需要真实终端（stdin/stdout 被重定向或处于 CI）。请直接在终端运行 --width-probe。");
            return 1;
        }

        List<ProbeResult> results;
        if (dir != null)
        {
            if (!Directory.Exists(dir))
            {
                Console.WriteLine($"目录不存在：{dir}");
                return 1;
            }
            Console.WriteLine($"扫描源码字符：{dir}");
            var chars = ScanNonAsciiChars(dir);
            Console.WriteLine($"发现 {chars.Count} 个非 ASCII 唯一字符，逐个实测（每个一次 CPR 往返）…");
            results = ProbeAllChars(chars, timeoutMs);
            return PrintScanResults(results);
        }

        Console.WriteLine("终端字符宽度实测（+字符* + CPR 光标列） vs 静态宽度表 AnsiString.CharWidth");
        Console.WriteLine("── 字符 ── 名称 ───────────── 静态 ─ 实测 ─ 判定 ─");
        results = ProbeAll(timeoutMs);
        return PrintTableResults(results);
    }

    /// <summary>ProbeSet 全表报告（字符量少，逐行输出）。</summary>
    private static int PrintTableResults(List<ProbeResult> results)
    {
        int measured = 0, mismatch = 0;
        foreach (var r in results)
        {
            if (r.ActualWidth is not int a) continue;
            measured++;
            var mark = r.Consistent ? "✅" : "❌";
            if (!r.Consistent) mismatch++;
            Console.WriteLine($"  {Cell(Visible(r.Char), 4)} {Cell(r.Label, 22)} {r.StaticWidth,4} {a,5}   {mark}");
        }
        Console.WriteLine($"\n共 {measured} 项可测：一致 {measured - mismatch}，不一致 {mismatch}");
        if (mismatch == 0)
            Console.WriteLine("静态宽度表与当前终端完全吻合，无需校准。");
        else
            Console.WriteLine("不一致项即静态表 AnsiString.CharWidth 需按当前终端字体修正的字符。");
        return mismatch;
    }

    /// <summary>扫描模式报告：先列出全部不一致项，再按实测宽度统计源码字符分布。</summary>
    private static int PrintScanResults(List<ProbeResult> results)
    {
        int measured = 0, mismatch = 0;
        var byWidth = new Dictionary<int, int>();
        foreach (var r in results)
        {
            if (r.ActualWidth is not int a) continue;
            measured++;
            byWidth[a] = byWidth.GetValueOrDefault(a) + 1;
            if (!r.Consistent)
            {
                mismatch++;
                Console.WriteLine($"  ❌ {Cell(Visible(r.Char), 4)} {Cell(r.Label, 26)} 静态{r.StaticWidth} → 实测{a}");
            }
        }

        Console.WriteLine($"\n源码字符实测分布：");
        foreach (var (w, n) in byWidth.OrderBy(kv => kv.Key))
            Console.WriteLine($"  宽 {w} 列：{n} 个");
        Console.WriteLine($"共 {measured} 项可测：一致 {measured - mismatch}，不一致 {mismatch}");
        if (mismatch == 0)
            Console.WriteLine("静态宽度表与当前终端完全吻合，无需校准。");
        else
            Console.WriteLine("❌ 列出的不一致项即静态表 AnsiString.CharWidth 需修正的字符。");
        return mismatch;
    }

    // ═══════════════════════════════════════════════════════════════
    //  源码字符扫描
    // ═══════════════════════════════════════════════════════════════

    /// <summary>
    /// 递归扫描目录下源码文件，返回其中出现过的全部非 ASCII 字符（按首次出现顺序去重）。
    /// 跳过 bin/obj/.git 目录与超大文件（&gt;512KB），避免把构建产物/生成资源卷进来。
    /// </summary>
    public static List<string> ScanNonAsciiChars(string root, int maxFileBytes = 512 * 1024)
    {
        var result = new List<string>();
        var seen = new HashSet<string>();
        foreach (var file in EnumerateSourceFiles(root))
        {
            try
            {
                if (new FileInfo(file).Length > maxFileBytes) continue;
                using var sr = new StreamReader(file, Encoding.UTF8);
                foreach (var rune in sr.ReadToEnd().EnumerateRunes())
                {
                    if (rune.Value <= 0x7F) continue;
                    var s = rune.ToString();
                    if (seen.Add(s)) result.Add(s);
                }
            }
            catch (IOException) { /* 文件被占用/不可读，跳过 */ }
            catch (UnauthorizedAccessException) { /* 权限不足，跳过 */ }
        }
        return result;
    }

    /// <summary>枚举源码文件：扩展名白名单 + 排除构建/版本控制目录。</summary>
    private static IEnumerable<string> EnumerateSourceFiles(string root)
    {
        foreach (var file in Directory.EnumerateFiles(root, "*.*", SearchOption.AllDirectories))
        {
            var ext = Path.GetExtension(file).ToLowerInvariant();
            if (ext is not (".cs" or ".tui" or ".md" or ".html" or ".js" or ".css" or ".json" or ".xml" or ".txt" or ".py" or ".sh"))
                continue;
            var dir = Path.GetDirectoryName(file) ?? "";
            if (dir.Contains($"{Path.DirectorySeparatorChar}bin")
                || dir.Contains($"{Path.DirectorySeparatorChar}obj")
                || dir.Contains($"{Path.DirectorySeparatorChar}.git")
                || dir.Contains($"{Path.DirectorySeparatorChar}node_modules"))
                continue;
            yield return file;
        }
    }

    /// <summary>逐个实测给定字符集，返回「静态表宽 vs 实测宽」结果表。</summary>
    public static List<ProbeResult> ProbeAllChars(IEnumerable<string> chars, int timeoutMs = 500)
    {
        var list = chars as IReadOnlyList<string> ?? chars.ToList();
        var results = new List<ProbeResult>(list.Count);
        for (int i = 0; i < list.Count; i++)
        {
            var s = list[i];
            var staticWidth = s.EnumerateRunes().Sum(r => AnsiString.CharWidth(r));
            var actual = MeasureDisplayWidth(s, timeoutMs);
            results.Add(new ProbeResult(s, Describe(s), staticWidth, actual));
        }
        return results;
    }

    /// <summary>字符描述：U+XXXX + Unicode 类别（扫描模式报告用）。</summary>
    private static string Describe(string s)
    {
        var r = s.EnumerateRunes().First();
        return $"U+{r.Value:X4} {Rune.GetUnicodeCategory(r)}";
    }

    /// <summary>按显示宽度左对齐补空格（CJK 按 2 列算），最小 1 空格。</summary>
    private static string Cell(string s, int width)
    {
        var w = AnsiHelper.DisplayWidth(s);
        return s + new string(' ', Math.Max(1, width - w));
    }

    /// <summary>零宽/控制字符不可见，用 ∅ 占位展示，避免干扰对齐。按 Rune 遍历（代理对不拆半）。</summary>
    private static string Visible(string s)
        => s.EnumerateRunes().All(r => r.Value < 0x20 || (r.Value is >= 0x7F and < 0xA0) || AnsiString.CharWidth(r) == 0)
            ? "∅" : s;

    // ═══════════════════════════════════════════════════════════════
    //  CPR 响应读取
    // ═══════════════════════════════════════════════════════════════

    /// <summary>
    /// 读取并解析 CPR 响应 `\x1b[{row};{col}R`，返回 col（1 基）。
    /// raw 输入模式下 Console.ReadKey 会把 `\x1b` 当 escape 前缀吞掉整个序列（实测返回空字符）、
    /// Console.OpenStandardInput().ReadAsync 也读不到（.NET stdin stream 与手动 raw 不兼容），
    /// 故用 TerminalRawMode.ReadRawByte（libc poll + read 直连 fd0）逐字节读。
    /// 超时返回 null。
    /// </summary>
    private static int? ReadCprColumn(int timeoutMs)
    {
        var bytes = new List<byte>(16);
        var deadline = Environment.TickCount64 + timeoutMs;
        while (Environment.TickCount64 < deadline)
        {
            long remaining = deadline - Environment.TickCount64;
            if (remaining <= 0) break;
            int b = TerminalRawMode.ReadRawByte((int)Math.Min(remaining, 200));
            if (b < 0) break; // 超时/EOF
            bytes.Add((byte)b);
            if (b == 'R') break;   // CSI 以 0x40-0x7E 结尾，CPR 用 'R'
            if (bytes.Count > 64) return null;   // 非 CPR 杂音，保护
        }

        if (bytes.Count < 4 || bytes[0] != 0x1b) return null; // 需含 ESC

        // bytes 形如 "\x1b[1;5R"（R 是 CSI 终止字节，已含在 bytes 里）
        // 剥离 ESC[ 和尾部 R 后拆 row;col —— 直接 Split 会把 "5R" 当数字导致 TryParse 失败
        var s = Encoding.ASCII.GetString(bytes.ToArray()).TrimStart('\x1b', '[').TrimEnd('R');
        var parts = s.Split(';');
        return parts.Length == 2 && int.TryParse(parts[1], out var col) ? col : null;
    }
}
