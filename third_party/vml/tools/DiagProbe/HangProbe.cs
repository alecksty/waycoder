// ============================================================================
//  HangProbe —— 「**编译器不许挂死**」这一档的判据。
//
//  用法：`diagprobe --truncate <语言>`（语言名与 `--list` 的键同源）
//
//  ## 为什么要有它，以及为什么不放在 DiagProbe 的表格里
//
//  起因是真事（v0.96.321 之前的 Dart 前端）：一份**写了一半**的源文件会让编译器
//  **死循环** —— 进程不退出、不打任何输出。用户那边看到的是「点了编译，界面永远
//  卡在编译中」，而**构建全绿、语料里全绿**（语料里全是完整文件）。
//
//  那个具体缺陷（`Check` 的 `IsAtEnd` 短路让 `Check(EOF)` 永假，104 处）已经修了，
//  另加了运行期兜底 `ProgressGuard`（「位置长时间不动 ⇒ 判死循环」，抛带位置的错
//  而不是挂死）。但**这两条都不构成判据**：
//    · 修的是**已知**的那一处；
//    · `ProgressGuard` 只挂在解析器 `Cur` 与词法器 `Peek` 两处热路径 ——
//      循环里若不读当前位置（比如纯字符串处理），它根本不会触发。
//
//  ⇒ 真正能钉住「不许挂死」的，只有**喂它一份不完整的输入、要求它在有限时间内退出**。
//  这就是本档：把 `Examples/<语言>/` 里的真实例程**按行截断**成几十份不完整的源码，
//  逐份编译，**判据只有一条 —— 必须返回**。
//
//  ## 为什么不报「位置」（所以单列一档、不进 DiagProbe 的表格）
//
//  位置那一套（三档 22/22）问的是「报得准不准」；本档问的是「**还有没有反应**」。
//  两者极性不同：一份截断的源码**报不报错、报在哪一行都不作要求**（截断点本来
//  就没意义），只有「挂住」是失败。混进同一张表会让「位置对了」与「没挂住」
//  互相稀释 —— 与 `vml-out-probe`／`vml-diag-probe` 当初必须分开是同一个道理。
//
//  ## 判据（**只看是否返回**，不看报得对不对）
//
//  | 档 | 含义 | 算不算失败 |
//  |---|---|---|
//  | `HANG` | 超时未返回 —— **用户要防的就是这个** | ✘ 失败 |
//  | `CRASH` | 未捕获异常（内部错误）—— 返回了，但把「编译器崩了」推给用户 | ✘ 失败 |
//  | `ERR` | 报了一条诊断后返回 | ✓ |
//  | `PASS` | 编译通过 | ✓（**不判对错**：截断点落在完整语句之后本就该通过） |
//  | `NOBASE` / `BASEBAD` | **探针自己的问题**（找不到例程 / 例程本身编不过） | ⚠ 单列，混进失败会误导 |
//
//  ⚠ `PASS` 那一档**不是"静默错编"的判据** —— 截断到某一行恰好是完整的程序，
//  本来就该通过。要查「静默错编」得另开一档（判据是"这段源码**本该**报错"），
//  别把这两个问题混在一个数字里。
// ============================================================================

using System.Text;
using CompilerBase;
using VMLAssembler;

namespace DiagProbe;

internal static class HangProbe
{
    /// <summary>单次编译的时限。**判据是"无限循环"，所以给多大都行** ——
    /// 给到 15 秒是为了让"慢但合法"的大文件不至于假红（真机上 C 前端整份要一分多钟，
    /// 桌面版量级在秒级；截断后的前缀只会更短）。</summary>
    private const int DefaultTimeoutMs = 15000;

    /// <summary>每个基准文件最多截几个点。</summary>
    private const int DefaultMaxCuts = 40;

    /// <summary>每种语言最多试几个候选例程（试到能编过的就留下当基准）。</summary>
    private const int MaxBaseCandidates = 6;

    /// <summary>每种语言最多留几个基准。多个基准 = 多种语法形态，不只一种写法。</summary>
    private const int MaxBasesPerLang = 3;

    /// <summary>少于这么多行的例程不当基准 —— 连声明带体都放不下，切不出有意义的形态。</summary>
    private const int MinBaseLines = 3;

    /// <summary>挑基准时"结构点"算到多少就够 —— 再多也不增加形态多样性，只增加编译时间。</summary>
    private const int EnoughOpeners = 20;

    private sealed record CutResult(int LinesKept, string Verdict, string Detail, string Tail);

    private static int _timeoutMs = DefaultTimeoutMs;
    private static int _maxCuts = DefaultMaxCuts;

    // ── 入口 ─────────────────────────────────────────────────────────────────
    internal static int Run(string[] langs, string? vmlHome)
    {
        Console.OutputEncoding = Encoding.UTF8;

        if (vmlHome is null)
        {
            Console.Error.WriteLine("✘ 找不到 vendored 的 third_party/vml（含 Lib/ 与 VMLPrepares/ 的那一层）。");
            Console.Error.WriteLine("  本档要靠 `Examples/<语言>/` 的真实例程做截断源，没有它就无从量起。");
            return 2;
        }

        // 语言接线**直接复用 `Samples()`** —— 它已经是「语言 → 扩展名 → 编译入口 →
        // 补链语言」的唯一真源。另建一张表就是本仓头号坑（平行表必然漂移）。
        var wiring = Program.Samples()
            .GroupBy(s => s.Lang)
            .Select(g => g.First())
            .Where(s => langs.Length == 0 || langs.Contains(s.Lang))
            .OrderBy(s => s.Lang, StringComparer.Ordinal)
            .ToList();

        if (langs.Length > 0)
        {
            var unknown = langs.Where(l => wiring.All(s => s.Lang != l)).ToArray();
            if (unknown.Length > 0)
            {
                Console.Error.WriteLine($"✘ 未知语言：{string.Join(", ", unknown)}（用 --list 看全部）");
                return 2;
            }
        }

        Console.WriteLine($"HangProbe —— 「截断输入必须有限时间内返回」（{wiring.Count} 门，单次时限 {_timeoutMs} ms）");
        Console.WriteLine();

        int bad = 0, noBase = 0, totalCuts = 0;
        var hangs = new List<(string Lang, string File, CutResult R)>();
        var crashes = new List<(string Lang, string File, CutResult R)>();

        foreach (var w in wiring)
        {
            // 候选来源：语言自己的 `Examples/<语言>/` 在前，共享的 `_selftest/out.<ext>` 兜底。
            // ⚠ **`_selftest` 一定要排在后面**：它按字母序在 `_` 打头、天然排在 `c`/`go`… 之前，
            //   第一版 `FindExamplesDir` 取"第一个含该扩展名的目录"，于是 22 门**全部**落到
            //   这份 3~10 行的共享最小语料上 —— 量是量到了，但形态单一到近乎没量。
            var cands = new List<string>();
            var own = FindExamplesDir(vmlHome, w.Ext);
            if (own != null) cands.AddRange(Candidates(own, w.Ext));
            var selfTest = Path.Combine(vmlHome, "Examples", "_selftest", "out." + w.Ext);
            if (File.Exists(selfTest)) cands.Add(selfTest);

            if (cands.Count == 0)
            {
                Console.WriteLine($"{w.Lang,-6} NOBASE    Examples/ 下没有可用的 .{w.Ext}（探针问题）");
                noBase++;
                continue;
            }

            // 逐个试，**凡能编过的都留下当基准**（不只留第一个）—— 基准编不过的话，
            // 后面每一刀都只会"报错"，量到的全是噪音、看不出挂住没有。
            // 留下多个 = 覆盖多种语法形态（函数 / 类 / 嵌套块 / 字符串…）。
            var bases = new List<(string Path, string[] Lines)>();
            var tried = new List<string>();
            foreach (var f in cands.Take(MaxBaseCandidates + MaxBasesPerLang))
            {
                if (bases.Count >= MaxBasesPerLang) break;
                string src;
                try { src = File.ReadAllText(f); } catch { continue; }
                var r = Compile(src, w);
                if (r.Verdict == "PASS")
                    bases.Add((f, src.Replace("\r\n", "\n").Replace('\r', '\n').TrimEnd('\n').Split('\n')));
                else
                    tried.Add($"{Path.GetFileName(f)}→{r.Verdict}");
            }

            if (bases.Count == 0)
            {
                Console.WriteLine($"{w.Lang,-6} BASEBAD   候选例程都编不过 ⇒ 本轮量不到（探针问题，不是前端问题）");
                Console.WriteLine($"        试过：{string.Join("  ", tried)}");
                noBase++;
                continue;
            }

            var results = new List<CutResult>();
            string? hangFile = null;
            foreach (var (baseFile, lines) in bases)
            {
                foreach (var k in CutPoints(lines, _maxCuts))
                {
                    var r = Compile(string.Join("\n", lines.Take(k)), w, lines, k);
                    results.Add(r);
                    totalCuts++;
                    if (r.Verdict == "HANG") { hangs.Add((w.Lang, baseFile, r)); hangFile = baseFile; break; }
                    if (r.Verdict == "CRASH") crashes.Add((w.Lang, baseFile, r));
                }
                if (hangFile != null) break;
            }

            var counts = results.GroupBy(r => r.Verdict)
                                .OrderBy(g => g.Key, StringComparer.Ordinal)
                                .Select(g => $"{g.Key}={g.Count()}");
            var flag = results.Any(r => r.Verdict is "HANG" or "CRASH") ? "  ✘" : "";
            var names = string.Join(",", bases.Select(b => Path.GetFileName(b.Path)));
            Console.WriteLine($"{w.Lang,-6} {names,-34} 刀数 {results.Count,3}  {string.Join("  ", counts)}{(hangFile != null ? "  （挂住，本门中止）" : "")}{flag}");
            bad += results.Count(r => r.Verdict is "HANG" or "CRASH");

            if (hangFile != null) break;   // 挂住的那条线程停不下来 —— 整轮到此为止（见 Compile 的注释）
        }

        Console.WriteLine();
        if (hangs.Count > 0)
        {
            Console.WriteLine("── HANG：**超时未返回** —— 用户那边就是「点了编译，界面永远卡在编译中」 ──");
            foreach (var (lang, file, r) in hangs)
            {
                Console.WriteLine($"{lang}  {file}  截到第 {r.LinesKept} 行");
                Console.WriteLine($"    末尾源码：{r.Tail}");
            }
            Console.WriteLine();
        }
        if (crashes.Count > 0)
        {
            Console.WriteLine("── CRASH：返回了，但抛的是**未捕获异常**（应当是诊断，不是内部错误）──");
            foreach (var (lang, file, r) in crashes)
            {
                Console.WriteLine($"{lang}  {file}  截到第 {r.LinesKept} 行");
                Console.WriteLine($"    {r.Detail}");
                Console.WriteLine($"    末尾源码：{r.Tail}");
            }
            Console.WriteLine();
        }

        Console.WriteLine($"扫过 {totalCuts} 刀（{wiring.Count} 门）—— HANG {hangs.Count}、CRASH {crashes.Count}、探针自身问题 {noBase} 门。");
        if (bad == 0 && hangs.Count == 0)
            Console.WriteLine(noBase > 0
                ? "没有挂死；但上面那几门没量到（探针问题），结论只覆盖量到的部分。"
                : "全部在时限内返回。");
        return hangs.Count > 0 || crashes.Count > 0 ? 1 : 0;
    }

    // ── 截断点 ───────────────────────────────────────────────────────────────
    //
    // 不是"每 N 行切一刀"：**会让编译器挂住的是"块开了没关"**，而块开在
    // `{` / `(` / `[` / `:` 这些记号之后。所以优先切在**含这些记号的行之后**，
    // 再补几个均匀点兜住"截在表达式中间"的形态。
    private static readonly char[] Openers = { '{', '(', '[', ':', '=', ',' };

    private static List<int> CutPoints(string[] lines, int max)
    {
        // **够短就切遍每一个行边界** —— 单次编译在毫秒级（实测 117 刀共 1.1 秒），
        // 抽样省下的那点时间换不来覆盖率。行边界是"截断"最自然的定义：
        // 每一处都是"用户在编辑器里改到这一行就先存一下"的真实形态。
        if (lines.Length <= max)
            return Enumerable.Range(1, lines.Length).ToList();

        // 行数超了才抽样：**块开头优先**（`{`/`(`/`[`/`:` 之后 = "开了没关"，
        // 正是会让编译器挂住的那一类），不够再均匀补，最后那一刀（整份）一定保住。
        var openers = Enumerable.Range(1, lines.Length)
                                .Where(k => lines[k - 1].IndexOfAny(Openers) >= 0)
                                .ToList();
        var picked = new SortedSet<int> { lines.Length };
        if (openers.Count <= max - 1)
        {
            foreach (var k in openers) picked.Add(k);
        }
        else
        {
            for (int i = 0; i < max - 1; i++)
                picked.Add(openers[(int)((long)i * (openers.Count - 1) / Math.Max(1, max - 1))]);
        }
        return picked.ToList();
    }

    // ── 编译一刀（带时限） ────────────────────────────────────────────────────
    //
    // ⚠ **挂住之后那条线程停不下来**（.NET 没有安全的中止线程），所以：
    //    · 线程设为**后台线程** ⇒ `Main` 一返回进程就结束，那条线程随之消失
    //      （不需要 `Environment.Exit`，也就不会把汇总行吞掉）；
    //    · 一挂住就**立刻停掉整轮**（`stopAll`）—— 留着它空转既烧 CPU，
    //      又可能占着编译器里的静态锁，让后续每一刀都假红成「挂住」，
    //      那就再也分不清"真挂"与"被带累"了。
    //    ⇒ 因此**一门一次进程**：驱动脚本逐门起一次，挂住的那门才不拖累下一门。
    private static CutResult Compile(string source, Sample w, string[]? allLines = null, int kept = 0)
    {
        string? err = null, crash = null;
        var realOut = Console.Out;
        var realErr = Console.Error;
        var sink = new StringWriter();

        var th = new Thread(() =>
        {
            Console.SetOut(sink);
            Console.SetError(sink);
            try
            {
                var prog = w.Compile(source);
                if (w.PostLinkLang != null)
                    CompilerHelper.LinkStandardLibrary((VmlProgram)prog!, w.PostLinkLang, null);
            }
            catch (CompilationException ex) { err = ex.Message ?? ""; }
            catch (UnresolvedSymbolException ex) { err = ex.Message ?? ""; }
            catch (Exception ex) { crash = $"{ex.GetType().Name}: {ex.Message}"; }
            finally
            {
                Console.SetOut(realOut);
                Console.SetError(realErr);
            }
        })
        { IsBackground = true };
        th.Start();

        if (!th.Join(_timeoutMs))
        {
            Console.SetOut(realOut);
            Console.SetError(realErr);
            var tail = allLines is null || kept == 0
                ? ""
                : string.Join(" / ", allLines.Skip(Math.Max(0, kept - 2)).Take(2).Select(l => l.Trim()));
            return new CutResult(kept, "HANG", $"超过 {_timeoutMs} ms 未返回", tail);
        }

        if (crash != null) return new CutResult(kept, "CRASH", crash, "");
        if (err != null) return new CutResult(kept, "ERR", err, "");
        return new CutResult(kept, "PASS", "", "");
    }

    // ── 找例程 ───────────────────────────────────────────────────────────────
    //
    // **按扩展名找目录，不按语言名** —— 语言键（`bas` / `cs` / `f90` / `kt` / `rb` …）
    // 与目录名（`basic` / `csharp` / `fortran` / `kotlin` / `ruby` …）并不一一对应，
    // 写一张手工映射表就是本仓头号坑（"必须手工同步的平行表"）。扩展名是两边都有的键。
    private static string? FindExamplesDir(string vmlHome, string ext)
    {
        var examples = Path.Combine(vmlHome, "Examples");
        if (!Directory.Exists(examples)) return null;
        foreach (var d in Directory.GetDirectories(examples).OrderBy(d => d, StringComparer.Ordinal))
        {
            // `_selftest` 是**跨语言共享**的最小语料（每个语言一个 `out.<ext>`），
            // 由调用方显式兜底，不在这里当"某语言自己的目录"命中。
            if (Path.GetFileName(d).StartsWith('_')) continue;
            if (Directory.GetFiles(d, "*." + ext, SearchOption.TopDirectoryOnly).Length > 0)
                return d;
        }
        return null;
    }

    /// <summary>
    /// 候选例程：**只看顶层**（`Examples/c/` 下的 `stb`/`stm32` 是上游整包，又大又编不过，
    /// 递归进去只会白跑）。
    ///
    /// ⚠ **不按行数挑，按"结构点"挑**（含块开头记号的行数）。第一版写的是
    /// 「行数 8~120 里最大的那个」，实测 22 门里有 **14 门判 NOBASE** ——
    /// 因为例程是**两极分布**的：要么是 1~10 行的演示（放不下结构），要么是
    /// 120~870 行的完整程序（超上限）。行数既不是"有没有结构"的判据、也不是
    /// "切得动切不动"的判据，选它等于选了个无关的量。
    ///
    /// 现在的排序：**结构点多的优先（够 20 点就封顶），同样多再挑行数少的** ——
    /// 要的是形态丰富而编译便宜，不是文件大。
    /// </summary>
    private static List<string> Candidates(string dir, string ext)
    {
        var list = new List<(string Path, int Lines, int Openers)>();
        // **含下一级子目录**：有的语言把例程放在 `<语言>/<例子名>/main.<ext>` 下
        //（ladder 的 `motor_control/main.ld` 就是），只扫顶层会一门都挑不出来。
        // 再往下不递归（`Examples/c/stb`、`stm32` 是上游整包，又大又编不过）。
        var dirs = new List<string> { dir };
        try { dirs.AddRange(Directory.GetDirectories(dir).OrderBy(d => d, StringComparer.Ordinal)); }
        catch { /* 权限之类：拿不到子目录就只扫顶层 */ }

        foreach (var d in dirs)
        {
            string[] files;
            try { files = Directory.GetFiles(d, "*." + ext, SearchOption.TopDirectoryOnly); }
            catch { continue; }
            foreach (var f in files)
            {
                string[] ls;
                try { ls = File.ReadAllLines(f); } catch { continue; }
                if (ls.Length < MinBaseLines) continue;
                int openers = ls.Count(l => l.IndexOfAny(Openers) >= 0);
                list.Add((f, ls.Length, openers));
            }
        }
        return list.OrderByDescending(x => Math.Min(x.Openers, EnoughOpeners))
                   .ThenBy(x => x.Lines)
                   .Select(x => x.Path)
                   .ToList();
    }

    private static string Rel(string path, string root)
    {
        try { return Path.GetRelativePath(root, path).Replace('\\', '/'); }
        catch { return path; }
    }

    private static string Cut(string s, int n) => s.Length <= n ? s : s[..n] + "…";

    internal static void Configure(int timeoutMs, int maxCuts)
    {
        if (timeoutMs > 0) _timeoutMs = timeoutMs;
        if (maxCuts > 0) _maxCuts = maxCuts;
    }

    // ── 自检：「这条判据能不能响」 ─────────────────────────────────────────────
    //
    // 本仓铁律：**不响的自测比没有更糟**。上面那套「超时即报 HANG」的逻辑若本身写错
    // （比如 `Join` 的返回值判反了、或后台线程没有真的被当成挂住），量出来的
    // 「HANG 0」就是假的 —— 而假的绿会让人放心地不再看它。
    //
    // 所以拿两个**行为已知**的假编译入口喂它，要求各判出对的档：
    //   · 死循环（`while(true){}`）  ⇒ 必须 `HANG`（挂住而不是"返回了"）
    //   · 抛未捕获异常              ⇒ 必须 `CRASH`
    //   · 抛 `CompilationException` ⇒ 必须 `ERR`
    //   · 正常返回                  ⇒ 必须 `PASS`
    // 自检用短时限（1 秒）——它验的是**判据**，不是前端的快慢。
    internal static int SelfTest()
    {
        Console.OutputEncoding = Encoding.UTF8;
        var save = _timeoutMs;
        _timeoutMs = 1000;
        int fail = 0;
        try
        {
            Console.WriteLine("HangProbe 自检 —— 用行为已知的假编译入口验「判据会不会响」：");

            var spinning = new Thread(() => { while (true) { } }) { IsBackground = true };
            Check("死循环必须判成 HANG", Fake(() => { spinning.Start(); spinning.Join(); }), "HANG", ref fail);
            Check("未捕获异常必须判成 CRASH", Fake(() => throw new InvalidOperationException("boom")), "CRASH", ref fail);
            Check("CompilationException 必须判成 ERR",
                  Fake(() => throw new CompilationException(ErrorCode.Parser_SyntaxError, "x")), "ERR", ref fail);
            Check("正常返回必须判成 PASS", Fake(() => { }), "PASS", ref fail);
        }
        finally { _timeoutMs = save; }
        Console.WriteLine(fail == 0 ? "自检全过。" : $"自检 {fail} 条不过 —— **上面那些结论不作数**。");
        return fail == 0 ? 0 : 1;
    }

    /// <summary>造一个只走"编译这一下"的假入口（`PostLinkLang` 为 null ⇒ 不补链接）。</summary>
    private static Sample Fake(Action body)
        => new("selftest", "x", "selftest", "selftest", 1, 0, ["x"],
               _ => { body(); return null; });

    private static void Check(string what, Sample fake, string expected, ref int fail)
    {
        var r = Compile("x", fake);
        bool ok = r.Verdict == expected;
        if (!ok) fail++;
        Console.WriteLine($"  {(ok ? "✓" : "✘")} {what} —— 实得 {r.Verdict}{(ok ? "" : $"（应 {expected}）")}");
    }
}
