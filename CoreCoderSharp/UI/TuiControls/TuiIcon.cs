using System.Text;

namespace CoreCoderSharp.UI.Controls;

/// <summary>
/// 图标控件 —— 单字符图标，固定 2×1 大小。
/// 用于角色指示（👤🤖⚙）、状态标记等。
/// </summary>
public class TuiIcon : TuiControl
{
    /// <summary>图标字符</summary>
    public string Glyph { get; set; } = "•";

    public TuiIcon()
    {
        Width = 2;
        Height = 1;
    }

    public TuiIcon(string glyph)
    {
        Glyph = glyph;
        Width = 2;
        Height = 1;
    }

    protected override void OnRender(StringBuilder sb, int absX, int absY)
    {
        WriteLine(sb, 0, 0, Glyph, Fg > 0 ? Fg : 37);
    }

    // ── 预设图标 ──

    public static TuiIcon User() => new("👤") { Fg = 32 };     // Green
    public static TuiIcon Assistant() => new("🤖") { Fg = 36 }; // Cyan
    public static TuiIcon System() => new("⚙️") { Fg = 33 };   // Yellow
    public static TuiIcon Tool() => new("🔧") { Fg = 90 };     // Gray
    public static TuiIcon Error() => new("❌") { Fg = 31 };     // Red
    public static TuiIcon Warn() => new("⚠️") { Fg = 33 };     // Yellow
    public static TuiIcon Ok() => new("✅") { Fg = 32 };       // Green
    public static TuiIcon Info() => new("ℹ️") { Fg = 36 };     // Cyan
    public static TuiIcon File() => new("📄") { Fg = 37 };     // White
    public static TuiIcon Folder() => new("📁") { Fg = 33 };   // Yellow
    public static TuiIcon Lock() => new("🔒") { Fg = 31 };     // Red
}
