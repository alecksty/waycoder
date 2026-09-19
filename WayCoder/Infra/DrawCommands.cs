using System.Globalization;
using System.Text;

namespace WayCoder.Infra;

/// <summary>
/// 内置绘图指令，经 [ModuleInitializer] 自动注册。
/// 每条指令实现 IDrawCommand，插件可仿照此自定义指令并注册到 DrawCommandRegistry。
/// 变换（translate/rotate/scale/push/pop）与渐变定义（gradient）在 DrawRunner.Parse 里作为状态处理，不在此处。
/// </summary>

internal static class DrawParse
{
    public static double Num(DrawToken t) => Canvas.TryNum(t.Value, out var v) ? v : double.NaN;
    /// <summary>格式化为 SVG 数值：NaN/Infinity 非法（会生成 "NaN"/"Infinity" 破坏 SVG 解析），钳为 0。</summary>
    public static string F(double v) => !double.IsFinite(v) ? "0" : Math.Abs(v) < 1e-9 ? "0" : v.ToString("0.###", CultureInfo.InvariantCulture);

    /// <summary>线头形状：butt/round/square（忽略大小写）。非三者返回 false 且 cap="butt"。</summary>
    public static bool TryCap(string s, out string cap)
    {
        var low = s.ToLowerInvariant();
        if (low is "butt" or "round" or "square") { cap = low; return true; }
        cap = "butt"; return false;
    }
    public static string EscapeXml(string s)
        => s.Replace("&", "&amp;").Replace("<", "&lt;").Replace(">", "&gt;").Replace("\"", "&quot;");

    /// <summary>取 Args 里的点，格式化为 SVG points="x,y x,y ..."。</summary>
    public static string Points(DrawFigure f)
    {
        var sb = new StringBuilder();
        for (int i = 0; i + 1 < f.Args.Count; i += 2)
        {
            if (i > 0) sb.Append(' ');
            sb.Append(F(f.Args[i])).Append(',').Append(F(f.Args[i + 1]));
        }
        return sb.ToString();
    }

    /// <summary>
    /// 从 a[start] 起解析尾部样式：第一个颜色→Fill（未设时）、后续颜色→Stroke、
    /// 数值→StrokeWidth、@id→GradientRef。用于填充形状的可选样式段。
    /// </summary>
    public static void ParseStyle(IReadOnlyList<DrawToken> a, int start, DrawFigure f, ref bool fillSet)
    {
        for (int i = start; i < a.Count; i++)
        {
            var s = a[i].Value;
            if (s.Length >= 2 && s[0] == '@') { f.GradientRef = s[1..]; fillSet = true; continue; }
            if (ColorUtil.TryParse(s, out var c))
            {
                if (!fillSet) { f.Fill = c; fillSet = true; }
                else f.Stroke = c;
            }
            else if (Canvas.TryNum(s, out var v)) f.StrokeWidth = v;
        }
    }

    /// <summary>
    /// **描边类**图元（`line` / `arrow` / `polyline`）的样式段：线帽 / 虚线 / 颜色 / 线宽。
    ///
    /// 为什么单独一个入口而不是复用 <see cref="ParseStyle"/>：这两类图元的**颜色语义不同** ——
    /// 填充形状是"第一个颜色=填充、第二个=描边"，而描边类**只有一个颜色位**（就是描边本身）。
    /// 硬套 `ParseStyle` 会让 `line 0 0 10 10 #f00` 把红色吃成"填充"、描边留在默认黑
    /// （`line` 不填充 ⇒ 屏幕上什么都看不见）。
    ///
    /// ⚠ **这三条以前各写了一份逐字相同的循环**（`LineCommand` / `ArrowCommand` / `PolylineCommand`），
    ///   属本仓头号坑「同一规则四处实现」：改一处不漏另两处，症状是
    ///   "多边形好使、折线不好使" —— 而只测矩形/折线中一条的自测**照不出来**。
    ///   收口成一处之后，加语法（显式 `stroke`/`width` 关键字、`@id` 描边刷子）
    ///   只需要动这一个函数。
    /// </summary>
    public static void ParseStrokeStyle(IReadOnlyList<DrawToken> a, int start, DrawFigure f)
    {
        for (int i = start; i < a.Count; i++)
        {
            if (TryCap(a[i].Value, out var cap)) { f.LineCap = cap; continue; }
            var low = a[i].Value.ToLowerInvariant();
            if (low is "dash" or "dashed") { f.Dashed = true; continue; }
            if (ColorUtil.TryParse(a[i].Value, out var c)) f.Stroke = c;
            else { var v = Num(a[i]); if (!double.IsNaN(v)) f.StrokeWidth = v; }
        }
    }

    /// <summary>fill（支持渐变 url(#id)）+ 可选 stroke/stroke-width 属性串。</summary>
    public static string FillStrokeAttrs(DrawFigure f)
    {
        var sb = new StringBuilder();
        sb.Append(" fill=\"").Append(f.GradientRef != null ? "url(#" + EscapeXml(f.GradientRef) + ")" : ColorUtil.ToHex(f.Fill)).Append('"');
        if (f.Stroke != 0)
        {
            sb.Append(" stroke=\"").Append(ColorUtil.ToHex(f.Stroke)).Append('"')
              .Append(" stroke-width=\"").Append(F(f.StrokeWidth)).Append('"')
              .Append(" stroke-linejoin=\"round\"");
        }
        return sb.ToString();
    }
}

/// <summary>几何点生成器（点列表 x,y 交替，局部坐标）。</summary>
internal static class DrawGeo
{
    public static string Pts(IReadOnlyList<double> pts)
    {
        var sb = new StringBuilder();
        for (int i = 0; i + 1 < pts.Count; i += 2)
        {
            if (i > 0) sb.Append(' ');
            sb.Append(DrawParse.F(pts[i])).Append(',').Append(DrawParse.F(pts[i + 1]));
        }
        return sb.ToString();
    }

    public static (double, double, double, double) BBox(IReadOnlyList<double> pts)
    {
        double minX = double.MaxValue, minY = double.MaxValue, maxX = double.MinValue, maxY = double.MinValue;
        for (int i = 0; i + 1 < pts.Count; i += 2)
        {
            minX = Math.Min(minX, pts[i]); maxX = Math.Max(maxX, pts[i]);
            minY = Math.Min(minY, pts[i + 1]); maxY = Math.Max(maxY, pts[i + 1]);
        }
        return (minX, minY, maxX, maxY);
    }

    public static List<double> Star(double cx, double cy, double R, double r, int n, double rotDeg)
    {
        var pts = new List<double>(n * 4);
        double rot = rotDeg * Math.PI / 180.0;
        for (int k = 0; k < 2 * n; k++)
        {
            double ang = -Math.PI / 2 + rot + k * Math.PI / n;
            double rad = (k % 2 == 0) ? R : r;
            pts.Add(cx + rad * Math.Cos(ang));
            pts.Add(cy + rad * Math.Sin(ang));
        }
        return pts;
    }

    public static List<double> Regular(double cx, double cy, double r, int n, double rotDeg)
    {
        var pts = new List<double>(n * 2);
        double rot = rotDeg * Math.PI / 180.0;
        for (int k = 0; k < n; k++)
        {
            double ang = -Math.PI / 2 + rot + k * 2 * Math.PI / n;
            pts.Add(cx + r * Math.Cos(ang));
            pts.Add(cy + r * Math.Sin(ang));
        }
        return pts;
    }

    public static List<double> Ellipse(double cx, double cy, double rx, double ry, int seg = 64)
    {
        var pts = new List<double>((seg + 1) * 2);
        for (int k = 0; k <= seg; k++)
        {
            double a = k * 2 * Math.PI / seg;
            pts.Add(cx + rx * Math.Cos(a));
            pts.Add(cy + ry * Math.Sin(a));
        }
        return pts;
    }

    public static List<double> Ring(double cx, double cy, double R, double r, int seg = 64)
    {
        var pts = new List<double>((seg + 1) * 4);
        for (int k = 0; k <= seg; k++)
        {
            double a = k * 2 * Math.PI / seg;
            pts.Add(cx + R * Math.Cos(a));
            pts.Add(cy + R * Math.Sin(a));
        }
        for (int k = seg; k >= 0; k--)
        {
            double a = k * 2 * Math.PI / seg;
            pts.Add(cx + r * Math.Cos(a));
            pts.Add(cy + r * Math.Sin(a));
        }
        return pts;
    }

    public static List<double> RoundRect(double x, double y, double w, double h, double r, int seg = 12)
    {
        double rr = Math.Max(0, Math.Min(r, Math.Min(w, h) / 2));
        var pts = new List<double>();
        void Arc(double cx, double cy, double a0, double a1)
        {
            for (int k = 0; k <= seg; k++)
            {
                double a = a0 + (a1 - a0) * k / seg;
                pts.Add(cx + rr * Math.Cos(a));
                pts.Add(cy + rr * Math.Sin(a));
            }
        }
        Arc(x + w - rr, y + rr, -Math.PI / 2, 0);
        Arc(x + w - rr, y + h - rr, 0, Math.PI / 2);
        Arc(x + rr, y + h - rr, Math.PI / 2, Math.PI);
        Arc(x + rr, y + rr, Math.PI, 3 * Math.PI / 2);
        return pts;
    }

    public static List<double> Pie(double cx, double cy, double r, double a0, double a1, int seg = 64)
    {
        var pts = new List<double>();
        pts.Add(cx); pts.Add(cy);
        double span = a1 - a0;
        int n = Math.Max(2, (int)Math.Ceiling(seg * Math.Min(1, Math.Abs(span) / 360.0)));
        for (int k = 0; k <= n; k++)
        {
            double a = (a0 + span * k / n) * Math.PI / 180.0;
            pts.Add(cx + r * Math.Cos(a));
            pts.Add(cy + r * Math.Sin(a));
        }
        return pts;
    }

    public static List<double> Heart(double cx, double cy, double size, int seg = 64)
    {
        double s = size / 29.0;
        var pts = new List<double>(seg * 2);
        for (int k = 0; k < seg; k++)
        {
            double t = k * 2 * Math.PI / seg;
            double px = 16 * Math.Pow(Math.Sin(t), 3);
            double py = 13 * Math.Cos(t) - 5 * Math.Cos(2 * t) - 2 * Math.Cos(3 * t) - Math.Cos(4 * t);
            pts.Add(cx + px * s);
            pts.Add(cy + (py + 2.5) * s);
        }
        return pts;
    }
}

/// <summary>多边形填充形状的共享光栅化（变换 + 渐变 + 描边）。</summary>
internal static class DrawFill
{
    /// <summary>
    /// "等比缩放 + 平移"（无旋转/错切、两轴同倍率）时取出倍率与平移量。
    ///
    /// 有这个接缝的理由：抗锯齿 = 整幅放大 3 倍再降采样，等于给每个图元挂 <c>Scale(3,3)</c>；
    /// 而各图元一旦发现变换不是恒等，就退到 <c>FillTransformed</c> 那条**逐像素布尔判定**的路
    /// （圆角矩形按 40 点多边形判、圆按距离判），代价是每像素一次 O(点数) —— 实测 120 个
    /// 圆角矩形在 3× 下要 1.1 秒。等比缩放的形状缩完还是同一个形状，直接乘倍率走整数扫描线
    /// 路径即可。恒等变换也走这条路（s=1、无平移），所以调用方**不必再单独判 IsIdentity**。
    /// </summary>
    public static bool TryScaled(Affine t, out double s, out double tx, out double ty)
    {
        s = 0; tx = t.E; ty = t.F;
        if (!t.TryAxisScale(out var sx, out var sy) || sx != sy) return false;
        s = sx;
        return true;
    }

    public static void Polygon(Canvas c, IReadOnlyList<double> pts, DrawFigure f)
    {
        if (f.Transform.IsIdentity && f.Gradient == null)
        {
            c.FillPolygon(pts, f.Fill);
        }
        else
        {
            var (minX, minY, maxX, maxY) = DrawGeo.BBox(pts);
            c.FillTransformed(f.Transform, minX, minY, maxX, maxY,
                (lx, ly) => Canvas.PointInPolygon(lx, ly, pts), f.Fill, f.Gradient);
        }
    }

    public static void Stroke(Canvas c, IReadOnlyList<double> pts, DrawFigure f)
    {
        if (f.Stroke != 0)
            c.StrokePolygon(Canvas.TransformPoints(f.Transform, pts), f.StrokeWidth, f.Stroke);
    }
}

/// <summary>rect x y w h [fill] [stroke] [width]</summary>
internal sealed partial class RectCommand : IDrawCommand
{
    public string Name => "rect";
    public DrawFigure? Parse(IReadOnlyList<DrawToken> a)
    {
        if (a.Count < 4) return null;
        var f = new DrawFigure { Kind = "rect" };
        for (int i = 0; i < 4; i++) f.Args.Add(DrawParse.Num(a[i]));
        bool fillSet = false;
        DrawParse.ParseStyle(a, 4, f, ref fillSet);
        return f;
    }
    public void EmitSvg(StringBuilder sb, DrawFigure f)
        => sb.Append("  <rect x=\"").Append(DrawParse.F(f.Args[0])).Append("\" y=\"").Append(DrawParse.F(f.Args[1]))
          .Append("\" width=\"").Append(DrawParse.F(f.Args[2])).Append("\" height=\"").Append(DrawParse.F(f.Args[3]))
          .Append('"').Append(DrawParse.FillStrokeAttrs(f)).Append("/>\n");
    public void Rasterize(Canvas c, DrawFigure f)
    {
        double x = f.Args[0], y = f.Args[1], w = f.Args[2], h = f.Args[3];
        if (f.Gradient == null && DrawFill.TryScaled(f.Transform, out var s, out var tx, out var ty))
            c.FillRect((int)Math.Round(tx + x * s), (int)Math.Round(ty + y * s),
                (int)Math.Round(w * s), (int)Math.Round(h * s), f.Fill);
        else
            c.FillTransformed(f.Transform, x, y, x + w, y + h,
                (lx, ly) => lx >= x && lx <= x + w && ly >= y && ly <= y + h, f.Fill, f.Gradient);
        DrawFill.Stroke(c, new double[] { x, y, x + w, y, x + w, y + h, x, y + h }, f);
    }
}

/// <summary>roundrect x y w h r [fill] [stroke] [width]</summary>
internal sealed partial class RoundRectCommand : IDrawCommand
{
    public string Name => "roundrect";
    public DrawFigure? Parse(IReadOnlyList<DrawToken> a)
    {
        if (a.Count < 5) return null;
        var f = new DrawFigure { Kind = "roundrect" };
        for (int i = 0; i < 5; i++) f.Args.Add(DrawParse.Num(a[i]));
        bool fillSet = false;
        DrawParse.ParseStyle(a, 5, f, ref fillSet);
        return f;
    }
    public void EmitSvg(StringBuilder sb, DrawFigure f)
        => sb.Append("  <rect x=\"").Append(DrawParse.F(f.Args[0])).Append("\" y=\"").Append(DrawParse.F(f.Args[1]))
          .Append("\" width=\"").Append(DrawParse.F(f.Args[2])).Append("\" height=\"").Append(DrawParse.F(f.Args[3]))
          .Append("\" rx=\"").Append(DrawParse.F(f.Args[4]))
          .Append('"').Append(DrawParse.FillStrokeAttrs(f)).Append("/>\n");
    public void Rasterize(Canvas c, DrawFigure f)
    {
        double x = f.Args[0], y = f.Args[1], w = f.Args[2], h = f.Args[3], r = f.Args[4];
        if (f.Gradient == null && DrawFill.TryScaled(f.Transform, out var s, out var tx, out var ty))
        {
            c.FillRoundRect(tx + x * s, ty + y * s, w * s, h * s, r * s, f.Fill);
        }
        else
        {
            var pts = DrawGeo.RoundRect(x, y, w, h, r);
            var (minX, minY, maxX, maxY) = DrawGeo.BBox(pts);
            c.FillTransformed(f.Transform, minX, minY, maxX, maxY,
                (lx, ly) => Canvas.PointInPolygon(lx, ly, pts), f.Fill, f.Gradient);
        }
        DrawFill.Stroke(c, DrawGeo.RoundRect(x, y, w, h, r), f);
    }
}

/// <summary>circle cx cy r [fill] [stroke] [width]</summary>
internal sealed partial class CircleCommand : IDrawCommand
{
    public string Name => "circle";
    public DrawFigure? Parse(IReadOnlyList<DrawToken> a)
    {
        if (a.Count < 3) return null;
        var f = new DrawFigure { Kind = "circle" };
        for (int i = 0; i < 3; i++) f.Args.Add(DrawParse.Num(a[i]));
        bool fillSet = false;
        DrawParse.ParseStyle(a, 3, f, ref fillSet);
        return f;
    }
    public void EmitSvg(StringBuilder sb, DrawFigure f)
        => sb.Append("  <circle cx=\"").Append(DrawParse.F(f.Args[0])).Append("\" cy=\"").Append(DrawParse.F(f.Args[1]))
          .Append("\" r=\"").Append(DrawParse.F(f.Args[2]))
          .Append('"').Append(DrawParse.FillStrokeAttrs(f)).Append("/>\n");
    public void Rasterize(Canvas c, DrawFigure f)
    {
        double cx = f.Args[0], cy = f.Args[1], r = f.Args[2];
        if (f.Gradient == null && DrawFill.TryScaled(f.Transform, out var s, out var tx, out var ty))
            c.FillCircle(tx + cx * s, ty + cy * s, r * s, f.Fill);
        else
            c.FillTransformed(f.Transform, cx - r, cy - r, cx + r, cy + r,
                (lx, ly) => { double dx = lx - cx, dy = ly - cy; return dx * dx + dy * dy <= r * r; },
                f.Fill, f.Gradient);
        DrawFill.Stroke(c, DrawGeo.Regular(cx, cy, r, 64, 0), f);
    }
}

/// <summary>ellipse cx cy rx ry [fill] [stroke] [width]</summary>
internal sealed partial class EllipseCommand : IDrawCommand
{
    public string Name => "ellipse";
    public DrawFigure? Parse(IReadOnlyList<DrawToken> a)
    {
        if (a.Count < 4) return null;
        var f = new DrawFigure { Kind = "ellipse" };
        for (int i = 0; i < 4; i++) f.Args.Add(DrawParse.Num(a[i]));
        bool fillSet = false;
        DrawParse.ParseStyle(a, 4, f, ref fillSet);
        return f;
    }
    public void EmitSvg(StringBuilder sb, DrawFigure f)
        => sb.Append("  <ellipse cx=\"").Append(DrawParse.F(f.Args[0])).Append("\" cy=\"").Append(DrawParse.F(f.Args[1]))
          .Append("\" rx=\"").Append(DrawParse.F(f.Args[2])).Append("\" ry=\"").Append(DrawParse.F(f.Args[3]))
          .Append('"').Append(DrawParse.FillStrokeAttrs(f)).Append("/>\n");
    public void Rasterize(Canvas c, DrawFigure f)
    {
        double cx = f.Args[0], cy = f.Args[1], rx = f.Args[2], ry = f.Args[3];
        if (f.Gradient == null && DrawFill.TryScaled(f.Transform, out var s, out var tx, out var ty))
            c.FillEllipse(tx + cx * s, ty + cy * s, rx * s, ry * s, f.Fill);
        else
            c.FillTransformed(f.Transform, cx - rx, cy - ry, cx + rx, cy + ry,
                (lx, ly) => { double dx = (lx - cx) / rx, dy = (ly - cy) / ry; return dx * dx + dy * dy <= 1; },
                f.Fill, f.Gradient);
        DrawFill.Stroke(c, DrawGeo.Ellipse(cx, cy, rx, ry, 64), f);
    }
}

/// <summary>line x1 y1 x2 y2 [color] [width]</summary>
internal sealed partial class LineCommand : IDrawCommand
{
    public string Name => "line";
    public DrawFigure? Parse(IReadOnlyList<DrawToken> a)
    {
        if (a.Count < 4) return null;
        var f = new DrawFigure { Kind = "line", Stroke = 0xFF000000 };
        for (int i = 0; i < 4; i++) f.Args.Add(DrawParse.Num(a[i]));
        DrawParse.ParseStrokeStyle(a, 4, f);
        return f;
    }
    public void EmitSvg(StringBuilder sb, DrawFigure f)
    {
        sb.Append("  <line x1=\"").Append(DrawParse.F(f.Args[0])).Append("\" y1=\"").Append(DrawParse.F(f.Args[1]))
          .Append("\" x2=\"").Append(DrawParse.F(f.Args[2])).Append("\" y2=\"").Append(DrawParse.F(f.Args[3]))
          .Append("\" stroke=\"").Append(ColorUtil.ToHex(f.Stroke))
          .Append("\" stroke-width=\"").Append(DrawParse.F(f.StrokeWidth)).Append("\" stroke-linecap=\"").Append(f.LineCap).Append("\"");
        if (f.Dashed) sb.Append(" stroke-dasharray=\"6 4\"");
        sb.Append("/>\n");
    }
    public void Rasterize(Canvas c, DrawFigure f)
    {
        double x1 = f.Args[0], y1 = f.Args[1], x2 = f.Args[2], y2 = f.Args[3];
        var (wx1, wy1) = f.Transform.Apply(x1, y1);
        var (wx2, wy2) = f.Transform.Apply(x2, y2);
        if (f.Dashed) c.DrawLineDashed(wx1, wy1, wx2, wy2, f.Stroke, f.StrokeWidth, f.LineCap);
        else c.DrawLine(wx1, wy1, wx2, wy2, f.Stroke, f.StrokeWidth, f.LineCap);
    }
}

/// <summary>arrow x1 y1 x2 y2 [color] [width]</summary>
internal sealed partial class ArrowCommand : IDrawCommand
{
    public string Name => "arrow";
    public DrawFigure? Parse(IReadOnlyList<DrawToken> a)
    {
        if (a.Count < 4) return null;
        var f = new DrawFigure { Kind = "arrow", Stroke = 0xFF000000 };
        for (int i = 0; i < 4; i++) f.Args.Add(DrawParse.Num(a[i]));
        DrawParse.ParseStrokeStyle(a, 4, f);
        return f;
    }
    public void EmitSvg(StringBuilder sb, DrawFigure f)
    {
        double x1 = f.Args[0], y1 = f.Args[1], x2 = f.Args[2], y2 = f.Args[3];
        var (hx1, hy1, hx2, hy2) = Head(x1, y1, x2, y2, f.StrokeWidth);
        sb.Append("  <line x1=\"").Append(DrawParse.F(x1)).Append("\" y1=\"").Append(DrawParse.F(y1))
          .Append("\" x2=\"").Append(DrawParse.F(x2)).Append("\" y2=\"").Append(DrawParse.F(y2))
          .Append("\" stroke=\"").Append(ColorUtil.ToHex(f.Stroke))
          .Append("\" stroke-width=\"").Append(DrawParse.F(f.StrokeWidth)).Append("\" stroke-linecap=\"").Append(f.LineCap).Append("\"");
        if (f.Dashed) sb.Append(" stroke-dasharray=\"6 4\"");
        sb.Append("/>\n");
        sb.Append("  <polygon points=\"").Append(DrawParse.F(x2)).Append(',').Append(DrawParse.F(y2)).Append(' ')
          .Append(DrawParse.F(hx1)).Append(',').Append(DrawParse.F(hy1)).Append(' ')
          .Append(DrawParse.F(hx2)).Append(',').Append(DrawParse.F(hy2))
          .Append("\" fill=\"").Append(ColorUtil.ToHex(f.Stroke)).Append("\"/>\n");
    }
    public void Rasterize(Canvas c, DrawFigure f)
    {
        double x1 = f.Args[0], y1 = f.Args[1], x2 = f.Args[2], y2 = f.Args[3];
        var (hx1, hy1, hx2, hy2) = Head(x1, y1, x2, y2, f.StrokeWidth);
        var a = f.Transform.Apply(x1, y1);
        var b = f.Transform.Apply(x2, y2);
        var c1 = f.Transform.Apply(hx1, hy1);
        var c2 = f.Transform.Apply(hx2, hy2);
        if (f.Dashed) c.DrawLineDashed(a.X, a.Y, b.X, b.Y, f.Stroke, f.StrokeWidth, f.LineCap);
        else c.DrawLine(a.X, a.Y, b.X, b.Y, f.Stroke, f.StrokeWidth, f.LineCap);
        c.DrawLine(b.X, b.Y, c1.X, c1.Y, f.Stroke, f.StrokeWidth, f.LineCap);
        c.DrawLine(b.X, b.Y, c2.X, c2.Y, f.Stroke, f.StrokeWidth, f.LineCap);
    }
    static (double, double, double, double) Head(double x1, double y1, double x2, double y2, double width)
    {
        double angle = Math.Atan2(y2 - y1, x2 - x1);
        double len = Math.Max(8, width * 4);
        double spread = Math.PI / 7;
        double a1 = angle + Math.PI - spread, a2 = angle + Math.PI + spread;
        return (x2 + len * Math.Cos(a1), y2 + len * Math.Sin(a1), x2 + len * Math.Cos(a2), y2 + len * Math.Sin(a2));
    }
}

/// <summary>polygon x1 y1 x2 y2 ... [fill] [stroke] [width]（偶数个点 + 可选样式段）</summary>
internal sealed partial class PolygonCommand : IDrawCommand
{
    public string Name => "polygon";
    public DrawFigure? Parse(IReadOnlyList<DrawToken> a)
    {
        if (a.Count < 6) return null;
        var f = new DrawFigure { Kind = "polygon" };
        int i = 0;
        while (i < a.Count && Canvas.TryNum(a[i].Value, out var v)) { f.Args.Add(v); i++; }
        if (f.Args.Count < 6 || f.Args.Count % 2 != 0) return null;
        bool fillSet = false;
        DrawParse.ParseStyle(a, i, f, ref fillSet);
        return f;
    }
    public void EmitSvg(StringBuilder sb, DrawFigure f)
        => sb.Append("  <polygon points=\"").Append(DrawParse.Points(f))
          .Append('"').Append(DrawParse.FillStrokeAttrs(f)).Append("/>\n");
    public void Rasterize(Canvas c, DrawFigure f)
    {
        DrawFill.Polygon(c, f.Args, f);
        DrawFill.Stroke(c, f.Args, f);
    }
}

/// <summary>polyline x1 y1 x2 y2 ... [color] [width]（偶数个点 + 可选样式段）</summary>
internal sealed partial class PolylineCommand : IDrawCommand
{
    public string Name => "polyline";
    public DrawFigure? Parse(IReadOnlyList<DrawToken> a)
    {
        if (a.Count < 4) return null;
        var f = new DrawFigure { Kind = "polyline", Stroke = 0xFF000000 };
        int i = 0;
        while (i < a.Count && Canvas.TryNum(a[i].Value, out var v)) { f.Args.Add(v); i++; }
        if (f.Args.Count < 4 || f.Args.Count % 2 != 0) return null;
        DrawParse.ParseStrokeStyle(a, i, f);
        return f;
    }
    public void EmitSvg(StringBuilder sb, DrawFigure f)
    {
        sb.Append("  <polyline points=\"").Append(DrawParse.Points(f))
          .Append("\" fill=\"none\" stroke=\"").Append(ColorUtil.ToHex(f.Stroke))
          .Append("\" stroke-width=\"").Append(DrawParse.F(f.StrokeWidth)).Append("\" stroke-linecap=\"").Append(f.LineCap).Append("\"");
        if (f.Dashed) sb.Append(" stroke-dasharray=\"6 4\"");
        sb.Append("/>\n");
    }
    public void Rasterize(Canvas c, DrawFigure f)
    {
        var w = Canvas.TransformPoints(f.Transform, f.Args);
        for (int i = 0; i + 2 < w.Length; i += 2)
        {
            if (f.Dashed) c.DrawLineDashed(w[i], w[i + 1], w[i + 2], w[i + 3], f.Stroke, f.StrokeWidth, f.LineCap);
            else c.DrawLine(w[i], w[i + 1], w[i + 2], w[i + 3], f.Stroke, f.StrokeWidth, f.LineCap);
        }
    }
}

/// <summary>
/// path "d" [stroke色] [width] [cap] [dash] [fill 色|@渐变id]
///
/// ## 曲线（v0.96.176）
///
/// 解析与展平在 <see cref="DrawPath"/>（纯数学、可自测），这里只管"展平结果怎么落到像素上"。
/// 原先光栅化只认 `M`/`L`/`Z`、**曲线段被静默丢掉**，而 <see cref="EmitSvg"/> 把整条 `d`
/// 原样交给矢量后端 —— 于是**同一份 DSL 导出 PNG 与导出 SVG 图形不一样**。
/// 现在两条路都走同一个展平器，曲线在两边形状一致。
///
/// ## 填充
///
/// 老写法「裸颜色 = 描边」保持不动（桌面 `draw` 工具与既有脚本都这么用）。要填充得显式写
/// `fill &lt;颜色|@渐变id&gt;`。多个子路径之间按**奇偶规则**挖洞（`M…Z M…Z` 画圆环那种）。
/// </summary>
internal sealed partial class PathCommand : IDrawCommand
{
    public string Name => "path";
    public DrawFigure? Parse(IReadOnlyList<DrawToken> a)
    {
        if (a.Count < 1) return null;
        var f = new DrawFigure { Kind = "path", Stroke = 0xFF000000, Text = a[0].Value, FillSet = false };
        for (int i = 1; i < a.Count; i++)
        {
            var tok = a[i].Value;
            if (tok.Equals("fill", StringComparison.OrdinalIgnoreCase) && i + 1 < a.Count)
            {
                var v = a[++i].Value;
                if (v.Length >= 2 && v[0] == '@') f.GradientRef = v[1..];
                else if (ColorUtil.TryParse(v, out var fc)) { f.Fill = fc; f.FillSet = true; }
                continue;
            }
            if (tok.Equals("dash", StringComparison.OrdinalIgnoreCase) || tok.Equals("dashed", StringComparison.OrdinalIgnoreCase))
            { f.Dashed = true; continue; }
            if (DrawParse.TryCap(tok, out var cap)) { f.LineCap = cap; continue; }
            if (tok.Length >= 2 && tok[0] == '@') { f.GradientRef = tok[1..]; continue; }
            if (ColorUtil.TryParse(tok, out var c)) f.Stroke = c;
            else { var v = DrawParse.Num(a[i]); if (!double.IsNaN(v)) f.StrokeWidth = v; }
        }
        return f;
    }

    public void EmitSvg(StringBuilder sb, DrawFigure f)
    {
        var fill = f.GradientRef != null ? "url(#" + DrawParse.EscapeXml(f.GradientRef) + ")"
                 : f.FillSet ? ColorUtil.ToHex(f.Fill) : "none";
        sb.Append("  <path d=\"").Append(DrawParse.EscapeXml(f.Text ?? ""))
          .Append("\" fill=\"").Append(fill)
          .Append("\" stroke=\"").Append(ColorUtil.ToHex(f.Stroke))
          .Append("\" stroke-width=\"").Append(DrawParse.F(f.StrokeWidth)).Append("\" stroke-linecap=\"").Append(f.LineCap).Append('"');
        if (f.Dashed) sb.Append(" stroke-dasharray=\"6 4\"");
        sb.Append("/>\n");
    }

    public void Rasterize(Canvas c, DrawFigure f)
    {
        var subs = DrawPath.Flatten(f.Text);
        if (subs.Count == 0) return;

        // 填充：多子路径按**奇偶规则**挖洞 ⇒ 被奇数条子路径包含的点才算在图形内。
        // 交给 FillTransformed（它自带变换与渐变采样），把"点内测试"作为委托传进去。
        if (f.FillSet || f.GradientRef != null)
        {
            ComputeBounds(subs, out var minX, out var minY, out var maxX, out var maxY);
            var polys = new List<double[]>(subs.Count);
            foreach (var sp in subs)
            {
                var arr = new double[sp.Points.Count * 2];
                for (int i = 0; i < sp.Points.Count; i++) { arr[i * 2] = sp.Points[i].X; arr[i * 2 + 1] = sp.Points[i].Y; }
                polys.Add(arr);
            }
            c.FillTransformed(f.Transform, minX, minY, maxX, maxY, (lx, ly) =>
            {
                bool inside = false;
                foreach (var p in polys)
                    if (Canvas.PointInPolygon(lx, ly, p)) inside = !inside;
                return inside;
            }, f.Fill, f.Gradient);
        }

        // 描边：逐子路径折线
        if (f.StrokeWidth <= 0) return;
        foreach (var sp in subs)
        {
            var pts = new List<double>(sp.Points.Count * 2);
            foreach (var p in sp.Points)
            {
                var q = f.Transform.Apply(p.X, p.Y);
                pts.Add(q.X); pts.Add(q.Y);
            }
            if (pts.Count < 4) continue;
            for (int i = 0; i + 3 < pts.Count; i += 2)
            {
                if (f.Dashed) c.DrawLineDashed(pts[i], pts[i + 1], pts[i + 2], pts[i + 3], f.Stroke, f.StrokeWidth, f.LineCap);
                else c.DrawLine(pts[i], pts[i + 1], pts[i + 2], pts[i + 3], f.Stroke, f.StrokeWidth, f.LineCap);
            }
        }
    }

    static void ComputeBounds(List<DrawPath.SubPath> subs, out double minX, out double minY, out double maxX, out double maxY)
    {
        minX = double.MaxValue; minY = double.MaxValue;
        maxX = double.MinValue; maxY = double.MinValue;
        foreach (var sp in subs)
            foreach (var p in sp.Points)
            {
                if (p.X < minX) minX = p.X;
                if (p.Y < minY) minY = p.Y;
                if (p.X > maxX) maxX = p.X;
                if (p.Y > maxY) maxY = p.Y;
            }
    }
}

/// <summary>text x y "内容" [size] [color] [anchor] [bold|italic|bolditalic] [fontFamily]</summary>
internal sealed partial class TextCommand : IDrawCommand
{
    public string Name => "text";
    public DrawFigure? Parse(IReadOnlyList<DrawToken> a)
    {
        if (a.Count < 3) return null;
        var f = new DrawFigure { Kind = "text" };
        f.Args.Add(DrawParse.Num(a[0]));
        f.Args.Add(DrawParse.Num(a[1]));
        f.Text = a[2].Value.Replace("\\n", "\n"); // DSL 里 \n 转义 → 真实换行，供多行文字
        for (int i = 3; i < a.Count; i++)
        {
            var s = a[i].Value;
            // ⚠ **`@id` 必须排在最后那个"其余裸词视为字体族名"的兜底之前** ——
            //    原先没有这一支，于是 `text 10 20 "hi" @g1` 里那个 `@g1` 落进兜底、
            //    被当成**字体族名**（`FontFamily = "@g1"`），而填充保持默认黑：
            //    渐变不但没生效，连"名字去哪了"都看不出来（字体找不到会静默回退默认字体）。
            //    与 `polyline` 的 `@id` 被丢是同一族问题 —— 静默丢失。
            if (s.Length >= 2 && s[0] == '@') { f.GradientRef = s[1..]; continue; }
            if (ColorUtil.TryParse(s, out var c)) { f.Fill = c; continue; }
            if (Canvas.TryNum(s, out var v)) { f.FontSize = v; continue; }
            var low = s.ToLowerInvariant();
            if (low is "start" or "middle" or "end") { f.Anchor = low; continue; }
            if (low is "bold" or "b") { f.FontWeight = "bold"; continue; }
            if (low is "italic" or "i") { f.FontStyle = "italic"; continue; }
            if (low is "bolditalic" or "bold-italic" or "bi") { f.FontWeight = "bold"; f.FontStyle = "italic"; continue; }
            f.FontFamily = s; // 其余裸词视为字体族名
        }
        return f;
    }
    public void EmitSvg(StringBuilder sb, DrawFigure f)
    {
        // 多行文字：按 \n 拆分为多个 <tspan>（x 对齐锚点，dy 逐行下移），单行保持原样。
        var lines = (f.Text ?? "").Split('\n');
        sb.Append("  <text x=\"").Append(DrawParse.F(f.Args[0])).Append("\" y=\"").Append(DrawParse.F(f.Args[1]))
          .Append("\" font-family=\"").Append(DrawParse.EscapeXml(f.FontFamily))
          .Append("\" font-size=\"").Append(DrawParse.F(f.FontSize))
          .Append("\" font-weight=\"").Append(f.FontWeight)
          .Append("\" font-style=\"").Append(f.FontStyle)
          .Append("\" fill=\"").Append(ColorUtil.ToHex(f.Fill))
          .Append("\" text-anchor=\"").Append(f.Anchor).Append("\">");
        if (lines.Length == 1)
        {
            sb.Append(DrawParse.EscapeXml(lines[0])).Append("</text>\n");
            return;
        }
        double lineH = f.FontSize * 1.3;
        for (int i = 0; i < lines.Length; i++)
        {
            if (i > 0) sb.Append('\n').Append("    ");
            sb.Append("<tspan x=\"").Append(DrawParse.F(f.Args[0])).Append('"');
            if (i > 0) sb.Append(" dy=\"").Append(DrawParse.F(lineH)).Append('"');
            sb.Append('>').Append(DrawParse.EscapeXml(lines[i])).Append("</tspan>");
        }
        sb.Append("</text>\n");
    }
    public void Rasterize(Canvas c, DrawFigure f)
    {
        // 变换仅平移锚点，字重/斜体近似；旋转文字不支持（保持轴对齐）。
        // 均匀缩放（含超采样）通过 ScaleFactor 缩放字号，使 PNG 与 SVG 在 scale 下尺寸一致。
        var p = f.Transform.Apply(f.Args[0], f.Args[1]);
        double size = f.FontSize * f.Transform.ScaleFactor;
        // 优先 TrueType 系统字体（含字形抗锯齿），找不到则回退 5×7 位图。
        var font = TrueTypeFont.Resolve(f.FontFamily);
        var lines = (f.Text ?? "").Split('\n');
        double lineH = size * 1.3;
        for (int i = 0; i < lines.Length; i++)
        {
            double y = p.Y + lineH * i;
            if (font != null)
                font.Render(c, lines[i], p.X, y, size, f.Fill, f.Anchor,
                    f.FontWeight == "bold", f.FontStyle == "italic");
            else
                c.DrawText(p.X, y, lines[i], size, f.Fill, f.Anchor,
                    f.FontWeight == "bold", f.FontStyle == "italic");
        }
    }
}

/// <summary>star cx cy R r n [rot] [fill] [stroke] [width] — n 尖星</summary>
internal sealed partial class StarCommand : IDrawCommand
{
    public string Name => "star";
    public DrawFigure? Parse(IReadOnlyList<DrawToken> a)
    {
        if (a.Count < 5) return null;
        var f = new DrawFigure { Kind = "star" };
        for (int i = 0; i < 5; i++)
        {
            double v = DrawParse.Num(a[i]);
            if (double.IsNaN(v)) return null;
            f.Args.Add(v);
        }
        if ((int)Math.Round(f.Args[4]) < 2 || (int)Math.Round(f.Args[4]) > 4096) return null;
        int start = 5;
        if (a.Count > 5 && Canvas.TryNum(a[5].Value, out _)) { f.Args.Add(DrawParse.Num(a[5])); start = 6; }
        else f.Args.Add(0);
        bool fillSet = false;
        DrawParse.ParseStyle(a, start, f, ref fillSet);
        return f;
    }
    public void EmitSvg(StringBuilder sb, DrawFigure f)
    {
        var pts = DrawGeo.Star(f.Args[0], f.Args[1], f.Args[2], f.Args[3], (int)Math.Round(f.Args[4]), f.Args[5]);
        sb.Append("  <polygon points=\"").Append(DrawGeo.Pts(pts)).Append('"')
          .Append(DrawParse.FillStrokeAttrs(f)).Append("/>\n");
    }
    public void Rasterize(Canvas c, DrawFigure f)
    {
        var pts = DrawGeo.Star(f.Args[0], f.Args[1], f.Args[2], f.Args[3], (int)Math.Round(f.Args[4]), f.Args[5]);
        DrawFill.Polygon(c, pts, f);
        DrawFill.Stroke(c, pts, f);
    }
}

/// <summary>regular cx cy r n [rot] [fill] [stroke] [width] — 正 n 边形</summary>
internal sealed partial class RegularCommand : IDrawCommand
{
    public string Name => "regular";
    public DrawFigure? Parse(IReadOnlyList<DrawToken> a)
    {
        if (a.Count < 4) return null;
        var f = new DrawFigure { Kind = "regular" };
        for (int i = 0; i < 4; i++)
        {
            double v = DrawParse.Num(a[i]);
            if (double.IsNaN(v)) return null;
            f.Args.Add(v);
        }
        if ((int)Math.Round(f.Args[3]) < 3 || (int)Math.Round(f.Args[3]) > 4096) return null;
        int start = 4;
        if (a.Count > 4 && Canvas.TryNum(a[4].Value, out _)) { f.Args.Add(DrawParse.Num(a[4])); start = 5; }
        else f.Args.Add(0);
        bool fillSet = false;
        DrawParse.ParseStyle(a, start, f, ref fillSet);
        return f;
    }
    public void EmitSvg(StringBuilder sb, DrawFigure f)
    {
        var pts = DrawGeo.Regular(f.Args[0], f.Args[1], f.Args[2], (int)Math.Round(f.Args[3]), f.Args[4]);
        sb.Append("  <polygon points=\"").Append(DrawGeo.Pts(pts)).Append('"')
          .Append(DrawParse.FillStrokeAttrs(f)).Append("/>\n");
    }
    public void Rasterize(Canvas c, DrawFigure f)
    {
        var pts = DrawGeo.Regular(f.Args[0], f.Args[1], f.Args[2], (int)Math.Round(f.Args[3]), f.Args[4]);
        DrawFill.Polygon(c, pts, f);
        DrawFill.Stroke(c, pts, f);
    }
}

/// <summary>ring cx cy R r [fill] [stroke] [width] — 圆环（even-odd 挖孔）</summary>
internal sealed partial class RingCommand : IDrawCommand
{
    public string Name => "ring";
    public DrawFigure? Parse(IReadOnlyList<DrawToken> a)
    {
        if (a.Count < 4) return null;
        var f = new DrawFigure { Kind = "ring" };
        for (int i = 0; i < 4; i++) f.Args.Add(DrawParse.Num(a[i]));
        bool fillSet = false;
        DrawParse.ParseStyle(a, 4, f, ref fillSet);
        return f;
    }
    public void EmitSvg(StringBuilder sb, DrawFigure f)
    {
        var pts = DrawGeo.Ring(f.Args[0], f.Args[1], f.Args[2], f.Args[3]);
        sb.Append("  <polygon points=\"").Append(DrawGeo.Pts(pts)).Append("\" fill-rule=\"evenodd\"")
          .Append(DrawParse.FillStrokeAttrs(f)).Append("/>\n");
    }
    public void Rasterize(Canvas c, DrawFigure f)
    {
        var pts = DrawGeo.Ring(f.Args[0], f.Args[1], f.Args[2], f.Args[3]);
        DrawFill.Polygon(c, pts, f);
        DrawFill.Stroke(c, pts, f);
    }
}

/// <summary>pie cx cy r a0 a1 [fill] [stroke] [width] — 扇形（角度制，a0→a1 逆时针）</summary>
internal sealed partial class PieCommand : IDrawCommand
{
    public string Name => "pie";
    public DrawFigure? Parse(IReadOnlyList<DrawToken> a)
    {
        if (a.Count < 5) return null;
        var f = new DrawFigure { Kind = "pie" };
        for (int i = 0; i < 5; i++) f.Args.Add(DrawParse.Num(a[i]));
        bool fillSet = false;
        DrawParse.ParseStyle(a, 5, f, ref fillSet);
        return f;
    }
    public void EmitSvg(StringBuilder sb, DrawFigure f)
    {
        double cx = f.Args[0], cy = f.Args[1], r = f.Args[2], a0 = f.Args[3], a1 = f.Args[4];
        double span = a1 - a0;
        if (Math.Abs(span) >= 359.9)
        {
            sb.Append("  <circle cx=\"").Append(DrawParse.F(cx)).Append("\" cy=\"").Append(DrawParse.F(cy))
              .Append("\" r=\"").Append(DrawParse.F(r)).Append('"')
              .Append(DrawParse.FillStrokeAttrs(f)).Append("/>\n");
            return;
        }
        double r0 = a0 * Math.PI / 180, r1 = a1 * Math.PI / 180;
        double x0 = cx + r * Math.Cos(r0), y0 = cy + r * Math.Sin(r0);
        double x1 = cx + r * Math.Cos(r1), y1 = cy + r * Math.Sin(r1);
        int large = Math.Abs(span) > 180 ? 1 : 0;
        int sweep = span > 0 ? 1 : 0;
        sb.Append("  <path d=\"M ").Append(DrawParse.F(cx)).Append(' ').Append(DrawParse.F(cy))
          .Append(" L ").Append(DrawParse.F(x0)).Append(' ').Append(DrawParse.F(y0))
          .Append(" A ").Append(DrawParse.F(r)).Append(' ').Append(DrawParse.F(r)).Append(" 0 ")
          .Append(large).Append(' ').Append(sweep).Append(' ')
          .Append(DrawParse.F(x1)).Append(' ').Append(DrawParse.F(y1))
          .Append(" Z\"").Append(DrawParse.FillStrokeAttrs(f)).Append("/>\n");
    }
    public void Rasterize(Canvas c, DrawFigure f)
    {
        var pts = DrawGeo.Pie(f.Args[0], f.Args[1], f.Args[2], f.Args[3], f.Args[4]);
        DrawFill.Polygon(c, pts, f);
        DrawFill.Stroke(c, pts, f);
    }
}

/// <summary>heart x y size [fill] [stroke] [width] — 心形（参数式采样）</summary>
internal sealed partial class HeartCommand : IDrawCommand
{
    public string Name => "heart";
    public DrawFigure? Parse(IReadOnlyList<DrawToken> a)
    {
        if (a.Count < 3) return null;
        var f = new DrawFigure { Kind = "heart" };
        for (int i = 0; i < 3; i++) f.Args.Add(DrawParse.Num(a[i]));
        bool fillSet = false;
        DrawParse.ParseStyle(a, 3, f, ref fillSet);
        return f;
    }
    public void EmitSvg(StringBuilder sb, DrawFigure f)
    {
        var pts = DrawGeo.Heart(f.Args[0], f.Args[1], f.Args[2]);
        sb.Append("  <polygon points=\"").Append(DrawGeo.Pts(pts)).Append('"')
          .Append(DrawParse.FillStrokeAttrs(f)).Append("/>\n");
    }
    public void Rasterize(Canvas c, DrawFigure f)
    {
        var pts = DrawGeo.Heart(f.Args[0], f.Args[1], f.Args[2]);
        DrawFill.Polygon(c, pts, f);
        DrawFill.Stroke(c, pts, f);
    }
}

/// <summary>
/// image x y w h "路径" [crop sx sy sw sh] [round r] [rect] — 把 PNG/JPG/BMP 图片贴到画布（拉伸到 w×h）。
/// 可选裁剪：crop 裁源图子矩形（像素坐标）、round 裁目标圆角（圆角半径 r）、rect 显式直角矩形（默认）。
/// SVG 输入无法栅格化，仅 SVG 端透传。
/// </summary>
internal sealed partial class ImageCommand : IDrawCommand
{
    public string Name => "image";
    public DrawFigure? Parse(IReadOnlyList<DrawToken> a)
    {
        if (a.Count < 5) return null;
        var f = new DrawFigure { Kind = "image" };
        for (int i = 0; i < 4; i++)
        {
            double v = DrawParse.Num(a[i]);
            if (double.IsNaN(v)) return null;
            f.Args.Add(v);
        }
        f.Text = a[4].Value;
        f.ImagePath = a[4].Value;
        f.Image = ImageLoader.Load(a[4].Value);

        // 可选裁剪段：crop sx sy sw sh / round r / rect
        int i5 = 5;
        while (i5 < a.Count)
        {
            var kw = a[i5].Value.ToLowerInvariant();
            if (kw == "crop" && i5 + 4 < a.Count)
            {
                double sx = DrawParse.Num(a[i5 + 1]), sy = DrawParse.Num(a[i5 + 2]);
                double sw = DrawParse.Num(a[i5 + 3]), sh = DrawParse.Num(a[i5 + 4]);
                if (!double.IsNaN(sx) && !double.IsNaN(sy) && !double.IsNaN(sw) && !double.IsNaN(sh) && sw > 0 && sh > 0)
                {
                    f.SrcX = sx; f.SrcY = sy; f.SrcW = sw; f.SrcH = sh;
                }
                i5 += 5;
            }
            else if (kw == "round" && i5 + 1 < a.Count)
            {
                double r = DrawParse.Num(a[i5 + 1]);
                if (!double.IsNaN(r) && r >= 0) f.CornerRadius = r;
                i5 += 2;
            }
            else if (kw == "rect") { f.CornerRadius = 0; i5++; } // 显式直角矩形裁剪（默认）
            else break; // 未知 token，忽略后续
        }
        return f;
    }
    public void EmitSvg(StringBuilder sb, DrawFigure f)
    {
        double x = f.Args[0], y = f.Args[1], w = f.Args[2], h = f.Args[3];
        if (f.Image == null)
        {
            // 无法栅格化（svg 输入 / 加载失败）：SVG 端引用原路径
            sb.Append("  <image x=\"").Append(DrawParse.F(x)).Append("\" y=\"").Append(DrawParse.F(y))
              .Append("\" width=\"").Append(DrawParse.F(w)).Append("\" height=\"").Append(DrawParse.F(h))
              .Append("\" href=\"").Append(DrawParse.EscapeXml(f.ImagePath ?? "")).Append("\"/>\n");
            return;
        }
        // 重编码为 PNG 内嵌 data URI，保证 SVG 自包含（可离线打开）
        string href = "data:image/png;base64," + Convert.ToBase64String(PngEncoder.Encode(f.Image.Width, f.Image.Height, f.Image.Rgba));
        bool hasCrop = f.SrcW > 0 && f.SrcH > 0;
        if (f.CornerRadius <= 0 && !hasCrop)
        {
            sb.Append("  <image x=\"").Append(DrawParse.F(x)).Append("\" y=\"").Append(DrawParse.F(y))
              .Append("\" width=\"").Append(DrawParse.F(w)).Append("\" height=\"").Append(DrawParse.F(h))
              .Append("\" preserveAspectRatio=\"none\" href=\"").Append(href).Append("\"/>\n");
            return;
        }
        // 需要 clipPath：目标裁剪区域为 (x,y,w,h) 圆角矩形；源图子矩形映射到该区域
        string cid = f.ClipId ?? "imgClip";
        double imgW = f.Image.Width, imgH = f.Image.Height;
        double sw = hasCrop ? f.SrcW : imgW;
        double sh = hasCrop ? f.SrcH : imgH;
        double sx = hasCrop ? f.SrcX : 0;
        double sy = hasCrop ? f.SrcY : 0;
        double scaleW = w / sw, scaleH = h / sh;
        double ix = x - sx * scaleW, iy = y - sy * scaleH;
        double iw = imgW * scaleW, ih = imgH * scaleH;
        double rr = Math.Min(f.CornerRadius, Math.Min(w, h) / 2);
        sb.Append("  <clipPath id=\"").Append(cid).Append("\"><rect x=\"").Append(DrawParse.F(x))
          .Append("\" y=\"").Append(DrawParse.F(y)).Append("\" width=\"").Append(DrawParse.F(w))
          .Append("\" height=\"").Append(DrawParse.F(h));
        if (rr > 0) sb.Append("\" rx=\"").Append(DrawParse.F(rr)).Append("\" ry=\"").Append(DrawParse.F(rr));
        sb.Append("\"/></clipPath>\n");
        sb.Append("  <image x=\"").Append(DrawParse.F(ix)).Append("\" y=\"").Append(DrawParse.F(iy))
          .Append("\" width=\"").Append(DrawParse.F(iw)).Append("\" height=\"").Append(DrawParse.F(ih))
          .Append("\" preserveAspectRatio=\"none\" clip-path=\"url(#").Append(cid).Append(")\" href=\"").Append(href).Append("\"/>\n");
    }
    public void Rasterize(Canvas c, DrawFigure f)
    {
        if (f.Image == null) return; // svg 输入 / 加载失败：PNG 端跳过
        c.DrawImage(f.Image, f.Transform, f.Args[0], f.Args[1], f.Args[2], f.Args[3],
            f.SrcX, f.SrcY, f.SrcW, f.SrcH, f.CornerRadius);
    }
}

/// <summary>内置指令自动注册（AOT 无反射，随模块加载执行）。</summary>
internal static class DrawCommandInit
{
    [System.Runtime.CompilerServices.ModuleInitializer]
    internal static void Init()
    {
        DrawCommandRegistry.Register(new RectCommand());
        DrawCommandRegistry.Register(new RoundRectCommand());
        DrawCommandRegistry.Register(new CircleCommand());
        DrawCommandRegistry.Register(new EllipseCommand());
        DrawCommandRegistry.Register(new LineCommand());
        DrawCommandRegistry.Register(new ArrowCommand());
        DrawCommandRegistry.Register(new PolygonCommand());
        DrawCommandRegistry.Register(new PolylineCommand());
        DrawCommandRegistry.Register(new PathCommand());
        DrawCommandRegistry.Register(new TextCommand());
        DrawCommandRegistry.Register(new StarCommand());
        DrawCommandRegistry.Register(new RegularCommand());
        DrawCommandRegistry.Register(new RingCommand());
        DrawCommandRegistry.Register(new PieCommand());
        DrawCommandRegistry.Register(new HeartCommand());
        DrawCommandRegistry.Register(new ImageCommand());
    }
}
