using System.Text;

namespace CoreCoderSharp.UI;

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

    /// <summary>是否自动滚到底部（内容增长时自动跟底）</summary>
    public bool IsAutoScrollToEnd { get; set; } = true;
    /// <summary>已弃用，请使用 IsAutoScrollToEnd</summary>
    [Obsolete("请使用 IsAutoScrollToEnd")]
    public bool AutoScroll { get => IsAutoScrollToEnd; set => IsAutoScrollToEnd = value; }

    /// <summary>项间距</summary>
    public int ItemSpacing { get; set; }

    /// <summary>选中项背景色</summary>
    public int SelBg { get; set; } = 46;

    /// <summary>选中项前景色</summary>
    public int SelFg { get; set; } = 30;

    /// <summary>选择变化回调</summary>
    public Action<int>? OnSelectionChanged { get; set; }

    /// <summary>项被点击/Enter 回调</summary>
    public Action<int>? OnItemActivated { get; set; }

    public TuiListView()
    {
        Height = 10;
        Width = 60;
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
    }

    /// <summary>移除指定索引的项</summary>
    public void RemoveItem(int index)
    {
        if (index < 0 || index >= Children.Count) return;
        Children[index].Parent = null;
        Children.RemoveAt(index);
        if (SelectedIndex >= Children.Count) SelectedIndex = Children.Count - 1;
        ReLayout();
    }

    /// <summary>清空所有项</summary>
    public void ClearItems()
    {
        foreach (var c in Children) c.Parent = null;
        Children.Clear();
        SelectedIndex = -1;
        ScrollOffset = 0;
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
        IsAutoScrollToEnd = false;
        ScrollOffset = Math.Max(0, ScrollOffset - lines);
    }

    public void ScrollDown(int lines = 3)
    {
        int maxScroll = Math.Max(0, ContentHeight - Height);
        if (ScrollOffset + lines >= maxScroll)
        {
            ScrollOffset = maxScroll;
            IsAutoScrollToEnd = true;
        }
        else ScrollOffset += lines;
    }

    public void ScrollToTop() { ScrollOffset = 0; IsAutoScrollToEnd = false; }

    public void ScrollToBottom()
    {
        ScrollOffset = Math.Max(0, ContentHeight - Height);
        IsAutoScrollToEnd = true;
    }

    // ── 渲染 ──

    protected override void OnRender(StringBuilder sb, int absX, int absY)
    {
        int visH = Height;
        if (visH <= 0) return;

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

        foreach (var child in Children)
        {
            if (!child.Visible) continue;

            int childScreenY = absY + child.Y - ScrollOffset;
            int childScreenBottom = childScreenY + child.Height;

            // 裁剪：完全不可见则跳过
            if (childScreenBottom <= absY || childScreenY >= absY + visH)
                continue;

            // 渲染子项
            child.Render(sb, absX, absY - ScrollOffset,
                ClipLeft, ClipTop, ClipRight, ClipBottom);
        }
    }

    // ── 输入 ──

    public override bool HandleKey(ConsoleKeyInfo key)
    {
        if (Children.Count == 0) return false;

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
                SelectedIndex = Math.Max(0, SelectedIndex - 5);
                OnSelectionChanged?.Invoke(SelectedIndex);
                return true;
            case ConsoleKey.PageDown:
                ScrollDown(Height);
                SelectedIndex = Math.Min(Children.Count - 1, SelectedIndex + 5);
                OnSelectionChanged?.Invoke(SelectedIndex);
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
