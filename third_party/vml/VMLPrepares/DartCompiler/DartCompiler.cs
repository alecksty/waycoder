using System;
using System.Collections.Generic;
using VMLAssembler;
using System.IO;
using CompilerBase;
using VMLPlugins;

namespace DartCompiler;

public class DartCompiler
{
    private static readonly Dictionary<string, string> PredefinedMacros = new(CompilerHelper.BasePredefinedMacros())
    {
        ["__DART__"] = "1",
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
            CompilerHelper.LinkStandardLibrary(prog, "dart", null);
            return prog;
        });
    }

    public static VmlProgram CompileFile(string filePath, List<string>? includePaths = null, List<string>? libraryPaths = null, bool autoLinkStdLib = true)
    {
        string source = CompilerHelper.ReadSourceFile(filePath);
        string sourceDir = Path.GetDirectoryName(Path.GetFullPath(filePath))!;

        return CompilerHelper.CompileFileStandard(filePath, "dart", Compile, ExtractImports, PredefinedMacros, includePaths, libraryPaths, autoLinkStdLib);
    }

    public static string CompileFileWithIncludes(string filePath, List<string>? includePaths = null, List<string>? libraryPaths = null, bool autoLinkStdLib = true, bool useSharedLibrary = true)
        => CompilerHelper.BuildCompileFileWithIncludes(
            CompileFile(filePath, includePaths, null, false),
            filePath, libraryPaths, autoLinkStdLib, useSharedLibrary, "dart");

    private static List<string> ExtractImports(string source)
    {
        var libs = new List<string>();
        // Match dart imports: import '...';
        foreach (System.Text.RegularExpressions.Match m in System.Text.RegularExpressions.Regex.Matches(
            source, @"import\s+'(?:[^']*\.vml|[^']+)'", System.Text.RegularExpressions.RegexOptions.Multiline))
        {
            string full = m.Value;
            // Extract the path between quotes
            int start = full.IndexOf('\'') + 1;
            int end = full.LastIndexOf('\'');
            if (start > 0 && end > start)
            {
                string name = full.Substring(start, end - start);
                // Strip .vml extension and dart: prefix for library resolution
                if (name.EndsWith(".vml", StringComparison.OrdinalIgnoreCase))
                    name = name[..^4];
                if (name.StartsWith("dart:"))
                    name = name[5..];
                if (name.StartsWith("package:"))
                    name = name[8..];
                if (!libs.Contains(name)) libs.Add(name);
            }
        }
        return libs;
    }
}
