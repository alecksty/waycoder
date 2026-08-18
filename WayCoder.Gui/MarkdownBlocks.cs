using System.Text;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Documents;
using Avalonia.Layout;
using Avalonia.Media;

namespace WayCoder.UI.Gui;

/// <summary>
/// Markdown 块级渲染：把 LLM 输出的 Markdown + «» 标记构建为一组 Control。
/// 支持段落/标题/引用/列表/分隔线/代码块（语法高亮）/表格 —— 消息气泡内部多 block 布局。
/// </summary>
public static class MarkdownBlocks
{
    private static readonly FontFamily Mono = new("Menlo,Consolas,monospace");

    /// <summary>从主题资源取色（TextBrush/DimTextBrush/AccentBrush），支持深浅主题切换；缺失回退固定色。</summary>
    private static Color ThemeColor(string key, string fallbackHex)
    {
        if (Application.Current?.Resources.TryGetResource(key, null, out var v) == true
            && v is SolidColorBrush sb)
            return sb.Color;
        return Color.Parse(fallbackHex);
    }
    private static Color Text => ThemeColor("TextBrush", "#e6e8ee");
    private static Color Dim => ThemeColor("DimTextBrush", "#8b93a7");
    private static Color Accent => ThemeColor("AccentBrush", "#4f8cff");

    /// <summary>把 markdown 构建为 block 控件列表（供气泡 Render 重建）。</summary>
    public static List<Control> Build(string markdown)
    {
        var result = new List<Control>();
        var lines = markdown.Replace("\r\n", "\n").Split('\n');
        int i = 0;

        while (i < lines.Length)
        {
            var line = lines[i].TrimEnd();

            // 代码围栏
            var trimmed = line.TrimStart();
            if (trimmed.StartsWith("```"))
            {
                var lang = trimmed.Length > 3 ? trimmed[3..].Trim() : "";
                var buf = new StringBuilder();
                i++;
                while (i < lines.Length && !lines[i].TrimStart().StartsWith("```"))
                {
                    buf.AppendLine(lines[i]);
                    i++;
                }
                i++; // 跳过结束围栏
                result.Add(CodeBlock(buf.ToString(), lang));
                continue;
            }

            // 表格：当前行及后续连续行都以 | 开头
            if (line.TrimStart().StartsWith('|') && i + 1 < lines.Length && lines[i + 1].TrimStart().StartsWith('|'))
            {
                var rows = new List<string>();
                while (i < lines.Length && lines[i].TrimStart().StartsWith('|'))
                {
                    rows.Add(lines[i]);
                    i++;
                }
                result.Add(Table(rows));
                continue;
            }

            // 标题
            if (line.StartsWith("### ") || line.StartsWith("## ") || line.StartsWith("# "))
            {
                int lvl = line.StartsWith("### ") ? 3 : line.StartsWith("## ") ? 2 : 1;
                var tb = new SelectableTextBlock
                {
                    FontWeight = FontWeight.Bold,
                    FontSize = lvl == 1 ? 17 : lvl == 2 ? 15 : 14,
                    TextWrapping = TextWrapping.Wrap,
                    Margin = new Thickness(0, 4, 0, 2),
                };
                tb.Inlines.Add(new Run(line[lvl..].Trim()) { Foreground = new SolidColorBrush(Text) });
                result.Add(tb);
                i++;
                continue;
            }

            // 分隔线
            if (line.Trim() is "---" or "***" or "___")
            {
                result.Add(new Border
                {
                    Height = 1,
                    Background = new SolidColorBrush(Color.Parse("#262b3a")),
                    Margin = new Thickness(0, 6),
                });
                i++;
                continue;
            }

            // 空行：跳过
            if (string.IsNullOrWhiteSpace(line)) { i++; continue; }

            // 引用块（连续 > 行）
            if (line.TrimStart().StartsWith("> "))
            {
                var para = new List<string>();
                while (i < lines.Length && lines[i].TrimStart().StartsWith("> "))
                {
                    para.Add(lines[i].TrimStart()[2..]);
                    i++;
                }
                result.Add(Quote(para));
                continue;
            }

            // 段落 / 列表（收集到空行或特殊块）
            var blockLines = new List<string>();
            while (i < lines.Length)
            {
                var l = lines[i].TrimEnd();
                if (string.IsNullOrWhiteSpace(l)) break;
                if (l.TrimStart().StartsWith("```")) break;
                if (l.TrimStart().StartsWith('|') && i + 1 < lines.Length && lines[i + 1].TrimStart().StartsWith('|')) break;
                blockLines.Add(l);
                i++;
            }
            result.Add(Paragraph(blockLines));
        }

        return result;
    }

    /// <summary>段落/列表：每行渲染内联（列表行加前缀色）。</summary>
    private static Control Paragraph(List<string> lines)
    {
        var tb = new SelectableTextBlock
        {
            TextWrapping = TextWrapping.Wrap,
            FontSize = 13,
            LineHeight = 20,
        };
        for (int k = 0; k < lines.Count; k++)
        {
            var l = lines[k];
            if (k > 0) tb.Inlines.Add(new LineBreak());

            if (IsListLine(l))
            {
                int prefix = ListPrefixLen(l);
                var bullet = l[..prefix];
                tb.Inlines.Add(new Run("  " + bullet + " ") { Foreground = new SolidColorBrush(Dim) });
                AddInlines(tb, MarkdownInlines.RenderInline(l[prefix..]));
            }
            else
            {
                AddInlines(tb, MarkdownInlines.RenderInline(l));
            }
        }
        return tb;
    }

    private static Control Quote(List<string> lines)
    {
        var tb = new SelectableTextBlock
        {
            TextWrapping = TextWrapping.Wrap,
            FontSize = 13,
            Margin = new Thickness(6, 2, 0, 2),
        };
        for (int k = 0; k < lines.Count; k++)
        {
            if (k > 0) tb.Inlines.Add(new LineBreak());
            tb.Inlines.Add(new Run("│ ") { Foreground = new SolidColorBrush(Accent) });
            AddInlines(tb, MarkdownInlines.RenderInline(lines[k]));
        }
        return tb;
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
        foreach (var inl in inlines) tb.Inlines.Add(inl);

        var border = new Border
        {
            Child = tb,
            Background = new SolidColorBrush(Color.Parse("#161b22")),
            CornerRadius = new CornerRadius(10),
            Padding = new Thickness(12, 8),
            Margin = new Thickness(0, 4, 0, 4),
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

    /// <summary>表格：解析连续 | 行，构建 Grid（表头加粗 + 分隔线），外层带边框。</summary>
    private static Control Table(List<string> rows)
    {
        var parsed = rows.Select(ParseRow).ToList();
        int colCount = parsed.Max(r => r.Count);

        var grid = new Grid { Margin = new Thickness(0, 4, 0, 4) };
        for (int c = 0; c < colCount; c++)
            grid.ColumnDefinitions.Add(new ColumnDefinition(GridLength.Auto));

        int gridRow = 0;
        for (int r = 0; r < parsed.Count; r++)
        {
            // 分隔行（---）跳过
            if (r == 1 && parsed[1].Count > 0 && parsed[1].All(cell => cell.Trim().Trim('-', ':').Length == 0))
                continue;
            grid.RowDefinitions.Add(new RowDefinition(GridLength.Auto));
            for (int c = 0; c < parsed[r].Count; c++)
            {
                var cell = new SelectableTextBlock
                {
                    Text = parsed[r][c].Trim(),
                    FontSize = 12.5,
                    TextWrapping = TextWrapping.Wrap,
                    FontWeight = r == 0 ? FontWeight.Bold : FontWeight.Normal,
                    Margin = new Thickness(10, 4, 10, 4),
                };
                Grid.SetRow(cell, gridRow);
                Grid.SetColumn(cell, c);
                grid.Children.Add(cell);
            }
            gridRow++;
        }

        return new Border
        {
            Child = grid,
            BorderBrush = new SolidColorBrush(Color.Parse("#262b3a")),
            BorderThickness = new Thickness(1),
            CornerRadius = new CornerRadius(8),
            Padding = new Thickness(2),
            Margin = new Thickness(0, 4, 0, 4),
            ClipToBounds = true,
        };
    }

    private static List<string> ParseRow(string line)
    {
        var s = line.Trim().Trim('|'); // 去掉首尾 |
        return s.Split('|').Select(c => c.Trim()).ToList();
    }

    private static bool IsListLine(string l)
        => (l.StartsWith("- ") || l.StartsWith("* ") || l.StartsWith("+ ")) ||
           (l.Length > 2 && char.IsDigit(l[0]) && l[1] == '.' && l[2] == ' ');

    private static int ListPrefixLen(string l)
    {
        if (l.StartsWith("- ") || l.StartsWith("* ") || l.StartsWith("+ ")) return 2;
        int i = 0;
        while (i < l.Length && char.IsDigit(l[i])) i++;
        if (i < l.Length && l[i] == '.' && i + 1 < l.Length && l[i + 1] == ' ') return i + 2;
        return 0;
    }

    private static void AddInlines(SelectableTextBlock tb, List<Inline> inlines)
    {
        foreach (var inl in inlines) tb.Inlines.Add(inl);
    }
}
