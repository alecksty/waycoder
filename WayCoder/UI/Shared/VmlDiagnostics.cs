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
///
/// **「这条错误是哪个文件的」和位置一样重要**：报错文本里既有用户自己文件的错、
/// 也有 `#include` 进来的头文件里的错，而后者只有行号。不区分的话，头文件的行号会被
/// **硬贴到用户文件上**（真机症状：第 112 行那句无害的 `/// &lt;summary&gt;` 被标红）。
/// 所以 <see cref="Parse"/> 收「当前文件」，**别的文件来的诊断一律不做行锚**
/// （<c>Line = 0</c> + 文件名写进消息正文），它们照旧躺在错误列表里。
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
    /// 解析报错文本。
    /// </summary>
    /// <param name="currentFile">
    /// **正在编的那个文件**（`BuildProgram` 手里的路径）。传了它才能判出
    /// 「这条诊断属于**别的文件**」—— `#include` 进来的头文件里的错也走同一份报错文本，
    /// 它只有行号：照旧贴到当前文件上，用户就会看到"编译器指着我这句没问题的代码报错"。
    ///
    /// <para>
    /// 不传（null/空）= 老行为，一个字节都不变 —— 既有调用点与自测不受影响。
    /// </para>
    /// </param>
    /// <param name="atLeastOne">
    /// 一条都解析不出来时，**要不要补一条无锚诊断**。默认 `true` = 老行为。
    ///
    /// <para>
    /// **失败出口用默认值**：那儿手上就是「为什么编不过」，哪怕一条都解析不出来，
    /// 也得让用户看到编译器到底说了什么 —— 那正是这支兜底存在的理由
    /// （当初就是为「明明有报错、编辑器里什么都不显示」加的）。
    /// </para>
    ///
    /// <para>
    /// ⚠ **成功出口必须传 `false`**：那儿手上是**编译期的日志**（前端 stderr + 链接器 stderr），
    /// 编过了却一条都没解析出来 = **这段日志里本来就没有诊断**，此时补一条等于
    /// **凭空造一个错误**——而且造出来的还是 `Severity.Error`（比原来的警告更吓人）。
    /// 实测踩到：把链接器那条「库内部」提示改成 `[库内部]` 前缀（不再匹配 `BareErrRx`）之后，
    /// 一个 7 行、编得过跑得动的 Pascal 程序，编辑器错误列表里冒出一条**红色错误**。
    /// </para>
    /// </param>
    public static List<Diagnostic> Parse(string? errorText, string? currentFile = null,
                                         bool atLeastOne = true)
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
        bool located = TryGcc(GccRx, text, list, hasCol: true, currentFile)
                    || TryGcc(GccNoColRx, text, list, hasCol: false, currentFile);
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
        //    失败出口**仍然给一条**，只是没有锚点。
        //    ⚠ 成功出口不许补（`atLeastOne: false`）—— 那里「解析不出来」= 日志里本来就没有诊断，
        //      补一条等于凭空造一个红色错误，见参数说明。
        if (list.Count == 0 && atLeastOne)
            list.Add(new Diagnostic(0, 0, Severity.Error, FirstLine(text), null));
        return list;
    }

    private static bool TryGcc(Regex rx, string text, List<Diagnostic> list, bool hasCol, string? currentFile)
    {
        foreach (Match m in rx.Matches(text))
        {
            int line = ParseInt(m.Groups[2].Value);
            int col = hasCol ? ParseInt(m.Groups[3].Value) : 0;
            var sevText = m.Groups[hasCol ? 4 : 3].Value;
            var raw = m.Groups[hasCol ? 5 : 4].Value.Trim();
            if (line <= 0) continue;

            var msg = StripCode(raw, out var code);
            // Groups[1] 就是报错里写的那个文件名（两条 GCC 正则都是第 1 组）。
            // 它属于**别的文件**（头文件）时不给行锚，并把文件名写进消息正文。
            var file = m.Groups[1].Value.Trim();
            if (currentFile is { Length: > 0 } && !IsSameFile(file, currentFile))
            {
                var name = FileNameOf(file);
                list.Add(new Diagnostic(0, 0, SeverityOf(sevText), $"「{name}」{msg}", code, name));
                continue;
            }

            list.Add(new Diagnostic(line, col, SeverityOf(sevText), msg, code));
        }
        return list.Count > 0;
    }

    /// <summary>
    /// 报错里那个文件名是不是**当前正在编的文件**。
    ///
    /// <para>
    /// **比文件名、不比全路径**：同一次编译里两边可能一个是绝对路径、一个是相对路径
    /// （前端各自的 `FileName` 口径就不一致），比全路径会把当前文件自己的错误误判成别人的。
    /// </para>
    ///
    /// <para>
    /// **尖括号占位符一律当"当前文件"**：`&lt;input&gt;` / `&lt;unknown&gt;` 是前端在
    /// 「手里没有真实路径」时用的名字（`CompilerHelper` / `CppCompiler.Compile`），
    /// 把它们当成别的文件会让**用户自己文件里的错误**也失去行锚（比不判还糟）。
    /// </para>
    /// </summary>
    private static bool IsSameFile(string file, string currentFile)
    {
        if (file.Length == 0) return true;                    // 没写文件名 = 就是正在编的这个
        if (file[0] == '<' && file[^1] == '>') return true;    // <input> / <unknown> 等占位符
        return string.Equals(FileNameOf(file), FileNameOf(currentFile), StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    /// 取文件名（去目录）。取不到时原样返回 —— 消息正文里总得有东西可显示。
    ///
    /// <para>
    /// ⚠ **不能用 `Path.GetFileName`**：它按**当前平台**的分隔符切，而这里要处理的是
    /// **编译器吐出来的路径**，形态由**产出它的那台机器**决定，不由我们运行在哪台机器决定。
    /// Unix 上 `\` 不是分隔符 ⇒ `D:\proj\main.cpp` 会被**原样返回**，与 `currentFile`
    /// 一比就"不是同一个文件"，于是 <see cref="IsSameFile"/> 判成"别的文件的诊断"
    /// —— 后果是**行锚被丢掉**（`Line = 0`，编辑器里不再画箭头），而气泡本身照常出现，
    /// 所以从界面上看只是"位置没了"，很难联想到分隔符。
    /// </para>
    ///
    /// <para>
    /// 实测：`SelfTest` 里"真机上的路径形态（Windows 盘符 + 反斜杠）"那一组在 Windows 上
    /// 全绿、在 macOS/Linux 上**必红一条**（`那条警告锚在 #include 那一行（6 行）`）——
    /// 两条反斜杠的用例互相抵消看不出来，出问题的正是"一边反斜杠、一边相对名"的那条。
    /// </para>
    /// </summary>
    private static string FileNameOf(string path) => PathText.FileNameOf(path);

    /// <summary>
    /// 把两批诊断并起来，按 <see cref="Diagnostic"/> 的**逐字段相等性**去重（同一条只留第一次出现的）。
    ///
    /// <para>
    /// **为什么需要**：编译失败时，同一段报错会**走两条路**进宿主 —— 异常消息里一份、
    /// 编译期 stderr 里一份。链接器的未解析清单就是**逐字相同的两份**（实测
    /// `error: 未定义的函数 'nosuchfn'（引用 1 次）` 两边都打），不去重的话同一个错误
    /// 会变成两个气泡。
    /// </para>
    ///
    /// <para>
    /// 而去重**不能靠"只取一份"**：stderr 里还有异常消息里没有的东西 ——
    /// 比如预处理器那句「找不到头文件 "Windows.h"」（它是**警告**、不抛异常）。
    /// 只取异常消息就会把「为什么这个变量没声明」的答案一起丢掉。
    /// </para>
    /// </summary>
    public static List<Diagnostic> Merge(List<Diagnostic> first, List<Diagnostic> second)
    {
        var result = new List<Diagnostic>(first.Count + second.Count);
        var seen = new HashSet<Diagnostic>();   // `Diagnostic` 是 record：相等性逐字段
        foreach (var d in first) if (seen.Add(d)) result.Add(d);
        foreach (var d in second) if (seen.Add(d)) result.Add(d);
        return result;
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
