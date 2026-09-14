using System;
using System.Collections.Generic;

namespace VMLToHex.Assemblers;

public class AssemblerX86 : BaseAssembler
{
    public override string Name => "x86";
    public override string Description => "Intel x86 (32-bit)";

    public AssemblerX86()
    {
        // MOV reg, reg / reg, [mem] etc.
        CustomHandlers["MOV"] = MovHandler();
        CustomHandlers["MOVB"] = MovbHandler();
        CustomHandlers["ADD"] = AluRegHandler(0x01, 0x03, 0x83, 0x04, 0x00);
        CustomHandlers["SUB"] = AluRegHandler(0x29, 0x2B, 0x83, 0x05, 0x28);
        CustomHandlers["CMP"] = AluRegHandler(0x39, 0x3B, 0x83, 0x07, 0x38);
        CustomHandlers["AND"] = AluRegHandler(0x21, 0x23, 0x83, 0x04, 0x20);
        CustomHandlers["OR"] = AluRegHandler(0x09, 0x0B, 0x83, 0x01, 0x08);
        CustomHandlers["XOR"] = AluRegHandler(0x31, 0x33, 0x83, 0x06, 0x30);

        // INC/DEC reg
        CustomHandlers["INC"] = (ops, _, _) => new byte[] { (byte)(0x40 | RegNum(ops[0])) };
        CustomHandlers["DEC"] = (ops, _, _) => new byte[] { (byte)(0x48 | RegNum(ops[0])) };

        // MUL/DIV - implicit EAX
        CustomHandlers["MUL"] = (ops, _, _) =>
        {
            int rm = RegNum(ops[0]);
            return new byte[] { 0xF7, (byte)(0xE0 | ((rm & 7) << 3) | rm) };
        };
        CustomHandlers["DIV"] = (ops, _, _) =>
        {
            int rm = RegNum(ops[0]);
            return new byte[] { 0xF7, (byte)(0xF0 | ((rm & 7) << 3) | rm) };
        };
        CustomHandlers["NOT"] = (ops, _, _) =>
        {
            int rm = RegNum(ops[0]);
            return new byte[] { 0xF7, (byte)(0xD0 | ((rm & 7) << 3) | rm) };
        };
        CustomHandlers["NEG"] = (ops, _, _) =>
        {
            int rm = RegNum(ops[0]);
            return new byte[] { 0xF7, (byte)(0xD8 | ((rm & 7) << 3) | rm) };
        };

        // SHL/SHR reg, imm
        CustomHandlers["SHL"] = (ops, labels, addr) =>
        {
            int rm = RegNum(ops[0]);
            int imm = ops.Length > 1 ? ResolveValue(ops[1], labels, addr) : 1;
            if (imm == 1) return new byte[] { 0xD1, (byte)(0xE0 | ((rm & 7) << 3) | rm) };
            return new byte[] { 0xC1, (byte)(0xE0 | ((rm & 7) << 3) | rm), (byte)imm };
        };
        CustomHandlers["SHR"] = (ops, labels, addr) =>
        {
            int rm = RegNum(ops[0]);
            int imm = ops.Length > 1 ? ResolveValue(ops[1], labels, addr) : 1;
            if (imm == 1) return new byte[] { 0xD1, (byte)(0xE8 | ((rm & 7) << 3) | rm) };
            return new byte[] { 0xC1, (byte)(0xE8 | ((rm & 7) << 3) | rm), (byte)imm };
        };
        CustomHandlers["SAR"] = (ops, labels, addr) =>
        {
            int rm = RegNum(ops[0]);
            int imm = ops.Length > 1 ? ResolveValue(ops[1], labels, addr) : 1;
            if (imm == 1) return new byte[] { 0xD1, (byte)(0xF8 | ((rm & 7) << 3) | rm) };
            return new byte[] { 0xC1, (byte)(0xF8 | ((rm & 7) << 3) | rm), (byte)imm };
        };

        // PUSH/POP reg
        CustomHandlers["PUSH"] = PushPopHandler(0x50, true);
        CustomHandlers["POP"] = PushPopHandler(0x58, false);

        // Conditional jumps (near)
        CustomHandlers["JE"] = JccHandler(0x0F, 0x84);
        CustomHandlers["JNE"] = JccHandler(0x0F, 0x85);
        CustomHandlers["JG"] = JccHandler(0x0F, 0x8F);
        CustomHandlers["JL"] = JccHandler(0x0F, 0x8C);
        CustomHandlers["JGE"] = JccHandler(0x0F, 0x8D);
        CustomHandlers["JLE"] = JccHandler(0x0F, 0x8E);
        CustomHandlers["JA"] = JccHandler(0x0F, 0x87);
        CustomHandlers["JB"] = JccHandler(0x0F, 0x82);
        CustomHandlers["JAE"] = JccHandler(0x0F, 0x83);
        CustomHandlers["JBE"] = JccHandler(0x0F, 0x86);
        CustomHandlers["JO"] = JccHandler(0x0F, 0x80);
        CustomHandlers["JNO"] = JccHandler(0x0F, 0x81);
        CustomHandlers["JS"] = JccHandler(0x0F, 0x88);
        CustomHandlers["JNS"] = JccHandler(0x0F, 0x89);
        CustomHandlers["JP"] = JccHandler(0x0F, 0x8A);
        CustomHandlers["JNP"] = JccHandler(0x0F, 0x8B);

        // JMP / CALL (near)
        CustomHandlers["JMP"] = JmpCallHandler(0xE9);
        CustomHandlers["CALL"] = JmpCallHandler(0xE8);

        // LEA
        CustomHandlers["LEA"] = (ops, labels, addr) =>
        {
            int rd = RegNum(ops[0]);
            var combined = ops[1];
            int parenIdx = combined.IndexOf('(');
            if (parenIdx >= 0)
            {
                string offsetPart = combined.Substring(0, parenIdx).Trim();
                string basePart = combined.Substring(parenIdx + 1).TrimEnd(')');
                int rb = RegNum(basePart);
                int offset = string.IsNullOrEmpty(offsetPart) ? 0 : ResolveValue(offsetPart, labels, addr);
                return new byte[] { 0x8D, (byte)(ModRM(0, rd, rb)), (byte)(offset & 0xFF), (byte)((offset >> 8) & 0xFF), (byte)((offset >> 16) & 0xFF), (byte)((offset >> 24) & 0xFF) };
            }
            return new byte[] { 0x8D, (byte)(ModRM(0, rd, 5)) };
        };

        // XCHG
        CustomHandlers["XCHG"] = (ops, _, _) =>
        {
            int ra = RegNum(ops[0]), rb = RegNum(ops[1]);
            if (ra == 0) return new byte[] { (byte)(0x90 | rb) };
            if (rb == 0) return new byte[] { (byte)(0x90 | ra) };
            return new byte[] { 0x87, (byte)(ModRM(3, ra, rb)) };
        };

        // TEST
        CustomHandlers["TEST"] = (ops, labels, addr) =>
        {
            int ra = RegNum(ops[0]), rb = ops.Length > 1 ? RegNum(ops[1]) : ra;
            return new byte[] { 0x85, (byte)(ModRM(3, ra, rb)) };
        };

        // MOVZX/MOVSX
        CustomHandlers["MOVZX"] = (ops, _, _) =>
        {
            int rd = RegNum(ops[0]), rm = RegNum(ops[1]);
            return new byte[] { 0x0F, 0xB6, (byte)(ModRM(3, rd, rm)) };
        };
        CustomHandlers["MOVSX"] = (ops, _, _) =>
        {
            int rd = RegNum(ops[0]), rm = RegNum(ops[1]);
            return new byte[] { 0x0F, 0xBE, (byte)(ModRM(3, rd, rm)) };
        };

        // No-register instructions
        Opcodes["NOP"] = new byte[] { 0x90 };
        Opcodes["PUSHA"] = new byte[] { 0x60 };
        Opcodes["POPA"] = new byte[] { 0x61 };
        Opcodes["INT"] = new byte[] { 0xCD, 0xFD };
        Opcodes["IRET"] = new byte[] { 0xCF };
        Opcodes["CLC"] = new byte[] { 0xF8 };
        Opcodes["STC"] = new byte[] { 0xF9 };
        Opcodes["CMC"] = new byte[] { 0xF5 };
        Opcodes["CLD"] = new byte[] { 0xFC };
        Opcodes["STD"] = new byte[] { 0xFD };
        Opcodes["LAHF"] = new byte[] { 0x9F };
        Opcodes["SAHF"] = new byte[] { 0x9E };
        Opcodes["CBW"] = new byte[] { 0x98 };
        Opcodes["CWDE"] = new byte[] { 0x98 };
        Opcodes["CDQ"] = new byte[] { 0x99 };
        Opcodes["CWD"] = new byte[] { 0x99 };
        Opcodes["PUSHFD"] = new byte[] { 0x9C };
        Opcodes["POPFD"] = new byte[] { 0x9D };
        Opcodes["LEAVE"] = new byte[] { 0xC9 };
        Opcodes["ENTER"] = new byte[] { 0xC8, 0xFD, 0xFE, 0x00 };
        Opcodes["RET"] = new byte[] { 0xC3 };
        Opcodes["RETN"] = new byte[] { 0xC3 };
        Opcodes["RETF"] = new byte[] { 0xCB };
        Opcodes["INT3"] = new byte[] { 0xCC };
        Opcodes["SYSCALL"] = new byte[] { 0x0F, 0x05 };
        Opcodes["SYSRET"] = new byte[] { 0x0F, 0x07 };
    }

    private static int ToRegNum(string reg)
    {
        if (string.IsNullOrEmpty(reg)) return 0;
        var r = reg.Trim().ToLowerInvariant();
        return r switch
        {
            "eax" or "ax" or "al" or "ah" => 0,
            "ecx" or "cx" or "cl" or "ch" => 1,
            "edx" or "dx" or "dl" or "dh" => 2,
            "ebx" or "bx" or "bl" or "bh" => 3,
            "esp" or "sp" or "spl" => 4,
            "ebp" or "bp" or "bpl" => 5,
            "esi" or "si" or "sil" => 6,
            "edi" or "di" or "dil" => 7,
            _ when r.StartsWith("e") && r.Length > 1 && int.TryParse(r.Substring(1), out var n) && n >= 0 && n <= 7 => n,
            _ when int.TryParse(r, out var n) && n >= 0 && n <= 7 => n,
            _ => 0
        };
    }

    protected override int RegNum(string reg) => ToRegNum(reg);

    private static byte ModRM(int mod, int reg, int rm)
    {
        return (byte)(((mod & 3) << 6) | ((reg & 7) << 3) | (rm & 7));
    }

    private Func<string[], Dictionary<string, int>, int, byte[]> MovHandler()
    {
        return (ops, labels, addr) =>
        {
            int dst = ToRegNum(ops[0]);
            string src = ops[1];

            // MOV reg, [mem+offset] or MOV [mem+offset], reg
            int parenIdx = src.IndexOf('(');
            if (parenIdx >= 0)
            {
                string offsetPart = src.Substring(0, parenIdx).Trim();
                string basePart = src.Substring(parenIdx + 1).TrimEnd(')');
                int rb = ToRegNum(basePart);
                int offset = string.IsNullOrEmpty(offsetPart) ? 0 : ResolveValue(offsetPart, labels, addr);
                return new byte[] { 0x8B, ModRM(0, dst, rb), (byte)offset, (byte)(offset >> 8), (byte)(offset >> 16), (byte)(offset >> 24) };
            }

            // MOV reg, imm
            if (src.StartsWith("#") || char.IsDigit(src[0]) || src.StartsWith("0x") || src.StartsWith("$"))
            {
                int imm = ResolveValue(src, labels, addr);
                return new byte[] { (byte)(0xB8 | dst), (byte)imm, (byte)(imm >> 8), (byte)(imm >> 16), (byte)(imm >> 24) };
            }

            // MOV reg, reg
            int srcReg = ToRegNum(src);
            return new byte[] { 0x89, ModRM(3, srcReg, dst) };
        };
    }

    private Func<string[], Dictionary<string, int>, int, byte[]> MovbHandler()
    {
        return (ops, labels, addr) =>
        {
            int dst = ToRegNum(ops[0]);
            string src = ops[1];

            if (src.StartsWith("#") || char.IsDigit(src[0]) || src.StartsWith("0x"))
            {
                int imm = ResolveValue(src, labels, addr) & 0xFF;
                return new byte[] { (byte)(0xB0 | dst), (byte)imm };
            }

            int srcReg = ToRegNum(src);
            return new byte[] { 0x88, ModRM(3, srcReg, dst) };
        };
    }

    private Func<string[], Dictionary<string, int>, int, byte[]> AluRegHandler(int opMr, int opRm, int opImm, int ext, int immExt)
    {
        return (ops, labels, addr) =>
        {
            string op0 = ops[0], op1 = ops[1];

            // Check if immediate
            int val;
            bool isImm = !IsRegister(op1, out val);
            if (isImm && IsRegister(op0, out _))
            {
                int rd = ToRegNum(op0);
                int imm = ResolveValue(op1, labels, addr);
                // Use accumulator short form for EAX
                if (rd == 0 && imm == (sbyte)imm)
                    return new byte[] { (byte)immExt, (byte)(imm & 0xFF) };
                if (imm == (sbyte)imm)
                    return new byte[] { (byte)opImm, ModRM(3, ext, rd), (byte)(imm & 0xFF) };
                return new byte[] { 0x81, ModRM(3, ext, rd), (byte)imm, (byte)(imm >> 8), (byte)(imm >> 16), (byte)(imm >> 24) };
            }

            // [mem], reg or reg, [mem] or reg, reg
            int ra = ToRegNum(op0), rb = ToRegNum(op1);
            if (ops[0].Contains("(") || ops[0].Contains("["))
            {
                // [mem], reg
                return new byte[] { (byte)opMr, ModRM(0, rb, ra) };
            }
            // reg, [mem] or reg, reg
            return new byte[] { (byte)opRm, ModRM(3, ra, rb) };
        };
    }

    private Func<string[], Dictionary<string, int>, int, byte[]> PushPopHandler(int baseOp, bool isPush)
    {
        return (ops, _, _) =>
        {
            string op0 = ops[0];
            if (IsRegister(op0, out _))
            {
                int r = ToRegNum(op0);
                return new byte[] { (byte)(baseOp | r) };
            }
            return isPush
                ? new byte[] { 0x68, 0xFD, 0xFE, 0xFF, 0xFF }
                : new byte[] { 0x58 };
        };
    }

    private Func<string[], Dictionary<string, int>, int, byte[]> JccHandler(int prefix, int opcode)
    {
        return (ops, labels, addr) =>
        {
            int target = ResolveValue(ops[0], labels, addr);
            int offset = target - addr - 2;
            return new byte[] { (byte)prefix, (byte)opcode, (byte)(offset & 0xFF), (byte)((offset >> 8) & 0xFF), (byte)((offset >> 16) & 0xFF), (byte)((offset >> 24) & 0xFF) };
        };
    }

    private Func<string[], Dictionary<string, int>, int, byte[]> JmpCallHandler(int opcode)
    {
        return (ops, labels, addr) =>
        {
            int target = ResolveValue(ops[0], labels, addr);
            int offset = target - addr - 5;
            return new byte[] { (byte)opcode, (byte)(offset & 0xFF), (byte)((offset >> 8) & 0xFF), (byte)((offset >> 16) & 0xFF), (byte)((offset >> 24) & 0xFF) };
        };
    }

    private static bool IsRegister(string s, out int val)
    {
        val = 0;
        s = s.Trim();
        if (s.Contains("(") || s.Contains("[")) return false;
        if (s.StartsWith("#") || char.IsDigit(s[0]) || s.StartsWith("0x") || s.StartsWith("$") || s.StartsWith("0b"))
            return false;
        val = ToRegNum(s);
        return true;
    }
}
