using System.Text;
namespace CoreCoderSharp.UI.Controls;

/// <summary>横幅控件 —— 居中展示 ASCII 艺术标题。</summary>
public class TuiBanner : TuiControl
{
    public string Title { get; set; } = "";
    public string Subtitle { get; set; } = "";

    public TuiBanner() { Height = 3; Width = 60; }

    protected override void OnRender(StringBuilder sb, int absX, int absY)
    {
        if (string.IsNullOrEmpty(Title)) return;

        var rb = new Terminal.RenderBuffer();
        int fg = Fg > 0 ? Fg : 36;

        // 标题居中
        var titleVw = TuiHelper.DisplayWidth(Title);
        var titleX = absX + Math.Max(0, (Width - titleVw) / 2);
        rb.Write(absY, titleX, Title, fg: fg);

        // 副标题
        if (!string.IsNullOrEmpty(Subtitle))
        {
            var subVw = TuiHelper.DisplayWidth(Subtitle);
            var subX = absX + Math.Max(0, (Width - subVw) / 2);
            rb.Write(absY + 1, subX, Subtitle, fg: 90);
        }

        sb.Append(rb.ToString());
    }
}
