using Microsoft.Maui.Controls;
using Microsoft.Maui.Graphics;
using WayCoder.UI.Shared;

namespace WayCoder.Maui.Markup;

/// <summary>
/// «» 中间格式 → MAUI 富文本（FormattedString）渲染器。
///
/// 复用主工程 UI/Shared 的 <see cref="MarkdownParser.ParseInline"/>（已被 MAUI 共享源码编译）
/// 解析 «tag»…«/» 与基础 markdown inline，得到 (text, color, bg) 三元组，再映射到 MAUI：
///   - color 1-9   → 样式（1=bold 2=dim 3=italic 4=underline 9=strikethrough）
///   - color ≥0x1000000 → 真彩 RGB（AnsiTty.RgbCode 编码，提取低 24 位）
///   - color 30-37/90-97/扩展色 → 命名色（对齐 Windows Terminal/VSCode 默认 16 色）
/// 颜色值同源主工程 AnsiColors / MarkdownParser，保证与 CLI/TUI/Web/GUI 四端观感一致。
/// </summary>
public static class MarkupToFormattedString
{
    private static readonly IReadOnlyDictionary<int, string> AnsiRgb = new Dictionary<int, string>
    {
        [30] = "#0C0C0C", [31] = "#C50F1F", [32] = "#13A10E", [33] = "#C19C00",
        [34] = "#0037DA", [35] = "#881798", [36] = "#3A96DD", [37] = "#CCCCCC",
        [40] = "#0C0C0C", [41] = "#C50F1F", [42] = "#13A10E", [43] = "#C19C00",
        [44] = "#0037DA", [45] = "#881798", [46] = "#3A96DD", [47] = "#CCCCCC",
        [90] = "#767676", [91] = "#E74856", [92] = "#16C60C", [93] = "#F9F1A5",
        [94] = "#3B78FF", [95] = "#B4009E", [96] = "#61D6D6", [97] = "#F2F2F2",
        [100] = "#767676", [101] = "#E74856", [102] = "#16C60C", [103] = "#F9F1A5",
        [104] = "#3B78FF", [105] = "#B4009E", [106] = "#61D6D6", [107] = "#F2F2F2",
        [208] = "#FF8700", [172] = "#D78700", [247] = "#9E9E9E",
    };

    private static readonly Color DarkDefault = Color.FromArgb("#E0E0E0");
    private static readonly Color LightDefault = Color.FromArgb("#1A1A1A");
    private static readonly Color DarkDim = Color.FromArgb("#888888");
    private static readonly Color LightDim = Color.FromArgb("#666666");

    /// <summary>把 «» 中间格式文本解析成 MAUI FormattedString（自适应深浅主题默认色）。</summary>
    public static FormattedString Convert(string? markup, bool isDark)
    {
        var fs = new FormattedString();
        if (string.IsNullOrEmpty(markup)) return fs;

        var defaultColor = isDark ? DarkDefault : LightDefault;
        var dimColor = isDark ? DarkDim : LightDim;

        foreach (var (text, color, bg) in MarkdownParser.ParseInline(markup))
        {
            var span = new Span { Text = text, TextColor = ResolveFg(color, defaultColor, dimColor) };

            switch (color)
            {
                case 1: span.FontAttributes = FontAttributes.Bold; break;        // bold/bright
                case 3: span.FontAttributes = FontAttributes.Italic; break;      // italic
                case 4: span.TextDecorations = TextDecorations.Underline; break;
                case 9: span.TextDecorations = TextDecorations.Strikethrough; break;
            }

            if (bg >= 30) span.BackgroundColor = ResolveColor(bg, Colors.Transparent);

            fs.Spans.Add(span);
        }
        return fs;
    }

    private static Color ResolveFg(int code, Color fallback, Color dim)
    {
        if (code == 2) return dim; // dim/faint
        return ResolveColor(code, fallback);
    }

    private static Color ResolveColor(int code, Color fallback)
    {
        if (code >= 0x1000000) // 真彩 RGB（AnsiTty.RgbCode = 0x1000000 | r<<16 | g<<8 | b）
            return Color.FromRgb((code >> 16) & 0xFF, (code >> 8) & 0xFF, code & 0xFF);
        if (code >= 30 && AnsiRgb.TryGetValue(code, out var hex))
            return Color.FromArgb(hex);
        return fallback; // 样式码 1-9 或未知 → 默认色
    }
}
