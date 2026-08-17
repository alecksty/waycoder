using System.Text;
using System.Text.RegularExpressions;
using WayCoder.UI.Shared.Terminal;

namespace WayCoder.UI.Shared;

/// <summary>
/// 终端 UI 通用工具 —— CJK 宽度计算、文本截断、标记转义。
/// 所有方法假定 UTF-8 终端环境，中文/全角字符按 2 列宽度计算。
/// </summary>
public static class AnsiHelper
{
    // ---- CJK 宽度 ----

    /// <summary>
    /// 计算字符串在终端中的显示宽度。
    /// ASCII/窄字符 = 1 列，CJK/全角字符 = 2 列。
    /// 覆盖范围：CJK 统一汉字、日文假名、韩文、全角标点、emoji（近似）。
    /// </summary>
    /// <summary>计算纯文本的终端显示宽度（CJK=2, ASCII=1）。不含转义符。</summary>
    public static int DisplayWidth(string text)
    {
        if (string.IsNullOrEmpty(text)) return 0;
        var width = 0;
        foreach (var rune in text.EnumerateRunes())
            width += RuneWidth(rune);
        return width;
    }

    /// <summary>省略号 "…" 的终端显示宽度（全角，占 2 列）。</summary>
    private static readonly int EllipsisWidth = DisplayWidth("…");

    /// <summary>
    /// 按显示宽度截断文本（不是字符数），末尾追加 "…"。
    /// 如果文本不超出宽度，原样返回。
    /// </summary>
    public static string TruncateByWidth(string text, int maxWidth)
    {
        if (maxWidth <= 0) return "";
        if (DisplayWidth(text) <= maxWidth) return text;

        // 省略号（宽 2 列）放不下时退化为无省略号截断，避免返回超宽 "…"（maxWidth=1 时）
        var reserved = maxWidth >= EllipsisWidth ? EllipsisWidth : 0;
        var runes = text.EnumerateRunes().ToList();
        var width = 0;
        var count = 0;
        foreach (var r in runes)
        {
            var w = RuneWidth(r);
            if (width + w + reserved > maxWidth) break;
            width += w;
            count++;
        }

        var sb = new StringBuilder();
        for (var i = 0; i < count; i++)
            sb.Append(runes[i].ToString());
        if (reserved > 0) sb.Append('…');
        return sb.ToString();
    }

    /// <summary>按显示宽度截断文本，末尾不加省略号（供 WrapText 预留省略号宽度后补 "…" 用）。</summary>
    private static string TruncateByWidthPlain(string text, int maxWidth)
    {
        if (maxWidth <= 0) return "";
        if (DisplayWidth(text) <= maxWidth) return text;

        var runes = text.EnumerateRunes().ToList();
        var sb = new StringBuilder();
        int width = 0;
        foreach (var r in runes)
        {
            int w = RuneWidth(r);
            if (width + w > maxWidth) break;
            width += w;
            sb.Append(r.ToString());
        }
        return sb.ToString();
    }

    /// <summary>
    /// 按 maxWidth 折行文本，支持显式换行符（\n）和自动折行。
    /// 英文文本尽量在空格处断行，CJK 文本在字符边界断行。
    /// 超出 maxLines 时最后一行末尾追加 "…"。
    /// </summary>
    /// <param name="maxWidth">终端显示宽度上限</param>
    /// <param name="maxLines">最大行数（默认 10）</param>
    /// <returns>折行后的文本行列表（不含换行符）</returns>
    public static List<string> WrapText(string text, int maxWidth, int maxLines = 10)
    {
        if (string.IsNullOrEmpty(text)) return [""];
        if (maxWidth <= 0) return [text];

        var result = new List<string>();
        var rawLines = text.Replace("\r\n", "\n").Split('\n');
        bool truncated = false;

        foreach (var line in rawLines)
        {
            if (result.Count >= maxLines) { truncated = true; break; }
            if (WrapLine(line, maxWidth, result, maxLines))
            {
                truncated = true;
                break;
            }
        }

        // 内容被截断：最后一行末尾追加省略号（先预留省略号宽度再补）
        if (truncated && result.Count > 0)
            result[^1] = TruncateByWidthPlain(result[^1], maxWidth - EllipsisWidth) + "…";

        return result;
    }

    /// <summary>折行单行（不含 \n）。返回 true 表示因 maxLines 上限截断了未显示内容。</summary>
    private static bool WrapLine(string line, int maxWidth, List<string> result, int maxLines)
    {
        if (result.Count >= maxLines) return true; // 已满，此行未消费 → 截断

        if (DisplayWidth(line) <= maxWidth)
        {
            result.Add(line);
            return false;
        }

        // 长行需要折行
        while (line.Length > 0)
        {
            if (result.Count >= maxLines) return true; // 折行中达到上限，剩余未显示 → 截断
            if (DisplayWidth(line) <= maxWidth)
            {
                result.Add(line);
                return false;
            }

            // 查找断点：优先在空格处、否则在字符边界
            int breakIdx = FindBreakIndex(line, maxWidth);
            if (breakIdx <= 0) breakIdx = RuneIndexToStringIndex(line, 1); // 安全兜底：至少取一个完整码点，避免 emoji/扩展区汉字被切半

            result.Add(line[..breakIdx]);
            line = line[breakIdx..].TrimStart(); // 去掉段首空格
        }

        return false;
    }

    /// <summary>在 maxWidth 内查找最佳断行点（优先空格，其次字符边界）</summary>
    private static int FindBreakIndex(string text, int maxWidth)
    {
        var runes = text.EnumerateRunes().ToList();
        int width = 0;
        int lastSpace = -1;

        for (int i = 0; i < runes.Count; i++)
        {
            int rw = RuneWidth(runes[i]);
            if (width + rw > maxWidth)
            {
                // 超了：优先在最后一个空格处断
                if (lastSpace > 0) return lastSpace;
                // 否则在当前字符前断
                // 需要回溯到前一个字符的结束位置（即 i 个 rune 的 string 长度）
                return RuneIndexToStringIndex(text, i);
            }

            width += rw;

            // 记录空格位置（英文词边界）
            if (runes[i].Value == ' ')
                lastSpace = RuneIndexToStringIndex(text, i) + 1; // 断在空格之后
        }

        return text.Length; // 全放下了
    }

    /// <summary>将 rune 索引转换为 string 的 char 索引</summary>
    private static int RuneIndexToStringIndex(string text, int runeIdx)
    {
        int ri = 0;
        int si = 0;
        foreach (var r in text.EnumerateRunes())
        {
            if (ri >= runeIdx) return si;
            ri++;
            si += r.ToString().Length; // Rune 转 string 可能占 1~4 个 char
        }

        return text.Length;
    }

    /// <summary>
    /// 按显示宽度右填充空格，使总显示宽度达到 totalWidth。
    /// 用于终端文本手动对齐（Spectre Table 自动对齐时不需要此方法）。
    /// </summary>
    public static string PadRightByWidth(string text, int totalWidth)
    {
        var current = DisplayWidth(text);
        var needed = totalWidth - current;
        if (needed <= 0) return text;
        return text + new string(' ', needed);
    }

    /// <summary>
    /// 按显示宽度左填充空格，使总显示宽度达到 totalWidth。
    /// </summary>
    public static string PadLeftByWidth(string text, int totalWidth)
    {
        var current = DisplayWidth(text);
        var needed = totalWidth - current;
        if (needed <= 0) return text;
        return new string(' ', needed) + text;
    }

    // ---- 安全转义 ----

    /// <summary>
    /// 转义文本中的 « » 标记字符（避免与 markup 标签 «color»text«/» 冲突）。
    /// 跳过 ANSI 转义序列。 [ ] 不再需要转义（markup 已改用 « »）。
    /// </summary>
    public static string Esc(string? text)
    {
        if (text == null) return "";
        var sb = new StringBuilder();
        for (int i = 0; i < text.Length; i++)
        {
            if (text[i] == AnsiTty.AnsiCharPrefix)
            {
                // ANSI 序列 — 原样复制
                int j = i;
                while (j < text.Length && text[j] != 'm') j++;
                if (j < text.Length) j++; // include 'm'
                sb.Append(text[i..j]);
                i = j - 1;
            }
            else if (text[i] == '\xAB') // « — 转义左书名号（极罕见）
            {
                sb.Append("««");
            }
            else if (text[i] == '\xBB') // » — 转义右书名号（极罕见）
            {
                sb.Append("»»");
            }
            else
            {
                sb.Append(text[i]);
            }
        }

        return sb.ToString();
    }

    /// <summary>
    /// 移除类 Spectre 标记标签（如 «bold yellow»、«cyan»、«/»），
    /// 返回纯文本。用于从带标记的字符串中提取显示文本以计算宽度。
    /// </summary>
    public static string StripMarkup(string markup)
    {
        // [a-z#0-9 ]*（星号而非加号）：允许空标签内容，否则结束标记 «/»（/ 后无字母）无法匹配而残留。
        return Regex.Replace(markup, @"\xAB/?[a-z#0-9 ]*\xBB", "");
    }

    // ---- 内部 ----

    /// <summary>
    /// 计算单个 Rune 的终端显示宽度。
    /// 委托给 Terminal 层唯一的宽度真源 AnsiString.CharWidth，避免双份实现分叉。
    /// </summary>
    internal static int RuneWidth(Rune rune) => AnsiString.CharWidth(rune);

    // ── 边框字符映射 ──

    /// <summary>边框字符集：左上 右上 左下 右下 水平 垂直，上水平 下水平（默认同 H）</summary>
    /// <param name="TL">左上角字符</param>
    /// <param name="TR">右上角字符</param>
    /// <param name="BL">左下角字符</param>
    /// <param name="BR">右下角字符</param>
    /// <param name="H">水平字符</param>
    /// <param name="V">垂直字符</param>
    /// <param name="HTop">上水平字符（默认同 H）</param>
    /// <param name="HBottom">下水平字符（默认同 H）</param>
    public record struct BorderChars(
        string TL,
        string TR,
        string BL,
        string BR,
        string H,
        string V,
        string? HTop = null,
        string? HBottom = null)
    {
        /// <summary>上边框水平线（默认同 H）</summary>
        public string HT => HTop ?? H;

        /// <summary>下边框水平线（默认同 H）</summary>
        public string HB => HBottom ?? H;
    }

    /// <summary>根据边框样式获取对应的 Unicode 边框字符集</summary>
    public static BorderChars GetBorderChars(WindowBorder border) => border switch
    {
        WindowBorder.None => new(" ", " ", " ", " ", " ", " "),
        WindowBorder.Double => new("╔", "╗", "╚", "╝", "═", "║"),
        WindowBorder.Thick => new("┏", "┓", "┗", "┛", "━", "┃"),
        WindowBorder.Single => new("┌", "┐", "└", "┘", "─", "│"),
        WindowBorder.Solid => new("█", "█", "█", "█", "█", "█", HTop: "▀", HBottom: "▄"),
        WindowBorder.Dotted => new("┌", "┐", "└", "┘", "┄", "┆"),
        WindowBorder.Dashed => new("┌", "┐", "└", "┘", "┅", "┇"),
        WindowBorder.Ascii => new("+", "+", "+", "+", "-", "|"),
        WindowBorder.Slash => new("/", "\\", "\\", "/", "-", "|"),
        WindowBorder.Triangle => new("▶", "◀", "◀", "▶", "─", "│"),
        _ => new("╭", "╮", "╰", "╯", "─", "│"), // Rounded
    };
}

/// <summary>窗口边框样式（与 BorderChars 配套，供 GetBorderChars 映射）</summary>
public enum WindowBorder
{
    None, Single, Double, Rounded, Thick,
    Solid, Dotted, Dashed, Ascii, Slash, Triangle
}