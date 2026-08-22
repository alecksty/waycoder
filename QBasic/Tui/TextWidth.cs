// QBasic/Tui/TextWidth.cs
// CJK 字符宽度计算：CJK 字符占 2 列，其余占 1 列。
namespace QBasic.Tui;

public static class TextWidth
{
    // 判断一个码点是否为宽字符（中日韩全角、假名、谚文等）。
    public static bool IsWide(int codePoint)
    {
        if (codePoint < 0x1100) return false;

        // CJK 统一表意文字及其扩展
        if (codePoint >= 0x1100 && codePoint <= 0x115F) return true;   // 谚文字母
        if (codePoint >= 0x2E80 && codePoint <= 0xA4CF) return true;   // CJK 部首、注音、假名、谚文、统一表意
        if (codePoint >= 0xAC00 && codePoint <= 0xD7A3) return true;   // 谚文音节
        if (codePoint >= 0xF900 && codePoint <= 0xFAFF) return true;   // 兼容表意文字
        if (codePoint >= 0xFE30 && codePoint <= 0xFE4F) return true;   // 兼容形式
        if (codePoint >= 0xFF00 && codePoint <= 0xFF60) return true;   // 全角形式
        if (codePoint >= 0xFFE0 && codePoint <= 0xFFE6) return true;   // 全角符号
        if (codePoint >= 0x1F300 && codePoint <= 0x1F64F) return true; // emoji 等
        if (codePoint >= 0x1F900 && codePoint <= 0x1F9FF) return true;
        if (codePoint >= 0x20000 && codePoint <= 0x3FFFD) return true; // 扩展 B+

        return false;
    }

    // 计算字符串的显示宽度（按 Unicode 码点）。
    public static int Measure(string? text)
    {
        if (string.IsNullOrEmpty(text)) return 0;
        int width = 0;
        for (int i = 0; i < text.Length;)
        {
            int cp = char.ConvertToUtf32(text, i);
            width += IsWide(cp) ? 2 : 1;
            i += char.IsSurrogatePair(text, i) ? 2 : 1;
        }
        return width;
    }

    // 将字符串截断到指定显示宽度（超出部分省略号）。
    public static string Truncate(string text, int maxWidth)
    {
        if (Measure(text) <= maxWidth) return text;
        if (maxWidth <= 0) return "";
        if (maxWidth == 1) return "…";

        var sb = new System.Text.StringBuilder();
        int width = 0;
        for (int i = 0; i < text.Length;)
        {
            int cp = char.ConvertToUtf32(text, i);
            int w = IsWide(cp) ? 2 : 1;
            if (width + w > maxWidth - 1) break;
            sb.Append(char.ConvertFromUtf32(cp));
            width += w;
            i += char.IsSurrogatePair(text, i) ? 2 : 1;
        }
        sb.Append('…');
        return sb.ToString();
    }
}
