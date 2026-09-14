using System.Collections.Generic;

namespace VMLTranslators
{
    /// <summary>
    /// 转译后的程序
    /// </summary>
    public class TranslatedProgram
    {
        public string Code { get; set; }
        public string TargetArch { get; set; }
        public Dictionary<string, object> Metadata { get; set; }

        public TranslatedProgram(string code, string targetArch, Dictionary<string, object> metadata)
        {
            Code = code;
            TargetArch = targetArch;
            Metadata = metadata;
        }
    }
}
