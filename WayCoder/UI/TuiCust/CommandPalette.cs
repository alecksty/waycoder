using System.Text;
using WayCoder.Terminal;

namespace WayCoder.UI;

/// <summary>
/// 命令面板对话框 —— 对标 Crush command palette。
/// 模糊搜索所有命令、分组显示、实时过滤、Enter 执行。
/// </summary>
public static class CommandPalette
{
    public record Command(string Id, string Label, string Category, string Shortcut, string Desc, Action Action);

    /// <summary>
    /// 显示命令面板。返回 true 表示执行了命令，false = 取消。
    /// </summary>
    public static bool Show(List<Command> commands)
    {
        var filter = "";
        int selectedIdx = 0;
        int scrollOffset = 0;

        var (tw, th) = (Tty.Cols, Tty.Rows);

        try
        {
        while (true)
        {
            var filtered = string.IsNullOrEmpty(filter)
                ? commands
                : commands.Where(c =>
                    c.Label.Contains(filter, StringComparison.OrdinalIgnoreCase) ||
                    c.Category.Contains(filter, StringComparison.OrdinalIgnoreCase) ||
                    c.Desc.Contains(filter, StringComparison.OrdinalIgnoreCase) ||
                    c.Shortcut.Contains(filter, StringComparison.OrdinalIgnoreCase)).ToList();

            selectedIdx = Math.Clamp(selectedIdx, 0, Math.Max(0, filtered.Count - 1));
            int visibleItems = Math.Max(3, th - 5);

            if (selectedIdx < scrollOffset) scrollOffset = selectedIdx;
            if (selectedIdx >= scrollOffset + visibleItems) scrollOffset = selectedIdx - visibleItems + 1;
            scrollOffset = Math.Clamp(scrollOffset, 0, Math.Max(0, filtered.Count - visibleItems));

            // ── 渲染 ──
            var sb = new StringBuilder();
            sb.Append(AnsiTty.CursorHide).Append(AnsiTty.Home);

            // 标题栏
            var title = "🔍 命令面板";
            sb.Append(AnsiTty.FgBg(30, 46)).Append($"  {title}")
              .Append(new string(' ', Math.Max(0, tw - VW(title) - 2)))
              .Append(AnsiTty.SgrReset).Append('\n');

            // 搜索栏
            sb.Append(AnsiTty.FgBg(30, 47));
            var prompt = filter.Length > 0 ? $"> {filter}" : "> 输入搜索...";
            var style = filter.Length > 0 ? "" : AnsiTty.SgrDim;
            var countLabel = $"  {filtered.Count}/{commands.Count}";
            sb.Append(style).Append(prompt).Append(AnsiTty.SgrReset);
            var pad = Math.Max(0, tw - VW(prompt) - VW(countLabel) - 1);
            sb.Append(new string(' ', pad))
              .Append(AnsiTty.SgrDim).Append(countLabel).Append(AnsiTty.SgrReset);
            sb.Append('\n');

            // 分类分隔线
            var lastCat = "";
            int listTop = 3;

            for (int i = 0; i < visibleItems; i++)
            {
                int ci = scrollOffset + i;
                sb.Append(AnsiTty.CursorPos(listTop + i, 1)).Append(AnsiTty.ClearToEnd);
                if (ci >= filtered.Count) continue;

                var cmd = filtered[ci];
                bool sel = ci == selectedIdx;

                // 分类分隔
                if (cmd.Category != lastCat)
                {
                    sb.Append(AnsiTty.Sgr(36, 0, 1))
                      .Append($"  ── {cmd.Category} ──")
                      .Append(AnsiTty.SgrReset);
                    lastCat = cmd.Category;
                    continue;
                }

                int rowFg = 37, rowBg = 0;
                if (sel) { rowFg = 30; rowBg = 46; }

                var prefix = sel ? "▶ " : "  ";
                var shortcut = string.IsNullOrEmpty(cmd.Shortcut) ? "" : $" {cmd.Shortcut}";

                sb.Append(sel ? AnsiTty.FgBg(rowFg, rowBg) : "");
                var line = $"{prefix}{cmd.Label}{AnsiTty.SgrDim} {cmd.Desc}{AnsiTty.SgrReset}{AnsiTty.Sgr(33, rowBg, 1)}{shortcut}";
                sb.Append(line).Append(AnsiTty.SgrReset);
            }

            // 帮助栏
            int helpRow = listTop + visibleItems;
            sb.Append(AnsiTty.CursorPos(helpRow, 1))
              .Append(AnsiTty.FgBg(30, 47))
              .Append("[↑/↓] 导航  [Enter] 执行  [Esc] 取消  输入关键词过滤")
              .Append(new string(' ', Math.Max(0, tw - 45)))
              .Append(AnsiTty.SgrReset);

            Console.Write(sb.ToString());

            // ── 输入 ──
            var key = Console.ReadKey(intercept: true);

            switch (key.Key)
            {
                case ConsoleKey.UpArrow:
                    if (selectedIdx > 0) selectedIdx--;
                    break;
                case ConsoleKey.DownArrow:
                    if (selectedIdx < filtered.Count - 1) selectedIdx++;
                    break;
                case ConsoleKey.Home:
                    selectedIdx = 0;
                    break;
                case ConsoleKey.End:
                    selectedIdx = Math.Max(0, filtered.Count - 1);
                    break;
                case ConsoleKey.PageUp:
                    selectedIdx = Math.Max(0, selectedIdx - visibleItems);
                    break;
                case ConsoleKey.PageDown:
                    selectedIdx = Math.Min(filtered.Count - 1, selectedIdx + visibleItems);
                    break;
                case ConsoleKey.Enter:
                    if (filtered.Count > 0 && selectedIdx < filtered.Count)
                    {
                        filtered[selectedIdx].Action();
                        return true;
                    }
                    break;
                case ConsoleKey.Escape:
                    return false;
                case ConsoleKey.Backspace:
                    if (filter.Length > 0)
                    {
                        filter = filter[..^1];
                        selectedIdx = 0;
                    }
                    break;
                default:
                    if (key.KeyChar >= ' ' && key.KeyChar <= '~')
                    {
                        filter += key.KeyChar;
                        selectedIdx = 0;
                    }
                    break;
            }
        }
        }
        finally
        {
            TuiManager.RequestFullRefresh();
        }
    }

    private static int VW(string text) => TuiHelper.DisplayWidth(text);
}
