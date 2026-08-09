using System.Text;
namespace CoreCoderSharp.UI.Controls;

/// <summary>
/// 按钮控件 —— 可点击，有焦点高亮。
/// 响应：Enter/Spacebar（触发点击），方向键（在同级窗口内切换焦点），Tab（下一焦点）。
/// </summary>
public class TuiButton : TuiControl
{
    public string Text { get; set; } = "OK";
    public Action<TuiButton>? OnClick { get; set; }

    public TuiButton() { Height = 1; Width = 10; }
    public TuiButton(string text, Action<TuiButton>? onClick = null)
    {
        Text = text; OnClick = onClick; Height = 1;
        Width = TuiHelper.DisplayWidth(text) + 4;
    }

    protected override void OnRender(StringBuilder sb, int absX, int absY)
    {
        var label = $" {Text} ";
        if (TuiHelper.DisplayWidth(label) > Width)
            label = TuiHelper.TruncateByWidth(label, Width);
        var pad = Math.Max(0, Width - TuiHelper.DisplayWidth(label));
        var display = label + new string(' ', pad);

        int fg = Focused ? 30 : (Fg > 0 ? Fg : 37);
        int bg = Focused ? 46 : (Bg > 0 ? Bg : 44);

        var rb = new Terminal.RenderBuffer();
        rb.Write(absY, absX, display, fg: fg, bg: bg);
        sb.Append(rb.ToString());
    }

    public override bool HandleKey(ConsoleKeyInfo key)
    {
        if (!Focused) return false;

        switch (key.Key)
        {
            case ConsoleKey.Enter:
            case ConsoleKey.Spacebar:
                OnClick?.Invoke(this);
                return true;

            case ConsoleKey.LeftArrow:
            case ConsoleKey.UpArrow:
                FindRootView()?.FocusPrev();
                return true;

            case ConsoleKey.RightArrow:
            case ConsoleKey.DownArrow:
                FindRootView()?.FocusNext();
                return true;
        }
        return false;
    }

    /// <summary>沿 Parent 链找到顶层根视图（窗口的 RootView）</summary>
    private TuiView? FindRootView()
    {
        TuiView? p = Parent;
        while (p?.Parent != null) p = p.Parent;
        return p ?? Parent;
    }
}
