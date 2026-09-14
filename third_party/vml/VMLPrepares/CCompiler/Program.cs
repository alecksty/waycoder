using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using CompilerBase;
using VMLAssembler;

namespace CCompiler
{
    public sealed class CCompilerProgram : CompilerProgramBase
    {
        protected override string LanguageName      => "c";
        protected override string EnvVarName        => "VML_C_LIB";
        protected override string DefaultLibSubDir  => "c";

        // C 编译器特有标志
        private bool _preprocessOnly  = false;
        private bool _includeMode     = false;
        private bool _noLink          = false;
        private bool _useSharedLibrary = true;
        private List<string> _libraryPaths = new List<string>();

        // ----------------------------------------------------------------
        // 重写参数解析：在基类解析完成前插入 -E / --include-mode / --no-shared
        // ----------------------------------------------------------------
        protected override bool TryParseExtraArg(string[] args, ref int i)
        {
            switch (args[i])
            {
                case "-E":
                    _preprocessOnly = true;
                    return true;
                case "--include-mode":
                    _includeMode = true;
                    return true;
                case "--no-shared":
                    _useSharedLibrary = false;
                    return true;
                case "--no-link":
                    _noLink = true;
                    return true;
                case "-L" when i + 1 < args.Length:
                    _libraryPaths.Add(args[++i]);
                    return true;
                default:
                    return false;
            }
        }

        // ----------------------------------------------------------------
        // 重写 Usage
        // ----------------------------------------------------------------
        protected override void PrintUsage()
        {
            Console.WriteLine("用法: CCompiler [选项] <source_file1> [source_file2] ...");
            Console.WriteLine("选项:");
            Console.WriteLine("  -E             只进行预处理，不进行编译");
            Console.WriteLine("  -I <path>      添加源码搜索路径（用于#include）");
            Console.WriteLine("  -L <path>      添加VML库文件搜索路径（用于链接）");
            Console.WriteLine("  -o <file>      指定输出文件名（单文件模式）");
            Console.WriteLine("  --include-mode 使用包含模式（生成.include伪指令）");
            Console.WriteLine("  --no-shared    不使用共享库");
            Console.WriteLine("  --no-link      不自动链接标准库（用于生成独立模块）");
            Console.WriteLine("  --timeout <s>  设置编译超时秒数");
            Console.WriteLine("  --target mcu|os       编译目标模式（mcu=跳过OS特性，默认; os=全部特性）");
            Console.WriteLine("  --ram k|m|g           内存级别（k=KB, m=MB默认, g=GB）");
            Console.WriteLine("  --stack-size <bytes>  手动指定栈大小（默认自动根据--ram分配）");
            Console.WriteLine("多文件模式: 指定多个源文件，每个文件生成对应的.vml文件");
            Console.WriteLine("单文件模式: 指定一个源文件和-o选项，生成指定文件");
        }

        // ----------------------------------------------------------------
        // 重写编译：单文件 include-mode 走 CompileFileWithIncludes，否则走普通编译
        // ----------------------------------------------------------------
        protected override string Compile(string sourceFile, List<string> includePaths, List<string> libraryPaths)
        {
            if (_preprocessOnly)
            {
                // -E 模式：只预处理，结果保存到 .i 文件
                var sourceCode = File.ReadAllText(sourceFile);
                var pp = new Preprocessor(sourceCode, includePaths);
                string processed = pp.Process(sourceFile);
                string outFile = Path.ChangeExtension(sourceFile, ".i");
                File.WriteAllText(outFile, processed);
                Console.WriteLine($"预处理成功! 已保存: {outFile}");
                return null; // 不生成 .vml
            }

            if (_includeMode)
                return CCompiler.CompileFileWithIncludes(sourceFile, includePaths, libraryPaths, false, _useSharedLibrary);

            if (_noLink)
            {
                // --no-link 模式：不链接任何库，仅输出纯编译代码
                var noLinkProg = CCompiler.CompileFile(sourceFile, includePaths, null, false);
                noLinkProg.IsLibrary = true;  // 库模式: 不含启动代码
                Console.WriteLine($"生成的指令数: {noLinkProg.Instructions.Count}");
                Console.WriteLine($"数据段大小: {noLinkProg.DataSection.Count}");
                Console.WriteLine("（无自动链接）");
                return noLinkProg.ToString();
            }

            // 普通编译 — 自动检测: 有 main 则链接库，无 main 则纯编译（库模式）
            bool hasMain = System.Text.RegularExpressions.Regex.IsMatch(
                File.ReadAllText(sourceFile), @"\bmain\s*\(");
            bool autoLink = hasMain || libraryPaths?.Count > 0;
            var compProg = CCompiler.CompileFile(sourceFile, includePaths, libraryPaths, autoLink);
            Console.WriteLine($"生成的指令数: {compProg.Instructions.Count}");
            Console.WriteLine($"数据段大小: {compProg.DataSection.Count}");
            if (!autoLink)
                Console.WriteLine("（库模式：无 main 函数，不自动链接）");
            return compProg.ToString();
        }

        // ----------------------------------------------------------------
        // 重写多文件逻辑：支持链接模式（多文件 + -o）
        // ----------------------------------------------------------------
        protected override async Task RunMultiFileAsync(
            List<string> sourceFiles, string outputFile,
            List<string> includePaths, List<string> allPaths, int timeoutSeconds)
        {
            if (!string.IsNullOrEmpty(outputFile))
            {
                // 多文件链接模式
                Console.WriteLine($"多文件链接模式: 编译 {sourceFiles.Count} 个文件并链接到 {outputFile}");
                var generatedVmlFiles = new List<string>();

                foreach (var sourceFile in sourceFiles)
                {
                    Console.WriteLine($"\n编译文件: {sourceFile}");
                    if (!File.Exists(sourceFile)) { Console.WriteLine($"错误: 文件不存在: {sourceFile}"); continue; }

                    if (_preprocessOnly)
                    {
                        var src = File.ReadAllText(sourceFile);
                        var pp = new Preprocessor(src, includePaths);
                        string processed = pp.Process(sourceFile);
                        string ppFile = Path.ChangeExtension(sourceFile, ".i");
                        File.WriteAllText(ppFile, processed);
                        Console.WriteLine($"预处理文件保存成功: {ppFile}");
                    }
                    else
                    {
                        try
                        {
                            var captured = sourceFile;
                            var prog = await CompilerCliParser.CompileWithTimeout(
                                () => CCompiler.CompileFile(captured, includePaths), timeoutSeconds);
                            if (prog == null) return;
                            Console.WriteLine($"生成的指令数: {prog.Instructions.Count}");
                            string tempVml = Path.ChangeExtension(sourceFile, ".vml");
                            File.WriteAllText(tempVml, prog.ToString());
                            generatedVmlFiles.Add(tempVml);
                            Console.WriteLine($"临时VML文件保存成功: {tempVml}");
                        }
                        catch (Exception ex) { Console.WriteLine($"编译错误: {ex.Message}"); }
                    }
                }

                if (!_preprocessOnly && generatedVmlFiles.Count > 0)
                {
                    Console.WriteLine($"\n开始链接 {generatedVmlFiles.Count} 个VML文件...");
                    new Linker().Link(generatedVmlFiles, outputFile);
                    Console.WriteLine($"链接完成! 输出文件: {outputFile}");
                    foreach (var f in generatedVmlFiles) File.Delete(f);
                    Console.WriteLine("临时文件已清理");
                }

                Console.WriteLine($"\n多文件链接完成! 共处理 {sourceFiles.Count} 个源文件");
            }
            else
            {
                // 多文件独立编译模式（回退到基类行为）
                await base.RunMultiFileAsync(sourceFiles, outputFile, includePaths, allPaths, timeoutSeconds);
            }
        }
    }

    public static class CompilerProgram
    {
        static async Task Main(string[] args)
            => await new CCompilerProgram().RunAsync(args);
    }
}
