using System;
using System.Collections.Generic;

namespace WayCoder.UI.Shared;

/// <summary>
/// **免费版 / 全能版的唯一分界** —— 这一份就是判据，别在别处再写一遍。
///
/// <para>
/// <b>口径（2026-09-28 定）：</b>
/// <list type="bullet">
///   <item><description><b>免费</b>：编辑器、<b>C 语言</b>编译运行、<b>VML 汇编</b>（<c>.vml</c>）编译运行；
///     <b>没有优化器</b>（设置里不出现那一项，按不优化跑）。</description></item>
///   <item><description><b>全能版</b>（一次性买断 <c>com.tanso.dolaima.full</c>，美国区 $9.99）：
///     <b>22 门语言</b>的编译器 + <b>优化器</b>。</description></item>
/// </list>
/// 完整口径与逐屏上架流程见 <c>docs/上架资料包.md</c> 与 <c>docs/上架AppStore.md</c>。
/// </para>
///
/// <para>
/// <b>VML 汇编不在这里判：</b><c>.vml</c> / <c>.vmb</c> 走的是
/// <c>MauiVml.RunAssembly</c> 那条路，根本不经过前端编译器，所以「免费能用 VML 汇编」
/// 不需要本文件写任何东西 —— 别为了"完整性"在此加一条汇编的判据，那是多余的。
/// </para>
///
/// <para>
/// ⚠ <b>为什么优化器的门不在这里</b>：本文件位于 <c>WayCoder/UI/Shared/</c>，会被<b>两个工程</b>一起编译，
/// 而主工程 <c>WayCoder.csproj</c> <b>没有引用 VML 项目</b>（原因见
/// <c>VMLAssembler/OptimizationPolicy.cs</c> 的类注释）⇒ 这里<b>不能出现 <c>VMLAssembler</c> 的任何类型</b>。
/// 所以优化器的门放在 Maui 侧、直接用 <c>OptimizationPolicy.Off</c>，
/// 而**不在这里复制一个「0」常量** —— 复制就又成了一张会漂移的平行表，本仓为此付过多次代价。
/// </para>
///
/// <para>
/// <b>改这里要同步哪里</b>：<c>MauiVml</c> 的语言门、设置页/文件页的加锁显示、App Store Connect 的
/// 元数据描述、以及 <c>docs/上架资料包.md</c> 第一节。少改一处，商店页就会和 App 实际能力对不上。
/// </para>
/// </summary>
public static class FreeTierPolicy
{
    /// <summary>
    /// <b>解锁全能版的非消耗型内购产品 ID</b>（买断、可跨设备恢复）。
    ///
    /// <para>
    /// ⚠ <b>这个字面量在三处必须逐字相同，改错一处就是「买完不解锁」或「购买入口查不到商品」</b>：
    /// ① App Store Connect 的产品 ID（**建完不能改**，要改只能新建一个产品）；
    /// ② Google Play Console 的应用内商品 ID；
    /// ③ 这里。桌面自测有一条断言钉住它 —— 它放在本文件（而不是 Maui 的
    /// <c>EntitlementStore</c>）正是为了能被那条断言看见：**口径的一部分就该和口径放一起**。
    /// </para>
    /// </summary>
    public const string FullEditionProductId = "com.tanso.dolaima.full";

    /// <summary>
    /// 免费版唯一允许的前端编译器名 —— 上游各编译器自报的 <c>Name</c>（此处是小写形式，比对照样小写）。
    /// C 编译器在上游自报为 <c>"C"</c>，归一化后即 <c>"c"</c>（见 <see cref="VmlFrontendCompilerList"/>）。
    ///
    /// <para>
    /// ⚠ <b>C++ 不受影响</b>：上游 <c>CppCompiler</c> 自报的是 <c>"cpp"</c>，不是 <c>"c"</c> ——
    /// 这条已被自测钉住（"免费语言恰好一门、且是 C 编译器"），别再凭"c 是 cpp 的前缀"担心一次。
    /// </para>
    /// </summary>
    public const string FreeLanguageName = "c";

    /// <summary>免费版允许的语言集合。只有一个元素，但用集合表达 —— 将来要放开第二门语言时只改这里。</summary>
    public static readonly IReadOnlyCollection<string> FreeLanguages =
        new HashSet<string>(StringComparer.OrdinalIgnoreCase) { FreeLanguageName };

    /// <summary>这门语言免费版能不能用。<paramref name="compilerName"/> 传编译器自报的 Name（大小写不敏感）。</summary>
    public static bool IsLanguageFree(string? compilerName) =>
        !string.IsNullOrEmpty(compilerName) && FreeLanguages.Contains(compilerName);

    /// <summary>
    /// 这次调用该不该被语言门拦下。**这是语言门的唯一判据** ——
    /// Maui 侧拿到 <c>lang</c> 之后直接问它，不要在那里再写一次 <c>lang != "c"</c>。
    /// </summary>
    /// <param name="isFull">是否已解锁全能版（由 <c>EntitlementStore.IsFull</c> 提供）。</param>
    /// <param name="compilerName">前端编译器自报的 Name。</param>
    public static bool IsLanguageLocked(bool isFull, string? compilerName) =>
        !isFull && !IsLanguageFree(compilerName);

    /// <summary>
    /// <b>文件页的菜单要不要给这个文件打锁</b>（用户 2026-09-29 定的口径：
    /// 「标准版只有 <b>C 语言和 VML 语言</b>可以编译运行，全能版支持全部」）。
    ///
    /// <para>
    /// ⚠ <b>它与 <see cref="IsLanguageLocked"/> 的唯一差别，是「没有语言」该算哪一种</b>：
    /// <see cref="IsLanguageLocked"/> 拿不到编译器的名字时**拦住**（fail-closed）——
    /// 那条路上拿不到名字说明"编译器没认出来"，拦住是安全侧。
    /// 而文件页拿到的 <c>null</c> 有更常见的第二种含义：<b>这个文件压根不需要前端编译器</b>
    /// —— <c>.vml</c>（汇编）与 <c>.vmb</c>（字节码）走的是 <c>RunAssembly</c> 那条路，
    /// <b>免费版本来就该能用</b>。两者混为一谈的后果是「免费版的 VML 汇编被锁上」，
    /// 正好把这档最核心的能力之一砍掉（实测差一点就这么发出去了）。
    /// </para>
    ///
    /// <para>
    /// 判据收在这里而不是在文件页写个 <c>&amp;&amp;</c>，是为了**能被自测钉住** ——
    /// 这条既是"免费版能给什么"的口径的一部分，就该和口径放一起、一起被测。
    /// </para>
    /// </summary>
    /// <param name="isFull">是否已解锁全能版。</param>
    /// <param name="compilerName">这个文件对应的前端编译器名；<b>不需要编译器时传 null</b>。</param>
    public static bool IsFileLocked(bool isFull, string? compilerName) =>
        compilerName is not null && IsLanguageLocked(isFull, compilerName);
}
