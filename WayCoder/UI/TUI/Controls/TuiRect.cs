using System.Text;
using WayCoder.UI.Shared;
using WayCoder.UI.TUI.Base;

namespace WayCoder.UI.Tui.Controls;

/// <summary>矩形框控件 —— 只画外框（边框），内部空白。纯展示，无交互。</summary>
public class TuiRect : TuiControl
{
    /// <summary>线形（复用 WindowBorder）。</summary>
    public WindowBorder Style { get; set; } = WindowBorder.Single;

    public override bool CanFocus => false;

    protected override void OnRender(StringBuilder sb, int absX, int absY)
    {
        var bc = AnsiHelper.GetBorderChars(Style);
        int fg = EffectiveFg;
        int bg = EffectiveBg;
        int w = Math.Max(1, Width);
        int h = Math.Max(1, Height);
        char hc = bc.H[0];

        // 上边框
        string top = bc.TL + new string(hc, Math.Max(0, w - 2)) + (w > 1 ? bc.TR : "");
        ControlRenderer.DrawLine(sb, absY, absX, top, fg, bg);

        // 中间：左右竖边框 + 内部空白
        for (int y = 1; y < h - 1; y++)
        {
            string mid = bc.V + new string(' ', Math.Max(0, w - 2)) + (w > 1 ? bc.V : "");
            ControlRenderer.DrawLine(sb, absY + y, absX, mid, fg, bg);
        }

        // 下边框
        if (h > 1)
        {
            string bottom = bc.BL + new string(hc, Math.Max(0, w - 2)) + (w > 1 ? bc.BR : "");
            ControlRenderer.DrawLine(sb, absY + h - 1, absX, bottom, fg, bg);
        }
    }
}
