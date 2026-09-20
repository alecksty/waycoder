namespace WayCoder;

public static partial class SelfTest
{
    /// <summary>
    /// 绘图**矢量后端**（v0.96.180）：第三条画法（前两条：手搓光栅化、SVG 导出）。
    ///
    /// 为什么这么测：矢量后端最终落在**平台画布**上（MAUI 的 `ICanvas`），而自测跑在桌面上、
    /// 没有平台画布。所以这里不去比像素，而是用一个**记录型落笔面**（把"谁被画了、画成什么"
    /// 一条条记下来），断言"文档 → 落笔"这一层的映射：
    ///
    ///   · 几何来自与另外两条画法**同一个** `DrawGeo`（点集形状对得上）；
    ///   · 变换**先落到点上**（平台侧不做变换，两边坐标语义才一致）；
    ///   · 透明填充、无描边色这些"不画"的判据与光栅侧一致；
    ///   · **每个注册指令都能画出东西**（表驱动：新增指令忘了写矢量画法，这里会红）。
    ///
    /// 真机上的"看起来对不对"另有一层：整屏截图逐项比对（那是只能在设备上做的部分）。
    /// </summary>
    private static void TestChunk23(Action<string> Section, Action<string, bool> Check, Action<string> Fail)
    {
        Section("矢量后端：文档 → 落笔的映射");

        // ── 基元：矩形填充是 4 个点的多边形，颜色原样传下去 ──
        var rec = Paint("rect 10 20 30 40 #ff0000");
        Check("矢量: rect → 一次多边形填充，8 个坐标（4 点）",
            rec.Fills.Count == 1 && rec.Fills[0].Points.Count == 8);
        Check("矢量: rect 的填充色原样传到落笔面",
            rec.Fills.Count == 1 && rec.Fills[0].Color == 0xFFFF0000);
        Check("矢量: rect 的点坐标就是场景坐标（未变换时不偏移）",
            rec.Fills.Count == 1 && rec.Fills[0].Points[0] == 10 && rec.Fills[0].Points[1] == 20);

        // ── 几何取自 DrawGeo：星形是 2n 个点 ──
        var star = Paint("star 50 50 40 20 5 #00ff00");
        Check("矢量: star 5 角 → 2×5 个点（与 SVG/光栅同一个 DrawGeo）",
            star.Fills.Count == 1 && star.Fills[0].Points.Count == 20);

        // ── 变换落到点上（平台侧不再做变换）──
        var moved = Paint("translate 100 0\nrect 10 20 30 40 #ff0000");
        Check("矢量: translate 体现在点坐标上（10+100=110）",
            moved.Fills.Count == 1 && moved.Fills[0].Points[0] == 110);

        // ── 透明填充不画（DSL 里"空心图形"靠全透明表达，光栅侧像素级无效，矢量侧直接跳过）──
        var hollow = Paint("rect 0 0 10 10 #00000000 #ffffff 2");
        Check("矢量: 全透明填充被跳过，只留描边",
            hollow.Fills.Count == 0 && hollow.Strokes.Count == 1);
        Check("矢量: 空心矩形描边闭合（首尾相连）", hollow.Strokes.Count == 1 && hollow.Strokes[0].Close);

        // ── 渐变跟着填充一起下去（几何是"相对这张形状包围盒"的 0..1，落笔面**原样**交给平台）──
        // ⚠ 这句注释原先写的是"由落笔面按场景尺寸换算" —— 那正是 v0.96.182 走弯路的那个错误前提
        // （见 MauiVectorTarget.BuildPaint 的注释：换算方向对、但"相对谁归一化"错了）。
        // 光栅侧 `nx = (lx - minX) / spanX`、SVG 侧 `objectBoundingBox`，都是相对形状的。
        var grad = Paint("gradient g linear #ff0000 #0000ff 0 0 1 0\nrect 0 0 100 50 @g");
        Check("矢量: 渐变填充把 Gradient 传给落笔面（不是烤成纯色）",
            grad.Fills.Count == 1 && grad.Fills[0].Gradient != null);
        Check("矢量: 渐变的线性端点原样保留（0,0 → 1,0）",
            grad.Fills.Count == 1 && grad.Fills[0].Gradient!.X1 == 0 && grad.Fills[0].Gradient!.X2 == 1);

        // ── 文字：走 DrawVector.Text（多行按 1.3 行距拆开，锚点/字号带上变换缩放）──
        var text = Paint("text 10 20 \"你好\\n世界\" 16 #ffffff start");
        Check("矢量: 两行文字拆成两次落笔", text.Texts.Count == 2);
        Check("矢量: 行距 = 字号 × 1.3（与光栅侧同一个行距）",
            text.Texts.Count == 2 && Math.Abs(text.Texts[1].Y - text.Texts[0].Y - 16 * 1.3) < 1e-6);
        Check("矢量: 锚点原样传给落笔面", text.Texts.Count == 2 && text.Texts[0].Anchor == "start");

        // ── path 的多子路径按奇偶规则挖洞 ──
        var path = Paint("path \"M0 0 L40 0 L40 40 L0 40 Z M10 10 L30 10 L30 30 L10 30 Z\" fill #00ff00 2");
        Check("矢量: path 两个子路径 → 一次 evenOdd 填充（挖洞靠它）",
            path.Fills.Count == 1 && path.Fills[0].EvenOdd && path.Fills[0].Subpaths.Count == 2);

        // ── 表驱动：**每个注册指令都要能画出东西**（新增指令忘写矢量画法时会红）──
        //    image 是例外：它要么有文件路径、要么明确 MarkUnsupported（两种都算"交代过了"）。
        var samples = new (string Name, string Dsl, bool CanPaint)[]
        {
            ("rect", "rect 1 2 3 4 #ff0000", true),
            ("roundrect", "roundrect 1 2 30 20 4 #ff0000", true),
            ("circle", "circle 20 20 8 #ff0000", true),
            ("ellipse", "ellipse 20 20 8 5 #ff0000", true),
            ("line", "line 0 0 10 10 #ff0000 1", true),
            ("arrow", "arrow 0 0 10 10 #ff0000 2", true),
            ("polygon", "polygon 0 0 10 0 10 10 #ff0000", true),
            ("polyline", "polyline 0 0 10 0 10 10 #ff0000 1", true),
            ("path", "path \"M0 0 L10 0 L10 10 Z\" fill #ff0000 2", true),
            ("text", "text 5 5 \"x\" 12 #ff0000", true),
            ("star", "star 20 20 10 5 5 #ff0000", true),
            ("regular", "regular 20 20 10 6 #ff0000", true),
            ("ring", "ring 20 20 10 6 #ff0000", true),
            ("pie", "pie 20 20 10 0 90 #ff0000", true),
            ("heart", "heart 20 20 20 #ff0000", true),
            ("image", "image 0 0 10 10 \"nope.png\"", false),
        };
        var missing = new List<string>();
        foreach (var (name, dsl, canPaint) in samples)
        {
            var r = Paint(dsl);
            var painted = r.Fills.Count + r.Strokes.Count + r.Texts.Count + r.Images.Count;
            if (canPaint ? painted == 0 : painted == 0 && r.Unsupported.Count == 0)
                missing.Add(name);
        }
        Check($"矢量: 16 个内置指令都交代了矢量画法（缺 {string.Join("/", missing)}）", missing.Count == 0);

        // ── 落笔面的"画不了"要如实上报（宿主据此回退光栅后端，宁可慢别少画）──
        var unsup = Paint("image 0 0 10 10 \"nope.png\"");
        Check("矢量: 贴图无文件路径 → MarkUnsupported（宿主会回退光栅）",
            unsup.Unsupported.Contains("image"));

        _ = Fail;
    }

    /// <summary>跑一段 DSL，记下落笔面收到了什么。</summary>
    private static RecordingVectorTarget Paint(string dsl)
    {
        var rec = new RecordingVectorTarget();
        var doc = DrawRunner.Parse("canvas 100 100 #00000000\nantialias\n" + dsl);
        foreach (var f in doc.Figures)
            DrawCommandRegistry.Get(f.Kind)?.Vector(rec, f);
        return rec;
    }

    /// <summary>
    /// 记录型落笔面：**不画，只记**。桌面自测没有平台画布，而矢量后端的错法多半是
    /// "几何没传对""该跳过的没跳过"—— 这些在落笔这一层就看得出来，不必等上真机。
    /// </summary>
    private sealed class RecordingVectorTarget : IVectorTarget
    {
        public sealed class Fill
        {
            public List<double> Points = new();
            public List<IReadOnlyList<double>> Subpaths = new();
            public uint Color;
            public Gradient? Gradient;
            public bool EvenOdd;
            /// <summary>显式刷子矩形（世界坐标）；null = 用子路径自己的外接矩形。</summary>
            public (double MinX, double MinY, double MaxX, double MaxY)? Box;
        }

        public sealed class Stroke
        {
            public List<double> Points = new();
            public uint Color;
            public double Width;
            public string Cap = "butt";
            public bool Dashed;
            public bool Close;
        }

        public sealed class TextCall
        {
            public string Text = "";
            public double X, Y, Size;
            public uint Color;
            public string Anchor = "start";
            public bool Bold, Italic;
        }

        public sealed class ImageCall
        {
            public string? Path;
            public double X, Y, W, H;
        }

        public readonly List<Fill> Fills = new();
        public readonly List<Stroke> Strokes = new();
        public readonly List<TextCall> Texts = new();
        public readonly List<ImageCall> Images = new();
        public readonly HashSet<string> Unsupported = new(StringComparer.Ordinal);

        public double SceneWidth => 100;
        public double SceneHeight => 100;

        public void FillShape(IReadOnlyList<IReadOnlyList<double>> subpaths, uint fill, Gradient? gradient,
            bool evenOdd) => FillShape(subpaths, fill, gradient, evenOdd, null);

        public void FillShape(IReadOnlyList<IReadOnlyList<double>> subpaths, uint fill, Gradient? gradient,
            bool evenOdd, (double MinX, double MinY, double MaxX, double MaxY)? box)
        {
            var f = new Fill { Color = fill, Gradient = gradient, EvenOdd = evenOdd, Box = box };
            foreach (var sp in subpaths)
            {
                f.Subpaths.Add(sp);
                foreach (var v in sp) f.Points.Add(v);
            }
            Fills.Add(f);
        }

        public void StrokePolyline(IReadOnlyList<double> pts, double width, uint color, string cap, bool dashed, bool close)
        {
            var s = new Stroke { Color = color, Width = width, Cap = cap, Dashed = dashed, Close = close };
            foreach (var v in pts) s.Points.Add(v);
            Strokes.Add(s);
        }

        public void DrawText(double x, double y, string text, double size, uint color, string anchor, bool bold, bool italic)
            => Texts.Add(new TextCall { X = x, Y = y, Text = text, Size = size, Color = color, Anchor = anchor, Bold = bold, Italic = italic });

        public void DrawImage(string? path, double x, double y, double w, double h,
            double srcX, double srcY, double srcW, double srcH, double cornerRadius, bool transformed)
        {
            if (string.IsNullOrEmpty(path) || transformed || !File.Exists(path)) { MarkUnsupported("image", path); return; }
            Images.Add(new ImageCall { Path = path, X = x, Y = y, W = w, H = h });
        }

        public void MarkUnsupported(string kind, string? detail = null) => Unsupported.Add(kind);
    }
}
