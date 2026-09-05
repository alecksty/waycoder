using System.Text;
using WayCoder.UI.Shared;
using WayCoder.UI.TUI.Base;

namespace WayCoder.UI.Tui.Controls;

/// <summary>进度条控件 —— 百分比条形显示。
/// [仅演示/标记句法演示用，发货 UI 未实例化——重构需谨慎勿当死码删]
/// 只被 TuiMarkup 工厂（&lt;Progress&gt; 标签，showcase.tui 演示）与 Test 使用；聊天压缩进度条是 ChatScreen 手绘，与本控件无关。</summary>
public class TuiProgress : TuiDisplayControl
{
    public double Percent { get; set; }
    public string Label { get; set; } = "";

    /// <summary>进度条是纯展示控件，不可获得焦点</summary>

    public TuiProgress() { Height = 1; Width = 40; }

    protected override void OnRender(StringBuilder sb, int absX, int absY)
    {
        int barW = string.IsNullOrEmpty(Label) ? Width : Width - AnsiHelper.DisplayWidth(Label) - 2;
        if (barW < 0) barW = 0; // Label 过宽时 barW 为负，Math.Clamp(min>max) 会抛 ArgumentException
        int filled = (int)Math.Round(barW * Percent / 100.0);
        filled = Math.Clamp(filled, 0, barW);

        ControlRenderer.DrawBarLine(sb, this, absX, absY,
            barW, filled, string.IsNullOrEmpty(Label) ? "" : $" {Label}",
            '█', '░', TuiTheme.Current.ControlFg, 0);
    }
}
