using System.IO.Compression;
using System.Security.Cryptography;
using System.Text;
using VMLAssembler;
using VMLPlugins;
using VMLPlugins.Interfaces;
using VMLRuntime;
using WayCoder.Tools;
using WayCoder.UI.Shared;
using WayCoder.UI.Shared.Terminal;
using WayCoder.UI.Tui.Edit;

namespace WayCoder.Maui.Services;

/// <summary>
/// 把 VML（用户自己的编译器 + 翻译器 + 虚拟机）**在进程内**接进 App。
///
/// 关键决定：**不把它编成可执行文件丢进 shell 跑**，而是以库的形式链进来。
/// 三条理由，每一条单独都足够：① iOS 的 `fork/exec` 被沙箱物理拒绝 —— 进程外那条路永远上不了 iOS；
/// ② Android 10+ 的 W^X 让 app 私有目录里的文件不可 exec，得把二进制塞进 `jniLibs` 当 `.so`；
/// ③ 自包含 .NET 运行时几百 MB，而托管代码链进来会被链接器裁。
///
/// 两条路都在这一个类里：纯 VML 汇编（<see cref="RunAssembly"/>）与高级语言
/// （<see cref="CompileAndRun"/>，22 种语言按扩展名自动派发）。
/// </summary>
internal static class MauiVml
{
    /// <summary>
    /// **耗时且无输出**的阶段往上报的钩子（目前只有「解压标准库」与「前端编译」两处）。
    ///
    /// 存在的理由：解压 39 MB / 5200+ 个文件要好几秒，前端编译也要几秒 —— 这期间屏幕上
    /// **一个字都不变**，用户看到的和"卡死了"完全一样。宿主接到就写进输出区，把等待变成可见的状态。
    ///
    /// 只在**宿主主动发起的运行**期间由宿主装上（见 `ShellPage`），AI 调 `vml` 工具那条路
    /// 保持 null —— 否则用户在命令行页好好地看着，聊天那边跑个 VML 会凭空冒出一行解压提示。
    /// 回调**在后台线程上触发**，接的人自己负责切回 UI 线程。
    /// </summary>
    public static Action<string>? OnProgress;

    /// <summary>
    /// **输出流式出口**：程序还在跑的时候，就把新增的输出一段段交出来。
    ///
    /// <para>
    /// 不装它（null）时走的是老路 —— 跑完拿整段。**装不装由宿主决定**，与
    /// <see cref="OnProgress"/> 同一条规矩：只在宿主主动发起的运行时装
    /// （`ShellPage`），AI 调 `vml` 工具那条路保持 null（它要的是"最终结果"那一整段）。
    /// </para>
    ///
    /// <para>
    /// <b>为什么非有它不可</b>：`top` / `vim` / `mc` / `cmatrix` 这一整类
    /// （PC/Linux/老 Mac 程序里的绝大多数）是"不退出就一直画"的。整段缓冲的观感是
    /// **运行期间屏幕上什么都没有**，按 q 也没反应 —— 这一整类一个都跑不起来。
    /// </para>
    ///
    /// <para>
    /// 回调**在 VM 线程上同步触发**（不是后台线程）—— 正因如此它才不需要锁：
    /// 刷新的时机全在写输出的那一个线程上（见 `CaptureIo.FlushIfDue` 的说明）。
    /// 接的人自己负责切回 UI 线程。
    /// </para>
    /// </summary>
    public static Action<string>? OnOutputChunk;

    /// <summary>流式输出的节流间隔（毫秒）。全屏程序每秒重画几十次，不节流会把 UI 拖死。</summary>
    private const int OutputFlushMs = 80;

    /// <summary>流式输出的节流字节数（攒够这么多立刻刷，不等时间）。</summary>
    private const int OutputFlushBytes = 16 * 1024;

    /// <summary>
    /// 最近一次运行的**诊断尾巴**（运行时报错 / 被强制停止 / syscall 被拒），已转 markup。
    ///
    /// <para>
    /// **只在流式那条路上用**：正文已经边跑边交给宿主了，但诊断是**跑完才知道**的
    /// （`diag`/`diagErr` 在 `vm.Run` 之后的 `finally` 里才读得到），只能补在后面。
    /// 不这么做的话，流式一开，`内存错误(PC=…)` 那套报错就**全丢了** ——
    /// 而"程序崩了、用户只看到没有输出"正是这个平台反复修过的那类故障。
    /// </para>
    /// </summary>
    public static string LastDiagnostics = "";

    /// <summary>本次运行走没走流式（= 装没装 <see cref="OnOutputChunk"/>）。宿主据此决定收尾怎么写。</summary>
    public static bool LastRunStreamed;

    /// <summary>
    /// 内置副本的版本 —— **只是写进 `.lib-version` 给人看的**（排查时一眼知道手机上那份是哪版）。
    /// 判断「要不要重新解压」用的是 <see cref="LibFingerprint"/>（zip 内容指纹），**不是它**。
    ///
    /// 这一点是刻意的：靠「记得改常量」来触发重解压，迟早会漏 —— 往 `Lib/` 里加了文件、
    /// 或改了 `vmltool.config.xml` 却忘了动这个常量，手机就会一直用**旧的解压结果**，
    /// 表现为「新加的东西在桌面上好好的、装到手机上就是没有」，排查方向还会被"版本号对得上"带偏。
    /// </summary>
    private const string LibVersion = "v1.66.67";

    /// <summary>内建的自检程序（汇编形式）—— 用来验证「汇编 → 跑 → 收输出」这条链路是通的。</summary>
    public const string HelloWorldAsm = """
.entry main
.stack 256
.vectors 0x0
.data
msg: .string "Hello, VML!"
.text
main:
LEA R0 msg
SYSCALL #1
HALT
""";

    /// <summary>
    /// **按「给的是源码还是文件」+ 扩展名选路，然后跑** —— 这是**派发的唯一实现**。
    ///
    /// `VmlTool`（AI 调用）与命令行页（用户敲）都走这里。分派规则不是我们自己定的，
    /// 是上游 `VMLTool` 的（`Program.Compile.cs:34` 把 `.vml` 直接当汇编；其余交给
    /// `PluginManager.GetCompilerByFileName` 按扩展名找编译器，22 种语言共用一条流水线）：
    ///
    ///   source 直接给了 → 汇编（内联源码一律当 VML 汇编，`vml test` 走这条）
    ///   `.vml` 文件     → 汇编
    ///   其余扩展名      → 前端编译 → 汇编 → 链标准库 → 运行
    ///
    /// 之所以不在这里再列一张扩展名表：`.vml` **不在**那 22 个编译器的注册表里，
    /// 所以"派发得到编译器 = 编译、派发不到 = 汇编或认不出"这条边界由**上游注册表本身**守住，
    /// 上游将来加了新语言，这里一个字都不用改。
    ///
    /// 三条路（与上游 `Program.Actions.cs:292` 的 `ExecuteRun` 同一口径）：
    ///
    ///   source 直接给了 → 汇编（内联源码一律当 VML 汇编，`vml test` 走这条）
    ///   `.vml` 文件     → 汇编
    ///   `.vmb` 文件     → **直接装载字节码**（编译产物，既没有前端编译器也不该再过汇编器）
    ///   其余扩展名      → 前端编译 → 汇编 → 链标准库 → 运行
    /// </summary>
    /// <param name="source">内联源码（与 filePath 二选一）</param>
    /// <param name="filePath">**已解析好的**文件路径（调用方负责用 CwdContext 解析，见 VmlTool 注释）</param>
    /// <param name="timeoutSeconds">超时秒数。⚠ 交互式（<paramref name="readLine"/> 非空）要放大，见 RunProgram 注释</param>
    /// <param name="readLine">stdin 输入源；null = 无输入（读到空串）</param>
    /// <param name="markup">
    /// 返回值要不要带**给人看的颜色标记**（`«»` 中间格式）：
    ///
    /// · `false`（默认）= 老行为，纯文本，**AI 走的 `vml` 工具必须是这一支** ——
    ///   颜色标记混进工具结果里只会污染模型看到的内容。
    /// · `true` = 命令行页/编辑器用：程序自己的 stdout 里的裸 ANSI 翻成标记，
    ///   而 **stderr 整段套红**（`VML 错误`、寄存器 dump、`Permission denied` 都走 stderr，
    ///   它们本来就该一眼看出来是"出事了"，而不是跟正常输出混在一起）。
    ///
    /// 两个流的**先后顺序与老行为完全一致**（stdout 在前、诊断在后），
    /// 只在这一处做分色 —— 不拆成两个返回值，免得每一层调用都要多带一个 out。
    /// </param>
    /// <summary>
    /// 复位「上一次运行」留下的状态。**每次 <see cref="Run"/> 进来第一件事就是它**。
    ///
    /// <para>
    /// 为什么非要有：这几个字段是**静态**的（各端共用一份 <see cref="MauiVml"/>），
    /// 而调用方拿它们判断「这一轮到底跑到哪一步了」。编译失败那条路**早退**，
    /// 根本走不到 <see cref="RunProgram"/>（`LastRunStreamed` 在那里才被赋值的），
    /// 于是页面读到的就是**上一轮**的值 —— 上一轮若跑过流式，它就是 `true`，
    /// 命令行页据此认定「正文已经交出去了」而把这一轮的返回文本丢掉，
    /// 结果**编译错误一个字都不显示**（真机实测的「点了没反应」）。
    /// </para>
    /// <para>
    /// 同一类坑本仓记过多次（「上一轮量到的值用之前先问它还算不算数」）：
    /// 跨轮次复用的静态状态，**要么每轮复位、要么别拿它当本轮判据**。
    /// </para>
    /// </summary>
    private static void ResetRunState()
    {
        LastRunStreamed = false;
        LastOutputWasGrid = false;
        LastDiagnostics = "";
        LastCursor = default;
    }

    public static string Run(string? source, string? filePath, int timeoutSeconds, Func<string>? readLine = null,
        CancellationToken ct = default, bool markup = false, Func<char>? readKey = null)
    {
        ResetRunState();
        // 给了内联源码 → 一律当 VML 汇编
        if (!string.IsNullOrWhiteSpace(source))
            return RunAssembly(source!, timeoutSeconds, readLine, ct, markup);

        if (string.IsNullOrWhiteSpace(filePath))
            return "⚠️ 需要 `source`（VML 源码）或 `file_path`（文件路径）二者之一。";

        if (!File.Exists(filePath))
            return $"⚠️ 找不到文件：{filePath}";

        var ext = Path.GetExtension(filePath);

        if (ext.Equals(".vml", StringComparison.OrdinalIgnoreCase))
        {
            try { return RunAssembly(File.ReadAllText(filePath), timeoutSeconds, readLine, ct, markup); }
            catch (Exception ex) { return $"⚠️ 读文件失败：{ex.Message}"; }
        }

        // `.vmb` = VML 字节码。走装载而不是汇编/编译 —— 这正是「编译」与「运行」两个动作
        // 在文件页上分开之后要有的那半条路（编译产物应当能被直接跑起来）。
        if (ext.Equals(".vmb", StringComparison.OrdinalIgnoreCase))
        {
            try { return RunProgram(VmlProgram.LoadFromVmbFile(filePath), timeoutSeconds, readLine, ct, markup, readKey); }
            catch (Exception ex) { return $"⚠️ VMB 装载失败：{ex.Message}"; }
        }

        return CompileAndRun(filePath, timeoutSeconds, readLine, ct, markup);
    }

    /// <summary>
    /// VML 前端认识的扩展名（含点号、小写）—— **直接问上游注册表**（每个编译器的
    /// <c>SupportedExtensions</c>），不另立一张表：上游加一门语言，文件页的颜色分类与
    /// 「VML 编译」入口自动跟上（"同一规则两处实现"是本仓库头号坑）。
    /// </summary>
    public static IReadOnlySet<string> CompilableExtensions => _compilableExts ??= CollectExtensions();

    private static HashSet<string>? _compilableExts;

    private static HashSet<string> CollectExtensions()
    {
        var set = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        try
        {
            var pm = new PluginManager { Quiet = true };
            VmlFrontendCompilers.RegisterAll(pm);
            foreach (var compiler in pm.GetAllFrontendCompilers())
                foreach (var ext in compiler.SupportedExtensions.Split(',', StringSplitOptions.RemoveEmptyEntries))
                    if (ext.Trim().Length > 0) set.Add(ext.Trim().ToLowerInvariant());
        }
        catch (Exception ex) when (ex is not VmlCompilerListDriftException)
        {
            // 注册失败不该把文件页带崩：退化成"一门语言都不认"，只是少一个「VML 编译」入口
            //
            // ⚠ 但**清单漂移不在此列**（所以上面那条 when 把 DriftException 排除掉）：
            //    那是我们自己代码里的不一致，吞掉就成了「护栏看着在、其实不拦」——
            //    症状还会退化成「文件页上某个语言莫名不能用」，正是这份清单要防的那件事。
            //    注册失败是环境问题，可以退化；清单不一致是 bug，必须炸出来。
        }
        return set;
    }

    /// <summary>这个文件 VML 前端能不能编译（判据与 <see cref="Run"/> 的派发同源）。</summary>
    public static bool CanCompile(string fileName)
    {
        var ext = Path.GetExtension(fileName);
        return ext.Length > 0 && CompilableExtensions.Contains(ext.ToLowerInvariant());
    }

    /// <summary>
    /// VML 编译链上的**下一级产物路径**：`main.c` → `main.vml`，`main.vml` → `main.vmb`。
    /// 同目录、同名，只换扩展名（与上游 CLI 的默认产物同规矩，见 <see cref="CompileToVml"/>）。
    ///
    /// 这条命名规则**只有这一份实现**：文件页的菜单要先知道产物叫什么才谈得上问"要不要覆盖"，
    /// 命令行页执行编译时又要按同一个名字写 —— 两处各推一遍就是"必须手工同步的平行表"。
    /// 相对路径与绝对路径都能传（`Path.ChangeExtension` 会把目录部分原样带过去）。
    /// </summary>
    public static string NextArtifact(string filePath)
        => Path.ChangeExtension(filePath,
            Path.GetExtension(filePath).Equals(".vml", StringComparison.OrdinalIgnoreCase) ? ".vmb" : ".vml");

    /// <summary>
    /// 汇编并运行一段 VML 源码，返回它写到控制台的东西。
    /// ⚠ 同步阻塞，调用方要自己放后台线程。
    /// </summary>
    public static string RunAssembly(string source, int timeoutSeconds = 10, Func<string>? readLine = null,
        CancellationToken ct = default, bool markup = false, Func<char>? readKey = null)
        => RunProgram(new VmlAssembler().Assemble(source), timeoutSeconds, readLine, ct, markup, readKey);

    /// <summary>
    /// **编译一个高级语言源文件并运行它**（按扩展名自动选编译器，22 种语言）。
    ///
    /// 编译那半截在 <see cref="BuildProgram"/>（只有一份实现，<see cref="CompileToVml"/> 共用），
    /// 这里只负责把它跑起来。
    ///
    /// ⚠ 同步阻塞（前端编译本身就吃 CPU），调用方要自己放后台线程。
    /// </summary>
    public static string CompileAndRun(string filePath, int timeoutSeconds = 30, Func<string>? readLine = null,
        CancellationToken ct = default, bool markup = false, Func<char>? readKey = null)
    {
        // `ct` 对两段都有效：编译段是"带超时地等一个不可取消的编译"（见 BuildProgram 的看门狗），
        // 运行段是"主循环每条指令查一次"。用户按「强制停止」时两段都能停下来。
        var (prog, _, error, _) = BuildProgram(filePath, ct);
        return prog == null ? error! : RunProgram(prog, timeoutSeconds, readLine, ct, markup, readKey);
    }

    /// <summary>`vml make <工程.vmk>` 的结果。</summary>
    public sealed record MakeOutcome(string? OutRel, string? SizeNote, List<string> Notes, string? Error);

    /// <summary>
    /// **按工程文件（`.vmk`）编译** —— `vml make proj.vmk` 走这条。
    ///
    /// <para>
    /// 工程文件本身是纯逻辑（`VmlProject`，在 `UI/Shared/` —— 桌面自测、`vmlcli`、
    /// 手机端**编同一份源码**），这里只负责：拿它给出的入口 / 头文件路径 / 宏，
    /// 走**同一条** <see cref="BuildProgram"/>，再把产物落到沙箱里。
    /// </para>
    ///
    /// <para>
    /// ⚠ **不在这里自己拼编译链**：`MauiVml` 是全端唯一的编译入口，
    /// 多一条"顺便编译"的路 = 两处实现迟早漂（见本文件顶部那段说明）。
    /// 所以这里只给 <see cref="BuildProgram"/> 多传两个参数（`extraIncludes`/`extraDefines`）。
    /// </para>
    ///
    /// <para>
    /// ⚠ **未实现的产物格式必须明确拒绝**（与桌面 `vmlcli` 同一口径）——
    /// 用户写了 `Format="hex"` 却拿到一个 `.vml`，会以为"转译完了"，
    /// 而手上是个**根本烧不进芯片**的文件。
    /// </para>
    /// </summary>
    /// <param name="vmkAbsPath">**已解析好的**工程文件绝对路径（沙箱内）</param>
    public static MakeOutcome MakeProject(string vmkAbsPath, CancellationToken ct = default)
    {
        VmlProject proj;
        try
        {
            proj = VmlProject.Load(vmkAbsPath);
        }
        catch (VmlProjectException ex)
        {
            return new(null, null, [], $"⚠️ {ex.Message}");
        }

        var entry = proj.EntryPath;
        if (!File.Exists(entry))
            return new(null, null, [],
                $"⚠️ 找不到入口源文件：{SandboxFsService.Abbreviate(entry)}"
                + $"（工程文件里写的是 <Entry>{proj.Entry}</Entry>）");

        // ⚠ 认得出名字但**还没实现**的格式 —— 拒绝，**绝不静默退回 .vml**（见方法注释）
        if (proj.OutputFormat is not (VmlProject.FormatVml or VmlProject.FormatVmb))
            return new(null, null, [],
                $"⚠️ 产物格式 `{proj.OutputFormat}` 还没做（预留的名字之一）。"
                + $"现在能出的是：{string.Join("、", VmlProject.ImplementedFormats)}。");

        var outAbs = proj.OutputPath;
        var rel = SandboxFsService.ToRelative(outAbs);
        if (rel is null)
            return new(null, null, [], $"⚠️ 产物路径在工作区外，写不了：{outAbs}");

        // ── 多文件：附加编译单元各编成**目标文件**，再链进入口 ──
        //
        // 见 `BuildProgram(asObject:)` 的注释：不这么做的话标准库会被链 N 遍，
        // 而且**结果是错的**（同名函数被两份实现各定义一次）。
        // 中间产物放 `<.vmk 目录>/.vmk-obj/`（每次构建重生成，不脏源码树）。
        var extraLibs = new List<string>();
        if (proj.SourcePaths.Count > 0)
        {
            var objDir = proj.ObjDirPath;
            try { Directory.CreateDirectory(objDir); }
            catch (Exception ex) { return new(null, null, [], $"⚠️ 建不出中间产物目录：{ex.Message}"); }

            foreach (var src in proj.SourcePaths)
            {
                if (!File.Exists(src))
                    return new(null, null, [], $"⚠️ <Sources> 里的文件不存在：{SandboxFsService.Abbreviate(src)}");

                var (objProg, _, objErr, _) = BuildProgram(src, ct,
                    proj.IncludePaths, proj.DefineArgs, asObject: true);
                if (objProg is null)
                    return new(null, null, [], $"⚠️ 编译 `{Path.GetFileName(src)}` 失败：{objErr}");

                var objPath = Path.Combine(objDir, Path.GetFileNameWithoutExtension(src) + ".vml");
                try
                {
                    File.WriteAllText(objPath, objProg.ToString(), new System.Text.UTF8Encoding(false));
                }
                catch (Exception ex) { return new(null, null, [], $"⚠️ 写目标文件失败：{ex.Message}"); }

                extraLibs.Add(objPath);
            }
        }

        // 入口：（目标文件就位之后）带上它们一起编 —— `--lib` 那条路的等价物
        var (prog, _, error, _) = BuildProgram(entry, ct, proj.IncludePaths, proj.DefineArgs,
            asObject: false, extraLibs: extraLibs);
        if (prog is null) return new(null, null, [], error);

        // 库型别：不删"没人调用"的函数（库本来就是给别人调的）
        if (proj.OutputKind == VmlProject.KindLib) prog.IsLibrary = true;

        try
        {
            if (proj.OutputFormat == VmlProject.FormatVmb)
            {
                var bytes = prog.ToVmbBytes();
                SandboxFsService.WriteBytesAtomic(rel, bytes);
                return new(rel, $"{bytes.Length:#,0} 字节", [], null);
            }

            // ⚠ `ToString()` 会**就地**做死代码消除 —— `make` 只出产物、不跑，正好不冲突。
            //   原子写 + UTF-8 **不带 BOM**：产物几十万字符，写一半崩掉会留下半截文件；
            //   带 BOM 则会让汇编器认不出首行的 `.entry`（与文件页「VML 编译」同一口径）。
            var text = prog.ToString();
            SandboxFsService.WriteTextAtomic(rel, text, new System.Text.UTF8Encoding(false), crlf: false);
            return new(rel, $"{text.Length:#,0} 字符", [], null);
        }
        catch (Exception ex)
        {
            return new(null, null, [], $"⚠️ 写入失败：{ex.Message}");
        }
    }

    /// <summary>
    /// **把源文件编译成自包含的 VML 汇编文本**（文件页的「VML 编译」用它，产物落成 <c>main.vml</c>）。
    ///
    /// 产物形态照抄上游 CLI 的**默认输出**（`Program.Compile.cs` 的 `Path.ChangeExtension(files[0], ".vml")`
    /// —— `vmltool main.c` 写出来的就是同目录的 `main.vml`）：<c>VmlProgram.ToString()</c>，
    /// 也就是**链接之后**的整份程序。
    ///
    /// **"自包含"是这里唯一要紧的事**：标准库已经链进来、`.include` 已经展开、标签已经解析完，
    /// 产物里不再有任何外部路径依赖 ⇒ 存成一个文件就能独立运行。存前端那一版（带 `.include "Lib/..."`）
    /// 做不到 —— 那份文本只有在 VML 根目录旁边才汇编得出来，拷到别处就散架。
    ///
    /// ⚠ `ToString()` 会**就地**做死代码消除，调用之后这个 prog 不能再拿去跑（所以先编文本、后跑
    /// 是两条独立的路，各自 BuildProgram 一次）。同步阻塞，调用方自己放后台线程。
    /// </summary>
    public static (string? Text, string? Error) CompileToVml(string filePath, CancellationToken ct = default)
    {
        var (prog, _, error, _) = BuildProgram(filePath, ct);
        return prog == null ? (null, error) : (prog.ToString(), null);
    }

    /// <summary>
    /// **编辑器那条路**：编一次，**同时**给出①产物文本（交给命令行页去跑）与②结构化诊断（画气泡）。
    ///
    /// 存在的理由是一个冲突：气泡要求「报错显示在编辑器里」，而运行交给命令行页。
    /// 若编辑器编一遍、命令行页再编一遍，手机上就是**两个一分钟**（前端编译实测一分多钟），
    /// 不可接受。所以这里把**已链接的自包含汇编文本**一并带过去，命令行页只付一次
    /// **汇编**（秒级）—— 省掉的正是前端编译那一段。
    ///
    /// 与 <see cref="CompileToVml"/> 共用 <see cref="BuildProgram"/>，区别只是把诊断一起带出来
    /// （所以不存在"能跑的编不过、能存的不带诊断"这种分家）。
    ///
    /// ⚠ 与 <c>CompileToVml</c> 一样：<c>ToString()</c> 会**就地**做死代码消除，
    /// 返回之后这个 prog 不能再拿去跑 —— 这里只要文本，符合。
    /// </summary>
    public static (string? Text, List<Diagnostic> Diags, string? Error) CompileForEditor(
        string filePath, CancellationToken ct = default)
    {
        var (prog, _, error, diags) = BuildProgram(filePath, ct);
        return prog == null ? (null, diags, error) : (prog.ToString(), diags, null);
    }

    /// <summary>
    /// **把一个 <c>.vml</c> 汇编成 VMB 字节码**（<c>.vmb</c>）—— 链上的第二级产物。
    ///
    /// `.vml` 是文本汇编，每次运行都要现场汇编一遍；`.vmb` 是它的二进制形态，装载即执行，
    /// 同一个程序跑起来更快（用户提的：vml 可以编译成 vmb，vmb 运行更快）。
    ///
    /// 汇编走的是与 <see cref="RunAssembly"/> **同一份实现**（上游 <c>VmlAssembler.AssembleToVmb</c>），
    /// 区别只是产物写盘、不运行 ⇒ **能跑的 .vml 必然编得出 .vmb**，不存在两套口径。
    /// ⚠ 同步阻塞，调用方自己放后台线程。
    /// </summary>
    public static (byte[]? Bytes, string? Error) AssembleVmlToVmb(string filePath)
    {
        if (!File.Exists(filePath)) return (null, $"⚠️ 找不到文件：{filePath}");
        try
        {
            // 与上游 `AssembleToVmb(source)` 是同一件事（Assemble → ToVmbBytes），
            // 中间多一步 `NormalizeLongConstants`（理由见那个方法）。
            var prog = new VmlAssembler().Assemble(File.ReadAllText(filePath));
            NormalizeLongConstants(prog);
            return (prog.ToVmbBytes(), null);
        }
        catch (Exception ex)
        {
            return (null, $"⚠️ 汇编失败：{ex.Message}");
        }
    }

    /// <summary>
    /// 把数据段里的 <c>long</c> 常量**按位**换成 <c>double</c> —— 绕开上游 VMB 编码器的一个缺口。
    ///
    /// `VmlProgram.ToVmbBytes()` 的数据段只认 string / int / float / double / IList / DataString，
    /// 碰到 `long` 直接抛 `NotSupportedException: Unsupported data type: System.Int64`。
    /// 而 64 位常量在 C 标准库里**到处都是** —— 实测一个 hello 级别的 C 程序，数据段 160 项里有
    /// 7 项是 Int64（全部来自 `conv.vml` / `convert64.vml` 的浮点常量），也就是说
    /// 不处理的话「.vml 编 .vmb」对**几乎所有 C 程序**都会失败，这个菜单项等于摆设。
    ///
    /// **为什么按位换成 double 是等价而不是"糊弄过去"**（三处全是字节搬运，没有一处按数值语义解释它）：
    ///   · 编码侧 `BinaryWriter.Write(double)` 与 `Write(long)` 都是 8 字节小端，位模式相同；
    ///   · 解码侧 `ReadDataSection` 认 tag 0x10 就 `ReadDouble()`，8 个字节原样读回；
    ///   · 装载侧运行时对 double 是 `BitConverter.GetBytes(doubleValue)` 逐字节写进 VM 内存
    ///     （`VMLRuntime.cs:479`）—— 写进去的 8 个字节与原来 `GetBytes(long)` 完全相同。
    /// 实测：改前 `.vmb` 编不出来，改后编出的 `.vmb` 装载运行的输出与直接运行**逐字符相同**
    /// （见 `.scratch/vmlround`）。
    ///
    /// **不去改 `third_party/vml`**：那是另一份仓库（`sync.sh` 会 rsync 覆盖），缺口在上游，
    /// 该在上游补；宿主侧这份只是绕行，且改的是**自己的**程序对象、不碰公共状态。
    /// </summary>
    private static void NormalizeLongConstants(VmlProgram prog)
    {
        if (prog.DataSection.Count == 0) return;
        List<string>? longs = null;
        foreach (var key in prog.DataSection.Keys)
            if (prog.DataSection[key] is long) (longs ??= []).Add(key);
        if (longs == null) return;

        foreach (var key in longs)
            prog.DataSection[key] = BitConverter.Int64BitsToDouble((long)prog.DataSection[key]);
    }

    /// <summary>
    /// 走完「前端编译 → 汇编 → 链接 → 导出」四步，返回**可直接装载运行的 VmlProgram**。
    ///
    /// 这是「编译一个高级语言源文件」的**唯一实现**：<see cref="CompileAndRun"/>（编完就跑）
    /// 与 <see cref="CompileToVml"/>（编完存成 .vml）都从这里出发 —— 两条路各写一份的话，
    /// 迟早出现"能跑的编不过、能存的跑不了"（本仓库头号坑就是"同一规则两处实现"）。
    ///
    /// 失败时 <c>Prog</c> 为 null、<c>Error</c> 是可读原因（照旧带 ⚠️ 前缀，与其它失败文案一致）。
    /// </summary>
    /// <summary>
    /// 前端编译的**看门狗**（秒）。超了就报错收场，不再傻等。
    ///
    /// ⚠ 值必须**明显大于合法编译的耗时**，否则会把正常程序误杀：手机上一份 C 程序
    /// （前端 + 汇编 + 链接 3.7 万条指令）实测要**一分多钟**，所以给到 180 秒 ——
    /// 它防的是"编译器自己卡死"（源码里有让前端死循环的写法），不是"慢"。
    /// </summary>
    public const int CompileTimeoutSeconds = 180;

    /// <summary>
    /// 失败出口的统一构造 —— 把「给用户看的原因」**同时**解析成结构化诊断。
    ///
    /// 两件事都做而不是二选一：命令行页要的是那句话，编辑器气泡要的是行列。
    /// 分两处各解析一次就是「同一规则两处实现」，迟早一边改了另一边没改。
    /// 解析不出位置时 <see cref="VmlDiagnostics.Parse"/> 会给一条无锚诊断，
    /// 气泡照常显示内容、只是不画指向某一行的箭头。
    ///
    /// <paramref name="filePath"/> = **正在编的那个文件**，一路传给
    /// <see cref="VmlDiagnostics.Parse"/>：报错文本里混着 `#include` 进来的头文件的错，
    /// 只有知道"哪个文件是当前文件"才判得出「这条不属于它」——判不出来的话头文件的行号
    /// 会被硬贴到用户正在看的文件上（真机上就是"第 112 行那句注释被标红"）。
    /// </summary>
    private static (VmlProgram? Prog, string Lang, string? Error, List<Diagnostic> Diags) Fail(
        string lang, string message, string? filePath)
        => (null, lang, message, VmlDiagnostics.Parse(message, filePath));

    /// <param name="extraIncludes">
    /// 工程文件（`.vmk`）带来的**追加**头文件搜索路径（对应 `vml make`）。
    /// **排在** VML 内置 `Lib` 之前 —— 用户的头该能覆盖库里的同名头，与 gcc 的 `-I` 一致。
    /// </param>
    /// <param name="extraDefines">
    /// 工程文件带来的宏（`名=值`）。**不是**走 `VMLTOOL_DEFINE` 环境变量 ——
    /// 多数前端的 `PredefinedMacros` 是 `static readonly`、**只读一次**，
    /// 环境变量在"第一次用到该编译器"时就被快照，同一个进程里换宏不生效
    /// （桌面 `vmlcli` 同处注释记了完整的理由）。走 `SetConfig("defines", …)` 才是对的。
    /// </param>
    /// <param name="asObject">
    /// 把这一份编成**目标文件**（多文件程序用）：**不挂任何库**、并标成库。
    ///
    /// <para>
    /// ⚠ 这是多文件能不能用的**关键**。每个附加编译单元若按普通程序编，会把**整份标准库**
    /// 一起编进去（实测 49030 条指令）—— 两个文件直接链就是 **98080 条 = 正好两倍**，
    /// 而且**结果是错的**（同名函数被两份实现各定义一次，链接器的重映射会指到错的那份：
    /// 实测 `helper(20)` 该得 43、实得 102944）。按目标文件编只有 **20~28 条**
    /// （用户代码本身），链进入口后总计 49070 条。
    /// </para>
    /// </param>
    private static (VmlProgram? Prog, string Lang, string? Error, List<Diagnostic> Diags) BuildProgram(
        string filePath, CancellationToken ct,
        IReadOnlyList<string>? extraIncludes = null,
        IReadOnlyList<string>? extraDefines = null,
        bool asObject = false,
        IReadOnlyList<string>? extraLibs = null)
    {
        if (!File.Exists(filePath)) return Fail("", $"⚠️ 找不到文件：{filePath}", filePath);

        var libRoot = EnsureLibExtracted();
        if (libRoot == null)
            return Fail("", "⚠️ VML 标准库（Lib/）解压失败 —— 没有它就编不了高级语言（链接阶段会找不到 stdlib）。", filePath);

        // 静态注册 22 个前端编译器，**绕开 PluginManager 的 Assembly.LoadFrom 反射路径**
        // （那条路在 MAUI 的裁剪/AOT 下不可靠，上游自己也在 StaticLink 模式里绕开了它）。
        //
        // 这份注册是**本地副本**（`VmlFrontendCompilers`，抄自上游 StaticLinkInitializer 的
        // 前端那半截）：上游那版连 18 个后端翻译器一起注册，而翻译器来自 VMLTranslators
        // （+ VMLToHex）—— 裸机/单片机的输出后端，手机端用不到，正是从包里去掉了那 ~460 KB。
        // 后端一个都不注册 ⇒ `PluginManager.BackendTranslatorCount` 恒为 0，而我们这条链
        // （编译 → 汇编 → 链接 → 运行）从来不查后端翻译器，上游 CLI 的「导出」动作才是它的用户。
        var pm = new PluginManager();
        VmlFrontendCompilers.RegisterAll(pm);

        // 扩展名派发用上游现成的 —— 自己遍历 SupportedExtensions 就是第二份实现
        var compiler = pm.GetCompilerByFileName(Path.GetFileName(filePath));
        if (compiler is not IFrontendCompilerEx ex)
            return Fail("", $"⚠️ 认不出这个扩展名（{Path.GetExtension(filePath)}），没有对应的前端编译器。", filePath);

        var lang = compiler.Name.ToLowerInvariant();

        // **`VML_HOME` 必须指向解压出来的 VML 根**（手机上就是 `Global.Home/vml`，固定值）。
        //
        // 这不是"锦上添花"，是**能不能链上标准库的唯一开关**：前端产物里的
        // `.linked "Lib/c/builtin.vml"` 是**相对路径**，`LibraryLinker.ResolveLinkedFile`
        // 解析它时依次试「父库目录 → `VML_HOME` → CWD → 搜索路径」——手机上 CWD 是
        // workspace 根、不是 VML 根，`$VML_HOME` 不设就**整条链都断**，症状是运行期
        // `未找到标签: shared_puts`（标准库一个都没进来）。
        //
        // 上游 CLI 自己不设这个变量，它靠"从 VML 根目录运行"让 CWD 命中；我们没有那个
        // 奢侈（CWD 属于用户的工作区），所以显式设 —— 上游在 4 处读它
        // （`LibraryLinker.ResolveLinkedFile` / `FindSharedDir` / `GetLibrarySearchPaths` /
        // `CompilerHelper.ResolveImportLibrary`），设它正是上游预期的用法。
        Environment.SetEnvironmentVariable("VML_HOME", libRoot);

        // ① 前端编译（预处理 + include + 标准库挂载）
        //
        // 两条路径的口径**照抄上游 CLI**（`Program.Compile.cs:78-85` + `VmlToolConfig`），
        // 不自己发挥 —— 这两处各踩过一个坑，都是"看起来更周到、实际更糟"：
        //
        // `IncludePaths`：上游只加 `<VML_HOME>/Lib` 一项。自己再补 `Lib/c`、`Lib/shared`
        //   并不需要（`ResolveIncludePath` 内部本来就有这两处兜底），补多了只是多几层搜索。
        // `LibraryPaths`：**必须给"解析好的库文件"，绝不能给目录**。给目录会让
        //   `ConvertLibraryPathsToIncludes` 把该目录下**每一个** `.vml` 都挂上去，
        //   再让 `LinkLibraries` 全量链接 —— 实测同样一个 hello.c，给目录是 93423 条指令、
        //   给库文件是 36261 条（上游 CLI 35329）。上游为此专门留了注释：
        //   `ApplyPaths`: 「仅添加已解析的库文件，不添加目录路径（避免全库链接）」。
        //
        // 库清单不在这里硬编码（那是本仓库反复踩的"平行表"），直接问上游的配置解析器：
        // `ResolveLibs` 读 `vmltool.config.xml` 的 `DefaultLibs`（= `builtins.vml`，**所有语言**
        // 都有）+ 该语言的 `<Language Libs="...">`（C 是 `crt.vml`）。这份 XML 随库一起解压，
        // 与 `Lib/` 同源，改了配置不用改我们的代码。
        // （读 XML 那份是 `VmlLibConfig`，上游 `VmlToolConfig` 的精简本地副本 —— 原类 340 行里
        //   绝大部分是 CLI 的东西，且依赖一堆 VMLTool Exe 侧的类型，手机端只用得到这两件事。）
        var libConfig = VmlLibConfig.Load(Path.Combine(libRoot, "vmltool.config.xml"));

        // 工程文件给的 `-I` **排在**内置那条之前（用户的头可以覆盖库里的同名头）
        var includePaths = new List<string>();
        if (extraIncludes is not null)
            foreach (var d in extraIncludes)
                if (Directory.Exists(d)) includePaths.Add(d);
        var builtinLib = Path.Combine(libRoot, "Lib");
        if (Directory.Exists(builtinLib)) includePaths.Add(builtinLib);

        // ⚠ **编目标文件时库清单必须是空的** —— 把库挂上去的话，即使 `autoLinkStdLib: false`
        //   也仍然会把库内联进来（实测 39916 条指令 vs 20 条），而且链出来的程序**结果会错**。
        var libraryPaths = asObject ? new List<string>() : (libConfig?.ResolveLibs(lang, libRoot) ?? []);

        // 多文件：把别的编译单元的目标文件并进库清单（链接器会做数据标签重映射，
        // 所以两个单元的同名 `static` 互不干扰 —— 这是整条路能成立的前提）。
        if (!asObject && extraLibs is not null)
            foreach (var l in extraLibs)
                if (!libraryPaths.Contains(l)) libraryPaths.Add(l);

        // `LinkLibraries` 第一行就是 `if (libraryPaths.Count == 0) return mainProgram;` ——
        // 空清单等于**静默不链接**。所以库清单为空必须当失败处理，不能让用户拿到一个
        // "编译成功、一跑就找不到函数"的程序。
        // （目标文件本来就不该有库清单，见上面那条 —— 别把它判成错误。）
        if (libraryPaths.Count == 0 && !asObject)
            return Fail(lang, "⚠️ 标准库清单为空 —— 多半是 `vmltool.config.xml` 没跟着解压出来（或解压目录不对）。"
                 + "没有它，`LinkLibraries` 会直接跳过整个链接阶段。", filePath);

        // 前端编译同样是个静默段（手机上**一分钟起步**）—— 与解压那条提示同一个道理
        OnProgress?.Invoke($"⏳ 正在编译 {Path.GetFileName(filePath)}（前端编译 + 链接标准库，手机上要一两分钟）…");

        // **看门狗**：前端编译是同步的、且**没有取消入口**（`IFrontendCompiler` 上没有任何 token），
        // 所以只能把它丢到独立线程上跑，主线程**带超时地等**。
        //
        // 为什么非要有：某些源码会让编译器自己陷进去（死循环/病态输入），而这条链上
        // **从前没有任何出口** —— 界面卡在"正在编译…"永远不出来，用户连「强制停止」都按不动
        // （那个 token 只作用于 VM 的运行阶段，编译根本没走到）。
        //
        // ⚠ 说清楚代价：超时之后**那个编译线程还在跑**（.NET 没法中止线程），会一直烧一个核，
        // 直到它自己结束或 App 退出。所以这只是"把控制权还给用户"，不是"杀掉编译" ——
        // 在手机上没有 fork/exec 可用，这是唯一做得到的形态。
        // 工程文件给的宏 —— 判据与桌面 `vmlcli` 逐字相同（**不是**环境变量，理由见参数注释）
        if (extraDefines is { Count: > 0 } && compiler is CompilerBase.CompilerBase cb)
            cb.SetConfig("defines", extraDefines.ToList());

        var compile = Task.Run(() => ex.CompileFileWithIncludes(filePath, includePaths, libraryPaths,
            autoLinkStdLib: !asObject, useSharedLibrary: true), ct);

        string vmlText;
        // **编译期的 stderr 也要接住** —— 此前只接了运行期（`RunProgram` 那段），
        // 于是前端在编译阶段写的诊断（未使用符号警告之类）在手机上**全部落进虚空**：
        // MAUI 没有可见控制台，`Console.Error` 就是一条看不见的流。
        // 症状不是"报错"，是"功能明明做了、用户永远看不到"。
        //
        // 与运行期那段同一个闸门、同一套理由：`Console.SetError` 是**进程级**的，
        // 收进来的内容**并进结果**而不是丢掉（只多不少）。代价是这把锁要持有一两分钟
        // （编译本身就那么久），但 VML 工具是 Exclusive、ShellPage 另有 `_busy` 闸门，
        // 正常不会与运行期的捕获并发。
        string compileDiag = "";
        try
        {
            lock (ConsoleRedirectGate)
            {
                var prevErr = Console.Error;
                var errSink = new StringWriter();
                try
                {
                    Console.SetError(errSink);
                    if (!compile.Wait(TimeSpan.FromSeconds(CompileTimeoutSeconds), ct))
                    {
                        // 早退之后这个 Task 没人 await：挂个空的续体把异常吃掉，
                        // 否则它最终抛出来会变成"未观察的任务异常"（只在日志里，看不出是谁）。
                        _ = compile.ContinueWith(static t => _ = t.Exception, TaskScheduler.Default);
                        return Fail(lang, $"⚠️ 编译超时（{CompileTimeoutSeconds} 秒）—— 多半是源码里有让前端编译器"
                            + "卡住的写法。编译线程还在后台跑，建议改完源码再试；实在不行退出 App 重来。", filePath);
                    }
                    vmlText = compile.Result;
                }
                finally
                {
                    Console.SetError(prevErr);
                    compileDiag = errSink.ToString();
                }
            }
        }
        catch (OperationCanceledException)
        {
            _ = compile.ContinueWith(static t => _ = t.Exception, TaskScheduler.Default);
            return Fail(lang, "⏹ 编译已被停止。", filePath);
        }
        catch (Exception compileError)
        {
            // 编译器抛异常 = **源码有问题**，要变成用户看得见的一句话（v0.96.183）。
            // 上游前端有一条"顶层解析异常恢复"的容错路径，会把不少错误吞成一行 stderr 日志
            // 然后照常编下去；而"本编译器不支持…"这类错误是**硬失败**的（patches/0008），
            // 异常会一路穿到这里。若不接住，它落在 `Task.Run` 里就成了"未观察的任务异常"，
            // 手机上表现为"点了没反应"—— 本仓已经踩过一次同型（见 CLAUDE.md 移动端十三期 ⑨b）。
            // ⚠ 变量名不能叫 `ex`：外层作用域已有同名局部变量，CS0136（实测踩过）。
            // ⚠ 还要**剥掉 AggregateException 的壳**：`Task<T>.Result` 会把内层异常包一层，
            //   不剥的话真机上打出来是 `AggregateException_ctor_DefaultMessage (语法错误 …)`——
            //   前面那段噪音正是用户第一眼看到的东西（真机截图看出来的，构建全绿）。
            var inner = compileError is AggregateException agg ? agg.GetBaseException() : compileError;
            // **异常消息 + 编译期 stderr 一起进诊断**（按逐字段相等性去重，见 `Merge`）。
            //
            // 只取异常消息是不够的：**不抛异常的那些诊断**（预处理器打的警告 ——
            // 比如「找不到头文件 "Windows.h"」）只存在于 stderr 里，而它恰恰解释了
            // 后面那串「未声明的变量 'STD_OUTPUT_HANDLE'」是**为什么**。
            // 反过来只取 stderr 也不行：异常消息里那份位置更全。
            // 两份在链接期错误上是**逐字相同**的，所以必须去重、不能简单相加。
            return (null, lang, $"⚠️ 编译失败：{inner.Message}",
                VmlDiagnostics.Merge(VmlDiagnostics.Parse($"⚠️ 编译失败：{inner.Message}", filePath),
                                     VmlDiagnostics.Parse(compileDiag, filePath)));
        }

        // **自检：产物得像 VML 汇编。**
        // 上游那个「失败就静默原样返回」的行为会把所有编译错误伪装成汇编期的
        // 「未知指令」，手机上尤其难查。这里拦一道，把真正的原因亮出来。
        if (string.IsNullOrWhiteSpace(vmlText) || !LooksLikeVml(vmlText))
        {
            var head = vmlText ?? "(空)";
            return Fail(lang, $"⚠️ 前端编译没有产出 VML 汇编（{compiler.Name}）—— 多半是标准库/include 路径不对，"
                 + $"或源码本身有语法错误。产物前 200 字符：\n"
                 + head[..Math.Min(200, head.Length)], filePath);
        }

        // ② 汇编（.include / .macro 解析基准指向解压出来的根，使 "Lib/..." 能解析）
        var langDefines = new List<string>
        {
            $"VML_{compiler.Name.ToUpperInvariant()}",
            "VML_MODE_MCU",     // 与运行时一致：MCU 模式（见 RunProgram 的安全边界说明）
            "VML_RAM_M",
        };
        //
        // ⚠ **汇编器与链接器抛的异常必须在这里接住**（v0.96.328 起）。它们抛的都是**用户可见的
        //   诊断**，不是内部故障 —— 例如手写 `asm("MOVE @R0 R99")` 的「寄存器名越界：R99…」、
        //   未知指令、未找到标签。不接住的话它会**穿出本方法**，在手机端落在 `Task.Run` 里
        //   变成"未观察的任务异常"：界面既不报错也不动，就是**点了没反应** ——
        //   本仓已经踩过两次同型（见 CLAUDE.md 移动端十三期 ⑨b / ⑲）。
        //   `Fail` 那条出口与前端编译失败走的是同一条（用户看到的是同一句话式的错误）。
        VmlProgram prog;
        try
        {
            prog = new VmlAssembler().AssembleWithIncludes(vmlText, libRoot, langDefines);
        }
        catch (Exception asmError)
        {
            var inner = asmError is AggregateException agg ? agg.GetBaseException() : asmError;
            return Fail(lang, $"⚠️ 汇编失败：{inner.Message}"
                + $"\n（前端已产出 VML 汇编，是汇编/链接阶段报错 —— 多半是 `asm()` 里写了越界寄存器名"
                + $"或未知指令；产物前 200 字符：\n{Head(vmlText)}）", filePath);
        }

        // ③ 链接共享库 ④ 应用导出符号
        //
        // ⚠ **必须传 `libraryPaths`，不能传 `[]`** —— `LinkLibraries` 首行
        // `if (libraryPaths == null || libraryPaths.Count == 0) return mainProgram;` 是**早退**，
        // 空清单等于「链接器一个字都不干」，而它返回的 `VmlProgram` 看起来完全正常
        // （不抛异常、指令数就是主程序本身），错误一路拖到运行期才炸成
        // `未找到标签: shared_puts`。上游 CLI 传的是 `options.LibraryPaths`（同样非空）。
        //
        // 它内部会把 `Lib/c/builtin.vml` 这类相对 `.linked` 解析出来（靠上面设的 `VML_HOME`），
        // 递归链上 `builtins.vml` → `string/math/io/printf/...` 整个标准库。
        LibraryLinker.LinkLibraries(prog, libraryPaths);
        prog.ApplyExports();

        // 目标文件：标成库 ⇒ 后面 `ToString()` 的**死代码消除不会删掉"没人调用"的函数**
        //（它本来就是给别人调的 —— 入口那边才是唯一知道谁被调了的地方）。
        if (asObject) prog.IsLibrary = true;

        // ⑤ 交出去：编完就跑的走 RunProgram，编完存文件的走 prog.ToString()（见 CompileToVml）
        //
        // 把**编译期捕获到的诊断**一并带上（见上面 `compileDiag` 那段）——
        // 取 `Warning` 级别的：错误走的是抛异常那条路（`Fail`），这里是"编过了但有问题"。
        // 这正是用户要的「未使用符号出警告，可以给 IDE 报警示提示用」在手机上的落点：
        // `CompileForEditor` 拿这份列表画气泡，`VmlDiagnostics` 早就把 GCC 风格认全了。
        //
        // ⚠ 同样要传 `filePath`（与 `Fail` 那条出口同一个理由）：编译期的 stderr 里
        //   头文件产生的警告不在少数，不判文件就会把它们贴到用户文件的行号上。
        var compileWarnings = VmlDiagnostics.Parse(compileDiag, filePath)
            .Where(d => d.Severity == Severity.Warning)
            .ToList();
        return (prog, lang, null, compileWarnings);
    }

    /// <summary>产物的前 200 字符（报错时给用户看个开头，够判断「编到哪儿了」）。</summary>
    private static string Head(string? text)
    {
        var s = text ?? "";
        return s.Length <= 200 ? s : s[..200];
    }

    /// <summary>
    /// 产物像不像 VML 汇编 —— 用来识破「前端编译其实没干活」的产物。
    ///
    /// 判据分两半，**缺一半就会漏**：
    /// ① 正判据：出现 `.text` / `.entry` / `.data` 这类**行首**伪指令（源码里不会有）。
    /// ② 反判据：**残留的 `#include`**。这条是加正判据时没想到的坑 —— 预处理没解析掉的
    ///    `#include` 会**和真汇编混在一起**：产物里既有 `.text`（正判据命中、自检放行），
    ///    又有未展开的 `#include`，直到汇编器报「未知指令: #INCLUDE」才暴露。
    ///    所以「像汇编」不等于「是完整汇编」，还得确认预处理器把该展开的都展开了。
    /// </summary>
    private static bool LooksLikeVml(string text)
    {
        bool hasAsmMarkers =
            text.Contains("\n.entry", StringComparison.Ordinal)
            || text.Contains("\n.text", StringComparison.Ordinal)
            || text.Contains("\n.data", StringComparison.Ordinal)
            || text.StartsWith(".entry", StringComparison.Ordinal)
            || text.StartsWith(".text", StringComparison.Ordinal);

        return hasAsmMarkers && !HasRawInclude(text);
    }

    /// <summary>
    /// 产物里有没有**真的没被展开**的 `#include`。
    ///
    /// ⚠ 判据是「**行首** + **跳过注释行**」，不是 `text.Contains("#include")`：
    ///   产物里的 `; N: &lt;源码行原文&gt;` 注释会**原样引用用户写的源码行** ——
    ///   而 C 程序第一行往往就是 `#include &lt;stdio.h&gt;`（`vmlcli` 里那份同款判据
    ///   就是被这个打成假失败的，实测 `scripts/vml-out-probe/langs/nat.c`）。
    /// </summary>
    private static bool HasRawInclude(string text)
    {
        foreach (var line in text.Split('\n'))
        {
            var s = line.TrimStart();
            if (s.StartsWith(";", StringComparison.Ordinal)) continue;   // 注释行：引用的源码不算
            if (s.StartsWith("#include", StringComparison.OrdinalIgnoreCase)) return true;
        }
        return false;
    }

    /// <summary>
    /// 装载并运行一个已汇编的程序，返回它写到控制台的东西（两条路共用这一份）。
    ///
    /// <paramref name="ct"/> = **用户按「强制停止」**（ShellPage 上运行中的返回拦截）。
    /// 运行时的主循环**每条指令都查一次**这个 token（`VMLRuntime.cs:681`），所以取消是即时的。
    /// </summary>
    /// <param name="readKey">
    /// **逐键输入源**（交互式程序要的"按一下立刻拿到"）。给了它，`ReadChar` 就走它；
    /// 没给则退回"攒一行再逐字符吐"的老路。两者**天然区分**：VM 调的是哪个读接口，
    /// 就决定了要哪种输入，不需要程序额外声明什么。
    /// </param>
    private static string RunProgram(VmlProgram prog, int timeoutSeconds, Func<string>? readLine = null,
        CancellationToken ct = default, bool markup = false, Func<char>? readKey = null)
    {
        // **每次跑之前必须重置单例 DeviceManager**：它跨运行保留状态，
        // 不重置的话第二次运行的 MMIO 地址会和第一次串掉（这是 VML 自带测试里的做法）。
        VMLRuntime.Device.DeviceManager.Instance.Reset();

        // ⚠ **`readLine` 非空时，`ReadString()` 会阻塞在 VM 线程上**，而 `TimeoutSeconds` 是
        // 从 `Run()` 起就走的**墙钟** `CancellationTokenSource` —— 也就是说**用户思考的时间
        // 也在扣超时**。等输入久一点，程序就会在提示符上被超时杀掉，而且报的是"超时"、看不出
        // 是在等人。所以**交互式运行必须把 `timeoutSeconds` 放到足够大**（宿主侧另有看门狗兜底，
        // 见 ShellPage），不能沿用非交互那个 10~30 秒的口径。
        // 流式出口**只在 markup 那条路装**（`markup:true` = 宿主主动运行、有命令行页可以边跑边画）。
        // AI 那条路要的是"最终结果"那一整段，边跑边交出去反而会打乱它的取用方式。
        LastRunStreamed = markup && OnOutputChunk != null;
        var io = new CaptureIo(readLine, markup ? OnOutputChunk : null, readKey);

        // 宿主 UI syscall（对话框 / 窗体绘图 / 输入，号段 500–599，见 UI/Shared/VmlUiProtocol.cs）。
        // 两件事缺一不可：① 把号段加进运行时白名单（否则 mcu 模式下 dispatch 顶部就先拒了，
        // 根本走不到处理器 —— 现象是"处理器注册了却永远不被调用"）；
        // ② 把处理器挂到 VM 上（运行时会**先问处理器、再走内置 switch**）。
        VmlUiCalls.EnsureReservedSyscallsAllowed();
        var uiCalls = new VmlUiCalls();
        uiCalls.Reset(); // 清上一轮的消息/定时器/场景

        // **把运行令牌交给宿主** —— 这一句就是"能不能强制终止"的分水岭。
        // 没有它：VM 的令牌检查只在**执行指令**时生效，而程序卡在宿主里等消息/等弹框答案时
        // 根本不执行指令 ⇒ 关窗口、点强制停止都叫不醒它（用户实测：「旧 BGI 程序，
        // 退出弹窗后程序还没结束」—— 那类程序结尾是 `getch()`，就停在宿主的等待上）。
        // 有了它：令牌一响，宿主的阻塞等待当场抛 `OperationCanceledException`，
        // 穿出宿主、终止整次运行。
        uiCalls.SetRunToken(ct);

        // **DOS 老程序的字符串是 CP437 字节**（框线 C4、重音 E9…），而绘图侧原先按 UTF-8
        // 硬解 ⇒ 全是 U+FFFD（用户真机看到的"一串问号"）。控制台那条路早就在用 CP437，
        // 这里把**同一张表**（只有一份，在 VM 里）接给共享层，两条路从此一致。
        WayCoder.UI.Shared.VmlHostRuntime.NonUtf8ByteDecoder ??= VMLRuntime.Device.Cp437.ToChar;

        // ⚠ **永远只用 "mcu" 模式，这是手机端的安全边界，不是默认值凑巧。**
        //
        // 切到 "os"（privilegeLevel=0）会放开 syscall 300-376：线程/互斥量、Socket/DNS、
        // mkdir/stat/readdir、以及 **Exec（syscall 320 → System.Diagnostics.Process.Start）**。
        // 在手机上这些要么被沙箱挡、要么本就不该由一段 VML 程序触发（那处 Process.Start
        // 在 iOS 上还会直接抛）。MCU 模式下它们全部返回 SYSCALL_PERMISSION_DENIED，够不着。
        //
        // 模式是**宿主侧**参数、VML 程序自己改不了 —— 所以只要这里写死，那三层就是死代码。
        // （也正因如此，没去删 VMLRuntime.Syscall.OS.cs / .FFI.cs：删了只是删死代码，
        //   却要在 sync.sh 里加一步「同步后删文件」，那是最脆的一类本地适配。）
        using var vm = new VmRuntime(2 * 1024 * 1024, MakeConfig(), [], mode: "mcu")
        {
            // **超时必须设**：VML 程序里的死循环会把 App 挂死，手机上没有 Ctrl+C 可按。
            //
            // 上限从 60 提到 600 是因为**交互式运行时等用户输入的时间也算在里面**（见下）。
            // 手机上一行输入敲几十秒很正常，60 秒会在提示符上被超时杀掉。
            // 代价：失控程序最长挂 10 分钟。真正的解法是"等输入时不计时"（暂停 CTS），
            // 那要动 VML 的超时实现，留作后续；现阶段用宿主侧的可中断入口兜底。
            TimeoutSeconds = Math.Clamp(timeoutSeconds, 1, 600),
            ConsoleIO = io,
            SystemCallHandler = uiCalls,

            // **文件沙箱根 = 当前工作目录**（手机上就是 app 的 workspace）。
            //
            // 不设的话，VML 的 `open("a.txt","w")` 会按**进程 CWD** 解析，而手机上那既不是
            // workspace、还常常是个只读目录 —— 实测报「Read-only file system」。
            // 更该防的是另一半：它不总是"写不进去"，也可能是**写到了别的地方**。
            //
            // 判据取 `CwdContext.Root` 而不是写死 workspace 路径：这样 `cd` 之后沙箱跟着走，
            // 与 shell 页、文件页用同一把尺子（本仓库的头号坑就是"同一规则两处实现"）。
            FileSystemRoot = CwdContext.Root,
        };

        // **网络：只放客户端那一半**（见 VMLRuntime 的 HostAllowedSyscalls）。
        //
        //   330 SocketCreate / 334 SocketConnect / 335 SocketSend / 336 SocketRecv
        //   337 SocketClose / 338 DnsResolve
        //
        // **故意不给**的三类，每一条都有理由：
        //   · 331 Bind / 332 Listen / 333 Accept —— 服务端。手机是终端设备，
        //     一段 VML 程序不该在它上面开监听端口（这正是用户定的边界）。
        //   · 340 MkDir / 341 Remove / 342 Rename / 343 ReadDir / 344 Stat —— 目录与文件。
        //     ⚠ 它们走的是 `VMLRuntime.Syscall.OS.cs` 那条 OS 版本实现，**不经过**
        //     上面设的 `FileSystemRoot` 沙箱 —— 放行等于把刚立起来的文件沙箱拆掉一半。
        //     要放行必须先把它们也接上沙箱，那是另一件事。
        //   · 320 Exec（`Process.Start` 任意进程）、370/371 DLOpen/DLSym（FFI）、
        //     361 SetEnv（改进程环境变量）—— 手机端最不该开的口子，维持封禁。
        foreach (var syscall in new[] { 330, 334, 335, 336, 337, 338 })
            vm.HostAllowedSyscalls.Add(syscall);

        // 回填运行时：通用宿主调用口（577–580）里 float8/long4/double4 的参数在
        // 浮点/长整数/双精度寄存器组里，而处理器只拿得到 32 位的通用整数寄存器
        //（见 VmlUiCalls.Vm 的注释）。必须在 Run() 之前接上。
        uiCalls.Vm = vm;

        vm.LoadProgram(prog);

        // **运行时的诊断输出走的是 `System.Console`，而手机上那是一个看不见的流。**
        //
        // 这是"程序明明报错了、用户却只看到没有输出"的根因：`VMLRuntime` 把
        // `内存错误(PC=…): …`、`标签错误`、`VML 错误`、`未预期的运行时崩溃` **连同 16 个寄存器的
        // dump** 全部 `Console.Error.WriteLine`（`VMLRuntime.cs:768-910`），把
        // `Permission denied: syscall N …`、`VM execution cancelled` 写 `Console.Out` ——
        // 桌面 CLI 上这些直接进终端，手机上**全部落进虚空**（MAUI 没有可见控制台）。
        //
        // 所以运行期间把这两个流临时接到内存里，跑完并进返回值：报错、被强制停止、
        // syscall 被拒，用户都能在命令行页上看见。
        //
        // 代价说清楚：`Console.SetOut/SetError` 是**进程级**的，这几十毫秒里别处写控制台的
        // 内容也会被一起收进来 —— 但它们是**并进输出**而不是被丢掉，所以只多不少。
        // 用锁串行化：VML 工具是 Exclusive，ShellPage 也有 `_busy` 闸门，正常不会并发，
        // 锁只是兜住"将来谁绕过那两道闸门"。
        // 两个流**分开接**（原来接进同一个 sink）—— 命令行页要把 stderr 显示成红的，
        // 合并之后就再也分不出哪一段是报错了（`VML 错误`/寄存器 dump 走 stderr，
        // `Permission denied`/`VM execution cancelled` 走 stdout，混在一起只能靠猜）。
        string diag;
        string diagErr;
        lock (ConsoleRedirectGate)
        {
            var prevOut = Console.Out;
            var prevErr = Console.Error;
            var outSink = new StringWriter();
            var errSink = new StringWriter();
            try
            {
                Console.SetOut(outSink);
                Console.SetError(errSink);
                vm.Run(ct);
            }
            finally
            {
                Console.SetOut(prevOut);
                Console.SetError(prevErr);
                diag = outSink.ToString();
                diagErr = errSink.ToString();
                // ⚠ **收尾必须再刷一次**：末尾那一截如果还在节流窗口里没出去（或者程序写完之后
                //   再没写过任何东西），不刷就永远看不见了。
                io.FlushAll();
            }
        }

        if (!markup)
        {
            // 老行为（AI 那条路）：两个流按 stdout→诊断 的顺序并成一段纯文本
            if (diag.Length == 0 && diagErr.Length == 0) return io.Text;
            var tail = (diag.TrimEnd() + "\n" + diagErr.TrimEnd()).TrimEnd();
            return io.Text.Length == 0 ? tail : io.Text.TrimEnd() + "\n" + tail;
        }

        // ── 全屏程序：**按网格渲染**，不是折行往下堆 ──
        //
        // `nyancat` / `sl` / `nethack` / 一切 curses 程序都是"一屏一屏画"的：`ESC[H` 回到原点
        // 重画、`ESC[2J` 清屏。这类输出**折行与回滚缓存都是错的**（折行把画面搅烂、
        // 堆叠起来就是几十屏残影）。判据在 `ScreenOutput`（纯逻辑、桌面自测钉过）。
        if (ScreenOutput.LooksFullScreen(io.Text))
        {
            LastOutputWasGrid = true;
            var rows = TermRows > 0 ? TermRows : 25;      // 0 = 未知 ⇒ 老程序通用的 80×25
            var cols = TermCols > 0 ? TermCols : 80;
            var screen = new FrameBuffer(rows, cols);
            screen.Apply(io.Text);
            LastCursor = (screen.CursorRow, screen.CursorCol, screen.CursorVisible);
            var gridTxt = AnsiMarkup.ToMarkup(string.Join("\n", screen.DumpAnsi())).TrimEnd();

            var gridErr = AnsiMarkup.ToMarkup(diag + "\n" + diagErr).TrimEnd();
            LastDiagnostics = gridErr.Trim().Length == 0 ? "" : "«red»" + gridErr + "«/»";
            return gridErr.Trim().Length == 0
                ? gridTxt
                : (gridTxt.Length > 0 ? gridTxt + "\n" : "") + LastDiagnostics;
        }

        // 给人看的：裸 ANSI 翻成标记，stderr 整段套红（内层自带的颜色会盖掉这层红，正是想要的）
        //
        // ⚠ **控制字符必须在 `ToMarkup` 之前解释**：`AnsiMarkup` 会把 `\r`/`\t`/`\b` 这类
        //   非 SGR 的控制字节**吃掉**（它只保留颜色与样式），于是：
        //     · `\r` 没了 ⇒ **进度条的五帧挤成一行**（`progress [1/5]  progress [2/5] …`）
        //     · `\t` 没了 ⇒ **表格的列全歪**
        //   放到 `ShellPage.Append` 里做是**来不及的** —— 那边拿到的已经是标记文本，
        //   控制字符早在这一步就没了（实测：探针程序在命令行页上就是那副样子）。
        LastOutputWasGrid = false;       // 非全屏：是文本，照常折行
        var outTxt = AnsiMarkup.ToMarkup(ShellControls.Apply(io.Text)).TrimEnd();
        var midTxt = AnsiMarkup.ToMarkup(ShellControls.Apply(diag)).TrimEnd();
        var errTxt = AnsiMarkup.ToMarkup(ShellControls.Apply(diagErr)).TrimEnd();
        // 诊断尾巴收成**一处**：流式那条路要单独拿它（正文已经边跑边出去了，只有它是跑完才知道的）
        LastDiagnostics = BuildDiagnostics(midTxt, errTxt);

        var sb = new StringBuilder();
        if (outTxt.Length > 0) sb.Append(outTxt);
        if (LastDiagnostics.Length > 0) sb.Append(sb.Length > 0 ? "\n" : "").Append(LastDiagnostics);
        return sb.ToString();
    }

    /// <summary>
    /// 把两股诊断（`diag` 走 stdout、`diagErr` 走 stderr）拼成一段。
    /// **只此一份** —— 正序输出与流式的收尾都取它，两处各拼一遍必然漂移。
    /// </summary>
    private static string BuildDiagnostics(string midTxt, string errTxt)
    {
        var sb = new StringBuilder();
        if (midTxt.Length > 0) sb.Append(midTxt);
        if (errTxt.Length > 0) sb.Append(sb.Length > 0 ? "\n" : "").Append("«red»").Append(errTxt).Append("«/»");
        return sb.ToString();
    }

    /// <summary>串行化 <see cref="RunProgram"/> 里的控制台重定向（见那里的说明）。</summary>
    private static readonly object ConsoleRedirectGate = new();

    /// <summary>
    /// 命令行页当前的**终端尺寸**（列 / 行）—— 全屏输出按这个尺寸建网格。
    /// 0 = 未知 ⇒ 退回 **80×25**（老程序通用的假设）。
    ///
    /// ⚠ 做成**静态**的依据与 <see cref="OnProgress"/> 那条相同：**VML 的执行是排他的**
    /// （`VmlTool.ExecutionMode = Exclusive` 加上命令行页的 `_busy` 闸门），
    /// 同一时刻只可能有一次运行在读它。做成一路透传的参数则要改 6 个签名，而它们
    /// 全都只为了把这一个数送到最里面 —— 那种"接线"正是本仓库反复踩的漂移来源。
    /// </summary>
    /// <summary>
    /// 上一次 <see cref="RunProgram"/> 的输出**是不是一块全屏网格**（命令行页据此决定
    /// 要不要给它折行）。
    ///
    /// 为什么是静态的：与 <see cref="OnProgress"/> 同一条理由 —— **VML 的执行是排他的**
    /// （`VmlTool.ExecutionMode = Exclusive` + 命令行页的 `_busy` 闸门），
    /// 同一时刻只可能有一次运行在读写它。
    ///
    /// ⚠ 为什么不能用"逐行猜"代替这个标志：第一版按"可见字符全是空格"认画面行，
    ///   而全屏画面里的**标题栏、菜单项、状态行都是有文字的** ⇒ 它们照旧被折。
    ///   真机实测：80 列的网格被按自适应的 46 列折开，标题栏断成两行、边框全错位。
    ///   **"这一块是画面"是块级事实，只能由产生它的人（这里）说出来。**
    /// </summary>
    public static bool LastOutputWasGrid { get; private set; }

    /// <summary>
    /// 上一次全屏输出结束时**光标停在哪**（行/列，0 起；外加程序有没有要求显示它）。
    ///
    /// 命令行页据此在网格上画一个光标方块 —— 用户点名的：「光标位置也要显示光标，
    /// 除非指令关闭了光标」。
    ///
    /// 为什么走静态旁路而不是塞进返回的文本：光标不是**内容**，是一层"程序此刻在哪"的
    /// 状态。混进文本就要发明一种标记（而 `«»` 那套是给颜色/样式用的，塞位置进去
    /// 会让所有解析方都得认识它）；而静态旁路与 <see cref="LastOutputWasGrid"/> 完全同构 ——
    /// 理由也一样（**VML 的执行是排他的**，同一时刻只有一次运行在读写它）。
    ///
    /// 只在 <see cref="LastOutputWasGrid"/> 为真时有意义；文本输出那条路不设它。
    /// </summary>
    public static (int Row, int Col, bool Visible) LastCursor { get; private set; }
    public static int TermCols { get; set; }

    /// <inheritdoc cref="TermCols"/>
    public static int TermRows { get; set; }

    /// <summary>
    /// 空设备白名单 + 地址全部归零 = 关掉所有 MMIO 设备（VGA 压到 1x1，手机上没有它的窗口）。
    /// 不这么设的话单例 <c>DeviceManager</c> 会把上一次运行留下的地址带进下一次。
    /// </summary>
    private static VmConfig MakeConfig()
    {
        var cfg = new VmConfig
        {
            KeyboardDataAddress = 0,
            KeyboardStatusAddress = 0,
            MouseXAddress = 0,
            MouseYAddress = 0,
            VgaStartAddress = 0,
        };
        cfg.VgaDisplay.Width = 1;
        cfg.VgaDisplay.Height = 1;
        return cfg;
    }

    /// <summary>
    /// 把打包在 APK 里的 <c>vml_lib.zip</c>（= `third_party/vml/Lib`，39 MB / 5314 个文件）解压到可写目录，
    /// 返回**可以作为汇编基准路径的根**（它下面就是 `Lib/`）。**内容没变就直接返回，不解压。**
    ///
    /// 为什么必须解压：`LinkStandardLibrary` 与 `.include` 都是**按文件路径**去找
    /// `Lib/&lt;lang&gt;/…`，而 APK 里的资产不是文件 —— 编译器和汇编器都读不到。
    /// 打包成**一个 zip** 而不是逐文件 MauiAsset：39 MB → 6.1 MB，且 MAUI 没有「列出资产目录」的 API。
    ///
    /// **解压到 App 私有目录（`AppDataDirectory/vml`），不放在 `Global.Home` 下。**
    /// 这是被"重复解压"逼出来的：`Global.Home` 在 Android 上**会随「所有文件访问」权限在
    /// 私有目录 ↔ `sdcard/waycoder/config` 之间跳**（见 `MauiBootstrap.ResolveHomeDir`），
    /// 而标记文件跟着 Home 走 ⇒ 用户在系统设置里授权/撤销一次，新位置没有标记，
    /// **39 MB / 5314 个文件白解压一遍**。App 私有目录不随任何权限变化，标记永远不动。
    /// （顺带也对齐了本仓库自己的移动端铁律第 3 条：取路径一律走
    /// `FileSystem.Current.AppDataDirectory`，不许硬编码。）
    /// </summary>
    private static string? EnsureLibExtracted()
    {
        try
        {
            // 印记 = 人类可读的版本 + **内容**指纹。指纹那半截才是判据（见 LibVersion 注释）。
            var stamp = $"{LibVersion}:{LibFingerprint()}";

            var root = Path.Combine(FileSystem.AppDataDirectory, "vml");
            if (IsUsableLibRoot(root, stamp)) return root;   // 已经解压过且内容一致 → 不解压

            // **老位置兼容**：老版本解压到 `<Global.Home>/vml`。已经在那儿解压过、且内容对得上的
            // 用户**就地接着用**，别为了搬家白解压 39 MB（等哪天真换了内容，自然会解压到新位置）。
            // 权限没授时 `Global.Home` 本来就是 App 私有目录，这时两者是同一个路径，跳过。
            var legacy = Path.Combine(WayCoder.Global.Home, "vml");
            if (!string.Equals(legacy, root, StringComparison.OrdinalIgnoreCase)
                && IsUsableLibRoot(legacy, stamp))
                return legacy;

            Directory.CreateDirectory(root);

            // 解压是这条链上最长的静默段（39 MB / 5200+ 个文件，手机上好几秒）——
            // 不报一声的话，用户看到的就是"点了运行，屏幕一动不动"。
            OnProgress?.Invoke("⏳ 正在解压 VML 标准库（39 MB / 5200+ 个文件，仅首次或库更新后需要）…");

            // `OpenAppPackageFileAsync` 是异步的，而编译这条路整体是同步的（本来就跑在后台线程上）。
            // 同步等待在这里是安全的：调用方一定不在 UI 线程（见 VmlTool 的 Task.Run）。
            using var zipStream = FileSystem.OpenAppPackageFileAsync("vml_lib.zip").GetAwaiter().GetResult();
            using var zip = new ZipArchive(zipStream, ZipArchiveMode.Read);

            // 先解到临时目录再改名 —— 中途失败不会留下一个「半个 Lib」被当成完整的用
            var tmp = root + ".tmp";
            if (Directory.Exists(tmp)) Directory.Delete(tmp, recursive: true);
            zip.ExtractToDirectory(tmp, overwriteFiles: true);

            if (Directory.Exists(root)) Directory.Delete(root, recursive: true);
            Directory.Move(tmp, root);

            File.WriteAllText(Path.Combine(root, ".lib-version"), stamp);
            OnProgress?.Invoke("✔ 标准库解压完成。");
            return root;
        }
        catch
        {
            return null;   // 解压失败不该把编译之外的路径也带崩
        }
    }

    /// <summary>
    /// 这个 root 是不是已经有一份**当前内容**的库：标记对得上 + `Lib/` 真的在。
    /// 两样缺一不可 —— 只看标记会信一个"标记写了但目录被删了"的残骸；只看目录会在库更新后一直用旧的。
    /// </summary>
    private static bool IsUsableLibRoot(string root, string stamp)
    {
        try
        {
            var marker = Path.Combine(root, ".lib-version");
            return File.Exists(marker)
                   && File.ReadAllText(marker).Trim() == stamp
                   && Directory.Exists(Path.Combine(root, "Lib"));
        }
        catch
        {
            return false;   // 读不动（权限/残留）就当不可用，走解压那条路
        }
    }

    private static string? _libFingerprint;

    /// <summary>
    /// 内置标准库的**内容**指纹（懒算一次，整个进程只算一次）。
    ///
    /// 优先读随包的 <c>vml_lib.hash</c>（几十字节，由 `scripts/make-vml-lib.sh` 生成）——
    /// 它按「条目名 + 长度 + 内容」算、**不看时间戳**，所以"同样的内容重新打个包"指纹不变、
    /// 不会白解压一次；而且读取成本从 6 MB 降到几十字节。
    /// 没有这个资产（老包）才退回把整个 `vml_lib.zip` 哈希一遍 —— 行为退化成加它之前那样，不会更糟。
    /// </summary>
    private static string LibFingerprint()
    {
        if (_libFingerprint != null) return _libFingerprint;

        try
        {
            using var s = FileSystem.OpenAppPackageFileAsync("vml_lib.hash").GetAwaiter().GetResult();
            using var reader = new StreamReader(s);
            var text = reader.ReadToEnd().Trim();
            if (text.Length > 0) return _libFingerprint = text;
        }
        catch
        {
            // 没打进这个资产（或读不了）→ 走下面的整包哈希
        }

        using var stream = FileSystem.OpenAppPackageFileAsync("vml_lib.zip").GetAwaiter().GetResult();
        _libFingerprint = Convert.ToHexString(SHA256.HashData(stream))[..16];
        return _libFingerprint;
    }

    /// <summary>
    /// 把 VML 的输出收进内存。
    ///
    /// 不实现这个的话 <c>VmRuntime</c> 会回退到 <c>System.Console</c> ——
    /// 那在 MAUI 里等于「输出凭空消失」（没有可见的控制台）。
    /// 输入侧一律返回「没有输入」：命令行页是「敲一条、跑一条」的模型，不做交互式 stdin。
    /// </summary>
    private sealed class CaptureIo : IConsoleIO
    {
        private readonly StringBuilder _sb = new();

        /// <summary>
        /// 输入源。**这是阻塞的**：VM 在后台线程上跑，它调 <see cref="ReadString"/> 时就停在那儿，
        /// 直到宿主拿到一行才返回。为 null = 没有输入源（程序读到空串）。
        /// </summary>
        private readonly Func<string>? _readLine;

        /// <summary>当前这一行**还没吐出去**的剩余（<see cref="ReadChar"/> 逐字符消费它）。</summary>
        private string _cur = "";

        public CaptureIo(Func<string>? readLine = null, Action<string>? sink = null, Func<char>? readKey = null)
        {
            _readLine = readLine;
            _sink = sink;
            _readKey = readKey;
        }

        /// <summary>逐键输入源（见 <c>RunProgram</c> 的 readKey 说明）。null = 走按行那条老路。</summary>
        private readonly Func<char>? _readKey;

        /// <summary>把**新增的那部分**交给宿主。null = 不流式（跑完一次性给）。</summary>
        private readonly Action<string>? _sink;

        /// <summary>已经交出去的长度（`_sb` 是全量，靠它算增量）。</summary>
        private int _pumped;

        private long _lastFlushTicks;

        /// <summary>
        /// 有新增就按节流交出去。<paramref name="force"/> = 无视节流（**读输入之前**与**收尾**用）。
        ///
        /// <para>
        /// ⚠ **读输入之前必须强制刷**：程序打印完提示符就阻塞等键，那一屏如果还在节流窗口里
        /// 没出去，用户看到的就是"卡住了"—— 而那正是最需要看见画面的一刻。
        /// </para>
        /// </summary>
        private void FlushIfDue(bool force = false)
        {
            if (_sink == null) return;
            int total = _sb.Length;
            int pending = total - _pumped;
            if (pending <= 0) return;

            if (!force)
            {
                bool bySize = pending >= OutputFlushBytes;
                bool byTime = Environment.TickCount64 - _lastFlushTicks >= OutputFlushMs;
                if (!bySize && !byTime) return;
            }

            string delta = _sb.ToString(_pumped, pending);
            _pumped = total;
            _lastFlushTicks = Environment.TickCount64;
            _sink(delta);
        }

        /// <summary>收尾：把最后一段也交出去（不这么做的话，末尾没换行的那截会丢）。</summary>
        public void FlushAll() => FlushIfDue(force: true);

        public string Text => _sb.ToString();

        /* ── 流式出口 ──
           `_sb` 是**累积**的（跑完那条路还要拿全量），`TakeDelta` 只把**新增的**交出去。
           ⚠ 刷的时机**全在 VM 线程上**：不用定时器 —— 那边读、这边写同一个 `StringBuilder`
             就是数据竞争。写成"**有新增**才判节流"，程序不写就没有可刷的，天然不需要定时器。 */
        public void WriteString(string str) { _sb.Append(str); FlushIfDue(); }
        public void WriteChar(char ch) { _sb.Append(ch); FlushIfDue(); }
        public void WriteInt(int value) { _sb.Append(value); FlushIfDue(); }
        public void WriteFloat(float value) { _sb.Append(value); FlushIfDue(); }
        public void WriteHex(int value) { _sb.Append(value.ToString("X")); FlushIfDue(); }

        // 输入侧：全部走同一个 _readLine，按各自的类型转换。
        // 没给输入源时回退成空值/0（AI 工具那条路就是这种，见 VmlTool 的 `stdin` 参数）。
        public string ReadString()
        {
            FlushIfDue(force: true);        // 阻塞等键之前，先把画面放出去（见 FlushIfDue 的说明）
            return _readLine?.Invoke() ?? "";
        }

        /// <summary>
        /// ⚠ **逐字符吐，一次一个** —— 不能写成"取一行再返回 `line[0]`"。
        ///
        /// 那个写法（本方法原来就是）把用户敲的一行里**除首字符以外的全丢了**，
        /// 而接口契约明明是"读取一个字符"。后果经一整条链放大：`conio` 的 `getch()`
        /// 是拿 `getchar()` **循环攒一行**进键盘缓冲的，宿主一次只给一个字符 ⇒
        /// 用户敲 `wsad` 只有 `w` 进得了程序，`sad` 消失 —— 玩家那边的观感是
        /// "按键没反应"（游戏里就是"方向键不响应"），而且**不报错**。
        /// 桌面脚手架 `scripts/vml-c-probe/cases/16-conio-key.c` 钉的就是这一条
        /// （判据：敲 wsad ⇒ 四次 `getch` 依次拿到 119/115/97/100）。
        ///
        /// 行末补 `'\n'` = "用户按了回车"，那正是 `getch()` 循环等的收尾符；
        /// 没有输入源时 `ReadString()` 给空串 ⇒ 也是 `'\n'` ⇒ DOS 语义的"空行确认"。
        /// </summary>
        public char ReadChar()
        {
            /* **逐键直通**：宿主给了按键源就用它。
               老路（攒一行再逐字符吐）留着 —— 它对应"程序要的是一行，只是按字符读"的场景，
               两条路由**VM 调的是哪个接口**天然区分，不需要程序声明。 */
            if (_readKey != null)
            {
                FlushIfDue(force: true);      // 阻塞等键之前，先把画面放出去
                return _readKey();
            }
            if (_cur.Length == 0) _cur = ReadString() + "\n";   // ReadString 里已经刷过画面了
            var c = _cur[0];
            _cur = _cur.Substring(1);
            return c;
        }
        public int ReadInt() => int.TryParse(ReadString().Trim(), out var v) ? v : 0;
        public float ReadFloat() => float.TryParse(ReadString().Trim(), out var v) ? v : 0f;

        // ⚠ 手机端**预知不了**"下一个键什么时候来"：`_readLine` 是"弹输入框、等用户敲
        // 一行"的回调，**问一次就消费一次**，没有队列可以窥探。所以这里只能如实回答
        // **手上还没读完的那几个字符**（`_cur`），输入源还在时就倾向于说"有"。
        //
        // 与桌面 `vmlcli`（`scripts/vmlcli/Program.cs`）**有意不同的语义**：那边输入是
        // 一整条队列（`--stdin`），能如实回答"队列里还有没有"，于是 `conio.kbhit()` /
        // `curses` 的 `timeout(0)+wgetch` 能真正工作。手机上这条路对**用 curses 的老程序**
        // 仍会偏保守（`kbhit` 恒真 ⇒ 非阻塞读键退化成阻塞）——已知差距，见
        // `docs/老程序兼容性.md`。手机上的 VML 游戏走的是 `ui_poll`/`ui_wait` 那套消息
        // 机制，不经过这里，**不受影响**。
        public bool KeyAvailable() => _cur.Length > 0 || _readLine != null;

        /// <summary>
        /// 输入源**再也给不出东西** ⇒ `true`：只有"两个源都没有"才算
        /// （`_readLine == null && _readKey == null`）。
        ///
        /// ⚠ **不能写成"没有 `_readLine` 就算耗尽"** —— 手机上的 `_readKey`
        /// （逐键直通）与 `_readLine`（弹输入框等一行）是**两个不同的源**，
        /// 给其中一个也算有源。而**有源时**这一条必须是 `false`：
        /// `getchar` 现在是**阻塞**的（`io.c` 里 `MOVE R0 #1` + `SYSCALL #14`），
        /// 说成"耗尽"会让每个交互程序一读键就拿到 `EOF` 直接退出。
        ///
        /// 为什么"两个源都没有"要给 `true`：那种情况下**没有输入会到来**
        /// （AI 工具那条路就是它，见 `VmlTool` 的 `stdin` 参数），
        /// 给 `false` 会让阻塞读**空转到超时**，而正确语义是 `EOF`
        /// —— 程序照样能跑完、能打印结果，只是输入部分为空。
        /// </summary>
        public bool InputExhausted => _readLine is null && _readKey is null;
    }
}
