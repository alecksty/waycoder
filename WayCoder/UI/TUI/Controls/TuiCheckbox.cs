using System.Text;
using WayCoder.UI.Shared;
using WayCoder.UI.TUI.Base;

namespace WayCoder.UI.Tui.Controls;

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
        Width = AnsiHelper.DisplayWidth(label) + 4;
    }

    /// <summary>
    /// 渲染复选框
    /// </summary>
    /// <param name="sb">输出缓冲区</param>
    /// <param name="absX">绝对 X 坐标</param>
    /// <param name="absY">绝对 Y 坐标</param>
    protected override void OnRender(StringBuilder sb, int absX, int absY)
    {
        var t = TuiTheme.Current;
        var marker = Checked ? "☑" : "☐";
        ControlRenderer.DrawCheckLine(sb, this, absX, absY,
            marker, Label, TextAlign,
            t.ControlFg, 0, t.ControlFocusedFg, t.ControlFocusedBg);
    }


    /// <summary>
    /// 处理键盘输入
    /// </summary>
    /// <param name="key"></param>
    /// <returns></returns>
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
    public override bool OnMouse(InputEvent ev)
    {
        if (!IsEnabled) return false;
        if (ev is { Type: InputType.Mouse, MouseLeft: true })
        {
            Focused = true;
            Checked = !Checked;
            OnChanged?.Invoke(Checked);
            return true;
        }

        return false;
    }
}