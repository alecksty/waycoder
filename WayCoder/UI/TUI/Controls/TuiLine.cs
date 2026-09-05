using System.Text;
using WayCoder.UI.Shared;
using WayCoder.UI.TUI.Base;

namespace WayCoder.UI.Tui.Controls;

/// <summary>线控件 —— 画横线或竖线，指定线形（复用 WindowBorder 的横/竖线字符）。纯展示，无交互。
/// [仅演示/标记句法演示用，发货 UI 未实例化——重构需谨慎勿当死码删]
/// 注意：chat.tui SidePanel 里 &lt;Section&gt; 内的 &lt;Line&gt; 由 TuiMarkup 手解析成 PanelSection 文本，不产生本控件。</summary>
public class TuiLine : TuiBorderedControl
{
    /// <summary>true=竖线（占 Height 行），false=横线（占 Width 列）。</summary>
    public bool Vertical { get; set; }

    public TuiLine()
    {
        Width = 1;
        Height = 1;
        BorderStyle = WindowBorder.Single;
    } // 线默认 1 列（横线指定 width，竖线指定 height）

    protected override void OnRender(StringBuilder sb, int absX, int absY)
    {
        var bc = GetBorderChars();
        var fg = EffectiveFg;
        var bg = EffectiveBg;

        if (Vertical)
        {
            for (var y = 0; y < Height; y++)
            {
                ControlRenderer.DrawLine(sb, absY + y, absX, bc.V, fg, bg);
            }
        }
        else
        {
            var w = Math.Max(1, Width);
            ControlRenderer.DrawLine(sb, absY, absX, new string(bc.H[0], w), fg, bg);
        }
    }
}