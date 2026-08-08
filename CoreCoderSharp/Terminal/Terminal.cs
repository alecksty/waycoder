namespace CoreCoderSharp.Terminal;

/// <summary>
/// ANSI 终端抽象层 —— 统一封装所有 TTY 操作。
/// 所有终端渲染都通过此类，不直接拼接 ANSI 转义字符串。
/// </summary>
public static class TTY
{
    // ================================================================
    // 屏幕控制
    // ================================================================

    /// <summary>进入备用屏（保存原始终端内容）</summary>
    public static void EnterAltScreen() => WriteRaw("\x1b[?1049h\x1b[2J\x1b[?25l");

    /// <summary>退出备用屏（恢复原始终端）</summary>
    public static void ExitAltScreen() => WriteRaw("\x1b[?25h\x1b[?1049l");

    /// <summary>清屏</summary>
    public static void Clear() => WriteRaw("\x1b[2J\x1b[H");

    /// <summary>隐藏光标</summary>
    public static void HideCursor() => WriteRaw("\x1b[?25l");

    /// <summary>显示光标</summary>
    public static void ShowCursor() => WriteRaw("\x1b[?25h");

    /// <summary>终端宽度（列数）</summary>
    public static int Cols => Console.WindowWidth;

    /// <summary>终端高度（行数）</summary>
    public static int Rows => Console.WindowHeight;

    /// <summary>是否尺寸变化</summary>
    public static bool SizeChanged(ref int lastW, ref int lastH)
    {
        var (w, h) = (Cols, Rows);
        if (w == lastW && h == lastH) return false;
        lastW = w; lastH = h;
        return true;
    }

    /// <summary>写入原始 ANSI 序列到终端</summary>
    public static void WriteRaw(string ansi) => Console.Write(ansi);

    // ================================================================
    // 颜色
    // ================================================================

    public static readonly AnsiColor Default = new(0);
    public static readonly AnsiColor Black = new(30);
    public static readonly AnsiColor Red = new(31);
    public static readonly AnsiColor Green = new(32);
    public static readonly AnsiColor Yellow = new(33);
    public static readonly AnsiColor Blue = new(34);
    public static readonly AnsiColor Magenta = new(35);
    public static readonly AnsiColor Cyan = new(36);
    public static readonly AnsiColor White = new(37);
    public static readonly AnsiColor BrightBlack = new(90);
    public static readonly AnsiColor BrightRed = new(91);
    public static readonly AnsiColor BrightGreen = new(92);
    public static readonly AnsiColor BrightYellow = new(93);
    public static readonly AnsiColor BrightBlue = new(94);
    public static readonly AnsiColor BrightMagenta = new(95);
    public static readonly AnsiColor BrightCyan = new(96);
    public static readonly AnsiColor BrightWhite = new(97);

    // 背景色 (40-47, 100-107)
    public static readonly AnsiColor BgBlack = new(40);
    public static readonly AnsiColor BgRed = new(41);
    public static readonly AnsiColor BgGreen = new(42);
    public static readonly AnsiColor BgYellow = new(43);
    public static readonly AnsiColor BgBlue = new(44);
    public static readonly AnsiColor BgMagenta = new(45);
    public static readonly AnsiColor BgCyan = new(46);
    public static readonly AnsiColor BgWhite = new(47);
    public static readonly AnsiColor BgGray = new(100);

    // 样式
    public const string Bold = "\x1b[1m";
    public const string Dim = "\x1b[2m";
    public const string Italic = "\x1b[3m";
    public const string Reset = "\x1b[0m";
}

/// <summary>ANSI 颜色值（不可变）</summary>
public readonly record struct AnsiColor(int Code)
{
    /// <summary>生成 SGR 前景色序列：\x1b[36m</summary>
    public string Fg() => $"\x1b[{Code}m";
    /// <summary>生成 SGR 背景色序列：\x1b[46m</summary>
    public string Bg() => $"\x1b[{Code + 10}m"; // 仅对 30-37 有效
    /// <summary>前景+背景组合</summary>
    public string FgBg(AnsiColor bg) => $"\x1b[{Code};{bg.Code}m";

    /// <summary>ANSI 转义码前缀部分（用于拼接）</summary>
    public string SgrCode => Code.ToString();

    public static implicit operator int(AnsiColor c) => c.Code;
    public static implicit operator AnsiColor(int code) => new(code);
}
