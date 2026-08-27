using Microsoft.Maui.Controls;
using Microsoft.Maui.Graphics;
using WayCoder.UI.Shared;
using WayCoder.UI.Tui.Edit;

namespace WayCoder.Maui.Markup;

/// <summary>
/// «» 中间格式 → MAUI 富文本（FormattedString）渲染器。
///
/// 复用主工程 UI/Shared 的 <see cref="MarkdownParser.ParseInline"/>（已被 MAUI 共享源码编译）
/// 解析 «tag»…«/» 与基础 markdown inline，得到 (text, color, bg) 三元组，再映射到 MAUI：
///   - color 1-9   → 样式（1=bold 2=dim 3=italic 4=underline 9=strikethrough）
///   - color ≥0x1000000 → 真彩 RGB（AnsiTty.RgbCode 编码，提取低 24 位）
///   - color 30-37/90-97/扩展色 → 命名色（对齐 Windows Terminal/VSCode 默认 16 色）
/// 颜色值同源主工程 AnsiColors / MarkdownParser，保证与 CLI/TUI/Web/GUI 四端观感一致。
/// </summary>
public static class MarkupToFormattedString
{
    private static readonly IReadOnlyDictionary<int, string> AnsiRgb = new Dictionary<int, string>
    {
        [30] = "#0C0C0C", [31] = "#C50F1F", [32] = "#13A10E", [33] = "#C19C00",
        [34] = "#0037DA", [35] = "#881798", [36] = "#3A96DD", [37] = "#CCCCCC",
        [40] = "#0C0C0C", [41] = "#C50F1F", [42] = "#13A10E", [43] = "#C19C00",
        [44] = "#0037DA", [45] = "#881798", [46] = "#3A96DD", [47] = "#CCCCCC",
        [90] = "#767676", [91] = "#E74856", [92] = "#16C60C", [93] = "#F9F1A5",
        [94] = "#3B78FF", [95] = "#B4009E", [96] = "#61D6D6", [97] = "#F2F2F2",
        [100] = "#767676", [101] = "#E74856", [102] = "#16C60C", [103] = "#F9F1A5",
        [104] = "#3B78FF", [105] = "#B4009E", [106] = "#61D6D6", [107] = "#F2F2F2",
        [208] = "#FF8700", [172] = "#D78700", [247] = "#9E9E9E",
    };

    private static readonly Color DarkDefault = Color.FromArgb("#E0E0E0");
    private static readonly Color LightDefault = Color.FromArgb("#1A1A1A");
    private static readonly Color DarkDim = Color.FromArgb("#888888");
    private static readonly Color LightDim = Color.FromArgb("#666666");

    /// <summary>把 «» 中间格式文本解析成 MAUI FormattedString（自适应深浅主题默认色）。
    /// 同时支持 ```lang 围栏代码块：块内用 Syntax 逐行 Tokenize 语法高亮。</summary>
    public static FormattedString Convert(string? markup, bool isDark)
    {
        var fs = new FormattedString();
        if (string.IsNullOrEmpty(markup)) return fs;
        RenderSegments(markup, fs, isDark);
        return fs;
    }

    /// <summary>按行渲染：围栏块 / markdown 表格块走专门渲染，其余累积后走 ParseInline（保留跨行 «» 块）。</summary>
    private static void RenderSegments(string markup, FormattedString fs, bool isDark)
    {
        var lines = markup.Replace("\r\n", "\n").Split('\n');
        var inline = new System.Text.StringBuilder();
        int i = 0;

        void FlushInline()
        {
            if (inline.Length == 0) return;
            RenderInline(inline.ToString(), fs, isDark);
            inline.Clear();
        }

        while (i < lines.Length)
        {
            var line = lines[i];

            // 围栏代码块 ```lang
            if (line.TrimStart().StartsWith("```"))
            {
                FlushInline();
                var lang = line.TrimStart()[3..].Trim();
                var code = new System.Text.StringBuilder();
                i++;
                while (i < lines.Length && !lines[i].TrimStart().StartsWith("```"))
                {
                    code.AppendLine(lines[i]);
                    i++;
                }
                i++; // 跳过闭合 ```（可能越界=未闭合）
                var syntax = lang.Length > 0 ? Syntax.ByLanguage(lang) : Syntax.Detect(code.ToString()) ?? Syntax.ByLanguage("");
                if (syntax.Name != "纯文本")
                    RenderCode(code.ToString().TrimEnd('\n'), syntax, fs, isDark);
                else
                    RenderInline("```" + lang + "\n" + code + "```", fs, isDark);
                continue;
            }

            // markdown 表格块：当前行以 | 开头，且下一行是分隔线（|---|---|）
            if (line.TrimStart().StartsWith('|') && IsTableSeparator(lines, i + 1))
            {
                FlushInline();
                var tbl = new List<string>();
                while (i < lines.Length && lines[i].TrimStart().StartsWith('|'))
                {
                    tbl.Add(lines[i]);
                    i++;
                }
                RenderTable(tbl, fs, isDark);
                continue;
            }

            inline.Append(line);
            if (i < lines.Length - 1) inline.Append('\n');
            i++;
        }
        FlushInline();
    }

    private static bool IsTableSeparator(string[] lines, int idx)
    {
        if (idx >= lines.Length) return false;
        var s = lines[idx].Trim();
        if (!s.StartsWith('|')) return false;
        foreach (var c in s)
            if (c is not ('|' or '-' or ':' or ' ' or '\t')) return false;
        return s.Contains('-');
    }

    private static string[] ParseTableRow(string line)
    {
        var s = line.Trim();
        if (s.StartsWith('|')) s = s[1..];
        if (s.EndsWith('|')) s = s[..^1];
        return s.Split('|').Select(c => c.Trim()).ToArray();
    }

    private static bool IsTableSeparatorRow(string[] cells)
        => cells.Length > 0 && cells.All(c => c.All(ch => ch is '-' or ':' or ' ' or '\t'));

    /// <summary>markdown 表格 → 等宽对齐文本（列宽补齐 + Courier New 等宽 + 表头加粗）。</summary>
    private static void RenderTable(List<string> rawLines, FormattedString fs, bool isDark)
    {
        var rows = rawLines.Select(ParseTableRow).ToList();
        if (rows.Count < 2) { foreach (var l in rawLines) RenderInline(l + "\n", fs, isDark); return; }

        var cols = rows.Max(r => r.Length);
        var widths = new int[cols];
        for (int c = 0; c < cols; c++)
            widths[c] = rows.Select(r => c < r.Length ? r[c].Length : 0).Max();

        for (int r = 0; r < rows.Count; r++)
        {
            if (IsTableSeparatorRow(rows[r])) continue; // 跳过 |---|---| 分隔行

            var sb = new System.Text.StringBuilder("| ");
            for (int c = 0; c < cols; c++)
                sb.Append((c < rows[r].Length ? rows[r][c] : "").PadRight(widths[c])).Append(" | ");

            var span = new Span
            {
                Text = sb.ToString().TrimEnd(),
                FontFamily = "Courier New",
                TextColor = ColorForToken(0, isDark),
            };
            if (r == 0) span.FontAttributes = FontAttributes.Bold; // 表头加粗
            fs.Spans.Add(span);
            if (r < rows.Count - 1) fs.Spans.Add(new Span { Text = "\n" });
        }
    }

    /// <summary>单段 ParseInline 渲染（非代码块段）。</summary>
    private static void RenderInline(string segment, FormattedString fs, bool isDark)
    {
        var defaultColor = isDark ? DarkDefault : LightDefault;
        var dimColor = isDark ? DarkDim : LightDim;

        foreach (var (text, color, bg) in MarkdownParser.ParseInline(segment))
        {
            var span = new Span { Text = text, TextColor = ResolveFg(color, defaultColor, dimColor) };

            switch (color)
            {
                case 1: span.FontAttributes = FontAttributes.Bold; break;        // bold/bright
                case 3: span.FontAttributes = FontAttributes.Italic; break;      // italic
                case 4: span.TextDecorations = TextDecorations.Underline; break;
                case 9: span.TextDecorations = TextDecorations.Strikethrough; break;
            }

            if (bg >= 30) span.BackgroundColor = ResolveColor(bg, Colors.Transparent);

            fs.Spans.Add(span);
        }
    }

    /// <summary>代码块逐行 Tokenize 上色（每行间保留换行）。相邻同色 token 合并成单个 Span，
    /// 避免大代码块拆出上万 Span 导致移动端 Label 渲染卡死（ANR）。</summary>
    private static void RenderCode(string code, Syntax syntax, FormattedString fs, bool isDark)
    {
        // 超大代码块降级纯文本，防主线程长时间分词/渲染
        if (code.Length > 100_000)
        {
            fs.Spans.Add(new Span { Text = code });
            return;
        }

        var lines = code.Replace("\r\n", "\n").Split('\n');
        for (int i = 0; i < lines.Length; i++)
        {
            foreach (var (text, color) in syntax.Tokenize(lines[i]))
                AppendSpan(fs, text, ColorForToken(color, isDark));
            if (i < lines.Length - 1)
                AppendSpan(fs, "\n", ColorForToken(0, isDark));
        }
    }

    /// <summary>追加 Span：与上一个同色且无样式的 Span 合并文本，减少 Span 总数（渲染性能）。</summary>
    internal static void AppendSpan(FormattedString fs, string text, Color color)
    {
        if (fs.Spans.Count > 0)
        {
            var last = fs.Spans[^1];
            if (last.TextColor == color && last.Text.Length < 4096
                && last.FontAttributes == FontAttributes.None
                && last.TextDecorations == TextDecorations.None)
            {
                last.Text += text;
                return;
            }
        }
        fs.Spans.Add(new Span { Text = text, TextColor = color });
    }

    /// <summary>Syntax token 色码 → MAUI Color（2=dim，其余走 ANSI 表；供代码高亮复用）。</summary>
    public static Color ColorForToken(int code, bool isDark)
    {
        var fallback = isDark ? DarkDefault : LightDefault;
        var dim = isDark ? DarkDim : LightDim;
        return ResolveFg(code, fallback, dim);
    }

    private static Color ResolveFg(int code, Color fallback, Color dim)
    {
        if (code == 2) return dim; // dim/faint
        return ResolveColor(code, fallback);
    }

    private static Color ResolveColor(int code, Color fallback)
    {
        if (code >= 0x1000000) // 真彩 RGB（AnsiTty.RgbCode = 0x1000000 | r<<16 | g<<8 | b）
            return Color.FromRgb((code >> 16) & 0xFF, (code >> 8) & 0xFF, code & 0xFF);
        if (code >= 30 && AnsiRgb.TryGetValue(code, out var hex))
            return Color.FromArgb(hex);
        return fallback; // 样式码 1-9 或未知 → 默认色
    }
}
