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
            //   现在按 `Classify` 的口径办：该上抛的上抛，只有**意外**异常才收编。
            catch (Exception ex)
            {
                if (Classify(ex) is { } wrapped) throw wrapped;
                throw;
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
            catch (Exception ex)
            {
                if (Classify(ex) is { } wrapped) throw wrapped;
                throw;
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
        /// 两个 `catch` 共用的**唯一**异常分类点：返回 <c>null</c> 表示「原样上抛」。
        ///
        /// <para>
        /// 分三档，与 `CompilerHelper.CompileWithDiagnostics` 同一口径：
        /// </para>
        /// <list type="bullet">
        /// <item><b>编译错误</b>（<see cref="CompilationException"/>）—— 原样上抛。
        ///   它已经带着位置，包一层只会把 `文件:行:列: error:` 变成两层。</item>
        /// <item><b>取消</b>（<see cref="OperationCanceledException"/>）—— 原样上抛。
        ///   用户按了中断就当用户的中断，不能变成一条"语法错误"。</item>
        /// <item><b>链接期的未定义函数</b>（<see cref="UnresolvedSymbolException"/>）——
        ///   **是用户源码的错**，不是内部错误。少了这一支它会落进兜底被标成
        ///   「内部错误」，用户看到「编译器坏了」而不是「我调了个不存在的函数」（实测踩到）。</item>
        /// <item><b>其余</b>—— 才是真的内部错误：收编成带位置的编译错误，别让它穿到宿主
        ///   变成一句 `Object reference not set…`。</item>
        /// </list>
        /// </summary>
        private static CompilationException? Classify(Exception ex) => ex switch
        {
            CompilationException => null,
            OperationCanceledException => null,
            UnresolvedSymbolException => new CompilationException(ErrorCode.CodeGen_UndefinedFunction, ex.Message, ex),
            _ => new CompilationException(ErrorCode.Compilation_InternalError, $"<input>: 内部错误: {ex.Message}", ex),
        };

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
