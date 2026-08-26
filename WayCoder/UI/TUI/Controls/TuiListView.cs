using System.Text;
using WayCoder.UI.TUI.Base;
using Terminal = WayCoder.UI.Shared.Terminal;

namespace WayCoder.UI.Tui.Controls;

/// <summary>
/// 列表视图 —— 可滚动的视图项列表。
/// 每个项是任意 TuiControl（如 TuiMarkdown、TuiLabel）。
/// 支持选择、滚动、键鼠导航。
/// </summary>
public class TuiListView : TuiView
{
    /// <summary>当前选中项索引（-1 = 无选中）</summary>
    public int SelectedIndex { get; set; } = -1;

    /// <summary>垂直滚动偏移（像素行）</summary>
    public int ScrollOffset { get; set; }
    /// <inheritdoc/>
    public override int EffectiveScrollOffset => ScrollOffset;

    /// <summary>是否自动滚到底部（内容增长时自动跟底）</summary>
    public bool IsAutoScrollToEnd { get; set; } = true;
    /// <summary>已弃用，请使用 IsAutoScrollToEnd</summary>
    [Obsolete("请使用 IsAutoScrollToEnd")]
    public bool AutoScroll { get => IsAutoScrollToEnd; set => IsAutoScrollToEnd = value; }

    /// <summary>项间距</summary>
    public int ItemSpacing { get; set; }

    /// <summary>选中项背景色</summary>
    public int SelBg { get; set; }

    /// <summary>选中项前景色</summary>
    public int SelFg { get; set; }

    /// <summary>选择变化回调</summary>
    public Action<int>? OnSelectionChanged { get; set; }

    /// <summary>项被点击/Enter 回调</summary>
    public Action<int>? OnItemActivated { get; set; }

    public TuiListView()
    {
        Height = 10;
        Width = 60;
        SelBg = TuiTheme.Current.ListSelBg;
        SelFg = TuiTheme.Current.ListSelFg;
    }

    // ── 项管理 ──

    /// <summary>获取项数</summary>
    public int ItemCount => Children.Count;

    /// <summary>添加列表项</summary>
    public void AddItem(TuiControl item)
    {
        item.Parent = this;
        item.Width = Width;
        Children.Add(item);
        ReLayout();
        if (IsAutoScrollToEnd) ScrollToBottom();
        MarkDirtyTree(); // 增删内容必须标脏：擦除与重绘成对，覆盖 ScrollToBottom 边界 no-op 的场景
    }

    /// <summary>批量添加项</summary>
    public void AddItems(IEnumerable<TuiControl> items)
    {
        foreach (var item in items)
        {
            item.Parent = this;
            item.Width = Width;
            Children.Add(item);
        }
        ReLayout();
        if (IsAutoScrollToEnd) ScrollToBottom();
        MarkDirtyTree();
    }

    /// <summary>移除指定索引的项</summary>
    public void RemoveItem(int index)
    {
        if (index < 0 || index >= Children.Count) return;
        Children[index].Parent = null;
        Children.RemoveAt(index);
        if (SelectedIndex >= Children.Count) SelectedIndex = Children.Count - 1;
        ReLayout();
        MarkDirtyTree(); // 删除后剩余项上移，需擦除重绘
    }

    /// <summary>清空所有项</summary>
    public void ClearItems()
    {
        foreach (var c in Children) c.Parent = null;
        Children.Clear();
        SelectedIndex = -1;
        ScrollOffset = 0;
        MarkDirty(); // 清空后仅需擦除视口，无需标脏子项
    }

    /// <summary>获取指定项</summary>
    public TuiControl? GetItem(int index) =>
        index >= 0 && index < Children.Count ? Children[index] : null;

    // ── 布局 ──

    /// <summary>内容总高度（布局后更新）</summary>
    public int ContentHeight { get; private set; } = 1;

    /// <summary>重新计算所有项位置</summary>
    public void ReLayout()
    {
        int y = 0;
        foreach (var child in Children)
        {
            child.X = 0;
            child.Y = y;
            child.Width = Width;
            y += child.Height + ItemSpacing;
        }
        ContentHeight = Math.Max(1, y - ItemSpacing);
        // Height 由父容器设置作为视口高度，不在此覆盖
    }

    public override void Layout() => ReLayout();

    // ── 滚动 ──

    /// <summary>实际可见区域高度（由父容器设置的视口）</summary>
    public int ViewportHeight => Height;

    public void ScrollUp(int lines = 3)
    {
        int newOffset = Math.Max(0, ScrollOffset - lines);
        if (newOffset == ScrollOffset && !IsAutoScrollToEnd) return; // 已在顶部，翻页无效（防闪屏）
        ScrollOffset = newOffset;
        IsAutoScrollToEnd = false;
        MarkDirtyTree();
    }

    public void ScrollDown(int lines = 3)
    {
        int maxScroll = Math.Max(0, ContentHeight - Height);
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
        if (newOffset == ScrollOffset && newAuto == IsAutoScrollToEnd) return; // 已在底部，翻页无效（防闪屏）
        ScrollOffset = newOffset;
        IsAutoScrollToEnd = newAuto;
        MarkDirtyTree();
    }

    /// <summary>钳制滚动偏移到有效范围（内容被裁剪删除后调用，防偏移越界）。</summary>
    public void ClampScroll()
    {
        int maxScroll = Math.Max(0, ContentHeight - Height);
        if (ScrollOffset > maxScroll) { ScrollOffset = maxScroll; MarkDirtyTree(); }
    }

    public void ScrollToTop()
    {
        if (ScrollOffset == 0 && !IsAutoScrollToEnd) return; // 已在顶部
        ScrollOffset = 0;
        IsAutoScrollToEnd = false;
        MarkDirtyTree();
    }

    public void ScrollToBottom()
    {
        int newOffset = Math.Max(0, ContentHeight - Height);
        if (ScrollOffset == newOffset && IsAutoScrollToEnd) return; // 已在底部
        ScrollOffset = newOffset;
        IsAutoScrollToEnd = true;
        MarkDirtyTree();
    }

    // ── 鼠标 ──

    /// <summary>
    /// 鼠标滚轮滚动列表（3 行/格）；鼠标左键选中项。
    /// </summary>
    public override bool OnMouse(InputEvent ev)
    {
        if (ev.Type != InputType.Mouse) return false;

        // 检查鼠标是否在列表区域内
        int absX = GetAbsoluteX();
        int absY = GetAbsoluteY();
        if (ev.MouseX < absX || ev.MouseX >= absX + Width ||
            ev.MouseY < absY || ev.MouseY >= absY + Height)
            return false;

        // 滚轮滚动
        if (ev.MouseScrollUp) { ScrollUp(3); return true; }
        if (ev.MouseScrollDown) { ScrollDown(3); return true; }

        // 左键点击：定位选中项 + 聚焦（点击后方向键才路由到本列表）
        if (ev.MouseLeft)
        {
            Focused = true;
            int relY = ev.MouseY - absY + ScrollOffset;
            for (int i = 0; i < Children.Count; i++)
            {
                var child = Children[i];
                if (relY >= child.Y && relY < child.Y + child.Height)
                {
                    SelectedIndex = i;
                    OnItemActivated?.Invoke(i);
                    MarkDirty();
                    return true;
                }
            }
            return true; // 在区域内消费事件
        }

        return base.OnMouse(ev);
    }

    // ── 渲染 ──

    /// <summary>二分查找第一个可见项（scrollOffset 对应的 children 索引）</summary>
    private int FindFirstVisibleIndex()
    {
        if (Children.Count == 0) return 0;
        int lo = 0, hi = Children.Count - 1;
        while (lo < hi)
        {
            int mid = (lo + hi) / 2;
            var child = Children[mid];
            if (child.Y + child.Height <= ScrollOffset)
                lo = mid + 1;
            else
                hi = mid;
        }
        return lo;
    }

    protected override void OnRender(StringBuilder sb, int absX, int absY)
    {
        int visH = Height;
        if (visH <= 0) return;

        // 未标脏则不擦除也不重绘 —— 保留终端上已有的上一帧内容。
        // 擦除（视口填充）必须与重绘成对出现：只有内容真的变了（滚动/增删）才擦除，
        // 否则后台渲染（如状态栏动画每 30ms 一帧）会擦掉正文、又不重绘非脏叶子 → 黑屏闪烁。
        if (!IsDirty) return;

        // 填充整个视口背景，清除滚动残影（右边也刷到控件右缘）。
        // 子项渲染不补齐整行宽度，滚动后旧像素会残留在右侧/间隙。
        int fillBg = GetInheritedBg();
        int l = Math.Max(ClipLeft, absX);
        int r = Math.Min(ClipRight, absX + Width);
        int t = Math.Max(ClipTop, absY);
        int b = Math.Min(ClipBottom, absY + visH);
        if (r > l && b > t)
        {
            var rb = new Terminal.RenderBuffer();
            if (fillBg <= 0) rb.Reset(); // 透明背景：先复位到终端默认底色，空格才能清掉残留
            for (int row = t; row < b; row++)
                rb.Fill(row, l, r - l, fillBg);
            sb.Append(rb.ToString());
        }

        // 确保选中项可见
        if (SelectedIndex >= 0 && SelectedIndex < Children.Count)
        {
            var sel = Children[SelectedIndex];
            if (sel.Y < ScrollOffset)
                ScrollOffset = sel.Y;
            else if (sel.Y + sel.Height > ScrollOffset + visH)
                ScrollOffset = sel.Y + sel.Height - visH;
            ScrollOffset = Math.Max(0, ScrollOffset);
        }

        // 二分查找起始项，避免遍历所有子项
        int startIdx = FindFirstVisibleIndex();
        int screenBottom = absY + visH;

        for (int i = startIdx; i < Children.Count; i++)
        {
            var child = Children[i];
            if (!child.Visible) continue;

            int childScreenY = absY + child.Y - ScrollOffset;
            if (childScreenY >= screenBottom) break; // 后续项更远，直接停止

            int childScreenBottom = childScreenY + child.Height;

            // 裁剪：完全不可见则跳过
            if (childScreenBottom <= absY) continue;

            // 渲染子项
            child.Render(sb, absX, absY - ScrollOffset,
                ClipLeft, ClipTop, ClipRight, ClipBottom);
        }
    }

    // ── 输入 ──

    public override bool OnKey(ConsoleKeyInfo key)
    {
        if (!IsEnabled || Children.Count == 0) return false;

        switch (key.Key)
        {
            case ConsoleKey.UpArrow:
                SelectPrev();
                return true;
            case ConsoleKey.DownArrow:
                SelectNext();
                return true;
            case ConsoleKey.Home:
                SelectItem(0);
                return true;
            case ConsoleKey.End:
                SelectItem(Children.Count - 1);
                return true;
            case ConsoleKey.PageUp:
                ScrollUp(Height);
                // 走 SelectItem：同步 Focused 高亮 + MarkDirtyTree，勿直接改 SelectedIndex（会与反白高亮失步）
                SelectItem(Math.Max(0, SelectedIndex - Math.Max(1, Height)));
                return true;
            case ConsoleKey.PageDown:
                ScrollDown(Height);
                SelectItem(Math.Min(Children.Count - 1, SelectedIndex + Math.Max(1, Height)));
                return true;
            case ConsoleKey.Enter:
                if (SelectedIndex >= 0)
                    OnItemActivated?.Invoke(SelectedIndex);
                return true;
        }
        return false;
    }

    // ── 选择 ──

    public void SelectItem(int index)
    {
        if (index < 0 || index >= Children.Count) return;
        // 取消旧选择
        if (SelectedIndex >= 0 && SelectedIndex < Children.Count)
            Children[SelectedIndex].Focused = false;
        SelectedIndex = index;
        Children[index].Focused = true;
        MarkDirtyTree(); // 选中态变化（反白）+ 滚动到可见需重绘
        OnSelectionChanged?.Invoke(index);
    }

    public void SelectNext()
    {
        if (SelectedIndex < Children.Count - 1)
            SelectItem(SelectedIndex + 1);
        else if (Children.Count > 0)
            SelectItem(0); // 循环
    }

    public void SelectPrev()
    {
        if (SelectedIndex > 0)
            SelectItem(SelectedIndex - 1);
        else if (Children.Count > 0)
            SelectItem(Children.Count - 1); // 循环
    }
}
