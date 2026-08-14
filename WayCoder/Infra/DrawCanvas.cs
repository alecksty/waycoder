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
    public void SetPixel(int x, int y, uint c)
    {
        if (x < 0 || y < 0 || x >= Width || y >= Height) return;
        var i = (y * Width + x) * 4;
        Pixels[i] = ColorUtil.R(c);
        Pixels[i + 1] = ColorUtil.G(c);
        Pixels[i + 2] = ColorUtil.B(c);
        Pixels[i + 3] = ColorUtil.A(c);
    }

    // ── 形状 ──
    public void FillRect(int x, int y, int w, int h, uint c)
    {
        for (int py = y; py < y + h; py++)
            for (int px = x; px < x + w; px++)
                SetPixel(px, py, c);
    }

    public void FillRoundRect(double x, double y, double w, double h, double r, uint c)
    {
        FillRect((int)Math.Ceiling(x + r), (int)Math.Round(y), (int)(w - 2 * r), (int)Math.Round(h), c);
        FillRect((int)Math.Round(x), (int)Math.Ceiling(y + r), (int)Math.Round(w), (int)(h - 2 * r), c);
        FillCircle(x + r, y + r, r, c);
        FillCircle(x + w - r, y + r, r, c);
        FillCircle(x + r, y + h - r, r, c);
        FillCircle(x + w - r, y + h - r, r, c);
    }

    public void FillCircle(double cx, double cy, double r, uint c)
    {
        int r0 = Math.Max(0, (int)Math.Ceiling(r));
        for (int dy = -r0; dy <= r0; dy++)
        {
            double dx = Math.Sqrt(r * r - dy * dy);
            int x0 = (int)Math.Ceiling(cx - dx);
            int x1 = (int)Math.Floor(cx + dx);
            for (int x = x0; x <= x1; x++)
                SetPixel(x, (int)Math.Round(cy + dy), c);
        }
    }

    public void FillEllipse(double cx, double cy, double rx, double ry, uint c)
    {
        int r0 = Math.Max(0, (int)Math.Ceiling(ry));
        for (int dy = -r0; dy <= r0; dy++)
        {
            double dx = rx * Math.Sqrt(Math.Max(0, 1 - (dy * dy) / (ry * ry)));
            int x0 = (int)Math.Ceiling(cx - dx);
            int x1 = (int)Math.Floor(cx + dx);
            for (int x = x0; x <= x1; x++)
                SetPixel(x, (int)Math.Round(cy + dy), c);
        }
    }

    public void DrawLine(double x1, double y1, double x2, double y2, uint c, double width)
    {
        if (width <= 1)
        {
            Bresenham((int)Math.Round(x1), (int)Math.Round(y1), (int)Math.Round(x2), (int)Math.Round(y2), c);
            return;
        }
        // 粗线：以线段为中轴、width 为宽的填充四边形
        double dx = x2 - x1, dy = y2 - y1;
        double len = Math.Sqrt(dx * dx + dy * dy);
        if (len < 1e-6) { FillCircle(x1, y1, width / 2, c); return; }
        double nx = -dy / len * width / 2;
        double ny = dx / len * width / 2;
        FillPolygon(new[] { x1 + nx, y1 + ny, x1 - nx, y1 - ny, x2 - nx, y2 - ny, x2 + nx, y2 + ny }, c);
    }

    void Bresenham(int x0, int y0, int x1, int y1, uint c)
    {
        int dx = Math.Abs(x1 - x0), sx = x0 < x1 ? 1 : -1;
        int dy = -Math.Abs(y1 - y0), sy = y0 < y1 ? 1 : -1;
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

    // ── 文字 ──
    public void DrawText(double x, double y, string text, double size, uint c, string anchor)
    {
        if (string.IsNullOrEmpty(text)) return;
        double scale = Math.Max(1, size / 7.0);
        double total = TextWidth(text, scale);
        double ox = anchor switch
        {
            "middle" => -total / 2,
            "end" => -total,
            _ => 0,
        };
        double cx = x + ox;
        foreach (var ch in text)
        {
            var glyph = Glyph(ch);
            for (int r = 0; r < 7; r++)
            {
                var row = r < glyph.Length ? glyph[r] : ".....";
                for (int col = 0; col < 5; col++)
                {
                    bool on = col < row.Length && row[col] == '#';
                    if (!on) continue;
                    int px = (int)Math.Round(cx + col * scale);
                    int py = (int)Math.Round(y + r * scale);
                    int s = Math.Max(1, (int)Math.Round(scale));
                    FillRect(px, py, s, s, c);
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

    /// <summary>数值解析工具（供指令 Parse 使用，InvariantCulture）。</summary>
    public static bool TryNum(string s, out double v)
        => double.TryParse(s, NumberStyles.Float, CultureInfo.InvariantCulture, out v);
}
