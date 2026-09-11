using Terminal = WayCoder.UI.Shared.Terminal;

namespace WayCoder.UI.Tui;

/// <summary>
/// Tui 列表/滚动控件共用的滚动函数 —— 只收口各控件重复的
/// 「可见钳制 / 滚动条滑块几何 / 页滚动 / 滚动条落笔」这一套。供 TuiList/TuiTableList/
/// TuiMenu/TuiTreeView/DiffPreview 复用（各控件保留自己的渲染、擦除、防闪屏与语义差异）。
/// </summary>
public static class TuiScrollMath
{
    /// <summary>滚动条滑块字符。四处内联实现各写一遍字面量，改样式时必然漏改一处。</summary>
    public const string ThumbChar = "█";

    /// <summary>滚动条轨道字符。</summary>
    public const string TrackChar = "│";

    /// <summary>
    /// 把一个竖直滚动条画进 <paramref name="rb"/> —— **唯一实现**。
    ///
    /// `TuiList` / `TuiMenu` / `TuiTableList` / `DiffPreview` 此前各写一遍
    /// 「Bar 取几何 → 逐行判滑块 → 写目标列」，四份连字符字面量都逐字相同，只有两处不同：
    /// 目标列、颜色来源（TuiList 用 `fg: 2`、TuiMenu 用 `AnsiTty.StyleDim` —— 同为 SGR 2 淡化；
    /// TuiTableList 用主题 `SeekBarThumbFg`/`SeparatorFg`；DiffPreview 也走淡化）。
    /// 这两种差异正是调用方的正当语义，故留作参数；几何仍由 <see cref="Bar"/> 提供，
    /// 这里只负责落笔。
    ///
    /// **不需要滚动条时（`total &lt;= visible`）不落任何笔**：四处调用点本就各自判着同一个条件，
    /// 收在这里是为了不再依赖「四个调用方都记得判」——`Bar` 在 `total &lt;= visible` 时滑块会长过
    /// 视口，`(long)(vis - thumb)` 变负会让 `Math.Clamp` 的 min &gt; max 抛 `ArgumentException`，
    /// 少判一处就是一次崩溃。
    /// </summary>
    public static void Paint(Terminal.RenderBuffer rb, int topRow, int leftCol, int height,
        int total, int visible, int offset, int thumbFg, int trackFg)
    {
        if (height <= 0 || total <= visible) return;
        var (thumb, pos) = Bar(total, visible, offset);
        for (int i = 0; i < height; i++)
        {
            var isThumb = i >= pos && i < pos + thumb;
            rb.Write(topRow + i, leftCol, isThumb ? ThumbChar : TrackChar,
                fg: isThumb ? thumbFg : trackFg);
        }
    }

    /// <summary>两段式可见钳制：选中行不在视口则滚动到让其可见，返回新 ScrollOffset。
    /// idx 由调用方传「要可见的定位行」（选中行 / 当前 hunk 首行等）。</summary>
    public static int EnsureVisible(int idx, int offset, int count, int viewport)
    {
        if (viewport <= 0 || count <= 0) return 0;
        var i = Math.Clamp(idx, 0, count - 1);
        if (i < offset) offset = i;
        else if (i >= offset + viewport) offset = i - viewport + 1;
        return Math.Clamp(offset, 0, Math.Max(0, count - viewport));
    }

    /// <summary>滚动条滑块几何（long 防溢出，对齐 TuiScrollbar 组件公式）。返回 (thumbHeight, thumbPos)。</summary>
    public static (int Thumb, int Pos) Bar(int total, int vis, int offset)
    {
        if (total <= 0 || vis <= 0) return (1, 0);
        var thumb = (int)Math.Max(1L, (long)vis * vis / total);
        var maxScroll = Math.Max(0, total - vis);
        var pos = maxScroll <= 0 ? 0 : (int)Math.Clamp((long)vis * offset / maxScroll, 0L, (long)(vis - thumb));
        return (thumb, pos);
    }

    /// <summary>页滚动：offset 按 delta 页移动后 clamp 到有效区间（delta 负数=上翻页）。</summary>
    public static int PageMove(int offset, int viewport, int count, int delta)
        => Math.Clamp(offset + delta * viewport, 0, Math.Max(0, count - viewport));

    /// <summary>滚轮滚动 N 行后的新 offset（delta=±N，正下负上），clamp 到有效区间。</summary>
    public static int Wheel(int offset, int count, int viewport, int delta)
        => Math.Clamp(offset + delta, 0, Math.Max(0, count - viewport));

    /// <summary>把滚动值钳制到 [0, count-viewport] 有效区间（纯值钳制，供各控件就地 clamp 复用）。
    public static int Clamp(int value, int count, int viewport)
        => Math.Clamp(value, 0, Math.Max(0, count - viewport));
}
