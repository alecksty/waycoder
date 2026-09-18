namespace WayCoder.Maui.Controls;

/// <summary>
/// 辅助输入条的**拖动面 / 命中面**：一个不参与测量、只负责接触摸的 <see cref="GraphicsView"/>。
///
/// <para>
/// **存在的理由是「撑满但不撑大」**：它必须铺满整条（否则按钮之外的区域收不到触摸，
/// 那条子就只有 ⣿ 能拖），可普通 <c>GraphicsView</c> 配上 <c>Fill</c> 会把自己的
/// **期望尺寸**报成「可用空间的全部」—— 它所在的 Grid 于是被撑满，
/// 整条 Border 变成近全屏（真机实测踩到过，用户报「辅助输入条几乎全屏了」）。
/// </para>
///
/// <para>
/// 这里把期望尺寸报成 0：Grid 的大小仍然**只由那排按钮决定**，而 <c>Fill</c> 的对齐
/// 照样把它**排布**成整格大小 ⇒ 尺寸对了、触摸面也齐了。
/// 注意「测量」与「排布」是两件事：测量只影响父容器长多大，排布才决定自己占多大。
/// </para>
///
/// <para>
/// 顺带把 <see cref="GraphicsView.Drawable"/> 设成空实现 —— 它一个像素都不画，
/// 但不能留 <c>null</c>。
/// </para>
/// </summary>
public sealed class AssistDragSurface : GraphicsView
{
    public AssistDragSurface()
    {
        Drawable = Noop;
        BackgroundColor = Colors.Transparent;
    }

    /// <summary>期望尺寸恒为 0 —— 不参与父容器的尺寸计算（理由见类注释）。</summary>
    protected override Size MeasureOverride(double widthConstraint, double heightConstraint) => Size.Zero;

    private static readonly IDrawable Noop = new NoopDrawable();

    private sealed class NoopDrawable : IDrawable
    {
        public void Draw(ICanvas canvas, RectF dirtyRect) { }
    }
}
