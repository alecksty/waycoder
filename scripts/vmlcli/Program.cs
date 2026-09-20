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
        var prog = new VmlAssembler().AssembleWithIncludes(vmlText, vmlRoot, langDefines);

        // ③ 链接共享库 ④ 应用导出符号
        //
        // ⚠ 这一步与 ② 一起**必须包 try**：有些前端（Pascal / Forth / Ladder / Basic）
        //   不在自己的 `CompileFileWithIncludes` 里链接 —— 链接是在**这里**做的
        //   （`MauiVml` 同样是"编译→汇编→链接"三步，那两步也没包 try，一并记着）。
        //   所以「用户代码调用了不存在的函数」这条编译期错误，对那几门语言是在这一步抛出来的。
        try
        {
            LibraryLinker.LinkLibraries(prog, libraryPaths);
            prog.ApplyExports();
        }
        catch (Exception linkError)
        {
            return (null, lang, $"⚠️ 编译失败：{linkError.Message}");
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

    private static string RunProgram(VmlProgram prog, string sourcePath, CliOptions opt)
    {
        // 每次都重置单例 DeviceManager（跨运行保留状态，不重置第二次跑的 MMIO 地址会和第一次串）。
        VMLRuntime.Device.DeviceManager.Instance.Reset();

        var io = new CaptureIo();

        // **永远只用 "mcu" 模式** —— 这是手机端的安全边界（见 MauiVml 那段长注释：
        // os 模式会放开线程/Socket/mkdir/Exec 等 300–376 号，手机上要么被沙箱挡、要么不该开）。
        using var vm = new VmRuntime(2 * 1024 * 1024, MakeConfig(), [], mode: "mcu")
        {
            TimeoutSeconds = Math.Clamp(opt.TimeoutSeconds, 1, 600),
            ConsoleIO = io,
            SystemCallHandler = new NullUiCalls(),
            // 文件沙箱根 = 源文件所在目录（手机上等价物是 CwdContext.Root = workspace）。
            FileSystemRoot = Path.GetDirectoryName(sourcePath) ?? Directory.GetCurrentDirectory(),
        };

        // 网络：只放客户端那一半，与 MauiVml 逐号相同。
        foreach (var syscall in new[] { 330, 334, 335, 336, 337, 338 })
            vm.HostAllowedSyscalls.Add(syscall);

        vm.LoadProgram(prog);

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
internal sealed class CliOptions
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
    public bool Help { get; private set; }

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
internal sealed class CaptureIo : IConsoleIO
{
    private readonly StringBuilder _sb = new();
    public string Text => _sb.ToString();

    public void WriteString(string str) => _sb.Append(str);
    public void WriteChar(char ch) => _sb.Append(ch);
    public void WriteInt(int value) => _sb.Append(value);
    public void WriteFloat(float value) => _sb.Append(value);
    public void WriteHex(int value) => _sb.Append(value.ToString("X"));

    public string ReadString() => "";
    public char ReadChar() => '\0';
    public int ReadInt() => 0;
    public float ReadFloat() => 0f;
    public bool KeyAvailable() => false;
}

/// <summary>
/// 宿主 syscall 处理器 —— **只为了让 500–599 号段（ui_rect / ui_present / dlg_* …）不报错**。
///
/// <para>
/// 手机端那份是 <c>WayCoder.Maui/Services/VmlUiCalls.cs</c>（真画窗口、真弹对话框）。
/// 桌面 CLI 只需要「**编译产物与运行结果与手机端一致**」，UI 画到哪里无关紧要；
/// 而**不能不做这一层**：运行时的 dispatch 是「先问宿主处理器，再走内置 switch」，没有处理器时
/// 这些号会掉进内置 switch 的 default 分支（或过不了 mcu 的白名单门）并打出
/// `Permission denied: syscall 525 …` —— 那是一行**手机端不会有的输出**，会污染比对。
/// </para>
///
/// <para>
/// 两道手续与手机端逐条相同：① 把 500–599 加进 <c>SyscallConstants.UserAllowed</c>
/// （与运行时内部那个 <c>UserAllowedSyscalls</c> 是**同一个对象引用**，见
/// <c>VMLRuntime/VMLRuntime.cs:179</c>）—— 漏了这步的现象是「处理器注册了却永远不被调用」；
/// ② 处理器对号段返回 true 并置 <c>R0 = 0</c>（这些 syscall 的返回值都是 0）。
/// </para>
/// </summary>
internal sealed class NullUiCalls : ISystemCallHandler
{
    private const int ReservedFirst = 500;
    private const int ReservedLast = 599;

    static NullUiCalls()
    {
        for (int n = ReservedFirst; n <= ReservedLast; n++)
            SyscallConstants.UserAllowed.Add(n);
    }

    /// <summary>`CALLJSON`（#573）—— 桌面端**唯一真正实现**的一个号。</summary>
    private const int CallJsonNum = 573;

    public bool HandleSyscall(int syscallNumber, int[] registers, byte[] memory, ref int pc)
    {
        if (syscallNumber == CallJsonNum) { registers[0] = CallJson(registers, memory); return true; }
        if (syscallNumber is < ReservedFirst or > ReservedLast) return false;
        registers[0] = 0;
        return true;
    }

    /// <summary>
    /// **`CALLJSON`(#573) 的桌面实现**。
    ///
    /// ## 为什么这一个号不能像其他号那样"空着"
    ///
    /// 其余 500–599 是"画到哪儿无所谓"的 UI 号（桌面 CLI 只要编译产物与运行结果一致），
    /// 但 `CALLJSON` 返回的是**数据**，程序拿它做逻辑与输出。空着 = 返回空串 ⇒
    /// `ui_call_json_print()` 一个字节都打不出来。
    ///
    /// 实测（2026-09-20）：`Examples/*/sysinfo.*` **22 门语言全部零输出** ——
    /// 因为每个 `sysinfo.*` 都是同一句 `ui_call_json_s("sysinfo","")` + `ui_call_json_print()`，
    /// 这是 CALLJSON 的自检程序。空着的时候它们"编译成功、运行成功、什么都不打印"，
    /// 极易被读成"程序没问题"（我自己第一轮只数了告警，就没看出来）。
    ///
    /// ## 与手机端的关系
    ///
    /// 信封格式（`{"ok":true,"result":…}`）与手机端 `VmlJsonApi` **同形**，
    /// 但数值来自**桌面环境**，且 `app`/`version` 报的是"桌面脚手架"而不是 `Global.Version`
    /// —— 桌面 CLI 刻意**不引用 WayCoder 核心**（csproj 里写着只引 vendored 的 `third_party/vml`），
    /// 拿不到那个常量。**这是有意为之，不是漏了**：与其抄一个会漂的版本号进来，
    /// 不如如实说"这是桌面脚手架"。
    /// </summary>
    private static int CallJson(int[] r, byte[] mem)
    {
        var fn = Str(mem, r[0]);
        var json = fn == "sysinfo" ? SysinfoJson() : VmlJsonEnvelope.NotFound(fn);
        int n = WriteString(mem, r[2], r[3], json);
        if (n >= 0) return n;
        // 装不下 ⇒ 回一个说明原因的短信封（与手机端同一套两段式退让）
        return WriteString(mem, r[2], r[3],
            VmlJsonEnvelope.TooLong(System.Text.Encoding.UTF8.GetByteCount(json)));
    }

    /// <summary>VM 内存里的 NUL 结尾 C 字符串；指针为 0 或越界时返回空串（与手机端的 `Str` 同语义）。</summary>
    private static string Str(byte[] mem, int ptr)
    {
        if (ptr <= 0 || ptr >= mem.Length) return "";
        int end = ptr;
        while (end < mem.Length && mem[end] != 0) end++;
        return System.Text.Encoding.UTF8.GetString(mem, ptr, end - ptr);
    }

    /// <summary>把 UTF-8 写进 VM 缓冲区，返回**实际需要**的字节数；放不下返回 -1（结尾留一个 NUL）。</summary>
    private static int WriteString(byte[] mem, int dst, int cap, string text)
    {
        if (dst < 0 || cap <= 1 || dst + cap > mem.Length) return -1;
        var bytes = System.Text.Encoding.UTF8.GetBytes(text);
        int n = Math.Min(bytes.Length, cap - 1);
        Array.Copy(bytes, 0, mem, dst, n);
        mem[dst + n] = 0;
        return bytes.Length <= cap - 1 ? n : -1;
    }

    /// <summary>桌面环境的 sysinfo。字段名与手机端逐一对齐（跨语言契约）。</summary>
    private static string SysinfoJson()
        => "{\"ok\":true,\"result\":{" +
           "\"app\":\"WayCoder\"," +
           "\"version\":\"(desktop-cli)\"," +
           "\"platform\":\"desktop\"," +
           "\"os\":\"" + Escape(Environment.OSVersion.Platform.ToString()) + "\"," +
           "\"osVersion\":\"" + Escape(Environment.OSVersion.VersionString) + "\"," +
           "\"deviceModel\":\"(desktop)\",\"deviceName\":\"(desktop)\",\"manufacturer\":\"(desktop)\"," +
           "\"arch\":\"" + System.Runtime.InteropServices.RuntimeInformation.ProcessArchitecture.ToString().ToLowerInvariant() + "\"," +
           "\"cpuCount\":" + Environment.ProcessorCount + "," +
           "\"memoryMb\":" + (GC.GetGCMemoryInfo().TotalAvailableMemoryBytes / (1024 * 1024)) + "," +
           "\"deviceId\":\"(desktop-cli)\"," +
           "\"screen\":{\"w\":0,\"h\":0,\"density\":1,\"canvasW\":0,\"canvasH\":0}," +
           "\"orientation\":0}}";

    /// <summary>JSON 字符串转义（只处理必要字符；这里的值全是我们自己造的，但空值与路径可能带 `\`）。</summary>
    private static string Escape(string s)
        => s.Replace("\\", "\\\\").Replace("\"", "\\\"");
}

/// <summary>桌面端的**信封**（与手机端 `VmlJsonApi` 同构：成功 `ok:true`／失败 `ok:false` + `error`）。</summary>
internal static class VmlJsonEnvelope
{
    public static string NotFound(string fn)
        => "{\"ok\":false,\"error\":\"桌面脚手架未实现该函数：" + fn.Replace("\"", "") + "\"}";

    public static string TooLong(int needed)
        => "{\"ok\":false,\"error\":\"结果太长：需要 " + needed + " 字节，缓冲区装不下\"}";
}
