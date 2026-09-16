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
///   这里要换算成文本框顶端（`基线 − 上升`）。用 0.8×字号 估，属于已知的细微差异；
/// · **渐变**：平台刷子按绝对几何铺，与光栅侧归一化采样的结果在小尺寸上可能差一两个色阶；
/// · **虚线**：平台按 `StrokeDashPattern` 走，相位从路径起点算 —— 与光栅的逐段绘制
///   在长折线上可能有半个周期的差。
///
/// 这些都是"观感差异"而不是"画错"，但值得在自测里留一条**抽样比对**的护栏。
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

    public void FillShape(IReadOnlyList<IReadOnlyList<double>> subpaths, uint fill, Gradient? gradient, bool evenOdd)
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

        if (gradient != null)
        {
            // 渐变按**整条路径的外接矩形**铺（平台刷子要一个 rect 来定位；与光栅侧的归一化几何同口径）
            _canvas.SetFillPaint(BuildPaint(gradient), path.Bounds);
        }
        else
        {
            _canvas.FillColor = Col(fill);
        }
        _canvas.FillPath(path, evenOdd ? WindingMode.EvenOdd : WindingMode.NonZero);
    }

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
        // 0.8×字号 是常见字体的上升比例；这条差异在自测里用抽样比对兜着（见类注释）。
        var top = y - size * 0.8;
        var align = anchor switch
        {
            "middle" => HorizontalAlignment.Center,
            "end" => HorizontalAlignment.Right,
            _ => HorizontalAlignment.Left,
        };
        // 平台没有"只给锚点"的 DrawString 重载，最近的是"给一个矩形 + 对齐方式"⇒
        // 矩形取"锚点往右到画布边"，垂直方向按文本框顶端对齐（top 已换算过基线）。
        var boxW = (float)Math.Max(1, SceneWidth - x);
        var boxH = (float)Math.Max(1, size * 2);
        _canvas.DrawString(text, (float)x, (float)top, boxW, boxH, align, VerticalAlignment.Top);
    }

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

    /// <summary>归一化渐变几何 → 平台刷子（按场景尺寸换算成绝对坐标）。</summary>
    private Paint BuildPaint(Gradient g)
    {
        var a = Col(g.ColorA);
        var b = Col(g.ColorB);
        if (g.Radial)
        {
            var cx = (float)(g.Cx * SceneWidth);
            var cy = (float)(g.Cy * SceneHeight);
            // 归一化半径按**长边**换算：圆在非方形画布上才不会被拉成椭圆（与光栅侧同一口径）
            var r = (float)(g.R * Math.Max(SceneWidth, SceneHeight));
            return new RadialGradientPaint
            {
                Center = new Point(cx, cy),
                Radius = Math.Max(1, r),
                StartColor = a,
                EndColor = b,
            };
        }
        return new LinearGradientPaint
        {
            StartPoint = new Point((float)(g.X1 * SceneWidth), (float)(g.Y1 * SceneHeight)),
            EndPoint = new Point((float)(g.X2 * SceneWidth), (float)(g.Y2 * SceneHeight)),
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

    /// <summary>`0xAARRGGBB`（VML 的颜色序，与 <c>RasterImage.ColorAt</c> 一致）→ 平台颜色。</summary>
    private static Color Col(uint argb) => Color.FromRgba(
        (int)((argb >> 16) & 0xFF), (int)((argb >> 8) & 0xFF), (int)(argb & 0xFF), (int)((argb >> 24) & 0xFF));
}
