#nullable disable
using VMLPlugins;
using VMLPlugins.Interfaces;
using VMLAssembler;
using VMLRuntime;
using VMLTranslators;
using VMLToHex.Assemblers;
using CompilerBase;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace VMLTool
{
    public static partial class Program
    {
        /// <summary>
        /// 执行编译操作（新格式）
        /// </summary>
        private static void ExecuteCompile(CommandLineOptions options)
        {
            var files = options.InputFiles.Count > 0 ? options.InputFiles :
                        !string.IsNullOrEmpty(options.InputFile) ? new List<string> { options.InputFile } :
                        new List<string>();

            if (files.Count == 0)
            {
                Console.WriteLine("错误: 未指定输入文件");
                return;
            }

            // 如果输入是 .vml 文件且有目标架构，直接走构建流水线
            if (files.Count == 1 && files[0].EndsWith(".vml", StringComparison.OrdinalIgnoreCase)
                && !string.IsNullOrEmpty(options.TargetArchitecture))
            {
                ExecuteBuildFromVml(files[0], options);
                return;
            }

            var outputFile = options.OutputFile;
            if (string.IsNullOrEmpty(outputFile))
            {
                outputFile = files.Count == 1
                    ? Path.ChangeExtension(files[0], ".vml")
                    : "output.vml";
            }

            try
            {
                // 自动检测语言 (根据文件扩展名)
                var lang = options.Language;
                if (string.IsNullOrEmpty(lang) && files.Count > 0)
                {
                    var ext = Path.GetExtension(files[0]).ToLower();
                    lang = ext switch
                    {
                        ".bas" => "basic", ".c" => "c", ".cpp" => "cpp", ".cs" => "csharp",
                        ".d" => "d", ".dart" => "dart", ".f90" => "fortran", ".fth" => "forth",
                        ".go" => "go", ".java" => "java", ".js" => "javascript", ".kt" => "kotlin",
                        ".ld" => "ladder", ".lua" => "lua", ".m" => "objc", ".py" => "python",
                        ".pas" => "pascal", ".r" => "r", ".rb" => "ruby", ".rs" => "rust",
                        ".scm" => "scheme", ".swift" => "swift", ".vml" => "vml",
                        _ => "default"
                    };
                }
                // 应用 XML 配置文件默认值
                Console.Error.Write("解析配置... ");
                VmlToolConfig.Load(options.ConfigFile).ApplyTo(options, lang);
                Console.Error.WriteLine("完成");

                // 从环境变量读取额外路径 (GCC 兼容)
                var envIncludes = ReadEnvPaths(new[] { "C_INCLUDE_PATH", "CPATH", "VML_INCLUDE_PATH" });
                var envLibs = ReadEnvPaths(new[] { "VML_LIB_PATH", "VML_LIBRARY_PATH", "LIBRARY_PATH" });
                if (envIncludes.Count > 0) options.IncludePaths.AddRange(envIncludes);
                if (envLibs.Count > 0) options.LibraryPaths.AddRange(envLibs);

                // 从 VML_HOME 添加库搜索路径 (优先级 3+4: Lib/<lang>/ 和 Lib/shared/)
                // 注意: 五层优先级为 ./ → -L → VML_HOME/lib/<lang>/ → VML_HOME/lib/shared/ → VML_LIBRARY_PATH
                // -L 路径已在前面处理，VML_HOME 路径追加在 -L 之后
                var vmlHome = Environment.GetEnvironmentVariable("VML_HOME")
                    ?? Environment.GetEnvironmentVariable("VML_TOOL_PATH");
                if (!string.IsNullOrEmpty(vmlHome) && Directory.Exists(vmlHome))
                {
                    var libDir = Path.Combine(vmlHome, "Lib");
                    if (Directory.Exists(libDir) && !options.IncludePaths.Contains(libDir))
                        options.IncludePaths.Add(libDir);
                }

                var allPrograms = new List<VmlProgram>();

                foreach (var inputFile in files)
                {
                    // 增量编译: 检查缓存 (v1.66.32+)
                    if (options.OptimizationLevel == 0 && !options.DebugMode)
                    {
                    }

                    var language = options.Language;
                    IFrontendCompiler compiler = null;

                    // 确定使用哪个编译器
                    if (!string.IsNullOrEmpty(language))
                    {
                        compiler = _pluginManager.GetFrontendCompiler(language);
                        if (compiler == null)
                        {
                            Console.WriteLine($"错误: 不支持的语言 '{language}'");
                            Console.WriteLine("支持的语言:");
                            foreach (var c in _pluginManager.GetAllFrontendCompilers())
                                Console.WriteLine($"  {c.Name}");
                            return;
                        }
                    }
                    else
                    {
                        compiler = _pluginManager.GetCompilerByFileName(inputFile);
                        if (compiler == null)
                        {
                            Console.WriteLine($"错误: 无法确定编译器: {Path.GetExtension(inputFile)} ({inputFile})");
                            Console.WriteLine("请使用 --lang 参数指定语言");
                            return;
                        }
                    }

                    // 通过 SetConfig 设置编译器参数
                    if (compiler is CompilerBase.CompilerBase cb)
                    {
                        cb.SetConfig("defines", options.Defines);
                        cb.SetConfig("undefines", options.Undefines);
                        cb.SetConfig("debug", options.DebugMode);
                        cb.SetConfig("warnings", options.WarningLevel);
                        cb.SetConfig("warningsAsErrors", options.WarningsAsErrors);
                        cb.SetConfig("languageStandard", options.LanguageStandard ?? "");
                        cb.SetConfig("targetMode", options.TargetMode);
                        cb.SetConfig("memoryLevel", options.MemoryLevel);
                        cb.SetConfig("stackSize", options.StackSize ?? 0);
                        cb.SetConfig("float32", options.Float32Mode);
                        cb.SetConfig("float64", options.Float64Mode);
                        cb.SetConfig("int64", options.Int64Mode);
                        cb.SetConfig("sourceComment", options.SourceComment);
                        cb.SetConfig("dumpmode", options.DumpProgress);
                        cb.SetConfig("preparelogmode", options.PrepareLog);
                        cb.SetConfig("dumppreprocess", options.DumpPreprocess);
                        cb.SetConfig("includePaths", options.IncludePaths);
                        cb.SetConfig("libraryPaths", options.LibraryPaths);
                        cb.SetConfig("autoLinkStdLib", options.AutoIncludeStdLib);
                        cb.SetConfig("useSharedLibrary", options.UseSharedLibrary);
                    }

                    var copts = new CompilerOptions
                    {
                        Defines = options.Defines,
                        Undefines = options.Undefines,
                        DebugMode = options.DebugMode,
                        DumpMode = options.DumpProgress,
                        PrepareLogMode = options.PrepareLog,
                        DumpPreprocess = options.DumpPreprocess,
                        WarningLevel = options.WarningLevel,
                        WarningsAsErrors = options.WarningsAsErrors,
                        LanguageStandard = options.LanguageStandard,
                        TargetMode = options.TargetMode,
                        MemoryLevel = options.MemoryLevel,
                        StackSize = options.StackSize ?? 0,
                        Float32Mode = options.Float32Mode,
                        Float64Mode = options.Float64Mode,
                        Int64Mode = options.Int64Mode,
                        SourceComment = options.SourceComment,
                        BasicDialect = ParseBasicDialect(options.BasicType),
                        BasicGraphics = ParseBasicGraphics(options.BasicGfx),
                        PascalDialect = ParsePascalDialect(options.PascalType),
                    };
                    LibraryLinker.DebugOutput = copts.DebugMode || options.DumpCall || options.DumpLink;
                    if (copts.DumpMode) Console.Error.WriteLine("[VMLTool] DumpMode=true 已传递给 CompilerOptions");
                    Console.WriteLine($"[{Path.GetFileName(inputFile)}] 使用编译器: {compiler.Name}" +
                        (copts.Defines.Count > 0 ? $" (-D {string.Join(" ", copts.Defines)})" : "") +
                        (copts.DebugMode ? " (-g)" : ""));
                    var program = RunWithTimeout(() => CompilerOptionsContext.RunWith(copts, () =>
                    {
                        if (compiler is IFrontendCompilerEx ex)
                        {
                            // 自动识别库模式：无 main 函数 → 不链接任何库
                            bool autoLink = options.AutoIncludeStdLib;
                            if (autoLink && !HasMainFunction(inputFile))
                                autoLink = false;
                            var vmlText = ex.CompileFileWithIncludes(inputFile, options.IncludePaths, options.LibraryPaths, autoLink, options.UseSharedLibrary);
                            var asm = new VmlAssembler();
                            // 优先使用 VML 工具根目录（包含 Lib/）作为基准路径，使 .linked  "Lib/..." 能正确解析
                            var basePath = DetectVmlRoot() ?? Directory.GetCurrentDirectory();
                            // 传入语言宏定义 (VML_C, VML_BASIC, ...) 供 #ifdef 条件编译
                            var langDefines = new List<string> { $"VML_{compiler.Name.ToUpper()}" };
                            // 同时添加用户通过 -D 指定的宏定义
                            if (options.Defines != null) langDefines.AddRange(options.Defines);
                            // 根据模式/内存添加预处理宏定义 (全部语言支持)
                            langDefines.Add(options.TargetMode == TargetMode.OS ? "VML_MODE_OS" : "VML_MODE_MCU");
                            langDefines.Add(options.MemoryLevel switch
                            {
                                MemoryLevel.RAM_K => "VML_RAM_K",
                                MemoryLevel.RAM_G => "VML_RAM_G",
                                _ => "VML_RAM_M"
                            });
                            // 根据数值模式添加软浮点/软 int64 宏定义
                            if (options.Float32Mode == Float32Mode.Soft) langDefines.Add("VML_FLOAT32_SOFT");
                            if (options.Float64Mode == Float64Mode.Soft) langDefines.Add("VML_FLOAT64_SOFT");
                            if (options.Int64Mode == Int64Mode.Soft) langDefines.Add("VML_INT64_SOFT");
                            var prog = asm.AssembleWithIncludes(vmlText, basePath, langDefines);
                            // v1.66.51+: AutoDetectSharedLibs 已移除，依赖由 builtin.vml 链解析
                            var libPaths = new List<string>();
                            if (options.LibraryPaths != null) libPaths.AddRange(options.LibraryPaths);
                            if (libPaths.Count > 0)
                                VMLAssembler.LibraryLinker.LinkLibraries(prog, libPaths);
                            prog.ApplyExports();
                            return prog;
                        }
                        return compiler.CompileFile(inputFile, options.IncludePaths, options.LibraryPaths);
                    }), options.TimeoutSeconds, $"编译 {Path.GetFileName(inputFile)}");
                    allPrograms.Add(program);

                }

                // 合并多文件程序
                var mergedProgram = allPrograms.Count == 1 ? allPrograms[0] : MergePrograms(allPrograms);

                // v1.66.55+: 共享库使用裸名，BareCNameMap 已移除，CALL 标签直接匹配
                var libPaths = new List<string>(options.LibraryPaths ?? new());

                // 运行优化流水线
                var prog = mergedProgram;
                if (options.OptimizationLevel > 0)
                {
                    var optOptions = new OptimizationOptions
                    {
                        OptimizationLevel = options.OptimizationLevel,
                        EnableNopElimination = options.OptimizationLevel >= 1,
                        EnableConstantFolding = false,  // 实验性
                        EnableJumpChaining = false,  // 实验性, 有标签损坏 bug
                        EnableDeadCodeElimination = false,  // 实验性, 链接库程序误删除代码
                        EnableDeadStoreElimination = false,  // O2 有标签丢失 bug, 暂时禁用
                        EnableCopyPropagation = false,     // O2 有标签丢失 bug, 暂时禁用
                        EnablePeepholeOptimization = false  // O2+ 实验性, 有输出损坏 bug
                    };
                    Console.WriteLine($"优化级别: O{options.OptimizationLevel}");
                    var pipeline = OptimizationPipeline.CreateDefault();
                    var beforeCount = prog.Instructions.Count;
                    prog = pipeline.Run(prog, optOptions);
                    var afterCount = prog.Instructions.Count;
                    Console.WriteLine($"优化完成: {beforeCount} -> {afterCount} 条指令 (减少 {beforeCount - afterCount} 条)");
                }

                // 根据输出格式决定后续流水线：
                //   .vml        → 编译到 VML 文本 → 停止
                //   .vmb        → 编译到 VML 二进制 → 停止
                //   .exe/.bat   → 编译到 VML → 打包自解压可执行文件 → 停止
                //   .s/.asm     → 编译 → 转译到目标架构汇编 → 停止
                //   .hex/…      → 编译 → 转译 → 汇编 → 输出目标格式 → 停止

                var outExt = Path.GetExtension(options.OutputFile ?? "").ToLowerInvariant();

                if (!string.IsNullOrEmpty(options.TargetArchitecture) && outExt is ".s" or ".asm")
                {
                    // 流水线 4: 编译 → 转译到目标汇编
                    ExecuteBuild(prog, options, outputFile);
                }
                else if (!string.IsNullOrEmpty(options.TargetArchitecture))
                {
                    // 流水线 5: 编译 → 转译 → 汇编 → 输出目标格式
                    ExecuteBuild(prog, options, outputFile);
                }
                else if (outExt == ".vmb")
                {
                    // 流水线 2: 编译 → VMB 二进制
                    var vmbBytes = prog.ToVmbBytes();
                    File.WriteAllBytes(outputFile, vmbBytes);
                    Console.WriteLine($"VMB 输出: {outputFile} ({vmbBytes.Length} 字节)");
                }
                else if (options.CreateExe)
                {
                    // 流水线 3: 编译 → VMB → 打包独立可执行文件
                    var exeOutput = options.OutputFile ?? Path.ChangeExtension(files[0], ".exe");
                    CreateExecutable(prog, exeOutput, options.ExeRuntime);
                }
                else
                {
                    // 流水线 1: 编译 → VML 文本 (默认)
                    var vmlText = prog.ToString();
                    File.WriteAllText(outputFile, vmlText);
                    Console.WriteLine($"编译完成: {outputFile} ({prog.Instructions.Count} 条指令)");
                }

                // 如果需要 dump，输出详细指令文件
                if (!string.IsNullOrEmpty(options.DumpFile))
                    DumpProgram(prog, options.DumpFile);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"编译错误: {ex.Message}");
                if (ex.InnerException != null)
                {
                    Console.WriteLine($"内部错误: {ex.InnerException.Message}");
                }
            }
        }

        /// <summary>
        /// 从 .vml 文件直接构建 HEX/ELF/BIN
        /// </summary>
        private static void ExecuteBuildFromVml(string vmlFile, CommandLineOptions options)
        {
            try
            {
                // 读取并汇编 VML
                var source = File.ReadAllText(vmlFile);

                // 自动链接 VML-level SYSCALL helpers
                var vmlHelpers = FindVmlHelpers();
                if (vmlHelpers != null)
                    source = File.ReadAllText(vmlHelpers) + "\n" + source;

                var assembler = new VmlAssembler();
                var prog = assembler.Assemble(source);

                // 优化
                if (options.OptimizationLevel > 0)
                {
                    var before = prog.Instructions.Count;
                    var opts = new OptimizationOptions
                    {
                        OptimizationLevel = options.OptimizationLevel,
                        EnableNopElimination = true,
                        EnableConstantFolding = false,  // 实验性
                        EnableJumpChaining = true,
                        EnableDeadCodeElimination = false,  // 实验性, 链接库程序误删除代码
                        EnableDeadStoreElimination = false,  // O2 有标签丢失 bug, 暂时禁用
                        EnableCopyPropagation = false,     // O2 有标签丢失 bug, 暂时禁用
                    };
                    prog = OptimizationPipeline.CreateDefault().Run(prog, opts);
                    var after = prog.Instructions.Count;
                    if (before != after)
                        Console.WriteLine($"优化: {before} → {after} 条指令");
                }

                ExecuteBuild(prog, options, vmlFile);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"构建错误: {ex.Message}");
            }
        }

        /// <summary>
        /// 自动检测 VML 根目录（包含 Lib/ 的目录）。
        /// 优先级: $VML_HOME → $VML_TOOL_PATH → 可执行文件位置向上搜索
        /// </summary>
        private static string? DetectVmlRoot()
        {
            // 1. 环境变量 VML_HOME（用户显式设置）
            var vmlHome = Environment.GetEnvironmentVariable("VML_HOME");
            if (!string.IsNullOrEmpty(vmlHome) && Directory.Exists(Path.Combine(vmlHome, "Lib")))
                return vmlHome;

            // 2. 环境变量 VML_TOOL_PATH（兼容旧名）
            vmlHome = Environment.GetEnvironmentVariable("VML_TOOL_PATH");
            if (!string.IsNullOrEmpty(vmlHome) && Directory.Exists(Path.Combine(vmlHome, "Lib")))
                return vmlHome;

            // 3. 从可执行文件位置向上搜索
            var dir = AppContext.BaseDirectory;
            for (int i = 0; i < 8; i++)
            {
                if (Directory.Exists(Path.Combine(dir, "Lib")))
                    return dir;
                var parent = Directory.GetParent(dir);
                if (parent == null) break;
                dir = parent.FullName;
            }
            return null;
        }

        /// <summary>
        /// 检测源文件是否包含入口函数（C: main, BASIC: main, Pascal: main 等）
        /// 无入口函数视为库文件，不自动链接 builtin
        /// </summary>
        private static bool HasMainFunction(string filePath)
        {
            try
            {
                var ext = Path.GetExtension(filePath).ToLower();
                // 这些语言使用隐式入口点(顶层代码), 始终自动链接
                if (ext is ".fth" or ".swift" or ".lua" or ".ld" or ".f90"
                         or ".bas" or ".py" or ".js" or ".pas" or ".rb"
                         or ".r" or ".dart" or ".kt" or ".scm" or ".m") return true;
                var content = File.ReadAllText(filePath);
                // C/C++/Java/C#/D/Go/Rust 等: main() 或 Main()
                if (System.Text.RegularExpressions.Regex.IsMatch(content, @"\b[Mm]ain\s*\(")) return true;
                // Forth: : main 或 : start
                if (ext == ".fth" && System.Text.RegularExpressions.Regex.IsMatch(content, @":\s+(main|start)\b")) return true;
                // Swift: func main
                if (ext == ".swift" && System.Text.RegularExpressions.Regex.IsMatch(content, @"func\s+main\b")) return true;
                return false;
            }
            catch { return false; }
        }

        /// <summary>
        /// 查找 VML SYSCALL helpers 文件
        /// </summary>
        private static string? FindVmlHelpers()
        {
            var root = DetectVmlRoot() ?? AppContext.BaseDirectory;
            for (int i = 0; i < 6; i++)
            {
                var probe = Path.Combine(root, "Lib", "vml", "Bios", "syscall_helpers.vml");
                if (File.Exists(probe)) return probe;
                var parent = Directory.GetParent(root);
                if (parent == null) break;
                root = parent.FullName;
            }
            return null;
        }

        /// <summary>
        /// 执行构建操作：编译→翻译→汇编→输出 (hex/elf/bin/exe)
        /// </summary>
        private static void ExecuteBuild(VmlProgram prog, CommandLineOptions options, string vmlOutputFile)
        {
            var arch = options.TargetArchitecture!;
            var format = options.OutputFormat ?? "hex";

            try
            {
                // 步骤 1: 翻译 VML → 目标汇编
                Console.WriteLine($"翻译: VML -> {arch.ToUpper()} 汇编");
                VMLTranslators.BaseTranslator translator;
                try
                {
                    translator = VMLTranslators.TranslatorFactory.GetTranslator(arch, prog);
                }
                catch (ArgumentException)
                {
                    Console.WriteLine($"错误: 不支持的架构 '{arch}'");
                    return;
                }
                var result = translator.Translate();
                var code = result.Code;

                // 输出目标汇编文件（-S / --asm）
                if (!string.IsNullOrEmpty(options.AsmOutput))
                {
                    File.WriteAllText(options.AsmOutput, code);
                    Console.WriteLine($"汇编输出: {options.AsmOutput}");
                }

                // 如果用户指定 .s/.asm 为最终输出（-o file.s），在此停下
                // 如果 AsmOutput 和 OutputFile 不同（如 -o file.hex -S file.s），则继续
                if (!string.IsNullOrEmpty(options.AsmOutput)
                    && string.Equals(options.AsmOutput, options.OutputFile, StringComparison.OrdinalIgnoreCase))
                    return;

                // 保存中间汇编（-save-temps）
                if (options.SaveTemps)
                {
                    var asmFile = Path.ChangeExtension(vmlOutputFile, $".{arch}.s");
                    File.WriteAllText(asmFile, code);
                    Console.WriteLine($"中间汇编: {asmFile}");
                }

                // 步骤 2: 自动链接 BIOS
                var biosFile = FindBiosFile(arch);
                if (biosFile != null)
                {
                    Console.WriteLine($"BIOS: {Path.GetFileName(biosFile)}");
                    code = File.ReadAllText(biosFile) + "\n" + code;
                }

                // 步骤 3: 汇编 → 机器码
                Console.WriteLine($"汇编: {arch.ToUpper()} -> {format.ToUpper()}");
                var assembler = AssemblerFactory.Create(arch);
                var data = assembler.Assemble(code, out int baseAddr, out string archName);

                if (data == null || data.Length == 0)
                {
                    Console.WriteLine("错误: 汇编输出为空");
                    return;
                }

                // 步骤 4: 输出格式化
                var outputFile = options.OutputFile;
                if (string.IsNullOrEmpty(outputFile))
                {
                    outputFile = format switch
                    {
                        "hex" => Path.ChangeExtension(vmlOutputFile, ".hex"),
                        "elf" => Path.ChangeExtension(vmlOutputFile, ".elf"),
                        "exe" => Path.ChangeExtension(vmlOutputFile, ".exe"),
                        "com" => Path.ChangeExtension(vmlOutputFile, ".com"),
                        "bin" => Path.ChangeExtension(vmlOutputFile, ".bin"),
                        "s19" => Path.ChangeExtension(vmlOutputFile, ".s19"),
                        "dump" => Path.ChangeExtension(vmlOutputFile, ".dump"),
                        _ => Path.ChangeExtension(vmlOutputFile, $".{format}"),
                    };
                }

                OutputFormat.Write(format, data, baseAddr, outputFile);
                Console.WriteLine($"构建完成: {outputFile} ({data.Length} 字节)");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"构建错误: {ex.Message}");
                if (ex.InnerException != null)
                    Console.WriteLine($"内部错误: {ex.InnerException.Message}");
            }
        }

        /// <summary>
        /// 查找架构对应的 BIOS 汇编文件
        /// </summary>
        private static string? FindBiosFile(string arch)
        {
            var root = DetectVmlRoot() ?? AppContext.BaseDirectory;
            for (int i = 0; i < 6; i++)
            {
                var probe = Path.Combine(root, "Lib", "vml", "Bios");
                if (Directory.Exists(probe))
                {
                    var biosName = arch.ToLower() switch
                    {
                        "avr" => "avr_bios.s",
                        "8051" => "mcs51_bios.asm",
                        "arm-cm" or "arm" => "arm_cm_bios.s",
                        "pic" => "pic_bios.s",
                        "pic24" => "pic24_bios.s",
                        "msp430" => "msp430_bios.s",
                        "6502" => "mcs6502_bios.s",
                        "z80" => "z80_bios.s",
                        "x86" or "8086" => "x86_bios.s",
                        "riscv" or "risc-v" => "riscv_bios.s",
                        "mips" => "mips_bios.s",
                        "68000" or "m68k" => "m68k_bios.s",
                        "powerpc" or "ppc" => "ppc_bios.s",
                        "sparc" => "sparc_bios.s",
                        _ => null
                    };
                    if (biosName != null)
                    {
                        var biosFile = Path.Combine(probe, biosName);
                        if (File.Exists(biosFile)) return biosFile;
                    }
                    return null;
                }
                var parent = Directory.GetParent(root);
                if (parent == null) break;
                root = parent.FullName;
            }
            return null;
        }

        /// <summary>
        /// 根据 -o 输出文件扩展名自动推断编译流水线
        /// </summary>
        private static void AutoDetectByExtension(CommandLineOptions options)
        {
            var outFile = options.OutputFile ?? "";
            if (string.IsNullOrEmpty(outFile)) return;

            var fn = Path.GetFileName(outFile);
            var parts = fn.Split('.');
            if (parts.Length < 2) return;

            var lastExt = "." + parts[parts.Length - 1].ToLowerInvariant();

            // 架构名列表
            var archs = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
            { "x86","arm-cm","avr","8051","riscv","mips","6502","z80","pic","msp430","pic24",
              "68000","powerpc","sparc","jvm","dotnet" };

            // 检测多段扩展名: start.riscv.hex → arch=riscv, type=hex
            var arch = "";
            if (parts.Length >= 3 && archs.Contains(parts[parts.Length - 2]))
            {
                arch = parts[parts.Length - 2];
            }

            switch (lastExt)
            {
                case ".vml":
                    if (!options.LinkOnly) options.CompileOnly = true;
                    break;
                case ".vmb":
                    if (!options.LinkOnly) options.CompileOnly = true;
                    break;
                case ".s":
                case ".asm":
                    options.CompileOnly = true;
                    if (string.IsNullOrEmpty(options.TargetArchitecture))
                        options.TargetArchitecture = string.IsNullOrEmpty(arch) ? "x86" : arch;
                    options.AsmOutput = outFile;
                    break;
                case ".hex":
                case ".bin":
                case ".com":
                case ".s19":
                case ".elf":
                    options.CompileOnly = true;
                    if (string.IsNullOrEmpty(options.TargetArchitecture))
                        options.TargetArchitecture = string.IsNullOrEmpty(arch) ? "x86" : arch;
                    options.OutputFormat = lastExt.TrimStart('.');
                    break;
                case ".exe":
                case ".bat":
                    options.CreateExe = true;
                    break;
            }
        }

        /// <summary>
        /// 将编译好的 VML 程序打包为独立可执行文件
        /// 双路径策略:
        ///   快速路径: 复制 vmlrun 原生二进制 + 尾部附加 VMB (当前平台,毫秒级)
        ///   跨平台路径: dotnet publish VMLPacker 生成目标平台 .exe (需要 --runtime)
        /// 支持 PE (Windows)、ELF (Linux)、Mach-O (macOS)
        /// </summary>
        private static void CreateExecutable(VmlProgram program, string outputPath, string? targetRuntime = null)
        {
            try
            {
                Console.WriteLine($"打包中 -> {outputPath}");

                // 1. 编译为 VMB 二进制
                var vmbBytes = program.ToVmbBytes();
                if (vmbBytes == null || vmbBytes.Length == 0)
                {
                    Console.WriteLine("打包错误: VMB 编译结果为空");
                    return;
                }

                // 2. 检测当前平台 RID
                var currentRid = GetCurrentRuntimeId();
                var targetRid = string.IsNullOrEmpty(targetRuntime) ? currentRid : targetRuntime;

                // 3. vmlrun 快速路径: 仅对小于 64KB 的非链接程序使用
                //    大程序(自动链接)用 dotnet publish, 标签解析更完整
                if ((targetRid == currentRid || string.IsNullOrEmpty(targetRuntime)) && vmbBytes.Length < 65536)
                {
                    var vmlrunPath = FindVmlRunBinary(targetRid);
                    if (vmlrunPath != null)
                    {
                        CreateWithVmlRun(vmlrunPath, vmbBytes, outputPath);
                        return;
                    }
                }

                // 4. 跨平台或回退路径: 使用 VMLPacker + dotnet publish
                CreateWithDotNetPublish(program, vmbBytes, outputPath, targetRid);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"打包错误: {ex.Message}");
            }
        }

        /// <summary>
        /// 快速路径: 复制 vmlrun 二进制 + 尾部附加 VMB 数据
        /// </summary>
        private static void CreateWithVmlRun(string vmlrunPath, byte[] vmbBytes, string outputPath)
        {
            var dir = Path.GetDirectoryName(outputPath);
            if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
                Directory.CreateDirectory(dir);
            File.Copy(vmlrunPath, outputPath, true);

            // 尾部附加: [VMB数据] + [4字节size LE] + [8字节魔数]
            using (var fs = new FileStream(outputPath, FileMode.Append, FileAccess.Write))
            {
                fs.Write(vmbBytes, 0, vmbBytes.Length);
                var sizeBytes = BitConverter.GetBytes((uint)vmbBytes.Length);
                fs.Write(sizeBytes, 0, 4);
                var magic = new byte[] { (byte)'V', (byte)'M', (byte)'B', (byte)'E', (byte)'X', (byte)'E', 0x01, 0x00 };
                fs.Write(magic, 0, 8);
            }

            // Unix 平台设置可执行权限
            if (!System.Runtime.InteropServices.RuntimeInformation.IsOSPlatform(
                System.Runtime.InteropServices.OSPlatform.Windows))
            {
                File.SetUnixFileMode(outputPath,
                    UnixFileMode.UserRead | UnixFileMode.UserWrite | UnixFileMode.UserExecute |
                    UnixFileMode.GroupRead | UnixFileMode.GroupExecute |
                    UnixFileMode.OtherRead | UnixFileMode.OtherExecute);
            }

            Console.WriteLine($"打包完成: {outputPath} ({new FileInfo(outputPath).Length:#,0} 字节, VMB {vmbBytes.Length:#,0} 字节) [vmlrun]");
        }

        /// <summary>
        /// 跨平台路径: 使用 VMLPacker + dotnet publish 生成目标平台独立可执行文件
        /// </summary>
        private static void CreateWithDotNetPublish(VmlProgram program, byte[] vmbBytes, string outputPath, string targetRid)
        {
            Console.WriteLine($"  目标平台: {targetRid}，使用 dotnet publish...");

            // 1. 定位 VMLPacker 项目
            var prjDir = FindVMLPackerProject();
            if (prjDir == null)
            {
                Console.WriteLine("打包错误: 未找到 VMLPacker 项目，无法跨平台打包");
                Console.WriteLine("  请确保 tools/VMLPacker/VMLPacker.csproj 存在");
                return;
            }

            // 2. 写入 VML 源码作为嵌入式资源
            var vmlSource = program.ToVmlTextWithIncludes();
            var mainVmlPath = Path.Combine(prjDir, "main.vml");
            File.WriteAllText(mainVmlPath, vmlSource);

            // 3. dotnet publish (安全: 异步读取 stdout/stderr 避免死锁)
            var publishDir = Path.Combine(Path.GetTempPath(), "vml_exe_" + Guid.NewGuid().ToString("N")[..8]);
            var csproj = Path.Combine(prjDir, "VMLPacker.csproj");
            var stderrOutput = new System.Text.StringBuilder();
            var stdoutOutput = new System.Text.StringBuilder();

            try
            {
                var psi = new System.Diagnostics.ProcessStartInfo
                {
                    FileName = "dotnet",
                    Arguments = $"publish \"{csproj}\" -c Release -r {targetRid} -o \"{publishDir}\" --self-contained true " +
                               "-p:PublishSingleFile=true -p:IncludeNativeLibrariesForSelfExtract=true " +
                               "-p:PublishTrimmed=true -p:TrimMode=partial -p:DebugType=None -p:DebugSymbols=false",
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    CreateNoWindow = true
                };

                using var proc = System.Diagnostics.Process.Start(psi)!;
                // 异步读取 stdout/stderr 避免管道缓冲区满导致死锁
                proc.OutputDataReceived += (_, e) => { if (e.Data != null) stdoutOutput.AppendLine(e.Data); };
                proc.ErrorDataReceived += (_, e) => { if (e.Data != null) stderrOutput.AppendLine(e.Data); };
                proc.BeginOutputReadLine();
                proc.BeginErrorReadLine();

                // 超时 300 秒
                const int timeoutMs = 300_000;
                if (!proc.WaitForExit(timeoutMs))
                {
                    try { proc.Kill(entireProcessTree: true); } catch { }
                    Console.WriteLine($"打包错误: dotnet publish 超时 ({timeoutMs / 1000}秒)");
                    return;
                }

                if (proc.ExitCode != 0)
                {
                    Console.WriteLine($"打包错误: dotnet publish 失败 (退出码 {proc.ExitCode}): {stderrOutput}");
                    return;
                }
            }
            catch (System.ComponentModel.Win32Exception)
            {
                Console.WriteLine("打包错误: 未找到 dotnet 命令，请安装 .NET SDK");
                Console.WriteLine("  下载: https://dotnet.microsoft.com/download");
                return;
            }

            // 4. 复制生成的 EXE 到输出路径
            var exeName = targetRid.StartsWith("win") ? "VMLPacker.exe" : "VMLPacker";
            var publishedExe = Path.Combine(publishDir, exeName);
            if (File.Exists(publishedExe))
            {
                var dir = Path.GetDirectoryName(outputPath);
                if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
                    Directory.CreateDirectory(dir);
                File.Copy(publishedExe, outputPath, true);

                if (!targetRid.StartsWith("win") && !OperatingSystem.IsWindows())
                {
                    File.SetUnixFileMode(outputPath,
                        UnixFileMode.UserRead | UnixFileMode.UserWrite | UnixFileMode.UserExecute |
                        UnixFileMode.GroupRead | UnixFileMode.GroupExecute |
                        UnixFileMode.OtherRead | UnixFileMode.OtherExecute);
                }

                Console.WriteLine($"打包完成: {outputPath} ({new FileInfo(outputPath).Length:#,0} 字节) [dotnet/{targetRid}]");
            }
            else
            {
                Console.WriteLine("打包错误: dotnet publish 未生成预期输出");
            }

            // 5. 清理临时发布目录（无论成功或失败）
            try { Directory.Delete(publishDir, true); } catch { }
        }

        /// <summary>
        /// 检测当前平台的 .NET RID
        /// </summary>
        private static string GetCurrentRuntimeId()
        {
            var arch = System.Runtime.InteropServices.RuntimeInformation.ProcessArchitecture;
            if (System.Runtime.InteropServices.RuntimeInformation.IsOSPlatform(
                System.Runtime.InteropServices.OSPlatform.Windows))
                return arch == System.Runtime.InteropServices.Architecture.Arm64 ? "win-arm64" : "win-x64";
            if (System.Runtime.InteropServices.RuntimeInformation.IsOSPlatform(
                System.Runtime.InteropServices.OSPlatform.OSX))
                return arch == System.Runtime.InteropServices.Architecture.Arm64 ? "osx-arm64" : "osx-x64";
            return arch == System.Runtime.InteropServices.Architecture.Arm64 ? "linux-arm64" : "linux-x64";
        }

        /// <summary>
        /// 查找指定平台的 vmlrun 原生二进制文件
        /// </summary>
        private static string? FindVmlRunBinary(string targetRid)
        {
            var baseDir = DetectVmlRoot() ?? AppContext.BaseDirectory;
            var exeName = targetRid.StartsWith("win") ? "vmlrun.exe" : "vmlrun";

            // 1. 同目录 (bin/<rid>/)
            var path = Path.Combine(baseDir, exeName);
            if (File.Exists(path)) return path;

            // 2. 平台子目录 (如 vmltool 在 bin/ 下，vmlrun 在 bin/<rid>/)
            var ridDir = Path.Combine(baseDir, targetRid);
            path = Path.Combine(ridDir, exeName);
            if (File.Exists(path)) return path;

            // 3. 当前工作目录
            path = Path.Combine(Directory.GetCurrentDirectory(), exeName);
            if (File.Exists(path)) return path;

            // 4. PATH 环境变量
            var envPath = Environment.GetEnvironmentVariable("PATH");
            if (!string.IsNullOrEmpty(envPath))
            {
                foreach (var d in envPath.Split(Path.PathSeparator))
                {
                    var candidate = Path.Combine(d.Trim(), exeName);
                    if (File.Exists(candidate)) return candidate;
                }
            }

            return null;
        }

        /// <summary>
        /// 查找 VMLPacker 项目目录
        /// </summary>
        private static string? FindVMLPackerProject()
        {
            var baseDir = DetectVmlRoot() ?? AppContext.BaseDirectory;

            // 开发模式: baseDir = VMLTool/bin/Debug/net10.0/
            var devProjDir = Path.GetFullPath(Path.Combine(baseDir, "..", "..", "..", "..", "tools", "VMLPacker"));
            if (File.Exists(Path.Combine(devProjDir, "VMLPacker.csproj")))
                return devProjDir;

            // 发布模式: 从 baseDir 往上找
            var probe = baseDir;
            for (int i = 0; i < 6; i++)
            {
                var candidate = Path.Combine(probe, "tools", "VMLPacker");
                if (File.Exists(Path.Combine(candidate, "VMLPacker.csproj")))
                    return candidate;
                candidate = Path.Combine(probe, "VMLPacker");
                if (File.Exists(Path.Combine(candidate, "VMLPacker.csproj")))
                    return candidate;
                var parent = Directory.GetParent(probe);
                if (parent == null) break;
                probe = parent.FullName;
            }

            return null;
        }

        private static void DumpProgram(VmlProgram program, string dumpPath)
        {
            var sb = new StringBuilder();
            sb.AppendLine("=" .PadRight(60, '='));
            sb.AppendLine("VML Program Dump");
            sb.AppendLine("=" .PadRight(60, '='));
            sb.AppendLine($"Entry Point: {program.EntryPoint}");
            sb.AppendLine($"Stack Top:   {program.StackTop}");
            sb.AppendLine($"Vector Tbl:  0x{program.VectorTable:X}");
            sb.AppendLine($"Instructions: {program.Instructions.Count}");
            sb.AppendLine($"Labels:       {program.Labels.Count}");
            sb.AppendLine($"Data Items:   {program.DataSection.Count}");
            sb.AppendLine($"Constants:    {program.Constants.Count}");
            sb.AppendLine();

            // 指令统计
            var opCounts = new Dictionary<string, int>();
            foreach (var instr in program.Instructions)
            {
                var name = instr.Opcode.ToString();
                opCounts[name] = opCounts.GetValueOrDefault(name) + 1;
            }
            sb.AppendLine("--- Instruction Counts ---");
            foreach (var kv in opCounts.OrderByDescending(kv => kv.Value))
                sb.AppendLine($"  {kv.Key,-20} {kv.Value,4}");
            sb.AppendLine();

            // 标签列表
            sb.AppendLine("--- Labels ---");
            foreach (var kv in program.Labels.OrderBy(kv => kv.Value))
                sb.AppendLine($"  {kv.Key,-30} @{kv.Value,4}");
            sb.AppendLine();

            // 数据段
            sb.AppendLine("--- Data Section ---");
            foreach (var kv in program.DataSection)
                sb.AppendLine($"  {kv.Key,-20} = {kv.Value}");
            sb.AppendLine();

            // 详细指令列表
            sb.AppendLine("--- Instructions ---");
            var addrLabels = new Dictionary<int, List<string>>();
            foreach (var kv in program.Labels)
            {
                if (!addrLabels.ContainsKey(kv.Value))
                    addrLabels[kv.Value] = new List<string>();
                addrLabels[kv.Value].Add(kv.Key);
            }
            for (int i = 0; i < program.Instructions.Count; i++)
            {
                var addr = i;
                if (addrLabels.ContainsKey(addr))
                    foreach (var lbl in addrLabels[addr])
                        sb.AppendLine($"  {lbl}:");

                var instr = program.Instructions[i];
                sb.AppendLine($"  [{addr,4}] {instr.Opcode,-12} {string.Join(", ", instr.Operands.Select(o => FormatOperand(o)))}");
            }
            sb.AppendLine("=" .PadRight(60, '='));

            File.WriteAllText(dumpPath, sb.ToString());
            Console.WriteLine($"Dump saved: {dumpPath}");
        }

        private static string FormatOperand(Operand op)
        {
            if (op.Type == OperandType.REGISTER) return $"R{op.Value}";
            if (op.Type == OperandType.IMMEDIATE) return $"#{op.Value}";
            if (op.Type == OperandType.MEMORY) return $"[{op.Value}]";
            if (op.Type == OperandType.LABEL) return op.Value?.ToString() ?? "?";
            if (op.Type == OperandType.INDIRECT) return $"@{op.Value}";
            return op.Value?.ToString() ?? "?";
        }

        private static VmlProgram MergePrograms(List<VmlProgram> programs)
        {
            var allInstructions = new List<Instruction>();
            var allLabels = new Dictionary<string, int>();
            var allData = new Dictionary<string, object>();
            var allConstants = new Dictionary<string, object>();
            int offset = 0;

            foreach (var p in programs)
            {
                // 添加指令并偏移标签
                foreach (var instr in p.Instructions)
                {
                    allInstructions.Add(instr);
                }

                // 合并标签（偏移地址）
                foreach (var kv in p.Labels)
                {
                    if (!allLabels.ContainsKey(kv.Key))
                        allLabels[kv.Key] = kv.Value + offset;
                }

                // 合并数据段
                foreach (var kv in p.DataSection)
                {
                    if (!allData.ContainsKey(kv.Key))
                        allData[kv.Key] = kv.Value;
                }

                // 合并常量
                foreach (var kv in p.Constants)
                {
                    if (!allConstants.ContainsKey(kv.Key))
                        allConstants[kv.Key] = kv.Value;
                }

                offset = allInstructions.Count;
            }

            return new VmlProgram(allInstructions, allLabels, allData, allConstants)
            {
                EntryPoint = programs.FirstOrDefault()?.EntryPoint ?? "main",
                StackTop = programs.FirstOrDefault()?.StackTop ?? 1048576,
                VectorTable = programs.FirstOrDefault()?.VectorTable ?? 0,
            };
        }

        /// <summary>从环境变量读取路径列表 (GCC 兼容: C_INCLUDE_PATH, LIBRARY_PATH 等)</summary>
        private static List<string> ReadEnvPaths(string[] varNames)
        {
            var result = new List<string>();
            foreach (var name in varNames)
            {
                var val = Environment.GetEnvironmentVariable(name);
                if (!string.IsNullOrEmpty(val))
                {
                    var sep = Environment.OSVersion.Platform == PlatformID.Win32NT ? ';' : ':';
                    foreach (var p in val.Split(sep, StringSplitOptions.RemoveEmptyEntries))
                    {
                        var trimmed = p.Trim();
                        if (!string.IsNullOrEmpty(trimmed) && Directory.Exists(trimmed))
                            result.Add(trimmed);
                    }
                }
            }
            return result;
        }

        private static VMLPlugins.PascalDialect ParsePascalDialect(string? dialect)
    {
        if (string.IsNullOrEmpty(dialect)) return VMLPlugins.PascalDialect.Turbo;
        return dialect.ToLowerInvariant() switch
        {
            "turbo" => VMLPlugins.PascalDialect.Turbo,
            "delphi" => VMLPlugins.PascalDialect.Delphi,
            "freepascal" or "fpc" => VMLPlugins.PascalDialect.FreePascal,
            "iso" or "standard" => VMLPlugins.PascalDialect.IsoPascal,
            "ucsd" => VMLPlugins.PascalDialect.UcsdPascal,
            "oberon" => VMLPlugins.PascalDialect.Oberon,
            _ => VMLPlugins.PascalDialect.Turbo
        };
    }

    private static VMLPlugins.BasicDialect ParseBasicDialect(string? dialect)
        {
            if (string.IsNullOrEmpty(dialect)) return VMLPlugins.BasicDialect.QBasic;
            return dialect.ToLowerInvariant() switch
            {
                "qbasic" => VMLPlugins.BasicDialect.QBasic,
                "turbobasic" => VMLPlugins.BasicDialect.TurboBasic,
                "freebasic" => VMLPlugins.BasicDialect.FreeBasic,
                "truebasic" => VMLPlugins.BasicDialect.TrueBasic,
                "purebasic" => VMLPlugins.BasicDialect.PureBasic,
                "chipbasic" => VMLPlugins.BasicDialect.ChipBasic,
                "minibasic" => VMLPlugins.BasicDialect.MiniBasic,
                "gwbasic" => VMLPlugins.BasicDialect.GwBasic,
                "powerbasic" => VMLPlugins.BasicDialect.PowerBasic,
                "visualbasic" => VMLPlugins.BasicDialect.VisualBasic,
                _ => VMLPlugins.BasicDialect.QBasic
            };
        }

        /// <summary>
        /// BASIC 图形语句的后端：`ui`（默认，走宿主 ui_* 图元）/ `pcgfx`（老的写 DOS 显存）。
        /// 认不出来一律回默认 ui —— 老路在所有宿主上都是空操作（画进虚空），不该是"拼错就走它"。
        /// </summary>
        private static VMLPlugins.BasicGraphics ParseBasicGraphics(string? gfx)
            => string.Equals(gfx, "pcgfx", StringComparison.OrdinalIgnoreCase)
                ? VMLPlugins.BasicGraphics.PcGfx
                : VMLPlugins.BasicGraphics.Ui;

        /// <summary>在指定超时时间内执行操作（防编译器卡死）</summary>
        private static T RunWithTimeout<T>(Func<T> action, int timeoutSeconds, string description)
        {
            if (timeoutSeconds <= 0) return action();
            try
            {
                var task = System.Threading.Tasks.Task.Run(action);
                if (task.Wait(TimeSpan.FromSeconds(timeoutSeconds)))
                    return task.Result;
                throw new TimeoutException($"{description} 超时 ({timeoutSeconds}秒)，已强制终止");
            }
            catch (AggregateException ae) when (ae.InnerException != null)
            {
                throw ae.InnerException;
            }
        }

    }
}
