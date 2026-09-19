using System.Globalization;

namespace WayCoder.Infra;

/// <summary>
/// 像素画布 + 光栅化（AOT 安全，纯 C#，无 System.Drawing）。
/// 形状：矩形/圆角矩形/圆/椭圆/线段/多边形（扫描线 even-odd 填充）。
/// 文字：内置 5×7 点阵字体（ASCII 32–126），非 ASCII 用实心块占位；SVG 端文字由系统字体渲染。
/// </summary>
public sealed class Canvas
{
    public int Width { get; }
    public int Height { get; }
    public byte[] Pixels { get; } // RGBA

    public Canvas(int width, int height, uint background)
    {
        Width = Math.Max(1, width);
        Height = Math.Max(1, height);
        Pixels = new byte[Width * Height * 4];
        for (int i = 0; i < Width * Height; i++)
        {
            Pixels[i * 4] = ColorUtil.R(background);
            Pixels[i * 4 + 1] = ColorUtil.G(background);
            Pixels[i * 4 + 2] = ColorUtil.B(background);
            Pixels[i * 4 + 3] = ColorUtil.A(background);
        }
    }

    public byte[] ToPng() => PngEncoder.Encode(Width, Height, Pixels);

    // ── 像素 ──
    /// <summary>
    /// 写像素（source-over 混合）。
    ///
    /// **必须按 alpha 混合，不能直接覆盖** —— 直接覆盖时「全透明填充」不是"不画"，而是把一个
    /// 透明黑像素盖到画布上，等于**把底图打了个洞**：PNG 的 alpha 变 0，显示端透出来的是
    /// 宿主背景（亮色主题下就是白底）。实测现象：VML 程序画的空心矩形内部整片变白，
    /// 而不是露出画布背景色 —— 因为空心图形的实现就是"先铺一层全透明填充、再描边"。
    ///
    /// 不透明色（alpha=255）走原来的直接赋值快路径 ⇒ **既有渲染产物逐字节不变**，
    /// 只有原本就有缺陷的半透明/全透明路径行为改变。
    /// </summary>
    public void SetPixel(int x, int y, uint c)
    {
        if (x < 0 || y < 0 || x >= Width || y >= Height) return;
        var i = (y * Width + x) * 4;
        var a = ColorUtil.A(c);

        if (a == 255)
        {
            Pixels[i] = ColorUtil.R(c);
            Pixels[i + 1] = ColorUtil.G(c);
            Pixels[i + 2] = ColorUtil.B(c);
            Pixels[i + 3] = 255;
            return;
        }
        if (a == 0) return; // 全透明 = 不画（保留底图）

        // source-over：out = src·α + dst·(1-α)
        double sa = a / 255.0, da = Pixels[i + 3] / 255.0;
        double outA = sa + da * (1 - sa);
        if (outA <= 0) return;
        Pixels[i] = (byte)Math.Round((ColorUtil.R(c) * sa + Pixels[i] * da * (1 - sa)) / outA);
        Pixels[i + 1] = (byte)Math.Round((ColorUtil.G(c) * sa + Pixels[i + 1] * da * (1 - sa)) / outA);
        Pixels[i + 2] = (byte)Math.Round((ColorUtil.B(c) * sa + Pixels[i + 2] * da * (1 - sa)) / outA);
        Pixels[i + 3] = (byte)Math.Round(outA * 255);
    }

    /// <summary>带 alpha 覆盖率混合到既有像素（用于字形/线条抗锯齿）。coverage ∈ [0,1]。</summary>
    public void BlendPixel(int x, int y, uint c, double coverage)
    {
        if (x < 0 || y < 0 || x >= Width || y >= Height || coverage <= 0) return;
        if (coverage >= 1) { SetPixel(x, y, c); return; }
        var i = (y * Width + x) * 4;
        double ca = coverage;
        Pixels[i] = (byte)(ColorUtil.R(c) * ca + Pixels[i] * (1 - ca));
        Pixels[i + 1] = (byte)(ColorUtil.G(c) * ca + Pixels[i + 1] * (1 - ca));
        Pixels[i + 2] = (byte)(ColorUtil.B(c) * ca + Pixels[i + 2] * (1 - ca));
        Pixels[i + 3] = (byte)(ColorUtil.A(c) * ca + Pixels[i + 3] * (1 - ca));
    }

    // ── 形状 ──
    public void FillRect(int x, int y, int w, int h, uint c)
    {
        if (w <= 0 || h <= 0) return;
        // 钳制到画布内：负坐标 / 超大尺寸若直接循环会产生数十亿次无效迭代（DoS）
        int x0 = Math.Max(0, x);
        int y0 = Math.Max(0, y);
        int x1 = (int)Math.Min((long)Width, (long)x + w);
        int y1 = (int)Math.Min((long)Height, (long)y + h);
        for (int py = y0; py < y1; py++)
            for (int px = x0; px < x1; px++)
                SetPixel(px, py, c);
    }

    public void FillRoundRect(double x, double y, double w, double h, double r, uint c)
    {
        // 钳制圆角半径到边长一半：r 过大时 w-2r/h-2r 变负，FillRect 画不出中间条、只剩四个圆角圆重叠
        double rr = Math.Max(0, Math.Min(r, Math.Min(w, h) / 2));
        FillRect((int)Math.Ceiling(x + rr), (int)Math.Round(y), (int)(w - 2 * rr), (int)Math.Round(h), c);
        FillRect((int)Math.Round(x), (int)Math.Ceiling(y + rr), (int)Math.Round(w), (int)(h - 2 * rr), c);
        FillCircle(x + rr, y + rr, rr, c);
        FillCircle(x + w - rr, y + rr, rr, c);
        FillCircle(x + rr, y + h - rr, rr, c);
        FillCircle(x + w - rr, y + h - rr, rr, c);
    }

    public void FillCircle(double cx, double cy, double r, uint c)
    {
        // 防 NaN/Inf 参数与超大半径（r*r 溢出 / 循环上亿次）——钳制到画布对角线，SetPixel 越界自会忽略
        if (!double.IsFinite(cx) || !double.IsFinite(cy) || !double.IsFinite(r) || r < 0) return;
        double diag = Math.Sqrt((double)Width * Width + (double)Height * Height);
        if (r > diag) r = diag;
        int r0 = (int)Math.Ceiling(r);
        for (int dy = -r0; dy <= r0; dy++)
        {
            // 非整数半径时 dy=±r0 可能 > r，r*r-dy*dy 为负开方出 NaN（与 FillEllipse 一致钳制）
            double dx = Math.Sqrt(Math.Max(0, r * r - dy * dy));
            int x0 = (int)Math.Ceiling(cx - dx);
            int x1 = (int)Math.Floor(cx + dx);
            for (int x = x0; x <= x1; x++)
                SetPixel(x, (int)Math.Round(cy + dy), c);
        }
    }

    public void FillEllipse(double cx, double cy, double rx, double ry, uint c)
    {
        // 防 NaN/Inf 参数、超大半径、退化椭圆（ry=0 会导致除零 NaN）
        if (!double.IsFinite(cx) || !double.IsFinite(cy) || !double.IsFinite(rx) || !double.IsFinite(ry) || rx < 0 || ry < 0) return;
        double diag = Math.Sqrt((double)Width * Width + (double)Height * Height);
        if (rx > diag) rx = diag;
        if (ry > diag) ry = diag;
        if (ry == 0) return;
        int r0 = (int)Math.Ceiling(ry);
        for (int dy = -r0; dy <= r0; dy++)
        {
            double dx = rx * Math.Sqrt(Math.Max(0, 1 - (dy * dy) / (ry * ry)));
            int x0 = (int)Math.Ceiling(cx - dx);
            int x1 = (int)Math.Floor(cx + dx);
            for (int x = x0; x <= x1; x++)
                SetPixel(x, (int)Math.Round(cy + dy), c);
        }
    }

    public void DrawLine(double x1, double y1, double x2, double y2, uint c, double width, string cap = "butt")
    {
        if (width <= 1)
        {
            Bresenham((int)Math.Round(x1), (int)Math.Round(y1), (int)Math.Round(x2), (int)Math.Round(y2), c);
            if (cap == "round")
            {
                // 细线 round 头：两端各补一个像素点
                SetPixel((int)Math.Round(x1), (int)Math.Round(y1), c);
                SetPixel((int)Math.Round(x2), (int)Math.Round(y2), c);
            }
            return;
        }
        // 粗线：以线段为中轴、width 为宽的填充四边形
        double dx = x2 - x1, dy = y2 - y1;
        double len = Math.Sqrt(dx * dx + dy * dy);
        if (len < 1e-6) { FillCircle(x1, y1, width / 2, c); return; }
        double nx = -dy / len * width / 2;
        double ny = dx / len * width / 2;

        // square 头：两端沿方向各外延 width/2
        double ex1 = x1, ey1 = y1, ex2 = x2, ey2 = y2;
        if (cap == "square")
        {
            double ux = dx / len, uy = dy / len;
            ex1 = x1 - ux * width / 2; ey1 = y1 - uy * width / 2;
            ex2 = x2 + ux * width / 2; ey2 = y2 + uy * width / 2;
        }
        FillPolygon(new[] { ex1 + nx, ey1 + ny, ex1 - nx, ey1 - ny, ex2 - nx, ey2 - ny, ex2 + nx, ey2 + ny }, c);

        // round 头：两端各补一个半圆
        if (cap == "round")
        {
            FillCircle(x1, y1, width / 2, c);
            FillCircle(x2, y2, width / 2, c);
        }
    }

    /// <summary>虚线线段：把线段切成「实段/空段」交替（dashOn/dashOff），逐实段调用 DrawLine。</summary>
    public void DrawLineDashed(double x1, double y1, double x2, double y2, uint c, double width,
        string cap = "butt", double dashOn = 6, double dashOff = 4)
    {
        double dx = x2 - x1, dy = y2 - y1;
        double len = Math.Sqrt(dx * dx + dy * dy);
        if (len < 1e-6) { DrawLine(x1, y1, x2, y2, c, width, cap); return; }
        double ux = dx / len, uy = dy / len;
        double t = 0;
        while (t < len)
        {
            double segEnd = Math.Min(t + dashOn, len);
            DrawLine(x1 + ux * t, y1 + uy * t, x1 + ux * segEnd, y1 + uy * segEnd, c, width, cap);
            t = segEnd + dashOff;
        }
    }

    void Bresenham(int x0, int y0, int x1, int y1, uint c)
    {
        // 防整数溢出/病态坐标死循环：端点跨度超 int 范围时 Math.Abs(x1-x0) 溢出为负
        // （int.Min→int.Max 差 2^32-1），dx 变小、x0 += sx 回绕永远到不了 x1，无限循环。
        // 此外 err 与 e2=2*err 均为 int，跨度须 ≤ int.MaxValue/2，否则 2*err 溢出为负、
        // x0/y0 收敛条件永不满足 → 死循环（如 line 0 0 2000000000 0）。
        long dxl = Math.Abs((long)x1 - x0);
        long dyl = Math.Abs((long)y1 - y0);
        if (dxl > int.MaxValue / 2 || dyl > int.MaxValue / 2) return;
        int dx = (int)dxl, sx = x0 < x1 ? 1 : -1;
        int dy = -(int)dyl, sy = y0 < y1 ? 1 : -1;
        int err = dx + dy;
        while (true)
        {
            SetPixel(x0, y0, c);
            if (x0 == x1 && y0 == y1) break;
            int e2 = 2 * err;
            if (e2 >= dy) { err += dy; x0 += sx; }
            if (e2 <= dx) { err += dx; y0 += sy; }
        }
    }

    /// <summary>扫描线 even-odd 多边形填充。pts 为 x,y 交替的顶点坐标。</summary>
    public void FillPolygon(IReadOnlyList<double> pts, uint c)
    {
        int n = pts.Count / 2;
        if (n < 3) return;
        double minY = double.MaxValue, maxY = double.MinValue;
        for (int i = 0; i < n; i++)
        {
            var y = pts[i * 2 + 1];
            minY = Math.Min(minY, y);
            maxY = Math.Max(maxY, y);
        }
        int y0 = Math.Max(0, (int)Math.Ceiling(minY));
        int y1 = Math.Min(Height - 1, (int)Math.Floor(maxY));
        for (int y = y0; y <= y1; y++)
        {
            var xs = new List<double>();
            for (int i = 0; i < n; i++)
            {
                int j = (i + 1) % n;
                double ay = pts[i * 2 + 1], by = pts[j * 2 + 1];
                if ((ay <= y && by > y) || (by <= y && ay > y))
                {
                    double ax = pts[i * 2], bx = pts[j * 2];
                    xs.Add(ax + (y - ay) / (by - ay) * (bx - ax));
                }
            }
            xs.Sort();
            for (int k = 0; k + 1 < xs.Count; k += 2)
            {
                int xa = Math.Max(0, (int)Math.Ceiling(xs[k]));
                int xb = Math.Min(Width - 1, (int)Math.Floor(xs[k + 1]));
                for (int x = xa; x <= xb; x++) SetPixel(x, y, c);
            }
        }
    }

    /// <summary>仿射变换点列表（x,y 交替）到世界坐标。</summary>
    public static double[] TransformPoints(Affine t, IReadOnlyList<double> pts)
    {
        var w = new double[pts.Count];
        for (int i = 0; i + 1 < pts.Count; i += 2)
        {
            var (wx, wy) = t.Apply(pts[i], pts[i + 1]);
            w[i] = wx; w[i + 1] = wy;
        }
        return w;
    }

    /// <summary>even-odd 点内测试（与 FillPolygon 扫描线一致）。</summary>
    public static bool PointInPolygon(double x, double y, IReadOnlyList<double> pts)
    {
        int n = pts.Count / 2;
        bool inside = false;
        for (int i = 0, j = n - 1; i < n; j = i++)
        {
            double xi = pts[i * 2], yi = pts[i * 2 + 1];
            double xj = pts[j * 2], yj = pts[j * 2 + 1];
            if ((yi > y) != (yj > y) && x < (xj - xi) * (y - yi) / (yj - yi) + xi)
                inside = !inside;
        }
        return inside;
    }

    /// <summary>
    /// 通用变换填充：把局部包围盒 4 角变换到世界得到扫描范围，逐像素逆变换回局部坐标，
    /// 用 inside（点内测试）+ 渐变采样（gradient 非空时）决定着色。变换为恒等且无渐变时走快路径。
    /// </summary>
    public void FillTransformed(Affine t, double minX, double minY, double maxX, double maxY,
        Func<double, double, bool> inside, uint fill, Gradient? gradient,
        (double MinX, double MinY, double MaxX, double MaxY)? norm = null)
    {
        if (t.IsIdentity && gradient == null)
        {
            for (int y = Math.Max(0, (int)Math.Ceiling(minY)); y <= Math.Min(Height - 1, (int)Math.Floor(maxY)); y++)
                for (int x = Math.Max(0, (int)Math.Ceiling(minX)); x <= Math.Min(Width - 1, (int)Math.Floor(maxX)); x++)
                    if (inside(x + 0.5, y + 0.5)) SetPixel(x, y, fill);
            return;
        }

        var inv = t.Inverse();
        double minWX = double.MaxValue, minWY = double.MaxValue, maxWX = double.MinValue, maxWY = double.MinValue;
        void Expand(double lx, double ly)
        {
            var (wx, wy) = t.Apply(lx, ly);
            minWX = Math.Min(minWX, wx); maxWX = Math.Max(maxWX, wx);
            minWY = Math.Min(minWY, wy); maxWY = Math.Max(maxWY, wy);
        }
        Expand(minX, minY); Expand(maxX, minY); Expand(minX, maxY); Expand(maxX, maxY);

        // 扫描范围与**归一化盒**是两件事：填充时两者相同（传 null），
        // 描边时扫描范围是"被线宽撑大的轮廓盒"、归一化盒必须是**原几何**的盒
        // ——拿轮廓盒归一化，渐变会整体偏半个线宽（SVG 的 objectBoundingBox 同样是几何盒）。
        double nbx = norm?.MinX ?? minX, nby = norm?.MinY ?? minY;
        double spanX = (norm?.MaxX ?? maxX) - nbx, spanY = (norm?.MaxY ?? maxY) - nby;
        int x0 = Math.Max(0, (int)Math.Ceiling(minWX));
        int x1 = Math.Min(Width - 1, (int)Math.Floor(maxWX));
        int y0 = Math.Max(0, (int)Math.Ceiling(minWY));
        int y1 = Math.Min(Height - 1, (int)Math.Floor(maxWY));
        for (int y = y0; y <= y1; y++)
        {
            for (int x = x0; x <= x1; x++)
            {
                var (lx, ly) = inv.Apply(x + 0.5, y + 0.5);
                if (!inside(lx, ly)) continue;
                uint col = fill;
                if (gradient != null)
                {
                    double nx = spanX <= 0 ? 0 : (lx - nbx) / spanX;
                    double ny = spanY <= 0 ? 0 : (ly - nby) / spanY;
                    col = GradientSampler.Sample(gradient, nx, ny);
                }
                SetPixel(x, y, col);
            }
        }
    }

    /// <summary>开放折线描边（世界坐标，逐边画粗线）。</summary>
    public void StrokePolyline(IReadOnlyList<double> pts, double width, uint color)
    {
        for (int i = 0; i + 2 < pts.Count; i += 2)
            DrawLine(pts[i], pts[i + 1], pts[i + 2], pts[i + 3], color, width);
    }

    /// <summary>闭合折线描边（世界坐标，含末点连首点）。</summary>
    public void StrokePolygon(IReadOnlyList<double> pts, double width, uint color)
    {
        if (pts.Count < 6) return;
        StrokePolyline(pts, width, color);
        DrawLine(pts[^2], pts[^1], pts[0], pts[1], color, width);
    }

    /// <summary>
    /// **渐变描边**：与 <see cref="StrokePolyline"/>/<see cref="DrawLine"/> **逐段同构** ——
    /// 同一套粗线四边形、同一套线帽规则（butt 直切 / square 外延 hw / round 两端补圆），
    /// 连"段与段之间不做圆角接合"这个既有行为都照搬（`StrokePolyline` 本来就是逐段 DrawLine）。
    /// 唯一的差别是每个像素的颜色按**几何局部坐标**从刷子里取。
    ///
    /// 为什么逐段调 <see cref="FillTransformed"/> 而不是先拼一个整轮廓多边形：
    /// 拼轮廓在凹多边形（星形）上会自交，用奇偶规则填出来的接缝处会**漏洞**；
    /// 而"逐段四边形取并集"正是既有实现的定义，照搬它既简单又天然等价。
    /// 代价是每段各自扫一次自己的包围盒（不是整个图形的盒），实测与描边面积成正比。
    /// </summary>
    /// <param name="localPts">**局部**坐标（x,y 交替）——变换由本方法自己做，与填充那条路同口径。</param>
    /// <param name="norm">渐变归一化盒（原几何的局部包围盒）；基准与 SVG 的 objectBoundingBox 一致。</param>
    public void StrokePolylineBrushed(Affine t, IReadOnlyList<double> localPts, double width,
        string cap, Gradient g, (double MinX, double MinY, double MaxX, double MaxY) norm,
        bool dashed = false, bool closed = false)
    {
        if (localPts.Count < 4 || g == null) return;
        // 与 DrawLine 一致：width ≤ 1 那条路是 Bresenham。渐变版退化成"1 像素宽的四边形"
        // （视觉上等价，只是斜线上取的像素格子可能与 Bresenham 不同）。
        double hw = Math.Max(1.0, width) / 2;
        int n = localPts.Count / 2;
        const double DashOn = 6, DashOff = 4;   // 与 DrawLineDashed 的默认值同源

        for (int i = 0; i + 1 < n; i++)
            Segment(localPts[i * 2], localPts[i * 2 + 1], localPts[(i + 1) * 2], localPts[(i + 1) * 2 + 1]);
        if (closed && n >= 3)
            Segment(localPts[(n - 1) * 2], localPts[(n - 1) * 2 + 1], localPts[0], localPts[1]);
        return;

        void Segment(double x1, double y1, double x2, double y2)
        {
            double dx = x2 - x1, dy = y2 - y1;
            double len = Math.Sqrt(dx * dx + dy * dy);
            if (len < 1e-6) { Disc(x1, y1); return; }

            if (dashed)
            {
                // 虚线在**局部**空间切。从前 DrawLineDashed 拿到的是世界点，
                // 缩放/旋转会改变虚线的节奏（挤密或拉长）—— 顺手修正。
                double ux = dx / len, uy = dy / len;
                for (double s0 = 0; s0 < len; s0 += DashOn + DashOff)
                {
                    double s1 = Math.Min(s0 + DashOn, len);
                    Piece(x1 + ux * s0, y1 + uy * s0, x1 + ux * s1, y1 + uy * s1);
                }
                return;
            }
            Piece(x1, y1, x2, y2);
        }

        void Piece(double x1, double y1, double x2, double y2)
        {
            double dx = x2 - x1, dy = y2 - y1;
            double len = Math.Sqrt(dx * dx + dy * dy);
            if (len < 1e-6) { Disc(x1, y1); return; }
            double ux = dx / len, uy = dy / len;
            double nx = -uy * hw, ny = ux * hw;
            double ex1 = x1, ey1 = y1, ex2 = x2, ey2 = y2;
            if (cap == "square") { ex1 -= ux * hw; ey1 -= uy * hw; ex2 += ux * hw; ey2 += uy * hw; }
            var quad = new[] { ex1 + nx, ey1 + ny, ex1 - nx, ey1 - ny, ex2 - nx, ey2 - ny, ex2 + nx, ey2 + ny };
            FillPiece(quad, (lx, ly) => PointInPolygon(lx, ly, quad));
            if (cap == "round") { Disc(x1, y1); Disc(x2, y2); }
        }

        void Disc(double cx, double cy)
        {
            double r2 = hw * hw;
            FillPiece(new[] { cx - hw, cy - hw, cx + hw, cy + hw },
                (lx, ly) => { double ax = lx - cx, ay = ly - cy; return ax * ax + ay * ay <= r2; });
        }

        void FillPiece(IReadOnlyList<double> pts, Func<double, double, bool> inside)
        {
            double mnX = double.MaxValue, mnY = double.MaxValue, mxX = double.MinValue, mxY = double.MinValue;
            for (int i = 0; i + 1 < pts.Count; i += 2)
            {
                mnX = Math.Min(mnX, pts[i]); mxX = Math.Max(mxX, pts[i]);
                mnY = Math.Min(mnY, pts[i + 1]); mxY = Math.Max(mxY, pts[i + 1]);
            }
            FillTransformed(t, mnX, mnY, mxX, mxY, inside, 0, g, norm);
        }
    }

    // ── 贴图 ──
    /// <summary>
    /// 把位图贴到画布：拉伸到局部矩形 (x,y,w,h)，支持仿射变换（逆映射最近邻采样），尊重源 alpha。
    /// 可裁剪：srcX/srcY/srcW/srcH 指定源图子矩形（像素坐标，srcW/srcH ≤ 0 表示全图）；
    /// cornerRadius &gt; 0 时把目标裁剪成圆角矩形（圆心角为圆角，圆外不画）。
    /// 恒等变换走快路径（逐像素直接映射）；有变换则先算世界包围盒、再逆变换回局部取色。
    /// </summary>
    public void DrawImage(RasterImage img, Affine t, double x, double y, double w, double h,
        double srcX = 0, double srcY = 0, double srcW = 0, double srcH = 0, double cornerRadius = 0)
    {
        if (img == null || w <= 0 || h <= 0) return;
        // 源图裁剪矩形（像素坐标）；srcW/srcH ≤ 0 表示全图
        double sx0 = srcW > 0 ? srcX : 0;
        double sy0 = srcH > 0 ? srcY : 0;
        double sW = srcW > 0 ? srcW : img.Width;
        double sH = srcH > 0 ? srcH : img.Height;
        bool clip = cornerRadius > 0;
        double rr = clip ? Math.Min(cornerRadius, Math.Min(w, h) / 2) : 0;

        if (t.IsIdentity)
        {
            int x0 = (int)Math.Round(x), y0 = (int)Math.Round(y);
            int iw = Math.Max(1, (int)Math.Round(w)), ih = Math.Max(1, (int)Math.Round(h));
            // 钳制到画布内：w/h 过大（如 2e9）会致数十亿次无效迭代（DoS），与 FillRect 一致
            int px0 = Math.Max(0, -x0);
            int py0 = Math.Max(0, -y0);
            int px1 = Math.Min(iw, Width - x0);
            int py1 = Math.Min(ih, Height - y0);
            for (int py = py0; py < py1; py++)
                for (int px = px0; px < px1; px++)
                {
                    if (clip && !InRoundRect(px + 0.5, py + 0.5, w, h, rr)) continue;
                    int sx = (int)(sx0 + (px + 0.5) / iw * sW);
                    int sy = (int)(sy0 + (py + 0.5) / ih * sH);
                    if (sx < 0 || sx >= img.Width || sy < 0 || sy >= img.Height) continue;
                    uint c = img.ColorAt(sx, sy);
                    BlendPixel(x0 + px, y0 + py, c, ColorUtil.A(c) / 255.0);
                }
            return;
        }

        var inv = t.Inverse();
        double minWX = double.MaxValue, minWY = double.MaxValue, maxWX = double.MinValue, maxWY = double.MinValue;
        void Expand(double lx, double ly)
        {
            var (wx, wy) = t.Apply(lx, ly);
            minWX = Math.Min(minWX, wx); maxWX = Math.Max(maxWX, wx);
            minWY = Math.Min(minWY, wy); maxWY = Math.Max(maxWY, wy);
        }
        Expand(x, y); Expand(x + w, y); Expand(x, y + h); Expand(x + w, y + h);

        int wx0 = Math.Max(0, (int)Math.Ceiling(minWX));
        int wx1 = Math.Min(Width - 1, (int)Math.Floor(maxWX));
        int wy0 = Math.Max(0, (int)Math.Ceiling(minWY));
        int wy1 = Math.Min(Height - 1, (int)Math.Floor(maxWY));
        for (int wy = wy0; wy <= wy1; wy++)
            for (int wx = wx0; wx <= wx1; wx++)
            {
                var (lx, ly) = inv.Apply(wx + 0.5, wy + 0.5);
                double u = (lx - x) / w, v = (ly - y) / h;
                if (u < 0 || u >= 1 || v < 0 || v >= 1) continue;
                if (clip && !InRoundRect(u * w, v * h, w, h, rr)) continue;
                int sx = (int)(sx0 + u * sW);
                int sy = (int)(sy0 + v * sH);
                if (sx < 0 || sx >= img.Width || sy < 0 || sy >= img.Height) continue;
                uint c = img.ColorAt(sx, sy);
                BlendPixel(wx, wy, c, ColorUtil.A(c) / 255.0);
            }
    }

    /// <summary>圆角矩形点内测试：局部坐标 (lx,ly) ∈ [0,w]×[0,h]，圆角半径 r（已钳制 ≤ min(w,h)/2）。</summary>
    static bool InRoundRect(double lx, double ly, double w, double h, double r)
    {
        if (lx < 0 || ly < 0 || lx > w || ly > h) return false;
        double cx = Math.Clamp(lx, r, w - r);
        double cy = Math.Clamp(ly, r, h - r);
        double dx = lx - cx, dy = ly - cy;
        return dx * dx + dy * dy <= r * r;
    }

    // ── 文字 ──
    public void DrawText(double x, double y, string text, double size, uint c, string anchor, bool bold = false, bool italic = false)
    {
        if (string.IsNullOrEmpty(text)) return;
        // size 过大（如 1e300）时 scale 转 int 会 OverflowException；钳到画布对角线，
        // 超出屏幕的文字本就不可见，SetPixel/FillRect 越界自动忽略，防 (int) 转换崩溃。
        if (!double.IsFinite(size) || size <= 0) return;
        double diagT = Math.Sqrt((double)Width * Width + (double)Height * Height);
        if (size > diagT) size = diagT;
        double scale = Math.Max(1, size / 7.0);
        double total = TextWidth(text, scale);
        double ox = anchor switch
        {
            "middle" => -total / 2,
            "end" => -total,
            _ => 0,
        };
        double cx = x + ox;
        int s = Math.Max(1, (int)Math.Round(scale));
        int boldOff = bold ? Math.Max(1, (int)Math.Round(scale * 0.4)) : 0;
        foreach (var ch in text)
        {
            var glyph = Glyph(ch);
            for (int r = 0; r < 7; r++)
            {
                var row = r < glyph.Length ? glyph[r] : ".....";
                int shear = italic ? (int)Math.Round((6 - r) * scale * 0.25) : 0;
                for (int col = 0; col < 5; col++)
                {
                    bool on = col < row.Length && row[col] == '#';
                    if (!on) continue;
                    int px = (int)Math.Round(cx + col * scale) + shear;
                    int py = (int)Math.Round(y + r * scale);
                    FillRect(px, py, s, s, c);
                    if (boldOff > 0) FillRect(px + s, py, boldOff, s, c);
                }
            }
            cx += 6 * scale;
        }
    }

    double TextWidth(string text, double scale)
    {
        double w = 0;
        foreach (var ch in text) w += 6 * scale;
        return w;
    }

    /// <summary>取字符的 7 行点阵（'#'=亮，'.'=暗）。非 ASCII 返回实心块。</summary>
    static string[] Glyph(char ch)
    {
        if (ch >= 32 && ch <= 126)
            return Font5x7[ch - 32].Split('/');
        return new[] { "#####", "#####", "#####", "#####", "#####", "#####", "#####" };
    }

    // ══════════ 5×7 点阵字体（ASCII 32–126，每字符 7 行 5 列，'/' 分隔） ══════════
    static readonly string[] Font5x7 =
    {
        // 32 空格
        "...../...../...../...../...../...../.....",
        "..#../..#../..#../..#../..#../...../..#..", // !
        ".#.#./.#.#./...../...../...../...../.....", // "
        ".#.#./.#.#./#####/.#.#./#####/.#.#./.#.#.", // #
        "..#../.####/#.#../.###./..#.#/####./..#..", // $
        "##..#/##.#./..#../.#.#./.#.##/#..##/.....", // %
        ".##../#..#./#.#../.#.../#.#.#/#..#./.##.#", // &
        "..#../..#../...../...../...../...../.....", // '
        "..#../.#.../#..../#..../#..../.#.../..#..", // (
        "..#../...#./....#/....#/....#/...#./..#..", // )
        "...../.#.#./.###./#####/.###./.#.#./.....", // *
        "...../..#../..#../#####/..#../..#../.....", // +
        "...../...../...../...../...../.#.../.#...", // ,
        "...../...../...../#####/...../...../.....", // -
        "...../...../...../...../...../.##../.##..", // .
        "....#/...#./..#../.#.../#..../...../.....", // /
        ".###./#...#/#..##/#.#.#/##..#/#...#/.###.", // 0
        "..#../.##../..#../..#../..#../..#../#####", // 1
        ".###./#...#/....#/..##./.#.../#..../#####", // 2
        "#####/....#/...#./..##./....#/#...#/.###.", // 3
        "...#./..##./.#.#./#..#./#####/...#./...#.", // 4
        "#####/#..../####./....#/....#/#...#/.###.", // 5
        "..##./.#.../#..../####./#...#/#...#/.###.", // 6
        "#####/....#/...#./..#../.#.../.#.../.#...", // 7
        ".###./#...#/#...#/.###./#...#/#...#/.###.", // 8
        ".###./#...#/#...#/.####/....#/...#./.##..", // 9
        "...../.##../.##../...../.##../.##../.....", // :
        "...../.##../.##../...../.##../.#.../#....", // ;
        "...#./..#../.#.../#..../.#.../..#../...#.", // <
        "...../...../#####/...../#####/...../.....", // =
        "#..../.#.../..#../...#./..#../.#.../#....", // >
        ".###./#...#/....#/..##./..#../...../..#..", // ?
        ".###./#...#/#.###/#.#.#/#.###/#..../.###.", // @
        "..#../.#.#./#...#/#...#/#####/#...#/#...#", // A
        "####./#...#/#...#/####./#...#/#...#/####.", // B
        ".###./#...#/#..../#..../#..../#...#/.###.", // C
        "####./#...#/#...#/#...#/#...#/#...#/####.", // D
        "#####/#..../#..../####./#..../#..../#####", // E
        "#####/#..../#..../####./#..../#..../#....", // F
        ".###./#...#/#..../#.###/#...#/#...#/.####", // G
        "#...#/#...#/#...#/#####/#...#/#...#/#...#", // H
        "#####/..#../..#../..#../..#../..#../#####", // I
        "....#/....#/....#/....#/#...#/#...#/.###.", // J
        "#...#/#..#./#.#../##.../#.#../#..#./#...#", // K
        "#..../#..../#..../#..../#..../#..../#####", // L
        "#...#/##.##/#.#.#/#.#.#/#...#/#...#/#...#", // M
        "#...#/##..#/#.#.#/#..##/#...#/#...#/#...#", // N
        ".###./#...#/#...#/#...#/#...#/#...#/.###.", // O
        "####./#...#/#...#/####./#..../#..../#....", // P
        ".###./#...#/#...#/#...#/#.#.#/#..#./.##.#", // Q
        "####./#...#/#...#/####./#.#../#..#./#...#", // R
        ".###./#...#/#..../.###./....#/#...#/.###.", // S
        "#####/..#../..#../..#../..#../..#../..#..", // T
        "#...#/#...#/#...#/#...#/#...#/#...#/.###.", // U
        "#...#/#...#/#...#/#...#/#...#/.#.#./..#..", // V
        "#...#/#...#/#...#/#.#.#/#.#.#/##.##/#...#", // W
        "#...#/#...#/.#.#./..#../.#.#./#...#/#...#", // X
        "#...#/#...#/.#.#./..#../..#../..#../..#..", // Y
        "#####/....#/...#./..#../.#.../#..../#####", // Z
        ".###./.#.../.#.../.#.../.#.../.#.../.###.", // [
        "#..../.#.../..#../...#./....#/...../.....", // backslash
        ".###./...#./...#./...#./...#./...#./.###.", // ]
        "..#../.#.#./#...#/...../...../...../.....", // ^
        "...../...../...../...../...../...../#####", // _
        ".#.../..#../...../...../...../...../.....", // `
        "...../...../.###./....#/.####/#...#/.####", // a
        "#..../#..../####./#...#/#...#/#...#/####.", // b
        "...../...../.###./#...#/#..../#...#/.###.", // c
        "....#/....#/.####/#...#/#...#/#...#/.####", // d
        "...../...../.###./#...#/#####/#..../.###.", // e
        "..##./.#..#/.#.../###../.#.../.#.../.#...", // f
        "...../...../.####/#...#/#...#/.####/....#", // g
        "#..../#..../#.##./##..#/#...#/#...#/#...#", // h
        "..#../...../.##../..#../..#../..#../#####", // i
        "...#./...../..##./...#./...#./#..#./.##..", // j
        "#..../#..../#..#./#.#../##.../#.#../#..#.", // k
        ".##../..#../..#../..#../..#../..#../#####", // l
        "...../...../##.#./#.#.#/#.#.#/#...#/#...#", // m
        "...../...../####./#...#/#...#/#...#/#...#", // n
        "...../...../.###./#...#/#...#/#...#/.###.", // o
        "...../...../####./#...#/#...#/####./#....", // p
        "...../...../.####/#...#/#...#/.####/....#", // q
        "...../...../#.##./##..#/#..../#..../#....", // r
        "...../...../.####/#..../.###./....#/####.", // s
        "..#../..#../###../..#../..#../..#.#/...#.", // t
        "...../...../#...#/#...#/#...#/#...#/.###.", // u
        "...../...../#...#/#...#/#...#/.#.#./..#..", // v
        "...../...../#...#/#...#/#.#.#/#.#.#/.#.#.", // w
        "...../...../#...#/.#.#./..#../.#.#./#...#", // x
        "...../...../#...#/#...#/#...#/.####/....#", // y
        "...../...../#####/...#./..#../.#.../#####", // z
        "..##./..#../..#../.#.../..#../..#../..##.", // {
        "..#../..#../..#../..#../..#../..#../..#..", // |
        ".##../..#../..#../...#./..#../..#../.##..", // }
        "...../...../.#.../#.#.#/...#./...../.....", // ~
    };

    /// <summary>数值解析工具（供指令 Parse 使用，InvariantCulture）。拒绝 NaN/Infinity 注入几何计算。</summary>
    public static bool TryNum(string s, out double v)
        => double.TryParse(s, NumberStyles.Float, CultureInfo.InvariantCulture, out v) && double.IsFinite(v);
}

/// <summary>渐变采样：归一化局部坐标（0..1）→ ARGB 插值。线性取投影、径向取距心归一化。</summary>
public static class GradientSampler
{
    public static uint Sample(Gradient g, double nx, double ny)
    {
        double t;
        if (g.Radial)
        {
            double dx = nx - g.Cx, dy = ny - g.Cy;
            double d = Math.Sqrt(dx * dx + dy * dy) / Math.Max(1e-9, g.R);
            t = Math.Clamp(d, 0.0, 1.0);
        }
        else
        {
            double dx = g.X2 - g.X1, dy = g.Y2 - g.Y1;
            double len2 = dx * dx + dy * dy;
            t = len2 < 1e-12 ? 0.0 : Math.Clamp(((nx - g.X1) * dx + (ny - g.Y1) * dy) / len2, 0.0, 1.0);
        }
        return Lerp(g.ColorA, g.ColorB, t);
    }

    static uint Lerp(uint a, uint b, double t)
    {
        byte r = (byte)(ColorUtil.R(a) + (ColorUtil.R(b) - ColorUtil.R(a)) * t);
        byte gg = (byte)(ColorUtil.G(a) + (ColorUtil.G(b) - ColorUtil.G(a)) * t);
        byte bl = (byte)(ColorUtil.B(a) + (ColorUtil.B(b) - ColorUtil.B(a)) * t);
        byte al = (byte)(ColorUtil.A(a) + (ColorUtil.A(b) - ColorUtil.A(a)) * t);
        return ((uint)al << 24) | ((uint)r << 16) | ((uint)gg << 8) | bl;
    }
}
