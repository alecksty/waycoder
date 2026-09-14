using VMLAssembler;

namespace VMLTranslators
{
    /// <summary>
    /// 转译器工厂
    /// </summary>
    public static class TranslatorFactory
    {
        /// <summary>
        /// 获取指定架构的转译器
        /// </summary>
        public static BaseTranslator GetTranslator(string arch, VmlProgram vmlProgram, string mode = "32")
        {
            switch (arch.ToLower())
            {
                case "6502":
                    return new Translator6502(vmlProgram);
                case "z80":
                    return new TranslatorZ80(vmlProgram);
                case "8051":
                    return new Translator8051(vmlProgram);
                case "avr":
                    return new TranslatorAVR(vmlProgram);
                case "pic":
                    return new TranslatorPIC(vmlProgram);
                case "msp430":
                    return new TranslatorMSP430(vmlProgram);
                case "pic24":
                    return new TranslatorPIC24(vmlProgram);
                case "arm-cm":
                case "arm-m0": // 向后兼容
                case "arm-m1": // 向后兼容
                case "arm-m2": // 向后兼容
                case "arm-m4": // 向后兼容
                case "arm-m7": // 向后兼容
                    return new TranslatorARMCM(vmlProgram);
                case "x86":
                    return new TranslatorX86(vmlProgram, mode);
                case "mips":
                    return new TranslatorMIPS(vmlProgram);
                case "riscv":
                case "risc-v":
                    return new TranslatorRISCV(vmlProgram);
                case "68000":
                case "68k":
                    return new Translator68000(vmlProgram);
                case "sparc":
                    return new TranslatorSPARC(vmlProgram);
                case "powerpc":
                case "ppc":
                    return new TranslatorPowerPC(vmlProgram);
                case "jvm":
                case "java":
                    return new TranslatorJVM(vmlProgram);
                case "dotnet":
                case "cil":
                case "msil":
                    return new TranslatorDotNET(vmlProgram);
                case "wasm":
                case "webassembly":
                    return new TranslatorWasm(vmlProgram);
                default:
                    throw new ArgumentException($"不支持的目标架构：{arch}");
            }
        }
    }
}
