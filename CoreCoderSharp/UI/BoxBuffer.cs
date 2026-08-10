using CoreCoderSharp.Terminal;
using System.Text;

namespace CoreCoderSharp.UI;

/// <summary>
/// 边框样式
/// </summary>
public enum BorderStyle
{
    None,    // 无边框
    Single,  // ┌─┐ │ └─┘
    Double,  // ╔═╗ ║ ╚═╝
    Thick,   // ┏━┓ ┃ ┗━┛
    Solid,   // █ 实心块
    Star,    // ★ 星形角
    Circle,  // ● 圆形角
    Custom,  // 自定义字符
}

/// <summary>
/// 矩形缓冲区 —— 所有带边框的 UI 控件的基类。
/// 先绘制矩形区域（边框+背景），内容叠加绘制在上面。
/// </summary>
public class BoxBuffer
{
    // ---- 位置与大小 ----
    public int X { get; set; }
    public int Y { get; set; }
    public int Width { get; set; }
    public int Height { get; set; }

    // ---- 颜色 (ANSI 颜色码: "31"红 "32"绿 "33"黄 "34"蓝 "35"紫 "36"青 "37"白) ----
    public string FgColor { get; set; } = "37";
    public string BgColor { get; set; } = "";       // 如 "44" 蓝底

    // ---- 边框 ----
    public BorderStyle Border { get; set; } = BorderStyle.Single;

    // ---- 自定义边框字符 (仅 BorderStyle.Custom 时生效) ----
    public string CustomTL { get; set; } = "┌";  // Top-Left
    public string CustomTR { get; set; } = "┐";  // Top-Right
    public string CustomBL { get; set; } = "└";  // Bottom-Left
    public string CustomBR { get; set; } = "┘";  // Bottom-Right
    public string CustomH  { get; set; } = "─";  // Horizontal
    public string CustomV  { get; set; } = "│";  // Vertical

    // ---- 内部区域 (内容可绘制区域，不含边框) ----
    public int ContentLeft => Border == BorderStyle.None ? X : X + 1;
    public int ContentTop  => Border == BorderStyle.None ? Y : Y + 1;
    public int ContentWidth  => Border == BorderStyle.None ? Width : Width - 2;
    public int ContentHeight => Border == BorderStyle.None ? Height : Height - 2;

    // ================================================================
    // 边框字符获取
    // ================================================================

    private (string tl, string tr, string bl, string br, string h, string v) BorderChars()
    {
        return Border switch
        {
            BorderStyle.None   => (" ", " ", " ", " ", " ", " "),
            BorderStyle.Single => ("┌", "┐", "└", "┘", "─", "│"),
            BorderStyle.Double => ("╔", "╗", "╚", "╝", "═", "║"),
            BorderStyle.Thick  => ("┏", "┓", "┗", "┛", "━", "┃"),
            BorderStyle.Solid  => ("█", "█", "█", "█", "█", "█"),
            BorderStyle.Star   => ("★", "★", "★", "★", "─", "│"),
            BorderStyle.Circle => ("●", "●", "●", "●", "─", "│"),
            BorderStyle.Custom => (CustomTL, CustomTR, CustomBL, CustomBR, CustomH, CustomV),
            _ => ("┌", "┐", "└", "┘", "─", "│"),
        };
    }

    // ================================================================
    // 渲染
    // ================================================================

    /// <summary>将整个矩形（边框+背景填充）写入 StringBuilder</summary>
    public void Render(StringBuilder sb)
    {
        var (tl, tr, bl, br, h, v) = BorderChars();
        int fg = int.TryParse(FgColor, out var f) ? f : 0;
        int bg = int.TryParse(BgColor, out var b) ? b : 0;
        var rb = new Terminal.RenderBuffer();

        // 上边框
        rb.Write(Y, X, tl + new string(h[0], Width - 2) + tr, fg: fg, bg: bg);
        // 中间行
        var fill = new string(' ', Width - 2);
        for (int i = 1; i < Height - 1; i++)
            rb.Write(Y + i, X, v + fill + v, fg: fg, bg: bg);
        // 下边框
        if (Height > 1)
            rb.Write(Y + Height - 1, X, bl + new string(h[0], Width - 2) + br, fg: fg, bg: bg);

        sb.Append(rb.ToString());
    }

    /// <summary>在内部相对坐标写入文本（自动裁剪到内容区域）</summary>
    public void WriteAt(StringBuilder sb, int relRow, int relCol, string text)
    {
        if (relRow < 0 || relRow >= ContentHeight) return;
        var absRow = ContentTop + relRow;
        var absCol = ContentLeft + relCol;

        // 截断到内容宽度
        var maxLen = ContentWidth - relCol;
        if (maxLen <= 0) return;

        string display;
        if (VW(text) > maxLen)
        {
            // 按视觉宽度截断
            display = TruncateByVW(text, maxLen - 1) + "…";
        }
        else
        {
            display = text;
        }

        var bgOn = string.IsNullOrEmpty(BgColor) ? "" : AnsiTty.Bg(int.Parse(BgColor));
        var bgOff = string.IsNullOrEmpty(BgColor) ? "" : AnsiTty.SgrReset;
        sb.Append(AnsiTty.CursorPos(absRow, absCol)).Append(bgOn).Append(display).Append(bgOff);
    }

    /// <summary>在内部相对坐标写入，右侧填充空格到内容区右边界</summary>
    public void WriteLine(StringBuilder sb, int relRow, int relCol, string text)
    {
        if (relRow < 0 || relRow >= ContentHeight) return;
        var absRow = ContentTop + relRow;
        var absCol = ContentLeft + relCol;

        var maxLen = ContentWidth - relCol;
        if (maxLen <= 0) return;

        var textVW = VwPlainText(text);
        string display;
        int padLen;

        if (textVW > maxLen)
        {
            display = TruncateByVW(text, maxLen - 1) + "…";
            padLen = 0;
        }
        else
        {
            display = text;
            padLen = maxLen - textVW;
        }

        var bgOn = string.IsNullOrEmpty(BgColor) ? "" : AnsiTty.Bg(int.Parse(BgColor));
        var bgOff = string.IsNullOrEmpty(BgColor) ? "" : AnsiTty.SgrReset;
        sb.Append(AnsiTty.CursorPos(absRow, absCol)).Append(bgOn).Append(display);
        if (padLen > 0) sb.Append(new string(' ', padLen));
        sb.Append(bgOff);
    }

    /// <summary>填充整个内部区域为指定字符（默认空格=清空）</summary>
    public void Fill(StringBuilder sb, char ch = ' ')
    {
        var fill = new string(ch, ContentWidth);
        var bgOn = string.IsNullOrEmpty(BgColor) ? "" : AnsiTty.Bg(int.Parse(BgColor));
        var bgOff = string.IsNullOrEmpty(BgColor) ? "" : AnsiTty.SgrReset;
        for (int i = 0; i < ContentHeight; i++)
        {
            sb.Append(AnsiTty.CursorPos(ContentTop + i, ContentLeft)).Append(bgOn).Append(fill).Append(bgOff);
        }
    }

    // ================================================================
    // 视觉宽度工具 (静态，供子类使用)
    // ================================================================

    /// <summary>CJK 字符 = 2 列，ASCII = 1 列</summary>
    public static int VW(string s)
    {
        int w = 0;
        foreach (var r in s.EnumerateRunes())
            w += r.Value > 127 ? 2 : 1;
        return w;
    }

    /// <summary>去除 ANSI 转义序列后的视觉宽度</summary>
    public static int VwPlainText(string text)
    {
        int w = 0;
        var span = text.AsSpan();
        for (int i = 0; i < span.Length; i++)
        {
            if (span[i] == AnsiTty.AnsiCharPrefix && i + 1 < span.Length && span[i + 1] == AnsiTty.AnsiCharEscape)
            {
                i += 2;
                while (i < span.Length && span[i] != 'm') i++;
                continue;
            }
            w += span[i] > 127 ? 2 : 1;
        }
        return w;
    }

    /// <summary>按视觉宽度截断文本</summary>
    public static string TruncateByVW(string text, int maxVW)
    {
        int w = 0;
        var runes = text.EnumerateRunes().ToArray();
        for (int i = 0; i < runes.Length; i++)
        {
            var cw = runes[i].Value > 127 ? 2 : 1;
            if (w + cw > maxVW)
                return string.Concat(runes.Take(i));
            w += cw;
        }
        return text;
    }

    /// <summary>在内部相对坐标写入整行，指定前景色和背景色（用于高亮行）</summary>
    public void WriteLineHighlight(StringBuilder sb, int relRow, string fgColor, string bgColor, string text)
    {
        if (relRow < 0 || relRow >= ContentHeight) return;
        var absRow = ContentTop + relRow;
        var absCol = ContentLeft;
        var textVW = VwPlainText(text);
        var maxLen = ContentWidth;
        string display; int padLen;
        if (textVW > maxLen) { display = TruncateByVW(text, maxLen - 1) + "…"; padLen = 0; }
        else { display = text; padLen = maxLen - textVW; }
        var rb = new Terminal.RenderBuffer();
        rb.Write(absRow, absCol, display + (padLen > 0 ? new string(' ', padLen) : ""),
            fg: int.TryParse(fgColor, out var _f) ? _f : 0,
            bg: int.TryParse(bgColor, out var _b) ? _b : 0);
        sb.Append(rb.ToString());
    }

    /// <summary>
    /// 生成迷你进度条字符串，例如 "████░░░░ 50%"。不依赖 Spectre。
    /// </summary>
    public static string MiniBar(double percent, int width = 8)
    {
        var clamped = Math.Clamp(percent, 0, 100);
        var filled = (int)(clamped / 100 * width);
        var empty = width - filled;
        var barColor = clamped switch { < 50 => TuiColors.Green, < 80 => TuiColors.Yellow, _ => TuiColors.Red };
        return $"{AnsiText.Fg($"{new string('█', filled)}{new string('░', empty)}", barColor)} {clamped:F0}%";
    }

    // ================================================================
    // 便捷静态方法 — 在 BoxBuffer 内部绘制文字
    // ================================================================

    /// <summary>
    /// 在 BoxBuffer 内部相对坐标 (x,y) 处绘制文字，指定前景色。
    /// 超出内容区域自动裁剪。
    /// </summary>
    public static void ShowText(BoxBuffer buf, int x, int y, int color, string text)
    {
        if (y < 0 || y >= buf.ContentHeight) return;
        var absRow = buf.ContentTop + y;
        var absCol = buf.ContentLeft + x;
        var maxW = buf.ContentWidth - x;
        if (maxW <= 0) return;

        var display = VW(text) > maxW ? TruncateByVW(text, maxW - 1) + "…" : text;
        var sb = new StringBuilder();
        sb.Append(AnsiTty.CursorPos(absRow, absCol));
        if (!string.IsNullOrEmpty(buf.BgColor)) sb.Append(AnsiTty.Bg(int.Parse(buf.BgColor)));
        sb.Append(AnsiTty.Fg(color)).Append(display).Append(AnsiTty.SgrReset);
        Console.Write(sb.ToString());
    }

    /// <summary>
    /// 在 BoxBuffer 内部相对坐标 (x,y) 处绘制文字，限制宽度，指定前后景颜色。
    /// 超出内容区域自动裁剪。
    /// </summary>
    public static void ShowTextLimit(BoxBuffer buf, int x, int y, int limitWidth,
        int foreColor, int backColor, string text)
    {
        if (y < 0 || y >= buf.ContentHeight) return;
        var absRow = buf.ContentTop + y;
        var absCol = buf.ContentLeft + x;
        var maxW = Math.Min(buf.ContentWidth - x, limitWidth);
        if (maxW <= 0) return;

        var display = VW(text) > maxW ? TruncateByVW(text, maxW - 1) + "…" : text;
        var remain = maxW - VW(display);
        var sb = new StringBuilder();
        sb.Append(AnsiTty.CursorPos(absRow, absCol));
        sb.Append(AnsiTty.Fg(foreColor));
        if (backColor > 0) sb.Append(AnsiTty.Bg(backColor));
        sb.Append(display);
        if (remain > 0) sb.Append(new string(' ', remain));
        sb.Append(AnsiTty.SgrReset);
        Console.Write(sb.ToString());
    }
}
