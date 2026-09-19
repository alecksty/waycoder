using System;
using System.Collections.Generic;
using System.IO;
using VMLAssembler;
using CompilerBase;
using VMLPlugins;

namespace BasicCompiler
{
    /// <summary>
    /// BASIC编译器静态类（流水线入口）
    /// </summary>
    public static class BasicCompiler
    {
        private static readonly Dictionary<string, string> BasicPredefinedMacros = new(CompilerHelper.BasePredefinedMacros())
        {
            ["__BASIC__"] = "1",
        };

        private static string GetDialectMacro(VMLPlugins.BasicDialect dialect) => dialect switch
        {
            VMLPlugins.BasicDialect.QBasic => "__QBASIC__",
            VMLPlugins.BasicDialect.TurboBasic => "__TURBOBASIC__",
            VMLPlugins.BasicDialect.FreeBasic => "__FREEBASIC__",
            VMLPlugins.BasicDialect.TrueBasic => "__TRUEBASIC__",
            VMLPlugins.BasicDialect.PureBasic => "__PUREBASIC__",
            VMLPlugins.BasicDialect.ChipBasic => "__CHIPBASIC__",
            VMLPlugins.BasicDialect.MiniBasic => "__MINIBASIC__",
            VMLPlugins.BasicDialect.GwBasic => "__GWBASIC__",
            VMLPlugins.BasicDialect.PowerBasic => "__POWERBASIC__",
            VMLPlugins.BasicDialect.VisualBasic => "__VISUALBASIC__",
            _ => "__QBASIC__"
        };

        public static VmlProgram Compile(string source, bool autoLink = true)
        {
            source = CompilerHelper.InjectDefines(source, "c", CompilerOptionsContext.Current);
            if (source.Contains('#'))
            {
                var pp = new Preprocessor(source, null, BasicPredefinedMacros);
                source = pp.Process();
            }

            return CompilerHelper.CompileWithDiagnostics(null, diagnostics =>
            {
                var lexer = new Lexer(source) { FileName = "<input>", Diagnostics = diagnostics };
                var tokens = lexer.Tokenize();
                var parser = new Parser(tokens) { FileName = "<input>", Diagnostics = diagnostics };
                var ast = parser.Parse();
                var codeGen = new CodeGenerator(ast);
                // `OPTION EXPLICIT` 是**每个源文件**的开关（不是每语言的）——
                // 决定未声明变量是报错还是只警告。见 `Parser.OptionExplicit`。
                codeGen.StrictDeclarations = parser.OptionExplicit;
                codeGen.SourceLines = source.Split('\n');
                var prog = codeGen.GenerateCode();
                if (autoLink)
                    CompilerHelper.LinkStandardLibrary(prog, "basic", null);
                return prog;
            });
        }

        public static VmlProgram CompileFile(string filePath, List<string>? includePaths = null, List<string>? libraryPaths = null, bool autoLinkStdLib = true)
        {
            var diagnostics = new DiagnosticBag();
            string sourceDir = Path.GetDirectoryName(Path.GetFullPath(filePath))!;
            Preprocessor? pp = null;
            try
            {
                var source = File.ReadAllText(filePath);
                if (source.Contains('#'))
                {
                    pp = new Preprocessor(source, includePaths, BasicPredefinedMacros);
                    source = pp.Process(filePath);
                }

                var lexer = new Lexer(source) { FileName = filePath, Diagnostics = diagnostics };
                var tokens = lexer.Tokenize();
                var parser = new Parser(tokens) { FileName = filePath, Diagnostics = diagnostics };
                var ast = parser.Parse();
                var codeGen = new CodeGenerator(ast);
                // 同 `Compile` 那条：`OPTION EXPLICIT` 逐文件生效
                codeGen.StrictDeclarations = parser.OptionExplicit;
                codeGen.SourceLines = source.Split('\n');
                var prog = codeGen.GenerateCode();

                var allLibraryPaths = new List<string>();
                if (libraryPaths != null)
                    allLibraryPaths.AddRange(libraryPaths);
                if (pp != null)
                {
                    foreach (var lib in pp.ParamLibraries)
                    {
                        var resolved = CompilerHelper.ResolveImportLibrary(lib, sourceDir, "basic");
                        if (resolved != null && !allLibraryPaths.Contains(resolved))
                            allLibraryPaths.Add(resolved);
                    }
                }
                foreach (var lib in lexer.ParamLibraries)
                {
                    var resolved = CompilerHelper.ResolveImportLibrary(lib, sourceDir, "basic");
                    if (resolved != null && !allLibraryPaths.Contains(resolved))
                        allLibraryPaths.Add(resolved);
                }

                if (autoLinkStdLib)
                    CompilerHelper.LinkStandardLibrary(prog, "basic", allLibraryPaths);
                else if (allLibraryPaths.Count > 0)
                    LibraryLinker.LinkLibraries(prog, allLibraryPaths);

                return prog;
            }
            catch (ParseException ex)
            {
                throw new CompilationException(ex.Code,
                    diagnostics.HasErrors ? diagnostics.FormatAll() : $"{filePath}: error: {ex.Message}", ex);
            }
            catch (CodeGenerationException ex)
            {
                throw new CompilationException(ex.Code, $"{filePath}: error: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// 编译文件并生成包含 .linked  伪指令的 VML 文本
        /// </summary>
        public static string CompileFileWithIncludes(string filePath, List<string>? includePaths = null, List<string>? libraryPaths = null, bool autoLinkStdLib = true, bool useSharedLibrary = true)
            => CompilerHelper.BuildCompileFileWithIncludes(
                CompileFile(filePath, includePaths, null, false),
                filePath, libraryPaths, autoLinkStdLib, useSharedLibrary, "basic");
    }

    /// <summary>
    /// BASIC编译器插件适配器 — 继承 CompilerPluginExBase，委托给 BasicCompiler 静态类
    /// </summary>
    public class BasicCompilerPlugin : CompilerPluginExBase
    {
        public override string Name => "basic";
        public override string Description => "BASIC语言编译器";
        public override string SupportedExtensions => ".bas";
        protected override Func<string, VmlProgram> CompileFunc => s => BasicCompiler.Compile(s, true);
        protected override Func<string, List<string>, List<string>, bool, VmlProgram> CompileFileFunc => BasicCompiler.CompileFile;
        protected override Func<string, List<string>, List<string>, bool, bool, string> CompileFileWithIncludesFunc => BasicCompiler.CompileFileWithIncludes;
    }
}
