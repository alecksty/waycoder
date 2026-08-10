using System.Text;

namespace WayCoder.UI.TuiControls;

/// <summary>横幅控件 —— 居中展示 ASCII 艺术标题。</summary>
public class TuiBanner : TuiControl
{
    public string Title { get; set; } = "";
    public string Subtitle { get; set; } = "";

    /// <summary>横幅是纯展示控件，不可获得焦点</summary>
    public override bool CanFocus => false;

    public TuiBanner() { Height = 3; Width = 60; TextAlign = HAlign.Center; }

    protected override void OnRender(StringBuilder sb, int absX, int absY)
    {
        if (string.IsNullOrEmpty(Title)) return;

        var t = TuiTheme.Current;
        ControlRenderer.DrawLabelLine(sb, this, absX, absY,
            Title, TextAlign, t.BannerFg, 0);

        if (!string.IsNullOrEmpty(Subtitle))
            ControlRenderer.DrawLabelLine(sb, this, absX, absY + 1,
                Subtitle, TextAlign, t.BannerSubFg, 0);
    }
}
