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

    /// <summary>是否启用渐变背景</summary>
    public bool GradientBg { get; set; }
    /// <summary>渐变背景起始色（TrueColor 码）</summary>
    public int GradientBgStart { get; set; }
    /// <summary>渐变背景终止色（TrueColor 码）</summary>
    public int GradientBgEnd { get; set; }

    public TuiButton() { Height = 1; Width = 10; TextAlign = HAlign.Center; }
    public TuiButton(string text, Action<TuiButton>? onClick = null)
    {
        Text = text; OnClick = onClick; Height = 1;
        Width = TuiHelper.DisplayWidth(text) + 4;
        TextAlign = HAlign.Center;
    }

    protected override void OnRender(StringBuilder sb, int absX, int absY)
    {
        var t = TuiTheme.Current;
        if (GradientBg && GradientBgStart >= 0x1000000 && GradientBgEnd >= 0x1000000)
        {
            ControlRenderer.DrawButtonGradientLine(sb, this, absX, absY,
                ControlRenderer.PadText(Text), TextAlign,
                t.ButtonFg, t.ControlFocusedFg, t.ControlDisabledFg,
                GradientBgStart, GradientBgEnd);
        }
        else
        {
            ControlRenderer.DrawButtonLine(sb, this, absX, absY,
                ControlRenderer.PadText(Text), TextAlign,
                t.ButtonFg, t.ButtonBg, t.ControlFocusedFg, t.ControlFocusedBg);
        }
    }

    public override bool OnKey(ConsoleKeyInfo key)
    {
        if (!IsEnabled || !Focused) return false;

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

    /// <summary>鼠标左键点击触发按钮</summary>
    public override bool HandleMouse(InputEvent ev)
    {
        if (!IsEnabled) return false;
        if (ev.Type == InputType.Mouse && ev.MouseLeft)
        {
            Focused = true;
            OnClick?.Invoke(this);
            return true;
        }
        return false;
    }
}
