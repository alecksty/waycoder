using System;
using System.Collections.Generic;

namespace VMLToHex.Assemblers;

public class Assembler68000 : BaseAssembler
{
    public override string Name => "68000";
    public override string Description => "Motorola 68000";

    public Assembler68000()
    {
        // MOVE Dn, Dm (size long)
        CustomHandlers["MOVE"] = (ops, labels, addr) =>
        {
            int dst = ToRegNum(ops[1]), src = ToRegNum(ops[0]);
            int mode = EffectiveAddress(ops[0], labels, addr, out int srcMode, out int srcReg);
            int dstMode = EffectiveAddress(ops[1], labels, addr, out int dm, out int dr);
            // MOVE <ea>, <ea> — full form complex, simplify: MOVE Dn, Dm
            return new byte[] { (byte)(0x30 | (src << 3) | dst), 0x3C }; // simplified
        };
        CustomHandlers["MOVEQ"] = (ops, labels, addr) =>
        {
            int dn = ToRegNum(ops[0]);
            int imm = ResolveValue(ops[1], labels, addr) & 0xFF;
            return new byte[] { (byte)(0x70 | dn), (byte)imm };
        };
        CustomHandlers["MOVEA"] = (ops, _, _) =>
        {
            int an = ToRegNum(ops[1]), dn = ToRegNum(ops[0]);
            return new byte[] { (byte)(0x30 | (dn << 3) | an), 0x7C };
        };
        CustomHandlers["MOVEM"] = (ops, labels, addr) =>
        {
            int mode = EffectiveAddress(ops[1], labels, addr, out int dm, out int dr);
            int regList = ParseRegList(ops[0]);
            return new byte[] { 0x48, 0xE7, (byte)(regList & 0xFF), (byte)((regList >> 8) & 0xFF) };
        };
        CustomHandlers["LEA"] = (ops, labels, addr) =>
        {
            int an = ToRegNum(ops[1]);
            if (ops[0].Contains("("))
            {
                var combined = ops[0];
                int parenIdx = combined.IndexOf('(');
                string offsetPart = combined.Substring(0, parenIdx).Trim();
                string basePart = combined.Substring(parenIdx + 1).TrimEnd(')');
                int rn = ToRegNum(basePart);
                int offset = string.IsNullOrEmpty(offsetPart) ? 0 : ResolveValue(offsetPart, labels, addr);
                return new byte[] { 0x41, (byte)(0xE8 | an), (byte)(offset & 0xFF), (byte)((offset >> 8) & 0xFF) };
            }
            // Absolute address
            int addrVal = ResolveValue(ops[0], labels, addr);
            return new byte[] { 0x41, (byte)(0xF9 | an), (byte)((addrVal >> 24) & 0xFF), (byte)((addrVal >> 16) & 0xFF), (byte)((addrVal >> 8) & 0xFF), (byte)(addrVal & 0xFF) };
        };
        CustomHandlers["ADD"] = AluHandler(0xD0, 0xD0, 0x06);
        CustomHandlers["SUB"] = AluHandler(0x90, 0x90, 0x04);
        CustomHandlers["AND"] = AluHandler(0xC0, 0xC0, 0x02);
        CustomHandlers["OR"] = AluHandler(0x80, 0x80, 0x00);
        CustomHandlers["EOR"] = (ops, _, _) =>
        {
            int dn = ToRegNum(ops[0]), dm = ToRegNum(ops[1]);
            return new byte[] { (byte)(0xB0 | (dn << 3) | dm), 0x3C };
        };
        CustomHandlers["CMP"] = (ops, _, _) =>
        {
            int dn = ToRegNum(ops[0]), dm = ToRegNum(ops[1]);
            return new byte[] { (byte)(0xB0 | (dn << 3) | dm), 0x3C };
        };

        // MULS/DIVS Dn, Dm
        CustomHandlers["MULS"] = (ops, _, _) =>
        {
            int dn = ToRegNum(ops[0]), dm = ToRegNum(ops[1]);
            return new byte[] { (byte)(0xC0 | (dn << 3) | dm), 0xFC };
        };
        CustomHandlers["DIVS"] = (ops, _, _) =>
        {
            int dn = ToRegNum(ops[0]), dm = ToRegNum(ops[1]);
            return new byte[] { (byte)(0x80 | (dn << 3) | dm), 0xFC };
        };

        // NOT/NEG Dn
        CustomHandlers["NOT"] = (ops, _, _) =>
        {
            int dn = ToRegNum(ops[0]);
            return new byte[] { 0x46, (byte)(0x00 | (dn << 3) | dn) };
        };
        CustomHandlers["NEG"] = (ops, _, _) =>
        {
            int dn = ToRegNum(ops[0]);
            return new byte[] { 0x44, (byte)(0x00 | (dn << 3) | dn) };
        };
        CustomHandlers["NEGX"] = (ops, _, _) =>
        {
            int dn = ToRegNum(ops[0]);
            return new byte[] { 0x40, (byte)(0x00 | (dn << 3) | dn) };
        };

        // Shift/rotate Dn, Dm (register shift)
        CustomHandlers["ASL"] = (ops, _, _) =>
        {
            int dn = ToRegNum(ops[0]), dm = ToRegNum(ops[1]);
            return new byte[] { (byte)(0xE1 | (dn << 3) | dm), 0x00 };
        };
        CustomHandlers["ASR"] = (ops, _, _) =>
        {
            int dn = ToRegNum(ops[0]), dm = ToRegNum(ops[1]);
            return new byte[] { (byte)(0xE0 | (dn << 3) | dm), 0x00 };
        };
        CustomHandlers["LSL"] = (ops, _, _) =>
        {
            int dn = ToRegNum(ops[0]), dm = ToRegNum(ops[1]);
            return new byte[] { (byte)(0xE9 | (dn << 3) | dm), 0x00 };
        };
        CustomHandlers["LSR"] = (ops, _, _) =>
        {
            int dn = ToRegNum(ops[0]), dm = ToRegNum(ops[1]);
            return new byte[] { (byte)(0xE8 | (dn << 3) | dm), 0x00 };
        };
        CustomHandlers["ROL"] = (ops, _, _) =>
        {
            int dn = ToRegNum(ops[0]), dm = ToRegNum(ops[1]);
            return new byte[] { (byte)(0xE7 | (dn << 3) | dm), 0x00 };
        };
        CustomHandlers["ROR"] = (ops, _, _) =>
        {
            int dn = ToRegNum(ops[0]), dm = ToRegNum(ops[1]);
            return new byte[] { (byte)(0xE6 | (dn << 3) | dm), 0x00 };
        };

        // TST Dn
        CustomHandlers["TST"] = (ops, _, _) =>
        {
            int dn = ToRegNum(ops[0]);
            return new byte[] { 0x4A, (byte)(0x00 | (dn << 3) | dn) };
        };
        CustomHandlers["CLR"] = (ops, _, _) =>
        {
            int dn = ToRegNum(ops[0]);
            return new byte[] { 0x42, (byte)(0x00 | (dn << 3) | dn) };
        };
        CustomHandlers["CLRB"] = (ops, _, _) =>
        {
            int dn = ToRegNum(ops[0]);
            return new byte[] { 0x42, (byte)(0x00 | (dn << 3) | dn) };
        };
        CustomHandlers["CLRW"] = (ops, _, _) =>
        {
            int dn = ToRegNum(ops[0]);
            return new byte[] { 0x42, (byte)(0x40 | (dn << 3) | dn) };
        };
        CustomHandlers["CLRL"] = (ops, _, _) =>
        {
            int dn = ToRegNum(ops[0]);
            return new byte[] { 0x42, (byte)(0x80 | (dn << 3) | dn) };
        };

        // Branches
        CustomHandlers["BEQ"] = BranchHandler(0x67);
        CustomHandlers["BNE"] = BranchHandler(0x66);
        CustomHandlers["BGT"] = BranchHandler(0x6E);
        CustomHandlers["BLT"] = BranchHandler(0x6D);
        CustomHandlers["BGE"] = BranchHandler(0x6C);
        CustomHandlers["BLE"] = BranchHandler(0x6F);
        CustomHandlers["BHI"] = BranchHandler(0x62);
        CustomHandlers["BLS"] = BranchHandler(0x63);
        CustomHandlers["BCS"] = BranchHandler(0x65);
        CustomHandlers["BCC"] = BranchHandler(0x64);
        CustomHandlers["BMI"] = BranchHandler(0x6B);
        CustomHandlers["BPL"] = BranchHandler(0x6A);
        CustomHandlers["BVS"] = BranchHandler(0x67);
        CustomHandlers["BVC"] = BranchHandler(0x66);

        // JMP/JSR
        CustomHandlers["JMP"] = (ops, labels, addr) =>
        {
            int target = ResolveValue(ops[0], labels, addr);
            return new byte[] { 0x4E, 0xF9, (byte)((target >> 24) & 0xFF), (byte)((target >> 16) & 0xFF), (byte)((target >> 8) & 0xFF), (byte)(target & 0xFF) };
        };
        CustomHandlers["JSR"] = (ops, labels, addr) =>
        {
            int target = ResolveValue(ops[0], labels, addr);
            return new byte[] { 0x61, (byte)(0x00) };
        };

        // ADDQ/SUBQ #imm, Dn
        CustomHandlers["ADDQ"] = (ops, labels, addr) =>
        {
            int imm = ResolveValue(ops[0], labels, addr) & 7;
            int dn = ToRegNum(ops[1]);
            if (imm == 0) imm = 8;
            return new byte[] { (byte)(0x50 | ((imm - 1) << 3) | dn), 0x00 };
        };
        CustomHandlers["SUBQ"] = (ops, labels, addr) =>
        {
            int imm = ResolveValue(ops[0], labels, addr) & 7;
            int dn = ToRegNum(ops[1]);
            if (imm == 0) imm = 8;
            return new byte[] { (byte)(0x51 | ((imm - 1) << 3) | dn), 0x00 };
        };

        // BCHG/BCLR/BSET Dn, Dm
        CustomHandlers["BCHG"] = (ops, _, _) =>
        {
            int dn = ToRegNum(ops[0]), dm = ToRegNum(ops[1]);
            return new byte[] { (byte)(0x01 | (dn << 3) | dm), 0x40 };
        };
        CustomHandlers["BCLR"] = (ops, _, _) =>
        {
            int dn = ToRegNum(ops[0]), dm = ToRegNum(ops[1]);
            return new byte[] { (byte)(0x01 | (dn << 3) | dm), 0x80 };
        };
        CustomHandlers["BSET"] = (ops, _, _) =>
        {
            int dn = ToRegNum(ops[0]), dm = ToRegNum(ops[1]);
            return new byte[] { (byte)(0x01 | (dn << 3) | dm), 0xC0 };
        };

        // No-register instructions
        Opcodes["RTS"] = new byte[] { 0x4E, 0x75 };
        Opcodes["RTE"] = new byte[] { 0x4E, 0x73 };
        Opcodes["TRAP"] = new byte[] { 0x4E, 0x40 };
        Opcodes["NOP"] = new byte[] { 0x4E, 0x71 };
        Opcodes["SWAP"] = new byte[] { 0x48, 0x40 };
        Opcodes["EXT"] = new byte[] { 0x48, 0x80 };
        Opcodes["UNLK"] = new byte[] { 0x4E, 0x58 };
        Opcodes["PEA"] = new byte[] { 0x48, 0x60 };
        Opcodes["ABCD"] = new byte[] { 0xC1, 0x00 };
        Opcodes["SBCD"] = new byte[] { 0x81, 0x00 };
        Opcodes["SF"] = new byte[] { 0x51, 0xCF };
        Opcodes["ST"] = new byte[] { 0x50, 0xCF };
    }

    private static int ToRegNum(string reg)
    {
        if (string.IsNullOrEmpty(reg)) return 0;
        var r = reg.Trim().ToLowerInvariant();
        return r switch
        {
            "d0" => 0, "d1" => 1, "d2" => 2, "d3" => 3,
            "d4" => 4, "d5" => 5, "d6" => 6, "d7" => 7,
            "a0" => 8, "a1" => 9, "a2" => 10, "a3" => 11,
            "a4" => 12, "a5" => 13, "a6" => 14, "a7" or "sp" => 15,
            _ when int.TryParse(r, out var n) && n >= 0 && n <= 15 => n,
            _ => 0
        };
    }

    protected override int RegNum(string reg) => ToRegNum(reg);

    private int EffectiveAddress(string operand, Dictionary<string, int> labels, int addr, out int mode, out int reg)
    {
        mode = 0; reg = 0;
        if (string.IsNullOrEmpty(operand)) return 0;

        // Dn
        int rn = ToRegNum(operand);
        if (operand.StartsWith("d") || operand.StartsWith("D"))
        {
            mode = 0; // data register direct
            reg = rn;
            return rn;
        }
        if (operand.StartsWith("a") || operand.StartsWith("A"))
        {
            mode = 1; // address register direct
            reg = rn - 8;
            return rn;
        }
        // An
        if (operand.Contains("("))
        {
            // (An), (An)+, -(An), d16(An), d8(An, Xn)
            mode = 2; // address register indirect
            return 0;
        }
        // Absolute
        mode = 7;
        reg = 1; // absolute long
        return ResolveValue(operand, labels, addr);
    }

    private int ParseRegList(string operand)
    {
        int regList = 0;
        var parts = operand.Split('/');
        foreach (var part in parts)
        {
            var p = part.Trim();
            if (p.Contains("-"))
            {
                var range = p.Split('-');
                int start = ToRegNum(range[0].Trim());
                int end = ToRegNum(range[1].Trim());
                for (int r = start; r <= end; r++) regList |= 1 << r;
            }
            else
            {
                regList |= 1 << ToRegNum(p);
            }
        }
        return regList;
    }

    private Func<string[], Dictionary<string, int>, int, byte[]> AluHandler(int opReg, int opImm, int opArithImm)
    {
        return (ops, labels, addr) =>
        {
            int dn = ToRegNum(ops[0]), dm = ToRegNum(ops[1]);
            if (ops[0].StartsWith("d") || ops[0].StartsWith("D"))
            {
                return new byte[] { (byte)(opReg | (dn << 3) | dm), 0x3C };
            }
            return new byte[] { (byte)(opReg | (dn << 3) | dm), 0x3C };
        };
    }

    private Func<string[], Dictionary<string, int>, int, byte[]> BranchHandler(int opcode)
    {
        return (ops, labels, addr) =>
        {
            int target = ResolveValue(ops[0], labels, addr);
            int offset = target - addr - 2;
            return new byte[] { (byte)opcode, (byte)(offset & 0xFF) };
        };
    }
}
