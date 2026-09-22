using System.Diagnostics;
using System.Text;
using VMLAssembler;
using VMLPlugins;
using VMLPlugins.Interfaces;
using VMLRuntime;
using VMLTool;
using WayCoder.UI.Shared;

namespace VmlCli;

/// <summary>
/// 桌面端 VML 编译 + 运行 CLI —— **手机端 <c>WayCoder.Maui/Services/MauiVml.cs</c> 的等价物**。
///
/// <para>
/// 存在的理由只有一个：手机上验收「22 种语言只有 7 种能跑通游戏骨架」时，每改一行都要
/// 打 APK + 装模拟器（约 2 分钟）。这个 CLI 走**完全相同的流水线**（同一份 vendored
/// <c>third_party/vml</c>、同一批前端编译器、同一套库清单解析），把它压到秒级。
/// </para>
///
/// <para>
/// <b>「等价」是设计目标，不是顺带</b>：凡是 MauiVml 里做过的决定（includePaths 只给
/// <c>Lib</c>、libraryPaths 只给**解析好的库文件**、<c>VML_HOME</c> 必须设、mcu 模式、
/// 空设备、500–599 号段放行），这里都照抄并注明出处。**不要「顺手改进」** ——
/// 改了就不再等价，CLI 上验过的修复到手机上可能不成立。
/// </para>
/// </summary>
internal static class Program
{
    private static int Main(string[] args)
    {
        CliOptions opt;
        try
        {
            opt = CliOptions.Parse(args);
        }
        catch (CliArgumentException ex)
        {
            Console.Error.WriteLine($"✘ {ex.Message}");
            Usage();
            return 2;
        }

        if (opt.Help) { Usage(); return 0; }

        // ── 模式二：重建 Lib 模块 ─────────────────────────────────────────────────
        // 把 Lib/shared/src/<模块>.c 编成 Lib/shared/<模块>.vml，**等价上游 CCompiler 的
        // `--no-link`**（`dotnet run --project CCompiler -- --no-link <src> -o <out>`）。
        //
        // 为什么不直接用上游那条路：vendored 的 `CCompiler.csproj` 是 `OutputType=Library`
        // （vmlcli 与 WayCoder.Maui 都要以项目引用它），而 `dotnet run` 要求可运行项目；
        // 用 `-p:OutputType=Exe` 覆盖又会**传播到所有被引用的子项目**，VMLPlugins 没有 Main
        // 直接 CS5001 构建失败。所以本仓自己留一个入口，走**同一个** `CompileFile` API。
        if (opt.RebuildLibSource is not null)
            return RebuildLibModule(opt);


        if (opt.SourcePath is null)
        {
            Console.Error.WriteLine("✘ 缺少源文件路径。");
            Usage();
            return 2;
        }

        var sourcePath = Path.GetFullPath(opt.SourcePath);
        if (!File.Exists(sourcePath))
        {
            Console.Error.WriteLine($"✘ 找不到文件：{sourcePath}");
            return 2;
        }

        // ── 找 vendored VML 根（= Lib/ 所在的那一层，也就是 VML_HOME）────────────────
        var vmlRoot = opt.VmlHome ?? FindVmlRoot(sourcePath);
        if (vmlRoot is null)
        {
            Console.Error.WriteLine("✘ 没找到 vendored 的 third_party/vml（含 Lib/ 与 vmltool.config.xml 的那一层）。"
                + "用 --vml-home <路径> 显式指定。");
            return 2;
        }

        // **`VML_HOME` 必须设** —— 不设的话前端产物里的相对 `.linked "Lib/c/builtin.vml"`
        // 全部解析不到，链接阶段静默少链标准库，运行期才炸成「未找到标签: shared_puts」。
        // （MauiVml.BuildProgram 里那段长注释记的就是这个坑。）
        Environment.SetEnvironmentVariable("VML_HOME", vmlRoot);

        var sw = Stopwatch.StartNew();

        // ── ① 编译 + 汇编 + 链接 ────────────────────────────────────────────────────
        // 这一段上游会往 Console 写不少东西（编译器诊断、链接器的「未解析标签」警告…）。
        // 先接到内存里再整体转发到 **stderr** —— stdout 要留给「VML 程序自己的输出」，
        // 否则验收时没法逐字比对。手机端这些内容会落进看不见的控制台（所以设备报告里
        // 看不到链接警告），这里把它们显式亮出来，是**多给的信息**、不影响等价性。
        var compileLog = new StringBuilder();
        VmlProgram? prog;
        string lang;
        string? error;
        lock (ConsoleGate)
        {
            var prevOut = Console.Out;
            var prevErr = Console.Error;
            try
            {
                Console.SetOut(new StringWriter(compileLog));
                Console.SetError(new StringWriter(compileLog));
                (prog, lang, error) = BuildProgram(sourcePath, vmlRoot, opt);
            }
            finally
            {
                Console.SetOut(prevOut);
                Console.SetError(prevErr);
            }
        }

        var compileMs = sw.ElapsedMilliseconds;
        if (compileLog.Length > 0)
            Console.Error.Write(compileLog.ToString());

        if (prog is null)
        {
            Console.Error.WriteLine($"✘ 编译失败（{lang}）：{error}");
            return 1;
        }

        Console.Error.WriteLine($"✔ 编译完成（{lang}，{compileMs} ms，{prog.Instructions?.Count ?? 0} 条指令）");

        // `--vml <路径>`：把链接之后的整份 VML 汇编存下来（排查前端产物用）。
        // ⚠ `ToString()` 会**就地**做死代码消除，调用之后这个 prog 不能再拿去跑
        //（MauiVml.CompileToVml 的长注释），所以存盘之后**直接退出**，不继续运行。
        if (opt.EmitVmlPath is not null)
        {
            File.WriteAllText(opt.EmitVmlPath, prog.ToString(), new UTF8Encoding(false));
            Console.Error.WriteLine($"✔ 已写出 VML 汇编：{Path.GetFullPath(opt.EmitVmlPath)}");
            return 0;
        }

        // ── ② 运行 ─────────────────────────────────────────────────────────────────
        var output = RunProgram(prog, sourcePath, opt);
        Console.Out.Write(output);
        if (output.Length > 0 && !output.EndsWith('\n')) Console.Out.Write('\n');

        Console.Error.WriteLine($"✔ 运行完成（总 {sw.ElapsedMilliseconds} ms）");
        return 0;
    }

    // ══════════════════════════════════════════════════════════════════════════════
    // 编译：与 MauiVml.BuildProgram 逐步对齐
    // ══════════════════════════════════════════════════════════════════════════════

    private static (VmlProgram? Prog, string Lang, string? Error) BuildProgram(
        string filePath, string vmlRoot, CliOptions opt)
    {
        // 静态注册 22 个前端编译器（绕开 PluginManager 的 Assembly.LoadFrom 反射路径 ——
        // 上游自己在 StaticLink 模式里也绕开了它；见 MauiVml 同处注释）。
        var pm = new PluginManager();
        RegisterFrontendCompilers(pm);

        // ── `.vml` = **已经是汇编**，根本不进前端注册表 ──
        //
        // 那 22 个前端里没有 `.vml`，落到下面只会得到"认不出这个扩展名"。
        // 派发规则照抄 `MauiVml.Run`（上游 `VMLTool/Program.Compile.cs:34` 也是这么分的：
        // `.vml` 直接当汇编，其余交给 `PluginManager.GetCompilerByFileName`）。
        //
        // ⚠ 走的实现也**必须与 `MauiVml.RunAssembly` 同一份** —— 它用的是
        //   `new VmlAssembler().Assemble(text)`（**不解析 `.include`、不链标准库**），
        //   不是下面 ② 那条 `AssembleWithIncludes` + `LinkLibraries`。
        //   要 `.include`/`.linked` 的用例得自己写出来 —— 两边不一样就不是等价。
        if (Path.GetExtension(filePath).Equals(".vml", StringComparison.OrdinalIgnoreCase))
        {
            try { return (new VmlAssembler().Assemble(File.ReadAllText(filePath)), "vml", null); }
            catch (Exception asmError) { return (null, "vml", $"⚠️ 汇编失败：{asmError.Message}"); }
        }

        // 扩展名派发用上游现成的（`--lang` 显式指定时按名字查，等价于上游 CLI 的 -l）
        var compiler = opt.Lang is not null
            ? pm.GetFrontendCompiler(opt.Lang)
            : pm.GetCompilerByFileName(Path.GetFileName(filePath));

        if (compiler is null)
        {
            var how = opt.Lang is not null
                ? $"没有名为 `{opt.Lang}` 的前端编译器"
                : $"认不出这个扩展名（{Path.GetExtension(filePath)}）";
            var all = string.Join(", ", pm.GetAllFrontendCompilers().Select(c => c.Name).OrderBy(n => n, StringComparer.Ordinal));
            return (null, opt.Lang ?? "?", $"⚠️ {how}。可用：{all}");
        }

        if (compiler is not IFrontendCompilerEx ex)
            return (null, compiler.Name, "⚠️ 这个编译器没实现 IFrontendCompilerEx，走不了带 include 的编译链。");

        var lang = compiler.Name.ToLowerInvariant();

        // 库清单 / include 路径**照抄上游 CLI 与 MauiVml 的口径**，不自己发挥：
        //   IncludePaths：上游只加 `<VML_HOME>/Lib` 一项（ResolveIncludePath 内部另有
        //     Lib/c、Lib/shared 兜底，补多了只是多几层搜索）；
        //   LibraryPaths：**必须给「解析好的库文件」，绝不能给目录** —— 给目录会让
        //     ConvertLibraryPathsToIncludes 把该目录下每一个 .vml 都挂上去再全量链接
        //     （实测同一个 hello.c：给目录 93423 条指令、给库文件 36261 条）。
        var libConfig = VmlToolConfig.Load(Path.Combine(vmlRoot, "vmltool.config.xml"));
        var includePaths = new[] { Path.Combine(vmlRoot, "Lib") }.Where(Directory.Exists).ToList();
        var libraryPaths = libConfig.ResolveLibs(lang, vmlRoot);

        if (libraryPaths.Count == 0)
            return (null, lang, "⚠️ 标准库清单为空 —— `vmltool.config.xml` 没读到或路径不对。"
                + "没有它 `LinkLibraries` 会直接早退（等于静默不链接）。");

        // ① 前端编译（预处理 + include + 标准库挂载）
        //
        // ⚠ **不要再包一层 `CompilerOptionsContext.RunWith`**：所有编译器的基类
        //   `CompilerBase.SyncToContext()` 在每个入口（Compile/CompileFile/CompileFileWithIncludes）
        //   的第一句就是 `CompilerOptionsContext.Current = Config.ToCompilerOptions()`，
        //   **无条件覆盖** —— 外面设的 copts 一个字都不生效。所以上游 CLI 那层 RunWith 对
        //   这 22 个编译器是空转，设备上真正生效的是 `CompilerConfig` 的默认值
        //   （TargetMode=MCU / MemoryLevel=RAM_M / Float64Mode=Hard / Int64Mode=Hard）。
        //   这里与 MauiVml 同样**直接调**，保证和手机端同一口径。
        //
        // `autoLinkStdLib: true, useSharedLibrary: true` —— 与 MauiVml 完全一致
        //   （上游 CLI 还多一道「没有 main 就不自动链」的自动识别，MauiVml 没做，这里也不做）。
        string vmlText;
        try
        {
            vmlText = ex.CompileFileWithIncludes(filePath, includePaths, libraryPaths,
                autoLinkStdLib: true, useSharedLibrary: true);
        }
        catch (Exception compileError)
        {
            // ⚠ 编译/链接失败要**当成"编译失败"报**，不能让它穿到顶层变成
            //   `Unhandled exception` + 一屏堆栈 —— 那看上去像编译器自己崩了，
            //   而实际上绝大多数是**用户源码的问题**（今天新加的那条"未定义的函数"
            //   就是最典型的一个：拼错一个函数名，以前要等到**运行**才抛
            //   `KeyNotFoundException`，现在编译期就拦下来，但如果在这里变成堆栈，
            //   用户看到的仍然是一屏看不懂的东西）。
            //   与 `MauiVml` 同口径：只取 `Message`（它已经是 GCC 风格的多行诊断）。
            var inner = compileError is AggregateException agg ? agg.GetBaseException() : compileError;
            return (null, lang, $"⚠️ 编译失败：{inner.Message}");
        }

        // 自检：产物得像 VML 汇编（上游「失败就静默原样返回」会把编译错误伪装成汇编期的
        // 「未知指令」，很难查）。判据与 MauiVml.LooksLikeVml 逐条相同。
        if (string.IsNullOrWhiteSpace(vmlText) || !LooksLikeVml(vmlText))
        {
            var head = vmlText ?? "(空)";
            return (null, lang, $"⚠️ 前端编译没有产出 VML 汇编（{compiler.Name}）。产物前 200 字符：\n"
                + head[..Math.Min(200, head.Length)]);
        }

        // ② 汇编（.include / .macro 解析基准指向 VML 根，使 "Lib/..." 能解析）
        //
        // langDefines 与 MauiVml 逐项相同（`VML_{LANG}` + MCU 模式 + RAM 级别）——
        // 这三个宏会让 `#ifdef` 分支走不同代码路径，**不一致就不是等价**。
        var langDefines = new List<string>
        {
            $"VML_{compiler.Name.ToUpperInvariant()}",
            "VML_MODE_MCU",
            $"VML_RAM_{opt.RamSuffix}",
        };
        // ⚠ ② 汇编与 ③ 链接一起**必须包 try**，两条理由：
        //   ① 有些前端（Pascal / Forth / Ladder / Basic）不在自己的 `CompileFileWithIncludes`
        //      里链接 —— 链接是在**这里**做的，所以「用户代码调用了不存在的函数」这条编译期
        //      错误，对那几门语言是在这一步抛出来的；
        //   ② 汇编器抛的同样是**用户可见的诊断**（未知指令 / 寄存器名越界 / 未找到标签），
        //      不接住就打穿成 `Unhandled exception` + 一屏堆栈（实测：只留一句
        //      `寄存器名越界：R99`，看不出是**哪一行**汇编，也看不出这是编译错误）。
        //   口径与 `MauiVml` 同步 —— 那边也补上了同一层 try（此前它的注释里就记着这个缺口）。
        VmlProgram prog;
        try
        {
            prog = new VmlAssembler().AssembleWithIncludes(vmlText, vmlRoot, langDefines);
            LibraryLinker.LinkLibraries(prog, libraryPaths);
            prog.ApplyExports();
        }
        catch (Exception asmOrLinkError)
        {
            return (null, lang, $"⚠️ 编译失败：{asmOrLinkError.Message}");
        }

        return (prog, lang, null);
    }

    /// <summary>
    /// 重建单个 Lib 模块：`<src>.c` → `<out>.vml`。
    ///
    /// 与上游 `CCompiler --no-link` 逐步对齐（`Program.cs` 的 `_noLink` 分支）：
    /// ```csharp
    /// var prog = CCompiler.CompileFile(sourceFile, includePaths, null, false);
    /// prog.IsLibrary = true;
    /// return prog.ToString();
    /// ```
    /// `includePaths` 传空是**对的** —— 上游重建脚本一个 `-I` 都不传，`CompileFile` 自己会把
    /// 源文件所在目录加进去（`CCompiler.cs` 的 `sourceDir`），`Lib/shared/src/*.c` 的
    /// `#include "shared_decls.h"` 就是靠这条解析到的。
    /// </summary>
    private static int RebuildLibModule(CliOptions opt)
    {
        var src = Path.GetFullPath(opt.RebuildLibSource!);
        if (!File.Exists(src))
        {
            Console.Error.WriteLine($"✘ 找不到源文件：{src}");
            return 2;
        }
        var outPath = Path.GetFullPath(opt.RebuildLibOut
            ?? Path.ChangeExtension(src, ".vml"));

        lock (ConsoleGate)
        {
            var prevOut = Console.Out;
            var sink = new StringWriter();
            try
            {
                Console.SetOut(sink);
                var prog = CCompiler.CCompiler.CompileFile(src, new List<string>(), null, false);
                prog.IsLibrary = true;
                File.WriteAllText(outPath, prog.ToString(), new UTF8Encoding(false));
                Console.SetOut(prevOut);
                var n = prog.Instructions?.Count ?? 0;
                Console.Error.WriteLine($"✔ 已重建 {Path.GetFileName(outPath)}（{n} 条指令）");
                return 0;
            }
            catch (Exception ex)
            {
                Console.SetOut(prevOut);
                // ⚠ 只打 `ex.Message` 会把**唯一能定位的线索（堆栈）**吞掉 ——
                //   实测 `bitops64.c` 报 "Value was either too large or too small for a
                //   UInt64."，而全树搜不到任何 `ToUInt64` 调用 ⇒ 没有堆栈就完全查不下去。
                Console.Error.WriteLine($"✘ 重建失败：{ex.Message}");
                Console.Error.WriteLine(ex.ToString());
                // `StringWriter` 没有 `Length`（要经 `GetStringBuilder()`）——
                // 与上面 `BuildProgram` 里用 `StringBuilder` 记日志的写法不同，别照抄。
                if (sink.GetStringBuilder().Length > 0) Console.Error.WriteLine(sink.ToString());
                return 1;
            }
        }
    }

    /// <summary>与 MauiVml.LooksLikeVml 同判据（正判据 + 「残留 #include」反判据）。</summary>
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
    ///   而 C 程序第一行往往就是 `#include &lt;stdio.h&gt;`。用 `Contains` 判的话，
    ///   只要源行注释一开，**所有带 #include 的程序都会假失败**
    ///   （实测：`scripts/vml-out-probe/langs/nat.c` 从 PASS 直接变"前端编译没有产出 VML 汇编"）。
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

    // ══════════════════════════════════════════════════════════════════════════════
    // 运行：与 MauiVml.RunProgram 逐步对齐（去掉 MAUI 专有的 UI 部分）
    // ══════════════════════════════════════════════════════════════════════════════

    /// <summary>`argv`：`[0]` = 程序名（源文件名），其后依次是 `--arg` 给的值。</summary>
    private static List<string> BuildArgv(string sourcePath, List<string> args)
    {
        var argv = new List<string> { Path.GetFileName(sourcePath) };
        argv.AddRange(args);
        return argv;
    }

    private static string RunProgram(VmlProgram prog, string sourcePath, CliOptions opt)
    {
        // 每次都重置单例 DeviceManager（跨运行保留状态，不重置第二次跑的 MMIO 地址会和第一次串）。
        VMLRuntime.Device.DeviceManager.Instance.Reset();

        var io = new CaptureIo(opt.StdinText);
        CliErr.WriteLine($"[dbg] StdinText={(opt.StdinText is null ? "<null>" : "'" + opt.StdinText + "'")} KeyAvailable={io.KeyAvailable()}");

        // **永远只用 "mcu" 模式** —— 这是手机端的安全边界（见 MauiVml 那段长注释：
        // os 模式会放开线程/Socket/mkdir/Exec 等 300–376 号，手机上要么被沙箱挡、要么不该开）。
        // 处理器先建出来（下面 `uiCalls.Runtime = vm` 要回填 —— 通用调用口读浮点/长整数
        // 寄存器组必须拿得到运行时，见 NullUiCalls.Runtime 的注释）。
        var uiCalls = new CliUiCalls(opt.ToHostConfig(sourcePath));
        using var vm = new VmRuntime(2 * 1024 * 1024, MakeConfig(), [], mode: "mcu")
        {
            TimeoutSeconds = Math.Clamp(opt.TimeoutSeconds, 1, 600),
            ConsoleIO = io,
            SystemCallHandler = uiCalls,
            // 文件沙箱根 = 源文件所在目录（手机上等价物是 CwdContext.Root = workspace）。
            FileSystemRoot = Path.GetDirectoryName(sourcePath) ?? Directory.GetCurrentDirectory(),
            /* 命令行参数：`argv[0]` = **程序名**（源文件名），其后依次是 `--arg` 给的值。
               两边都保证 `argc >= 1`（C 的语义）—— 宿主给了就按宿主的，
               宿主一个都不给时运行时会补一个空串。 */
            CommandLineArgs = BuildArgv(sourcePath, opt.Args),
        };
        uiCalls.Vm = vm;

        // 网络：只放客户端那一半，与 MauiVml 逐号相同。
        foreach (var syscall in new[] { 330, 334, 335, 336, 337, 338 })
            vm.HostAllowedSyscalls.Add(syscall);

        vm.LoadProgram(prog);

        // **脚本化输入**（`--input`）：手机端程序靠 `ui_wait_msg`/`ui_poll` 收按键与触摸，
        // 桌面端没有输入源 ⇒ 不装这个，任何交互程序都只能空转（这正是"桌面上什么也证明不了"
        // 的另一半：前半是画不出来，后半是动不了）。投递走的是与手机端 UI 线程**同一条路**。
        uiCalls.Host.StartInputScript();

        // 运行时的诊断（内存错误 / 标签错误 / 未预期崩溃 + 16 个寄存器 dump 走 Console.Error，
        // Permission denied / VM execution cancelled 走 Console.Out）—— 接进内存并进返回值。
        string diag;
        lock (ConsoleGate)
        {
            var prevOut = Console.Out;
            var prevErr = Console.Error;
            var sink = new StringWriter();
            try
            {
                Console.SetOut(sink);
                Console.SetError(sink);
                vm.Run();
            }
            finally
            {
                Console.SetOut(prevOut);
                Console.SetError(prevErr);
                diag = sink.ToString();
            }
        }

        // **出图**（`--frame` / `--frames`）：程序跑完之后才做，所以拍到的是**它声明过的最后一帧**
        //（`VmlScene.PresentedDsl` 是 `ui_present()` 那一刻当场拍的快照，不是"此刻的场景"）。
        uiCalls.Host.EmitRequestedOutputs();

        if (diag.Length == 0) return io.Text;
        return io.Text.Length == 0 ? diag.TrimEnd() : io.Text.TrimEnd() + "\n" + diag.TrimEnd();
    }

    /// <summary>与 MauiVml.MakeConfig 相同：空设备白名单 + 地址全归零（VGA 压到 1x1）。</summary>
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

    /// <summary>串行化控制台重定向（<c>Console.SetOut</c> 是进程级的）。</summary>
    private static readonly object ConsoleGate = new();

    // ══════════════════════════════════════════════════════════════════════════════
    // 前端编译器注册（清单唯一真源 = WayCoder/UI/Shared/VmlFrontendCompilerList.cs）
    // ══════════════════════════════════════════════════════════════════════════════

    /// <summary>
    /// 注册全部 22 个前端编译器，注册完**断言「注册到的 == 清单里的」**（与手机端
    /// <c>VmlFrontendCompilers.RegisterAll</c> 同一套护栏，只是不做「上游漂移」那半截）。
    ///
    /// 断言失败就抛 —— 静默少一种语言的症状是「某个语言莫名不能用」，极难排查；
    /// 宁可响亮地炸一次。
    /// </summary>
    private static void RegisterFrontendCompilers(PluginManager manager)
    {
        var registered = new List<IFrontendCompiler>(VmlFrontendCompilerList.All.Count);

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

        var wantTypes = VmlFrontendCompilerList.PluginTypes;
        var gotTypes = registered.Select(c => c.GetType().FullName ?? c.GetType().Name)
            .ToHashSet(StringComparer.Ordinal);

        var missing = wantTypes.Except(gotTypes).OrderBy(x => x, StringComparer.Ordinal).ToList();
        var extra = gotTypes.Except(wantTypes).OrderBy(x => x, StringComparer.Ordinal).ToList();

        if (missing.Count == 0 && extra.Count == 0
            && manager.FrontendCompilerCount == VmlFrontendCompilerList.All.Count)
            return;

        throw new InvalidOperationException(
            "VML 前端编译器清单漂移 —— 本文件的 RegisterFrontendCompilers 与 "
            + "WayCoder/UI/Shared/VmlFrontendCompilerList.cs 的 All 对不上。\n"
            + $"  期望 {VmlFrontendCompilerList.All.Count} 个 / 实际注册到 {manager.FrontendCompilerCount} 个\n"
            + (missing.Count > 0 ? $"  清单里有但没注册：{string.Join(", ", missing)}\n" : "")
            + (extra.Count > 0 ? $"  注册了但清单里没有：{string.Join(", ", extra)}\n" : ""));

        void Register(IFrontendCompiler compiler)
        {
            manager.RegisterFrontendCompiler(compiler);
            registered.Add(compiler);
        }
    }

    // ══════════════════════════════════════════════════════════════════════════════
    // 辅助
    // ══════════════════════════════════════════════════════════════════════════════

    /// <summary>
    /// 从「源文件所在目录」与「本进程工作目录」两个起点向上找 vendored VML 根
    /// （判据：该目录下同时有 <c>Lib/</c> 与 <c>vmltool.config.xml</c>）。
    /// </summary>
    private static string? FindVmlRoot(string sourcePath)
    {
        foreach (var start in new[]
                 {
                     Path.GetDirectoryName(sourcePath),
                     Directory.GetCurrentDirectory(),
                     AppContext.BaseDirectory,
                 })
        {
            for (var dir = start; !string.IsNullOrEmpty(dir); dir = Path.GetDirectoryName(dir))
            {
                var candidate = Path.Combine(dir, "third_party", "vml");
                if (File.Exists(Path.Combine(candidate, "vmltool.config.xml"))
                    && Directory.Exists(Path.Combine(candidate, "Lib")))
                    return Path.GetFullPath(candidate);

                // 也允许直接指到 vml 根（比如本 CLI 就住在 <repo>/scripts/vmlcli 时不会命中，
                // 但用户把源码放在 third_party/vml/Examples 下就会先命中这一支）。
                if (File.Exists(Path.Combine(dir, "vmltool.config.xml"))
                    && Directory.Exists(Path.Combine(dir, "Lib")))
                    return Path.GetFullPath(dir);
            }
        }
        return null;
    }

    private static void Usage()
    {
        Console.Error.WriteLine("""
用法：dotnet run --project scripts/vmlcli -- <源文件路径> [选项]
      （已构建的话更快：dotnet scripts/vmlcli/bin/Release/net10.0/vmlcli.dll <源文件路径>）

选项：
  --lang <名字>        强制指定前端编译器（c/python/lua/java/forth/…；默认按扩展名派发）
  --profile <大|中|小> 内存档位 → VML_RAM_G / VML_RAM_M / VML_RAM_K（默认 中 = RAM_M，与手机端一致）
  --timeout <秒>       运行超时（默认 30，上限 600）
  --vml-home <路径>    显式指定 vendored VML 根（含 Lib/ 与 vmltool.config.xml 的那一层）
  --vml <路径>         把链接之后的 VML 汇编写出到文件（只编不跑）
  -h, --help           显示本帮助

绘图窗口（UI 程序：ui_win_open / ui_rect / ui_present …）：
  --frame <路径>       运行结束后把「最新呈现帧」渲成 PNG 写到这里
                       （判据是 ui_present 拍下的快照，不是"此刻的场景"—— 见 VmlScene.Present）
  --frames <目录>      每个 ui_present 落一帧（看动画用，文件名 frame_0000.png …）
  --frames-max <N>     帧数上限（默认 120，防死循环程序写满磁盘）
  --screen <宽x高>     可用绘图区 / SCR_W、SCR_H 报的数（默认 480x640）

交互（UI 程序靠 ui_wait_msg / ui_poll 取输入，桌面没有输入源就要脚本喂）：
  --input <路径>       脚本化输入事件表，每行 `<毫秒> <事件> [参数…]`：
                         keydown/keyup <虚拟键码>   mousemove/mousedown/mouseup <x> <y>
                         touchdown/touchmove/touchup <x> <y>
                         resize <w> <h>   orient <0|1>   close   wait
                       键码沿用 Win32 虚拟键值（方向键 37–40、回车 13、空格 32、A/B/X/Y 就是字母）
  --answer <值>        对话框按顺序消费的答案（可重复）。
                        消息框：ok/yes（默认）| cancel/no；单选：下标；多选：逗号分隔下标；
                        输入框：任意文本；cancel 一律表示取消
  --store <路径>       键值存档落成 JSON（不给就只在本次进程内，不碰用户目录）
  --stdin <文本>       脚本化标准输入（给 getchar/getch/scanf 这类读字节的程序）。
                       换行写成字面 `\n`；没给就"读到的恒为空"。
                       语义与手机端逐条对齐（一次读一整行、ReadChar 取行首字符）——
                       见 CaptureIo 的注释：**两边不同源的话这里测出来的代表不了手机**。

重建 Lib 模块（与上面互斥，走单独一条路）：
  --rebuild-lib <源.c> [--out <输出.vml>]
                       把 Lib/shared/src/<模块>.c 编成 Lib/shared/<模块>.vml
                       （等价上游 CCompiler 的 --no-link；--out 默认与源文件同名）

输出：stdout = VML 程序自己的输出（+ 运行期诊断）；stderr = 编译/链接日志与进度。
""");
    }
}

// ══════════════════════════════════════════════════════════════════════════════════

/// <summary>命令行参数。</summary>
internal sealed partial class CliOptions
{
    public string? SourcePath { get; private set; }
    public string? Lang { get; private set; }
    public string? VmlHome { get; private set; }
    public string? EmitVmlPath { get; private set; }
    /// <summary>`--rebuild-lib`：要走「重建 Lib 模块」这条路的源文件（.c）。</summary>
    public string? RebuildLibSource { get; private set; }
    /// <summary>`--out`：重建模式的输出路径（默认与源文件同名的 .vml）。</summary>
    public string? RebuildLibOut { get; private set; }
    public int TimeoutSeconds { get; private set; } = 30;

    /// <summary>
    /// <c>--arg &lt;值&gt;</c>：给程序传一个命令行参数（**可重复**，按出现顺序累加）。
    ///
    /// ⚠ **刻意不做引号解析**：本仓踩过"命令按空白切分、路径带空格就断成两截"
    /// （见 `ShellCommandRegistry.Split`）。要带空格的参数就整段当一个值给：
    /// <c>--arg "hello world"</c>。
    ///
    /// `argv[0]` 由我们补成**源文件名**（C 的语义：`argv[0]` 是程序名），
    /// 所以 `--arg` 给的值从 `argv[1]` 开始 —— 宿主一个都不给时 `argc` 就是 1。
    /// </summary>
    public List<string> Args { get; } = new();
    public bool Help { get; private set; }

    /// <summary>
    /// <c>--stdin</c>：脚本化标准输入。**换行写成字面 `\n`**（命令行里带真换行不好写），
    /// 由 <see cref="CaptureIo"/> 自己翻译。语义与手机端逐条对齐 —— 见那个类的注释。
    /// </summary>
    public string? StdinText { get; private set; }

    /// <summary><c>VML_RAM_*</c> 宏的后缀（手机端恒为 M —— 见 <c>MauiVml.BuildProgram</c>）。</summary>
    public string RamSuffix { get; private set; } = "M";

    public static CliOptions Parse(string[] args)
    {
        var o = new CliOptions();

        for (int i = 0; i < args.Length; i++)
        {
            var a = args[i];
            switch (a)
            {
                case "-h":
                case "--help":
                    o.Help = true;
                    break;

                case "--lang":
                    o.Lang = Require(args, ref i, "--lang").ToLowerInvariant();
                    break;

                case "--vml-home":
                    o.VmlHome = Require(args, ref i, "--vml-home");
                    break;

                case "--vml":
                    o.EmitVmlPath = Require(args, ref i, "--vml");
                    break;

                case "--rebuild-lib":
                    o.RebuildLibSource = Require(args, ref i, "--rebuild-lib");
                    break;

                case "--out":
                    o.RebuildLibOut = Require(args, ref i, "--out");
                    break;

                case "--timeout":
                    var t = Require(args, ref i, "--timeout");
                    if (!int.TryParse(t, out var secs) || secs <= 0)
                        throw new CliArgumentException($"--timeout 需要一个正整数，收到 `{t}`");
                    o.TimeoutSeconds = secs;
                    break;

                case "--arg":
                    o.Args.Add(Require(args, ref i, "--arg"));
                    break;

                case "--stdin":
                    o.StdinText = Require(args, ref i, "--stdin");
                    break;

                case "--profile":
                    var p = Require(args, ref i, "--profile");
                    o.RamSuffix = p switch
                    {
                        "大" or "big" or "large" or "g" => "G",
                        "中" or "mid" or "medium" or "m" => "M",
                        "小" or "small" or "tiny" or "k" => "K",
                        _ => throw new CliArgumentException($"--profile 只认 大/中/小，收到 `{p}`"),
                    };
                    break;

                default:
                    // 宿主相关的选项（--frame / --input / --screen / --answer / --store …）
                    // 认领不了才往下走 —— **绝不静默跳过**（拼错的选项必须响亮地失败）。
                    if (o.TryParseHostOption(a, args, ref i)) break;
                    if (a.StartsWith('-'))
                        throw new CliArgumentException($"未知选项 `{a}`");
                    if (o.SourcePath is not null)
                        throw new CliArgumentException($"多余的参数 `{a}`（只接受一个源文件路径）");
                    o.SourcePath = a;
                    break;
            }
        }

        return o;
    }

    private static string Require(string[] args, ref int i, string name)
    {
        if (i + 1 >= args.Length) throw new CliArgumentException($"{name} 缺少参数值");
        return args[++i];
    }
}

internal sealed class CliArgumentException(string message) : Exception(message);

/// <summary>
/// 把 VML 的输出收进内存（与 <c>MauiVml.CaptureIo</c> 同形）。
/// 输入侧一律返回空：命令行工具不做交互式 stdin（与手机端命令行页同一模型）。
/// </summary>
/// <summary>
/// 桌面端的 <see cref="IConsoleIO"/>：输出收进内存，输入从 <c>--stdin</c> 来。
///
/// **输入侧逐条照抄手机端**（<c>WayCoder.Maui/Services/MauiVml.cs</c> 的 <c>CaptureIo</c>）——
/// 本工具存在的意义就是「与手机端逐字等价地秒级迭代」，两边一旦不同源，
/// 这里跑绿（或跑红）**都不能代表手机**，而那是本仓排第一的坑。手机端那三条是：
///
///   · <see cref="ReadString"/> 每次取**一整行**（手机端是"在命令行页敲一行、按运行提交"）；
///   · <see cref="ReadChar"/> 取**那一行的第一个字符** —— 这条最要紧：`conio` 的
///     `getch()` 是拿 `getchar()` 逐字符攒缓冲的，宿主一次给一行的话能拿到什么，
///     全由这一行的实现决定；
///   · <see cref="KeyAvailable"/> 只要**有输入源**就恒为 true（手机端原话：
///     "让 `while(!KeyAvailable()); getchar()` 这类轮询能推进"）。
///
/// ⚠ 与手机端**唯一**的差别在"输入用完之后"：手机端的 <c>_readLine</c> 是**阻塞**的
/// （等到用户敲下一行为止），桌面跑脚本不能挂在那儿，于是用**空行**代替 ——
/// 空行在 DOS 语义里是"按了回车"，正是老程序里最常见的确认。
/// </summary>
internal sealed class CaptureIo : IConsoleIO
{
    private readonly StringBuilder _sb = new();
    private readonly Queue<string>? _lines;
    /// <summary>当前这一行**还没吐出去**的剩余（<see cref="ReadChar"/> 逐字符消费它）。</summary>
    private string _cur = "";

    /// <param name="stdin">
    /// `--stdin` 的原文。**换行写成字面 `\n`**（命令行里带真换行没法写），在这里翻译。
    /// 传 null = 没有输入源（读到的恒为空）。
    /// </param>
    public CaptureIo(string? stdin = null)
    {
        if (stdin is null) return;
        _lines = new Queue<string>(stdin.Replace("\\n", "\n").Split('\n'));
    }

    public string Text => _sb.ToString();

    public void WriteString(string str) => _sb.Append(str);
    public void WriteChar(char ch) => _sb.Append(ch);
    public void WriteInt(int value) => _sb.Append(value);
    public void WriteFloat(float value) => _sb.Append(value);
    public void WriteHex(int value) => _sb.Append(value.ToString("X"));

    public string ReadString() => _lines is { Count: > 0 } q ? q.Dequeue() : "";

    /// <summary>
    /// ⚠ **逐字符吐，一次一个** —— 不能写成"取一行再返回 `line[0]`"。
    ///
    /// 那个写法（本类原来就是）把一行里**除首字符以外的全丢了**，而 `IConsoleIO.ReadChar`
    /// 的契约明明是"读取一个字符"。后果经过一整条链放大：`conio` 的 `getch()` 是拿
    /// `getchar()` **循环攒一行**进键盘缓冲的，宿主一次只给一个字符 ⇒ 用户敲的
    /// `wsad` 只有 `w` 进得了程序，`sad` 消失 —— 玩家那边的观感就是"按下没反应"，
    /// 而且**不报错**（`scripts/vml-c-probe/cases/16-conio-key.c` 钉的就是这个）。
    ///
    /// 行末补一个 `'\n'`：那是"用户按了回车"，正是 `getch()` 循环等的收尾符。
    /// 队列空了也走同一条路（空行）—— 与手机端一致，见类注释。
    /// </summary>
    public char ReadChar()
    {
        if (_cur.Length == 0) _cur = ReadString() + "\n";
        var c = _cur[0];
        _cur = _cur.Substring(1);
        return c;
    }

    public int ReadInt() => int.TryParse(ReadString().Trim(), out var v) ? v : 0;
    public float ReadFloat() => float.TryParse(ReadString().Trim(), out var v) ? v : 0f;
    /// <summary>
    /// "此刻还有没有按键待读" —— `kbhit()` / `nodelay` 模式下 `getch()` 的判据。
    ///
    /// ⚠ 原来的实现是 `_lines is not null`，**恒为真**（只要有输入源就真）。
    /// 那等于告诉程序"永远有键"，而 `read_file` 那条链的下场是：
    /// `conio.kbhit()` 恒返 1 ⇒ `curses` 的 `wgetch` 在 `timeout(0)` 下**从不
    /// 返回 `ERR`** ⇒ 老程序（`cmatrix` / `tty-clock` 那类）主循环里
    /// "没按键就画下一帧"的分支**永远走不到**，第一帧都画不出来就卡在读键上。
    ///
    /// 正确语义是**还有没有未消费的字符**：当前行剩的（`_cur`）或者队列里
    /// 还没取的行（`_lines`）。两处都空了才是"没有键"。
    /// ⚠ 与手机端 `WayCoder.Maui` 的同名实现是一对，改一处要改另一处。
    /// </summary>
    public bool KeyAvailable() => _cur.Length > 0 || _lines is { Count: > 0 };

    /// <summary>
    /// 输入源**已经给不出东西了** ⇒ `true`。两种情形：
    /// ① `--stdin` 的队列空了、当前行的剩余也吐完了；
    /// ② **压根没有输入源**（`_lines == null`）—— 那意味着"以后也不会再有"。
    ///
    /// ⚠ ②原来返回 `false`（注释写的是"交互式就该继续等"），但**桌面 `vmlcli` 没有交互终端**
    /// —— 它是个脚本运行器，问不到用户。把"没有输入源"当成"还没敲"的后果是
    /// `getchar()` **空转到 `TimeoutSeconds`**（实测 5 秒上限下每次读键耗 5 秒、
    /// 最后 `VM execution cancelled`），而它真正的语义是 **EOF**。
    /// 改成"没有源 = 耗尽"之后：stdio 的 `getchar` 立刻拿到 `EOF`（老程序正常收尾），
    /// `conio` 的 `getch` 立刻拿到空行（DOS 的"确认"语义）。两边都对。
    ///
    /// ⚠ **手机端不能照抄这一条**：那边的"没有 `_readLine`"与"用户还没敲"是两件事，
    /// 它有真的交互终端（见 `WayCoder.Maui/Services/MauiVml.cs` 的同名实现）。
    /// </summary>
    public bool InputExhausted =>
        _lines is null || (_cur.Length == 0 && _lines.Count == 0);
}

// 桌面宿主（500–599 号段 + VM 内置的 #57 的真实实现）在 `CliVmlHost.cs`：
//   · `CliVmlHost` —— IVmlHost 的桌面实现（出图 / 脚本化输入 / 对话框 / 存档 / 音效震动）
//   · `CliUiCalls`  —— ISystemCallHandler 的薄壳（把寄存器递进共享运行时）
// 逻辑本身在 `WayCoder/UI/Shared/VmlHostRuntime.cs`，与手机端**编同一份**。
