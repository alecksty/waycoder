using Microsoft.Maui.Controls;
using Microsoft.Maui.Graphics;
using WayCoder.UI.Shared;
using WayCoder.UI.Tui.Edit;
// 只取 AnsiTty，别 using 整个 Terminal 命名空间 —— 那里也有个 Color，会与 Maui.Graphics.Color 撞名
using AnsiTty = WayCoder.UI.Shared.Terminal.AnsiTty;

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

    /// <summary>命令行/代码块等宽字体（对齐、像终端）。</summary>
    internal const string MonoFont = "Courier New";

    /// <summary>把 «» 中间格式文本解析成 MAUI FormattedString（自适应深浅主题默认色）。
    /// 同时支持 ```lang 围栏代码块：块内用 Syntax 逐行 Tokenize 语法高亮。</summary>
    /// <param name="maxHighlightChars">
    /// 超过这个长度的**单个代码块**直接降级成一个纯文本 Span（不做语法高亮）。
    /// 默认 10 万字符（一次性的最终渲染用）；**流式渲染必须传小得多**（见
    /// <see cref="StreamingHighlightMaxChars"/>）—— 那里的代价是 O(长度) 且被
    /// 反复支付，见 ChatPage.ShouldRecomputeFormatted 的注释。
    /// </param>
    public static FormattedString Convert(string? markup, bool isDark, int maxHighlightChars = DefaultHighlightMaxChars)
    {
        var fs = new FormattedString();
        if (string.IsNullOrEmpty(markup)) return fs;
        RenderSegments(markup, fs, isDark, maxHighlightChars);
        return fs;
    }

    /// <summary>按行渲染：围栏块 / markdown 表格块走专门渲染，其余累积后走 ParseInline（保留跨行 «» 块）。</summary>
    /// <summary>一次性（最终）渲染的高亮长度上限。</summary>
    internal const int DefaultHighlightMaxChars = 100_000;

    /// <summary>
    /// **流式渲染**的高亮长度上限 —— 比一次性渲染低一个数量级。
    /// 理由：流式期间每 120ms~1.2s 就把整段重算一次，代价 O(长度)，
    /// 一个几万字符的代码块按语法高亮会造出**数千个 Span**，每次重建都要
    /// `SpannableString` + 整段重新排版 —— 单次耗时一旦超过节拍，UI 线程就永远追不上
    /// （占用率 100% ⇒ 界面完全无响应，用户实测「让 AI 写个超级玛丽，写着写着死机」）。
    /// 流式时高亮本来就看不清楚（内容还在长），降级成纯文本 = 1 个 Span，几乎免费；
    /// 本轮结束时 finally 会走一次默认上限的全量渲染，语法高亮照样有。
    /// </summary>
    internal const int StreamingHighlightMaxChars = 8_000;

    private static void RenderSegments(string markup, FormattedString fs, bool isDark,
        int maxHighlightChars = DefaultHighlightMaxChars)
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

            // 围栏代码块 ```lang（判据与 TUI 共用 CodeFence，含反引号写少了的容错）
            if (CodeFence.TryOpen(line, out var lang, out var ticks))
            {
                FlushInline();
                var code = new System.Text.StringBuilder();
                i++;
                while (i < lines.Length && !CodeFence.IsClose(lines[i], ticks))
                {
                    code.AppendLine(lines[i]);
                    i++;
                }
                i++; // 跳过闭合围栏（可能越界=未闭合）
                var syntax = lang.Length > 0 ? Syntax.ByLanguage(lang) : Syntax.Detect(code.ToString()) ?? Syntax.ByLanguage("");
                if (syntax.Name != "纯文本")
                    RenderCode(code.ToString().TrimEnd('\n'), syntax, fs, isDark, maxHighlightChars);
                else
                    RenderInline("```" + lang + "\n" + code + "```", fs, isDark);
                continue;
            }

            // markdown 表格块：当前行以 | 开头，且下一行是分隔线（|---|---|）
            if (line.TrimStart().StartsWith('|') && MarkdownTable.IsSeparator(lines, i + 1))
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

    /// <summary>markdown 表格 → 等宽对齐文本（列宽补齐 + Courier New 等宽 + 表头加粗）。</summary>
    private static void RenderTable(List<string> rawLines, FormattedString fs, bool isDark)
    {
        var rows = rawLines.Select(MarkdownTable.SplitRow).ToList();
        if (rows.Count < 2) { foreach (var l in rawLines) RenderInline(l + "\n", fs, isDark); return; }

        var cols = rows.Max(r => r.Length);
        var widths = new int[cols];
        for (int c = 0; c < cols; c++)
            widths[c] = rows.Select(r => c < r.Length ? r[c].Length : 0).Max();

        for (int r = 0; r < rows.Count; r++)
        {
            if (MarkdownTable.IsSeparatorRow(rows[r])) continue; // 跳过 |---|---| 分隔行

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

            // 粗体位（AnsiTty.BoldFlag）：«bold»«orange» 这类嵌套解析后是 `色值 | BoldFlag`
            if (color == 1 || (color & WayCoder.UI.Shared.Terminal.AnsiTty.BoldFlag) != 0)
                span.FontAttributes = FontAttributes.Bold;
            switch (color & ~WayCoder.UI.Shared.Terminal.AnsiTty.BoldFlag)
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
    private static void RenderCode(string code, Syntax syntax, FormattedString fs, bool isDark,
        int maxHighlightChars = DefaultHighlightMaxChars)
        => AppendCodeLines(fs, code, syntax, isDark, maxHighlightChars: maxHighlightChars);

    /// <summary>
    /// 代码块逐行 Tokenize 上色 —— **唯一实现**（三处调用：本类的 RenderCode、
    /// <see cref="ToolOutputFormatter"/>、<see cref="MarkdownPreview"/>）。
    /// 相邻同色 token 合并成单个 Span，避免大代码块拆出上万 Span 导致移动端渲染卡死（ANR）；
    /// 超大代码块直接降级纯文本，防主线程长时间分词。
    /// </summary>
    /// <param name="monoFont">是否用等宽字体（命令行/代码块对齐）。预览页不需要，传 false。</param>
    internal static void AppendCodeLines(FormattedString fs, string code, Syntax syntax, bool isDark,
        bool monoFont = true, int maxHighlightChars = DefaultHighlightMaxChars)
    {
        if (code.Length > maxHighlightChars)
        {
            fs.Spans.Add(new Span { Text = code });
            return;
        }

        var font = monoFont ? MonoFont : null;
        var lines = code.Replace("\r\n", "\n").Split('\n');
        for (int i = 0; i < lines.Length; i++)
        {
            foreach (var (text, color) in syntax.Tokenize(lines[i]))
                AppendSpan(fs, text, ColorForToken(color, isDark), font);
            if (i < lines.Length - 1)
                AppendSpan(fs, "\n", ColorForToken(0, isDark), font);
        }
    }

    /// <summary>追加 Span：与上一个同色且无样式的 Span 合并文本，减少 Span 总数（渲染性能）。
    /// 可选等宽字体（命令行/代码块对齐用）。</summary>
    internal static void AppendSpan(FormattedString fs, string text, Color color, string? fontFamily = null)
    {
        if (fs.Spans.Count > 0)
        {
            var last = fs.Spans[^1];
            if (last.TextColor == color && last.Text.Length < 4096
                && last.FontAttributes == FontAttributes.None
                && last.TextDecorations == TextDecorations.None
                && string.Equals(last.FontFamily, fontFamily, StringComparison.OrdinalIgnoreCase))
            {
                last.Text += text;
                return;
            }
        }
        fs.Spans.Add(new Span { Text = text, TextColor = color, FontFamily = fontFamily });
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
        // 先剥粗体位再解析：那个位是 MarkdownParser 用来把「粗体 + 颜色」塞进同一个 int 的，
        // 不剥的话 `≥0x1000000` 的真彩分支会把它当成一个巨大的 RGB 值解出乱色。
        code &= ~WayCoder.UI.Shared.Terminal.AnsiTty.BoldFlag;
        return ResolveColor(code, fallback);
    }

    private static Color ResolveColor(int code, Color fallback)
    {
        if (code >= 0x1000000) // 真彩 RGB（AnsiTty.RgbCode = 0x1000000 | r<<16 | g<<8 | b）
            return Color.FromRgb((code >> 16) & 0xFF, (code >> 8) & 0xFF, code & 0xFF);
        if (AnsiRgb.TryGetValue(code, out var hex)) // 16 色走显式表（Windows Terminal 配色）
            return Color.FromArgb(hex);
        if (code is >= 16 and <= 255) // 256 色盘：走 xterm 算法，别逐色补表（补必漏）
            return FromXterm256(code);
        return fallback; // 样式码 1-9 或未知 → 默认色
    }

    /// <summary>
    /// xterm 256 色盘 → RGB：16-231 是 6×6×6 RGB 立方（每级 0/95/135/175/215/255），
    /// 232-255 是 24 级灰阶（8 起、步长 10）。
    ///
    /// 为什么要算法而不是继续往 <see cref="AnsiRgb"/> 里补条目：主工程的语法高亮用 256 色
    /// （见 <c>Syntax.Keyword</c> 等），逐色补表意味着「主工程每换一次配色，移动端就整片失色」——
    /// 补漏之前 256 色一律落进 fallback（默认色），代码高亮全灰。
    /// </summary>
    internal static Color FromXterm256(int code)
    {
        // 换算的唯一实现在主工程 AnsiTty（Web/TUI 侧也用它把 256 色塞进 «fg:#rrggbb» 标记）
        var (r, g, b) = AnsiTty.Xterm256ToRgb(code);
        return Color.FromRgb(r, g, b);
    }
}
