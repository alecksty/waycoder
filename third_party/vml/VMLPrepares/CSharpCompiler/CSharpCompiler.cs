using System.Collections.Generic;
using CompilerBase;
using VMLAssembler;
using VMLPlugins;

namespace CSharpCompiler
{
    /// <summary>
    /// C#语言编译器
    /// </summary>
    public class CSharpCompiler
    {
        private static readonly Dictionary<string, string> PredefinedMacros = new(CompilerHelper.BasePredefinedMacros())
        {
            ["__CSHARP__"] = "1",
        };

        public VmlProgram Compile(string source, bool debugMode = false)
        {
            source = CompilerHelper.InjectDefines(source, "csharp", CompilerOptionsContext.Current);
            if (source.Contains('#'))
            {
                var pp = new Preprocessor(source, null, PredefinedMacros);
                source = pp.Process();
            }

            var vmlProgram = CompilerHelper.CompileWithDiagnostics(null, diagnostics =>
            {
                var lexer = new Lexer(source) { FileName = "<input>", Diagnostics = diagnostics };
                var tokens = lexer.Tokenize();
                var parser = new Parser(tokens, debugMode) { FileName = "<input>", Diagnostics = diagnostics };
                var program = parser.Parse();
                var codeGenerator = new CodeGenerator();
                codeGenerator.SourceLines = source.Split('\n');
                return codeGenerator.Generate(program);
            });

            // LinkStandardLibrary 由 CompileCore/CompileFileStandard 统一调用, 避免双重链接
            return vmlProgram;
        }

        public static VmlProgram CompileFile(string filePath, List<string>? includePaths = null, List<string>? libraryPaths = null, bool autoLinkStdLib = true)
        {
            return CompilerHelper.CompileFileStandard(filePath, "csharp", s => new CSharpCompiler().Compile(s), ExtractImports, PredefinedMacros, includePaths, libraryPaths, autoLinkStdLib);
        }

        private static List<string> ExtractImports(string source)
        {
            var libs = new List<string>();
            foreach (System.Text.RegularExpressions.Match m in System.Text.RegularExpressions.Regex.Matches(source, @"using\s+(\w+)\s*;", System.Text.RegularExpressions.RegexOptions.Multiline))
            {
                string name = m.Groups[1].Value;
                if (!libs.Contains(name)) libs.Add(name);
            }
            return libs;
        }

        public string CompileFileWithIncludes(string filePath, List<string>? includePaths = null, List<string>? libraryPaths = null, bool autoLinkStdLib = true, bool useSharedLibrary = true)
            => CompilerHelper.BuildCompileFileWithIncludes(
                CompileFile(filePath, includePaths, null, false),
                filePath, libraryPaths, autoLinkStdLib, useSharedLibrary, "csharp");
    }
}