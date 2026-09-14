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
        => ExpandTabsWithMap(line, tabColumns).Text;

    /// <summary>
    /// 展开 tab，**并给出「原串下标 → 展开后下标」的映射**（长度 = `line.Length + 1`，
    /// 末项 = 展开后的长度）。
    ///
    /// 两件事必须在**同一次遍历**里算出来，理由有两条、都是踩过的：
    ///
    /// ① **tab 的推进规则只能有一份实现**。画布画的是展开后的串，而光标/点击/选区要按**原串**
    ///    的下标定位；两处各写一套规则，就会漂移 —— 而且必然在「tab 前面有中文/emoji」时暴露。
    ///    这里曾经就分裂过：本函数按**字符数**推进 tab stop（CJK 也只算 1），
    ///    <see cref="MeasureColumns"/> 按**显示格**推进（CJK 算 2），于是同一行
    ///    「画出来的宽度」与「点击算出来的位置」差出一格，表现就是「点 tab 后面那段落错位置」。
    ///
    /// ② 现在统一按**显示格**推进（半角 1 格、全角 2 格，判据是与 <see cref="MeasureColumns"/>
    ///    同一个 <c>AnsiString.CharWidth</c>）：一个 tab 在视觉上永远补齐到下一个 4 的倍数格 ——
    ///    也就是「tab 相当于 4 个空格」这句话的字面意思（全角字符占 2 格，所以它前面那个 tab
    ///    补的空格数会相应少，落点仍在同一列上）。
    ///
    /// 映射的语义：`Map[i]` = 第 i 个 UTF-16 码元**在展开串里的起点**。代理对的两个 char 指向
    /// 同一位置（码元中间不是合法的光标位置，归到该字形之前）。调用方拿 `Map[charIndex]` 去切
    /// 展开串，就与画布画的那一串**完全同源**。
    /// </summary>
    public static (string Text, int[] Map) ExpandTabsWithMap(string line, int tabColumns = 4,
        Func<Rune, int>? widthOf = null)
    {
        int n = line?.Length ?? 0;
        var map = new int[n + 1];

        // 无 tab：不分配、映射就是恒等
        if (string.IsNullOrEmpty(line) || line.IndexOf('\t') < 0)
        {
            for (int i = 0; i <= n; i++) map[i] = i;
            return (line ?? "", map);
        }
        if (tabColumns <= 0)   // 与原行为一致：非正 tab 宽 = 把 tab 删掉
        {
            var stripped = line.Replace("\t", "");
            int w = 0;
            for (int i = 0; i < n; i++) { map[i] = w; if (line[i] != '\t') w++; }
            map[n] = w;
            return (stripped, map);
        }

        var sb = new StringBuilder(n + 8);
        int col = 0, idx = 0;
        foreach (var rune in line.EnumerateRunes())
        {
            map[idx] = sb.Length;
            if (rune.Value == '\t')
            {
                int next = (col / tabColumns + 1) * tabColumns;
                sb.Append(' ', next - col);
                col = next;
            }
            else
            {
                sb.Append(rune.ToString());
                col += Math.Max(0, WidthOf(rune, widthOf));   // 按**显示格**推进（全角 2 格）
            }

            idx += rune.Utf16SequenceLength;
            // 代理对的**后半个 char**：指向该字形之后，与 <see cref="MeasureColumns"/> 的
            // 「下标落在代理对中间时算整个字形」保持一致（见那边的遍历）。
            // 码元中间本来不是合法的光标位置，但两条路径必须给同一个答案 —— 否则同一个下标
            // 「点击算出来的位置」与「画出来的宽度」会差一个字形宽。
            if (rune.Utf16SequenceLength == 2 && idx <= n) map[idx - 1] = sb.Length;
        }
        map[n] = sb.Length;
        // 兜底：任何没填到的下标沿用前一个（理论上只有 n 需要，防将来改遍历时不填满）
        for (int i = 1; i <= n; i++) if (map[i] < map[i - 1]) map[i] = map[i - 1];
        return (sb.ToString(), map);
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
    // ══ 网格（列）模型 —— 定位的唯一真源 ═══════════════════════════════════
    //
    // 「字符位置 ↔ 横坐标」在整个项目里**只此一份**：GUI（Avalonia 自绘）与 MAUI（自绘
    // 画布）共用它，所以两端的光标定位、点击命中、横向滚动上限不可能各算各的。
    //
    // 立场是「**尺子只有一把，就是我们自己**」—— 位置由列号算出，**不看字体度量**。
    // MAUI 那边为此折腾了八轮：只要测量与渲染是两条路径就必然差一点（行宽被平台取整、
    // 全角标点被压缩、字体解析分两条路），把偏差从 24px 压到 1.5px 之后问题依然在，
    // 因为「两把尺子」这件事本身还在。改自绘的控件请一律走这里，别再自己量字宽。

    /// <summary>行内第 <paramref name="charIndex"/> 个 UTF-16 码元之前占多少**列**。</summary>
    public static int MeasureColumns(string? line, int charIndex, int tabColumns = 4,
        Func<Rune, int>? widthOf = null)
    {
        if (string.IsNullOrEmpty(line) || charIndex <= 0) return 0;
        int limit = Math.Min(charIndex, line.Length);
        int col = 0, idx = 0;
        foreach (var r in line.EnumerateRunes())
        {
            if (idx >= limit) break;
            col += r.Value == '\t'
                ? (col / tabColumns + 1) * tabColumns - col
                : WidthOf(r, widthOf);
            idx += r.Utf16SequenceLength;
        }
        return col;
    }

    /// <summary>列 → 横坐标（相对正文左端）。<paramref name="halfWidth"/> 是半角列宽（= 字号 ÷ 2）。</summary>
    public static float ColumnsToX(int columns, float halfWidth) => columns * halfWidth;

    /// <summary>
    /// 横坐标 → **连续列位置**（不做取整）。
    ///
    /// 刻意保留小数：取整会把「格子内部靠右的一点」推到下一个格子的边界上，
    /// 于是 `！`（11–13 列）右半边的点击被算成「下一个字符之前」—— 点哪儿都往后跳一格。
    /// **中点判定必须拿到未取整的位置才做得对**（见 <see cref="ColumnToCharIndex"/>）。
    /// </summary>
    public static float XToColumn(float x, float halfWidth)
        => x <= 0 ? 0 : x / Math.Max(0.5f, halfWidth);

    /// <summary>
    /// 连续列位置 → 行内 UTF-16 码元下标。
    ///
    /// **落在字符前半归它前面、后半归它后面** —— 与「点字定位光标」的直觉一致。
    /// 全角字符因此不会被劈开：整格 2 列，中点在第 1.5 列处，左半边一律归到它之前。
    /// </summary>
    public static int ColumnToCharIndex(string? line, float column, int tabColumns = 4,
        Func<Rune, int>? widthOf = null)
    {
        if (string.IsNullOrEmpty(line) || column <= 0) return 0;
        float col = 0;
        int idx = 0;
        foreach (var r in line.EnumerateRunes())
        {
            float w = r.Value == '\t'
                ? (col / tabColumns + 1) * tabColumns - col
                : WidthOf(r, widthOf);
            if (w <= 0) { idx += r.Utf16SequenceLength; continue; }   // 零宽字符不占格
            if (column < col + w)
                return column < col + w / 2f ? idx : idx + r.Utf16SequenceLength;
            col += w;
            idx += r.Utf16SequenceLength;
        }
        return idx;
    }

    /// <summary>宽字符判定 —— 默认走全仓唯一真源（CJK/emoji=2，半角=1，零宽/组合=0）。</summary>
    private static int WidthOf(Rune r, Func<Rune, int>? widthOf)
        => (widthOf ?? WayCoder.UI.Shared.Terminal.AnsiString.CharWidth)(r);
}
