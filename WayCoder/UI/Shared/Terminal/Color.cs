namespace WayCoder.UI.Shared.Terminal;

/// <summary>
/// 终端颜色 —— 支持 16 标准色、256 色调色板、True Color (RGB)。
/// 色码语义：
///   0 = 默认
///   1-15, 30-37, 40-47, 90-97, 100-107 = 标准 16 色 ANSI
///   16-255 = 256 色调色板
///   >= 0x1000000 (16,777,216) = True Color RGB（编码为 0xRRGGBB + 0x1000000）
/// </summary>
public readonly struct Color
{
    public int AnsiCode { get; }
    public int Fg => AnsiCode;
    public int Bg { get; }

    /// <summary>是否为 True Color (24-bit RGB)</summary>
    public bool IsTrueColor => AnsiCode >= 0x1000000;

    /// <summary>True Color 的 RGB 分量</summary>
    public (byte R, byte G, byte B) Rgb => IsTrueColor
        ? ((byte)(((AnsiCode - 0x1000000) >> 16) & 0xFF), (byte)(((AnsiCode - 0x1000000) >> 8) & 0xFF), (byte)((AnsiCode - 0x1000000) & 0xFF))
        : ((byte)0, (byte)0, (byte)0);

    /// <summary>是否为 256 色调色板颜色</summary>
    public bool IsPalette256 => AnsiCode is >= 16 and < 256;

    public Color(int code)
    {
        AnsiCode = code;
        Bg = 0;
    }

    /// <summary>
    /// 创建一个前景色和背景色的组合颜色。
    /// 前景色范围：1-15, 30-37, 40-47, 90-97, 100-107
    /// 背景色范围：0-15, 40-47, 90-97, 100-107
    /// 256 色调色板范围：16-255
    /// True Color 色调范围：16,777,216-2147483647
    /// </summary>
    /// <param name="fg">前景色码</param>
    /// <param name="bg">背景色码</param>
    public Color(int fg, int bg)
    {
        AnsiCode = fg;
        Bg = bg;
    }

    // ── 工厂方法 ──

    /// <summary>从 256 色调色板创建前景色 (16-255)</summary>
    public static Color From256(int index) => new(Math.Clamp(index, 16, 255));

    /// <summary>从 RGB 创建 True Color (0-255 each)</summary>
    public static Color FromRgb(byte r, byte g, byte b) => new(0x1000000 | (r << 16) | (g << 8) | b);

    /// <summary>从灰度值创建 True Color</summary>
    public static Color FromGray(byte gray) => FromRgb(gray, gray, gray);

    // ── 常用 True Color 预置 ──
    public static readonly Color RgbOrange = FromRgb(255, 165, 0);
    public static readonly Color RgbPink = FromRgb(255, 105, 180);
    public static readonly Color RgbTeal = FromRgb(0, 128, 128);
    public static readonly Color RgbGold = FromRgb(255, 215, 0);
    public static readonly Color RgbDarkBg = FromRgb(30, 30, 30);

    // ================================================================
    // 前景色 (30-37, 90-97)
    // ================================================================
    public static readonly Color Default = new(0);
    public static readonly Color Black = new(30);
    public static readonly Color Red = new(31);
    public static readonly Color Green = new(32);
    public static readonly Color Yellow = new(33);
    public static readonly Color Blue = new(34);
    public static readonly Color Magenta = new(35);
    public static readonly Color Cyan = new(36);
    public static readonly Color White = new(37);
    public static readonly Color BrightBlack = new(90);
    public static readonly Color BrightRed = new(91);
    public static readonly Color BrightGreen = new(92);
    public static readonly Color BrightYellow = new(93);
    public static readonly Color BrightBlue = new(94);
    public static readonly Color BrightMagenta = new(95);
    public static readonly Color BrightCyan = new(96);
    public static readonly Color BrightWhite = new(97);

    // ================================================================
    // 背景色 (40-47, 100-107)
    // ================================================================
    public static readonly Color BgBlack = new(40);
    public static readonly Color BgRed = new(41);
    public static readonly Color BgGreen = new(42);
    public static readonly Color BgYellow = new(43);
    public static readonly Color BgBlue = new(44);
    public static readonly Color BgMagenta = new(45);
    public static readonly Color BgCyan = new(46);
    public static readonly Color BgWhite = new(47);
    public static readonly Color BgGray = new(100);

    // ================================================================
    // 常用配色
    // ================================================================
    public static readonly Color SelFg = Black;
    public static readonly Color SelBg = BgCyan;
    public static readonly Color Border = Cyan;
    public static readonly Color TitleFg = White;
    public static readonly Color DimText = BrightBlack;
    public static readonly Color MaskBg = BgGray;
    public static readonly Color InputFocus = new(37, 44);
    public static readonly Color ButtonNormal = new(37, 44);
    public static readonly Color ButtonFocus = new(30, 46);

    /// <summary>带指定背景</summary>
    public Color On(Color bg) => new(AnsiCode, bg.AnsiCode);

    public static implicit operator int(Color c) => c.AnsiCode;

    public override string ToString() => IsTrueColor
        ? $"#{Rgb.R:X2}{Rgb.G:X2}{Rgb.B:X2}"
        : Bg > 0
            ? $"{AnsiCode}/{Bg}"
            : $"{AnsiCode}";
}