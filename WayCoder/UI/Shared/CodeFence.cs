namespace WayCoder.UI.Shared;

/// <summary>
/// 代码围栏识别 —— markdown 围栏（<c>```lang … ```</c>）的**唯一判据**，含「模型把反引号写少了」的容错。
///
/// 为什么需要容错：模型经常把三个反引号写成**一个或两个**，形态是
/// 「独占一行的 <c>`语言名</c>」…「独占一行的 <c>`</c>」。按标准 markdown 这既不是围栏（要 3 个）、
/// 又不是行内代码（行内代码不能跨行）—— 于是整块代码被当普通文本渲染，用户看到「没识别出是代码」。
///
/// 判据收得很紧，只认「**整行只有反引号 + 语言名**」且语言名形如标识符：
/// 文本里独立成行的 <c>`foo`</c>（行内代码）不会被误吞，而 <c>`csharp</c> / `py` / `js`</c> 会。
///
/// TUI（<c>MarkdownRenderer</c>）与 MAUI（<c>MarkupToFormattedString</c>）共用这里 ——
/// 此前两处各写一套「TrimStart().StartsWith("```")」，容错只改一处就会两端行为不一致。
/// </summary>
public static class CodeFence
{
    /// <summary>标准围栏的反引号个数（≥3 个都算，AI 在内容含 ``` 时常用 4 个）</summary>
    public const int StandardTicks = 3;

    /// <summary>
    /// 该行是不是围栏开栏行。是则给出语言标签与反引号个数。
    /// 接受 ≥3 个的标准形态（语言标签可空，<c>```</c> 单独一行也合法），
    /// 以及 1-2 个的**容错**形态（此时必须整行只有「反引号 + 语言名」）。
    /// </summary>
    public static bool TryOpen(string line, out string lang, out int ticks)
    {
        lang = "";
        ticks = 0;
        var t = line.TrimStart();
        if (t.Length == 0 || t[0] != '`') return false;

        int n = 0;
        while (n < t.Length && t[n] == '`') n++;
        var rest = t[n..].Trim();

        if (n >= StandardTicks)
        {
            ticks = n;
            lang = rest;
            return true;
        }

        // 容错形态：反引号写少了。放宽是有代价的 —— 文本里独立成行的 `foo` 与后面某行的 `
        // 会被当成一对围栏，所以这里要求「语言名」必须像标识符（字母开头，不含空格等）。
        if (rest.Length is 0 or > 20 || !IsLangToken(rest)) return false;
        ticks = n;
        lang = rest;
        return true;
    }

    /// <summary>
    /// 该行是不是闭栏行：整行只有反引号，且个数不少于开栏（标准 markdown 允许闭栏更长）。
    /// 注意**不能**用 <c>StartsWith("```")</c> 判 —— 容错形态下开栏只有 1 个反引号，
    /// 那样任何行内代码行都会把块提前闭合。
    /// </summary>
    public static bool IsClose(string line, int ticks)
    {
        var t = line.Trim();
        if (t.Length < ticks) return false;
        foreach (var c in t)
            if (c != '`') return false;
        return true;
    }

    /// <summary>语言名形态：字母开头，其余为字母数字或 + # . - _（c# / c++ / objective-c / f# 都认）</summary>
    private static bool IsLangToken(string s)
    {
        if (s.Length == 0 || !char.IsAsciiLetter(s[0])) return false;
        foreach (var c in s)
            if (!char.IsAsciiLetterOrDigit(c) && c is not ('+' or '#' or '.' or '-' or '_'))
                return false;
        return true;
    }
}
