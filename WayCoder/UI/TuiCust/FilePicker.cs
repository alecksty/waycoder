using System.Text;
using WayCoder.Terminal;

namespace WayCoder.UI;

/// <summary>
/// 文件选择对话框 —— 对标 Crush filepicker。
/// 居中带边框对话框（非全屏），目录浏览、文件过滤、⇅导航、Enter 确认、Esc 取消。
/// </summary>
public static class FilePicker
{
    public record FileEntry(string Name, string FullPath, bool IsDir, long Size, DateTime Modified);

    private const int MinW = 70, MinH = 16;
    private const int FrameH = 9; // 顶框1+标题1+路径1+搜索1+上分隔1+列头1 + 下分隔1+帮助1+底框1

    // 右侧三列固定宽度（大小右对齐 / 日期 / 时间）
    private const int SizeW = 8, DateW = 5, TimeW = 5, ColGap = 2;

    /// <summary>
    /// 显示文件选择对话框。返回选中的文件路径，null = 取消。
    /// </summary>
    /// <param name="startDir">起始目录，默认当前目录</param>
    /// <param name="filter">文件过滤（如 "*.cs"），null = 全部</param>
    /// <param name="title">对话框标题</param>
    public static string? Show(string? startDir = null, string? filter = null, string title = "选择文件")
    {
        var dir = startDir ?? Environment.CurrentDirectory;
        dir = Path.GetFullPath(dir);
        var pattern = filter ?? "*";

        var entries = LoadDir(dir, pattern);
        int selectedIdx = 0;
        int scrollOffset = 0;
        var searchFilter = "";

        try
        {
        while (true)
        {
            // 过滤
            var filtered = string.IsNullOrEmpty(searchFilter)
                ? entries
                : entries.Where(e =>
                    e.Name.Contains(searchFilter, StringComparison.OrdinalIgnoreCase)).ToList();

            var (bx, by, dw, dh, innerW) = DialogFrame.Layout(MinW, MinH);
            int listH = Math.Max(4, dh - FrameH);

            selectedIdx = Math.Clamp(selectedIdx, 0, Math.Max(0, filtered.Count - 1));
            if (selectedIdx < scrollOffset) scrollOffset = selectedIdx;
            if (selectedIdx >= scrollOffset + listH) scrollOffset = selectedIdx - listH + 1;
            scrollOffset = Math.Clamp(scrollOffset, 0, Math.Max(0, filtered.Count - listH));

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
              .Append(AnsiTty.SgrBold).Append(TruncateByVW(title, innerW - 4)).Append(AnsiTty.SgrReset);
            DialogFrame.SideR(sb, y, bx, dw);

            // 路径栏
            y = by + 2;
            DialogFrame.SideL(sb, y, bx);
            sb.Append(AnsiTty.CursorPos(y, bx + 2))
              .Append(AnsiTty.FgBgCode(TuiColors.BrightBlack, DialogFrame.DimBg))
              .Append("📁 ").Append(TruncLeftVW(dir, innerW - 4))
              .Append(AnsiTty.SgrReset);
            DialogFrame.SideR(sb, y, bx, dw);

            // 搜索栏
            y = by + 3;
            DialogFrame.SideL(sb, y, bx);
            sb.Append(AnsiTty.CursorPos(y, bx + 2))
              .Append(AnsiTty.FgBgCode(TuiColors.White, DialogFrame.DimBg));
            var st = searchFilter.Length > 0 ? searchFilter : "输入过滤...";
            var ss = searchFilter.Length > 0 ? "" : AnsiTty.SgrDim;
            sb.Append("搜索: ").Append(ss).Append(TruncateByVW(st, innerW - 6)).Append(AnsiTty.SgrReset);
            DialogFrame.SideR(sb, y, bx, dw);

            // 上分隔线
            y = by + 4;
            DialogFrame.SepLine(sb, y, bx, dw);

            // 列标题行
            y = by + 5;
            DialogFrame.SideL(sb, y, bx);
            DialogFrame.FillInner(sb, y, bx, innerW, TuiColors.BrightBlack, DialogFrame.DimBg);
            DrawColHeader(sb, y, bx, innerW);
            DialogFrame.SideR(sb, y, bx, dw);

            // 文件列表
            int dataTop = by + 6;
            for (int i = 0; i < listH; i++)
            {
                int ei = scrollOffset + i, row = dataTop + i;
                DialogFrame.SideL(sb, row, bx);

                if (ei >= filtered.Count)
                {
                    DialogFrame.FillInner(sb, row, bx, innerW, TuiColors.White, DialogFrame.DimBg);
                    DialogFrame.SideR(sb, row, bx, dw);
                    continue;
                }

                var entry = filtered[ei];
                bool sel = ei == selectedIdx;

                int bg = sel ? TuiColors.BgCyan : DialogFrame.DimBg;
                int fg = sel ? TuiColors.Black : (entry.IsDir ? TuiColors.Blue : TuiColors.White);
                DialogFrame.FillInner(sb, row, bx, innerW, fg, bg);
                DrawFileRow(sb, row, bx, innerW, entry, fg, bg, sel);

                DialogFrame.SideR(sb, row, bx, dw);
            }

            // 下分隔线
            int sep2 = dataTop + listH;
            DialogFrame.SepLine(sb, sep2, bx, dw);

            // 帮助行
            DialogFrame.SideL(sb, sep2 + 1, bx);
            sb.Append(AnsiTty.CursorPos(sep2 + 1, bx + 2))
              .Append(AnsiTty.FgBgCode(TuiColors.BrightBlack, DialogFrame.DimBg))
              .Append("[↑/↓] 导航  [Enter] 选择/打开  [Backspace] 上级目录  [Esc] 取消");
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
                    selectedIdx = Math.Max(0, selectedIdx - listH);
                    break;
                case ConsoleKey.PageDown:
                    selectedIdx = Math.Min(filtered.Count - 1, selectedIdx + listH);
                    break;
                case ConsoleKey.Enter:
                    if (filtered.Count > 0 && selectedIdx < filtered.Count)
                    {
                        var entry = filtered[selectedIdx];
                        if (entry.IsDir)
                        {
                            dir = entry.FullPath;
                            entries = LoadDir(dir, pattern);
                            selectedIdx = 0; scrollOffset = 0; searchFilter = "";
                        }
                        else
                        {
                            return entry.FullPath;
                        }
                    }
                    break;
                case ConsoleKey.Backspace:
                    var parent = Path.GetDirectoryName(dir);
                    if (parent != null && Directory.Exists(parent))
                    {
                        dir = parent;
                        entries = LoadDir(dir, pattern);
                        selectedIdx = 0; scrollOffset = 0; searchFilter = "";
                    }
                    break;
                case ConsoleKey.Escape:
                    return null;
                default:
                    if (key.KeyChar >= ' ' && key.KeyChar <= '~')
                    {
                        // 退格：删除搜索字符
                        if (key.KeyChar == '\b' && searchFilter.Length > 0)
                            searchFilter = searchFilter[..^1];
                        else if (key.KeyChar >= ' ' && key.KeyChar != '\b')
                            searchFilter += key.KeyChar;
                        selectedIdx = 0;
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

    private static List<FileEntry> LoadDir(string dir, string pattern)
    {
        var list = new List<FileEntry>();
        try
        {
            // 父目录快捷项
            var parent = Path.GetDirectoryName(dir);
            if (parent != null)
                list.Add(new FileEntry("..", parent, true, 0, DateTime.MinValue));

            foreach (var d in Directory.GetDirectories(dir))
            {
                try
                {
                    var di = new DirectoryInfo(d);
                    list.Add(new FileEntry(di.Name, di.FullName, true, 0, di.LastWriteTime));
                }
                catch { }
            }

            foreach (var f in Directory.GetFiles(dir, pattern))
            {
                try
                {
                    var fi = new FileInfo(f);
                    list.Add(new FileEntry(fi.Name, fi.FullName, false, fi.Length, fi.LastWriteTime));
                }
                catch { }
            }
        }
        catch { }

        return list
            .OrderBy(e => e.IsDir && e.Name != ".." ? 0 : e.Name == ".." ? -1 : 1)
            .ThenBy(e => e.Name, StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    private static string FormatSize(long bytes) => bytes switch
    {
        < 1024 => $"{bytes}B",
        < 1024 * 1024 => $"{bytes / 1024.0:F1}K",
        < 1024 * 1024 * 1024 => $"{bytes / (1024.0 * 1024):F1}M",
        _ => $"{bytes / (1024.0 * 1024 * 1024):F1}G"
    };

    /// <summary>计算四列（文件/大小/日期/时间）的起始列与文件名列宽。</summary>
    private static (int cx, int sizeStart, int dateStart, int timeStart, int nameW) Cols(int bx, int innerW)
    {
        int cx = bx + 2;
        int contentW = innerW - 4;
        int timeStart = cx + contentW - TimeW;
        int dateStart = timeStart - ColGap - DateW;
        int sizeStart = dateStart - ColGap - SizeW;
        int nameW = Math.Max(8, sizeStart - ColGap - cx);
        return (cx, sizeStart, dateStart, timeStart, nameW);
    }

    /// <summary>列标题行：文件 / 大小 / 日期 / 时间。</summary>
    private static void DrawColHeader(StringBuilder sb, int y, int bx, int innerW)
    {
        var (cx, sizeStart, dateStart, timeStart, _) = Cols(bx, innerW);
        sb.Append(AnsiTty.FgBgCode(TuiColors.BrightBlack, DialogFrame.DimBg))
          .Append(AnsiTty.SgrDim);
        sb.Append(AnsiTty.CursorPos(y, cx)).Append("文件");
        sb.Append(AnsiTty.CursorPos(y, sizeStart + SizeW - VW("大小"))).Append("大小");
        sb.Append(AnsiTty.CursorPos(y, dateStart)).Append("日期");
        sb.Append(AnsiTty.CursorPos(y, timeStart)).Append("时间");
        sb.Append(AnsiTty.SgrReset);
    }

    /// <summary>文件列表行：文件名列 + 右对齐大小列 + 固定日期/时间列，各列用 CursorPos 对齐。</summary>
    private static void DrawFileRow(StringBuilder sb, int y, int bx, int innerW, FileEntry entry,
        int fg, int bg, bool sel)
    {
        var (cx, sizeStart, dateStart, timeStart, nameW) = Cols(bx, innerW);

        var icon = entry.IsDir ? "📁" : "📄";
        var prefix = sel ? "▶ " : "  ";
        var sizeStr = entry.IsDir ? "" : FormatSize(entry.Size);
        bool hasTime = entry.Modified != DateTime.MinValue;
        var dateStr = hasTime ? entry.Modified.ToString("MM-dd") : "";
        var timeStr = hasTime ? entry.Modified.ToString("HH:mm") : "";

        // 文件名列（含图标），目录加粗
        sb.Append(AnsiTty.CursorPos(y, cx)).Append(AnsiTty.FgBgCode(fg, bg));
        if (entry.IsDir && !sel) sb.Append(AnsiTty.SgrBold);
        sb.Append(TruncateByVW(prefix + icon + " " + entry.Name, nameW));
        sb.Append(AnsiTty.SgrReset);

        // 大小（右对齐）
        if (sizeStr.Length > 0)
            sb.Append(AnsiTty.CursorPos(y, sizeStart + SizeW - VW(sizeStr)))
              .Append(AnsiTty.FgBgCode(fg, bg)).Append(AnsiTty.SgrDim).Append(sizeStr).Append(AnsiTty.SgrReset);

        // 日期
        if (dateStr.Length > 0)
            sb.Append(AnsiTty.CursorPos(y, dateStart))
              .Append(AnsiTty.FgBgCode(fg, bg)).Append(AnsiTty.SgrDim).Append(dateStr).Append(AnsiTty.SgrReset);

        // 时间
        if (timeStr.Length > 0)
            sb.Append(AnsiTty.CursorPos(y, timeStart))
              .Append(AnsiTty.FgBgCode(fg, bg)).Append(AnsiTty.SgrDim).Append(timeStr).Append(AnsiTty.SgrReset);
    }

    private static int VW(string text) => TuiHelper.DisplayWidth(text);

    private static string TruncateByVW(string text, int maxVW)
    {
        if (string.IsNullOrEmpty(text)) return "";
        int vw = 0, chars = 0;
        foreach (var r in text.EnumerateRunes())
        {
            var w = TuiHelper.RuneWidth(r);
            if (vw + w > maxVW) break;
            vw += w; chars += r.Utf16SequenceLength;
        }
        return chars == text.Length ? text : text[..chars] + "…";
    }

    /// <summary>超长时从左侧截断（保留末尾，用于路径显示）。</summary>
    private static string TruncLeftVW(string text, int maxVW)
    {
        if (string.IsNullOrEmpty(text)) return "";
        if (VW(text) <= maxVW) return text;
        var runes = text.EnumerateRunes().ToList();
        int budget = maxVW - 1; // 预留 1 宽给 …
        var sb = new StringBuilder();
        for (int i = runes.Count - 1; i >= 0; i--)
        {
            int rw = TuiHelper.RuneWidth(runes[i]);
            if (budget - rw < 0) break;
            budget -= rw;
            sb.Insert(0, runes[i].ToString());
        }
        return "…" + sb;
    }
}
