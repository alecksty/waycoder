// ============================================================================
// DiagProbe —— 22 门 VML 前端的「报错位置准确性」探针
// ============================================================================
//
// 用法：
//   dotnet run --project third_party/vml/tools/DiagProbe                    # 全部 22 门
//   dotnet run --project third_party/vml/tools/DiagProbe -- py c kt         # 只跑这几门
//   dotnet run --project third_party/vml/tools/DiagProbe -- --full          # 失败项打完整报错文本
//   dotnet run --project third_party/vml/tools/DiagProbe -- --list          # 只列用例，不编译
//
// 退出码：0 = 全 PASS；1 = 有任一门不是 PASS（可直接当 CI 判据用）。
//
// ---------------------------------------------------------------------------
// 判据（六档，**「没报错」「报了但没位置」「文件指错了」都算 FAIL**，且各自单列一档）
// ---------------------------------------------------------------------------
//   PASS    报了编译错误，且报错文本里的行号 == 期望行号
//   FILE    报了编译错误，行号也对，但**文件名不对**（只有「头文件里的错」那一档判文件）
//   FAIL    报了编译错误，但行号 != 期望行号（位置报错了地方）
//   NOPOS   报了编译错误，但文本里**没有任何行号**（用户看不到位置）
//   NOERR   编译"成功"了 —— 前端静默接受了一段本该报错的源码
//   CRASH   抛的不是编译错误（内部错误 / 未捕获异常）—— 位置判定不了
//
// ⚠ 为什么 `NOERR` 与 `NOPOS` 必须单列而不是并进 FAIL：
//   本仓的规矩是「冒烟不许冒充 PASS」。「跑通了」既不是「位置对」，
//   也不是「位置错」—— 它是**另一种故障**，混进 FAIL 会让下一轮修的人
//   把「前端根本没做这个检查」当成「位置算错了」去修。
//
// ⚠ 为什么 `FILE` 也要单列：本轮的命题就是「**错在头文件里**时别指到用户文件上」。
//   只判行号的话，「报的是用户文件里恰好同一行号」会被算成 PASS ——
//   而那正是被修掉的那个 bug 的伪装形态（行号碰巧对上，文件整个是错的）。
//
// ---------------------------------------------------------------------------
// 样本规则（三档：未定义标识符 / 语法错误 / 头文件里的错）
// ---------------------------------------------------------------------------
// · **每门语言一份最小样本**，错误埋在一个**明确的行**上（= `Lines` 数组下标 + 1）。
// · 只用该语言最基本的语法，**不碰标准库、不用花哨特性** —— 否则前端会先挂在
//   别的地方，量到的就是别的问题（这一条是 `scripts/vml-diag-probe/README.md`
//   里用真金白银换来的：`undef-fn.go` 当年写成了 Rust 语法，测出来的是"用例写错了"）。
// · 前两档的错误类型统一取「**读一个未定义的标识符**」：
//     - 静态类型语言 → 未声明的**变量**（c/cpp/cs/java/kt/swift/d/objc/rs/go/f90/pas/bas/fth/ld/dart）
//     - 动态语言 6 门 → 未声明的**函数**（py/rb/lua/js/r/scm）
//       因为这 6 门的「未声明即隐式全局」是**合法语义**（见 `vml-diag-probe` 的
//       `dyn-global` 组），用未声明变量测不出错来。
// · 前两档每份样本都有 1~3 行**有效的前导语句**，让错误落在第 3~4 行而不是第 1 行 ——
//   第 1 行出错太容易"碰巧对"，量不出真实的位置计算。
// · **第三档（头文件里的错）**另有一套形状，见那一档自己的注释块（`Samples()` 里）。
//
// ---------------------------------------------------------------------------
// 为什么不写文件、不走 CLI
// ---------------------------------------------------------------------------
// 前两档直接调各前端的 `Compile(string)` 入口（进程内），**不经文件读写**：
// 这样量到的纯粹是「**前端自己的位置计算**」，与宿主/编辑器那一侧
// （文件路径、BOM、编码、`VmlDiagnostics` 解析）完全解耦 —— 两者混在一起量，
// 出了偏差分不清是谁的锅。
// `scripts/vml-diag-probe/run.sh` 走的是外部 CLI 进程、判的是"有没有点名符号"；
// 本探针走进程内、判的是"**行号对不对**"。两者互补，不重复。
//
// ⚠ **第三档是唯一的例外：它必须真写文件**（`HeaderPath`）。`#include` 只有
//   "真去读磁盘"一条路，而那一档的命题恰恰是"拼接流里的行号能不能映射回
//   它来自哪个文件" —— 不落盘就量不到。写成**临时目录**里的独立文件，
//   内容就在样本里（自包含，不引用 `Examples/` 或用户的任何文件）。
//
// ⚠ 样本用 `\n` 拼（不是 CRLF）：先排除行尾差异这个变量。将来若要量 CRLF 的影响，
//   应该**另开一档**，别把两种行尾混进同一列数字里。
//
// ⚠ 默认 `CompilerOptionsContext.Current` 是 `TargetMode.MCU`（= `IsMCU == true`），
//   此时 `InjectDefines` **不会**给源码前面插 `#define VML_WSTRING 1`
//   （见 `CompilerHelper.InjectDefines` 开头那段）。手机端与 VMLTool 默认也走 MCU，
//   探针因此与线上一致；**没有**隐含的行号偏移。
// ============================================================================

using System.Text;
using System.Text.RegularExpressions;
using CompilerBase;

// 命名空间与类同名（`CCompiler.CCompiler`），直接写会有歧义 —— 一律走 global:: 别名。
using LBasic = global::BasicCompiler.BasicCompiler;
using LC = global::CCompiler.CCompiler;
using LCs = global::CSharpCompiler.CSharpCompiler;
using LCpp = global::CppCompiler.CppCompiler;
using LD = global::DCompiler.DCompiler;
using LDart = global::DartCompiler.DartCompiler;
using LForth = global::ForthCompiler.ForthCompiler;
using LFortran = global::FortranCompiler.FortranCompiler;
using LGo = global::GoCompiler.GoCompiler;
using LJava = global::JavaCompiler.JavaCompiler;
using LJs = global::JavaScriptCompiler.JavaScriptCompiler;
using LKt = global::KotlinCompiler.KotlinCompiler;
using LLadder = global::LadderCompiler.LadderCompiler;
using LLua = global::LuaCompiler.LuaCompiler;
using LObjC = global::ObjCCompiler.ObjCCompiler;
using LPascal = global::PascalCompiler.PascalCompiler;
using LPy = global::PythonCompiler.PythonCompiler;
using LR = global::RCompiler.RCompiler;
using LRb = global::RubyCompiler.RubyCompiler;
using LRust = global::RustCompiler.RustCompiler;
using LScm = global::SchemeCompiler.SchemeCompiler;
using LSwift = global::SwiftCompiler.SwiftCompiler;

namespace DiagProbe;

/// <summary>判定档位（见文件头）。名称即结论，别再靠注释区分。</summary>
internal enum Verdict
{
    Pass,
    WrongFile,     // 表里显示 FILE —— **行号对了、文件不对**（"半对"，必须单列，见下）
    WrongLine,     // 表里显示 FAIL
    WrongColumn,   // 表里显示 COL —— 行对了、列不对（"半对"，必须单列）
    NoPosition,    // 表里显示 NOPOS
    NoError,       // 表里显示 NOERR
    Crash,         // 表里显示 CRASH
}

/// <summary>
/// 一份错误样本。
/// <para>
/// `Lines` **就是**源文件的内容（用 `\n` 拼），期望行号 = 出问题那一行的下标 + 1。
/// 源与期望写在同一处，是为了杜绝「样本挪了一行、期望值忘改」这种平行表漂移。
/// </para>
/// </summary>
internal sealed record Sample(
    string Lang,          // 语言名（同时也是筛选用的键）
    string Ext,           // 扩展名（只用于展示与对照）
    string Group,         // 用例档：未定义标识符 / 语法错误（见文件头「用例档」）
    string ErrorKind,     // 错误类型：未定义变量 / 未定义函数 / 语法错误
    int Expected,         // 期望行号（1 基）
    // 期望列号（1 基，= 出错标识符/记号的首字符）；**0 = 本档不判列**。
    //
    // ⚠ 为什么列是**逐条显式给**、不做"自动从源码算"：能算的地方（标识符在行里的
    //   位置）一算就准、反而掩盖了前端算没算；而算不出来的地方（链接期那条报的是
    //   CALL 指令、没有列的概念）又会被算出一个假的期望值。写死在这里，改样本必须
    //   一起改 —— 与 `Expected` 同一套防漂移思路。
    int ExpectedColumn,   // 0 = 不判列
    string[] Lines,       // 样本源码（按行）
    Func<string, object?> Compile,     // 该前端的源码入口
    // 探针补链的语言目录（null = 该前端的 `Compile(string)` 自己链了，或本样本是前端错误）
    //
    // ⚠ **为什么需要这个字段**：`Compile(string)` 与 `CompileFile` 不是同一条流水线 ——
    //   五门（c / cpp / ladder / forth / cs+java+js+swift 的实例入口）在 `Compile(string)` 里
    //   **不调 `LinkStandardLibrary`**（C 的注释写着「由 CompileCore/测试框架统一调用」），
    //   而「未定义**函数**」恰恰只有链接器看得见。不补这一步，探针会把
    //   「探针自己没接上链接」误报成「前端静默接受」。
    //   实测只有 Forth 这一门需要（其余各门的样本都是前端自己就会报的错）。
    string? PostLinkLang = null,
    // ── 只用「头文件里的错」那一档（`Groups.Header`）的两个字段 ──────────────────
    //
    // 那一档的命题是：**错在 `#include` 进来的头文件里时，报错必须指到头文件**。
    // 头文件是**运行时**才落到临时目录的（`#include` 只有真去读磁盘一条路），
    // 所以样本用「头文件正文」描述它，主文件里的那句 `#include "<绝对路径>"`
    // 由 `WithInclude()` 现拼 —— 路径写不进源码常量里。
    string[]? HeaderLines = null,
    // 期望报错**指到这个文件**（比对的子串，= 头文件名）。
    // `null` = 本档不判文件（前两档都是 null，判据与从前逐字相同）。
    //
    // ⚠ 为什么必须判文件：只判行号的话，「报的是主文件里恰好同一行号」会算成 PASS，
    //   而那正是本轮要修的那个 bug 的伪装形态（用户看到的就是"编译器指着我这句
    //   没问题的代码报错"）。
    string? ExpectedInFile = null)
{
    public string Source => string.Join("\n", Lines) + "\n";
}

/// <summary>一条测得的实际结果。</summary>
internal sealed record Outcome(
    Sample Sample,
    Verdict Verdict,
    int? FoundLine,
    int? FoundColumn,
    string? PositionKind,   // GCC / GCC-无列 / 中文 / 英文
    string? FirstMessage,   // 报错文本里的第一条诊断（一行）
    string RawText,         // 完整报错文本
    string? ExceptionType,
    string Noise,           // 编译期间被探针截下来的 stdout/stderr（链接器日志等）
    string? FoundFile = null,  // 报错文本里点名的文件（只判得出文件名的形态才有）
    // CRASH 那一档的完整调用栈（`--stack` 才填）。见 `Program.ShowStack` 的注释。
    string? Stack = null)
{
    public string VerdictText => Verdict switch
    {
        Verdict.Pass => "PASS",
        Verdict.WrongFile => "FILE",
        Verdict.WrongLine => "FAIL",
        Verdict.WrongColumn => "COL",
        Verdict.NoPosition => "NOPOS",
        Verdict.NoError => "NOERR",
        _ => "CRASH",
    };
}

internal static class Program
{
    /// <summary>
    /// `--stack`：CRASH 那一档额外打**完整调用栈**。
    ///
    /// ⚠ 为什么必须有这一档：CRASH 的正文（`ex.Message`）往往是一句
    /// `Object reference not set to an instance of an object.` ——
    /// **一句话四个字都没有信息量**，光看它连是哪个前端、哪一步崩的都定不下来。
    /// 本仓的规矩是「不响的自测比没有更糟」，而这条是它的同一面：
    /// **报了崩溃但报不出位置，等于把排查成本推给下一个人**。
    /// 默认不开（输出会长），崩了才需要。
    /// </summary>
    private static bool ShowStack;

    private static int Main(string[] args)
    {
        bool full = args.Contains("--full");
        bool listOnly = args.Contains("--list");
        ShowStack = args.Contains("--stack");
        var filters = args.Where(a => !a.StartsWith("--")).ToArray();

        // ── `--truncate`：另一档（见 HangProbe.cs 的文件头）────────────────────
        // 位置那一套问「报得准不准」，这一档问「**还有没有反应**」。极性不同，
        // 所以走独立分支、不混进下面那张表。
        if (args.Contains("--truncate"))
        {
            HangProbe.Configure(OptInt(args, "--timeout-ms"), OptInt(args, "--max-cuts"));
            // ⚠ 选项的**值**（`--timeout-ms 15000` 里的 `15000`）不以 `--` 开头，
            //   会被上面那句 `filters` 当成语言名捡进来 ⇒ 在这里再排掉一次。
            var optValues = new[] { OptRaw(args, "--timeout-ms"), OptRaw(args, "--max-cuts") }
                                .Where(v => v != null).ToArray();
            var truncLangs = filters.Where(f => !optValues.Contains(f)).ToArray();
            if (args.Contains("--selftest")) return HangProbe.SelfTest();
            return HangProbe.Run(truncLangs, ResolveVmlHome());
        }

        var samples = Samples();
        if (listOnly)
        {
            // 三档之后"条数"与"门数"不再是同一个数 —— 分开报，别让 66 被读成 66 门语言。
            foreach (var s in samples)
                Console.WriteLine($"{s.Lang,-10} .{s.Ext,-6} [{s.Group}] 期望第 {s.Expected} 行  {s.ErrorKind}");
            Console.WriteLine($"共 {samples.Count} 条样本 / {samples.Select(s => s.Lang).Distinct().Count()} 门语言");
            return 0;
        }

        if (filters.Length > 0)
        {
            var unknown = filters.Where(f => !samples.Any(s => s.Lang == f || s.Ext == f)).ToArray();
            if (unknown.Length > 0)
            {
                Console.Error.WriteLine($"✘ 未知语言：{string.Join(", ", unknown)}（用 --list 看全部）");
                return 2;
            }
            samples = samples.Where(s => filters.Contains(s.Lang) || filters.Contains(s.Ext)).ToList();
        }

        Console.OutputEncoding = Encoding.UTF8;
        Console.WriteLine($"DiagProbe —— 前端「报错位置准确性」探针（{samples.Count} 门）");

        // ⚠ **必须先把标准库根找出来**：6 门动态语言（py/rb/lua/js/r/scm）与 Forth 的
        //   「未定义函数」**不在前端报、在链接期报**。`LinkStandardLibrary` 找不到 `Lib/`
        //   时 `allPaths` 为空、静默跳过链接 ⇒ 探针会看到「编译成功」，把**探针自己
        //   没接上标准库**误报成「前端静默接受」（实测踩过：那一轮 7 门 NOERR 全是假的）。
        var vmlHome = ResolveVmlHome();
        if (vmlHome != null) Environment.SetEnvironmentVariable("VML_HOME", vmlHome);
        Console.WriteLine($"标准库根（VML_HOME）：{vmlHome ?? "(未找到 —— 链接期错误查不出来，本轮的 NOERR 不作数)"}");
        Console.WriteLine();

        var outcomes = new List<Outcome>();
        foreach (var s in samples)
            outcomes.Add(RunOne(s));

        PrintTable(outcomes);
        PrintDetails(outcomes, full);
        return PrintSummary(outcomes);
    }

    /// <summary>读 `--名 值` 形式选项的**原样字符串**（没给返回 null）。</summary>
    private static string? OptRaw(string[] args, string name)
    {
        int i = Array.IndexOf(args, name);
        return i >= 0 && i + 1 < args.Length ? args[i + 1] : null;
    }

    /// <summary>读 `--名 值` 形式的整数选项；没给/不是整数一律返回 0（= 用默认值）。</summary>
    private static int OptInt(string[] args, string name)
        => int.TryParse(OptRaw(args, name), out var v) ? v : 0;

    // ── 找标准库根 ───────────────────────────────────────────────────────────
    //
    // `CompilerHelper.FindLibPath` 认 `VML_HOME`／`VML_TOOL_PATH` 环境变量，其次才是
    // CWD 与 BaseDirectory。探针从 `tools/DiagProbe/bin/Release/net10.0/` 起跑，
    // **三条自带路径一条都不命中**，所以要自己上溯找到 `Lib/` + `VMLPrepares/` 那一级
    // （= `third_party/vml`）并显式设上环境变量。
    private static string? ResolveVmlHome()
    {
        var env = Environment.GetEnvironmentVariable("VML_HOME")
               ?? Environment.GetEnvironmentVariable("VML_TOOL_PATH");
        if (!string.IsNullOrEmpty(env) && Directory.Exists(Path.Combine(env, "Lib"))) return env;

        var dir = AppDomain.CurrentDomain.BaseDirectory;
        for (int i = 0; i < 10 && !string.IsNullOrEmpty(dir); i++)
        {
            if (Directory.Exists(Path.Combine(dir, "Lib")) &&
                Directory.Exists(Path.Combine(dir, "VMLPrepares")))
                return dir;
            dir = Path.GetDirectoryName(dir);
        }
        return null;
    }

    // ── 量一门 ───────────────────────────────────────────────────────────────
    private static Outcome RunOne(Sample s)
    {
        // 编译器（尤其链接器）会往 stdout/stderr 直接打日志（实测一次运行 400+ 行
        // 「成功链接: xxx.vml」），会把表格冲散。整段截下来存进 Outcome，
        // 只在 `--full` 或失败详情里露出来。
        var realOut = Console.Out;
        var realErr = Console.Error;
        var noise = new StringWriter();
        Console.SetOut(noise);
        Console.SetError(noise);
        try
        {
            var prog = s.Compile(s.Source);
            if (s.PostLinkLang != null)
                CompilerHelper.LinkStandardLibrary((VMLAssembler.VmlProgram)prog!, s.PostLinkLang, null);
            // 编译"成功"了 —— 前端静默接受了一段本该报错的源码。
            return new Outcome(s, Verdict.NoError, null, null, null, null, "", null, "");
        }
        catch (CompilationException ex)
        {
            // ⚠ `--stack` 在这一支**同样要能出堆栈**，而且取的是 `InnerException`：
            //   `CompilerHelper`（1043 行附近）把内部异常**包装**成
            //   `CompilationException("<file>: 内部错误: <msg>", ex)` —— 正文只剩一句
            //   「Object reference not set…」，**真因与调用栈全在 InnerException 里**。
            //   只看 `ex.ToString()` 会得到"包装层"的栈（就在包装点上），
            //   那对定位毫无用处。实测就是这么被骗过一轮。
            return Judge(s, ex.Message ?? "", ex.GetType().Name, noise.ToString(),
                         Stack: ShowStack ? (ex.InnerException ?? ex).ToString() : null);
        }
        catch (Exception ex) when (ex is VMLAssembler.UnresolvedSymbolException)
        {
            // 链接期的「未定义函数」**是用户源码的错**，不是内部错误 ——
            // `CompilerHelper.CompileWithDiagnostics` 就是这么收编它的（那里有一条同样的注释）。
            // 而探测用的几个入口（CppCompiler / ForthCompiler 的 `Compile(string)`，
            // 以及探针自己补的那一步 `LinkStandardLibrary`）**不经过它**，会原样抛出来。
            // 探针按同一个口径收编，否则会把「用户写错了一个函数名」误判成 CRASH。
            return Judge(s, ex.Message ?? "", ex.GetType().Name, noise.ToString());
        }
        catch (Exception ex)
        {
            // 非编译错误（内部错误 / 未捕获异常）—— 位置判定不了，**必须单列**。
            // 那正是"修复没生效、只是换个地方崩"的伪装形态。
            var text = $"{ex.GetType().Name}: {ex.Message}";
            return new Outcome(s, Verdict.Crash, null, null, null, text.Replace('\n', ' '), text, ex.GetType().Name, noise.ToString(),
                               Stack: ShowStack ? ex.ToString() : null);
        }
        finally
        {
            Console.SetOut(realOut);
            Console.SetError(realErr);
        }
    }

    /// <summary>
    /// 判定一条测得的报错文本。**行与列两段判据只此一处** ——
    /// 「先判行、行对了再看列」这套顺序写两遍就会漂移（两条 catch 分支原来就各写了一遍）。
    ///
    /// 列只在样本显式给了 <see cref="Sample.ExpectedColumn"/> 时才判：
    /// 链接期那几门报的是「哪条 CALL 指令」、根本没有列的概念，判了就是假红。
    /// </summary>
    private static Outcome Judge(Sample s, string text, string exceptionType, string noise, string? Stack = null)
    {
        var pos = ExtractPosition(text);
        var first = FirstDiagnosticLine(text);
        if (pos is null)
            return new Outcome(s, Verdict.NoPosition, null, null, null, first, text, exceptionType, noise, Stack: Stack);

        var v = Verdict.Pass;
        // **文件先判、行后判**：本档的命题就是"错在哪个文件"，文件不对的严重性高于行号不对
        //（行号对了文件错了正是被修掉的那个 bug 的伪装形态）。
        if (s.ExpectedInFile != null && !FileMatches(pos.Value.File, s.ExpectedInFile))
            v = Verdict.WrongFile;
        // `Expected == 0` = **本档不判行**（与 `ExpectedColumn == 0` 同一套约定）。
        // 「块未闭合」那一档用它：锚在开块行还是锚在 EOF，各门习惯不同且都说得通，
        // 硬钉一个行号等于把一种随手选的约定当成判据。判据只压在"报不报"上。
        else if (s.Expected > 0 && pos.Value.Line != s.Expected)
            v = Verdict.WrongLine;
        else if (s.ExpectedColumn > 0 && pos.Value.Column != s.ExpectedColumn)
            v = Verdict.WrongColumn;

        return new Outcome(s, v, pos.Value.Line, pos.Value.Column, pos.Value.Kind, first, text,
                           exceptionType, noise, pos.Value.File, Stack);
    }

    /// <summary>
    /// 报错点名的文件是不是期望的那个头文件（按**文件名**比，不比整条路径 ——
    /// 临时目录的绝对路径每个机器都不一样，比全路径等于把判据钉在环境上）。
    /// </summary>
    private static bool FileMatches(string? found, string expectedInFile)
    {
        if (found == null) return false;
        var name = Path.GetFileName(found.Trim());
        return name.Equals(expectedInFile, StringComparison.OrdinalIgnoreCase);
    }

    // ── 从报错文本里抽行号 ───────────────────────────────────────────────────
    //
    // 三种形态（与 `CompilerError.ToString()` / 各前端的自定义文案对应）：
    //   1) GCC：`<file>:行:列: error: 消息`（`CompilerError.LocationString`，形状唯一）
    //   2) 中文：`在第 N 行…`
    //   3) 英文后缀：`… at line N`
    // **按顺序取第一条命中的** —— 用户先看到的就是它。
    private static (int Line, int Column, string Kind, string? File)? ExtractPosition(string text)
    {
        foreach (var raw in text.Split('\n'))
        {
            var line = raw.TrimEnd('\r').TrimStart();
            if (line.Length == 0) continue;
            // 跳过源码回显行（` 4 | int x = …`）与插入符行（` |     ^`）
            if (line.StartsWith("|") || Regex.IsMatch(line, @"^\d+\s*\|")) continue;

            // 带列：file:line:col:
            var m = Regex.Match(line, @"^(?<f>\S+?):(?<l>\d+):(?<c>\d+):");
            if (m.Success)
                return (int.Parse(m.Groups["l"].Value), int.Parse(m.Groups["c"].Value), "GCC", m.Groups["f"].Value);
            // 不带列：file:line:
            m = Regex.Match(line, @"^(?<f>\S+?):(?<l>\d+):");
            if (m.Success)
                return (int.Parse(m.Groups["l"].Value), -1, "GCC-无列", m.Groups["f"].Value);
        }

        var cn = Regex.Match(text, @"第\s*(\d+)\s*行");
        if (cn.Success) return (int.Parse(cn.Groups[1].Value), -1, "中文", null);

        var en = Regex.Match(text, @"at line\s+(\d+)", RegexOptions.IgnoreCase);
        if (en.Success) return (int.Parse(en.Groups[1].Value), -1, "英文", null);

        return null;
    }

    /// <summary>报错文本里的第一条诊断（给表格用的一行摘要）。</summary>
    private static string FirstDiagnosticLine(string text)
    {
        foreach (var raw in text.Split('\n'))
        {
            var line = raw.TrimEnd('\r').Trim();
            if (line.Length == 0) continue;
            if (line.StartsWith("|") || Regex.IsMatch(line, @"^\d+\s*\|")) continue;
            if (line == "^") continue;
            return line;
        }
        return "(空)";
    }

    // ── 输出 ─────────────────────────────────────────────────────────────────
    private static void PrintTable(List<Outcome> outcomes)
    {
        // 按**用例档**分段打印：未定义标识符 / 语法错误 两档各自一段。
        // 档与档之间不做任何汇总混算 —— 「这一档全绿」不等于「另一档也在绿」。
        foreach (var group in outcomes.Select(o => o.Sample.Group).Distinct())
        {
            Console.WriteLine($"── 用例档：{group} ──");
            Console.WriteLine($"{"语言",-10} {"期望行",-7} {"实得行",-7} {"期望列",-7} {"实得列",-7} {"结论",-7} 诊断");
            Console.WriteLine(new string('-', 120));
            foreach (var o in outcomes.Where(o => o.Sample.Group == group))
            {
                var found = o.FoundLine?.ToString() ?? "-";
                var expCol = o.Sample.ExpectedColumn > 0 ? o.Sample.ExpectedColumn.ToString() : "-";
                var col = o.FoundColumn is > 0 ? o.FoundColumn.Value.ToString() : "-";
                // 报错点名的文件（只有判得出文件名的形态才有）—— **一行里就要看得出
                // "指到哪个文件去了"**，否则 FILE/FAIL 只差一个字母，还得翻详情。
                var file = o.FoundFile != null ? $"[{Path.GetFileName(o.FoundFile)}] " : "";
                var brief = Truncate(file + (o.FirstMessage ?? ""), 58);
                Console.WriteLine($"{o.Sample.Lang,-10} {o.Sample.Expected,-7} {found,-7} {expCol,-7} {col,-7} {o.VerdictText,-7} {brief}");
            }
            Console.WriteLine(new string('-', 120));
        }
        Console.WriteLine();
    }

    private static void PrintDetails(List<Outcome> outcomes, bool full)
    {
        var bad = outcomes.Where(o => o.Verdict != Verdict.Pass).ToList();
        if (bad.Count == 0) return;

        Console.WriteLine();
        Console.WriteLine("================ 非 PASS 项详情 ================");
        foreach (var o in bad)
        {
            Console.WriteLine();
            Console.WriteLine($"── {o.Sample.Lang} (.{o.Sample.Ext}) [{o.VerdictText}] " +
                              $"期望第 {o.Sample.Expected} 行，实得 {(o.FoundLine?.ToString() ?? "无位置")}");
            Console.WriteLine($"   错误类型：{o.Sample.ErrorKind}");
            if (o.Sample.ExpectedInFile != null)
                Console.WriteLine($"   期望文件：{o.Sample.ExpectedInFile}（含此文件名的路径都算对）" +
                                  $"　实得文件：{o.FoundFile ?? "(报错文本里没点名文件)"}");
            Console.WriteLine("   样本源码：");
            for (int i = 0; i < o.Sample.Lines.Length; i++)
            {
                var mark = (i + 1) == o.Sample.Expected ? " >>" : "   ";
                Console.WriteLine($"{mark} {i + 1,3} | {o.Sample.Lines[i]}");
            }
            Console.WriteLine($"   异常类型：{o.ExceptionType}");
            Console.WriteLine("   报错文本：");
            var text = o.RawText.Length == 0 ? "(无 —— 编译成功)" : o.RawText;
            if (!full && text.Length > 1200) text = text[..1200] + "\n   …（截断，加 --full 看全文）";
            foreach (var l in text.TrimEnd('\n').Split('\n'))
                Console.WriteLine("     | " + l.TrimEnd('\r'));

            if (o.Stack != null)
            {
                Console.WriteLine("   调用栈（--stack）：");
                foreach (var l in o.Stack.TrimEnd('\n').Split('\n'))
                    Console.WriteLine("     @ " + l.TrimEnd('\r'));
            }

            // 编译期间被截下来的 stdout/stderr（链接器日志、各前端的 warning）。
            // `full` 之外只打尾巴 —— 有用的那几行通常在最后。
            var noise = o.Noise.TrimEnd('\n', '\r');
            if (noise.Length > 0)
            {
                var lines = noise.Split('\n');
                var show = full ? lines : lines.Skip(Math.Max(0, lines.Length - 8)).ToArray();
                if (!full && lines.Length > show.Length)
                    Console.WriteLine($"   编译日志（只显示最后 {show.Length} / 共 {lines.Length} 行，加 --full 看全部）：");
                else
                    Console.WriteLine("   编译日志：");
                foreach (var l in show)
                    Console.WriteLine("     # " + l.TrimEnd('\r'));
            }
        }
    }

    private static int PrintSummary(List<Outcome> outcomes)
    {
        Console.WriteLine("================ 汇总 ================");
        bool ok = true;
        foreach (var group in outcomes.Select(o => o.Sample.Group).Distinct())
        {
            var g = outcomes.Where(o => o.Sample.Group == group).ToList();
            int pass = g.Count(o => o.Verdict == Verdict.Pass);
            int wfile = g.Count(o => o.Verdict == Verdict.WrongFile);
            int wrong = g.Count(o => o.Verdict == Verdict.WrongLine);
            int col = g.Count(o => o.Verdict == Verdict.WrongColumn);
            int nopos = g.Count(o => o.Verdict == Verdict.NoPosition);
            int noerr = g.Count(o => o.Verdict == Verdict.NoError);
            int crash = g.Count(o => o.Verdict == Verdict.Crash);

            Console.WriteLine();
            Console.WriteLine($"【{group}】共 {g.Count} 门 —— PASS {pass} / 文件不对 {wfile} / 行不对 {wrong} / 列不对 {col} / " +
                              $"无位置 {nopos} / 没报错 {noerr} / 崩溃 {crash}");
            if (wfile > 0) Console.WriteLine($"  行对文件不对：{Names(g, Verdict.WrongFile)}");
            if (wrong > 0) Console.WriteLine($"  行号不对：{Names(g, Verdict.WrongLine)}");
            if (col > 0) Console.WriteLine($"  行对列不对：{Names(g, Verdict.WrongColumn)}");
            if (nopos > 0) Console.WriteLine($"  报了错但没位置：{Names(g, Verdict.NoPosition)}");
            if (noerr > 0) Console.WriteLine($"  该报错却没报：{Names(g, Verdict.NoError)}");
            if (crash > 0) Console.WriteLine($"  非编译错误（内部错误/未捕获异常）：{Names(g, Verdict.Crash)}");

            if (wfile + wrong + col + nopos + noerr + crash > 0) ok = false;
        }
        return ok ? 0 : 1;
    }

    private static string Names(List<Outcome> outcomes, Verdict v) =>
        string.Join(" ", outcomes.Where(o => o.Verdict == v).Select(o => o.Sample.Lang));

    private static string Truncate(string s, int max)
    {
        s = s.Replace('\n', ' ').Replace('\r', ' ').Trim();
        return s.Length <= max ? s : s[..max] + "…";
    }

/// <summary>用例档名 —— **只此一处**，表格与汇总都引它，避免字符串各写一遍。</summary>
internal static class Groups
{
    public const string Undef = "未定义标识符";
    public const string Syntax = "语法错误";
    public const string Header = "头文件里的错";
    /// <summary>档四：块开了没关 —— **必须报错**。判据只压在"报不报"上（`Expected = 0` 不判行）。</summary>
    public const string Unclosed = "块未闭合";
}

    // ── 用例表 ───────────────────────────────────────────────────────────────
    //
    // ⚠ 改样本时**期望行号/列号就在旁边**，挪行必须一起改 —— 样本与期望同处一处，
    //   就是为了让"挪了行忘了改期望"这种错误在 code review 里一眼看得见。
    //
    // ── 两档 ─────────────────────────────────────────────────────────────────
    //   · `未定义标识符` —— 走**语义/链接**期（错误由 codegen 的 `ReportUndefined` 或
    //     `LibraryLinker` 的未解析符号产生）。这一档是"位置计算"的主战场：
    //     列号就是从这一档调准的（22 门全部落到出错标识符的首字符上）。
    //   · `语法错误` —— 走**解析**期（`ParserBase.Error` / `ParseException`）。
    //     这一档此前是**空档**：探针只覆盖语义错误，于是"语法错误报的位置准不准"
    //     一直没人量。两档的代码路径完全不同（一个在 Parser、一个在 CodeGenerator），
    //     一档全绿证明不了另一档 —— 这正是「用一档冒充整体」的典型。
    // ── 「头文件里的错」那一档的两个小工具 ───────────────────────────────────
    //
    // 这一档**必须真写文件**（前两档刻意不写 —— `#include` 只有"真去读磁盘"一条路，
    // 而这里的命题恰恰是「拼接流里的行号能不能映射回它来自哪个文件」）。
    // 写进临时目录，每门一份 `probe_bad_<lang>.h`，**内容就在样本里**（自包含：
    // 不引用 `Examples/` 下的任何文件、也不碰用户的 `game17.cpp`）。
    // 每次启动重写一遍（内容变了立刻生效，不用手工清理）。
    private static string HeaderPath(string lang, string[] lines)
    {
        var dir = Path.Combine(Path.GetTempPath(), "vml_diagprobe_hdr");
        Directory.CreateDirectory(dir);
        var path = Path.Combine(dir, $"probe_bad_{lang}.h");
        // 无 BOM（本仓铁律：不给自己产的文件凭空加 BOM）。
        File.WriteAllText(path, string.Join("\n", lines) + "\n", new UTF8Encoding(false));
        // 统一用 `/` 分隔：`#include` 里的路径会原样进报错文本，
        // 反斜杠在部分语言的词法器里有转义含义，正斜杠三端都认。
        return path.Replace('\\', '/');
    }

    /// <summary>
    /// 主文件 = 前导 + 一句 `#include "<绝对路径>"` + 后置。
    ///
    /// ⚠ **前导必须排在那句 `#include` 之前**（不是装饰，是判据的一部分）：
    /// 头文件正文在拼接流里的行号因此比它自己的行号大，一个"没做映射"的实现
    /// 会报出偏移后的行号 —— 前导为零的话，拼接行号与头文件行号天然相等，
    /// 这一档就退化成"只要报了个位置就算过"。
    /// </summary>
    private static string[] WithInclude(string headerPath, string[] prelude, string[] postlude)
        => prelude.Append($"#include \"{headerPath}\"").Concat(postlude).ToArray();

    /// <summary>
    /// 「头文件里的错」那一档的样本工厂 —— **头文件正文只写一处**：
    /// 既拿去落盘，又经过 `#include` 进主文件；`ExpectedInFile` 由文件名推出来
    ///（写死第二份必然漂移）。
    /// </summary>
    private static Sample Hdr(string lang, string ext, string errorKind, int expected,
                              string[] header, string[] prelude, string[] postlude,
                              Func<string, object?> compile, string? postLink = null)
    {
        var path = HeaderPath(lang, header);
        return new Sample(lang, ext, Groups.Header, errorKind, expected, 0,
                          WithInclude(path, prelude, postlude), compile,
                          PostLinkLang: postLink, HeaderLines: header,
                          ExpectedInFile: $"probe_bad_{lang}.h");
    }

    // `internal`（不是 private）：`--truncate` 那一档（HangProbe.cs）要复用这张表的
    // 「语言 → 扩展名 → 编译入口 → 补链语言」接线。**不另建一张**——那正是本仓
    // 反复踩的「必须手工同步的平行表」。
    internal static List<Sample> Samples() =>
    [
        // ═══════════ 用例档一：未定义标识符 ═══════════
        // ── C 家族 ────────────────────────────────────────────────────────────
        new("c", "c", Groups.Undef, "未定义变量", 4, 20,
            ["int main(void) {", "    int a = 1;", "    int b = 2;", "    return a + b + nosuch;", "}"],
            s => LC.Compile(s)),

        new("cpp", "cpp", Groups.Undef, "未定义变量", 4, 20,
            ["int main() {", "    int a = 1;", "    int b = 2;", "    return a + b + nosuch;", "}"],
            s => LCpp.Compile(s)),

        new("objc", "m", Groups.Undef, "未定义变量", 4, 20,
            ["int main(void) {", "    int a = 1;", "    int b = 2;", "    return a + b + nosuch;", "}"],
            s => LObjC.Compile(s)),

        new("cs", "cs", Groups.Undef, "未定义变量", 4, 17,
            ["class P {", "  static void Main() {", "    int a = 1;", "    int b = a + nosuch;", "  }", "}"],
            s => new LCs().Compile(s)),

        new("java", "java", Groups.Undef, "未定义变量", 4, 17,
            ["public class P {", "  public static void main(String[] a) {", "    int x = 1;", "    int y = x + nosuch;", "  }", "}"],
            s => new LJava().Compile(s)),

        // ── JVM / .NET / Apple 系 ─────────────────────────────────────────────
        new("kt", "kt", Groups.Undef, "未定义变量", 4, 21,
            ["fun main() {", "    val a = 1", "    val b = 2", "    val c = a + b + nosuch", "}"],
            s => LKt.Compile(s)),

        new("swift", "swift", Groups.Undef, "未定义变量", 4, 21,
            ["func main() {", "    let a = 1", "    let b = 2", "    let c = a + b + nosuch", "}"],
            s => new LSwift().Compile(s)),

        new("d", "d", Groups.Undef, "未定义变量", 4, 21,
            ["void main() {", "    int a = 1;", "    int b = 2;", "    int c = a + b + nosuch;", "}"],
            s => LD.Compile(s)),

        new("dart", "dart", Groups.Undef, "未定义变量", 4, 19,
            ["void main() {", "  int a = 1;", "  int b = 2;", "  int c = a + b + nosuch;", "}"],
            s => LDart.Compile(s)),

        new("rs", "rs", Groups.Undef, "未定义变量", 4, 21,
            ["fn main() {", "    let a = 1;", "    let b = 2;", "    let c = a + b + nosuch;", "}"],
            s => LRust.Compile(s)),

        new("go", "go", Groups.Undef, "未定义变量", 4, 17,
            ["package main", "func main() {", "    println_int(1)", "    println_int(nosuch)", "}"],
            s => LGo.Compile(s)),

        // ── 传统系 ───────────────────────────────────────────────────────────
        new("f90", "f90", Groups.Undef, "未定义变量", 4, 12,
            ["program p", "  implicit none", "  print *, 1", "  print *, nosuch", "end program p"],
            s => LFortran.Compile(s)),

        new("pas", "pas", Groups.Undef, "未定义变量", 4, 11,
            ["program p;", "begin", "  WriteLn(1);", "  WriteLn(nosuch);", "end."],
            s => LPascal.Compile(s)),

        new("bas", "bas", Groups.Undef, "未定义变量", 4, 7,
            ["OPTION EXPLICIT", "PRINT 1", "PRINT 2", "PRINT nosuch"],
            s => LBasic.Compile(s)),

        // Forth 的 `Compile(string)` **不链标准库**（`CompileFile` 才链），
        // 而「未用到定义的词」只有链接器看得见 ⇒ 探针显式补一步 `LinkStandardLibrary`，
        // 否则量到的是"探针没接上链接"而不是前端行为。
        new("fth", "fth", Groups.Undef, "未定义字", 4, 0,
            ["( line 1 )", "( line 2 )", "( line 3 )", "nosuch ."],
            s => LForth.Compile(s), PostLinkLang: "forth"),

        new("ld", "ld", Groups.Undef, "未定义变量", 3, 11,
            ["PRINT_INT 1", "PRINT_INT 2", "PRINT_INT nosuch", "END_PROGRAM"],
            s => LLadder.Compile(s)),

        // ── 动态语言 6 门（未声明即隐式全局是**合法语义**，只能拿未定义**函数**测）──
        //
        // ⚠ 这 6 门的「未定义函数」**在链接期才报**（`LibraryLinker.ReportUnresolved`），
        //   而它手上只有一条 CALL 指令、没有列的概念 ⇒ `ExpectedColumn` 一律给 0
        //   （= 本档不判列）。给个假的期望值只会制造假红。
        new("py", "py", Groups.Undef, "未定义函数", 4, 0,
            ["println_int(1)", "println_int(2)", "println_int(3)", "nosuch(1)"],
            s => LPy.Compile(s)),

        new("rb", "rb", Groups.Undef, "未定义函数", 4, 0,
            ["puts(1)", "puts(2)", "puts(3)", "nosuch(1)"],
            s => LRb.Compile(s)),

        new("lua", "lua", Groups.Undef, "未定义函数", 4, 0,
            ["print(1)", "print(2)", "print(3)", "nosuch(1)"],
            s => LLua.Compile(s)),

        new("js", "js", Groups.Undef, "未定义函数", 4, 0,
            ["println_int(1);", "println_int(2);", "println_int(3);", "nosuch(1);"],
            s => new LJs().Compile(s)),

        new("r", "r", Groups.Undef, "未定义函数", 4, 0,
            ["cat(1)", "cat(2)", "cat(3)", "print(nosuch(1))"],
            s => LR.Compile(s)),

        new("scm", "scm", Groups.Undef, "未定义函数", 4, 0,
            ["(display 1)", "(display 2)", "(display 3)", "(nosuch 1)"],
            s => LScm.Compile(s)),

        // ═══════════ 用例档二：语法错误（解析期） ═══════════
        //
        // 形状统一取「**二元运算符后面缺右操作数**」（`a + ;` / `1 +`）：
        //   · 它是这些语言里都成立的**语法**错（不是语义错 —— 名字认不认识不该在这里报）；
        //   · 错误的位置就在那一行，不像"缺右括号"那样可能被报到块尾/EOF 上去。
        // 期望列一律先给 0（只判行）—— 这一档此前没人量过列，先拿到实测值再定判据，
        // 别拿"我以为的列"去当真值（本仓反复踩过：自测期望值写错，测出来的是用例自己错）。
        new("c", "c", Groups.Syntax, "语法错误", 4, 0,
            ["int main(void) {", "    int a = 1;", "    int b = 2;", "    int c = a + ;", "}"],
            s => LC.Compile(s)),

        new("cpp", "cpp", Groups.Syntax, "语法错误", 4, 0,
            ["int main() {", "    int a = 1;", "    int b = 2;", "    int c = a + ;", "}"],
            s => LCpp.Compile(s)),

        new("objc", "m", Groups.Syntax, "语法错误", 4, 0,
            ["int main(void) {", "    int a = 1;", "    int b = 2;", "    int c = a + ;", "}"],
            s => LObjC.Compile(s)),

        new("cs", "cs", Groups.Syntax, "语法错误", 4, 0,
            ["class P {", "  static void Main() {", "    int a = 1;", "    int b = a + ;", "  }", "}"],
            s => new LCs().Compile(s)),

        new("java", "java", Groups.Syntax, "语法错误", 4, 0,
            ["public class P {", "  public static void main(String[] a) {", "    int x = 1;", "    int y = x + ;", "  }", "}"],
            s => new LJava().Compile(s)),

        new("kt", "kt", Groups.Syntax, "语法错误", 4, 0,
            ["fun main() {", "    val a = 1", "    val b = 2", "    val c = a +", "}"],
            s => LKt.Compile(s)),

        new("swift", "swift", Groups.Syntax, "语法错误", 4, 0,
            ["func main() {", "    let a = 1", "    let b = 2", "    let c = a +", "}"],
            s => new LSwift().Compile(s)),

        new("d", "d", Groups.Syntax, "语法错误", 4, 0,
            ["void main() {", "    int a = 1;", "    int b = 2;", "    int c = a + ;", "}"],
            s => LD.Compile(s)),

        new("dart", "dart", Groups.Syntax, "语法错误", 4, 0,
            ["void main() {", "  int a = 1;", "  int b = 2;", "  int c = a + ;", "}"],
            s => LDart.Compile(s)),

        new("rs", "rs", Groups.Syntax, "语法错误", 4, 0,
            ["fn main() {", "    let a = 1;", "    let b = 2;", "    let c = a + ;", "}"],
            s => LRust.Compile(s)),

        new("go", "go", Groups.Syntax, "语法错误", 4, 0,
            ["package main", "func main() {", "    println_int(1)", "    println_int(1 + )", "}"],
            s => LGo.Compile(s)),

        new("f90", "f90", Groups.Syntax, "语法错误", 4, 0,
            ["program p", "  implicit none", "  print *, 1", "  print *, 1 +", "end program p"],
            s => LFortran.Compile(s)),

        new("pas", "pas", Groups.Syntax, "语法错误", 4, 0,
            ["program p;", "begin", "  WriteLn(1);", "  WriteLn(1 + );", "end."],
            s => LPascal.Compile(s)),

        new("bas", "bas", Groups.Syntax, "语法错误", 4, 0,
            ["PRINT 1", "PRINT 2", "PRINT 3", "PRINT 1 +"],
            s => LBasic.Compile(s)),

        // ⚠ **必须补 `PostLinkLang`**（与上一档同一处置，这里当初漏了）。
        //   Forth 是栈式语言：`+` 少操作数是**运行期**属性，不是语法错；
        //   这一句真正该报的是 `nosuch` / `nosuch2` 这两个**未定义的字**，
        //   而「未定义的字」只有**链接器**看得见（前端 `Compile(string)` 不链标准库）。
        //   不补这一步，量到的是"探针没接上链接"，而不是前端行为 —— 于是这一档
        //   长期显示 `NOERR`，看着像"Forth 静默接受了一段坏代码"，其实全是探针的锅。
        //   （同一类"用例写错了"本仓有前例：`undef-fn.go` 当年写成了 Rust 语法。）
        new("fth", "fth", Groups.Syntax, "语法错误", 4, 0,
            ["( line 1 )", "( line 2 )", "( line 3 )", "nosuch + nosuch2 +"],
            s => LForth.Compile(s), PostLinkLang: "forth"),

        new("ld", "ld", Groups.Syntax, "语法错误", 3, 0,
            ["PRINT_INT 1", "PRINT_INT 2", "PRINT_INT 1 +", "END_PROGRAM"],
            s => LLadder.Compile(s)),

        new("py", "py", Groups.Syntax, "语法错误", 4, 0,
            ["println_int(1)", "println_int(2)", "println_int(3)", "x = 1 +"],
            s => LPy.Compile(s)),

        new("rb", "rb", Groups.Syntax, "语法错误", 4, 0,
            ["puts(1)", "puts(2)", "puts(3)", "x = 1 +"],
            s => LRb.Compile(s)),

        new("lua", "lua", Groups.Syntax, "语法错误", 4, 0,
            ["print(1)", "print(2)", "print(3)", "x = 1 +"],
            s => LLua.Compile(s)),

        new("js", "js", Groups.Syntax, "语法错误", 4, 0,
            ["println_int(1);", "println_int(2);", "println_int(3);", "var x = 1 + ;"],
            s => new LJs().Compile(s)),

        new("r", "r", Groups.Syntax, "语法错误", 4, 0,
            ["cat(1)", "cat(2)", "cat(3)", "x <- 1 +"],
            s => LR.Compile(s)),

        new("scm", "scm", Groups.Syntax, "语法错误", 4, 0,
            ["(display 1)", "(display 2)", "(display 3)", "(display (+ 1))"],
            s => LScm.Compile(s)),

        // ═══════════ 用例档三：头文件里的错 ═══════════
        //
        // ── 这一档要回答的问题 ────────────────────────────────────────────────
        // 用户报过：「我现在打开的文件，报 112 行错误，但是这里报错是不对的」——
        // 他的第 112 行是一句无害的注释，**真正的错在 `#include` 进来的头文件里**。
        // `#include` 是把头文件正文**拼进同一个流**的，于是词法/语法/代码生成拿到的
        // 行号都是**拼接后**的；映射表（`Preprocessor.LineMap`）一直都在，
        // 缺的只是"谁把它交给报错那一端"。前两档量不到这件事：它们的样本里没有 `#include`。
        //
        // ── 样本形状（22 门统一）──────────────────────────────────────────────
        //   主文件 = 两行前导（该语言最平凡的合法代码）+ 一句 `#include "<临时目录>/probe_bad_<lang>.h"`
        //           + 可选后置；**错埋在头文件的第 2 行**。
        //   期望行 = 头文件里的那一行（= 2），期望文件 = 那个头文件（`WrongFile` 那一档）。
        //
        // ⚠ 前导那两行**必须自己不出错**，否则第一个错误变成前导里的错，量到的就不是头文件
        //   （BASIC 第一版就是这么废掉的：`OPTION EXPLICIT` 一开，前导里的
        //   `LET probeMainA = 1` 自己先报「未声明的变量」，而且它排在头文件那条前面）。
        //
        // ── 错误类型按语言选 ─────────────────────────────────────────────────
        //   · **静态语言**：头文件里**用了一个没声明的名字** —— 就是用户真遇到的那一类
        //     （`Lib/c/time.h` 里的 `typedef struct tm tm_t;` 让下游报「未声明的变量」）。
        //     注意要放进**函数 / 方法体**里：C / C# / Java 的**全局 / 字段初始化式**
        //     是不被检查的（实测那三门对整个 NOERR，前端连错都不报）。
        //   · **动态语言 6 门**：「未声明即隐式全局」是合法语义
        //     （见 `CodeGeneratorBase.ImplicitDeclarationAllowed`），取语法错。
        //     其中 js / scm 两门的未定义**函数**只能由链接器报（它手上只有一条 CALL 指令，
        //     见 `LibraryLinker.ReportUnresolved`），而链接器手里是**拼接流**的位置 ——
        //     这两门 + Forth 的实测结论就是「文件不对」，见汇总。

        // ⚠ C 这一门的主文件**必须真的调用**头文件里那个坏函数（`return probe_b();`）：
        //   C 的 codegen 不生成**没人调用**的函数体 ⇒ 主文件写 `return 0;` 时整个 NOERR
        //   （实测：`int main(void) { return 0; }` 编译"成功"，换成 `return probe_b();` 立刻报出来）。
        //   cpp / objc 没有这个行为（不调用也照样生成），但既然真实程序就是会调用它，
        //   三门统一这么写更贴近实际。
        Hdr("c", "c", "头文件里的未声明变量", 2,
            ["int probe_ok_a(void) { return 1; }",
             "int probe_b(void) { return nosuch_ident; }",
             "int probe_ok_b(void) { return 2; }"],
            ["int probe_main_a = 1;", "int probe_main_b = 2;"],
            ["int main(void) { return probe_b(); }"],
            s => LC.Compile(s)),

        Hdr("cpp", "cpp", "头文件里的未声明变量", 2,
            ["int probe_ok_a(void) { return 1; }",
             "int probe_b(void) { return nosuch_ident; }",
             "int probe_ok_b(void) { return 2; }"],
            ["int probe_main_a = 1;", "int probe_main_b = 2;"],
            ["int main() { return probe_b(); }"],
            s => LCpp.Compile(s)),

        Hdr("objc", "m", "头文件里的未声明变量", 2,
            ["int probe_ok_a(void) { return 1; }",
             "int probe_b(void) { return nosuch_ident; }",
             "int probe_ok_b(void) { return 2; }"],
            ["int probe_main_a = 1;", "int probe_main_b = 2;"],
            ["int main(void) { return probe_b(); }"],
            s => LObjC.Compile(s)),

        Hdr("cs", "cs", "头文件里的未声明变量", 2,
            ["  static int probeBadA = 1;",
             "  static void Bad() { int x = nosuch_ident; }",
             "  static int probeBadB = 2;"],
            ["class ProbeMain {", "  static int probeMainA = 1;"],
            ["  static void Main() { }", "}"],
            s => new LCs().Compile(s)),

        Hdr("java", "java", "头文件里的未声明变量", 2,
            ["  static int probeBadA = 1;",
             "  static void bad() { int x = nosuch_ident; }",
             "  static int probeBadB = 2;"],
            ["public class ProbeMain {", "  static int probeMainA = 1;"],
            ["  public static void main(String[] a) { }", "}"],
            s => new LJava().Compile(s)),

        Hdr("kt", "kt", "头文件里的未声明变量", 2,
            ["val probeOkA = 1", "val probeB = nosuch_ident", "val probeOkB = 2"],
            ["val probeMainA = 1", "val probeMainB = 2"],
            ["fun main() { }"],
            s => LKt.Compile(s)),

        Hdr("swift", "swift", "头文件里的未声明变量", 2,
            ["let probeOkA = 1", "let probeB = nosuch_ident", "let probeOkB = 2"],
            ["let probeMainA = 1", "let probeMainB = 2"],
            ["func main() { }"],
            s => new LSwift().Compile(s)),

        Hdr("d", "d", "头文件里的未声明变量", 2,
            ["int probeOkA = 1;", "int probeB = nosuch_ident;", "int probeOkB = 2;"],
            ["int probeMainA = 1;", "int probeMainB = 2;"],
            ["void main() { }"],
            s => LD.Compile(s)),

        Hdr("dart", "dart", "头文件里的未声明变量", 2,
            ["int probeOkA = 1;", "int probeB = nosuch_ident;", "int probeOkB = 2;"],
            ["int probeMainA = 1;", "int probeMainB = 2;"],
            ["void main() { }"],
            s => LDart.Compile(s)),

        Hdr("rs", "rs", "头文件里的未声明变量", 2,
            ["fn probe_ok_a() -> i32 { 1 }",
             "fn probe_b() -> i32 { nosuch_ident }",
             "fn probe_ok_b() -> i32 { 2 }"],
            ["fn probe_main_a() -> i32 { 1 }", "fn probe_main_b() -> i32 { 2 }"],
            ["fn main() { }"],
            s => LRust.Compile(s)),

        Hdr("go", "go", "头文件里的未声明变量", 2,
            ["func probeOkA() { println_int(1) }",
             "func probeB() { println_int(nosuch_ident) }",
             "func probeOkB() { println_int(2) }"],
            ["package main", "func probeMainA() { println_int(1) }"],
            ["func main() { }"],
            s => LGo.Compile(s)),

        // Fortran / Pascal / BASIC / Ladder 的**程序单元**结构不允许"半句话"，
        // 所以头文件正文落在单元内部：前导开单元、后置收单元，错误仍在头文件第 2 行。
        Hdr("f90", "f90", "头文件里的未声明变量", 2,
            ["      integer :: probe_ok_a",
             "      probe_b = nosuch_ident",
             "      integer :: probe_ok_b"],
            ["      program probemain", "      implicit none"],
            ["      print *, 1", "      end program probemain"],
            s => LFortran.Compile(s)),

        Hdr("pas", "pas", "头文件里的未声明变量", 2,
            ["  WriteLn(7);", "  WriteLn(nosuch);", "  WriteLn(3);"],
            ["program probemain;", "begin"],
            ["  WriteLn(1);", "end."],
            s => LPascal.Compile(s)),

        // ⚠ 前导里**不能**出现未声明变量：头文件里那句 `OPTION EXPLICIT` 一生效，
        //   前导自己的 `LET` 就会被判定为未声明，而那条错误排在头文件那条**前面**。
        Hdr("bas", "bas", "头文件里的未声明变量", 2,
            ["OPTION EXPLICIT", "PRINT nosuch", "PRINT 3"],
            ["PRINT 1", "PRINT 2"],
            [],
            s => LBasic.Compile(s)),

        Hdr("ld", "ld", "头文件里的未声明变量", 2,
            ["PRINT_INT 7", "PRINT_INT nosuch", "END_PROGRAM"],
            ["PRINT_INT 1", "PRINT_INT 2"],
            [],
            s => LLadder.Compile(s)),

        Hdr("py", "py", "头文件里的语法错误", 2,
            ["probe_ok_a = 1", "probe_b = )", "probe_ok_b = 2"],
            ["probe_main_a = 1", "probe_main_b = 2"],
            [],
            s => LPy.Compile(s)),

        Hdr("rb", "rb", "头文件里的语法错误", 2,
            ["probe_ok_a = 1", "probe_b = )", "probe_ok_b = 2"],
            ["probe_main_a = 1", "probe_main_b = 2"],
            [],
            s => LRb.Compile(s)),

        Hdr("lua", "lua", "头文件里的语法错误", 2,
            ["probe_ok_a = 1", "probe_b = )", "probe_ok_b = 2"],
            ["probe_main_a = 1", "probe_main_b = 2"],
            [],
            s => LLua.Compile(s)),

        Hdr("r", "r", "头文件里的语法错误", 2,
            ["probe_ok_a <- 1", "probe_b <- )", "probe_ok_b <- 2"],
            ["probe_main_a <- 1", "probe_main_b <- 2"],
            [],
            s => LR.Compile(s)),

        // js / scm：未定义**函数**由链接器报（前端 codegen 对这两门豁免「未声明」）。
        // 探针量到的就是链接器那一条 —— 实测**没**指到头文件（汇总里的「文件不对」）。
        Hdr("js", "js", "头文件里的未定义函数", 2,
            ["var probe_ok_a = 1;", "probe_b();", "var probe_ok_b = 2;"],
            ["var probe_main_a = 1;", "var probe_main_b = 2;"],
            [],
            s => new LJs().Compile(s)),

        Hdr("scm", "scm", "头文件里的未定义函数", 2,
            ["(define probe-ok-a 1)", "(define probe-b (nosuch-ident))", "(define probe-ok-b 2)"],
            ["(define probe-main-a 1)", "(define probe-main-b 2)"],
            [],
            s => LScm.Compile(s)),

        // Forth 的 `Compile(string)` 不链标准库 ⇒ 补一步（与第一档同一处置）。
        Hdr("fth", "fth", "头文件里的未定义字", 2,
            ["( probe bad )", "nosuch .", ": probe-ok 1 ;"],
            ["( probe main 1 )", "( probe main 2 )"],
            [],
            s => LForth.Compile(s), postLink: "forth"),

        // ══════════════════════════════════════════════════════════════════════
        //  档四：块未闭合 —— **必须报错**
        // ══════════════════════════════════════════════════════════════════════
        //
        // 命题（用户定的规矩）：**所有语言的块必须闭合，不闭合的代码必须报错**。
        //
        // ⚠ 本档的 `Expected` **一律 0**（= 不判行，只要求"报了错且有位置"）。
        //   这是刻意的：锚在**开块那一行**还是锚在 **EOF**，各门习惯不同且都说得通
        //   （GCC 报在 `end of input`；本仓 C 修完锚在开括号）。
        //   硬钉一个期望行号，等于把我随手选的一种约定变成判据 ——
        //   那是"用一档冒充整体"的变体。判据只压在**"报不报"**上，
        //   那正是用户那句要求本身。
        //
        // 每一份样本都**恰好只差最后一行闭合符**：闭上的那一版**实测 22/22 全部编过**
        //   （见提交信息）。所以本档一旦红，红的必然是"未闭合没报错"这一件事，
        //   不可能是"样本本身写坏了"。
        new("c", "c", Groups.Unclosed, "块未闭合", 0, 0,
            ["int main(void) {", "    int a = 1;", "    return a;"],
            s => LC.Compile(s)),

        new("cpp", "cpp", Groups.Unclosed, "块未闭合", 0, 0,
            ["int main() {", "    int a = 1;", "    return a;"],
            s => LCpp.Compile(s)),

        new("objc", "m", Groups.Unclosed, "块未闭合", 0, 0,
            ["int main(void) {", "    int a = 1;", "    return a;"],
            s => LObjC.Compile(s)),

        new("cs", "cs", Groups.Unclosed, "块未闭合", 0, 0,
            ["class P {", "  static void Main() {", "    int a = 1;", "  }"],
            s => new LCs().Compile(s)),

        new("java", "java", Groups.Unclosed, "块未闭合", 0, 0,
            ["public class P {", "  public static void main(String[] a) {", "    int x = 1;", "  }"],
            s => new LJava().Compile(s)),

        new("kt", "kt", Groups.Unclosed, "块未闭合", 0, 0,
            ["fun main() {", "    val a = 1", "    val b = 2"],
            s => LKt.Compile(s)),

        new("swift", "swift", Groups.Unclosed, "块未闭合", 0, 0,
            ["func main() {", "    let a = 1", "    let b = 2"],
            s => new LSwift().Compile(s)),

        new("d", "d", Groups.Unclosed, "块未闭合", 0, 0,
            ["void main() {", "    int a = 1;", "    int b = 2;"],
            s => LD.Compile(s)),

        new("dart", "dart", Groups.Unclosed, "块未闭合", 0, 0,
            ["void main() {", "  int a = 1;", "  int b = 2;"],
            s => LDart.Compile(s)),

        new("rs", "rs", Groups.Unclosed, "块未闭合", 0, 0,
            ["fn main() {", "    let a = 1;", "    let b = 2;"],
            s => LRust.Compile(s)),

        new("go", "go", Groups.Unclosed, "块未闭合", 0, 0,
            ["package main", "func main() {", "    println_int(1)"],
            s => LGo.Compile(s)),

        new("js", "js", Groups.Unclosed, "块未闭合", 0, 0,
            ["function f(x) {", "  return x + 1;"],
            s => new LJs().Compile(s)),

        new("r", "r", Groups.Unclosed, "块未闭合", 0, 0,
            ["f <- function(x) {", "  x + 1"],
            s => LR.Compile(s)),

        new("scm", "scm", Groups.Unclosed, "块未闭合", 0, 0,
            ["(define (f x)", "  (+ x 1)"],
            s => LScm.Compile(s)),

        new("f90", "f90", Groups.Unclosed, "块未闭合", 0, 0,
            ["program p", "  implicit none", "  print *, 1"],
            s => LFortran.Compile(s)),

        new("pas", "pas", Groups.Unclosed, "块未闭合", 0, 0,
            ["program p;", "begin", "  WriteLn(1);"],
            s => LPascal.Compile(s)),

        new("bas", "bas", Groups.Unclosed, "块未闭合", 0, 0,
            ["IF 1 THEN", "PRINT 1"],
            s => LBasic.Compile(s)),

        new("py", "py", Groups.Unclosed, "块未闭合", 0, 0,
            ["def f(x):"],
            s => LPy.Compile(s)),

        new("rb", "rb", Groups.Unclosed, "块未闭合", 0, 0,
            ["def f(x)", "  x + 1"],
            s => LRb.Compile(s)),

        new("lua", "lua", Groups.Unclosed, "块未闭合", 0, 0,
            ["function f(x)", "  return x + 1"],
            s => LLua.Compile(s)),

        // ⚠ ladder 这份**返工过一次**，记下来免得下次又写错：
        //   第一版写的是 `PRINT_INT 1`（裸打印语句），探针报 NOERR —— 看着像"ladder 不收块"，
        //   其实**是样本没写块**：`END_PROGRAM` 在本方言里**是可选**的（三份随包例程一份都没写），
        //   而裸语句模式（省略 `BEGIN`）本来就只有打印语句。真正的块是 `IF/END_IF`、
        //   `WHILE/END_WHILE`、`FUNCTION/END_FUNCTION` —— 实测未闭合那几个**都会报错**（ladder 没问题）。
        //   还是本仓那条老账：**先怀疑用例，再怀疑实现**（`undef-fn.go` 写成 Rust 语法那次）。
        new("ld", "ld", Groups.Unclosed, "块未闭合", 0, 0,
            ["BEGIN", "IF 1 THEN"],
            s => LLadder.Compile(s)),

        new("fth", "fth", Groups.Unclosed, "块未闭合", 0, 0,
            [": sq", "  dup *"],
            s => LForth.Compile(s), PostLinkLang: "forth"),
    ];
}
