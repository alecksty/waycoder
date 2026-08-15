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
public readonly struct Affine
{
    public readonly double A, B, C, D, E, F;
    public Affine(double a, double b, double c, double d, double e, double f)
    { A = a; B = b; C = c; D = d; E = e; F = f; }

    public static readonly Affine Identity = new(1, 0, 0, 1, 0, 0);
    public bool IsIdentity => A == 1 && B == 0 && C == 0 && D == 1 && E == 0 && F == 0;

    /// <summary>均匀缩放因子（行列式平方根）。恒等/纯旋转为 1，纯缩放为缩放比。</summary>
    public double ScaleFactor => Math.Sqrt(Math.Abs(A * D - B * C));

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
    public uint Fill = 0xFF000000;
    public uint Stroke = 0;
    public double StrokeWidth = 1;
    public string LineCap = "butt"; // 线头形状：butt | round | square
    public double FontSize = 14;
    public string Anchor = "start";
    public string FontFamily = "sans-serif";
    public string FontWeight = "normal";
    public string FontStyle = "normal";
    /// <summary>解析时捕获的当前仿射变换（默认恒等）。</summary>
    public Affine Transform = Affine.Identity;
    /// <summary>渐变引用 id（fill 以 @ 开头时设置），否则 null 表示纯色填充。</summary>
    public string? GradientRef;
    /// <summary>解析完成后解析出的渐变定义（GradientRef 命中时）；未命中/未引用为 null。</summary>
    public Gradient? Gradient;
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

            var cmd = DrawCommandRegistry.Get(name);
            if (cmd == null) { doc.Error = $"未知指令: {name}"; continue; }
            var fig = cmd.Parse(args);
            if (fig == null) { doc.Error = $"参数错误: {line}"; continue; }
            fig.Transform = current;
            doc.Figures.Add(fig);
        }

        // 解析完成后，把 @id 引用解析为渐变定义；未命中则退化为纯色。
        if (doc.Gradients.Count > 0)
        {
            var map = new Dictionary<string, Gradient>(StringComparer.Ordinal);
            foreach (var g in doc.Gradients) map[g.Id] = g;
            foreach (var f in doc.Figures)
            {
                if (f.GradientRef == null) continue;
                if (map.TryGetValue(f.GradientRef, out var g)) f.Gradient = g;
                else f.GradientRef = null;
            }
        }
        return doc;
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

        int clipN = 0;
        foreach (var f in doc.Figures)
        {
            var cmd = DrawCommandRegistry.Get(f.Kind);
            if (cmd == null) continue;
            // image 图元需要裁剪（圆角/源图子矩形）时分配文档内唯一 clipPath id
            if (f.Kind == "image" && (f.CornerRadius > 0 || (f.SrcW > 0 && f.SrcH > 0)))
                f.ClipId = "imgClip" + (clipN++);
            if (f.Transform.IsIdentity)
            {
                cmd.EmitSvg(sb, f);
            }
            else
            {
                sb.Append("  <g transform=\"").Append(f.Transform.ToString()).Append("\">\n");
                cmd.EmitSvg(sb, f);
                sb.Append("  </g>\n");
            }
        }
        sb.Append("</svg>\n");
        return sb.ToString();
    }

    /// <summary>发射渐变定义（linearGradient / radialGradient，objectBoundingBox 归一化坐标）。</summary>
    static void EmitGradient(StringBuilder sb, Gradient g)
    {
        sb.Append(g.Radial ? "    <radialGradient id=\"" : "    <linearGradient id=\"")
          .Append(g.Id).Append('"');
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

    static string FmtNum(double v) => Math.Abs(v) < 1e-9 ? "0" : v.ToString("0.###", CultureInfo.InvariantCulture);

    /// <summary>渲染为 PNG 字节流（doc.Antialias 时走 3× 超采样降采样消除锯齿）。</summary>
    public static byte[] ToPng(DrawDocument doc)
    {
        if (doc.Width <= 0 || doc.Height <= 0 || (long)doc.Width * doc.Height > MaxCanvasPixels)
            throw new InvalidOperationException("画布尺寸非法或过大");
        if (doc.Antialias)
        {
            int W = doc.Width * 3, H = doc.Height * 3;
            if ((long)W * H > MaxCanvasPixels) return RenderPng(doc, doc.Width, doc.Height); // 超大画布跳过超采样
            return ToPngAntialiased(doc, 3);
        }
        return RenderPng(doc, doc.Width, doc.Height);
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
