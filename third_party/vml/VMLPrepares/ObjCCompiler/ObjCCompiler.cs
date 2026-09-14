using System;
using System.Collections.Generic;
using VMLAssembler;
using System.IO;
using CompilerBase;
using VMLPlugins;

namespace ObjCCompiler;

public class ObjCCompiler
{
    private static readonly Dictionary<string, string> PredefinedMacros = new(CompilerHelper.BasePredefinedMacros())
    {
        ["__OBJC__"] = "1",
        ["__stdcall"] = "",
        ["__cdecl"] = "",
        ["__fastcall"] = "",
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
            codeGenerator._structDefs = parser._structDefs;
            codeGenerator.SourceLines = source.Split('\n');
            var prog = codeGenerator.GenerateCode(ast);
            CompilerHelper.LinkStandardLibrary(prog, "objc", null);
            return prog;
        });
    }

    public static VmlProgram CompileFile(string filePath, List<string>? includePaths = null, List<string>? libraryPaths = null, bool autoLinkStdLib = true)
    {
        string source = CompilerHelper.ReadSourceFile(filePath);
        string sourceDir = Path.GetDirectoryName(Path.GetFullPath(filePath))!;

        return CompilerHelper.CompileFileStandard(filePath, "objc", Compile, ExtractImports, PredefinedMacros, includePaths, libraryPaths, autoLinkStdLib);
    }

    public static string CompileFileWithIncludes(string filePath, List<string>? includePaths = null, List<string>? libraryPaths = null, bool autoLinkStdLib = true, bool useSharedLibrary = true)
        => CompilerHelper.BuildCompileFileWithIncludes(
            CompileFile(filePath, includePaths, null, false),
            filePath, libraryPaths, autoLinkStdLib, useSharedLibrary, "objc");

    private static List<string> ExtractImports(string source)
    {
        var libs = new List<string>();
        foreach (System.Text.RegularExpressions.Match m in System.Text.RegularExpressions.Regex.Matches(
            source, @"#import\s+[<""]([^>""]+)[>""]|#include\s+[<""]([^>""]+)[>""]",
            System.Text.RegularExpressions.RegexOptions.Multiline))
        {
            string name = m.Groups[1].Success ? m.Groups[1].Value : m.Groups[2].Value;
            if (!libs.Contains(name)) libs.Add(name);
        }
        return libs;
    }
}
