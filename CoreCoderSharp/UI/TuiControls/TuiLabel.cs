using System.Text;
namespace CoreCoderSharp.UI.Controls;

/// <summary>静态文本标签 —— 单行文本，可设前景色。</summary>
public class TuiLabel : TuiControl
{
    public string Text { get; set; } = "";

    /// <summary>标签是纯展示控件，不可获得焦点</summary>
    public override bool CanFocus => false;

    public TuiLabel() { Height = 1; }
    public TuiLabel(string text) { Text = text; Height = 1; }

    protected override void OnRender(StringBuilder sb, int absX, int absY)
    {
        if (string.IsNullOrEmpty(Text)) return;
        ControlRenderer.DrawLabelLine(sb, this, absX, absY,
            Text, TextAlign, TuiTheme.Current.ControlFg, 0);
    }
}
