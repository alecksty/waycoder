using System.Text;
using WayCoder.UI.TUI.Base;

namespace WayCoder.UI.Tui.Controls;

/// <summary>
/// 分割线 —— 水平或垂直分隔符。
/// 水平模式占 1 行全宽，垂直模式占 1 列全高。
/// </summary>
public class TuiSeparator : TuiControl
{
    /// <summary>分割线方向</summary>
    public SeparatorDirection Direction { get; set; } = SeparatorDirection.Horizontal;

    /// <summary>分割线是纯展示控件，不可获得焦点</summary>
    public override bool CanFocus => false;

    /// <summary>线条字符（默认 ─ ）</summary>
    public string LineChar { get; set; } = "─";

    /// <summary>线条颜色（0=使用前景色）</summary>
    public int LineColor { get; set; }

    /// <summary>居中文本（仅水平模式，空=无文字）</summary>
    public string Text { get; set; } = "";

    public TuiSeparator()
    {
        Height = 1;
        Width = 60;
    }

    public TuiSeparator(SeparatorDirection dir)
    {
        Direction = dir;
        Height = dir == SeparatorDirection.Horizontal ? 1 : 5;
        Width = dir == SeparatorDirection.Horizontal ? 60 : 1;
    }

    protected override void OnRender(StringBuilder sb, int absX, int absY)
    {
        if (Direction == SeparatorDirection.Vertical)
        {
            // 垂直分割线：逐行绘制（复杂控件自己处理）
            int fg = ResolveLineFg();
            for (int r = 0; r < Height; r++)
            {
                int row = absY + r;
                if (row < ClipTop || row >= ClipBottom) continue;
                ControlRenderer.DrawLine(sb, row, absX, "│", fg, Bg);
            }

            return;
        }

        // 水平分割线：委托给 ControlRenderer
        ControlRenderer.DrawSeparatorLine(sb, this, absX, absY,
            Text, LineChar[0],
            ResolveLineFg(), Bg);
    }

    private int ResolveLineFg()
        => LineColor > 0 ? LineColor : (Fg > 0 ? Fg : TuiTheme.Current.SeparatorFg);


    public override void OnResize(int newParentW, int newParentH)
    {
    }
}

/// <summary>分割线方向</summary>
public enum SeparatorDirection
{
    Horizontal,
    Vertical
}