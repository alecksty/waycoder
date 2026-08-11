namespace WayCoder.Terminal;

/// <summary>
/// TTY 终端抽象层 —— 所有终端操作通过此 API，不手写转义符。
/// </summary>
public static class Tty
{
    /// <summary>是否已进入备用屏</summary>
    private static bool _altScreen;

    static Tty()
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
        try { Console.Write($"{AnsiTty.MouseDisable}{AnsiTty.CursorShow}{AnsiTty.ExitAlt}"); Console.Out.Flush(); }
        catch { /* 进程即将退出，忽略所有错误 */ }
    }
    // ================================================================
    // 屏幕切换
    // ================================================================

    /// <summary>进入备用屏：保存终端内容、清屏、隐藏光标</summary>
    public static void EnterAltScreen() { _altScreen = true; Write($"{AnsiTty.EnterAlt}{AnsiTty.ClearScreen}{AnsiTty.CursorHide}"); }

    /// <summary>退出备用屏：显示光标、恢复原始终端内容</summary>
    public static void ExitAltScreen() { _altScreen = false; Write($"{AnsiTty.CursorShow}{AnsiTty.ExitAlt}"); }

    /// <summary>清屏并归位光标</summary>
    public static void Clear() => Write($"{AnsiTty.ClearScreen}{AnsiTty.Home}");

    /// <summary>清除当前行从光标到行尾</summary>
    public static void ClearToEndOfLine() => Write(AnsiTty.ClearToEnd);

    /// <summary>清除当前行</summary>
    public static void ClearLine() => Write(AnsiTty.ClearLine);

    // ================================================================
    // 光标
    // ================================================================

    /// <summary>移动光标到指定位置（1-based 终端坐标）</summary>
    public static void MoveTo(int row, int col) => Write(AnsiTty.CursorPos(row, col));

    /// <summary>隐藏光标</summary>
    public static void HideCursor() => Write(AnsiTty.CursorHide);

    /// <summary>显示光标</summary>
    public static void ShowCursor() => Write(AnsiTty.CursorShow);

    /// <summary>保存光标位置</summary>
    public static void SaveCursor() => Write(AnsiTty.CursorSave);

    /// <summary>恢复光标位置</summary>
    public static void RestoreCursor() => Write(AnsiTty.CursorRestore);

    // ================================================================
    // 滚动
    // ================================================================

    /// <summary>向上滚动 n 行</summary>
    public static void ScrollUp(int n = 1) => Write(AnsiTty.ScrollUp(n));

    /// <summary>向下滚动 n 行</summary>
    public static void ScrollDown(int n = 1) => Write(AnsiTty.ScrollDown(n));

    // ================================================================
    // 尺寸
    // ================================================================

    /// <summary>终端宽度（列数），无控制台时返回 80 安全默认值</summary>
    public static int Cols
    {
        get
        {
            try { return Console.WindowWidth; }
            catch (IOException) { return 80; }
        }
    }

    /// <summary>终端高度（行数），无控制台时返回 24 安全默认值</summary>
    public static int Rows
    {
        get
        {
            try { return Console.WindowHeight; }
            catch (IOException) { return 24; }
        }
    }

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

    /// <summary>等待按键到达（忙等），最多 timeoutMs 毫秒；超时返回 false</summary>
    public static bool WaitForKey(int timeoutMs)
    {
        int waited = 0;
        while (!Console.KeyAvailable)
        {
            if (waited >= timeoutMs) return false;
            Thread.Sleep(1);
            waited++;
        }
        return true;
    }

    /// <summary>
    /// 吞掉 \x1b 开头的转义序列（SGR 鼠标 \x1b[&lt;...、其他 CSI \x1b[...），防止泄漏为文本。
    /// 调用前提：\x1b 已被读取。
    /// 返回 true = 序列被完整吞掉；false = 不是转义序列（\x1b 是独立按键）。
    /// 注意：若 \x1b 后跟的是非 '[' 字符（如 Alt+字母），该字符已被消费且无法退回，调用方应丢弃。
    /// </summary>
    public static bool ConsumeEscapeSequence()
    {
        if (!WaitForKey(20)) return false;
        var bracket = Console.ReadKey(intercept: true);
        if (bracket.KeyChar != '[') return false;

        // 读到终止字节为止：CSI 以 0x40-0x7E 结尾，SGR 鼠标以 M/m 结尾
        for (int i = 0; i < 40; i++)
        {
            if (!WaitForKey(10)) break;
            var c = Console.ReadKey(intercept: true);
            if (c.KeyChar == 'M' || c.KeyChar == 'm' ||
                (c.KeyChar >= 0x40 && c.KeyChar <= 0x7E))
                return true;
        }
        return true;
    }

    /// <summary>启用鼠标跟踪（SGR 扩展协议）</summary>
    public static void EnableMouse() => Write(AnsiTty.MouseEnable);

    /// <summary>禁用鼠标跟踪</summary>
    public static void DisableMouse() => Write(AnsiTty.MouseDisable);

    /// <summary>启用 bracketed paste 模式</summary>
    public static void EnableBracketedPaste() => Write(AnsiTty.BracketedPasteEnable);

    /// <summary>禁用 bracketed paste 模式</summary>
    public static void DisableBracketedPaste() => Write(AnsiTty.BracketedPasteDisable);

    /// <summary>启用 Kitty 键盘协议</summary>
    public static void EnableKittyKeyboard() => Write(AnsiTty.KittyEnable);

    /// <summary>禁用 Kitty 键盘协议</summary>
    public static void DisableKittyKeyboard() => Write(AnsiTty.KittyDisable);

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

    public static void SetBold()    => Write(AnsiTty.SgrBold);
    public static void SetDim()     => Write(AnsiTty.SgrDim);
    public static void SetItalic()  => Write(AnsiTty.SgrItalic);
    public static void SetUnderline()=>Write(AnsiTty.SgrUnderline);
    public static void SetBlink()   => Write(AnsiTty.SgrBlink);
    public static void ResetStyle() => Write(AnsiTty.SgrReset);
}

/// <summary>ANSI 颜色值（不可变）</summary>
public readonly record struct AnsiColor(int Code)
{
    public string Fg() => AnsiTty.Fg(Code);
    public string Bg() => AnsiTty.Bg(Code + 10);
    public string FgBg(AnsiColor bg) => AnsiTty.FgBg(Code, bg.Code);
    public string SgrCode => Code.ToString();

    public static implicit operator int(AnsiColor c) => c.Code;
    public static implicit operator AnsiColor(int code) => new(code);
}
