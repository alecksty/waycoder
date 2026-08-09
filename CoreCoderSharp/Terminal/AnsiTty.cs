using System.Text;

namespace CoreCoderSharp.Terminal;

/// <summary>
/// ANSI 转义序列集中定义 —— 项目中唯一直接写 \x1b 的地方。
/// 提供静态字符串常量和 StringBuilder 扩展方法。
///
/// 原则：
///   - 所有 \x1b 只在这里出现
///   - 其他文件通过 AnsiTty 常量/方法 或 RenderBuffer API 间接使用
///   - 颜色值语义：0-15=标准ANSI, 16-255=256色, ≥0x1000000=TrueColor RGB
/// </summary>
public static class AnsiTty
{
    // ═══════════════════════════════════════════════════════════════
    // 光标控制
    // ═══════════════════════════════════════════════════════════════

    /// <summary>移动光标到 (row, col) — 1-based 终端坐标</summary>
    public static string CursorPos(int row, int col) => $"\x1b[{row};{col}H";

    /// <summary>移动光标到 (row, col) — 0-based 自动转 1-based</summary>
    public static string CursorPos0(int row, int col) => $"\x1b[{row + 1};{col + 1}H";

    public const string CursorHide   = "\x1b[?25l";
    public const string CursorShow   = "\x1b[?25h";
    public const string CursorSave   = "\x1b[s";
    public const string CursorRestore = "\x1b[u";

    // ═══════════════════════════════════════════════════════════════
    // 屏幕控制
    // ═══════════════════════════════════════════════════════════════

    public const string ClearScreen  = "\x1b[2J";
    public const string ClearToEnd   = "\x1b[K";
    public const string ClearLine    = "\x1b[2K";
    public const string Home         = "\x1b[H";
    public const string EnterAlt     = "\x1b[?1049h";
    public const string ExitAlt      = "\x1b[?1049l";

    public static string ScrollUp(int n = 1)   => $"\x1b[{n}S";
    public static string ScrollDown(int n = 1) => $"\x1b[{n}T";

    // ═══════════════════════════════════════════════════════════════
    // 鼠标协议
    // ═══════════════════════════════════════════════════════════════

    public const string MouseEnable  = "\x1b[?1000h\x1b[?1003h\x1b[?1015h\x1b[?1006h";
    public const string MouseDisable = "\x1b[?1006l\x1b[?1015l\x1b[?1003l\x1b[?1000l";

    // ═══════════════════════════════════════════════════════════════
    // SGR 样式（字符属性）
    // ═══════════════════════════════════════════════════════════════

    public const string SgrReset   = "\x1b[0m";
    public const string SgrBold    = "\x1b[1m";
    public const string SgrDim     = "\x1b[2m";
    public const string SgrItalic  = "\x1b[3m";
    public const string SgrUnderline = "\x1b[4m";
    public const string SgrBlink   = "\x1b[5m";
    public const string SgrResetFg = "\x1b[39m";
    public const string SgrResetBg = "\x1b[49m";

    // ═══════════════════════════════════════════════════════════════
    // SGR 颜色（标准 16 色 ANSI）
    // ═══════════════════════════════════════════════════════════════

    /// <summary>前景色序列 \x1b[CODE m</summary>
    public static string Fg(int code) => $"\x1b[{code}m";

    /// <summary>背景色序列 \x1b[CODE m（code 应为 40-47/100-107）</summary>
    public static string Bg(int code) => $"\x1b[{code}m";

    /// <summary>前景+背景组合 \x1b[FG;BG m</summary>
    public static string FgBg(int fg, int bg) => $"\x1b[{fg};{bg}m";

    /// <summary>粗体前景色 \x1b[1;CODE m</summary>
    public static string BoldFg(int code) => $"\x1b[1;{code}m";

    /// <summary>组合 SGR 参数 \x1b[A;B;... m</summary>
    public static string Sgr(params int[] codes) =>
        $"\x1b[{string.Join(";", codes)}m";

    // ═══════════════════════════════════════════════════════════════
    // 256 色调色板
    // ═══════════════════════════════════════════════════════════════

    public static string Fg256(int code)  => $"\x1b[38;5;{code}m";
    public static string Bg256(int code)  => $"\x1b[48;5;{code}m";

    // ═══════════════════════════════════════════════════════════════
    // True Color (24-bit RGB)
    // ═══════════════════════════════════════════════════════════════

    public static string FgRgb(int r, int g, int b) => $"\x1b[38;2;{r};{g};{b}m";
    public static string BgRgb(int r, int g, int b) => $"\x1b[48;2;{r};{g};{b}m";

    // ═══════════════════════════════════════════════════════════════
    // 颜色码 → 序列（自动识别 16色/256色/TrueColor）
    // ═══════════════════════════════════════════════════════════════

    /// <summary>根据颜色码生成前景序列（自动识别：标准 ANSI 30-37/90-97 → 256色 → TrueColor）</summary>
    public static string FgCode(int code)
    {
        if (code <= 0) return "";
        if (code >= 0x1000000)
        {
            int rgb = code - 0x1000000;
            return FgRgb((rgb >> 16) & 0xFF, (rgb >> 8) & 0xFF, rgb & 0xFF);
        }
        // 标准 ANSI 前景色 30-37 和 亮前景 90-97
        if ((code >= 30 && code <= 37) || (code >= 90 && code <= 97))
            return Fg(code);
        if (code >= 16) return Fg256(code);
        return Fg(code);
    }

    /// <summary>根据颜色码生成背景序列（自动识别：标准 ANSI 40-47/100-107 → 256色 → TrueColor）</summary>
    public static string BgCode(int code)
    {
        if (code <= 0) return "";
        if (code >= 0x1000000)
        {
            int rgb = code - 0x1000000;
            return BgRgb((rgb >> 16) & 0xFF, (rgb >> 8) & 0xFF, rgb & 0xFF);
        }
        // 标准 ANSI 背景色 40-47 和 亮背景 100-107
        if ((code >= 40 && code <= 47) || (code >= 100 && code <= 107))
            return Bg(code);
        if (code >= 16) return Bg256(code);
        return Bg(code);
    }

    /// <summary>前景+背景组合（自动识别类型）</summary>
    public static string FgBgCode(int fg, int bg)
    {
        if (fg <= 0 && bg <= 0) return "";
        if (fg <= 0) return BgCode(bg);
        if (bg <= 0) return FgCode(fg);

        // 两者都是标准 ANSI 16 色可合并为 \x1b[fg;bg m
        bool bothStd = fg < 256 && bg < 256 && fg < 0x1000000 && bg < 0x1000000;
        if (bothStd) return $"\x1b[{fg};{bg}m";

        return FgCode(fg) + BgCode(bg);
    }

    // ═══════════════════════════════════════════════════════════════
    // 便捷：包裹文本（用于简单场景，如权限提示、帮助文本）
    // ═══════════════════════════════════════════════════════════════

    public static string FgText(string text, int color) => $"{Fg(color)}{text}{SgrReset}";
    public static string FgBgText(string text, int fg, int bg) => $"{FgBg(fg, bg)}{text}{SgrReset}";
    public static string BoldFgText(string text, int color) => $"{BoldFg(color)}{text}{SgrReset}";
    public static string Accent(string text) => $"{Fg(36)}{text}{SgrReset}";
    public static string Warn(string text)   => $"{Fg(33)}{text}{SgrReset}";
    public static string Error(string text)  => $"{Fg(31)}{text}{SgrReset}";
    public static string Success(string text) => $"{Fg(32)}{text}{SgrReset}";
    public static string DimText(string text) => $"{SgrDim}{text}{SgrReset}";
    public static string BoldText(string text) => $"{SgrBold}{text}{SgrReset}";
    public static string HeadingText(string text) => $"{BoldFg(33)}{text}{SgrReset}";
    public static string PromptText(string text) => $"{Fg(36)}{text}{SgrReset}";

}
