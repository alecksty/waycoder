using VMLAssembler;
using System.IO;
using System;
using System.Collections.Generic;
using CompilerBase;
using VMLPlugins;

namespace CCompiler
{
    /// <summary>
    /// C 语言编译器
    /// </summary>
    public class CCompiler
    {
        private static Dictionary<string, string> PredefinedMacros => new(CompilerHelper.BasePredefinedMacros())
        {
            ["__STDC__"] = "1",
            ["__STDC_VERSION__"] = "199901L",
            ["__STDC_HOSTED__"] = "1",
            ["LLONG_MAX"] = "9223372036854775807",
            ["LLONG_MIN"] = "-9223372036854775808",
            ["ULLONG_MAX"] = "18446744073709551615",
            // VML_WSTRING is set here instead of via InjectDefines to avoid
            // triggering the slow Preprocessor for files without real # directives
            ["VML_WSTRING"] = VMLPlugins.CompilerOptionsContext.Current?.IsMCU == false ? "1" : "0",
        };

        public static VmlProgram Compile(string source, List<(string, int)> lineMap = null, List<string> includePaths = null, string filePath = "")
        {
            // 注入命令行宏定义 (必须在 hasPreprocessor 之前，因为 InjectDefines 可能添加 # 指令)
            source = CompilerHelper.InjectDefines(source, "c", CompilerOptionsContext.Current);
            // 如果源中没有预处理器指令，跳过预处理 (保留原始换行结构)
            bool hasPreprocessor = source.Contains('#');
            if (CompilerOptionsContext.Current.DumpMode)
                Console.Error.WriteLine($"[CCompiler.Compile] source={source.Length} chars, hasPreprocessor={hasPreprocessor}");
            string processedSource = source;
            List<(string, int)> finalLineMap = lineMap ?? new List<(string, int)>();

            var diagnostics = new DiagnosticBag();
            try
            {
                if (hasPreprocessor)
                {
                    CompilerBase.Preprocessor preprocessor = new CompilerBase.Preprocessor(source, includePaths ?? new List<string>(), PredefinedMacros);
                    if (VMLPlugins.CompilerOptionsContext.Current.DumpMode)
                        preprocessor.DumpMode = true;
                    if (VMLPlugins.CompilerOptionsContext.Current.PrepareLogMode)
                        preprocessor.PrepareLogMode = true;
                    if (VMLPlugins.CompilerOptionsContext.Current.DumpPreprocess)
                        preprocessor.DumpPreprocess = true;
                    processedSource = preprocessor.Process(filePath);
                    finalLineMap.AddRange(preprocessor.LineMap);
                    if (preprocessor.ParamPrefixes.Count > 0)
                    {
                        var ctx = VMLPlugins.CompilerOptionsContext.Current;
                        if (!ctx.Defines.Any(d => d.StartsWith("VML_PREFIX=")))
                            ctx.Defines.Add($"VML_PREFIX={string.Join(" ", preprocessor.ParamPrefixes)}");
                    }
                }

                // 词法分析
                Lexer lexer = new Lexer(processedSource, finalLineMap, filePath);
                lexer.Diagnostics = diagnostics;
                if (VMLPlugins.CompilerOptionsContext.Current.DumpMode)
                    lexer.DumpMode = true;
                var tokens = lexer.Tokenize();

                // 语法分析
                Parser parser = new Parser(tokens);
                parser.FileName = filePath;
                parser.Diagnostics = diagnostics;
                var ast = parser.Parse();

                // 注入内置类型别名
                InjectBuiltinTypedefs(ast);

                    // 代码生成
                CodeGenerator codeGen = new CodeGenerator(ast);
                codeGen.InitLabelCounter(string.IsNullOrEmpty(filePath) ? "source" : Path.GetFileNameWithoutExtension(filePath));
                codeGen.SourceLines = processedSource.Split('\n');
                var prog = codeGen.GenerateCode();

                // ⚠ **成功路径也要看 bag** —— 漏掉这一句就是「收集到的错误被整个丢掉、程序照编照跑」。
                //
                // `diagnostics` 里的错分两种来路：① 词法器/解析器**收集**（`GccError`，不抛）；
                // ② 抛出后被 `catch` 收编。这一条走的是**第 ①种**：解析器认出了错、算好了位置、
                // 收进 bag，然后**顺着正常路径返回** —— 一个异常都没有 ⇒ 下面那些 `catch`
                // 一条也不会进 ⇒ 错误静默消失、编译"成功"。
                //
                // 实测症状：`int c = a + ;` 经 `CCompiler.Compile(string)` **编译成功**
                // （DiagProbe【语法错误】档 c 一栏 NOERR），而同一份源码走 `CompileFile`
                // （vmlcli）却**报错** —— 同一份源码两条路径两个答案，正是因为这一句只在
                // `CompileFile` 那条链上被别处补上了。
                //
                // `CompilerHelper.CompileWithDiagnostics` 里早就写着这条（「**成功路径也要看 bag**。
                // 此前这里直接 `return compile(...)`，于是"收集不抛"的那条路……收集到的错误被
                // 整个丢掉」）—— 17 门走的是那条共用路径，**C 这条是手写的，漏了**。
                if (diagnostics.HasErrors)
                    throw new CompilerBase.CompilationException(diagnostics.FirstErrorCode, diagnostics.FormatAll());

                // LinkStandardLibrary 由 CompileCore/测试框架统一调用，避免重复链接
                return prog;
            }
            catch (CompilerBase.ParseException ex)
            {
                if (diagnostics.HasErrors)
                    throw new CompilerBase.CompilationException(ex.Code, diagnostics.FormatAll(), ex);
                throw new CompilerBase.CompilationException(ex.Code, $"{filePath}: error: {ex.Message}", ex);
            }
            catch (CompilerBase.CodeGenerationException ex)
            {
                throw new CompilerBase.CompilationException(ex.Code, $"{filePath}: error: {ex.Message}", ex);
            }
            catch (CompilerBase.CompilationException)
            {
                throw;
            }
            catch (System.Exception ex)
            {
                throw new CompilerBase.CompilationException(ErrorCode.Compilation_InternalError, $"{filePath}: 内部错误: {ex.Message}", ex);
            }
        }

        /// <summary>向 AST 注入内置类型别名（如果用户代码未定义）</summary>
        private static void InjectBuiltinTypedefs(Program ast)
        {
            if (!ast.TypeDefs.ContainsKey("wchar_t"))
                ast.TypeDefs["wchar_t"] = "unsigned short";
            if (!ast.TypeDefs.ContainsKey("char32_t"))
                ast.TypeDefs["char32_t"] = "unsigned int";
        }

        public static VmlProgram CompileFile(string filePath, List<string> includePaths = null, List<string> libraryPaths = null, bool autoLinkStdLib = false)
        {
            string source = CompilerHelper.ReadSourceFile(filePath);
            string sourceDir = Path.GetDirectoryName(Path.GetFullPath(filePath));
            // 注入命令行宏定义 (必须在 hasPreprocessor 之前，因为 InjectDefines 可能添加 # 指令)
            source = CompilerHelper.InjectDefines(source, "c", CompilerOptionsContext.Current);
            bool hasPreprocessor = source.Contains('#');
            var lineMap = new List<(string, int)>();
            string processedSource;
            CompilerBase.Preprocessor? preprocessor = null;
            if (hasPreprocessor)
            {
                preprocessor = new CompilerBase.Preprocessor(source, includePaths ?? new List<string>(), PredefinedMacros);
                if (VMLPlugins.CompilerOptionsContext.Current.DumpMode)
                    preprocessor.DumpMode = true;
                if (VMLPlugins.CompilerOptionsContext.Current.PrepareLogMode)
                    preprocessor.PrepareLogMode = true;
                if (VMLPlugins.CompilerOptionsContext.Current.DumpPreprocess)
                    preprocessor.DumpPreprocess = true;
                processedSource = preprocessor.Process(filePath);
                lineMap.AddRange(preprocessor.LineMap);
                // #param prefix("xxx") → 注入到编译选项
                if (preprocessor.ParamPrefixes.Count > 0)
                {
                    var ctx = VMLPlugins.CompilerOptionsContext.Current;
                    if (!ctx.Defines.Any(d => d.StartsWith("VML_PREFIX=")))
                        ctx.Defines.Add($"VML_PREFIX={string.Join(" ", preprocessor.ParamPrefixes)}");
                }
            }
            else
            {
                processedSource = source;
            }

            // **诊断袋必须接上** —— 词法与语法错误都收集到它里面。
            //
            // ⚠ 这条路（`CompileFile`）此前**一个都不接**：`Lexer.Diagnostics` 与
            //   `Parser.Diagnostics` 都留在 null ⇒ `ParserBase.GccError` 走"没袋子就抛"的分支、
            //   而顶层的容错恢复只往 stderr 写一行 `[SKIP]`/`[RECOVER]`（**手机上那是看不见的流**）
            //   ⇒ 用户看到的是"编译成功"，代码却少了一整段。
            //   `vmlcli` 走的正是这条路（另一条入口 `Compile` 从 v0.96.269 起就接了）。
            //   与 `Compile` 同口径——同一个东西不该两条路两种行为。
            var parseDiagnostics = new DiagnosticBag();

            // 词法分析
            Lexer lexer = new Lexer(processedSource, lineMap, filePath);
            lexer.Diagnostics = parseDiagnostics;
            if (VMLPlugins.CompilerOptionsContext.Current.DumpMode)
                lexer.DumpMode = true;
            var tokens = lexer.Tokenize();

            // 语法分析
            Parser parser = new Parser(tokens);
            parser.FileName = filePath;
            parser.Diagnostics = parseDiagnostics;
            var ast = parser.Parse();

            // 词法/语法错**收集后一次抛出**（与代码生成那半的 `BuildProgram` 同口径）：
            // 一次把文件里能看到的错全报出来，而不是遇到第一个就停。
            if (parseDiagnostics.HasErrors)
                throw new CompilationException(parseDiagnostics.FirstErrorCode, parseDiagnostics.FormatAll());

            // 代码生成
            CodeGenerator codeGen = new CodeGenerator(ast);
            codeGen.InitLabelCounter(Path.GetFileNameWithoutExtension(filePath));
            codeGen.SourceLines = processedSource.Split('\n');
            VmlProgram mainProgram = codeGen.GenerateCode();

            // 构建库路径列表
            List<string> allLibraryPaths = new List<string>();

            // 用户命令行指定的库路径
            if (libraryPaths != null)
                allLibraryPaths.AddRange(libraryPaths);

            // #param lib(...) 指定的库 (已去重)
            if (preprocessor != null && preprocessor.ParamLibraries.Count > 0)
            {
                foreach (var lib in preprocessor.ParamLibraries)
                {
                    string libBase = lib.EndsWith(".vml", StringComparison.OrdinalIgnoreCase) ? lib[..^4] : lib;
                    string resolved = CompilerHelper.ResolveImportLibrary(libBase, sourceDir, "c");
                    if (resolved == null && allLibraryPaths != null)
                    {
                        foreach (var libDir in allLibraryPaths)
                        {
                            string candidate = Path.Combine(libDir, libBase + ".vml");
                            if (File.Exists(candidate)) { resolved = candidate; break; }
                            candidate = Path.Combine(libDir, libBase);
                            if (File.Exists(candidate)) { resolved = candidate; break; }
                            candidate = Path.Combine(libDir, libBase + "_lib.vml");
                            if (File.Exists(candidate)) { resolved = candidate; break; }
                        }
                    }
                    if (resolved != null)
                    {
                        if (autoLinkStdLib)
                        {
                            // 应用模式: 直接内联链接
                            if (!allLibraryPaths.Contains(resolved))
                                allLibraryPaths.Add(resolved);
                        }
                        else
                        {
                            // 库模式: 生成 .linked 指令, 依赖推迟到最终链接时解析
                            string vmlFileName = Path.GetFileName(resolved);
                                                        if (!mainProgram.LinkedFiles.Contains(vmlFileName))
                                mainProgram.LinkedFiles.Add(vmlFileName);
                        }
                    }
                    else if (autoLinkStdLib)
                    {
                        Console.WriteLine($"CCompiler: #param lib({lib}) 未找到独立文件，将由标准库提供");
                    }
                }
            }

            // 自动链接标准库 (兜底，默认 false)
            if (autoLinkStdLib)
            {
                string stdLibPath = GetStandardLibraryPath();
                if (!string.IsNullOrEmpty(stdLibPath) && !allLibraryPaths.Contains(stdLibPath))
                    allLibraryPaths.Add(stdLibPath);

                // 自动检测共享库 (零配置: 扫描CALL标签→自动链接)
                // 仅在 autoLinkStdLib 时启用，库模式跳过以避免膨胀
                CompilerHelper.AutoDetectSharedLibs(mainProgram, allLibraryPaths);
            }

            // 多前缀别名: #param prefix("basic_","java_") → 生成 basic_func: java_func:
            var prefixes = preprocessor?.ParamPrefixes;

            // 链接库（线程安全：参数传递替代静态属性）
            if (allLibraryPaths.Count > 0)
            {
                if (VMLPlugins.CompilerOptionsContext.Current.DebugMode)
                    Console.WriteLine($"CCompiler: 链接库: {string.Join(", ", allLibraryPaths)}");
                VMLAssembler.LibraryLinker.LinkLibraries(mainProgram, allLibraryPaths,
                    multiPrefixes: prefixes?.Count > 0 ? prefixes : null,
                    debug: VMLPlugins.CompilerOptionsContext.Current.DebugMode);
            }

            return mainProgram;
        }
        
        /// <summary>
        /// 编译文件并生成包含.include伪指令的VML文本
        /// </summary>
        public static string CompileFileWithIncludes(string filePath, List<string> includePaths = null, List<string> libraryPaths = null, bool autoLinkStdLib = false, bool useSharedLibrary = true)
            => CompilerHelper.BuildCompileFileWithIncludes(
                CompileFile(filePath, includePaths, null, false),
                filePath, libraryPaths, autoLinkStdLib, useSharedLibrary, "c");
        
        /// <summary>
        /// 获取标准库路径
        /// </summary>
        private static string GetStandardLibraryPath()
        {
            // 尝试从多个位置查找标准库
            string[] possiblePaths = new string[]
            {
                Path.Combine(Directory.GetCurrentDirectory(), "Lib", "c", "builtin.vml"),
                Path.Combine(Directory.GetCurrentDirectory(), "..", "Lib", "c", "builtin.vml"),
                Path.Combine(Directory.GetCurrentDirectory(), "..", "..", "Lib", "c", "builtin.vml"),
                Path.Combine(Directory.GetCurrentDirectory(), "..", "..", "..", "Lib", "c", "builtin.vml"),
                Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Lib", "c", "builtin.vml"),
                Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "..", "Lib", "c", "builtin.vml"),
                Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "..", "..", "Lib", "c", "builtin.vml"),
            };
            
            foreach (string path in possiblePaths)
            {
                if (File.Exists(path))
                {
                    if (VMLPlugins.CompilerOptionsContext.Current.DebugMode)
                        Console.WriteLine($"CCompiler: 找到标准库文件: {path}");
                    return Path.GetDirectoryName(path);
                }
            }
            
            if (VMLPlugins.CompilerOptionsContext.Current.DebugMode)
                Console.WriteLine("CCompiler: 警告: 未找到标准库文件");
            return null;
        }

    }
}