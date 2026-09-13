using System.Text;
using WayCoder.UI.Shared.Terminal;
using WayCoder.UI.Tui.Edit;

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
        var syntax = SyntaxFor(filePath);
        sb.Append("«bright green»").Append(filePath).Append(" · ").Append(total).Append(" 行«/»\n");
        for (int i = 0; i < count; i++)
            sb.Append("«bright green»").Append($"{i + 1,4} +«/»").Append(Colorize(lines[i], syntax)).Append('\n');
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
        var syntax = SyntaxFor(filePath);
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
                    // 三种行都是「行号/标记用 diff 语义色 + 代码按语法上色」，区别只在有没有背景：
                    //   +/- 行铺暗绿/暗红底（对标 Claude Code / Crush 的 diff，扫读时一眼分得清增删）
                    //   上下文行无底色（未改动的行不该抢眼），但**代码同样上语法色**
                    // 嵌套写法：先开 bg（外层），内层各段 fg 自己开合，行尾再关 bg —— «» 是按栈配对的
                    case '+':
                        sb.Append(AddedBg).Append("«bright green»").Append($"{l.NewLine,4} +«/»")
                          .Append(Colorize(l.Text, syntax)).Append("«/»\n");
                        break;
                    case '-':
                        sb.Append(RemovedBg).Append("«bright red»").Append($"{l.OldLine,4} -«/»")
                          .Append(Colorize(l.Text, syntax)).Append("«/»\n");
                        break;
                    default: // 上下文
                        sb.Append("«grey»").Append($"{l.OldLine,4}  «/»")
                          .Append(Colorize(l.Text, syntax)).Append('\n');
                        break;
                }
            }
        }
        return sb.ToString().TrimEnd('\n');
    }

    /// <summary>增/删行的整行底色（暗绿 / 暗红）—— 对标 Claude Code 与 Crush 的 diff 观感</summary>
    private const string AddedBg = "«bg:#0e2a17»";
    private const string RemovedBg = "«bg:#2c1417»";

    /// <summary>
    /// 按文件后缀取语法定义（认不出返回 null）。
    /// 工具输出就是靠这个拿到语言的：write / edit 贴出来的代码**没有语言标注**，
    /// 但**文件路径一定有** —— 按扩展名判比内容启发式（<see cref="Syntax.Detect"/>）准得多。
    /// </summary>
    private static Syntax? SyntaxFor(string filePath)
    {
        var s = Syntax.ForFile(filePath);
        return s.Keywords.Count == 0 ? null : s; // Plain（.txt / .md / 未知扩展名）关键字表为空 → 不上色
    }

    /// <summary>
    /// 代码按语法上色，产出 «fg:#rrggbb» 分段；行号与 +/- 标记由调用方另加（保持 diff 语义）。
    ///
    /// 两个刻意的不作为：
    /// ① 行内含 «» 字面量时整行不上色 —— 标记语法没有转义机制，硬塞会把解析器带偏；
    /// ② 非 256 色值（标准 16 色 / 样式码）不上色 —— Syntax 只用 256 色，
    ///    遇到别的值说明来路不对，宁可不色也别错色。
    /// </summary>
    private static string Colorize(string code, Syntax? syntax)
    {
        if (syntax == null || code.Length == 0) return code;
        if (code.Contains('«') || code.Contains('»')) return code;

        var sb = new StringBuilder(code.Length + 16);
        foreach (var (text, color) in syntax.Tokenize(code))
        {
            if (color is < 16 or > 255) { sb.Append(text); continue; } // 0 = 默认前景（含样式码）
            sb.Append("«fg:").Append(AnsiTty.Xterm256ToHex(color)).Append('»').Append(text).Append("«/»");
        }
        return sb.ToString();
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
