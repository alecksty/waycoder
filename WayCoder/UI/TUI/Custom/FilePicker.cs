using System.Text;
using WayCoder.UI.Shared.Terminal;
using WayCoder.UI.Tui.Controls;
using WayCoder.UI.Shared;
using WayCoder.UI.TUI;
using WayCoder.UI.TUI.Base;

namespace WayCoder.UI.Tui;

/// <summary>
/// 文件选择对话框 —— 对标 Crush filepicker。
/// 居中带边框对话框（非全屏），目录浏览、文件过滤、⇅导航、Enter 确认、Esc 取消。
///
/// 功能：
///   - 目录浏览（Enter 进入子目录 / ".." 上级；Backspace 无搜索词时返回上级）
///   - 实时过滤（搜索框按名称模糊匹配）
///   - 多列列表（文件/大小/日期/时间，目录 📁、文件 📄）
///
/// 实现：TuiWindow（模态）+ TuiVBox + TuiLabel（路径）+ TuiHBox（搜索标签+输入）+ TuiTableList（列表），
/// 走 UxHelper.RenderWait 阻塞 → 事件桥接，不再自造 Console.ReadKey 循环。
/// 顺带修复旧实现「Backspace 删搜索词 vs 返回上级目录」的冲突（仅无搜索词时返回上级）。
/// </summary>
public static class FilePicker
{
    /// <summary>
    /// 文件列表项
    /// </summary>
    /// <param name="Name">文件名</param>
    /// <param name="FullPath">完整路径</param>
    /// <param name="IsDir">是否为目录</param>
    /// <param name="Size">文件大小</param>
    /// <param name="Modified">最后修改时间</param>
    public record FileEntry(string Name, string FullPath, bool IsDir, long Size, DateTime Modified);

    private const int MinW = 70, MaxW = 100;
    private const int ListH = 10; // 列表可见行数（含 2 行列头 + 分隔线）

    // 右侧三列固定宽度（大小右对齐 / 日期 / 时间）
    private const int SizeW = 8, DateW = 5, TimeW = 5;

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

        string? result = null;
        using var evt = new ManualResetEventSlim(false);
        try
        {
            var screen = TuiManager.Instance?.ActiveScreen;
            var win = BuildWindow(dir, pattern, title, screen, p =>
            {
                result = p;
                evt.Set();
            });
            screen?.ShowWindow(win);
            UxHelper.RenderWait(screen, evt, 30_000, win);
        }
        catch
        {
            evt.Set();
        }

        return result;
    }

    // ── 窗口构建 ──

    private static TuiWindow BuildWindow(string initialDir, string pattern, string title,
        TuiScreen? screen, Action<string?> onDone)
    {
        int winW = Math.Clamp(Tty.Cols - 4, MinW, MaxW);
        int listW = Math.Max(10, winW - 2); // 内容区宽（去左右边框）
        int nameW = Math.Max(8, listW - 1 - SizeW - DateW - TimeW); // 预留 1 列滚动条
        int winH = ListH + 5; // 上框 + 路径 + 搜索 + 列表 + 帮助 + 下框

        // 标记加载：结构/ids 来自 filepicker.tui（布局写标记），动态内容与事件 code-behind
        var res = TuiMarkup.LoadResource("dialogs/filepicker.tui");
        var win = res.Window ?? throw new InvalidOperationException("filepicker.tui 根应为 Dialog");
        win.Title = title;
        win.Width = winW; win.Height = winH;
        win.MinWidth = MinW; win.MinHeight = 10;
        win.WinBg = TuiTheme.Current.WindowBg;
        var g = TuiTheme.Current.DialogGradient; // 统一对话框渐变（与 TuiDialog 系一致）
        win.GradientBorder = true;
        win.GradientStart = g.start;
        win.GradientEnd = g.end;

        // 控件接线（结构在标记里，精确样式/列/数据/事件在此）
        var pathLabel = res.Find<TuiLabel>("path")!;
        var search = res.Find<TuiInput>("search")!;
        var table = res.Find<TuiTableList>("table")!;
        var help = res.Find<TuiLabel>("help")!;
        search.Fg = AnsiColors.White;
        search.Bg = AnsiColors.BgBlack;
        table.Height = ListH;
        table.AddColumn("文件", nameW);
        table.AddColumn("大小", SizeW);
        table.AddColumn("日期", DateW);
        table.AddColumn("时间", TimeW);

        // ── 状态 ──
        var dir = initialDir;
        var entries = LoadDir(dir, pattern);
        var filtered = new List<FileEntry>();

        // ── 刷新 / 动作 ──

        void Refresh()
        {
            filtered = string.IsNullOrEmpty(search.Text)
                ? entries
                : entries.Where(e =>
                    e.Name.Contains(search.Text, StringComparison.OrdinalIgnoreCase)).ToList();

            table.ClearRows();
            foreach (var e in filtered)
            {
                table.AddRow(
                    (e.IsDir ? "📁 " : "📄 ") + e.Name,
                    e.IsDir ? "" : FormatSize(e.Size).PadLeft(SizeW),
                    e.Modified != DateTime.MinValue ? e.Modified.ToString("MM-dd") : "",
                    e.Modified != DateTime.MinValue ? e.Modified.ToString("HH:mm") : "");
            }

            table.SelectedIndex = 0;
            table.ScrollOffset = 0;
            table.EnsureSelectedVisible();
            help.Text = $"[↑/↓] 导航  [Enter] 打开  [Backspace] 上级  [Esc] 取消  ·  {filtered.Count} 项";
            screen?.MarkDirty();
        }

        void ReloadDir(string newDir)
        {
            dir = newDir;
            entries = LoadDir(dir, pattern);
            pathLabel.Text = "📁 " + TruncLeftVW(dir, Math.Max(1, listW - 3));
            search.Text = "";
            Refresh();
        }

        void Finish(string? path)
        {
            onDone(path);
            win.OnClosed?.Invoke();
        }

        void Activate()
        {
            int idx = table.SelectedIndex;
            if (idx < 0 || idx >= filtered.Count) return;
            var entry = filtered[idx];
            if (entry.IsDir) ReloadDir(entry.FullPath);
            else Finish(entry.FullPath);
        }

        void GoParent()
        {
            var parent = Path.GetDirectoryName(dir);
            if (parent != null && Directory.Exists(parent))
                ReloadDir(parent);
        }

        // 搜索输入：字母进过滤词（OnTextChanged 实时过滤），↑↓ 导航列表，Enter 打开，Backspace 上级
        search.OnTextChanged = Refresh;
        search.KeyHook = key =>
        {
            switch (key.Key)
            {
                case ConsoleKey.UpArrow:
                case ConsoleKey.DownArrow:
                case ConsoleKey.Home:
                case ConsoleKey.End:
                case ConsoleKey.PageUp:
                case ConsoleKey.PageDown:
                    table.OnKey(key);
                    table.MarkDirty();
                    screen?.MarkDirty();
                    return true;
                case ConsoleKey.Enter:
                    Activate();
                    return true;
                case ConsoleKey.Backspace:
                    if (search.Text.Length == 0)
                    {
                        GoParent();
                        return true;
                    }

                    return false; // 有搜索词 → 交给输入框删除字符
            }

            return false;
        };
        table.OnSelect = _ => Activate(); // 若焦点切到列表，Enter 亦可打开

        win.RegisterShortcut(ConsoleKey.Escape, () => Finish(null));

        pathLabel.Text = "📁 " + TruncLeftVW(dir, Math.Max(1, listW - 3));
        Refresh();
        return win;
    }

    // ── 纯逻辑（AOT 安全，可自测）──

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
                catch
                {
                }
            }

            foreach (var f in Directory.GetFiles(dir, pattern))
            {
                try
                {
                    var fi = new FileInfo(f);
                    list.Add(new FileEntry(fi.Name, fi.FullName, false, fi.Length, fi.LastWriteTime));
                }
                catch
                {
                }
            }
        }
        catch
        {
        }

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

    /// <summary>超长时从左侧截断（保留末尾，用于路径显示）。</summary>
    private static string TruncLeftVW(string text, int maxVW)
    {
        if (string.IsNullOrEmpty(text)) return "";
        if (AnsiHelper.DisplayWidth(text) <= maxVW) return text;
        var runes = text.EnumerateRunes().ToList();
        int budget = maxVW - 1; // 预留 1 宽给 …
        var sb = new StringBuilder();
        for (int i = runes.Count - 1; i >= 0; i--)
        {
            int rw = AnsiHelper.RuneWidth(runes[i]);
            if (budget - rw < 0) break;
            budget -= rw;
            sb.Insert(0, runes[i].ToString());
        }

        return "…" + sb;
    }
}