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
        var marker = Checked ? "☑" : "☐";
        var display = $"{marker} {Label}";
        if (TuiHelper.DisplayWidth(display) > Width)
            display = TuiHelper.TruncateByWidth(display, Width);

        int fg = Focused ? 30 : (Fg > 0 ? Fg : 37);
        int bg = Focused ? 46 : (Bg > 0 ? Bg : 0);

        // Pad to fill
        var pad = Math.Max(0, Width - TuiHelper.DisplayWidth(display));
        WriteLine(sb, 0, 0, display + new string(' ', pad), fg, bg);
    }

    public override bool HandleKey(ConsoleKeyInfo key)
    {
        if (key.Key is ConsoleKey.Enter or ConsoleKey.Spacebar)
        {
            Checked = !Checked;
            OnChanged?.Invoke(Checked);
            return true;
        }
        return false;
    }
}
