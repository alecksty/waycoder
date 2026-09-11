namespace WayCoder.Maui.Markup;

/// <summary>
/// Markdown 表格的解析原语 —— **唯一实现**。
///
/// `MarkdownPreview` 与 `MarkupToFormattedString` 各写了一份 `IsTableSeparator` /
/// 单元格切分（一边叫 `SplitCells`、一边叫 `ParseTableRow`，**逐字相同**）。
/// 两处「目前一致」，但改一处必忘另一处 —— 表格渲染的判定分家会让「预览」与「聊天」两种呈现
/// 对同一段 markdown 给出不同结果。
/// </summary>
internal static class MarkdownTable
{
    /// <summary>`lines[idx]` 是否是表格的分隔行（`|---|:--:|`）。</summary>
    internal static bool IsSeparator(string[] lines, int idx)
    {
        if (idx < 0 || idx >= lines.Length) return false;
        var s = lines[idx].Trim();
        if (!s.StartsWith('|')) return false;
        foreach (var c in s)
            if (c is not ('|' or '-' or ':' or ' ' or '\t')) return false;
        return s.Contains('-');
    }

    /// <summary>按 `|` 切分一行并去掉首尾管道、逐格 trim。</summary>
    internal static string[] SplitRow(string line)
    {
        var s = line.Trim();
        if (s.StartsWith('|')) s = s[1..];
        if (s.EndsWith('|')) s = s[..^1];
        return s.Split('|').Select(c => c.Trim()).ToArray();
    }

    /// <summary>已切好的单元格是否整行都由分隔符字符构成（`MarkupToFormattedString` 侧的判据）。</summary>
    internal static bool IsSeparatorRow(string[] cells)
        => cells.Length > 0 && cells.All(c => c.All(ch => ch is '-' or ':' or ' ' or '\t'));
}
