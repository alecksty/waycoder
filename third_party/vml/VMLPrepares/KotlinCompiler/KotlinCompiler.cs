#nullable enable

using System;
using System.Collections.Generic;
using System.IO;
using VMLAssembler;
using CompilerBase;
using VMLPlugins;

namespace KotlinCompiler;
public class KotlinCompiler {
    private static readonly Dictionary<string, string> PredefinedMacros = new(CompilerHelper.BasePredefinedMacros())
    {
        ["__KOTLIN__"] = "1",
    };

    public static VmlProgram Compile(string source) {
        source = CompilerHelper.InjectDefines(source, "c", CompilerOptionsContext.Current);
        if (source.Contains('#'))
        {
            var pp = new Preprocessor(source, null, PredefinedMacros);
            source = pp.Process();
        }

        return CompilerHelper.CompileWithDiagnostics(null, diagnostics =>
        {
            bool isMCU = CompilerOptionsContext.Current.IsMCU;
            var lexer = new Lexer(source) { FileName = "<input>", Diagnostics = diagnostics };
            var tokens = lexer.Tokenize();
            var parser = new Parser(tokens) { FileName = "<input>", Diagnostics = diagnostics };
            var program = parser.Parse();
            var codegen = new CodeGenerator(program);
            codegen.SourceLines = source.Split('\n');
            var prog = codegen.GenerateCode();
            // ⚠ **不在这里链接标准库** —— 与 Java / C# / C / C++ 同一条约定
            //   （`JavaCompiler.cs:37`、`CSharpCompiler.cs:38`、`CCompiler.cs:101` 都写着
            //    「LinkStandardLibrary 由调用方统一处理，避免重复链接」）。
            //
            //   此前这里调的是 `LinkStandardLibrary(prog, "kotlin", null)` —— **库清单是 null**，
            //   于是只认 `builtins.vml`（自动）+ `SharedPrefixMap` 探测到的模块
            //   （`ui_` → `vmlui`，所以 `demo_ui` 能跑），而 `tty_*` / `initgraph` 这类
            //   **语言库里声明的函数**当场被判成「未定义的函数」**硬错误抛出**
            //   ⇒ 后面那次「带着真库清单」的链接（`CompileFileStandard` 里那次）**根本没机会跑**。
            //   症状：`--lib` 与 `Libs=` 对本门语言**完全失效**（实测 `demo_tty.kt`
            //   加不加 `--lib Lib/shared/tty.vml` 报的是同一串「未定义的函数」）；
            //   而手机端没有 `--lib` 入口，不修就永远失败。
            //
            //   为什么不选「把那个 null 换成真实库清单」：`Compile(source)` 的签名里
            //   **没有库清单**，`CompilerOptionsContext` / `CompilerOptions` 也**不携带**库路径
            //   （`CompilerConfig.LibraryPaths` 只活在实例上，本方法却是静态的）——
            //   那条路要在共享代码里新开一条「环境里藏着库清单」的通道，而调用方
            //   （`CompileFileStandard`）本来就已经带着真清单链过一次，纯属重复。
            //   何况预链接**还会越过调用方的 `autoLinkStdLib: false`**：`CompileFileWithIncludes`
            //   那条路明确要求"先不链、只产出 `.include` 交给宿主链"，预链接等于把它的决定推翻。
            return prog;
        });
    }

    public static VmlProgram CompileFile(string filePath, List<string>? includePaths = null, List<string>? libraryPaths = null, bool autoLinkStdLib = true)
        => CompilerHelper.CompileFileStandard(filePath, "kotlin", Compile, ExtractImports, PredefinedMacros, includePaths, libraryPaths, autoLinkStdLib);

    public static string CompileFileWithIncludes(string filePath, List<string>? includePaths = null, List<string>? libraryPaths = null, bool autoLinkStdLib = true, bool useSharedLibrary = true)
        => CompilerHelper.BuildCompileFileWithIncludes(
            CompileFile(filePath, includePaths, null, false),
            filePath, libraryPaths ?? new List<string>(), autoLinkStdLib, useSharedLibrary, "kotlin");

    private static List<string> ExtractImports(string source)
    {
        var libs = new List<string>();
        foreach (System.Text.RegularExpressions.Match m in System.Text.RegularExpressions.Regex.Matches(source, @"import\s+((?:[\w]+\.)*[\w]+)\.\*", System.Text.RegularExpressions.RegexOptions.Multiline))
        {
            string name = m.Groups[1].Value;  // 完整包名如 "a.b.c.d"
            if (!libs.Contains(name)) libs.Add(name);
        }
        return libs;
    }
}
