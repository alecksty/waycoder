using System.Text;

namespace CoreCoderSharp.UI.Controls;

/// <summary>
/// 分割线 —— 水平或垂直分隔符。
/// 水平模式占 1 行全宽，垂直模式占 1 列全高。
/// </summary>
public class TuiSeparator : TuiControl
{
    /// <summary>分割线方向</summary>
    public SeparatorDirection Direction { get; set; } = SeparatorDirection.Horizontal;

    /// <summary>线条字符（默认 ─ ）</summary>
    public string LineChar { get; set; } = "─";

    /// <summary>线条颜色（0=使用前景色）</summary>
    public int LineColor { get; set; }

    /// <summary>居中文本（仅水平模式，空=无文字）</summary>
    public string Text { get; set; } = "";

    public TuiSeparator()
    {
        Height = 1; Width = 60;
    }

    public TuiSeparator(SeparatorDirection dir)
    {
        Direction = dir;
        Height = dir == SeparatorDirection.Horizontal ? 1 : 5;
        Width = dir == SeparatorDirection.Horizontal ? 60 : 1;
    }

    protected override void OnRender(StringBuilder sb, int absX, int absY)
    {
        int fg = LineColor > 0 ? LineColor : (Fg > 0 ? Fg : 90); // 默认灰色

        if (Direction == SeparatorDirection.Vertical)
        {
            // 垂直：逐行绘制竖线
            for (int r = 0; r < Height; r++)
            {
                int row = absY + r;
                if (row < ClipTop || row >= ClipBottom) continue;
                WriteAt(sb, row, absX, "│", fg, Bg);
            }
            return;
        }

        // 水平：单行横线（可选居中文本）
        if (!string.IsNullOrEmpty(Text))
        {
            var leftW = (Width - TuiHelper.DisplayWidth(Text) - 2) / 2;
            var rightW = Width - TuiHelper.DisplayWidth(Text) - 2 - leftW;
            WriteAt(sb, absY, absX, new string(LineChar[0], Math.Max(0, leftW)), fg, Bg);
            WriteAt(sb, absY, absX + leftW, $" {Text} ", Fg > 0 ? Fg : 37, Bg);
            WriteAt(sb, absY, absX + leftW + TuiHelper.DisplayWidth(Text) + 2, new string(LineChar[0], Math.Max(0, rightW)), fg, Bg);
        }
        else
        {
            WriteAt(sb, absY, absX, new string(LineChar[0], Width), fg, Bg);
        }
    }

    public override bool HandleKey(ConsoleKeyInfo key) => false;
    public override void OnResize(int newParentW, int newParentH) { }
}

/// <summary>分割线方向</summary>
public enum SeparatorDirection { Horizontal, Vertical }
