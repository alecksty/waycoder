using System.Text;
using CoreCoderSharp.Terminal;

namespace CoreCoderSharp.UI.Controls;

/// <summary>
/// 滑块/Seek Bar —— 范围值选择控件。
/// 渲染为 `━━●━━━━` 风格，支持拖动和键盘微调。
/// 键盘：←→ 微调，Home/End 跳到边界，PgUp/PgDn 大步跳。
/// </summary>
public class TuiSeekBar : TuiControl
{
    /// <summary>最小值</summary>
    public int MinValue { get; set; }

    /// <summary>最大值</summary>
    public int MaxValue { get; set; } = 100;

    /// <summary>当前值</summary>
    public int Value
    {
        get => _value;
        set
        {
            var clamped = Math.Clamp(value, MinValue, MaxValue);
            if (clamped != _value)
            {
                _value = clamped;
                OnValueChanged?.Invoke(_value);
            }
        }
    }
    private int _value;

    /// <summary>步长（键盘微调的量）</summary>
    public int Step { get; set; } = 1;

    /// <summary>大步长（PgUp/PgDn 跳转量）</summary>
    public int LargeStep { get; set; } = 10;

    /// <summary>滑块字符</summary>
    public string ThumbChar { get; set; } = "●";

    /// <summary>轨道字符（已填充部分）</summary>
    public string TrackFilled { get; set; } = "━";

    /// <summary>轨道字符（未填充部分）</summary>
    public string TrackEmpty { get; set; } = "─";

    /// <summary>轨道前景色</summary>
    public int TrackFg { get; set; }

    /// <summary>空轨道前景色</summary>
    public int EmptyFg { get; set; }

    /// <summary>滑块前景色</summary>
    public int ThumbFg { get; set; }

    /// <summary>是否显示数字标签</summary>
    public bool ShowLabel { get; set; } = true;

    /// <summary>值变化回调</summary>
    public Action<int>? OnValueChanged { get; set; }

    public TuiSeekBar()
    {
        Width = 30;
        Height = 1;
        Focused = true;
        TrackFg = TuiTheme.Current.SeekBarFilledFg;
        EmptyFg = TuiTheme.Current.SeekBarEmptyFg;
        ThumbFg = TuiTheme.Current.SeekBarThumbFg;
    }

    public TuiSeekBar(int min, int max, int initial, int step = 1)
    {
        MinValue = min;
        MaxValue = max;
        _value = Math.Clamp(initial, min, max);
        Step = step;
        Width = 30;
        Height = 1;
        Focused = true;
    }

    protected override void OnRender(StringBuilder sb, int absX, int absY)
    {
        int trackW = Width;
        if (ShowLabel) trackW -= 8; // 为 " 100/100" 预留

        if (trackW < 4) trackW = 4;

        double ratio = MaxValue > MinValue
            ? (double)(Value - MinValue) / (MaxValue - MinValue)
            : 0;
        int thumbPos = (int)(ratio * (trackW - 1));

        bool isFocused = Focused && IsEnabled;
        int fg = !IsEnabled ? (DisabledFg > 0 ? DisabledFg : TuiTheme.Current.ControlDisabledFg)
               : isFocused ? TrackFg : EmptyFg;
        int bg = !IsEnabled ? (DisabledBg > 0 ? DisabledBg : Bg)
               : isFocused && Bg == 0 ? TuiTheme.Current.WindowBg : Bg;

        // 背景填充
        if (bg > 0 && Focused)
        {
            var rbBg = new RenderBuffer();
            rbBg.Write(absY, absX, new string(' ', Width), bg: bg);
            sb.Append(rbBg.ToString());
        }

        for (int i = 0; i < trackW; i++)
        {
            string ch;
            int cf;
            if (i == thumbPos)
            {
                ch = ThumbChar;
                cf = ThumbFg;
            }
            else if (i < thumbPos)
            {
                ch = TrackFilled;
                cf = fg;
            }
            else
            {
                ch = TrackEmpty;
                cf = EmptyFg;
            }
            WriteAt(sb, absY, absX + i, ch, cf, bg);
        }

        // 数字标签
        if (ShowLabel)
        {
            string label = $" {Value}/{MaxValue}";
            WriteAt(sb, absY, absX + trackW, label, TuiTheme.Current.ControlFg, bg);
        }
    }

    public override bool OnKey(ConsoleKeyInfo key)
    {
        if (!IsEnabled) return false;
        switch (key.Key)
        {
            case ConsoleKey.RightArrow:
                Value += Step;
                return true;
            case ConsoleKey.LeftArrow:
                Value -= Step;
                return true;
            case ConsoleKey.Home:
                Value = MinValue;
                return true;
            case ConsoleKey.End:
                Value = MaxValue;
                return true;
            case ConsoleKey.PageUp:
                Value += LargeStep;
                return true;
            case ConsoleKey.PageDown:
                Value -= LargeStep;
                return true;
        }
        return false;
    }

    /// <summary>鼠标点击 / 拖拽跳转到对应位置</summary>
    public override bool HandleMouse(InputEvent ev)
    {
        if (ev.Type != InputType.Mouse || !ev.MouseLeft) return false;
        if (!IsEnabled || !Visible) return false;

        int trackW = Width;
        if (ShowLabel) trackW -= 8;
        if (trackW < 4) trackW = 4;

        // 计算鼠标在轨道内的位置
        int relX = ev.MouseX - GetAbsoluteX();
        int thumbPos = Math.Clamp(relX, 0, trackW - 1);

        // 映射为值
        double ratio = (double)thumbPos / (trackW - 1);
        Value = MinValue + (int)Math.Round(ratio * (MaxValue - MinValue));

        Focused = true;
        return true;
    }

    public override void OnResize(int newParentW, int newParentH) { }
}
