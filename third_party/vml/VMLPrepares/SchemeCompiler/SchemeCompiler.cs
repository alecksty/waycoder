#nullable enable

using System;
using System.Collections.Generic;
using System.IO;
using VMLAssembler;
using CompilerBase;
using VMLPlugins;

namespace SchemeCompiler;
public class SchemeCompiler {
    private static readonly Dictionary<string, string> PredefinedMacros = new(CompilerHelper.BasePredefinedMacros())
    {
        ["__SCHEME__"] = "1",
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
            var tokens = new Lexer(source) { FileName = "<input>", Diagnostics = diagnostics }.Tokenize();
            var ast = new Parser(tokens) { FileName = "<input>", Diagnostics = diagnostics }.Parse();
            var gen = new CodeGenerator(ast);
            gen.SourceLines = source.Split('\n');
            var prog = gen.GenerateCode();
            CompilerHelper.LinkStandardLibrary(prog, "scheme", null);
            return prog;
        });
    }

    public static VmlProgram CompileFile(string filePath, List<string>? includePaths = null, List<string>? libraryPaths = null, bool autoLinkStdLib = true)
        => CompilerHelper.CompileFileStandard(filePath, "scheme", Compile, ExtractImports, PredefinedMacros, includePaths, libraryPaths, autoLinkStdLib);

    public static string CompileFileWithIncludes(string filePath, List<string>? includePaths = null, List<string>? libraryPaths = null, bool autoLinkStdLib = true, bool useSharedLibrary = true)
        => CompilerHelper.BuildCompileFileWithIncludes(
            CompileFile(filePath, includePaths, null, false),
            filePath, libraryPaths ?? new List<string>(), autoLinkStdLib, useSharedLibrary, "scheme");

    private static List<string> ExtractImports(string source)
    {
        var libs = new List<string>();
        foreach (System.Text.RegularExpressions.Match m in System.Text.RegularExpressions.Regex.Matches(source, @"\(load\s+""([^""]+)""", System.Text.RegularExpressions.RegexOptions.Multiline))
        {
            string name = m.Groups[1].Value;
            if (!libs.Contains(name)) libs.Add(name);
        }
        return libs;
    }
}
