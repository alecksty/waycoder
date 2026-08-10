namespace WayCoder.Terminal;

/// <summary>
/// 边框字符集 —— 13 种预设 + 自定义。
/// 用法：var (tl, tr, bl, br, h, v) = BoxChars.For("rounded");
/// </summary>
public readonly record struct BoxCharSet(string TL, string TR, string BL, string BR, string H, string V)
{
    // 预设
    public static readonly BoxCharSet Single = new("┌", "┐", "└", "┘", "─", "│");
    public static readonly BoxCharSet Double = new("╔", "╗", "╚", "╝", "═", "║");
    public static readonly BoxCharSet Rounded = new("╭", "╮", "╰", "╯", "─", "│");
    public static readonly BoxCharSet Thick = new("┏", "┓", "┗", "┛", "━", "┃");
    public static readonly BoxCharSet Solid = new("█", "█", "█", "█", "▀", "▌");
    public static readonly BoxCharSet SemiSolid = new("▄", "▄", "▀", "▀", "▀", "▐");
    public static readonly BoxCharSet Dotted = new("┌", "┐", "└", "┘", "┈", "┆");
    public static readonly BoxCharSet Dashed = new("┌", "┐", "└", "┘", "┅", "┇");
    public static readonly BoxCharSet Ascii = new("+", "+", "+", "+", "-", "|");
    public static readonly BoxCharSet Slash = new("╱", "╲", "╲", "╱", "╱", "╲");
    public static readonly BoxCharSet Triangle = new("◣", "◤", "◥", "◢", "═", "║");

    /// <summary>按名称获取预设</summary>
    public static BoxCharSet For(string name) => name switch
    {
        "single" => Single, "double" => Double, "rounded" => Rounded,
        "thick" => Thick, "solid" => Solid, "semisolid" => SemiSolid,
        "dotted" => Dotted, "dashed" => Dashed, "ascii" => Ascii,
        "slash" => Slash, "triangle" => Triangle,
        _ => Single,
    };

    /// <summary>从6字符自定义字符串解析</summary>
    public static BoxCharSet Custom(string sixChars)
    {
        if (string.IsNullOrEmpty(sixChars) || sixChars.Length < 6) return Single;
        var r = sixChars.EnumerateRunes().ToList();
        return new BoxCharSet(r[0].ToString(), r[1].ToString(), r[2].ToString(),
            r[3].ToString(), r[4].ToString(), r[5].ToString());
    }
}