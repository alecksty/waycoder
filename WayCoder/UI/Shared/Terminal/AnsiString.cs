namespace WayCoder.UI.Shared.Terminal;

/// <summary>
/// ANSI 字符串工具 —— 剥离/检测/截断 ANSI 转义序列。
/// 所有 ANSI 识别逻辑集中于此，不依赖 TuiHelper。
/// </summary>
public static class AnsiString
{
    public const char AnsiCharPrefix = '\x1b';
    public const char AnsiCharEscape = '[';

    /// <summary>
    /// 检测字符串是否包含 ANSI 转义序列。
    /// </summary>
    /// <param name="text">待检测的字符串。</param>
    /// <returns>如果包含 ANSI 转义序列则返回 true，否则返回 false。</returns>
    public static bool ContainsAnsi(string text) => text.Contains(AnsiCharPrefix);

    /// <summary>
    /// 从字符串中剥离 ANSI 转义序列。
    /// </summary>
    /// <param name="text">待剥离的字符串。</param>
    /// <returns>剥离后的字符串。</returns>
    public static string Strip(string text)
    {
        if (!ContainsAnsi(text)) return text;
        var sb = new System.Text.StringBuilder();
        var span = text.AsSpan();

        for (var i = 0; i < span.Length; i++)
        {
            if (span[i] == AnsiCharPrefix && i + 1 < span.Length && span[i + 1] == AnsiCharEscape)
            {
                i += 2;
                while (i < span.Length && (span[i] < 0x40 || span[i] > 0x7E))
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
            if (text[i] == AnsiCharPrefix && i + 1 < text.Length && text[i + 1] == AnsiCharEscape)
            {
                int j = i + 2; // 跳过 ESC 与 '[' 引入符，避免把 '['（0x5B）误判为终止符
                while (j < text.Length && (text[j] < 0x40 || text[j] > 0x7E)) j++;
                // 无终止符时钳制到 text.Length，防 j+1 越界（如 "\x1b[" 末尾悬空）
                int end = j < text.Length ? j + 1 : j;
                sb.Append(text[i..end]);
                i = j;
                continue;
            }

            var rune = System.Text.Rune.GetRuneAt(text, i);
            var w = CharWidth(rune);
            if (vw + w > maxVw) break;
            vw += w;
            sb.Append(rune); // 追加完整 rune（代理对不拆半）
            i += rune.Utf16SequenceLength - 1; // for 循环自增 1，补足剩余码元
        }

        sb.Append(AnsiTty.SgrReset);
        return sb.ToString();
    }

    /// <summary>
    /// 从字符串中剥离 ANSI 转义序列（使用正则表达式）。
    /// </summary>
    /// <param name="text">待剥离的字符串。</param>
    /// <returns>剥离后的字符串。</returns>
    public static string StripWithRegex(string text)
        => System.Text.RegularExpressions.Regex.Replace(text, $"[{AnsiCharPrefix}][{AnsiCharEscape}][0-9;]*m", "");

    /// <summary>
    /// 单字符终端显示宽度（CJK=2, ASCII=1，零宽/组合标记=0）。
    /// 参考 Unicode East Asian Width + wcwidth 实现。
    /// 注意：这是全仓唯一的宽度判定真源，TuiHelper.RuneWidth 委托到此。
    /// </summary>
    public static int CharWidth(System.Text.Rune rune)
    {
        int cp = rune.Value;

        // 控制字符 / 零宽字符
        if (cp == 0) return 0;
        if (cp < 0x20) return 0; // C0 控制字符
        if (cp is >= 0x7F and < 0xA0) return 0; // DEL + C1 控制字符

        // 零宽字符范围
        if (cp is >= 0x200B and <= 0x200F) return 0; // 零宽空格等
        if (cp is >= 0x2028 and <= 0x202E) return 0; // 方向控制
        if (cp is >= 0x2060 and <= 0x206F) return 0; // 零宽连接符等
        if (cp is >= 0xFE00 and <= 0xFE0F) return 0; // 变体选择器（✂️ 的 ️）
        if (cp == 0xFEFF) return 0; // BOM / ZWNBS
        if (cp is >= 0xFFF9 and <= 0xFFFB) return 0; // 标注控制
        if (cp is >= 0xE0100 and <= 0xE01EF) return 0; // 补充变体选择器

        // 组合标记（Combining Marks）—— 依附于前一个字符，宽度为 0
        if (cp is >= 0x0300 and <= 0x036F) return 0;
        if (cp is >= 0x0483 and <= 0x0489) return 0;
        if (cp is >= 0x0591 and <= 0x05BD) return 0;
        if (cp is >= 0x0610 and <= 0x061A) return 0;
        if (cp is >= 0x064B and <= 0x065F) return 0;
        if (cp == 0x0670) return 0;
        if (cp is >= 0x06D6 and <= 0x06DC) return 0;
        if (cp is >= 0x06DF and <= 0x06E4) return 0;
        if (cp is >= 0x06E7 and <= 0x06E8) return 0;
        if (cp is >= 0x06EA and <= 0x06ED) return 0;
        if (cp == 0x0711) return 0;
        if (cp is >= 0x0730 and <= 0x074A) return 0;
        if (cp is >= 0x07A6 and <= 0x07B0) return 0;
        if (cp is >= 0x0900 and <= 0x0902) return 0;
        if (cp == 0x093A) return 0;
        if (cp == 0x093C) return 0;
        if (cp is >= 0x0941 and <= 0x0948) return 0;
        if (cp is >= 0x0951 and <= 0x0957) return 0;
        if (cp is >= 0x0962 and <= 0x0963) return 0;
        if (cp == 0x0981) return 0;
        if (cp == 0x09BC) return 0;
        if (cp is >= 0x09C1 and <= 0x09C4) return 0;
        if (cp == 0x09CD) return 0;
        if (cp is >= 0x0A01 and <= 0x0A02) return 0;
        if (cp == 0x0A3C) return 0;
        if (cp is >= 0x0A41 and <= 0x0A42) return 0;
        if (cp is >= 0x0A47 and <= 0x0A48) return 0;
        if (cp is >= 0x0A4B and <= 0x0A4D) return 0;
        if (cp is >= 0x0A70 and <= 0x0A71) return 0;
        if (cp is >= 0x0A81 and <= 0x0A82) return 0;
        if (cp == 0x0ABC) return 0;
        if (cp is >= 0x0AC1 and <= 0x0AC5) return 0;
        if (cp is >= 0x0AC7 and <= 0x0AC8) return 0;
        if (cp == 0x0ACD) return 0;
        if (cp == 0x0B01) return 0;
        if (cp == 0x0B3C) return 0;
        if (cp == 0x0B3F) return 0;
        if (cp is >= 0x0B41 and <= 0x0B44) return 0;
        if (cp == 0x0B4D) return 0;
        if (cp == 0x0B56) return 0;
        if (cp == 0x0B82) return 0;
        if (cp == 0x0BC0) return 0;
        if (cp == 0x0BCD) return 0;
        if (cp is >= 0x0C3E and <= 0x0C40) return 0;
        if (cp is >= 0x0C46 and <= 0x0C48) return 0;
        if (cp is >= 0x0C4A and <= 0x0C4D) return 0;
        if (cp is >= 0x0C55 and <= 0x0C56) return 0;
        if (cp == 0x0CBC) return 0;
        if (cp == 0x0CBF) return 0;
        if (cp == 0x0CC6) return 0;
        if (cp is >= 0x0CCC and <= 0x0CCD) return 0;
        if (cp is >= 0x0D41 and <= 0x0D44) return 0;
        if (cp == 0x0D4D) return 0;
        if (cp == 0x0DCA) return 0;
        if (cp is >= 0x0DD2 and <= 0x0DD4) return 0;
        if (cp == 0x0DD6) return 0;
        if (cp == 0x0E31) return 0;
        if (cp is >= 0x0E34 and <= 0x0E3A) return 0;
        if (cp is >= 0x0E47 and <= 0x0E4E) return 0;
        if (cp == 0x0EB1) return 0;
        if (cp is >= 0x0EB4 and <= 0x0EB9) return 0;
        if (cp is >= 0x0EBB and <= 0x0EBC) return 0;
        if (cp is >= 0x0EC8 and <= 0x0ECD) return 0;
        if (cp is >= 0x0F18 and <= 0x0F19) return 0;
        if (cp == 0x0F35) return 0;
        if (cp == 0x0F37) return 0;
        if (cp == 0x0F39) return 0;
        if (cp is >= 0x0F71 and <= 0x0F7E) return 0;
        if (cp is >= 0x0F80 and <= 0x0F84) return 0;
        if (cp is >= 0x0F86 and <= 0x0F87) return 0;
        if (cp is >= 0x0F90 and <= 0x0F97) return 0;
        if (cp is >= 0x0F99 and <= 0x0FBC) return 0;
        if (cp == 0x0FC6) return 0;
        if (cp is >= 0x102D and <= 0x1030) return 0;
        if (cp is >= 0x1032 and <= 0x1037) return 0;
        if (cp is >= 0x1039 and <= 0x103A) return 0;
        if (cp is >= 0x103D and <= 0x103E) return 0;
        if (cp is >= 0x1058 and <= 0x1059) return 0;
        if (cp is >= 0x105E and <= 0x1060) return 0;
        if (cp is >= 0x1071 and <= 0x1074) return 0;
        if (cp == 0x1082) return 0;
        if (cp is >= 0x1085 and <= 0x1086) return 0;
        if (cp == 0x108D) return 0;
        if (cp == 0x109D) return 0;
        if (cp is >= 0x1160 and <= 0x11FF) return 0; // 韩文 Jungseong/Jongseong
        if (cp is >= 0x135D and <= 0x135F) return 0;
        if (cp is >= 0x1712 and <= 0x1714) return 0;
        if (cp is >= 0x1732 and <= 0x1734) return 0;
        if (cp is >= 0x1752 and <= 0x1753) return 0;
        if (cp is >= 0x1772 and <= 0x1773) return 0;
        if (cp is >= 0x17B4 and <= 0x17B5) return 0;
        if (cp is >= 0x17B7 and <= 0x17BD) return 0;
        if (cp == 0x17C6) return 0;
        if (cp is >= 0x17C9 and <= 0x17D3) return 0;
        if (cp == 0x17DD) return 0;
        if (cp is >= 0x180B and <= 0x180E) return 0;
        if (cp is >= 0x1885 and <= 0x1886) return 0;
        if (cp == 0x18A9) return 0;
        if (cp is >= 0x1920 and <= 0x1922) return 0;
        if (cp is >= 0x1927 and <= 0x1928) return 0;
        if (cp == 0x1932) return 0;
        if (cp is >= 0x1939 and <= 0x193B) return 0;
        if (cp is >= 0x1A17 and <= 0x1A18) return 0;
        if (cp is >= 0x1B00 and <= 0x1B03) return 0;
        if (cp == 0x1B34) return 0;
        if (cp is >= 0x1B36 and <= 0x1B3A) return 0;
        if (cp == 0x1B3C) return 0;
        if (cp == 0x1B42) return 0;
        if (cp is >= 0x1B6B and <= 0x1B73) return 0;
        if (cp is >= 0x1DC0 and <= 0x1DFF) return 0;
        if (cp is >= 0x200B and <= 0x200F) return 0;
        if (cp is >= 0x2028 and <= 0x202E) return 0;
        if (cp is >= 0x20D0 and <= 0x20F0) return 0;
        if (cp is >= 0x2CEF and <= 0x2CF1) return 0;
        if (cp == 0x2D7F) return 0;
        if (cp is >= 0x2DE0 and <= 0x2DFF) return 0;
        if (cp is >= 0xA66F and <= 0xA672) return 0;
        if (cp is >= 0xA674 and <= 0xA67D) return 0;
        if (cp == 0xA69F) return 0;
        if (cp is >= 0xA6F0 and <= 0xA6F1) return 0;
        if (cp == 0xA802) return 0;
        if (cp == 0xA806) return 0;
        if (cp == 0xA80B) return 0;
        if (cp is >= 0xA825 and <= 0xA826) return 0;
        if (cp is >= 0xA8C4 and <= 0xA8C5) return 0;
        if (cp is >= 0xA8E0 and <= 0xA8F1) return 0;
        if (cp is >= 0xA926 and <= 0xA92D) return 0;
        if (cp is >= 0xA947 and <= 0xA951) return 0;
        if (cp is >= 0xAA29 and <= 0xAA2E) return 0;
        if (cp is >= 0xAA31 and <= 0xAA32) return 0;
        if (cp is >= 0xAA35 and <= 0xAA36) return 0;
        if (cp == 0xAA43) return 0;
        if (cp == 0xAA4C) return 0;
        if (cp is >= 0xFE00 and <= 0xFE0F) return 0;

        // 全角 / 宽字符（East Asian Wide + Fullwidth + Emoji）
        if (cp is >= 0x1100 and <= 0x115F) return 2; // 韩文 Choseong
        if (cp is >= 0x2010 and <= 0x2027) return 2; // 通用标点（— … " " ' ' ※ 等 EA Ambiguous）
        if (cp is >= 0x2030 and <= 0x2043) return 2; // 补充标点（‰ ′ ″ ※ 等）
        if (cp is >= 0x2329 and <= 0x232A) return 2; // 〈 〉
        if (cp is >= 0x2600 and <= 0x27BF) return 2; // 杂项符号 + 装饰符号（☀ ★ ❤ ➿ 等）
        if (cp is >= 0x2E80 and <= 0xA4CF) return 2; // CJK 部首 ~ 彝文
        if (cp is >= 0xA960 and <= 0xA97C) return 2; // 韩文扩展
        if (cp is >= 0xAC00 and <= 0xD7A3) return 2; // 韩文音节
        if (cp is >= 0xF900 and <= 0xFAFF) return 2; // CJK 兼容汉字
        if (cp is >= 0xFE10 and <= 0xFE19) return 2; // 竖排标点
        if (cp is >= 0xFE30 and <= 0xFE6F) return 2; // CJK 兼容标点
        if (cp is >= 0xFF01 and <= 0xFF60) return 2; // 全角 ASCII
        if (cp is >= 0xFFE0 and <= 0xFFE6) return 2; // 全角符号
        if (cp is >= 0x1F000 and <= 0x1FAFF) return 2; // Emoji / 符号（麻将～补充-A）
        if (cp is >= 0x20000 and <= 0x2FFFD) return 2; // CJK 扩展 B+
        if (cp is >= 0x30000 and <= 0x3FFFD) return 2; // CJK 扩展 G+

        return 1; // 默认窄字符
    }
}