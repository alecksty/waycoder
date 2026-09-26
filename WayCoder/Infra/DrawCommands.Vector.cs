namespace WayCoder.Infra;

// ══════════════════════════════════════════════════════════════════════════
// 各绘图指令的**矢量画法**（第三条路，与 DrawCommands.cs 里的 `Rasterize` / `EmitSvg` 并列）。
//
// 为什么单独一个文件而不是塞回 DrawCommands.cs：那个文件已经 1000 行出头，
// 而这里有 16 个方法、每个都只有几行 —— 拆开之后"一条指令三个画法"这件事
// 仍然是**编译器保证**的（`IDrawCommand` 上 Vector 是必需成员，漏一个就编不过），
// 只是三份代码分两个文件放。几何一律取自 `DrawGeo`（与 SVG 那条路同一个来源）。
//
// ⚠ **两边的差异都记在 `MauiVectorTarget` 的类注释里**（文字度量、渐变口径、虚线相位），
//   这里是"几何到落笔"的映射，不该有第二套规则。
// ══════════════════════════════════════════════════════════════════════════

// ⚠ 这四个（矩形 / 圆角矩形 / 圆 / 椭圆）**填充走平台原生**（`DrawVector.Rect`/`Ellipse`
//   会判变换恒等后调 `IVectorTarget.FillRect`/`FillEllipse`），只有**描边**还用折线。
//   原因见 `IVectorTarget.FillRect` 的说明：通用那条路要逐点建平台路径，一帧几百个图元
//   就是几万次 JNI。而这四类是一帧图元的绝大多数（游戏里的窗户、楼体、地面、按键全是矩形）。
//
// ⚠ 描边那半边的点集**先问 `HasStroke` 再拼** —— `Stroke` 自己会在"没给描边色"时提前
//   返回，但点集是调用方拼的，不先问一句就是白建一份（圆那份还是 64 个点）。

internal sealed partial class RectCommand
{
    public void Vector(IVectorTarget t, DrawFigure f)
    {
        double x = f.Args[0], y = f.Args[1], w = f.Args[2], h = f.Args[3];
        DrawVector.Rect(t, x, y, w, h, 0, f);
        if (DrawVector.HasStroke(f))
            DrawVector.Stroke(t, new List<double> { x, y, x + w, y, x + w, y + h, x, y + h }, f, close: true);
    }
}

internal sealed partial class RoundRectCommand
{
    public void Vector(IVectorTarget t, DrawFigure f)
    {
        double x = f.Args[0], y = f.Args[1], w = f.Args[2], h = f.Args[3], r = f.Args[4];
        DrawVector.Rect(t, x, y, w, h, r, f);
        if (DrawVector.HasStroke(f))
            DrawVector.Stroke(t, DrawGeo.RoundRect(x, y, w, h, r), f, close: true);
    }
}

internal sealed partial class CircleCommand
{
    public void Vector(IVectorTarget t, DrawFigure f)
    {
        double cx = f.Args[0], cy = f.Args[1], r = f.Args[2];
        DrawVector.Ellipse(t, cx, cy, r, r, f);
        if (DrawVector.HasStroke(f))
            DrawVector.Stroke(t, DrawGeo.Ellipse(cx, cy, r, r, 64), f, close: true);
    }
}

internal sealed partial class EllipseCommand
{
    public void Vector(IVectorTarget t, DrawFigure f)
    {
        double cx = f.Args[0], cy = f.Args[1], rx = f.Args[2], ry = f.Args[3];
        DrawVector.Ellipse(t, cx, cy, rx, ry, f);
        if (DrawVector.HasStroke(f))
            DrawVector.Stroke(t, DrawGeo.Ellipse(cx, cy, rx, ry, 64), f, close: true);
    }
}

internal sealed partial class LineCommand
{
    public void Vector(IVectorTarget t, DrawFigure f)
        => DrawVector.Stroke(t, new List<double> { f.Args[0], f.Args[1], f.Args[2], f.Args[3] }, f);
}

internal sealed partial class ArrowCommand
{
    public void Vector(IVectorTarget t, DrawFigure f)
    {
        // 箭头 = 主干 + 两条箭头边（与光栅侧同一个 Head 计算 —— 同一个类里，不会漂）
        double x1 = f.Args[0], y1 = f.Args[1], x2 = f.Args[2], y2 = f.Args[3];
        var (hx1, hy1, hx2, hy2) = Head(x1, y1, x2, y2, f.StrokeWidth);
        DrawVector.Stroke(t, new List<double> { x1, y1, x2, y2 }, f);
        DrawVector.Stroke(t, new List<double> { x2, y2, hx1, hy1 }, f);
        DrawVector.Stroke(t, new List<double> { x2, y2, hx2, hy2 }, f);
    }
}

internal sealed partial class PolygonCommand
{
    public void Vector(IVectorTarget t, DrawFigure f)
    {
        DrawVector.Polygon(t, f.Args, f);
        DrawVector.Stroke(t, f.Args, f, close: true);
    }
}

internal sealed partial class PolylineCommand
{
    public void Vector(IVectorTarget t, DrawFigure f) => DrawVector.Stroke(t, f.Args, f);
}

internal sealed partial class PathCommand
{
    public void Vector(IVectorTarget t, DrawFigure f)
    {
        var subs = DrawPath.Flatten(f.Text);
        if (subs.Count == 0) return;

        if (f.FillSet || f.GradientRef != null)
        {
            var lists = new List<IReadOnlyList<double>>(subs.Count);
            foreach (var sp in subs)
            {
                var arr = new double[sp.Points.Count * 2];
                for (var i = 0; i < sp.Points.Count; i++)
                {
                    arr[i * 2] = sp.Points[i].X;
                    arr[i * 2 + 1] = sp.Points[i].Y;
                }
                lists.Add(arr);
            }
            // 多子路径按**奇偶规则**挖洞（与光栅侧同一个判据：被奇数条子路径包含才算在内）
            DrawVector.Subpaths(t, lists, f);
        }

        if (f.StrokeWidth <= 0) return;
        foreach (var sp in subs)
        {
            var pts = new List<double>(sp.Points.Count * 2);
            foreach (var p in sp.Points) { pts.Add(p.X); pts.Add(p.Y); }
            DrawVector.Stroke(t, pts, f, close: sp.Closed);
        }
    }
}

internal sealed partial class TextCommand
{
    public void Vector(IVectorTarget t, DrawFigure f) => DrawVector.Text(t, f);
}

internal sealed partial class StarCommand
{
    public void Vector(IVectorTarget t, DrawFigure f)
    {
        var pts = DrawGeo.Star(f.Args[0], f.Args[1], f.Args[2], f.Args[3], (int)Math.Round(f.Args[4]), f.Args[5]);
        DrawVector.Polygon(t, pts, f);
        DrawVector.Stroke(t, pts, f, close: true);
    }
}

internal sealed partial class RegularCommand
{
    public void Vector(IVectorTarget t, DrawFigure f)
    {
        var pts = DrawGeo.Regular(f.Args[0], f.Args[1], f.Args[2], (int)Math.Round(f.Args[3]), f.Args[4]);
        DrawVector.Polygon(t, pts, f);
        DrawVector.Stroke(t, pts, f, close: true);
    }
}

internal sealed partial class RingCommand
{
    public void Vector(IVectorTarget t, DrawFigure f)
    {
        // 环的点集是"外圈 + 反向内圈"一条自交轮廓 ⇒ **按奇偶规则**填，中间的洞才挖得出来
        var pts = DrawGeo.Ring(f.Args[0], f.Args[1], f.Args[2], f.Args[3]);
        if (pts.Count >= 6 && ((f.Fill >> 24) != 0 || f.Gradient != null))
            t.FillShape(new[] { Canvas.TransformPoints(f.Transform, pts) }, f.Fill, f.Gradient, evenOdd: true);
        DrawVector.Stroke(t, DrawGeo.Ring(f.Args[0], f.Args[1], f.Args[2], f.Args[3]), f, close: true);
    }
}

internal sealed partial class PieCommand
{
    public void Vector(IVectorTarget t, DrawFigure f)
    {
        var pts = DrawGeo.Pie(f.Args[0], f.Args[1], f.Args[2], f.Args[3], f.Args[4]);
        DrawVector.Polygon(t, pts, f);
        DrawVector.Stroke(t, pts, f, close: true);
    }
}

internal sealed partial class HeartCommand
{
    public void Vector(IVectorTarget t, DrawFigure f)
    {
        var pts = DrawGeo.Heart(f.Args[0], f.Args[1], f.Args[2]);
        DrawVector.Polygon(t, pts, f);
        DrawVector.Stroke(t, pts, f, close: true);
    }
}

internal sealed partial class ImageCommand
{
    public void Vector(IVectorTarget t, DrawFigure f)
    {
        // 任意仿射（旋转/错切）在平台画布上未必支持 ⇒ 明确告诉宿主"这个画不了"，
        // 由它回退到光栅后端（宁可慢，别默默少画）。等比缩放+平移是最常见的情形，照常走。
        var affine = !DrawFill.TryScaled(f.Transform, out _, out _, out _);
        t.DrawImage(f.ImagePath, f.Args[0], f.Args[1], f.Args[2], f.Args[3],
            f.SrcX, f.SrcY, f.SrcW, f.SrcH, f.CornerRadius, affine);
    }
}
