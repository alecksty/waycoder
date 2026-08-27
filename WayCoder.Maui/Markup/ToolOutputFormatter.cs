using Microsoft.Maui.Controls;
using Microsoft.Maui.Graphics;
using WayCoder.UI.Tui.Edit;

namespace WayCoder.Maui.Markup;

/// <summary>
/// 工具输出 → MAUI 富文本渲染器（聊天 read/write/edit/diff 代码片段语法高亮）。
///
/// 优先级对齐 TUI/Web 端（见 TuiMarkdown.RenderCodeBlock / app.js renderToolOutput）：
///   1. 含 «» 标记 → <see cref="MarkupToFormattedString.Convert"/> 解码（write/edit 的
///      ContentDiffFormatter diff 已用 «bright green» 等中间格式带色，必须先解码，
///      否则手机端裸显示「«bright green»」字面标签）；
///   2. diff 输出（首非空行以 ---/+++/@@/diff --git 开头）→ + 行绿 / - 行红 / @@ 青；
///   3. 代码（file_path 扩展名 → Syntax.ForFile，或 Syntax.Detect 启发式）→ 逐行 Tokenize 上色；
///   4. 兜底纯文本（Convert 无 «» 时原样返回）。
/// </summary>
public static class ToolOutputFormatter
{
    /// <summary>判断文本是否为 diff 输出（首个非空行以 diff 标记开头，移植 TUI IsDiffOutput）。</summary>
    public static bool IsDiffOutput(string content)
    {
        if (string.IsNullOrEmpty(content)) return false;
        foreach (var raw in content.Split('\n'))
        {
            var line = raw.TrimStart();
            if (line.Length == 0) continue;
            return line.StartsWith("---") || line.StartsWith("+++")
                || line.StartsWith("@@") || line.StartsWith("diff --git");
        }
        return false;
    }

    /// <summary>渲染工具输出为 FormattedString（按上述优先级）。超大内容降级纯文本，防主线程卡死。</summary>
    public static FormattedString Render(string? content, string? filePath, bool isDark)
    {
        var fs = new FormattedString();
        if (string.IsNullOrEmpty(content)) return fs;
        if (content.Length > 100_000) return MarkupToFormattedString.Convert(content, isDark);

        // 1) «» 中间格式（write/edit diff 已带色）直接解码
        if (content.Contains('«'))
            return MarkupToFormattedString.Convert(content, isDark);

        // 2) diff 红绿高亮
        if (IsDiffOutput(content))
            return RenderDiff(content, isDark);

        // 3) 代码语法高亮
        var syntax = ResolveSyntax(filePath, content);
        if (syntax != null && syntax.Name != "纯文本")
            return RenderCode(content, syntax, isDark);

        // 4) 纯文本（Convert 无 «» 时原样返回，含基础 inline markdown）
        return MarkupToFormattedString.Convert(content, isDark);
    }

    /// <summary>编辑器用：按文件扩展名渲染纯代码高亮（不做 diff/«» 检测，避免代码误判）。</summary>
    public static FormattedString RenderEditor(string content, string? filePath, bool isDark)
    {
        var syntax = ResolveSyntax(filePath, content);
        if (syntax != null && syntax.Name != "纯文本")
            return RenderCode(content, syntax, isDark);
        return MarkupToFormattedString.Convert(content, isDark);
    }

    /// <summary>从 file_path 扩展名或内容启发式解析语法。</summary>
    private static Syntax? ResolveSyntax(string? filePath, string content)
    {
        if (!string.IsNullOrEmpty(filePath))
        {
            var byFile = Syntax.ForFile(filePath);
            if (byFile.Name != "纯文本") return byFile;
        }
        return Syntax.Detect(content);
    }

    /// <summary>代码逐行 Tokenize 上色（相邻同色 token 合并 Span，防渲染卡死；等宽字体对齐）。</summary>
    private static FormattedString RenderCode(string content, Syntax syntax, bool isDark)
    {
        var fs = new FormattedString();
        var lines = content.Replace("\r\n", "\n").Split('\n');
        for (int i = 0; i < lines.Length; i++)
        {
            foreach (var (text, color) in syntax.Tokenize(lines[i]))
                MarkupToFormattedString.AppendSpan(fs, text, MarkupToFormattedString.ColorForToken(color, isDark), MarkupToFormattedString.MonoFont);
            if (i < lines.Length - 1)
                MarkupToFormattedString.AppendSpan(fs, "\n", MarkupToFormattedString.ColorForToken(0, isDark), MarkupToFormattedString.MonoFont);
        }
        return fs;
    }

    /// <summary>diff 逐行着色：+ 绿 / - 红 / @@ 青（等宽字体对齐）。</summary>
    private static FormattedString RenderDiff(string content, bool isDark)
    {
        var green = Color.FromArgb("#16C60C");
        var red = Color.FromArgb("#E74856");
        var cyan = Color.FromArgb("#3B78FF");

        var fs = new FormattedString();
        var lines = content.Replace("\r\n", "\n").Split('\n');
        for (int i = 0; i < lines.Length; i++)
        {
            var line = lines[i];
            var trimmed = line.TrimStart();
            Color? c = null;
            if (trimmed.StartsWith("@@")) c = cyan;
            else if (trimmed.StartsWith('+')) c = green;
            else if (trimmed.StartsWith('-')) c = red;

            MarkupToFormattedString.AppendSpan(fs, line, c ?? MarkupToFormattedString.ColorForToken(0, isDark), MarkupToFormattedString.MonoFont);
            if (i < lines.Length - 1)
                MarkupToFormattedString.AppendSpan(fs, "\n", MarkupToFormattedString.ColorForToken(0, isDark), MarkupToFormattedString.MonoFont);
        }
        return fs;
    }
}
