using System;
using System.Collections.Generic;

namespace VMLToHex.Assemblers
{
    public static class AssemblerFactory
    {
        private static readonly Dictionary<string, Func<BaseAssembler>> _creators = new(StringComparer.OrdinalIgnoreCase)
        {
            ["6502"] = () => new Assembler6502(),
            ["z80"] = () => new AssemblerZ80(),
            ["8051"] = () => new Assembler8051(),
            ["arm-cm"] = () => new AssemblerArmCm(),
            ["arm"] = () => new AssemblerArmCm(),
            ["x86"] = () => new AssemblerX86(),
            ["68000"] = () => new Assembler68000(),
            ["mips"] = () => new AssemblerMips(),
            ["riscv"] = () => new AssemblerRiscV(),
            ["avr"] = () => new AssemblerAvr(),
            ["pic"] = () => new AssemblerPic(),
            ["pic24"] = () => new AssemblerPic24(),
            ["msp430"] = () => new AssemblerMsp430(),
            ["sparc"] = () => new AssemblerSparc(),
            ["powerpc"] = () => new AssemblerPowerPc(),
            ["wasm"] = () => new AssemblerWasm(),
            ["wat"] = () => new AssemblerWasm(),
        };

        public static BaseAssembler Create(string arch)
        {
            if (_creators.TryGetValue(arch, out var factory))
                return factory();
            throw new ArgumentException($"不支持的架构: {arch}");
        }

        public static IEnumerable<string> SupportedArchitectures => _creators.Keys;
    }
}
