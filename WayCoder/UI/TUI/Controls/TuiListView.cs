using System.Text;
using WayCoder.UI.TUI.Base;
using Terminal = WayCoder.UI.Shared.Terminal;

namespace WayCoder.UI.Tui.Controls;

/// <summary>
/// 列表视图 —— 可滚动的视图项列表。
/// 每个项是任意 TuiControl（如 TuiMarkdown、TuiLabel）。
/// 支持选择、滚动、键鼠导航。
/// 滚动状态机（ScrollOffset / 四个滚动方法 / 跟底标志 / OnResize 钳制）在
/// <see cref="TuiScrollable"/>，本类只管「逐项布局 + 选中态 + 键鼠导航」。
/// </summary>
public class TuiListView : TuiScrollable
{
    /// <summary>当前选中项索引（-1 = 无选中）</summary>
    public int SelectedIndex { get; set; } = -1;

    /// <summary>一「格」滚轮 / 无参 ScrollUp·ScrollDown 的步长（列表视图 3 行）。</summary>
    public override int ScrollStepLines => 3;

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

    /// <summary>
    /// 标记列表及其全部后代为脏（只置 IsDirty，不唤醒渲染帧闸门）。
    /// 列表 OnRender 先整视口擦除背景再重绘脏叶子，故内容变化后须整棵子树标脏，否则被擦除的未变消息
    /// 不会重画而消失。供渲染帧内（如 <c>ChatScreen.FlushStreamingLayout</c>）调用：帧已在渲染中，
    /// 无需再唤醒 Manager，避免多排一帧空渲染。
    /// 递归遍历本身收在 <see cref="TuiView.MarkDirtyTreeQuiet"/>，与 <c>MarkDirtyTree</c> 同一份实现。
    /// </summary>
    public void MarkTreeDirty() => MarkDirtyTreeQuiet();

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

    // ── 鼠标 ──

    /// <summary>
    /// 鼠标滚轮滚动列表（3 行/格）；鼠标左键选中项。
    /// </summary>
    public override bool OnMouse(InputEvent ev)
    {
        // 命中判定（渲染缓存命中，含窗口偏移，防弹窗内点击错位）；relY 供行命中换算
        if (!MouseInBounds(ev, out _, out int relY)) return false;

        // 滚轮滚动
        if (ev.MouseScrollUp) { ScrollUp(3); return true; }
        if (ev.MouseScrollDown) { ScrollDown(3); return true; }

        // 左键点击：定位选中项 + 聚焦（点击后方向键才路由到本列表）
        if (ev.MouseLeft)
        {
            Focused = true;
            relY += ScrollOffset;
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

        int fillBg = GetInheritedBg();
        int l = Math.Max(ClipLeft, absX);
        int r = Math.Min(ClipRight, absX + Width);
        int t = Math.Max(ClipTop, absY);
        int b = Math.Min(ClipBottom, absY + visH);

        // 确保选中项可见（必须先于下面的分区间填充：填充范围依赖 ScrollOffset）
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

        // 内容级脏（如流式追加：只有最后一条正文在变、其后无项需要位移）走窄路径——
        // 只擦「脏项自己的行区间」+「最后一项底边以下的空档」，不擦整个视口。
        // 整视口擦除是给「滚动 / 条目增删 / 多项位移」的保险；按流式频率（每渲染帧一次）
        // 整片擦掉再画，就是聊天区「内容没变也一直闪」的根源。
        // 滚动偏移本帧若变化（如流式追加触发自动滚到底），可视条目会整体位移，
        // 未被标脏的条目不会重绘 → 必须退回全量擦除，否则留下错位残影。
        bool contentOnly = _contentOnlyDirty && ScrollOffset == _lastRenderedScroll;
        _contentOnlyDirty = false;
        _lastRenderedScroll = ScrollOffset;

        if (r > l && b > t)
        {
            var rb = new Terminal.RenderBuffer();
            if (fillBg <= 0) rb.Reset(); // 透明背景：先复位到终端默认底色，空格才能清掉残留

            if (!contentOnly)
            {
                // 全量：填充整个视口背景，清除滚动残影（右边也刷到控件右缘）。
                for (int row = t; row < b; row++)
                    rb.Fill(row, l, r - l, fillBg);
            }
            else
            {
                // 窄路径：只擦脏项覆盖的行（按新高度），其余行由各自的子项原地覆盖
                int coveredBottom = t;
                for (int i = startIdx; i < Children.Count; i++)
                {
                    var c = Children[i];
                    if (!c.Visible || !c.IsDirty) continue;
                    int cTop = absY + c.Y - ScrollOffset;
                    int cBottom = cTop + c.Height;
                    for (int row = Math.Max(t, cTop); row < Math.Min(b, cBottom); row++)
                        rb.Fill(row, l, r - l, fillBg);
                    coveredBottom = Math.Max(coveredBottom, Math.Min(b, cBottom));
                }
                // 末尾空档：该项变矮时腾出的行不属于任何子项，必须补擦，否则残留旧像素
                for (int row = coveredBottom; row < b; row++)
                    rb.Fill(row, l, r - l, fillBg);
            }
            sb.Append(rb.ToString());
        }

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

    /// <summary>
    /// 内容级脏：只有第 <paramref name="index"/> 项的**正文**变了（典型场景：流式追加最后一条），
    /// 其后没有子项需要跟着位移、也没有条目增删/滚动。
    ///
    /// 此时不必擦整个视口：只擦这一项自己的行区间即可，其余项原地不动。调用方须自行确认
    /// 「被改的是最后一项」——中间项变高会把后续项挤下去，那些项必须一起重绘，走 <see cref="MarkTreeDirty"/>。
    /// </summary>
    public void MarkItemContentDirty(int index)
    {
        if (index < 0 || index >= Children.Count) return;
        // 必须整棵子树标脏（与 MarkTreeDirty 同理）：本路径会先擦掉该项所在的行区间，
        // 而 TuiView 的 parentDirty 只向下传播一层 —— 只标容器的话，标题等叶子不会重画，
        // 擦掉的像素就补不回来（实测：流式消息的「● 智能体」标题行被擦成空白）。
        SetTreeDirty(Children[index]);
        _contentOnlyDirty = true;
        MarkDirty();
    }

    /// <summary>本帧是否为「内容级脏」（只擦脏项自己的行，不擦整个视口）。</summary>
    private bool _contentOnlyDirty;

    /// <summary>上一帧实际使用的滚动偏移：用于判断「本帧可视条目是否整体位移过」。</summary>
    private int _lastRenderedScroll = -1;

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
