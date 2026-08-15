// =============================================================
// Ansi.cs —— ANSI 转义序列生成器（零依赖，纯字符串拼接）
//
// 提供终端控制序列的统一生成接口：颜色、光标、清屏、备用屏。
// 全部返回字符串，由上层通过标准输出写出。
// =============================================================
using System.Text;

namespace QBasic.Tui;

/// <summary>ANSI 转义序列工具。</summary>
public static class Ansi
{
    public const string Esc = "\u001b";

    /// <summary>光标定位到 row（1-based）、col（1-based）。</summary>
    public static string CursorTo(int row, int col) => $"{Esc}[{row};{col}H";

    /// <summary>光标上移 n 行。</summary>
    public static string CursorUp(int n) => $"{Esc}[{n}A";

    /// <summary>光标下移 n 行。</summary>
    public static string CursorDown(int n) => $"{Esc}[{n}B";

    /// <summary>光标右移 n 列。</summary>
    public static string CursorRight(int n) => $"{Esc}[{n}C";

    /// <summary>光标左移 n 列。</summary>
    public static string CursorLeft(int n) => $"{Esc}[{n}D";

    /// <summary>保存光标位置。</summary>
    public static string SaveCursor => $"{Esc}[s";

    /// <summary>恢复光标位置。</summary>
    public static string RestoreCursor => $"{Esc}[u";

    /// <summary>显示光标。</summary>
    public static string ShowCursor => $"{Esc}[?25h";

    /// <summary>隐藏光标。</summary>
    public static string HideCursor => $"{Esc}[?25l";

    /// <summary>整屏清屏。</summary>
    public static string ClearScreen => $"{Esc}[2J";

    /// <summary>清除从光标到行尾。</summary>
    public static string ClearLine => $"{Esc}[K";

    /// <summary>进入备用屏幕缓冲。</summary>
    public static string EnterAltScreen => $"{Esc}[?1049h";

    /// <summary>退出备用屏幕缓冲。</summary>
    public static string ExitAltScreen => $"{Esc}[?1049l";

    /// <summary>启用括号粘贴协议。</summary>
    public static string EnableBracketedPaste => $"{Esc}[?2004h";

    /// <summary>禁用括号粘贴协议。</summary>
    public static string DisableBracketedPaste => $"{Esc}[?2004l";

    /// <summary>启用 SGR 鼠标协议。</summary>
    public static string EnableSgrMouse => $"{Esc}[?1000h{Esc}[?1006h";

    /// <summary>禁用鼠标协议。</summary>
    public static string DisableSgrMouse => $"{Esc}[?1000l{Esc}[?1006l";

    /// <summary>基础前景色。</summary>
    public static string Fg(Color c) => c switch
    {
        Color.Black => $"{Esc}[30m",
        Color.Red => $"{Esc}[31m",
        Color.Green => $"{Esc}[32m",
        Color.Yellow => $"{Esc}[33m",
        Color.Blue => $"{Esc}[34m",
        Color.Magenta => $"{Esc}[35m",
        Color.Cyan => $"{Esc}[36m",
        Color.White => $"{Esc}[37m",
        Color.BrightBlack => $"{Esc}[90m",
        Color.BrightRed => $"{Esc}[91m",
        Color.BrightGreen => $"{Esc}[92m",
        Color.BrightYellow => $"{Esc}[93m",
        Color.BrightBlue => $"{Esc}[94m",
        Color.BrightMagenta => $"{Esc}[95m",
        Color.BrightCyan => $"{Esc}[96m",
        Color.BrightWhite => $"{Esc}[97m",
        _ => $"{Esc}[38;5;{(int)c}m",
    };

    /// <summary>基础背景色。</summary>
    public static string Bg(Color c) => c switch
    {
        Color.Black => $"{Esc}[40m",
        Color.Red => $"{Esc}[41m",
        Color.Green => $"{Esc}[42m",
        Color.Yellow => $"{Esc}[43m",
        Color.Blue => $"{Esc}[44m",
        Color.Magenta => $"{Esc}[45m",
        Color.Cyan => $"{Esc}[46m",
        Color.White => $"{Esc}[47m",
        Color.BrightBlack => $"{Esc}[100m",
        Color.BrightRed => $"{Esc}[101m",
        Color.BrightGreen => $"{Esc}[102m",
        Color.BrightYellow => $"{Esc}[103m",
        Color.BrightBlue => $"{Esc}[104m",
        Color.BrightMagenta => $"{Esc}[105m",
        Color.BrightCyan => $"{Esc}[106m",
        Color.BrightWhite => $"{Esc}[107m",
        _ => $"{Esc}[48;5;{(int)c}m",
    };

    /// <summary>24 位真彩前景色。</summary>
    public static string FgRgb(int r, int g, int b) => $"{Esc}[38;2;{r};{g};{b}m";

    /// <summary>24 位真彩背景色。</summary>
    public static string BgRgb(int r, int g, int b) => $"{Esc}[48;2;{r};{g};{b}m";

    /// <summary>重置所有属性。</summary>
    public static string Reset => $"{Esc}[0m";

    /// <summary>粗体。</summary>
    public static string Bold => $"{Esc}[1m";

    /// <summary>下划线。</summary>
    public static string Underline => $"{Esc}[4m";

    /// <summary>反显。</summary>
    public static string Reverse => $"{Esc}[7m";

    /// <summary>闪烁（用于光标提示）。</summary>
    public static string Blink => $"{Esc}[5m";
}

/// <summary>基础颜色枚举；值 ≥16 表示 256 色索引。</summary>
public enum Color
{
    Black = 0,
    Red = 1,
    Green = 2,
    Yellow = 3,
    Blue = 4,
    Magenta = 5,
    Cyan = 6,
    White = 7,
    BrightBlack = 8,
    BrightRed = 9,
    BrightGreen = 10,
    BrightYellow = 11,
    BrightBlue = 12,
    BrightMagenta = 13,
    BrightCyan = 14,
    BrightWhite = 15,
    // 256 色索引直接以 int 值表示
    Grey82 = 82,
    Orange = 208,
    SkyBlue = 75,
    PaleGreen = 114,
    Gold = 178,
    Violet = 141,
}
