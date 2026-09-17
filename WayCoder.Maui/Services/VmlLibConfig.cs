using System.Xml.Linq;

namespace WayCoder.Maui.Services;

/// <summary>
/// <c>vmltool.config.xml</c> 的最小读取器 —— 只为「这个源文件该链接哪些标准库」这一件事。
///
/// <para>
/// <b>抄自 vendored 上游：</b><c>third_party/vml/VMLTool/VmlToolConfig.cs</c>。
/// 原类 340 行，绝大部分是 CLI 用的东西（<c>ApplyTo(CommandLineOptions)</c>、内存/优化/输出格式、
/// 数值模式、方言…），**那些类型（<c>CommandLineOptions</c>/<c>TargetMode</c>/<c>NumericHelper</c>…）
/// 全在 VMLTool 的 <c>Exe</c> 侧**，我们一个都用不到。手机端真正用到的只有两件事：
/// </para>
/// <list type="number">
///   <item><description><c>Load(path)</c> —— 读那份 XML（<c>&lt;DefaultLibs&gt;</c> + <c>&lt;Languages&gt;</c> + 两个路径元素）；</description></item>
///   <item><description><c>ResolveLibs(lang, baseDir)</c> —— 把库名解析成**已存在的库文件绝对路径**。</description></item>
/// </list>
///
/// <para>
/// <b>为什么必须照抄而不是自己写一条「C 要链 crt.vml」的规则：</b>那就是本仓库反复踩的平行表 ——
/// 上游改了库清单（加 <c>vmlui.vml</c>、换语言绑定的库）时我们这份不会跟着变，且是**静默**的
/// （链接少了库 ⇒ 运行期才报「未找到标签: shared_puts」）。这份 XML 随标准库一起解压、
/// 与 <c>Lib/</c> 同源，所以「改了配置不用改我们的代码」。
/// </para>
///
/// <para>
/// <b>语义与上游逐条对齐</b>（两处都是踩过坑的）：
/// ① <c>ResolveLibs</c> **先 <c>DefaultLibs</c> 再该语言的 <c>Libs</c>**，两边都按
///    <c>SharedPath → LibPath</c> 的顺序找文件、去重；
/// ② <c>ResolveLibPath</c> 返回的是**解析好的库文件路径**，绝不是目录 —— 给目录会让
///    <c>ConvertLibraryPathsToIncludes</c> 把该目录下每一个 <c>.vml</c> 都挂上去（上游注释：
///    「仅添加已解析的库文件，不添加目录路径（避免全库链接）」），实测同一个 hello.c
///    给目录是 93423 条指令、给库文件是 36261 条。
/// </para>
///
/// <para>
/// ⚠ <b>上游改了要同步哪里：</b><c>VmlToolConfig.cs</c> 的 <c>Parse</c> / <c>ResolveLibs</c> /
/// <c>ResolveLibPath</c>。改了字段名或查找顺序，这份要跟着改 —— 判据在调用方：
/// <c>MauiVml.BuildProgram</c> 拿到空库清单会**当失败处理**（不会静默给用户一个「编译成功但一跑就
/// 找不到函数」的程序），所以漏同步是响亮的，不是静默的。
/// </para>
///
/// <para>
/// 与上游的**唯一有意差异**：上游 <c>Load</c> 有个静态单例缓存（<c>_instance</c>，三层搜索
/// 「CLI 路径 → CWD → $VML_HOME」，第一次成功之后就不再读盘）。手机端只有一个调用点、每次都显式
/// 传「解压出来的库根」，缓存与那两层兜底搜索都没有用处，反而会让「第一次没读到就永远读不到」；
/// 所以这里**不做缓存、只认显式路径**，读不到就返回 <c>null</c>（调用方会亮出来）。
/// </para>
/// </summary>
internal sealed class VmlLibConfig
{
    /// <summary>共享库目录（XML 的 <c>&lt;SharedPath&gt;</c>，默认 <c>Lib/shared</c>）。</summary>
    public string SharedPath { get; private set; } = "Lib/shared";

    /// <summary>库目录（XML 的 <c>&lt;LibPath&gt;</c>，默认 <c>Lib</c>）。</summary>
    public string LibPath { get; private set; } = "Lib";

    /// <summary>所有语言都链的库（XML 的 <c>&lt;DefaultLibs&gt;</c>，ex. <c>builtins.vml</c>）。</summary>
    public List<string> DefaultLibs { get; } = [];

    /// <summary>各语言的额外库（XML 的 <c>&lt;Languages&gt;&lt;Language Name Libs&gt;</c>）。</summary>
    public List<VmlLangLibs> Languages { get; } = [];

    /// <summary>
    /// 读一份 <c>vmltool.config.xml</c>。文件不存在 / XML 损坏 → <c>null</c>
    /// （调用方 <c>MauiVml.BuildProgram</c> 会把它变成一句给用户看的失败原因，不静默）。
    /// </summary>
    public static VmlLibConfig? Load(string configPath)
    {
        try
        {
            if (!File.Exists(configPath)) return null;

            var root = XElement.Load(configPath);
            var c = new VmlLibConfig
            {
                SharedPath = Val(root, "SharedPath", "Lib/shared"),
                LibPath = Val(root, "LibPath", "Lib"),
            };

            foreach (var e in root.Element("DefaultLibs")?.Elements("Lib") ?? [])
            {
                var v = e.Value?.Trim();
                if (!string.IsNullOrEmpty(v)) c.DefaultLibs.Add(v);
            }

            foreach (var le in root.Element("Languages")?.Elements("Language") ?? [])
                c.Languages.Add(new VmlLangLibs
                {
                    Name = le.Attribute("Name")?.Value ?? "",
                    Libs = le.Attribute("Libs")?.Value ?? "",
                });

            return c;
        }
        catch
        {
            return null;   // 配置读不了 ⇒ 调用方把「库清单为空」当失败亮出来
        }
    }

    /// <summary>这门语言的库配置；没有对应条目时返回一个空的（与上游同口径：不算错，只是没有额外库）。</summary>
    public VmlLangLibs GetLangLibs(string lang)
    {
        if (string.IsNullOrEmpty(lang)) lang = "default";
        return Languages.FirstOrDefault(l => string.Equals(l.Name, lang, StringComparison.OrdinalIgnoreCase))
               ?? new VmlLangLibs { Name = lang };
    }

    /// <summary>
    /// 解析出「该链接哪些库文件」（绝对路径，**文件**不是目录，已去重）。
    ///
    /// <para><paramref name="baseDir"/> = 解压出来的 VML 库根（<c>&lt;baseDir&gt;/Lib/...</c> 就是库）。</para>
    /// </summary>
    public List<string> ResolveLibs(string lang, string baseDir)
    {
        var result = new List<string>();

        foreach (var lib in DefaultLibs)
            Add(ResolveLibPath(lib, baseDir));

        var libs = GetLangLibs(lang).Libs?.Split([',', ';'], StringSplitOptions.RemoveEmptyEntries)
                   ?? [];
        foreach (var lib in libs)
            Add(ResolveLibPath(lib.Trim(), baseDir));

        return result;

        void Add(string? path)
        {
            if (path != null && !result.Contains(path)) result.Add(path);
        }
    }

    /// <summary>
    /// 库名 → 库文件绝对路径。已经是存在的绝对路径就原样返回；否则补 <c>.vml</c> 后缀，
    /// 依次在 <c>SharedPath</c> / <c>LibPath</c> 下找（与上游同序），都没有则 <c>null</c>。
    /// </summary>
    private string? ResolveLibPath(string libName, string baseDir)
    {
        if (Path.IsPathRooted(libName) && File.Exists(libName)) return libName;
        if (!libName.EndsWith(".vml", StringComparison.OrdinalIgnoreCase)) libName += ".vml";

        foreach (var dir in new[] { SharedPath, LibPath })
        {
            if (string.IsNullOrEmpty(dir)) continue;
            var fullPath = Path.GetFullPath(Path.Combine(baseDir, dir, libName));
            if (File.Exists(fullPath)) return fullPath;
        }
        return null;
    }

    private static string Val(XElement root, string name, string def) =>
        root.Element(name)?.Value ?? def;
}

/// <summary>一门语言的库清单（对应 XML 的 <c>&lt;Language Name=".." Libs="a.vml,b.vml" /&gt;</c>）。</summary>
internal sealed class VmlLangLibs
{
    public string Name { get; init; } = "";

    /// <summary>逗号/分号分隔的库名。</summary>
    public string Libs { get; init; } = "";
}
