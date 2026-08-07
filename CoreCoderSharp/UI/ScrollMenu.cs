using System.Text;

namespace CoreCoderSharp.UI;

/// <summary>
/// 可滚动选择菜单 — 继承 BoxBuffer，各处复用。
/// 用法：
///   var idx = ScrollMenu.Show("标题", items);
///   if (idx >= 0) { ... }
///
/// 实例用法（内嵌在其他界面中）：
///   var menu = new ScrollMenu { X=5, Y=3, Width=30, Height=12, Items=... };
///   menu.Render(sb);
///   menu.HandleKey(key, out _);
/// </summary>
public class ScrollMenu : BoxBuffer
{
    public string? Title { get; set; }
    public List<string> Items { get; set; } = [];
    public int VisibleCount
    {
        get => ContentHeight - (Title != null ? 1 : 0);
        set => Height = value + 2 + (Title != null ? 1 : 0);
    }

    public int SelectedIndex { get; set; }
    public int ScrollOffset { get; private set; }
    public string HighlightFg { get; set; } = "30";
    public string HighlightBg { get; set; } = "46";
    public string HintText { get; set; } = "↑↓ 选择  Enter 确认  Esc 取消";

    /// <summary>计算建议宽度（基于最长项）</summary>
    public int AutoWidth
    {
        get
        {
            int max = Title != null ? VW(Title) : 0;
            foreach (var item in Items)
            {
                var w = VW(item);
                if (w > max) max = w;
            }
            return max + 4; // 左右留白
        }
    }

    /// <summary>更新滚动偏移，保证选中项可见</summary>
    public void EnsureVisible()
    {
        var vc = VisibleCount;
        if (SelectedIndex < ScrollOffset)
            ScrollOffset = SelectedIndex;
        if (SelectedIndex >= ScrollOffset + vc)
            ScrollOffset = SelectedIndex - vc + 1;
        ScrollOffset = Math.Clamp(ScrollOffset, 0,
            Math.Max(0, Items.Count - vc));
    }

    /// <summary>处理按键，返回 true 表示已确认/取消</summary>
    public bool HandleKey(ConsoleKeyInfo key, out bool cancelled)
    {
        cancelled = false;
        EnsureVisible();

        switch (key.Key)
        {
            case ConsoleKey.UpArrow:
                if (SelectedIndex > 0) SelectedIndex--;
                EnsureVisible();
                return false;
            case ConsoleKey.DownArrow:
                if (SelectedIndex < Items.Count - 1) SelectedIndex++;
                EnsureVisible();
                return false;
            case ConsoleKey.Home:
                SelectedIndex = 0;
                EnsureVisible();
                return false;
            case ConsoleKey.End:
                SelectedIndex = Items.Count - 1;
                EnsureVisible();
                return false;
            case ConsoleKey.PageUp:
                SelectedIndex = Math.Max(0, SelectedIndex - VisibleCount);
                EnsureVisible();
                return false;
            case ConsoleKey.PageDown:
                SelectedIndex = Math.Min(Items.Count - 1,
                    SelectedIndex + VisibleCount);
                EnsureVisible();
                return false;
            case ConsoleKey.Enter:
                return true;
            case ConsoleKey.Escape:
                cancelled = true;
                return true;
        }
        return false;
    }

    /// <summary>渲染到 StringBuilder（覆盖 BoxBuffer.Render）</summary>
    public new void Render(StringBuilder sb)
    {
        base.Render(sb);

        int row = 0;
        var vc = VisibleCount;

        // 标题行
        if (Title != null)
        {
            WriteLine(sb, row, 0, $" {Title}");
            row++;
        }

        // 内容区
        int visible = Math.Min(Items.Count, vc);
        for (int i = 0; i < visible; i++)
        {
            int itemIdx = ScrollOffset + i;
            if (itemIdx >= Items.Count) break;

            var text = " " + Items[itemIdx];
            var isSel = itemIdx == SelectedIndex;

            if (isSel)
                WriteLineHighlight(sb, row + i, HighlightFg, HighlightBg, text);
            else
                WriteLine(sb, row + i, 0, text);
        }
    }

    // ================================================================
    // 静态便捷方法
    // ================================================================

    /// <summary>
    /// 全屏居中显示可滚动菜单并返回选中索引。
    /// 返回 -1 表示用户取消（Esc）。
    /// </summary>
    public static int Show(string title, List<string> items,
        int visibleCount = 10)
    {
        if (items.Count == 0) return -1;

        var sm = ScreenManager.Instance;
        var wasActive = sm.IsActive;
        if (!wasActive) sm.Enter();

        try
        {
            var menu = new ScrollMenu
            {
                Title = title,
                Items = items,
                SelectedIndex = 0,
                FgColor = "37",
                Border = BorderStyle.Single,
            };
            menu.VisibleCount = Math.Min(visibleCount, items.Count);
            menu.Width = Math.Max(20, menu.AutoWidth) + 2; // +2 for borders

            while (true)
            {
                menu.X = Math.Max(1, (Console.WindowWidth - menu.Width) / 2);
                menu.Y = Math.Max(1, (Console.WindowHeight - menu.Height - 1) / 2);

                Console.CursorVisible = false;
                var sb = new StringBuilder();
                sb.Append("\x1b[2J\x1b[H"); // 清屏
                menu.Render(sb);

                // 底栏提示
                var hintY = menu.Y + menu.Height;
                var hintX = menu.X + 1;
                var hint = menu.HintText;
                if (VW(hint) > menu.Width - 2)
                    hint = TruncateByVW(hint, menu.Width - 3) + "…";
                sb.Append($"\x1b[{hintY};{hintX}H\x1b[2m{hint}\x1b[0m");

                Console.Write(sb.ToString());

                var key = Console.ReadKey(intercept: true);
                if (menu.HandleKey(key, out var cancelled))
                {
                    Console.CursorVisible = true;
                    return cancelled ? -1 : menu.SelectedIndex;
                }
            }
        }
        finally
        {
            if (!wasActive) sm.Exit(); else sm.Render();
            Console.CursorVisible = true;
        }
    }
}
