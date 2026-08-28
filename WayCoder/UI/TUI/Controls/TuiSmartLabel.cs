using System.Text;
using WayCoder.UI.Shared;
using WayCoder.UI.Shared.Terminal;
using WayCoder.UI.TUI.Base;

namespace WayCoder.UI.Tui.Controls;

/// <summary>
/// 智能文本标签 —— 支持中间格式 «tag»…«/» 标记的按段着色渲染。
///
/// 与 TuiLabel 的分工：TuiLabel 是纯文本单色；本控件把文本里内嵌的 «color» / «bold» / «dim»
/// 等标记解析成各段独立前景/背景色（颜色真源 MarkdownParser.ParseMarkupOnly），
/// 供「标签暗、值亮/彩」的状态行用（如模型/模式信息行）。普通纯文本标签用 TuiLabel。
/// </summary>
public class TuiSmartLabel : TuiDisplayControl
{
    /// <summary>标记文本。改了自动标脏。</summary>
    public string Text
    {
        get => _text;
        set => SetDirty(ref _text, value);
    }
    private string _text = "";

    /// <summary>纯展示控件，不可获得焦点</summary>

    public TuiSmartLabel() { Height = 1; }
    public TuiSmartLabel(string text) { Text = text; Height = 1; }

    protected override void OnRender(StringBuilder sb, int absX, int absY)
    {
        if (string.IsNullOrEmpty(Text)) return;
        int defaultFg = Fg > 0 ? Fg : TuiTheme.Current.ControlFg;
        var segments = MarkdownParser.ParseMarkupOnly(Text, defaultFg, 0);

        // 总宽 = 各段显示宽之和（不含标记），用于左/中/右对齐
        int totalW = 0;
        foreach (var (t, _, _) in segments) totalW += AnsiString.DisplayWidth(t);
        int startX = absX + TextAlign switch
        {
            EHAlign.Center => Math.Max(0, (Width - totalW) / 2),
            EHAlign.Right => Math.Max(0, Width - totalW),
            _ => 0,
        };

        var rb = new RenderBuffer();
        // 先清整行：切换后文本变短时，旧字符会残留在最右侧（只覆盖新文本不清理旧内容），
        // 用背景色空格填满 Width 覆盖掉残留。
        rb.Fill(absY, absX, Width, Bg > 0 ? Bg : 0);
        int col = startX;
        foreach (var (t, color, bg) in segments)
        {
            rb.Write(absY, col, t, fg: color > 0 ? color : defaultFg, bg: bg);
            col += AnsiString.DisplayWidth(t);
        }
        sb.Append(rb.ToString());
    }
}
