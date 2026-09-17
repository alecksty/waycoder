using System.Text;
using System.Text.RegularExpressions;

namespace WayCoder.UI.Shared;

/// <summary>
/// 手机端**静态注册的 VML 前端编译器清单** —— 唯一真源，同时也是「上游漂移」的判据。
///
/// <para>
/// <b>这份清单抄自哪里：</b>vendored 上游
/// <c>third_party/vml/VMLTool/StaticLink/StaticLinkInitializer.cs</c> 的
/// <c>RegisterFrontendCompilers(PluginManager)</c>（<c>#if STATIC_LINK</c> 分支里那 22 行
/// <c>manager.RegisterFrontendCompiler(new XxxPlugin())</c>）。
/// </para>
///
/// <para>
/// <b>为什么抄，而不继续引 <c>VMLTool.csproj</c>：</b>
/// <c>VMLTool.csproj</c> 除了这 22 个前端编译器，还**编译依赖**两个跟手机端毫无关系的项目 ——
/// <c>VMLTranslators</c>（18 个后端翻译器，302.5 KB）与 <c>VMLToHex</c>（158 KB，产物
/// <c>vml2hex.dll</c>）。那两个是给**裸机/单片机**输出后端汇编与 hex/elf/bin 的；手机端只用得到
/// 「高级语言 → VML 汇编 → 虚拟机执行」这一条链，一个都用不上。引 <c>VMLTool.csproj</c> 就会
/// 把它们一起链进 APK（实测 APK 里确实躺着 <c>libaot-VMLTranslators.dll.so</c> 与
/// <c>libaot-vml2hex.dll.so</c>）。
/// 把清单搬进我们自己的代码、直接引 22 个编译器 csproj，那 ~460 KB 就不进包了。
/// </para>
///
/// <para>
/// <b>为什么不去改 vendored 源码（把 <c>VMLTool.csproj</c> 里的两个引用删掉）：</b>
/// <c>third_party/vml/**</c> 是上游副本，有一套补丁机制（<c>patches/*.patch</c> + <c>sync.sh</c> 重放
/// + <c>scripts/check-vml-patches.sh</c> 复核），改它要付长期维护代价。抄一份清单的代价是
/// **可能漂移**，而漂移可以靠下面那套判据变成响亮失败，比改 vendored 便宜。
/// </para>
///
/// <para>
/// ⚠ <b>上游改了要同步哪里（加一门语言时三处都要动，缺一处就编不过或静默少一种）：</b>
/// <list type="number">
///   <item><description>本文件的 <see cref="All"/> 加一行（<c>PluginType</c> = 插件类的完整名，<c>Name</c> = <c>Plugin.Name</c>）；</description></item>
///   <item><description><c>WayCoder.Maui/Services/VmlFrontendCompilers.cs</c> 的 <c>RegisterAll</c> 补上对应的 <c>new</c>；</description></item>
///   <item><description><c>WayCoder.Maui/WayCoder.Maui.csproj</c> 加上那个编译器工程的 <c>ProjectReference</c>。</description></item>
/// </list>
/// </para>
///
/// <para>
/// <b>漏了会怎样（两道护栏，都是响亮失败）：</b>
/// ① 桌面自测（<c>dotnet run -- --test</c>）里的 <c>[VML 前端编译器清单]</c> 段会**解析上游那个
/// <c>.cs</c> 文件**并逐项比对，不一致直接 ❌ —— 这条抓的是「上游加了语言、我们没跟」；
/// ② 手机端 <c>VmlFrontendCompilers.RegisterAll</c> 注册完会**断言注册到的集合 == 本清单**
/// （插件类型名 + Name + 数量），不一致抛 <see cref="InvalidOperationException"/> —— 这条抓的是
/// 「本清单与 <c>RegisterAll</c> 里的 <c>new</c> 对不上」。
/// 两条都不依赖「人记得改」，所以**静默少一种语言**在这套判据下不可能发生。
/// </para>
/// </summary>
public static class VmlFrontendCompilerList
{
    /// <summary>一个前端编译器：<paramref name="PluginType"/> 是插件类完整名（命名空间.类名），<paramref name="Name"/> 是它自报的 <c>Name</c>。</summary>
    public sealed record Entry(string PluginType, string Name);

    /// <summary>
    /// 上游那份清单所在的文件（仓库根相对路径）。自测靠它找到上游、逐项比对。
    /// </summary>
    public const string UpstreamRelativePath = "third_party/vml/VMLTool/StaticLink/StaticLinkInitializer.cs";

    /// <summary>
    /// 上游文件里「前端注册段」的方法签名片段 —— 解析时用它定位**定义**（不是 <c>RegisterAll</c> 里的调用点）。
    /// 带上 <c>void </c> 是为了避开那个调用点：调用点写作 <c>RegisterFrontendCompilers(manager);</c>，不含 <c>void</c>。
    /// </summary>
    public const string UpstreamMethodAnchor = "void RegisterFrontendCompilers";

    /// <summary>前端注册段的**结束**锚点（后端翻译器段）。</summary>
    public const string UpstreamMethodEndAnchor = "void RegisterBackendTranslators";

    /// <summary>
    /// 22 个前端编译器。顺序与上游 <c>RegisterFrontendCompilers</c> 一致（便于逐行对照），
    /// 但**判据与顺序无关**（比的是集合）。
    /// </summary>
    public static readonly IReadOnlyList<Entry> All =
    [
        new("CCompiler.CCompilerPlugin", "C"),
        new("BasicCompiler.BasicCompilerPlugin", "basic"),
        new("PascalCompiler.PascalCompilerPlugin", "pascal"),
        new("PythonCompiler.PythonCompilerPlugin", "python"),
        new("LuaCompiler.LuaCompilerPlugin", "lua"),
        new("ForthCompiler.ForthCompilerPlugin", "forth"),
        new("RustCompiler.RustCompilerPlugin", "rust"),
        new("GoCompiler.GoCompilerPlugin", "go"),
        new("LadderCompiler.LadderCompilerPlugin", "ladder"),
        new("CSharpCompiler.CSharpCompilerPlugin", "csharp"),
        new("JavaCompiler.JavaCompilerPlugin", "java"),
        new("JavaScriptCompiler.JavaScriptCompilerPlugin", "javascript"),
        new("SwiftCompiler.SwiftCompilerPlugin", "swift"),
        new("CppCompiler.CppCompilerPlugin", "cpp"),
        new("KotlinCompiler.KotlinCompilerPlugin", "Kotlin"),
        new("SchemeCompiler.SchemePlugin", "Scheme"),
        new("RubyCompiler.RubyCompilerPlugin", "ruby"),
        new("DartCompiler.DartCompilerPlugin", "dart"),
        new("ObjCCompiler.ObjCCompilerPlugin", "objc"),
        new("RCompiler.RCompilerPlugin", "r"),
        new("DCompiler.DCompilerPlugin", "d"),
        new("FortranCompiler.FortranCompilerPlugin", "fortran"),
    ];

    /// <summary>本清单里的插件类型名（判据用）。</summary>
    public static IReadOnlyCollection<string> PluginTypes =>
        All.Select(e => e.PluginType).ToHashSet(StringComparer.Ordinal);

    /// <summary>本清单里的编译器名（上游按 <c>Name.ToLower()</c> 做字典键，这里同样归一化成小写）。</summary>
    public static IReadOnlyCollection<string> NormalizedNames =>
        All.Select(e => e.Name.ToLowerInvariant()).ToHashSet(StringComparer.Ordinal);

    /// <summary>
    /// 从上游 <c>StaticLinkInitializer.cs</c> 的源码文本里抠出**前端**注册段的插件类型名。
    ///
    /// <para>
    /// 只取 <c>RegisterFrontendCompilers</c> 与 <c>RegisterBackendTranslators</c> 两个方法**定义之间**的
    /// 那一段 —— 后端那 18 行也长着 <c>new VMLTranslators.XxxPlugin()</c> 的形状，不圈定范围会把它们
    /// 一起数进来（于是「本仓少了 18 个」这种假红）。
    /// </para>
    /// <para>
    /// 解析失败（锚点找不到 / 一行都没解析出来）**不返回空集合**，而是带着 <c>Error</c> 返回 ——
    /// 「没解析出来」和「解析出来是空的」必须分得开，否则上游一改方法名，这道防线就静默失效了。
    /// </para>
    /// </summary>
    public static (IReadOnlyList<string> Types, string? Error) ParseUpstream(string source)
    {
        var start = source.IndexOf(UpstreamMethodAnchor, StringComparison.Ordinal);
        if (start < 0)
            return ([], $"上游源码里找不到前端注册段（锚点 `{UpstreamMethodAnchor}`）—— 方法名被改了？");

        var end = source.IndexOf(UpstreamMethodEndAnchor, start, StringComparison.Ordinal);
        if (end < 0)
            return ([], $"上游源码里找不到后端注册段（锚点 `{UpstreamMethodEndAnchor}`）—— 定位不到前端段的结尾。");

        var body = source[start..end];
        var types = Regex.Matches(body, @"new\s+([A-Za-z_]\w*\.[A-Za-z_]\w*)\s*\(\s*\)")
            .Select(m => m.Groups[1].Value)
            .Distinct(StringComparer.Ordinal)
            .ToList();

        return types.Count == 0
            ? ([], "上游前端注册段里一行 `new Xxx.YyyPlugin()` 都没解析出来 —— 写法变了？")
            : (types, null);
    }

    /// <summary>
    /// 漂移判定：给定**上游**的插件类型名集合，返回 <c>null</c> 表示一致，否则返回一段人话（含怎么修）。
    /// 纯逻辑，不碰文件系统 —— 自测可以直接喂合成输入来验证「上游多一个 / 少一个 / 改名」都能被抓到。
    /// </summary>
    public static string? DescribeDrift(IReadOnlyCollection<string> upstreamPluginTypes)
    {
        var ours = PluginTypes;
        var upstream = upstreamPluginTypes.ToHashSet(StringComparer.Ordinal);

        var upstreamOnly = upstream.Except(ours).OrderBy(x => x, StringComparer.Ordinal).ToList();
        var oursOnly = ours.Except(upstream).OrderBy(x => x, StringComparer.Ordinal).ToList();
        if (upstreamOnly.Count == 0 && oursOnly.Count == 0) return null;

        var sb = new StringBuilder();
        sb.Append($"VML 前端编译器清单与上游不一致（上游 {upstream.Count} 个 / 本仓 {ours.Count} 个）：");
        if (upstreamOnly.Count > 0)
            sb.Append($"\n  · 上游有、本仓没有（多半是上游加了新语言）：{string.Join(", ", upstreamOnly)}");
        if (oursOnly.Count > 0)
            sb.Append($"\n  · 本仓有、上游没有（多半是抄错了或上游删了）：{string.Join(", ", oursOnly)}");
        sb.Append($"\n  · 同步三处：① 本文件的 All；② WayCoder.Maui/Services/VmlFrontendCompilers.cs 的 RegisterAll；")
          .Append($"③ WayCoder.Maui/WayCoder.Maui.csproj 的 ProjectReference。")
          .Append($"\n    上游来源：{UpstreamRelativePath} 的 RegisterFrontendCompilers。");
        return sb.ToString();
    }
}
