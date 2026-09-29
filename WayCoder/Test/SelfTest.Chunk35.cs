using System;
using System.IO;
using System.Linq;
using WayCoder.UI.Shared;

namespace WayCoder;

/// <summary>
/// <b>免费版 / 全能版分界</b>（<see cref="FreeTierPolicy"/>）的自测，外加两条
/// **「门到底接上了没有」的源码护栏**。
///
/// <para>
/// 分界本身是纯逻辑，断言直白；真正值钱的是最后一节 ——
/// <b>v0.96.577 之前 <c>FreeTierPolicy</c> 是零生产调用点的死代码</b>：
/// 口径写进了文件、文档也写好了，而**两个门一个都没接**，于是所有人都在白用
/// 22 门语言 + 优化器。<b>「策略存在」与「策略生效」是两件事</b>，
/// 只有读调用点才分得开 —— 这一节就是干这个的。
/// </para>
/// </summary>
public static partial class SelfTest
{
    private static void TestChunk35(Action<string> Section, Action<string, bool> Check, Action<string> Fail)
    {
        Section("免费版 / 全能版分界（FreeTierPolicy）");

        // ── ① 内购产品 ID：三处必须逐字相同（商店后台 / Play Console / 这里）────
        // 改错的表现是"商店查不到商品"或"买完不解锁"，而这两种都**不会报错**。
        Check("内购产品 ID 逐字钉住 com.tanso.dolaima.full",
            FreeTierPolicy.FullEditionProductId == "com.tanso.dolaima.full");

        // ── ② 免费语言恰好一门，且是 C ────────────────────────────────────
        Check("免费语言恰好一门", FreeTierPolicy.FreeLanguages.Count == 1);
        Check("免费语言就是 c", FreeTierPolicy.FreeLanguages.Contains("c"));

        // 大小写：上游 C 编译器自报的是大写 `"C"`，而 `MauiVml` 那边会 `ToLowerInvariant()`
        // 之后再判 —— 两条路都得认，否则"免费版连 C 都编不了"。
        Check("IsLanguageFree: 认大写 \"C\"（上游自报形式）", FreeTierPolicy.IsLanguageFree("C"));
        Check("IsLanguageFree: 认小写 \"c\"（归一化后形式）", FreeTierPolicy.IsLanguageFree("c"));
        Check("IsLanguageFree: \"cpp\" 不认（C++ 不在免费档）", !FreeTierPolicy.IsLanguageFree("cpp"));
        Check("IsLanguageFree: null / 空串不认",
            !FreeTierPolicy.IsLanguageFree(null) && !FreeTierPolicy.IsLanguageFree(""));

        // ── ③ 门：四格 + fail-closed ──────────────────────────────────────
        Check("门: 免费版 + C ⇒ 放行", !FreeTierPolicy.IsLanguageLocked(isFull: false, "C"));
        Check("门: 免费版 + Python ⇒ 拦", FreeTierPolicy.IsLanguageLocked(isFull: false, "python"));
        Check("门: 全能版 + C ⇒ 放行", !FreeTierPolicy.IsLanguageLocked(isFull: true, "C"));
        Check("门: 全能版 + Python ⇒ 放行", !FreeTierPolicy.IsLanguageLocked(isFull: true, "python"));

        // ⚠ **fail-closed**：拿不到编译器名时应当**拦住**而不是放行 ——
        // 反过来写的话，"上游改了接口、我们拿不到名字"这条路上所有人都成了全能版。
        Check("门: 拿不到语言名 ⇒ 拦（fail-closed，不是放行）",
            FreeTierPolicy.IsLanguageLocked(isFull: false, null));

        // ── ③b 文件页的门：**「没有语言」在文件页是另一种含义** ──────────────
        //
        // 用户 2026-09-29 定的口径：「标准版只有 C 语言和 VML 语言可以编译运行」。
        // `.vml`（汇编）/ `.vmb`（字节码）走的是汇编器那条路、**根本不经过前端语言门**，
        // 所以在文件页它们的 `compilerName` 是 null —— 而 null 在这里必须解释成
        // **"这个门管不着"**，不是"被锁"。
        //
        // 这一条是**实测差点发出去的 bug**：第一版文件页直接用了 `IsLanguageLocked`，
        // 于是 `.vml` 的「VML 编译 / VML 运行」两项目录上被打了锁、点下去还弹"需要全能版" ——
        // 正好把免费档最核心的能力之一砍掉，而且**构建全绿**。
        Check("文件页: 不需要编译器（.vml / .vmb）⇒ 不上锁",
            !FreeTierPolicy.IsFileLocked(isFull: false, null));
        Check("文件页: 免费版 + C ⇒ 不上锁", !FreeTierPolicy.IsFileLocked(isFull: false, "c"));
        Check("文件页: 免费版 + Python ⇒ 上锁", FreeTierPolicy.IsFileLocked(isFull: false, "python"));
        Check("文件页: 全能版 + Python ⇒ 不上锁", !FreeTierPolicy.IsFileLocked(isFull: true, "python"));
        Check("文件页: 全能版 + 不需要编译器 ⇒ 不上锁", !FreeTierPolicy.IsFileLocked(isFull: true, null));
        // 与上面那条 fail-closed 并列，把"两种 null 含义不同"这件事钉在相邻两行上
        Check("两种 null 含义确实不同（语言门拦、文件页不拦）",
            FreeTierPolicy.IsLanguageLocked(isFull: false, null) && !FreeTierPolicy.IsFileLocked(isFull: false, null));

        // ── ④ 与真实清单对齐（防"免费语言名字写错 ⇒ 免费版一门都编不了"）────
        var all = VmlFrontendCompilerList.All;
        Check("前端清单非空", all.Count > 0);

        foreach (var lang in FreeTierPolicy.FreeLanguages)
            Check($"免费语言 {lang} 在前端清单里确实存在",
                all.Any(e => string.Equals(e.Name, lang, StringComparison.OrdinalIgnoreCase)));

        var freeEntries = all.Where(e => FreeTierPolicy.IsLanguageFree(e.Name)).ToList();
        Check("清单里被判为免费的恰好一门", freeEntries.Count == 1);
        // 这一条同时钉住"C++ 别被前缀吃掉"（CppCompiler 自报的是 `cpp`，不是 `c`）
        Check("那一门是 C 编译器，不是 C++",
            freeEntries.Count == 1 && freeEntries[0].PluginType == "CCompiler.CCompilerPlugin");
        Check("C++ 在付费侧", all.Any(e => e.Name == "cpp") && !FreeTierPolicy.IsLanguageFree("cpp"));

        // ── ⑤ 两个门真的接上了吗（读源码，不是读注释）──────────────────────
        //
        // 这道护栏针对的**不是**"将来有人改坏算法"，而是我自己刚踩过的那个坑：
        // 策略文件写好了、注释齐全、文档也对，**唯一的问题是没有任何地方调它**。
        // 纯逻辑断言对此完全无感（它只验函数本身），所以只能查调用点。
        Section("[两个门接上了没有：查调用点]");

        static string? FindRepoFile(string rel)
        {
            for (var d = new DirectoryInfo(Directory.GetCurrentDirectory()); d != null; d = d.Parent)
            {
                var p = Path.Combine(d.FullName, rel);
                if (File.Exists(p)) return p;
            }
            return null;
        }

        var vmlPath = FindRepoFile("WayCoder.Maui/Services/MauiVml.cs");
        var storePath = FindRepoFile("WayCoder.Maui/Services/MauiCompileStore.cs");
        var storeEntPath = FindRepoFile("WayCoder.Maui/Services/EntitlementStore.cs");

        if (vmlPath is null || storePath is null || storeEntPath is null)
        {
            // 扫不到就**报错**，不能算通过 —— 同 `examples-lang-audit.py` 传全路径时
            // "扫 0 个、exit 0"那条教训：**"我验过了"其实是"我什么都没验"**。
            Fail("找不到 Maui 侧的三个源文件（MauiVml / MauiCompileStore / EntitlementStore）——判据没跑，不算通过");
        }
        else
        {
            var vmlText = File.ReadAllText(vmlPath);
            var storeText = File.ReadAllText(storePath);

            Check("语言门已接：MauiVml 里调了 FreeTierPolicy.IsLanguageLocked",
                vmlText.Contains("FreeTierPolicy.IsLanguageLocked(", StringComparison.Ordinal));
            Check("语言门的判据取自 EntitlementStore.IsFull（不是别处又判一遍）",
                vmlText.Contains("EntitlementStore.IsFull", StringComparison.Ordinal));

            Check("优化器门已接：MauiCompileStore 用 OptimizationPolicy.Off 夹住",
                storeText.Contains("OptimizationPolicy.Off", StringComparison.Ordinal));
            Check("优化器门的判据取自 EntitlementStore.IsFull",
                storeText.Contains("EntitlementStore.IsFull", StringComparison.Ordinal));
        }

        // 解锁状态**只有一个写入口**：`Iap` 拿到已完成的交易之后调。
        // UI 直接写 = "点了购买按钮就把自己标成已解锁"，是这类实现最经典的漏洞。
        var iapPath = FindRepoFile("WayCoder.Maui/Services/Iap.cs");
        if (iapPath is null)
        {
            Fail("找不到 Iap.cs —— 判据没跑，不算通过");
        }
        else
        {
            var iapText = File.ReadAllText(iapPath);
            Check("Iap 是解锁状态的唯一写入口（它调 EntitlementStore.Set）",
                iapText.Contains("EntitlementStore.Set(", StringComparison.Ordinal));
        }
    }
}
