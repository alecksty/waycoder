using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace VMLToHex.Assemblers;

public class Assembler8051 : BaseAssembler
{
    public override string Name => "8051";
    public override string Description => "Intel 8051 8-bit MCU";

    private static readonly Dictionary<string, int> RegMap = new(StringComparer.OrdinalIgnoreCase)
    {
        ["r0"] = 0, ["r1"] = 1, ["r2"] = 2, ["r3"] = 3,
        ["r4"] = 4, ["r5"] = 5, ["r6"] = 6, ["r7"] = 7,
        ["a"] = 8, ["b"] = 9, ["c"] = 10, ["dptr"] = 11
    };

    protected override int RegNum(string reg) => RegMap.TryGetValue(reg, out var r) ? r : 0;

    private static int ResolveOp8051(string op, Dictionary<string, int> labels, int addr)
    {
        if (string.IsNullOrEmpty(op)) return 0;
        if (op.StartsWith("#")) op = op.Substring(1);
        if (op.StartsWith("/")) op = op.Substring(1);
        if (labels != null && labels.TryGetValue(op, out var la)) return la;
        // Hex
        if (op.StartsWith("0x", StringComparison.OrdinalIgnoreCase))
            return int.TryParse(op.Substring(2), NumberStyles.HexNumber, null, out var h) ? h : 0;
        if (op.EndsWith("h", StringComparison.OrdinalIgnoreCase) && op.Length > 1)
            return int.TryParse(op.TrimEnd('h', 'H'), NumberStyles.HexNumber, null, out var he) ? he : 0;
        if (int.TryParse(op, out var d)) return d;
        return 0;
    }

    public Assembler8051()
    {
        // MOV A, Rn — 0xE8 | n
        // MOV Rn, A — 0xF8 | n
        // MOV A, @Ri — 0xE6 | i
        // MOV @Ri, A — 0xF6 | i
        // MOV A, #data — 0x74 data
        // MOV Rn, #data — 0x78 | n, data
        // MOV A, direct — 0xE5 direct
        // MOV direct, A — 0xF5 direct
        CustomHandlers["MOV"] = (ops, labels, addr) =>
        {
            if (ops.Length < 2) return new byte[] { 0 };
            var dst = ops[0].Trim();
            var src = ops[1].Trim();
            int regN;
            // MOV A, ...
            if (dst.Equals("a", StringComparison.OrdinalIgnoreCase))
            {
                if (src.Equals("a", StringComparison.OrdinalIgnoreCase)) return new byte[] { 0 };
                // MOV A, Rn
                if (src.Length == 2 && src[0] == 'r' && char.IsDigit(src[1]))
                {
                    regN = src[1] - '0';
                    return new byte[] { (byte)(0xE8 | regN) };
                }
                // MOV A, @Ri
                if (src.StartsWith("@r") && src.Length == 3 && char.IsDigit(src[2]))
                {
                    int ri = src[2] - '0';
                    return new byte[] { (byte)(0xE6 | ri) };
                }
                // MOV A, #data
                if (src.StartsWith("#"))
                {
                    int val = ResolveOp8051(src, labels, addr);
                    return new byte[] { 0x74, (byte)(val & 0xFF) };
                }
                // MOV A, direct
                int dir = ResolveOp8051(src, labels, addr);
                return new byte[] { 0xE5, (byte)(dir & 0xFF) };
            }
            // MOV Rn, ...
            if (dst.Length == 2 && dst[0] == 'r' && char.IsDigit(dst[1]))
            {
                regN = dst[1] - '0';
                // MOV Rn, A
                if (src.Equals("a", StringComparison.OrdinalIgnoreCase))
                    return new byte[] { (byte)(0xF8 | regN) };
                // MOV Rn, #data
                if (src.StartsWith("#"))
                {
                    int val = ResolveOp8051(src, labels, addr);
                    return new byte[] { (byte)(0x78 | regN), (byte)(val & 0xFF) };
                }
                // MOV Rn, direct
                int d = ResolveOp8051(src, labels, addr);
                return new byte[] { (byte)(0xA8 | regN), (byte)(d & 0xFF) };
            }
            // MOV @Ri, A
            if (dst.StartsWith("@r") && dst.Length == 3 && char.IsDigit(dst[2]) && src.Equals("a", StringComparison.OrdinalIgnoreCase))
            {
                int ri = dst[2] - '0';
                return new byte[] { (byte)(0xF6 | ri) };
            }
            // MOV @Ri, #data
            if (dst.StartsWith("@r") && dst.Length == 3 && char.IsDigit(dst[2]) && src.StartsWith("#"))
            {
                int ri = dst[2] - '0';
                int val = ResolveOp8051(src, labels, addr);
                return new byte[] { (byte)(0x76 | ri), (byte)(val & 0xFF) };
            }
            // MOV direct, A
            if (src.Equals("a", StringComparison.OrdinalIgnoreCase) && !dst.StartsWith("@"))
            {
                int d = ResolveOp8051(dst, labels, addr);
                return new byte[] { 0xF5, (byte)(d & 0xFF) };
            }
            // MOV direct, Rn
            if (src.Length == 2 && src[0] == 'r' && char.IsDigit(src[1]) && !dst.StartsWith("@"))
            {
                regN = src[1] - '0';
                int d = ResolveOp8051(dst, labels, addr);
                return new byte[] { (byte)(0x88 | regN), (byte)(d & 0xFF) };
            }
            // MOV direct, #data
            if (src.StartsWith("#") && !dst.StartsWith("@") && !dst.StartsWith("r", StringComparison.OrdinalIgnoreCase) && !dst.Equals("a", StringComparison.OrdinalIgnoreCase))
            {
                int d = ResolveOp8051(dst, labels, addr);
                int val = ResolveOp8051(src, labels, addr);
                return new byte[] { 0x75, (byte)(d & 0xFF), (byte)(val & 0xFF) };
            }
            // MOV DPTR, #data16
            if (dst.Equals("dptr", StringComparison.OrdinalIgnoreCase) && src.StartsWith("#"))
            {
                int val = ResolveOp8051(src, labels, addr);
                return new byte[] { 0x90, (byte)((val >> 8) & 0xFF), (byte)(val & 0xFF) };
            }
            return new byte[] { 0 };
        };

        // ADD A, Rn — 0x28 | n
        // ADD A, @Ri — 0x26 | i
        // ADD A, #data — 0x24 data
        // ADDC A, Rn — 0x38 | n
        // SUBB A, Rn — 0x98 | n
        void AluRn(string name, byte opRn, byte opRi, byte opImm)
        {
            CustomHandlers[name] = (ops, labels, addr) =>
            {
                var src = ops.Last().Trim();
                if (src.Length == 2 && src[0] == 'r' && char.IsDigit(src[1]))
                    return new byte[] { (byte)(opRn | (src[1] - '0')) };
                if (src.StartsWith("@r") && src.Length == 3 && char.IsDigit(src[2]))
                    return new byte[] { (byte)(opRi | (src[2] - '0')) };
                if (src.StartsWith("#"))
                {
                    int val = ResolveOp8051(src, labels, addr);
                    return new byte[] { opImm, (byte)(val & 0xFF) };
                }
                return new byte[] { opImm, (byte)(ResolveOp8051(src, labels, addr) & 0xFF) };
            };
        }

        AluRn("ADD", 0x28, 0x26, 0x24);
        AluRn("ADDC", 0x38, 0x36, 0x34);
        AluRn("SUBB", 0x98, 0x96, 0x94);

        // ANL A, Rn — 0x58 | n
        // ORL A, Rn — 0x48 | n
        // XRL A, Rn — 0x68 | n
        AluRn("ANL", 0x58, 0x56, 0x54);
        AluRn("ORL", 0x48, 0x46, 0x44);
        AluRn("XRL", 0x68, 0x66, 0x64);

        // INC Rn — 0x08 | n
        // DEC Rn — 0x18 | n
        void IncDec(string name, byte opRn)
        {
            CustomHandlers[name] = (ops, labels, addr) =>
            {
                var r = ops[0].Trim();
                if (r.Length == 2 && r[0] == 'r' && char.IsDigit(r[1]))
                    return new byte[] { (byte)(opRn | (r[1] - '0')) };
                if (r.Equals("a", StringComparison.OrdinalIgnoreCase))
                    return new byte[] { (byte)(opRn == 0x08 ? 0x04 : 0x14) };
                if (r.Equals("dptr", StringComparison.OrdinalIgnoreCase))
                    return new byte[] { 0xA3 };
                // INC/DEC direct
                int dir = ResolveOp8051(r, labels, addr);
                return new byte[] { (byte)(opRn == 0x08 ? 0x05 : 0x15), (byte)(dir & 0xFF) };
            };
        }

        IncDec("INC", 0x08);
        IncDec("DEC", 0x18);

        // CJNE A, #data, rel — 0xB4 data rel
        // CJNE Rn, #data, rel — 0xBF | n, data, rel
        // DJNZ Rn, rel — 0xDF | n, rel
        // DJNZ direct, rel — 0xD5 direct, rel
        CustomHandlers["CJNE"] = (ops, labels, addr) =>
        {
            var src1 = ops[0].Trim();
            var src2 = ops[1].Trim();
            var target = ops[2].Trim();
            int rel = (sbyte)(ResolveOp8051(target, labels, addr) - (addr + 3));
            if (src1.Equals("a", StringComparison.OrdinalIgnoreCase) && src2.StartsWith("#"))
            {
                int val = ResolveOp8051(src2, labels, addr);
                return new byte[] { 0xB4, (byte)(val & 0xFF), (byte)(rel & 0xFF) };
            }
            if (src1.Length == 2 && src1[0] == 'r' && char.IsDigit(src1[1]) && src2.StartsWith("#"))
            {
                int val = ResolveOp8051(src2, labels, addr);
                return new byte[] { (byte)(0xB8 | (src1[1] - '0')), (byte)(val & 0xFF), (byte)(rel & 0xFF) };
            }
            return new byte[] { 0 };
        };

        CustomHandlers["DJNZ"] = (ops, labels, addr) =>
        {
            var src = ops[0].Trim();
            var target = ops[1].Trim();
            if (src.Length == 2 && src[0] == 'r' && char.IsDigit(src[1]))
            {
                int rel = (sbyte)(ResolveOp8051(target, labels, addr) - (addr + 2));
                return new byte[] { (byte)(0xD8 | (src[1] - '0')), (byte)(rel & 0xFF) };
            }
            int dir = ResolveOp8051(src, labels, addr);
            int rel2 = (sbyte)(ResolveOp8051(target, labels, addr) - (addr + 3));
            return new byte[] { 0xD5, (byte)(dir & 0xFF), (byte)(rel2 & 0xFF) };
        };

        // JC rel — 0x40 rel
        // JNC rel — 0x50 rel
        // JB bit, rel — 0x20 bit, rel
        // JNB bit, rel — 0x30 bit, rel
        // JBC bit, rel — 0x10 bit, rel
        void JumpRel(string name, byte opcode)
        {
            CustomHandlers[name] = (ops, labels, addr) =>
            {
                int val = ResolveOp8051(ops[0], labels, addr);
                int rel = (sbyte)(val - (addr + 2));
                return new byte[] { opcode, (byte)(rel & 0xFF) };
            };
        }

        JumpRel("JC", 0x40); JumpRel("JNC", 0x50);
        JumpRel("JZ", 0x60); JumpRel("JNZ", 0x70);

        void JumpBit(string name, byte opcode)
        {
            CustomHandlers[name] = (ops, labels, addr) =>
            {
                int bitAddr = ResolveOp8051(ops[0], labels, addr);
                string target = ops[1].Trim();
                int rel = (sbyte)(ResolveOp8051(target, labels, addr) - (addr + 3));
                return new byte[] { opcode, (byte)(bitAddr & 0xFF), (byte)(rel & 0xFF) };
            };
        }

        JumpBit("JB", 0x20); JumpBit("JNB", 0x30); JumpBit("JBC", 0x10);

        // SJMP rel — 0x80 rel
        CustomHandlers["SJMP"] = (ops, labels, addr) =>
        {
            int target = ResolveOp8051(ops[0], labels, addr);
            int rel = (sbyte)(target - (addr + 2));
            return new byte[] { 0x80, (byte)(rel & 0xFF) };
        };

        // LJMP addr — 0x02 addr16
        CustomHandlers["LJMP"] = (ops, labels, addr) =>
        {
            int target = ResolveOp8051(ops[0], labels, addr);
            return new byte[] { 0x02, (byte)((target >> 16) & 0xFF), (byte)((target >> 8) & 0xFF), (byte)(target & 0xFF) };
        };

        // AJMP addr11 — 0x01 | ((addr >> 8) & 0xE0), addr & 0xFF
        CustomHandlers["AJMP"] = (ops, labels, addr) =>
        {
            int target = ResolveOp8051(ops[0], labels, addr);
            byte p1 = (byte)(0x01 | ((target >> 3) & 0xE0));
            return new byte[] { p1, (byte)(target & 0xFF) };
        };

        // ACALL addr11 — 0x11 | ((addr >> 8) & 0xE0), addr & 0xFF
        CustomHandlers["ACALL"] = (ops, labels, addr) =>
        {
            int target = ResolveOp8051(ops[0], labels, addr);
            byte p1 = (byte)(0x11 | ((target >> 3) & 0xE0));
            return new byte[] { p1, (byte)(target & 0xFF) };
        };

        // LCALL addr — 0x12 addr16
        CustomHandlers["LCALL"] = (ops, labels, addr) =>
        {
            int target = ResolveOp8051(ops[0], labels, addr);
            return new byte[] { 0x12, (byte)((target >> 16) & 0xFF), (byte)((target >> 8) & 0xFF), (byte)(target & 0xFF) };
        };

        // MOVC A, @A+DPTR — 0x93
        // MOVC A, @A+PC — 0x83
        // MOVX A, @Ri — 0xE2 | i
        // MOVX @Ri, A — 0xF2 | i
        CustomHandlers["MOVC"] = (ops, labels, addr) =>
        {
            var src = ops.Last().Trim();
            if (src.Contains("dptr", StringComparison.OrdinalIgnoreCase)) return new byte[] { 0x93 };
            return new byte[] { 0x83 };
        };

        CustomHandlers["MOVX"] = (ops, labels, addr) =>
        {
            var dst = ops[0].Trim();
            var src = ops.Length > 1 ? ops[1].Trim() : "";
            if (dst.Equals("a", StringComparison.OrdinalIgnoreCase) && src.StartsWith("@r") && src.Length == 3 && char.IsDigit(src[2]))
                return new byte[] { (byte)(0xE2 | (src[2] - '0')) };
            if (src.Equals("a", StringComparison.OrdinalIgnoreCase) && dst.StartsWith("@r") && dst.Length == 3 && char.IsDigit(dst[2]))
                return new byte[] { (byte)(0xF2 | (dst[2] - '0')) };
            return new byte[] { 0 };
        };

        // DA A — 0xD4
        // SWAP A — 0xC4
        // RLC A — 0x33
        // RRC A — 0x13
        // RL A — 0x23
        // RR A — 0x03

        // CPL bit — 0xB2 bit / CPL A — 0xF4
        CustomHandlers["CPL"] = (ops, labels, addr) =>
        {
            if (ops[0].Trim().Equals("a", StringComparison.OrdinalIgnoreCase)) return new byte[] { 0xF4 };
            int bit = ResolveOp8051(ops[0], labels, addr);
            return new byte[] { 0xB2, (byte)(bit & 0xFF) };
        };

        // CLR bit — 0xC2 bit / CLR A — 0xE4 / CLR C — 0xC3
        CustomHandlers["CLR"] = (ops, labels, addr) =>
        {
            var r = ops[0].Trim().ToLowerInvariant();
            if (r == "a") return new byte[] { 0xE4 };
            if (r == "c") return new byte[] { 0xC3 };
            int bit = ResolveOp8051(r, labels, addr);
            return new byte[] { 0xC2, (byte)(bit & 0xFF) };
        };

        // SETB bit — 0xD2 bit / SETB C — 0xD3
        CustomHandlers["SETB"] = (ops, labels, addr) =>
        {
            if (ops[0].Trim().Equals("c", StringComparison.OrdinalIgnoreCase)) return new byte[] { 0xD3 };
            int bit = ResolveOp8051(ops[0], labels, addr);
            return new byte[] { 0xD2, (byte)(bit & 0xFF) };
        };

        // PUSH direct — 0xC0 direct
        CustomHandlers["PUSH"] = (ops, labels, addr) => { int d = ResolveOp8051(ops[0], labels, addr); return new byte[] { 0xC0, (byte)(d & 0xFF) }; };
        // POP direct — 0xD0 direct
        CustomHandlers["POP"] = (ops, labels, addr) => { int d = ResolveOp8051(ops[0], labels, addr); return new byte[] { 0xD0, (byte)(d & 0xFF) }; };

        // XCH A, Rn — 0xC8 | n
        // XCH A, @Ri — 0xC6 | i
        CustomHandlers["XCH"] = (ops, labels, addr) =>
        {
            var r = ops.Last().Trim();
            if (r.Length == 2 && r[0] == 'r' && char.IsDigit(r[1])) return new byte[] { (byte)(0xC8 | (r[1] - '0')) };
            if (r.StartsWith("@r") && r.Length == 3 && char.IsDigit(r[2])) return new byte[] { (byte)(0xC6 | (r[2] - '0')) };
            return new byte[] { 0 };
        };

        // Single-byte instructions
        Opcodes["NOP"] = new byte[] { 0x00 };
        Opcodes["RET"] = new byte[] { 0x22 };
        Opcodes["RETI"] = new byte[] { 0x32 };
    }
}
