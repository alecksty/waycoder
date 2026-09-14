using System;
using System.Collections.Generic;
using System.IO;
using VMLAssembler;
using CompilerBase;
using VMLPlugins;

namespace CppCompiler
{
    public class CppCompiler
    {
        private static readonly Dictionary<string, string> PredefinedMacros = new(CompilerHelper.BasePredefinedMacros())
        {
            ["__cplusplus"] = "199711L",
        };

        public static VmlProgram Compile(string source)
        {
            source = CompilerHelper.InjectDefines(source, "c", CompilerOptionsContext.Current);
            if (source.Contains('#'))
            {
                var incPaths = new List<string> {
                    Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "..", "..", "..", "..", "Lib", "cpp"),
                    Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "..", "..", "..", "..", "Lib", "c"),
                };
                var pp = new Preprocessor(source, incPaths, PredefinedMacros);
                source = pp.Process();
            }
            return CompilerHelper.CompileWithDiagnostics(null, diagnostics =>
            {
                Lexer lexer = new Lexer(source) { FileName = "<input>", Diagnostics = diagnostics };
                var tokens = lexer.Tokenize();
                bool isMCU = CompilerOptionsContext.Current.IsMCU;
                Parser parser = new Parser(tokens, isMCU) { FileName = "<input>", Diagnostics = diagnostics };
                var ast = parser.Parse();
                CodeGenerator codeGen = new CodeGenerator(ast);
                codeGen.SourceLines = source.Split('\n');
                return codeGen.GenerateCode();
            });
        }

        public static VmlProgram CompileFile(string filePath, List<string> includePaths = null, List<string> libraryPaths = null, bool autoLinkStdLib = false)
        {
            string source = CompilerHelper.ReadSourceFile(filePath);
            string sourceDir = Path.GetDirectoryName(Path.GetFullPath(filePath));

            source = CompilerHelper.InjectDefines(source, "c", CompilerOptionsContext.Current);
            if (source.Contains('#'))
            {
                var pp = new Preprocessor(source, includePaths, PredefinedMacros);
                source = pp.Process(filePath);
                // DEBUG: System.IO.File.WriteAllText("/tmp/cpp_preprocessed.txt", source);
            }
            Lexer lexer = new Lexer(source);
            var tokens = lexer.Tokenize();
            bool isMCU = CompilerOptionsContext.Current.IsMCU;
            Parser parser = new Parser(tokens, isMCU);
            var ast = parser.Parse();
            CodeGenerator codeGen = new CodeGenerator(ast);
            codeGen.SourceLines = source.Split('\n');
            var prog = codeGen.GenerateCode();

            // 构建库路径列表
            List<string> allLibraryPaths = new List<string>();

            if (libraryPaths != null)
                allLibraryPaths.AddRange(libraryPaths);

            // #param lib(...) 指定的库 (已去重)
            foreach (var lib in lexer.ParamLibraries)
            {
                string resolved = CompilerHelper.ResolveImportLibrary(lib, sourceDir, "cpp");
                if (resolved != null)
                {
                    if (!allLibraryPaths.Contains(resolved))
                        allLibraryPaths.Add(resolved);
                }
                else
                    throw new CompilationException(ErrorCode.Compilation_InternalError, $"CppCompiler: 找不到 #param 指定的库: {lib}");
            }

            if (autoLinkStdLib)
                CompilerHelper.LinkStandardLibrary(prog, "cpp", allLibraryPaths);
            else if (allLibraryPaths.Count > 0)
                VMLAssembler.LibraryLinker.LinkLibraries(prog, allLibraryPaths);

            return prog;
        }

        public static string CompileFileWithIncludes(string filePath, List<string> includePaths, List<string> libraryPaths, bool autoLinkStdLib, bool useSharedLibrary)
        {
            VmlProgram program = CompileFile(filePath, includePaths, null, false);
            return CompilerHelper.BuildCompileFileWithIncludes(
                program, filePath, libraryPaths, autoLinkStdLib, useSharedLibrary, "cpp");
        }

    }
}
