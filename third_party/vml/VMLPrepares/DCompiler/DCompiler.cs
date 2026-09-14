using System;
using System.Collections.Generic;
using VMLAssembler;
using System.IO;
using CompilerBase;
using VMLPlugins;

namespace DCompiler;

public class DCompiler
{
    private static readonly Dictionary<string, string> PredefinedMacros = new(CompilerHelper.BasePredefinedMacros())
    {
        ["__D__"] = "1",
    };

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
            var parser = new Parser(lexer.Tokens) { FileName = "<input>", Diagnostics = diagnostics };
            var ast = parser.Parse();
            var codeGenerator = new CodeGenerator();
            codeGenerator.SourceLines = source.Split('\n');
            var prog = codeGenerator.GenerateCode(ast);
            CompilerHelper.LinkStandardLibrary(prog, "d", null);
            return prog;
        });
    }

    public static VmlProgram CompileFile(string filePath, List<string>? includePaths = null, List<string>? libraryPaths = null, bool autoLinkStdLib = true)
    {
        return CompilerHelper.CompileFileStandard(filePath, "d", Compile, ExtractImports, PredefinedMacros, includePaths, libraryPaths, autoLinkStdLib);
    }

    public static string CompileFileWithIncludes(string filePath, List<string>? includePaths = null, List<string>? libraryPaths = null, bool autoLinkStdLib = true, bool useSharedLibrary = true)
        => CompilerHelper.BuildCompileFileWithIncludes(
            CompileFile(filePath, includePaths, null, false),
            filePath, libraryPaths, autoLinkStdLib, useSharedLibrary, "d");

    private static List<string> ExtractImports(string source)
    {
        var libs = new List<string>();
        foreach (System.Text.RegularExpressions.Match m in System.Text.RegularExpressions.Regex.Matches(
            source, @"import\s+(?:[^;]+?\.([a-zA-Z_][a-zA-Z0-9_]*)\s*;|([a-zA-Z_][a-zA-Z0-9_]*)\s*;)", System.Text.RegularExpressions.RegexOptions.Multiline))
        {
            string name = m.Groups[1].Success ? m.Groups[1].Value : m.Groups[2].Value;
            if (!string.IsNullOrEmpty(name) && !libs.Contains(name)) libs.Add(name);
        }
        return libs;
    }
}
