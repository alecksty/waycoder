using System.Text.RegularExpressions;
using WayCoder.UI.Tui.Edit;

namespace WayCoder.UI.Shared;

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
    /// ⑤ **没有位置**的裸错误行：<c>error: 未定义的函数 'nosuch'（引用 1 次）</c>。
    ///
    /// 出处：<c>LibraryLinker.ReportUnresolved</c> —— 它一次把所有未解析的名字都列出来，
    /// **有源码行号的**写成 GCC 形状、**取不到行号的**就只有 <c>error: …</c> 这一种形状
    /// （`firstLine` 查不到时 `where` 是空串）。两族在同一次输出里**混排**。
    ///
    /// 判据要求**行首就是** <c>error</c> —— 带位置的那些以 <c>&lt;input&gt;:</c> 或
    /// <c>main.c:12:</c> 开头，因此天然不会重复计数。
    /// </summary>
    private static readonly Regex BareErrRx = new(
        @"^[ \t]*(error|warning|错误|警告)[ \t]*[:：][ \t]*(.+)$",
        RegexOptions.Multiline | RegexOptions.Compiled);

    /// <summary>
    /// 把 <see cref="BareErrRx"/> 匹配到的**无位置**错误补进列表（line/col 记 0 ——
    /// 气泡照常显示内容，只是不画箭头指向某一行）。
    ///
    /// 锚不到行的气泡**仍然有意义**：用户至少能看到「一共错了几个、分别是什么」，
    /// 而不是只看到第一个。这是「一次多报」在编辑器里的最后一环。
    /// </summary>
    private static void AppendUnlocated(string text, List<Diagnostic> list)
    {
        foreach (Match m in BareErrRx.Matches(text))
        {
            var raw = m.Groups[2].Value.Trim();
            if (raw.Length == 0) continue;
            var sev = m.Groups[1].Value switch
            {
                "warning" or "警告" => Severity.Warning,
                _ => Severity.Error,
            };
            list.Add(new Diagnostic(0, 0, sev, StripCode(raw, out var code), code));
        }
    }

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

        // ① **带位置**的那一族：三条规则按序尝试、**命中即停**。
        //    必须互斥：同一条错误被两轮匹配会变成两条气泡。
        //    GCC 优先于中文 —— 它的位置信息更全（带列）。
        //
        //    ⚠ 注意 `GccNoColRx` 其实**也**能匹配带列的行（它会把 `main.c:12` 吃进"文件名"
        //      那一段），所以两条 `TryGcc` 是互斥而非叠加，这个顺序不能动。
        bool located = TryGcc(GccRx, text, list, hasCol: true)
                    || TryGcc(GccNoColRx, text, list, hasCol: false);
        if (!located)
        {
            var cn = CnRx.Match(text);
            if (cn.Success)
            {
                int line = ParseInt(cn.Groups[1].Value);
                int col = cn.Groups[2].Success ? ParseInt(cn.Groups[2].Value) : 0;
                var msg = cn.Groups[3].Value.Trim();
                if (msg.Length > 0)
                {
                    list.Add(new Diagnostic(line, col, Severity.Error, msg, null));
                    located = true;
                }
            }
        }
        if (!located)
        {
            // ④ 英文尾缀 `… at line 112:`（位置在句尾，前三条都匹配不到）
            var at = AtLineRx.Match(text);
            if (at.Success)
            {
                int line = ParseInt(at.Groups[1].Value);
                int col = at.Groups[2].Success ? ParseInt(at.Groups[2].Value) : 0;
                var msg = FirstLine(text);
                list.Add(new Diagnostic(line, col, Severity.Error, msg, null));
                located = true;
            }
        }

        // ② **没有位置**的那些：`error: 未定义的函数 'a'` 这种裸行，单独再扫一遍补进来。
        //
        // ⚠ 此前这里是「命中即停 + `return list`」，于是**链接器那份混排的输出只留得下第一条**：
        //   链接器一次会把所有未解析的名字都列出来（`LibraryLinker.ReportUnresolved`），
        //   有行号的写成 `<input>:12: error: …`、没有行号的只有 `error: …`；
        //   而 `TryGcc` 只收**同一种形状**的匹配 ⇒ 用户看到的气泡数从 N 掉到 1
        //   （一条都没带位置时更彻底：三条规则全不命中，最后退化成 `FirstLine(text)` 一条）。
        //   这正是「一次多报」在 UI 上失效的那一环 —— CLI 那边 N 行照打，只有编辑器里并成一条。
        //
        //   裸行正则**天然不会**匹配上面那些带位置的行（它们以 `<input>:` / `main.c:12:`
        //   开头，而这里要求行首就是 `error`），所以两族不会重复计数。
        AppendUnlocated(text, list);

        // ③ 一条都没解析出来（汇编阶段的「未知指令」、标准库路径不对、超时…）——
        //    仍然给一条，只是没有锚点，气泡不画箭头。
        if (list.Count == 0)
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
