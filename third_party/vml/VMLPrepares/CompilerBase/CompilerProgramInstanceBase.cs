using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using VMLPlugins;
using VMLAssembler;

namespace CompilerBase
{
    /// <summary>
    /// 实例编译器 CLI 基类（模式B）。
    /// 适用于：Java / JavaScript / Swift / C# 等创建编译器实例、
    /// 调用 compiler.Compile(source, debugMode) 的编译器。
    /// 子类只需提供语言名和 CreateCompiler() 工厂即可。
    /// </summary>
    public abstract class CompilerProgramInstanceBase
    {
        /// <summary>语言名称，用于帮助信息和标准库路径（如 "java"、"swift"）</summary>
        protected abstract string LanguageName { get; }

        /// <summary>标准库子目录（默认等于 LanguageName，可覆盖）</summary>
        protected virtual string DefaultLibSubDir => LanguageName.ToLower();

        /// <summary>创建编译器实例并执行编译，返回 VmlProgram</summary>
        protected abstract VmlProgram CompileSource(string sourceCode, bool debugMode);

        /// <summary>CLI 入口</summary>
        public async Task RunAsync(string[] args)
        {
            if (args.Length < 1)
            {
                PrintUsage();
                return;
            }

            bool debugMode        = false;
            bool useSharedLibrary = true;

            // 使用 extraParser 解析本基类特有参数（-debug, --no-shared）
            var cli = CompilerCliParser.ParseCommonArgs(args, (a, i) =>
            {
                switch (a[i])
                {
                    case "-debug":
                        debugMode = true;
                        return true;
                    case "--include-mode":
                        return true;
                    case "--no-shared":
                        useSharedLibrary = false;
                        return true;
                }
                return false;
            });

            var sourceFiles    = cli.SourceFiles;
            var includePaths   = cli.IncludePaths;
            var libraryPaths   = cli.LibraryPaths;
            string outputFile  = cli.OutputFile;
            int timeoutSeconds = cli.TimeoutSeconds;

            if (sourceFiles.Count == 0)
            {
                Console.WriteLine("错误: 没有指定源文件");
                return;
            }

            try
            {
                // 单文件模式（指定了 -o）
                if (!string.IsNullOrEmpty(outputFile))
                {
                    if (sourceFiles.Count > 1)
                        Console.WriteLine("警告: 单文件模式只处理第一个源文件");

                    string sourceFile = sourceFiles[0];
                    Console.WriteLine($"单文件编译模式: 编译 {sourceFile}");

                    if (!File.Exists(sourceFile))
                    {
                        Console.WriteLine($"错误: 文件不存在 {sourceFile}");
                        return;
                    }

                    string sourceCode = File.ReadAllText(sourceFile, Encoding.UTF8);
                    Console.WriteLine($"读取文件 {sourceFile} 成功!");
                    Console.WriteLine("开始编译...");

                    var captured = debugMode;
                    var program  = await CompilerCliParser.CompileWithTimeout(() => CompileSource(sourceCode, captured), timeoutSeconds);

                    if (program == null)
                    {
                        Console.WriteLine("编译失败!");
                        return;
                    }

                    // 链接库
                    program = LinkLibraries(program, libraryPaths, useSharedLibrary);

                    // 保存
                    File.WriteAllText(outputFile, program.ToString());
                    Console.WriteLine($"VML文件保存成功: {outputFile}");
                    Console.WriteLine($"编译成功!");
                    Console.WriteLine($"生成的指令数: {program.Instructions.Count}");
                    Console.WriteLine($"数据段大小: {program.DataSection.Count}");
                }
                else
                {
                    // 多文件模式
                    Console.WriteLine($"多文件编译模式: 处理 {sourceFiles.Count} 个文件");

                    foreach (string sourceFile in sourceFiles)
                    {
                        if (!File.Exists(sourceFile))
                        {
                            Console.WriteLine($"错误: 文件不存在 {sourceFile}");
                            continue;
                        }

                        string outputVmlFile = Path.ChangeExtension(sourceFile, ".vml");
                        Console.WriteLine($"编译 {sourceFile} -> {outputVmlFile}");

                        string sourceCode = File.ReadAllText(sourceFile, Encoding.UTF8);
                        var captured = debugMode;
                        var program  = await CompilerCliParser.CompileWithTimeout(() => CompileSource(sourceCode, captured), timeoutSeconds);

                        if (program == null)
                        {
                            Console.WriteLine($"  {sourceFile}: 编译失败!");
                            continue;
                        }

                        File.WriteAllText(outputVmlFile, program.ToString());
                        Console.WriteLine($"  {sourceFile}: 编译成功，生成 {outputVmlFile}");
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"错误: {ex.Message}");
                Console.WriteLine($"错误堆栈: {ex.StackTrace}");
            }
        }

        /// <summary>链接标准库和用户库</summary>
        private VmlProgram LinkLibraries(VmlProgram program, List<string> userLibraryPaths, bool useSharedLibrary)
        {
            var allLibraryPaths = new List<string>();
            allLibraryPaths.AddRange(userLibraryPaths);

            // 不再自动链接 builtins.vml — 用户需显式通过 #param lib / import 指定

            Console.WriteLine($"{char.ToUpper(LanguageName[0])}{LanguageName.Substring(1)}Compiler: 开始链接库，库路径数量: {allLibraryPaths.Count}");
            program = VMLAssembler.LibraryLinker.LinkLibraries(program, allLibraryPaths);
            Console.WriteLine($"链接完成，总指令数: {program.Instructions.Count}");
            return program;
        }

        /// <summary>打印帮助信息</summary>
        protected virtual void PrintUsage()
        {
            Console.WriteLine("用法: Compiler [选项] <源文件>");
            Console.WriteLine("选项:");
            CompilerCliParser.PrintCommonUsage();
            Console.WriteLine("  -debug         启用调试输出");
        }

    }
}
