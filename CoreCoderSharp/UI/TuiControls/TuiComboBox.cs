using System.Text;
using CoreCoderSharp.Terminal;

namespace CoreCoderSharp.UI.Controls;

/// <summary>
/// 组合框 —— 点击展开下拉列表选择。
/// 收起时显示当前选中项 + ▼ 箭头，展开时弹出选项列表。
/// 键盘：Enter/↓ 展开，↑↓ 在列表内导航，Enter 确认，Esc 收起。
/// </summary>
public class TuiComboBox : TuiControl
{
    /// <summary>选项列表</summary>
    public List<string> Options { get; set; } = [];

    /// <summary>当前选中索引（-1 = 未选择）</summary>
    public int SelectedIndex { get; set; } = -1;

    /// <summary>下拉列表是否展开</summary>
    public bool IsExpanded { get; set; }

    /// <summary>未选择时的占位文本</summary>
    public string Placeholder { get; set; } = "请选择...";

    /// <summary>箭头颜色</summary>
    public int ArrowColor { get; set; } = 36;

    /// <summary>选择变化回调</summary>
    public Action<int>? OnSelectionChanged { get; set; }

    /// <summary>展开状态变化回调</summary>
    public Action<bool>? OnExpandedChanged { get; set; }

    public TuiComboBox()
    {
        Width = 24;
        Height = 1;
        Focused = true;
    }

    public TuiComboBox(List<string> options, int defaultIdx = -1)
    {
        Options = options;
        SelectedIndex = defaultIdx >= 0 && defaultIdx < options.Count ? defaultIdx : -1;
        Width = Math.Max(24, options.Count > 0 ? options.Max(o => TuiHelper.DisplayWidth(o)) + 6 : 24);
        Height = 1; // 收起时只占一行
        Focused = true;
    }

    public int ExpandedHeight => IsExpanded ? 1 + Math.Min(Options.Count, 10) : 1;

    protected override void OnRender(StringBuilder sb, int absX, int absY)
    {
        int fg = Focused ? 36 : 37;
        int bg = Bg > 0 ? Bg : (Focused ? 0 : 0);

        // ── 主行：选中项 + ▼ ──
        string display = SelectedIndex >= 0 && SelectedIndex < Options.Count
            ? Options[SelectedIndex]
            : Placeholder;
        int dfg = SelectedIndex >= 0 ? 37 : 90; // 占位文本灰色

        // 背景
        if (Focused)
        {
            var rbBg = new RenderBuffer();
            rbBg.Write(absY, absX, new string(' ', Width), bg: 7);
            sb.Append(rbBg.ToString());
        }

        // 文本 + 箭头
        int maxTextW = Width - 4; // 预留 " ▼" 位置
        var text = TuiHelper.DisplayWidth(display) > maxTextW
            ? TuiHelper.TruncateByWidth(display, maxTextW)
            : display;
        WriteAt(sb, absY, absX + 1, text, dfg, Focused ? 7 : Bg);

        string arrow = IsExpanded ? "▲" : "▼";
        WriteAt(sb, absY, absX + Width - 3, $" {arrow}", ArrowColor, Focused ? 7 : Bg);

        // ── 下拉列表 ──
        if (IsExpanded && Options.Count > 0)
        {
            int listH = Math.Min(Options.Count, 10);
            for (int i = 0; i < listH; i++)
            {
                int row = absY + 1 + i;
                if (row < ClipTop || row >= ClipBottom) continue;

                bool sel = i == SelectedIndex;
                int lFg = sel ? 30 : 37;
                int lBg = sel ? 46 : 7;

                var text2 = TuiHelper.DisplayWidth(Options[i]) > Width - 3
                    ? TuiHelper.TruncateByWidth(Options[i], Width - 3)
                    : Options[i];
                var pad = Width - 3 - TuiHelper.DisplayWidth(text2);

                var rb = new RenderBuffer();
                rb.Write(row, absX + 1, $" {text2}{new string(' ', Math.Max(0, pad))}", fg: lFg, bg: lBg);
                rb.Write(row, absX + Width - 1, " ", bg: lBg);
                sb.Append(rb.ToString());
            }
        }
    }

    public override bool HandleKey(ConsoleKeyInfo key)
    {
        if (Options.Count == 0) return false;

        if (IsExpanded)
        {
            switch (key.Key)
            {
                case ConsoleKey.UpArrow:
                    SelectedIndex = Math.Max(0, SelectedIndex - 1);
                    OnSelectionChanged?.Invoke(SelectedIndex);
                    return true;
                case ConsoleKey.DownArrow:
                    SelectedIndex = Math.Min(Options.Count - 1, SelectedIndex + 1);
                    OnSelectionChanged?.Invoke(SelectedIndex);
                    return true;
                case ConsoleKey.Home:
                    SelectedIndex = 0;
                    OnSelectionChanged?.Invoke(SelectedIndex);
                    return true;
                case ConsoleKey.End:
                    SelectedIndex = Options.Count - 1;
                    OnSelectionChanged?.Invoke(SelectedIndex);
                    return true;
                case ConsoleKey.Enter:
                    IsExpanded = false;
                    OnExpandedChanged?.Invoke(false);
                    if (SelectedIndex >= 0)
                        OnSelectionChanged?.Invoke(SelectedIndex);
                    return true;
                case ConsoleKey.Escape:
                    IsExpanded = false;
                    OnExpandedChanged?.Invoke(false);
                    return true;
            }
        }
        else
        {
            switch (key.Key)
            {
                case ConsoleKey.Enter:
                case ConsoleKey.Spacebar:
                    IsExpanded = true;
                    if (SelectedIndex < 0) SelectedIndex = 0;
                    OnExpandedChanged?.Invoke(true);
                    return true;
                case ConsoleKey.UpArrow:
                    SelectedIndex = Math.Max(0, SelectedIndex - 1);
                    OnSelectionChanged?.Invoke(SelectedIndex);
                    return true;
                case ConsoleKey.DownArrow:
                    SelectedIndex = Math.Min(Options.Count - 1, SelectedIndex + 1);
                    OnSelectionChanged?.Invoke(SelectedIndex);
                    return true;
            }
        }
        return false;
    }

    public override void OnResize(int newParentW, int newParentH) { }

    /// <summary>设置为指定索引</summary>
    public void Select(int index)
    {
        if (index >= 0 && index < Options.Count)
        {
            SelectedIndex = index;
            OnSelectionChanged?.Invoke(index);
        }
    }
}
