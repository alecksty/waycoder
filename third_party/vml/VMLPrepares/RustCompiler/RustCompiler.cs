using System;
using System.Collections.Generic;
using VMLAssembler;
using CompilerBase;
using VMLPlugins;

namespace RustCompiler
{
    /// <summary>
    /// Rust编译器
    /// </summary>
    public class RustCompiler
    {
        private static readonly Dictionary<string, string> PredefinedMacros = new(CompilerHelper.BasePredefinedMacros())
        {
            ["__RUST__"] = "1",
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

            return CompilerHelper.CompileWithDiagnostics(null, diagnostics =>
            {
                bool isMCU = CompilerOptionsContext.Current.IsMCU;
                var lexer = new Lexer(source) { FileName = "<input>", Diagnostics = diagnostics };
                var tokens = lexer.Tokenize();
                if (CompilerOptionsContext.Current.DebugMode)
                {
                    Console.WriteLine("\n=== 词法分析结果 ===");
                    foreach (var token in tokens) Console.WriteLine($"{token.Type}: '{token.Value}'");
                    Console.WriteLine("=== 词法分析结束 ===\n");
                }
                var parser = new Parser(tokens) { FileName = "<input>", Diagnostics = diagnostics };
                var ast = parser.Parse();
                var codeGenerator = new CodeGenerator(ast);
                codeGenerator.SourceLines = source.Split('\n');
                var prog = codeGenerator.GenerateCode();
                CompilerHelper.LinkStandardLibrary(prog, "rust", null);
                return prog;
            });
        }

        public static VmlProgram CompileFile(string filePath, List<string> includePaths = null, List<string> libraryPaths = null, bool autoLinkStdLib = true)
        {
            string source = CompilerHelper.ReadSourceFile(filePath);
            string sourceDir = Path.GetDirectoryName(Path.GetFullPath(filePath));

            return CompilerHelper.CompileFileStandard(filePath, "rust", Compile, ExtractImports, PredefinedMacros, includePaths, libraryPaths, autoLinkStdLib);
        }
        
        /// <summary>
        /// 编译文件并生成包含.include伪指令的VML文本
        /// </summary>
        public static string CompileFileWithIncludes(string filePath, List<string> includePaths = null, List<string> libraryPaths = null, bool autoLinkStdLib = true, bool useSharedLibrary = true)
            => CompilerHelper.BuildCompileFileWithIncludes(
                CompileFile(filePath, includePaths, null, false),
                filePath, libraryPaths, autoLinkStdLib, useSharedLibrary, "rust");

        private static List<string> ExtractImports(string source)
        {
            var libs = new List<string>();
            foreach (System.Text.RegularExpressions.Match m in System.Text.RegularExpressions.Regex.Matches(source, @"use\s+(\w+)::", System.Text.RegularExpressions.RegexOptions.Multiline))
            {
                string name = m.Groups[1].Value;
                if (!libs.Contains(name)) libs.Add(name);
            }
            return libs;
        }
    }
}
