using VMLAssembler;
using System.Collections.Generic;

namespace VMLPlugins.Interfaces
{
    /// <summary>
    /// 前端编译器插件接口
    /// </summary>
    public interface IFrontendCompiler
    {
        /// <summary>
        /// 编译器名称
        /// </summary>
        string Name { get; }
        
        /// <summary>
        /// 编译器描述
        /// </summary>
        string Description { get; }
        
        /// <summary>
        /// 支持的文件扩展名（多个用逗号分隔）
        /// </summary>
        string SupportedExtensions { get; }
        
        /// <summary>
        /// 编译源代码到VML程序
        /// </summary>
        /// <param name="source">源代码</param>
        /// <returns>VML程序</returns>
        VmlProgram Compile(string source);
        
        /// <summary>
        /// 编译文件到VML程序
        /// </summary>
        /// <param name="filePath">文件路径</param>
        /// <param name="includePaths">包含路径列表</param>
        /// <param name="libraryPaths">库路径列表</param>
        /// <param name="autoLinkStdLib">是否自动链接标准库</param>
        /// <returns>VML程序</returns>
        VmlProgram CompileFile(string filePath, List<string> includePaths = null, List<string> libraryPaths = null, bool autoLinkStdLib = true);
        
        /// <summary>
        /// 获取编译器版本信息
        /// </summary>
        /// <returns>版本信息</returns>
        string GetVersion();
    }
}