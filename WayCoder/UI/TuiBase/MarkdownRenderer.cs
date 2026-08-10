namespace WayCoder.UI;

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

/// <summary>列表项 - 或 * 或 1.</summary>
public class MdListItem : MdNode
{
    public string Text { get; set; } = "";
    public bool Ordered { get; set; }
    public int OrderNum { get; set; }
    public int Level { get; set; }  // 缩进级别 (0/1/2...)
}

/// <summary>分割线 ---</summary>
public class MdRule : MdNode { }

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
                if (table != null) nodes.Add(table);
                continue;
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
                nodes.Add(new MdListItem { Text = itemText.Trim(), Ordered = isOrdered, OrderNum = orderNum, Level = level });
                i++; continue;
            }

            // 普通段落（可能跨多行）
            {
                var paraSb = new System.Text.StringBuilder();
                while (i < lines.Length && !string.IsNullOrWhiteSpace(lines[i])
                    && !lines[i].TrimStart().StartsWith("```")
                    && !lines[i].TrimStart().StartsWith('|')
                    && !lines[i].TrimStart().StartsWith('#')
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
    /// 支持 **加粗**、*斜体*、`代码`。
    /// 返回 (文本, ANSI颜色码, 背景色码) 列表。
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

        while (i < text.Length)
        {
            // 内联代码 `code`
            if (text[i] == '`' && i + 1 < text.Length)
            {
                var end = text.IndexOf('`', i + 1);
                if (end > i)
                {
                    if (current.Length > 0)
                    {
                        result.Add((current.ToString(), defaultColor, defaultBg));
                        current.Clear();
                    }
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
                    if (current.Length > 0)
                    {
                        result.Add((current.ToString(), defaultColor, defaultBg));
                        current.Clear();
                    }
                    result.Add((text[(i + 2)..end], 1, defaultBg)); // Bold
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
                    if (current.Length > 0)
                    {
                        result.Add((current.ToString(), defaultColor, defaultBg));
                        current.Clear();
                    }
                    result.Add((text[(i + 1)..end], 3, defaultBg)); // Italic
                    i = end + 1;
                    continue;
                }
            }

            current.Append(text[i]);
            i++;
        }

        if (current.Length > 0)
            result.Add((current.ToString(), defaultColor, defaultBg));

        return result;
    }

    // ================================================================
    // 内部工具
    // ================================================================

    private static MdTable? ParseTable(string[] lines, ref int i)
    {
        var allRows = new List<string[]>();
        while (i < lines.Length && lines[i].TrimStart().StartsWith('|'))
        {
            var cells = lines[i].Trim().Trim('|').Split('|')
                .Select(c => c.Trim()).ToArray();
            if (cells.Length > 0) allRows.Add(cells);
            i++;
        }
        if (allRows.Count < 2) return null;

        // 跳过分隔行 |---|----|
        var hasSeparator = allRows.Count > 1 &&
            allRows[1].All(c => c.All(ch => ch == '-' || ch == ':' || ch == ' '));
        var headers = allRows[0].ToList();
        var dataRows = hasSeparator
            ? allRows.Skip(2).Select(r => r.ToList()).ToList()
            : allRows.Skip(1).Select(r => r.ToList()).ToList();

        return new MdTable { Headers = headers, Rows = dataRows };
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
