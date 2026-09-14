#nullable disable
using VMLPlugins.Interfaces;
using VMLAssembler;
using VMLRuntime;
using VMLTranslators;
using System.Collections.Generic;
using System.IO;

namespace VMLTool
{
    public static partial class Program
    {
        /// <summary>
        /// 执行汇编操作（新格式）
        /// </summary>
        private static void ExecuteAssemble(CommandLineOptions options)
        {
            var inputFile = options.InputFile;
            var outputFile = options.OutputFile;

            if (string.IsNullOrEmpty(outputFile))
            {
                // 如果没有指定输出文件，使用默认名称
                outputFile = Path.ChangeExtension(inputFile, ".bin");
            }

            try
            {
                // 获取输入文件的目录作为基础路径
                var basePath = Path.GetDirectoryName(Path.GetFullPath(inputFile));

                var assembler = new VmlAssembler();
                // 使用新的AssembleWithIncludes方法支持.include和.macro
                var program = assembler.AssembleWithIncludes(inputFile, basePath);

                // 保存VML文本文件（简化实现，实际应该保存二进制）
                File.WriteAllText(outputFile, program.ToString());
                Console.WriteLine($"汇编完成: {outputFile}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"汇编错误: {ex.Message}");
            }
        }

        /// <summary>
        /// 汇编VML代码（旧格式）
        /// </summary>
        private static void Assemble(string[] args)
        {
            if (args.Length < 3)
            {
                Console.WriteLine("用法: vmltool assemble/-a <input.vml> <output.bin>");
                return;
            }
            var options = new CommandLineOptions
            {
                InputFile = args[1],
                OutputFile = args[2]
            };
            ExecuteAssemble(options);
        }

        /// <summary>
        /// 执行转译操作（新格式）
        /// </summary>
        private static void ExecuteTranslate(CommandLineOptions options)
        {
            var inputFile = options.InputFile;
            var outputFile = options.OutputFile;
            var architecture = options.TargetArchitecture;

            if (string.IsNullOrEmpty(outputFile))
            {
                // 如果没有指定输出文件，使用默认名称
                outputFile = Path.ChangeExtension(inputFile, $".{architecture}.asm");
            }

            try
            {
                // 读取VML程序
                var source = File.ReadAllText(inputFile);
                var assembler = new VmlAssembler();
                var program = assembler.Assemble(source);

                // 获取翻译器
                var translator = _pluginManager.GetBackendTranslator(architecture);
                if (translator == null)
                {
                    Console.WriteLine($"错误: 不支持的目标架构 '{architecture}'");
                    Console.WriteLine("支持的架构:");
                    foreach (var t in _pluginManager.GetAllBackendTranslators())
                    {
                        Console.WriteLine($"  {t.TargetArchitecture}");
                    }
                    return;
                }

                // 翻译
                var translationOptions = new TranslationOptions();
                var result = translator.Translate(program, translationOptions);

                // 保存转译后的代码
                File.WriteAllText(outputFile, result.Code);
                Console.WriteLine($"转译完成: {outputFile}");
                Console.WriteLine($"统计: {result.Stats.TotalInstructions} 条指令, {result.Stats.GeneratedLines} 行代码, {result.Stats.TranslationTimeMs}ms");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"转译错误: {ex.Message}");
            }
        }

        /// <summary>
        /// 转译到目标架构（旧格式）
        /// </summary>
        private static void Translate(string[] args)
        {
            if (args.Length < 4)
            {
                Console.WriteLine("用法: vmltool translate/-t <input.vml> <output> <architecture>");
                return;
            }
            var options = new CommandLineOptions
            {
                InputFile = args[1],
                OutputFile = args[2],
                TargetArchitecture = args[3]
            };
            ExecuteTranslate(options);
        }

        /// <summary>
        /// 编译源代码到VML（旧格式）
        /// </summary>
        private static void Compile(string[] args)
        {
            if (args.Length < 3)
            {
                Console.WriteLine("用法: vmltool compile/-c <input> <output> [--lang <language>]");
                Console.WriteLine("如果不指定语言，将根据文件扩展名自动检测");
                return;
            }
            string language = null;
            for (var i = 3; i < args.Length; i++)
            {
                if ((args[i] == "--lang" || args[i] == "-l") && i + 1 < args.Length)
                {
                    language = args[++i];
                    break;
                }
            }
            var options = new CommandLineOptions
            {
                InputFile = args[1],
                OutputFile = args[2],
                Language = language
            };
            ExecuteCompile(options);
        }

        /// <summary>
        /// 执行预处理操作（新格式）
        /// </summary>
        private static void ExecutePreprocess(CommandLineOptions options)
        {
            var inputFile = options.InputFile;
            var outputFile = options.OutputFile;

            if (string.IsNullOrEmpty(outputFile))
            {
                // 如果没有指定输出文件，使用默认名称
                outputFile = Path.ChangeExtension(inputFile, ".i");
            }

            try
            {
                // 这里需要C编译器的预处理功能
                // 暂时简化实现
                var source = File.ReadAllText(inputFile);
                File.WriteAllText(outputFile, source);
                Console.WriteLine($"预处理完成: {outputFile} (简化实现)");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"预处理错误: {ex.Message}");
            }
        }

        /// <summary>
        /// 预处理C代码（旧格式）
        /// </summary>
        private static void Preprocess(string[] args)
        {
            if (args.Length < 3)
            {
                Console.WriteLine("用法: vmltool preprocess/-p <input.c> <output.i>");
                return;
            }
            var options = new CommandLineOptions
            {
                InputFile = args[1],
                OutputFile = args[2]
            };
            ExecutePreprocess(options);
        }

        /// <summary>
        /// 执行链接操作（新格式）
        /// </summary>
        private static void ExecuteLink(CommandLineOptions options)
        {
            var inputFile = options.InputFile;
            var outputFile = options.OutputFile;
            var linkFiles = options.LinkFiles;

            if (string.IsNullOrEmpty(outputFile))
            {
                // 如果没有指定输出文件，使用默认名称
                outputFile = "linked.vml";
            }

            try
            {
                var programs = new List<VmlProgram>();
                var assembler = new VmlAssembler();

                // 添加主输入文件
                if (!string.IsNullOrEmpty(inputFile))
                {
                    var source = File.ReadAllText(inputFile);
                    var program = assembler.Assemble(source);
                    programs.Add(program);
                }

                // 添加链接文件
                foreach (var linkFile in linkFiles)
                {
                    var source = File.ReadAllText(linkFile);
                    var program = assembler.Assemble(source);
                    programs.Add(program);
                }

                if (programs.Count == 0)
                {
                    Console.WriteLine("错误: 没有有效的程序可以链接");
                    return;
                }

                // 使用 VML 链接器合并多个程序
                var linkedProgram = programs.Count == 1 ? programs[0] : VmlProgram.Link(programs, options.GcSections);

                // 保存链接后的程序
                File.WriteAllText(outputFile, linkedProgram.ToString());
                Console.WriteLine($"链接完成: {outputFile} ({linkedProgram.Instructions.Count} 条指令)");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"链接错误: {ex.Message}");
            }
        }

        /// <summary>
        /// 链接多个VML文件（旧格式）
        /// </summary>
        private static void Link(string[] args)
        {
            if (args.Length < 4)
            {
                Console.WriteLine("用法: vmltool link/-l <input1.vml> <input2.vml> ... <output.vml>");
                return;
            }
            var options = new CommandLineOptions
            {
                InputFile = args[1],
                OutputFile = args[^1],
                LinkFiles = new List<string>(args[2..^1])
            };
            ExecuteLink(options);
        }

        /// <summary>
        /// 执行运行操作（新格式）
        /// </summary>
        private static void ExecuteRun(CommandLineOptions options)
        {
            var inputFile = options.InputFile;

            try
            {
                VmlProgram program;
                if (inputFile.EndsWith(".vmb", StringComparison.OrdinalIgnoreCase))
                {
                    program = VmlProgram.LoadFromVmbFile(inputFile);
                    Console.WriteLine($"VMB 加载: {program.Instructions.Count} 条指令");
                }
                else
                {
                    // 自动检测语言 (.vml 默认走 CRT)
                    var lang = options.Language ?? "vml";
                    VmlToolConfig.Instance.ApplyTo(options, lang);

                    var basePath = Path.GetDirectoryName(Path.GetFullPath(inputFile)) ?? ".";
                    var assembler = new VmlAssembler();
                    program = assembler.AssembleWithIncludes(inputFile, basePath);
                }

                // Link libraries (配置 + -L 参数)
                if (options.LibraryPaths.Count > 0)
                {
                    LibraryLinker.DebugOutput = options.DumpLink;
                    program = LibraryLinker.LinkLibraries(program, options.LibraryPaths);
                    Console.WriteLine($"库链接完成: {program.Instructions.Count} 条指令");
                }

                // 创建虚拟机并运行
                var mode = options.TargetMode == VMLPlugins.TargetMode.OS ? "os" : "mcu";
                var vm = new VmRuntime(memorySize: 2 * 1024 * 1024, mode: mode);
                vm.LoadProgram(program);
                vm.Run();

                Console.WriteLine("程序执行完成");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"运行错误: {ex.Message}");
            }
        }

        /// <summary>
        /// 运行VML程序（旧格式）
        /// </summary>
        private static void Run(string[] args)
        {
            if (args.Length < 2)
            {
                Console.WriteLine("用法: vmltool run/-r <input.vml>");
                return;
            }
            var options = new CommandLineOptions
            {
                InputFile = args[1]
            };
            ExecuteRun(options);
        }

        /// <summary>
        /// 执行 VMB 操作（查看/反汇编 VMB 二进制文件）
        /// </summary>
        private static void ExecuteVmb(CommandLineOptions options)
        {
            var inputFile = options.InputFile;
            var vmbCommand = options.VmbCommand ?? "info";

            try
            {
                var program = VmlProgram.LoadFromVmbFile(inputFile);

                switch (vmbCommand.ToLower())
                {
                    case "info":
                        ShowVmbInfo(program, inputFile);
                        break;

                    case "disasm":
                    case "disassemble":
                        ShowVmbDisasm(program);
                        break;

                    case "asm":
                        ShowVmbAsm(program);
                        break;

                    default:
                        Console.WriteLine($"未知 VMB 子命令: {vmbCommand}");
                        Console.WriteLine("支持的子命令: info, disasm, asm");
                        break;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"VMB 错误: {ex.Message}");
            }
        }

        /// <summary>
        /// 显示 VMB 文件信息
        /// </summary>
        private static void ShowVmbInfo(VmlProgram program, string? filePath)
        {
            var fileInfo = new FileInfo(filePath);
            Console.WriteLine("=== VMB 文件信息 ===");
            Console.WriteLine($"  文件: {filePath}");
            Console.WriteLine($"  大小: {fileInfo.Length} 字节");
            Console.WriteLine($"入口点: {program.EntryPoint}");
            Console.WriteLine($"  栈顶: 0x{program.StackTop:X}");
            Console.WriteLine($"向量表: 0x{program.VectorTable:X}");
            Console.WriteLine();
            Console.WriteLine($"代码段: {program.Instructions.Count} 条指令");
            Console.WriteLine($"数据段: {program.DataSection.Count} 项");
            Console.WriteLine($"常量段: {program.Constants.Count} 项");
            Console.WriteLine($"  标签: {program.Labels.Count} 个");
            Console.WriteLine("=====================");
        }

        /// <summary>
        /// 反汇编 VMB 程序
        /// </summary>
        private static void ShowVmbDisasm(VmlProgram program)
        {
            Console.WriteLine("=== VMB 反汇编 ===");
            Console.WriteLine(program.ToString());
        }

        /// <summary>
        /// 显示 VMB 程序（文本格式）
        /// </summary>
        private static void ShowVmbAsm(VmlProgram program)
        {
            Console.WriteLine(program.ToString());
        }
    }
}
