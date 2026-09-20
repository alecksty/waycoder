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

    /// <summary>
    /// 按**块**渲染：交给共享 <see cref="MarkdownParser"/> 出 AST，再逐块渲染。
    ///
    /// ⚠ 此前这里是**按行扫描**、只认围栏与表格，其余整段并进一个行内段
    /// ⇒ 标题 / 列表 / 引用 / 分割线 / 任务项**全部字面显示**（手机上是一串 `- `、
    /// 终端上却是真列表 —— 同一段 md 两端不一样，这正是「四套实现」那个结构债的现场）。
    /// 共享 AST 已被 WayCoder / Gui / Maui 三端一起编译，接上它就等于三端同源。
    /// </summary>
    private static void RenderSegments(string markup, FormattedString fs, bool isDark,
        int maxHighlightChars = DefaultHighlightMaxChars)
    {
        List<MdNode> nodes;
        try { nodes = MarkdownParser.Parse(markup); }
        catch { RenderInline(markup, fs, isDark); return; }   // 渲染层崩掉比少一行格式更糟

        RenderBlocks(nodes, fs, isDark, maxHighlightChars, depth: 0);
    }

    /// <summary>
    /// 依次渲染一串块：**只在块之间插换行，末尾不插**。
    ///
    /// ⚠ 「末尾不插」是**硬要求**，不是风格问题：本方法也被当**行内渲染器**用 ——
    /// `MarkdownPreview.AppendPlain` → `Convert` 渲染**表格单元格**。末尾多一个 `\n`
    /// 会让每个格子多出整整一行，症状是「表格行变高、文字顶到上面、像多了个空行」。
    /// （原始实现在这里就是「只在行间插」，重写时漏掉这条，真机上当场现形。）
    /// </summary>
    private static void RenderBlocks(List<MdNode> nodes, FormattedString fs, bool isDark,
        int maxHighlightChars, int depth)
    {
        for (int n = 0; n < nodes.Count; n++)
        {
            RenderBlock(nodes[n], fs, isDark, maxHighlightChars, depth);
            if (n < nodes.Count - 1) fs.Spans.Add(new Span { Text = "\n" });
        }
    }

    /// <summary>渲染一个块 —— **不自带尾换行**（块间换行由 <see cref="RenderBlocks"/> 负责）。</summary>
    private static void RenderBlock(MdNode node, FormattedString fs, bool isDark,
        int maxHighlightChars, int depth)
    {
        switch (node)
        {
            case MdHeading h:
                // 图形界面按级别放大字号 + 加粗（TUI 那边只能用颜色，能力所限）
                fs.Spans.Add(new Span
                {
                    Text = h.Text,
                    FontAttributes = FontAttributes.Bold,
                    TextColor = ColorForToken(0, isDark),
                    FontSize = h.Level switch { 1 => 22, 2 => 19, 3 => 17, 4 => 16, _ => 15 },
                });
                break;

            case MdParagraph p:
                RenderInline(p.Text, fs, isDark);
                break;

            case MdCodeBlock c:
            {
                var syntax = c.Language.Length > 0
                    ? Syntax.ByLanguage(c.Language)
                    : Syntax.Detect(c.Code) ?? Syntax.ByLanguage("");
                // ⚠ 不再有「纯文本就退化成连着反引号一起显示」那条分支 —— 认不出语言也是代码块，
                //   只是不高亮（此前 ```text 整块字面输出，手机上能看到 ```text 这几个字）
                RenderCode(c.Code, syntax, fs, isDark, maxHighlightChars);
                break;
            }

            case MdListItem li:
            {
                for (int k = 0; k < li.Level; k++) fs.Spans.Add(new Span { Text = "  " });
                var marker = li.Checked is bool ck ? (ck ? "☑ " : "☐ ")
                    : li.Ordered ? $"{li.OrderNum}. " : "• ";
                fs.Spans.Add(new Span
                {
                    Text = marker,
                    TextColor = ColorForToken(li.Checked is true ? 32 : 0, isDark),
                });
                RenderInline(li.Text, fs, isDark);
                break;
            }

            case MdBlockQuote q:
            {
                // 引用是**容器块**：内部块先渲染到临时串，再给每一行统一加 `▎ ` 前缀。
                // 因为内部末尾不带换行，替换后不会留下悬空的竖条（早先那个 TrimTrailingBar 补丁已不需要）。
                var inner = new FormattedString();
                RenderBlocks(q.Blocks, inner, isDark, maxHighlightChars, depth + 1);
                if (inner.Spans.Count == 0) RenderInline(q.Text, inner, isDark);

                fs.Spans.Add(new Span { Text = "▎ ", TextColor = ColorForToken(2, isDark) });
                foreach (var s in inner.Spans)
                {
                    s.Text = s.Text.Replace("\n", "\n▎ ");   // 每行都补前缀
                    fs.Spans.Add(s);
                }
                break;
            }

            case MdRule:
                fs.Spans.Add(new Span
                {
                    Text = new string('─', 24),
                    TextColor = ColorForToken(2, isDark),
                });
                break;

            case MdMarkup m:
                RenderInline(m.Text, fs, isDark, baseColor: m.Style);
                break;

            case MdTable tbl:
                RenderTable(tbl, fs, isDark);
                break;
        }
    }

    /// <summary>
    /// Markdown 表格 → 等宽对齐文本（列宽补齐 + Courier New + 表头加粗）。
    /// 现在直接吃 <see cref="MdTable"/>：`**粗**` / `` `code` `` 这类**单元格内行内格式生效**，
    /// `:--` / `:-:` / `--:` **对齐标记也生效**（此前只有 Web 端认对齐）。
    /// </summary>
    private static void RenderTable(MdTable tbl, FormattedString fs, bool isDark)
    {
        var rows = new List<List<string>> { tbl.Headers };
        rows.AddRange(tbl.Rows);

        var cols = rows.Max(r => r.Count);
        if (cols == 0) return;

        // 列宽按**去掉标记后的可见文本**量 —— 用原始串会把 `**` 也算进宽度，列宽虚胖
        var widths = new int[cols];
        for (int c = 0; c < cols; c++)
            foreach (var r in rows)
                if (c < r.Count) widths[c] = Math.Max(widths[c], PlainWidth(r[c]));

        for (int r = 0; r < rows.Count; r++)
        {
            var row = rows[r];
            var border = ColorForToken(2, isDark);
            fs.Spans.Add(new Span { Text = "| ", FontFamily = MonoFont, TextColor = border });

            for (int c = 0; c < cols; c++)
            {
                var cell = c < row.Count ? row[c] : "";
                var pad = Math.Max(0, widths[c] - PlainWidth(cell));
                var align = c < tbl.Alignments.Count ? tbl.Alignments[c] : 0;
                var left = align == 3 ? pad : align == 2 ? pad / 2 : 0;   // 3=右对齐 2=居中
                if (left > 0) fs.Spans.Add(new Span { Text = new string(' ', left), FontFamily = MonoFont });

                var before = fs.Spans.Count;
                RenderInline(cell, fs, isDark);
                for (int k = before; k < fs.Spans.Count; k++)
                {
                    fs.Spans[k].FontFamily = MonoFont;
                    if (r == 0) fs.Spans[k].FontAttributes = FontAttributes.Bold;   // 表头加粗
                }

                var right = pad - left;
                if (right > 0) fs.Spans.Add(new Span { Text = new string(' ', right), FontFamily = MonoFont });
                fs.Spans.Add(new Span { Text = " | ", FontFamily = MonoFont, TextColor = border });
            }
            // 行间换行；**末行不补**（与 RenderBlocks 同一条规矩：渲染单元末尾不留换行）
            if (r < rows.Count - 1) fs.Spans.Add(new Span { Text = "\n" });
        }
    }

    /// <summary>单元格可见宽度 = 行内解析后各段文本长度之和（`**`/`` ` `` 这类标记不计入）。</summary>
    private static int PlainWidth(string cell)
    {
        int w = 0;
        foreach (var (text, _, _) in MarkdownParser.ParseInline(cell)) w += text.Length;
        return w;
    }

    /// <summary>单段 ParseInline 渲染（非代码块段）。</summary>
    /// <param name="baseColor">
    /// 基础**色码**（不是 Color）—— 0 表示用主题默认前景。
    /// 块级 `«dim»…«/»` 推理内容走这个：那一整块被外面的标记定性，块内的行内标记再在其上叠加。
    /// </param>
    private static void RenderInline(string segment, FormattedString fs, bool isDark, int baseColor = 0)
    {
        var defaultColor = isDark ? DarkDefault : LightDefault;
        var dimColor = isDark ? DarkDim : LightDim;

        foreach (var (text, color, bg) in MarkdownParser.ParseInline(segment, baseColor))
        {
            var span = new Span { Text = text, TextColor = ResolveFg(color, defaultColor, dimColor, isDark) };

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
        return ResolveFg(code, fallback, dim, isDark);
    }

    /// <summary>
    /// 前景色解析。**背景色不要走这里**（见 <see cref="ResolveColor"/> 的另一处调用）——
    /// 下面那个「浅色主题压暗」的调整只对文字成立，套到背景上会变成反的。
    /// </summary>
    private static Color ResolveFg(int code, Color fallback, Color dim, bool isDark)
    {
        if (code == 2) return dim; // dim/faint
        // 先剥粗体位再解析：那个位是 MarkdownParser 用来把「粗体 + 颜色」塞进同一个 int 的，
        // 不剥的话 `≥0x1000000` 的真彩分支会把它当成一个巨大的 RGB 值解出乱色。
        code &= ~WayCoder.UI.Shared.Terminal.AnsiTty.BoldFlag;
        var c = ResolveColor(code, fallback);

        // 浅色主题下，把**照深色底挑的**语法色翻过来。只挑 256 色盘里不在 AnsiRgb 表内的那几个
        // （= `Syntax` 那套语法配色）：16 色表是终端标准色、diff/输出在用，真彩是调用方显式指定的，
        // 两者都不该被我们代改。
        if (!isDark && code is >= 16 and <= 255 && !AnsiRgb.ContainsKey(code))
            c = ForLightBackground(c);
        return c;
    }

    /// <summary>
    /// 为**浅色底**翻新一个为深色底挑的颜色。
    ///
    /// 起因（用户实测）：白天主题下代码里的**标识符几乎看不见**，而旁边的行号正常。
    /// 标识符用的是 <c>Syntax.Identifier = 253</c>（xterm 亮灰 <c>#dadada</c>）——
    /// 那套配色整体照深色底挑（One Dark 系），放到白底上普遍偏淡，标识符是最极端的一个
    /// （行号走的是另一套 <c>GutterFg</c>，所以不受影响）。
    ///
    /// **第一版做错了**：按 RGB 等比缩放。等比会连**色差的绝对值**一起缩小 ⇒ 颜色发灰，
    /// 用户的原话是「所有的颜色都变淡了」。正解是在 **HSL 里只动 L、原样保留色相与饱和度** ——
    /// 颜色还是那个颜色，只是变深。
    ///
    /// 两步：① 灰调（注释 / 标识符 / 括号，饱和度≈0）没有色相可保留，直接把亮度翻过来；
    /// ② 彩色先按 <c>1 − L</c> 翻（保住调色板内部的明暗层次），再按**感知亮度**兜一道底 ——
    /// HSL 的 L 不是感知亮度，青/绿在同样的 L 下亮得多，不兜底的话青色在白底上依旧偏淡。
    /// </summary>
    private static Color ForLightBackground(Color c)
    {
        float max = MathF.Max(c.Red, MathF.Max(c.Green, c.Blue));
        float min = MathF.Min(c.Red, MathF.Min(c.Green, c.Blue));
        float l = (max + min) / 2f;

        if (max - min < 0.02f)                       // 灰调：只有明暗，没有色相
        {
            float gray = l > 0.5f ? 1f - l : l;
            return new Color(gray, gray, gray, 1f);
        }

        float d = max - min;
        float s = l > 0.5f ? d / (2f - max - min) : d / (max + min);
        float hue = max == c.Red
            ? (c.Green - c.Blue) / d + (c.Green < c.Blue ? 6f : 0f)
            : max == c.Green
                ? (c.Blue - c.Red) / d + 2f
                : (c.Red - c.Green) / d + 4f;
        hue /= 6f;

        float target = Math.Clamp(l > 0.5f ? 1f - l : l, 0.30f, 0.46f);
        var result = FromHsl(hue, s, target);
        if (RelativeLuminance(result) <= LightMaxLuminance) return result;

        // 感知亮度兜底：二分降 L —— 色相与饱和度一个字都不动。
        float lo = 0f, hi = target;
        for (int i = 0; i < 20; i++)
        {
            float mid = (lo + hi) / 2f;
            if (RelativeLuminance(FromHsl(hue, s, mid)) > LightMaxLuminance) hi = mid;
            else lo = mid;
        }
        return FromHsl(hue, s, lo);
    }

    /// <summary>
    /// 白底上对比度 ≥ 4.5 所对应的亮度上限：<c>(1.05 / 4.5) − 0.05</c>。
    /// 实测这套配色改完之后每个色号都在 4.61 以上。
    /// </summary>
    private const double LightMaxLuminance = 0.18;

    /// <summary>
    /// WCAG 相对亮度（sRGB 线性化后加权）。**不要拿 HSL 的 L 当它用** ——
    /// 同一个 L 下青/绿比紫/红亮得多，那正是上面要兜底的原因。
    /// </summary>
    private static double RelativeLuminance(Color c)
        => 0.2126 * ToLinear(c.Red) + 0.7152 * ToLinear(c.Green) + 0.0722 * ToLinear(c.Blue);

    private static double ToLinear(float v)
        => v <= 0.04045f ? v / 12.92 : Math.Pow((v + 0.055) / 1.055, 2.4);

    /// <summary>HSL → RGB（h/s/l 都是 0..1）。</summary>
    private static Color FromHsl(float h, float s, float l)
    {
        float c = (1f - MathF.Abs(2f * l - 1f)) * s;
        float x = c * (1f - MathF.Abs(h * 6f % 2f - 1f));
        float m = l - c / 2f;
        var (r, g, b) = ((int)(h * 6f)) switch
        {
            0 => (c, x, 0f),
            1 => (x, c, 0f),
            2 => (0f, c, x),
            3 => (0f, x, c),
            4 => (x, 0f, c),
            _ => (c, 0f, x),
        };
        return new Color(r + m, g + m, b + m, 1f);
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
