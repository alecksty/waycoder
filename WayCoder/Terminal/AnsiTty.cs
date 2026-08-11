using System.Text;

namespace WayCoder.Terminal;

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
    public const char AnsiCharPrefix = '\x1b';
    public const char AnsiCharEscape = '[';

    // ═══════════════════════════════════════════════════════════════
    // 光标控制
    // ═══════════════════════════════════════════════════════════════

    /// <summary>移动光标到 (row, col) — 1-based 终端坐标</summary>
    public static string CursorPos(int row, int col) => $"{AnsiCharPrefix}{AnsiCharEscape}{row};{col}H";

    /// <summary>移动光标到 (row, col) — 0-based 自动转 1-based</summary>
    public static string CursorPos0(int row, int col) => $"{AnsiCharPrefix}{AnsiCharEscape}{row + 1};{col + 1}H";

    /// <summary>隐藏光标</summary>
    public static string CursorHide = $"{AnsiCharPrefix}{AnsiCharEscape}?25l";

    /// <summary>显示光标</summary>
    public static string CursorShow = $"{AnsiCharPrefix}{AnsiCharEscape}?25h";

    /// <summary>保存光标位置</summary>
    public static string CursorSave = $"{AnsiCharPrefix}{AnsiCharEscape}s";

    /// <summary>恢复光标位置</summary>
    public static string CursorRestore = $"{AnsiCharPrefix}{AnsiCharEscape}u";

    // ═══════════════════════════════════════════════════════════════
    // 屏幕控制
    // ═══════════════════════════════════════════════════════════════

    public static readonly string ClearScreen = $"{AnsiCharPrefix}{AnsiCharEscape}2J";
    public static readonly string ClearToEnd = $"{AnsiCharPrefix}{AnsiCharEscape}K";
    public static readonly string ClearLine = $"{AnsiCharPrefix}{AnsiCharEscape}2K";
    public static readonly string Home = $"{AnsiCharPrefix}{AnsiCharEscape}H";
    public static readonly string EnterAlt = $"{AnsiCharPrefix}{AnsiCharEscape}?1049h";
    public static readonly string ExitAlt = $"{AnsiCharPrefix}{AnsiCharEscape}?1049l";

    public static string ScrollUp(int n = 1) => $"{AnsiCharPrefix}{AnsiCharEscape}{n}S";
    public static string ScrollDown(int n = 1) => $"{AnsiCharPrefix}{AnsiCharEscape}{n}T";

    // ═══════════════════════════════════════════════════════════════
    // 鼠标协议
    // ═══════════════════════════════════════════════════════════════

    public static readonly string MouseEnable = $"{AnsiCharPrefix}{AnsiCharEscape}?1000h{AnsiCharPrefix}{AnsiCharEscape}?1003h{AnsiCharPrefix}{AnsiCharEscape}?1015h{AnsiCharPrefix}{AnsiCharEscape}?1006h";
    public static readonly string MouseDisable = $"{AnsiCharPrefix}{AnsiCharEscape}?1006l{AnsiCharPrefix}{AnsiCharEscape}?1015l{AnsiCharPrefix}{AnsiCharEscape}?1003l{AnsiCharPrefix}{AnsiCharEscape}?1000l";

    // ═══════════════════════════════════════════════════════════════
    // 粘贴协议 (bracketed paste)
    // ═══════════════════════════════════════════════════════════════

    /// <summary>启用 bracketed paste：终端包裹粘贴内容为 \x1b[200~...\x1b[201~</summary>
    public static readonly string BracketedPasteEnable = $"{AnsiCharPrefix}{AnsiCharEscape}?2004h";
    /// <summary>禁用 bracketed paste</summary>
    public static readonly string BracketedPasteDisable = $"{AnsiCharPrefix}{AnsiCharEscape}?2004l";

    // ═══════════════════════════════════════════════════════════════
    // Kitty 键盘协议
    // ═══════════════════════════════════════════════════════════════

    /// <summary>查询 Kitty 键盘协议支持</summary>
    public static readonly string KittyQuery = $"{AnsiCharPrefix}{AnsiCharEscape}>q";
    /// <summary>启用 Kitty 键盘协议 Level 1（修饰键 + 功能键报告为 CSI u 序列）</summary>
    public static readonly string KittyEnable = $"{AnsiCharPrefix}{AnsiCharEscape}>1u";
    /// <summary>禁用 Kitty 键盘协议</summary>
    public static readonly string KittyDisable = $"{AnsiCharPrefix}{AnsiCharEscape}>0u";

    // ═══════════════════════════════════════════════════════════════
    // SGR 样式（字符属性）
    // ═══════════════════════════════════════════════════════════════

    public static readonly string SgrReset = $"{AnsiCharPrefix}{AnsiCharEscape}0m";
    public static readonly string SgrBold = $"{AnsiCharPrefix}{AnsiCharEscape}1m";
    public static readonly string SgrDim = $"{AnsiCharPrefix}{AnsiCharEscape}2m";
    public static readonly string SgrItalic = $"{AnsiCharPrefix}{AnsiCharEscape}3m";
    public static readonly string SgrUnderline = $"{AnsiCharPrefix}{AnsiCharEscape}4m";
    public static readonly string SgrBlink = $"{AnsiCharPrefix}{AnsiCharEscape}5m";
    public static readonly string SgrResetFg = $"{AnsiCharPrefix}{AnsiCharEscape}39m";
    public static readonly string SgrResetBg = $"{AnsiCharPrefix}{AnsiCharEscape}49m";

    // ═══════════════════════════════════════════════════════════════
    // SGR 颜色（标准 16 色 ANSI）
    // ═══════════════════════════════════════════════════════════════

    /// <summary>前景色序列 \x1b[CODE m</summary>
    public static string Fg(int code) => $"{AnsiCharPrefix}{AnsiCharEscape}{code}m";

    /// <summary>背景色序列 \x1b[CODE m（code 应为 40-47/100-107）</summary>
    public static string Bg(int code) => $"{AnsiCharPrefix}{AnsiCharEscape}{code}m";

    /// <summary>前景+背景组合 \x1b[FG;BG m</summary>
    public static string FgBg(int fg, int bg) => $"{AnsiCharPrefix}{AnsiCharEscape}{fg};{bg}m";

    /// <summary>粗体前景色 \x1b[1;CODE m</summary>
    public static string BoldFg(int code) => $"{AnsiCharPrefix}{AnsiCharEscape}1;{code}m";

    /// <summary>组合 SGR 参数 \x1b[A;B;... m</summary>
    public static string Sgr(params int[] codes) => $"{AnsiCharPrefix}{AnsiCharEscape}{string.Join(";", codes)}m";

    // ═══════════════════════════════════════════════════════════════
    // 256 色调色板
    // ═══════════════════════════════════════════════════════════════

    public static string Fg256(int code) => $"{AnsiCharPrefix}{AnsiCharEscape}38;5;{code}m";
    public static string Bg256(int code) => $"{AnsiCharPrefix}{AnsiCharEscape}48;5;{code}m";

    // ═══════════════════════════════════════════════════════════════
    // True Color (24-bit RGB)
    // ═══════════════════════════════════════════════════════════════

    public static string FgRgb(int r, int g, int b) => $"{AnsiCharPrefix}{AnsiCharEscape}38;2;{r};{g};{b}m";
    public static string BgRgb(int r, int g, int b) => $"{AnsiCharPrefix}{AnsiCharEscape}48;2;{r};{g};{b}m";

    /// <summary>将 RGB 编码为内部 TrueColor 颜色码（≥0x1000000），可透传给 FgCode/BgCode</summary>
    public static int RgbCode(int r, int g, int b) => 0x1000000 | ((r & 0xFF) << 16) | ((g & 0xFF) << 8) | (b & 0xFF);

    /// <summary>从 TrueColor 颜色码解码 RGB 分量</summary>
    public static (int r, int g, int b) DecodeRgb(int code)
    {
        int rgb = code - 0x1000000;
        return ((rgb >> 16) & 0xFF, (rgb >> 8) & 0xFF, rgb & 0xFF);
    }

    /// <summary>在两个 TrueColor 颜色码之间线性插值</summary>
    public static int LerpRgb(int from, int to, float t)
    {
        var (r1, g1, b1) = DecodeRgb(from);
        var (r2, g2, b2) = DecodeRgb(to);
        return RgbCode(
            (int)(r1 + (r2 - r1) * t),
            (int)(g1 + (g2 - g1) * t),
            (int)(b1 + (b2 - b1) * t));
    }

    /// <summary>将 TrueColor 向白色方向调亮（amount=0 不变，1 全白）</summary>
    public static int LightenRgb(int code, float amount)
    {
        var (r, g, b) = DecodeRgb(code);
        return RgbCode(
            Math.Min(255, (int)(r + (255 - r) * amount)),
            Math.Min(255, (int)(g + (255 - g) * amount)),
            Math.Min(255, (int)(b + (255 - b) * amount)));
    }

    /// <summary>将 TrueColor 向黑色方向调暗（amount=0 不变，1 全黑）</summary>
    public static int DarkenRgb(int code, float amount)
    {
        var (r, g, b) = DecodeRgb(code);
        return RgbCode(
            Math.Max(0, (int)(r * (1 - amount))),
            Math.Max(0, (int)(g * (1 - amount))),
            Math.Max(0, (int)(b * (1 - amount))));
    }

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
        if (code is >= 30 and <= 37 || code is >= 90 and <= 97)
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
        if (code is >= 40 and <= 47 or >= 100 and <= 107)
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
        if (bothStd) return $"{FgBg(fg, bg)}";

        return FgCode(fg) + BgCode(bg);
    }

    // ═══════════════════════════════════════════════════════════════
    // 便捷：包裹文本（用于简单场景，如权限提示、帮助文本）
    // ═══════════════════════════════════════════════════════════════

    public static string FgText(string text, int color) => $"{Fg(color)}{text}{SgrReset}";
    public static string FgBgText(string text, int fg, int bg) => $"{FgBg(fg, bg)}{text}{SgrReset}";
    public static string BoldFgText(string text, int color) => $"{BoldFg(color)}{text}{SgrReset}";
    public static string Accent(string text) => $"{Fg(36)}{text}{SgrReset}";
    public static string Warn(string text) => $"{Fg(33)}{text}{SgrReset}";
    public static string Error(string text) => $"{Fg(31)}{text}{SgrReset}";
    public static string Success(string text) => $"{Fg(32)}{text}{SgrReset}";
    public static string DimText(string text) => $"{SgrDim}{text}{SgrReset}";
    public static string BoldText(string text) => $"{SgrBold}{text}{SgrReset}";
    public static string HeadingText(string text) => $"{BoldFg(33)}{text}{SgrReset}";
    public static string PromptText(string text) => $"{Fg(36)}{text}{SgrReset}";
}