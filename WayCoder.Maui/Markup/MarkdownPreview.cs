using Microsoft.Maui.Controls;
using Microsoft.Maui.Graphics;
using WayCoder.UI.Tui.Edit;

namespace WayCoder.Maui.Markup;

/// <summary>
/// Markdown → MAUI View 渲染器（编辑器「预览」模式、使用说明页用）：
/// 标题 / 表格 / 代码块 / 列表 / 段落 / 分割线。
/// 表格用 Grid 按列渲染（表头加粗、单元格对齐）；代码块复用 <see cref="Syntax"/> 逐行高亮；
/// 行内格式（粗体/斜体/行内代码/链接）走 <see cref="MarkupToFormattedString.Convert"/>。
///
/// ## 两套配色（白天 / 夜间）
///
/// 这里画的每一处颜色都要**跟着主题走**：原先正文色恒返回亮灰 `#E0E0E0`，
/// 白天主题下就是白底白字 —— 说明页整篇看不见（用户实测）。
/// 代码高亮那半边本来就分两套（<see cref="MarkupToFormattedString.ColorForToken"/> 里
/// 有 <c>ForLightBackground</c> 做浅底翻新），漏的是本文件自己画的
/// 标题 / 表格 / 分割线 / 代码块底色。
///
/// ⚠ **新增任何一处颜色都要给两天套**，别只写一个值 —— 单值在另一套主题下必然是错的，
/// 而且构建与自测都不会告诉你。
/// </summary>
public static class MarkdownPreview
{
    /// <summary>渲染整个 markdown 文本为一个可滚动的 VerticalStackLayout。</summary>
    public static View Render(string markdown, bool isDark)
    {
        var stack = new VerticalStackLayout { Spacing = 4, Padding = new Thickness(14, 10) };
        var lines = (markdown ?? "").Replace("\r\n", "\n").Split('\n');

        int i = 0;
        while (i < lines.Length)
        {
            var line = lines[i].TrimEnd();

            // 代码围栏 ```lang
            if (line.StartsWith("```"))
            {
                var lang = line[3..].Trim();
                var sb = new System.Text.StringBuilder();
                i++;
                while (i < lines.Length && !lines[i].TrimStart().StartsWith("```"))
                {
                    sb.AppendLine(lines[i]);
                    i++;
                }
                i++; // 跳过闭合 ```
                stack.Add(RenderCodeBlock(sb.ToString().TrimEnd('\n'), lang, isDark));
                continue;
            }

            // 表格：当前行以 | 开头，且下一行是分隔线（|---| 或 |-:|）
            if (line.StartsWith('|') && MarkdownTable.IsSeparator(lines, i + 1))
            {
                var rows = new List<string[]>();
                while (i < lines.Length && lines[i].TrimStart().StartsWith('|'))
                {
                    rows.Add(MarkdownTable.SplitRow(lines[i]));
                    i++;
                }
                stack.Add(RenderTable(rows, isDark));
                continue;
            }

            // 标题 #
            if (line.StartsWith('#'))
            {
                var level = line.TakeWhile(c => c == '#').Count();
                var text = line[level..].Trim();
                stack.Add(new Label
                {
                    Text = text,
                    FontSize = level <= 1 ? 20 : level == 2 ? 17 : 15,
                    FontAttributes = FontAttributes.Bold,
                    TextColor = Ink(isDark, 232, 232, 234, 22, 24, 28),
                });
                i++;
                continue;
            }

            // 分割线 --- / ***
            if (line is "---" or "***" or "___")
            {
                stack.Add(new BoxView
                {
                    HeightRequest = 1,
                    // 分割线是**装饰**不是内容，两套都得让它在自己的底上看得见即可，
                    // 不必追求高对比（画太重反而喧宾夺主）
                    Color = Ink(isDark, 110, 110, 116, 190, 194, 200),
                    Margin = new Thickness(0, 4),
                });
                i++;
                continue;
            }

            // 列表项 - / * / 1.
            if (IsListItem(line, out var marker))
            {
                var items = new List<string>();
                while (i < lines.Length && IsListItem(lines[i].TrimEnd(), out _))
                {
                    items.Add(lines[i].Trim().TrimStart('-', '*', ' ', '\t'));
                    i++;
                }
                stack.Add(RenderList(items, isDark));
                continue;
            }

            // 空行跳过
            if (string.IsNullOrWhiteSpace(line))
            {
                i++;
                continue;
            }

            // 段落：累积到下一个空行/特殊块
            var para = new System.Text.StringBuilder(line);
            i++;
            while (i < lines.Length && !string.IsNullOrWhiteSpace(lines[i])
                   && !lines[i].TrimStart().StartsWith('|')
                   && !lines[i].TrimStart().StartsWith("```")
                   && !lines[i].TrimStart().StartsWith('#'))
            {
                para.Append('\n').Append(lines[i]);
                i++;
            }
            stack.Add(BuildParagraph(para.ToString(), isDark));
        }

        return stack;
    }

    /// <summary>
    /// 段落 → Label：把 <c>[文字](目标)</c> 变成**可点的链接**，其余部分照旧走 markup 渲染。
    ///
    /// ## 为什么要有链接（而不只是好看）
    ///
    /// 说明是一棵树。原先层级完全写死在代码里（`HelpCatalog.Topic.Children`），
    /// **每加一层就要动一次 C#、还要动列表页**；而且"哪些节点是目录、哪些有正文"这个判断漏一处，
    /// 现象就是"点下去什么也不发生"（实测就坏过一次：点「22 种语言」去开一个从不存在的
    /// `help/vml/languages.md`）。改成**正文里写链接**之后，层级由内容决定 ——
    /// **多少级都行，加页面只写 markdown**。
    ///
    /// ## 目标怎么写
    ///
    /// · `[C 语言](help:vml/lang/c)` —— 跳到另一篇说明（也可以直接写裸 id：`(vml/lang/c)`）
    /// · `[官网](https://…)` —— 交给系统浏览器
    private static Label BuildParagraph(string text, bool isDark, double fontSize = 15,
        bool attachLinkTap = true)
    {
        var links = FindLinks(text);

        // **整块就是一个链接**（表格里的「语言」列、列表里的一行条目都是这种形态）：
        // 手势直接挂在 Label 上。这是**唯一实测能触发**的做法 ——
        // Span 级的 `GestureRecognizers` 在 Android 上点了没反应（链接画得对、就是点不动，
        // 长按也一样），排查成本远高于多写这几行。
        if (attachLinkTap && links.Count == 1 && links[0].Start == 0 && links[0].End == text.Length)
        {
            var only = new Label
            {
                Text = links[0].Label,
                FontSize = fontSize,
                LineHeight = 1.35,
                TextColor = Ink(isDark, 106, 168, 255, 0, 90, 200),
                TextDecorations = TextDecorations.Underline,
            };
            var t = new TapGestureRecognizer();
            t.Tapped += (_, _) => ActivateLink(links[0].Target);
            only.GestureRecognizers.Add(t);
            return only;
        }

        var fs = new FormattedString();
        var pos = 0;

        foreach (var (start, end, label, target) in links)
        {
            if (start > pos) AppendPlain(fs, text[pos..start], isDark);
            fs.Spans.Add(LinkSpan(label, target, isDark, attachLinkTap));
            pos = end;
        }
        if (pos < text.Length) AppendPlain(fs, text[pos..], isDark);

        return new Label
        {
            FormattedText = fs,
            FontSize = fontSize,
            LineHeight = 1.35,
            // 兜底色：绝大多数 span 在 Convert 里已经带上自己的颜色，这里只管没带色的那些
            TextColor = Ink(isDark, 226, 226, 228, 32, 35, 40),
        };
    }

    /// <summary>把一段**不含链接**的文本按 markup 渲染后并进目标 FormattedString。</summary>
    private static void AppendPlain(FormattedString fs, string text, bool isDark)
    {
        if (text.Length == 0) return;
        foreach (var span in MarkupToFormattedString.Convert(text, isDark).Spans)
            fs.Spans.Add(span);
    }

    /// <param name="tappable">
    /// 是否**在这个 Span 上**装手势。
    ///
    /// ⚠ 整格可点的单元格必须传 `false`：Span 上的手势会挂上 `LinkMovementMethod`，
    /// 它**把触摸整个吃掉**（而它自己的 Span 手势在 Android 上又不触发）⇒
    /// 事件传不到外层那个真正管用的 `Border`，表现为"点了没反应"。
    /// 这一条是真机上试出来的：同样的格子，只把 Span 手势留着就点不动。
    /// </param>
    private static Span LinkSpan(string label, string target, bool isDark, bool tappable = true)
    {
        var span = new Span
        {
            Text = label,
            // 链接色：两天套（与 Ink 同一套规矩，别只写一个值）
            TextColor = Ink(isDark, 106, 168, 255, 0, 90, 200),
            TextDecorations = TextDecorations.Underline,
        };
        if (tappable)
        {
            var tap = new TapGestureRecognizer();
            tap.Tapped += (_, _) => ActivateLink(target);
            span.GestureRecognizers.Add(tap);
        }
        return span;
    }

    /// <summary>
    /// 点链接：`http(s)://` 交给系统；其余一律当**说明页 id**，在 App 内跳转。
    ///
    /// ⚠ `Shell.Current` 在页面已销毁时会是 null（异步回调晚到），别直接点下去。
    /// </summary>
    private static void ActivateLink(string target)
    {
        var t = target.Trim();
        if (t.StartsWith("help:", StringComparison.OrdinalIgnoreCase)) t = t[5..];

        if (t.StartsWith("http://", StringComparison.OrdinalIgnoreCase) ||
            t.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
        {
            try { _ = Launcher.Default.OpenAsync(t); }
            catch (Exception ex) { ErrorLog.Error("MarkdownPreview", $"打开链接失败: {t}", ex); }
            return;
        }

        // 包内说明：走与列表页同一条路由（`helptopic?id=…`），
        // 这样"从列表点进去"与"从链接点进去"落在同一个页面上。
        try
        {
            if (Shell.Current is { } shell)
                _ = shell.GoToAsync($"helptopic?id={Uri.EscapeDataString(t)}");
        }
        catch (Exception ex) { ErrorLog.Error("MarkdownPreview", $"跳转说明失败: {t}", ex); }
    }

    /// <summary>
    /// 找出 <c>[文字](目标)</c>。返回 `(起点, 终点, 文字, 目标)`，终点是**开区间**（紧跟 `)` 之后）。
    ///
    /// 只认"一行之内、目标里没有空白"的形态 —— 够用于说明文档，且不会把
    /// 正文里偶然出现的方括号圆括号吃进去（宁可少认，不可误吃）。
    /// </summary>
    private static List<(int Start, int End, string Label, string Target)> FindLinks(string text)
    {
        var list = new List<(int, int, string, string)>();
        for (var i = 0; i < text.Length; i++)
        {
            if (text[i] != '[') continue;
            var close = text.IndexOf(']', i + 1);
            if (close < 0 || close + 1 >= text.Length || text[close + 1] != '(') continue;
            var end = text.IndexOf(')', close + 2);
            if (end < 0) continue;

            var target = text[(close + 2)..end];
            if (target.Length == 0 || target.Any(char.IsWhiteSpace)) continue;

            list.Add((i, end + 1, text[(i + 1)..close], target));
            i = end;
        }
        return list;
    }

    private static bool IsListItem(string line, out char marker)
    {
        var t = line.TrimStart();
        marker = '\0';
        if (t.Length >= 2 && (t[0] == '-' || t[0] == '*') && t[1] == ' ') { marker = t[0]; return true; }
        if (t.Length >= 3 && char.IsDigit(t[0]) && (t[1] == '.' || t[1] == ')') && t[2] == ' ') { marker = '1'; return true; }
        return false;
    }

    /// <summary>
    /// 表格：**画出格线**，并且**整格可点**。
    ///
    /// ## 格线怎么画
    ///
    /// MAUI 的 `Border` 只能四边一起描边（没有单独画某一边的 API），
    /// 一格一个 Border 拼起来会在相邻处叠成 2px、外圈 1px 的"粗细不一"。
    /// 所以用**留缝**的办法：Grid 的底色 = 线色，`RowSpacing`/`ColumnSpacing` 各留 1px，
    /// 每格自己铺底色盖住中间 —— 露出来的就是均匀的 1px 格线，外圈再靠 `Padding = 1` 兜一圈。
    /// 格线宽度只由 spacing 决定，改一处即可，不会有"某条线偏粗"。
    ///
    /// ## 为什么整格可点
    ///
    /// 用户报「表格有点小，点击不方便」：原先只有链接那几个字有手势，
    /// 手指要精准落在文字上。现在**手势挂到整格**，格子又加了内边距 ——
    /// 一格就是一整块触摸区（约 40×40dp，达到可点尺寸的下限）。
    /// ⚠ 这时**不能再让 Label 自己也有手势**（一次点击会推两页），
    /// 所以纯链接的格子用 `attachLinkTap: false` 渲染。
    /// </summary>
    private static View RenderTable(List<string[]> rows, bool isDark)
    {
        if (rows.Count == 0) return new VerticalStackLayout();

        // ⚠ 把 markdown 的**分隔行**（`|---|---|`）丢掉 —— 它是排版记号，不是内容。
        // 原先是靠"数据行从下标 2 开始"绕开的；改成整表统一循环之后它就被当成数据渲染出来了
        // （屏幕上多一行 `--- | ---`）。判据写成"每格只由 `-`/`:`/空白组成"，比写死下标 1 稳。
        rows = rows.Where((_, idx) => idx != 1 || !IsSeparatorRow(rows[1])).ToList();

        var cols = rows.Max(r => r.Length);
        var lineColor = Ink(isDark, 62, 64, 72, 205, 209, 216);

        var grid = new Grid
        {
            // 格线 = 露出来的 Grid 底色（见方法注释）
            BackgroundColor = lineColor,
            RowSpacing = 1,
            ColumnSpacing = 1,
            Padding = new Thickness(1),   // 外圈那一圈线
            Margin = new Thickness(0, 4),
        };
        for (int c = 0; c < cols; c++)
            grid.ColumnDefinitions.Add(new ColumnDefinition(GridLength.Auto));

        for (int r = 0; r < rows.Count; r++)
        {
            grid.RowDefinitions.Add(new RowDefinition(GridLength.Auto));
            var cells = rows[r];
            for (int c = 0; c < cols; c++)
                grid.Add(BuildCell(c < cells.Length ? cells[c] : "", isDark, header: r == 0), c, r);
        }

        // 表格**允许横向滚动**。
        //
        // 代码块可以折行（折了还能读），表格不行 —— 折行会把列对错，那就不是表格了。
        // 而文档里的表格经常比手机屏宽（比方说「目标标准 | Lua 5.1 子集 (2006)」这种），
        // 外层只有竖向滚动的话右边几列就永远看不到。
        //
        // 列宽是 Auto ⇒ 横向 ScrollView 给的是无限宽约束，各列按内容取自然宽度，
        // 整张表比屏幕宽时就能拖动；比屏幕窄时宽度就是内容宽，不会撑满留白。
        return new ScrollView
        {
            Orientation = ScrollOrientation.Horizontal,
            HorizontalScrollBarVisibility = ScrollBarVisibility.Default,
            Content = grid,
        };
    }

    /// <summary>`|---|---|` 这种分隔行（每格只由 `-`/`:`/空白组成）。</summary>
    private static bool IsSeparatorRow(string[] cells)
        => cells.Length > 0 && cells.All(c => c.Length > 0 && c.All(ch => ch is '-' or ':' or ' '));

    /// <summary>一个单元格：底色 + 内边距（撑出可点的面积）；整格是一个链接时**整格可点**。</summary>
    private static View BuildCell(string text, bool isDark, bool header)
    {
        var links = FindLinks(text);
        var whole = links.Count == 1 && links[0].Start == 0 && links[0].End == text.Length;

        var cell = new Border
        {
            Padding = new Thickness(12, 12),     // 触摸面积主要就来自这里（约 44dp 高）
            StrokeThickness = 0,
            BackgroundColor = header
                ? Ink(isDark, 38, 40, 48, 238, 240, 244)
                : Ink(isDark, 22, 23, 28, 255, 255, 255),
            // 纯链接格：交给整格的手势（attachLinkTap: false 免得点一次推两页）
            Content = BuildParagraph(header ? $"«bold»{text}«/»" : text, isDark, 14,
                                     attachLinkTap: !whole),
        };

        if (whole)
        {
            var t = new TapGestureRecognizer();
            var target = links[0].Target;
            t.Tapped += (_, _) => ActivateLink(target);
            cell.GestureRecognizers.Add(t);
        }
        return cell;
    }

    private static View RenderList(List<string> items, bool isDark)
    {
        var stack = new VerticalStackLayout { Spacing = 2 };
        foreach (var item in items)
            stack.Add(BuildParagraph($"• {item}", isDark, 14));
        return stack;
    }

    private static View RenderCodeBlock(string code, string lang, bool isDark)
    {
        var syntax = lang.Length > 0 ? Syntax.ByLanguage(lang) : Syntax.Detect(code) ?? Syntax.ByLanguage("");
        var fs = new FormattedString();
        // 预览页不用等宽字体；共享实现天然不在末尾补换行（原写法是「每行都补、最后再删掉」）
        // 代码里的字色**本来就分两套**（AppendCodeLines → ColorForToken → ResolveFg 里
        // 有浅底翻新），所以这里只管底色。
        MarkupToFormattedString.AppendCodeLines(fs, code, syntax, isDark, monoFont: false);

        // 代码块**横向可滚**，与表格同一个道理：
        // 外层只有竖向滚动，代码行比屏幕宽时右边就被吃掉了 —— 而代码恰恰不该折行
        // （折了就毁掉缩进与对齐，读起来更糟）；横向滚动既保住原样又能看全。
        // 两种情况都覆盖：短行时 Label 的自然宽度就是内容宽，不会撑开留白。
        return new ScrollView
        {
            Orientation = ScrollOrientation.Horizontal,
            HorizontalScrollBarVisibility = ScrollBarVisibility.Default,
            Content = new Border
            {
                // 夜间：比页面底更深的"纸"，让代码块自己成块；白天：比页面底略灰，同理
                BackgroundColor = Ink(isDark, 24, 25, 31, 243, 244, 247),
                StrokeThickness = 0,
                Padding = new Thickness(10, 6),
                Margin = new Thickness(0, 2),
                StrokeShape = new Microsoft.Maui.Controls.Shapes.RoundRectangle { CornerRadius = 6 },
                Content = new Label
                {
                    FormattedText = fs,
                    FontFamily = "Courier New",
                    FontSize = 12,
                    LineHeight = 1.3,
                    LineBreakMode = LineBreakMode.NoWrap,
                },
            },
        };
    }

    /// <summary>
    /// 一处颜色给**两天套值**（夜间 / 白天），按当前主题取一个。
    ///
    /// 为什么不写成"给一个值、另一个自动反相"：反相只在灰色上碰巧成立，
    /// 一旦有人给这里塞个彩色就会得到谁也想不到的结果。显式两套，读代码的人一眼看得见。
    /// </summary>
    private static Color Ink(bool isDark,
        byte darkR, byte darkG, byte darkB,
        byte lightR, byte lightG, byte lightB)
        => isDark ? Color.FromRgb(darkR, darkG, darkB) : Color.FromRgb(lightR, lightG, lightB);
}
