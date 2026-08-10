using System.Text;

namespace WayCoder.UI.TuiControls;

/// <summary>进度条控件 —— 百分比条形显示。</summary>
public class TuiProgress : TuiControl
{
    public double Percent { get; set; }
    public string Label { get; set; } = "";

    /// <summary>进度条是纯展示控件，不可获得焦点</summary>
    public override bool CanFocus => false;

    public TuiProgress() { Height = 1; Width = 40; }

    protected override void OnRender(StringBuilder sb, int absX, int absY)
    {
        int barW = string.IsNullOrEmpty(Label) ? Width : Width - TuiHelper.DisplayWidth(Label) - 2;
        int filled = (int)Math.Round(barW * Percent / 100.0);
        filled = Math.Clamp(filled, 0, barW);

        ControlRenderer.DrawBarLine(sb, this, absX, absY,
            barW, filled, string.IsNullOrEmpty(Label) ? "" : $" {Label}",
            '█', '░', TuiTheme.Current.ControlFg, 0);
    }
}
