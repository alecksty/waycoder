using System.Text;
using WayCoder.UI.Shared;

namespace WayCoder.UI.Tui.Controls;

/// <summary>
/// 单选按钮组 —— 互斥选项列表。
/// 每个选项渲染为 ◉/○ 符号 + 标签文本。
/// 键盘：↑↓ 切换选择，Enter/Space 确认。
/// </summary>
public class TuiRadioGroup : TuiControl
{
    /// <summary>选项列表</summary>
    public List<string> Options { get; set; } = [];

    /// <summary>当前选中索引（-1 = 无选中）</summary>
    public int SelectedIndex { get; set; } = -1;

    /// <summary>选中符号前景色</summary>
    public int SelFg { get; set; }

    /// <summary>选中项背景色（反白白底）</summary>
    public int SelBg { get; set; }

    /// <summary>常规前景色</summary>
    public int ItemFg { get; set; }

    /// <summary>选择变化回调</summary>
    public Action<int>? OnSelectionChanged { get; set; }

    public TuiRadioGroup()
    {
        Width = 30;
        Height = 1;
        Focused = true;
        SelFg = TuiTheme.Current.ControlFocusedFg;   // 选中项黑字（反白）
        SelBg = TuiTheme.Current.ControlFocusedBg;   // 选中项白底（反白）
        ItemFg = TuiTheme.Current.ControlFg;
    }

    public TuiRadioGroup(List<string> options, int defaultIdx = 0)
    {
        Options = options;
        SelectedIndex = defaultIdx >= 0 && defaultIdx < options.Count ? defaultIdx : -1;
        Width = options.Count > 0 ? options.Max(o => AnsiHelper.DisplayWidth(o)) + 4 : 30;
        Height = Math.Max(1, options.Count);
        Focused = true;
        SelFg = TuiTheme.Current.ControlFocusedFg;   // 选中项黑字（反白）
        SelBg = TuiTheme.Current.ControlFocusedBg;   // 选中项白底（反白）
        ItemFg = TuiTheme.Current.ControlFg;
    }

    protected override void OnRender(StringBuilder sb, int absX, int absY)
    {
        for (int i = 0; i < Options.Count; i++)
        {
            int row = absY + i;
            if (row < ClipTop || row >= ClipBottom) continue;

            bool sel = i == SelectedIndex;
            string bullet = sel ? "◉" : "○";
            int fg = !IsEnabled ? (DisabledFg > 0 ? DisabledFg : TuiTheme.Current.ControlDisabledFg)
                : sel ? SelFg : ItemFg;
            int bg = sel ? SelBg : 0;

            // 选中项整行反白（白底 + 黑字），对齐输入框光标行 / 列表选中行约定
            if (sel && SelBg > 0)
                FillLine(sb, i, ' ', bg: SelBg);

            WriteAt(sb, row, absX, $"{bullet} {Options[i]}", fg, bg);
        }

        if (Options.Count > 0 && Height < Options.Count)
            Height = Options.Count;
    }

    public override bool OnKey(ConsoleKeyInfo key)
    {
        if (!IsEnabled || Options.Count == 0) return false;

        switch (key.Key)
        {
            case ConsoleKey.UpArrow:
                SetIndex(Math.Max(0, SelectedIndex - 1));
                return true;
            case ConsoleKey.DownArrow:
                SetIndex(Math.Min(Options.Count - 1, SelectedIndex + 1));
                return true;
            case ConsoleKey.Home:
                SetIndex(0);
                return true;
            case ConsoleKey.End:
                SetIndex(Options.Count - 1);
                return true;
            case ConsoleKey.Enter:
            case ConsoleKey.Spacebar:
                if (SelectedIndex >= 0)
                    OnSelectionChanged?.Invoke(SelectedIndex);
                return true;
        }

        return false;
    }

    private void SetIndex(int idx)
    {
        if (idx == SelectedIndex) return;
        SelectedIndex = idx;
        OnSelectionChanged?.Invoke(idx);
    }

    public override void OnResize(int newParentW, int newParentH)
    {
    }
}