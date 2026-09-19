using System;
using System.Collections.Generic;
using VMLAssembler;
using System.IO;
using CompilerBase;
using VMLPlugins;

namespace FortranCompiler;

public class FortranCompiler
{
    private static readonly Dictionary<string, string> PredefinedMacros = new(CompilerHelper.BasePredefinedMacros())
    {
        ["__FORTRAN__"] = "1",
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
            // `implicit none` 是**每个源文件**的开关（不是每语言的）—— 从解析器带过来，
            // 决定未声明变量是报错还是只警告。见 `Parser.ImplicitNone`。
            codeGenerator.StrictDeclarations = parser.ImplicitNone;
            var prog = codeGenerator.GenerateCode(ast);
            CompilerHelper.LinkStandardLibrary(prog, "fortran", null);
            return prog;
        });
    }

    public static VmlProgram CompileFile(string filePath, List<string>? includePaths = null, List<string>? libraryPaths = null, bool autoLinkStdLib = true)
    {
        string source = CompilerHelper.ReadSourceFile(filePath);
        string sourceDir = Path.GetDirectoryName(Path.GetFullPath(filePath))!;

        return CompilerHelper.CompileFileStandard(filePath, "fortran", Compile, ExtractImports, PredefinedMacros, includePaths, libraryPaths, autoLinkStdLib);
    }

    public static string CompileFileWithIncludes(string filePath, List<string>? includePaths = null, List<string>? libraryPaths = null, bool autoLinkStdLib = true, bool useSharedLibrary = true)
        => CompilerHelper.BuildCompileFileWithIncludes(
            CompileFile(filePath, includePaths, null, false),
            filePath, libraryPaths, autoLinkStdLib, useSharedLibrary, "fortran");

    private static List<string> ExtractImports(string source)
    {
        var libs = new List<string>();
        foreach (System.Text.RegularExpressions.Match m in System.Text.RegularExpressions.Regex.Matches(
            source, @"use\s+(\w+)", System.Text.RegularExpressions.RegexOptions.IgnoreCase | System.Text.RegularExpressions.RegexOptions.Multiline))
        {
            string name = m.Groups[1].Value;
            if (!libs.Contains(name)) libs.Add(name);
        }
        return libs;
    }
}
