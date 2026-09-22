namespace WayCoder.UI.Shared.Terminal;

/// <summary>
/// ANSI 字符串工具 —— 剥离/检测/截断 ANSI 转义序列。
/// 所有 ANSI 识别逻辑集中于此，不依赖 AnsiHelper。
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
            if (span[i] == AnsiCharPrefix && i + 1 < span.Length)
            {
                // OSC(ESC ])/DCS(ESC P) 等以 BEL 或 ST(ESC \) 终止的序列——若不剥离，
                // 终端会把「设置标题/剪贴板」等控制命令当真执行（终端转义注入）。
                // CSI(ESC [) 是唯一以 0x40-0x7E 单字节终止的常见序列，其余走各自终止规则。
                i = SkipEscapeSequence(span, i);
                continue;
            }

            sb.Append(span[i]);
        }

        return sb.ToString();
    }

    /// <summary>
    /// 从 span[i]（当前指向 ESC 0x1B）跳过整条转义序列，返回「最后一个已消费字节」的索引，
    /// 由调用方 for 循环的 i++ 补足到下一个待扫描字符。覆盖三类：CSI（ESC [，终止于 0x40-0x7E）、
    /// OSC/DCS/PM/APC（ESC ]/P/^/_，终止于 BEL 或 ST=ESC \）、其余单/双字符 ESC 序列。
    /// 孤立 ESC（末尾悬空）返回 i 仅消费 ESC 本身，不越界。
    /// </summary>
    private static int SkipEscapeSequence(ReadOnlySpan<char> span, int i)
    {
        if (i + 1 >= span.Length) return i; // 孤立 ESC：只消费 ESC 一个字节
        char c = span[i + 1];
        if (c == '[')
        {
            // CSI：参数/中间字节区间 0x20-0x3F 之后以最终字节 0x40-0x7E 终止
            i += 2;
            while (i < span.Length && (span[i] < 0x40 || span[i] > 0x7E))
                i++;
            return i; // 终止字节索引（或 span.Length 时由调用方钳制）
        }
        if (c == ']' || c == 'P' || c == '^' || c == '_')
        {
            // OSC/DCS/PM/APC：终止于 BEL(0x07) 或 ST(ESC \)；无终止符时吞到串尾，防泄控制字节
            i += 2;
            while (i < span.Length)
            {
                if (span[i] == '\x07') return i; // BEL 是最后一个消费字节
                if (span[i] == AnsiCharPrefix && i + 1 < span.Length && span[i + 1] == '\\') return i + 1; // ST 末字节 '\'
                i++;
            }
            return span.Length - 1; // 无终止符：消费到串尾
        }
        // 其余（ESC c / ESC 7 / ESC M 等）：ESC + 单字符，共 2 字节
        return i + 1;
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
            if (text[i] == AnsiCharPrefix && i + 1 < text.Length)
            {
                // CSI（ESC [）颜色码保留（渲染需要），其余 ESC 序列（OSC/DCS/孤立 ESC）剥离——
                // 透传 OSC 会把「设置终端标题/剪贴板」等控制命令注入终端。
                if (text[i + 1] == AnsiCharEscape)
                {
                    int j = i + 2; // 跳过 ESC 与 '[' 引入符，避免把 '['（0x5B）误判为终止符
                    while (j < text.Length && (text[j] < 0x40 || text[j] > 0x7E)) j++;
                    // 无终止符时钳制到 text.Length，防 j+1 越界（如 "\x1b[" 末尾悬空）
                    int end = j < text.Length ? j + 1 : j;
                    sb.Append(text[i..end]);
                    i = j;
                }
                else
                {
                    i = SkipEscapeSequence(text.AsSpan(), i);
                }
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
    ///
    /// ⚠ 判定表**已下沉到 `CharMetrics.Width`**（`UI/Shared/CharMetrics.cs`）：本文件
    /// 不自足（引用 `AnsiTty` → `RenderBuffer`/`Terminal`），而桌面 `scripts/vmlcli`
    /// 是白名单式编译、拖不进那条链。这里只**转调**，唯一真源仍是那一份，
    /// 别在本文件里再抄一张表。
    /// </summary>
    public static int CharWidth(System.Text.Rune rune) => CharMetrics.Width(rune);
}