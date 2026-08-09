using System.Text;

namespace CoreCoderSharp.UI.Controls;

/// <summary>
/// 复选框控件 —— ☑/☐ 切换。
/// Space/Enter 切换状态。
/// </summary>
public class TuiCheckbox : TuiControl
{
    /// <summary>标签文本</summary>
    public string Label { get; set; } = "";

    /// <summary>是否选中</summary>
    public bool Checked { get; set; }

    /// <summary>状态变化回调</summary>
    public Action<bool>? OnChanged { get; set; }

    public TuiCheckbox()
    {
        Height = 1;
        Width = 20;
    }

    public TuiCheckbox(string label, bool @checked = false)
    {
        Label = label;
        Checked = @checked;
        Height = 1;
        Width = TuiHelper.DisplayWidth(label) + 4;
    }

    protected override void OnRender(StringBuilder sb, int absX, int absY)
    {
        var t = TuiTheme.Current;
        var marker = Checked ? "☑" : "☐";
        ControlRenderer.DrawCheckLine(sb, this, absX, absY,
            marker, Label, TextAlign,
            t.ControlFg, 0, t.ControlFocusedFg, t.ControlFocusedBg);
    }

    public override bool OnKey(ConsoleKeyInfo key)
    {
        if (!IsEnabled) return false;
        if (key.Key is ConsoleKey.Enter or ConsoleKey.Spacebar)
        {
            Checked = !Checked;
            OnChanged?.Invoke(Checked);
            return true;
        }
        return false;
    }

    /// <summary>鼠标左键点击切换复选框状态</summary>
    public override bool HandleMouse(InputEvent ev)
    {
        if (!IsEnabled) return false;
        if (ev.Type == InputType.Mouse && ev.MouseLeft)
        {
            Focused = true;
            Checked = !Checked;
            OnChanged?.Invoke(Checked);
            return true;
        }
        return false;
    }
}
