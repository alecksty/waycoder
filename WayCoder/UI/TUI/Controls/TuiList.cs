using System.Text;
using WayCoder.UI.Shared;
using Terminal = WayCoder.UI.Shared.Terminal;

namespace WayCoder.UI.Tui.Controls;

/// <summary>可滚动列表选单 —— 单选/多选，键盘导航。</summary>
public class TuiList : TuiControl
{
    public List<string> Items { get; set; } = [];
    public int SelectedIndex { get; set; }
    public HashSet<int> CheckedIndices { get; set; } = [];
    public bool MultiSelect { get; set; }
    public int ScrollOffset { get; set; }
    public Action<int>? OnSelect { get; set; }

    public TuiList()
    {
        Height = 5;
        Width = 30;
    }

    protected override void OnRender(StringBuilder sb, int absX, int absY)
    {
        int visRows = Height;
        int totalItems = Items.Count;
        if (totalItems == 0 || visRows <= 0) return;

        // 确保选中项可见
        if (SelectedIndex < ScrollOffset) ScrollOffset = SelectedIndex;
        if (SelectedIndex >= ScrollOffset + visRows) ScrollOffset = SelectedIndex - visRows + 1;
        ScrollOffset = Math.Clamp(ScrollOffset, 0, Math.Max(0, totalItems - visRows));

        var rb = new Terminal.RenderBuffer();
        for (int i = 0; i < visRows; i++)
        {
            int idx = ScrollOffset + i;
            int row = absY + i;
            if (idx >= totalItems) break;

            var item = Items[idx];
            var marker = MultiSelect
                ? (CheckedIndices.Contains(idx) ? "☑ " : "☐ ")
                : (idx == SelectedIndex ? "▶ " : "  ");
            var display = $"{marker}{item}";
            if (TuiHelper.DisplayWidth(display) > Width)
                display = TuiHelper.TruncateByWidth(display, Width);

            int fg = !IsEnabled ? (DisabledFg > 0 ? DisabledFg : TuiTheme.Current.ControlDisabledFg)
                : idx == SelectedIndex ? TuiTheme.Current.ListSelFg
                : (Fg > 0 ? Fg : TuiTheme.Current.ListFg);
            int bg = !IsEnabled ? (DisabledBg > 0 ? DisabledBg : 0)
                : idx == SelectedIndex ? TuiTheme.Current.ListSelBg
                : (Bg > 0 ? Bg : 0);

            rb.Write(row, absX, display + new string(' ', Math.Max(0, Width - TuiHelper.DisplayWidth(display))), fg: fg, bg: bg);
        }

        // 滚动条
        if (totalItems > visRows)
        {
            int barH = Math.Max(1, visRows * visRows / totalItems);
            int barPos = visRows * ScrollOffset / Math.Max(1, totalItems - visRows);
            barPos = Math.Clamp(barPos, 0, visRows - barH);
            for (int i = 0; i < visRows; i++)
            {
                int row = absY + i;
                var ch = (i >= barPos && i < barPos + barH) ? "█" : "│";
                rb.Write(row, absX + Width, ch, fg: 2);
            }
        }

        sb.Append(rb.ToString());
    }

    public override bool OnKey(ConsoleKeyInfo key)
    {
        if (!IsEnabled) return false;
        switch (key.Key)
        {
            case ConsoleKey.UpArrow:
                if (SelectedIndex > 0) { SelectedIndex--; MarkDirty(); }
                return true;
            case ConsoleKey.DownArrow:
                if (SelectedIndex < Items.Count - 1) { SelectedIndex++; MarkDirty(); }
                return true;
            case ConsoleKey.Home:
                if (SelectedIndex != 0) { SelectedIndex = 0; MarkDirty(); }
                return true;
            case ConsoleKey.End:
                if (SelectedIndex != Items.Count - 1) { SelectedIndex = Items.Count - 1; MarkDirty(); }
                return true;
            case ConsoleKey.PageUp:
                {
                    var next = Math.Max(0, SelectedIndex - Math.Max(1, Height));
                    if (next != SelectedIndex) { SelectedIndex = next; MarkDirty(); }
                }
                return true;
            case ConsoleKey.PageDown:
                {
                    var next = Math.Min(Items.Count - 1, SelectedIndex + Math.Max(1, Height));
                    if (next != SelectedIndex) { SelectedIndex = next; MarkDirty(); }
                }
                return true;
            case ConsoleKey.Spacebar:
                if (MultiSelect)
                {
                    if (CheckedIndices.Contains(SelectedIndex))
                        CheckedIndices.Remove(SelectedIndex);
                    else
                        CheckedIndices.Add(SelectedIndex);
                    MarkDirty();
                    return true;
                }
                OnSelect?.Invoke(SelectedIndex); // 单选：空格 = 激活（等同回车）
                return true;
            case ConsoleKey.Enter:
                OnSelect?.Invoke(SelectedIndex);
                return true;
        }

        return false;
    }
}