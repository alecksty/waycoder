using System.Text;
namespace CoreCoderSharp.UI.Controls;

/// <summary>静态文本标签 —— 单行文本，可设前景色。</summary>
public class TuiLabel : TuiControl
{
    public string Text { get; set; } = "";

    public TuiLabel() { Height = 1; }
    public TuiLabel(string text) { Text = text; Height = 1; }

    protected override void OnRender(StringBuilder sb, int absX, int absY)
    {
        if (string.IsNullOrEmpty(Text)) return;
        var clipped = Truncate(Text, Width);
        WriteLine(sb, 0, 0, clipped, Fg);
    }

    private static string Truncate(string s, int maxVw)
    {
        if (TuiHelper.DisplayWidth(s) <= maxVw) return s;
        return TuiHelper.TruncateByWidth(s, maxVw);
    }
}
