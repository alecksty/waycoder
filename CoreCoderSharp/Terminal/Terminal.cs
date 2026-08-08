namespace CoreCoderSharp.Terminal;

/// <summary>
/// TTY 终端抽象层 —— 所有终端操作通过此 API，不手写转义符。
/// </summary>
public static class TTY
{
    /// <summary>是否已进入备用屏</summary>
    private static bool _altScreen;

    static TTY()
    {
        // 进程退出时自动恢复终端（即使崩溃）
        AppDomain.CurrentDomain.ProcessExit += (_, _) =>
        {
            if (_altScreen) ExitAltScreenDirect();
        };
    }

    /// <summary>静默恢复终端（进程退出时调用，忽略错误）</summary>
    private static void ExitAltScreenDirect()
    {
        try { Console.Write("\x1b[?25h\x1b[?1049l"); Console.Out.Flush(); }
        catch { /* 进程即将退出，忽略所有错误 */ }
    }
    // ================================================================
    // 屏幕切换
    // ================================================================

    /// <summary>进入备用屏：保存终端内容、清屏、隐藏光标</summary>
    public static void EnterAltScreen() { _altScreen = true; Write("\x1b[?1049h\x1b[2J\x1b[?25l"); }

    /// <summary>退出备用屏：显示光标、恢复原始终端内容</summary>
    public static void ExitAltScreen() { _altScreen = false; Write("\x1b[?25h\x1b[?1049l"); }

    /// <summary>清屏并归位光标</summary>
    public static void Clear() => Write("\x1b[2J\x1b[H");

    /// <summary>清除当前行从光标到行尾</summary>
    public static void ClearToEndOfLine() => Write("\x1b[K");

    /// <summary>清除当前行</summary>
    public static void ClearLine() => Write("\x1b[2K");

    // ================================================================
    // 光标
    // ================================================================

    /// <summary>移动光标到指定位置（1-based 终端坐标）</summary>
    public static void MoveTo(int row, int col) => Write($"\x1b[{row};{col}H");

    /// <summary>隐藏光标</summary>
    public static void HideCursor() => Write("\x1b[?25l");

    /// <summary>显示光标</summary>
    public static void ShowCursor() => Write("\x1b[?25h");

    /// <summary>保存光标位置</summary>
    public static void SaveCursor() => Write("\x1b[s");

    /// <summary>恢复光标位置</summary>
    public static void RestoreCursor() => Write("\x1b[u");

    // ================================================================
    // 滚动
    // ================================================================

    /// <summary>向上滚动 n 行</summary>
    public static void ScrollUp(int n = 1) => Write($"\x1b[{n}S");

    /// <summary>向下滚动 n 行</summary>
    public static void ScrollDown(int n = 1) => Write($"\x1b[{n}T");

    // ================================================================
    // 尺寸
    // ================================================================

    /// <summary>终端宽度（列数）</summary>
    public static int Cols => Console.WindowWidth;

    /// <summary>终端高度（行数）</summary>
    public static int Rows => Console.WindowHeight;

    /// <summary>检测尺寸变化，变化时更新 lastW/lastH 并返回 true</summary>
    public static bool SizeChanged(ref int lastW, ref int lastH)
    {
        var (w, h) = (Cols, Rows);
        if (w == lastW && h == lastH) return false;
        lastW = w; lastH = h;
        return true;
    }

    // ================================================================
    // 输入
    // ================================================================

    /// <summary>是否有按键可读</summary>
    public static bool KeyAvailable => Console.KeyAvailable;

    /// <summary>读取一个按键（不回显）</summary>
    public static ConsoleKeyInfo ReadKey() => Console.ReadKey(intercept: true);

    /// <summary>启用鼠标跟踪（SGR 扩展协议）</summary>
    public static void EnableMouse() =>
        Write("\x1b[?1000h\x1b[?1003h\x1b[?1015h\x1b[?1006h");

    /// <summary>禁用鼠标跟踪</summary>
    public static void DisableMouse() =>
        Write("\x1b[?1006l\x1b[?1015l\x1b[?1003l\x1b[?1000l");

    // ================================================================
    // 输出
    // ================================================================

    /// <summary>写入 ANSI 序列到终端</summary>
    public static void Write(string ansi) => Console.Write(ansi);

    /// <summary>写入单字符</summary>
    public static void Write(char c) => Console.Write(c);

    /// <summary>写入一行文本（无 ANSI 码）</summary>
    public static void WriteLine(string text = "") => Console.WriteLine(text);

    /// <summary>刷新输出缓冲</summary>
    public static void Flush() => Console.Out.Flush();

    // ================================================================
    // 颜色常量
    // ================================================================

    public static readonly AnsiColor Default      = new(0);
    public static readonly AnsiColor Black        = new(30);
    public static readonly AnsiColor Red          = new(31);
    public static readonly AnsiColor Green        = new(32);
    public static readonly AnsiColor Yellow       = new(33);
    public static readonly AnsiColor Blue         = new(34);
    public static readonly AnsiColor Magenta      = new(35);
    public static readonly AnsiColor Cyan         = new(36);
    public static readonly AnsiColor White        = new(37);
    public static readonly AnsiColor BrightBlack  = new(90);
    public static readonly AnsiColor BrightRed    = new(91);
    public static readonly AnsiColor BrightGreen  = new(92);
    public static readonly AnsiColor BrightYellow = new(93);
    public static readonly AnsiColor BrightBlue   = new(94);
    public static readonly AnsiColor BrightMagenta= new(95);
    public static readonly AnsiColor BrightCyan   = new(96);
    public static readonly AnsiColor BrightWhite  = new(97);
    public static readonly AnsiColor BgBlack      = new(40);
    public static readonly AnsiColor BgRed        = new(41);
    public static readonly AnsiColor BgGreen      = new(42);
    public static readonly AnsiColor BgYellow     = new(43);
    public static readonly AnsiColor BgBlue       = new(44);
    public static readonly AnsiColor BgMagenta    = new(45);
    public static readonly AnsiColor BgCyan       = new(46);
    public static readonly AnsiColor BgWhite      = new(47);
    public static readonly AnsiColor BgGray       = new(100);

    // ================================================================
    // 样式快捷方式
    // ================================================================

    public static void SetBold()    => Write("\x1b[1m");
    public static void SetDim()     => Write("\x1b[2m");
    public static void SetItalic()  => Write("\x1b[3m");
    public static void SetUnderline()=>Write("\x1b[4m");
    public static void SetBlink()   => Write("\x1b[5m");
    public static void ResetStyle() => Write("\x1b[0m");
}

/// <summary>ANSI 颜色值（不可变）</summary>
public readonly record struct AnsiColor(int Code)
{
    public string Fg() => $"\x1b[{Code}m";
    public string Bg() => $"\x1b[{Code + 10}m";
    public string FgBg(AnsiColor bg) => $"\x1b[{Code};{bg.Code}m";
    public string SgrCode => Code.ToString();

    public static implicit operator int(AnsiColor c) => c.Code;
    public static implicit operator AnsiColor(int code) => new(code);
}
