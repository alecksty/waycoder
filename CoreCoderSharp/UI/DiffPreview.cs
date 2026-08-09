using CoreCoderSharp.Terminal;

namespace CoreCoderSharp.UI;

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

        while (true)
        {
            bool isHunkAccepted = accepted.Contains(currentHunk);
            var (tw, th) = (TTY.Cols, TTY.Rows);
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

            // 自动滚动到当前 hunk
            int currentLine = 0;
            for (int i = 0; i < allLines.Count; i++)
            {
                if (allLines[i].hunkIdx == currentHunk && allLines[i].line.Kind != '@')
                { currentLine = i; break; }
            }
            scrollOffset = Math.Clamp(scrollOffset, 0, Math.Max(0, allLines.Count - contentH));
            if (currentLine < scrollOffset) scrollOffset = currentLine;
            if (currentLine >= scrollOffset + contentH) scrollOffset = currentLine - contentH + 1;
            scrollOffset = Math.Clamp(scrollOffset, 0, Math.Max(0, allLines.Count - contentH));

            // 渲染
            var sb = new System.Text.StringBuilder();
            sb.Append("\x1b[?25l\x1b[H");

            // 标题栏
            var title = $"Diff 预览: {filePath}  ({hunks.Count} hunks)";
            var titleBg = 44; // 蓝底
            sb.Append($"\x1b[30;{titleBg}m");
            sb.Append(title);
            sb.Append(new string(' ', Math.Max(0, tw - VW(title))));
            sb.Append("\x1b[0m\n");

            // Diff 内容
            for (int i = 0; i < contentH - 1; i++)
            {
                int li = scrollOffset + i;
                sb.Append($"\x1b[{i + 2};1H\x1b[K");

                if (li >= allLines.Count) continue;

                var (hi, line) = allLines[li];

                if (hi == -1)
                {
                    // hunk 间分隔
                    sb.Append($"\x1b[2m{new string('─', Math.Min(tw, 60))}\x1b[0m");
                }
                else if (hi == -2)
                {
                    // hunk 头
                    var hdr = TruncateByVW(line.Text, tw - 1);
                    sb.Append($"\x1b[36m{hdr}\x1b[0m");
                }
                else
                {
                    bool isCurrentHunk = hi == currentHunk;
                    bool isAccepted = accepted.Contains(hi);

                    string prefix, fgBg;
                    if (line.Kind == '-')
                    {
                        prefix = $"{Padding(line.OldLine),4} -";
                        fgBg = isCurrentHunk ? "\x1b[30;41;1m" : "\x1b[37;41m";
                    }
                    else if (line.Kind == '+')
                    {
                        prefix = $"     +";
                        fgBg = isCurrentHunk ? "\x1b[30;42;1m" : "\x1b[37;42m";
                    }
                    else
                    {
                        prefix = $"{Padding(line.OldLine),4}  ";
                        fgBg = isCurrentHunk ? "\x1b[30;46m" : (isAccepted ? "\x1b[2m" : "");
                    }

                    var maxTextW = tw - 7;
                    var text = TruncateByVW(line.Text, maxTextW);
                    sb.Append($"{fgBg}{prefix} {text}\x1b[0m");
                }
            }

            // 状态栏
            int statusRow = contentH + 1;
            sb.Append($"\x1b[{statusRow};1H\x1b[30;47m"); // 白底黑字

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
            sb.Append("\x1b[0m");

            // 滚动指示器
            if (allLines.Count > contentH)
            {
                var pct = allLines.Count > 0 ? (int)((float)scrollOffset / (allLines.Count - contentH) * 100) : 0;
                sb.Append($"\x1b[{statusRow};{tw - 8}H\x1b[30;47m{pct}%\x1b[0m");
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

        var choice = TuiList.Select("如何处理此变更？",
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
    // 工具方法
    // ================================================================

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
