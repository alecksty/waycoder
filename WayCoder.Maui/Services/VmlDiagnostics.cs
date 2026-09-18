using System.Text.RegularExpressions;
using WayCoder.UI.Tui.Edit;

namespace WayCoder.Maui.Services;

/// <summary>
/// 把 VML 前端编译器的报错文本解析成结构化的 <see cref="Diagnostic"/>。**唯一实现**
/// —— 编辑器的气泡、行下波浪线、状态栏计数都从这一份结果出发，别处不要再解析一遍。
///
/// **为什么不能只按一种格式写**：22 个前端编译器里，报错至少有两种形状同时存在
/// （实测出处见每条正则上面的注释）。只认一种，另一族语言的报错就会退化成
/// 「一行没有位置的文字」，气泡没地方指。
///
/// 解析不出来时**不假装成功**：返回一条 <c>Line = 0</c> 的「无锚」诊断，
/// 气泡照常显示内容、只是不画箭头指向某一行 —— 用户至少能看到编译器到底说了什么。
/// </summary>
internal static class VmlDiagnostics
{
    /// <summary>
    /// ① GCC 格式（带列）：
    /// <code>main.c:12:5: error: 未预期的 token [Parser_UnexpectedToken]</code>
    ///
    /// 出处：<c>CompilerBase/ParserBase.cs</c> 的 <c>GccError</c> → <c>DiagnosticBag</c> →
    /// <c>CompilerError.ToString()</c>，经 <c>CompilerPluginBase</c> 的
    /// <c>IsGccFormat</c> 判定为真后**原样透传**。
    /// </summary>
    private static readonly Regex GccRx = new(
        @"^(.+?):(\d+):(\d+):\s*(error|warning|note):\s*(.*)$",
        RegexOptions.Multiline | RegexOptions.Compiled);

    /// <summary>② GCC 格式（不带列）：<c>main.c:12: error: …</c></summary>
    private static readonly Regex GccNoColRx = new(
        @"^(.+?):(\d+):\s*(error|warning|note):\s*(.*)$",
        RegexOptions.Multiline | RegexOptions.Compiled);

    /// <summary>
    /// ③ 中文格式：<c>语法错误 main.c在第12行5列：未预期的 token</c>
    ///
    /// 出处：<c>CCompiler/Parser.Core.cs</c> 与 <c>Lexer.cs</c> 的 <c>ParseException</c>，
    /// 同形文本在 Go / Lua / Python / Pascal 与 <c>CompilerBase/LexerBase.cs</c> 都出现
    /// —— 是**整族前端的主流格式**，也是这里的主力规则。
    /// 列号可能缺（只报行）。
    /// </summary>
    private static readonly Regex CnRx = new(
        @"在第(\d+)行(?:第?(\d+)列)?[：:]?\s*(.*)",
        RegexOptions.Compiled);

    /// <summary>
    /// ④ 英文尾缀：<c>Expected SEMICOLON but got IDENTIFIER ('tm_t') at line 112:</c>
    ///
    /// 出处：C/C++ 前端那条英文报错路（真机上在 `.cpp` 文件里撞见的）。
    /// 位置在句**尾**而不是开头，所以前面三条规则都匹配不到 —— 症状是气泡显示「（无位置）」、
    /// 指不到行上，而消息里其实明明白白写着 `at line 112`。
    /// 列号可有可无（有的写 `at line 112, column 5`）。
    /// </summary>
    private static readonly Regex AtLineRx = new(
        @"\bat\s+line\s+(\d+)(?:\s*,\s*(?:column|col)\s+(\d+))?",
        RegexOptions.Compiled | RegexOptions.IgnoreCase);

    /// <summary>
    /// 解析报错文本。返回的列表**至少有一条**（解析不出位置时给一条无锚诊断），
    /// 除非传进来的本来就是空/纯空白。
    /// </summary>
    public static List<Diagnostic> Parse(string? errorText)
    {
        var list = new List<Diagnostic>();
        if (string.IsNullOrWhiteSpace(errorText)) return list;

        var text = errorText.Replace("\r\n", "\n");

        // 剥掉宿主自己加的前缀，否则会混进消息正文里显示给用户
        text = text.Replace("⚠️ 编译失败：", "")
                   .Replace("⚠️ 前端编译没有产出 VML 汇编", "前端编译没有产出 VML 汇编");

        // 三个规则**按序尝试、命中即停**：同一条错误被两轮匹配会变成两条气泡。
        // GCC 优先于中文 —— 它的位置信息更全（带列）。
        if (TryGcc(GccRx, text, list, hasCol: true)) return list;
        if (TryGcc(GccNoColRx, text, list, hasCol: false)) return list;

        var cn = CnRx.Match(text);
        if (cn.Success)
        {
            int line = ParseInt(cn.Groups[1].Value);
            int col = cn.Groups[2].Success ? ParseInt(cn.Groups[2].Value) : 0;
            var msg = cn.Groups[3].Value.Trim();
            if (msg.Length > 0)
            {
                list.Add(new Diagnostic(line, col, Severity.Error, msg, null));
                return list;
            }
        }

        // ④ 英文尾缀 `… at line 112:`（位置在句尾，前三条都匹配不到）
        var at = AtLineRx.Match(text);
        if (at.Success)
        {
            int line = ParseInt(at.Groups[1].Value);
            int col = at.Groups[2].Success ? ParseInt(at.Groups[2].Value) : 0;
            var msg = FirstLine(text);
            list.Add(new Diagnostic(line, col, Severity.Error, msg, null));
            return list;
        }

        // 完全没有位置信息（汇编阶段的「未知指令」、标准库路径不对、超时…）——
        // 仍然给一条，只是没有锚点，气泡不画箭头。
        list.Add(new Diagnostic(0, 0, Severity.Error, FirstLine(text), null));
        return list;
    }

    private static bool TryGcc(Regex rx, string text, List<Diagnostic> list, bool hasCol)
    {
        foreach (Match m in rx.Matches(text))
        {
            int line = ParseInt(m.Groups[2].Value);
            int col = hasCol ? ParseInt(m.Groups[3].Value) : 0;
            var sevText = m.Groups[hasCol ? 4 : 3].Value;
            var raw = m.Groups[hasCol ? 5 : 4].Value.Trim();
            if (line <= 0) continue;

            list.Add(new Diagnostic(line, col, SeverityOf(sevText), StripCode(raw, out var code), code));
        }
        return list.Count > 0;
    }

    private static Severity SeverityOf(string s) => s switch
    {
        "warning" => Severity.Warning,
        "note" => Severity.Info,
        _ => Severity.Error,
    };

    /// <summary>
    /// 把 GCC 消息尾巴上的 <c>[Parser_UnexpectedToken]</c> 摘出来放进 <c>Code</c>，
    /// 正文里就不再重复它 —— 气泡里那两行地方很宝贵。
    /// </summary>
    private static string StripCode(string message, out string? code)
    {
        code = null;
        var m = Regex.Match(message, @"\[([A-Za-z_][A-Za-z0-9_]*)\]\s*$");
        if (!m.Success) return message;
        code = m.Groups[1].Value;
        return message[..m.Index].TrimEnd();
    }

    /// <summary>取第一行非空文本 —— 编译器的「源码行 + ^ 指示」那两行不进气泡。</summary>
    private static string FirstLine(string text)
    {
        foreach (var raw in text.Split('\n'))
        {
            var s = raw.Trim();
            if (s.Length > 0) return s;
        }
        return "编译失败";
    }

    private static int ParseInt(string s) => int.TryParse(s, out var v) ? v : 0;
}
