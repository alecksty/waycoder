using Microsoft.Maui.Graphics;
using Microsoft.Maui.Graphics.Platform;
// IImage 在 Microsoft.Maui 与 Microsoft.Maui.Graphics 下都有 ⇒ 必须限定，否则 CS0104
using IImage = Microsoft.Maui.Graphics.IImage;
using GFont = Microsoft.Maui.Graphics.Font;
using WayCoder.Infra;

namespace WayCoder.Maui.Services;

/// <summary>
/// <see cref="IVectorTarget"/> 在 **MAUI 图形栈**上的实现 —— 把图元直接画到平台画布，
/// 光栅化交给 GPU。一套代码两端跑（Android / iOS），所以安卓上验完不用为 iOS 重写。
///
/// ## 与光栅那条路的差别（照实写清楚，别指望"看起来一样"）
///
/// · **抗锯齿**：平台做，比"3× 超采样再降采样"更好看（斜线/圆角/文字边缘）；
/// · **文字**：用平台字体（比手搓 TrueType 好看），但**度量不同** —— 我们那条路 `y` 是基线，
///   这里要换算成文本框顶端（`基线 − 上升`）。上升**问平台要**（见 `Ascent`）：从前拿
///   0.8×字号 估，实测差 0.245×字号，真机上表现为"文字靠下、不居中"；
/// · **渐变**：平台刷子按绝对几何铺，与光栅侧归一化采样的结果在小尺寸上可能差一两个色阶；
/// · **虚线**：平台按 `StrokeDashPattern` 走，相位从路径起点算 —— 与光栅的逐段绘制
///   在长折线上可能有半个周期的差。
///
/// 这些都是"观感差异"而不是"画错"，但值得在自测里留一条**抽样比对**的护栏。
///
/// ## ⚠ 渐变的余荫：用过一次渐变之后，**后面的纯色全都画不出来**（v0.96.297 修）
///
/// 这是"两条路都对、拼起来就错"的一类，桌面自测（记录型落笔面）**照不出来** ——
/// 因为它错在**平台实现**里，而不是我们的映射里。
///
/// 机制（逐层读 MAUI 源码确认，`Microsoft.Maui.Graphics` 10.0.20）：
///   · `PlatformCanvas.SetFillPaint(paint, rect)` 遇到渐变会把 shader **挂到那个
///     Android `Paint` 对象上**（`CurrentState.SetFillPaintShader(shader)`），
///     并在**开头**清掉上一个 shader —— 它是**唯一**会清 shader 的入口；
///   · 而 `PlatformCanvas.FillColor` 的 setter **只写 `_fillColor`**，不碰 shader；
///   · 真正上屏用的是 `CurrentState.FillPaintWithAlpha` —— 它拿同一个 `Paint`，
///     `SetARGB(...)` 写上颜色**就返回了**。而 Android 里 **shader 优先级高于颜色**，
///     颜色写得再对也不起作用。
///   ⇒ 只要这一帧里画过任何一个渐变，**之后所有 `FillColor = …` 都是空操作**，
///     统统被那个旧渐变接管：落在刷子矩形内的部分是渐变，落在外的按 `TileMode.Clamp`
///     取端点色。
///
/// **实测症状**（`Examples/c/calc.c`，用户报的「按键颜色还是不对」）：
/// 计算器先画"玻璃面板"（一个从 `0x33FFFFFF` 到 `0x11FFFFFF` 的竖向渐变），
/// **再画二十个按键** —— 于是每个按键都被那块玻璃渐变接管；按键全在面板包围盒**下方**
/// 被 clamp 到 EndColor `0x11FFFFFF`，等价于"给底图叠了 7% 白"：
/// 数字键/运算符/功能键/等号**四档配色全被抹平**，屏幕上只剩背景那层径向渐变在透出来。
/// 量出来的证据：十条同色横带里，渐变之后那四条画成了**红→蓝的渐变**（刷子 `g` 的
/// 红→蓝被原样搬过来了），渐变之前的两条是**精确的 `#2A3346`**。
///
/// **规矩**：这条路上**不要再出现裸的 `_canvas.FillColor = …`**。纯色一律走
/// `SetFillPaint(SolidPaint, rect)`（多花一次调用，换来"上一个 shader 一定被清掉"）。
/// </summary>
internal sealed class MauiVectorTarget : IVectorTarget
{
    private readonly ICanvas _canvas;
    private readonly Dictionary<string, IImage?> _images = new(StringComparer.Ordinal);
    private readonly HashSet<string> _unsupported = new(StringComparer.Ordinal);

    public MauiVectorTarget(ICanvas canvas, double sceneWidth, double sceneHeight)
    {
        _canvas = canvas;
        SceneWidth = sceneWidth <= 0 ? 1 : sceneWidth;
        SceneHeight = sceneHeight <= 0 ? 1 : sceneHeight;
    }

    public double SceneWidth { get; }
    public double SceneHeight { get; }

    /// <summary>本帧里画不出来的图元（自定义指令没实现矢量画法）。宿主据此回退到光栅后端。</summary>
    public IReadOnlyCollection<string> Unsupported => _unsupported;

    public void MarkUnsupported(string kind, string? detail = null) => _unsupported.Add(kind);

    // ── 图元变换 ───────────────────────────────────────────────────────────

    /// <summary>
    /// 把图元的**刚体变换**（纯旋转 + 平移）交给平台坐标系 —— 于是矩形还是矩形、
    /// 圆还是圆，都能走平台原生图元，不必逐点建路径。理由与收益见
    /// <see cref="IVectorTarget.PushTransform"/> 的说明（真机实测差三个数量级）。
    ///
    /// ⚠ **与 `PushClip`/`PushMask` 共用平台那一套 `SaveState`/`RestoreState`** ——
    ///   只要严格配对就没有问题（栈本来就是配对的），但**不能交叉着不关**
    ///   （比如进入图元变换后又去 push 蒙版、然后只 pop 一次）。
    ///   驱动层是"一个图元一次 push/pop"，天然配对。
    /// </summary>
    public bool PushTransform(Affine t)
    {
        _canvas.SaveState();

        // ⚠⚠ **不要用 `ConcatenateTransform(Matrix3x2)`** —— 踩过一次，记下来：
        //   它的语义（.NET 的行主序 `Matrix3x2` 与 Android `Matrix` 的列主序、
        //   以及 MAUI 中间那层 `AsAndroidMatrix` 的换算）**实测对不上**。
        //   症状很有欺骗性：**平移分量被吃掉、旋转变成错切** ——
        //   一个 `rotate 45` 的矩形画出来是**平行四边形**，而且所有图元都挤到原点附近。
        //   而它**不报错、不崩**，只是画得不对（真机 A/B 截屏才看出来）。
        //
        //   改用 `Translate` + `Rotate` 拼 —— 这两个是**本文件已经在用**的 API
        //   （`DrawVectorFrame` 里那对 `Translate`/`Scale` 一直是对的），约定已经被验证过。
        //   刚体变换恰好能这么拆：`Affine` 对刚体就是"平移 + 纯旋转"，
        //   没有缩放也没有错切 ⇒ `A = cos θ`、`B = sin θ`，角度由 `atan2(B, A)` 还原。
        //
        //   ⚠ 顺序是**先转后移**（`T ∘ R`），与 `Affine` 的 `E/F = 平移` 同一口径 ——
        //     反过来写是"沿旋转后的轴平移"，`(0,0)` 之外全都偏。
        _canvas.Translate((float)t.E, (float)t.F);
        var deg = Math.Atan2(t.B, t.A) * 180.0 / Math.PI;
        if (Math.Abs(deg) > 1e-6) _canvas.Rotate((float)deg);
        return true;
    }

    public void PopTransform() => _canvas.RestoreState();

    // ── 裁剪 ───────────────────────────────────────────────────────────────
    // 平台画布自己就有裁剪栈，直接借它的 `SaveState`/`RestoreState` 配对 ——
    // **不要自己再维护一份矩形栈**：平台的裁剪跟它的变换是一体的，
    // 自己算一份等于把"哪一级压了多少"记两遍，迟早对不上。
    public void PushClip(double x, double y, double w, double h)
    {
        var __t0 = VectorProbe.Begin();
        _canvas.SaveState();
        _canvas.ClipRectangle((float)x, (float)y, (float)w, (float)h);
        VectorProbe.End(6, __t0);
    }

    public void PopClip()
    {
        var __t0 = VectorProbe.Begin();
        _canvas.RestoreState();
        VectorProbe.End(6, __t0);
    }

    // 蒙版同理借平台的裁剪栈 —— 只是形状从矩形换成任意路径。
    // ⚠ `ClipPath` 与 `ClipRectangle` 压的是**同一层栈**，所以 `PushMask`/`PopMask`
    //   必须与 `PushClip`/`PopClip` 严格配对（驱动层按 DSL 顺序发，天然成对）。
    public void PushMask(IReadOnlyList<IReadOnlyList<double>> subpaths, bool evenOdd)
    {
        // ⚠ 蒙版是**替换**语义（不是 push/pop 配对）：新的一条要把上一条**先关掉**，
        //   否则 `mask_clear` 之后的内容仍被上一个蒙版箍着（SVG 那边踩过同一个坑）。
        var __t0 = VectorProbe.Begin();
        if (_maskOpen) { _canvas.RestoreState(); _maskOpen = false; }

        var path = new PathF();
        foreach (var sub in subpaths)
        {
            if (sub.Count < 4) continue;
            path.MoveTo((float)sub[0], (float)sub[1]);
            for (int i = 2; i + 1 < sub.Count; i += 2)
                path.LineTo((float)sub[i], (float)sub[i + 1]);
            path.Close();
        }
        _canvas.SaveState();
        _canvas.ClipPath(path, evenOdd ? WindingMode.EvenOdd : WindingMode.NonZero);
        _maskOpen = true;
        VectorProbe.End(6, __t0);
    }

    public void PopMask()
    {
        if (!_maskOpen) return;
        var __t0 = VectorProbe.Begin();
        _canvas.RestoreState();
        _maskOpen = false;
        VectorProbe.End(6, __t0);
    }

    /// <summary>
    /// 当前有没有压着蒙版那一层。
    /// ⚠ 它和裁剪共用平台那一套 `SaveState`/`RestoreState` —— 两条**交叉**使用
    ///   （裁剪的生命周期跨越一次蒙版替换）时会关错层。VML 生成的绘制流不会这样
    ///   （`ui_gfx` 的蒙版与裁剪各管各的段落），手写 DSL 才会碰上；真碰上就拆成两段。
    /// </summary>
    private bool _maskOpen;

    // ── 填充 / 描边 ────────────────────────────────────────────────────────

    public void FillShape(IReadOnlyList<IReadOnlyList<double>> subpaths, uint fill, Gradient? gradient,
        bool evenOdd) => FillShape(subpaths, fill, gradient, evenOdd, null);

    public void FillShape(IReadOnlyList<IReadOnlyList<double>> subpaths, uint fill, Gradient? gradient,
        bool evenOdd, (double MinX, double MinY, double MaxX, double MaxY)? box)
    {
        var __t0 = VectorProbe.Begin();
        var path = new PathF();
        var any = false;
        var __pts = 0;
        foreach (var pts in subpaths)
        {
            if (pts.Count < 6) continue;
            path.MoveTo((float)pts[0], (float)pts[1]);
            for (var i = 2; i + 1 < pts.Count; i += 2)
                path.LineTo((float)pts[i], (float)pts[i + 1]);
            path.Close();
            __pts += pts.Count / 2;
            any = true;
        }
        if (!any) { VectorProbe.End(2, __t0, 0); return; }

        // ⚠ **两条分支都必须走 `SetFillPaint`，纯色那条不能只写 `FillColor`** ——
        //    原因见类注释里「渐变的余荫」。一句话：平台把渐变挂成 Android `Paint` 的
        //    shader，而 shader **优先级高于颜色**，只改 `FillColor` 是改不动的。
        // 刷子矩形：默认取这条路径的外接矩形；**描边必须显式传**原几何的盒 ——
        // 描边轮廓比几何胖出 width/2，用轮廓盒归一化会让渐变整体偏半个线宽（肉眼看不出来）。
        var rect = box is { } b
            ? new RectF((float)b.MinX, (float)b.MinY, (float)(b.MaxX - b.MinX), (float)(b.MaxY - b.MinY))
            : path.Bounds;
        if (gradient != null)
        {
            // 渐变坐标本身就是"相对这个矩形"的 0..1（见 BuildPaint）
            _canvas.SetFillPaint(BuildPaint(gradient), rect);
        }
        else
        {
            _solid.Color = Col(fill);
            _canvas.SetFillPaint(_solid, rect);
        }
        _canvas.FillPath(path, evenOdd ? WindingMode.EvenOdd : WindingMode.NonZero);
        VectorProbe.End(2, __t0, __pts);
    }

    /// <summary>
    /// **轴对齐矩形 / 椭圆的平台原生填充** —— 一帧里绝大多数图元走这条。
    ///
    /// ⚠ 为什么不复用 <see cref="FillShape"/>：那条路要把点集交给平台**逐点建路径**
    ///   （`AsAndroidPath()` 里每个点一次 JNI）。一个矩形 4 个点、一个圆 64 个点，
    ///   一帧几百个图元就是几万次 JNI —— 真机实测单帧 70ms、只有 13fps 的主因。
    ///   `FillRectangle`/`DrawRoundRect`/`DrawOval` 都是 Android 的原生图元，
    ///   不建路径、不碰点集。
    ///
    /// ⚠ 刷子仍要经过 `SetFillPaint`（不能只写 `FillColor`）—— 理由见类注释「渐变的余荫」。
    ///   渐变按**这个矩形**归一化，与 `FillShape` 用 `path.Bounds` 是同一个口径
    ///   （矩形就是它自己的外接矩形；椭圆的外接矩形也正是 `cx±rx, cy±ry`）。
    /// </summary>
    public void FillRect(double x, double y, double w, double h, uint fill, Gradient? gradient, double radius)
    {
        if (gradient == null && (fill >> 24) == 0) return;
        var __t0 = VectorProbe.Begin();
        var rect = new RectF((float)x, (float)y, (float)w, (float)h);
        if (gradient != null)
        {
            _canvas.SetFillPaint(BuildPaint(gradient), rect);
        }
        else
        {
            _solid.Color = Col(fill);
            _canvas.SetFillPaint(_solid, rect);
        }
        if (radius > 0) _canvas.FillRoundedRectangle(rect, (float)radius);
        else _canvas.FillRectangle(rect);
        VectorProbe.End(0, __t0);
    }

    /// <summary>实心椭圆 —— 同 <see cref="FillRect"/>，平台走原生 `DrawOval`。</summary>
    public void FillEllipse(double cx, double cy, double rx, double ry, uint fill, Gradient? gradient)
    {
        if (gradient == null && (fill >> 24) == 0) return;
        var __t0 = VectorProbe.Begin();
        var rect = new RectF((float)(cx - rx), (float)(cy - ry), (float)(rx * 2), (float)(ry * 2));
        if (gradient != null)
        {
            _canvas.SetFillPaint(BuildPaint(gradient), rect);
        }
        else
        {
            _solid.Color = Col(fill);
            _canvas.SetFillPaint(_solid, rect);
        }
        _canvas.FillEllipse(rect);
        VectorProbe.End(1, __t0);
    }

    /// <summary>
    /// 纯色填充用的可复用刷子。
    ///
    /// 只用来**把上一次的渐变 shader 顶掉**（`SetFillPaint` 是唯一会清 shader 的入口，
    /// 见「渐变的余荫」）。平台对 SolidPaint 的处理就是一句 `FillColor = paint.Color`，
    /// 读完即弃，所以一个实例反复改 `.Color` 是安全的 —— 这条路每帧要走上百次，
    /// 不值得每次都 new 一个（本后端当初就是为了消掉每帧的垃圾才做的）。
    /// </summary>
    private readonly SolidPaint _solid = new(Colors.White);

    public void StrokePolyline(IReadOnlyList<double> pts, double width, uint color, string cap, bool dashed, bool close)
    {
        var __t0 = VectorProbe.Begin();
        var path = BuildPath(pts, close);
        _canvas.StrokeColor = Col(color);
        _canvas.StrokeSize = (float)Math.Max(0.1, width);
        _canvas.StrokeLineCap = cap switch
        {
            "round" => LineCap.Round,
            "square" => LineCap.Square,
            _ => LineCap.Butt,
        };
        // 虚线间距按线宽给（与我方光栅那条路的 dash 语义一致：短划 3×、空档 2×线宽）
        _canvas.StrokeDashPattern = dashed
            ? new[] { (float)(width * 3), (float)(width * 2) }
            : null;
        _canvas.DrawPath(path);
        VectorProbe.End(3, __t0, pts.Count / 2);
    }

    // ── 文本 / 贴图 ────────────────────────────────────────────────────────

    public void DrawText(double x, double y, string text, double size, uint color, string anchor, bool bold, bool italic)
    {
        if (string.IsNullOrEmpty(text)) return;
        var __t0 = VectorProbe.Begin();
        _canvas.Font = bold || italic ? GFont.DefaultBold : GFont.Default;
        _canvas.FontSize = (float)Math.Max(1, size);
        _canvas.FontColor = Col(color);

        // 我方约定 `y` 是**基线**，而平台 `DrawString` 的 y 是文本框顶端 ⇒ 上移一个"上升"。
        // ⚠ **上升要问平台要**（见 `Ascent`）：共享层那个 0.8 是给**自绘**（光栅/SVG）用的
        //   近似值，与平台字体并不相同 —— 实测差 0.245×字号（76px 的算盘按键上差 19px，
        //   肉眼一眼看出"靠下"）。两处都用同一个近似值**并不能**互相抵消，只会把误差留在结果里。
        var top = y - Ascent(size, bold || italic);
        var align = anchor switch
        {
            "middle" => HorizontalAlignment.Center,
            "end" => HorizontalAlignment.Right,
            _ => HorizontalAlignment.Left,
        };
        // 平台没有"只给锚点"的 DrawString 重载，最近的是"给一个矩形 + 对齐方式"⇒
        // 矩形的摆法交给共享层的 VmlUi.TextAnchorBox（纯逻辑，桌面自测能锁住它）；
        // 垂直方向按文本框顶端对齐（top 已换算过基线）。
        var (boxX, boxW) = WayCoder.UI.Shared.VmlUi.TextAnchorBox(x, SceneWidth, anchor);
        var boxH = (float)Math.Max(1, size * 2);
        _canvas.DrawString(text, (float)boxX, (float)top, (float)boxW, boxH, align, VerticalAlignment.Top);
        VectorProbe.End(4, __t0);
    }

    /// <summary>
    /// 文字**上升**（盒顶 = 基线 − 本值）。**优先问平台要真实字体度量**。
    ///
    /// <para>
    /// ⚠ 为什么不直接用共享层那个 <see cref="WayCoder.Infra.DrawParse.TextAscentRatio"/>（0.8）：
    /// 那是给**自绘**两条路（光栅 TrueType / SVG）用的近似值，而这里是**平台字体** ——
    /// 两者的真实上升并不相同。实测（真机 1080×2400，字号 76px 的算盘按键）：
    /// 用 0.8 换算会让文字整体**偏低约 0.245×字号（19px）**，用户看到的就是「文字靠下、不居中」。
    /// </para>
    ///
    /// <para>
    /// 而竖对齐偏移（<c>TextVOffset</c>）也是拿 0.8 算的 ⇒ 两处一比，误差正好留在结果里：
    /// `基线 = y + (0.8s − 0.5s) − 0.8s + A·s = y + (A − 0.5)s`。
    /// 换成**真实上升 A** 之后 A 项相消 ⇒ `基线 = y + 0.3s`，正是 MIDDLE 档的本意。
    /// （所以这是"把近似换成实测"，而不是再补一个人工偏移 —— 后者换个字号就不准了。）
    /// </para>
    ///
    /// <para>
    /// 非 Android 退回共享常量：那是**有意**的（iOS 要用 CoreText 的对应度量，没验过就不假装）。
    /// 判据同本仓一贯口径 —— 改这里要在**真机上量**，构建通过证明不了什么。
    /// </para>
    /// </summary>
    private double Ascent(double size, bool bold)
    {
#if ANDROID
        // ── 先查缓存 ──────────────────────────────────────────────────────────
        // ⚠ 这个缓存不是"锦上添花"，是**必须的**：下面那段每次都要
        //   `SetTypeface(ToTypeface())` + `GetFontMetrics()`，而后者**每次分配一个
        //   Java 的 `Paint.FontMetrics` 对象**（JNI + 托管/Java 两侧的 GC 压力）。
        //   真机实测（gorilla，16 次文字/帧）：光文字一项 30 帧就要 133ms ≈ **每次 281μs** ——
        //   而一门程序的字号总共才两三种，同样的度量被反复重算了几百遍。
        //
        // ⚠ **key 用 `double` 原值而不是取整**：`13.0` 与 `13.4` 就是要分开算的
        //   （四舍五入会让它们共用同一个度量，字号越大偏差越明显）。
        //   字号是程序自己算出来的，稳定值会精确命中；偶然的抖动只是多算一次，不会算错。
        var key = (Size: size, Bold: bold);
        if (AscentCache.TryGetValue(key, out var hit)) return hit;
        try
        {
            MetricsProbe ??= new Android.Graphics.Paint();
            // ⚠ 绑定里 `Paint.Typeface` 是**只读**属性，只能走 SetTypeface（实测 CS0200）
            MetricsProbe.SetTypeface((bold ? GFont.DefaultBold : GFont.Default).ToTypeface());
            MetricsProbe.TextSize = (float)Math.Max(1, size);
            var m = MetricsProbe.GetFontMetrics();
            // Android 的 `Ascent` 是**负值**（基线以上的距离取负），故取反
            if (m is not null && m.Ascent < 0)
            {
                var a = -m.Ascent;
                AscentCache[key] = a;
                return a;
            }
        }
        catch { /* 取不到度量就退回近似值，绝不因此不画字 */ }
#endif
        return size * DrawParse.TextAscentRatio;
    }

#if ANDROID
    /// <summary>
    /// 量字体度量用的画笔（只读度量，不落笔）。
    ///
    /// ⚠ **静态**：它只是"量尺"，度量只跟字体与字号有关，跟哪个画布、哪个窗口无关 ——
    ///   而 `MauiVectorTarget` 是**每帧新建**的，做成实例字段就等于每帧重造一把尺子。
    /// </summary>
    private static Android.Graphics.Paint? MetricsProbe;

    /// <summary>
    /// 上升的缓存 —— key 是（字号，是否粗体），**与画布无关**，所以也是静态的。
    /// 度量只取决于平台字体与字号，跨窗口共享安全（见 <see cref="Ascent"/> 的说明）。
    /// </summary>
    private static readonly Dictionary<(double Size, bool Bold), double> AscentCache = new();
#endif

    public void DrawImage(string? path, double x, double y, double w, double h,
        double srcX, double srcY, double srcW, double srcH, double cornerRadius, bool transformed)
    {
        // 平台画布对"任意仿射"的支持不一（旋转/错切）⇒ 明说画不了，让宿主回退光栅后端
        if (transformed) { MarkUnsupported("image", "带旋转/错切的贴图"); return; }
        if (string.IsNullOrEmpty(path)) { MarkUnsupported("image", "只有内存位图、没有文件路径"); return; }
        var img = LoadImage(path);
        if (img == null) { MarkUnsupported("image", path); return; }
        var __t0 = VectorProbe.Begin();

        // 裁剪用"缩放到让裁剪区落进目标矩形 + 按目标矩形裁"这两步做 ——
        // 不依赖平台是否提供带源矩形的 DrawImage 重载（那份重载各版本不一）。
        var crop = srcW > 0 && srcH > 0;
        var scale = crop ? w / srcW : 1;
        var dx = crop ? x - srcX * scale : x;
        var dy = crop ? y - srcY * scale : y;
        var dw = crop ? img.Width * scale : w;
        var dh = crop ? img.Height * scale : h;

        _canvas.SaveState();
        if (cornerRadius > 0)
        {
            var clip = new PathF();
            AddRoundRect(clip, (float)x, (float)y, (float)w, (float)h, (float)cornerRadius);
            _canvas.ClipPath(clip);
        }
        else
        {
            _canvas.ClipRectangle((float)x, (float)y, (float)w, (float)h);
        }
        _canvas.DrawImage(img, (float)dx, (float)dy, (float)dw, (float)dh);
        _canvas.RestoreState();
        VectorProbe.End(5, __t0);
    }

    /// <summary>图片按路径缓存 —— 每帧重新解码一张图会把"省掉 PNG"的收益又还回去。</summary>
    private IImage? LoadImage(string path)
    {
        if (_images.TryGetValue(path, out var cached)) return cached;
        IImage? img = null;
        try
        {
            using var fs = File.OpenRead(path);
            img = PlatformImage.FromStream(fs);
        }
        catch (Exception ex)
        {
            ErrorLog.Warning("VmlVector", $"贴图解码失败 {path}: {ex.Message}");
        }
        _images[path] = img;
        return img;
    }

    // ── 小工具 ────────────────────────────────────────────────────────────

    /// <summary>点集 → 路径（x,y 交替）。</summary>
    private static PathF BuildPath(IReadOnlyList<double> pts, bool close)
    {
        var p = new PathF();
        if (pts.Count < 2) return p;
        p.MoveTo((float)pts[0], (float)pts[1]);
        for (var i = 2; i + 1 < pts.Count; i += 2)
            p.LineTo((float)pts[i], (float)pts[i + 1]);
        if (close) p.Close();
        return p;
    }

    /// <summary>
    /// 渐变几何 → 平台刷子（**原样透传**，不做任何坐标换算）。
    ///
    /// 我方 `Gradient` 的坐标是**相对「这张形状自己的包围盒」**归一化的 —— 这不是随手定的，
    /// 有三处真源互相印证：光栅侧 `FillTransformed` 的 `nx = (lx - minX) / spanX`（局部包围盒）、
    /// SVG 导出用 `objectBoundingBox`（`EmitGradient` 注释逐字如此）、`Gradient` 的字段注释。
    /// 而 MAUI 的 `LinearGradientPaint` / `RadialGradientPaint` 要的**正好也是**「相对刷子矩形的
    /// 0..1」（文档：*typically expressed in relative coordinates from (0,0) to (1,1)*；
    /// 径向默认 center (0.5,0.5) / radius 0.5）⇒ 两者同源，直接给过去即可。
    ///
    /// ⚠ **这里我改错过两次，两条弯路都记下来**（因为"看着都对"）：
    /// ① 起初把 `g.X1 * SceneWidth` 这类**绝对场景坐标**给平台 —— 平台只认 0..1，
    ///    100 多的数被当成"远在形状之外" ⇒ 整块落在 t≈0 ⇒ **渲染成纯色**；
    /// ② 于是"修"成「场景归一化 → 绝对 → 再按包围盒归一化」，这回数是对的、渐变也真出来了，
    ///    但语义错了：场景级的渐变铺到小形状上变成**一段切片**（实测紫→蓝），
    ///    而程序想要的是"这块的左边红、右边蓝"。**换算的方向对，前提（相对谁归一化）错**。
    /// 教训：**先问"这个数相对谁归一化"，再动手算** —— 光栅侧一行除法就写着答案。
    /// </summary>
    private Paint BuildPaint(Gradient g)
    {
        var a = Col(g.ColorA);
        var b = Col(g.ColorB);

        if (g.Radial)
        {
            return new RadialGradientPaint
            {
                Center = new Point((float)g.Cx, (float)g.Cy),
                Radius = (float)Math.Max(1e-4, g.R),
                StartColor = a,
                EndColor = b,
            };
        }
        return new LinearGradientPaint
        {
            StartPoint = new Point((float)g.X1, (float)g.Y1),
            EndPoint = new Point((float)g.X2, (float)g.Y2),
            StartColor = a,
            EndColor = b,
        };
    }

    private static void AddRoundRect(PathF p, float x, float y, float w, float h, float r)
    {
        r = Math.Min(r, Math.Min(w, h) / 2f);
        p.MoveTo(x + r, y);
        p.LineTo(x + w - r, y);
        p.QuadTo(x + w, y, x + w, y + r);
        p.LineTo(x + w, y + h - r);
        p.QuadTo(x + w, y + h, x + w - r, y + h);
        p.LineTo(x + r, y + h);
        p.QuadTo(x, y + h, x, y + h - r);
        p.LineTo(x, y + r);
        p.QuadTo(x, y, x + r, y);
        p.Close();
    }

    /// <summary>场景底色等处也要用（画布铺底）⇒ 暴露一个只读入口，避免第二份换算。</summary>
    internal static Color ColOf(uint argb) => Col(argb);

    /// <summary>
    /// **纯色填充的唯一入口** —— 直接用 <see cref="ICanvas"/> 铺底（场景背景那类）的地方也走它。
    ///
    /// 存在的理由只有一个：别让谁再写出裸的 `canvas.FillColor = …`。那句话本身没错，
    /// 错在它**清不掉上一个渐变挂上去的 shader**（见类注释「渐变的余荫」）——
    /// 而那是个"只有真机才看得见"的错。多包一层，规则就只有一条：
    /// **这条路上填色一律经过 <c>SetFillPaint</c>**。
    /// </summary>
    internal static void FillSolid(ICanvas canvas, uint argb, RectF rect)
    {
        canvas.SetFillPaint(new SolidPaint(Col(argb)), rect);
        canvas.FillRectangle(rect.X, rect.Y, rect.Width, rect.Height);
    }

    /// <summary>
    /// `0xAARRGGBB`（VML 的颜色序，与 <c>RasterImage.ColorAt</c> 一致）→ 平台颜色（**带缓存**）。
    ///
    /// ⚠ 缓存是必要的：`Color.FromRgba` **每次分配一个 `Color` 对象**，而这条路上一帧要调
    ///   几百次（每个矩形 / 椭圆 / 描边 / 文字至少一次）—— 全是短命垃圾。
    ///   颜色是程序里写死的有限集合（gorilla 实测几十种），缓存下来既省分配又省换算。
    ///
    /// ⚠ 上限是**防"程序用计算出来的颜色"把内存撑爆**（比如按渐变位置采样上色）。
    ///   超了就退回"每次新算"—— 慢一点，但不会无界增长。颜色本身是不可变值语义，
    ///   缓存共享没有副作用。
    ///
    /// ⚠ 只在**绘制线程**（UI 线程）访问 —— 这条路上所有落笔都从 `Draw` 回调进来，
    ///   单线程访问字典是安全的。将来若有别的线程直接调它，这里要改成并发字典。
    /// </summary>
    private static Color Col(uint argb)
    {
        if (ColorCache.TryGetValue(argb, out var c)) return c;
        c = Color.FromRgba(
            (int)((argb >> 16) & 0xFF), (int)((argb >> 8) & 0xFF), (int)(argb & 0xFF), (int)((argb >> 24) & 0xFF));
        if (ColorCache.Count < 4096) ColorCache[argb] = c;
        return c;
    }

    private static readonly Dictionary<uint, Color> ColorCache = new();
}

/// <summary>
/// 矢量后端的**分指令耗时探针**（诊断件，默认关；`Enabled` 打开后才计时）。
///
/// ## 为什么非得分类量
///
/// 日志里那句「绘制 243ms」只是**总数**，而这条路上有七类互不相干的开销，
/// 它们**优化手段完全不同**：
///   · 原生图元（矩形/椭圆）—— 已是 `DrawRect`/`DrawOval`，无从再省；
///   · 通用填充 / 描边 / 蒙版 —— 要**逐点建平台路径**（每点一次 JNI），是候选大头；
///   · 文字 —— 每次调用重新排版（`DrawText` 的已知代价）；
///   · 贴图 —— 解码只在首次，之后是纯 blit。
/// 不分类就只能猜，而本仓的规矩是**先立基准、再按数据优化**（v0.96.176 那次
/// "先关抗锯齿再量"才找到 87% 的杠杆，就是同一个道理）。
///
/// ## 读法
///
/// `LogFrameStats` 每 30 帧打一行，随后 `Reset()` —— 所以那一行是**这 30 帧的合计**，
/// 除以 30 才是每帧。`×N` 是调用次数、`/N点` 是累计点数（判断"贵在点数还是贵在次数"）。
/// </summary>
internal static class VectorProbe
{
    public const int Slots = 14;

    /// <summary>
    /// **诊断开关，正式版保持 false**。
    ///
    /// 要查"绘制为什么慢"时把它改成 true，重新构建，然后
    /// `adb logcat -s WCVML` 就会在每 30 帧的分段行下面多打一行**分指令耗时**。
    /// 计时本身开销很小（每图元两次 `Stopwatch.GetTimestamp`，实测整帧 &lt;1ms），
    /// 但既然它只在诊断时有用，就不该常驻在每帧几千次的路径上。
    /// </summary>
    public static bool Enabled = false;

    public static readonly long[] Ticks = new long[Slots];
    public static readonly int[] Counts = new int[Slots];
    public static readonly long[] Points = new long[Slots];

    /// <summary>
    /// ⚠ 8~10 是**帧级**开销，不是"某个指令"：它们的存在是因为"绘制 440ms"
    /// 与"各指令加起来 70ms"曾经差了六倍 —— 差额必须有个去处，否则就只能靠猜。
    /// 建了这几格之后账才闭合：`画布状态 + 背景 + 遍历开销 + 各指令 ≈ LastDrawMs`。
    /// </summary>
    public static readonly string[] Names =
        ["矩形原生", "椭圆原生", "通用填充", "描边", "文字", "贴图", "裁剪蒙版", "建目标",
         "背景填充", "画布状态", "遍历开销", "绘图调用", "推拉坐标系", "查表"];

    public static long Begin() => Enabled ? System.Diagnostics.Stopwatch.GetTimestamp() : 0L;

    public static void End(int slot, long t0, int pts = 0)
    {
        if (!Enabled || t0 == 0L) return;
        Ticks[slot] += System.Diagnostics.Stopwatch.GetTimestamp() - t0;
        Counts[slot]++;
        Points[slot] += pts;
    }

    /// <summary>这一窗的分布；**一次都没画过则返回 null**（光栅后端下不该打空行）。</summary>
    public static string? Report()
    {
        var total = 0;
        for (var i = 0; i < Slots; i++) total += Counts[i];
        if (total == 0) return null;

        var perMs = (double)System.Diagnostics.Stopwatch.Frequency / 1000.0;
        var sb = new System.Text.StringBuilder("指令分布：");
        for (var i = 0; i < Slots; i++)
        {
            if (Counts[i] == 0) continue;
            sb.Append(' ').Append(Names[i]).Append(' ')
              .Append((Ticks[i] / perMs).ToString("F1")).Append("ms×").Append(Counts[i]);
            if (Points[i] > 0) sb.Append('/').Append(Points[i]).Append("点");
        }
        return sb.ToString();
    }

    public static void Reset()
    {
        Array.Clear(Ticks);
        Array.Clear(Counts);
        Array.Clear(Points);
    }
}
