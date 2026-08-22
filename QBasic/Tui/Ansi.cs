// QBasic/Tui/Ansi.cs
// ANSI 转义序列生成：颜色（256 色 + RGB）、光标、清屏、备用屏缓冲。
namespace QBasic.Tui;

public static class Ansi
{
    public static string CursorUp(int n) => $"\x1b[{n}A";
    public static string CursorDown(int n) => $"\x1b[{n}B";
    public static string CursorRight(int n) => $"\x1b[{n}C";
    public static string CursorLeft(int n) => $"\x1b[{n}D";
    public static string MoveTo(int row, int col) => $"\x1b[{row};{col}H";
    public static string ClearScreen() => "\x1b[2J";
    public static string ClearLine() => "\x1b[2K";
    public static string ClearToEndOfLine() => "\x1b[K";
    public static string ShowCursor() => "\x1b[?25h";
    public static string HideCursor() => "\x1b[?25l";
    public static string EnterAltScreen() => "\x1b[?1049h";
    public static string ExitAltScreen() => "\x1b[?1049l";
    public static string Reset() => "\x1b[0m";
    public static string EnableMouseSgr() => "\x1b[?1000h\x1b[?1006h";
    public static string DisableMouseSgr() => "\x1b[?1006l\x1b[?1000l";
    public static string EnableBracketedPaste() => "\x1b[?2004h";
    public static string DisableBracketedPaste() => "\x1b[?2004l";

    public static string Fg(int color) => $"\x1b[38;5;{color}m";
    public static string Bg(int color) => $"\x1b[48;5;{color}m";
    public static string FgRgb(int r, int g, int b) => $"\x1b[38;2;{r};{g};{b}m";
    public static string BgRgb(int r, int g, int b) => $"\x1b[48;2;{r};{g};{b}m";

    public static string Bold() => "\x1b[1m";
    public static string Dim() => "\x1b[2m";
    public static string Italic() => "\x1b[3m";
    public static string Underline() => "\x1b[4m";
    public static string Inverse() => "\x1b[7m";
}
