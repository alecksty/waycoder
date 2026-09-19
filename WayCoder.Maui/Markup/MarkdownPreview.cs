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
            stack.Add(new Label
            {
                FormattedText = MarkupToFormattedString.Convert(para.ToString(), isDark),
                FontSize = 15,
                LineHeight = 1.35,
                // 兜底色：绝大多数 span 在 Convert 里已经带上自己的颜色，这里只管没带色的那些
                TextColor = Ink(isDark, 226, 226, 228, 32, 35, 40),
            });
        }

        return stack;
    }

    private static bool IsListItem(string line, out char marker)
    {
        var t = line.TrimStart();
        marker = '\0';
        if (t.Length >= 2 && (t[0] == '-' || t[0] == '*') && t[1] == ' ') { marker = t[0]; return true; }
        if (t.Length >= 3 && char.IsDigit(t[0]) && (t[1] == '.' || t[1] == ')') && t[2] == ' ') { marker = '1'; return true; }
        return false;
    }

    private static View RenderTable(List<string[]> rows, bool isDark)
    {
        if (rows.Count == 0) return new VerticalStackLayout();

        var cols = rows.Max(r => r.Length);
        var grid = new Grid { ColumnSpacing = 10, RowSpacing = 2, Margin = new Thickness(0, 2) };
        for (int c = 0; c < cols; c++)
            grid.ColumnDefinitions.Add(new ColumnDefinition(GridLength.Auto));

        var header = rows[0];
        for (int c = 0; c < cols; c++)
            grid.Add(new Label
            {
                Text = c < header.Length ? header[c] : "",
                FontAttributes = FontAttributes.Bold,
                FontSize = 13,
                TextColor = Ink(isDark, 232, 232, 234, 18, 20, 24),
            }, c, 0);
        // 表头分隔线
        grid.RowDefinitions.Add(new RowDefinition(GridLength.Auto));
        grid.RowDefinitions.Add(new RowDefinition(GridLength.Auto));
        grid.Add(new BoxView
        {
            HeightRequest = 1,
            Color = Ink(isDark, 96, 96, 102, 196, 200, 206),
        }, 0, 1);
        Grid.SetColumnSpan((View)grid.Children[^1], cols);

        // 数据行（跳过表头与分隔线）
        for (int r = 2; r < rows.Count; r++)
        {
            grid.RowDefinitions.Add(new RowDefinition(GridLength.Auto));
            var cells = rows[r];
            for (int c = 0; c < cols; c++)
                grid.Add(new Label
                {
                    Text = c < cells.Length ? cells[c] : "",
                    FontSize = 13,
                    LineHeight = 1.3,
                    TextColor = Ink(isDark, 204, 204, 208, 52, 56, 62),
                }, c, r);
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

    private static View RenderList(List<string> items, bool isDark)
    {
        var stack = new VerticalStackLayout { Spacing = 2 };
        foreach (var item in items)
            stack.Add(new Label
            {
                FormattedText = MarkupToFormattedString.Convert($"• {item}", isDark),
                FontSize = 14,
                LineHeight = 1.3,
            });
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
