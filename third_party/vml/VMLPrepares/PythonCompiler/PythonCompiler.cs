using System;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using VMLAssembler;
using VMLPlugins;
using CompilerBase;

namespace PythonCompiler
{
    /// <summary>
    /// Python 编译器主类
    /// </summary>
    public static class PythonCompiler
    {
        private static readonly Dictionary<string, string> PredefinedMacros = new(CompilerHelper.BasePredefinedMacros())
        {
            ["__PYTHON__"] = "1",
        };

        /// <summary>
        /// 编译 Python 源代码
        /// </summary>
        public static VmlProgram Compile(string source)
        {
            source = CompilerHelper.InjectDefines(source, "c", CompilerOptionsContext.Current);
            if (source.Contains('#'))
            {
                var pp = new Preprocessor(source, null, PredefinedMacros);
                source = pp.Process();
            }

            return CompilerHelper.CompileWithDiagnostics(null, diagnostics =>
            {
                var lexer = new Lexer(source) { FileName = "<input>", Diagnostics = diagnostics };
                lexer.Tokenize();
                bool isMCU = VMLPlugins.CompilerOptionsContext.Current.IsMCU;
                var parser = new Parser(lexer.Tokens, isMCU) { FileName = "<input>", Diagnostics = diagnostics };
                var ast = parser.Parse();
                var generator = new CodeGenerator();
                generator.SourceLines = source.Split('\n');
                var prog = generator.Generate(ast);
                CompilerHelper.LinkStandardLibrary(prog, "python", null);
                return prog;
            });
        }

        /// <summary>
        /// 从文件编译
        /// </summary>
        public static VmlProgram CompileFile(string filePath, List<string> includePaths = null, List<string> libraryPaths = null, bool autoLinkStdLib = true)
        {
            string source = CompilerHelper.ReadSourceFile(filePath);
            string sourceDir = Path.GetDirectoryName(Path.GetFullPath(filePath));

            // 预处理
            return CompilerHelper.CompileFileStandard(filePath, "python", Compile, ExtractImports, PredefinedMacros, includePaths, libraryPaths, autoLinkStdLib);
        }

        private static List<string> ExtractImports(string source)
        {
            var libs = new List<string>();
            foreach (Match m in Regex.Matches(source, @"^import\s+(\w+)", RegexOptions.Multiline))
            {
                string name = m.Groups[1].Value;
                if (!libs.Contains(name)) libs.Add(name);
            }
            foreach (Match m in Regex.Matches(source, @"^from\s+(\w+)\s+import", RegexOptions.Multiline))
            {
                string name = m.Groups[1].Value;
                if (!libs.Contains(name)) libs.Add(name);
            }
            return libs;
        }

        /// <summary>
        /// 编译并输出 VML 汇编
        /// </summary>
        public static string CompileToString(string source)
        {
            var program = Compile(source);
            return program.ToString();
        }
        
        /// <summary>
        /// 编译文件并生成包含.include伪指令的VML文本
        /// </summary>
        public static string CompileFileWithIncludes(string filePath, List<string> includePaths = null, List<string> libraryPaths = null, bool autoLinkStdLib = true, bool useSharedLibrary = true)
            => CompilerHelper.BuildCompileFileWithIncludes(
                CompileFile(filePath, includePaths, null, false),
                filePath, libraryPaths, autoLinkStdLib, useSharedLibrary, "python");
    }
}
