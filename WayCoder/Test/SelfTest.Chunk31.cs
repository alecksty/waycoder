using WayCoder.UI.Shared;

namespace WayCoder;

public static partial class SelfTest
{
    /// <summary>
    /// 跨平台路径文本处理（<see cref="PathText"/>）—— **两种分隔符都认**。
    ///
    /// ## 为什么这一批必须钉住
    ///
    /// 起因是一条**只在 macOS/Linux 上红**的自测：`SelfTest` 里"真机上的路径形态
    /// （Windows 盘符 + 反斜杠）"那一组，在 Windows 上全绿、在 Unix 上必红一条
    /// （`那条警告锚在 #include 那一行（6 行）`）。根因是 `VmlDiagnostics.FileNameOf`
    /// 用了 **`Path.GetFileName`** —— 它按**当前平台**的分隔符切，而这里处理的是
    /// **编译器吐出来的路径**，形态由**产出它的那台机器**决定。
    ///
    /// Unix 上 `Path.GetFileName(@"D:\proj\main.cpp")` **原样返回**，
    /// 于是 `IsSameFile` 判成"这是别的文件的诊断" ⇒ **行锚丢掉**（气泡还在、不画箭头）。
    /// 从界面上看只是"位置没了"，根本联想不到分隔符。
    ///
    /// ## 判据的两层
    ///
    /// * **本组**：直接钉 `PathText` 自己的契约（两种分隔符、空段、绝对路径形态）；
    /// * **`TestChunk25`**：端到端钉一次（诊断的行锚真的落在第 6 行）。
    ///   两层都要 —— 只钉低层的话，"调用点又绕回 `Path.GetFileName`"这种改动抓不到；
    ///   只钉端到端的话，出了红也定位不到是哪条规则错。
    ///
    /// ⚠ 这一组**必须同时在 macOS/Linux 与 Windows 上跑**才有意义：
    /// 反斜杠那几条在 Windows 上**用 `Path.GetFileName` 也能过**（那边 `\` 本就是分隔符），
    /// 所以"在 Windows 上全绿"证明不了任何事。
    /// </summary>
    private static void TestChunk31(Action<string> Section, Action<string, bool> Check, Action<string> Fail)
    {
        Section("跨平台路径：正反斜杠都认");

        // ── ① 取文件名：两种分隔符 ──
        Check("Unix 形态 /a/b/main.c → main.c",
            PathText.FileNameOf("/a/b/main.c") == "main.c");
        Check(@"反斜杠 a\b\main.c → main.c（Unix 上最容易漏的一条）",
            PathText.FileNameOf(@"a\b\main.c") == "main.c");
        Check(@"Windows 盘符 D:\proj\main.cpp → main.cpp",
            PathText.FileNameOf(@"D:\proj\main.cpp") == "main.cpp");
        Check(@"混合形态 C:/proj\main.cpp → main.cpp",
            PathText.FileNameOf(@"C:/proj\main.cpp") == "main.cpp");
        Check("没有分隔符 → 原样（main.c）",
            PathText.FileNameOf("main.c") == "main.c");
        Check("以分隔符结尾 → 原样返回，**不是空串**（返空会让界面那一格变白）",
            PathText.FileNameOf("/a/b/") == "/a/b/");
        Check("空串 → 空串（不抛）", PathText.FileNameOf("") == "");

        // ── ② 分段：空段要丢掉 ──
        Check(@"D:\proj\a\main.c → 4 段",
            PathText.Segments(@"D:\proj\a\main.c").Length == 4);
        Check(@"连续分隔符 a//b 不产生空段（否则 [^1] 会拿到空串）",
            PathText.Segments("a//b").Length == 2);
        Check(@"C:\\x 同样不产生空段",
            PathText.Segments(@"C:\\x").Length == 2);
        Check("末段就是文件名",
            PathText.Segments(@"D:\proj\a\main.c")[^1] == "main.c");

        // ── ③ 绝对路径形态（判形态，不查文件系统）──
        Check("Unix 绝对 /x", PathText.IsAbsoluteShaped("/x"));
        Check(@"盘符 C:\x", PathText.IsAbsoluteShaped(@"C:\x"));
        Check(@"UNC \\srv\share", PathText.IsAbsoluteShaped(@"\\srv\share"));
        Check("相对 a/b 不算绝对", !PathText.IsAbsoluteShaped("a/b"));
        Check("空串不算绝对（不能取 [0] 崩）", !PathText.IsAbsoluteShaped(""));

        // ── ④ 归一化 ──
        Check(@"归一化把 \ 换成 /", PathText.Normalize(@"a\b\c") == "a/b/c");
        Check("归一化不碰已是 / 的", PathText.Normalize("a/b/c") == "a/b/c");

        // ── ⑤ **端到端的锚**（低层改对了、高层调用点又绕回去的那种改动，靠这条抓）──
        //    与 `TestChunk25` 的那条同源；这里再钉一次是因为**本组的语义更直白** ——
        //    看到红的那一行就知道是"文件名判定"而不是"诊断解析"坏了。
        var diag = VmlDiagnostics.Parse(
            @"D:\proj\main.cpp:6: warning: 找不到头文件 ""Windows.h"" [Preprocessor_IncludeNotFound]",
            "main.cpp");
        Check($"Windows 形态的路径 + 相对 currentFile ⇒ 仍认作同一文件、行锚 = 6"
              + (diag.Count == 1 ? $"（实得 Line={diag[0].Line}）" : $"（实得 {diag.Count} 条）"),
            diag.Count == 1 && diag[0].Line == 6);
    }
}
