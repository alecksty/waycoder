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
    /// 免费版唯一允许的前端编译器名 —— 上游各编译器自报的 <c>Name</c>（此处是小写形式，比对照样小写）。
    /// C 编译器在上游自报为 <c>"C"</c>，归一化后即 <c>"c"</c>（见 <see cref="VmlFrontendCompilerList"/>）。
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
}
