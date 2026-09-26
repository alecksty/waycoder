using System.Globalization;
using System.Text;

namespace WayCoder.Infra;

/// <summary>
/// 手搓绘图引擎（AOT 安全：零反射，不依赖 System.Drawing / System.Xml / System.Text.Json）。
/// 文本 DSL → 图元列表 → SVG（矢量）或 PNG（光栅化）双输出。
/// 指令可扩展：实现 <see cref="IDrawCommand"/> 并注册到 <see cref="DrawCommandRegistry"/>。
/// </summary>

/// <summary>颜色工具：解析 #hex / 命名色 → ARGB，反序列化 ARGB → #hex。</summary>
public static class ColorUtil
{
    static readonly Dictionary<string, uint> Named = new(StringComparer.OrdinalIgnoreCase)
    {
        ["black"] = 0xFF000000, ["white"] = 0xFFFFFFFF,
        ["red"] = 0xFFFF0000, ["green"] = 0xFF008000, ["blue"] = 0xFF0000FF,
        ["yellow"] = 0xFFFFFF00, ["cyan"] = 0xFF00FFFF, ["magenta"] = 0xFFFF00FF,
        ["gray"] = 0xFF808080, ["grey"] = 0xFF808080,
        ["orange"] = 0xFFFFA500, ["purple"] = 0xFF800080, ["pink"] = 0xFFFFC0CB,
        ["brown"] = 0xFFA52A2A, ["navy"] = 0xFF000080, ["teal"] = 0xFF008080,
        ["lime"] = 0xFF00FF00, ["maroon"] = 0xFF800000, ["olive"] = 0xFF808000,
        ["silver"] = 0xFFC0C0C0, ["gold"] = 0xFFFFD700, ["transparent"] = 0x00000000,
    };

    /// <summary>解析颜色：支持 #rgb / #rrggbb / #rrggbbaa / 命名色；失败返回 fallback。</summary>
    public static uint Parse(string? s, uint fallback)
    {
        if (string.IsNullOrWhiteSpace(s)) return fallback;
        s = s.Trim();
        if (s[0] == '#')
        {
            var hex = s[1..];
            if (hex.Length == 3)
            {
                int r = Hex(hex[0]), g = Hex(hex[1]), b = Hex(hex[2]);
                if (r < 0 || g < 0 || b < 0) return fallback;
                return 0xFF000000u | ((uint)(r * 17) << 16) | ((uint)(g * 17) << 8) | (uint)(b * 17);
            }
            if (hex.Length == 6 || hex.Length == 8)
            {
                if (uint.TryParse(hex, NumberStyles.HexNumber, CultureInfo.InvariantCulture, out var v))
                    return hex.Length == 6 ? 0xFF000000u | v : v;
            }
            return fallback;
        }
        return Named.TryGetValue(s, out var c) ? c : fallback;
    }

    static int Hex(char c) => c switch
    {
        >= '0' and <= '9' => c - '0',
        >= 'a' and <= 'f' => c - 'a' + 10,
        >= 'A' and <= 'F' => c - 'A' + 10,
        _ => -1,
    };

    /// <summary>严格解析颜色：仅当 s 是合法 #hex 或命名色时返回 true。</summary>
    public static bool TryParse(string? s, out uint c)
    {
        c = 0;
        if (string.IsNullOrWhiteSpace(s)) return false;
        s = s.Trim();
        if (s.Length >= 2 && s[0] == '#')
        {
            var hex = s[1..];
            if (hex.Length == 3)
            {
                if (Hex(hex[0]) < 0 || Hex(hex[1]) < 0 || Hex(hex[2]) < 0) return false;
                c = Parse(s, 0);
                return true;
            }
            if (hex.Length == 6 || hex.Length == 8)
            {
                if (!uint.TryParse(hex, NumberStyles.HexNumber, CultureInfo.InvariantCulture, out _)) return false;
                c = Parse(s, 0);
                return true;
            }
            return false;
        }
        return Named.TryGetValue(s, out c);
    }

    public static byte R(uint c) => (byte)(c >> 16);
    public static byte G(uint c) => (byte)(c >> 8);
    public static byte B(uint c) => (byte)c;
    public static byte A(uint c) => (byte)(c >> 24);

    /// <summary>ARGB → "#rrggbb"（不透明时）或 "#rrggbbaa"（含透明时）。</summary>
    public static string ToHex(uint c)
        => A(c) == 255 ? $"#{(c & 0x00FFFFFFu):x6}" : $"#{c:x8}";
}

/// <summary>
/// 2D 仿射变换矩阵（对应 SVG matrix(a b c d e f)：x'=a·x+c·y+e，y'=b·x+d·y+f）。
/// 变换指令 translate/rotate/scale 组合成的当前变换，绘制时应用到图元。
/// </summary>
public readonly struct Affine : IEquatable<Affine>
{
    public readonly double A, B, C, D, E, F;

    /// <summary>
    /// 逐字段比较。
    ///
    /// <para>
    /// ⚠ 必须**显式实现**：默认的 <c>ValueType.Equals</c> 走反射（或至少是一次装箱比较），
    /// 而这里的调用点是**每帧、每图元一次**（矢量后端拿它判断"这个图元和上一个是不是
    /// 同一个变换"）。托管侧的反射比较在热路径上比这个手写版慢两个数量级。
    /// </para>
    /// </summary>
    public bool Equals(Affine o)
        => A == o.A && B == o.B && C == o.C && D == o.D && E == o.E && F == o.F;

    public override bool Equals(object? o) => o is Affine a && Equals(a);

    public override int GetHashCode() => HashCode.Combine(A, B, C, D, E, F);
    public Affine(double a, double b, double c, double d, double e, double f)
    { A = a; B = b; C = c; D = d; E = e; F = f; }

    public static readonly Affine Identity = new(1, 0, 0, 1, 0, 0);
    public bool IsIdentity => A == 1 && B == 0 && C == 0 && D == 1 && E == 0 && F == 0;

    /// <summary>均匀缩放因子（行列式平方根）。恒等/纯旋转为 1，纯缩放为缩放比。</summary>
    public double ScaleFactor => Math.Sqrt(Math.Abs(A * D - B * C));

    /// <summary>
    /// 是不是"轴向等比缩放 + 平移"（无旋转、无错切、两轴同倍率）。
    ///
    /// 用途：抗锯齿走的是"整幅放大 3 倍再降采样"，也就是给每个图元挂一个 <c>Scale(3,3)</c>。
    /// 而各图元的 <c>Rasterize</c> 一旦发现变换不是恒等，就会退到
    /// <c>FillTransformed</c> 那条**逐像素布尔判定**的路 —— 圆角矩形要按 40 个点的多边形
    /// 判、圆要按距离判，代价是每像素一次 O(点数)。实测 120 个圆角矩形在 3× 下要 1.1 秒。
    /// 但对"等比缩放"这种变换，形状缩完**还是同一个形状**（圆角矩形还是圆角矩形），
    /// 直接把参数乘上倍率走原来的整数扫描线路径即可 —— 输出几乎相同，快两个数量级。
    /// </summary>
    public bool TryAxisScale(out double sx, out double sy)
    {
        sx = A; sy = D;
        return B == 0 && C == 0 && A > 0 && D > 0;
    }

    /// <summary>
    /// 是不是**刚体变换**（只有旋转 + 平移：两轴正交、且长度都是 1）。
    ///
    /// <para>
    /// 用途：矢量后端据此决定"能不能把变换交给平台坐标系"（见
    /// <c>IVectorTarget.PushTransform</c>）。刚体变换**不改变形状** —— 矩形还是矩形、
    /// 圆还是圆、线宽与圆角半径都不变 —— 所以「把矩阵挂到画布上、图元照常画」
    /// 与「逐点变换后再画折线」**结果完全一致**，却省掉了每个点一次的平台路径构建
    /// （真机实测：带旋转的图块走折线时，一帧 15000 次通用填充要 2.9 秒）。
    /// </para>
    ///
    /// <para>
    /// ⚠ **不能放宽成"等比缩放也算"**：那时线宽会跟着放大，而本仓的语义是
    /// <c>StrokeWidth</c> **不**随变换走（`DrawVector.Stroke` 就是把点变换掉、线宽原样传下去）。
    /// 一旦放宽，同一个程序的描边会突然变粗 —— 那是观感变化，不是优化。
    /// 同理圆角半径、虚线相位也都只在刚体下才保证不变。
    /// </para>
    ///
    /// <para>
    /// 容差取 <c>1e-6</c> 而不是机器精度：变换是 <c>Compose</c> 累乘出来的，
    /// 转几次之后 `cos²+sin²` 与 1 的差会到 1e-15 量级，1e-9 在长链上仍可能失手；
    /// 而"真的缩放了 0.0001%"这种情形本来就不存在（程序里的缩放都是整数比）。
    /// </para>
    /// </summary>
    public bool IsRigid
    {
        get
        {
            const double eps = 1e-6;
            return Math.Abs(A * A + B * B - 1) < eps
                && Math.Abs(C * C + D * D - 1) < eps
                && Math.Abs(A * C + B * D) < eps;
        }
    }

    public static Affine Translate(double dx, double dy) => new(1, 0, 0, 1, dx, dy);
    public static Affine Scale(double sx, double sy) => new(sx, 0, 0, sy, 0, 0);
    public static Affine Rotate(double deg)
    {
        var r = deg * Math.PI / 180.0;
        var c = Math.Cos(r);
        var s = Math.Sin(r);
        return new(c, s, -s, c, 0, 0);
    }

    /// <summary>绕点 (px,py) 旋转：T(px,py) ∘ R ∘ T(-px,-py)。</summary>
    public static Affine Rotate(double deg, double px, double py)
        => Translate(px, py).Compose(Rotate(deg)).Compose(Translate(-px, -py));

    /// <summary>组合：this ∘ other（先应用 other，再应用 this）。</summary>
    public Affine Compose(Affine o) => new(
        A * o.A + C * o.B, B * o.A + D * o.B,
        A * o.C + C * o.D, B * o.C + D * o.D,
        A * o.E + C * o.F + E, B * o.E + D * o.F + F);

    public (double X, double Y) Apply(double x, double y)
        => (A * x + C * y + E, B * x + D * y + F);

    public Affine Inverse()
    {
        double det = A * D - B * C;
        if (Math.Abs(det) < 1e-12) return Identity;
        double ia = D / det, ib = -B / det, ic = -C / det, id = A / det;
        double ie = -(ia * E + ic * F), if_ = -(ib * E + id * F);
        return new(ia, ib, ic, id, ie, if_);
    }

    public override string ToString()
        => $"matrix({Fmt(A)} {Fmt(B)} {Fmt(C)} {Fmt(D)} {Fmt(E)} {Fmt(F)})";
    static string Fmt(double v) => Math.Abs(v) < 1e-9 ? "0" : v.ToString("0.###", CultureInfo.InvariantCulture);
}

/// <summary>渐变定义（形状 fill 用 @id 引用）。坐标归一化到 0..1（SVG objectBoundingBox 约定）。</summary>
public sealed class Gradient
{
    public string Id = "";
    public bool Radial = false;
    public uint ColorA = 0xFF000000;
    public uint ColorB = 0xFFFFFFFF;
    // linear 端点（归一化）
    public double X1 = 0, Y1 = 0, X2 = 1, Y2 = 0;
    // radial 中心/半径（归一化）
    public double Cx = 0.5, Cy = 0.5, R = 0.5;
}

/// <summary>分词 token：Value 为内容，Quoted 表示是否来自双引号字符串。</summary>
public readonly struct DrawToken
{
    public string Value { get; }
    public bool Quoted { get; }
    public DrawToken(string value, bool quoted) { Value = value; Quoted = quoted; }
}

/// <summary>绘图 DSL 分词器：空白/逗号作分隔（引号内除外），双引号内容为单个带引号 token。</summary>
public static class DrawTokenizer
{
    public static List<DrawToken> Tokenize(string line)
    {
        var tokens = new List<DrawToken>();
        int i = 0, n = line.Length;
        while (i < n)
        {
            char c = line[i];
            if (char.IsWhiteSpace(c) || c == ',') { i++; continue; }
            if (c == '"')
            {
                i++;
                var sb = new StringBuilder();
                while (i < n && line[i] != '"')
                {
                    if (line[i] == '\\' && i + 1 < n)
                    {
                        var nxt = line[i + 1];
                        if (nxt == '"' || nxt == '\\') i++; // 仅 \" 与 \\ 转义，其余反斜杠（如 Windows 路径）原样保留
                    }
                    sb.Append(line[i]);
                    i++;
                }
                i++; // 跳过闭合引号
                tokens.Add(new DrawToken(sb.ToString(), true));
            }
            else
            {
                var start = i;
                while (i < n && !char.IsWhiteSpace(line[i]) && line[i] != ',')
                    i++;
                tokens.Add(new DrawToken(line[start..i], false));
            }
        }
        return tokens;
    }
}

/// <summary>已解析的绘图图元。数值参数放 Args，文本/路径数据放 Text。</summary>
public sealed class DrawFigure
{
    public string Kind = "";
    public readonly List<double> Args = new();
    public string? Text;

    /// <summary>
    /// **子图元**（只给 `layer` 用）。解析期把 `layer_begin..layer_end` 之间的图元挂上来，
    /// `LayerCommand.Rasterize` 再把它们画进一块临时画布、整层按 alpha 合成。
    ///
    /// ⚠ 为什么不是"塞进 `Args`"（蒙版就是那么做的）：蒙版的形状是**几个数**
    /// （圆 = 三四个 double、矩形 = 四个），而图层里装的是**任意图元** ——
    /// 它们有颜色、文字、渐变引用、点列表，塞不进一个 `List&lt;double&gt;`。
    /// ⚠ 为什么不能"让指令回头去查文档"：`IDrawCommand.Rasterize(Canvas, DrawFigure)`
    ///   的签名**只看得到自己那一个图元** —— 所以收集必须在**解析期**做，那里才看得到全局。
    /// </summary>
    public List<DrawFigure>? Children;
    public uint Fill = 0xFF000000;
    /// <summary>
    /// 是否**真的要填充**。<see cref="Fill"/> 默认是黑色，不能拿"它非零"当判据 ——
    /// 描边类图元（line/polyline/arrow/path 的老写法）不填充但 Fill 有值，
    /// 拿 Fill 判断会把它们全填成黑块。
    /// </summary>
    public bool FillSet = true;
    public uint Stroke = 0;
    public double StrokeWidth = 1;
    public string LineCap = "butt"; // 线头形状：butt | round | square
    /// <summary>虚线开关（line/arrow/polyline 支持 dash 关键字）：SVG 用 stroke-dasharray，PNG 按段绘制。</summary>
    public bool Dashed;
    public double FontSize = 14;
    public string Anchor = "start";
    /// <summary>
    /// 文字**竖对齐**：`top`（默认）| `center` | `bottom`。
    ///
    /// 为什么默认是"顶"：`TextBlockBox` 的 `Y` 本来就等于文字图元的 `y`（盒顶 = `y`），
    /// 也就是说**老行为就是顶对齐** —— 所以加这一档是**纯增量**，老程序一个像素都不变。
    ///
    /// 用户报的问题正是缺这一档：程序写 `text x y "…" … middle`（横锚点=中）时，
    /// 文字**横向居中了、纵向却仍顶着 y** ⇒ 放在方框/按钮正中时看着偏上。
    /// 修之前只能靠程序自己估字号、手工减半个行高 —— 每个例子各估一次，还估不准。
    /// </summary>
    /// <summary>
    /// 竖对齐（见 `DrawParse.TextVOffset`）：`base` = 基线落在 y（**默认 = 老行为**）、
    /// `center` / `bottom` / `top`。
    ///
    /// ⚠ 默认值**必须是 `base`**：它对应"偏移 0"＝既有程序的渲染结果。
    ///   这个字段原先默认 `"top"`，而新模型里 `top` = 盒顶落在 y = 基线**下移一个上升**
    ///   ⇒ 一旦默认值不动，**所有没设竖对齐的文字会整体下移 0.8×字号**（真机上是"字都跑到框下面去了"）。
    ///   是自测里的 VAlign 判据抓出来的。
    /// </summary>
    public string VAnchor = "base";
    public string FontFamily = "sans-serif";
    public string FontWeight = "normal";
    public string FontStyle = "normal";
    /// <summary>解析时捕获的当前仿射变换（默认恒等）。</summary>
    public Affine Transform = Affine.Identity;
    /// <summary>渐变引用 id（fill 以 @ 开头时设置），否则 null 表示纯色填充。</summary>
    public string? GradientRef;
    /// <summary>解析完成后解析出的渐变定义（GradientRef 命中时）；未命中/未引用为 null。</summary>
    public Gradient? Gradient;
    /// <summary>
    /// **描边（画笔）**的渐变引用 id：来自 `stroke @id` 或描边类图元（line/arrow/polyline）的裸 `@id`。
    /// null = 纯色描边（走 <see cref="Stroke"/>）。与 <see cref="GradientRef"/> **互相独立** ——
    /// `rect … fill @a stroke @b 3` 是合法的（边框一个渐变、填充一个渐变）。
    /// </summary>
    public string? StrokeGradientRef;
    /// <summary>解析完成后解析出的**描边**渐变定义（StrokeGradientRef 命中时）；未命中/未引用为 null。</summary>
    public Gradient? StrokeGradient;
    /// <summary>image 指令：贴图原始路径（svg 输入或加载失败时保留，供 SVG 端透传引用）。</summary>
    public string? ImagePath;
    /// <summary>image 指令：已解码的位图（加载失败为 null，PNG 端跳过、SVG 端回退引用路径）。</summary>
    public RasterImage? Image;
    /// <summary>image 指令：源图裁剪矩形（像素坐标）。SrcW/SrcH ≤ 0 表示全图不裁剪。</summary>
    public double SrcX = 0, SrcY = 0, SrcW = 0, SrcH = 0;
    /// <summary>image 指令：目标裁剪圆角半径（0 = 直角矩形）。</summary>
    public double CornerRadius = 0;
    /// <summary>image 指令：SVG 端 clipPath 的 id（ToSvg 阶段按文档内顺序分配，保证唯一）。</summary>
    public string? ClipId;
    /// <summary>
    /// text 指令：SVG 端**文字专用渐变**的 id（ToSvg 阶段分配）。
    ///
    /// 为什么文字要单独一份渐变定义、不能与形状共用那一份：
    /// 形状用 `objectBoundingBox`（坐标相对**几何盒**归一化），而文字在 SVG 里那个盒
    /// 由渲染器**按字形墨迹**算 —— 与我们 `TextBlockBox`（行高 × 行数 + 最长行宽）
    /// 必然不等，PNG 与 SVG 的渐变位置会差一截。
    /// 所以文字那份必须 `gradientUnits="userSpaceOnUse"`，坐标按 `TextBlockBox` 算成绝对值。
    /// 同一份渐变被多个文字图元引用时各自一份（盒不同），故按图元分配。
    /// </summary>
    public string? TextGradientId;
}

/// <summary>绘图指令接口。插件可自定义实现并注册到 <see cref="DrawCommandRegistry"/>。</summary>
public interface IDrawCommand
{
    string Name { get; }
    /// <summary>解析参数（不含命令名）为图元；返回 null 表示参数错误。</summary>
    DrawFigure? Parse(IReadOnlyList<DrawToken> args);
    /// <summary>发射 SVG 片段。</summary>
    void EmitSvg(StringBuilder sb, DrawFigure f);
    /// <summary>光栅化到像素画布。</summary>
    void Rasterize(Canvas c, DrawFigure f);
    /// <summary>
    /// 画到平台矢量画布（第三条路，见 <see cref="IVectorTarget"/>）。
    /// **必需成员而不是默认空实现**：漏一个指令就会静默少画东西，那种错只有上手机才看得出；
    /// 编不过反而是最省事的提醒。
    /// </summary>
    void Vector(IVectorTarget t, DrawFigure f);
}

/// <summary>绘图指令注册表。内置指令经 [ModuleInitializer] 自动注册，插件亦可注册自定义指令。</summary>
public static class DrawCommandRegistry
{
    static readonly Dictionary<string, IDrawCommand> Cmds = new(StringComparer.OrdinalIgnoreCase);

    public static void Register(IDrawCommand cmd)
    {
        if (cmd == null || string.IsNullOrWhiteSpace(cmd.Name)) return;
        Cmds[cmd.Name] = cmd;
    }

    public static IDrawCommand? Get(string name)
        => Cmds.TryGetValue(name, out var c) ? c : null;

    public static bool Contains(string name) => Cmds.ContainsKey(name);

    public static IEnumerable<string> Names => Cmds.Keys;
}

/// <summary>解析后的绘图文档：画布尺寸/背景 + 图元列表 + 错误信息。</summary>
public sealed class DrawDocument
{
    public int Width = 800;
    public int Height = 600;
    public uint Background = 0xFFFFFFFF;
    public bool Antialias = false; // 消除锯齿（PNG 端超采样降采样）
    public readonly List<DrawFigure> Figures = new();
    public readonly List<Gradient> Gradients = new();

    public string? Error;
}

/// <summary>绘图运行器：DSL 解析 + SVG/PNG 渲染编排。</summary>
public static class DrawRunner
{
    /// <summary>画布像素数上限（防 `canvas W H` 超大尺寸导致 OOM），约 25MP。</summary>
    private const long MaxCanvasPixels = 25_000_000;

    /// <summary>解析 DSL 文本为文档。canvas 设置画布，其余为图元；非法行记入 Error 并跳过。</summary>
    public static DrawDocument Parse(string dsl)
    {
        var doc = new DrawDocument();
        if (string.IsNullOrWhiteSpace(dsl)) { doc.Error = "空输入"; return doc; }

        var current = Affine.Identity;
        var stack = new Stack<Affine>();
        List<DrawFigure>? maskBuf = null;   // 蒙版收集缓冲（`mask_begin` 开、`mask_end` 收）
        List<DrawFigure>? layerBuf = null;  // 图层收集缓冲（`layer_begin` 开、`layer_end` 收）
        // 当前蒙版的**累积段列表** —— 布尔运算必须有一处记着"当前蒙版是什么"
        // （`mask_end2(op)` 的语义就是"与它组合"）。见下面那段的长注释。
        var maskSegs = new List<MaskExpr.Segment>();
        bool maskInside = true;

        foreach (var raw in dsl.Split('\n'))
        {
            var line = raw.Trim();
            if (line.Length == 0 || line.StartsWith('#') || line.StartsWith("//")) continue;
            var tokens = DrawTokenizer.Tokenize(line);
            if (tokens.Count == 0) continue;
            var name = tokens[0].Value;
            var args = tokens.Skip(1).ToList();

            if (name.Equals("canvas", StringComparison.OrdinalIgnoreCase))
            {
                ParseCanvas(doc, args);
                continue;
            }
            if (name.Equals("push", StringComparison.OrdinalIgnoreCase)) { stack.Push(current); continue; }
            if (name.Equals("pop", StringComparison.OrdinalIgnoreCase))
            {
                current = stack.Count > 0 ? stack.Pop() : Affine.Identity;
                continue;
            }
            if (name.Equals("translate", StringComparison.OrdinalIgnoreCase) && args.Count >= 2)
            {
                current = current.Compose(Affine.Translate(Num(args[0]), Num(args[1])));
                continue;
            }
            if (name.Equals("scale", StringComparison.OrdinalIgnoreCase) && args.Count >= 1)
            {
                double sy = args.Count >= 2 ? Num(args[1]) : Num(args[0]);
                current = current.Compose(Affine.Scale(Num(args[0]), sy));
                continue;
            }
            if (name.Equals("rotate", StringComparison.OrdinalIgnoreCase) && args.Count >= 1)
            {
                current = args.Count >= 3
                    ? current.Compose(Affine.Rotate(Num(args[0]), Num(args[1]), Num(args[2])))
                    : current.Compose(Affine.Rotate(Num(args[0])));
                continue;
            }
            if (name.Equals("antialias", StringComparison.OrdinalIgnoreCase) || name.Equals("aa", StringComparison.OrdinalIgnoreCase))
            {
                doc.Antialias = args.Count == 0 || !args[0].Value.Equals("off", StringComparison.OrdinalIgnoreCase);
                continue;
            }
            if (name.Equals("gradient", StringComparison.OrdinalIgnoreCase))
            {
                var g = ParseGradient(args);
                if (g == null) doc.Error = $"参数错误: {line}";
                else doc.Gradients.Add(g);
                continue;
            }
            if (name.Equals("icon", StringComparison.OrdinalIgnoreCase))
            {
                ParseIcon(doc, args);
                continue;
            }
            if (name.Equals("flowchart", StringComparison.OrdinalIgnoreCase))
            {
                FlowchartCommand.Build(doc, args);
                continue;
            }

            // ── 蒙版：`mask_begin` … `mask_end [inside]` / `mask_end2 <op>` ──────
            // 中间画的形状**不上屏**，只被收成一条蒙版定义（见 `MaskCommand`）。
            // ⚠ 收集要在**解析期**做，不能留给指令：指令拿不到"后面还有什么"。
            //
            // ⚠ **布尔运算的累积也在这里，不在 `VmlScene`**：`mask_end2(op)` 的语义是
            //   "与**当前**蒙版按 op 组合"，所以必须有一处记着当前蒙版是什么；而解码那头
            //   （`MaskCommand.Rasterize`）每次只看到**一条自足的图元**（`Rasterize` 的签名
            //   够不到文档）⇒ 索性每发一条就把**完整段列表**带上（见 `MaskExpr.Encode`）。
            //   累积放在解析期还有个好处：它是**唯一**一处，VmlScene 那边只管发一行文本。
            if (name.Equals("mask_begin", StringComparison.OrdinalIgnoreCase))
            {
                maskBuf ??= new List<DrawFigure>();
                continue;
            }
            if (name.Equals("mask_end", StringComparison.OrdinalIgnoreCase)
                || name.Equals("mask_end2", StringComparison.OrdinalIgnoreCase))
            {
                var shapes = ExtractMaskShapes(maskBuf);
                maskBuf = null;

                int op = MaskOp.Replace;
                if (name.Equals("mask_end", StringComparison.OrdinalIgnoreCase))
                {
                    // 老形态：带 inside，且**整体取代**当前蒙版
                    //（v0.96.465 之前就是"替换"语义 —— 老程序的画面必须逐像素不变）
                    maskInside = args.Count < 1 || DrawParse.Num(args[0]) != 0;
                }
                else
                {
                    if (args.Count < 1) continue;
                    op = (int)DrawParse.Num(args[0]);
                    // 不认得的运算符：整条忽略（**别当 0 蒙混** —— 那会把"程序写错了"
                    // 变成"画面莫名其妙不一样"，最难查的一类）
                    if (!MaskOp.IsValid(op)) continue;
                }

                // 累积语义与运行时镜像（`VmlScene.ApplyMaskSegment`）**共用这一处**，
                // 否则 `ui_mask_test` 查到的蒙版会与画出来的不是一个（见 `MaskExpr.ApplySegment`）
                MaskExpr.ApplySegment(maskSegs, op, shapes);

                var mf2 = new DrawFigure { Kind = "mask" };
                MaskExpr.Encode(mf2, maskInside, maskSegs);
                mf2.Transform = current;
                doc.Figures.Add(mf2);
                continue;
            }

            // ── 图层：`layer_begin` … `layer_end [alpha]` ─────────────────────
            // 与蒙版**同一个理由**必须在解析期收集（指令只看得到自己那一个图元），
            // 但**去向相反**：蒙版的形状不上屏，图层的子图元要上屏（经过一次离屏合成）。
            if (name.Equals("layer_begin", StringComparison.OrdinalIgnoreCase))
            {
                layerBuf ??= new List<DrawFigure>();
                continue;
            }
            if (name.Equals("layer_end", StringComparison.OrdinalIgnoreCase))
            {
                var lf = new DrawFigure { Kind = "layer" };
                // alpha 收成 0..1：DSL 里写 0..255 的整数更贴近 C 的用法，
                // 换算**只在这一处**（两端各算一次就是本仓记过的"沉默的错"）
                double a255 = args.Count >= 1 ? DrawParse.Num(args[0]) : 255;
                lf.Args.Add(Math.Clamp(a255, 0, 255) / 255.0);
                lf.Children = layerBuf ?? new List<DrawFigure>();
                lf.Transform = current;
                layerBuf = null;
                doc.Figures.Add(lf);
                continue;
            }

            var cmd = DrawCommandRegistry.Get(name);
            if (cmd == null) { doc.Error = $"未知指令: {name}"; continue; }
            var fig = cmd.Parse(args);
            if (fig == null) { doc.Error = $"参数错误: {line}"; continue; }
            fig.Transform = current;
            // ⚠ **两个收集器同时开着时，蒙版先拿**。顺序反了会踩这个坑：
            //   形状被图层缓冲截走 ⇒ `mask_end` 收到一个**空蒙版** ⇒ 层内的蒙版**静默失效**，
            //   而且那些形状还会被当成普通图元画出来（"蒙版没用、形状照画"）。
            //   现在的顺序下，`mask_begin..mask_end` 之间的形状只进蒙版缓冲、不上屏，
            //   而 `mask_end` 生成的那条 `mask` 图元照常落进图层缓冲
            //   ⇒ **层内的蒙版独立生效**（在临时画布上），语义正好。
            if (maskBuf != null) { maskBuf.Add(fig); continue; }
            // 图层收集期：进缓冲（**照常上屏**，只是晚一步、经过合成）
            if (layerBuf != null) { layerBuf.Add(fig); continue; }
            doc.Figures.Add(fig);
        }

        // 解析完成后，把 @id 引用解析为渐变定义；未命中则退化为纯色。
        if (doc.Gradients.Count > 0)
        {
            var map = new Dictionary<string, Gradient>(StringComparer.Ordinal);
            foreach (var g in doc.Gradients) map[g.Id] = g;
            foreach (var f in doc.Figures)
            {
                // 填充与描边**各解析一次**，不是"二选一" —— 同一份渐变可以被两者同时引用
                // （`rect … fill @g stroke @g 3`：边框与填充同一个渐变）。
                if (f.GradientRef != null)
                {
                    if (map.TryGetValue(f.GradientRef, out var g)) f.Gradient = g;
                    else f.GradientRef = null;   // 悬空引用退化成纯色，与老行为一致
                }
                if (f.StrokeGradientRef != null)
                {
                    if (map.TryGetValue(f.StrokeGradientRef, out var sg)) f.StrokeGradient = sg;
                    else f.StrokeGradientRef = null;
                }
            }
        }
        return doc;
    }

    /// <summary>
    /// 把 `mask_begin` 期间收集到的图元转成**能闭式判定**的蒙版形状。
    ///
    /// **只认圆 / 矩形 / 多边形**（多边形来自 `path` 与 `polygon`）：别的形状（椭圆、星形、
    /// 文字、图像…）在蒙版里**明确忽略** —— 与其"看着支持了其实不生效"，不如不认。
    ///
    /// `roundrect` 当矩形处理（**忽略圆角**）：蒙版是"哪些点可见"，圆角差那几个像素
    /// 不值得为它单开一种形状。真要圆角蒙版，用 `path` 画一条带 `A` 的路径。
    ///
    /// ⚠ `path` 走的是 `DrawPath.Flatten`（**含贝塞尔与圆弧**，按 SVG 规范展平）——
    ///   它与光栅器画 `path` 图元用的是**同一个展平器**，所以"同一个形状当图元画"
    ///   与"当蒙版用"边界严丝合缝。**别为蒙版另写一个曲线展平器**（本仓头号坑）。
    /// </summary>
    static List<MaskShape> ExtractMaskShapes(List<DrawFigure>? buf)
    {
        var shapes = new List<MaskShape>();
        if (buf == null) return shapes;

        foreach (var mf in buf)
        {
            switch (mf.Kind)
            {
                case "circle" when mf.Args.Count >= 3:
                    shapes.Add(MaskShape.Circle(mf.Args[0], mf.Args[1], mf.Args[2]));
                    break;
                case "rect" or "roundrect" when mf.Args.Count >= 4:
                    shapes.Add(MaskShape.Rect(mf.Args[0], mf.Args[1], mf.Args[2], mf.Args[3]));
                    break;
                case "polygon" when mf.Args.Count >= 6:
                    shapes.Add(MaskShape.Polygon(new List<double>(mf.Args)));
                    break;
                case "path":
                    // 一条 `d` 可以开多条子路径（`M…Z M…Z`）：**每条子路径各算一个形状**，
                    // 而不是把点全串成一条折线 —— 串起来会凭空多出一条连接边，
                    // 那块区域被判成"在形状里"，洞就填上了。
                    foreach (var sp in DrawPath.Flatten(mf.Text))
                    {
                        if (sp.Points.Count < 3) continue;
                        var flat = new List<double>(sp.Points.Count * 2);
                        foreach (var (px, py) in sp.Points) { flat.Add(px); flat.Add(py); }
                        shapes.Add(MaskShape.Polygon(flat));
                    }
                    break;
                default:
                    break;   // 明确忽略（见上面的说明）
            }
        }
        return shapes;
    }

    static double Num(DrawToken t)
        => double.TryParse(t.Value, NumberStyles.Float, CultureInfo.InvariantCulture, out var v) ? v : 0;

    /// <summary>解析 gradient 定义：gradient id linear|radial cA cB [坐标…]。</summary>
    static Gradient? ParseGradient(IReadOnlyList<DrawToken> args)
    {
        if (args.Count < 4) return null; // id type cA cB
        bool radial = args[1].Value.Equals("radial", StringComparison.OrdinalIgnoreCase);
        bool linear = args[1].Value.Equals("linear", StringComparison.OrdinalIgnoreCase);
        if (!radial && !linear) return null;
        var g = new Gradient
        {
            Id = args[0].Value,
            Radial = radial,
            ColorA = ColorUtil.Parse(args[2].Value, 0xFF000000),
            ColorB = ColorUtil.Parse(args[3].Value, 0xFFFFFFFF),
        };
        if (radial)
        {
            if (args.Count >= 7) { g.Cx = Num(args[4]); g.Cy = Num(args[5]); g.R = Num(args[6]); }
        }
        else
        {
            if (args.Count >= 8) { g.X1 = Num(args[4]); g.Y1 = Num(args[5]); g.X2 = Num(args[6]); g.Y2 = Num(args[7]); }
        }
        return g;
    }

    static void ParseCanvas(DrawDocument doc, IReadOnlyList<DrawToken> args)
    {
        if (args.Count >= 2)
        {
            int nw = doc.Width, nh = doc.Height;
            if (double.TryParse(args[0].Value, NumberStyles.Float, CultureInfo.InvariantCulture, out var w) && w > 0)
                nw = (int)Math.Round(w);
            if (double.TryParse(args[1].Value, NumberStyles.Float, CultureInfo.InvariantCulture, out var h) && h > 0)
                nh = (int)Math.Round(h);
            if (nw <= 0 || nh <= 0 || (long)nw * nh > MaxCanvasPixels)
            {
                doc.Error = "画布尺寸非法或过大";
                return; // 保留默认尺寸，防 `new Canvas(W,H)` OOM
            }
            doc.Width = nw;
            doc.Height = nh;
        }
        if (args.Count >= 3)
            doc.Background = ColorUtil.Parse(args[2].Value, doc.Background);
    }

    /// <summary>解析 icon 平台名 → 应用图标模板（画布尺寸 + 背景形状 + 居中字形）。</summary>
    static void ParseIcon(DrawDocument doc, IReadOnlyList<DrawToken> args)
    {
        if (args.Count < 1) { doc.Error = "参数错误: icon 需平台名（mac/ios/android/windows）"; return; }
        string platform = args[0].Value.ToLowerInvariant();
        int size; double radius; string shape; uint defaultColor; string defaultGlyph;
        switch (platform)
        {
            case "mac": size = 1024; radius = 229; shape = "round"; defaultColor = 0xFF5AC8FA; defaultGlyph = "M"; break;
            case "ios": size = 1024; radius = 0; shape = "rect"; defaultColor = 0xFF1C1C1E; defaultGlyph = "i"; break;
            case "android": size = 512; radius = size / 2.0; shape = "circle"; defaultColor = 0xFF34C759; defaultGlyph = "A"; break;
            case "windows": size = 256; radius = 48; shape = "round"; defaultColor = 0xFF0078D4; defaultGlyph = "W"; break;
            default: doc.Error = $"未知图标平台: {args[0].Value}（可选 mac/ios/android/windows）"; return;
        }
        uint color = args.Count >= 2 ? ColorUtil.Parse(args[1].Value, defaultColor) : defaultColor;
        string glyph = args.Count >= 3 ? args[2].Value : defaultGlyph;

        doc.Width = size; doc.Height = size;

        var bg = new DrawFigure { Fill = color };
        if (shape == "circle")
        {
            bg.Kind = "circle";
            bg.Args.Add(size / 2.0); bg.Args.Add(size / 2.0); bg.Args.Add(size / 2.0);
        }
        else if (shape == "round")
        {
            bg.Kind = "roundrect";
            bg.Args.Add(0); bg.Args.Add(0); bg.Args.Add(size); bg.Args.Add(size); bg.Args.Add(radius);
        }
        else
        {
            bg.Kind = "rect";
            bg.Args.Add(0); bg.Args.Add(0); bg.Args.Add(size); bg.Args.Add(size);
        }
        doc.Figures.Add(bg);

        var tx = new DrawFigure
        {
            Kind = "text",
            Fill = 0xFFFFFFFF,
            Text = glyph,
            Anchor = "middle",
            FontSize = size * 0.5,
        };
        tx.Args.Add(size / 2.0);                // x 居中
        tx.Args.Add(size / 2.0 + size * 0.18);  // y 视觉居中（字体基线补偿）
        doc.Figures.Add(tx);
    }

    /// <summary>渲染为 SVG 文本（完整文档）。</summary>
    public static string ToSvg(DrawDocument doc)
    {
        var sb = new StringBuilder();
        sb.Append("<svg xmlns=\"http://www.w3.org/2000/svg\" width=\"").Append(doc.Width)
          .Append("\" height=\"").Append(doc.Height)
          .Append("\" viewBox=\"0 0 ").Append(doc.Width).Append(' ').Append(doc.Height).Append("\">\n");
        sb.Append("  <rect width=\"100%\" height=\"100%\" fill=\"").Append(ColorUtil.ToHex(doc.Background)).Append("\"/>\n");

        if (doc.Gradients.Count > 0)
        {
            sb.Append("  <defs>\n");
            foreach (var g in doc.Gradients)
                EmitGradient(sb, g);
            sb.Append("  </defs>\n");
        }

        // ── 文字渐变的 **userSpaceOnUse** 定义（必须发在这里，与图元同一遍）──
        // 形状那份 `objectBoundingBox` 的 def 文字用不了（见 `DrawFigure.TextGradientId` 的注释），
        // 所以这里给**每个**带渐变的 text 图元再发一份绝对坐标的。
        // ⚠ id **必须避开用户自己用过的渐变名**：DSL 里 `gradient tg0 linear …` 是合法的，
        //    撞上就是一个 SVG 里出现两个同名 id，`url(#tg0)` 指哪个由渲染器说了算 ——
        //    这类"平时没事、别人起个名就坏"的隐患不值得留（`ClipId` 那边是老代码，另说）。
        var usedSvgIds = new HashSet<string>(doc.Gradients.Select(g => g.Id), StringComparer.Ordinal);
        int textGradN = 0;
        foreach (var f in doc.Figures)
        {
            if (f.Kind != "text" || f.Gradient == null) continue;
            string id;
            do { id = "tg" + textGradN++; } while (!usedSvgIds.Add(id));
            f.TextGradientId = id;
            EmitTextGradient(sb, f, f.Gradient);
        }

        int clipN = 0;
        // 蒙版开的那层 `<g>` 是**跨图元**的，得有人关：
        //   · 蒙版是**替换**语义（不是 push/pop）⇒ 新的一条先把上一条的关掉，
        //     ⚠ 不关的话 `mask_clear` 之后的内容仍然被上一个蒙版箍着，画面全错；
        //   · 最后一条留到文档末尾统一关（`clippop` 是自己发 `</g>` 的，与这里无关）。
        // ⚠ 已知边界：`clip` 与 `mask` **交叉**使用（裁剪的生命周期跨越一次蒙版替换）时，
        //   这里的"先关一层"会关错那一层。VML 那条路不会混用（`ui_gfx` 的蒙版不带变换、
        //   裁剪与蒙版各管各的段落），手写 DSL 才会碰上 —— 真碰上请拆成两段。
        bool maskOpen = false;
        foreach (var f in doc.Figures)
        {
            var cmd = DrawCommandRegistry.Get(f.Kind);
            if (cmd == null) continue;
            // image 图元需要裁剪（圆角/源图子矩形）时分配文档内唯一 clipPath id
            if (f.Kind == "image" && (f.CornerRadius > 0 || (f.SrcW > 0 && f.SrcH > 0)))
                f.ClipId = "imgClip" + (clipN++);
            if (f.Kind == "mask" && maskOpen) { sb.Append("  </g>\n"); maskOpen = false; }
            // ⚠ 蒙版图元**不套**外层 `<g transform>`：它的变换要落进 `<defs>` 里的形状上
            //   （见 `SvgMaskEmitter.EmitShapes`），而 `<g mask>` 本身必须留在顶层 ——
            //   否则"跨图元的层"会被包进只在一条图元上生效的 transform 里，结构就错了。
            if (f.Transform.IsIdentity || f.Kind == "mask")
            {
                cmd.EmitSvg(sb, f);
            }
            else
            {
                sb.Append("  <g transform=\"").Append(f.Transform.ToString()).Append("\">\n");
                cmd.EmitSvg(sb, f);
                sb.Append("  </g>\n");
            }
            if (f.Kind == "mask") maskOpen = true;
        }
        if (maskOpen) sb.Append("  </g>\n");
        sb.Append("</svg>\n");
        return sb.ToString();
    }

    /// <summary>
    /// 发射**文字专用**的渐变定义：`gradientUnits="userSpaceOnUse"` + 绝对坐标。
    ///
    /// 归一化几何（0..1，相对形状盒那套）映射到该文字的 `TextBlockBox` 上。
    /// ⚠ 径向半径按 **SVG 规范对 `objectBoundingBox` 的定义**换算：
    /// 那里 `r` 是"归一化对角线"的比例（`sqrt(w²+h²)/sqrt(2)`），
    /// 不这么换的话非正方盒上的圆会比形状那份扁 —— 两条路就不一致了。
    /// ⚠ 这个 `<defs>` **不能**与形状那份合并：单位不同，同一个 id 只能有一种语义。
    /// </summary>
    static void EmitTextGradient(StringBuilder sb, DrawFigure f, Gradient g)
    {
        var box = DrawParse.TextBox(f);
        sb.Append(g.Radial ? "    <radialGradient id=\"" : "    <linearGradient id=\"")
          .Append(DrawParse.EscapeXml(f.TextGradientId ?? "")).Append('"')
          .Append(" gradientUnits=\"userSpaceOnUse\"");
        if (g.Radial)
        {
            double diag = Math.Sqrt(box.W * box.W + box.H * box.H) / Math.Sqrt(2);
            sb.Append(" cx=\"").Append(FmtNum(box.X + g.Cx * box.W))
              .Append("\" cy=\"").Append(FmtNum(box.Y + g.Cy * box.H))
              .Append("\" r=\"").Append(FmtNum(g.R * diag)).Append('"');
        }
        else
        {
            sb.Append(" x1=\"").Append(FmtNum(box.X + g.X1 * box.W))
              .Append("\" y1=\"").Append(FmtNum(box.Y + g.Y1 * box.H))
              .Append("\" x2=\"").Append(FmtNum(box.X + g.X2 * box.W))
              .Append("\" y2=\"").Append(FmtNum(box.Y + g.Y2 * box.H)).Append('"');
        }
        sb.Append(">\n")
          .Append("      <stop offset=\"0\" stop-color=\"").Append(ColorUtil.ToHex(g.ColorA)).Append("\"/>\n")
          .Append("      <stop offset=\"1\" stop-color=\"").Append(ColorUtil.ToHex(g.ColorB)).Append("\"/>\n")
          .Append(g.Radial ? "    </radialGradient>\n" : "    </linearGradient>\n");
    }

    /// <summary>发射渐变定义（linearGradient / radialGradient，objectBoundingBox 归一化坐标）。</summary>
    static void EmitGradient(StringBuilder sb, Gradient g)
    {
        sb.Append(g.Radial ? "    <radialGradient id=\"" : "    <linearGradient id=\"")
          .Append(DrawParse.EscapeXml(g.Id)).Append('"');
        if (g.Radial)
        {
            sb.Append(" cx=\"").Append(FmtNum(g.Cx)).Append("\" cy=\"").Append(FmtNum(g.Cy))
              .Append("\" r=\"").Append(FmtNum(g.R)).Append('"');
        }
        else
        {
            sb.Append(" x1=\"").Append(FmtNum(g.X1)).Append("\" y1=\"").Append(FmtNum(g.Y1))
              .Append("\" x2=\"").Append(FmtNum(g.X2)).Append("\" y2=\"").Append(FmtNum(g.Y2)).Append('"');
        }
        sb.Append(">\n")
          .Append("      <stop offset=\"0\" stop-color=\"").Append(ColorUtil.ToHex(g.ColorA)).Append("\"/>\n")
          .Append("      <stop offset=\"1\" stop-color=\"").Append(ColorUtil.ToHex(g.ColorB)).Append("\"/>\n");
        sb.Append(g.Radial ? "    </radialGradient>\n" : "    </linearGradient>\n");
    }

    static string FmtNum(double v) => !double.IsFinite(v) ? "0" : Math.Abs(v) < 1e-9 ? "0" : v.ToString("0.###", CultureInfo.InvariantCulture);

    /// <summary>渲染为 PNG 字节流（doc.Antialias 时走 3× 超采样降采样消除锯齿）。</summary>
    public static byte[] ToPng(DrawDocument doc)
    {
        if (doc.Width <= 0 || doc.Height <= 0 || (long)doc.Width * doc.Height > MaxCanvasPixels)
            throw new InvalidOperationException("画布尺寸非法或过大");
        if (doc.Antialias)
        {
            var s = ChooseSupersample(doc.Width, doc.Height);
            if (s <= 1) return RenderPng(doc, doc.Width, doc.Height);   // 画布太大：宁可不要抗锯齿也别卡住
            return ToPngAntialiased(doc, s);
        }
        return RenderPng(doc, doc.Width, doc.Height);
    }

    /// <summary>
    /// 超采样倍率（抗锯齿用）—— **按画布面积自适应**，不再固定 3×。
    ///
    /// ## 为什么必须自适应（v0.96.176 实测）
    ///
    /// 代价是 <c>O(W·H·s²)</c>，而画质收益是**固定的观感改善**、不随画布变大而变大。
    /// 固定 3× 的后果在手机尺寸上非常明显：`377×539` 的 VML 窗口 ×9 = 183 万像素，
    /// 实测抗锯齿**吃掉了光栅化那一段的 87~89%**（俄罗斯方块每帧 222ms 里 194ms 是它，
    /// 关掉抗锯齿只要 29ms）—— 也就是说每帧 4fps 基本都是花在"把画布放大 9 倍再缩回来"。
    ///
    /// 改成"按预算选倍率"之后：小画布（图标、缩略图）仍然拿满 3×，手机全屏这种大画布降到 2×
    /// （像素数 9×→4×，代价约减半，而 2× 超采样对线条/圆角的改善本来就接近饱和）。
    ///
    /// ⚠ 阈值与候选倍率都放在这一处：别在别处再写一个"3" —— 那是本仓反复踩的平行表。
    /// </summary>
    internal static int ChooseSupersample(int w, int h)
    {
        var area = (long)w * h;
        // 预算 120 万像素：2× 在手机全屏（377×539×4 ≈ 81 万）内，3× 只留给小画布。
        const long budget = 1_200_000;
        if (area * 9 <= budget) return 3;
        if (area * 4 <= budget) return 2;
        return 1;
    }

    /// <summary>
    /// **把文档光栅化到像素缓冲**（不编 PNG）—— 供**像素读回**用
    /// （`getpixel` / `floodfill` / `getimage` 那一族）。
    ///
    /// <para>
    /// 与 <see cref="ToPng"/> 走**同一条光栅路径**（同一个 <c>Canvas</c> + 同一批
    /// <c>Rasterize</c>），只差最后那一步编码。读回要的是像素，编成 PNG 再解开是纯浪费
    /// —— 本仓在绘图窗口那条链上刚因为同样的事吃过一次亏（每帧编 333KB PNG 又立刻解开）。
    /// </para>
    ///
    /// <para>
    /// ⚠ **刻意不开抗锯齿**：超采样会把画布放大 s² 倍再缩回来，读回的像素经过平均后
    /// **不再是程序画的那个颜色**（1px 的线会被摊成一片灰）。而 `floodfill` 判的正是
    /// "这个像素是不是边界色" —— 颜色被平均过就判错了。所以读回一律**原尺寸**。
    /// </para>
    /// </summary>
    public static Canvas Rasterize(DrawDocument doc)
    {
        if (doc.Width <= 0 || doc.Height <= 0 || (long)doc.Width * doc.Height > MaxCanvasPixels)
            throw new InvalidOperationException("画布尺寸非法或过大");
        var canvas = new Canvas(doc.Width, doc.Height, doc.Background);
        foreach (var f in doc.Figures)
            DrawCommandRegistry.Get(f.Kind)?.Rasterize(canvas, f);
        return canvas;
    }

    static byte[] RenderPng(DrawDocument doc, int w, int h)
    {
        var canvas = new Canvas(w, h, doc.Background);
        foreach (var f in doc.Figures)
            DrawCommandRegistry.Get(f.Kind)?.Rasterize(canvas, f);
        return canvas.ToPng();
    }

    /// <summary>3×（可调）超采样：放大画布逐图元重绘，再 s×s 盒式降采样平均。</summary>
    static byte[] ToPngAntialiased(DrawDocument doc, int s)
    {
        int W = doc.Width * s, H = doc.Height * s;
        var big = new Canvas(W, H, doc.Background);
        var scale = Affine.Scale(s, s);
        foreach (var f in doc.Figures)
        {
            var saved = f.Transform;
            f.Transform = scale.Compose(saved);
            DrawCommandRegistry.Get(f.Kind)?.Rasterize(big, f);
            f.Transform = saved;
        }

        var small = new byte[doc.Width * doc.Height * 4];
        int n = s * s;
        for (int y = 0; y < doc.Height; y++)
            for (int x = 0; x < doc.Width; x++)
            {
                int r = 0, g = 0, b = 0, a = 0;
                for (int dy = 0; dy < s; dy++)
                    for (int dx = 0; dx < s; dx++)
                    {
                        int bi = ((y * s + dy) * W + (x * s + dx)) * 4;
                        r += big.Pixels[bi];
                        g += big.Pixels[bi + 1];
                        b += big.Pixels[bi + 2];
                        a += big.Pixels[bi + 3];
                    }
                int oi = (y * doc.Width + x) * 4;
                small[oi] = (byte)(r / n);
                small[oi + 1] = (byte)(g / n);
                small[oi + 2] = (byte)(b / n);
                small[oi + 3] = (byte)(a / n);
            }
        return PngEncoder.Encode(doc.Width, doc.Height, small);
    }

    /// <summary>便捷入口：一次调用产出 svg 字符串或 png 字节（按 format）。</summary>
    public static object Render(string dsl, string format)
    {
        var doc = Parse(dsl);
        return format.Equals("png", StringComparison.OrdinalIgnoreCase) ? ToPng(doc) : ToSvg(doc);
    }
}
