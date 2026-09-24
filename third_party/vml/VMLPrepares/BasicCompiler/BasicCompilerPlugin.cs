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

        /* ── `'$INCLUDE: '文件.bi'` ──────────────────────────────────────────────
         *
         * QBasic/QuickBASIC 的**元命令**：把一个「BASIC 头文件」（约定后缀 `.bi`）整份内联进来。
         * 老程序用它共享 `DECLARE` / `NATIVE` 声明 / `CONST`。
         *
         * ## 为什么必须有（不是"锦上添花"）
         *
         * 元命令的正体是 `'$INCLUDE: ...`，而 **`'` 在 BASIC 里是注释** —— 所以这条命令
         * 在这之前**一个字节都不生效、而且不报错**：程序照编、照跑，只是被包含的那批
         * `NATIVE SUB/FUNCTION` 声明**一条都没有**，于是调用点全落到 `func_xxx` 前缀上，
         * 链接期报一串「未定义的函数 'func_ui_win_open'」。
         * 报出来的名字与源码里写的不是同一个、也指不到是哪一行 —— 这正是本仓
         * 「错误信息指不到根因」那一类。现在它在**编译期**就把文件读进来，
         * 找不到文件当场报错（带行号与找过的目录）。
         *
         * ## 判据
         *
         * · 只认「**整行**就是一个元命令」：行首（可有空白）是 `'` 或 `REM`，紧接 `$INCLUDE`。
         *   行尾的 `'$INCLUDE` 不认 —— 那更像一句注释。
         * · 文件名走**单引号**（QBasic 的两种写法 `'$INCLUDE: 'x.bi'` 与 `$INCLUDE: 'x.bi'` 都收）。
         * · 相对路径**以包含者的目录为基准**（嵌套包含层层相对），绝对路径原样用。
         * · 深度上限与**循环包含**都当场报错：不报的话要么栈溢出、要么静默半份程序。
         *
         * ⚠ 这一步必须排在 **Preprocessor 之前** —— 被包含文件自己可能带 `#` 指令，
         *   内联进来之后才会被那一步看到。
         * ⚠ 与 `Lib/c/waycoder_ui.h`（C 侧同一个 UI 接口的头文件）是**两份**：语言不同，
         *   没法共用一份源码；`Examples/basic/waycoder_ui.bi` 由脚本从那份 C 头文件生成，
         *   见该文件头部（**改接口先改 `waycoder_ui.h`，再重跑生成脚本**）。
         */
        private static readonly System.Text.RegularExpressions.Regex IncludeMetaCommand =
            new(@"^\s*(?:'|REM\s+)\s*\$INCLUDE\s*:?\s*'([^']*)'",
                System.Text.RegularExpressions.RegexOptions.IgnoreCase
                | System.Text.RegularExpressions.RegexOptions.Compiled);

        /// <summary>`'$INCLUDE` 的嵌套深度上限。</summary>
        private const int MaxIncludeDepth = 8;

        /// <summary>
        /// 把 `'$INCLUDE: 'x.bi'` 展开成被包含文件的正文（递归）。
        /// </summary>
        /// <param name="baseDir">相对路径的基准目录；null = 进程当前目录（`Compile` 那条没有文件路径的入口）。</param>
        /// <param name="stack">正在展开的文件链（循环检测 + 报错时能把整条链打出来）。</param>
        private static string ExpandIncludes(string source, string? baseDir, List<string>? stack = null, int depth = 0)
        {
            stack ??= new List<string>();
            if (depth > MaxIncludeDepth)
                throw new CompilationException(
                    $"'$INCLUDE 嵌套超过 {MaxIncludeDepth} 层（链：{string.Join(" -> ", stack)}）");

            var sb = new System.Text.StringBuilder(source.Length);
            var dir = baseDir ?? Directory.GetCurrentDirectory();
            foreach (var rawLine in source.Split('\n'))
            {
                var line = rawLine.TrimEnd('\r');
                var m = IncludeMetaCommand.Match(line);
                if (!m.Success)
                {
                    sb.Append(line).Append('\n');
                    continue;
                }

                var name = m.Groups[1].Value.Trim();
                if (name.Length == 0)
                    throw new CompilationException($"'$INCLUDE 后面没写文件名：{line.Trim()}");

                var full = Path.GetFullPath(Path.IsPathRooted(name) ? name : Path.Combine(dir, name));
                if (!File.Exists(full))
                    throw new CompilationException(
                        $"'$INCLUDE 找不到文件 '{name}'（在 {dir} 下找过；写成相对路径时" +
                        $"以**包含它的那个文件**所在目录为基准）");

                // 循环包含：报出来并给出整条链，别让它静静地展开到自己撑爆栈
                if (stack.Any(s => string.Equals(s, full, StringComparison.OrdinalIgnoreCase)))
                    throw new CompilationException(
                        $"'$INCLUDE 循环包含：{string.Join(" -> ", stack)} -> {full}");

                stack.Add(full);
                sb.Append(ExpandIncludes(File.ReadAllText(full), Path.GetDirectoryName(full), stack, depth + 1));
                stack.RemoveAt(stack.Count - 1);
                sb.Append('\n');
            }
            return sb.ToString();
        }

        public static VmlProgram Compile(string source, bool autoLink = true)
        {
            source = ExpandIncludes(source, null);
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
                // `'$INCLUDE: 'x.bi'` 先展开（相对路径以本文件所在目录为基准）——
                // 必须排在 Preprocessor 之前，被包含文件自己带的 `#` 指令才会被看到。
                var source = ExpandIncludes(File.ReadAllText(filePath), sourceDir);
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
