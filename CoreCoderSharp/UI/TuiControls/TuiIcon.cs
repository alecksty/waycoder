using System.Text;

namespace CoreCoderSharp.UI.TuiControls;

/// <summary>
/// 图标控件 —— 单字符图标，固定 2×1 大小。
/// 用于角色指示（👤🤖⚙）、状态标记等。
/// </summary>
public class TuiIcon : TuiControl
{
    /// <summary>图标字符</summary>
    public string Glyph { get; set; } = "•";

    /// <summary>图标是纯展示控件，不可获得焦点</summary>
    public override bool CanFocus => false;

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
        ControlRenderer.DrawLabelLine(sb, this, absX, absY,
            Glyph, HAlign.Left, TuiTheme.Current.ControlFg, 0);
    }

    // ── 预设图标 ──

    public static TuiIcon User() => new("●") { Fg = TuiTheme.Current.IconUserFg };
    public static TuiIcon Assistant() => new("●") { Fg = TuiTheme.Current.IconAssistantFg };
    public static TuiIcon System() => new("●") { Fg = TuiTheme.Current.IconSystemFg };
    public static TuiIcon Tool() => new("●") { Fg = TuiTheme.Current.IconToolFg };
    public static TuiIcon Error() => new("●") { Fg = TuiTheme.Current.IconErrorFg };
    public static TuiIcon Warn() => new("●") { Fg = TuiTheme.Current.IconWarnFg };
    public static TuiIcon Ok() => new("●") { Fg = TuiTheme.Current.IconOkFg };
    public static TuiIcon Info() => new("●") { Fg = TuiTheme.Current.IconInfoFg };
    public static TuiIcon File() => new("●") { Fg = TuiTheme.Current.IconFileFg };
    public static TuiIcon Folder() => new("●") { Fg = TuiTheme.Current.IconFolderFg };
    public static TuiIcon Lock() => new("●") { Fg = TuiTheme.Current.IconLockFg };
}
