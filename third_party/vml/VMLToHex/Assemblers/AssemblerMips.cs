using System;
using System.Collections.Generic;
using System.Linq;

namespace VMLToHex.Assemblers;

public class AssemblerMips : BaseAssembler
{
    public override string Name => "mips";
    public override string Description => "MIPS (32-bit)";

    public AssemblerMips()
    {
        // Instructions with register encoding use CustomHandlers
        // R-type (opcode=0, funct differs)
        CustomHandlers["ADD"] = RTypeHandler(0x20);
        CustomHandlers["ADDU"] = RTypeHandler(0x21);
        CustomHandlers["SUB"] = RTypeHandler(0x22);
        CustomHandlers["SUBU"] = RTypeHandler(0x23);
        CustomHandlers["MUL"] = RTypeHandler(0x02, 0x1C);
        CustomHandlers["AND"] = RTypeHandler(0x24);
        CustomHandlers["OR"] = RTypeHandler(0x25);
        CustomHandlers["XOR"] = RTypeHandler(0x26);
        CustomHandlers["NOR"] = RTypeHandler(0x27);
        CustomHandlers["SLT"] = RTypeHandler(0x2A);
        CustomHandlers["SLTU"] = RTypeHandler(0x2B);
        CustomHandlers["SLL"] = RTypeShiftHandler(0x00);
        CustomHandlers["SRL"] = RTypeShiftHandler(0x02);
        CustomHandlers["SRA"] = RTypeShiftHandler(0x03);
        CustomHandlers["SLLV"] = RTypeHandler(0x04);
        CustomHandlers["SRLV"] = RTypeHandler(0x06);
        CustomHandlers["SRAV"] = RTypeHandler(0x07);
        CustomHandlers["JR"] = JRHandler();
        CustomHandlers["JALR"] = JALRHandler();
        CustomHandlers["MFHI"] = RType1RegHandler(0x10);
        CustomHandlers["MFLO"] = RType1RegHandler(0x12);
        CustomHandlers["MTHI"] = new Func<string[], Dictionary<string, int>, int, byte[]>((ops, _, _) => EncodeRType(0, RegNum(ops[0]), 0, 0, 0x11));
        CustomHandlers["MTLO"] = new Func<string[], Dictionary<string, int>, int, byte[]>((ops, _, _) => EncodeRType(0, RegNum(ops[0]), 0, 0, 0x13));
        CustomHandlers["MULT"] = RType2RegHandler(0x18);
        CustomHandlers["MULTU"] = RType2RegHandler(0x19);
        CustomHandlers["DIV"] = RType2RegHandler(0x1A);
        CustomHandlers["DIVU"] = RType2RegHandler(0x1B);

        // I-type
        CustomHandlers["ADDI"] = ITypeHandler(0x08);
        CustomHandlers["ADDIU"] = ITypeHandler(0x09);
        CustomHandlers["SLTI"] = ITypeHandler(0x0A);
        CustomHandlers["SLTIU"] = ITypeHandler(0x0B);
        CustomHandlers["ANDI"] = ITypeHandler(0x0C);
        CustomHandlers["ORI"] = ITypeHandler(0x0D);
        CustomHandlers["XORI"] = ITypeHandler(0x0E);
        CustomHandlers["LW"] = ITypeHandler(0x23);
        CustomHandlers["LB"] = ITypeHandler(0x20);
        CustomHandlers["LBU"] = ITypeHandler(0x24);
        CustomHandlers["LH"] = ITypeHandler(0x21);
        CustomHandlers["LHU"] = ITypeHandler(0x25);
        CustomHandlers["SW"] = ITypeHandler(0x2B);
        CustomHandlers["SB"] = ITypeHandler(0x28);
        CustomHandlers["SH"] = ITypeHandler(0x29);
        CustomHandlers["BEQ"] = BranchHandler(0x04);
        CustomHandlers["BNE"] = BranchHandler(0x05);
        CustomHandlers["BGTZ"] = BranchZeroHandler(0x07);
        CustomHandlers["BLEZ"] = BranchZeroHandler(0x06);
        CustomHandlers["LUI"] = LUIHandler();
        CustomHandlers["LW_SP"] = new Func<string[], Dictionary<string, int>, int, byte[]>((ops, labels, addr) => EncodeIType(0x23, 29, RegNum(ops[0]), ResolveValue(ops[1], labels, addr)));

        // J-type
        CustomHandlers["J"] = JTypeHandler(0x02);
        CustomHandlers["JAL"] = JTypeHandler(0x03);

        // No-register instructions
        Opcodes["NOP"] = new byte[] { 0x00, 0x00, 0x00, 0x00 };
        Opcodes["SYSCALL"] = new byte[] { 0x00, 0x00, 0x00, 0x0C };
        Opcodes["BREAK"] = new byte[] { 0x00, 0x00, 0x00, 0x0D };
        Opcodes["WAIT"] = new byte[] { 0x42, 0x00, 0x00, 0x20 };
    }

    private static int ToRegNum(string reg)
    {
        if (string.IsNullOrEmpty(reg)) return 0;
        var r = reg.Trim().TrimStart('$');
        if (int.TryParse(r, out var n)) return n;
        return r.ToLowerInvariant() switch
        {
            "zero" => 0, "at" => 1, "v0" => 2, "v1" => 3,
            "a0" => 4, "a1" => 5, "a2" => 6, "a3" => 7,
            "t0" => 8, "t1" => 9, "t2" => 10, "t3" => 11,
            "t4" => 12, "t5" => 13, "t6" => 14, "t7" => 15,
            "s0" => 16, "s1" => 17, "s2" => 18, "s3" => 19,
            "s4" => 20, "s5" => 21, "s6" => 22, "s7" => 23,
            "t8" => 24, "t9" => 25,
            "k0" => 26, "k1" => 27,
            "gp" => 28, "sp" => 29, "fp" => 30, "ra" => 31,
            _ => 0
        };
    }

    protected override int RegNum(string reg) => ToRegNum(reg);

    // Encode R-type: opcode=0, rs, rt, rd, shamt, funct
    private static byte[] EncodeRType(int rs, int rt, int rd, int shamt, int funct)
    {
        uint instr = (uint)((rs << 21) | (rt << 16) | (rd << 11) | (shamt << 6) | funct);
        return new byte[] { (byte)(instr >> 24), (byte)(instr >> 16), (byte)(instr >> 8), (byte)instr };
    }

    // Encode I-type: opcode, rs, rt, imm16
    private static byte[] EncodeIType(int opcode, int rs, int rt, int imm)
    {
        uint instr = (uint)((opcode << 26) | (rs << 21) | (rt << 16) | (imm & 0xFFFF));
        return new byte[] { (byte)(instr >> 24), (byte)(instr >> 16), (byte)(instr >> 8), (byte)instr };
    }

    // Encode J-type: opcode, addr26
    private static byte[] EncodeJType(int opcode, int addr)
    {
        uint instr = (uint)((opcode << 26) | (addr & 0x3FFFFFF));
        return new byte[] { (byte)(instr >> 24), (byte)(instr >> 16), (byte)(instr >> 8), (byte)instr };
    }

    // R-type: op $rd, $rs, $rt
    private Func<string[], Dictionary<string, int>, int, byte[]> RTypeHandler(int funct)
    {
        return (ops, labels, addr) =>
        {
            int rd = ToRegNum(ops[0]), rs = ToRegNum(ops[1]), rt = ToRegNum(ops[2]);
            return EncodeRType(rs, rt, rd, 0, funct);
        };
    }

    // R-type with 3-op variant for MUL (SPECIAL2 opcode=0x1C)
    private Func<string[], Dictionary<string, int>, int, byte[]> RTypeHandler(int funct, int opcode)
    {
        return (ops, labels, addr) =>
        {
            int rd = ToRegNum(ops[0]), rs = ToRegNum(ops[1]), rt = ToRegNum(ops[2]);
            uint instr = (uint)((opcode << 26) | (rs << 21) | (rt << 16) | (rd << 11) | funct);
            return new byte[] { (byte)(instr >> 24), (byte)(instr >> 16), (byte)(instr >> 8), (byte)instr };
        };
    }

    // R-type shift: op $rd, $rt, shamt
    private Func<string[], Dictionary<string, int>, int, byte[]> RTypeShiftHandler(int funct)
    {
        return (ops, labels, addr) =>
        {
            int rd = ToRegNum(ops[0]), rt = ToRegNum(ops[1]);
            int shamt = ops.Length > 2 ? ResolveValue(ops[2], labels, addr) : 0;
            return EncodeRType(0, rt, rd, shamt, funct);
        };
    }

    // R-type with 1 reg (MFHI/MFLO)
    private Func<string[], Dictionary<string, int>, int, byte[]> RType1RegHandler(int funct)
    {
        return (ops, labels, addr) =>
        {
            int rd = ToRegNum(ops[0]);
            return EncodeRType(0, 0, rd, 0, funct);
        };
    }

    // R-type with 2 regs (MULT/DIV)
    private Func<string[], Dictionary<string, int>, int, byte[]> RType2RegHandler(int funct)
    {
        return (ops, labels, addr) =>
        {
            int rs = ToRegNum(ops[0]), rt = ToRegNum(ops[1]);
            return EncodeRType(rs, rt, 0, 0, funct);
        };
    }

    // I-type: op $rt, $rs, imm
    private Func<string[], Dictionary<string, int>, int, byte[]> ITypeHandler(int opcode)
    {
        return (ops, labels, addr) =>
        {
            int rt = ToRegNum(ops[0]), rs = ToRegNum(ops[1]);
            int imm = ResolveValue(ops[2], labels, addr);
            return EncodeIType(opcode, rs, rt, imm);
        };
    }

    // Branch: beq $rs, $rt, target
    private Func<string[], Dictionary<string, int>, int, byte[]> BranchHandler(int opcode)
    {
        return (ops, labels, addr) =>
        {
            int rs = ToRegNum(ops[0]), rt = ToRegNum(ops[1]);
            int target = ResolveValue(ops[2], labels, addr);
            int offset = (target - (addr + 4)) >> 2; // branch offset in words
            return EncodeIType(opcode, rs, rt, offset);
        };
    }

    // Branch on zero: bgtz/blez $rs, target
    private Func<string[], Dictionary<string, int>, int, byte[]> BranchZeroHandler(int opcode)
    {
        return (ops, labels, addr) =>
        {
            int rs = ToRegNum(ops[0]);
            int target = ResolveValue(ops[1], labels, addr);
            int offset = (target - (addr + 4)) >> 2;
            return EncodeIType(opcode, rs, 0, offset);
        };
    }

    // LUI: $rt, imm
    private Func<string[], Dictionary<string, int>, int, byte[]> LUIHandler()
    {
        return (ops, labels, addr) =>
        {
            int rt = ToRegNum(ops[0]);
            int imm = ResolveValue(ops[1], labels, addr);
            return EncodeIType(0x0F, 0, rt, imm);
        };
    }

    // JR: $rs
    private Func<string[], Dictionary<string, int>, int, byte[]> JRHandler()
    {
        return (ops, labels, addr) =>
        {
            int rs = ToRegNum(ops[0]);
            return EncodeRType(rs, 0, 0, 0, 0x08);
        };
    }

    // JALR: $rd, $rs
    private Func<string[], Dictionary<string, int>, int, byte[]> JALRHandler()
    {
        return (ops, labels, addr) =>
        {
            int rd = ops.Length > 1 ? ToRegNum(ops[0]) : 31;
            int rs = ops.Length > 1 ? ToRegNum(ops[1]) : ToRegNum(ops[0]);
            return EncodeRType(rs, 0, rd, 0, 0x09);
        };
    }

    // J-type: J/JAL target
    private Func<string[], Dictionary<string, int>, int, byte[]> JTypeHandler(int opcode)
    {
        return (ops, labels, addr) =>
        {
            int target = ResolveValue(ops[0], labels, addr);
            return EncodeJType(opcode, target >> 2);
        };
    }
}
