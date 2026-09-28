using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using VMLPlugins;
using VMLAssembler;

namespace CompilerBase
{
    /// <summary>
    /// 静态编译器 CLI 基类（模式A）。
    /// 适用于：Go / Rust / Forth / Lua / Ladder / Python / Pascal / Basic / C 等调用
    /// XxxCompiler.CompileFileWithIncludes() 静态方法的编译器。
    /// 子类只需提供语言名、环境变量名、编译委托即可。
    /// 子类可通过重写 TryParseExtraArg / RunMultiFileAsync 注入特殊逻辑。
    /// </summary>
    public abstract class CompilerProgramBase
    {
        /// <summary>语言名称，用于帮助信息（如 "go"）</summary>
        protected abstract string LanguageName { get; }

        /// <summary>环境变量名（如 "VML_GO_LIB"）</summary>
        protected abstract string EnvVarName { get; }

        /// <summary>默认标准库子目录名（如 "go"、"rust"、"Basic"）</summary>
        protected virtual string DefaultLibSubDir => LanguageName.ToLower();

        /// <summary>
        /// 实际编译调用，子类实现。返回 VML 文本；
        /// 对于预处理等特殊模式可返回 null（跳过写文件）。
        /// </summary>
        protected abstract string Compile(string sourceFile, List<string> includePaths, List<string> libraryPaths);

        /// <summary>
        /// 子类可重写此方法解析额外参数。
        /// 若成功消费了 args[i]（及可能的 args[++i]），返回 true；否则返回 false。
        /// </summary>
        protected virtual bool TryParseExtraArg(string[] args, ref int i) => false;

        /// <summary>CLI 入口</summary>
        public async Task RunAsync(string[] args)
        {
            if (args.Length < 1)
            {
                PrintUsage();
                return;
            }

            // 解析共享 CLI 参数 + 子类特有参数
            var cli = CompilerCliParser.ParseCommonArgs(args, (a, i) => TryParseExtraArg(a, ref i));
            var sourceFiles    = cli.SourceFiles;
            var includePaths   = cli.IncludePaths;
            var libraryPaths   = cli.LibraryPaths;
            string outputFile  = cli.OutputFile;
            int timeoutSeconds = cli.TimeoutSeconds;

            if (sourceFiles.Count == 0 && outputFile == "")
            {
                PrintUsage();
                return;
            }

            if (sourceFiles.Count == 0)
            {
                Console.WriteLine(VmlLang.Pick("错误: 必须指定至少一个源文件", "error: at least one source file must be specified"));
                return;
            }

            // 检查环境变量
            string envLibPath = Environment.GetEnvironmentVariable(EnvVarName);
            if (!string.IsNullOrEmpty(envLibPath))
            {
                Console.WriteLine(VmlLang.Pick($"从环境变量 {EnvVarName} 添加库路径: {envLibPath}", $"adding library path from environment variable {EnvVarName}: {envLibPath}"));
                libraryPaths.Add(envLibPath);
            }

            // 合并 includePaths 和 libraryPaths（供子类使用）
            var allPaths = new List<string>();
            allPaths.AddRange(includePaths);
            allPaths.AddRange(libraryPaths);

            try
            {
                if (sourceFiles.Count > 1)
                {
                    await RunMultiFileAsync(sourceFiles, outputFile, includePaths, allPaths, timeoutSeconds);
                }
                else
                {
                    // 单文件编译模式
                    string sourceFile = sourceFiles[0];

                    if (!File.Exists(sourceFile))
                    {
                        Console.WriteLine(VmlLang.Pick($"错误: 文件不存在: {sourceFile}", $"error: file not found: {sourceFile}"));
                        return;
                    }

                    Console.WriteLine(VmlLang.Pick($"编译文件: {sourceFile}", $"compiling file: {sourceFile}"));

                    try
                    {
                        var vmlText = await CompilerCliParser.CompileWithTimeout(
                            () => Compile(sourceFile, includePaths, allPaths),
                            timeoutSeconds);
                        if (vmlText == null) return;

                        if (string.IsNullOrEmpty(outputFile))
                            outputFile = Path.ChangeExtension(sourceFile, ".vml");

                        File.WriteAllText(outputFile, vmlText);
                        Console.WriteLine(VmlLang.Pick($"编译成功! 输出保存到: {outputFile}", $"compilation succeeded! output saved to: {outputFile}"));

                        if (libraryPaths.Count > 0)
                            Console.WriteLine(VmlLang.Pick($"库搜索路径: {string.Join(", ", libraryPaths)}", $"library search paths: {string.Join(", ", libraryPaths)}"));
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine(VmlLang.Pick($"编译错误: {ex.Message}", $"compile error: {ex.Message}"));
                        if (ex.InnerException != null)
                        {
                            Console.WriteLine(VmlLang.Pick($"内部异常: {ex.InnerException.Message}", $"inner exception: {ex.InnerException.Message}"));
                            Console.WriteLine(VmlLang.Pick($"堆栈跟踪: {ex.InnerException.StackTrace}", $"stack trace: {ex.InnerException.StackTrace}"));
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(VmlLang.Pick($"错误: {ex.Message}", $"error: {ex.Message}"));
                Console.WriteLine(ex.StackTrace);
            }
        }

        /// <summary>
        /// 多文件编译逻辑。子类可重写以实现链接等特殊行为。
        /// </summary>
        protected virtual async Task RunMultiFileAsync(
            List<string> sourceFiles, string outputFile,
            List<string> includePaths, List<string> allPaths, int timeoutSeconds)
        {
            if (!string.IsNullOrEmpty(outputFile))
            {
                Console.WriteLine(VmlLang.Pick("错误: 多文件模式不支持 -o 选项", "error: multi-file mode does not support the -o option"));
                return;
            }

            Console.WriteLine(VmlLang.Pick($"多文件模式: 编译 {sourceFiles.Count} 个文件", $"multi-file mode: compiling {sourceFiles.Count} files"));

            foreach (var sourceFile in sourceFiles)
            {
                Console.WriteLine(VmlLang.Pick($"\n编译文件: {sourceFile}", $"\ncompiling file: {sourceFile}"));

                if (!File.Exists(sourceFile))
                {
                    Console.WriteLine(VmlLang.Pick($"错误: 文件不存在: {sourceFile}", $"error: file not found: {sourceFile}"));
                    continue;
                }

                try
                {
                    var captured = sourceFile;
                    var vmlText = await CompilerCliParser.CompileWithTimeout(
                        () => Compile(captured, includePaths, allPaths),
                        timeoutSeconds);
                    if (vmlText == null) return;

                    string vmlFile = Path.ChangeExtension(sourceFile, ".vml");
                    File.WriteAllText(vmlFile, vmlText);
                    Console.WriteLine(VmlLang.Pick($"生成: {vmlFile}", $"generated: {vmlFile}"));
                }
                catch (Exception ex)
                {
                    Console.WriteLine(VmlLang.Pick($"编译错误: {ex.Message}", $"compile error: {ex.Message}"));
                }
            }

            Console.WriteLine(VmlLang.Pick($"\n多文件编译完成! 共处理 {sourceFiles.Count} 个源文件", $"\nmulti-file compilation complete! processed {sourceFiles.Count} source files"));
        }

        /// <summary>打印帮助信息</summary>
        protected virtual void PrintUsage()
        {
            string lang = LanguageName;
            Console.WriteLine(VmlLang.Pick($"用法: {char.ToUpper(lang[0])}{lang.Substring(1)}Compiler [选项] <source_file1> [source_file2] ...", $"usage: {char.ToUpper(lang[0])}{lang.Substring(1)}Compiler [options] <source_file1> [source_file2] ..."));
            Console.WriteLine(VmlLang.Pick("选项:", "options:"));
            CompilerCliParser.PrintCommonUsage();
            Console.WriteLine(VmlLang.Pick("多文件模式: 指定多个源文件，每个文件生成对应的.vml文件", "multi-file mode: specify multiple source files; each one generates its own .vml file"));
            Console.WriteLine(VmlLang.Pick("单文件模式: 指定一个源文件和-o选项，生成指定文件", "single-file mode: specify one source file plus the -o option to generate the specified file"));
        }

    }
}
