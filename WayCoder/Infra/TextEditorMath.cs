using System.Text;

namespace WayCoder.Infra;

/// <summary>
/// 编辑器渲染/光标共用的**纯计算** —— 没有 MAUI 类型、没有 IO，可直接被桌面自测覆盖。
///
/// 字宽由调用方以 <c>Func&lt;Rune,int&gt;</c> 注入（桌面传 <c>AnsiString.CharWidth</c>，
/// MAUI 传同一个），这样「一列」的定义在整个项目里只有一份实现，不会出现
/// 「自绘按 A 算、点击定位按 B 算」的经典错位。
/// </summary>
public static class TextEditorMath
{
    /// <summary>把制表符展开成空格（对齐下一个 tab stop）。没有 tab 时原样返回、不分配。</summary>
    public static string ExpandTabs(string line, int tabColumns = 4)
    {
        if (string.IsNullOrEmpty(line) || line.IndexOf('\t') < 0) return line;
        if (tabColumns <= 0) return line.Replace("\t", "");

        var sb = new StringBuilder(line.Length + 8);
        int col = 0;
        foreach (var rune in line.EnumerateRunes())
        {
            if (rune.Value == '\t')
            {
                int next = (col / tabColumns + 1) * tabColumns;
                sb.Append(' ', next - col);
                col = next;
            }
            else
            {
                sb.Append(rune.ToString());
                // tab stop 按**字符**列推进（与 VS Code 等主流编辑器一致），不是按显示宽度
                col++;
            }
        }
        return sb.ToString();
    }

    /// <summary>视觉列（显示列，CJK 占 2）→ 源字符索引（UTF-16 码元位置，可直接做 string 下标）。</summary>
    public static int VisualColToSourceIndex(string line, int visualCol,
        Func<Rune, int> widthOf, int tabColumns = 4)
    {
        if (string.IsNullOrEmpty(line) || visualCol <= 0) return 0;

        int col = 0, idx = 0, charCol = 0;
        foreach (var rune in line.EnumerateRunes())
        {
            if (col >= visualCol) return idx;
            int advance;
            if (rune.Value == '\t')
            {
                int next = tabColumns > 0 ? (charCol / tabColumns + 1) * tabColumns : charCol + 1;
                advance = Math.Max(1, next - charCol);
            }
            else
            {
                advance = Math.Max(1, widthOf(rune));
            }
            // 落在字符/tab 的中间 → 归到它的起点（否则点「中」的右半格会跳到下一个字符）
            if (col + advance > visualCol) return idx;
            col += advance;
            charCol++;
            idx += rune.Utf16SequenceLength;
        }
        return line.Length;
    }

    /// <summary>源字符索引 → 视觉列。</summary>
    public static int SourceIndexToVisualCol(string line, int sourceIndex,
        Func<Rune, int> widthOf, int tabColumns = 4)
    {
        if (string.IsNullOrEmpty(line) || sourceIndex <= 0) return 0;
        int limit = Math.Min(sourceIndex, line.Length);

        int col = 0, idx = 0, charCol = 0;
        foreach (var rune in line.EnumerateRunes())
        {
            if (idx >= limit) break;
            if (rune.Value == '\t')
            {
                int next = tabColumns > 0 ? (charCol / tabColumns + 1) * tabColumns : charCol + 1;
                col += Math.Max(1, next - charCol);
                charCol = next;
            }
            else
            {
                col += Math.Max(1, widthOf(rune));
                charCol++;
            }
            idx += rune.Utf16SequenceLength;
        }
        return col;
    }

    /// <summary>
    /// 超长行的**可见窗口**：只取落在 [startCol, startCol+maxCols) 里的那段，
    /// 避免把一条 100MB 的 minified 行整条交给文本绘制。
    /// 返回 (窗口文本, 窗口起点的字符索引)。
    /// </summary>
    public static (string Text, int SourceIndex) WindowByColumns(string line,
        int startCol, int maxCols, Func<Rune, int> widthOf, int tabColumns = 4)
    {
        if (string.IsNullOrEmpty(line) || maxCols <= 0) return ("", 0);

        int beginIdx = VisualColToSourceIndex(line, Math.Max(0, startCol), widthOf, tabColumns);
        if (beginIdx >= line.Length) return ("", beginIdx);

        int col = 0, idx = beginIdx, end = beginIdx;
        foreach (var rune in line[beginIdx..].EnumerateRunes())
        {
            int w = Math.Max(1, widthOf(rune));
            if (col + w > maxCols) break;
            col += w;
            idx += rune.Utf16SequenceLength;
            end = idx;
        }
        return (line[beginIdx..end], beginIdx);
    }

    /// <summary>
    /// 把某一行滚进视口，返回新的「首个可见行」。
    /// 与具体控件无关（视口高度以行数表达），所以能被 TUI/MAUI 共用与单测。
    /// </summary>
    public static long EnsureVisible(long firstLine, long targetLine, long visibleLines,
        long lineCount, bool center = false)
    {
        if (visibleLines <= 0 || lineCount <= 0) return 0;

        if (center)
            return ClampFirst(targetLine - visibleLines / 2, visibleLines, lineCount);

        if (targetLine < firstLine) return ClampFirst(targetLine, visibleLines, lineCount);
        if (targetLine >= firstLine + visibleLines)
            return ClampFirst(targetLine - visibleLines + 1, visibleLines, lineCount);
        return firstLine;
    }

    private static long ClampFirst(long first, long visibleLines, long lineCount)
    {
        long maxFirst = Math.Max(0, lineCount - visibleLines);
        return Math.Clamp(first, 0, maxFirst);
    }

    /// <summary>惯性的瞬时速度：<c>v(t) = v0 · f^t</c>（f = 每秒衰减系数）。</summary>
    public static double FlingVelocity(double v0, double elapsedSeconds, double frictionPerSecond)
        => frictionPerSecond is > 0 and < 1
            ? v0 * Math.Pow(frictionPerSecond, elapsedSeconds)
            : 0;

    /// <summary>惯性到停止的总位移：对 <c>v(t)</c> 积分，<c>s = v0 / ln(1/f)</c>。</summary>
    public static double FlingDistance(double v0, double frictionPerSecond)
        => frictionPerSecond is > 0 and < 1
            ? v0 / Math.Log(1.0 / frictionPerSecond)
            : 0;
}
