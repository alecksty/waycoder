using System;
using System.Collections.Generic;
using System.Linq;

namespace VMLToHex.Assemblers;

public class AssemblerMsp430 : BaseAssembler
{
    public override string Name => "msp430";
    public override string Description => "Texas Instruments MSP430 (16-bit MCU)";

    protected override int RegNum(string reg)
    {
        if (string.IsNullOrEmpty(reg)) return 0;
        var s = reg.Trim().TrimStart('r', 'R');
        if (s.Equals("pc", StringComparison.OrdinalIgnoreCase)) return 0;
        if (s.Equals("sp", StringComparison.OrdinalIgnoreCase)) return 1;
        if (s.Equals("sr", StringComparison.OrdinalIgnoreCase)) return 2;
        if (s.Equals("cg", StringComparison.OrdinalIgnoreCase)) return 3;
        return int.TryParse(s, out var n) ? (n >= 0 && n <= 15 ? n : 0) : 0;
    }

    private static byte[] Word16(uint w) => new byte[] { (byte)(w & 0xFF), (byte)((w >> 8) & 0xFF) };

    private static uint EncodeDoubleOp(int opcode, int src, int dst, bool byteMode)
    {
        return (uint)((opcode << 12) | ((src & 0xF) << 8) | (byteMode ? (1 << 6) : 0) | ((dst & 0xF) << 0));
    }

    private static uint EncodeSingleOp(int opcode, int dst, int b)
    {
        return (uint)((opcode << 12) | ((b & 1) << 6) | ((dst & 0xF) << 0));
    }

    public AssemblerMsp430()
    {
        // Double-operand arithmetic
        void DoubleOp(string name, int opcode) => CustomHandlers[name] = (ops, labels, addr) =>
        {
            bool byteMode = name.EndsWith(".B", StringComparison.OrdinalIgnoreCase);
            int src = RegNum(ops[0]);
            int dst = RegNum(ops[1]);
            if (ops.Length > 1 && ops[1].StartsWith("@"))
            {
                // Indirect
            }
            // Check if src has addressing mode
            int ad = 0;
            var srcOp = ops[0];
            if (srcOp.StartsWith("@"))
            {
                var reg = srcOp.TrimStart('@').TrimEnd('+');
                src = RegNum(reg);
                ad = srcOp.EndsWith("+") ? 3 : 2;
            }
            else if (srcOp.StartsWith("&"))
            {
                src = ResolveValue(srcOp.TrimStart('&'), labels, addr);
                ad = 1;
            }
            return Word16(EncodeDoubleOp(opcode, (ad << 4) | src, dst, byteMode));
        };

        var dblOps = new[] { "MOV", "ADD", "ADDC", "SUB", "SUBC", "CMP", "BIT", "BIC", "BIS", "XOR", "AND", "ADC", "DADD", "SBC" };
        foreach (var op in dblOps)
        {
            DoubleOp(op, 4);
            DoubleOp(op + ".B", 4);
        }

        // Jumps
        void Jump(string name, int cond) => CustomHandlers[name] = (ops, labels, addr) =>
        {
            int target = ResolveValue(ops[0], labels, addr);
            int offset = ((target - (addr + 2)) >> 1) & 0x3FF;
            return Word16((uint)((cond << 10) | offset));
        };

        Jump("JMP", 0x30 >> 2);
        Jump("JEQ", 0x24 >> 2);
        Jump("JNE", 0x20 >> 2);
        Jump("JC", 0x2C >> 2);
        Jump("JNC", 0x28 >> 2);
        Jump("JN", 0x34 >> 2);
        Jump("JGE", 0x38 >> 2);
        Jump("JL", 0x3C >> 2);

        // PUSH
        CustomHandlers["PUSH"] = (ops, labels, addr) =>
        {
            int src = ResolveValue(ops[0], labels, addr);
            return Word16((uint)(0x1200 | (src & 0xFFF)));
        };

        // CALL
        CustomHandlers["CALL"] = (ops, labels, addr) =>
        {
            int target = ResolveValue(ops[0], labels, addr);
            return Word16((uint)(0x1280 | (target & 0x7FF)));
        };

        // No-register instructions
        Opcodes["NOP"] = new byte[] { 0x03, 0x43 };
        Opcodes["SWPB"] = new byte[] { 0x08, 0x90 };
        Opcodes["RET"] = new byte[] { 0x30, 0x41 };
        Opcodes["RETI"] = new byte[] { 0x00, 0x13 };
        Opcodes["DINT"] = new byte[] { 0x32, 0xD0 };
        Opcodes["EINT"] = new byte[] { 0x32, 0xD2 };
    }
}
