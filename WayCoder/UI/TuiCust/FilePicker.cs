using System.Text;
using WayCoder.Terminal;

namespace WayCoder.UI;

/// <summary>
/// 文件选择对话框 —— 对标 Crush filepicker。
/// 目录浏览、文件过滤、⇅导航、Enter 确认、Esc 取消。
/// </summary>
public static class FilePicker
{
    public record FileEntry(string Name, string FullPath, bool IsDir, long Size, DateTime Modified);

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

        var (tw, th) = (Tty.Cols, Tty.Rows);

        try
        {
        while (true)
        {
            // 过滤
            var filtered = string.IsNullOrEmpty(searchFilter)
                ? entries
                : entries.Where(e =>
                    e.Name.Contains(searchFilter, StringComparison.OrdinalIgnoreCase)).ToList();

            selectedIdx = Math.Clamp(selectedIdx, 0, Math.Max(0, filtered.Count - 1));

            int visibleItems = Math.Max(5, th - 6); // title + path + help + margins
            if (selectedIdx < scrollOffset) scrollOffset = selectedIdx;
            if (selectedIdx >= scrollOffset + visibleItems) scrollOffset = selectedIdx - visibleItems + 1;
            scrollOffset = Math.Clamp(scrollOffset, 0, Math.Max(0, filtered.Count - visibleItems));

            // ── 渲染 ──
            var sb = new StringBuilder();
            sb.Append(AnsiTty.CursorHide).Append(AnsiTty.Home);

            // 标题
            sb.Append(AnsiTty.FgBg(30, 46)).Append($"  {title}  ")
              .Append(new string(' ', Math.Max(0, tw - VW(title) - 4)))
              .Append(AnsiTty.SgrReset).Append('\n');

            // 路径栏
            var pathText = $"📁 {dir}";
            if (pathText.Length > tw - 2) pathText = "…" + pathText[^(tw - 5)..];
            sb.Append(AnsiTty.FgBg(37, 44)).Append(pathText)
              .Append(new string(' ', Math.Max(0, tw - VW(pathText))))
              .Append(AnsiTty.SgrReset).Append('\n');

            // 搜索栏
            sb.Append(AnsiTty.FgBg(30, 47));
            var searchPrompt = "搜索: ";
            var st = searchFilter.Length > 0 ? searchFilter : "输入过滤...";
            var ss = searchFilter.Length > 0 ? "" : AnsiTty.SgrDim;
            var dirHint = "  Enter=打开目录  Backspace=上级  Esc=取消";
            sb.Append(searchPrompt).Append(ss).Append(st).Append(AnsiTty.SgrReset);
            var pad = Math.Max(0, tw - VW(searchPrompt + st) - VW(dirHint) - 2);
            sb.Append(new string(' ', pad)).Append(AnsiTty.SgrDim).Append(dirHint).Append(AnsiTty.SgrReset);
            sb.Append('\n');

            // 文件列表
            int listTop = 4;
            for (int i = 0; i < visibleItems; i++)
            {
                int ei = scrollOffset + i;
                sb.Append(AnsiTty.CursorPos(listTop + i, 1)).Append(AnsiTty.ClearToEnd);
                if (ei >= filtered.Count) continue;

                var entry = filtered[ei];
                bool sel = ei == selectedIdx;

                var icon = entry.IsDir ? "📁" : "📄";
                var sizeStr = entry.IsDir ? "" : FormatSize(entry.Size);
                var dateStr = entry.Modified.ToString("MM-dd HH:mm");
                var prefix = sel ? "▶ " : "  ";

                int rowFg = 37, rowBg = 0;
                if (sel) { rowFg = 30; rowBg = 46; }
                else if (entry.IsDir) rowFg = 34;

                sb.Append(sel ? AnsiTty.FgBg(rowFg, rowBg) :
                          entry.IsDir ? AnsiTty.Sgr(34, 0, 1) : "");

                var line = $"{prefix}{icon} {entry.Name}";
                // 右侧信息栏
                var infoStr = $"  {sizeStr,8}  {dateStr}";
                int nameMax = tw - 1 - VW(prefix + icon + " ") - VW(infoStr);
                if (nameMax < 10) nameMax = 10;
                var name = TruncateByVW(entry.Name, nameMax);
                line = $"{prefix}{icon} {name}{infoStr}";
                line = TruncateByVW(line, tw - 1);

                sb.Append(line).Append(AnsiTty.SgrReset);
            }

            // 帮助栏
            int helpRow = listTop + visibleItems;
            sb.Append(AnsiTty.CursorPos(helpRow, 1))
              .Append(AnsiTty.FgBg(30, 47))
              .Append("[↑/↓] 导航  [Enter] 选择/打开  [Backspace] 上级目录  [ESC] 取消  [字母] 搜索")
              .Append(new string(' ', Math.Max(0, tw - 60)))
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
}
