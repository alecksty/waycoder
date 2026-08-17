using System.Text;
using Avalonia.Controls.Documents;
using Avalonia.Media;

namespace WayCoder.UI.Gui;

/// <summary>
/// 把 LLM 输出的 Markdown + «tag»…«/» 中间格式标记渲染为 Avalonia Inline 列表。
/// 对标 Web 版 markupToHtml 的颜色映射（同源 AnsiColors），保证三端观感一致。
/// </summary>
public static class MarkdownInlines
{
    private static readonly Dictionary<string, Color> MarkupColors = new(StringComparer.OrdinalIgnoreCase)
    {
        ["red"] = Color.Parse("#ff7b72"), ["green"] = Color.Parse("#3fb950"),
        ["yellow"] = Color.Parse("#d29922"), ["cyan"] = Color.Parse("#39c5cf"),
        ["blue"] = Color.Parse("#58a6ff"), ["magenta"] = Color.Parse("#bc8cff"),
        ["white"] = Color.Parse("#c9d1d9"), ["orange3"] = Color.Parse("#d29922"),
        ["orange"] = Color.Parse("#d29922"), ["grey"] = Color.Parse("#6e7681"),
    };

    private static readonly IBrush CodeBg = new SolidColorBrush(Color.Parse("#1d2230"));
    private static readonly FontFamily MonoFont = new("Menlo,Consolas,monospace");

    /// <summary>把 markdown 渲染进目标 InlineCollection。</summary>
    public static void RenderTo(InlineCollection target, string markdown)
    {
        target.Clear();
        var lines = markdown.Replace("\r\n", "\n").Split('\n');

        var codeBuf = new StringBuilder();
        bool inCode = false;
        var block = new List<string>();

        void FlushParagraph(List<string> para)
        {
            if (para.Count == 0) return;
            bool isList = true;
            foreach (var l in para)
                if (!IsListLine(l)) { isList = false; break; }

            if (isList)
            {
                foreach (var l in para)
                {
                    int prefix = ListPrefixLen(l);
                    var bullet = l[..prefix];
                    var text = l[prefix..];
                    target.Add(new Run("  " + bullet + " ") { Foreground = new SolidColorBrush(Color.Parse("#8b93a7")) });
                    AddInlines(target, RenderInline(text));
                    target.Add(new LineBreak());
                }
                return;
            }

            foreach (var l in para)
            {
                AddInlines(target, RenderInline(l));
                target.Add(new LineBreak());
            }
        }

        foreach (var raw in lines)
        {
            var line = raw.TrimEnd();

            // 代码围栏
            if (line.TrimStart().StartsWith("```"))
            {
                if (inCode)
                {
                    var code = codeBuf.ToString().TrimEnd('\n');
                    if (code.Length > 0)
                    {
                        var span = new Span { Background = CodeBg, FontFamily = MonoFont, FontSize = 12.5 };
                        span.Inlines.Add(new Run(code) { Foreground = new SolidColorBrush(Color.Parse("#c9d1d9")) });
                        target.Add(span);
                        target.Add(new LineBreak());
                    }
                    codeBuf.Clear();
                    inCode = false;
                }
                else
                {
                    FlushParagraph(block); block.Clear();
                    inCode = true;
                }
                continue;
            }

            if (inCode)
            {
                codeBuf.AppendLine(line);
                continue;
            }

            // 标题
            if (line.StartsWith("### ") || line.StartsWith("## ") || line.StartsWith("# "))
            {
                FlushParagraph(block); block.Clear();
                int lvl = line.StartsWith("### ") ? 3 : line.StartsWith("## ") ? 2 : 1;
                var text = line[lvl..].Trim();
                var span = new Span
                {
                    FontWeight = FontWeight.Bold,
                    FontSize = lvl == 1 ? 17 : lvl == 2 ? 15 : 14,
                    Foreground = new SolidColorBrush(Color.Parse("#e6e8ee")),
                };
                span.Inlines.Add(new Run(text));
                target.Add(span);
                target.Add(new LineBreak());
                continue;
            }

            // 分隔线
            if (line.Trim() is "---" or "***" or "___")
            {
                FlushParagraph(block); block.Clear();
                target.Add(new Run("—".PadRight(40, '—')) { Foreground = new SolidColorBrush(Color.Parse("#262b3a")) });
                target.Add(new LineBreak());
                continue;
            }

            // 空行：段落边界
            if (string.IsNullOrWhiteSpace(line))
            {
                FlushParagraph(block); block.Clear();
                continue;
            }

            // 引用
            if (line.StartsWith("> "))
            {
                FlushParagraph(block); block.Clear();
                target.Add(new Run("│ ") { Foreground = new SolidColorBrush(Color.Parse("#4f8cff")) });
                AddInlines(target, RenderInline(line[2..]));
                target.Add(new LineBreak());
                continue;
            }

            block.Add(line);
        }

        FlushParagraph(block);
        if (inCode && codeBuf.Length > 0)
        {
            var span = new Span { Background = CodeBg, FontFamily = MonoFont, FontSize = 12.5 };
            span.Inlines.Add(new Run(codeBuf.ToString().TrimEnd('\n')) { Foreground = new SolidColorBrush(Color.Parse("#c9d1d9")) });
            target.Add(span);
        }
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

    private static void AddInlines(InlineCollection target, List<Inline> inlines)
    {
        foreach (var inl in inlines) target.Add(inl);
    }

    /// <summary>渲染单行内联：处理 «tag»…«/» 标记、**粗体**、`代码`。</summary>
    private static List<Inline> RenderInline(string text)
    {
        var result = new List<Inline>();
        var buf = new StringBuilder();
        var stack = new Stack<string>(); // 活跃的 «tag» 样式（颜色名/样式名）

        void Flush(bool bold, bool mono)
        {
            if (buf.Length == 0) return;
            var run = new Run(buf.ToString());
            ApplyStyles(run, stack, bold, mono);
            result.Add(run);
            buf.Clear();
        }

        bool bold = false, mono = false;
        int i = 0;
        while (i < text.Length)
        {
            // «tag»…«/» 中间格式标记
            if (text[i] == '«')
            {
                int close = text.IndexOf('»', i);
                if (close >= 0)
                {
                    var tag = text[(i + 1)..close].Trim();
                    Flush(bold, mono);
                    if (tag == "/")
                    {
                        if (stack.Count > 0) stack.Pop();
                    }
                    else
                    {
                        // 复合标签 «bold yellow» 按空格拆分，收集有效样式
                        var valid = new List<string>();
                        foreach (var p in tag.Split(' ', StringSplitOptions.RemoveEmptyEntries))
                        {
                            var key = p.Trim();
                            if (MarkupColors.ContainsKey(key) || key is "bold" or "dim" or "underline" or "italic")
                                valid.Add(key);
                        }
                        if (valid.Count > 0) stack.Push(string.Join(' ', valid));
                        else buf.Append('«').Append(tag).Append('»'); // 未知标签原样保留
                    }
                    i = close + 1;
                    continue;
                }
                buf.Append('«'); i++; continue;
            }

            // **粗体**
            if (text[i] == '*' && i + 1 < text.Length && text[i + 1] == '*')
            {
                Flush(bold, mono);
                bold = !bold;
                i += 2;
                continue;
            }
            // `行内代码`
            if (text[i] == '`')
            {
                Flush(bold, mono);
                mono = !mono;
                i++;
                continue;
            }

            buf.Append(text[i]);
            i++;
        }
        Flush(bold, mono);
        return result;
    }

    private static void ApplyStyles(Run run, Stack<string> stack, bool bold, bool mono)
    {
        bool isBold = bold, isItalic = false, isUnderline = false, isDim = false;
        Color? fg = null;

        foreach (var style in stack)
        {
            foreach (var p in style.Split(' ', StringSplitOptions.RemoveEmptyEntries))
            {
                switch (p)
                {
                    case "bold": isBold = true; break;
                    case "italic": isItalic = true; break;
                    case "underline": isUnderline = true; break;
                    case "dim": isDim = true; break;
                    default:
                        if (MarkupColors.TryGetValue(p, out var c)) fg = c;
                        break;
                }
            }
        }

        if (isBold) run.FontWeight = FontWeight.Bold;
        if (isItalic) run.FontStyle = FontStyle.Italic;
        if (isUnderline) run.TextDecorations = TextDecorations.Underline;
        if (mono) { run.FontFamily = MonoFont; run.Foreground = new SolidColorBrush(Color.Parse("#a5d6ff")); }
        else if (isDim) run.Foreground = new SolidColorBrush(Color.Parse("#6e7681")); // dim 用淡灰
        else if (fg.HasValue) run.Foreground = new SolidColorBrush(fg.Value);
        else run.Foreground = new SolidColorBrush(Color.Parse("#e6e8ee"));
    }
}
