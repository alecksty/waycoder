using System;
using System.Collections.Generic;
using VMLAssembler;
using System.IO;
using CompilerBase;
using VMLPlugins;

namespace LadderCompiler
{
    /// <summary>
    /// 梯形图编译器
    /// </summary>
    public class LadderCompiler
    {
        private static readonly Dictionary<string, string> PredefinedMacros = new(CompilerHelper.BasePredefinedMacros())
        {
            ["__LADDER__"] = "1",
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
                var tokens = lexer.Tokenize();
                var parser = new Parser(tokens) { FileName = "<input>", Diagnostics = diagnostics };
                var ast = parser.Parse();
                var codeGen = new CodeGenerator((ProgramNode)ast);
                codeGen.SourceLines = source.Split('\n');
                return codeGen.GenerateCode();
            });
        }

        public static VmlProgram CompileFile(string filePath, List<string> includePaths = null, List<string> libraryPaths = null, bool autoLinkStdLib = true)
        {
            string source = CompilerHelper.ReadSourceFile(filePath);
            string sourceDir = Path.GetDirectoryName(Path.GetFullPath(filePath));

            // 预处理
            Preprocessor pp = null;
            if (source.Contains('#'))
            {
                pp = new Preprocessor(source, includePaths, PredefinedMacros);
                source = pp.Process(filePath);
            }

            var lexer = new Lexer(source);
            var tokens = lexer.Tokenize();

            var parser = new Parser(tokens);
            var ast = parser.Parse();

            var codeGen = new CodeGenerator((ProgramNode)ast);
            codeGen.SourceLines = source.Split('\n');
            var prog = codeGen.GenerateCode();

            List<string> allLibraryPaths = new List<string>();
            if (libraryPaths != null)
                allLibraryPaths.AddRange(libraryPaths);

            if (pp != null)
            {
                foreach (var lib in pp.ParamLibraries)
                {
                    string resolved = CompilerHelper.ResolveImportLibrary(lib, sourceDir, "ladder");
                    if (resolved != null && !allLibraryPaths.Contains(resolved))
                        allLibraryPaths.Add(resolved);
                }
            }
            foreach (var lib in lexer.ParamLibraries)
            {
                string resolved = CompilerHelper.ResolveImportLibrary(lib, sourceDir, "ladder");
                if (resolved != null && !allLibraryPaths.Contains(resolved))
                    allLibraryPaths.Add(resolved);
            }

            if (autoLinkStdLib)
                CompilerHelper.LinkStandardLibrary(prog, "ladder", allLibraryPaths);
            else if (allLibraryPaths.Count > 0)
                LibraryLinker.LinkLibraries(prog, allLibraryPaths);

            return prog;
        }
        
        /// <summary>
        /// 编译文件并生成包含.include伪指令的VML文本
        /// </summary>
        public static string CompileFileWithIncludes(string filePath, List<string> includePaths = null, List<string> libraryPaths = null, bool autoLinkStdLib = true, bool useSharedLibrary = true)
            => CompilerHelper.BuildCompileFileWithIncludes(
                CompileFile(filePath, includePaths, null, false),
                filePath, libraryPaths, autoLinkStdLib, useSharedLibrary, "ladder");
    }
}