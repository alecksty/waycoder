namespace WayCoder.UI.Shared.Terminal;

/// <summary>
/// **电脑屏窗口的"显示变换"** —— 程序视角与屏幕上那块画布之间的换算，只此一份。
///
/// <para>
/// 语义是用户定的：「**默认等比缩放看全貌，双指放大后 1:1 平移细看**」。
/// 关键在于它**只改显示、不动场景坐标** —— 程序眼里的分辨率永远是它自己声明的那个
/// （`SCREEN_W/H`、图元坐标、鼠标坐标全都是那套），缩放平移纯粹是"用户拿放大镜看"。
/// </para>
///
/// <para>
/// ⚠ **为什么单独抽一个纯类**：这套换算有两个消费方 ——
/// 画布落笔（`canvas.Translate/Scale`）与**触摸反算**（视图坐标 → 场景坐标）。
/// 两边各算一遍就是本仓头号坑（"同一规则两处实现"），而且它们一旦漂移，
/// 症状是**点哪儿偏哪儿**，还偏得不单调（缩放比越大偏得越多）—— 极难从现象反推。
/// 抽出来之后它是纯 double 运算，**桌面自测能逐条钉**（`ToView(ToScene(p)) ≈ p` 之类）。
/// </para>
///
/// <para>
/// 内部只存三个量：<see cref="Scale"/>（一个场景单位 = 屏幕上几个点）与
/// <see cref="OffsetX"/>/<see cref="OffsetY"/>（场景原点落在视口的哪里）。
/// 用户调的"缩放比"是 <see cref="Zoom"/>，`Scale = 基准缩放 × Zoom`。
/// </para>
/// </summary>
public sealed class VmlViewTransform
{
    /// <summary>缩放下限：**1 = 正好看全**（再小就没有意义了，画面会更小还留更多黑边）。</summary>
    public const double MinZoom = 1.0;

    /// <summary>缩放上限。8 倍对 640×480 的"电脑屏"够看清一个字符了，再大只是浪费。</summary>
    public const double MaxZoom = 8.0;

    /// <summary>基准等比缩放（`zoom = 1` 时每单位场景占多少视口点）。</summary>
    private double _baseScale = 1;

    private double _viewW, _viewH;
    private double _sceneW = 1, _sceneH = 1;

    /// <summary>当前缩放比（1 = 看全貌）。</summary>
    public double Zoom { get; private set; } = 1;

    /// <summary>一个场景单位 = 屏幕上几个点。画布落笔用它当缩放系数。</summary>
    public double Scale { get; private set; } = 1;

    /// <summary>场景原点 (0,0) 落在视口的哪里。画布落笔用它当平移量。</summary>
    public double OffsetX { get; private set; }
    public double OffsetY { get; private set; }

    /// <summary>
    /// 视口或场景尺寸变了：**保持当前缩放比**重算基准（居中）。
    ///
    /// ⚠ 是"重算基准"而不是"重置"：程序转屏/收起键盘换了视口时，
    /// 用户刚放大到看细节的那一档不该被扔掉。
    /// </summary>
    public void Fit(double viewW, double viewH, double sceneW, double sceneH)
    {
        if (viewW <= 0 || viewH <= 0 || sceneW <= 0 || sceneH <= 0) return;

        _viewW = viewW; _viewH = viewH;
        _sceneW = sceneW; _sceneH = sceneH;
        // 等比：取小的那个方向，保证**两个方向都装得下**（只缩一个维度必然把另一个切掉）
        _baseScale = Math.Min(viewW / sceneW, viewH / sceneH);

        ApplyZoomAnchored(Zoom, viewW / 2, viewH / 2);
    }

    /// <summary>回到"看全貌"（双指双击）。</summary>
    public void Reset() => ApplyZoomAnchored(1, _viewW / 2, _viewH / 2);

    /// <summary>
    /// 以视口里的某一点为锚**缩放**：那一点底下的场景内容**保持不动**。
    /// （用户双指捏合时，两个手指中间那块内容不会跑 —— 那是手感的本体。）
    /// </summary>
    public void ZoomAt(double viewX, double viewY, double factor) => ApplyZoomAnchored(Zoom * factor, viewX, viewY);

    /// <summary>平移（单指拖动，仅在已经放大时才有意义）。</summary>
    public void Pan(double dx, double dy)
    {
        OffsetX += dx;
        OffsetY += dy;
        Clamp();
    }

    /// <summary>
    /// 视图坐标 → **场景坐标**。落在画面外返回 null（触摸落在黑边上时不该算数）。
    /// </summary>
    public (double X, double Y)? ToScene(double viewX, double viewY)
    {
        if (Scale <= 0) return null;
        double sx = (viewX - OffsetX) / Scale;
        double sy = (viewY - OffsetY) / Scale;
        if (sx < 0 || sy < 0 || sx >= _sceneW || sy >= _sceneH) return null;
        return (sx, sy);
    }

    /// <summary>场景坐标 → 视图坐标（画图元、摆浮层用）。</summary>
    public (double X, double Y) ToView(double sceneX, double sceneY)
        => (OffsetX + sceneX * Scale, OffsetY + sceneY * Scale);

    // ── 内部 ──────────────────────────────────────────────────

    private void ApplyZoomAnchored(double newZoom, double anchorX, double anchorY)
    {
        newZoom = Math.Clamp(newZoom, MinZoom, MaxZoom);

        // 锚点底下那个**场景点**：缩放前后它必须落在同一个视口位置
        double sceneX = Scale > 0 ? (anchorX - OffsetX) / Scale : 0;
        double sceneY = Scale > 0 ? (anchorY - OffsetY) / Scale : 0;

        Zoom = newZoom;
        Scale = _baseScale * Zoom;
        OffsetX = anchorX - sceneX * Scale;
        OffsetY = anchorY - sceneY * Scale;

        Clamp();
    }

    /// <summary>
    /// 把平移量约束回合理范围。
    ///
    /// <list type="bullet">
    /// <item>某个方向上**内容装得下** ⇒ 居中（**不能平移**：否则整幅被推走、
    ///   露出回不来的空白 —— 这正是"看全貌"那一档的默认状态）；</item>
    /// <item>装不下 ⇒ 限制成**内容始终盖满视口**（两头都不露白）。</item>
    /// </list>
    /// </summary>
    private void Clamp()
    {
        OffsetX = ClampAxis(OffsetX, _sceneW * Scale, _viewW);
        OffsetY = ClampAxis(OffsetY, _sceneH * Scale, _viewH);
    }

    private static double ClampAxis(double offset, double content, double view)
    {
        if (content <= view) return (view - content) / 2;     // 装得下 ⇒ 居中
        // 装不下 ⇒ offset 落在 [view - content, 0]，两头都不露白
        return Math.Clamp(offset, view - content, 0);
    }
}
