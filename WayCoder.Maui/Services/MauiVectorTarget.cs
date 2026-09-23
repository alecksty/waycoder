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

    // ── 填充 / 描边 ────────────────────────────────────────────────────────

    public void FillShape(IReadOnlyList<IReadOnlyList<double>> subpaths, uint fill, Gradient? gradient,
        bool evenOdd) => FillShape(subpaths, fill, gradient, evenOdd, null);

    public void FillShape(IReadOnlyList<IReadOnlyList<double>> subpaths, uint fill, Gradient? gradient,
        bool evenOdd, (double MinX, double MinY, double MaxX, double MaxY)? box)
    {
        var path = new PathF();
        var any = false;
        foreach (var pts in subpaths)
        {
            if (pts.Count < 6) continue;
            path.MoveTo((float)pts[0], (float)pts[1]);
            for (var i = 2; i + 1 < pts.Count; i += 2)
                path.LineTo((float)pts[i], (float)pts[i + 1]);
            path.Close();
            any = true;
        }
        if (!any) return;

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
    }

    // ── 文本 / 贴图 ────────────────────────────────────────────────────────

    public void DrawText(double x, double y, string text, double size, uint color, string anchor, bool bold, bool italic)
    {
        if (string.IsNullOrEmpty(text)) return;
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
        try
        {
            _metricsProbe ??= new Android.Graphics.Paint();
            // ⚠ 绑定里 `Paint.Typeface` 是**只读**属性，只能走 SetTypeface（实测 CS0200）
            _metricsProbe.SetTypeface((bold ? GFont.DefaultBold : GFont.Default).ToTypeface());
            _metricsProbe.TextSize = (float)Math.Max(1, size);
            var m = _metricsProbe.GetFontMetrics();
            // Android 的 `Ascent` 是**负值**（基线以上的距离取负），故取反
            if (m is not null && m.Ascent < 0) return -m.Ascent;
        }
        catch { /* 取不到度量就退回近似值，绝不因此不画字 */ }
#endif
        return size * DrawParse.TextAscentRatio;
    }

#if ANDROID
    /// <summary>量字体度量用的画笔（只读度量，不落笔）。</summary>
    private Android.Graphics.Paint? _metricsProbe;
#endif

    public void DrawImage(string? path, double x, double y, double w, double h,
        double srcX, double srcY, double srcW, double srcH, double cornerRadius, bool transformed)
    {
        // 平台画布对"任意仿射"的支持不一（旋转/错切）⇒ 明说画不了，让宿主回退光栅后端
        if (transformed) { MarkUnsupported("image", "带旋转/错切的贴图"); return; }
        if (string.IsNullOrEmpty(path)) { MarkUnsupported("image", "只有内存位图、没有文件路径"); return; }
        var img = LoadImage(path);
        if (img == null) { MarkUnsupported("image", path); return; }

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

    /// <summary>`0xAARRGGBB`（VML 的颜色序，与 <c>RasterImage.ColorAt</c> 一致）→ 平台颜色。</summary>
    private static Color Col(uint argb) => Color.FromRgba(
        (int)((argb >> 16) & 0xFF), (int)((argb >> 8) & 0xFF), (int)(argb & 0xFF), (int)((argb >> 24) & 0xFF));
}
