using System.Collections.Generic;
using System.Text;
using CompilerBase;
using VMLAssembler;
using VMLPlugins;

namespace SwiftCompiler
{
    /// <summary>
    /// Swift语言编译器
    /// </summary>
    public class SwiftCompiler
    {
        private static readonly Dictionary<string, string> PredefinedMacros = new(CompilerHelper.BasePredefinedMacros())
        {
            ["__SWIFT__"] = "1",
        };

        public static VmlProgram CompileFile(string filePath, List<string>? includePaths = null, List<string>? libraryPaths = null, bool autoLinkStdLib = true)
        {
            return CompilerHelper.CompileFileStandard(filePath, "swift", s => new SwiftCompiler().Compile(s), ExtractImports, PredefinedMacros, includePaths, libraryPaths, autoLinkStdLib);
        }

        private static List<string> ExtractImports(string source)
        {
            var libs = new List<string>();
            foreach (System.Text.RegularExpressions.Match m in System.Text.RegularExpressions.Regex.Matches(source, @"^import\s+(\w+)", System.Text.RegularExpressions.RegexOptions.Multiline))
            {
                string name = m.Groups[1].Value;
                if (!libs.Contains(name)) libs.Add(name);
            }
            return libs;
        }

        public string CompileFileWithIncludes(string filePath, List<string>? includePaths = null, List<string>? libraryPaths = null, bool autoLinkStdLib = true, bool useSharedLibrary = true)
            => CompilerHelper.BuildCompileFileWithIncludes(
                CompileFile(filePath, includePaths, null, false),
                filePath, libraryPaths, autoLinkStdLib, useSharedLibrary, "swift");

        public VmlProgram Compile(string source, bool debugMode = false)
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
                var tokens = lexer.Tokenize();
                var parser = new Parser(tokens) { FileName = "<input>", Diagnostics = diagnostics };
                var program = parser.Parse();
                var codeGenerator = new CodeGenerator();
                codeGenerator.SourceLines = source.Split('\n');
                var vmlProgram = codeGenerator.Generate(program);
                return vmlProgram;
            });
        }

    }
}