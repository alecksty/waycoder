using WayCoder.UI.Tui.Edit;

namespace WayCoder;

public static partial class SelfTest
{
    /// <summary>
    /// 移动端编辑器这一批改动的**纯逻辑**部分（v0.96.215）：全角→半角映射、字符串/注释保护区间、
    /// 诊断的注入与关闭。
    ///
    /// 为什么这些能自测、而删除/气泡不能：这三样都是**纯函数**（进什么出什么），
    /// 而删除与气泡落在平台输入连接与原生视图上，只能在真机上验。
    /// 「能在桌面自测的先钉住」正是为了让真机那一轮只需要验真正平台相关的部分。
    /// </summary>
    private static void TestChunk24(Action<string> Section, Action<string, bool> Check, Action<string> Fail)
    {
        Section("全角→半角：唯一映射表");

        // ── ① ASCII 的全角形式（U+FF01..FF5E 整段减 0xFEE0）──
        Check("全角 ， → ,", Half("，") == ",");
        Check("全角 。 → .", Half("。") == ".");
        Check("全角 ； → ;", Half("；") == ";");
        Check("全角 ： → :", Half("：") == ":");
        Check("全角 （） → ()", Half("（）") == "()");
        Check("全角 ！？ → !?", Half("！？") == "!?");
        Check("全角 ［］｛｝ → []{}", Half("［］｛｝") == "[]{}");
        // 用户确认过「全角数字与字母一起转」
        Check("全角 ０-９ Ａ-ｚ 一起转", Half("０１２ＡＢａｂ") == "012ABab");
        Check("全角 ～ (FF5E) → ~", Half("～") == "~");

        // ── ② U+3000 段：不在上面那一整段里，是最容易漏的一类 ──
        Check("顿号 、 → ,（用户确认要转）", Half("、") == ",");
        Check("中文引号 “” → \"", Half("“”") == "\"\"");
        Check("中文单引号 ‘’ → '", Half("‘’") == "''");
        Check("日文波折号 〜 (U+301C) → ~", Half("〜") == "~");

        // ── ③ 刻意排除的：没有 ASCII 对应物，猜一个等于静默产出错代码 ──
        Check("全角空格 U+3000 **不转**（转掉是改坏内容）", Half("　") == "　");
        Check("「」不转（猜成引号会凭空造出字符串定界符）", Half("「」") == "「」");
        Check("《》不转", Half("《》") == "《》");
        Check("【】不转", Half("【】") == "【】");
        Check("破折号 — 不转", Half("—") == "—");
        Check("省略号 … 不转", Half("…") == "…");
        Check("￥ (U+FFE5) 不转（在 FF5E 之外）", Half("￥") == "￥");

        // ── ④ 长度不变（这条性质撑着「只写模型、不回写输入框」的整套做法）──
        Check("归一化严格 1 字符换 1 字符（长度不变）",
            Half("，。；：（）「」　") .Length == "，。；：（）「」　".Length);

        // ── ⑤ 混合内容：代码段转、保护段不转 ──
        Section("全角→半角：字符串与注释保护");

        var line = "printf(\"你好，世界\"); // 注释，保留";
        // 走公开的 ForFile（与编辑器同一条路）—— 顺带钉住「.c 落到 C/C++ 语法」这件事
        var spans = Syntax.ForFile("x.c").ProtectedSpans(line);
        var keep = new bool[line.Length];
        foreach (var (start, len) in spans)
            for (int i = start; i < start + len && i < keep.Length; i++) keep[i] = true;
        var norm = WayCoder.Infra.FullWidthText.Normalize(line, keep);

        Check("保护区间非空（认出了字符串与注释）", spans.Count > 0);
        Check("字符串里的「，」**保持全角**", norm.Contains("你好，世界", StringComparison.Ordinal));
        Check("注释里的「，」**保持全角**", norm.Contains("注释，保留", StringComparison.Ordinal));
        Check("反引号外的分号仍是半角（没被误伤）", norm.Contains("\");", StringComparison.Ordinal));

        // 代码部分的全角标点该转
        var codeLine = "x，y";
        Check("代码部分的全角逗号被转成半角", WayCoder.Infra.FullWidthText.Normalize(codeLine, default) == "x,y");

        // ── ⑥ 短路判据 ──
        Check("MayContainCandidate：纯 ASCII 行判 false（省掉一次 tokenize）",
            !WayCoder.Infra.FullWidthText.MayContainCandidate("int x = 1; // hello"));
        Check("MayContainCandidate：含全角标点判 true",
            WayCoder.Infra.FullWidthText.MayContainCandidate("x，y"));

        // ── ⑦ 空行 / 空串不能崩（Tokenize("") 返回的是一个长度 1 的空格 token）──
        var emptySpans = Syntax.ForFile("x.c").ProtectedSpans("");
        Check("空行 ProtectedSpans 返回空表（越界区间会让 Android 侧 setSpan 崩）", emptySpans.Count == 0);
        Check("空串归一化原样返回", WayCoder.Infra.FullWidthText.Normalize("", default) == "");

        // ── ⑧ 诊断：注入 / 关闭 / 三档计数 ──
        Section("诊断：注入与关闭（气泡与波浪线的唯一数据源）");

        const string fakePath = "selftest/fake_diag.c";
        DiagnosticManager.Clear(fakePath);

        var injected = new List<Diagnostic>
        {
            new(3, 5, Severity.Error, "语法错误", "Parser_Err"),
            new(3, 9, Severity.Warning, "未使用变量", null),
            new(7, 1, Severity.Info, "提示", null),
        };
        DiagnosticManager.Inject(fakePath, injected);

        Check("注入后 GetAll 拿到 3 条", DiagnosticManager.GetAll(fakePath).Count == 3);
        Check("GetForLine 只给该行的", DiagnosticManager.GetForLine(fakePath, 3).Count == 2);
        Check("Counts 三档分别计数", DiagnosticManager.Counts(fakePath) == (1, 1, 1));
        Check("GetSummary 只给错误/警告两档（既有口径未变）",
            DiagnosticManager.GetSummary(fakePath) == (1, 1));

        // 关闭一条：波浪线读的是同一份，所以两边会同时消失
        DiagnosticManager.Dismiss(fakePath, injected[0]);
        Check("Dismiss 之后只剩 2 条", DiagnosticManager.GetAll(fakePath).Count == 2);
        Check("Dismiss 掉了错误那一档", DiagnosticManager.Counts(fakePath) == (0, 1, 1));

        // 关一条不存在的：不该有任何变化（按 record 的逐字段相等性判）
        DiagnosticManager.Dismiss(fakePath, injected[0]);
        Check("Dismiss 一条不存在的诊断 → 数量不变", DiagnosticManager.GetAll(fakePath).Count == 2);

        DiagnosticManager.Inject(fakePath, []);
        Check("注入空表 = 清空（用户一动手就清掉上一轮诊断）", DiagnosticManager.GetAll(fakePath).Count == 0);
        DiagnosticManager.Clear(fakePath);

        static string Half(string s) => WayCoder.Infra.FullWidthText.Normalize(s, default);
    }
}
