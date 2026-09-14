using System.Diagnostics.CodeAnalysis;
using VMLPlugins.Interfaces;
using VMLAssembler;

namespace VMLTranslators
{
    /// <summary>
    /// 泛型转译器插件 — 替代 18 个重复的 Plugin 文件。
    /// 用法: new TranslatorPlugin&lt;TranslatorARMCM&gt;("ARM-CM", "ARM Cortex-M 32位微控制器", "32", ".arm-cm.asm")
    /// </summary>
    public class TranslatorPlugin<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)] T> : BaseTranslatorPlugin where T : BaseTranslator
    {
        private readonly string _arch;
        private readonly string _desc;
        private readonly string _bits;
        private readonly string _ext;

        public TranslatorPlugin(string arch, string desc, string bits, string ext = ".asm")
        {
            _arch = arch;
            _desc = desc;
            _bits = bits;
            _ext = ext;
        }

        public override string TargetArchitecture => _arch;
        public override string Description => _desc;
        public override string SupportedBits => _bits;

        protected override BaseTranslator CreateTranslator(VmlProgram vmlProgram)
            => (T)Activator.CreateInstance(typeof(T), vmlProgram)!;

        protected override string GetOutputExtension() => _ext;
    }
}
