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
            List<(string, int)>? lineMap = null;
            if (source.Contains('#'))
            {
                var incPaths = new List<string> {
                    Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "..", "..", "..", "..", "Lib", "cpp"),
                    Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "..", "..", "..", "..", "Lib", "c"),
                };
                var pp = new Preprocessor(source, incPaths, PredefinedMacros);
                // 这里没有真实路径，用 `<input>` 当主文件名（**不是** 默认的 `<unknown>`）：
                // 宿主侧把 `<input>` 认作"正在编的这个文件"，于是行号照样映射得回来；
                // 叫 `<unknown>` 反而会被当成**另一个文件**，错误就只能在列表里躺着。
                source = pp.Process("<input>");
                lineMap = pp.LineMap;
            }
            return CompilerHelper.CompileWithDiagnostics(null, diagnostics =>
            {
                Lexer lexer = new Lexer(source, lineMap, "<input>") { Diagnostics = diagnostics };
                var tokens = lexer.Tokenize();
                bool isMCU = CompilerOptionsContext.Current.IsMCU;
                Parser parser = new Parser(tokens, isMCU) { FileName = "<input>", SourceLineMap = lineMap, Diagnostics = diagnostics };
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
            // 预处理会把 `#include` 的头文件内容**拼进同一个流** ⇒ 之后拿到的行号全是拼接后的。
            // `LineMap`（第 N 项 = 输出第 N 行的 (原文件, 原行)）是**唯一**能把它们换回原文件的东西，
            // 一路往下传给词法器/解析器（见 `LexerBase.SourceLineMap`），
            // 不传的话报错就会指到用户文件里别的行上（实测：`#include <stdio.h>` 有 103 行，
            // 它后面的错误整体前移/后移，最终落在一句无害的 `/// <summary>` 上）。
            List<(string, int)>? lineMap = null;
            if (source.Contains('#'))
            {
                var pp = new Preprocessor(source, includePaths, PredefinedMacros);
                source = pp.Process(filePath);
                lineMap = pp.LineMap;
                // DEBUG: System.IO.File.WriteAllText("/tmp/cpp_preprocessed.txt", source);
            }
            Lexer lexer = new Lexer(source, lineMap, filePath);
            var tokens = lexer.Tokenize();
            bool isMCU = CompilerOptionsContext.Current.IsMCU;
            Parser parser = new Parser(tokens, isMCU) { FileName = filePath, SourceLineMap = lineMap };
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
