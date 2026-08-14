using System.Text;
using WayCoder.Terminal;

namespace WayCoder.UI;

/// <summary>
/// 会话管理器对话框 —— 对标 Crush sessions.go。
/// 居中带边框对话框（非全屏），浏览/切换/重命名/删除历史会话。
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

    private const int MinW = 68, MinH = 17;
    private const int FrameH = 9; // 顶框1+标题1+统计1+搜索1+上分隔1 + 下分隔1+提示1+帮助1+底框1

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

            // 重命名模式：顶部多一行「新名称」输入行
            int extraTop = (mode == Mode.Renaming && selectedIdx < filtered.Count) ? 1 : 0;

            var (bx, by, dw, dh, innerW) = DialogFrame.Layout(MinW, MinH);
            int listH = Math.Max(3, dh - FrameH - extraTop);

            selectedIdx = Math.Clamp(selectedIdx, 0, Math.Max(0, filtered.Count - 1));

            // 滚动调整
            if (selectedIdx < scrollOffset) scrollOffset = selectedIdx;
            if (selectedIdx >= scrollOffset + listH) scrollOffset = selectedIdx - listH + 1;
            scrollOffset = Math.Clamp(scrollOffset, 0, Math.Max(0, filtered.Count - listH));

            // ── 渲染 ──
            var sb = new StringBuilder();
            sb.Append(AnsiTty.CursorHide);
            DialogFrame.DimArea(sb, bx, by, dw, dh);
            DialogFrame.TopBorder(sb, by, bx, dw);

            // 标题行
            var modeLabel = mode switch
            {
                Mode.Normal => "",
                Mode.Renaming => " — 重命名",
                Mode.Deleting => " — 确认删除",
                _ => ""
            };
            var title = mode == Mode.Deleting ? "⚠ 确认删除会话" : $"会话管理{modeLabel}";
            int titleBg = mode == Mode.Deleting ? TuiColors.BgRed : DialogFrame.DimBg;

            int y = by + 1;
            DialogFrame.SideL(sb, y, bx);
            sb.Append(AnsiTty.CursorPos(y, bx + 2))
              .Append(AnsiTty.FgBgCode(TuiColors.White, titleBg))
              .Append(AnsiTty.SgrBold).Append(TruncateByVW(title, innerW - 4)).Append(AnsiTty.SgrReset);
            DialogFrame.SideR(sb, y, bx, dw);

            // 统计行
            y = by + 2;
            DialogFrame.SideL(sb, y, bx);
            sb.Append(AnsiTty.CursorPos(y, bx + 2))
              .Append(AnsiTty.FgBgCode(TuiColors.Blue, DialogFrame.DimBg));
            var stats = $"{sessions.Count} 个历史会话" + (currentSessionId != null ? "  ← 当前标记 ✓" : "");
            sb.Append(TruncateByVW(stats, innerW - 4)).Append(AnsiTty.SgrReset);
            DialogFrame.SideR(sb, y, bx, dw);

            // 搜索行
            y = by + 3;
            DialogFrame.SideL(sb, y, bx);
            sb.Append(AnsiTty.CursorPos(y, bx + 2))
              .Append(AnsiTty.FgBgCode(TuiColors.White, DialogFrame.DimBg));
            var searchPrompt = "搜索: ";
            var searchText = filter.Length > 0 ? filter : "输入关键词过滤...";
            var searchStyle = filter.Length > 0 ? "" : AnsiTty.SgrDim;
            sb.Append(searchPrompt).Append(searchStyle).Append(TruncateByVW(searchText, innerW - 4 - VW(searchPrompt)))
              .Append(AnsiTty.SgrReset);
            DialogFrame.SideR(sb, y, bx, dw);

            // 上分隔线
            y = by + 4;
            DialogFrame.SepLine(sb, y, bx, dw);

            // 重命名输入行（列表上方）
            int listTop = by + 5;
            if (extraTop == 1)
            {
                DialogFrame.SideL(sb, by + 5, bx);
                DialogFrame.FillInner(sb, by + 5, bx, innerW, TuiColors.Black, TuiColors.BgCyan);
                var renameLine = $"  新名称: {renameBuffer}";
                if (DateTime.Now.Millisecond % 1000 < 500) renameLine += '▌';
                sb.Append(AnsiTty.CursorPos(by + 5, bx + 2))
                  .Append(AnsiTty.FgBgCode(TuiColors.Black, TuiColors.BgCyan))
                  .Append(TruncateByVW(renameLine, innerW - 4))
                  .Append(AnsiTty.SgrReset);
                DialogFrame.SideR(sb, by + 5, bx, dw);
                listTop++;
            }

            // 会话列表
            for (int i = 0; i < listH; i++)
            {
                int si = scrollOffset + i, row = listTop + i;
                DialogFrame.SideL(sb, row, bx);

                if (si >= filtered.Count)
                {
                    DialogFrame.FillInner(sb, row, bx, innerW, TuiColors.White, DialogFrame.DimBg);
                    DialogFrame.SideR(sb, row, bx, dw);
                    continue;
                }

                var session = filtered[si];
                bool isSelected = si == selectedIdx;
                bool isCurrent = session.Id == currentSessionId;

                int bg = isSelected
                    ? (mode == Mode.Deleting ? TuiColors.BgRed : TuiColors.BgBlue)
                    : DialogFrame.DimBg;
                int fg = isSelected ? TuiColors.Black : (isCurrent ? TuiColors.Blue : TuiColors.White);
                DialogFrame.FillInner(sb, row, bx, innerW, fg, bg);

                var prefix = isSelected ? "▶ " : "  ";
                var check = isCurrent ? " ✓" : "  ";
                var timeStr = FormatRelativeTime(session.SavedAt);
                var line = $"{prefix}{session.Id}  {timeStr}  [{session.Model}]{check}";
                line = TruncateByVW(line, innerW - 2);

                sb.Append(AnsiTty.CursorPos(row, bx + 2))
                  .Append(AnsiTty.FgBgCode(fg, bg))
                  .Append(line)
                  .Append(AnsiTty.SgrReset);

                DialogFrame.SideR(sb, row, bx, dw);
            }

            // 下分隔线
            int sep2 = listTop + listH;
            DialogFrame.SepLine(sb, sep2, bx, dw);

            // 模式提示行
            int hintRow = sep2 + 1;
            DialogFrame.SideL(sb, hintRow, bx);
            var modeText = mode switch
            {
                Mode.Normal => "",
                Mode.Renaming => "✏ 输入新名称，Enter 确认，Esc 取消",
                Mode.Deleting => "⚠ 确认删除此会话？[Y] 确认删除  [N] 取消",
                _ => ""
            };
            if (mode == Mode.Normal)
            {
                DialogFrame.FillInner(sb, hintRow, bx, innerW, TuiColors.White, DialogFrame.DimBg);
            }
            else
            {
                int hFg = mode == Mode.Deleting ? TuiColors.White : TuiColors.Black;
                int hBg = mode == Mode.Deleting ? TuiColors.BgRed : TuiColors.BgCyan;
                DialogFrame.FillInner(sb, hintRow, bx, innerW, hFg, hBg);
                sb.Append(AnsiTty.CursorPos(hintRow, bx + 2))
                  .Append(AnsiTty.FgBgCode(hFg, hBg))
                  .Append(TruncateByVW(modeText, innerW - 4))
                  .Append(AnsiTty.SgrReset);
            }
            DialogFrame.SideR(sb, hintRow, bx, dw);

            // 帮助行
            int helpRow = sep2 + 2;
            DialogFrame.SideL(sb, helpRow, bx);
            var helpText = mode switch
            {
                Mode.Normal => "[↑/↓] 导航  [Enter] 切换到此会话  [R] 重命名  [Del] 删除  [Esc] 关闭",
                Mode.Renaming => "[Enter] 确认重命名  [Esc] 取消  [←→] 移动光标  [Backspace] 删除",
                Mode.Deleting => "[Y] 确认删除  [N] / [Esc] 取消",
                _ => ""
            };
            sb.Append(AnsiTty.CursorPos(helpRow, bx + 2))
              .Append(AnsiTty.FgBgCode(TuiColors.BrightBlack, DialogFrame.DimBg))
              .Append(TruncateByVW(helpText, innerW - 4));
            DialogFrame.SideR(sb, helpRow, bx, dw);

            // 底框
            DialogFrame.BottomBorder(sb, helpRow + 1, bx, dw);

            sb.Append(AnsiTty.SgrReset);
            Console.Write(sb.ToString());

            // ── 输入 ──
            var key = Console.ReadKey(intercept: true);

            switch (mode)
            {
                case Mode.Normal:
                    HandleNormalKey(key, ref selectedIdx, ref filter, filtered, listH,
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

            // Esc 在 Normal 模式：取消退出
            if (mode == Mode.Normal && key.Key == ConsoleKey.Escape)
                return null;

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
            Console.Write(AnsiTty.CursorShow);
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
