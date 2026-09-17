using VMLPlugins;
using VMLPlugins.Interfaces;
using WayCoder.UI.Shared;

namespace WayCoder.Maui.Services;

/// <summary>
/// 前端编译器清单与本文件的 <c>RegisterAll</c> 对不上（见 <see cref="VmlFrontendCompilers.Verify"/>）。
///
/// <para>
/// <b>为什么要一个专门的类型：</b>调用方 <c>MauiVml.CollectExtensions</c> 对注册是
/// <b>catch-all</b> 的（「注册失败不该把文件页带崩」—— 那条容错本身是对的，注册失败时退化
/// 成「一门语言都不认」只是少一个入口）。但**清单漂移不属于那一类**：它是我们自己代码里的
/// 不一致，吞掉就等于「护栏看着在、其实不拦」，还顺手把症状变成「文件页上某个语言莫名不能用」。
/// 所以给它一个独立类型，让那个 catch 能把它排除在外 —— **漂移必须炸出来**。
/// </para>
/// </summary>
internal sealed class VmlCompilerListDriftException(string message) : InvalidOperationException(message);

/// <summary>
/// 把 22 个 VML 前端编译器**静态注册**进 <see cref="PluginManager"/> —— 手上这份清单的注册侧。
///
/// <para>
/// <b>这是 <c>third_party/vml/VMLTool/StaticLink/StaticLinkInitializer.RegisterFrontendCompilers</c>
/// 的本地副本</b>（只抄了前端那半截）。原版连 <c>RegisterBackendTranslators</c> 一起做，
/// 而后端那 18 个翻译器来自 <c>VMLTranslators</c>（+ <c>VMLToHex</c>）—— 那是给裸机/单片机输出
/// 汇编与 hex/elf/bin 的，手机端用不到，正是要从 APK 里去掉的那 ~460 KB。
/// 所以这里**只注册前端**，也不再引 <c>VMLTool.csproj</c>。
/// </para>
///
/// <para>
/// <b>清单本身不在这里</b>，在 <see cref="VmlFrontendCompilerList"/>（桌面自测要读同一份）。
/// 本文件只负责「按清单 new 出来」，并在注册完成后**断言注册到的集合就是清单里那 22 个**。
/// </para>
///
/// <para>
/// 为什么不走 <c>PluginManager.LoadPluginsFromAssembly</c>（上游的 <c>Assembly.LoadFrom</c> 反射路径）：
/// 那条路在 MAUI 的裁剪 / AOT 下不可靠（程序集是静态链进来的，磁盘上没有独立的 DLL 可 LoadFrom），
/// 上游自己在 STATIC_LINK 模式里也绕开了它。
/// </para>
/// </summary>
internal static class VmlFrontendCompilers
{
    /// <summary>
    /// 注册全部 22 个前端编译器，然后校验「注册到的 == 清单里的」。
    ///
    /// <para>
    /// <b>校验失败会抛</b> —— 这是刻意的：这份清单是**手抄**的，与上游之间没有编译期联系，
    /// 一旦有人改了本文件却没同步 <see cref="VmlFrontendCompilerList"/>（或反过来），
    /// 唯一能兜住的时刻就是这里。抛出来的消息会写清「少了谁 / 多了谁 / 去哪同步」。
    /// 症状上宁可响亮地炸一次，也不要「某个语言莫名不能用」——后者极难排查（见 CLAUDE.md
    /// 「平行表的典型来源」：漏改一处会不会有用户可见后果 ⇒ 会，就要钉死）。
    /// </para>
    /// </summary>
    public static void RegisterAll(PluginManager manager)
    {
        var registered = new List<IFrontendCompiler>(VmlFrontendCompilerList.All.Count);

        // 顺序与上游 RegisterFrontendCompilers 一致，便于逐行对照。
        Register(new CCompiler.CCompilerPlugin());
        Register(new BasicCompiler.BasicCompilerPlugin());
        Register(new PascalCompiler.PascalCompilerPlugin());
        Register(new PythonCompiler.PythonCompilerPlugin());
        Register(new LuaCompiler.LuaCompilerPlugin());
        Register(new ForthCompiler.ForthCompilerPlugin());
        Register(new RustCompiler.RustCompilerPlugin());
        Register(new GoCompiler.GoCompilerPlugin());
        Register(new LadderCompiler.LadderCompilerPlugin());
        Register(new CSharpCompiler.CSharpCompilerPlugin());
        Register(new JavaCompiler.JavaCompilerPlugin());
        Register(new JavaScriptCompiler.JavaScriptCompilerPlugin());
        Register(new SwiftCompiler.SwiftCompilerPlugin());
        Register(new CppCompiler.CppCompilerPlugin());
        Register(new KotlinCompiler.KotlinCompilerPlugin());
        Register(new SchemeCompiler.SchemePlugin());
        Register(new RubyCompiler.RubyCompilerPlugin());
        Register(new DartCompiler.DartCompilerPlugin());
        Register(new ObjCCompiler.ObjCCompilerPlugin());
        Register(new RCompiler.RCompilerPlugin());
        Register(new DCompiler.DCompilerPlugin());
        Register(new FortranCompiler.FortranCompilerPlugin());

        Verify(manager, registered);
        return;

        void Register(IFrontendCompiler compiler)
        {
            manager.RegisterFrontendCompiler(compiler);
            registered.Add(compiler);
        }
    }

    /// <summary>
    /// 断言「注册到的编译器 == <see cref="VmlFrontendCompilerList.All"/>」，不一致就抛。
    /// 判据取两组，**缺一组就会漏**：
    /// ① 插件类型完整名 —— 抓「清单里有一行、RegisterAll 里忘了 new」；
    /// ② 编译器自报的 <c>Name</c>（小写，与 <c>PluginManager</c> 的字典键同一口径）—— 抓
    ///    「上游把某个编译器的 Name 改了、我们清单里的名字过时了」（扩展名派发是按 Name 查表的）；
    /// ③ <c>PluginManager.FrontendCompilerCount</c> —— 抓「两个插件撞了同一个 Name，被字典静默覆盖」。
    /// </summary>
    private static void Verify(PluginManager manager, List<IFrontendCompiler> registered)
    {
        var wantTypes = VmlFrontendCompilerList.PluginTypes;
        var gotTypes = registered.Select(c => c.GetType().FullName ?? c.GetType().Name)
            .ToHashSet(StringComparer.Ordinal);

        var wantNames = VmlFrontendCompilerList.NormalizedNames;
        var gotNames = manager.GetAllFrontendCompilers()
            .Select(c => c.Name.ToLowerInvariant())
            .ToHashSet(StringComparer.Ordinal);

        var missingTypes = wantTypes.Except(gotTypes).OrderBy(x => x, StringComparer.Ordinal).ToList();
        var extraTypes = gotTypes.Except(wantTypes).OrderBy(x => x, StringComparer.Ordinal).ToList();
        var missingNames = wantNames.Except(gotNames).OrderBy(x => x, StringComparer.Ordinal).ToList();
        var extraNames = gotNames.Except(wantNames).OrderBy(x => x, StringComparer.Ordinal).ToList();

        if (missingTypes.Count == 0 && extraTypes.Count == 0
            && missingNames.Count == 0 && extraNames.Count == 0
            && manager.FrontendCompilerCount == VmlFrontendCompilerList.All.Count)
            return;

        var detail = string.Join("\n",
            $"  期望 {VmlFrontendCompilerList.All.Count} 个 / 实际注册到 {manager.FrontendCompilerCount} 个",
            missingTypes.Count > 0 ? $"  清单里有但没注册（RegisterAll 漏 new）：{string.Join(", ", missingTypes)}" : null,
            extraTypes.Count > 0 ? $"  注册了但清单里没有（清单漏记）：{string.Join(", ", extraTypes)}" : null,
            missingNames.Count > 0 ? $"  清单里的编译器名没被注册（上游改名了？）：{string.Join(", ", missingNames)}" : null,
            extraNames.Count > 0 ? $"  注册到的编译器名不在清单里：{string.Join(", ", extraNames)}" : null);

        throw new VmlCompilerListDriftException(
            "VML 前端编译器清单漂移 —— VmlFrontendCompilers.RegisterAll 注册到的集合与 "
            + "WayCoder/UI/Shared/VmlFrontendCompilerList.cs 的 All 对不上。\n"
            + detail + "\n"
            + "  同步三处：① VmlFrontendCompilerList.All；② 本文件 RegisterAll；"
            + "③ WayCoder.Maui.csproj 的 ProjectReference。\n"
            + $"  上游来源：{VmlFrontendCompilerList.UpstreamRelativePath} 的 RegisterFrontendCompilers。");
    }
}
