using System.Text;
using WayCoder.UI.Shared;
using WayCoder.UI.TUI.Base;

namespace WayCoder.UI.Tui.Controls;

/// <summary>线控件 —— 画横线或竖线，指定线形（复用 WindowBorder 的横/竖线字符）。纯展示，无交互。</summary>
public class TuiLine : TuiControl
{
    /// <summary>true=竖线（占 Height 行），false=横线（占 Width 列）。</summary>
    public bool Vertical { get; set; }

    /// <summary>线形（复用 WindowBorder 线字符）。</summary>
    public WindowBorder Style { get; set; } = WindowBorder.Single;

    public override bool CanFocus => false;

    public TuiLine() { Width = 1; Height = 1; } // 线默认 1 列（横线指定 width，竖线指定 height）

    protected override void OnRender(StringBuilder sb, int absX, int absY)
    {
        var bc = AnsiHelper.GetBorderChars(Style);
        int fg = EffectiveFg;
        int bg = EffectiveBg;

        if (Vertical)
        {
            for (int y = 0; y < Height; y++)
                ControlRenderer.DrawLine(sb, absY + y, absX, bc.V, fg, bg);
        }
        else
        {
            int w = Math.Max(1, Width);
            ControlRenderer.DrawLine(sb, absY, absX, new string(bc.H[0], w), fg, bg);
        }
    }
}
