using System.Text;
using WayCoder.UI.Shared;
using WayCoder.UI.TUI.Base;
using Terminal = WayCoder.UI.Shared.Terminal;

namespace WayCoder.UI.Tui.Controls;

/// <summary>可滚动列表选单 —— 单选/多选，键盘 + 鼠标。</summary>
public class TuiList : TuiListControl
{
    public List<string> Items { get; set; } = [];
    public HashSet<int> CheckedIndices { get; set; } = [];
    public bool MultiSelect { get; set; }
    public Action<int>? OnSelect { get; set; }

    protected override int ItemCount => Items.Count;

    public TuiList()
    {
        Height = 5;
        Width = 30;
    }

    /// <summary>鼠标支持：滚轮滚动 + 左键点击选中（单选触发 OnSelect，多选勾选切换）。
    /// 此前无 OnMouse —— 设置界面分类列表等点击无效，只能键盘导航。</summary>
    public override bool OnMouse(InputEvent ev)
    {
        if (ev.Type != InputType.Mouse) return false;
        int absX = GetAbsoluteX();
        int absY = GetAbsoluteY();
        if (ev.MouseX < absX || ev.MouseX >= absX + Width ||
            ev.MouseY < absY || ev.MouseY >= absY + Height)
            return false;

        if (ev.MouseScrollUp)
        {
            ScrollOffset = Math.Max(0, ScrollOffset - 3);
            MarkDirty();
            return true;
        }

        if (ev.MouseScrollDown)
        {
            ScrollOffset = Math.Min(Math.Max(0, Items.Count - Height), ScrollOffset + 3);
            MarkDirty();
            return true;
        }

        if (ev.MouseLeft)
        {
            Focused = true; // 点击后方向键才路由到本列表（对齐 TuiView.OnKey 只派发聚焦子控件）
            int idx = ScrollOffset + (ev.MouseY - absY);
            if (idx >= 0 && idx < Items.Count)
            {
                if (MultiSelect)
                {
                    if (CheckedIndices.Contains(idx)) CheckedIndices.Remove(idx);
                    else CheckedIndices.Add(idx);
                }
                else
                {
                    SelectedIndex = idx;
                    OnSelect?.Invoke(idx); // 点击 = 激活（对齐空格键语义）
                }

                MarkDirty();
            }

            return true; // 区域内消费事件
        }

        return base.OnMouse(ev);
    }

    protected override void OnRender(StringBuilder sb, int absX, int absY)
    {
        int visRows = Height;
        int totalItems = Items.Count;
        if (totalItems == 0 || visRows <= 0) return;

        // 确保选中项可见
        ScrollOffset = TuiScrollMath.EnsureVisible(SelectedIndex, ScrollOffset, totalItems, visRows);

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
            if (AnsiHelper.DisplayWidth(display) > Width)
                display = AnsiHelper.TruncateByWidth(display, Width);

            int fg = !IsEnabled ? (DisabledFg > 0 ? DisabledFg : TuiTheme.Current.ControlDisabledFg)
                : idx == SelectedIndex ? TuiTheme.Current.ListSelFg
                : (Fg > 0 ? Fg : TuiTheme.Current.ListFg);
            int bg = !IsEnabled ? (DisabledBg > 0 ? DisabledBg : TuiTheme.Current.ListBg)
                : idx == SelectedIndex ? TuiTheme.Current.ListSelBg
                : (Bg > 0 ? Bg : TuiTheme.Current.ListBg);

            rb.Write(row, absX, display + new string(' ', Math.Max(0, Width - AnsiHelper.DisplayWidth(display))), fg: fg, bg: bg);
        }

        // 滚动条
        if (totalItems > visRows)
        {
            var (barH, barPos) = TuiScrollMath.Bar(totalItems, visRows, ScrollOffset);
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
            case ConsoleKey.UpArrow: MoveUp(); return true;
            case ConsoleKey.DownArrow: MoveDown(); return true;
            case ConsoleKey.Home: MoveHome(); return true;
            case ConsoleKey.End: MoveEnd(); return true;
            case ConsoleKey.PageUp: MovePage(-1); return true;
            case ConsoleKey.PageDown: MovePage(1); return true;
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