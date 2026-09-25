using System.Text;

namespace WayCoder.Infra;

/// <summary>
/// 蒙版布尔运算符。数值是**跨语言契约**（C 头文件的 `VML_MASK_*` 宏、22 门语言的
/// `shared.*` 绑定都按这几个数写死）—— 改值会静默改变所有现有程序的语义。
/// </summary>
public static class MaskOp
{
    /// <summary>取代当前蒙版（`ui_mask_end(inside)` 的老语义）。</summary>
    public const int Replace = 0;

    /// <summary>与当前蒙版取并。</summary>
    public const int Union = 1;

    /// <summary>与当前蒙版取交。</summary>
    public const int Intersect = 2;

    /// <summary>从当前蒙版里挖掉新形状（当前且非新）。</summary>
    public const int Subtract = 3;

    /// <summary>与当前蒙版取异或。</summary>
    public const int Xor = 4;

    /// <summary>是不是认得的运算符。未知值一律拒掉（不能当 0 蒙混）。</summary>
    public static bool IsValid(int op) => op is >= Replace and <= Xor;
}

/// <summary>
/// 一个蒙版形状。三种：圆 / 矩形 / 多边形。
///
/// ## 为什么是"闭式判定"而不是离屏位图
///
/// 蒙版形状**本来就不上屏** —— 那它们只是"一组判定用的几何"，落到某个点上就是几行算术
/// （圆 = 比半径平方、矩形 = 比边界、多边形 = 射线法）。原设计（离屏位图 + 两遍遍历）
/// 要遍历两遍、还要在每个后端各维护一块临时画布，而收益是零。详见 `docs/VML宿主接口.md` §10。
///
/// ## 多边形用 even-odd
///
/// 与光栅器既有的 `Canvas.PointInPolygon` **同一套规则**（射线法、奇偶）——
/// 不另写一份，否则"同一份 path 当图元画"与"当蒙版用"会给出不同的形状。
/// </summary>
public sealed class MaskShape
{
    public const int KindCircle = 0;
    public const int KindRect = 1;
    public const int KindPolygon = 2;

    public int Kind { get; private init; }

    /// <summary>圆：`A`=cx `B`=cy `C`=r；矩形：`A`=x `B`=y `C`=w `D`=h。多边形不用。</summary>
    public double A, B, C, D;

    /// <summary>多边形顶点，**扁平的 x,y 序列**（与 `Canvas.PointInPolygon` 同一种表示）。</summary>
    public List<double>? Points;

    public static MaskShape Circle(double cx, double cy, double r)
        => new() { Kind = KindCircle, A = cx, B = cy, C = r };

    public static MaskShape Rect(double x, double y, double w, double h)
        => new() { Kind = KindRect, A = x, B = y, C = w, D = h };

    public static MaskShape Polygon(List<double> pts)
        => new() { Kind = KindPolygon, Points = pts };

    /// <summary>这一点在不在形状里。<paramref name="x"/>/<paramref name="y"/> 是**画布像素坐标**。</summary>
    public bool Hit(double x, double y)
    {
        switch (Kind)
        {
            case KindCircle:
            {
                double dx = x - A, dy = y - B;
                return dx * dx + dy * dy <= C * C;
            }
            case KindRect:
                return x >= A && x < A + C && y >= B && y < B + D;
            case KindPolygon:
                return Points != null && Points.Count >= 6 && Canvas.PointInPolygon(x, y, Points);
            default:
                return false;
        }
    }

    /// <summary>
    /// 把形状导出成一条 **SVG 路径字符串**（`ui_mask_path` 用）。
    ///
    /// 程序拿到它就能直接喂回 `ui_path`：**描洞口的边**（画断面、做外发光），
    /// 或者把它拿去做别的运算。这是"蒙版 → 路径"那个方向。
    ///
    /// 圆用两段半圆弧（`A`）而不是折线 —— 导出的是**路径**，让它保持是圆的，
    /// 而不是替调用方先把圆近似成多边形。
    /// </summary>
    public string ToSvgPath()
    {
        switch (Kind)
        {
            case KindCircle:
                return $"M{DrawParse.F(A - C)} {DrawParse.F(B)} "
                     + $"A{DrawParse.F(C)} {DrawParse.F(C)} 0 1 0 {DrawParse.F(A + C)} {DrawParse.F(B)} "
                     + $"A{DrawParse.F(C)} {DrawParse.F(C)} 0 1 0 {DrawParse.F(A - C)} {DrawParse.F(B)} Z";
            case KindRect:
                return $"M{DrawParse.F(A)} {DrawParse.F(B)} "
                     + $"L{DrawParse.F(A + C)} {DrawParse.F(B)} "
                     + $"L{DrawParse.F(A + C)} {DrawParse.F(B + D)} "
                     + $"L{DrawParse.F(A)} {DrawParse.F(B + D)} Z";
            case KindPolygon:
            {
                var pts = Points;
                if (pts == null || pts.Count < 4) return "";
                var sb = new StringBuilder();
                sb.Append('M').Append(DrawParse.F(pts[0])).Append(' ').Append(DrawParse.F(pts[1]));
                for (int i = 2; i + 1 < pts.Count; i += 2)
                    sb.Append(" L").Append(DrawParse.F(pts[i])).Append(' ').Append(DrawParse.F(pts[i + 1]));
                sb.Append(" Z");
                return sb.ToString();
            }
            default:
                return "";
        }
    }

    /// <summary>
    /// 形状的**包围盒** `(MinX, MinY, MaxX, MaxY)`。给"能不能折叠成一条路径"那套判据用
    /// （见 `MaskExpr.ToClipPath`）—— 那里全程用**保守**判据：包围盒不满足就一定不满足。
    /// </summary>
    public (double MinX, double MinY, double MaxX, double MaxY) Box()
    {
        switch (Kind)
        {
            case KindCircle:
                return (A - C, B - C, A + C, B + C);
            case KindRect:
                return (A, B, A + C, B + D);
            case KindPolygon:
            {
                var pts = Points;
                if (pts == null || pts.Count < 2) return (0, 0, 0, 0);
                double minX = pts[0], maxX = pts[0], minY = pts[1], maxY = pts[1];
                for (int i = 0; i + 1 < pts.Count; i += 2)
                {
                    if (pts[i] < minX) minX = pts[i];
                    if (pts[i] > maxX) maxX = pts[i];
                    if (pts[i + 1] < minY) minY = pts[i + 1];
                    if (pts[i + 1] > maxY) maxY = pts[i + 1];
                }
                return (minX, minY, maxX, maxY);
            }
            default:
                return (0, 0, 0, 0);
        }
    }

    /// <summary>
    /// 转成一条**子路径的点列**（扁平 `x,y,…`，首尾不重复）—— 给矢量后端的 `ClipPath` 用。
    ///
    /// 圆按 32 边形近似：矢量那边是"填充一条闭合路径"，段数只影响它有多圆，
    /// 而 32 段在手机屏上的圆已经看不出棱角。
    /// ⚠ 这是**矢量侧**的近似；光栅侧走的是闭式判定（圆就是圆）——
    ///   两边的差别只在这个多边形近似上，与"同一个形状两条路不一样"不是一回事。
    /// </summary>
    public List<double> ToSubpath(int circleSegments = 32)
    {
        var pts = new List<double>();
        switch (Kind)
        {
            case KindCircle:
            {
                for (int i = 0; i < circleSegments; i++)
                {
                    double t = 2 * Math.PI * i / circleSegments;
                    pts.Add(A + C * Math.Cos(t));
                    pts.Add(B + C * Math.Sin(t));
                }
                break;
            }
            case KindRect:
                pts.Add(A); pts.Add(B);
                pts.Add(A + C); pts.Add(B);
                pts.Add(A + C); pts.Add(B + D);
                pts.Add(A); pts.Add(B + D);
                break;
            case KindPolygon:
                if (Points != null) pts.AddRange(Points);
                break;
        }
        return pts;
    }

    /// <summary>
    /// 缩放（出图走 `ToPngAntialiased` 时画布被放大 s 倍，判定点也在那个空间里）。
    ///
    /// ⚠ 与 `ClipCommand` 是**同一个坑**：形状不过变换就落在未缩放坐标系里，
    ///   症状是"一开蒙版里面的东西全没了"。圆心与半径**一起**缩放 —— 半径是长度、不是坐标。
    /// </summary>
    public MaskShape Scaled(double s, double tx, double ty)
    {
        switch (Kind)
        {
            case KindCircle:
                return Circle(tx + A * s, ty + B * s, C * s);
            case KindRect:
                return Rect(tx + A * s, ty + B * s, C * s, D * s);
            case KindPolygon:
            {
                var src = Points ?? new List<double>();
                var dst = new List<double>(src.Count);
                for (int i = 0; i + 1 < src.Count; i += 2)
                {
                    dst.Add(tx + src[i] * s);
                    dst.Add(ty + src[i + 1] * s);
                }
                return Polygon(dst);
            }
            default:
                return this;
        }
    }
}

/// <summary>
/// 蒙版 = **一串布尔段** + 一个 `inside` 开关。
///
/// 求值是从左到右的左结合链：`M = S₀`，然后逐段 `M = M opₖ Sₖ`。
/// 这不是"任意表达式树"，但表达力覆盖了玩家要的全部形态（环形 = 圆减圆、镂空 = 矩形减圆、
/// 多块同显 = 圆的并），而且**编码是线性的、能顺序解析** —— 树要带括号和深度，
/// 而这套运算符全是左结合直觉，没必要。
///
/// ⚠ **`inside` 与 `SUBTRACT` 是正交的两件事**（文档 §10.2 要求定清楚的那一条）：
///   先算完整个布尔链，**最后**按 `inside` 取反。所以
///   `inside=0` + `SUBTRACT` 的结果是 `NOT(A AND NOT B)` —— 数学上唯一自洽的读法。
///   两种写法（`inside=0` 的老式挖洞 vs `SUBTRACT` 的新式挖洞）给出**同样的画面**，
///   那是"等价表达"、不是"两种语义"。
/// </summary>
public sealed class MaskExpr
{
    public sealed class Segment
    {
        public int Op;
        public readonly List<MaskShape> Shapes = new();
    }

    /// <summary>真 = 只在蒙版**里面**画；假 = 只在**外面**画。</summary>
    public bool Inside = true;

    public readonly List<Segment> Segments = new();

    /// <summary>
    /// 把整条布尔链**折叠成一条路径 + 一个填充规则**（矢量后端用）。
    ///
    /// 后端不必懂布尔运算 —— 但也因此**只认能折叠的形态**；折叠不了就返回 null，
    /// 调用方据此 `MarkUnsupported`（整窗回退光栅）。**宁可慢，也别画错**。
    ///
    /// | 形态 | 折叠成 | 为什么成立 |
    /// |---|---|---|
    /// | 单段（`REPLACE`/`UNION` = 形状并集）| 全部形状 + **NonZero** | 重叠处按非零环绕仍是"内部"，正是并集 |
    /// | `[A, SUBTRACT B…]` | A + B + **EvenOdd** | 洞落在 A 里时"穿过两次"⇒ 挖空 |
    ///
    /// ⚠ `SUBTRACT` 那条要三条**保守**前提，缺一就放弃：
    ///   ① 段全在 A 之后且都是 `SUBTRACT`；② 每个洞的包围盒都装得进 A 的包围盒；
    ///   ③ A 的形状之间、洞与洞之间**包围盒互不相交** —— 因为 even-odd 下
    ///   "两处重叠"就不再是"穿过两次"那么单纯了（重叠区反而会被填实）。
    ///   判据用包围盒是**故意保守**：包围盒不满足一定不满足，满足也未必（形状可能更小），
    ///   于是少量本该能折叠的形态会退回光栅 —— 那是慢一点，不是画错。
    ///
    /// ⚠ **`INTERSECT` / `XOR` / 混合链折叠不了**：那需要真正的路径布尔运算，
    ///   不是"塞进一条路径 + 换个填充规则"能表达的。
    /// </summary>
    public (List<List<double>> Subpaths, bool EvenOdd)? ToClipPath()
    {
        if (IsEmpty) return null;

        if (Segments.Count == 1)
        {
            var subs = new List<List<double>>();
            foreach (var s in Segments[0].Shapes) subs.Add(s.ToSubpath());
            return subs.Count == 0 ? null : (subs, false);
        }

        // 多段：只认 [A, SUBTRACT…]
        for (int i = 1; i < Segments.Count; i++)
        {
            if (Segments[i].Op != MaskOp.Subtract) return null;
        }

        var baseShapes = Segments[0].Shapes;
        if (baseShapes.Count == 0) return null;

        // ③a 底形状之间不能重叠（even-odd 下重叠区会被挖掉，那不是并集）
        for (int i = 0; i < baseShapes.Count; i++)
        {
            for (int j = i + 1; j < baseShapes.Count; j++)
            {
                if (Overlaps(baseShapes[i].Box(), baseShapes[j].Box())) return null;
            }
        }

        var holes = new List<MaskShape>();
        for (int i = 1; i < Segments.Count; i++) holes.AddRange(Segments[i].Shapes);
        if (holes.Count == 0) return null;

        var baseBox = baseShapes[0].Box();
        for (int i = 1; i < baseShapes.Count; i++)
        {
            var b = baseShapes[i].Box();
            baseBox = (Math.Min(baseBox.MinX, b.MinX), Math.Min(baseBox.MinY, b.MinY),
                       Math.Max(baseBox.MaxX, b.MaxX), Math.Max(baseBox.MaxY, b.MaxY));
        }

        // ② 每个洞都装得进底
        foreach (var h in holes)
        {
            var b = h.Box();
            if (b.MinX < baseBox.MinX || b.MinY < baseBox.MinY ||
                b.MaxX > baseBox.MaxX || b.MaxY > baseBox.MaxY) return null;
        }

        // ③b 洞与洞之间不重叠
        for (int i = 0; i < holes.Count; i++)
        {
            for (int j = i + 1; j < holes.Count; j++)
            {
                if (Overlaps(holes[i].Box(), holes[j].Box())) return null;
            }
        }

        var subs2 = new List<List<double>>();
        foreach (var s in baseShapes) subs2.Add(s.ToSubpath());
        foreach (var h in holes) subs2.Add(h.ToSubpath());
        return (subs2, true);   // EvenOdd：洞里"穿过两次" ⇒ 挖空
    }

    /// <summary>两个包围盒有没有交叠（保守判据，见 <see cref="ToClipPath"/>）。</summary>
    private static bool Overlaps((double MinX, double MinY, double MaxX, double MaxY) a,
                                 (double MinX, double MinY, double MaxX, double MaxY) b)
        => a.MinX < b.MaxX && b.MinX < a.MaxX && a.MinY < b.MaxY && b.MinY < a.MaxY;

    /// <summary>
    /// 收一段进段列表：`Replace` 清掉历史，其余**追加**（左结合链，见类注释）。
    ///
    /// **两个调用方共用这一处**：出图那条路（`DrawRunner.Parse`，形状来自解析好的图元）
    /// 与运行时镜像（`VmlScene`，形状来自方法参数，只服务 `ui_mask_test`）。
    /// 它们的差异只在"形状从哪来"—— 累积语义必须一模一样，否则
    /// **查到的蒙版与画出来的蒙版不是同一个**，而那是最难查的一类分叉。
    /// </summary>
    public static void ApplySegment(List<Segment> segs, int op, IEnumerable<MaskShape>? shapes)
    {
        if (op == MaskOp.Replace) segs.Clear();
        var seg = new Segment { Op = op };
        if (shapes != null) seg.Shapes.AddRange(shapes);
        segs.Add(seg);
    }

    /// <summary>
    /// 没有任何形状 ⇒ 蒙版不起作用（后续图元全部可见）。
    ///
    /// ⚠ 判据是"**所有段都空**"，不是"段数为 0"：`ui_mask_clear()` 的实现就是
    ///   "开一个空的再收"（见 `VmlScene.AddMaskClear`），产出的正是**一个空段** ——
    ///   若按段数判，取消蒙版会变成"一个空蒙版"⇒ `Hit` 恒假 ⇒ **整屏什么都画不出来**。
    ///   而"布尔链中途某一段是空的"（比如 SUBTRACT 一个空集合）是合法的：
    ///   它对该段无影响，不能因此把整个蒙版作废。
    /// </summary>
    public bool IsEmpty
    {
        get
        {
            for (int i = 0; i < Segments.Count; i++)
            {
                if (Segments[i].Shapes.Count > 0) return false;
            }
            return true;
        }
    }

    /// <summary>这一点该不该落笔。</summary>
    public bool Hit(double x, double y)
    {
        bool cur = false;
        for (int i = 0; i < Segments.Count; i++)
        {
            var seg = Segments[i];
            bool any = false;
            for (int k = 0; k < seg.Shapes.Count; k++)
            {
                if (seg.Shapes[k].Hit(x, y)) { any = true; break; }
            }

            if (i == 0) { cur = any; continue; }
            cur = seg.Op switch
            {
                MaskOp.Union => cur || any,
                MaskOp.Intersect => cur && any,
                MaskOp.Subtract => cur && !any,
                MaskOp.Xor => cur ^ any,
                // `Replace` 出现在非首段：按替换处理（解析期不会产出这种编码，
                // 但解码的是**程序给的数**，得有个确定的行为，不能落进未定义）
                _ => any,
            };
        }
        return Inside ? cur : !cur;
    }

    public MaskExpr Scaled(double s, double tx, double ty)
    {
        var e = new MaskExpr { Inside = Inside };
        foreach (var seg in Segments)
        {
            var ns = new Segment { Op = seg.Op };
            foreach (var sh in seg.Shapes) ns.Shapes.Add(sh.Scaled(s, tx, ty));
            e.Segments.Add(ns);
        }
        return e;
    }

    // ── 编解码 ────────────────────────────────────────────────────────────
    //
    // 编码（**只有这一份**，解析期编、光栅期解，成对放在同一个文件里免得漂移）：
    //
    //     [inside, segCount, op₀, n₀, (kind, len, v…)*n₀, op₁, n₁, …]
    //
    // 形状用**长度前缀**而不是定长 5 元组：圆 3 个值、矩形 4 个、多边形 2×点数（可变长）。
    // 定长元组塞不下多边形，而"多边形的点另存一张表"就是"同一件事两处存"。
    //
    // ⚠ 这个编码**不是**对外契约（C 程序看不到它），但它是 `mask` 这条 DSL 图元的格式 ⇒
    //   光栅 / 矢量 / SVG 三处都从它读，改这里要三处一起看。

    /// <summary>把"当前累积的段"编进一个 `mask` 图元的 `Args`。</summary>
    public static void Encode(DrawFigure f, bool inside, IReadOnlyList<Segment> segs)
    {
        f.Args.Clear();
        f.Args.Add(inside ? 1 : 0);
        f.Args.Add(segs.Count);
        foreach (var seg in segs)
        {
            f.Args.Add(seg.Op);
            f.Args.Add(seg.Shapes.Count);
            foreach (var sh in seg.Shapes)
            {
                f.Args.Add(sh.Kind);
                switch (sh.Kind)
                {
                    case MaskShape.KindCircle:
                        f.Args.Add(3); f.Args.Add(sh.A); f.Args.Add(sh.B); f.Args.Add(sh.C);
                        break;
                    case MaskShape.KindRect:
                        f.Args.Add(4); f.Args.Add(sh.A); f.Args.Add(sh.B); f.Args.Add(sh.C); f.Args.Add(sh.D);
                        break;
                    case MaskShape.KindPolygon:
                    {
                        var pts = sh.Points ?? new List<double>();
                        f.Args.Add(pts.Count);
                        f.Args.AddRange(pts);
                        break;
                    }
                    default:
                        f.Args.Add(0);   // 空形状：解码时跳过（不会命中任何点）
                        break;
                }
            }
        }
    }

    /// <summary>从 `mask` 图元的 `Args` 还原。**坏了就返回 null**（当作"没有蒙版"），不抛异常。</summary>
    public static MaskExpr? Decode(IReadOnlyList<double> args)
    {
        if (args.Count < 2) return null;
        var e = new MaskExpr { Inside = args[0] != 0 };
        int segCount = (int)args[1];
        if (segCount <= 0) return e;   // 空蒙版 = 不起作用

        int i = 2;
        for (int s = 0; s < segCount; s++)
        {
            if (i + 1 >= args.Count) break;
            var seg = new Segment { Op = (int)args[i++] };
            int n = (int)args[i++];
            for (int k = 0; k < n; k++)
            {
                if (i + 1 >= args.Count) return e;   // 截断：交出已解出的部分
                int kind = (int)args[i++];
                int len = (int)args[i++];
                if (len < 0 || i + len > args.Count) return e;
                switch (kind)
                {
                    case MaskShape.KindCircle when len >= 3:
                        seg.Shapes.Add(MaskShape.Circle(args[i], args[i + 1], args[i + 2]));
                        break;
                    case MaskShape.KindRect when len >= 4:
                        seg.Shapes.Add(MaskShape.Rect(args[i], args[i + 1], args[i + 2], args[i + 3]));
                        break;
                    case MaskShape.KindPolygon:
                    {
                        var pts = new List<double>(len);
                        for (int p = 0; p < len; p++) pts.Add(args[i + p]);
                        if (pts.Count >= 6) seg.Shapes.Add(MaskShape.Polygon(pts));
                        break;
                    }
                }
                i += len;
            }
            e.Segments.Add(seg);
        }
        return e;
    }
}
