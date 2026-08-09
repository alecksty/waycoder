namespace CoreCoderSharp.Terminal;

/// <summary>
/// ANSI 字符串工具 —— 剥离/检测/截断 ANSI 转义序列。
/// 所有 ANSI 识别逻辑集中于此，不依赖 TuiHelper。
/// </summary>
public static class AnsiString
{
    public static bool ContainsAnsi(string text) => text.Contains('\x1b');

    public static string Strip(string text)
    {
        if (!ContainsAnsi(text)) return text;
        var sb = new System.Text.StringBuilder();
        var span = text.AsSpan();
        for (int i = 0; i < span.Length; i++)
        {
            if (span[i] == '\x1b' && i + 1 < span.Length && span[i + 1] == '[')
            {
                i += 2;
                while (i < span.Length && span[i] != 'm' && span[i] != 'H' && span[i] != 'J' && span[i] != 'K')
                    i++;
                continue;
            }
            sb.Append(span[i]);
        }
        return sb.ToString();
    }

    /// <summary>计算不含 ANSI 码的纯文本视觉宽度</summary>
    public static int DisplayWidth(string text)
    {
        var clean = Strip(text);
        var width = 0;
        foreach (var rune in clean.EnumerateRunes())
            width += CharWidth(rune);
        return width;
    }

    /// <summary>按视觉宽度截断文本（保留 ANSI 码）</summary>
    public static string TruncateByWidth(string text, int maxVw)
    {
        var clean = Strip(text);
        var cleanVw = 0;
        foreach (var r in clean.EnumerateRunes()) cleanVw += CharWidth(r);
        if (cleanVw <= maxVw) return text;

        var sb = new System.Text.StringBuilder();
        int vw = 0;
        for (int i = 0; i < text.Length && vw < maxVw; i++)
        {
            if (text[i] == '\x1b' && i + 1 < text.Length && text[i + 1] == '[')
            {
                int j = i;
                while (j < text.Length && text[j] != 'm') j++;
                sb.Append(text[i..(j + 1)]);
                i = j;
                continue;
            }
            var rune = System.Text.Rune.GetRuneAt(text, i);
            var w = CharWidth(rune);
            if (vw + w > maxVw) break;
            vw += w;
            sb.Append(text[i]);
        }
        sb.Append(AnsiTty.SgrReset);
        return sb.ToString();
    }

    public static string StripWithRegex(string text)
        => System.Text.RegularExpressions.Regex.Replace(text, @"\x1b\[[0-9;]*m", "");

    /// <summary>单字符终端显示宽度（CJK=2, ASCII=1）</summary>
    public static int CharWidth(System.Text.Rune rune)
    {
        int v = rune.Value;
        if (v < 0x20 || (v >= 0x7F && v < 0xA0)) return 0;
        if (v >= 0x1100 && v <= 0x115F) return 2;    // Hangul Choseong
        if (v >= 0x2010 && v <= 0x2027) return 2;    // 通用标点 — … "" '' ※
        if (v >= 0x2030 && v <= 0x2043) return 2;    // 补充标点 ‰ ′ ″ ※
        if (v >= 0x2600 && v <= 0x27BF) return 2;    // 杂项符号 ☀ ★ ❤
        if (v >= 0x2E80 && v <= 0xA4CF) return 2;    // CJK Radicals ~ Yi
        if (v >= 0xAC00 && v <= 0xD7AF) return 2;    // Hangul Syllables
        if (v >= 0xF900 && v <= 0xFAFF) return 2;    // CJK Compat
        if (v >= 0xFF01 && v <= 0xFF60) return 2;    // Fullwidth ASCII
        if (v >= 0xFFE0 && v <= 0xFFE6) return 2;    // Fullwidth signs
        if (v >= 0x1F000 && v <= 0x1FAFF) return 2;  // Emoji / Symbols
        if (v >= 0x20000 && v <= 0x2FFFD) return 2;  // CJK Ext B+
        if (v >= 0x30000 && v <= 0x3FFFD) return 2;  // CJK Ext G+
        return 1;
    }
}
