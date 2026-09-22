using System.Text;
using System.Xml.Linq;

namespace WayCoder.UI.Shared;

/// <summary>
/// **VML 工程文件**（`.vmk`）—— 一个 XML 描述"**这一个** VML 程序怎么编"。
///
/// <para>
/// ## 为什么需要它
///
/// 在此之前 VML 只有"把一个源文件丢给编译器"这一种用法。而老程序（autoconf 时代那批）
/// 的宏常常是**构建系统喂进去的** —— `gcc -DVERSION='"cmatrix 2.0"' -DHAVE_CONFIG_H …`
/// —— 没有构建系统，这类宏全缺，且**补头文件补不出来**
/// （见 `docs/老程序兼容性.md` 第九节；实测 `cmatrix` 就是这么卡住的）。
/// 当时的应急修法是 `VMLTOOL_DEFINE` 环境变量：能用，但**一个项目的构建描述
/// 不该待在环境变量里** —— 换台机器、换个终端、CI 里跑都要重新 export，
/// 而且没地方记"入口是哪个文件、头在哪、要链哪些库"。
/// </para>
///
/// <para>
/// ## ⚠ `<Entry>` 是**单数**，这不是省事，是照实描述既有能力
///
/// **VML 没有跨编译单元的链接。** `CompilerProgramBase` 的多文件模式明确拒绝 `-o`
/// （"多文件模式不支持 -o 选项"），它只是把每个 `.c` **各编成同名的 `.vml`**，
/// 没有符号合并；而 `CompileFileWithIncludes` 的签名也是单入口的。
/// </para>
///
/// <para>
/// 所以这里写成 `SRCS` 列表会**许下一个做不到的承诺** —— 用户会以为多个 `.c` 会
/// 一起进程序，实际不会。多文件靠 `#include`（老 C 程序的单 TU 写法，
/// `Examples/c/nyancat.c` 就是这么做的）。
/// </para>
///
/// <para>
/// ## 与 `vmltool.config.xml` 的分工
///
/// 这里**只放项目级**的东西（入口 / 搜索路径 / 宏 / 库 / 产物）。
/// 编译器行为（`Mode` / `Float64` / `GcSections` / 优化级别…）**不在这里** ——
/// 那些归 `vmltool.config.xml`。**同一个值能写在两处**正是本仓最反复踩的坑
/// （"平行表漂移"），宁可少一个便利，也不立第二张表。
/// </para>
///
/// <para>
/// 解析风格**逐字沿用** <see cref="VmlToolConfig"/>：`XElement`（AOT 兼容 ——
/// 被禁的是 `XmlSerializer`，不是它）+ 同一套取值口径。
/// </para>
/// </summary>
public sealed class VmlProject
{
    /// <summary>工程名（只用于显示；缺省时用入口文件名）。</summary>
    public string Name { get; set; } = "";

    /// <summary>**唯一入口**源文件（含 `main` 的那个）。相对路径，基准是 `.vmk` 所在目录。</summary>
    public string Entry { get; set; } = "";

    /// <summary>产物路径。留空 = 与入口同目录同名、按 <see cref="OutputFormat"/> 定后缀。</summary>
    public string Output { get; set; } = "";

    /// <summary>
    /// 产物**类型**：`exe`（可执行程序，默认）或 `lib`（库）。
    ///
    /// <para>
    /// 落到实现上是 <c>VmlProgram.IsLibrary</c> —— 它影响**死代码消除与链接**：
    /// 库不删"没人调用"的函数（它本来就是给别人调的），入口也不要求有 `main`。
    /// </para>
    /// </summary>
    public string OutputKind { get; set; } = KindExe;

    /// <summary>
    /// 产物**格式**。目前实现了两种，其余是**预留的名字**（见 <see cref="ImplementedFormats"/>）。
    ///
    /// <para>
    /// 之所以现在就把名字留出来：`.vmk` 是**枢纽格式**，一旦发出去就有别人的工程文件
    /// 在用。等真做转译时再改字段名，等于让所有既有 `.vmk` 失效 ——
    /// 留个位置的成本是零，改格式的成本是所有用户。
    /// </para>
    /// </summary>
    public string OutputFormat { get; set; } = FormatVml;

    public const string KindExe = "exe";
    public const string KindLib = "lib";

    public const string FormatVml = "vml";
    public const string FormatVmb = "vmb";

    /// <summary>**已经能出**的格式。其余的会**明确报错**，不会静默退回 `.vml`。</summary>
    public static readonly string[] ImplementedFormats = { FormatVml, FormatVmb };

    /// <summary>
    /// **预留**的格式名（转译后端已在 `VMLTranslators/` 里，但还没接到 `.vmk` 这条路上）。
    /// 收在这里是为了让错误信息能说清楚"这个名字认，只是还没做"。
    /// </summary>
    public static readonly string[] ReservedFormats =
        { "bin", "rom", "elf", "hex", "s19", "exe", "dll", "class" };

    /// <summary>格式 → 默认后缀（`<Output>` 没写时用）。</summary>
    public static string ExtensionFor(string format) => format.ToLowerInvariant() switch
    {
        FormatVml => ".vml",
        FormatVmb => ".vmb",
        "bin" or "rom" or "exe" or "dll" => ".bin",
        "elf" => ".elf",
        "hex" => ".hex",
        "s19" => ".s19",
        "class" => ".class",
        _ => ".vml",   // 认不出的格式后面会被挡下来，这里不给它一个新的失败点
    };

    /// <summary>
    /// **其余的编译单元**（除了 <see cref="Entry"/> 之外的 `.c`）。老程序大多是**多文件**的，
    /// 这一项就是为它们准备的。
    ///
    /// <para>
    /// ## 怎么做到的（VML 并没有"多文件编译"这个功能）
    ///
    /// `CompilerProgramBase` 的多文件模式只是把每个 `.c` **各写成一个 `.vml`**，不链接。
    /// 但**库那条路是通的** —— `LinkLibraries` 会把库里的数据标签**重映射**后再合并
    /// （`LibraryLinker.LinkSingleLibrary` 的 `labelMapping`），两个编译单元的同名
    /// `static` 因此互不干扰。
    /// </para>
    ///
    /// <para>
    /// 所以"多文件"在这里是**两步**：每个附加编译单元先编成**目标文件**
    /// （`autoLinkStdLib: false` + `IsLibrary = true` ⇒ 只有用户代码，实测 28 条指令），
    /// 再把它们作为库链进入口。**不这么做的话标准库会被链 N 遍** ——
    /// 实测两个文件直接链：98080 条指令（正好两倍），走目标文件：49070 条。
    /// </para>
    /// </summary>
    public List<string> Sources { get; } = new();

    /// <summary>
    /// 中间产物（目标文件 `.vml`）放哪。留空 = `<.vmk 所在目录>/.vmk-obj/`。
    ///
    /// <para>
    /// 单独放一个目录而不是写在源文件旁边：中间产物**每次构建都重生成**，
    /// 混在源码树里既脏又容易被误提交（老项目的 `.gitignore` 可不会认我们的东西）。
    /// </para>
    /// </summary>
    public string ObjDir { get; set; } = "";

    /// <summary>追加的头文件搜索路径（对应 `-I`）。</summary>
    public List<string> Includes { get; } = new();

    /// <summary>
    /// 宏定义（对应 `-D`）。值 `"1"` 表示"只有名字"（与 `-DFOO` 同义）。
    /// </summary>
    public List<(string Name, string Value)> Defines { get; } = new();

    /// <summary>额外要挂的库模块名（选填；留空就用 `vmltool.config.xml` 里的默认清单）。</summary>
    public List<string> Libs { get; } = new();

    /// <summary>`.vmk` 所在目录 —— 所有相对路径的基准。</summary>
    public string BaseDir { get; private set; } = ".";

    // ═══════════════════════════════════════════════════════════════
    // 解析
    // ═══════════════════════════════════════════════════════════════

    /// <summary>认识的所有顶层元素 —— 用来把**拼错的**标签报出来（见 <see cref="Parse"/>）。</summary>
    private static readonly string[] KnownRoots =
        { "Name", "Entry", "Output", "ObjDir", "Sources", "Includes", "Defines", "Libs" };

    /// <summary>
    /// 从 `.vmk` 文本解析。**出错就抛**（带人话），不返回半个工程。
    ///
    /// <para>
    /// ⚠ **不认识的元素必须报出来**，不能静默跳过 —— 这是本仓的 CLI 铁律
    /// （"有错即报错退出，绝不静默忽略"；被 `--modle` 拼错却静默生效坑过一次）。
    /// 拼错的 `<Defnes>` 如果静默忽略，用户会得到"宏没生效、但一个字都没提示"，
    /// 而这类失败**没有任何编译期信号**，只能靠人回头怀疑自己拼错了。
    /// </para>
    /// </summary>
    /// <param name="xml">`.vmk` 的文本</param>
    /// <param name="baseDir">相对路径的基准目录（一般是 `.vmk` 所在目录）</param>
    public static VmlProject Parse(string xml, string baseDir)
    {
        XElement root;
        try
        {
            root = XElement.Parse(xml);
        }
        catch (Exception ex)
        {
            throw new VmlProjectException($"工程文件不是合法的 XML：{ex.Message}");
        }

        if (!string.Equals(root.Name.LocalName, "VMLProject", StringComparison.Ordinal))
            throw new VmlProjectException(
                $"根元素必须是 <VMLProject>，实际是 <{root.Name.LocalName}>。");

        var p = new VmlProject { BaseDir = baseDir };

        // 认不出的顶层元素 —— 报出来（见方法注释里那条铁律）
        var unknown = root.Elements()
            .Select(e => e.Name.LocalName)
            .Where(n => !KnownRoots.Contains(n, StringComparer.Ordinal))
            .Distinct()
            .ToList();
        if (unknown.Count > 0)
            throw new VmlProjectException(
                $"工程文件里有不认识的元素：{string.Join("、", unknown.Select(n => "<" + n + ">"))}。"
                + $"可用的只有：{string.Join("、", KnownRoots.Select(n => "<" + n + ">"))}。");

        p.Name = Val(root, "Name");
        p.Entry = Val(root, "Entry");

        // `<Output Kind="exe" Format="vml">build/x.vml</Output>`
        // 两个属性都**选填**（缺省 exe/vml = 老行为，既有 `.vmk` 一个字节都不用改）。
        var outEl = root.Element("Output");
        p.Output = outEl?.Value?.Trim() ?? "";
        var kind = ((string?)outEl?.Attribute("Kind"))?.Trim().ToLowerInvariant();
        var fmt = ((string?)outEl?.Attribute("Format"))?.Trim().ToLowerInvariant();

        if (!string.IsNullOrEmpty(kind))
        {
            if (kind != KindExe && kind != KindLib)
                throw new VmlProjectException(
                    $"<Output Kind=\"{kind}\"> 不认得 —— 只支持 `exe`（可执行程序）与 `lib`（库）。");
            p.OutputKind = kind;
        }

        if (!string.IsNullOrEmpty(fmt))
        {
            // ⚠ 认得出名字但**还没实现**的，与"压根不认识的名字"要分开报 ——
            //   前者是"排期问题"，后者多半是拼错了。混成一句会让用户去猜是哪种。
            if (!ImplementedFormats.Contains(fmt) && !ReservedFormats.Contains(fmt))
                throw new VmlProjectException(
                    $"<Output Format=\"{fmt}\"> 不认得。现在能做的是 "
                    + $"{string.Join("、", ImplementedFormats)}；预留了 "
                    + $"{string.Join("、", ReservedFormats)}。");
            p.OutputFormat = fmt;
        }

        p.ObjDir = Val(root, "ObjDir");
        foreach (var e in root.Element("Sources")?.Elements("File") ?? Enumerable.Empty<XElement>())
        {
            var v = e.Value?.Trim();
            if (!string.IsNullOrEmpty(v)) p.Sources.Add(v);
        }

        foreach (var e in root.Element("Includes")?.Elements("Dir") ?? Enumerable.Empty<XElement>())
        {
            var v = e.Value?.Trim();
            if (!string.IsNullOrEmpty(v)) p.Includes.Add(v);
        }

        foreach (var e in root.Element("Defines")?.Elements("Define") ?? Enumerable.Empty<XElement>())
        {
            // 名字走**属性**（`<Define Name="VERSION" Value="1.2"/>`）——
            // 与 `vmltool.config.xml` 里 `<Language Name= Libs=>` 是同一种点缀用法。
            // ⚠ 也容忍 `<Define>FOO=1</Define>` 这种图省事的写法（人写 XML 容易这么写），
            //   但**只有两种形态都认不出时才报错**，不猜。
            var name = (string?)e.Attribute("Name");
            var value = (string?)e.Attribute("Value");

            if (string.IsNullOrWhiteSpace(name))
            {
                var text = e.Value?.Trim() ?? "";
                int eq = text.IndexOf('=');
                if (eq < 0 && text.Length > 0) { name = text; value = null; }
                else if (eq > 0) { name = text[..eq].Trim(); value = text[(eq + 1)..].Trim(); }
            }

            if (string.IsNullOrWhiteSpace(name))
                throw new VmlProjectException(
                    "`<Defines>` 里有一条 `<Define>` 没写名字。写成 "
                    + "<Define Name=\"VERSION\" Value=\"1.2\"/>，或者 <Define Name=\"DEBUG\"/>。");

            // 没有 Value ⇒ "1"，与 `-DFOO` 的语义一致
            p.Defines.Add((name.Trim(), string.IsNullOrEmpty(value) ? "1" : value));
        }

        foreach (var e in root.Element("Libs")?.Elements("Lib") ?? Enumerable.Empty<XElement>())
        {
            var v = e.Value?.Trim();
            if (!string.IsNullOrEmpty(v)) p.Libs.Add(v);
        }

        if (string.IsNullOrWhiteSpace(p.Entry))
            throw new VmlProjectException("工程文件里没写 <Entry>（入口源文件，含 main 的那个）。");

        if (string.IsNullOrWhiteSpace(p.Name))
            p.Name = Path.GetFileNameWithoutExtension(p.Entry);

        return p;
    }

    /// <summary>从文件读（找不到/读不了直接抛）。</summary>
    public static VmlProject Load(string path)
    {
        var full = Path.GetFullPath(path);
        if (!File.Exists(full))
            throw new VmlProjectException($"找不到工程文件：{full}");

        var p = Parse(File.ReadAllText(full), Path.GetDirectoryName(full) ?? ".");
        p.SourcePath = full;
        return p;
    }

    /// <summary>这个工程是从哪个 `.vmk` 读出来的（手工构造时为 null）。</summary>
    public string? SourcePath { get; private set; }

    // ═══════════════════════════════════════════════════════════════
    // 解析出来的路径（相对 → 绝对，基准是 .vmk 所在目录）
    // ═══════════════════════════════════════════════════════════════

    /// <summary>
    /// 路径分隔符统一成 `/`。
    ///
    /// <para>
    /// ⚠ **`.vmk` 是跨平台的**：Windows 上生成的工程文件要能在 macOS / 安卓上打开。
    /// 而 `Path.Combine("base", "src\\main.c")` 在 Unix 上得到的是
    /// **一个名字里带反斜杠的文件** —— 不报错，只是"文件不存在"，最难查的那种。
    /// 所以在**读写两个边界**各转一次：写的时候转（见 <see cref="ToXml"/>），
    /// 读的时候也转（这里）—— 手写的 `.vmk` 同样可能带反斜杠。
    /// </para>
    ///
    /// <para>
    /// 规则本身**不在这里重写** —— 调 <see cref="PathText.Normalize"/>（同目录，带自测）。
    /// `.vmk` 与 `PathText` 都住在 `UI/Shared/`，正是"跨端共享纯逻辑"该待的地方：
    /// 桌面自测、`scripts/vmlcli`、手机端**编的是同一份源码**，不会漂。
    /// </para>
    /// </summary>
    private static string Norm(string p) => PathText.Normalize(p);

    /// <summary>入口源文件的**绝对路径**。</summary>
    public string EntryPath => Path.GetFullPath(Path.Combine(BaseDir, Norm(Entry)));

    /// <summary>
    /// 产物的**绝对路径** —— 没写 `<Output>` 时与入口同目录、同名的 `.vml`
    /// （与手机端 `MauiVml.NextArtifact` 的规则一致）。
    /// </summary>
    public string OutputPath => string.IsNullOrWhiteSpace(Output)
        ? Path.ChangeExtension(EntryPath, ExtensionFor(OutputFormat))
        : Path.GetFullPath(Path.Combine(BaseDir, Norm(Output)));

    /// <summary>附加编译单元的**绝对路径**列表。</summary>
    public List<string> SourcePaths =>
        Sources.Select(f => Path.GetFullPath(Path.Combine(BaseDir, Norm(f)))).ToList();

    /// <summary>中间产物目录的**绝对路径**（没写 `<ObjDir>` 时用 `.vmk-obj/`）。</summary>
    public string ObjDirPath => string.IsNullOrWhiteSpace(ObjDir)
        ? Path.Combine(BaseDir, ".vmk-obj")
        : Path.GetFullPath(Path.Combine(BaseDir, Norm(ObjDir)));

    /// <summary>`-I` 用的绝对路径列表。</summary>
    public List<string> IncludePaths =>
        Includes.Select(d => Path.GetFullPath(Path.Combine(BaseDir, Norm(d)))).ToList();

    /// <summary>`-D` 用的 `名=值` 列表（值恒非空，缺省是 `"1"`）。</summary>
    public List<string> DefineArgs => Defines.Select(d => $"{d.Name}={d.Value}").ToList();

    // ═══════════════════════════════════════════════════════════════
    // 序列化（导入器写 `.vmk` 用）
    // ═══════════════════════════════════════════════════════════════

    /// <summary>
    /// 写成 `.vmk` 文本。**手写 XML 而不是 `XElement.ToString()`** ——
    /// 要带中文注释说明每个字段的来历，而 `XElement` 只会吐光秃秃的标签。
    /// </summary>
    /// <param name="header">顶部注释（导入器用来写"由哪个 Makefile 生成"）</param>
    public string ToXml(string? header = null)
    {
        var sb = new StringBuilder();
        sb.Append("<?xml version=\"1.0\" encoding=\"utf-8\"?>\n");
        if (!string.IsNullOrEmpty(header))
            sb.Append("<!-- ").Append(header).Append(" -->\n");
        sb.Append("<VMLProject Version=\"1\">\n");
        sb.Append("  <Name>").Append(Esc(Name)).Append("</Name>\n");
        sb.Append("  <!-- 唯一入口源文件（含 main 的那个）。相对本文件所在目录。 -->\n");
        sb.Append("  <Entry>").Append(Esc(Norm(Entry))).Append("</Entry>\n");
        // 属性只在**非默认**时写出来 —— 默认的 exe/vml 是老行为，写出来只是噪音
        var kindAttr = OutputKind != KindExe ? $" Kind=\"{Esc(OutputKind)}\"" : "";
        var fmtAttr = OutputFormat != FormatVml ? $" Format=\"{Esc(OutputFormat)}\"" : "";
        if (!string.IsNullOrWhiteSpace(Output) || kindAttr.Length > 0 || fmtAttr.Length > 0)
            sb.Append("  <Output").Append(kindAttr).Append(fmtAttr).Append('>')
              .Append(Esc(Norm(Output))).Append("</Output>\n");

        if (Sources.Count > 0)
        {
            sb.Append("  <!-- 其余编译单元：先各编成目标文件，再链进入口 -->\n");
            sb.Append("  <Sources>\n");
            foreach (var f in Sources) sb.Append("    <File>").Append(Esc(Norm(f))).Append("</File>\n");
            sb.Append("  </Sources>\n");
        }

        if (Includes.Count > 0)
        {
            sb.Append("  <!-- 追加的头文件搜索路径（对应 -I） -->\n");
            sb.Append("  <Includes>\n");
            foreach (var d in Includes) sb.Append("    <Dir>").Append(Esc(Norm(d))).Append("</Dir>\n");
            sb.Append("  </Includes>\n");
        }

        if (Defines.Count > 0)
        {
            sb.Append("  <!-- 宏定义（对应 -D）；没有 Value 的取 1，与 -DFOO 同义 -->\n");
            sb.Append("  <Defines>\n");
            foreach (var (n, v) in Defines)
            {
                sb.Append("    <Define Name=\"").Append(Esc(n)).Append('"');
                if (v != "1") sb.Append(" Value=\"").Append(Esc(v)).Append('"');
                sb.Append("/>\n");
            }
            sb.Append("  </Defines>\n");
        }

        if (Libs.Count > 0)
        {
            sb.Append("  <Libs>\n");
            foreach (var l in Libs) sb.Append("    <Lib>").Append(Esc(l)).Append("</Lib>\n");
            sb.Append("  </Libs>\n");
        }

        sb.Append("</VMLProject>\n");
        return sb.ToString();
    }

    private static string Esc(string s) => s
        .Replace("&", "&amp;").Replace("<", "&lt;").Replace(">", "&gt;").Replace("\"", "&quot;");

    // ═══ 辅助（与 VmlToolConfig 同一套口径）═══

    private static string Val(XElement root, string name, string def = "") =>
        root.Element(name)?.Value?.Trim() ?? def;
}

/// <summary>`.vmk` 读不了 / 写不对时抛这个（消息是给用户看的人话）。</summary>
public sealed class VmlProjectException : Exception
{
    public VmlProjectException(string message) : base(message) { }
}
