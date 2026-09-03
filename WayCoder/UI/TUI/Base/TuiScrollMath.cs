namespace WayCoder.UI.Tui;

/// <summary>
/// Tui 列表/滚动控件共用的滚动纯函数 —— 无状态、无 UI 依赖，只收口各控件重复的
/// 「可见钳制 / 滚动条滑块几何 / 页滚动」数学。供 TuiList/TuiTableList/TuiMenu/
/// TuiTreeView/DiffPreview 复用（各控件保留自己的渲染、擦除、防闪屏与语义差异）。
/// </summary>
public static class TuiScrollMath
{
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
}
