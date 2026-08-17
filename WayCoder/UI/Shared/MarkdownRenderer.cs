namespace WayCoder.UI.Shared;

/// <summary>
/// Markdown AST 节点。
/// </summary>
public abstract class MdNode
{
    public int StartLine { get; set; }
}

/// <summary>标题 # ## ###</summary>
public class MdHeading : MdNode
{
    public int Level { get; set; }  // 1-4
    public string Text { get; set; } = "";
}

/// <summary>普通段落</summary>
public class MdParagraph : MdNode
{
    public string Text { get; set; } = "";
}

/// <summary>代码块 ```lang\ncode\n```</summary>
public class MdCodeBlock : MdNode
{
    public string Language { get; set; } = "";
    public string Code { get; set; } = "";
}

/// <summary>表格 | a | b |</summary>
public class MdTable : MdNode
{
    public List<string> Headers { get; set; } = [];
    public List<List<string>> Rows { get; set; } = [];
}

/// <summary>列表项 - 或 * 或 1.（任务清单用 Checked 标记 [x]/[ ]）</summary>
public class MdListItem : MdNode
{
    public string Text { get; set; } = "";
    public bool Ordered { get; set; }
    public int OrderNum { get; set; }
    public int Level { get; set; }  // 缩进级别 (0/1/2...)
    public bool? Checked { get; set; }  // null=普通列表；true=[x]；false=[ ]
}

/// <summary>分割线 ---</summary>
public class MdRule : MdNode { }

/// <summary>引用块 &gt;（多行合并）</summary>
public class MdBlockQuote : MdNode
{
    public string Text { get; set; } = "";
}

/// <summary>«tag»…«/» 块级标记（跨多行的推理/思考内容，保留原始换行与空行）</summary>
public class MdMarkup : MdNode
{
    public string Text { get; set; } = "";
    public int Style { get; set; }
}

// ================================================================
// Markdown 解析器
// ================================================================

/// <summary>
/// 轻量 Markdown 解析器 —— 纯 C# 实现，AOT 兼容，零依赖。
/// 支持：标题、段落、代码块、表格、列表、分割线、内联格式。
/// </summary>
public static class MarkdownParser
{
    /// <summary>将 Markdown 文本解析为 AST 节点列表</summary>
    public static List<MdNode> Parse(string markdown)
    {
        var nodes = new List<MdNode>();
        if (string.IsNullOrWhiteSpace(markdown)) return nodes;

        var lines = markdown.Replace("\r\n", "\n").Split('\n');
        int i = 0;

        while (i < lines.Length)
        {
            var line = lines[i];

            // 空行跳过
            if (string.IsNullOrWhiteSpace(line)) { i++; continue; }

            // «tag»…«/» 块级标记：跨多行（含空行/代码/列表）的推理内容，按原始文本整体渲染。
            // 与内联用法区分：仅当开标签在行首、且本行内无闭合 «/» 时才走块级（否则交 ParseInline 内联处理）。
            if (line.TrimStart().StartsWith('\xAB'))
            {
                var tline = line.TrimStart();
                int openClose = tline.IndexOf('\xBB');
                if (openClose > 1)
                {
                    string openTag = tline[1..openClose].Trim();
                    int style = openTag == "/" ? 0 : MapMarkupTag(openTag);
                    if (style > 0)
                    {
                        string rest = tline[(openClose + 1)..];
                        if (rest.IndexOf("\xAB/\xBB", StringComparison.Ordinal) < 0)
                        {
                            var sb = new System.Text.StringBuilder(rest);
                            i++;
                            while (i < lines.Length)
                            {
                                var rl = lines[i];
                                int close = rl.IndexOf("\xAB/\xBB", StringComparison.Ordinal);
                                if (close >= 0)
                                {
                                    if (sb.Length > 0) sb.Append('\n');
                                    sb.Append(rl[..close]);
                                    i++;
                                    break;
                                }
                                if (sb.Length > 0) sb.Append('\n');
                                sb.Append(rl);
                                i++;
                            }
                            nodes.Add(new MdMarkup { Text = sb.ToString(), Style = style });
                            continue;
                        }
                    }
                }
            }

            // 代码块 ```lang\n...\n```
            if (line.TrimStart().StartsWith("```"))
            {
                var lang = line.TrimStart()[3..].Trim();
                var sb = new System.Text.StringBuilder();
                i++;
                while (i < lines.Length)
                {
                    if (lines[i].TrimStart().StartsWith("```")) { i++; break; }
                    sb.AppendLine(lines[i]);
                    i++;
                }
                nodes.Add(new MdCodeBlock { Language = lang, Code = sb.ToString().TrimEnd(), StartLine = 0 });
                continue;
            }

            // 表格 | a | b |
            if (line.TrimStart().StartsWith('|') && line.TrimEnd().EndsWith('|'))
            {
                var table = ParseTable(lines, ref i);
                if (table != null)
                {
                    nodes.Add(table);
                    continue;
                }
                // 非表格竖线内容（如单行「| 文本 |」）→ 剥掉首尾竖线按普通段落处理，避免被吞行
                var stripped = line.Trim().Trim('|').Trim();
                if (stripped.Length > 0)
                {
                    nodes.Add(new MdParagraph { Text = stripped });
                    i++;
                    continue;
                }
            }

            // 标题 # ## ### ####
            var headingLevel = 0;
            var trimmed = line.TrimStart();
            while (headingLevel < trimmed.Length && trimmed[headingLevel] == '#' && headingLevel < 6)
                headingLevel++;
            if (headingLevel > 0 && headingLevel <= 4 &&
                (headingLevel < trimmed.Length && trimmed[headingLevel] == ' '))
            {
                nodes.Add(new MdHeading
                {
                    Level = headingLevel,
                    Text = trimmed[(headingLevel + 1)..].Trim(),
                });
                i++; continue;
            }

            // 引用块 > （连续多行合并为一个节点）
            if (line.TrimStart().StartsWith('>'))
            {
                var quoteLines = new List<string>();
                while (i < lines.Length)
                {
                    var q = lines[i].TrimStart();
                    if (!q.StartsWith('>')) break;
                    quoteLines.Add(q[1..].TrimStart());
                    i++;
                }
                if (quoteLines.Count > 0)
                    nodes.Add(new MdBlockQuote { Text = string.Join("\n", quoteLines) });
                continue;
            }

            // 分割线 --- *** ___
            if (IsHorizontalRule(line.Trim()))
            {
                nodes.Add(new MdRule());
                i++; continue;
            }

            // 列表项 - 或 * 或 1.（根据前导空格判断层级）
            if (IsListItem(line.TrimStart(), out var isOrdered, out var orderNum, out var itemText))
            {
                var leading = line.Length - line.TrimStart().Length;
                var level = leading / 2; // 每2空格=1级缩进

                // 任务清单 - [ ] / - [x]
                bool? checkedBox = null;
                var text = itemText.Trim();
                if (text.StartsWith("[ ]") || text.StartsWith("[x]") || text.StartsWith("[X]"))
                {
                    checkedBox = text.StartsWith("[x]") || text.StartsWith("[X]");
                    text = text[3..].TrimStart();
                }

                nodes.Add(new MdListItem
                {
                    Text = text,
                    Ordered = isOrdered,
                    OrderNum = orderNum,
                    Level = level,
                    Checked = checkedBox,
                });
                i++; continue;
            }

            // 普通段落（可能跨多行）
            {
                var paraSb = new System.Text.StringBuilder();
                while (i < lines.Length && !string.IsNullOrWhiteSpace(lines[i])
                    && !lines[i].TrimStart().StartsWith("```")
                    && !lines[i].TrimStart().StartsWith('|')
                    && !lines[i].TrimStart().StartsWith('#')
                    && !lines[i].TrimStart().StartsWith('>')
                    && !IsHorizontalRule(lines[i].Trim())
                    && !IsListItem(lines[i].TrimStart(), out _, out _, out _))
                {
                    if (paraSb.Length > 0) paraSb.Append(' ');
                    paraSb.Append(lines[i].Trim());
                    i++;
                }
                if (paraSb.Length > 0)
                    nodes.Add(new MdParagraph { Text = paraSb.ToString() });
            }
        }
        return nodes;
    }

    // ================================================================
    // 内联格式处理
    // ================================================================

    /// <summary>
    /// 将一行文本中的内联格式转换为带 ANSI 颜色的片段。
    /// 支持 **加粗**、*斜体*、`代码`、~~删除线~~、[链接](url)，以及 «tag»…«/» 标记。
    /// 返回 (文本, ANSI颜色码, 背景色码) 列表。
    /// 颜色码语义：1-9=样式属性(粗体/淡化/斜体/下划线/反白/删除线)，30-37/90-97=标准色。
    /// </summary>
    public static List<(string Text, int Color, int Bg)> ParseInline(string text,
        int defaultColor = 0, int defaultBg = 0)
    {
        var result = new List<(string Text, int Color, int Bg)>();
        if (string.IsNullOrEmpty(text))
        {
            result.Add(("", defaultColor, defaultBg));
            return result;
        }

        int i = 0;
        var current = new System.Text.StringBuilder();
        // «tag» 样式栈：进入 span 前压栈，«/» 弹栈恢复（支持嵌套与流式未闭合 span）
        var styleStack = new Stack<int>();
        int curColor = defaultColor;

        void FlushCurrent()
        {
            if (current.Length > 0)
            {
                result.Add((current.ToString(), curColor, defaultBg));
                current.Clear();
            }
        }

        while (i < text.Length)
        {
            // Markup 标记 «tag»（样式/颜色）与 «/»（复位到上一级）
            if (text[i] == '\xAB') // «
            {
                int close = text.IndexOf('\xBB', i + 1);
                if (close > i)
                {
                    string tag = text[(i + 1)..close].Trim();
                    if (tag == "/")
                    {
                        FlushCurrent();
                        curColor = styleStack.Count > 0 ? styleStack.Pop() : defaultColor;
                        i = close + 1;
                        continue;
                    }
                    int code = MapMarkupTag(tag);
                    if (code > 0)
                    {
                        FlushCurrent();
                        styleStack.Push(curColor);
                        curColor = code;
                        i = close + 1;
                        continue;
                    }
                    // 未知标签：不识别，按字面输出（保留 « 原样）
                }
            }

            // 链接 [文字](url)
            if (text[i] == '[')
            {
                var closeBracket = text.IndexOf(']', i + 1);
                if (closeBracket > i && closeBracket + 1 < text.Length && text[closeBracket + 1] == '(')
                {
                    var closeParen = text.IndexOf(')', closeBracket + 2);
                    if (closeParen > closeBracket)
                    {
                        FlushCurrent();
                        var linkText = text[(i + 1)..closeBracket];
                        var url = text[(closeBracket + 2)..closeParen];
                        result.Add((linkText, 36, defaultBg)); // 青色链接文字
                        if (!string.IsNullOrEmpty(url) && url != linkText)
                            result.Add(($" ({url})", 2, defaultBg)); // 弱化显示 URL
                        i = closeParen + 1;
                        continue;
                    }
                }
            }

            // 内联代码 `code`
            if (text[i] == '`' && i + 1 < text.Length)
            {
                var end = text.IndexOf('`', i + 1);
                if (end > i)
                {
                    FlushCurrent();
                    result.Add((text[(i + 1)..end], 33, 48));  // 黄色文字 + 深色背景
                    i = end + 1;
                    continue;
                }
            }

            // **加粗**
            if (text[i] == '*' && i + 1 < text.Length && text[i + 1] == '*')
            {
                var end = text.IndexOf("**", i + 2);
                if (end > i)
                {
                    FlushCurrent();
                    result.Add((text[(i + 2)..end], 1, defaultBg)); // Bold
                    i = end + 2;
                    continue;
                }
            }

            // ~~删除线~~
            if (text[i] == '~' && i + 1 < text.Length && text[i + 1] == '~')
            {
                var end = text.IndexOf("~~", i + 2);
                if (end > i)
                {
                    FlushCurrent();
                    result.Add((text[(i + 2)..end], 2, defaultBg)); // 弱化 = 删除线
                    i = end + 2;
                    continue;
                }
            }

            // *斜体* (单独 *，不是 **)
            if (text[i] == '*' && (i == 0 || text[i - 1] != '*') &&
                (i + 1 >= text.Length || text[i + 1] != '*'))
            {
                var end = text.IndexOf('*', i + 1);
                if (end > i + 1 && (end + 1 >= text.Length || text[end + 1] != '*'))
                {
                    FlushCurrent();
                    result.Add((text[(i + 1)..end], 3, defaultBg)); // Italic
                    i = end + 1;
                    continue;
                }
            }

            current.Append(text[i]);
            i++;
        }

        FlushCurrent();
        return result;
    }

    /// <summary>
    /// 将 «» 标记的标签名映射为颜色码（样式属性 1-9 / 标准色 30-37 / 亮色 90-97）。
    /// 未知标签返回 0（调用方按字面输出）。多词标签（如「bold yellow」「bright red」）
    /// 取颜色词、样式前缀丢弃（对齐 Program.cs MarkupLine 的「粗体=颜色」约定）。
    /// </summary>
    private static int MapMarkupTag(string tag)
    {
        tag = tag.Trim().ToLowerInvariant();
        switch (tag)
        {
            case "bold": case "bright": return 1;    // 粗体/加亮
            case "dim": case "faint": return 2;      // 淡化
            case "italic": case "i": return 3;       // 斜体
            case "underline": case "u": return 4;    // 下划线
            case "blink": return 5;                  // 闪烁
            case "reverse": case "invert": return 7; // 反白
            case "strike": case "strikethrough": case "s": return 9; // 删除线
        }

        // 颜色（支持 bright 前缀与 bold/underline 等样式前缀）
        string colorName = tag;
        bool bright = false;
        if (tag.Contains(' '))
        {
            foreach (var p in tag.Split(' '))
            {
                if (p.Length == 0) continue;
                if (p is "bold" or "dim" or "underline" or "italic") continue;
                if (p == "bright") { bright = true; continue; }
                colorName = p;
                break;
            }
        }

        int code = colorName switch
        {
            "black" => 30,
            "red" => 31,
            "green" => 32,
            "yellow" => 33,
            "blue" => 34,
            "magenta" or "purple" => 35,
            "cyan" => 36,
            "white" => 37,
            "grey" or "gray" => 90,
            "orange3" or "orange" => 33,
            _ => 0,
        };
        if (code == 0) return 0;
        if (bright && code is >= 30 and <= 37)
            return code + 60;   // 亮色 90-97
        return code;
    }

    // ================================================================
    // 内部工具
    // ================================================================

    private static MdTable? ParseTable(string[] lines, ref int i)
    {
        // 先窥探连续的 | 行（不消费），不足 2 行不构成表格，交由调用方按普通文本处理，避免吞行
        int peek = i;
        var allRows = new List<string[]>();
        while (peek < lines.Length && lines[peek].TrimStart().StartsWith('|'))
        {
            var cells = SplitTableCells(lines[peek]);
            if (cells.Length > 0) allRows.Add(cells);
            peek++;
        }
        if (allRows.Count < 2) return null;

        i = peek; // 确认构成表格后才统一消费

        // 跳过分隔行 |---|----|
        var hasSeparator = allRows[1].All(c => c.All(ch => ch == '-' || ch == ':' || ch == ' '));
        var headers = allRows[0].ToList();
        var dataRows = hasSeparator
            ? allRows.Skip(2).Select(r => r.ToList()).ToList()
            : allRows.Skip(1).Select(r => r.ToList()).ToList();

        return new MdTable { Headers = headers, Rows = dataRows };
    }

    /// <summary>
    /// 按 | 拆分表格单元格，支持「\|」转义竖线（单元格内出现字面竖线时不误拆）。
    /// 首尾竖线剥除后，仅「\|」被视为转义（替换为 |），其余字符原样保留。
    /// </summary>
    private static string[] SplitTableCells(string line)
    {
        var s = line.Trim();
        if (s.StartsWith('|')) s = s[1..];
        if (s.EndsWith('|')) s = s[..^1];

        var cells = new List<string>();
        var sb = new System.Text.StringBuilder();
        for (var j = 0; j < s.Length; j++)
        {
            var ch = s[j];
            if (ch == '\\' && j + 1 < s.Length && s[j + 1] == '|')
            {
                sb.Append('|'); // 转义竖线
                j++;
                continue;
            }
            if (ch == '|')
            {
                cells.Add(sb.ToString().Trim());
                sb.Clear();
                continue;
            }
            sb.Append(ch);
        }
        cells.Add(sb.ToString().Trim());
        return cells.ToArray();
    }

    private static bool IsHorizontalRule(string line)
    {
        if (line.Length < 3) return false;
        var ch = line[0];
        if (ch != '-' && ch != '*' && ch != '_') return false;
        return line.All(c => c == ch || c == ' ');
    }

    private static bool IsListItem(string line, out bool ordered,
        out int orderNum, out string text)
    {
        ordered = false; orderNum = 0; text = "";

        // 无序列表 - 或 *
        if ((line.StartsWith("- ") || line.StartsWith("* ")) && line.Length > 2)
        {
            text = line[2..];
            return true;
        }

        // 有序列表 1. 2. etc
        int j = 0;
        while (j < line.Length && char.IsDigit(line[j])) j++;
        if (j > 0 && j < line.Length - 2 && line[j] == '.' && line[j + 1] == ' ')
        {
            if (int.TryParse(line[..j], out orderNum))
            {
                ordered = true;
                text = line[(j + 2)..];
                return true;
            }
        }

        return false;
    }
}
