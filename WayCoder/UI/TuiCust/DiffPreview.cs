using WayCoder.Terminal;
using WayCoder.UI.TuiScreens;

namespace WayCoder.UI;

/// <summary>
/// Diff 预览 + 逐 Hunk 确认对话框。
/// 对标竞品的 diff 确认流程 —— 写文件前让用户看到差异。
///
/// 模式：
///   AcceptAll  — 接受全部变更
///   RejectAll  — 拒绝全部变更
///   ReviewHunks — 逐 hunk 审查（y=接受此 hunk / n=跳过此 hunk / q=取消剩余）
/// </summary>
public static class DiffPreview
{
    /// <summary>
    /// 一个 diff hunk：上下文行 + 删除行 + 添加行。
    /// </summary>
    public class Hunk
    {
        public int OldStart, OldCount, NewStart, NewCount;
        public string Header = "";
        public List<HunkLine> Lines = [];
    }

    public class HunkLine
    {
        public char Kind;  // ' ' 上下文, '-' 删除, '+' 添加
        public string Text = "";
        public int OldLine, NewLine;
    }

    public enum Decision { AcceptAll, RejectAll, Partial }

    /// <summary>
    /// 显示 diff 预览并返回决策。
    /// oldContent = 原始文件内容, newContent = 修改后内容, filePath = 文件名。
    /// 返回：(决策, 被接受的 hunk 索引集合)
    /// </summary>
    public static (Decision Decision, HashSet<int>? AcceptedHunks) Show(
        string oldContent, string newContent, string filePath)
    {
        var hunks = BuildHunks(oldContent, newContent);

        // 无实际变更 → 直接放行
        if (hunks.Count == 0 || hunks.All(h => h.Lines.All(l => l.Kind == ' ')))
            return (Decision.AcceptAll, null);

        return TuiManager.Instance.ActiveScreen is ChatScreen
            ? ShowFullScreen(oldContent, newContent, filePath, hunks)
            : ShowFallback(oldContent, newContent, filePath);
    }

    // ================================================================
    // 全屏交互模式
    // ================================================================

    private static (Decision, HashSet<int>?) ShowFullScreen(
        string oldContent, string newContent, string filePath, List<Hunk> hunks)
    {
        var accepted = new HashSet<int>();
        int currentHunk = 0;
        int scrollOffset = 0;
        var mode = "review"; // "review" | "all"
        var syntax = GetSyntaxForFile(filePath);

        while (true)
        {
            bool isHunkAccepted = accepted.Contains(currentHunk);
            var (tw, th) = (Tty.Cols, Tty.Rows);
            var statusH = 2;
            var contentH = Math.Max(5, th - statusH);

            // 计算当前 hunk 的可见行范围
            var allLines = new List<(int hunkIdx, HunkLine line)>();
            foreach (var h in hunks)
            {
                int hi = hunks.IndexOf(h);
                // hunk 分隔线
                if (allLines.Count > 0) allLines.Add((-1, new HunkLine { Kind = ' ', Text = "" }));
                allLines.Add((-2, new HunkLine { Kind = '@', Text = h.Header }));
                foreach (var l in h.Lines)
                    allLines.Add((hi, l));
            }

            // 判断是否使用分屏模式（宽度 >= 120 列时自动切换）
            var useSplitMode = tw >= 120;
            var splitRows = useSplitMode ? BuildSplitRows(hunks, tw) : null;
            var totalVisualLines = useSplitMode ? splitRows!.Count : allLines.Count;

            // 自动滚动到当前 hunk
            int currentLine = 0;
            if (useSplitMode)
            {
                for (int i = 0; i < splitRows!.Count; i++)
                {
                    if (splitRows[i].HunkIdx == currentHunk && !splitRows[i].IsHeader)
                    { currentLine = i; break; }
                }
            }
            else
            {
                for (int i = 0; i < allLines.Count; i++)
                {
                    if (allLines[i].hunkIdx == currentHunk && allLines[i].line.Kind != '@')
                    { currentLine = i; break; }
                }
            }
            scrollOffset = Math.Clamp(scrollOffset, 0, Math.Max(0, totalVisualLines - contentH));
            if (currentLine < scrollOffset) scrollOffset = currentLine;
            if (currentLine >= scrollOffset + contentH) scrollOffset = currentLine - contentH + 1;
            scrollOffset = Math.Clamp(scrollOffset, 0, Math.Max(0, totalVisualLines - contentH));

            // 渲染
            var sb = new System.Text.StringBuilder();
            sb.Append(AnsiTty.CursorHide).Append(AnsiTty.Home);

            // 标题栏
            var title = useSplitMode
                ? $"Diff 预览 (分屏): {filePath}  ({hunks.Count} hunks)"
                : $"Diff 预览: {filePath}  ({hunks.Count} hunks)";
            var titleBg = TuiColors.BgBlue; // 蓝底
            sb.Append(AnsiTty.FgBg(30, titleBg));
            sb.Append(title);
            sb.Append(new string(' ', Math.Max(0, tw - VW(title))));
            sb.Append(AnsiTty.SgrReset).Append('\n');

            // Diff 内容 — 根据终端宽度选择统一或分屏模式
            if (useSplitMode)
            {
                // ── 分屏模式（宽度 >= 120 列）──
                for (int i = 0; i < contentH - 1; i++)
                {
                    int li = scrollOffset + i;
                    sb.Append(AnsiTty.CursorPos(i + 2, 1)).Append(AnsiTty.ClearToEnd);
                    if (li >= splitRows!.Count) continue;
                    RenderSplitRow(sb, splitRows[li], tw, currentHunk, accepted, syntax);
                }
            }
            else
            {
                // ── 统一模式 ──
                for (int i = 0; i < contentH - 1; i++)
                {
                    int li = scrollOffset + i;
                    sb.Append(AnsiTty.CursorPos(i + 2, 1)).Append(AnsiTty.ClearToEnd);
                    if (li >= allLines.Count) continue;

                    var (hi, line) = allLines[li];

                    if (hi == -1)
                    {
                        sb.Append(AnsiTty.SgrDim);
                        sb.Append(new string('─', Math.Min(tw, 60)));
                        sb.Append(AnsiTty.SgrReset);
                    }
                    else if (hi == -2)
                    {
                        var hdr = TruncateByVW(line.Text, tw - 1);
                        sb.Append(AnsiTty.Fg(36)).Append(hdr).Append(AnsiTty.SgrReset);
                    }
                    else
                    {
                        bool isCurrentHunk = hi == currentHunk;
                        bool isAccepted = accepted.Contains(hi);

                        if (line.Kind == '-')
                        {
                            var prefix = $"{Padding(line.OldLine),4} -";
                            int fg = isCurrentHunk ? 30 : 37;
                            int bg = 41;
                            var maxTextW = tw - 7;
                            sb.Append(isCurrentHunk ? AnsiTty.Sgr(fg, bg, 1) : AnsiTty.FgBg(fg, bg));
                            sb.Append(prefix).Append(' ');
                            AppendHighlightedCode(sb, line.Text, syntax, fg, bg, isCurrentHunk, maxTextW);
                            sb.Append(AnsiTty.SgrReset);
                        }
                        else if (line.Kind == '+')
                        {
                            var prefix = "     +";
                            int fg = isCurrentHunk ? 30 : 37;
                            int bg = 42;
                            var maxTextW = tw - 7;
                            sb.Append(isCurrentHunk ? AnsiTty.Sgr(fg, bg, 1) : AnsiTty.FgBg(fg, bg));
                            sb.Append(prefix).Append(' ');
                            AppendHighlightedCode(sb, line.Text, syntax, fg, bg, isCurrentHunk, maxTextW);
                            sb.Append(AnsiTty.SgrReset);
                        }
                        else
                        {
                            var prefix = $"{Padding(line.OldLine),4}  ";
                            var maxTextW = tw - 7;
                            if (isCurrentHunk)
                            {
                                sb.Append(AnsiTty.FgBg(30, 46));
                                sb.Append(prefix).Append(' ');
                                AppendHighlightedCode(sb, line.Text, syntax, 30, 46, false, maxTextW);
                                sb.Append(AnsiTty.SgrReset);
                            }
                            else if (isAccepted)
                            {
                                sb.Append(AnsiTty.SgrDim);
                                sb.Append(prefix).Append(' ');
                                // 已接受的上下文行：不语法高亮，直接 dim
                                var t = TruncateByVW(line.Text, maxTextW);
                                sb.Append(t);
                                sb.Append(AnsiTty.SgrReset);
                            }
                            else
                            {
                                // 普通上下文行：语法高亮，无背景色
                                sb.Append(prefix).Append(' ');
                                AppendHighlightedCode(sb, line.Text, syntax, 37, 0, false, maxTextW);
                            }
                        }
                    }
                }
            }

            // 状态栏
            int statusRow = contentH + 1;
            sb.Append(AnsiTty.CursorPos(statusRow, 1)).Append(AnsiTty.FgBg(30, 47)); // 白底黑字

            if (mode == "all")
            {
                sb.Append(" 全部接受? [Y]是 [N]否  ");
            }
            else
            {
                var acceptedCount = accepted.Count;
                isHunkAccepted = accepted.Contains(currentHunk);
                sb.Append($" [{currentHunk + 1}/{hunks.Count}] ");
                sb.Append("[Y]接受 [N]跳过 [A]全接受 [Q]取消  ");
            }
            sb.Append(new string(' ', Math.Max(0, tw - 80)));
            sb.Append(AnsiTty.SgrReset);

            // 滚动指示器
            if (totalVisualLines > contentH)
            {
                var pct = totalVisualLines > 0 ? (int)((float)scrollOffset / (totalVisualLines - contentH) * 100) : 0;
                sb.Append(AnsiTty.CursorPos(statusRow, tw - 8)).Append(AnsiTty.FgBg(30, 47)).Append(pct).Append('%').Append(AnsiTty.SgrReset);
            }

            Console.Write(sb.ToString());

            // 读键
            var key = Console.ReadKey(intercept: true);

            if (mode == "all")
            {
                switch (key.Key)
                {
                    case ConsoleKey.Y: return (Decision.AcceptAll, null);
                    case ConsoleKey.N: case ConsoleKey.Escape: mode = "review"; break;
                }
                if (key.KeyChar == 'y' || key.KeyChar == 'Y') return (Decision.AcceptAll, null);
                if (key.KeyChar == 'n' || key.KeyChar == 'N') mode = "review";
                continue;
            }

            switch (key.Key)
            {
                case ConsoleKey.Y:
                    if (!isHunkAccepted) accepted.Add(currentHunk);
                    else accepted.Remove(currentHunk);
                    break;
                case ConsoleKey.N:
                    if (isHunkAccepted) accepted.Remove(currentHunk);
                    else currentHunk = Math.Min(hunks.Count - 1, currentHunk + 1);
                    break;
                case ConsoleKey.A:
                    mode = "all";
                    break;
                case ConsoleKey.Q: case ConsoleKey.Escape:
                    if (accepted.Count == 0) return (Decision.RejectAll, null);
                    return (Decision.Partial, accepted);
                case ConsoleKey.UpArrow: case ConsoleKey.K:
                    currentHunk = Math.Max(0, currentHunk - 1);
                    break;
                case ConsoleKey.DownArrow: case ConsoleKey.J:
                    currentHunk = Math.Min(hunks.Count - 1, currentHunk + 1);
                    break;
                case ConsoleKey.LeftArrow: case ConsoleKey.H:
                    scrollOffset = Math.Max(0, scrollOffset - 3);
                    break;
                case ConsoleKey.RightArrow: case ConsoleKey.L:
                    scrollOffset = Math.Min(Math.Max(0, allLines.Count - contentH), scrollOffset + 3);
                    break;
                case ConsoleKey.PageUp:
                    scrollOffset = Math.Max(0, scrollOffset - contentH);
                    break;
                case ConsoleKey.PageDown:
                    scrollOffset = Math.Min(Math.Max(0, allLines.Count - contentH), scrollOffset + contentH);
                    break;
                case ConsoleKey.Enter:
                    if (accepted.Count > 0) return (Decision.Partial, accepted);
                    break;
                default:
                    if (key.KeyChar == 'y' || key.KeyChar == 'Y')
                    { if (!isHunkAccepted) accepted.Add(currentHunk); else accepted.Remove(currentHunk); }
                    else if (key.KeyChar == 'n' || key.KeyChar == 'N')
                    { if (isHunkAccepted) accepted.Remove(currentHunk); else currentHunk = Math.Min(hunks.Count - 1, currentHunk + 1); }
                    else if (key.KeyChar == 'a' || key.KeyChar == 'A') mode = "all";
                    else if (key.KeyChar == 'q' || key.KeyChar == 'Q')
                    { if (accepted.Count == 0) return (Decision.RejectAll, null); return (Decision.Partial, accepted); }
                    break;
            }
        }
    }

    // ================================================================
    // 非全屏回退模式
    // ================================================================

    private static (Decision, HashSet<int>?) ShowFallback(
        string oldContent, string newContent, string filePath)
    {
        var diff = GenerateUnifiedDiff(oldContent, newContent, filePath);
        Console.WriteLine(AnsiText.Accent($"\n=== Diff 预览: {filePath} ==="));
        Console.WriteLine(diff);
        Console.WriteLine();

        var choice = UxHelper.Select("如何处理此变更？",
            ["全部接受 (Y)", "全部拒绝 (N)", "逐项审查 (R)"]);
        return choice switch
        {
            "全部接受 (Y)" => (Decision.AcceptAll, null),
            "全部拒绝 (N)" => (Decision.RejectAll, null),
            _ => (Decision.RejectAll, null), // TUI 回退不支持逐项
        };
    }

    // ================================================================
    // Hunk 构建
    // ================================================================

    /// <summary>
    /// 将旧/新内容拆分为 hunk 列表。
    /// 使用简单的 LCS 行级 diff。
    /// </summary>
    public static List<Hunk> BuildHunks(string oldContent, string newContent)
    {
        var oldLines = oldContent.Replace("\r\n", "\n").Split('\n');
        var newLines = newContent.Replace("\r\n", "\n").Split('\n');

        // 简单逐行比较，分组为 hunks
        var edits = ComputeLineEdits(oldLines, newLines);
        var hunks = GroupIntoHunks(edits, oldLines, newLines, contextLines: 3);
        return hunks;
    }

    private static List<(int OldIdx, int NewIdx, char Kind)> ComputeLineEdits(
        string[] oldL, string[] newL)
    {
        var result = new List<(int, int, char)>();

        // 使用简单的 Myers 式逐行比较
        int oi = 0, ni = 0;
        while (oi < oldL.Length || ni < newL.Length)
        {
            if (oi < oldL.Length && ni < newL.Length && oldL[oi] == newL[ni])
            {
                result.Add((oi, ni, ' '));
                oi++; ni++;
            }
            else
            {
                // 查找同步点
                int syncOld = -1, syncNew = -1;
                for (int so = oi; so < Math.Min(oi + 10, oldL.Length) && syncOld < 0; so++)
                {
                    for (int sn = ni; sn < Math.Min(ni + 10, newL.Length); sn++)
                    {
                        if (oldL[so] == newL[sn])
                        { syncOld = so; syncNew = sn; break; }
                    }
                }

                if (syncOld >= 0)
                {
                    // 删除行到同步点
                    while (oi < syncOld) { result.Add((oi, -1, '-')); oi++; }
                    // 添加行到同步点
                    while (ni < syncNew) { result.Add((-1, ni, '+')); ni++; }
                }
                else
                {
                    // 无同步点：剩余全部不同
                    if (oi < oldL.Length) { result.Add((oi, -1, '-')); oi++; }
                    else if (ni < newL.Length) { result.Add((-1, ni, '+')); ni++; }
                }
            }
        }
        return result;
    }

    private static List<Hunk> GroupIntoHunks(
        List<(int OldIdx, int NewIdx, char Kind)> edits,
        string[] oldL, string[] newL, int contextLines)
    {
        var hunks = new List<Hunk>();
        int i = 0;

        while (i < edits.Count)
        {
            // 跳过连续上下文
            while (i < edits.Count && edits[i].Kind == ' ') i++;
            if (i >= edits.Count) break;

            // 找到变更起始（含上下文）
            int hunkStart = Math.Max(0, i - contextLines);
            // 回退到最近的 hunk 边界避免重叠
            if (hunks.Count > 0)
            {
                var prevEnd = edits.FindLastIndex(e => e.Kind != ' ') + contextLines;
                // 简化：取最后一个变更行之后
            }

            int hunkEnd = i;
            while (hunkEnd < edits.Count && (edits[hunkEnd].Kind != ' ' || (hunkEnd < i + contextLines + 5 && hunkEnd < edits.Count - 1 && edits[hunkEnd + 1].Kind != ' ')))
                hunkEnd++;
            hunkEnd = Math.Min(edits.Count, hunkEnd + contextLines);

            // 确保 hunkEnd 的下一个不是变更行
            while (hunkEnd + 1 < edits.Count && edits[hunkEnd + 1].Kind != ' ') hunkEnd++;

            var hunk = new Hunk();
            int oldStart = edits[hunkStart].OldIdx >= 0 ? edits[hunkStart].OldIdx + 1 : 1;
            int newStart = edits[hunkStart].NewIdx >= 0 ? edits[hunkStart].NewIdx + 1 : 1;
            int oldCount = 0, newCount = 0;

            for (int j = hunkStart; j < Math.Min(hunkEnd, edits.Count); j++)
            {
                var (oi, ni, kind) = edits[j];
                var text = kind switch
                {
                    '-' => (oi >= 0 && oi < oldL.Length) ? oldL[oi] : "",
                    '+' => (ni >= 0 && ni < newL.Length) ? newL[ni] : "",
                    _ => (oi >= 0 && oi < oldL.Length) ? oldL[oi] : "",
                };
                int oldLineNo = oi >= 0 ? oi + 1 : 0;
                int newLineNo = ni >= 0 ? ni + 1 : 0;
                hunk.Lines.Add(new HunkLine { Kind = kind, Text = text, OldLine = oldLineNo, NewLine = newLineNo });
                if (kind == '-' || kind == ' ') oldCount++;
                if (kind == '+' || kind == ' ') newCount++;
            }

            hunk.OldStart = hunk.Lines.FirstOrDefault(l => l.OldLine > 0)?.OldLine ?? oldStart;
            hunk.NewStart = hunk.Lines.FirstOrDefault(l => l.NewLine > 0)?.NewLine ?? newStart;
            hunk.OldCount = oldCount;
            hunk.NewCount = newCount;
            hunk.Header = $"@@ -{hunk.OldStart},{hunk.OldCount} +{hunk.NewStart},{hunk.NewCount} @@";
            hunks.Add(hunk);

            i = hunkEnd;
        }

        return hunks;
    }

    // ================================================================
    // 统一 Diff 生成（回退 + 调试）
    // ================================================================

    public static string GenerateUnifiedDiff(string oldContent, string newContent, string filePath)
    {
        var oldLines = oldContent.Replace("\r\n", "\n").Split('\n');
        var newLines = newContent.Replace("\r\n", "\n").Split('\n');
        var sb = new System.Text.StringBuilder();

        sb.AppendLine($"--- a/{filePath}");
        sb.AppendLine($"+++ b/{filePath}");

        var hunks = BuildHunks(oldContent, newContent);
        foreach (var h in hunks)
        {
            sb.AppendLine(h.Header);
            foreach (var l in h.Lines)
                sb.AppendLine($"{l.Kind}{l.Text}");
        }

        var result = sb.ToString();
        if (result.Length > 3000)
            result = result[..2500] + "\n...（diff 已截断）\n";
        return result;
    }

    /// <summary>
    /// 将接受的 hunks 应用到旧内容，生成最终内容。
    /// 拒绝的 hunk 保留原行；接受的 hunk 应用删除/添加。
    /// 相邻 hunk 共享的上下文行只输出一次。
    /// </summary>
    public static string ApplyAccepted(string oldContent, List<Hunk> hunks, HashSet<int> accepted)
    {
        var oldLines = oldContent.Replace("\r\n", "\n").Split('\n');
        var result = new List<string>();
        int oldIdx = 0;

        foreach (var (h, hi) in hunks.Select((h, i) => (h, i)))
        {
            bool accept = accepted.Contains(hi);
            int hunkStart = h.Lines.Where(l => l.OldLine > 0).Min(l => l.OldLine) - 1;

            while (oldIdx < hunkStart && oldIdx < oldLines.Length)
                result.Add(oldLines[oldIdx++]);

            foreach (var l in h.Lines)
            {
                if (l.Kind == '-' || l.Kind == ' ')
                {
                    int lineIdx = l.OldLine - 1;
                    if (oldIdx > lineIdx) continue;
                    if (l.Kind == '-' && accept)
                    {
                        oldIdx++;
                    }
                    else
                    {
                        if (oldIdx < oldLines.Length) result.Add(oldLines[oldIdx]);
                        oldIdx++;
                    }
                }
                else if (accept)
                {
                    result.Add(l.Text);
                }
            }
        }

        while (oldIdx < oldLines.Length)
            result.Add(oldLines[oldIdx++]);

        return string.Join('\n', result);
    }

    // ================================================================
    // 分屏 Diff 渲染（终端宽度 >= 120 列时自动启用）
    // ================================================================

    /// <summary>
    /// 分屏模式的一行数据：左右各一段文本。
    /// </summary>
    private class SplitRow
    {
        public int HunkIdx;
        public string LeftText = "";   // 旧文件内容（删除行或上下文）
        public int LeftLineNo;
        public char LeftKind;          // '-' 或 ' '
        public string RightText = "";  // 新文件内容（添加行或上下文）
        public int RightLineNo;
        public char RightKind;         // '+' 或 ' '
        public bool IsHeader;          // hunk 头部
        public string HeaderText = "";
    }

    /// <summary>
    /// 将 hunk 列表转换为分屏行对（左旧右新）。
    /// 删除行显示在左侧、添加行显示在右侧、上下文行左右同时显示。
    /// </summary>
    private static List<SplitRow> BuildSplitRows(List<Hunk> hunks, int terminalWidth)
    {
        var rows = new List<SplitRow>();
        int panelWidth = (terminalWidth - 3) / 2; // 3 = " │ " 分隔符
        int textWidth = Math.Max(20, panelWidth - 6); // 6 = 行号(4) + 标记(1) + 空格(1)

        foreach (var (h, hi) in hunks.Select((h, i) => (h, i)))
        {
            // hunk 头
            rows.Add(new SplitRow { IsHeader = true, HeaderText = h.Header });

            // 将 hunk 内的行配对
            var adds = h.Lines.Where(l => l.Kind == '+').ToList();

            // 将删除行和添加行按顺序配对
            int ai = 0;
            var consumedAdds = new HashSet<int>();
            foreach (var line in h.Lines)
            {
                if (line.Kind == ' ')
                {
                    // 上下文行：左右同时显示
                    rows.Add(new SplitRow
                    {
                        HunkIdx = hi,
                        LeftText = TruncateByVW(line.Text, textWidth),
                        LeftLineNo = line.OldLine, LeftKind = ' ',
                        RightText = TruncateByVW(line.Text, textWidth),
                        RightLineNo = line.NewLine, RightKind = ' ',
                    });
                }
                else if (line.Kind == '-')
                {
                    // 删除行：左边显示，尝试配对一个添加行到右边
                    string? rightText = null;
                    int rightLine = 0;
                    while (ai < adds.Count && consumedAdds.Contains(ai))
                        ai++;
                    if (ai < adds.Count)
                    {
                        rightText = TruncateByVW(adds[ai].Text, textWidth);
                        rightLine = adds[ai].NewLine;
                        consumedAdds.Add(ai);
                        ai++;
                    }
                    rows.Add(new SplitRow
                    {
                        HunkIdx = hi,
                        LeftText = TruncateByVW(line.Text, textWidth),
                        LeftLineNo = line.OldLine, LeftKind = '-',
                        RightText = rightText ?? "",
                        RightLineNo = rightLine, RightKind = rightText != null ? '+' : ' ',
                    });
                }
            }
            // 处理未配对的添加行（右边显示，左留空）
            for (int i = 0; i < adds.Count; i++)
            {
                if (!consumedAdds.Contains(i))
                {
                    rows.Add(new SplitRow
                    {
                        HunkIdx = hi,
                        LeftText = "", LeftLineNo = 0, LeftKind = ' ',
                        RightText = TruncateByVW(adds[i].Text, textWidth),
                        RightLineNo = adds[i].NewLine, RightKind = '+',
                    });
                }
            }
        }
        return rows;
    }

    /// <summary>
    /// 渲染分屏模式的一行。
    /// 格式：lnno - 旧内容... │ lnno + 新内容...
    /// </summary>
    private static void RenderSplitRow(System.Text.StringBuilder sb, SplitRow row,
        int tw, int currentHunk, HashSet<int> accepted, Syntax? syntax)
    {
        int panelWidth = (tw - 3) / 2;

        if (row.IsHeader)
        {
            var hdr = TruncateByVW(row.HeaderText, tw - 1);
            sb.Append(AnsiTty.Fg(36)).Append(hdr).Append(AnsiTty.SgrReset);
            return;
        }

        bool isCurrentHunk = row.HunkIdx == currentHunk;
        bool isAccepted = accepted.Contains(row.HunkIdx);

        // ── 左面板 ──
        int leftFg, leftBg;
        bool leftBold = isCurrentHunk && row.LeftKind == '-';
        if (row.LeftKind == '-')
            { leftFg = isCurrentHunk ? 30 : 37; leftBg = 41; }
        else if (isCurrentHunk)
            { leftFg = 30; leftBg = 46; leftBold = false; }
        else
            { leftFg = 37; leftBg = 0; }

        var leftPrefix = row.LeftKind == '-'
            ? $"{Padding(row.LeftLineNo),4} -"
            : row.LeftText.Length > 0 ? $"{Padding(row.LeftLineNo),4}  " : "      ";

        if (isAccepted && !isCurrentHunk && row.LeftKind != '-')
        {
            // 已接受的上下文：dim 渲染，不高亮
            sb.Append(AnsiTty.SgrDim);
            var lc = leftPrefix + " " + row.LeftText;
            var lp = Math.Max(0, panelWidth - VW(lc));
            sb.Append(lc).Append(new string(' ', lp)).Append(AnsiTty.SgrReset);
        }
        else
        {
            sb.Append(leftBold ? AnsiTty.Sgr(leftFg, leftBg, 1) :
                     leftBg > 0 ? AnsiTty.FgBg(leftFg, leftBg) : "");
            sb.Append(leftPrefix).Append(' ');
            int maxCodeW = panelWidth - VW(leftPrefix) - 1;
            var leftCode = TruncateByVW(row.LeftText, maxCodeW);
            int codeVW = VW(leftCode);
            AppendHighlightedCode(sb, leftCode, syntax, leftFg, leftBg, leftBold, int.MaxValue);
            int leftPad = Math.Max(0, panelWidth - VW(leftPrefix) - 1 - codeVW);
            sb.Append(new string(' ', leftPad));
            sb.Append(AnsiTty.SgrReset);
        }

        // 分隔符
        sb.Append(AnsiTty.SgrDim).Append(" │ ").Append(AnsiTty.SgrReset);

        // ── 右面板 ──
        int rightFg, rightBg;
        bool rightBold = isCurrentHunk && row.RightKind == '+';
        if (row.RightKind == '+')
            { rightFg = isCurrentHunk ? 30 : 37; rightBg = 42; }
        else if (isCurrentHunk)
            { rightFg = 30; rightBg = 46; rightBold = false; }
        else
            { rightFg = 37; rightBg = 0; }

        var rightPrefix = row.RightKind == '+'
            ? $"{Padding(row.RightLineNo),4} +"
            : row.RightText.Length > 0 ? $"{Padding(row.RightLineNo),4}  " : "      ";

        if (isAccepted && !isCurrentHunk && row.RightKind != '+')
        {
            sb.Append(AnsiTty.SgrDim);
            sb.Append(rightPrefix).Append(' ').Append(row.RightText);
            sb.Append(AnsiTty.SgrReset);
        }
        else
        {
            sb.Append(rightBold ? AnsiTty.Sgr(rightFg, rightBg, 1) :
                     rightBg > 0 ? AnsiTty.FgBg(rightFg, rightBg) : "");
            sb.Append(rightPrefix).Append(' ');
            int maxCodeW = panelWidth - VW(rightPrefix) - 1;
            var rightCode = TruncateByVW(row.RightText, maxCodeW);
            AppendHighlightedCode(sb, rightCode, syntax, rightFg, rightBg, rightBold, int.MaxValue);
            sb.Append(AnsiTty.SgrReset);
        }
    }

    // ================================================================
    // 工具方法
    // ================================================================

    // ================================================================
    // 语法高亮
    // ================================================================

    /// <summary>根据文件路径获取语法定义（缓存友好，一次 diff 仅调用一次）</summary>
    private static Syntax? GetSyntaxForFile(string filePath)
    {
        if (string.IsNullOrEmpty(filePath)) return null;
        try { return Syntax.ForFile(filePath); }
        catch { return null; }
    }

    /// <summary>
    /// 在 diff 背景色上渲染语法高亮代码。
    /// 每个 token 使用语法颜色作为前景色，diff 背景色作为背景色。
    /// </summary>
    private static void AppendHighlightedCode(System.Text.StringBuilder sb, string code,
        Syntax? syntax, int baseFg, int bgColor, bool bold, int maxWidth)
    {
        if (syntax == null || string.IsNullOrEmpty(code))
        {
            var t = TruncateByVW(code, maxWidth);
            if (bold) sb.Append(AnsiTty.Sgr(baseFg, bgColor, 1));
            else if (bgColor > 0) sb.Append(AnsiTty.FgBg(baseFg, bgColor));
            sb.Append(t);
            return;
        }

        var tokens = syntax.Tokenize(code);
        int remaining = maxWidth;
        foreach (var (text, tokFg) in tokens)
        {
            if (remaining <= 0) break;
            var t = TruncateByVW(text, remaining);
            if (t.Length == 0) continue;
            remaining -= VW(t);

            int fg = tokFg > 0 ? tokFg : baseFg;
            if (bold)
                sb.Append(AnsiTty.Sgr(fg, bgColor, 1));
            else if (bgColor > 0)
                sb.Append(AnsiTty.FgBg(fg, bgColor));
            else
                sb.Append(AnsiTty.Fg(fg));
            sb.Append(t);
        }
    }

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
    private static string Padding(int n) => n > 0 ? n.ToString() : "";
}
