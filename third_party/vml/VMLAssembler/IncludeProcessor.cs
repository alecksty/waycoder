using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace VMLAssembler
{
    /// <summary>
    /// 包含文件处理器 - 用于在VML代码中添加.include伪指令
    /// </summary>
    public static class IncludeProcessor
    {
        /// <summary>
        /// 将VML程序转换为包含.include伪指令的文本
        /// </summary>
        /// <param name="program">VML程序</param>
        /// <param name="includeFiles">要包含的文件列表</param>
        /// <param name="basePath">基础路径（用于相对路径解析）</param>
        /// <returns>包含.include伪指令的VML文本</returns>
        public static string ToVmlTextWithIncludes(VmlProgram program, List<string>? includeFiles = null, string? basePath = null)
        {
            var sb = new StringBuilder();
            
            // 添加.include伪指令
            if (includeFiles != null && includeFiles.Count > 0)
            {
                foreach (var includeFile in includeFiles)
                {
                    string includePath = includeFile;
                    
                    // 如果是相对路径，尝试转换为相对于basePath或当前目录
                    if (!Path.IsPathRooted(includeFile))
                    {
                        // 保留原始相对路径（相对于工作目录）
                        // 不使用basePath计算相对路径，因为#include指令是在工作目录解析的
                        includePath = includeFile.Replace('\\', '/');
                    }
                    
                    sb.AppendLine($".linked  \"{includePath}\"");
                }
                sb.AppendLine();
            }
            
            // 添加程序内容
            sb.Append(program.ToString());
            
            return sb.ToString();
        }
        
        /// <summary>
        /// 为特定语言生成标准库包含指令
        /// 默认只自动链接 builtin.vml（必备运行时）
        /// stdlib.vml / vmllib.vml 需用户在代码中显式 .linked  或命令行指定
        /// </summary>
        /// <param name="language">语言名称（c, basic, pascal等）</param>
        /// <param name="useSharedLibrary">是否使用共享库</param>
        /// <returns>包含指令列表</returns>
        public static List<string> GetStandardLibraryIncludes(string language, bool useSharedLibrary = true)
        {
            var includes = new List<string>();
            string lang = language.ToLower();

            // 语言名 → Lib/ 目录名映射
            string langDir = lang switch
            {
                "basic" => "basic",
                "pascal" => "pascal",
                "kotlin" => "kotlin",
                "scheme" => "scheme",
                _ => lang
            };

            // 默认只自动链接 builtin.vml（必备运行时）
            // 用户需要更多库时，在代码中 .linked "stdlib.vml" 或 .linked "vmllib.vml"
            includes.Add($"Lib/{langDir}/builtin.vml");

            return includes;
        }
        
        private static void AddMinimalShared(List<string> includes)
        {
            includes.Add("Lib/shared/io.vml");
            includes.Add("Lib/shared/string.vml");
            includes.Add("Lib/shared/builtins.vml");
            includes.Add("Lib/shared/memory.vml");
            includes.Add("Lib/shared/sysinfo.vml");
            includes.Add("Lib/shared/float.vml");
        }

        /// <summary>
        /// 处理库路径，将库目录中的.vml文件转换为.include指令
        /// </summary>
        /// <param name="libraryPaths">库路径列表</param>
        /// <returns>.include指令列表</returns>
        public static List<string> ConvertLibraryPathsToIncludes(List<string> libraryPaths)
        {
            var includes = new List<string>();
            
            if (libraryPaths == null || libraryPaths.Count == 0)
                return includes;
            
            foreach (var libPath in libraryPaths)
            {
                if (Directory.Exists(libPath))
                {
                    // 扫描目录中的.vml文件
                    var vmlFiles = Directory.GetFiles(libPath, "*.vml");
                    foreach (var vmlFile in vmlFiles)
                    {
                        includes.Add(vmlFile);
                    }
                }
                else if (File.Exists(libPath) && libPath.EndsWith(".vml", StringComparison.OrdinalIgnoreCase))
                {
                    // 直接添加.vml文件
                    includes.Add(libPath);
                }
            }
            
            return includes;
        }
    }
}