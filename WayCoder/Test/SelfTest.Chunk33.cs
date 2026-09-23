using WayCoder.UI.Shared;

namespace WayCoder;

public static partial class SelfTest
{
    /// <summary>
    /// 沙箱相对路径的**目录导航**（<see cref="SandboxPath"/>）—— 文件页返回键与「↩ 上级」
    /// 按钮共用的那一份。
    ///
    /// ## 为什么这一批必须钉住
    ///
    /// 返回键的行为**完全由这两个函数的返回值决定**：
    /// <c>ParentOf</c> 给出上一层 ⇒ 退一层并刷新；给出 <c>null</c> ⇒ 弹「要不要退出应用」。
    /// 也就是说 <c>ParentOf("")</c> 一旦返回空串而不是 <c>null</c>，
    /// 文件页就**永远退不到「该问退出」那一档**（每按一次返回都刷一遍同一个根目录，
    /// 用户看到的是「按了没反应」）—— 而 Android 上的返回键行为只有真机/模拟器能验，
    /// 所以边界必须在这一层钉死。
    ///
    /// ## 两条容易写错的边界
    ///
    /// * **「没有上一级」与「上一级是根」必须分得开**：前者是 <c>null</c>、后者是空串；
    /// * **形态不唯一**：<c>a/b/</c>、<c>a//b</c>、<c>\a\b</c> 都是同一个目录 ——
    ///   手机端的路径会经过多个来源（<c>ToRelative</c> 的产物、用户粘贴、跨端同步的配置），
    ///   按字符串直接切最后一个分隔符时，尾部那个多余的分隔符会让「上一级」整整错一层。
    ///
    /// ⚠ 这一组测的是纯文本形状，**不碰文件系统**（也不知道沙箱根在哪）——
    ///   那是 <c>SandboxFsService.ResolveInSandbox</c> 的职责，别在这里重测。
    /// </summary>
    private static void TestChunk33(Action<string> Section, Action<string, bool> Check, Action<string> Fail)
    {
        Section("沙箱路径导航：归一化 / 根判据 / 上一级 / 显示");

        // ── ① 根判据 ──
        Check("空串 = 根", SandboxPath.IsRoot(""));
        Check("null = 根（不抛）", SandboxPath.IsRoot(null));
        Check("`/` = 根（界面上「已经在根」的显示形态传回来也得认）", SandboxPath.IsRoot("/"));
        Check("只有分隔符的 `//` 也算根", SandboxPath.IsRoot("//"));
        Check("`a` 不是根", !SandboxPath.IsRoot("a"));
        Check("`a/b` 不是根", !SandboxPath.IsRoot("a/b"));

        // ── ② 归一化 ──
        Check("去掉尾部多余的分隔符：`a/b/` → `a/b`", SandboxPath.Normalize("a/b/") == "a/b");
        Check("丢掉空段：`a//b` → `a/b`", SandboxPath.Normalize("a//b") == "a/b");
        Check("认 Windows 形态的分隔符（路径可能来自别的端）",
            SandboxPath.Normalize(@"a\b") == "a/b");
        Check("去掉首部的分隔符（沙箱内只有相对路径）：`/a/b` → `a/b`", SandboxPath.Normalize("/a/b") == "a/b");
        Check("已经是规范形态的原样返回", SandboxPath.Normalize("a/b") == "a/b");
        Check("根归一化后仍是根（空串）", SandboxPath.Normalize("/") == "");

        // ── ③ 上一级：**返回键的行为就由它决定** ──
        Check("`a/b` → `a`", SandboxPath.ParentOf("a/b") == "a");
        Check("`a/b/c` → `a/b`", SandboxPath.ParentOf("a/b/c") == "a/b");
        Check("`a` → 根（空串，**不是 null**）", SandboxPath.ParentOf("a") == "");
        Check("**已在根 ⇒ null**（「没有上一级」与「上一级是根」必须分得开）",
            SandboxPath.ParentOf("") is null);
        Check("`/`（根的显示形态）同样 ⇒ null，退不到更上面去", SandboxPath.ParentOf("/") is null);
        Check("尾部多余的分隔符不影响：`a/b/` → `a`", SandboxPath.ParentOf("a/b/") == "a");
        Check("重复分隔符不影响：`a//b` → `a`", SandboxPath.ParentOf("a//b") == "a");

        // 一路退到根就再也退不动（不会越界、不会在根上打转）
        var dir = "examples/c/old";
        var steps = 0;
        while (SandboxPath.ParentOf(dir) is { } up)
        {
            dir = up;
            if (++steps > 16) break;
        }
        Check($"`examples/c/old` 一路退到根后停下（应 3 步 / 空串，实得 {steps} 步）",
            steps == 3 && dir == "");

        // ── ④ 显示形态 ──
        Check("根显示成 `/`", SandboxPath.Display("") == "/");
        Check("`a/b` 显示成 `/a/b`", SandboxPath.Display("a/b") == "/a/b");
        Check("归一化过的才拼（`a/b/` 不该显示成 `/a/b/`）", SandboxPath.Display("a/b/") == "/a/b");
        Check("`/a` 不该显示成 `//a`", SandboxPath.Display("/a") == "/a");
    }
}
