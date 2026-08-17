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
    public static string F(double v) => Math.Abs(v) < 1e-9 ? "0" : v.ToString("0.###", CultureInfo.InvariantCulture);

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

    /// <summary>fill（支持渐变 url(#id)）+ 可选 stroke/stroke-width 属性串。</summary>
    public static string FillStrokeAttrs(DrawFigure f)
    {
        var sb = new StringBuilder();
        sb.Append(" fill=\"").Append(f.GradientRef != null ? "url(#" + f.GradientRef + ")" : ColorUtil.ToHex(f.Fill)).Append('"');
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
internal sealed class RectCommand : IDrawCommand
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
        if (f.Transform.IsIdentity && f.Gradient == null)
            c.FillRect((int)Math.Round(x), (int)Math.Round(y), (int)Math.Round(w), (int)Math.Round(h), f.Fill);
        else
            c.FillTransformed(f.Transform, x, y, x + w, y + h,
                (lx, ly) => lx >= x && lx <= x + w && ly >= y && ly <= y + h, f.Fill, f.Gradient);
        DrawFill.Stroke(c, new double[] { x, y, x + w, y, x + w, y + h, x, y + h }, f);
    }
}

/// <summary>roundrect x y w h r [fill] [stroke] [width]</summary>
internal sealed class RoundRectCommand : IDrawCommand
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
        if (f.Transform.IsIdentity && f.Gradient == null)
        {
            c.FillRoundRect(x, y, w, h, r, f.Fill);
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
internal sealed class CircleCommand : IDrawCommand
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
        if (f.Transform.IsIdentity && f.Gradient == null)
            c.FillCircle(cx, cy, r, f.Fill);
        else
            c.FillTransformed(f.Transform, cx - r, cy - r, cx + r, cy + r,
                (lx, ly) => { double dx = lx - cx, dy = ly - cy; return dx * dx + dy * dy <= r * r; },
                f.Fill, f.Gradient);
        DrawFill.Stroke(c, DrawGeo.Regular(cx, cy, r, 64, 0), f);
    }
}

/// <summary>ellipse cx cy rx ry [fill] [stroke] [width]</summary>
internal sealed class EllipseCommand : IDrawCommand
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
        if (f.Transform.IsIdentity && f.Gradient == null)
            c.FillEllipse(cx, cy, rx, ry, f.Fill);
        else
            c.FillTransformed(f.Transform, cx - rx, cy - ry, cx + rx, cy + ry,
                (lx, ly) => { double dx = (lx - cx) / rx, dy = (ly - cy) / ry; return dx * dx + dy * dy <= 1; },
                f.Fill, f.Gradient);
        DrawFill.Stroke(c, DrawGeo.Ellipse(cx, cy, rx, ry, 64), f);
    }
}

/// <summary>line x1 y1 x2 y2 [color] [width]</summary>
internal sealed class LineCommand : IDrawCommand
{
    public string Name => "line";
    public DrawFigure? Parse(IReadOnlyList<DrawToken> a)
    {
        if (a.Count < 4) return null;
        var f = new DrawFigure { Kind = "line", Stroke = 0xFF000000 };
        for (int i = 0; i < 4; i++) f.Args.Add(DrawParse.Num(a[i]));
        for (int i = 4; i < a.Count; i++)
        {
            if (DrawParse.TryCap(a[i].Value, out var cap)) { f.LineCap = cap; continue; }
            if (ColorUtil.TryParse(a[i].Value, out var c)) f.Stroke = c;
            else { var v = DrawParse.Num(a[i]); if (!double.IsNaN(v)) f.StrokeWidth = v; }
        }
        return f;
    }
    public void EmitSvg(StringBuilder sb, DrawFigure f)
        => sb.Append("  <line x1=\"").Append(DrawParse.F(f.Args[0])).Append("\" y1=\"").Append(DrawParse.F(f.Args[1]))
          .Append("\" x2=\"").Append(DrawParse.F(f.Args[2])).Append("\" y2=\"").Append(DrawParse.F(f.Args[3]))
          .Append("\" stroke=\"").Append(ColorUtil.ToHex(f.Stroke))
          .Append("\" stroke-width=\"").Append(DrawParse.F(f.StrokeWidth)).Append("\" stroke-linecap=\"").Append(f.LineCap).Append("\"/>\n");
    public void Rasterize(Canvas c, DrawFigure f)
    {
        double x1 = f.Args[0], y1 = f.Args[1], x2 = f.Args[2], y2 = f.Args[3];
        var (wx1, wy1) = f.Transform.Apply(x1, y1);
        var (wx2, wy2) = f.Transform.Apply(x2, y2);
        c.DrawLine(wx1, wy1, wx2, wy2, f.Stroke, f.StrokeWidth, f.LineCap);
    }
}

/// <summary>arrow x1 y1 x2 y2 [color] [width]</summary>
internal sealed class ArrowCommand : IDrawCommand
{
    public string Name => "arrow";
    public DrawFigure? Parse(IReadOnlyList<DrawToken> a)
    {
        if (a.Count < 4) return null;
        var f = new DrawFigure { Kind = "arrow", Stroke = 0xFF000000 };
        for (int i = 0; i < 4; i++) f.Args.Add(DrawParse.Num(a[i]));
        for (int i = 4; i < a.Count; i++)
        {
            if (DrawParse.TryCap(a[i].Value, out var cap)) { f.LineCap = cap; continue; }
            if (ColorUtil.TryParse(a[i].Value, out var c)) f.Stroke = c;
            else { var v = DrawParse.Num(a[i]); if (!double.IsNaN(v)) f.StrokeWidth = v; }
        }
        return f;
    }
    public void EmitSvg(StringBuilder sb, DrawFigure f)
    {
        double x1 = f.Args[0], y1 = f.Args[1], x2 = f.Args[2], y2 = f.Args[3];
        var (hx1, hy1, hx2, hy2) = Head(x1, y1, x2, y2, f.StrokeWidth);
        sb.Append("  <line x1=\"").Append(DrawParse.F(x1)).Append("\" y1=\"").Append(DrawParse.F(y1))
          .Append("\" x2=\"").Append(DrawParse.F(x2)).Append("\" y2=\"").Append(DrawParse.F(y2))
          .Append("\" stroke=\"").Append(ColorUtil.ToHex(f.Stroke))
          .Append("\" stroke-width=\"").Append(DrawParse.F(f.StrokeWidth)).Append("\" stroke-linecap=\"").Append(f.LineCap).Append("\"/>\n");
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
        c.DrawLine(a.X, a.Y, b.X, b.Y, f.Stroke, f.StrokeWidth, f.LineCap);
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
internal sealed class PolygonCommand : IDrawCommand
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
internal sealed class PolylineCommand : IDrawCommand
{
    public string Name => "polyline";
    public DrawFigure? Parse(IReadOnlyList<DrawToken> a)
    {
        if (a.Count < 4) return null;
        var f = new DrawFigure { Kind = "polyline", Stroke = 0xFF000000 };
        int i = 0;
        while (i < a.Count && Canvas.TryNum(a[i].Value, out var v)) { f.Args.Add(v); i++; }
        if (f.Args.Count < 4 || f.Args.Count % 2 != 0) return null;
        for (int j = i; j < a.Count; j++)
        {
            if (DrawParse.TryCap(a[j].Value, out var cap)) { f.LineCap = cap; continue; }
            if (ColorUtil.TryParse(a[j].Value, out var c)) f.Stroke = c;
            else { var v = DrawParse.Num(a[j]); if (!double.IsNaN(v)) f.StrokeWidth = v; }
        }
        return f;
    }
    public void EmitSvg(StringBuilder sb, DrawFigure f)
        => sb.Append("  <polyline points=\"").Append(DrawParse.Points(f))
          .Append("\" fill=\"none\" stroke=\"").Append(ColorUtil.ToHex(f.Stroke))
          .Append("\" stroke-width=\"").Append(DrawParse.F(f.StrokeWidth)).Append("\" stroke-linecap=\"").Append(f.LineCap).Append("\"/>\n");
    public void Rasterize(Canvas c, DrawFigure f)
    {
        var w = Canvas.TransformPoints(f.Transform, f.Args);
        for (int i = 0; i + 2 < w.Length; i += 2)
            c.DrawLine(w[i], w[i + 1], w[i + 2], w[i + 3], f.Stroke, f.StrokeWidth, f.LineCap);
    }
}

/// <summary>path "d" [color] [width]</summary>
internal sealed class PathCommand : IDrawCommand
{
    public string Name => "path";
    public DrawFigure? Parse(IReadOnlyList<DrawToken> a)
    {
        if (a.Count < 1) return null;
        var f = new DrawFigure { Kind = "path", Stroke = 0xFF000000, Text = a[0].Value };
        for (int i = 1; i < a.Count; i++)
        {
            if (DrawParse.TryCap(a[i].Value, out var cap)) { f.LineCap = cap; continue; }
            if (ColorUtil.TryParse(a[i].Value, out var c)) f.Stroke = c;
            else { var v = DrawParse.Num(a[i]); if (!double.IsNaN(v)) f.StrokeWidth = v; }
        }
        return f;
    }
    public void EmitSvg(StringBuilder sb, DrawFigure f)
        => sb.Append("  <path d=\"").Append(DrawParse.EscapeXml(f.Text ?? ""))
          .Append("\" fill=\"none\" stroke=\"").Append(ColorUtil.ToHex(f.Stroke))
          .Append("\" stroke-width=\"").Append(DrawParse.F(f.StrokeWidth)).Append("\" stroke-linecap=\"").Append(f.LineCap).Append("\"/>\n");
    public void Rasterize(Canvas c, DrawFigure f)
    {
        // 手搓光栅化器不支持任意 SVG path 曲线，退化为解析 M/L 直线段
        var seg = ParsePathSegments(f.Text);
        for (int i = 0; i + 1 < seg.Count; i++)
        {
            var a = f.Transform.Apply(seg[i].X, seg[i].Y);
            var b = f.Transform.Apply(seg[i + 1].X, seg[i + 1].Y);
            c.DrawLine(a.X, a.Y, b.X, b.Y, f.Stroke, f.StrokeWidth, f.LineCap);
        }
    }
    internal static List<(double X, double Y)> ParsePathSegments(string? d)
    {
        var pts = new List<(double, double)>();
        if (string.IsNullOrWhiteSpace(d)) return pts;
        double cx = double.NaN, cy = 0; // cx 初始须为 NaN，否则首个数字被误当 y 与 x=0 配对、首点丢失
        foreach (var token in DrawTokenizer.Tokenize(d))
        {
            var s = token.Value;
            if (s.Equals("M", StringComparison.OrdinalIgnoreCase) || s.Equals("L", StringComparison.OrdinalIgnoreCase)) continue;
            if (s.Equals("Z", StringComparison.OrdinalIgnoreCase)) { if (pts.Count > 0) pts.Add(pts[0]); continue; }
            if (!Canvas.TryNum(s, out var v)) continue;
            if (double.IsNaN(cx)) cx = v;
            else { cy = v; pts.Add((cx, cy)); cx = double.NaN; cy = 0; }
        }
        return pts;
    }
}

/// <summary>text x y "内容" [size] [color] [anchor] [bold|italic|bolditalic] [fontFamily]</summary>
internal sealed class TextCommand : IDrawCommand
{
    public string Name => "text";
    public DrawFigure? Parse(IReadOnlyList<DrawToken> a)
    {
        if (a.Count < 3) return null;
        var f = new DrawFigure { Kind = "text" };
        f.Args.Add(DrawParse.Num(a[0]));
        f.Args.Add(DrawParse.Num(a[1]));
        f.Text = a[2].Value;
        for (int i = 3; i < a.Count; i++)
        {
            var s = a[i].Value;
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
        => sb.Append("  <text x=\"").Append(DrawParse.F(f.Args[0])).Append("\" y=\"").Append(DrawParse.F(f.Args[1]))
          .Append("\" font-family=\"").Append(DrawParse.EscapeXml(f.FontFamily))
          .Append("\" font-size=\"").Append(DrawParse.F(f.FontSize))
          .Append("\" font-weight=\"").Append(f.FontWeight)
          .Append("\" font-style=\"").Append(f.FontStyle)
          .Append("\" fill=\"").Append(ColorUtil.ToHex(f.Fill))
          .Append("\" text-anchor=\"").Append(f.Anchor).Append("\">")
          .Append(DrawParse.EscapeXml(f.Text ?? "")).Append("</text>\n");
    public void Rasterize(Canvas c, DrawFigure f)
    {
        // 变换仅平移锚点，字重/斜体近似；旋转文字不支持（保持轴对齐）。
        // 均匀缩放（含超采样）通过 ScaleFactor 缩放字号，使 PNG 与 SVG 在 scale 下尺寸一致。
        var p = f.Transform.Apply(f.Args[0], f.Args[1]);
        double size = f.FontSize * f.Transform.ScaleFactor;
        // 优先 TrueType 系统字体（含字形抗锯齿），找不到则回退 5×7 位图。
        var font = TrueTypeFont.Resolve(f.FontFamily);
        if (font != null)
        {
            font.Render(c, f.Text ?? "", p.X, p.Y, size, f.Fill, f.Anchor,
                f.FontWeight == "bold", f.FontStyle == "italic");
            return;
        }
        c.DrawText(p.X, p.Y, f.Text ?? "", size, f.Fill, f.Anchor,
            f.FontWeight == "bold", f.FontStyle == "italic");
    }
}

/// <summary>star cx cy R r n [rot] [fill] [stroke] [width] — n 尖星</summary>
internal sealed class StarCommand : IDrawCommand
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
internal sealed class RegularCommand : IDrawCommand
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
internal sealed class RingCommand : IDrawCommand
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
internal sealed class PieCommand : IDrawCommand
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
internal sealed class HeartCommand : IDrawCommand
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
internal sealed class ImageCommand : IDrawCommand
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
