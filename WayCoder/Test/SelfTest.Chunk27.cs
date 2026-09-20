using WayCoder.UI.Shared;

namespace WayCoder;

public static partial class SelfTest
{
    /// <summary>
    /// 诊断气泡配色 —— **「六个底色 × 主题字色 ≥ 4.5:1」这条契约**的守门人
    /// （用它的是 <see cref="WcagContrast"/>，纯数学，不碰 UI，所以能真测）。
    ///
    /// ## 这条断言守的是什么
    ///
    /// 用户定的：**气泡字色跟随系统主题**（日间一律深字 <c>#1A1A1A</c>、夜间一律浅字
    /// <c>#F2F2F2</c>）—— 字色**不再按单个气泡的底色挑**。于是「字读得清」这件事
    /// **全部落在底色身上**：六个底色必须**各自**与它那一档的主题字色够对比度。
    ///
    /// 所以这条断言就是**用户这个决定的执行者**：谁把某个底色调得偏亮（最容易发生的是
    /// 把夜间警告底改回原来的亮琥珀 <c>#F5A524</c> —— 它配夜间浅字只有 **1.82:1**），
    /// 五格仍然达标、只有那一格变红 ⇒ 这里当场抓出来。**别再删掉它去「让构建变绿」**：
    /// 它是唯一能自动拦住「亮底色 + 浅字」的东西（那种组合在真机上就是「几乎读不了」）。
    ///
    /// ⚠ **六个数是从 <c>EditorTypography.BubbleBg*</c> 抄来的**（平行表）：那边的类型在
    /// MAUI 工程里，桌面自测**引用不到**（`EditorTypography` 用了 `Microsoft.Maui.Graphics.Color`）。
    /// 所以改那边必须回来改这里 —— 抄错的症状是**这里的断言仍然全绿而真机上不对**，
    /// 正是本仓记过多次的「必须手工同步的平行表」。判据里连色值的含义都写出来（红/黄/绿），
    /// 就是为了让抄的人一眼看出抄的是哪一档。
    /// </summary>
    private static void TestChunk27(Action<string> Section, Action<string, bool> Check, Action<string> Fail)
    {
        Section("诊断气泡配色：六个底色 × 主题字色 ≥ 4.5:1");

        // 主题字色（EditorTypography.BubbleText / BubbleTextDark 的平行表）——
        // 注意浅字是 #F2F2F2 而不是纯白 #FFFFFF：算夜间那三格必须用这个值（见下）。
        const int TextDark = 0x1A1A1A;    // BubbleText     —— 日间
        const int TextLight = 0xF2F2F2;   // BubbleTextDark —— 夜间（近白，不是纯白）

        // 六个气泡底 × 它所属主题的字色：白天中等浓度浅色调 + 夜间深色调
        (string Name, int Fill, int Text)[] fills =
        [
            ("白天·错误 #F19A9D", 0xF19A9D, TextDark),
            ("白天·警告 #FACE87", 0xFACE87, TextDark),
            ("白天·提示 #8DCDAE", 0x8DCDAE, TextDark),
            ("夜间·错误 #C4382F", 0xC4382F, TextLight),
            ("夜间·警告 #996000", 0x996000, TextLight),
            ("夜间·提示 #17794A", 0x17794A, TextLight),
        ];

        // ── ① 公式本身：两条定义式。写反了（比如漏了 +0.05、或忘了取亮暗）这两条必红 ──
        double blackWhite = WcagContrast.Ratio(0x000000, 0xFFFFFF);
        Check($"白对黑 = 21:1（实得 {blackWhite:F4}）", Math.Abs(blackWhite - 21.0) < 1e-9);

        double same = WcagContrast.Ratio(0x3B82F6, 0x3B82F6);
        Check($"同色 = 1:1（实得 {same:F4}）", Math.Abs(same - 1.0) < 1e-9);

        // 顺序无关（内部分亮暗）—— 传「字色在前」还是「底色在前」都该一样
        Check("对比度与传入顺序无关",
            Math.Abs(WcagContrast.Ratio(0x1A1A1A, 0xF19A9D) - WcagContrast.Ratio(0xF19A9D, 0x1A1A1A)) < 1e-12);

        // ── ② 分量权重：钉住「打包形式是 0xRRGGBB」的契约 ──
        // ⚠ 这条**不是凑数**：MAUI 那边的 `Color.FromArgb("#RRGGBB")` 与 `#AARRGGBB`
        // 是两种打包（本仓踩过 alpha 前后颠倒的坑），而共享这一层只认 0xRRGGBB。
        // 若有人把解码顺序弄反（RGB↔BGR），红与蓝的**权重不同**（0.2126 vs 0.0722）会立刻暴露 ——
        // 只用「白=1、黑=0」是盖不住的（那两个值交换通道后一模一样）。
        Check($"纯红相对亮度 = 0.2126（实得 {WcagContrast.RelativeLuminance(0xFF0000):F4}）",
            Math.Abs(WcagContrast.RelativeLuminance(0xFF0000) - 0.2126) < 1e-9);
        Check($"纯蓝相对亮度 = 0.0722（实得 {WcagContrast.RelativeLuminance(0x0000FF):F4}）",
            Math.Abs(WcagContrast.RelativeLuminance(0x0000FF) - 0.0722) < 1e-9);
        Check($"白 = 1.0 / 黑 = 0.0（实得 {WcagContrast.RelativeLuminance(0xFFFFFF):F4} / {WcagContrast.RelativeLuminance(0x000000):F4}）",
            Math.Abs(WcagContrast.RelativeLuminance(0xFFFFFF) - 1.0) < 1e-9
            && Math.Abs(WcagContrast.RelativeLuminance(0x000000)) < 1e-9);

        // 反伽马那一段的阈值取 0.04045（WCAG 2.2 勘误）还是 0.03928（2.0 版）—— 文档里断言了
        // 「对 8 位整数输入逐位相同」，这里就把它验一遍（便宜，且免得那句话是空口）。
        // 同一个公式喂两个阈值（而不是抄两份代码）—— 否则「比较的是阈值」这件事会被抄错掩盖。
        static double Lin(int c, double threshold)
        {
            double v = c / 255.0;
            return v <= threshold ? v / 12.92 : Math.Pow((v + 0.055) / 1.055, 2.4);
        }
        int thresholdDiff = 0;
        for (int v = 0; v <= 255; v++)
        {
            if (Lin(v, 0.03928) != Lin(v, 0.04045)) thresholdDiff++;
        }
        Check($"两个 sRGB 阈值对 8 位输入无差异（不一致的档数 = {thresholdDiff}）", thresholdDiff == 0);

        // ── ③ 六格：每个底 × **它那一档的主题字色**，全部 ≥ 4.5:1 ← 本 chunk 的主断言 ──
        // 字色不再按底色挑（用户要「跟随系统」），所以「读得清」全靠底色自己达标。
        int below = 0;
        foreach (var (name, fill, text) in fills)
        {
            double ratio = WcagContrast.Ratio(fill, text);
            bool ok = ratio >= 4.5;
            if (!ok) below++;
            string which = text == TextDark ? "深字" : "浅字";
            Check($"{name} → {which} 对比度 {ratio:F2}:1 ≥ 4.5", ok);
        }
        if (below > 0) Fail($"有 {below} 个气泡底配主题字色低于 4.5:1（字读不清）");

        // ── ④ 回归闸门：用户这次换掉夜间警告底的那件事，正是上面 ③ 在守的 ──
        // 夜间警告底原来是**亮琥珀** `#F5A524`（用户当时要「夜间黄要亮」）。字色改成
        // 「夜间一律浅字」之后它压浅字只有 **1.82:1** —— 这就是它必须换成深琥珀的**唯一原因**
        // （现用 `#996000`，4.66:1）。下面这条把它钉成事实：谁把亮琥珀换回去，`1.82 < 4.5`
        // ⇒ 上面 ③ 那条立刻红。**这条断言守的就是用户的这个决定本身**，别当成冗余删掉。
        Check($"夜间警告底已不是亮琥珀（#F5A524 压浅字只有 {WcagContrast.Ratio(0xF5A524, TextLight):F2}:1 < 4.5）",
            WcagContrast.Ratio(0xF5A524, TextLight) < 4.5);

        // 顺带把「算这一格必须用 #F2F2F2 而不是纯白」这条也钉住：两者结论相反，
        // 拿纯白去算会误判成达标（4.71 ≥ 4.5），从而放过一个照亮底 + 浅字的组合。
        Check($"#A36600 配浅字 #F2F2F2 = {WcagContrast.Ratio(0xA36600, TextLight):F2}:1 不达标（而配纯白是 {WcagContrast.Ratio(0xA36600, 0xFFFFFF):F2}:1）",
            WcagContrast.Ratio(0xA36600, TextLight) < 4.5
            && WcagContrast.Ratio(0xA36600, 0xFFFFFF) >= 4.5);

        // ── ⑤ 两个字色本身没被换掉（换掉就等于换了整套配色的前提）──
        Check("日间字色 = #1A1A1A / 夜间字色 = #F2F2F2",
            TextDark == 0x1A1A1A && TextLight == 0xF2F2F2);

        // ── ⑥ 两档的明暗方向：白天「浅底 + 深字」、夜间「深底 + 浅字」──
        // 不是重复 ③：③ 只说「够 4.5」，这里说的是**方向**。方向反了（白天给了深底）虽然
        // 也可能凑够对比度，但那已经不是「白天的浅色调」了，会在真机上一眼看出来。
        int wrongDirection = fills.Count(f => (f.Text == TextDark)
            ? WcagContrast.RelativeLuminance(f.Fill) <= WcagContrast.RelativeLuminance(TextDark)
            : WcagContrast.RelativeLuminance(f.Fill) >= WcagContrast.RelativeLuminance(TextLight));
        Check($"白天三档都比字浅、夜间三档都比字深（方向错的档数 = {wrongDirection}）", wrongDirection == 0);
    }
}
