using VMLAssembler;
using System.Collections.Generic;
using System.IO;
using System;
using System.Linq;
using CompilerBase;
using VMLPlugins;

namespace PascalCompiler
{
    /// <summary>
    /// Pascal 语言编译器
    /// </summary>
    public class PascalCompiler
    {
        private static readonly Dictionary<string, string> PredefinedMacros = new(CompilerHelper.BasePredefinedMacros())
        {
            ["__PASCAL__"] = "1",
        };

        public static VmlProgram Compile(string source, List<(string, int)> lineMap = null)
        {
            source = CompilerHelper.InjectDefines(source, "c", CompilerOptionsContext.Current);
            if (source.Contains('#'))
            {
                var pp = new Preprocessor(source, null, PredefinedMacros);
                source = pp.Process();
            }
            return CompilerHelper.CompileWithDiagnostics(null, diagnostics =>
            {
                Lexer lexer = new Lexer(source) { FileName = "<input>", Diagnostics = diagnostics };
                var tokens = lexer.Tokenize();
                Parser parser = new Parser(tokens) { FileName = "<input>", Diagnostics = diagnostics };
                var ast = parser.Parse();
                if (ast is ProgramNode programNode)
                {
                    CodeGenerator codeGen = new CodeGenerator(programNode);
                    codeGen.SourceLines = source.Split('\n');
                    codeGen.UseCrtOutput = parser.UsesNames.Contains("crt");
                    return codeGen.GenerateCode();
                }
                if (ast is UnitNode unitNode)
                {
                    CodeGenerator codeGen = new CodeGenerator(unitNode);
                    codeGen.SourceLines = source.Split('\n');
                    codeGen.UseCrtOutput = parser.UsesNames.Contains("crt");
                    return codeGen.GenerateCode();
                }
                throw new CodeGenerationException(ErrorCode.CodeGen_UnsupportedExpression, "不支持的AST节点类型");
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

            Lexer lexer = new Lexer(source);
            var tokens = lexer.Tokenize();

            Parser parser = new Parser(tokens);
            var ast = parser.Parse();

            VmlProgram prog;
            if (ast is ProgramNode programNode)
            {
                CodeGenerator codeGen = new CodeGenerator(programNode);
                codeGen.SourceLines = source.Split('\n');
                codeGen.UseCrtOutput = parser.UsesNames.Contains("crt");
                prog = codeGen.GenerateCode();
            }
            else if (ast is UnitNode unitNode)
            {
                CodeGenerator codeGen = new CodeGenerator(unitNode);
                codeGen.SourceLines = source.Split('\n');
                prog = codeGen.GenerateCode();
                prog.IsLibrary = true; // 单元编译为库模式
            }
            else
                throw new CompilationException(ErrorCode.Compilation_InternalError, "不支持的AST节点类型");

            List<string> allLibraryPaths = new List<string>();
            if (libraryPaths != null)
                allLibraryPaths.AddRange(libraryPaths);

            // 合并预处理器和 Lexer 中收集的 #param lib 库
            if (pp != null)
            {
                foreach (var lib in pp.ParamLibraries)
                {
                    string resolved = CompilerHelper.ResolveImportLibrary(lib, sourceDir, "pascal");
                    if (resolved != null && !allLibraryPaths.Contains(resolved))
                        allLibraryPaths.Add(resolved);
                }
            }
            foreach (var lib in lexer.ParamLibraries)
            {
                string resolved = CompilerHelper.ResolveImportLibrary(lib, sourceDir, "pascal");
                if (resolved != null && !allLibraryPaths.Contains(resolved))
                    allLibraryPaths.Add(resolved);
            }

            // 解析 uses 子句中引用的单元文件 (v1.66.32+)
            List<string> searchPaths = new List<string> { sourceDir };
            if (includePaths != null) searchPaths.AddRange(includePaths);
            searchPaths.Add(Path.Combine(sourceDir, "..", "..", "..", "Lib", "pascal"));
            searchPaths.Add(Path.GetFullPath(Path.Combine(sourceDir, "..", "Lib", "pascal")));

            var unitPrograms = new List<VmlProgram>();
            foreach (string unitName in parser.UsesNames)
            {
                string unitPath = ResolveUnitFile(unitName, searchPaths);
                if (unitPath != null)
                {
                    // 递归编译单元文件 (处理嵌套 uses)
                    var unitProg = CompileFile(unitPath, includePaths, libraryPaths, false);
                    unitPrograms.Add(unitProg);
                }
            }

            // 链接所有单元到主程序 (主程序+单元 → 合并)
            if (unitPrograms.Count > 0)
            {
                // 先应用每个程序的 exports，创建公开别名
                prog.ApplyExports();
                foreach (var up in unitPrograms) up.ApplyExports();

                var allPrograms = new List<VmlProgram> { prog };
                allPrograms.AddRange(unitPrograms);
                prog = VmlProgram.Link(allPrograms);
            }

            // 自动检测 uses xxx → 链接对应库 (v1.66.33)
            AutoLinkUnit(parser, "dos", "dos.vml", "DOS 系统调用库", sourceDir, allLibraryPaths);
            AutoLinkUnit(parser, "graph", "graph.vml", "BGI 图形库", sourceDir, allLibraryPaths);
            AutoLinkUnit(parser, "conv", "conv.vml", "类型转换库", sourceDir, allLibraryPaths);

            // 注册 uses 单元的外部函数返回类型 (v1.66.46)
            foreach (string un in parser.UsesNames) { string up = ResolveUnitFile(un, searchPaths); if (up != null) RegisterUnitFunctions(up); }

            if (autoLinkStdLib)
                CompilerHelper.LinkStandardLibrary(prog, "pascal", allLibraryPaths);
            else if (allLibraryPaths.Count > 0)
                VMLAssembler.LibraryLinker.LinkLibraries(prog, allLibraryPaths);

            return prog;
        }

        /// <summary>
        /// 搜索单元文件: 先找 .pas 源文件，再找 .vml 预编译库
        /// </summary>
        private static string ResolveUnitFile(string unitName, List<string> searchPaths)
        {
            foreach (var dir in searchPaths)
            {
                if (string.IsNullOrEmpty(dir)) continue;
                string pasPath = Path.Combine(dir, unitName + ".pas");
                if (File.Exists(pasPath)) return pasPath;
                string vmlPath = Path.Combine(dir, unitName + ".vml");
                if (File.Exists(vmlPath)) return vmlPath;
                // 不区分大小写
                if (Directory.Exists(dir))
                {
                    foreach (var f in Directory.GetFiles(dir, "*.pas", SearchOption.TopDirectoryOnly))
                    {
                        if (string.Equals(Path.GetFileNameWithoutExtension(f), unitName, StringComparison.OrdinalIgnoreCase))
                            return f;
                    }
                    foreach (var f in Directory.GetFiles(dir, "*.vml", SearchOption.TopDirectoryOnly))
                    {
                        if (string.Equals(Path.GetFileNameWithoutExtension(f), unitName, StringComparison.OrdinalIgnoreCase))
                            return f;
                    }
                }
            }
            return null;
        }
        
        /// <summary>
        /// 编译文件并生成包含.include伪指令的VML文本
        /// </summary>
        public static string CompileFileWithIncludes(string filePath, List<string> includePaths = null, List<string> libraryPaths = null, bool autoLinkStdLib = true, bool useSharedLibrary = true)
            => CompilerHelper.BuildCompileFileWithIncludes(
                CompileFile(filePath, includePaths, null, false),
                filePath, libraryPaths, autoLinkStdLib, useSharedLibrary, "pascal");

    /// <summary>
    /// 自动检测 uses 子句中的单元名, 并链接对应的共享库 (v1.66.33)
    /// </summary>
    private static void RegisterUnitFunctions(string unitPath)
        {
            try {
                string s = File.ReadAllText(unitPath);
                var lx = new Lexer(s); var up = new Parser(lx.Tokenize()); var ast = up.Parse();
                if (ast is UnitNode un)
                    foreach (var sub in un.InterfaceSubprograms)
                        if (sub is FunctionDeclarationNode fd)
                            CodeGenerator.ExternalFuncTypes[fd.Name.ToLower()] =
                                fd.ReturnType is SimpleTypeNode st ? st.TypeName.ToUpper() : "INTEGER";
            } catch { }
        }

        private static void AutoLinkUnit(Parser parser, string unitName, string libFile, string desc, string sourceDir, List<string> allLibraryPaths)
    {
        if (!parser.UsesNames.Any(u => string.Equals(u, unitName, StringComparison.OrdinalIgnoreCase)))
            return;

        var searchPaths = new List<string>
        {
            Path.Combine(sourceDir, libFile),
            Path.Combine(sourceDir, "..", "..", "..", "Lib", "shared", libFile),
            Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "..", "..", "..", "Lib", "shared", libFile),
            Path.Combine(Directory.GetCurrentDirectory(), "Lib", "shared", libFile),
        };
        string? foundPath = searchPaths.FirstOrDefault(File.Exists);
        if (foundPath != null && !allLibraryPaths.Contains(foundPath))
        {
            Console.WriteLine($"    自动链接: {libFile} ({desc})");
            allLibraryPaths.Add(foundPath);
        }
    }
}
}

