using System.IO.Compression;
using System.Security.Cryptography;
using System.Text;
using VMLAssembler;
using VMLPlugins;
using VMLPlugins.Interfaces;
using VMLRuntime;
using VMLTool;
using VMLTool.StaticLink;
using WayCoder.Tools;

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
    /// </summary>
    /// <param name="source">内联源码（与 filePath 二选一）</param>
    /// <param name="filePath">**已解析好的**文件路径（调用方负责用 CwdContext 解析，见 VmlTool 注释）</param>
    /// <param name="timeoutSeconds">超时秒数。⚠ 交互式（<paramref name="readLine"/> 非空）要放大，见 RunProgram 注释</param>
    /// <param name="readLine">stdin 输入源；null = 无输入（读到空串）</param>
    public static string Run(string? source, string? filePath, int timeoutSeconds, Func<string>? readLine = null)
    {
        // 给了内联源码 → 一律当 VML 汇编
        if (!string.IsNullOrWhiteSpace(source))
            return RunAssembly(source!, timeoutSeconds, readLine);

        if (string.IsNullOrWhiteSpace(filePath))
            return "⚠️ 需要 `source`（VML 源码）或 `file_path`（文件路径）二者之一。";

        if (!File.Exists(filePath))
            return $"⚠️ 找不到文件：{filePath}";

        if (Path.GetExtension(filePath).Equals(".vml", StringComparison.OrdinalIgnoreCase))
        {
            try { return RunAssembly(File.ReadAllText(filePath), timeoutSeconds, readLine); }
            catch (Exception ex) { return $"⚠️ 读文件失败：{ex.Message}"; }
        }

        return CompileAndRun(filePath, timeoutSeconds, readLine);
    }

    /// <summary>
    /// 汇编并运行一段 VML 源码，返回它写到控制台的东西。
    /// ⚠ 同步阻塞，调用方要自己放后台线程。
    /// </summary>
    public static string RunAssembly(string source, int timeoutSeconds = 10, Func<string>? readLine = null)
        => RunProgram(new VmlAssembler().Assemble(source), timeoutSeconds, readLine);

    /// <summary>
    /// **编译一个高级语言源文件并运行它**（按扩展名自动选编译器，22 种语言）。
    ///
    /// 流水线照抄上游 `VMLTool/Program.Compile.cs:177-212`，**五步一步都不能省** ——
    /// 尤其 <c>LinkLibraries</c>：<c>IFrontendCompiler.Compile(string)</c> 那个纯字符串重载
    /// **不链标准库**，编出来的程序会在运行时找不到 stdlib 函数（实测：一个 Swift 斐波那契示例，
    /// 链库后总指令数 30396，其中 fixed.vml 620 + parserexpf.vml 488 全是这一步带进来的）。
    ///
    /// ⚠ 同步阻塞（前端编译本身就吃 CPU），调用方要自己放后台线程。
    /// </summary>
    public static string CompileAndRun(string filePath, int timeoutSeconds = 30, Func<string>? readLine = null)
    {
        if (!File.Exists(filePath)) return $"⚠️ 找不到文件：{filePath}";

        var libRoot = EnsureLibExtracted();
        if (libRoot == null)
            return "⚠️ VML 标准库（Lib/）解压失败 —— 没有它就编不了高级语言（链接阶段会找不到 stdlib）。";

        // 静态注册 22 个前端编译器，**绕开 PluginManager 的 Assembly.LoadFrom 反射路径**
        // （那条路在 MAUI 的裁剪/AOT 下不可靠，上游自己也在 StaticLink 模式里绕开了它）
        var pm = new PluginManager();
        StaticLinkInitializer.RegisterAll(pm);

        // 扩展名派发用上游现成的 —— 自己遍历 SupportedExtensions 就是第二份实现
        var compiler = pm.GetCompilerByFileName(Path.GetFileName(filePath));
        if (compiler is not IFrontendCompilerEx ex)
            return $"⚠️ 认不出这个扩展名（{Path.GetExtension(filePath)}），没有对应的前端编译器。";

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
        VmlToolConfig? toolConfig = null;
        try { toolConfig = VmlToolConfig.Load(Path.Combine(libRoot, "vmltool.config.xml")); }
        catch { /* 配置读不了就退回空库清单，下面自检会把它亮出来 */ }

        var includePaths = new[] { Path.Combine(libRoot, "Lib") }.Where(Directory.Exists).ToList();
        var libraryPaths = toolConfig?.ResolveLibs(lang, libRoot) ?? [];

        // `LinkLibraries` 第一行就是 `if (libraryPaths.Count == 0) return mainProgram;` ——
        // 空清单等于**静默不链接**。所以库清单为空必须当失败处理，不能让用户拿到一个
        // "编译成功、一跑就找不到函数"的程序。
        if (libraryPaths.Count == 0)
            return "⚠️ 标准库清单为空 —— 多半是 `vmltool.config.xml` 没跟着解压出来（或解压目录不对）。"
                 + "没有它，`LinkLibraries` 会直接跳过整个链接阶段。";

        var vmlText = ex.CompileFileWithIncludes(filePath, includePaths, libraryPaths,
            autoLinkStdLib: true, useSharedLibrary: true);

        // **自检：产物得像 VML 汇编。**
        // 上游那个「失败就静默原样返回」的行为会把所有编译错误伪装成汇编期的
        // 「未知指令」，手机上尤其难查。这里拦一道，把真正的原因亮出来。
        if (string.IsNullOrWhiteSpace(vmlText) || !LooksLikeVml(vmlText))
        {
            var head = vmlText ?? "(空)";
            return $"⚠️ 前端编译没有产出 VML 汇编（{compiler.Name}）—— 多半是标准库/include 路径不对，"
                 + $"或源码本身有语法错误。产物前 200 字符：\n"
                 + head[..Math.Min(200, head.Length)];
        }

        // ② 汇编（.include / .macro 解析基准指向解压出来的根，使 "Lib/..." 能解析）
        var langDefines = new List<string>
        {
            $"VML_{compiler.Name.ToUpperInvariant()}",
            "VML_MODE_MCU",     // 与运行时一致：MCU 模式（见 RunProgram 的安全边界说明）
            "VML_RAM_M",
        };
        var prog = new VmlAssembler().AssembleWithIncludes(vmlText, libRoot, langDefines);

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

        // ⑤ 运行
        return RunProgram(prog, timeoutSeconds, readLine);
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

        return hasAsmMarkers
               && !text.Contains("#include", StringComparison.Ordinal)
               && !text.Contains("#INCLUDE", StringComparison.Ordinal);
    }

    /// <summary>装载并运行一个已汇编的程序，返回它写到控制台的东西（两条路共用这一份）。</summary>
    private static string RunProgram(VmlProgram prog, int timeoutSeconds, Func<string>? readLine = null)
    {
        // **每次跑之前必须重置单例 DeviceManager**：它跨运行保留状态，
        // 不重置的话第二次运行的 MMIO 地址会和第一次串掉（这是 VML 自带测试里的做法）。
        VMLRuntime.Device.DeviceManager.Instance.Reset();

        // ⚠ **`readLine` 非空时，`ReadString()` 会阻塞在 VM 线程上**，而 `TimeoutSeconds` 是
        // 从 `Run()` 起就走的**墙钟** `CancellationTokenSource` —— 也就是说**用户思考的时间
        // 也在扣超时**。等输入久一点，程序就会在提示符上被超时杀掉，而且报的是"超时"、看不出
        // 是在等人。所以**交互式运行必须把 `timeoutSeconds` 放到足够大**（宿主侧另有看门狗兜底，
        // 见 ShellPage），不能沿用非交互那个 10~30 秒的口径。
        var io = new CaptureIo(readLine);

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

        vm.LoadProgram(prog);
        vm.Run();
        return io.Text;
    }

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
    /// 返回**可以作为汇编基准路径的根**（它下面就是 `Lib/`）。已经解压过就直接返回（用一个版本标记判断）。
    ///
    /// 为什么必须解压：`LinkStandardLibrary` 与 `.include` 都是**按文件路径**去找
    /// `Lib/&lt;lang&gt;/…`，而 APK 里的资产不是文件 —— 编译器和汇编器都读不到。
    /// 打包成**一个 zip** 而不是逐文件 MauiAsset：39 MB → 6.1 MB，且 MAUI 没有「列出资产目录」的 API。
    /// </summary>
    private static string? EnsureLibExtracted()
    {
        try
        {
            var root = Path.Combine(WayCoder.Global.Home, "vml");
            var marker = Path.Combine(root, ".lib-version");

            // 印记 = 人类可读的版本 + zip 内容指纹。指纹那半截才是判据（见 LibVersion 注释）。
            var stamp = $"{LibVersion}:{LibFingerprint()}";

            if (File.Exists(marker) && File.ReadAllText(marker).Trim() == stamp
                && Directory.Exists(Path.Combine(root, "Lib")))
                return root;   // 已经解压过且内容一致

            Directory.CreateDirectory(root);

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

            File.WriteAllText(marker, stamp);
            return root;
        }
        catch
        {
            return null;   // 解压失败不该把编译之外的路径也带崩
        }
    }

    private static string? _libFingerprint;

    /// <summary>
    /// 打进来的 `vml_lib.zip` 的内容指纹（懒算一次）。
    /// 6 MB 的 SHA256 在手机上约几十毫秒 —— 每次编译调用一次，但只在首次真算。
    /// </summary>
    private static string LibFingerprint()
    {
        if (_libFingerprint != null) return _libFingerprint;

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

        public CaptureIo(Func<string>? readLine = null) => _readLine = readLine;

        public string Text => _sb.ToString();

        public void WriteString(string str) => _sb.Append(str);
        public void WriteChar(char ch) => _sb.Append(ch);
        public void WriteInt(int value) => _sb.Append(value);
        public void WriteFloat(float value) => _sb.Append(value);
        public void WriteHex(int value) => _sb.Append(value.ToString("X"));

        // 输入侧：全部走同一个 _readLine，按各自的类型转换。
        // 没给输入源时回退成空值/0（AI 工具那条路就是这种，见 VmlTool 的 `stdin` 参数）。
        public string ReadString() => _readLine?.Invoke() ?? "";
        public char ReadChar()
        {
            var line = ReadString();
            return line.Length > 0 ? line[0] : '\0';
        }
        public int ReadInt() => int.TryParse(ReadString().Trim(), out var v) ? v : 0;
        public float ReadFloat() => float.TryParse(ReadString().Trim(), out var v) ? v : 0f;

        // 有输入源就恒为 true：让 `while(!KeyAvailable()); getchar()` 这类轮询能推进。
        // 真正的"还有没有输入"由宿主的输入源自己决定（读完了它自然会给出空行）。
        public bool KeyAvailable() => _readLine != null;
    }
}
