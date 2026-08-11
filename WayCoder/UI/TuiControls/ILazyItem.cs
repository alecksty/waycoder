namespace WayCoder.UI.TuiControls;

/// <summary>
/// 懒渲染列表项接口 —— 对标 Crush 的 Item 接口模式。
/// 实现此接口的控件可被 TuiListView 高效管理：
/// 列表只渲染视口内的项，跳过屏幕外的项。
///
/// TuiListView 已通过 Clip rect 裁剪实现视口跳过；
/// 此接口提供额外的高度预计算（无需完整渲染）和缓存标记。
/// </summary>
public interface ILazyItem
{
    /// <summary>在不完整渲染的情况下预计算高度（宽度已知时）</summary>
    int MeasureHeight(int width);

    /// <summary>渲染内容是否已缓存（TuiListView 可据此跳过已渲染项）</summary>
    bool IsRenderCached { get; }

    /// <summary>清除渲染缓存（内容变更时调用）</summary>
    void InvalidateCache();
}
