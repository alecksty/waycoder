using System.Text;

namespace WayCoder.UI.Shared;

/// <summary>
/// 行级 unified diff 引擎 —— **唯一实现**。
///
/// 放在 <c>UI/Shared/</c> 而不是 <c>UI/TUI/Custom/DiffPreview.cs</c> 里，是因为
/// **MAUI 工程排除了 <c>UI/TUI/**</c>**：引擎留在 TUI 下，`Tools/EditFileTool`、`Tools/MultiEditTool`
/// 在 MAUI 构建里就引用不到它，只能各抄一份（此前正是如此 —— 两份逐字拷贝的私有实现，
/// 且都**只找「首个差异行 → 末尾差异行」、只支持单块改动、无 @@ 头**，于是同一文件两处相隔较远的
/// 小改动会被呈现成「删掉中间全部 + 重新加」，误导模型）。
///
/// <see cref="WayCoder.UI.Tui.DiffPreview"/> 的 <c>BuildHunks</c>/<c>GenerateUnifiedDiff</c>
/// 现在都适配到本类，桌面与移动端共用同一份引擎。
/// </summary>
public static class UnifiedDiff
{
    /// <summary>一行变更。<paramref name="OldLine"/>/<paramref name="NewLine"/> 为 1-based，无对应行 = 0。</summary>
    public readonly record struct Line(char Kind, string Text, int OldLine, int NewLine);

    /// <summary>一个 hunk（连续变更块 + 前后各 3 行上下文）。</summary>
    public sealed class Block
    {
        public int OldStart, OldCount, NewStart, NewCount;
        public string Header = "";
        public List<Line> Lines = [];
    }

    /// <summary>把旧/新内容拆成 hunk 列表（行级 Myers 式比较 + 上下文分组）。</summary>
    public static List<Block> Build(string oldContent, string newContent, int contextLines = 3)
    {
        var oldLines = (oldContent ?? "").Replace("\r\n", "\n").Split('\n');
        var newLines = (newContent ?? "").Replace("\r\n", "\n").Split('\n');
        return Build(oldLines, newLines, contextLines);
    }

    /// <summary>同上，接受已切分的行数组。</summary>
    public static List<Block> Build(string[] oldLines, string[] newLines, int contextLines = 3)
    {
        var edits = ComputeLineEdits(oldLines, newLines);
        return GroupIntoHunks(edits, oldLines, newLines, contextLines);
    }

    /// <summary>渲染成 unified diff 文本（含 <c>--- a/</c> <c>+++ b/</c> 与 <c>@@</c> 头），并按既有约定截断。</summary>
    public static string Render(IReadOnlyList<Block> blocks, string filePath)
    {
        var sb = new StringBuilder();
        sb.AppendLine($"--- a/{filePath}");
        sb.AppendLine($"+++ b/{filePath}");
        foreach (var h in blocks)
        {
            sb.AppendLine(h.Header);
            foreach (var l in h.Lines)
                sb.AppendLine($"{l.Kind}{l.Text}");
        }

        var result = sb.ToString();
        if (result.Length > 3000)
            result = ContextManager.TruncateByRunes(result, 2500) + "\n...（diff 已截断）\n";
        return result;
    }

    /// <summary>一步到位的 unified diff 文本。</summary>
    public static string Generate(string oldContent, string newContent, string filePath)
        => Render(Build(oldContent, newContent), filePath);

    /// <summary>逐行比较，产出 (OldIdx, NewIdx, Kind) 序列；Kind: ' ' 上下文 / '-' 删除 / '+' 添加。</summary>
    public static List<(int OldIdx, int NewIdx, char Kind)> ComputeLineEdits(string[] oldL, string[] newL)
    {
        var result = new List<(int, int, char)>();

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
                // 在前后各 10 行窗口内找下一个同步点
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
                    while (oi < syncOld) { result.Add((oi, -1, '-')); oi++; }
                    while (ni < syncNew) { result.Add((-1, ni, '+')); ni++; }
                }
                else
                {
                    // 无同步点：逐个消费剩余行
                    if (oi < oldL.Length) { result.Add((oi, -1, '-')); oi++; }
                    else if (ni < newL.Length) { result.Add((-1, ni, '+')); ni++; }
                }
            }
        }
        return result;
    }

    /// <summary>把编辑序列按「变更块 + 前后上下文」分组为 hunk，重叠区间合并（相距 &lt; 2*context 并成一个）。</summary>
    public static List<Block> GroupIntoHunks(
        List<(int OldIdx, int NewIdx, char Kind)> edits,
        string[] oldL, string[] newL, int contextLines)
    {
        // 1. 连续非上下文行 = 变更块
        var blocks = new List<(int Start, int End)>();
        int bi = 0;
        while (bi < edits.Count)
        {
            while (bi < edits.Count && edits[bi].Kind == ' ') bi++;
            if (bi >= edits.Count) break;
            int s = bi;
            while (bi < edits.Count && edits[bi].Kind != ' ') bi++;
            blocks.Add((s, bi));
        }

        // 2. 每块前后扩 context 行
        var ranges = new List<(int S, int E)>();
        foreach (var (bs, be) in blocks)
            ranges.Add((Math.Max(0, bs - contextLines), Math.Min(edits.Count, be + contextLines)));

        // 3. 合并重叠区间（避免重复行 / 重叠 hunk）
        var merged = new List<(int S, int E)>();
        foreach (var (s, e) in ranges)
        {
            if (merged.Count > 0 && s <= merged[^1].E)
                merged[^1] = (merged[^1].S, Math.Max(merged[^1].E, e));
            else
                merged.Add((s, e));
        }

        // 4. 由区间构建 hunk
        var hunks = new List<Block>();
        foreach (var (hs, he) in merged)
        {
            var hunk = new Block();
            int oldCount = 0, newCount = 0;

            for (int j = hs; j < he; j++)
            {
                var (oi, ni, kind) = edits[j];
                var text = kind switch
                {
                    '-' => (oi >= 0 && oi < oldL.Length) ? oldL[oi] : "",
                    '+' => (ni >= 0 && ni < newL.Length) ? newL[ni] : "",
                    _ => (oi >= 0 && oi < oldL.Length) ? oldL[oi] : "",
                };
                hunk.Lines.Add(new Line(kind, text, oi >= 0 ? oi + 1 : 0, ni >= 0 ? ni + 1 : 0));
                if (kind is '-' or ' ') oldCount++;
                if (kind is '+' or ' ') newCount++;
            }

            // Line 是 record struct，FirstOrDefault 返回 default 而非 null —— 手工取首个有效行号
            int firstOld = 0, firstNew = 0;
            foreach (var l in hunk.Lines)
            {
                if (firstOld == 0 && l.OldLine > 0) firstOld = l.OldLine;
                if (firstNew == 0 && l.NewLine > 0) firstNew = l.NewLine;
            }
            hunk.OldStart = firstOld > 0 ? firstOld : 1;
            hunk.NewStart = firstNew > 0 ? firstNew : 1;
            hunk.OldCount = oldCount;
            hunk.NewCount = newCount;
            hunk.Header = $"@@ -{hunk.OldStart},{hunk.OldCount} +{hunk.NewStart},{hunk.NewCount} @@";
            hunks.Add(hunk);
        }

        return hunks;
    }
}
