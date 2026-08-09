using System.Text;
namespace CoreCoderSharp.UI.Controls;

/// <summary>可滚动列表选单 —— 单选/多选，键盘导航。</summary>
public class TuiList : TuiControl
{
    public List<string> Items { get; set; } = [];
    public int SelectedIndex { get; set; }
    public HashSet<int> CheckedIndices { get; set; } = [];
    public bool MultiSelect { get; set; }
    public int ScrollOffset { get; set; }
    public Action<int>? OnSelect { get; set; }

    public TuiList() { Height = 5; Width = 30; }

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

            int fg = idx == SelectedIndex ? 30 : (Fg > 0 ? Fg : 37);
            int bg = idx == SelectedIndex ? 46 : (Bg > 0 ? Bg : 0);

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

    public override bool HandleKey(ConsoleKeyInfo key)
    {
        switch (key.Key)
        {
            case ConsoleKey.UpArrow:
                if (SelectedIndex > 0) SelectedIndex--;
                return true;
            case ConsoleKey.DownArrow:
                if (SelectedIndex < Items.Count - 1) SelectedIndex++;
                return true;
            case ConsoleKey.Home:
                SelectedIndex = 0; return true;
            case ConsoleKey.End:
                SelectedIndex = Items.Count - 1; return true;
            case ConsoleKey.Spacebar when MultiSelect:
                if (CheckedIndices.Contains(SelectedIndex))
                    CheckedIndices.Remove(SelectedIndex);
                else
                    CheckedIndices.Add(SelectedIndex);
                return true;
            case ConsoleKey.Enter:
                OnSelect?.Invoke(SelectedIndex);
                return true;
        }
        return false;
    }
}
