using WayCoder.UI.Shared;
using WayCoder.UI.Tui.Edit;

namespace WayCoder;

public static partial class SelfTest
{
    /// <summary>
    /// 编译器报错文本 → 结构化诊断（<see cref="VmlDiagnostics.Parse"/>）。
    ///
    /// **为什么这一批必须钉住**：它是「一次多报」在**编辑器**里的最后一环 ——
    /// 链接器那边已经会把所有未解析的名字一次列全（`LibraryLinker.ReportUnresolved`），
    /// 但如果这里把它们并回一条，用户看到的还是一个气泡，CLI 上 N 行、手机上 1 条，
    /// 两端的观感就对不上了。
    ///
    /// 判据形状（与 `scripts/vml-diag-probe` 同一条思路）：**先钉「几条」，再钉「哪条指到哪一行」**。
    /// 只断言"第一条对"是抓不到并条的。
    ///
    /// ⚠ 这个类此前长在 `WayCoder.Maui/Services/` 下 —— 纯逻辑却放在 MAUI 工程里，
    ///   桌面自测**一个用例都碰不到**。已挪到 `UI/Shared/`（本仓对跨端纯逻辑的既定要求），
    ///   于是有了这一批。
    /// </summary>
    private static void TestChunk25(Action<string> Section, Action<string, bool> Check, Action<string> Fail)
    {
        Section("VML 报错解析：多错必须多气泡");

        // ── ① 无位置的多条错误 —— 这是回归判据本身 ────────────────────────────
        // 链接器在**取不到源码行号**时就只写 `error: …`（`firstLine` 查不到时 where 是空串），
        // 而今天的 C / Go / Python / Kotlin / C# / Java / Scheme / Swift / ObjC / Pascal 都取不到。
        // 改之前这里是「命中即停」，三条规则全不命中 ⇒ 退化成 `FirstLine(text)` **一条**。
        var three = VmlDiagnostics.Parse(
            "error: 未定义的函数 'aaa'（引用 1 次）\n" +
            "error: 未定义的函数 'bbb'（引用 2 次）\n" +
            "error: 未定义的函数 'ccc'（引用 3 次）\n" +
            "提示: 检查函数名拼写；库函数要在源码里 #include 对应头文件。\n");
        Check($"三条无位置错误 → 三条诊断（实得 {three.Count}）", three.Count == 3);
        Check("第一条点名 aaa", three.Count > 0 && three[0].Message.Contains("aaa"));
        Check("第二条点名 bbb", three.Count > 1 && three[1].Message.Contains("bbb"));
        Check("第三条点名 ccc", three.Count > 2 && three[2].Message.Contains("ccc"));
        Check("无位置 ⇒ Line=0（气泡不画箭头）", three.TrueForAll(d => d.Line == 0));
        // `提示:` 行**不进气泡**：它是给模型/人的提示语，不是一处错误。
        // 进了的话每条真实错误后面都会跟一个噪声泡。
        Check("「提示:」行不进气泡", three.TrueForAll(d => !d.Message.Contains("检查函数名拼写")));

        // ── ② 混排：有行号的 + 没行号的，**两条都要在** ─────────────────────────
        // 这正是链接器实际产出的形状（`ReportUnresolved` 逐条决定要不要写位置）。
        // 改之前 TryGcc 只收"同一种形状"的匹配 ⇒ 只剩带位置的那条。
        var mixed = VmlDiagnostics.Parse(
            "<input>:12: error: 未定义的函数 'aaa'（引用 1 次）\n" +
            "error: 未定义的函数 'bbb'（引用 1 次）\n");
        Check($"混排 → 两条（实得 {mixed.Count}）", mixed.Count == 2);
        Check("带位置的那条锚在 12 行", mixed.Count > 0 && mixed[0].Line == 12);
        Check("另一条仍保留（Line=0）", mixed.Count > 1 && mixed[1].Line == 0 && mixed[1].Message.Contains("bbb"));

        // ── ③ 带列 GCC：老规矩不能破 ─────────────────────────────────────────
        // `GccNoColRx` 其实也能匹配带列的行（把 `main.c:12` 吃进文件名那段），
        // 所以两条 TryGcc 必须互斥 —— 一旦叠加，同一条错误会变成两个气泡。
        var gcc = VmlDiagnostics.Parse("main.c:12:5: error: 未预期的 token [Parser_UnexpectedToken]");
        Check($"GCC 带列 → 一条（实得 {gcc.Count}）", gcc.Count == 1);
        Check("行 12 列 5", gcc.Count == 1 && gcc[0].Line == 12 && gcc[0].Column == 5);
        Check("错误码摘进 Code 字段", gcc.Count == 1 && gcc[0].Code == "Parser_UnexpectedToken");
        Check("正文里不再重复错误码",
            gcc.Count == 1 && !gcc[0].Message.Contains("Parser_UnexpectedToken"));

        // ── ④ 其余三种格式各一条 ─────────────────────────────────────────────
        var cn = VmlDiagnostics.Parse("语法错误 main.c在第12行5列：未预期的 token");
        Check("中文格式 → 行 12 列 5", cn.Count >= 1 && cn[0].Line == 12 && cn[0].Column == 5);

        var at = VmlDiagnostics.Parse("Expected SEMICOLON but got IDENTIFIER ('tm_t') at line 112:");
        Check("英文尾缀 at line → 行 112", at.Count >= 1 && at[0].Line == 112);

        var plain = VmlDiagnostics.Parse("未知指令: FROBNICATE");
        Check("完全无位置 → 仍给一条（Line=0）", plain.Count == 1 && plain[0].Line == 0);
        Check("无位置那条带着原文", plain.Count == 1 && plain[0].Message.Contains("FROBNICATE"));

        // ── ⑤ 空输入不给垃圾 ────────────────────────────────────────────────
        Check("空串 → 空列表", VmlDiagnostics.Parse("").Count == 0);
        Check("null → 空列表", VmlDiagnostics.Parse(null).Count == 0);
        Check("纯空白 → 空列表", VmlDiagnostics.Parse("   \n\t\n").Count == 0);

        // ── ⑥ warning 的级别要认出来（用户要「未使用符号」出警告，别掉进 Error）──
        var warn = VmlDiagnostics.Parse("warning: 未使用的局部变量 'tmp'");
        Check("裸 warning → Warning 级", warn.Count == 1 && warn[0].Severity == Severity.Warning);

        var warnLocated = VmlDiagnostics.Parse("main.c:7:3: warning: 未使用的局部变量 'tmp'");
        Check("带位置 warning → Warning 级",
            warnLocated.Count == 1 && warnLocated[0].Severity == Severity.Warning);

        // ── ⑦ 库档那行**不该**被当成错误收进来 ────────────────────────────────
        // 链接器对库代码是 `[库内部] 库代码里有 N 个未解析标签…` + 若干 `  xxx (引用 N 次)` 缩进行。
        // 那些行不属于**用户代码**的错误，混进气泡只会让用户去改他改不了的东西。
        //
        // ⚠ 表头那行**曾经**写作 `警告: 库代码里有 N 个…`，而 `BareErrRx` 要求行首就是
        //   error/warning/错误/警告 —— 「警告:」在中文里同样是行首 ⇒ **表头被收进诊断**，
        //   在编辑器里表现为一块盖住代码的浮层（用户真机报的：
        //   「有很多程序能编译，也能运行，但是编辑器上报一个无位置的警告」）。
        //   现在链接器改用它自己的 `[库内部]` 前缀（见 `LibraryLinker` 那段注释）。
        //   下面两条一起钉住这件事 —— **改回 `警告:` 会让它们立刻红**。
        var lib = VmlDiagnostics.Parse(
            "<input>:3: error: 未定义的函数 'mine'（引用 1 次）\n" +
            "提示: 检查函数名拼写。\n");
        Check("库明细不进气泡", lib.Count == 1 && lib[0].Message.Contains("mine"));

        // 表头（成功出口那条路：`atLeastOne: false`）⇒ **一条都没有**。
        // 用 `atLeastOne: false` 是因为真实调用点就是这么传的（`MauiVml.BuildProgram` 的成功出口），
        // 只测默认值测不出这条 —— 默认会把「一条都没解析出来」补成一条红色 Error。
        var libHead = VmlDiagnostics.Parse(
            "[库内部] 库代码里有 21 个未解析标签 (该路径一旦被执行就会崩):\n" +
            "  print_long (引用 1 次)\n", null, atLeastOne: false);
        Check($"库内部表头不进诊断（实得 {libHead.Count}）", libHead.Count == 0);

        // ── ⑦b 成功出口**不许补造诊断** ──────────────────────────────────────
        // 那份文本是**编译期日志**：编过了却一条都没解析出来 = 日志里本来就没有诊断。
        // 按默认值补一条的话会凭空造出一个 `Severity.Error`，比原来的警告更吓人
        // （实测：链接器表头换了前缀之后，一个 7 行、编得过跑得动的 Pascal 程序
        //   在错误列表里冒出一条红错「库代码里有 21 个未解析标签…」）。
        var logOnly = VmlDiagnostics.Parse("链接完成，总指令数: 65847\n一切正常。\n", null, atLeastOne: false);
        Check($"成功出口：无诊断形状的日志 → 零条（实得 {logOnly.Count}）", logOnly.Count == 0);

        // 反方向：**失败出口照旧兜底** —— 一条都解析不出来也得给用户一条（上面 ① 已经钉住
        // `Line == 0` 与原文，这里再钉一次默认值就是 `true`，免得有人把默认值顺手改成 false
        // 从而让「编译失败但编辑器一个字都没有」那个老毛病复发）。
        var failFallback = VmlDiagnostics.Parse("未知指令: FROBNICATE");
        Check("失败出口：默认仍补一条", failFallback.Count == 1);

        // ── ⑧ **别的文件**来的诊断不给行锚（用户真机报的「报错位置不对」）────────
        //
        // 病根：`#include` 是把头文件内容**拼进同一个流**的，头文件里的错也走同一份报错文本，
        // 而它只有行号 —— 照旧贴到当前文件上，用户看到的就是"编译器指着我这句没问题的
        // 代码报错"（真机：第 112 行那句无害的 `/// <summary>` 被标红）。
        TestForeignFileDiagnostics(Section, Check);
    }

    /// <summary>
    /// 「这条诊断属于哪个文件」的判据 —— <see cref="VmlDiagnostics.Parse"/> 的
    /// <c>currentFile</c> 参数。
    ///
    /// **不传 = 老行为**（下面的第一条断言就是钉这个）：既有调用点与既有用例不受影响。
    /// </summary>
    private static void TestForeignFileDiagnostics(Action<string> Section, Action<string, bool> Check)
    {
        Section("VML 报错解析：别把**头文件**的错贴到当前文件上");

        // ① 不传 currentFile ⇒ 行为与从前逐字相同（行锚照给）
        var noArg = VmlDiagnostics.Parse("Lib/c/time.h:31:5: error: 未预期的 token");
        Check("不传当前文件 → 老行为（仍锚在 31 行）", noArg.Count == 1 && noArg[0].Line == 31);

        // ② 头文件里的错，当前文件是 main.cpp ⇒ **不给行锚**（Line=0）、文件名进消息正文
        var foreign = VmlDiagnostics.Parse(
            "D:/proj/Lib/c/time.h:31:5: error: 未预期的 token", "D:/proj/main.cpp");
        Check($"头文件的错 → 一条（实得 {foreign.Count}）", foreign.Count == 1);
        Check("头文件的错 → Line=0（不画箭头、不画波浪线）", foreign.Count == 1 && foreign[0].Line == 0);
        Check("头文件的错 → File 字段是头文件名",
            foreign.Count == 1 && foreign[0].File == "time.h");
        Check("头文件的错 → 文件名写进消息正文",
            foreign.Count == 1 && foreign[0].Message.Contains("time.h"));

        // ③ 当前文件自己的错 ⇒ 照旧做行锚，且**不**在正文里啰嗦文件名
        var own = VmlDiagnostics.Parse("D:/proj/main.cpp:64:5: error: 未声明的变量 'nosuchvar'", "D:/proj/main.cpp");
        Check("本文件的错 → 锚在 64 行", own.Count == 1 && own[0].Line == 64 && own[0].Column == 5);
        Check("本文件的错 → File 留空（正文不重复文件名）",
            own.Count == 1 && own[0].File == null && !own[0].Message.Contains("main.cpp"));

        // ④ **全路径 vs 相对路径必须是同一个文件** —— 前端各自的口径不一致，
        //    比全路径会把当前文件自己的错误误判成别人的（那比不判更糟：用户看不到行锚了）
        var rel = VmlDiagnostics.Parse("main.cpp:12:3: error: x", "D:/proj/main.cpp");
        Check("相对 vs 绝对同名 → 仍认作本文件", rel.Count == 1 && rel[0].Line == 12);

        // ⑤ `<input>` / `<unknown>` 是前端在"没有真实路径"时用的**占位符**，
        //    必须当当前文件 —— 否则用户自己文件里的错也失去行锚（链接期那批全是 `<input>`）
        var ph = VmlDiagnostics.Parse("<input>:12: error: 未定义的函数 'aaa'", "main.cpp");
        Check("占位符 `<input>` → 当当前文件（仍锚 12 行）", ph.Count == 1 && ph[0].Line == 12);
        var ph2 = VmlDiagnostics.Parse("<unknown>:9:1: error: x", "main.cpp");
        Check("占位符 `<unknown>` → 当当前文件（仍锚 9 行）", ph2.Count == 1 && ph2[0].Line == 9);

        // ⑥ **级别标签一个字都不能动**（`SeverityOf` 按 warning/note 两个字面量判）：
        //    头文件来的诊断虽然不锚行，级别必须原样保留 —— 糊成 Error 会让气泡/列表图标全错。
        var warnForeign = VmlDiagnostics.Parse("other.h:7:1: warning: 未使用的局部变量 'tmp'", "main.cpp");
        Check("头文件的 warning → 仍是 Warning 级",
            warnForeign.Count == 1 && warnForeign[0].Severity == Severity.Warning && warnForeign[0].Line == 0);
        var noteOwn = VmlDiagnostics.Parse("main.cpp:3:1: note: 附注", "main.cpp");
        Check("本文件的 note → Info 级", noteOwn.Count == 1 && noteOwn[0].Severity == Severity.Info);

        // ⑦ **真机上的路径形态**：Windows 盘符 + 反斜杠。`GccRx` 的 `(.+?):(\d+):` 是**惰性**匹配，
        //    而 `D:\…` 里那个冒号后面跟的不是数字 ⇒ 它必须继续往后吃、把整个路径留给"文件名"那一组。
        //    这也顺手钉住"两边路径形态不同也得认出同一个文件"（比的是文件名，不是全路径）。
        var winOwn = VmlDiagnostics.Parse(
            @"D:\proj\main.cpp:64:5: error: 未声明的变量 'nosuchvar'",
            @"D:\proj\main.cpp");
        Check("反斜杠路径 → 仍锚在 64 行", winOwn.Count == 1 && winOwn[0].Line == 64);
        var winForeign = VmlDiagnostics.Parse(
            @"D:\proj\Lib\c\time.h:31:5: error: 未预期的 token",
            @"D:\proj\main.cpp");
        Check("反斜杠路径的头文件错 → 不锚行 + 点名前缀",
            winForeign.Count == 1 && winForeign[0].Line == 0 && winForeign[0].Message.Contains("time.h"));

        // ⑧ **两条路并起来必须去重**（`Merge`）：编译失败时同一段报错既在异常消息里、
        //    又在编译期 stderr 里（链接器的未解析清单是**逐字相同的两份**），
        //    简单相加 = 同一个错误两个气泡。
        var failMsg = VmlDiagnostics.Parse("⚠️ 编译失败：error: 未定义的函数 'nosuchfn'（引用 1 次）", "main.cpp");
        var stderrDiag = VmlDiagnostics.Parse("error: 未定义的函数 'nosuchfn'（引用 1 次）", "main.cpp");
        var merged = VmlDiagnostics.Merge(failMsg, stderrDiag);
        Check($"同一错误两边都有 → 去重成一条（实得 {merged.Count}）", merged.Count == 1);
        Check("去重后仍是无位置那条", merged.Count == 1 && merged[0].Line == 0 && merged[0].Message.Contains("nosuchfn"));

        // 而 stderr **独有**的（不抛异常的那类诊断，如「找不到头文件」警告）**必须留下来** ——
        // 这正是不能"只取异常消息"的理由：那行警告解释了后面「未声明」是为什么。
        var withWarn = VmlDiagnostics.Merge(
            failMsg,
            VmlDiagnostics.Parse(@"D:\proj\main.cpp:6: warning: 找不到头文件 ""Windows.h"" [Preprocessor_IncludeNotFound]", "main.cpp"));
        Check($"stderr 独有的警告要留下（实得 {withWarn.Count}）", withWarn.Count == 2);
        Check("那条警告锚在 #include 那一行（6 行）",
            withWarn.Count == 2 && withWarn[1].Line == 6 && withWarn[1].Severity == Severity.Warning);
        Check("警告的错误码摘进 Code", withWarn.Count == 2 && withWarn[1].Code == "Preprocessor_IncludeNotFound");
    }
}
