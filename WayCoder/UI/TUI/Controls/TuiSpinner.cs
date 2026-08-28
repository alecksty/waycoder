using WayCoder.UI.Shared;
using WayCoder.UI.TUI.Base;

namespace WayCoder.UI.Tui.Controls;

/// <summary>
/// 加载动画控件 —— 旋转指示器。
/// 帧字符循环：⣾⣽⣻⢿⡿⣟⣯⣷
/// 每帧调用 Tick() 推进动画。
/// </summary>
public class TuiSpinner : TuiDisplayControl
{
    /// <summary>
    /// 加载动画帧字符序列。
    /// </summary>
    private static readonly string[] Frames = ["⣾", "⣽", "⣻", "⢿", "⡿", "⣟", "⣯", "⣷"];

    /// <summary>标签文本（显示在动画左侧）</summary>
    public string Label { get; set; } = "";

    private int _frame;

    /// <summary>加载动画是纯展示控件，不可获得焦点</summary>

    public TuiSpinner()
    {
        Width = 20;
        Height = 1;
    }

    public TuiSpinner(string label)
    {
        Label = label;
        Width = AnsiHelper.DisplayWidth(label) + 4;
        Height = 1;
    }

    /// <summary>推进一帧</summary>
    public void Tick() => _frame = (_frame + 1) % Frames.Length;

    /// <summary>当前帧字符</summary>
    public string Frame => Frames[_frame];

    protected override void OnRender(System.Text.StringBuilder sb, int absX, int absY)
    {
        var display = string.IsNullOrEmpty(Label)
            ? $" {Frame} "
            : $"{Frame} {Label}";
        ControlRenderer.DrawLabelLine(sb, this, absX, absY,
            display, EHAlign.Left, TuiTheme.Current.SpinnerFg, 0);
    }
}