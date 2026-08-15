// =============================================================
// Graphics.cs —— 图形核心：离屏像素缓冲 + 文本层 + EGA 调色板
//
// 纯 C# 零依赖实现 QBasic 图形子系统：
//   - PixelBuffer：按坐标读写像素（SCREEN/LINE/CIRCLE/PSET/PAINT/POINT/GET/PUT 的基础）
//   - Sprite：GET/PUT 离屏矩形块的读取与写入（含 PSET / XOR 混合）
//   - TextLayer：LOCATE + PRINT 文本叠加（图形模式下的文字提示、名字、分数）
//   - EgaPalette：16 色索引 → 24 位 RGB，PALETTE 语句可改
//   - GfxDevice：把以上组装成 VM 可访问的图形设备（同时充当 IOutputSink）
// 全部 AOT 安全（无反射）。
// =============================================================
using System.Globalization;

namespace QBasic.Compiler;

/// <summary>24 位真彩色。</summary>
public readonly struct RgbColor
{
    public readonly byte R, G, B;
    public RgbColor(byte r, byte g, byte b) { R = r; G = g; B = b; }
    /// <summary>终端 256 色近似：映射到标准 16 色最接近者（简单方案）。</summary>
    public string Ansi24 => $"\u001b[38;2;{R};{G};{B}m";
}

/// <summary>
/// 离屏像素缓冲。每个单元存一个颜色索引（0-15），真实颜色由调色板决定。
/// 支持：读写像素、直线（Bresenham）、填充矩形、圆/圆弧（含 aspect 与起止角）、
/// 洪泛填充（PAINT）、sprite 块读取/写入（GET/PUT，PSET/XOR）。
/// </summary>
public sealed class PixelBuffer
{
    public int Width { get; }
    public int Height { get; }
    private readonly int[] _px;

    public PixelBuffer(int w, int h)
    {
        Width = w; Height = h;
        _px = new int[w * h];
    }

    /// <summary>读取像素，越界返回 0。</summary>
    public int Get(int x, int y)
    {
        if (x < 0 || y < 0 || x >= Width || y >= Height) return 0;
        return _px[y * Width + x];
    }

    /// <summary>写入像素，越界忽略。</summary>
    public void Set(int x, int y, int color)
    {
        if (x < 0 || y < 0 || x >= Width || y >= Height) return;
        _px[y * Width + x] = color;
    }

    /// <summary>整屏填充。</summary>
    public void Clear(int color = 0)
    {
        Array.Fill(_px, color);
    }

    /// <summary>Bresenham 直线。</summary>
    public void Line(int x1, int y1, int x2, int y2, int color)
    {
        int dx = Math.Abs(x2 - x1), sx = x1 < x2 ? 1 : -1;
        int dy = -Math.Abs(y2 - y1), sy = y1 < y2 ? 1 : -1;
        int err = dx + dy;
        while (true)
        {
            Set(x1, y1, color);
            if (x1 == x2 && y1 == y2) break;
            int e2 = 2 * err;
            if (e2 >= dy) { err += dy; x1 += sx; }
            if (e2 <= dx) { err += dx; y1 += sy; }
        }
    }

    /// <summary>填充矩形（LINE ..., BF）。自动归一化坐标。</summary>
    public void FillRect(int x1, int y1, int x2, int y2, int color)
    {
        int minx = Math.Min(x1, x2), maxx = Math.Max(x1, x2);
        int miny = Math.Min(y1, y2), maxy = Math.Max(y1, y2);
        for (int y = miny; y <= maxy; y++)
            for (int x = minx; x <= maxx; x++)
                Set(x, y, color);
    }

    /// <summary>
    /// 圆 / 椭圆 / 圆弧。r 为半径，aspect 为 y/x 比值；start/end 为弧度（可为负，
    /// 负的 start 表示从圆心向该点画半径线，QBasic 语义）。未指定角度则整圆。
    /// </summary>
    public void Circle(int cx, int cy, double r, int color,
        double start, double end, bool hasAngles, double aspect)
    {
        if (r < 0) r = 0;
        double a0 = hasAngles ? start : 0.0;
        double a1 = hasAngles ? end : 2.0 * Math.PI;
        // 负 start：从圆心到起点画半径
        bool radiusLine = hasAngles && start < 0;
        a0 = Math.Abs(a0);
        if (a1 < a0) { (a1, a0) = (a0, a1); }
        if (Math.Abs(a1 - a0) < 1e-9) a1 = a0 + 2.0 * Math.PI;

        double prevX = cx + r * Math.Cos(a0);
        double prevY = cy + r * Math.Sin(a0) * aspect;
        if (radiusLine) Line(cx, cy, (int)Math.Round(prevX), (int)Math.Round(prevY), color);
        Set((int)Math.Round(prevX), (int)Math.Round(prevY), color);
        double step = 1.0 / Math.Max(1.0, r * 4);
        for (double a = a0 + step; a <= a1 + step; a += step)
        {
            double x = cx + r * Math.Cos(a);
            double y = cy + r * Math.Sin(a) * aspect;
            Line((int)Math.Round(prevX), (int)Math.Round(prevY), (int)Math.Round(x), (int)Math.Round(y), color);
            prevX = x; prevY = y;
        }
    }

    /// <summary>
    /// 洪泛填充（PAINT）。从 (x,y) 出发，把等于起始色的像素替换为 fill 色，
    /// 遇到 boundary 色则停止；boundary 缺省等于 fill 色。
    /// </summary>
    public void Flood(int x, int y, int fill, int boundary)
    {
        if (x < 0 || y < 0 || x >= Width || y >= Height) return;
        int startColor = Get(x, y);
        if (startColor == fill && boundary == fill) return;
        var stack = new Stack<(int, int)>();
        stack.Push((x, y));
        var seen = new HashSet<int>();
        while (stack.Count > 0)
        {
            var (cx, cy) = stack.Pop();
            if (cx < 0 || cy < 0 || cx >= Width || cy >= Height) continue;
            int key = cy * Width + cx;
            if (seen.Contains(key)) continue;
            int c = Get(cx, cy);
            if (c != startColor || c == boundary) continue;
            seen.Add(key);
            Set(cx, cy, fill);
            stack.Push((cx + 1, cy));
            stack.Push((cx - 1, cy));
            stack.Push((cx, cy + 1));
            stack.Push((cx, cy - 1));
        }
    }

    /// <summary>
    /// GET：把矩形区域读到 sprite 数组 [w, h, 像素...]。坐标越界部分填 0。
    /// </summary>
    public int[] GetSprite(int x1, int y1, int x2, int y2)
    {
        int minx = Math.Min(x1, x2), maxx = Math.Max(x1, x2);
        int miny = Math.Min(y1, y2), maxy = Math.Max(y1, y2);
        int w = maxx - minx + 1, h = maxy - miny + 1;
        var data = new int[2 + w * h];
        data[0] = w; data[1] = h;
        int k = 2;
        for (int yy = 0; yy < h; yy++)
            for (int xx = 0; xx < w; xx++)
                data[k++] = Get(minx + xx, miny + yy);
        return data;
    }

    /// <summary>
    /// PUT：把 sprite 数组 [w,h,像素...] 写回 (x,y)。xor 为 true 时做异或混合（擦除）。
    /// </summary>
    public void PutSprite(int x, int y, int[] data, bool xor)
    {
        if (data.Length < 2) return;
        int w = data[0], h = data[1];
        int k = 2;
        for (int yy = 0; yy < h; yy++)
            for (int xx = 0; xx < w; xx++)
            {
                int c = k < data.Length ? data[k] : 0; k++;
                int px = x + xx, py = y + yy;
                if (px < 0 || py < 0 || px >= Width || py >= Height) continue;
                if (xor) Set(px, py, Get(px, py) ^ c);
                else Set(px, py, c);
            }
    }
}

/// <summary>
/// 文本层：LOCATE + PRINT 叠加。行/列为 1-based，字符带前景/背景色。
/// 供图形模式下的文字提示、玩家名、分数使用。
/// </summary>
public sealed class TextLayer
{
    public int Cols { get; private set; }
    public int Rows { get; private set; }
    private char[] _cells;
    private int[] _fg, _bg;
    public int CurRow = 1, CurCol = 1;
    public int Fg = 7, Bg = 0;

    public TextLayer(int cols = 80, int rows = 25)
    {
        Cols = cols; Rows = rows;
        _cells = new char[cols * rows];
        _fg = new int[cols * rows];
        _bg = new int[cols * rows];
        for (int i = 0; i < _cells.Length; i++) _cells[i] = ' ';
    }

    public void Resize(int cols, int rows)
    {
        var nc = new char[cols * rows];
        for (int i = 0; i < nc.Length; i++) nc[i] = ' ';
        var nf = new int[cols * rows];
        var nb = new int[cols * rows];
        int copyCols = Math.Min(Cols, cols), copyRows = Math.Min(Rows, rows);
        for (int r = 0; r < copyRows; r++)
            for (int c = 0; c < copyCols; c++)
            {
                nc[r * cols + c] = _cells[r * Cols + c];
                nf[r * cols + c] = _fg[r * Cols + c];
                nb[r * cols + c] = _bg[r * Cols + c];
            }
        Cols = cols; Rows = rows; _cells = nc; _fg = nf; _bg = nb;
    }

    public char GetChar(int r, int c) => _cells[r * Cols + c];
    public int GetFg(int r, int c) => _fg[r * Cols + c];
    public int GetBg(int r, int c) => _bg[r * Cols + c];

    public void SetCursor(int row, int col)
    {
        if (row >= 1 && row <= Rows) CurRow = row;
        if (col >= 1 && col <= Cols) CurCol = col;
    }

    public void Write(char c)
    {
        if (c == '\n') { CurRow++; CurCol = 1; if (CurRow > Rows) CurRow = 1; return; }
        if (CurCol > Cols) { CurRow++; CurCol = 1; if (CurRow > Rows) CurRow = 1; }
        int idx = (CurRow - 1) * Cols + (CurCol - 1);
        _cells[idx] = c; _fg[idx] = Fg; _bg[idx] = Bg;
        CurCol++;
    }

    public void Clear()
    {
        for (int i = 0; i < _cells.Length; i++) _cells[i] = ' ';
    }
}

/// <summary>
/// EGA 16 色调色板：索引 → 24 位 RGB。PALETTE 语句按 0-63 EGA 值改写。
/// </summary>
public sealed class EgaPalette
{
    private readonly byte[] _r = new byte[16], _g = new byte[16], _b = new byte[16];
    private static readonly byte[] StdR = { 0, 0, 0, 0, 170, 170, 170, 170, 85, 85, 85, 85, 255, 255, 255, 255 };
    private static readonly byte[] StdG = { 0, 0, 170, 170, 0, 0, 85, 170, 85, 85, 255, 255, 85, 85, 255, 255 };
    private static readonly byte[] StdB = { 0, 170, 0, 170, 0, 170, 0, 170, 85, 255, 85, 255, 85, 255, 85, 255 };

    public EgaPalette()
    {
        for (int i = 0; i < 16; i++) { _r[i] = StdR[i]; _g[i] = StdG[i]; _b[i] = StdB[i]; }
    }

    public RgbColor this[int idx] => new(_r[idx & 15], _g[idx & 15], _b[idx & 15]);

    /// <summary>按 QBasic EGA 0-63 值改写某颜色槽（低2位蓝、中2位绿、高2位红）。</summary>
    public void SetEga(int idx, int v)
    {
        int r = (v >> 4) & 3, g = (v >> 2) & 3, b = v & 3;
        static byte Scale(int x) => x switch { 0 => (byte)0, 1 => (byte)85, 2 => (byte)170, _ => (byte)255 };
        _r[idx & 15] = Scale(r); _g[idx & 15] = Scale(g); _b[idx & 15] = Scale(b);
    }
}

/// <summary>
/// 图形设备：组合像素缓冲 + 文本层 + 调色板，并充当 IOutputSink（PRINT 落到文本层）。
/// 供 VM 图形语句与终端渲染使用。
/// </summary>
public sealed class GfxDevice : IOutputSink
{
    public PixelBuffer Pixels = new(320, 200);
    public TextLayer Text = new(80, 25);
    public EgaPalette Palette = new();
    /// <summary>当前 SCREEN 模式：0=文本，1=CGA(320x200)，9=EGA(640x350)。</summary>
    public int Mode;

    public void SetMode(int mode)
    {
        Mode = mode;
        if (mode == 9) Pixels = new PixelBuffer(640, 350);
        else if (mode == 1) Pixels = new PixelBuffer(320, 200);
        // SCREEN 0 文本模式：缓冲可保留但不被渲染
    }

    public void Cls()
    {
        Pixels.Clear(0);
        Text.Clear();
    }

    // ---- IOutputSink：PRINT 落到文本层 ----
    public void Print(string s) { foreach (var c in s) Text.Write(c); }
    public void PrintLine(string s) { Print(s); Text.Write('\n'); }
    public void Newline() => Text.Write('\n');
}

/// <summary>
/// 终端输入服务：后台线程把按键读入队列，供 INKEY$（非阻塞单键）与
/// LINE INPUT / INPUT（读整行）使用。亦用于测试（可注入预置按键）。
/// </summary>
public interface IKeyProvider
{
    /// <summary>INKEY$：取一个按键；无键返回 ""。</summary>
    string ReadKey();
    /// <summary>读一整行（LINE INPUT / INPUT）。</summary>
    string ReadLine();
}

/// <summary>控制台原始键盘输入。</summary>
public sealed class ConsoleKeyProvider : IKeyProvider
{
    private readonly Queue<string> _keys = new();
    private readonly System.Threading.Thread? _thread;

    public ConsoleKeyProvider()
    {
        _thread = new System.Threading.Thread(ReadLoop) { IsBackground = true };
        _thread.Start();
    }

    private void ReadLoop()
    {
        try
        {
            while (true)
            {
                var k = Console.ReadKey(true);
                string s = k.Key switch
                {
                    ConsoleKey.Enter => "\r",
                    ConsoleKey.Backspace => "\b",
                    ConsoleKey.Escape => "\u001b",
                    _ => k.KeyChar.ToString(),
                };
                lock (_keys) _keys.Enqueue(s);
            }
        }
        catch
        {
            // 非交互环境（stdin 重定向）下 Console.ReadKey 抛异常，优雅退出
        }
    }

    public string ReadKey()
    {
        lock (_keys) return _keys.Count > 0 ? _keys.Dequeue() : "";
    }

    public string ReadLine()
    {
        var sb = new System.Text.StringBuilder();
        while (true)
        {
            string k = ReadKey();
            if (k == "\r" || k == "\n") return sb.ToString();
            if (k == "\b") { if (sb.Length > 0) sb.Length--; continue; }
            if (k.Length > 0) sb.Append(k);
            // 队列为空时轻微让步
            else System.Threading.Thread.Sleep(5);
        }
    }
}

/// <summary>预置按键队列（自测用）。</summary>
public sealed class QueueKeyProvider : IKeyProvider
{
    private readonly Queue<string> _q;
    public QueueKeyProvider(params string[] keys) { _q = new Queue<string>(keys); }
    public string ReadKey() => _q.Count > 0 ? _q.Dequeue() : "";
    public string ReadLine()
    {
        var sb = new System.Text.StringBuilder();
        while (true)
        {
            string k = ReadKey();
            if (k == "\r" || k == "\n" || k.Length == 0) return sb.ToString();
            if (k == "\b") { if (sb.Length > 0) sb.Length--; continue; }
            sb.Append(k);
        }
    }
}

/// <summary>
/// 按绝对时间释放按键（自测/冒烟用）。ReadKey 仅在其释放时刻过后才返回该键，之前返回 ""，
/// 从而让 GORILLA.BAS 的 `WHILE INKEY$ <> "": WEND` 清空循环在键释放前看到空队列、等待循环在键释放后看到按键。
/// </summary>
public sealed class TimedKeyProvider : IKeyProvider
{
    private readonly (long atMs, string key)[] _keys;
    private int _pos;
    private readonly System.Diagnostics.Stopwatch _sw = System.Diagnostics.Stopwatch.StartNew();
    public TimedKeyProvider(params (long atMs, string key)[] keys) { _keys = keys; }
    public string ReadKey()
    {
        if (_pos >= _keys.Length) return "";
        if (_sw.ElapsedMilliseconds < _keys[_pos].atMs) return "";
        return _keys[_pos++].key;
    }
    public string ReadLine()
    {
        var sb = new System.Text.StringBuilder();
        while (true)
        {
            string k = ReadKey();
            if (k == "\r" || k == "\n" || k.Length == 0) return sb.ToString();
            if (k == "\b") { if (sb.Length > 0) sb.Length--; continue; }
            sb.Append(k);
        }
    }
}

/// <summary>
/// 终端图形渲染器：把 GfxDevice 的像素缓冲（按调色板转真彩）以半块字符 ▀▄
/// 渲染到 ANSI 终端，并按需叠加文本层。支持盒式采样缩放以适配终端尺寸。
/// </summary>
public sealed class TerminalGfx
{
    private readonly GfxDevice _dev;
    // 上一帧单元格键（扁平）：文本=char|fg<<16|bg<<24；图形=top|bot<<8（文本叠加用高位标志）
    private int[] _prev = Array.Empty<int>();
    private int _prevMode = -1;

    public TerminalGfx(GfxDevice dev) { _dev = dev; }

    /// <summary>把整个画面渲染到终端。差分重绘：仅输出相对上一帧变化的单元格，避免全屏重写。</summary>
    public void Present()
    {
        var sw = new System.IO.StringWriter();
        var px = _dev.Pixels;
        int termCols = 0, termRows = 0;
        try { termCols = Console.WindowWidth; termRows = Console.WindowHeight; }
        catch { termCols = 80; termRows = 24; }
        if (termCols < 20) termCols = 20;
        if (termRows < 10) termRows = 10;

        if (_dev.Mode == 0)
            RenderTextOnly(sw, termCols, termRows);
        else
            RenderGraphics(sw, px, termCols, termRows);

        sw.Write("\u001b[H\u001b[0m");
        Console.Out.Write(sw.ToString());
        Console.Out.Flush();
    }

    private void RenderTextOnly(System.IO.StringWriter sw, int cols, int rows)
    {
        int r0 = Math.Min(rows, _dev.Text.Rows), c0 = Math.Min(cols, _dev.Text.Cols);
        if (_prev.Length != cols * rows || _prevMode != 0)
        {
            _prev = new int[cols * rows];
            Array.Fill(_prev, -1);
            sw.Write("\u001b[2J");
        }
        for (int r = 0; r < rows; r++)
        {
            for (int c = 0; c < cols; c++)
            {
                char ch = (r < r0 && c < c0) ? _dev.Text.GetChar(r, c) : ' ';
                int fg = (r < r0 && c < c0) ? _dev.Text.GetFg(r, c) : 7;
                int bg = (r < r0 && c < c0) ? _dev.Text.GetBg(r, c) : 0;
                int key = ch | (fg << 16) | (bg << 24);
                int i = r * cols + c;
                if (_prev[i] == key) continue;
                _prev[i] = key;
                var frgb = _dev.Palette[fg]; var brgb = _dev.Palette[bg];
                sw.Write($"\u001b[{r + 1};{c + 1}H\u001b[38;2;{frgb.R};{frgb.G};{frgb.B}m\u001b[48;2;{brgb.R};{brgb.G};{brgb.B}m{ch}");
            }
        }
        _prevMode = 0;
    }

    private void RenderGraphics(System.IO.StringWriter sw, PixelBuffer px, int termCols, int termRows)
    {
        // 半块：每单元纵向 2 像素；横向 1 像素。若过宽则盒式采样。
        int cellW = px.Width, cellH = px.Height / 2;
        int scaleW = Math.Max(1, (cellW + termCols - 1) / termCols);
        int scaleH = Math.Max(1, (cellH + termRows - 1) / termRows);
        int scale = Math.Max(scaleW, scaleH);
        int cols = cellW / scale, rows = Math.Min(termRows, cellH / scale);
        int total = cols * rows;
        if (_prev.Length != total || _prevMode != _dev.Mode)
        {
            _prev = new int[total];
            Array.Fill(_prev, -1);
            sw.Write("\u001b[2J");
        }

        for (int cy = 0; cy < rows; cy++)
        {
            for (int cx = 0; cx < cols; cx++)
            {
                int sx = cx * scale, sy = cy * scale * 2;
                int cTop = Sample(px, sx, sy, scale, 1);
                int cBot = Sample(px, sx, sy + scale, scale, 1);
                // 文本层叠加：非空格字符覆盖半块单元
                char ch = ' '; int fg = 7;
                if (cy < _dev.Text.Rows && cx < _dev.Text.Cols)
                {
                    ch = _dev.Text.GetChar(cy, cx);
                    if (ch != ' ') fg = _dev.Text.GetFg(cy, cx);
                }
                int i = cy * cols + cx;
                int key = ch != ' ' ? (1 << 30) | (fg << 16) | ch : (cTop | (cBot << 8));
                if (_prev[i] == key) continue;
                _prev[i] = key;
                if (ch != ' ')
                {
                    var frgb = _dev.Palette[fg];
                    sw.Write($"\u001b[{cy + 1};{cx + 1}H\u001b[38;2;{frgb.R};{frgb.G};{frgb.B}m{ch}");
                }
                else
                {
                    var trgb = _dev.Palette[cTop]; var brgb = _dev.Palette[cBot];
                    if (cTop == cBot)
                        sw.Write($"\u001b[{cy + 1};{cx + 1}H\u001b[38;2;{trgb.R};{trgb.G};{trgb.B}m\u001b[48;2;{trgb.R};{trgb.G};{trgb.B}m█");
                    else
                        sw.Write($"\u001b[{cy + 1};{cx + 1}H\u001b[38;2;{trgb.R};{trgb.G};{trgb.B}m\u001b[48;2;{brgb.R};{brgb.G};{brgb.B}m▀");
                }
            }
        }
        _prevMode = _dev.Mode;
    }

    private static int Sample(PixelBuffer px, int x, int y, int w, int h)
    {
        // 取采样块内最亮的像素（或首个非 0），提升小物体可见性
        long r = 0; int n = 0;
        for (int dy = 0; dy < h; dy++)
            for (int dx = 0; dx < w; dx++)
            {
                int c = px.Get(x + dx, y + dy);
                r += c; n++;
            }
        return n > 0 ? (int)(r / n) & 15 : 0;
    }
}
