namespace WayCoder.UI.Shared;

/// <summary>
/// ANSI 颜色常量 —— 命名清晰，替代魔数。
/// 所有 TuiTheme 和控件颜色都应使用这些常量。
/// </summary>
public static class TuiColors
{
    // ═══════════════════════════════════════════════════════
    // 标准前景色 (ANSI 30-37)
    // ═══════════════════════════════════════════════════════
    public const int Black   = 30;
    public const int Red     = 31;
    public const int Green   = 32;
    public const int Yellow  = 33;
    public const int Blue    = 34;
    public const int Magenta = 35;
    public const int Cyan    = 36;
    public const int White   = 37;

    // ═══════════════════════════════════════════════════════
    // 标准背景色 (ANSI 40-47)
    // ═══════════════════════════════════════════════════════
    public const int BgBlack   = 40;
    public const int BgRed     = 41;
    public const int BgGreen   = 42;
    public const int BgYellow  = 43;
    public const int BgBlue    = 44;
    public const int BgMagenta = 45;
    public const int BgCyan    = 46;
    public const int BgWhite   = 47;

    // ═══════════════════════════════════════════════════════
    // 亮前景色 (ANSI 90-97)
    // ═══════════════════════════════════════════════════════
    public const int BrightBlack   = 90;
    public const int BrightRed     = 91;
    public const int BrightGreen   = 92;
    public const int BrightYellow  = 93;
    public const int BrightBlue    = 94;
    public const int BrightMagenta = 95;
    public const int BrightCyan    = 96;
    public const int BrightWhite   = 97;

    // ═══════════════════════════════════════════════════════
    // 亮背景色 (ANSI 100-107)
    // ═══════════════════════════════════════════════════════
    public const int BgBrightBlack   = 100;
    public const int BgBrightRed     = 101;
    public const int BgBrightGreen   = 102;
    public const int BgBrightYellow  = 103;
    public const int BgBrightBlue    = 104;
    public const int BgBrightMagenta = 105;
    public const int BgBrightCyan    = 106;
    public const int BgBrightWhite   = 107;

    // ═══════════════════════════════════════════════════════
    // 语义别名（保留旧有兼容）
    // ═══════════════════════════════════════════════════════
    public const int Grey    = BrightBlack;   // 90
    public const int BgGrey  = BgBrightBlack; // 100
    public const int DimFg   = BrightBlack;   // 90

    // 功能色别名
    public const int Border       = Yellow;       // 33
    public const int HeadingFg    = Yellow;       // 33
    public const int AccentFg     = Cyan;         // 36
    public const int SuccessFg    = Green;        // 32
    public const int WarnFg       = Yellow;       // 33
    public const int ErrorFg      = Red;          // 31
    public const int TableBorder  = Yellow;       // 33
    public const int TableHeadingFg = Yellow;     // 33
}
