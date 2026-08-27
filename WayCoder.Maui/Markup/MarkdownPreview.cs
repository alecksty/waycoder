using Microsoft.Maui.Controls;
using Microsoft.Maui.Graphics;
using WayCoder.UI.Tui.Edit;

namespace WayCoder.Maui.Markup;

/// <summary>
/// Markdown → MAUI View 渲染器（编辑器「预览」模式用）：标题 / 表格 / 代码块 / 列表 / 段落 / 分割线。
/// 表格用 Grid 按列渲染（表头加粗、单元格对齐）；代码块复用 <see cref="Syntax"/> 逐行高亮；
/// 行内格式（粗体/斜体/行内代码/链接）走 <see cref="MarkupToFormattedString.Convert"/>。
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
            if (line.StartsWith('|') && IsTableSeparator(lines, i + 1))
            {
                var rows = new List<string[]>();
                while (i < lines.Length && lines[i].TrimStart().StartsWith('|'))
                {
                    rows.Add(SplitCells(lines[i]));
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
                    TextColor = TextColor(isDark, 224, 224, 224),
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
                    Color = TextColor(isDark, 120, 120, 120),
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
                TextColor = TextColor(isDark, 224, 224, 224),
            });
        }

        return stack;
    }

    private static bool IsTableSeparator(string[] lines, int idx)
    {
        if (idx >= lines.Length) return false;
        var s = lines[idx].Trim();
        if (!s.StartsWith('|')) return false;
        foreach (var c in s)
        {
            if (c is '|' or '-' or ':' or ' ' or '\t') continue;
            return false;
        }
        return s.Contains('-');
    }

    private static string[] SplitCells(string line)
    {
        var s = line.Trim();
        if (s.StartsWith('|')) s = s[1..];
        if (s.EndsWith('|')) s = s[..^1];
        return s.Split('|').Select(c => c.Trim()).ToArray();
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
                TextColor = TextColor(isDark, 230, 230, 230),
            }, c, 0);
        // 表头分隔线
        grid.RowDefinitions.Add(new RowDefinition(GridLength.Auto));
        grid.RowDefinitions.Add(new RowDefinition(GridLength.Auto));
        grid.Add(new BoxView
        {
            HeightRequest = 1,
            Color = TextColor(isDark, 100, 100, 100),
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
                    TextColor = TextColor(isDark, 200, 200, 200),
                }, c, r);
        }
        return grid;
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
        foreach (var raw in code.Replace("\r\n", "\n").Split('\n'))
        {
            foreach (var (text, color) in syntax.Tokenize(raw))
                MarkupToFormattedString.AppendSpan(fs, text, MarkupToFormattedString.ColorForToken(color, isDark));
            fs.Spans.Add(new Span { Text = "\n" });
        }
        if (fs.Spans.Count > 0 && fs.Spans[^1].Text == "\n") fs.Spans.RemoveAt(fs.Spans.Count - 1);

        return new Border
        {
            BackgroundColor = TextColor(isDark, 28, 28, 34),
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
        };
    }

    private static Color TextColor(bool isDark, byte r, byte g, byte b)
        => Color.FromRgb(r, g, b);
}
