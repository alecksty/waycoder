using System.Text;
using WayCoder.UI.Tui;

namespace WayCoder.UI.TUI.Base;

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
    public EHAlign ChildHAlign { get; set; } = EHAlign.Left;

    /// <summary>子控件垂直对齐方式（在布局容器内）</summary>
    public EVAlign ChildVAlign { get; set; } = EVAlign.Top;

    /// <summary>内容整体对齐方式（当子控件总尺寸小于容器尺寸时）</summary>
    public EVAlign ContentVAlign { get; set; } = EVAlign.Top;

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
            EHAlign.Center => (Width - childWidth) / 2,
            EHAlign.Right => Width - childWidth,
            EHAlign.Stretch => 0, // Stretch will set child.Width = Width
            _ => 0, // Left
        };
    }

    /// <summary>根据垂直对齐计算子控件 Y 偏移（在分配的行高内）</summary>
    protected int AlignY(int childHeight, int rowHeight)
    {
        return ChildVAlign switch
        {
            EVAlign.Middle => (rowHeight - childHeight) / 2,
            EVAlign.Bottom => rowHeight - childHeight,
            EVAlign.Stretch => 0,
            _ => 0, // Top
        };
    }

    /// <summary>
    /// 渲染子控件。全刷新模式下所有子控件渲染；增量模式下仅渲染脏的叶子控件，
    /// 但始终遍历子视图容器（TuiView）以递归查找脏后代。
    /// </summary>
    protected override void OnRender(StringBuilder sb, int absX, int absY)
    {
        var parentDirty = IsDirty;
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
            if (child is TuiView v)
            {
                v.MarkDirtyTree();
            }
            else
            {
                child.MarkDirty();
            }
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
    public override bool OnMouse(InputEvent ev)
    {
        if (ev.Type != InputType.Mouse) return false;
        var hit = HitTest(ev.MouseX, ev.MouseY);
        if (hit != null && hit != this)
        {
            // 尝试让最深命中的控件处理
            if (hit.OnMouse(ev)) return true;
            // 冒泡：逐级向上查找父控件，直到遇到自己或有人消费事件
            var current = hit.Parent;
            while (current != null && current != this)
            {
                if (current.OnMouse(ev)) return true;
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

    /// <summary>
    /// 递归收集所有可聚焦控件。
    /// </summary>
    /// <param name="view">当前视图</param>
    /// <param name="list">可聚焦控件列表</param>
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