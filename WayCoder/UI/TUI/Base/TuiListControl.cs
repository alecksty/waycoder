namespace WayCoder.UI.Tui;

/// <summary>
/// 行单位数据列表控件的公共基类 —— 选中/滚动状态 + 可见钳制 + 键盘导航骨架。
/// TuiList/TuiTableList/TuiTreeView 等「固定行高、按项索引」列表共享；
/// 渲染、回调时机、组头/多选等语义由子类实现（IsSelectable/OnSelectionMoved 虚钩子）。
/// 状态字段在此声明，子类删重复副本后继承。
/// </summary>
public abstract class TuiListControl : TuiControl
{
    /// <summary>当前选中项索引（含不可选项行号；对不可选项的定位由导航方法跳过）。</summary>
    public int SelectedIndex { get; set; }

    /// <summary>滚动偏移（首可见行索引）。</summary>
    public int ScrollOffset { get; set; }

    /// <summary>可选项总数（含不可选项如组头——由 <see cref="IsSelectable"/> 过滤）。</summary>
    protected abstract int ItemCount { get; }

    /// <summary>可见行数（表头/边框等控件覆写扣行）。</summary>
    protected virtual int VisibleRows => Height;

    /// <summary>该行是否可被选中（组头/分隔线覆写 false）。</summary>
    protected virtual bool IsSelectable(int index) => true;

    /// <summary>选中移动到新行的钩子（子类决定是否同步 OnSelectionChanged/高亮；默认仅标脏）。</summary>
    protected virtual void OnSelectionMoved(int index) => MarkDirty();

    /// <summary>调整 ScrollOffset 保证选中行落在可见数据区（共用公式见 TuiScrollMath.EnsureVisible）。</summary>
    public void EnsureSelectedVisible()
        => ScrollOffset = TuiScrollMath.EnsureVisible(SelectedIndex, ScrollOffset, ItemCount, VisibleRows);

    /// <summary>把选中朝 step 方向移动到目标行：钳制 + 跳过不可选项（到边界仍未可选则不动）。返回是否移动。</summary>
    protected bool MoveTo(int index, int step)
    {
        var count = ItemCount;
        if (count <= 0) return false;
        int target = Math.Clamp(index, 0, count - 1);
        while (!IsSelectable(target))
        {
            target += step;
            if (target < 0 || target >= count) return false;
        }
        if (target == SelectedIndex) return false;
        SelectedIndex = target;
        OnSelectionMoved(target);
        return true;
    }

    protected bool MoveUp() => MoveTo(SelectedIndex - 1, -1);
    protected bool MoveDown() => MoveTo(SelectedIndex + 1, 1);
    protected bool MoveHome() => MoveTo(0, 1);
    protected bool MoveEnd() => MoveTo(ItemCount - 1, -1);
    protected bool MovePage(int delta) => VisibleRows > 0 && MoveTo(SelectedIndex + delta * VisibleRows, delta);
}
