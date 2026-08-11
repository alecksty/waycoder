using System.Text;
using WayCoder.Terminal;

namespace WayCoder.UI;

/// <summary>
/// 会话管理器对话框 —— 对标 Crush sessions.go。
/// 全屏 ANSI 直写模式，浏览/切换/重命名/删除历史会话。
///
/// 功能：
///   - 列出所有历史会话（名称 + 模型 + 时间 + 消息预览）
///   - 三种模式：Normal（选择）、Renaming（重命名）、Deleting（确认删除）
///   - 实时搜索过滤
///   - Enter 切换会话 / R 重命名 / Del 删除
///   - 帮助栏随模式变化
/// </summary>
public static class SessionPicker
{
    /// <summary>选择结果</summary>
    public record Result(string Action, string SessionId, string? NewName = null)
    {
        public static Result SwitchTo(string id) => new("switch", id);
        public static Result Rename(string id, string newName) => new("rename", id, newName);
        public static Result Delete(string id) => new("delete", id);
    }

    /// <summary>操作模式</summary>
    private enum Mode { Normal, Renaming, Deleting }

    /// <summary>
    /// 显示会话管理对话框。返回操作结果，null = 取消。
    /// </summary>
    /// <param name="currentSessionId">当前会话 ID（用于标记 ✓）</param>
    public static Result? Show(string? currentSessionId = null)
    {
        var sessions = SessionManager.ListSessions(limit: 50);
        var filter = "";
        int selectedIdx = 0;
        int scrollOffset = 0;
        var mode = Mode.Normal;
        var renameBuffer = "";
        int renameCursorPos = 0;

        var (tw, th) = (Tty.Cols, Tty.Rows);

        // 找到当前会话索引
        if (currentSessionId != null)
        {
            for (int i = 0; i < sessions.Count; i++)
            {
                if (sessions[i].Id == currentSessionId)
                {
                    selectedIdx = i;
                    break;
                }
            }
        }

        try
        {
        while (true)
        {
            // 过滤
            var filtered = string.IsNullOrEmpty(filter)
                ? sessions
                : sessions.Where(s =>
                    s.Id.Contains(filter, StringComparison.OrdinalIgnoreCase) ||
                    s.Preview.Contains(filter, StringComparison.OrdinalIgnoreCase) ||
                    s.Model.Contains(filter, StringComparison.OrdinalIgnoreCase)).ToList();

            selectedIdx = Math.Clamp(selectedIdx, 0, Math.Max(0, filtered.Count - 1));

            // 可见行数
            int headerRows = 3; // 标题 + 统计 + 搜索
            int helpRows = 2;   // 模式提示 + 帮助栏
            int visibleItems = Math.Max(3, th - headerRows - helpRows - 2);

            // 滚动调整
            if (selectedIdx < scrollOffset) scrollOffset = selectedIdx;
            if (selectedIdx >= scrollOffset + visibleItems) scrollOffset = selectedIdx - visibleItems + 1;
            scrollOffset = Math.Clamp(scrollOffset, 0, Math.Max(0, filtered.Count - visibleItems));

            // ── 渲染 ──
            var sb = new StringBuilder();
            sb.Append(AnsiTty.CursorHide).Append(AnsiTty.Home);

            // 标题栏
            var modeLabel = mode switch
            {
                Mode.Normal => "",
                Mode.Renaming => " — 重命名",
                Mode.Deleting => " — 确认删除",
                _ => ""
            };
            var title = mode switch
            {
                Mode.Deleting => "⚠ 确认删除会话",
                _ => $"会话管理{modeLabel}"
            };

            int titleBg = mode == Mode.Deleting ? TuiColors.BgRed : TuiColors.BgBlue;
            sb.Append(AnsiTty.FgBg(37, titleBg));
            sb.Append($"  {title}  ");
            sb.Append(new string(' ', Math.Max(0, tw - VW(title) - 4)));
            sb.Append(AnsiTty.SgrReset).Append('\n');

            // 统计行
            sb.Append(AnsiTty.Fg(34)); // 蓝色
            var stats = $"  {sessions.Count} 个历史会话" + (currentSessionId != null ? "  ← 当前标记 ✓" : "");
            sb.Append(stats);
            sb.Append(new string(' ', Math.Max(0, tw - VW(stats))));
            sb.Append(AnsiTty.SgrReset).Append('\n');

            // 搜索栏
            sb.Append(AnsiTty.FgBg(30, 47));
            var searchPrompt = "搜索: ";
            var searchText = filter.Length > 0 ? filter : "输入关键词过滤...";
            var searchStyle = filter.Length > 0 ? "" : AnsiTty.SgrDim;
            sb.Append(searchPrompt).Append(searchStyle).Append(searchText).Append(AnsiTty.SgrReset);
            sb.Append(new string(' ', Math.Max(0, tw - VW(searchPrompt + searchText) - 2)));
            sb.Append(AnsiTty.SgrReset).Append('\n');

            // 会话列表
            int listTop = 4;

            // 重命名输入行（在列表上方）
            if (mode == Mode.Renaming && selectedIdx < filtered.Count)
            {
                sb.Append(AnsiTty.CursorPos(listTop, 1)).Append(AnsiTty.ClearToEnd);
                sb.Append(AnsiTty.FgBg(TuiColors.Black, TuiColors.BgCyan));
                sb.Append($"  新名称: {renameBuffer}");
                if (DateTime.Now.Millisecond % 1000 < 500) sb.Append('▌'); // 光标闪烁
                sb.Append(new string(' ', Math.Max(0, tw - VW($"  新名称: {renameBuffer}") - 2)));
                sb.Append(AnsiTty.SgrReset);
                listTop++;
                visibleItems--;
            }

            for (int i = 0; i < visibleItems; i++)
            {
                int si = scrollOffset + i;
                sb.Append(AnsiTty.CursorPos(listTop + i, 1)).Append(AnsiTty.ClearToEnd);

                if (si >= filtered.Count) continue;

                var session = filtered[si];
                bool isSelected = si == selectedIdx;
                bool isCurrent = session.Id == currentSessionId;

                var prefix = isSelected ? "▶ " : "  ";
                var check = isCurrent ? " ✓" : "  ";

                // 颜色
                if (mode == Mode.Deleting && isSelected)
                {
                    sb.Append(AnsiTty.FgBg(TuiColors.Black, TuiColors.BgRed));
                }
                else if (isSelected)
                {
                    sb.Append(AnsiTty.FgBg(TuiColors.Black, TuiColors.BgBlue));
                }
                else if (isCurrent)
                {
                    sb.Append(AnsiTty.Fg(34)); // 蓝色
                }

                // 时间格式化（相对时间）
                var timeStr = FormatRelativeTime(session.SavedAt);

                // 预览截断
                var preview = string.IsNullOrEmpty(session.Preview) ? "(空)" : session.Preview;
                if (preview.Length > 60) preview = preview[..60] + "…";

                var display = $"{prefix}{session.Id,-35} {timeStr,-14} [{session.Model}]{check}";
                display = TruncateByVW(display, tw - 1);
                sb.Append(display);

                // 第二行：预览
                if (isSelected && !string.IsNullOrEmpty(session.Preview))
                {
                    sb.Append('\n');
                    sb.Append(AnsiTty.CursorPos(listTop + i + 1, 1)).Append(AnsiTty.ClearToEnd);
                    sb.Append(isSelected
                        ? AnsiTty.FgBg(TuiColors.Black, TuiColors.BgBlue)
                        : "");
                    sb.Append($"     「{preview}」");
                    sb.Append(AnsiTty.SgrReset);
                }
            }

            // 模式提示行
            int modeRow = listTop + visibleItems + (mode == Mode.Renaming && selectedIdx < filtered.Count ? 1 : 0);
            sb.Append(AnsiTty.CursorPos(modeRow, 1));
            var modeText = mode switch
            {
                Mode.Normal => "",
                Mode.Renaming => "  ✏ 输入新名称，Enter 确认，Esc 取消",
                Mode.Deleting => "  ⚠ 确认删除此会话？[Y] 确认删除  [N] 取消",
                _ => ""
            };
            if (mode != Mode.Normal)
            {
                sb.Append(AnsiTty.FgBg(mode == Mode.Deleting ? TuiColors.White : TuiColors.Black,
                    mode == Mode.Deleting ? TuiColors.BgRed : TuiColors.BgCyan));
                sb.Append(modeText);
                sb.Append(new string(' ', Math.Max(0, tw - VW(modeText))));
                sb.Append(AnsiTty.SgrReset);
            }

            // 帮助栏
            int helpRow = mode == Mode.Normal ? modeRow : modeRow + 1;
            sb.Append(AnsiTty.CursorPos(helpRow, 1));
            sb.Append(AnsiTty.FgBg(30, 47));
            var helpText = mode switch
            {
                Mode.Normal => "[↑/↓] 导航  [Enter] 切换到此会话  [R] 重命名  [Del] 删除  [Esc] 关闭",
                Mode.Renaming => "[Enter] 确认重命名  [Esc] 取消  [←→] 移动光标  [Backspace] 删除",
                Mode.Deleting => "[Y] 确认删除  [N] / [Esc] 取消",
                _ => ""
            };
            sb.Append(helpText);
            sb.Append(new string(' ', Math.Max(0, tw - VW(helpText))));
            sb.Append(AnsiTty.SgrReset);

            // 滚动指示
            if (filtered.Count > visibleItems && mode == Mode.Normal)
            {
                var pct = filtered.Count > 1 ? scrollOffset * 100 / (filtered.Count - visibleItems) : 0;
                sb.Append(AnsiTty.CursorPos(helpRow, tw - 6))
                  .Append(AnsiTty.FgBg(30, 47))
                  .Append($"{pct}%")
                  .Append(AnsiTty.SgrReset);
            }

            Console.Write(sb.ToString());

            // ── 输入 ──
            var key = Console.ReadKey(intercept: true);

            switch (mode)
            {
                case Mode.Normal:
                    HandleNormalKey(key, ref selectedIdx, ref filter, filtered, visibleItems,
                        ref scrollOffset, ref mode, ref renameBuffer, ref renameCursorPos,
                        currentSessionId);
                    break;

                case Mode.Renaming:
                    HandleRenamingKey(key, ref mode, ref renameBuffer, ref renameCursorPos,
                        filtered, selectedIdx);
                    break;

                case Mode.Deleting:
                    var delResult = HandleDeletingKey(key, ref mode, filtered, selectedIdx);
                    if (delResult != null) return delResult;
                    break;
            }

            // Enter 在 Normal 模式：切换会话
            if (mode == Mode.Normal && key.Key == ConsoleKey.Enter && filtered.Count > 0
                && selectedIdx < filtered.Count)
            {
                return Result.SwitchTo(filtered[selectedIdx].Id);
            }
        }
        }
        finally
        {
            TuiManager.RequestFullRefresh();
        }
    }

    private static void HandleNormalKey(ConsoleKeyInfo key, ref int selectedIdx, ref string filter,
        List<SessionInfo> filtered, int visibleItems, ref int scrollOffset, ref Mode mode,
        ref string renameBuffer, ref int renameCursorPos, string? currentSessionId)
    {
        switch (key.Key)
        {
            case ConsoleKey.UpArrow:
                if (selectedIdx > 0) selectedIdx--;
                break;
            case ConsoleKey.DownArrow:
                if (selectedIdx < filtered.Count - 1) selectedIdx++;
                break;
            case ConsoleKey.Escape:
                // Exit handled in main loop
                break;
            case ConsoleKey.R:
                // 进入重命名模式（仅当有选中项且不是当前会话）
                if (filtered.Count > 0 && selectedIdx < filtered.Count)
                {
                    mode = Mode.Renaming;
                    renameBuffer = filtered[selectedIdx].Id;
                    renameCursorPos = renameBuffer.Length;
                }
                break;
            case ConsoleKey.Delete:
                // 进入删除确认模式（仅当有选中项且不是当前会话）
                if (filtered.Count > 0 && selectedIdx < filtered.Count
                    && filtered[selectedIdx].Id != currentSessionId)
                {
                    mode = Mode.Deleting;
                }
                break;
            case ConsoleKey.Backspace:
                if (filter.Length > 0)
                {
                    filter = filter[..^1];
                    selectedIdx = 0;
                }
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
            default:
                if (key.KeyChar >= ' ' && key.KeyChar <= '~')
                {
                    filter += key.KeyChar;
                    selectedIdx = 0;
                }
                break;
        }
    }

    private static void HandleRenamingKey(ConsoleKeyInfo key, ref Mode mode, ref string renameBuffer,
        ref int renameCursorPos, List<SessionInfo> filtered, int selectedIdx)
    {
        switch (key.Key)
        {
            case ConsoleKey.Escape:
                mode = Mode.Normal;
                break;
            case ConsoleKey.Enter:
                if (!string.IsNullOrWhiteSpace(renameBuffer) && filtered.Count > 0
                    && selectedIdx < filtered.Count)
                {
                    var oldId = filtered[selectedIdx].Id;
                    if (renameBuffer != oldId)
                    {
                        SessionManager.RenameSession(oldId, renameBuffer);
                    }
                }
                mode = Mode.Normal;
                break;
            case ConsoleKey.LeftArrow:
                if (renameCursorPos > 0) renameCursorPos--;
                break;
            case ConsoleKey.RightArrow:
                if (renameCursorPos < renameBuffer.Length) renameCursorPos++;
                break;
            case ConsoleKey.Home:
                renameCursorPos = 0;
                break;
            case ConsoleKey.End:
                renameCursorPos = renameBuffer.Length;
                break;
            case ConsoleKey.Backspace:
                if (renameCursorPos > 0)
                {
                    renameBuffer = renameBuffer[..(renameCursorPos - 1)] + renameBuffer[renameCursorPos..];
                    renameCursorPos--;
                }
                break;
            case ConsoleKey.Delete:
                if (renameCursorPos < renameBuffer.Length)
                {
                    renameBuffer = renameBuffer[..renameCursorPos] + renameBuffer[(renameCursorPos + 1)..];
                }
                break;
            default:
                if (key.KeyChar >= ' ' && key.KeyChar <= '~')
                {
                    renameBuffer = renameBuffer[..renameCursorPos] + key.KeyChar + renameBuffer[renameCursorPos..];
                    renameCursorPos++;
                }
                break;
        }
    }

    private static Result? HandleDeletingKey(ConsoleKeyInfo key, ref Mode mode,
        List<SessionInfo> filtered, int selectedIdx)
    {
        switch (key.Key)
        {
            case ConsoleKey.Y:
                if (filtered.Count > 0 && selectedIdx < filtered.Count)
                {
                    var id = filtered[selectedIdx].Id;
                    return Result.Delete(id);
                }
                break;
            case ConsoleKey.N:
            case ConsoleKey.Escape:
                mode = Mode.Normal;
                break;
        }
        return null;
    }

    // ── 工具 ──

    private static int VW(string text) => TuiHelper.DisplayWidth(text);

    private static string TruncateByVW(string text, int maxVW)
    {
        if (string.IsNullOrEmpty(text)) return "";
        int vw = 0, chars = 0;
        foreach (var rune in text.EnumerateRunes())
        {
            var w = TuiHelper.RuneWidth(rune);
            if (vw + w > maxVW) break;
            vw += w; chars += rune.Utf16SequenceLength;
        }
        return chars == text.Length ? text : text[..chars] + "…";
    }

    /// <summary>格式化相对时间</summary>
    private static string FormatRelativeTime(string savedAt)
    {
        if (!DateTime.TryParse(savedAt, out var dt))
            return savedAt.Length > 14 ? savedAt[..14] : savedAt;

        var diff = DateTime.Now - dt;

        if (diff.TotalSeconds < 60) return "刚刚";
        if (diff.TotalMinutes < 60) return $"{(int)diff.TotalMinutes} 分钟前";
        if (diff.TotalHours < 24) return $"{(int)diff.TotalHours} 小时前";
        if (diff.TotalDays < 7) return $"{(int)diff.TotalDays} 天前";
        if (diff.TotalDays < 30) return $"{(int)(diff.TotalDays / 7)} 周前";
        return dt.ToString("MM-dd HH:mm");
    }
}
