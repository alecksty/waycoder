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
            // ⚠ 从前这里把**所有**异常（含 `CompilationException`）换成一份
            //   `CreateErrorProgram` 的空程序返回 ⇒ `Compile` **永不失败**：
            //   宿主看 `prog != null` 就报"编译成功"，用户拿到一个什么都不做的程序，
            //   而诊断包里那条错（本来带着 `<input>:3:1`）就这么没了。
            //   现在只把**意外**异常收编成编译错误（带位置、能定位），编译错误原样上抛。
            catch (Exception ex) when (ex is not CompilationException and not OperationCanceledException)
            {
                throw new CompilationException(ErrorCode.Compilation_InternalError,
                    $"<input>: 内部错误: {ex.Message}", ex);
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
            // 同 `Compile`：编译错误不能被换成一份"能跑的空程序"（那等于报告成功）。
            catch (Exception ex) when (ex is not CompilationException and not OperationCanceledException)
            {
                throw new CompilationException(ErrorCode.Compilation_InternalError,
                    $"<input>: 内部错误: {ex.Message}", ex);
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

        /// <summary>
        /// 打印帮助信息
        /// </summary>
        /// <remarks>
        /// ⚠ 此处原有一个 `CreateErrorProgram(errorMessage)`（造一份打印退出码的空程序）。
        ///   它的**唯一**用途就是给上面两个 `catch` 当"失败也返回个东西"的替身，
        ///   而那个替身正是"编译失败被报成成功"的载体 —— 两个 catch 改成上抛之后它就成了
        ///   死代码（全仓 `grep` 只剩定义处），已删。**别再把它加回来**：
        ///   任何"失败时返回一份能跑的空程序"的写法，在宿主那边都读作编译成功。
        /// </remarks>
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
