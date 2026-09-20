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
// 判据（五档，**「没报错」与「报了但没位置」都算 FAIL**，且各自单列一档）
// ---------------------------------------------------------------------------
//   PASS    报了编译错误，且报错文本里的行号 == 期望行号
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
// ---------------------------------------------------------------------------
// 样本规则
// ---------------------------------------------------------------------------
// · **每门语言一份最小样本**，错误埋在一个**明确的行**上（= `Lines` 数组下标 + 1）。
// · 只用该语言最基本的语法，**不碰标准库、不用花哨特性** —— 否则前端会先挂在
//   别的地方，量到的就是别的问题（这一条是 `scripts/vml-diag-probe/README.md`
//   里用真金白银换来的：`undef-fn.go` 当年写成了 Rust 语法，测出来的是"用例写错了"）。
// · 错误类型统一取「**读一个未定义的标识符**」：
//     - 静态类型语言 → 未声明的**变量**（c/cpp/cs/java/kt/swift/d/objc/rs/go/f90/pas/bas/fth/ld/dart）
//     - 动态语言 6 门 → 未声明的**函数**（py/rb/lua/js/r/scm）
//       因为这 6 门的「未声明即隐式全局」是**合法语义**（见 `vml-diag-probe` 的
//       `dyn-global` 组），用未声明变量测不出错来。
// · 每份样本都有 1~3 行**有效的前导语句**，让错误落在第 3~4 行而不是第 1 行 ——
//   第 1 行出错太容易"碰巧对"，量不出真实的位置计算。
//
// ---------------------------------------------------------------------------
// 为什么不写文件、不走 CLI
// ---------------------------------------------------------------------------
// 直接调各前端的 `Compile(string)` 入口（进程内），**不经文件读写**：
// 这样量到的纯粹是「**前端自己的位置计算**」，与宿主/编辑器那一侧
// （文件路径、BOM、编码、`VmlDiagnostics` 解析）完全解耦 —— 两者混在一起量，
// 出了偏差分不清是谁的锅。
// `scripts/vml-diag-probe/run.sh` 走的是外部 CLI 进程、判的是"有没有点名符号"；
// 本探针走进程内、判的是"**行号对不对**"。两者互补，不重复。
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
    string? PostLinkLang = null)
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
    string Noise)           // 编译期间被探针截下来的 stdout/stderr（链接器日志等）
{
    public string VerdictText => Verdict switch
    {
        Verdict.Pass => "PASS",
        Verdict.WrongLine => "FAIL",
        Verdict.WrongColumn => "COL",
        Verdict.NoPosition => "NOPOS",
        Verdict.NoError => "NOERR",
        _ => "CRASH",
    };
}

internal static class Program
{
    private static int Main(string[] args)
    {
        bool full = args.Contains("--full");
        bool listOnly = args.Contains("--list");
        var filters = args.Where(a => !a.StartsWith("--")).ToArray();

        var samples = Samples();
        if (listOnly)
        {
            foreach (var s in samples)
                Console.WriteLine($"{s.Lang,-10} .{s.Ext,-6} 期望第 {s.Expected} 行  {s.ErrorKind}");
            Console.WriteLine($"共 {samples.Count} 门");
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
            return Judge(s, ex.Message ?? "", ex.GetType().Name, noise.ToString());
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
            return new Outcome(s, Verdict.Crash, null, null, null, text.Replace('\n', ' '), text, ex.GetType().Name, noise.ToString());
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
    private static Outcome Judge(Sample s, string text, string exceptionType, string noise)
    {
        var pos = ExtractPosition(text);
        var first = FirstDiagnosticLine(text);
        if (pos is null)
            return new Outcome(s, Verdict.NoPosition, null, null, null, first, text, exceptionType, noise);

        var v = Verdict.Pass;
        if (pos.Value.Line != s.Expected)
            v = Verdict.WrongLine;
        else if (s.ExpectedColumn > 0 && pos.Value.Column != s.ExpectedColumn)
            v = Verdict.WrongColumn;

        return new Outcome(s, v, pos.Value.Line, pos.Value.Column, pos.Value.Kind, first, text, exceptionType, noise);
    }

    // ── 从报错文本里抽行号 ───────────────────────────────────────────────────
    //
    // 三种形态（与 `CompilerError.ToString()` / 各前端的自定义文案对应）：
    //   1) GCC：`<file>:行:列: error: 消息`（`CompilerError.LocationString`，形状唯一）
    //   2) 中文：`在第 N 行…`
    //   3) 英文后缀：`… at line N`
    // **按顺序取第一条命中的** —— 用户先看到的就是它。
    private static (int Line, int Column, string Kind)? ExtractPosition(string text)
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
                return (int.Parse(m.Groups["l"].Value), int.Parse(m.Groups["c"].Value), "GCC");
            // 不带列：file:line:
            m = Regex.Match(line, @"^(?<f>\S+?):(?<l>\d+):");
            if (m.Success)
                return (int.Parse(m.Groups["l"].Value), -1, "GCC-无列");
        }

        var cn = Regex.Match(text, @"第\s*(\d+)\s*行");
        if (cn.Success) return (int.Parse(cn.Groups[1].Value), -1, "中文");

        var en = Regex.Match(text, @"at line\s+(\d+)", RegexOptions.IgnoreCase);
        if (en.Success) return (int.Parse(en.Groups[1].Value), -1, "英文");

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
                var brief = Truncate(o.FirstMessage ?? "", 58);
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
            int wrong = g.Count(o => o.Verdict == Verdict.WrongLine);
            int col = g.Count(o => o.Verdict == Verdict.WrongColumn);
            int nopos = g.Count(o => o.Verdict == Verdict.NoPosition);
            int noerr = g.Count(o => o.Verdict == Verdict.NoError);
            int crash = g.Count(o => o.Verdict == Verdict.Crash);

            Console.WriteLine();
            Console.WriteLine($"【{group}】共 {g.Count} 门 —— PASS {pass} / 行不对 {wrong} / 列不对 {col} / " +
                              $"无位置 {nopos} / 没报错 {noerr} / 崩溃 {crash}");
            if (wrong > 0) Console.WriteLine($"  行号不对：{Names(g, Verdict.WrongLine)}");
            if (col > 0) Console.WriteLine($"  行对列不对：{Names(g, Verdict.WrongColumn)}");
            if (nopos > 0) Console.WriteLine($"  报了错但没位置：{Names(g, Verdict.NoPosition)}");
            if (noerr > 0) Console.WriteLine($"  该报错却没报：{Names(g, Verdict.NoError)}");
            if (crash > 0) Console.WriteLine($"  非编译错误（内部错误/未捕获异常）：{Names(g, Verdict.Crash)}");

            if (wrong + col + nopos + noerr + crash > 0) ok = false;
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
    private static List<Sample> Samples() =>
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

        new("fth", "fth", Groups.Syntax, "语法错误", 4, 0,
            ["( line 1 )", "( line 2 )", "( line 3 )", "nosuch + nosuch2 +"],
            s => LForth.Compile(s)),

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
    ];
}
