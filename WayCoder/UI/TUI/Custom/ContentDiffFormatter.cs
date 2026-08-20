using System.Text;

namespace WayCoder.UI.Tui;

/// <summary>
/// 写文件工具（write_file / edit_file / multiedit）写入内容在聊天区的内联 diff 展示格式化。
///
/// 对标 Claude Code 的 FileEditToolUpdatedMessage：摘要头行 + 结构化 diff（行号 + 标记 + 颜色）。
/// 生成的是 «» 中间格式文本（CLI/TUI→ANSI、Web→HTML），纯函数无 UI 副作用、无反射（AOT 安全）。
/// 由 Agent 在工具写盘成功后读回内容调用，仅走 onToolOutput 展示，不进入 LLM 上下文。
/// </summary>
public static class ContentDiffFormatter
{
    /// <summary>
    /// 新建/覆写文件 → 全量新增 diff。每行 «行号 +内容»（绿色），头行 «path · N 行»。
    /// write_file（非追加）、multiedit 创建后调用。内容即当前磁盘文件全文。
    /// </summary>
    public static string FormatAddedContent(string content, string filePath, int maxLines = 2000)
    {
        var lines = NormalizeLines(content);
        int total = lines.Length;
        if (total > 0 && lines[^1].Length == 0) total--; // 去掉结尾 \n 产生的空行
        int count = Math.Min(total, maxLines);

        var sb = new StringBuilder();
        sb.Append("«bright green»").Append(filePath).Append(" · ").Append(total).Append(" 行«/»\n");
        for (int i = 0; i < count; i++)
            sb.Append("«bright green»").Append($"{i + 1,4} +").Append(lines[i]).Append("«/»\n");
        AppendTruncated(sb, count);
        return sb.ToString().TrimEnd('\n');
    }

    /// <summary>
    /// 编辑已有文件 → 变更 diff。头行 «path · +N/-M 行»，hunk 头青色、
    /// + 行绿色、- 行红色、上下文灰色，均带行号。edit_file / multiedit 编辑、
    /// write_file 追加后调用；oldContent 为编辑前内容，newContent 为当前磁盘全文。
    /// </summary>
    public static string FormatEditContent(string oldContent, string newContent, string filePath, int maxLines = 2000)
    {
        var hunks = DiffPreview.BuildHunks(oldContent ?? "", newContent);
        int added = 0, removed = 0;
        foreach (var h in hunks)
            foreach (var l in h.Lines)
            {
                if (l.Kind == '+') added++;
                else if (l.Kind == '-') removed++;
            }

        var sb = new StringBuilder();
        sb.Append("«bright green»").Append(filePath).Append(" · +").Append(added).Append("/-").Append(removed).Append(" 行«/»\n");

        int shown = 0;
        foreach (var h in hunks)
        {
            sb.Append("«cyan»").Append(h.Header).Append("«/»\n");
            foreach (var l in h.Lines)
            {
                if (shown >= maxLines)
                {
                    sb.Append("«dim»…（内容过长，已截断）«/»");
                    return sb.ToString().TrimEnd('\n');
                }
                shown++;
                switch (l.Kind)
                {
                    case '+':
                        sb.Append("«bright green»").Append($"{l.NewLine,4} +").Append(l.Text).Append("«/»\n");
                        break;
                    case '-':
                        sb.Append("«bright red»").Append($"{l.OldLine,4} -").Append(l.Text).Append("«/»\n");
                        break;
                    default: // 上下文
                        sb.Append("«grey»").Append($"{l.OldLine,4}  ").Append(l.Text).Append("«/»\n");
                        break;
                }
            }
        }
        return sb.ToString().TrimEnd('\n');
    }

    /// <summary>拆行前归一化行尾（CRLF/CR→LF），否则行内 \r 会让终端光标跳行首花屏。</summary>
    private static string[] NormalizeLines(string content)
    {
        if (string.IsNullOrEmpty(content)) return [];
        return content.Replace("\r\n", "\n").Replace('\r', '\n').Split('\n');
    }

    private static void AppendTruncated(StringBuilder sb, int shown)
    {
        if (shown <= 0) return;
        sb.Append("«dim»…（内容过长，仅显示前 ").Append(shown).Append(" 行）«/»\n");
    }
}
