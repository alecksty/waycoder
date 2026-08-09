using System.Text;
namespace CoreCoderSharp.UI.Controls;

/// <summary>进度条控件 —— 百分比条形显示。</summary>
public class TuiProgress : TuiControl
{
    public double Percent { get; set; }
    public string Label { get; set; } = "";

    public TuiProgress() { Height = 1; Width = 40; }

    protected override void OnRender(StringBuilder sb, int absX, int absY)
    {
        int barW = string.IsNullOrEmpty(Label) ? Width : Width - TuiHelper.DisplayWidth(Label) - 2;
        int filled = (int)Math.Round(barW * Percent / 100.0);
        filled = Math.Clamp(filled, 0, barW);
        var empty = barW - filled;

        var bar = new string('█', filled) + new string('░', empty);
        var display = string.IsNullOrEmpty(Label)
            ? bar
            : $" {Label} {bar}";

        WriteLine(sb, 0, 0, display, Fg > 0 ? Fg : 37, Bg);
    }
}
