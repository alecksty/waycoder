using System.Text;
using WayCoder.UI.TUI.Base;

namespace WayCoder.UI.Tui.Controls;

/// <summary>静态文本标签 —— 单行文本，可设前景色。</summary>
public class TuiLabel : TuiDisplayControl
{
    /// <summary>标签文字。改了自动标脏 —— code-behind 常拿它当状态回显（「扫描中…」），不标脏就看不见变化。</summary>
    public string Text
    {
        get => _text;
        set => SetDirty(ref _text, value);
    }
    private string _text = "";

    /// <summary>标签是纯展示控件，不可获得焦点</summary>

    public TuiLabel() { Height = 1; }
    public TuiLabel(string text) { Text = text; Height = 1; }

    protected override void OnRender(StringBuilder sb, int absX, int absY)
    {
        if (string.IsNullOrEmpty(Text)) return;
        ControlRenderer.DrawLabelLine(sb, this, absX, absY,
            Text, TextAlign, TuiTheme.Current.ControlFg, 0);
    }
}
