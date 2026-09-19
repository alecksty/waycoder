using System;
using System.Collections.Generic;
using System.IO;
using VMLAssembler;
using VMLPlugins;
using VMLPlugins.Interfaces;

namespace CompilerBase
{
    public class CompilerException : Exception
    {
        public string CompilerName { get; }
        public ErrorCode Code { get; }
        public CompilerException(string compilerName, string message)
            : this(compilerName, ErrorCode.Unknown, message) { }
        public CompilerException(string compilerName, ErrorCode code, string message)
            : base(FormatMessage(compilerName, message)) { CompilerName = compilerName; Code = code; }

        /// <summary>智能格式化: 如果消息已是 GCC 风格 (file:line:col: level: msg), 不添加前缀</summary>
        private static string FormatMessage(string name, string msg) =>
            IsGccFormat(msg) ? msg : $"{name}错误: {msg}";

        /// <summary>检测消息是否已使用 GCC 格式 (file:line:col: level: msg)</summary>
        public static bool IsGccFormat(string msg) =>
            msg.Contains(": error:") || msg.Contains(": warning:") || msg.Contains(": note:") || msg.Contains(" error(s) generated");
    }

    public class CompileFailedException : CompilerException
    {
        public CompileFailedException(string compilerName)
            : base(compilerName, ErrorCode.Compilation_InternalError, "编译失败") { }
    }

    /// <summary>
    /// 前端编译器插件基类 — 实例类架构（继承 CompilerBase）
    /// 用于 Java/Swift/C#/JavaScript 等以实例方式调用的编译器。
    /// 子类只需提供 Name/Description/SupportedExtensions/LibDirectory 和 CompileSource()。
    /// </summary>
    public abstract class CompilerPluginBase : CompilerBase
    {
        /// <summary>
        /// 标准库目录名（如 "java"、"swift"、"csharp"、"javascript"）
        /// </summary>
        protected abstract string LibDirectory { get; }

        /// <summary>
        /// 子类实现实际的编译逻辑，将源代码编译为 VmlProgram
        /// </summary>
        protected abstract VmlProgram CompileSource(string source);

        /// <summary>
        /// 从源代码中提取 import 库名（子类可覆写以实现语言特定的 import 语法）
        /// </summary>
        protected virtual List<string> ExtractImports(string source) => new List<string>();

        /// <summary>
        /// 从源代码中移除 import 语句（子类可覆写，用于解析器不支持 import 语法的语言）
        /// </summary>
        protected virtual string StripImports(string source) => source;

        public override VmlProgram Compile(string source)
        {
            try
            {
                SyncToContext();
                WarningEmitter.Clear();
                var opts = Config.ToCompilerOptions();
                if (opts.DebugMode)
                    System.Diagnostics.Debug.WriteLine($"[{Name}] 调试模式已启用");

                // 注入 -D/-U 宏定义（语言适配的常量声明）
                string processed = CompilerHelper.InjectDefines(source, LibDirectory.ToLower(), opts);
                var program = CompilerHelper.CompileWithTimeout(() => CompileSource(processed), Name);
                if (program == null)
                    throw new CompileFailedException(Name);

                if (opts.WarningLevel > 0 && WarningEmitter.Count > 0)
                    if (VMLPlugins.CompilerOptionsContext.Current.DebugMode)
                        Console.WriteLine($"[{Name}] {WarningEmitter.Count} 个警告");

                return program;
            }
            catch (CompileFailedException) { throw; }
            catch (ParseException ex) { throw new CompilerException(Name, ex.Code, CompilerException.IsGccFormat(ex.Message) ? ex.Message : $"语法错误: {ex.Message}"); }
            catch (CodeGenerationException ex) { throw new CompilerException(Name, ex.Code, CompilerException.IsGccFormat(ex.Message) ? ex.Message : $"代码生成错误: {ex.Message}"); }
            catch (CompilationException ex) { throw new CompilerException(Name, ex.Code, ex.Message); }
            // 链接期「用户代码调用了不存在的函数」——**用户源码的错**，不是内部故障。
            // 必须排在下面的 `catch (Exception)` 之前，否则会被标成 `Compilation_InternalError`
            // 并加上 `internal error:` 前缀（实测就是这样：一个拼错的函数名被报成"内部错误"，
            // 用户完全不知道该改哪里）。用 `CodeGen_UndefinedFunction` 这个现成的语义码。
            catch (VMLAssembler.UnresolvedSymbolException ex)
            {
                throw new CompilerException(Name, ErrorCode.CodeGen_UndefinedFunction, ex.Message);
            }
            catch (Exception ex)
            {
                throw new CompilerException(Name, ErrorCode.Compilation_InternalError, ex.Message);
            }
        }

        /// <summary>
        /// 预处理源代码（将 -D/-U 宏定义注入为语言对应的常量声明）
        /// </summary>
        protected string PreprocessDefines(string source)
        {
            return CompilerHelper.InjectDefines(source, LibDirectory.ToLower(), Config.ToCompilerOptions());
        }
        
        public override VmlProgram CompileFile(string filePath, List<string>? includePaths = null, List<string>? libraryPaths = null, bool autoLinkStdLib = false)
        {
            try
            {
                SyncToContext();
                if (!File.Exists(filePath))
                    throw new FileNotFoundException($"文件不存在: {filePath}");

                string sourceCode = File.ReadAllText(filePath, System.Text.Encoding.UTF8);
                // InjectDefines already done by CCompiler.CompileFile internally
                var program = CompileSource(sourceCode);

                if (program == null)
                    throw new CompileFailedException(Name);

                // 链接库文件
                var allLibraryPaths = new List<string>();

                if (libraryPaths != null)
                    allLibraryPaths.AddRange(libraryPaths);

                if (autoLinkStdLib)
                {
                    // 自动链接语言对应的 builtin.vml（必备运行时）
                    var vmlHome = Environment.GetEnvironmentVariable("VML_HOME")
                        ?? Environment.GetEnvironmentVariable("VML_TOOL_PATH");
                    string[] builtinsSearchRoots = {
                        !string.IsNullOrEmpty(vmlHome) ? vmlHome : null!,
                        AppContext.BaseDirectory,
                        Directory.GetCurrentDirectory()
                    };
                    bool builtinsFound = false;
                    foreach (var root in builtinsSearchRoots)
                    {
                        string builtinPath = Path.Combine(root, "Lib", LibDirectory, "builtin.vml");
                        if (File.Exists(builtinPath) && !allLibraryPaths.Contains(builtinPath))
                        {
                            allLibraryPaths.Add(builtinPath);
                            builtinsFound = true;
                            break;
                        }
                    }
                    if (!builtinsFound)
                    {
                        // Walk up looking for Lib/<lang>/builtin.vml
                        string dir = vmlHome ?? AppContext.BaseDirectory;
                        for (int i = 0; i < 4; i++)
                        {
                            string builtinPath = Path.Combine(dir, "Lib", LibDirectory, "builtin.vml");
                            if (File.Exists(builtinPath) && !allLibraryPaths.Contains(builtinPath))
                            { allLibraryPaths.Add(builtinPath); break; }
                            var parent = Directory.GetParent(dir);
                            if (parent == null) break;
                            dir = parent.FullName;
                        }
                    }
                }

                if (allLibraryPaths.Count > 0)
                    program = LibraryLinker.LinkLibraries(program, allLibraryPaths);

                return program;
            }
            catch (CompilerException) { throw; }
            catch (ParseException ex) { throw new CompilerException(Name, ex.Code, $"语法错误: {ex.Message}"); }
            catch (CodeGenerationException ex) { throw new CompilerException(Name, ex.Code, $"代码生成错误: {ex.Message}"); }
            catch (CompilationException ex) { throw new CompilerException(Name, ex.Code, ex.Message); }
            catch (Exception ex)
            {
                throw new CompilerException(Name, ErrorCode.Compilation_InternalError, ex.Message);
            }
        }

        public override string CompileFileWithIncludes(string filePath, List<string>? includePaths = null, List<string>? libraryPaths = null, bool autoLinkStdLib = false, bool useSharedLibrary = true)
        {
            try
            {
                SyncToContext();
                if (!File.Exists(filePath))
                    throw new FileNotFoundException($"文件不存在: {filePath}");

                string sourceCode = File.ReadAllText(filePath, System.Text.Encoding.UTF8);
                string sourceDir = Path.GetDirectoryName(Path.GetFullPath(filePath));

                // 提取 import 库名（在剥离前）
                var importLibs = ExtractImports(sourceCode);

                // 剥离 import 语句（对于解析器不支持 import 语法的语言）
                sourceCode = StripImports(sourceCode);
                // InjectDefines done at line 95; skip duplicate injection here
                var mainProgram = CompileSource(sourceCode);
                if (mainProgram == null)
                    throw new CompileFailedException(Name);

                // 解析 import 库路径
                var allLibraryPaths = new List<string>();
                if (libraryPaths != null)
                    allLibraryPaths.AddRange(libraryPaths);
                foreach (var lib in importLibs)
                {
                    string resolved = CompilerHelper.ResolveImportLibrary(lib, sourceDir, LibDirectory);
                    if (resolved != null && !allLibraryPaths.Contains(resolved))
                        allLibraryPaths.Add(resolved);
                }

                return CompilerHelper.BuildCompileFileWithIncludes(
                    mainProgram, filePath, allLibraryPaths, autoLinkStdLib, useSharedLibrary, LibDirectory);
            }
            catch (CompilerException) { throw; }
            catch (ParseException ex) { throw new CompilerException(Name, ex.Code, $"语法错误: {ex.Message}"); }
            catch (CodeGenerationException ex) { throw new CompilerException(Name, ex.Code, $"代码生成错误: {ex.Message}"); }
            catch (CompilationException ex) { throw new CompilerException(Name, ex.Code, ex.Message); }
            catch (Exception ex)
            {
                throw new CompilerException(Name, ErrorCode.Compilation_InternalError, ex.Message);
            }
        }

        public override string GetVersion() => "1.65.90";
    }
}
