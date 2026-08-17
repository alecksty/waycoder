using System.Text;

namespace WayCoder.UI.Tui;

/// <summary>水平对齐方式</summary>
public enum HAlign { Left, Center, Right, Stretch }

/// <summary>垂直对齐方式</summary>
public enum VAlign { Top, Middle, Bottom, Stretch }

/// <summary>
/// 视图 —— 布局容器，管理子控件排列。
/// 视图本身也是控件，可嵌套。
/// </summary>
public abstract class TuiView : TuiControl
{
    /// <summary>子控件列表</summary>
    public readonly List<TuiControl> Children = [];

    /// <summary>当前视图的有效滚动偏移（非滚动容器返回 0）。用于脏标记时计算子控件真实屏幕坐标。</summary>
    public virtual int EffectiveScrollOffset => 0;

    /// <summary>子控件水平对齐方式（在布局容器内）</summary>
    public HAlign ChildHAlign { get; set; } = HAlign.Left;

    /// <summary>子控件垂直对齐方式（在布局容器内）</summary>
    public VAlign ChildVAlign { get; set; } = VAlign.Top;

    /// <summary>内容整体对齐方式（当子控件总尺寸小于容器尺寸时）</summary>
    public VAlign ContentVAlign { get; set; } = VAlign.Top;

    /// <summary>添加子控件，设置 Parent 引用并触发 OnCreate 生命周期</summary>
    public virtual void Add(TuiControl child)
    {
        child.Parent = this;
        Children.Add(child);
        child.OnCreate();
    }

    /// <summary>移除子控件，触发 OnDestroy 后清除 Parent 引用</summary>
    public void Remove(TuiControl child)
    {
        child.OnDestroy();
        child.Parent = null;
        Children.Remove(child);
    }

    /// <summary>清空所有子控件，递归触发各子控件的 OnDestroy</summary>
    public void Clear()
    {
        foreach (var c in Children) c.OnDestroy();
        foreach (var c in Children) c.Parent = null;
        Children.Clear();
    }

    /// <summary>递归初始化所有子控件的生命周期</summary>
    public override void OnCreate()
    {
        foreach (var child in Children) child.OnCreate();
        base.OnCreate();
    }

    /// <summary>递归清理所有子控件的生命周期</summary>
    public override void OnDestroy()
    {
        foreach (var child in Children) child.OnDestroy();
        base.OnDestroy();
    }

    /// <summary>重新计算子控件布局（子类实现排列算法）</summary>
    public abstract void Layout();

    /// <summary>根据水平对齐计算子控件 X 偏移</summary>
    protected int AlignX(int childWidth)
    {
        return ChildHAlign switch
        {
            HAlign.Center => (Width - childWidth) / 2,
            HAlign.Right  => Width - childWidth,
            HAlign.Stretch => 0, // Stretch will set child.Width = Width
            _ => 0, // Left
        };
    }

    /// <summary>根据垂直对齐计算子控件 Y 偏移（在分配的行高内）</summary>
    protected int AlignY(int childHeight, int rowHeight)
    {
        return ChildVAlign switch
        {
            VAlign.Middle  => (rowHeight - childHeight) / 2,
            VAlign.Bottom  => rowHeight - childHeight,
            VAlign.Stretch => 0,
            _ => 0, // Top
        };
    }

    /// <summary>
    /// 渲染子控件。全刷新模式下所有子控件渲染；增量模式下仅渲染脏的叶子控件，
    /// 但始终遍历子视图容器（TuiView）以递归查找脏后代。
    /// </summary>
    protected override void OnRender(StringBuilder sb, int absX, int absY)
    {
        bool parentDirty = IsDirty;
        foreach (var child in Children)
        {
            if (!child.Visible) continue;

            if (child is TuiView)
            {
                // 始终遍历视图容器：全刷新时需重绘所有后代，增量时需递归查找脏叶子
                child.Render(sb, absX, absY, ClipLeft, ClipTop, ClipRight, ClipBottom);
            }
            else if (child.IsDirty || parentDirty)
            {
                // 叶子控件：仅脏时渲染，渲染后清除脏标记
                child.Render(sb, absX, absY, ClipLeft, ClipTop, ClipRight, ClipBottom);
            }

            // 渲染后清除脏标记（无论视图还是叶子）
            child.IsDirty = false;
        }
        IsDirty = false;
    }

    /// <summary>强制刷新：递归标记视图及其所有子控件为脏。</summary>
    public override void Invalidate()
    {
        IsDirty = true;
        foreach (var child in Children)
            child.Invalidate();
    }

    /// <summary>
    /// 递归标记自身及所有后代为脏（仅触发重绘，不失效内容解析缓存）。
    /// 用于滚动等「内容未变、仅视口位移」的场景——与 Invalidate() 的区别在于
    /// 不调用 TuiMarkdown.Invalidate()（那会清空 Markdown 解析缓存并强制重解析），
    /// 只设置 IsDirty，让增量渲染把已缓存的片段在正确的新位置重绘。
    /// </summary>
    protected void MarkDirtyTree()
    {
        MarkDirty();
        foreach (var child in Children)
        {
            if (child is TuiView v) v.MarkDirtyTree();
            else child.MarkDirty();
        }
    }

    /// <summary>
    /// 按键路由：丢给子焦点子对象 → 都不处理返回 false。
    /// </summary>
    public override bool OnKey(ConsoleKeyInfo key)
    {
        if (!IsEnabled) return false;
        // 丢给子焦点子对象（直接聚焦的子控件，或内部有聚焦控件的子视图）
        foreach (var child in Children)
        {
            if ((child.Focused || (child is TuiView v && v.FindFocused() != null))
                && child.OnKey(key))
                return true;
        }
        return false;
    }

    /// <summary>递归命中测试：按子控件列表逆序（后添加在上层），返回最深命中控件</summary>
    public override TuiControl? HitTest(int absX, int absY)
    {
        if (!Visible || !IsEnabled) return null;
        // 先检查自身区域
        int myAbsX = Parent != null ? GetAbsoluteX() : X;
        int myAbsY = Parent != null ? GetAbsoluteY() : Y;
        if (absX < myAbsX || absX >= myAbsX + Width ||
            absY < myAbsY || absY >= myAbsY + Height)
            return null;
        // 逆序遍历子控件（后面的在上层）
        for (int i = Children.Count - 1; i >= 0; i--)
        {
            var hit = Children[i].HitTest(absX, absY);
            if (hit != null) return hit;
        }
        return this;
    }

    /// <summary>
    /// 鼠标事件处理：命中测试 → 路由到最深子控件。
    /// 若子控件不处理，沿控件树向上冒泡。
    /// </summary>
    public override bool HandleMouse(InputEvent ev)
    {
        if (ev.Type != InputType.Mouse) return false;
        var hit = HitTest(ev.MouseX, ev.MouseY);
        if (hit != null && hit != this)
        {
            // 尝试让最深命中的控件处理
            if (hit.HandleMouse(ev)) return true;
            // 冒泡：逐级向上查找父控件，直到遇到自己或有人消费事件
            var current = hit.Parent;
            while (current != null && current != this)
            {
                if (current.HandleMouse(ev)) return true;
                current = current.Parent;
            }
        }
        return false;
    }

    /// <summary>
    /// 递归通知尺寸变化。父容器先调用此方法设置新尺寸，
    /// 再触发布局重算和子控件递归通知。
    /// </summary>
    public override void OnResize(int newParentW, int newParentH)
    {
        // 子类可在此调整自身尺寸
        Layout();
        foreach (var child in Children)
            child.OnResize(Width, Height);
    }

    /// <summary>查找焦点控件</summary>
    public TuiControl? FindFocused()
    {
        foreach (var c in Children)
        {
            if (c.Focused) return c;
            if (c is TuiView v)
            {
                var found = v.FindFocused();
                if (found != null) return found;
            }
        }
        return null;
    }

    /// <summary>将焦点移到下一个可聚焦控件（Tab 顺序）</summary>
    public void FocusNext()
    {
        var focused = FindFocused();
        var list = GetAllFocusable();
        if (list.Count == 0) return;
        var idx = focused != null ? list.IndexOf(focused) : -1;
        var next = (idx + 1) % list.Count;

        // 增量渲染：仅标记丢失焦点和获得焦点的控件为脏
        if (focused != null) focused.MarkDirty();
        list[next].MarkDirty();

        foreach (var c in list) c.Focused = false;
        list[next].Focused = true;
    }

    /// <summary>将焦点移到上一个可聚焦控件（Shift+Tab 顺序）</summary>
    public void FocusPrev()
    {
        var focused = FindFocused();
        var list = GetAllFocusable();
        if (list.Count == 0) return;
        var idx = focused != null ? list.IndexOf(focused) : 0;
        var prev = (idx - 1 + list.Count) % list.Count;

        // 增量渲染：仅标记丢失焦点和获得焦点的控件为脏
        if (focused != null) focused.MarkDirty();
        list[prev].MarkDirty();

        foreach (var c in list) c.Focused = false;
        list[prev].Focused = true;
    }

    /// <summary>获取所有可聚焦控件的扁平列表</summary>
    public List<TuiControl> GetAllFocusable()
    {
        var list = new List<TuiControl>();
        CollectFocusable(this, list);
        return list;
    }

    private static void CollectFocusable(TuiView view, List<TuiControl> list)
    {
        foreach (var c in view.Children)
        {
            if (!c.IsEnabled || !c.CanFocus) continue;
            if (c is TuiView v)
                CollectFocusable(v, list);
            else
                list.Add(c);
        }
    }
}

/// <summary>
/// 垂直布局容器 —— 子控件从上到下排列。
/// 支持水平对齐（ChildHAlign）和内容垂直对齐（ContentVAlign）。
/// </summary>
public class TuiVBox : TuiView
{
    public int Spacing { get; set; }

    public override void Layout()
    {
        // ── 0. Flex 弹性分配（在测量之前设置 Flex>0 子控件的高度；浮动控件不参与流式布局）──
        int totalFlex = 0;
        int totalFixedH = 0;
        int flowCount = 0;
        foreach (var child in Children)
        {
            if (child.Floating) continue;
            flowCount++;
            if (child.Flex > 0)
                totalFlex += child.Flex;
            else
                totalFixedH += child.Height + child.Margin.Vertical;
        }
        if (totalFlex > 0)
        {
            int flexMarginH = 0;
            foreach (var child in Children)
                if (child.Flex > 0 && !child.Floating)
                    flexMarginH += child.Margin.Vertical;
            int remaining = Height - totalFixedH - flexMarginH - Math.Max(0, flowCount - 1) * Spacing;
            if (remaining > 0)
            {
                int allocated = 0;
                TuiBase? lastFlexChild = null;
                foreach (var child in Children)
                {
                    if (child.Flex > 0 && !child.Floating)
                    {
                        int h = Math.Max(1, remaining * child.Flex / totalFlex);
                        child.Height = h;
                        allocated += h;
                        lastFlexChild = child;
                    }
                }
                if (lastFlexChild != null)
                {
                    // 欠分配 → 最后一个吸收剩余；超分配（Max(1,…) 导致总和 > remaining）→ 从最后一个减回
                    if (remaining > allocated)
                        lastFlexChild.Height += remaining - allocated;
                    else if (allocated > remaining && lastFlexChild.Height > 1)
                        lastFlexChild.Height = Math.Max(1, lastFlexChild.Height - (allocated - remaining));
                }
            }
        }

        // 第一遍：计算总高度（含 child margin），递归布局嵌套视图（浮动子视图仍递归布局内部）
        var totalH = 0;
        foreach (var child in Children)
        {
            if (child is TuiView childView)
                childView.Layout();
            if (child.Floating) continue;
            if (ChildHAlign == HAlign.Stretch)
                child.Width = Width;
            totalH += child.Height + child.Margin.Vertical + Spacing;
        }
        if (totalH > 0) totalH -= Spacing;

        // 内容垂直对齐偏移
        var contentOffset = ContentVAlign switch
        {
            VAlign.Middle => (Height - totalH) / 2,
            VAlign.Bottom => Height - totalH,
            _ => 0
        };

        // 第二遍：设置位置（Margin.Top 偏移，Margin.Left 水平对齐；浮动控件跳过，保留手动 X/Y）
        var y = Math.Max(0, contentOffset);
        foreach (var child in Children)
        {
            if (child.Floating) continue;
            child.Y = y + child.Margin.Top;
            child.X = AlignX(child.Width) + child.Margin.Left;
            y += child.Height + child.Margin.Vertical + Spacing;
        }

        // 如果内容超出容器，更新 Height
        if (contentOffset == 0)
            Height = Math.Max(1, y - Spacing);
    }
}

/// <summary>
/// 水平布局容器 —— 子控件从左到右排列。
/// 支持垂直对齐（ChildVAlign）和内容水平对齐。
/// </summary>
public class TuiHBox : TuiView
{
    public int Spacing { get; set; }

    /// <summary>内容水平对齐方式（当子控件总宽小于容器宽时）</summary>
    public HAlign ContentHAlign { get; set; } = HAlign.Left;

    public override void Layout()
    {
        // ── 0. Flex 弹性分配（在测量之前设置 Flex>0 子控件的宽度；浮动控件不参与流式布局）──
        int totalFlex = 0;
        int totalFixedW = 0;
        int flowCount = 0;
        foreach (var child in Children)
        {
            if (child.Floating) continue;
            flowCount++;
            if (child.Flex > 0)
                totalFlex += child.Flex;
            else
                totalFixedW += child.Width + child.Margin.Horizontal;
        }
        if (totalFlex > 0)
        {
            int flexMarginW = 0;
            foreach (var child in Children)
                if (child.Flex > 0 && !child.Floating)
                    flexMarginW += child.Margin.Horizontal;
            int remaining = Width - totalFixedW - flexMarginW - Math.Max(0, flowCount - 1) * Spacing;
            if (remaining > 0)
            {
                int allocated = 0;
                // 最后一个 Flex 子控件拿剩余，避免取整损失
                TuiBase? lastFlexChild = null;
                foreach (var child in Children)
                {
                    if (child.Flex > 0 && !child.Floating)
                    {
                        int w = Math.Max(1, remaining * child.Flex / totalFlex);
                        child.Width = w;
                        allocated += w;
                        lastFlexChild = child;
                    }
                }
                // 修正取整误差：最后一个 Flex 子控件补齐
                if (lastFlexChild != null && remaining > allocated)
                    lastFlexChild.Width += remaining - allocated;
            }
        }

        // 第一遍：计算总宽度（含 child margin）和最大行高，递归布局嵌套视图（浮动子视图仍递归布局内部）
        int totalW = 0;
        int maxH = 1;
        foreach (var child in Children)
        {
            if (child is TuiView childView)
                childView.Layout();
            if (child.Floating) continue;
            if (ChildVAlign == VAlign.Stretch)
                child.Height = Height;
            totalW += child.Width + child.Margin.Horizontal + Spacing;
            maxH = Math.Max(maxH, child.Height + child.Margin.Vertical);
        }
        if (totalW > 0) totalW -= Spacing;

        // 内容水平对齐偏移
        int contentOffset = ContentHAlign switch
        {
            HAlign.Center => (Width - totalW) / 2,
            HAlign.Right  => Width - totalW,
            HAlign.Stretch => 0,
            _ => 0
        };

        // 第二遍：设置位置（Margin.Left/Right/Top 偏移；浮动控件跳过，保留手动 X/Y）
        int x = Math.Max(0, contentOffset);
        foreach (var child in Children)
        {
            if (child.Floating) continue;
            child.X = x + child.Margin.Left;
            child.Y = AlignY(child.Height + child.Margin.Vertical, maxH) + child.Margin.Top;
            x += child.Width + child.Margin.Horizontal + Spacing;
        }

        Height = maxH;
        if (contentOffset == 0)
            Width = Math.Max(1, x - Spacing);
    }
}

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
    public bool AutoScroll { get => IsAutoScrollToEnd; set => IsAutoScrollToEnd = value; }

    public override void Layout()
    {
        var prevContentHeight = ContentHeight;
        int y = 0;
        foreach (var child in Children)
        {
            if (ChildHAlign == HAlign.Stretch)
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

    protected override void OnRender(StringBuilder sb, int absX, int absY)
    {
        // 调整裁剪区域：Y 偏移减去滚动量
        var savedTop = ClipTop;
        var savedBottom = ClipBottom;
        ClipTop = absY;
        ClipBottom = absY + Height;

        foreach (var child in Children)
        {
            if (!child.Visible) continue;
            var childAbsY = absY + child.Y - ScrollOffset;
            var childAbsX = absX + child.X;

            // 完全不可见则跳过
            if (childAbsY + child.Height <= ClipTop || childAbsY >= ClipBottom)
                continue;

            child.Render(sb, absX, absY - ScrollOffset);
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
