namespace WayCoder.Infra;

/// <summary>
/// SVG path（`d` 字符串）→ **折线**的展平器。纯数学、无 IO、无平台依赖 ⇒ 可自测、四端共用。
///
/// ## 为什么需要它
///
/// 绘图 DSL 早就有 `path "d" …` 这条指令，但光栅化那一侧只认 `M`/`L`/`Z`
/// （原注释：「手搓光栅化器不支持任意 SVG path 曲线，退化为解析 M/L 直线段」）——
/// 于是**曲线段被静默丢掉**：程序写 `M0 0 C…`，画出来只有第一个点，或者干脆什么都不画。
/// 而 `path` 走 SVG 输出时是全量的（交给浏览器/矢量后端），**同一份 DSL 两条路的图形不一样**，
/// 是那种"只在导出 PNG 时才发现"的坑。
///
/// 现在把 `d` 完整解析并**展平成折线**：曲线按控制多边形长度自适应分段，圆弧走 SVG 规范的
/// 中心参数化公式。展平之后，填充与描边都能复用现成的
/// <see cref="Canvas.FillTransformed"/> / <see cref="Canvas.StrokePolyline"/>（含变换与渐变），
/// 不必给光栅器再加一套曲线求值。
///
/// ## 支持的命令
///
/// `M/m L/l H/h V/v C/c S/s Q/q T/t A/a Z/z` —— 大小写区分绝对/相对，与 SVG 一致。
/// 隐含重复（`L 1 2 3 4` 表示两次 lineto）、`-`/`.` 直接相连的紧凑写法（`10-5.5.3`）都认。
/// </summary>
public static class DrawPath
{
    /// <summary>一条子路径：若干顶点。闭合子路径的终点会重复一次起点（便于描边直接连回去）。</summary>
    public sealed class SubPath
    {
        public readonly List<(double X, double Y)> Points = new();
        /// <summary>是否以 `Z` 闭合 —— 填充时它决定"要补最后一条边"。</summary>
        public bool Closed;
    }

    /// <summary>展平结果。`M` 开一条新的子路径。</summary>
    public static List<SubPath> Flatten(string? d)
    {
        var result = new List<SubPath>();
        if (string.IsNullOrWhiteSpace(d)) return result;

        var t = new Scanner(d);
        SubPath? cur = null;
        double px = 0, py = 0;          // 当前点
        double sx = 0, sy = 0;          // 当前子路径起点（Z 用）
        char prevCmd = '\0';
        double lastCx = 0, lastCy = 0;  // 上一个三次贝塞尔的控制点（S 用）
        double lastQx = 0, lastQy = 0;  // 上一个二次贝塞尔的控制点（T 用）

        while (t.HasMore)
        {
            char c = t.NextCommand(ref prevCmd);
            if (c == '\0') break;

            bool rel = char.IsLower(c);
            char u = char.ToUpperInvariant(c);
            double Ox = rel ? px : 0, Oy = rel ? py : 0;

            switch (u)
            {
                case 'M':
                {
                    px = t.Num() + Ox; py = t.Num() + Oy;
                    sx = px; sy = py;
                    cur = new SubPath();
                    cur.Points.Add((px, py));
                    result.Add(cur);
                    // 后续没有命令字母的数字对按 lineto 处理（SVG 的规定）
                    while (t.PeekIsNumber())
                    {
                        px = t.Num() + Ox; py = t.Num() + Oy;
                        cur.Points.Add((px, py));
                    }
                    break;
                }
                case 'L':
                {
                    cur ??= Start(ref px, ref py, ref sx, ref sy, result);
                    do
                    {
                        px = t.Num() + Ox; py = t.Num() + Oy;
                        cur.Points.Add((px, py));
                    } while (t.PeekIsNumber());
                    break;
                }
                case 'H':
                {
                    cur ??= Start(ref px, ref py, ref sx, ref sy, result);
                    do
                    {
                        px = t.Num() + Ox;
                        cur.Points.Add((px, py));
                    } while (t.PeekIsNumber());
                    break;
                }
                case 'V':
                {
                    cur ??= Start(ref px, ref py, ref sx, ref sy, result);
                    do
                    {
                        py = t.Num() + Oy;
                        cur.Points.Add((px, py));
                    } while (t.PeekIsNumber());
                    break;
                }
                case 'C':
                {
                    cur ??= Start(ref px, ref py, ref sx, ref sy, result);
                    do
                    {
                        double x1 = t.Num() + Ox, y1 = t.Num() + Oy;
                        double x2 = t.Num() + Ox, y2 = t.Num() + Oy;
                        double x = t.Num() + Ox, y = t.Num() + Oy;
                        AppendCubic(cur.Points, px, py, x1, y1, x2, y2, x, y);
                        lastCx = x2; lastCy = y2;
                        px = x; py = y;
                    } while (t.PeekIsNumber());
                    break;
                }
                case 'S':
                {
                    cur ??= Start(ref px, ref py, ref sx, ref sy, result);
                    do
                    {
                        // 第一个控制点 = 上一个控制点关于当前点的镜像（上一条不是 C/S 时退化为当前点）
                        double rx = (prevCmd is 'C' or 'c' or 'S' or 's') ? 2 * px - lastCx : px;
                        double ry = (prevCmd is 'C' or 'c' or 'S' or 's') ? 2 * py - lastCy : py;
                        double x2 = t.Num() + Ox, y2 = t.Num() + Oy;
                        double x = t.Num() + Ox, y = t.Num() + Oy;
                        AppendCubic(cur.Points, px, py, rx, ry, x2, y2, x, y);
                        lastCx = x2; lastCy = y2;
                        px = x; py = y;
                    } while (t.PeekIsNumber());
                    break;
                }
                case 'Q':
                {
                    cur ??= Start(ref px, ref py, ref sx, ref sy, result);
                    do
                    {
                        double qx = t.Num() + Ox, qy = t.Num() + Oy;
                        double x = t.Num() + Ox, y = t.Num() + Oy;
                        AppendQuadratic(cur.Points, px, py, qx, qy, x, y);
                        lastQx = qx; lastQy = qy;
                        px = x; py = y;
                    } while (t.PeekIsNumber());
                    break;
                }
                case 'T':
                {
                    cur ??= Start(ref px, ref py, ref sx, ref sy, result);
                    do
                    {
                        double qx = (prevCmd is 'Q' or 'q' or 'T' or 't') ? 2 * px - lastQx : px;
                        double qy = (prevCmd is 'Q' or 'q' or 'T' or 't') ? 2 * py - lastQy : py;
                        double x = t.Num() + Ox, y = t.Num() + Oy;
                        AppendQuadratic(cur.Points, px, py, qx, qy, x, y);
                        lastQx = qx; lastQy = qy;
                        px = x; py = y;
                    } while (t.PeekIsNumber());
                    break;
                }
                case 'A':
                {
                    cur ??= Start(ref px, ref py, ref sx, ref sy, result);
                    do
                    {
                        double rx = t.Num(), ry = t.Num();
                        double rot = t.Num();
                        bool large = t.Num() != 0;
                        bool sweep = t.Num() != 0;
                        double x = t.Num() + Ox, y = t.Num() + Oy;
                        AppendArc(cur.Points, px, py, rx, ry, rot, large, sweep, x, y);
                        px = x; py = y;
                    } while (t.PeekIsNumber());
                    break;
                }
                case 'Z':
                {
                    if (cur != null && cur.Points.Count > 0)
                    {
                        cur.Points.Add((sx, sy));
                        cur.Closed = true;
                        px = sx; py = sy;
                    }
                    break;
                }
                default:
                    // 认不出的字母：跳过它（沿用到下一个命令），避免整条路径作废
                    break;
            }
            prevCmd = c;
        }

        // 只有 1 个点的子路径画不出东西，去掉（`M` 之后没有后续命令时会出现）
        result.RemoveAll(sp => sp.Points.Count < 2);
        return result;
    }

    static SubPath Start(ref double px, ref double py, ref double sx, ref double sy, List<SubPath> result)
    {
        var sp = new SubPath();
        sp.Points.Add((px, py));
        result.Add(sp);
        sx = px; sy = py;
        return sp;
    }

    /// <summary>三次贝塞尔 → 折线。分段数按**控制多边形长度**定：长曲线多分、短曲线少分。</summary>
    internal static void AppendCubic(List<(double X, double Y)> pts, double x0, double y0, double x1, double y1, double x2, double y2, double x3, double y3, int minSeg = 8)
    {
        double len = Dist(x0, y0, x1, y1) + Dist(x1, y1, x2, y2) + Dist(x2, y2, x3, y3);
        int n = ClampSeg(len, minSeg, 128);
        for (int i = 1; i <= n; i++)
        {
            double t = (double)i / n, mt = 1 - t;
            double a = mt * mt * mt, b = 3 * mt * mt * t, c = 3 * mt * t * t, e = t * t * t;
            pts.Add((a * x0 + b * x1 + c * x2 + e * x3, a * y0 + b * y1 + c * y2 + e * y3));
        }
    }

    /// <summary>二次贝塞尔 → 折线。</summary>
    internal static void AppendQuadratic(List<(double X, double Y)> pts, double x0, double y0, double qx, double qy, double x1, double y1, int minSeg = 6)
    {
        double len = Dist(x0, y0, qx, qy) + Dist(qx, qy, x1, y1);
        int n = ClampSeg(len, minSeg, 96);
        for (int i = 1; i <= n; i++)
        {
            double t = (double)i / n, mt = 1 - t;
            double a = mt * mt, b = 2 * mt * t, c = t * t;
            pts.Add((a * x0 + b * qx + c * x1, a * y0 + b * qy + c * y1));
        }
    }

    /// <summary>
    /// 椭圆弧 → 折线。用 SVG 规范 F.6.5 的**端点参数化 → 中心参数化**换算：
    /// 给定起点终点、两个半径、旋转角、大小弧/方向标志，解出圆心与起止角，再按角步进采样。
    ///
    /// 半径过小（画不到终点）时按规范**等比放大半径** —— 这是 SVG 规定的行为，照着做才不会
    /// 与浏览器/矢量后端画得不一样（"同一条 path 两条路不同形"正是本文件开头要消灭的东西）。
    /// </summary>
    internal static void AppendArc(List<(double X, double Y)> pts, double x0, double y0,
        double rx, double ry, double rotDeg, bool largeArc, bool sweep, double x1, double y1)
    {
        if (rx == 0 || ry == 0) { pts.Add((x1, y1)); return; }   // 规范：半径为 0 视为直线
        rx = Math.Abs(rx); ry = Math.Abs(ry);
        if (Math.Abs(x0 - x1) < 1e-12 && Math.Abs(y0 - y1) < 1e-12) return;   // 起终点重合：不画

        double phi = rotDeg * Math.PI / 180.0;
        double cosP = Math.Cos(phi), sinP = Math.Sin(phi);

        double dx2 = (x0 - x1) / 2, dy2 = (y0 - y1) / 2;
        double x1p = cosP * dx2 + sinP * dy2;
        double y1p = -sinP * dx2 + cosP * dy2;

        // 半径不够大时按规范放大
        double lambda = (x1p * x1p) / (rx * rx) + (y1p * y1p) / (ry * ry);
        if (lambda > 1)
        {
            double s = Math.Sqrt(lambda);
            rx *= s; ry *= s;
        }

        double rx2 = rx * rx, ry2 = ry * ry;
        double num = rx2 * ry2 - rx2 * y1p * y1p - ry2 * x1p * x1p;
        double den = rx2 * y1p * y1p + ry2 * x1p * x1p;
        double coef = den <= 1e-12 ? 0 : Math.Sqrt(Math.Max(0, num / den));
        if (largeArc == sweep) coef = -coef;

        double cxp = coef * rx * y1p / ry;
        double cyp = -coef * ry * x1p / rx;

        double cx = cosP * cxp - sinP * cyp + (x0 + x1) / 2;
        double cy = sinP * cxp + cosP * cyp + (y0 + y1) / 2;

        double theta1 = Angle(1, 0, (x1p - cxp) / rx, (y1p - cyp) / ry);
        double dTheta = Angle((x1p - cxp) / rx, (y1p - cyp) / ry, (-x1p - cxp) / rx, (-y1p - cyp) / ry);
        if (!sweep && dTheta > 0) dTheta -= 2 * Math.PI;
        else if (sweep && dTheta < 0) dTheta += 2 * Math.PI;

        // 弧长 ≈ |dθ| × 平均半径；据此定分段数（长弧多分）
        double approx = Math.Abs(dTheta) * (rx + ry) / 2;
        int n = ClampSeg(approx, 4, 180);
        for (int i = 1; i <= n; i++)
        {
            double th = theta1 + dTheta * i / n;
            double ex = rx * Math.Cos(th), ey = ry * Math.Sin(th);
            pts.Add((cosP * ex - sinP * ey + cx, sinP * ex + cosP * ey + cy));
        }
    }

    static double Angle(double ux, double uy, double vx, double vy)
    {
        double dot = ux * vx + uy * vy;
        double len = Math.Sqrt((ux * ux + uy * uy) * (vx * vx + vy * vy));
        if (len < 1e-12) return 0;
        double a = Math.Acos(Math.Clamp(dot / len, -1, 1));
        return (ux * vy - uy * vx) < 0 ? -a : a;
    }

    static int ClampSeg(double len, int min, int max)
        => (int)Math.Clamp(Math.Ceiling(len / 3.0), min, max);

    static double Dist(double x0, double y0, double x1, double y1)
        => Math.Sqrt((x1 - x0) * (x1 - x0) + (y1 - y0) * (y1 - y0));

    /// <summary>
    /// `d` 的扫描器。**自己写数字扫描**而不是复用词法器：SVG path 允许 `10-5`、`.5.3`、`1e-3`
    /// 这类紧挨着的写法（用分隔符切词会把它们切错），而这段逻辑很小、放这里还能被自测钉住。
    /// </summary>
    private struct Scanner
    {
        private readonly string _s;
        private int _i;

        public Scanner(string s) { _s = s; _i = 0; }

        public bool HasMore
        {
            get { SkipSep(); return _i < _s.Length; }
        }

        /// <summary>取下一个命令字母；若不是字母（省略命令的重复参数）沿用 <paramref name="prev"/>。</summary>
        public char NextCommand(ref char prev)
        {
            SkipSep();
            if (_i >= _s.Length) return '\0';
            char c = _s[_i];
            if (char.IsLetter(c)) { _i++; return c; }
            return prev;   // 数字开头 ⇒ 沿用上一个命令
        }

        /// <summary>下一个记号是不是数字（用于判断隐含重复）。</summary>
        public bool PeekIsNumber()
        {
            SkipSep();
            if (_i >= _s.Length) return false;
            char c = _s[_i];
            return char.IsDigit(c) || c == '-' || c == '+' || c == '.';
        }

        /// <summary>取一个数；取不到返回 0（不抛异常 —— 一个坏数字不该让整张图消失）。</summary>
        public double Num()
        {
            SkipSep();
            int start = _i;
            if (_i < _s.Length && (_s[_i] == '-' || _s[_i] == '+')) _i++;
            while (_i < _s.Length && (char.IsDigit(_s[_i]) || _s[_i] == '.')) _i++;
            if (_i < _s.Length && (_s[_i] == 'e' || _s[_i] == 'E'))
            {
                int save = _i;
                _i++;
                if (_i < _s.Length && (_s[_i] == '-' || _s[_i] == '+')) _i++;
                if (_i < _s.Length && char.IsDigit(_s[_i])) { while (_i < _s.Length && char.IsDigit(_s[_i])) _i++; }
                else _i = save;   // `e` 后面不是指数 ⇒ 不是科学计数法，退回
            }
            var span = _s.AsSpan(start, _i - start);
            return double.TryParse(span, System.Globalization.NumberStyles.Float,
                System.Globalization.CultureInfo.InvariantCulture, out var v) && double.IsFinite(v) ? v : 0;
        }

        private void SkipSep()
        {
            while (_i < _s.Length && (_s[_i] == ' ' || _s[_i] == ',' || _s[_i] == '\t' ||
                                      _s[_i] == '\n' || _s[_i] == '\r')) _i++;
        }
    }
}
