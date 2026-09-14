using System.Collections.Generic;
using VMLAssembler;
using VMLPlugins;
using VMLPlugins.Interfaces;

namespace CompilerBase
{
    /// <summary>
    /// 编译器统一基类 — 实现 IFrontendCompilerEx，持有 CompilerConfig
    /// 所有编译器适配器（CompilerPluginBase / CompilerPluginExBase）继承此类
    /// </summary>
    public abstract class CompilerBase : IFrontendCompilerEx
    {
        /// <summary>
        /// 每个编译器实例持有的统一配置
        /// </summary>
        public CompilerConfig Config { get; } = new CompilerConfig();

        /// <summary>
        /// 便捷方法：设置单个配置项
        /// </summary>
        public void SetConfig(string key, object value) => Config.SetConfig(key, value);

        /// <summary>
        /// 便捷方法：读取单个配置项
        /// </summary>
        public T? GetConfig<T>(string key) => Config.GetConfig<T>(key);

        // ====== IFrontendCompiler 抽象成员 ======

        public abstract string Name { get; }
        public abstract string Description { get; }
        public abstract string SupportedExtensions { get; }

        public abstract VmlProgram Compile(string source);

        public abstract VmlProgram CompileFile(string filePath, List<string>? includePaths = null,
            List<string>? libraryPaths = null, bool autoLinkStdLib = true);

        public abstract string CompileFileWithIncludes(string filePath, List<string>? includePaths = null,
            List<string>? libraryPaths = null, bool autoLinkStdLib = true, bool useSharedLibrary = true);

        public virtual string GetVersion() => "1.65.90";

        // ====== 简化重载：从 Config 读取参数 ======

        /// <summary>
        /// 编译文件（从 Config 读取 includePaths/libraryPaths/autoLinkStdLib）
        /// </summary>
        public VmlProgram CompileFile(string filePath) =>
            CompileFile(filePath, Config.IncludePaths, Config.LibraryPaths, Config.AutoLinkStdLib);

        /// <summary>
        /// 编译文件为带 .linked  的 VML 文本（从 Config 读取参数）
        /// </summary>
        public string CompileFileWithIncludes(string filePath) =>
            CompileFileWithIncludes(filePath, Config.IncludePaths, Config.LibraryPaths,
                Config.AutoLinkStdLib, Config.UseSharedLibrary);

        // ====== 内部辅助 ======

        /// <summary>
        /// 同步 Config 到 CompilerOptionsContext，确保 CodeGenerator 内部代码可读取选项
        /// </summary>
        protected void SyncToContext()
        {
            CompilerOptionsContext.Current = Config.ToCompilerOptions();
        }
    }
}
