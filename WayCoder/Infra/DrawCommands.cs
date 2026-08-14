using System.Globalization;
using System.Text;

namespace WayCoder.Infra;

/// <summary>
/// 内置绘图指令（rect/circle/line/text 等 10 条），经 [ModuleInitializer] 自动注册。
/// 每条指令实现 IDrawCommand，插件可仿照此自定义指令并注册到 DrawCommandRegistry。
/// </summary>

internal static class DrawParse
{
    public static double Num(DrawToken t) => Canvas.TryNum(t.Value, out var v) ? v : double.NaN;
    public static string F(double v) => v.ToString("0.###", CultureInfo.InvariantCulture);
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
}

/// <summary>rect x y w h [fill]</summary>
internal sealed class RectCommand : IDrawCommand
{
    public string Name => "rect";
    public DrawFigure? Parse(IReadOnlyList<DrawToken> a)
    {
        if (a.Count < 4) return null;
        var f = new DrawFigure { Kind = "rect" };
        for (int i = 0; i < 4; i++) f.Args.Add(DrawParse.Num(a[i]));
        if (a.Count >= 5 && ColorUtil.TryParse(a[4].Value, out var c)) f.Fill = c;
        return f;
    }
    public void EmitSvg(StringBuilder sb, DrawFigure f)
        => sb.Append("  <rect x=\"").Append(DrawParse.F(f.Args[0])).Append("\" y=\"").Append(DrawParse.F(f.Args[1]))
          .Append("\" width=\"").Append(DrawParse.F(f.Args[2])).Append("\" height=\"").Append(DrawParse.F(f.Args[3]))
          .Append("\" fill=\"").Append(ColorUtil.ToHex(f.Fill)).Append("\"/>\n");
    public void Rasterize(Canvas c, DrawFigure f)
        => c.FillRect((int)Math.Round(f.Args[0]), (int)Math.Round(f.Args[1]), (int)Math.Round(f.Args[2]), (int)Math.Round(f.Args[3]), f.Fill);
}

/// <summary>roundrect x y w h r [fill]</summary>
internal sealed class RoundRectCommand : IDrawCommand
{
    public string Name => "roundrect";
    public DrawFigure? Parse(IReadOnlyList<DrawToken> a)
    {
        if (a.Count < 5) return null;
        var f = new DrawFigure { Kind = "roundrect" };
        for (int i = 0; i < 5; i++) f.Args.Add(DrawParse.Num(a[i]));
        if (a.Count >= 6 && ColorUtil.TryParse(a[5].Value, out var c)) f.Fill = c;
        return f;
    }
    public void EmitSvg(StringBuilder sb, DrawFigure f)
        => sb.Append("  <rect x=\"").Append(DrawParse.F(f.Args[0])).Append("\" y=\"").Append(DrawParse.F(f.Args[1]))
          .Append("\" width=\"").Append(DrawParse.F(f.Args[2])).Append("\" height=\"").Append(DrawParse.F(f.Args[3]))
          .Append("\" rx=\"").Append(DrawParse.F(f.Args[4]))
          .Append("\" fill=\"").Append(ColorUtil.ToHex(f.Fill)).Append("\"/>\n");
    public void Rasterize(Canvas c, DrawFigure f)
        => c.FillRoundRect(f.Args[0], f.Args[1], f.Args[2], f.Args[3], f.Args[4], f.Fill);
}

/// <summary>circle cx cy r [fill]</summary>
internal sealed class CircleCommand : IDrawCommand
{
    public string Name => "circle";
    public DrawFigure? Parse(IReadOnlyList<DrawToken> a)
    {
        if (a.Count < 3) return null;
        var f = new DrawFigure { Kind = "circle" };
        for (int i = 0; i < 3; i++) f.Args.Add(DrawParse.Num(a[i]));
        if (a.Count >= 4 && ColorUtil.TryParse(a[3].Value, out var c)) f.Fill = c;
        return f;
    }
    public void EmitSvg(StringBuilder sb, DrawFigure f)
        => sb.Append("  <circle cx=\"").Append(DrawParse.F(f.Args[0])).Append("\" cy=\"").Append(DrawParse.F(f.Args[1]))
          .Append("\" r=\"").Append(DrawParse.F(f.Args[2]))
          .Append("\" fill=\"").Append(ColorUtil.ToHex(f.Fill)).Append("\"/>\n");
    public void Rasterize(Canvas c, DrawFigure f)
        => c.FillCircle(f.Args[0], f.Args[1], f.Args[2], f.Fill);
}

/// <summary>ellipse cx cy rx ry [fill]</summary>
internal sealed class EllipseCommand : IDrawCommand
{
    public string Name => "ellipse";
    public DrawFigure? Parse(IReadOnlyList<DrawToken> a)
    {
        if (a.Count < 4) return null;
        var f = new DrawFigure { Kind = "ellipse" };
        for (int i = 0; i < 4; i++) f.Args.Add(DrawParse.Num(a[i]));
        if (a.Count >= 5 && ColorUtil.TryParse(a[4].Value, out var c)) f.Fill = c;
        return f;
    }
    public void EmitSvg(StringBuilder sb, DrawFigure f)
        => sb.Append("  <ellipse cx=\"").Append(DrawParse.F(f.Args[0])).Append("\" cy=\"").Append(DrawParse.F(f.Args[1]))
          .Append("\" rx=\"").Append(DrawParse.F(f.Args[2])).Append("\" ry=\"").Append(DrawParse.F(f.Args[3]))
          .Append("\" fill=\"").Append(ColorUtil.ToHex(f.Fill)).Append("\"/>\n");
    public void Rasterize(Canvas c, DrawFigure f)
        => c.FillEllipse(f.Args[0], f.Args[1], f.Args[2], f.Args[3], f.Fill);
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
            if (ColorUtil.TryParse(a[i].Value, out var c)) f.Stroke = c;
            else { var v = DrawParse.Num(a[i]); if (!double.IsNaN(v)) f.StrokeWidth = v; }
        }
        return f;
    }
    public void EmitSvg(StringBuilder sb, DrawFigure f)
        => sb.Append("  <line x1=\"").Append(DrawParse.F(f.Args[0])).Append("\" y1=\"").Append(DrawParse.F(f.Args[1]))
          .Append("\" x2=\"").Append(DrawParse.F(f.Args[2])).Append("\" y2=\"").Append(DrawParse.F(f.Args[3]))
          .Append("\" stroke=\"").Append(ColorUtil.ToHex(f.Stroke))
          .Append("\" stroke-width=\"").Append(DrawParse.F(f.StrokeWidth)).Append("\" stroke-linecap=\"round\"/>\n");
    public void Rasterize(Canvas c, DrawFigure f)
        => c.DrawLine(f.Args[0], f.Args[1], f.Args[2], f.Args[3], f.Stroke, f.StrokeWidth);
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
          .Append("\" stroke-width=\"").Append(DrawParse.F(f.StrokeWidth)).Append("\" stroke-linecap=\"round\"/>\n");
        sb.Append("  <polygon points=\"").Append(DrawParse.F(x2)).Append(',').Append(DrawParse.F(y2)).Append(' ')
          .Append(DrawParse.F(hx1)).Append(',').Append(DrawParse.F(hy1)).Append(' ')
          .Append(DrawParse.F(hx2)).Append(',').Append(DrawParse.F(hy2))
          .Append("\" fill=\"").Append(ColorUtil.ToHex(f.Stroke)).Append("\"/>\n");
    }
    public void Rasterize(Canvas c, DrawFigure f)
    {
        double x1 = f.Args[0], y1 = f.Args[1], x2 = f.Args[2], y2 = f.Args[3];
        var (hx1, hy1, hx2, hy2) = Head(x1, y1, x2, y2, f.StrokeWidth);
        c.DrawLine(x1, y1, x2, y2, f.Stroke, f.StrokeWidth);
        c.DrawLine(x2, y2, hx1, hy1, f.Stroke, f.StrokeWidth);
        c.DrawLine(x2, y2, hx2, hy2, f.Stroke, f.StrokeWidth);
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

/// <summary>polygon x1 y1 x2 y2 ... [fill]（偶数个点，末位可带填充色）</summary>
internal sealed class PolygonCommand : IDrawCommand
{
    public string Name => "polygon";
    public DrawFigure? Parse(IReadOnlyList<DrawToken> a)
    {
        if (a.Count < 6) return null;
        var f = new DrawFigure { Kind = "polygon" };
        int end = a.Count;
        if (ColorUtil.TryParse(a[^1].Value, out var c)) { f.Fill = c; end--; }
        for (int i = 0; i < end; i++) f.Args.Add(DrawParse.Num(a[i]));
        if (f.Args.Count < 6 || f.Args.Count % 2 != 0) return null;
        return f;
    }
    public void EmitSvg(StringBuilder sb, DrawFigure f)
        => sb.Append("  <polygon points=\"").Append(DrawParse.Points(f))
          .Append("\" fill=\"").Append(ColorUtil.ToHex(f.Fill)).Append("\"/>\n");
    public void Rasterize(Canvas c, DrawFigure f) => c.FillPolygon(f.Args, f.Fill);
}

/// <summary>polyline x1 y1 x2 y2 ... [color]（偶数个点，末位可带描边色）</summary>
internal sealed class PolylineCommand : IDrawCommand
{
    public string Name => "polyline";
    public DrawFigure? Parse(IReadOnlyList<DrawToken> a)
    {
        if (a.Count < 4) return null;
        var f = new DrawFigure { Kind = "polyline", Stroke = 0xFF000000 };
        int end = a.Count;
        if (ColorUtil.TryParse(a[^1].Value, out var c)) { f.Stroke = c; end--; }
        for (int i = 0; i < end; i++) f.Args.Add(DrawParse.Num(a[i]));
        if (f.Args.Count < 4 || f.Args.Count % 2 != 0) return null;
        return f;
    }
    public void EmitSvg(StringBuilder sb, DrawFigure f)
        => sb.Append("  <polyline points=\"").Append(DrawParse.Points(f))
          .Append("\" fill=\"none\" stroke=\"").Append(ColorUtil.ToHex(f.Stroke))
          .Append("\" stroke-width=\"").Append(DrawParse.F(f.StrokeWidth)).Append("\" stroke-linecap=\"round\"/>\n");
    public void Rasterize(Canvas c, DrawFigure f)
    {
        for (int i = 0; i + 2 < f.Args.Count; i += 2)
            c.DrawLine(f.Args[i], f.Args[i + 1], f.Args[i + 2], f.Args[i + 3], f.Stroke, f.StrokeWidth);
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
            if (ColorUtil.TryParse(a[i].Value, out var c)) f.Stroke = c;
            else { var v = DrawParse.Num(a[i]); if (!double.IsNaN(v)) f.StrokeWidth = v; }
        }
        return f;
    }
    public void EmitSvg(StringBuilder sb, DrawFigure f)
        => sb.Append("  <path d=\"").Append(DrawParse.EscapeXml(f.Text ?? ""))
          .Append("\" fill=\"none\" stroke=\"").Append(ColorUtil.ToHex(f.Stroke))
          .Append("\" stroke-width=\"").Append(DrawParse.F(f.StrokeWidth)).Append("\" stroke-linecap=\"round\"/>\n");
    public void Rasterize(Canvas c, DrawFigure f)
    {
        // 手搓光栅化器不支持任意 SVG path 曲线，退化为解析 M/L 直线段
        var seg = ParsePathSegments(f.Text);
        for (int i = 0; i + 1 < seg.Count; i++)
            c.DrawLine(seg[i].X, seg[i].Y, seg[i + 1].X, seg[i + 1].Y, f.Stroke, f.StrokeWidth);
    }
    static List<(double X, double Y)> ParsePathSegments(string? d)
    {
        var pts = new List<(double, double)>();
        if (string.IsNullOrWhiteSpace(d)) return pts;
        double cx = 0, cy = 0;
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

/// <summary>text x y "内容" [size] [color] [anchor]</summary>
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
            if (ColorUtil.TryParse(a[i].Value, out var c)) f.Fill = c;
            else
            {
                var v = DrawParse.Num(a[i]);
                if (!double.IsNaN(v)) f.FontSize = v;
                else f.Anchor = a[i].Value.ToLowerInvariant();
            }
        }
        return f;
    }
    public void EmitSvg(StringBuilder sb, DrawFigure f)
        => sb.Append("  <text x=\"").Append(DrawParse.F(f.Args[0])).Append("\" y=\"").Append(DrawParse.F(f.Args[1]))
          .Append("\" font-family=\"sans-serif\" font-size=\"").Append(DrawParse.F(f.FontSize))
          .Append("\" fill=\"").Append(ColorUtil.ToHex(f.Fill))
          .Append("\" text-anchor=\"").Append(f.Anchor).Append("\">")
          .Append(DrawParse.EscapeXml(f.Text ?? "")).Append("</text>\n");
    public void Rasterize(Canvas c, DrawFigure f)
        => c.DrawText(f.Args[0], f.Args[1], f.Text ?? "", f.FontSize, f.Fill, f.Anchor);
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
    }
}
