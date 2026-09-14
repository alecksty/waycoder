using VMLAssembler;
using System.Collections.Generic;

namespace VMLPlugins.Interfaces
{
    /// <summary>
    /// 扩展的前端编译器插件接口 - 支持包含伪指令
    /// </summary>
    public interface IFrontendCompilerEx : IFrontendCompiler
    {
        /// <summary>
        /// 编译文件到包含.include伪指令的VML文本
        /// </summary>
        /// <param name="filePath">文件路径</param>
        /// <param name="includePaths">包含路径列表</param>
        /// <param name="libraryPaths">库路径列表</param>
        /// <param name="autoLinkStdLib">是否自动链接标准库</param>
        /// <param name="useSharedLibrary">是否使用共享库</param>
        /// <returns>包含.include伪指令的VML文本</returns>
        string CompileFileWithIncludes(string filePath, List<string> includePaths = null, List<string> libraryPaths = null, bool autoLinkStdLib = true, bool useSharedLibrary = true);
    }
}