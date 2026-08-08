namespace CoreCoderSharp.Terminal;

/// <summary>
/// 终端颜色 —— 用名字代替 ANSI 数字码。
/// 用法：Color.Cyan, Color.BgBlue, Color.BrightYellow
/// </summary>
public readonly struct Color
{
    public int AnsiCode { get; }
    public int Fg => AnsiCode;
    public int Bg { get; }

    public Color(int code) { AnsiCode = code; Bg = 0; }
    public Color(int fg, int bg) { AnsiCode = fg; Bg = bg; }

    // ================================================================
    // 前景色 (30-37, 90-97)
    // ================================================================
    public static readonly Color Default       = new(0);
    public static readonly Color Black         = new(30);
    public static readonly Color Red           = new(31);
    public static readonly Color Green         = new(32);
    public static readonly Color Yellow        = new(33);
    public static readonly Color Blue          = new(34);
    public static readonly Color Magenta       = new(35);
    public static readonly Color Cyan          = new(36);
    public static readonly Color White         = new(37);
    public static readonly Color BrightBlack   = new(90);
    public static readonly Color BrightRed     = new(91);
    public static readonly Color BrightGreen   = new(92);
    public static readonly Color BrightYellow  = new(93);
    public static readonly Color BrightBlue    = new(94);
    public static readonly Color BrightMagenta = new(95);
    public static readonly Color BrightCyan    = new(96);
    public static readonly Color BrightWhite   = new(97);

    // ================================================================
    // 背景色 (40-47, 100-107)
    // ================================================================
    public static readonly Color BgBlack    = new(40);
    public static readonly Color BgRed      = new(41);
    public static readonly Color BgGreen    = new(42);
    public static readonly Color BgYellow   = new(43);
    public static readonly Color BgBlue     = new(44);
    public static readonly Color BgMagenta  = new(45);
    public static readonly Color BgCyan     = new(46);
    public static readonly Color BgWhite    = new(47);
    public static readonly Color BgGray     = new(100);

    // ================================================================
    // 常用配色
    // ================================================================
    public static readonly Color SelFg      = Black;
    public static readonly Color SelBg      = BgCyan;
    public static readonly Color Border     = Cyan;
    public static readonly Color TitleFg    = White;
    public static readonly Color DimText    = BrightBlack;
    public static readonly Color MaskBg     = BgGray;
    public static readonly Color InputFocus  = new(37, 44);
    public static readonly Color ButtonNormal= new(37, 44);
    public static readonly Color ButtonFocus = new(30, 46);

    /// <summary>带指定背景</summary>
    public Color On(Color bg) => new(AnsiCode, bg.AnsiCode);

    public static implicit operator int(Color c) => c.AnsiCode;
    public override string ToString() => Bg > 0 ? $"{AnsiCode}/{Bg}" : $"{AnsiCode}";
}
