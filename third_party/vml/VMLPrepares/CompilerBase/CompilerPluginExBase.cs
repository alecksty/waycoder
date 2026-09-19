using System;
using System.IO;
using VMLAssembler;
using VMLPlugins;
using VMLPlugins.Interfaces;
using System.Collections.Generic;

namespace CompilerBase
{
    /// <summary>
    /// 前端编译器插件基类 — 静态类架构（继承 CompilerBase）
    /// 子类只需提供 Name/Description/SupportedExtensions 和静态委托
    /// </summary>
    public abstract class CompilerPluginExBase : CompilerBase
    {
        /// <summary>
        /// 子类提供静态 Compile(string) 的委托
        /// </summary>
        protected abstract Func<string, VmlProgram> CompileFunc { get; }

        /// <summary>
        /// 子类提供静态 CompileFile(string, List&lt;string&gt;, List&lt;string&gt;, bool) 的委托
        /// </summary>
        protected abstract Func<string, List<string>, List<string>, bool, VmlProgram> CompileFileFunc { get; }

        /// <summary>
        /// 子类提供静态 CompileFileWithIncludes(string, List&lt;string&gt;, List&lt;string&gt;, bool, bool) 的委托
        /// </summary>
        protected abstract Func<string, List<string>, List<string>, bool, bool, string> CompileFileWithIncludesFunc { get; }

        /// <summary>
        /// 预处理源代码（将 -D/-U 宏定义注入为语言对应的常量声明）
        /// </summary>
        protected string PreprocessDefines(string source)
        {
            return CompilerHelper.InjectDefines(source, Name.ToLower(), Config.ToCompilerOptions());
        }

        /// <summary>
        /// 从文件读取源码并注入命令行宏定义
        /// </summary>
        protected string ReadSourceWithDefines(string filePath)
        {
            string source = File.ReadAllText(filePath, System.Text.Encoding.UTF8);
            return PreprocessDefines(source);
        }

        public override VmlProgram Compile(string source)
        {
            SyncToContext();
            return CompileFunc(source);
        }

        public override VmlProgram CompileFile(string filePath, List<string>? includePaths = null, List<string>? libraryPaths = null, bool autoLinkStdLib = true)
        {
            SyncToContext();
            return CompileFileFunc(filePath, includePaths, libraryPaths, autoLinkStdLib);
        }

        public override string CompileFileWithIncludes(string filePath, List<string>? includePaths = null, List<string>? libraryPaths = null, bool autoLinkStdLib = true, bool useSharedLibrary = true)
        {
            SyncToContext();
            // ⚠ 链接期的「用户代码调用了不存在的函数」在这一层统一翻成 `CompilationException`。
            //   不翻的话它会以一个 **Unhandled exception + 堆栈**的形态穿到 CLI/MAUI ——
            //   消息其实是对的，但看上去像编译器自己崩了，而且宿主侧那些
            //   「把 error: 那几行解析成编辑器气泡」的逻辑也接不上。
            //   放在**委托调用这一层**而不是各语言的 `CompileFileWithIncludes` 里：
            //   22 个前端的实现是各自注入的委托，逐个改是「同一规则 22 处实现」。
            try
            {
                return CompileFileWithIncludesFunc(filePath, includePaths, libraryPaths, autoLinkStdLib, useSharedLibrary);
            }
            catch (VMLAssembler.UnresolvedSymbolException ex)
            {
                throw new CompilationException(ErrorCode.CodeGen_UndefinedFunction, ex.Message, ex);
            }
        }

        public override string GetVersion() => "1.65.90";
    }
}
