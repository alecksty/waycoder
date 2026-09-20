using System.Text;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Documents;
using Avalonia.Layout;
using Avalonia.Media;
using WayCoder.UI.Shared;   // MarkdownParser / MdNode …（块级结构走共享 AST，别再本地扫行）

namespace WayCoder.UI.Gui;

/// <summary>
/// Markdown 块级渲染：把 LLM 输出的 Markdown + «» 标记构建为一组 Control。
/// 支持段落/标题/引用/列表/分隔线/代码块（语法高亮）/表格 —— 消息气泡内部多 block 布局。
///
/// ⚠ **块级结构一律由共享 <see cref="MarkdownParser"/> 决定**（与 TUI / MAUI 同源）。
/// 此前这里是**第三套独立的行扫描器**，四个缺陷全由它而来：
///   · 段落只断「空行 / ``` / 表格」⇒ 紧跟段落的 `# 标题` / `---` / `- 列表` **全被吸进同一段**
///   · 标题只认 1–3 级（`#### x` 落段落字面）
///   · 列表判定不 TrimStart ⇒ 嵌套列表当正文
///   · 引用必须写成 <c>"> "</c> 带空格，<c>&gt;&gt;</c> 整行漏判
/// </summary>
public static class MarkdownBlocks
{
    private static readonly FontFamily Mono = GuiFonts.Mono;

    /// <summary>从主题取色（深/浅随 RequestedThemeVariant 切换）。</summary>
    private static Color Text => GuiColors.TextColor;
    private static Color Dim => GuiColors.DimColor;
    private static Color Accent => GuiColors.AccentColor;

    /// <summary>把 markdown 构建为 block 控件列表（供气泡 Render 重建）。</summary>
    public static List<Control> Build(string markdown)
    {
        var result = new List<Control>();

        List<MdNode> nodes;
        try { nodes = MarkdownParser.Parse(markdown); }
        catch { nodes = [new MdParagraph { Text = markdown }]; }   // 渲染层崩掉比少一行格式更糟

        foreach (var node in nodes)
            result.Add(BuildBlock(node, depth: 0));

        return result;
    }

    /// <summary>一个 AST 块 → 一个 Control（结构由共享解析器定，这里只管「画成什么样」）。</summary>
    private static Control BuildBlock(MdNode node, int depth) => node switch
    {
        MdHeading h => Heading(h),
        MdParagraph p => Paragraph(p.Text, depth),
        MdCodeBlock c => CodeBlock(c.Code, c.Language),
        MdListItem li => ListItem(li, depth),
        MdBlockQuote q => Quote(q, depth),
        MdRule => Rule(),
        MdTable t => Table(t),
        MdMarkup m => Paragraph(m.Text, depth),   // 块级 «dim»…«/» 推理内容
        _ => new TextBlock(),
    };

    /// <summary>标题：1–6 级（此前只到 3 级），按级别给字号。</summary>
    private static Control Heading(MdHeading h)
    {
        var tb = new SelectableTextBlock
        {
            FontWeight = FontWeight.Bold,
            FontSize = h.Level switch { 1 => 19, 2 => 17, 3 => 15, 4 => 14, _ => 13.5 },
            TextWrapping = TextWrapping.Wrap,
            Margin = new Thickness(0, h.Level <= 2 ? 8 : 5, 0, 3),
        };
        // ⚠ 标题文本**要走行内解析**（与 TUI / MAUI 同源）—— 直接 `Run(h.Text)` 会把
        //   `### \`ui_circle(...)\`` 这类标题里的反引号**字面显示**出来。
        AddInlines(tb, MarkdownInlines.RenderInline(h.Text, Text, Dim));
        return tb;
    }

    private static Control Rule() => new Border
    {
        Height = 1,
        Background = new SolidColorBrush(GuiColors.BorderColor),
        Margin = new Thickness(0, 6),
    };

    /// <summary>列表项：标记（• / 序号. / ☑☐）+ 正文，缩进按 AST 的 Level（嵌套不再被拍平）。</summary>
    private static Control ListItem(MdListItem li, int depth)
    {
        var marker = li.Checked is bool ck ? (ck ? "☑" : "☐") : li.Ordered ? $"{li.OrderNum}." : "•";
        var tb = new SelectableTextBlock
        {
            TextWrapping = TextWrapping.Wrap,
            FontSize = 13,
            LineHeight = 20,
            Margin = new Thickness((depth + li.Level) * 14, 1, 0, 1),
        };
        tb.Inlines!.Add(new Run(marker + " ") { Foreground = new SolidColorBrush(Dim) });
        AddInlines(tb, MarkdownInlines.RenderInline(li.Text, Text, Dim));
        return tb;
    }

    /// <summary>段落：整段一个 SelectableTextBlock，按行补 LineBreak（保留原换行）。</summary>
    private static Control Paragraph(string text, int depth)
    {
        var tb = new SelectableTextBlock
        {
            TextWrapping = TextWrapping.Wrap,
            FontSize = 13,
            LineHeight = 20,
            Margin = new Thickness(depth * 14, 2, 0, 2),
        };
        var lines = text.Replace("\r\n", "\n").Split('\n');
        for (int k = 0; k < lines.Length; k++)
        {
            if (k > 0) tb.Inlines!.Add(new LineBreak());
            AddInlines(tb, MarkdownInlines.RenderInline(lines[k], Text, Dim));
        }
        return tb;
    }

    /// <summary>引用块：左侧竖条 + 内部块（**容器块**，内部递归走同一套 BuildBlock）。</summary>
    private static Control Quote(MdBlockQuote q, int depth)
    {
        var inner = new StackPanel { Spacing = 0 };
        foreach (var child in q.Blocks)
            inner.Children.Add(BuildBlock(child, depth + 1));
        if (inner.Children.Count == 0) inner.Children.Add(Paragraph(q.Text, depth + 1));

        var grid = new Grid
        {
            ColumnDefinitions =
            {
                new ColumnDefinition { Width = new GridLength(3) },
                new ColumnDefinition { Width = GridLength.Star },
            },
            Margin = new Thickness(depth * 14, 3, 0, 3),
        };
        var bar = new Border
        {
            Background = new SolidColorBrush(Accent),
            Opacity = 0.55,
        };
        Grid.SetColumn(bar, 0);
        Grid.SetColumn(inner, 1);
        inner.Margin = new Thickness(10, 0, 0, 0);
        grid.Children.Add(bar);
        grid.Children.Add(inner);
        return grid;
    }

    /// <summary>代码块：深色底 + 等宽字体 + 语法高亮。</summary>
    private static Control CodeBlock(string code, string lang)
    {
        var text = code.TrimEnd('\n');
        var tb = new SelectableTextBlock
        {
            FontFamily = Mono,
            FontSize = 12,
            TextWrapping = TextWrapping.Wrap,
            TextAlignment = TextAlignment.Left,
        };
        var inlines = SimpleHighlight.Highlight(text);
        foreach (var inl in inlines) tb.Inlines!.Add(inl);

        var border = new Border
        {
            Child = tb,
            Background = new SolidColorBrush(GuiColors.CodeBlockBgColor),
            CornerRadius = new CornerRadius(10),
            Padding = new Thickness(12, 8),
            Margin = new Thickness(0, 4, 0, 4),
            ClipToBounds = true, // 超长无空格 token（URL/长字符串）不溢出气泡边界
        };
        if (!string.IsNullOrEmpty(lang))
        {
            var head = new TextBlock
            {
                Text = lang,
                FontSize = 10.5,
                FontFamily = Mono,
                Foreground = new SolidColorBrush(Dim),
                Margin = new Thickness(0, 0, 0, 4),
            };
            var stack = new StackPanel();
            stack.Children.Add(head);
            stack.Children.Add(border);
            return stack;
        }
        return border;
    }

    /// <summary>
    /// 表格：直接吃 <see cref="MdTable"/>（表头加粗 + 边框）。
    /// 单元格走**行内渲染**（`**粗**` / `` `code` `` 生效），并按对齐标记设置对齐 ——
    /// 此前单元格是纯文本、对齐标记被忽略、`\|` 转义也会被拆错列（Split('|') 不看转义）。
    /// </summary>
    private static Control Table(MdTable t)
    {
        var rows = new List<List<string>> { t.Headers };
        rows.AddRange(t.Rows);
        if (rows.Count == 0) return new TextBlock();

        int colCount = rows.Max(r => r.Count);
        if (colCount == 0) return new TextBlock();

        var grid = new Grid { Margin = new Thickness(0, 4, 0, 4) };
        // Star 均分列：长单元格在列内换行，不再按内容全宽撑破气泡/屏幕（Auto 列会逃逸气泡宽度约束）
        for (int c = 0; c < colCount; c++)
            grid.ColumnDefinitions.Add(new ColumnDefinition(GridLength.Star));

        for (int r = 0; r < rows.Count; r++)
        {
            grid.RowDefinitions.Add(new RowDefinition(GridLength.Auto));
            for (int c = 0; c < colCount; c++)
            {
                var cell = c < rows[r].Count ? rows[r][c] : "";
                var align = c < t.Alignments.Count ? t.Alignments[c] : 0;
                var tb = new SelectableTextBlock
                {
                    FontSize = 12.5,
                    TextWrapping = TextWrapping.Wrap,
                    FontWeight = r == 0 ? FontWeight.Bold : FontWeight.Normal,
                    TextAlignment = align switch
                    {
                        2 => TextAlignment.Center,
                        3 => TextAlignment.Right,
                        _ => TextAlignment.Left,
                    },
                    Margin = new Thickness(10, 4, 10, 4),
                };
                AddInlines(tb, MarkdownInlines.RenderInline(cell, Text, Dim));
                Grid.SetRow(tb, r);
                Grid.SetColumn(tb, c);
                grid.Children.Add(tb);
            }
        }

        return new Border
        {
            Child = grid,
            BorderBrush = new SolidColorBrush(GuiColors.BorderColor),
            BorderThickness = new Thickness(1),
            CornerRadius = new CornerRadius(8),
            Padding = new Thickness(2),
            Margin = new Thickness(0, 4, 0, 4),
            ClipToBounds = true,
        };
    }

    private static void AddInlines(SelectableTextBlock tb, List<Inline> inlines)
    {
        foreach (var inl in inlines) tb.Inlines!.Add(inl);
    }
}
