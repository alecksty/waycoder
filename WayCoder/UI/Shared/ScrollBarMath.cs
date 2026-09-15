namespace WayCoder.UI.Shared;

/// <summary>
/// 滚动条几何 —— **纯计算，只有一份实现**。
///
/// 之所以抽出来：① 这部分是"看着对不对"最容易骗人的一类代码（差一点点在视觉上完全看不出来，
/// 但拖到两端就会差出一截、或者滑块能拖出轨道外），必须能断言；② 手机端已经有编辑器那套
/// 自绘滚动，再来一份手写几何就是本仓库排第一的坑「同一规则两处实现」。
///
/// 坐标一律是**相对轨道**的像素：`0` = 轨道顶端，`trackSize` = 轨道底端。
/// </summary>
public static class ScrollBarMath
{
    /// <summary>滑块最小长度（像素）。太短会细成一条线，既看不见也点不住。</summary>
    public const double MinThumb = 24;

    /// <summary>滑块的容差（像素）：内容只比视口高不到 1px 时不该冒出滚动条。</summary>
    public const double Epsilon = 1.0;

    /// <summary>内容是否超出视口（决定滚动条显不显示）。</summary>
    public static bool ShouldShow(double contentSize, double viewportSize)
        => contentSize > viewportSize + Epsilon;

    /// <summary>
    /// 滑块在轨道里的位置与长度。
    /// 内容没超出视口时返回整条轨道（调用方据此把滚动条整个藏起来，
    /// 这里给一个"占满"的值是为了让调用方**不必**再判一次 —— 边界守卫收在函数里，
    /// 本仓库踩过"四处调用点各判一次、漏一处就崩"的坑）。
    /// </summary>
    public static (double Top, double Height) Thumb(
        double contentSize, double viewportSize, double scrollOffset, double trackSize)
    {
        if (trackSize <= 0) return (0, 0);
        if (!ShouldShow(contentSize, viewportSize)) return (0, trackSize);

        // 长度按"视口/内容"的比例，并夹在 [MinThumb, 轨道长] 之间。
        // 夹下界用 MinThumb、夹上界用轨道长 —— 内容刚好超一点点时比例接近 1，
        // 不夹上界滑块会比轨道还长、直接画到界面外。
        var h = trackSize * (viewportSize / contentSize);
        if (h < MinThumb) h = MinThumb;
        if (h > trackSize) h = trackSize;

        var maxScroll = contentSize - viewportSize;
        var travel = trackSize - h;                  // 滑块能走的距离
        var t = maxScroll <= 0 ? 0 : (scrollOffset / maxScroll) * travel;
        if (t < 0) t = 0;
        if (t > travel) t = travel;
        return (t, h);
    }

    /// <summary>
    /// <see cref="Thumb"/> 的逆运算：把滑块顶端位置换算回滚动偏移。
    /// 拖动滑块要用它 —— 少了这一步就只能"显示"滚动条、拖不动。
    /// </summary>
    public static double OffsetForThumbTop(
        double thumbTop, double contentSize, double viewportSize, double trackSize)
    {
        if (!ShouldShow(contentSize, viewportSize)) return 0;

        var h = Thumb(contentSize, viewportSize, 0, trackSize).Height;
        var travel = trackSize - h;
        if (travel <= 0) return 0;

        var maxScroll = contentSize - viewportSize;
        var t = thumbTop;
        if (t < 0) t = 0;
        if (t > travel) t = travel;
        return t / travel * maxScroll;
    }

    /// <summary>滚动偏移是否已经贴着底（"跟底"判据 —— 贴底时才自动滚，否则用户正在往回看）。</summary>
    public static bool IsAtBottom(double contentSize, double viewportSize, double scrollOffset)
        => contentSize - viewportSize - scrollOffset <= Epsilon * 2;
}
