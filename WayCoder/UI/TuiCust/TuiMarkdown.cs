using WayCoder.Terminal;

namespace WayCoder.UI;

/// <summary>
/// Markdown → 聊天屏幕行适配器。
/// 将 ChatMsg 的文本内容经 Markdown 解析后，渲染为带 ANSI 颜色码的屏幕行。
/// 代码块自动调用 Syntax 进行语法高亮，表格使用 BoxBuffer 边框。
/// </summary>
public static class TuiMarkdown
{
    /// <summary>
    /// 渲染一条聊天消息为屏幕行列表。
    /// 每行是 (文本, 前景色, 背景色) 片段列表。
    /// </summary>
    public static List<List<(string Text, int Fg, int Bg)>> RenderMessage(
        string content, string role, int maxWidth, bool plainText = false)
    {
        var result = new List<List<(string Text, int Fg, int Bg)>>();

        // 彩虹横幅（role == "banner"）：逐行生成逐字 TrueColor 渐变片段
        // 横向居中由 TuiMarkdown.ContentAlign 在 OnRender 时处理
        if (plainText && role == "banner")
        {
            var lines = content.Split('\n');
            int totalLines = lines.Length;
            for (int li = 0; li < totalLines; li++)
            {
                var line = lines[li];
                if (line.Length == 0) { result.Add([]); continue; }
                result.Add(BuildRainbowSegments(line, li, totalLines));
            }
            return result;
        }

        // 纯文本模式 + ANSI 内容：按行原样渲染，不做 Markdown 解析
        if (plainText || content.Contains('\x1b'))
        {
            int defaultFg = FgForRole(role);
            foreach (var rawLine in content.Split('\n'))
            {
                result.Add(new List<(string, int, int)> { (rawLine, defaultFg, 0) });
            }
            return result;
        }

        // 尝试 Markdown 解析
        var nodes = MarkdownParser.Parse(content);

        if (nodes.Count == 0)
        {
            // 纯文本回退
            result.Add(new List<(string, int, int)> { (content, FgForRole(role), 0) });
            return result;
        }

        // 如果第一个节点是纯段落且没有特殊格式，回退到简单渲染
        if (nodes.Count == 1 && nodes[0] is MdParagraph p &&
            !p.Text.Contains('*') && !p.Text.Contains('`') && !p.Text.Contains('#'))
        {
            result.Add(new List<(string, int, int)> { (p.Text, FgForRole(role), 0) });
            return result;
        }

        // 渲染每个 AST 节点
        foreach (var node in nodes)
        {
            switch (node)
            {
                case MdHeading h:
                    RenderHeading(h, result, maxWidth);
                    break;
                case MdCodeBlock cb:
                    RenderCodeBlock(cb, result, maxWidth);
                    break;
                case MdTable t:
                    RenderTable(t, result, maxWidth);
                    break;
                case MdListItem li:
                    RenderListItem(li, result, maxWidth, FgForRole(role));
                    break;
                case MdRule:
                    result.Add(new List<(string, int, int)> { (new string('━', Math.Min(maxWidth, 60)), 2, 0) });
                    break;
                case MdParagraph para:
                    RenderParagraph(para, result, maxWidth, FgForRole(role));
                    break;
            }
            // 消息内部节点间不加空行（消息间空行由 TuiListView.ItemSpacing 统一控制）
        }

        return result.Count > 0 ? result
            : new List<List<(string, int, int)>> { new() { (content, FgForRole(role), 0) } };
    }

    // ================================================================
    // 节点渲染
    // ================================================================

    private static void RenderHeading(MdHeading h,
        List<List<(string, int, int)>> result, int maxWidth)
    {
        var prefix = new string('#', h.Level) + " ";
        var color = h.Level <= 2 ? 97 : 37;  // H1-H2 亮白，H3-H4 白
        var line = new List<(string, int, int)> { (prefix, 33, 0), (h.Text, color, 0) };
        result.Add(line);
    }

    private static void RenderCodeBlock(MdCodeBlock cb,
        List<List<(string, int, int)>> result, int maxWidth)
    {
        var syntax = GetSyntax(cb.Language);
        var codeLines = cb.Code.Split('\n');

        // 顶部边框：语言标签
        var langLabel = string.IsNullOrEmpty(cb.Language) ? " code " : $" {cb.Language} ";
        var topBorder = "┌" + langLabel + new string('─', Math.Max(0, Math.Min(maxWidth, 60) - langLabel.Length - 2)) + "┐";
        result.Add(new List<(string, int, int)> { (topBorder, 2, 0) });

        int lineNum = 1;
        foreach (var rawLine in codeLines)
        {
            var line = new List<(string, int, int)>();
            // 行号
            line.Add(($" {lineNum,3} ", 2, 0));

            if (string.IsNullOrEmpty(rawLine))
            {
                line.Add((" ", 0, 0));
            }
            else
            {
                var tokens = syntax.Tokenize(rawLine);
                foreach (var (text, color) in tokens)
                {
                    line.Add((text, color, 0));
                }
            }
            result.Add(line);
            lineNum++;
        }

        // 底部边框
        result.Add(new List<(string, int, int)> { (new string('─', Math.Min(maxWidth, 60)), 2, 0) });
    }

    private static void RenderTable(MdTable t,
        List<List<(string, int, int)>> result, int maxWidth)
    {
        if (t.Headers.Count == 0) return;

        // 计算每列的视觉宽度（解析内联格式后）
        var colCount = t.Headers.Count;
        var colWidths = new int[colCount];
        for (int c = 0; c < colCount; c++)
        {
            colWidths[c] = InlineVw(t.Headers[c]);
        }
        foreach (var row in t.Rows)
        {
            for (int c = 0; c < Math.Min(colCount, row.Count); c++)
            {
                var w = InlineVw(row[c]);
                if (w > colWidths[c]) colWidths[c] = w;
            }
        }
        // 每列最少 3 字符宽
        for (int c = 0; c < colCount; c++)
            colWidths[c] = Math.Max(3, colWidths[c] + 2);

        // 计算总宽并限制
        var totalW = colWidths.Sum() + colCount + 1;
        if (totalW > maxWidth)
        {
            var scale = (double)(maxWidth - colCount - 1) / (totalW - colCount - 1);
            for (int c = 0; c < colCount; c++)
                colWidths[c] = Math.Max(3, (int)(colWidths[c] * scale));
        }

        // 边框字符
        var top = "┌" + string.Join("┬", colWidths.Select(w => new string('─', w))) + "┐";
        var sep = "├" + string.Join("┼", colWidths.Select(w => new string('─', w))) + "┤";
        var bot = "└" + string.Join("┴", colWidths.Select(w => new string('─', w))) + "┘";

        // 渲染表头
        result.Add(new List<(string, int, int)> { (top, 2, 0) });
        result.Add(BuildRow(t.Headers, colWidths, isHeader: true));
        result.Add(new List<(string, int, int)> { (sep, 2, 0) });

        // 渲染数据行
        foreach (var row in t.Rows)
        {
            var cells = new List<string>();
            for (int c = 0; c < colCount; c++)
                cells.Add(c < row.Count ? row[c] : "");
            result.Add(BuildRow(cells, colWidths, isHeader: false));
        }
        result.Add(new List<(string, int, int)> { (bot, 2, 0) });
    }

    private static List<(string, int, int)> BuildRow(List<string> cells,
        int[] widths, bool isHeader)
    {
        var line = new List<(string, int, int)>();
        line.Add(("│", 2, 0));
        int defaultFg = isHeader ? 1 : 0;

        for (int c = 0; c < widths.Length; c++)
        {
            var text = c < cells.Count ? cells[c] : "";

            // 解析单元格内联格式（**加粗**、`代码` 等）
            var segments = MarkdownParser.ParseInline(text, defaultFg, 0);

            // 计算已渲染片段的视觉总宽
            int segVw = 0;
            foreach (var (segText, _, _) in segments)
                segVw += VwPlainText(segText);

            int pad = widths[c] - segVw;
            int padLeft = 0, padRight = pad;
            if (isHeader && pad > 0)
            {
                padLeft = pad / 2;
                padRight = pad - padLeft;
            }

            // 左边距（仅居中时使用）
            if (padLeft > 0)
                line.Add((new string(' ', padLeft), defaultFg, 0));

            // 内联格式化片段
            foreach (var seg in segments)
                line.Add(seg);

            // 右边距
            if (padRight > 0)
                line.Add((new string(' ', padRight), defaultFg, 0));

            line.Add(("│", 2, 0));
        }
        return line;
    }

    private static void RenderListItem(MdListItem li,
        List<List<(string, int, int)>> result, int maxWidth, int defaultFg)
    {
        var indent = new string(' ', li.Level * 2);  // 每级缩进2格
        var bullet = li.Ordered ? $"{li.OrderNum}." : "•";
        var prefix = $"{indent}  {bullet} ";
        var line = new List<(string, int, int)>();
        line.Add((prefix, 33, 0));
        foreach (var seg in MarkdownParser.ParseInline(li.Text, defaultFg))
            line.Add(seg);
        result.Add(line);
    }

    private static void RenderParagraph(MdParagraph p,
        List<List<(string, int, int)>> result, int maxWidth, int defaultFg)
    {
        var segments = MarkdownParser.ParseInline(p.Text, defaultFg);
        if (segments.Count == 1 && segments[0].Color == defaultFg)
        {
            // 纯文本，需要折行
            var text = segments[0].Text;
            foreach (var wrapped in WrapText(text, maxWidth - 2))
            {
                result.Add(new List<(string, int, int)> { (wrapped, defaultFg, 0) });
            }
        }
        else
        {
            // 有内联格式
            result.Add(segments.ToList());
        }
    }

    // ================================================================
    // 工具方法
    // ================================================================

    /// <summary>按视觉宽度折行</summary>
    private static List<string> WrapText(string text, int maxVw)
    {
        var lines = new List<string>();
        if (string.IsNullOrEmpty(text)) { lines.Add(""); return lines; }
        int start = 0;
        while (start < text.Length)
        {
            var slice = text[start..];
            int vw = 0, chars = 0;
            foreach (var rune in slice.EnumerateRunes())
            {
                var w = TuiHelper.RuneWidth(rune);
                if (vw + w > maxVw) break;
                vw += w; chars += rune.Utf16SequenceLength;
            }
            if (chars == 0) chars = 1; // 至少前进一个字符
            lines.Add(text[start..(start + chars)]);
            start += chars;
        }
        return lines;
    }

    /// <summary>按视觉宽度填充文本</summary>
    private static string PadByWidth(string text, int totalVw, bool center)
    {
        var vw = VwPlainText(text);
        var pad = totalVw - vw;
        if (pad <= 0) return text;
        if (center)
        {
            var left = pad / 2; var right = pad - left;
            return new string(' ', left) + text + new string(' ', right);
        }
        return text + new string(' ', pad);
    }

    /// <summary>计算纯文本视觉宽度（忽略 ANSI 转义码）</summary>
    public static int VwPlainText(string text)
    {
        // 剥离 ANSI 转义码
        var clean = StripAnsi(text);
        return TuiHelper.DisplayWidth(clean);
    }

    /// <summary>计算内联格式化文本的视觉宽度（排除标记符如 ** ` 等）</summary>
    private static int InlineVw(string text)
    {
        var segments = MarkdownParser.ParseInline(text, 0, 0);
        int total = 0;
        foreach (var (t, _, _) in segments)
            total += VwPlainText(t);
        return total;
    }

    /// <summary>剥离 ANSI 转义码 → Terminal.AnsiString</summary>
    public static string StripAnsi(string text) => Terminal.AnsiString.Strip(text);

    /// <summary>两个相邻节点之间是否需要空行</summary>
    private static bool NeedsSpacing(MdNode? current, MdNode? next)
    {
        if (current == null || next == null) return false;
        // 标题后留空
        if (current is MdHeading) return true;
        // 代码块后留空
        if (current is MdCodeBlock) return true;
        // 表格后留空
        if (current is MdTable) return true;
        // 分割线后不空
        if (current is MdRule) return false;
        // 列表项之间不空
        if (current is MdListItem && next is MdListItem) return false;
        // 段落跟标题/代码块/表格之间留空
        if (current is MdParagraph && (next is MdHeading or MdCodeBlock or MdTable)) return true;
        // 其他情况不空
        return false;
    }

    /// <summary>获取角色对应的默认前景色</summary>
    private static int FgForRole(string role) => role switch
    {
        "user" => 36,    // Cyan
        "tool" => 2,     // Dim
        "system" => 2,   // Dim
        _ => 0,          // Default (agent)
    };

    /// <summary>按语言名获取 Syntax 实例（代码块高亮）</summary>
    private static Syntax GetSyntax(string lang) => Syntax.ByLanguage(lang);

    // ════════════════════════════════════════════════════════════
    // 彩虹横幅渲染
    // ════════════════════════════════════════════════════════════

    /// <summary>彩虹七色锚点（红→橙→金→绿→青→蓝→紫）</summary>
    private static readonly int[] _rainbowStops =
    {
        AnsiTty.RgbCode(255, 0, 0),     // Red
        AnsiTty.RgbCode(255, 140, 0),   // Orange
        AnsiTty.RgbCode(255, 215, 0),   // Gold
        AnsiTty.RgbCode(0, 200, 0),     // Green
        AnsiTty.RgbCode(0, 200, 255),   // Cyan
        AnsiTty.RgbCode(100, 100, 255), // Blue
        AnsiTty.RgbCode(180, 0, 255),   // Purple
    };

    /// <summary>
    /// 为单行文本生成逐字 TrueColor 彩虹渐变片段列表。
    /// 第 lineIndex 行从 _rainbowStops[lineIndex] 渐变到 _rainbowStops[lineIndex+1]。
    /// 每个字符一个片段 (文本, 前景色, 0)，走标准 WriteAt 渲染路径。
    /// </summary>
    private static List<(string Text, int Fg, int Bg)> BuildRainbowSegments(
        string text, int lineIndex, int totalLines)
    {
        var segments = new List<(string, int, int)>();

        // 将行索引映射到彩虹色锚点（line 0→Red, line 5→Purple）
        int startIdx = Math.Min(lineIndex, _rainbowStops.Length - 2);
        int endIdx = Math.Min(lineIndex + 1, _rainbowStops.Length - 1);
        int startColor = _rainbowStops[startIdx];
        int endColor = _rainbowStops[endIdx];

        for (int i = 0; i < text.Length; i++)
        {
            float t = text.Length > 1 ? (float)i / (text.Length - 1) : 0;
            int color = AnsiTty.LerpRgb(startColor, endColor, t);
            segments.Add((text[i].ToString(), color, 0));
        }
        return segments;
    }
}
