using WayCoder.UI.Shared.Terminal;

using WayCoder.UI.Shared;
using WayCoder.UI.Tui.Edit;
using Terminal = WayCoder.UI.Shared.Terminal;

namespace WayCoder.UI.Tui;

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
    /// <param name="isError">错误输出模式：整体保持角色默认色（红色由控件层应用），不做语法高亮</param>
    public static List<List<(string Text, int Fg, int Bg)>> RenderMessage(
        string content, string role, int maxWidth, bool plainText = false, bool isError = false)
    {
        var result = new List<List<(string Text, int Fg, int Bg)>>();

        // 彩虹横幅（role == "banner"）：逐行生成逐字 TrueColor 渐变片段
        // 横向居中由 TuiMarkdown.ContentAlign 在 OnRender 时处理
        if (plainText && role == "banner")
        {
            var lines = content.Split('\n');
            int visualLines = lines.Count(l => l.Length > 0);
            int visualIdx = 0;
            for (int li = 0; li < lines.Length; li++)
            {
                var line = lines[li];
                if (line.Length == 0) { result.Add([]); continue; }
                result.Add(BuildRainbowSegments(line, visualIdx, visualLines));
                visualIdx++;
            }
            return result;
        }

        // 纯文本模式 + ANSI 内容：按行原样渲染，不做 Markdown 解析
        if (plainText || content.Contains(AnsiTty.AnsiCharPrefix))
        {
            int defaultFg = FgForRole(role);
            // 工具输出（system/tool 纯文本）启发式检测为代码时逐行语法着色：
            // read_file 读出的源码、grep 结果等不再单调灰色，与 assistant 代码块一致。
            // 错误输出/已含 ANSI 码的内容保持原样（红色由控件层 IsError 应用，避免被 token 色覆盖）。
            Syntax? codeSyntax = plainText && !isError
                && !content.Contains(AnsiTty.AnsiCharPrefix)
                ? Syntax.Detect(content)
                : null;

            foreach (var rawLine in content.Split('\n'))
            {
                // «grey» 这类中间格式标记必须在渲染层解码成颜色段，否则用户直接看到字面量。
                // 只解码 «»、不做完整内联解析 —— 纯文本走的是 system/tool 输出，
                // 里面的反引号/星号是数据，交给 ParseInline 会被当 Markdown 吃掉。
                if (rawLine.Contains('\xAB'))
                {
                    result.Add(MarkdownParser.ParseMarkupOnly(rawLine, defaultFg));
                    continue;
                }
                if (codeSyntax != null && !string.IsNullOrEmpty(rawLine))
                {
                    var segments = new List<(string, int, int)>();
                    foreach (var (text, color) in codeSyntax.Tokenize(rawLine))
                        segments.Add((text, color, 0));
                    result.Add(segments);
                }
                else
                {
                    result.Add([(rawLine, defaultFg, 0)]);
                }
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

        // 如果第一个节点是纯段落且没有特殊格式，回退到简单渲染（仍需按宽度折行，避免整段单行右截不可见）
        if (nodes.Count == 1 && nodes[0] is MdParagraph p &&
            !p.Text.Contains('*') && !p.Text.Contains('`') && !p.Text.Contains('#') &&
            !p.Text.Contains('\xAB'))
        {
            foreach (var wrapped in WrapText(p.Text, maxWidth - 2))
                result.Add(new List<(string, int, int)> { (wrapped, FgForRole(role), 0) });
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
                    result.Add(new List<(string, int, int)> { (new string('━', Math.Min(maxWidth, 60)), TuiTheme.Current.MdRuleFg, 0) });
                    break;
                case MdBlockQuote bq:
                    RenderBlockQuote(bq, result, maxWidth, FgForRole(role));
                    break;
                case MdParagraph para:
                    RenderParagraph(para, result, maxWidth, FgForRole(role));
                    break;
                case MdMarkup mk:
                    RenderMarkup(mk, result, maxWidth);
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
        var color = h.Level <= 2 ? TuiTheme.Current.MdH1H2Fg : AnsiColors.White;  // H1-H2 亮白，H3+ 白
        var line = new List<(string, int, int)> { (prefix, TuiTheme.Current.MdHeadingFg, 0), (h.Text, color, 0) };
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
        result.Add(new List<(string, int, int)> { (topBorder, TuiTheme.Current.CodeBlockBorderFg, 0) });

        // 预览行数上限：超过保留头尾、中间折叠省略（防巨型代码块拖慢列表）
        const string ellipsisMarker = "===WC_CODE_ELLIPSIS==="; // 哨兵（不会出现在真实代码里）
        int cap = Config.Instance.MaxCodePreviewLines;
        var renderLines = codeLines;
        bool truncated = false;
        int skipped = 0;
        if (cap > 0 && codeLines.Length > cap)
        {
            int head = cap * 3 / 5;          // 头部 60%
            int tail = cap - head - 1;       // 尾部 + 1 行省略标记
            skipped = codeLines.Length - head - tail;
            renderLines = codeLines[..head].Concat(new[] { ellipsisMarker }).Concat(codeLines[^tail..]).ToArray();
            truncated = true;
        }

        int lineNum = 1;
        foreach (var rawLine in renderLines)
        {
            // 折叠省略行（哨兵标记，不误伤真实空行）
            if (truncated && rawLine == ellipsisMarker)
            {
                result.Add(new List<(string, int, int)> { ($" … 省略 {skipped} 行 …", TuiTheme.Current.CodeBlockBorderFg, 0) });
                continue;
            }

            var line = new List<(string, int, int)>();
            // 行号（尾部行号不连续，标注原行号）
            line.Add(($" {lineNum,3} ", TuiTheme.Current.CodeBlockBorderFg, 0));

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
        result.Add(new List<(string, int, int)> { (new string('─', Math.Min(maxWidth, 60)), TuiTheme.Current.CodeBlockBorderFg, 0) });
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
        result.Add(new List<(string, int, int)> { (top, TuiTheme.Current.MdTableBorderFg, 0) });
        result.Add(BuildRow(t.Headers, colWidths, isHeader: true));
        result.Add(new List<(string, int, int)> { (sep, TuiTheme.Current.MdTableBorderFg, 0) });

        // 渲染数据行
        foreach (var row in t.Rows)
        {
            var cells = new List<string>();
            for (int c = 0; c < colCount; c++)
                cells.Add(c < row.Count ? row[c] : "");
            result.Add(BuildRow(cells, colWidths, isHeader: false));
        }
        result.Add(new List<(string, int, int)> { (bot, TuiTheme.Current.MdTableBorderFg, 0) });
    }

    private static List<(string, int, int)> BuildRow(List<string> cells,
        int[] widths, bool isHeader)
    {
        var line = new List<(string, int, int)>();
        line.Add(("│", TuiTheme.Current.MdTableBorderFg, 0));
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

            line.Add(("│", TuiTheme.Current.MdTableBorderFg, 0));
        }
        return line;
    }

    private static void RenderListItem(MdListItem li,
        List<List<(string, int, int)>> result, int maxWidth, int defaultFg)
    {
        var indent = new string(' ', li.Level * 2);  // 每级缩进2格
        var line = new List<(string, int, int)>();

        if (li.Checked.HasValue)
        {
            // 任务清单：☑ 已完成（绿） / ☐ 未完成（弱化）
            var box = li.Checked.Value ? "☑" : "☐";
            var boxColor = li.Checked.Value ? AnsiColors.Green : 2;
            line.Add(($"{indent}  {box} ", boxColor, 0));
        }
        else
        {
            var bullet = li.Ordered ? $"{li.OrderNum}." : "•";
            line.Add(($"{indent}  {bullet} ", TuiTheme.Current.MdListBulletFg, 0));
        }

        foreach (var seg in MarkdownParser.ParseInline(li.Text, defaultFg))
            line.Add(seg);
        result.Add(line);
    }

    private static void RenderBlockQuote(MdBlockQuote bq,
        List<List<(string, int, int)>> result, int maxWidth, int defaultFg)
    {
        foreach (var rawLine in bq.Text.Split('\n'))
        {
            var line = new List<(string, int, int)>();
            line.Add(("│ ", TuiTheme.Current.MdRuleFg, 0)); // 左侧竖线（弱化）
            foreach (var seg in MarkdownParser.ParseInline(rawLine, defaultFg))
                line.Add(seg);
            result.Add(line);
        }
    }

    private static void RenderParagraph(MdParagraph p,
        List<List<(string, int, int)>> result, int maxWidth, int defaultFg)
    {
        // 逐行处理（段落保留原始换行）：每行各自解析内联 + 折行，
        // 避免多行内容被当成整段折行导致行数塌缩（条目高度不足 → 长内容滚不动）
        foreach (var rawLine in p.Text.Split('\n'))
        {
            var segments = MarkdownParser.ParseInline(rawLine, defaultFg);
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
    }

    private static void RenderMarkup(MdMarkup mk,
        List<List<(string, int, int)>> result, int maxWidth)
    {
        // 逐行渲染块级 «tag»…«/» 内容，保留空行；每行以 mk.Style 为默认样式（内部仍可嵌套内联格式）
        foreach (var rawLine in mk.Text.Split('\n'))
            result.Add(MarkdownParser.ParseInline(rawLine, mk.Style, 0));
    }

    // ================================================================
    // 工具方法
    // ================================================================

    /// <summary>按视觉宽度折行。保留原始换行：`\n` 是行分隔符（先按行拆，再各自折行），
    /// 否则多行消息会被当作一个长段落按宽度折，行数被压缩、条目高度不足 → 长内容显示不全/滚不动。</summary>
    private static List<string> WrapText(string text, int maxVw)
    {
        var lines = new List<string>();
        if (string.IsNullOrEmpty(text)) { lines.Add(""); return lines; }
        foreach (var rawLine in text.Split('\n'))
        {
            int start = 0;
            while (start < rawLine.Length)
            {
                var slice = rawLine[start..];
                int vw = 0, chars = 0;
                foreach (var rune in slice.EnumerateRunes())
                {
                    var w = AnsiHelper.RuneWidth(rune);
                    if (vw + w > maxVw) break;
                    vw += w; chars += rune.Utf16SequenceLength;
                }
                if (chars == 0)
                    // 首个字符超宽：完整取一个字符，避免切半代理对成 U+FFFD
                    chars = System.Text.Rune.GetRuneAt(rawLine, start).Utf16SequenceLength;
                lines.Add(rawLine[start..(start + chars)]);
                start += chars;
            }
            if (rawLine.Length == 0) lines.Add("");
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
        return AnsiHelper.DisplayWidth(clean);
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

    /// <summary>获取角色对应的默认前景色（对齐 TuiListItem，统一走 TuiTheme）</summary>
    private static int FgForRole(string role) => role switch
    {
        "user" => TuiTheme.Current.ChatUserFg,
        "assistant" => TuiTheme.Current.ChatAssistantFg,
        "system" => TuiTheme.Current.ChatSystemFg,
        "tool" => TuiTheme.Current.ChatToolFg,
        _ => TuiTheme.Current.ControlFg,   // agent / 未知角色
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

        var runes = text.EnumerateRunes().ToArray();
        for (int i = 0; i < runes.Length; i++)
        {
            float t = runes.Length > 1 ? (float)i / (runes.Length - 1) : 0;
            int color = AnsiTty.LerpRgb(startColor, endColor, t);
            segments.Add((runes[i].ToString(), color, 0)); // 完整 rune，代理对不拆半
        }
        return segments;
    }
}
