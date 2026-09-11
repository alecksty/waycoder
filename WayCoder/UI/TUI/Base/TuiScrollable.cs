namespace WayCoder.UI.TUI.Base;

/// <summary>
/// 「可滚动视图」的状态机基类 —— **唯一实现**。
///
/// `TuiScrollView`（内容自适应高度的滚动视图）与 `TuiListView`（逐项滚动的列表视图）
/// 此前各持一份 `ScrollOffset` / `ContentHeight` / `IsAutoScrollToEnd` / `AutoScroll`(弃用别名)
/// 与 `ScrollUp` / `ScrollDown` / `ScrollToTop` / `ScrollToBottom`，**四段方法体逐字节相同**，
/// 连「已在顶部/底部则 no-op（防闪屏）」的判据与注释都一致。差异只剩一处：
/// 无参调用的默认步长（列表视图一格滚轮 = 3 行、滚动视图 = 1 行）→ `<see cref="ScrollStepLines"/>`。
///
/// **为什么值得收**：这几个方法不是纯 getter，而是「跟底状态 + 边界 no-op + 标脏」三件事
/// 交织的状态机 —— `ScrollDown` 到边界时要把 `IsAutoScrollToEnd` 置回 true（内容再增长时继续跟底），
/// 非边界时置 false（用户手动滚上去了就别再自动跟底），漏一处就会「滚一下就永久失去自动跟底」
/// 或「手动往上翻一页又被自动拽回底部」。两份各自演化时没有任何编译期提示。
///
/// **顺带修掉 `TuiListView` 缺 `OnResize` 复位**：截图/缩放终端后视口变高，旧的
/// `ScrollOffset` 可能已越过新的 `ContentHeight - Height`，偏移越界后列表会停在空白区。
/// 此前只有 `TuiScrollView` 覆写了 `OnResize` 做这件事，列表视图没有。
/// </summary>
public abstract class TuiScrollable : TuiView
{
    /// <summary>内容总高度（由子类布局时更新）。</summary>
    public int ContentHeight { get; protected set; } = 1;

    /// <summary>当前滚动偏移（行数），0 = 顶部。</summary>
    public int ScrollOffset { get; set; }

    /// <inheritdoc/>
    public override int EffectiveScrollOffset => ScrollOffset;

    /// <summary>是否自动滚到底部（内容增长时自动跟底）。</summary>
    public bool IsAutoScrollToEnd { get; set; } = true;

    /// <summary>已弃用，请使用 IsAutoScrollToEnd。</summary>
    [Obsolete("请使用 IsAutoScrollToEnd")]
    public bool AutoScroll
    {
        get => IsAutoScrollToEnd;
        set => IsAutoScrollToEnd = value;
    }

    /// <summary>无参 <see cref="ScrollUp()"/> / <see cref="ScrollDown()"/> 的步长。
    /// 列表视图一「格」滚轮滚 3 行（见 <c>TuiListView</c>），滚动视图滚 1 行。</summary>
    public virtual int ScrollStepLines => 1;

    /// <summary>最大可滚动行数（内容不超视口时为 0）。</summary>
    protected int MaxScroll => Math.Max(0, ContentHeight - Height);

    // ── 滚动 ──

    /// <summary>向上滚动一「格」。</summary>
    public void ScrollUp() => ScrollUp(ScrollStepLines);

    /// <summary>向下滚动一「格」。</summary>
    public void ScrollDown() => ScrollDown(ScrollStepLines);

    /// <summary>向上滚动 N 行。</summary>
    public void ScrollUp(int lines)
    {
        int newOffset = Math.Max(0, ScrollOffset - lines);
        if (newOffset == ScrollOffset && !IsAutoScrollToEnd) return; // 已在顶部，无效（防闪屏）
        ScrollOffset = newOffset;
        IsAutoScrollToEnd = false;
        MarkDirtyTree();
    }

    /// <summary>向下滚动 N 行；到达/越过底部则贴底并重新进入跟底。</summary>
    public void ScrollDown(int lines)
    {
        var maxScroll = MaxScroll;
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

    /// <summary>钳制滚动偏移到有效范围（内容被裁剪删除、视口变大后调用，防偏移越界停在空白区）。</summary>
    public void ClampScroll()
    {
        var maxScroll = MaxScroll;
        if (ScrollOffset > maxScroll) { ScrollOffset = maxScroll; MarkDirtyTree(); }
    }

    /// <summary>滚到顶部（并退出跟底）。</summary>
    public void ScrollToTop()
    {
        if (ScrollOffset == 0 && !IsAutoScrollToEnd) return; // 已在顶部
        ScrollOffset = 0;
        IsAutoScrollToEnd = false;
        MarkDirtyTree();
    }

    /// <summary>滚到底部（并进入跟底）。</summary>
    public void ScrollToBottom()
    {
        int newOffset = MaxScroll;
        if (ScrollOffset == newOffset && IsAutoScrollToEnd) return; // 已在底部
        ScrollOffset = newOffset;
        IsAutoScrollToEnd = true;
        MarkDirtyTree();
    }

    /// <summary>尺寸变化：重算布局 → 重新钳制滚动偏移 → 递归通知子控件。
    /// 基类 <see cref="TuiView.OnResize"/> 只做「布局 + 递归」，钳制是滚动视图独有的必要一步。</summary>
    public override void OnResize(int newParentW, int newParentH)
    {
        base.OnResize(newParentW, newParentH);
        ClampScroll();
    }
}
