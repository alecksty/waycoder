using VMLAssembler;

namespace VMLPlugins.Interfaces
{
    /// <summary>
    /// 翻译结果
    /// </summary>
    public class TranslationResult
    {
        /// <summary>
        /// 生成的代码
        /// </summary>
        public string Code { get; set; } = string.Empty;
        
        /// <summary>
        /// 输出文件扩展名
        /// </summary>
        public string OutputExtension { get; set; } = ".asm";
        
        /// <summary>
        /// 翻译统计信息
        /// </summary>
        public TranslationStats Stats { get; set; } = new TranslationStats();
    }
    
    /// <summary>
    /// 翻译统计信息
    /// </summary>
    public class TranslationStats
    {
        /// <summary>
        /// 总指令数
        /// </summary>
        public int TotalInstructions { get; set; }
        
        /// <summary>
        /// 翻译时间（毫秒）
        /// </summary>
        public long TranslationTimeMs { get; set; }
        
        /// <summary>
        /// 生成的代码行数
        /// </summary>
        public int GeneratedLines { get; set; }
    }
    
    /// <summary>
    /// 后端翻译器插件接口
    /// </summary>
    public interface IBackendTranslator
    {
        /// <summary>
        /// 目标架构名称
        /// </summary>
        string TargetArchitecture { get; }
        
        /// <summary>
        /// 架构描述
        /// </summary>
        string Description { get; }
        
        /// <summary>
        /// 支持的位数（如32, 64等）
        /// </summary>
        string SupportedBits { get; }
        
        /// <summary>
        /// 翻译VML程序到目标架构
        /// </summary>
        /// <param name="vmlProgram">VML程序</param>
        /// <param name="options">翻译选项</param>
        /// <returns>翻译结果</returns>
        TranslationResult Translate(VmlProgram vmlProgram, TranslationOptions? options = null);
    }
    
    /// <summary>
    /// 翻译选项
    /// </summary>
    public class TranslationOptions
    {
        /// <summary>
        /// 输出格式（如intel, att等）
        /// </summary>
        public string OutputFormat { get; set; } = "default";
        
        /// <summary>
        /// 优化级别
        /// </summary>
        public int OptimizationLevel { get; set; } = 0;
        
        /// <summary>
        /// 是否生成调试信息
        /// </summary>
        public bool GenerateDebugInfo { get; set; } = false;
        
        /// <summary>
        /// 其他自定义选项
        /// </summary>
        public Dictionary<string, string> CustomOptions { get; set; } = new Dictionary<string, string>();
    }
}