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
                    if (line[i] == '\\' && i + 1 < n) i++; // 简单转义：\" 等
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
    public double FontSize = 14;
    public string Anchor = "start";
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
    public readonly List<DrawFigure> Figures = new();
    public string? Error;
}

/// <summary>绘图运行器：DSL 解析 + SVG/PNG 渲染编排。</summary>
public static class DrawRunner
{
    /// <summary>解析 DSL 文本为文档。canvas 设置画布，其余为图元；非法行记入 Error 并跳过。</summary>
    public static DrawDocument Parse(string dsl)
    {
        var doc = new DrawDocument();
        if (string.IsNullOrWhiteSpace(dsl)) { doc.Error = "空输入"; return doc; }
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
            var cmd = DrawCommandRegistry.Get(name);
            if (cmd == null) { doc.Error = $"未知指令: {name}"; continue; }
            var fig = cmd.Parse(args);
            if (fig == null) { doc.Error = $"参数错误: {line}"; continue; }
            doc.Figures.Add(fig);
        }
        return doc;
    }

    static void ParseCanvas(DrawDocument doc, IReadOnlyList<DrawToken> args)
    {
        if (args.Count >= 2)
        {
            if (double.TryParse(args[0].Value, NumberStyles.Float, CultureInfo.InvariantCulture, out var w) && w > 0)
                doc.Width = (int)Math.Round(w);
            if (double.TryParse(args[1].Value, NumberStyles.Float, CultureInfo.InvariantCulture, out var h) && h > 0)
                doc.Height = (int)Math.Round(h);
        }
        if (args.Count >= 3)
            doc.Background = ColorUtil.Parse(args[2].Value, doc.Background);
    }

    /// <summary>渲染为 SVG 文本（完整文档）。</summary>
    public static string ToSvg(DrawDocument doc)
    {
        var sb = new StringBuilder();
        sb.Append("<svg xmlns=\"http://www.w3.org/2000/svg\" width=\"").Append(doc.Width)
          .Append("\" height=\"").Append(doc.Height)
          .Append("\" viewBox=\"0 0 ").Append(doc.Width).Append(' ').Append(doc.Height).Append("\">\n");
        sb.Append("  <rect width=\"100%\" height=\"100%\" fill=\"").Append(ColorUtil.ToHex(doc.Background)).Append("\"/>\n");
        foreach (var f in doc.Figures)
            DrawCommandRegistry.Get(f.Kind)?.EmitSvg(sb, f);
        sb.Append("</svg>\n");
        return sb.ToString();
    }

    /// <summary>渲染为 PNG 字节流。</summary>
    public static byte[] ToPng(DrawDocument doc)
    {
        var canvas = new Canvas(doc.Width, doc.Height, doc.Background);
        foreach (var f in doc.Figures)
            DrawCommandRegistry.Get(f.Kind)?.Rasterize(canvas, f);
        return canvas.ToPng();
    }

    /// <summary>便捷入口：一次调用产出 svg 字符串或 png 字节（按 format）。</summary>
    public static object Render(string dsl, string format)
    {
        var doc = Parse(dsl);
        return format.Equals("png", StringComparison.OrdinalIgnoreCase) ? ToPng(doc) : ToSvg(doc);
    }
}
