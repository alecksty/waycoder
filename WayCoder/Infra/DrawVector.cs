namespace WayCoder.Infra;

/// <summary>
/// **矢量落笔面** —— 一份几何、第三种画法（前两种：手搓光栅化、SVG 导出）。
///
/// ## 为什么要有它
///
/// 手机端画一帧原来是：图元 → DSL 文本 → 解析 → **手搓光栅化**（3× 超采样再降采样）→
/// **PNG 编码** → （UI 线程）**PNG 解码** → 贴图。实测这段每帧 `光栅+PNG 26ms + 解码 54ms`，
/// 而且每帧新建一张位图 ⇒ 25fps × 333KB ≈ **10MB/s 垃圾**，10 秒里观察到底层 GC 跑了 355 次
/// —— 用户报的「抖动」里，属于平台的那一半就是它。
///
/// PNG 那一段是**纯粹的浪费**：图刚编出来就立刻解回去上屏。矢量后端把整段删掉 ——
/// 直接把图元画到平台画布上，光栅化交给 GPU（抗锯齿也比自己的超采样好）。
///
/// ## 为什么不做成"安卓专用"
///
/// 落笔面这层用的是 MAUI 的 `ICanvas`（`Microsoft.Maui.Graphics`），**一套代码两端跑** ——
/// 先按用户要求只在安卓上验证，之后 iOS/桌面**不用重写**，只是把开关打开。
/// 反面做法是"保留光栅化、只把像素缓冲交给平台位图"，那要给 Android/iOS 各写一份
/// （`Bitmap.CreateFromPixels` / `CGBitmapContext`），正是本仓最忌讳的「同一规则两处实现」。
///
/// ## 几何一行都不用重写
///
/// 形状的点集本来就在 <see cref="DrawGeo"/> 里（星形/正多边形/椭圆/环/圆角矩形/扇形/心形），
/// 光栅与 SVG 两个画法**已经在共用它**。矢量这一路同样只做"把这些点落到平台上"，
/// 所以 <see cref="DrawFill"/> 与本文件是一对：前者落进像素画布，后者落进平台画布。
/// 每个指令类在 <c>DrawCommands.Vector.cs</c> 里各实现一份 <see cref="IDrawCommand.Vector"/>，
/// 与 <c>Rasterize</c>/<c>EmitSvg</c> 并列 —— 三个画法都在同一个类里，改一处不会漏另一处。
/// </summary>
public interface IVectorTarget
{
    /// <summary>场景尺寸（`canvas w h` 那两个数）—— 渐变几何是归一化的，换算成绝对坐标要用它。</summary>
    double SceneWidth { get; }
    double SceneHeight { get; }

    /// <summary>
    /// 把**图元的仿射变换**交给平台坐标系，之后的图元按**本地坐标**画即可。
    ///
    /// 返回 <c>true</c> = 已进入（调用方**必须**配一次 <see cref="PopTransform"/>）；
    /// <c>false</c> = 本实现不支持，调用方**退回逐点变换**（<c>Canvas.TransformPoints</c>）。
    ///
    /// <para>
    /// 为什么值得单开一个方法：通用那条路（<c>FillShape</c>/<c>StrokePolyline</c>）要
    /// **逐点**建平台路径（Android 上每个点一次 JNI），而平台原生图元
    /// （<c>FillRectangle</c>/<c>FillEllipse</c>/<c>DrawLine</c>）根本不碰点集 ——
    /// 但它们要求形状在**当前坐标系**里是轴对齐的。刚体变换（纯旋转 + 平移）
    /// 不改变形状，所以"把矩阵挂到画布上、图元照常画"与"逐点变换再画折线"
    /// 结果完全一致，却省掉整份点集。
    /// </para>
    ///
    /// <para>
    /// 真机实测（`Examples/c/block_bench.c`，一千架带旋转的图块）：一帧 15000 次通用填充
    /// 要 **2.9 秒**，而同类图元走原生只要 **0.4 毫秒 × 30 帧** —— 差三个数量级。
    /// 症结不在图元数，在"旋转变换把每个矩形都逼成了折线"。
    /// </para>
    ///
    /// <para>
    /// ⚠ 调用方**只在 <see cref="Affine.IsRigid"/> 为真时**才该调它（见那里的说明：
    /// 等比缩放会让线宽变粗，那是观感变化）。接口这层不替调用方判 ——
    /// 判据只有一份，在 <see cref="Affine.IsRigid"/>。
    /// </para>
    ///
    /// ⚠ **带默认实现**（与上面那个 5 参 <c>FillShape</c> 同一套兼容策略）：
    /// 不支持的实现方一个字都不用改，自动退回逐点变换 —— 结果相同，只是慢一点。
    /// </summary>
    bool PushTransform(Affine t) => false;

    /// <summary>退出 <see cref="PushTransform"/> 开的坐标系（**仅当它返回过 true 才调**）。</summary>
    void PopTransform() { }

    /// <summary>
    /// 填充一组子路径（每个子路径是 x,y 交替的点集）。
    /// <paramref name="evenOdd"/> 为真时按**奇偶规则**挖洞（`path` 的多子路径靠它做环）。
    /// </summary>
    void FillShape(IReadOnlyList<IReadOnlyList<double>> subpaths, uint fill, Gradient? gradient, bool evenOdd);

    /// <summary>
    /// 带**显式刷子矩形**的填充。矩形是世界坐标；null = 用这组子路径自己的外接矩形。
    ///
    /// 描边那条路**必须显式传**：描边的轮廓比原几何胖出 `width/2`，拿轮廓盒归一化会让
    /// 渐变整体偏半个线宽（肉眼看不出来）。填充传 null 即可 —— 那时两者本来就相等。
    /// 语义与 SVG 的 `objectBoundingBox` 一致（它取的也是**几何**的盒，不含描边）。
    ///
    /// ⚠ **这是一个带默认实现的新方法，不是给上面那个加参数** —— 兼容性优先：
    /// 接口方法加参数对**所有实现方**都是破坏性改动（包括本仓之外的插件），
    /// 而"能画渐变描边"这件事只有实现方在意。默认实现直接退到 4 参版（丢掉矩形），
    /// 于是**不关心这个矩形的实现方一个字都不用改** —— 最坏也只是渐变位置退化成原样。
    /// </summary>
    void FillShape(IReadOnlyList<IReadOnlyList<double>> subpaths, uint fill, Gradient? gradient,
        bool evenOdd, (double MinX, double MinY, double MaxX, double MaxY)? box)
        => FillShape(subpaths, fill, gradient, evenOdd);

    /// <summary>
    /// **轴对齐的实心矩形**（<paramref name="radius"/> &gt; 0 时是圆角矩形）—— 平台原生快路径。
    ///
    /// ⚠ 为什么值得单开一个方法：通用那条路（<see cref="FillShape"/>）要把点集交给实现方
    ///   **逐个点建平台路径**（Android 上每个点是一次 JNI 调用）。一个矩形 4 个点看似不多，
    ///   但一帧几百个图元、绝大多数正是矩形与圆 —— 真机实测每帧几万次 JNI，
    ///   单帧 70ms、只有 13fps 的主因就在这里。而平台自己就有 `DrawRect`/`DrawOval`
    ///   这类原生图元，根本不必绕路径。
    ///
    /// 约定与 <see cref="FillShape"/> 完全一致：几何**已经落到世界坐标**（变换由调用方做，
    /// 所以调用方必须保证变换是恒等 —— 带旋转的矩形已经不是"轴对齐矩形"了），
    /// <paramref name="gradient"/> 非空时按这个矩形归一化（与几何的外接矩形同一个口径）。
    ///
    /// ⚠ **带默认实现**（与上面那个 5 参 `FillShape` 同一套兼容策略）：认不出这个方法的
    ///   实现方一个字都不用改，自动退化成"四点折线走通用路径" —— 结果相同，只是慢一点。
    /// </summary>
    void FillRect(double x, double y, double w, double h, uint fill, Gradient? gradient, double radius)
    {
        if (gradient == null && (fill >> 24) == 0) return;
        IReadOnlyList<double> pts = radius > 0
            ? DrawGeo.RoundRect(x, y, w, h, radius)
            : new List<double> { x, y, x + w, y, x + w, y + h, x, y + h };
        FillShape(new[] { pts }, fill, gradient, evenOdd: false);
    }

    /// <summary>
    /// **实心椭圆**（`rx == ry` 就是圆）—— 平台原生快路径，理由同 <see cref="FillRect"/>。
    ///
    /// ⚠ 通用那条路是用 **64 段折线**近似一个圆，而游戏里几十个圆的半径只有 2~5px ——
    ///   折线在那儿既看不出圆、又要付 64 个点建路径的代价。平台原生 `DrawOval`
    ///   又更快又更圆。默认实现同样退回折线。
    /// </summary>
    void FillEllipse(double cx, double cy, double rx, double ry, uint fill, Gradient? gradient)
    {
        if (gradient == null && (fill >> 24) == 0) return;
        FillShape(new[] { DrawGeo.Ellipse(cx, cy, rx, ry, 64) }, fill, gradient, evenOdd: false);
    }

    /// <summary>描边折线；<paramref name="close"/> 为真时首尾相连（多边形轮廓）。</summary>
    void StrokePolyline(IReadOnlyList<double> pts, double width, uint color, string cap, bool dashed, bool close);

    /// <summary>
    /// 一行文本。`x` 是锚点列、`y` 是**基线**（与手搓字体那条路的语义一致），
    /// `anchor` 取 `start`/`middle`/`end` 决定水平对齐。多行由调用方按行距拆开。
    /// </summary>
    void DrawText(double x, double y, string text, double size, uint color, string anchor, bool bold, bool italic);

    /// <summary>
    /// 贴图。<paramref name="path"/> 是原始图片文件（矢量侧不解码成像素，交给平台按需解码并缓存）；
    /// <paramref name="transformed"/> 表示图元带了非"等比缩放+平移"的变换（旋转/错切）——
    /// 平台画布未必支持任意仿射，实现方可以据此选择放弃（<see cref="MarkUnsupported"/>）。
    /// </summary>
    void DrawImage(string? path, double x, double y, double w, double h,
        double srcX, double srcY, double srcW, double srcH, double cornerRadius, bool transformed);

    /// <summary>
    /// 本目标**画不了**这个图元（自定义指令没实现矢量画法）。
    /// 宿主据此把整个窗口回退到光栅后端 —— 宁可慢，也别默默少画东西。
    /// </summary>
    void MarkUnsupported(string kind, string? detail = null);

    /// <summary>
    /// 压入一级**矩形裁剪**（与上一级求交）—— DSL 的 `clip x y w h`，见 `ClipCommand`。
    ///
    /// ⚠ 与光栅那边**语义必须一致**：都是"从这条往后生效、`PopClip` 恢复上一级"。
    ///   两边不一致的症状是"手机上对了、导出的 PNG 不对"（或反过来），而那两种产物
    ///   平时根本不会摆在一起看。
    /// </summary>
    void PushClip(double x, double y, double w, double h);

    /// <summary>弹出一级裁剪（没有可弹的就什么都不做）。</summary>
    void PopClip();

    /// <summary>
    /// 压入一级**任意形状蒙版**（DSL 的 `mask`，见 `MaskCommand`）—— 与
    /// <see cref="PushClip"/> 并列，只是形状是任意路径。
    ///
    /// <paramref name="subpaths"/> 是**已经折叠好的一条路径**（多层布尔在前端算完了，
    /// 见 `MaskExpr.ToClipPath`），<paramref name="evenOdd"/> 是它的填充规则。
    /// 实现方**不必懂布尔运算** —— 前端要么给出一条能直接裁剪的路径，要么根本不调这个方法
    /// （折叠不了就整窗回退光栅）。
    ///
    /// ⚠ 与光栅那边**语义必须一致**：都是"从这条往后生效、`PopMask` 恢复上一级"。
    ///   不一致的症状是"手机上对了、导出的 PNG 不对"（或反过来），而这两种产物平时
    ///   根本不会摆在一起看。
    /// </summary>
    void PushMask(IReadOnlyList<IReadOnlyList<double>> subpaths, bool evenOdd);

    /// <summary>弹出蒙版那一级（没有可弹的就什么都不做）。</summary>
    void PopMask();
}

/// <summary>
/// 矢量后端的共享落笔助手 —— 与 <see cref="DrawFill"/> 一一对应（那个落进像素画布，这个落进平台画布）。
///
/// 三件事在这里统一，指令层不必各写一遍：
/// ① **变换先落到点上**（`Canvas.TransformPoints` / `Affine.Apply`，与光栅那条路同一个函数）——
///    平台画布因此不必再做变换，两边的坐标语义天然一致；
/// ② **透明即不画**：DSL 里"空心图形"靠把填充写成全透明色表达（见 <c>VmlScene.Style</c>），
///    光栅侧对 alpha=0 是像素级无效，矢量侧直接跳过更省（也免得平台对 alpha=0 的处理不一致）；
/// ③ **点太少不画**：少于 3 个点填不出面、少于 2 个点描不出线（与光栅侧的退化行为一致）。
/// </summary>
public static class DrawVector
{
    /// <summary>单个多边形的填充（**带变换**的图元走这条）。</summary>
    public static void Polygon(IVectorTarget t, IReadOnlyList<double> pts, DrawFigure f)
    {
        if (pts.Count < 6) return;
        if (f.Gradient == null && (f.Fill >> 24) == 0) return;
        t.FillShape(new[] { Canvas.TransformPoints(f.Transform, pts) }, f.Fill, f.Gradient, evenOdd: false);
    }

    /// <summary>
    /// 这个图元**要不要描边**。
    ///
    /// 用途是**省掉一份白建的点集**：`Stroke` 自己会在"没给描边色"时提前返回，但调用方
    /// 往往已经把点集拼好了（矩形那四个点、圆那 64 个点）。先问一句再拼。
    /// </summary>
    public static bool HasStroke(DrawFigure f)
        => f.StrokeGradient != null || (f.Stroke != 0 && (f.Stroke >> 24) != 0);

    /// <summary>
    /// 轴对齐矩形的填充：**能走平台原生就走原生**（见 `IVectorTarget.FillRect` 的说明），
    /// 带旋转/错切时退回折线 —— 那时它已经不是"轴对齐矩形"了。
    /// </summary>
    public static void Rect(IVectorTarget t, double x, double y, double w, double h, double radius, DrawFigure f)
    {
        if (f.Gradient == null && (f.Fill >> 24) == 0) return;
        if (f.Transform.IsIdentity)
        {
            t.FillRect(x, y, w, h, f.Fill, f.Gradient, radius);
            return;
        }
        var pts = radius > 0
            ? DrawGeo.RoundRect(x, y, w, h, radius)
            : new List<double> { x, y, x + w, y, x + w, y + h, x, y + h };
        Polygon(t, pts, f);
    }

    /// <summary>椭圆的填充 —— 同 <see cref="Rect"/>，能走原生 `DrawOval` 就不建那 64 个点。</summary>
    public static void Ellipse(IVectorTarget t, double cx, double cy, double rx, double ry, DrawFigure f)
    {
        if (f.Gradient == null && (f.Fill >> 24) == 0) return;
        if (f.Transform.IsIdentity)
        {
            t.FillEllipse(cx, cy, rx, ry, f.Fill, f.Gradient);
            return;
        }
        Polygon(t, DrawGeo.Ellipse(cx, cy, rx, ry, 64), f);
    }

    /// <summary>多子路径填充（`path` 的挖洞）。</summary>
    public static void Subpaths(IVectorTarget t, IReadOnlyList<IReadOnlyList<double>> subs, DrawFigure f)
    {
        if (subs.Count == 0) return;
        if (f.Gradient == null && (f.Fill >> 24) == 0) return;
        var outSubs = new List<IReadOnlyList<double>>(subs.Count);
        foreach (var s in subs)
        {
            if (s.Count < 6) continue;                        // 填不出面的子路径不参与奇偶判定
            outSubs.Add(Canvas.TransformPoints(f.Transform, s));
        }
        if (outSubs.Count == 0) return;
        t.FillShape(outSubs, f.Fill, f.Gradient, evenOdd: true);
    }

    /// <summary>描边（`Stroke == 0` 或全透明表示没给描边色，跳过）。</summary>
    public static void Stroke(IVectorTarget t, IReadOnlyList<double> pts, DrawFigure f, bool close = false)
    {
        // **渐变描边**：平台画布没有 `SetStrokePaint`（只有 `SetFillPaint`），
        // 但描边本质上就是一个填充多边形 ⇒ 把折线展成一组多边形再填即可，**不必回退光栅**。
        if (f.StrokeGradient != null)
        {
            StrokeBrushed(t, pts, f, close);
            return;
        }
        if (f.Stroke == 0 || (f.Stroke >> 24) == 0) return;
        if (pts.Count < 4) return;
        t.StrokePolyline(Canvas.TransformPoints(f.Transform, pts), f.StrokeWidth, f.Stroke,
            f.LineCap, f.Dashed, close);
    }

    /// <summary>
    /// **渐变描边的矢量画法**：几何取自 <see cref="DrawGeo.StrokePieces"/>
    /// （**与光栅后端同一份**），变换落到点上，再一次性填掉。
    ///
    /// ## 两条必须记住的约束
    ///
    /// ① **刷子矩形必须传原几何的盒**（`BoundsOf`），不能让它退到"轮廓的外接矩形" ——
    ///    轮廓比几何胖出 `width/2`，拿它归一化会让渐变整体偏半个线宽。偏一点**肉眼看不出来**，
    ///    所以这条只在断言里卡得住（光栅那边的自测就是拿"起点必须是纯起始色"卡的）。
    /// ② **必须非零环绕**：这些块之间是**并集**关系（接头处、圆帽与杆之间都重叠），
    ///    用奇偶规则会把每一处重叠都挖成洞。能这么写是因为 `StrokePieces` 产出的每块
    ///    **绕向一致**（四边形无论朝向、圆的鞋带和都是正的），所以非零规则下它们只会相加。
    ///
    /// 一次 `FillShape` 装下全部子路径，不是逐块调 —— 一条 64 段的椭圆描边加上圆帽
    /// 能到两百块，逐块调就是每帧两百次建路径。
    /// </summary>
    static void StrokeBrushed(IVectorTarget t, IReadOnlyList<double> pts, DrawFigure f, bool close)
    {
        var pieces = DrawGeo.StrokePieces(pts, f.StrokeWidth, f.LineCap, close, f.Dashed);
        if (pieces.Count == 0) return;
        var subs = new List<IReadOnlyList<double>>(pieces.Count);
        foreach (var p in pieces) subs.Add(Canvas.TransformPoints(f.Transform, p));
        t.FillShape(subs, 0, f.StrokeGradient, evenOdd: false, box: BoundsOf(f.Transform, pts));
    }

    /// <summary>局部包围盒的四角变换到世界之后的**轴对齐**包围盒（刷子矩形的口径）。</summary>
    static (double MinX, double MinY, double MaxX, double MaxY) BoundsOf(Affine tf, IReadOnlyList<double> pts)
    {
        var (x0, y0, x1, y1) = DrawGeo.BBox(pts);
        double minX = double.MaxValue, minY = double.MaxValue, maxX = double.MinValue, maxY = double.MinValue;
        foreach (var (lx, ly) in new[] { (x0, y0), (x1, y0), (x1, y1), (x0, y1) })
        {
            var (wx, wy) = tf.Apply(lx, ly);
            minX = Math.Min(minX, wx); maxX = Math.Max(maxX, wx);
            minY = Math.Min(minY, wy); maxY = Math.Max(maxY, wy);
        }
        return (minX, minY, maxX, maxY);
    }

    /// <summary>
    /// 多行文本：按 `字号 × 1.3` 的行距逐行画（与光栅那条路同一个行距），
    /// 锚点与字号都含图元变换（缩放走 <c>ScaleFactor</c>，与光栅/SVG 两侧口径一致）。
    /// </summary>
    public static void Text(IVectorTarget t, DrawFigure f)
    {
        if (string.IsNullOrEmpty(f.Text)) return;
        // **渐变文字**：平台那套文字 API（`ICanvas.DrawString`）只吃一个纯色，
        // 没有"字形 → 路径"的入口 ⇒ 矢量后端**做不了**，如实标记 ⇒ 宿主整窗回退光栅。
        // （渐变描边当初看着也"做不了"，但描边本质是填充多边形、能轮廓化；
        //   文字不行 —— 要拿到字形轮廓得走平台专有 API，正是本后端刻意避开的那类东西。）
        // 回退之后由光栅那条路画（它支持），而 v0.96.308 起回退会**重出这一帧**，
        // 所以静态程序也不会停在残缺帧上。
        if (f.Gradient != null)
        {
            t.MarkUnsupported("text-gradient", "text");
            return;
        }
        if ((f.Fill >> 24) == 0) return;
        // 竖对齐的偏移**加在局部 y 上**、再走变换 —— 与 SVG / 光栅两条路同源
        // （`DrawParse.TextVOffset`），别在这里另算一个（那就是"文字盒第三份实现"）。
        var p = f.Transform.Apply(f.Args[0], f.Args[1] + DrawParse.TextVOffset(f));
        var size = f.FontSize * f.Transform.ScaleFactor;
        var bold = f.FontWeight.Contains("bold", StringComparison.OrdinalIgnoreCase);
        var italic = f.FontStyle.Contains("italic", StringComparison.OrdinalIgnoreCase);
        var lines = f.Text.Split('\n');
        var lineH = size * 1.3;
        for (var i = 0; i < lines.Length; i++)
            t.DrawText(p.X, p.Y + lineH * i, lines[i], size, f.Fill, f.Anchor, bold, italic);
    }
}
