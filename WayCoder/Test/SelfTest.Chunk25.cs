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
        // 链接器对库代码是 `警告: 库代码里有 N 个未解析标签…` + 若干 `  xxx (引用 N 次)` 缩进行。
        // 那些行不属于**用户代码**的错误，混进气泡只会让用户去改他改不了的东西。
        // （`BareErrRx` 要求行首就是 error/warning/错误/警告 —— 「警告:」在中文里同样是行首，
        //   所以这里钉的是**缩进的库明细行**不被误收。）
        var lib = VmlDiagnostics.Parse(
            "<input>:3: error: 未定义的函数 'mine'（引用 1 次）\n" +
            "提示: 检查函数名拼写。\n");
        Check("库明细不进气泡", lib.Count == 1 && lib[0].Message.Contains("mine"));
    }
}
