using System;
using System.Collections.Generic;

namespace VMLToHex.Assemblers;

public class AssemblerRiscV : BaseAssembler
{
    public override string Name => "riscv";
    public override string Description => "RISC-V (32-bit)";

    public AssemblerRiscV()
    {
        // R-type: opcode, funct3, funct7
        CustomHandlers["ADD"] = RTypeHandler(0x33, 0x00, 0x00);
        CustomHandlers["SUB"] = RTypeHandler(0x33, 0x00, 0x20);
        CustomHandlers["SLL"] = RTypeHandler(0x33, 0x01, 0x00);
        CustomHandlers["SLT"] = RTypeHandler(0x33, 0x02, 0x00);
        CustomHandlers["SLTU"] = RTypeHandler(0x33, 0x03, 0x00);
        CustomHandlers["XOR"] = RTypeHandler(0x33, 0x04, 0x00);
        CustomHandlers["SRL"] = RTypeHandler(0x33, 0x05, 0x00);
        CustomHandlers["SRA"] = RTypeHandler(0x33, 0x05, 0x20);
        CustomHandlers["OR"] = RTypeHandler(0x33, 0x06, 0x00);
        CustomHandlers["AND"] = RTypeHandler(0x33, 0x07, 0x00);
        CustomHandlers["MUL"] = RTypeHandler(0x33, 0x00, 0x01);
        CustomHandlers["MULH"] = RTypeHandler(0x33, 0x01, 0x01);
        CustomHandlers["MULHU"] = RTypeHandler(0x33, 0x03, 0x01);
        CustomHandlers["DIV"] = RTypeHandler(0x33, 0x04, 0x01);
        CustomHandlers["DIVU"] = RTypeHandler(0x33, 0x05, 0x01);
        CustomHandlers["REM"] = RTypeHandler(0x33, 0x06, 0x01);
        CustomHandlers["REMU"] = RTypeHandler(0x33, 0x07, 0x01);

        // I-type: opcode, funct3
        CustomHandlers["ADDI"] = ITypeHandler(0x13, 0x00);
        CustomHandlers["SLTI"] = ITypeHandler(0x13, 0x02);
        CustomHandlers["SLTIU"] = ITypeHandler(0x13, 0x03);
        CustomHandlers["XORI"] = ITypeHandler(0x13, 0x04);
        CustomHandlers["ORI"] = ITypeHandler(0x13, 0x06);
        CustomHandlers["ANDI"] = ITypeHandler(0x13, 0x07);
        CustomHandlers["SLLI"] = ITypeShiftHandler(0x13, 0x01, 0x00);
        CustomHandlers["SRLI"] = ITypeShiftHandler(0x13, 0x05, 0x00);
        CustomHandlers["SRAI"] = ITypeShiftHandler(0x13, 0x05, 0x20);
        CustomHandlers["LW"] = ITypeLoadHandler(0x03, 0x02);
        CustomHandlers["LH"] = ITypeLoadHandler(0x03, 0x01);
        CustomHandlers["LB"] = ITypeLoadHandler(0x03, 0x00);
        CustomHandlers["LBU"] = ITypeLoadHandler(0x03, 0x04);
        CustomHandlers["LHU"] = ITypeLoadHandler(0x03, 0x05);
        CustomHandlers["JALR"] = ITypeHandler(0x67, 0x00);
        CustomHandlers["CSRRW"] = ITypeHandler(0x73, 0x01);
        CustomHandlers["CSRRS"] = ITypeHandler(0x73, 0x02);
        CustomHandlers["CSRRC"] = ITypeHandler(0x73, 0x03);

        // S-type: opcode, funct3
        CustomHandlers["SW"] = STypeHandler(0x23, 0x02);
        CustomHandlers["SH"] = STypeHandler(0x23, 0x01);
        CustomHandlers["SB"] = STypeHandler(0x23, 0x00);

        // B-type: opcode, funct3
        CustomHandlers["BEQ"] = BTypeHandler(0x63, 0x00);
        CustomHandlers["BNE"] = BTypeHandler(0x63, 0x01);
        CustomHandlers["BLT"] = BTypeHandler(0x63, 0x04);
        CustomHandlers["BGE"] = BTypeHandler(0x63, 0x05);
        CustomHandlers["BLTU"] = BTypeHandler(0x63, 0x06);
        CustomHandlers["BGEU"] = BTypeHandler(0x63, 0x07);

        // U-type: opcode
        CustomHandlers["LUI"] = UTypeHandler(0x37);
        CustomHandlers["AUIPC"] = UTypeHandler(0x17);

        // J-type: opcode
        CustomHandlers["JAL"] = JTypeHandler(0x6F);

        // Pseudo: NOP = ADDI x0, x0, 0
        CustomHandlers["NOP"] = (_, _, _) => new byte[] { 0x13, 0x00, 0x00, 0x00 };
        CustomHandlers["ECALL"] = (_, _, _) => new byte[] { 0x73, 0x00, 0x00, 0x00 };
        CustomHandlers["EBREAK"] = (_, _, _) => new byte[] { 0x73, 0x01, 0x00, 0x00 };
        CustomHandlers["FENCE"] = (_, _, _) => new byte[] { 0x0F, 0x00, 0x00, 0x00 };
        CustomHandlers["RET"] = (_, _, _) => EncodeIType32(0x67, 0x00, 0x00, 1, 0); // jalr x0, x1, 0

        // RV64W extensions (32-bit encodings)
        CustomHandlers["ADDW"] = RTypeHandler(0x3B, 0x00, 0x00);
        CustomHandlers["SUBW"] = RTypeHandler(0x3B, 0x00, 0x20);
        CustomHandlers["SLLIW"] = ITypeShiftHandler(0x1B, 0x01, 0x00);
        CustomHandlers["SRLIW"] = ITypeShiftHandler(0x1B, 0x05, 0x00);
        CustomHandlers["SRAIW"] = ITypeShiftHandler(0x1B, 0x05, 0x20);
    }

    protected override int RegNum(string reg)
    {
        if (string.IsNullOrEmpty(reg)) return 0;
        var r = reg.Trim();
        // Handle ABI names
        var lower = r.ToLowerInvariant();
        return lower switch
        {
            "zero" or "x0" => 0, "ra" or "x1" => 1, "sp" or "x2" => 2, "gp" or "x3" => 3,
            "tp" or "x4" => 4, "t0" or "x5" => 5, "t1" or "x6" => 6, "t2" or "x7" => 7,
            "s0" or "fp" or "x8" => 8, "s1" or "x9" => 9,
            "a0" or "x10" => 10, "a1" or "x11" => 11, "a2" or "x12" => 12, "a3" or "x13" => 13,
            "a4" or "x14" => 14, "a5" or "x15" => 15, "a6" or "x16" => 16, "a7" or "x17" => 17,
            "s2" or "x18" => 18, "s3" or "x19" => 19, "s4" or "x20" => 20, "s5" or "x21" => 21,
            "s6" or "x22" => 22, "s7" or "x23" => 23, "s8" or "x24" => 24, "s9" or "x25" => 25,
            "s10" or "x26" => 26, "s11" or "x27" => 27,
            "t3" or "x28" => 28, "t4" or "x29" => 29, "t5" or "x30" => 30, "t6" or "x31" => 31,
            _ when r.StartsWith("x") && int.TryParse(r.Substring(1), out var xn) && xn >= 0 && xn <= 31 => xn,
            _ when int.TryParse(r, out var n) && n >= 0 && n <= 31 => n,
            _ => 0
        };
    }

    // Encode 32-bit RISC-V instruction as little-endian bytes
    private static byte[] EncodeRType32(int opcode, int funct3, int funct7, int rd, int rs1, int rs2)
    {
        uint instr = (uint)((funct7 << 25) | (rs2 << 20) | (rs1 << 15) | (funct3 << 12) | (rd << 7) | opcode);
        return new byte[] { (byte)instr, (byte)(instr >> 8), (byte)(instr >> 16), (byte)(instr >> 24) };
    }

    private static byte[] EncodeIType32(int opcode, int funct3, int rd, int rs1, int imm12)
    {
        uint instr = (uint)(((imm12 & 0xFFF) << 20) | (rs1 << 15) | (funct3 << 12) | (rd << 7) | opcode);
        return new byte[] { (byte)instr, (byte)(instr >> 8), (byte)(instr >> 16), (byte)(instr >> 24) };
    }

    private static byte[] EncodeSType32(int opcode, int funct3, int rs1, int rs2, int imm12)
    {
        int imm11_5 = (imm12 >> 5) & 0x7F;
        int imm4_0 = imm12 & 0x1F;
        uint instr = (uint)((imm11_5 << 25) | (rs2 << 20) | (rs1 << 15) | (funct3 << 12) | (imm4_0 << 7) | opcode);
        return new byte[] { (byte)instr, (byte)(instr >> 8), (byte)(instr >> 16), (byte)(instr >> 24) };
    }

    private static byte[] EncodeBType32(int opcode, int funct3, int rs1, int rs2, int imm13)
    {
        int b12 = (imm13 >> 12) & 1;
        int b10_5 = (imm13 >> 5) & 0x3F;
        int b4_1 = (imm13 >> 1) & 0x0F;
        int b11 = (imm13 >> 11) & 1;
        uint instr = (uint)((b12 << 31) | (b10_5 << 25) | (rs2 << 20) | (rs1 << 15) | (funct3 << 12) | (b4_1 << 8) | (b11 << 7) | opcode);
        return new byte[] { (byte)instr, (byte)(instr >> 8), (byte)(instr >> 16), (byte)(instr >> 24) };
    }

    private static byte[] EncodeUType32(int opcode, int rd, int imm20)
    {
        uint instr = (uint)(((imm20 & 0xFFFFF) << 12) | (rd << 7) | opcode);
        return new byte[] { (byte)instr, (byte)(instr >> 8), (byte)(instr >> 16), (byte)(instr >> 24) };
    }

    private static byte[] EncodeJType32(int opcode, int rd, int imm21)
    {
        int j20 = (imm21 >> 20) & 1;
        int j10_1 = (imm21 >> 1) & 0x3FF;
        int j11 = (imm21 >> 11) & 1;
        int j19_12 = (imm21 >> 12) & 0xFF;
        uint instr = (uint)((j20 << 31) | (j19_12 << 12) | (j11 << 20) | (j10_1 << 21) | (rd << 7) | opcode);
        return new byte[] { (byte)instr, (byte)(instr >> 8), (byte)(instr >> 16), (byte)(instr >> 24) };
    }

    private Func<string[], Dictionary<string, int>, int, byte[]> RTypeHandler(int opcode, int funct3, int funct7)
    {
        return (ops, _, _) =>
        {
            int rd = RegNum(ops[0]), rs1 = RegNum(ops[1]), rs2 = RegNum(ops[2]);
            return EncodeRType32(opcode, funct3, funct7, rd, rs1, rs2);
        };
    }

    private Func<string[], Dictionary<string, int>, int, byte[]> ITypeHandler(int opcode, int funct3)
    {
        return (ops, labels, addr) =>
        {
            int rd = RegNum(ops[0]), rs1 = RegNum(ops[1]);
            int imm = ResolveValue(ops[2], labels, addr);
            return EncodeIType32(opcode, funct3, rd, rs1, imm);
        };
    }

    private Func<string[], Dictionary<string, int>, int, byte[]> ITypeShiftHandler(int opcode, int funct3, int funct7)
    {
        return (ops, labels, addr) =>
        {
            int rd = RegNum(ops[0]), rs1 = RegNum(ops[1]);
            int shamt = ResolveValue(ops[2], labels, addr) & 0x1F;
            return EncodeIType32(opcode, funct3, rd, rs1, (funct7 << 5) | shamt);
        };
    }

    private Func<string[], Dictionary<string, int>, int, byte[]> ITypeLoadHandler(int opcode, int funct3)
    {
        return (ops, labels, addr) =>
        {
            int rd = RegNum(ops[0]);
            // Parse "offset(base_reg)" format
            var combined = ops[1];
            int imm;
            int rs1;
            var parenIdx = combined.IndexOf('(');
            if (parenIdx >= 0)
            {
                imm = ResolveValue(combined.Substring(0, parenIdx), labels, addr);
                var baseReg = combined.Substring(parenIdx + 1).TrimEnd(')');
                rs1 = RegNum(baseReg);
            }
            else
            {
                // Fallback: ops[1] is offset, ops[2] is base reg
                imm = ResolveValue(combined, labels, addr);
                rs1 = ops.Length > 2 ? RegNum(ops[2]) : 0;
            }
            return EncodeIType32(opcode, funct3, rd, rs1, imm);
        };
    }

    private Func<string[], Dictionary<string, int>, int, byte[]> STypeHandler(int opcode, int funct3)
    {
        return (ops, labels, addr) =>
        {
            int rs2 = RegNum(ops[0]);
            // Parse "offset(base_reg)" format
            var combined = ops[1];
            int imm;
            int rs1;
            var parenIdx = combined.IndexOf('(');
            if (parenIdx >= 0)
            {
                imm = ResolveValue(combined.Substring(0, parenIdx), labels, addr);
                rs1 = RegNum(combined.Substring(parenIdx + 1).TrimEnd(')'));
            }
            else
            {
                imm = ResolveValue(combined, labels, addr);
                rs1 = ops.Length > 2 ? RegNum(ops[2]) : 0;
            }
            return EncodeSType32(opcode, funct3, rs1, rs2, imm);
        };
    }

    private Func<string[], Dictionary<string, int>, int, byte[]> BTypeHandler(int opcode, int funct3)
    {
        return (ops, labels, addr) =>
        {
            int rs1 = RegNum(ops[0]), rs2 = RegNum(ops[1]);
            int target = ResolveValue(ops[2], labels, addr);
            int offset = target - addr;
            return EncodeBType32(opcode, funct3, rs1, rs2, offset);
        };
    }

    private Func<string[], Dictionary<string, int>, int, byte[]> UTypeHandler(int opcode)
    {
        return (ops, labels, addr) =>
        {
            int rd = RegNum(ops[0]);
            int imm = ResolveValue(ops[1], labels, addr);
            return EncodeUType32(opcode, rd, imm >> 12);
        };
    }

    private Func<string[], Dictionary<string, int>, int, byte[]> JTypeHandler(int opcode)
    {
        return (ops, labels, addr) =>
        {
            int rd = RegNum(ops[0]);
            int target = ResolveValue(ops[1], labels, addr);
            int offset = target - addr;
            return EncodeJType32(opcode, rd, offset);
        };
    }
}
