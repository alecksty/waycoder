using System.Text;
using WayCoder.UI.Tui;

namespace WayCoder.UI.TUI.Base;

/// <summary>
/// 滚动视图 —— 内容实际高度（ContentHeight）可大于可见区域高度，
/// 通过 ScrollOffset 控制可见窗口位置。
/// 子控件以完整内容高度布局，渲染时自动偏移。
/// </summary>
public class TuiScrollView : TuiView
{
    /// <summary>内容总高度（由 Layout 计算）</summary>
    public int ContentHeight { get; protected set; }

    /// <summary>当前滚动偏移（行数），0=顶部</summary>
    public int ScrollOffset { get; set; }

    /// <inheritdoc/>
    public override int EffectiveScrollOffset => ScrollOffset;

    /// <summary>是否自动滚到底部（内容增长时自动跟底）</summary>
    public bool IsAutoScrollToEnd { get; set; } = true;

    /// <summary>已弃用，请使用 IsAutoScrollToEnd</summary>
    [Obsolete("请使用 IsAutoScrollToEnd")]
    public bool AutoScroll
    {
        get => IsAutoScrollToEnd;
        set => IsAutoScrollToEnd = value;
    }

    public override void Layout()
    {
        var prevContentHeight = ContentHeight;
        int y = 0;
        foreach (var child in Children)
        {
            // 递归布局嵌套视图（与 TuiVBox/TuiHBox 一致），否则嵌套容器子控件高度未计算、布局错乱
            if (child is TuiView childView)
                childView.Layout();
            if (ChildHAlign == EHAlign.Stretch)
                child.Width = Width;
            child.X = AlignX(child.Width) + child.Margin.Left;
            child.Y = y + child.Margin.Top;
            y += child.Height + child.Margin.Vertical;
        }

        ContentHeight = y;

        // 内容增长时自动跟底
        if (IsAutoScrollToEnd && ContentHeight > prevContentHeight)
        {
            ScrollOffset = Math.Max(0, ContentHeight - Height);
        }
    }

    /// <summary>
    /// 渲染滚动视图内容。
    /// </summary>
    /// <param name="sb">渲染缓冲区</param>
    /// <param name="absX">绝对 X 坐标</param>
    /// <param name="absY">绝对 Y 坐标</param>
    protected override void OnRender(StringBuilder sb, int absX, int absY)
    {
        // 调整裁剪区域：Y 偏移减去滚动量。须与父容器裁剪区取交集，否则嵌套在更紧裁剪的
        // 父容器内时，子控件会越过父裁剪边界越界绘制。
        var savedTop = ClipTop;
        var savedBottom = ClipBottom;
        ClipTop = Math.Max(savedTop, absY);
        ClipBottom = Math.Min(savedBottom, absY + Height);

        foreach (var child in Children)
        {
            if (!child.Visible) continue;
            var childAbsY = absY + child.Y - ScrollOffset;
            var childAbsX = absX + child.X;

            // 完全不可见则跳过
            if (childAbsY + child.Height <= ClipTop || childAbsY >= ClipBottom)
                continue;

            child.Render(sb, absX, absY - ScrollOffset, ClipLeft, ClipTop, ClipRight, ClipBottom);
        }

        ClipTop = savedTop;
        ClipBottom = savedBottom;
    }

    /// <summary>向上滚动</summary>
    public void ScrollUp(int lines = 1)
    {
        int newOffset = Math.Max(0, ScrollOffset - lines);
        if (newOffset == ScrollOffset && !IsAutoScrollToEnd) return; // 已在顶部，无效（防闪屏）
        ScrollOffset = newOffset;
        IsAutoScrollToEnd = false;
        MarkDirtyTree();
    }

    /// <summary>向下滚动</summary>
    public void ScrollDown(int lines = 1)
    {
        var maxScroll = Math.Max(0, ContentHeight - Height);
        int newOffset;
        bool newAuto;
        if (ScrollOffset + lines >= maxScroll)
        {
            newOffset = maxScroll;
            newAuto = true;
        }
        else
        {
            newOffset = ScrollOffset + lines;
            newAuto = false;
        }

        if (newOffset == ScrollOffset && newAuto == IsAutoScrollToEnd) return; // 已在底部，无效（防闪屏）
        ScrollOffset = newOffset;
        IsAutoScrollToEnd = newAuto;
        MarkDirtyTree();
    }

    /// <summary>滚到顶部</summary>
    public void ScrollToTop()
    {
        if (ScrollOffset == 0 && !IsAutoScrollToEnd) return; // 已在顶部
        ScrollOffset = 0;
        IsAutoScrollToEnd = false;
        MarkDirtyTree();
    }

    /// <summary>滚到底部</summary>
    public void ScrollToBottom()
    {
        int newOffset = Math.Max(0, ContentHeight - Height);
        if (ScrollOffset == newOffset && IsAutoScrollToEnd) return; // 已在底部
        ScrollOffset = newOffset;
        IsAutoScrollToEnd = true;
        MarkDirtyTree();
    }

    /// <summary>添加子控件后自动跟底</summary>
    public override void Add(TuiControl child)
    {
        base.Add(child);
        if (IsAutoScrollToEnd)
            ScrollToBottom();
    }

    /// <summary>更新布局后自动跟底</summary>
    public void RefreshLayout()
    {
        Layout();
        if (IsAutoScrollToEnd)
            ScrollToBottom();
    }

    /// <summary>尺寸变化时重新 clamp 滚动位置</summary>
    public override void OnResize(int newParentW, int newParentH)
    {
        Layout();
        // 重新 clamp 滚动偏移到有效范围
        var maxScroll = Math.Max(0, ContentHeight - Height);
        ScrollOffset = Math.Clamp(ScrollOffset, 0, maxScroll);
        foreach (var child in Children)
            child.OnResize(Width, Height);
    }
}