using System;
using System.Collections.Generic;
using System.IO;
using VMLAssembler;
using CompilerBase;
using VMLPlugins;

namespace ForthCompiler
{
    /// <summary>
    /// Forth语言编译器
    /// </summary>
    public class ForthCompiler
    {
        private static readonly Dictionary<string, string> PredefinedMacros = new(CompilerHelper.BasePredefinedMacros())
        {
            ["__FORTH__"] = "1",
        };

        public static VmlProgram Compile(string source)
        {
            source = CompilerHelper.InjectDefines(source, "c", CompilerOptionsContext.Current);
            // 预处理
            if (source.Contains('#'))
            {
                var pp = new Preprocessor(source, null, PredefinedMacros);
                source = pp.Process();
            }

            var savedCtx = CompilerOptionsContext.Current;
            try
            {
                return CompilerHelper.CompileWithDiagnostics(null, diagnostics =>
                {
                    Lexer lexer = new Lexer(source) { FileName = "<input>", Diagnostics = diagnostics };
                    var tokens = lexer.Tokenize();

                    if (CompilerOptionsContext.Current.DebugMode)
                    {
                        Console.WriteLine("词法分析结果:");
                        foreach (var token in tokens) Console.WriteLine($"  {token}");
                    }

                    Parser parser = new Parser(tokens) { FileName = "<input>", Diagnostics = diagnostics };
                    var ast = parser.ParseProgram();
                    CodeGenerator codeGen = new CodeGenerator(ast);
                    codeGen.SourceLines = source.Split('\n');
                    return codeGen.GenerateCode();
                });
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"<input>: error: {ex.Message}");
                return CreateErrorProgram($"Forth编译错误: {ex.Message}");
            }
            finally { CompilerOptionsContext.Current = savedCtx; }
        }

        public static VmlProgram CompileFile(string filePath, List<string> includePaths = null, List<string> libraryPaths = null, bool autoLinkStdLib = true)
        {
            string source = CompilerHelper.ReadSourceFile(filePath);
            string sourceDir = Path.GetDirectoryName(Path.GetFullPath(filePath));

            // 预处理
            Preprocessor pp = null;
            if (source.Contains('#'))
            {
                pp = new Preprocessor(source, includePaths, PredefinedMacros);
                source = pp.Process(filePath);
            }

            var savedCtx = CompilerOptionsContext.Current;
            try
            {
                Lexer lexer = new Lexer(source);
                var tokens = lexer.Tokenize();

                Parser parser = new Parser(tokens);
                var ast = parser.ParseProgram();

                CodeGenerator codeGen = new CodeGenerator(ast);
                codeGen.SourceLines = source.Split('\n');
                var prog = codeGen.GenerateCode();

                List<string> allLibraryPaths = new List<string>();
                if (libraryPaths != null)
                    allLibraryPaths.AddRange(libraryPaths);

                if (pp != null)
                {
                    foreach (var lib in pp.ParamLibraries)
                    {
                        string resolved = CompilerHelper.ResolveImportLibrary(lib, sourceDir, "forth");
                        if (resolved != null && !allLibraryPaths.Contains(resolved))
                            allLibraryPaths.Add(resolved);
                    }
                }
                foreach (var lib in lexer.ParamLibraries)
                {
                    string resolved = CompilerHelper.ResolveImportLibrary(lib, sourceDir, "forth");
                    if (resolved != null && !allLibraryPaths.Contains(resolved))
                        allLibraryPaths.Add(resolved);
                }

                if (autoLinkStdLib)
                    CompilerHelper.LinkStandardLibrary(prog, "forth", allLibraryPaths);
                else if (allLibraryPaths.Count > 0)
                    LibraryLinker.LinkLibraries(prog, allLibraryPaths);

                return prog;
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"Forth编译错误: {ex.Message}");
                return CreateErrorProgram($"Forth编译错误: {ex.Message}");
            }
            finally { CompilerOptionsContext.Current = savedCtx; }
        }
        
        /// <summary>
        /// 编译文件并生成包含.include伪指令的VML文本
        /// </summary>
        public static string CompileFileWithIncludes(string filePath, List<string> includePaths = null, List<string> libraryPaths = null, bool autoLinkStdLib = true, bool useSharedLibrary = true)
            => CompilerHelper.BuildCompileFileWithIncludes(
                CompileFile(filePath, includePaths, null, false),
                filePath, libraryPaths, autoLinkStdLib, useSharedLibrary, "forth");

        private static VmlProgram CreateErrorProgram(string errorMessage)
        {
            var instructions = new List<Instruction>();
            var labels = new Dictionary<string, int>();
            var dataSection = new Dictionary<string, object>();
            var constants = new Dictionary<string, object>();

            labels["main"] = 0;
            instructions.Add(new Instruction(OpCode.LABEL, new List<Operand> { new Operand(OperandType.LABEL, "main") }, 0, "main"));
            // EmitExit(): MOVE R0, #0; SYSCALL #3
            instructions.Add(new Instruction(OpCode.MOVE, [new Operand(OperandType.REGISTER, 0), new Operand(OperandType.IMMEDIATE, 0)], 1));
            instructions.Add(new Instruction(OpCode.SYSCALL, [new Operand(OperandType.IMMEDIATE, 3)], 2));

            return new VmlProgram(instructions, labels, dataSection, constants);
        }

        /// <summary>
        /// 打印帮助信息
        /// </summary>
        public static void PrintHelp()
        {
            Console.WriteLine("ForthCompiler - Forth语言到VML编译器");
            Console.WriteLine();
            Console.WriteLine("用法:");
            Console.WriteLine("  dotnet run --project VMLPrepares/ForthCompiler <输入文件> [-o <输出文件>]");
            Console.WriteLine();
            Console.WriteLine("示例:");
            Console.WriteLine("  dotnet run --project VMLPrepares/ForthCompiler input.fth -o output.vml");
            Console.WriteLine();
            Console.WriteLine("支持的Forth特性:");
            Console.WriteLine("  - 词定义 (: ... ;)");
            Console.WriteLine("  - 栈操作 (DUP, DROP, SWAP, OVER, ROT)");
            Console.WriteLine("  - 算术运算 (+, -, *, /)");
            Console.WriteLine("  - 字符串输出 (.\" ... \")");
            Console.WriteLine("  - 数字输出 (.)");
            Console.WriteLine("  - 换行输出 (CR)");
            Console.WriteLine();
            Console.WriteLine("注意: 这是基础版本，支持有限的Forth语法");
        }
    }
}
