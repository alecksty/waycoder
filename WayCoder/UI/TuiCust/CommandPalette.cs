using System.Text;
using WayCoder.Terminal;

namespace WayCoder.UI;

/// <summary>
/// 命令面板对话框 —— 对标 Crush command palette。
/// 居中带边框对话框（非全屏），模糊搜索所有命令、分组显示、实时过滤、Enter 执行。
/// </summary>
public static class CommandPalette
{
    public record Command(string Id, string Label, string Category, string Shortcut, string Desc, Action Action);

    private const int MinW = 52, MinH = 13;
    private const int FrameH = 7; // 顶框1 + 标题1 + 搜索1 + 上分隔1 + 下分隔1 + 帮助1 + 底框1

    /// <summary>
    /// 显示命令面板。返回 true 表示执行了命令，false = 取消。
    /// </summary>
    public static bool Show(List<Command> commands)
    {
        var filter = "";
        int sel = 0;          // 高亮行（rows 索引，始终指向命令行）
        int scrollOffset = 0; // 可见区顶行（rows 索引）

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

            // 构建显示行：分类头独占一行、与命令交错（不再隐藏每组第一条命令）
            var rows = new List<(bool IsHeader, int CmdIdx, string Cat)>();
            string? prevCat = null;
            for (int i = 0; i < filtered.Count; i++)
            {
                if (filtered[i].Category != prevCat)
                {
                    rows.Add((true, i, filtered[i].Category));
                    prevCat = filtered[i].Category;
                }
                rows.Add((false, i, filtered[i].Category));
            }

            var (bx, by, dw, dh, innerW) = DialogFrame.Layout(MinW, MinH);
            int listH = Math.Max(3, dh - FrameH);

            // 光标钳制到命令行
            int cmdCount = rows.Count(r => !r.IsHeader);
            if (cmdCount == 0) sel = 0;
            else
            {
                sel = Math.Clamp(sel, 0, rows.Count - 1);
                if (rows[sel].IsHeader) sel = StepToCommand(rows, sel, +1);
            }

            // 滚动：保证高亮命令行可见
            if (sel < scrollOffset) scrollOffset = sel;
            if (sel >= scrollOffset + listH) scrollOffset = sel - listH + 1;
            scrollOffset = Math.Clamp(scrollOffset, 0, Math.Max(0, rows.Count - listH));

            // ── 渲染 ──
            var sb = new StringBuilder();
            sb.Append(AnsiTty.CursorHide);
            DialogFrame.DimArea(sb, bx, by, dw, dh);
            DialogFrame.TopBorder(sb, by, bx, dw);

            // 标题行
            int y = by + 1;
            DialogFrame.SideL(sb, y, bx);
            sb.Append(AnsiTty.CursorPos(y, bx + 2))
              .Append(AnsiTty.FgBgCode(TuiColors.White, DialogFrame.DimBg))
              .Append(AnsiTty.SgrBold).Append("🔍 命令面板").Append(AnsiTty.SgrReset);
            var countLabel = $"{filtered.Count}/{commands.Count}";
            sb.Append(AnsiTty.CursorPos(y, bx + dw - 2 - VW(countLabel)))
              .Append(AnsiTty.FgBgCode(TuiColors.BrightBlack, DialogFrame.DimBg))
              .Append(AnsiTty.SgrDim).Append(countLabel).Append(AnsiTty.SgrReset);
            DialogFrame.SideR(sb, y, bx, dw);

            // 搜索行
            y = by + 2;
            DialogFrame.SideL(sb, y, bx);
            sb.Append(AnsiTty.CursorPos(y, bx + 2))
              .Append(AnsiTty.FgBgCode(TuiColors.White, DialogFrame.DimBg));
            var prompt = filter.Length > 0 ? $"> {filter}" : "> 输入搜索...";
            var style = filter.Length > 0 ? "" : AnsiTty.SgrDim;
            sb.Append(style).Append(TruncVW(prompt, innerW - 2));
            sb.Append(AnsiTty.SgrReset);
            DialogFrame.SideR(sb, y, bx, dw);

            // 上分隔线
            y = by + 3;
            DialogFrame.SepLine(sb, y, bx, dw);

            // 列表
            int dataTop = by + 4;
            for (int i = 0; i < listH; i++)
            {
                int ri = scrollOffset + i, row = dataTop + i;
                DialogFrame.SideL(sb, row, bx);

                if (ri >= rows.Count)
                {
                    DialogFrame.FillInner(sb, row, bx, innerW, TuiColors.White, DialogFrame.DimBg);
                    DialogFrame.SideR(sb, row, bx, dw);
                    continue;
                }

                var (isHeader, cmdIdx, cat) = rows[ri];

                if (isHeader)
                {
                    // 分类头：独立的整行，青色粗体
                    DialogFrame.FillInner(sb, row, bx, innerW, TuiColors.Cyan, DialogFrame.DimBg);
                    sb.Append(AnsiTty.CursorPos(row, bx + 2))
                      .Append(AnsiTty.FgBgCode(TuiColors.Cyan, DialogFrame.DimBg))
                      .Append(AnsiTty.SgrBold)
                      .Append("─ ").Append(TruncVW(cat, Math.Max(4, innerW - 4))).Append(" ─")
                      .Append(AnsiTty.SgrReset);
                }
                else
                {
                    var cmd = filtered[cmdIdx];
                    bool isSel = ri == sel;
                    int bg = isSel ? TuiColors.BgCyan : DialogFrame.DimBg;
                    int fg = isSel ? TuiColors.Black : TuiColors.White;
                    DialogFrame.FillInner(sb, row, bx, innerW, fg, bg);

                    // 标签/描述/快捷键各自截断，防止长命令溢出边框
                    var prefix = isSel ? "▶ " : "  ";
                    var shortcut = string.IsNullOrEmpty(cmd.Shortcut) ? "" : $" {cmd.Shortcut}";
                    int avail = Math.Max(4, innerW - 2);
                    int shortcutVW = VW(shortcut);
                    var label = TruncVW(prefix + cmd.Label, Math.Max(4, avail - shortcutVW));
                    int descMax = avail - VW(label) - shortcutVW;
                    var desc = descMax >= 3 ? TruncVW(" " + cmd.Desc, descMax) : "";

                    sb.Append(AnsiTty.CursorPos(row, bx + 2))
                      .Append(AnsiTty.FgBgCode(fg, bg))
                      .Append(label);
                    sb.Append(AnsiTty.SgrDim).Append(desc).Append(AnsiTty.SgrReset);
                    if (shortcut.Length > 0)
                        sb.Append(AnsiTty.FgBgCode(TuiColors.Yellow, bg)).Append(shortcut);
                    sb.Append(AnsiTty.SgrReset);
                }
                DialogFrame.SideR(sb, row, bx, dw);
            }

            // 下分隔线
            int sep2 = dataTop + listH;
            DialogFrame.SepLine(sb, sep2, bx, dw);

            // 帮助行
            DialogFrame.SideL(sb, sep2 + 1, bx);
            sb.Append(AnsiTty.CursorPos(sep2 + 1, bx + 2))
              .Append(AnsiTty.FgBgCode(TuiColors.BrightBlack, DialogFrame.DimBg))
              .Append(TruncVW("[↑/↓] 导航  [Enter] 执行  [Esc] 取消  输入关键词过滤", innerW - 4));
            DialogFrame.SideR(sb, sep2 + 1, bx, dw);

            // 底框
            DialogFrame.BottomBorder(sb, sep2 + 2, bx, dw);

            sb.Append(AnsiTty.SgrReset);
            Console.Write(sb.ToString());

            // ── 输入 ──
            var key = Console.ReadKey(intercept: true);

            switch (key.Key)
            {
                case ConsoleKey.UpArrow:
                    sel = StepToCommand(rows, sel, -1);
                    break;
                case ConsoleKey.DownArrow:
                    sel = StepToCommand(rows, sel, +1);
                    break;
                case ConsoleKey.Home:
                    sel = StepToCommand(rows, -1, +1);
                    break;
                case ConsoleKey.End:
                    sel = StepToCommand(rows, rows.Count, -1);
                    break;
                case ConsoleKey.PageUp:
                    sel = StepToCommand(rows, Math.Max(0, sel - listH), -1);
                    break;
                case ConsoleKey.PageDown:
                    sel = StepToCommand(rows, Math.Min(rows.Count - 1, sel + listH), +1);
                    break;
                case ConsoleKey.Enter:
                    if (cmdCount > 0 && sel >= 0 && sel < rows.Count && !rows[sel].IsHeader)
                    {
                        filtered[rows[sel].CmdIdx].Action();
                        return true;
                    }
                    break;
                case ConsoleKey.Escape:
                    return false;
                case ConsoleKey.Backspace:
                    if (filter.Length > 0)
                    {
                        filter = filter[..^1];
                        sel = 0;
                    }
                    break;
                default:
                    if (key.KeyChar >= ' ' && key.KeyChar <= '~')
                    {
                        filter += key.KeyChar;
                        sel = 0;
                    }
                    break;
            }
        }
        }
        finally
        {
            Console.Write(AnsiTty.CursorShow);
            TuiManager.RequestFullRefresh();
        }
    }

    /// <summary>移动到相邻命令行（跳过分类头）。dir=+1 向下、-1 向上；越界停在最近的命令行。</summary>
    internal static int StepToCommand(List<(bool IsHeader, int CmdIdx, string Cat)> rows, int from, int dir)
    {
        int p = from + dir;
        if (dir > 0)
        {
            if (p < 0) p = 0;
            while (p < rows.Count && rows[p].IsHeader) p++;
            if (p >= rows.Count)
            {
                p = rows.Count - 1;
                while (p >= 0 && rows[p].IsHeader) p--;
            }
        }
        else
        {
            if (p >= rows.Count) p = rows.Count - 1;
            while (p >= 0 && rows[p].IsHeader) p--;
            if (p < 0)
            {
                p = 0;
                while (p < rows.Count && rows[p].IsHeader) p++;
            }
        }
        return p;
    }

    private static int VW(string text) => TuiHelper.DisplayWidth(text);

    private static string TruncVW(string text, int max)
    {
        if (string.IsNullOrEmpty(text)) return "";
        if (VW(text) <= max) return text;
        var runes = text.EnumerateRunes().ToList();
        int w = 0, n = 0;
        foreach (var r in runes)
        {
            int rw = TuiHelper.RuneWidth(r);
            if (w + rw + 1 > max) break;
            w += rw; n++;
        }
        var sb = new StringBuilder();
        for (int i = 0; i < n; i++) sb.Append(runes[i].ToString());
        sb.Append('…');
        return sb.ToString();
    }
}
