using VMLPlugins.Interfaces;
using VMLAssembler;

namespace VMLTranslators
{
    /// <summary>
    /// 翻译器插件适配器基类
    /// </summary>
    public abstract class BaseTranslatorPlugin : IBackendTranslator
    {
        protected BaseTranslator _translator;
        
        public abstract string TargetArchitecture { get; }
        public abstract string Description { get; }
        public abstract string SupportedBits { get; }
        
        public TranslationResult Translate(VmlProgram vmlProgram, TranslationOptions options = null)
        {
            var startTime = DateTime.Now;
            
            // 创建翻译器实例
            _translator = CreateTranslator(vmlProgram);
            
            // 执行翻译
            var translatedProgram = _translator.Translate();
            
            var endTime = DateTime.Now;
            var translationTime = (endTime - startTime).TotalMilliseconds;
            
            return new TranslationResult
            {
                Code = translatedProgram.Code,
                OutputExtension = GetOutputExtension(),
                Stats = new TranslationStats
                {
                    TotalInstructions = vmlProgram.Instructions.Count,
                    TranslationTimeMs = (long)translationTime,
                    GeneratedLines = translatedProgram.Code.Split('\n').Length
                }
            };
        }
        
        /// <summary>
        /// 创建具体的翻译器实例
        /// </summary>
        protected abstract BaseTranslator CreateTranslator(VmlProgram vmlProgram);
        
        /// <summary>
        /// 获取输出文件扩展名
        /// </summary>
        protected virtual string GetOutputExtension()
        {
            return ".asm";
        }
    }
}