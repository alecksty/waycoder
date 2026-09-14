#nullable enable

using System;
using System.Collections.Generic;
using System.IO;
using VMLAssembler;
using CompilerBase;
using VMLPlugins;

namespace KotlinCompiler;
public class KotlinCompiler {
    private static readonly Dictionary<string, string> PredefinedMacros = new(CompilerHelper.BasePredefinedMacros())
    {
        ["__KOTLIN__"] = "1",
    };

    public static VmlProgram Compile(string source) {
        source = CompilerHelper.InjectDefines(source, "c", CompilerOptionsContext.Current);
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
            var parser = new Parser(tokens) { FileName = "<input>", Diagnostics = diagnostics };
            var program = parser.Parse();
            var codegen = new CodeGenerator(program);
            codegen.SourceLines = source.Split('\n');
            var prog = codegen.GenerateCode();
            CompilerHelper.LinkStandardLibrary(prog, "kotlin", null);
            return prog;
        });
    }

    public static VmlProgram CompileFile(string filePath, List<string>? includePaths = null, List<string>? libraryPaths = null, bool autoLinkStdLib = true)
        => CompilerHelper.CompileFileStandard(filePath, "kotlin", Compile, ExtractImports, PredefinedMacros, includePaths, libraryPaths, autoLinkStdLib);

    public static string CompileFileWithIncludes(string filePath, List<string>? includePaths = null, List<string>? libraryPaths = null, bool autoLinkStdLib = true, bool useSharedLibrary = true)
        => CompilerHelper.BuildCompileFileWithIncludes(
            CompileFile(filePath, includePaths, null, false),
            filePath, libraryPaths ?? new List<string>(), autoLinkStdLib, useSharedLibrary, "kotlin");

    private static List<string> ExtractImports(string source)
    {
        var libs = new List<string>();
        foreach (System.Text.RegularExpressions.Match m in System.Text.RegularExpressions.Regex.Matches(source, @"import\s+((?:[\w]+\.)*[\w]+)\.\*", System.Text.RegularExpressions.RegexOptions.Multiline))
        {
            string name = m.Groups[1].Value;  // 完整包名如 "a.b.c.d"
            if (!libs.Contains(name)) libs.Add(name);
        }
        return libs;
    }
}
