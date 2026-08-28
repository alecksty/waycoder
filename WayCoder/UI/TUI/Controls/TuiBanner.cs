using System.Text;
using WayCoder.UI.TUI.Base;

namespace WayCoder.UI.Tui.Controls;

/// <summary>横幅控件 —— 居中展示 ASCII 艺术标题。</summary>
public class TuiBanner : TuiDisplayControl
{
    /// <summary>
    /// 横幅标题
    /// </summary>
    public string Title { get; set; } = "";

    /// <summary>
    /// 横幅副标题
    /// </summary>
    public string Subtitle { get; set; } = "";

    /// <summary>横幅是纯展示控件，不可获得焦点</summary>

    public TuiBanner()
    {
        Height = 3;
        Width = 60;
        TextAlign = EHAlign.Center;
    }

    /// <summary>
    /// 渲染横幅标题和副标题
    /// </summary>
    /// <param name="sb">渲染缓冲区</param>
    /// <param name="absX">绝对 X 坐标</param>
    /// <param name="absY">绝对 Y 坐标</param>
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