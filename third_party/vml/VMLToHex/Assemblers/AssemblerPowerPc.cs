using System;
using System.Collections.Generic;
using System.Linq;

namespace VMLToHex.Assemblers;

public class AssemblerPowerPc : BaseAssembler
{
    public override string Name => "powerpc";
    public override string Description => "PowerPC (32-bit)";

    protected override int RegNum(string reg)
    {
        if (string.IsNullOrEmpty(reg)) return 0;
        var s = reg.Trim().TrimStart('r', 'R');
        return int.TryParse(s, out var n) ? n : 0;
    }

    private static uint EncodeD(int opcd, int rt, int ra, int imm)
    {
        return (uint)((opcd << 26) | (rt << 21) | (ra << 16) | (imm & 0xFFFF));
    }

    private static uint EncodeX(int rt, int ra, int rb, int xo)
    {
        return (31u << 26) | (uint)((rt << 21) | (ra << 16) | (rb << 11) | (xo << 1));
    }

    private static byte[] Word(uint w) => new byte[] { (byte)(w >> 24), (byte)(w >> 16), (byte)(w >> 8), (byte)w };

    public AssemblerPowerPc()
    {
        // X-form: opcd=31, op $ra, $rs, $rb (rd=ra for some)
        void XForm(string name, int xo) => CustomHandlers[name] = (ops, _, _) =>
        {
            int rt = RegNum(ops[0]), ra = RegNum(ops.Length > 1 ? ops[1] : ""), rb = ops.Length > 2 ? RegNum(ops[2]) : 0;
            return Word(EncodeX(rt, ra, rb, xo));
        };

        XForm("ADD", 266); XForm("SUBF", 40);
        XForm("AND", 28); XForm("OR", 444); XForm("XOR", 316); XForm("NOR", 124); XForm("NAND", 476);
        XForm("SLW", 24); XForm("SRW", 536); XForm("SRAW", 792);
        XForm("MULHW", 75); XForm("MULHWU", 11);
        XForm("DIVW", 491); XForm("DIVWU", 459);
        XForm("CROR", 449);

        // D-form: op $rt, $ra, $imm
        void DForm(string name, int opcd) => CustomHandlers[name] = (ops, labels, addr) =>
        {
            int rt = RegNum(ops[0]), ra = RegNum(ops.Length > 1 ? ops[1] : "");
            int imm = ops.Length > 2 ? ResolveValue(ops[2], labels, addr) : 0;
            return Word(EncodeD(opcd, rt, ra, imm));
        };

        DForm("ADDI", 14); DForm("ADDIS", 15);
        DForm("ORI", 24); DForm("ORIS", 25);
        DForm("ANDI", 28); DForm("ANDIS", 29);
        DForm("XORI", 26); DForm("XORIS", 27);

        // Load/store D-form: op $rt, $imm($ra)
        void LoadStore(string name, int opcd) => CustomHandlers[name] = (ops, labels, addr) =>
        {
            int rt = RegNum(ops[0]);
            var mem = ops.Length > 1 ? ops[1] : "";
            int imm = 0, ra = 0;
            int paren = mem.IndexOf('(');
            if (paren >= 0)
            {
                var offStr = mem.Substring(0, paren).Trim();
                if (offStr.Length > 0) imm = ResolveValue(offStr, labels, addr);
                ra = RegNum(mem.Substring(paren + 1).TrimEnd(')'));
            }
            else
            {
                ra = ops.Length > 1 ? RegNum(ops[1]) : 0;
                imm = ops.Length > 2 ? ResolveValue(ops[2], labels, addr) : 0;
            }
            return Word(EncodeD(opcd, rt, ra, imm));
        };

        LoadStore("LWZ", 32); LoadStore("STW", 36);
        LoadStore("LBZ", 34); LoadStore("STB", 38);
        LoadStore("LHZ", 40); LoadStore("STH", 44);
        LoadStore("LHA", 42);

        // CMP: opcd=31, crfD=0, L=0, ra, rb
        CustomHandlers["CMP"] = (ops, _, _) =>
        {
            int ra = RegNum(ops[0]), rb = RegNum(ops[1]);
            return Word((31u << 26) | (uint)((ra << 16) | (rb << 11) | (0x20 << 1)));
        };

        CustomHandlers["CMPI"] = (ops, labels, addr) =>
        {
            int ra = RegNum(ops[0]);
            int imm = ResolveValue(ops[1], labels, addr);
            return Word(EncodeD(11, 0, ra, imm));
        };

        // Branch: B, BL
        void Branch(string name, int lk) => CustomHandlers[name] = (ops, labels, addr) =>
        {
            int target = ResolveValue(ops[0], labels, addr);
            int offset = target - addr;
            uint w = (uint)((18 << 26) | (lk << 24) | ((offset >> 2) & 0x3FFFFFF));
            return Word(w);
        };

        Branch("B", 0); Branch("BL", 1);

        // Conditional branch: BEQ, BNE, etc.
        void CondBranch(string name, int bo, int bi) => CustomHandlers[name] = (ops, labels, addr) =>
        {
            int target = ResolveValue(ops[0], labels, addr);
            int offset = target - addr;
            uint w = (uint)((16 << 26) | (bo << 21) | (bi << 16) | ((offset >> 2) & 0xFFFC));
            return Word(w);
        };

        CondBranch("BEQ", 12, 2); CondBranch("BNE", 4, 2);
        CondBranch("BGT", 12, 1); CondBranch("BLT", 12, 0);
        CondBranch("BGE", 4, 0); CondBranch("BLE", 4, 1);
        CondBranch("BGEU", 4, 3); CondBranch("BLTU", 12, 3);

        // MFLR, MTLR
        CustomHandlers["MFLR"] = (ops, _, _) =>
        {
            int rt = RegNum(ops[0]);
            return Word((31u << 26) | (uint)((rt << 21) | (339 << 1)));
        };
        CustomHandlers["MTLR"] = (ops, _, _) =>
        {
            int rt = RegNum(ops[0]);
            return Word((31u << 26) | (uint)((rt << 21) | (467 << 1)));
        };

        // MCRF
        CustomHandlers["MCRF"] = (_, _, _) => Word((19u << 26) | 0);

        // SUBI = ADDI with negative imm (handled via CustomHandler that negates)
        CustomHandlers["SUBI"] = (ops, labels, addr) =>
        {
            int rt = RegNum(ops[0]), ra = RegNum(ops[1]);
            int imm = -ResolveValue(ops[2], labels, addr);
            return Word(EncodeD(14, rt, ra, imm));
        };

        // MUL and DIV aliases
        CustomHandlers["MUL"] = CustomHandlers["MULHW"];
        CustomHandlers["DIV"] = CustomHandlers["DIVW"];
        CustomHandlers["SUB"] = CustomHandlers["SUBF"];

        // SRAWI shift by immediate
        CustomHandlers["SRAWI"] = (ops, _, _) =>
        {
            int rt = RegNum(ops[0]), ra = RegNum(ops[1]);
            int sh = ops.Length > 2 ? RegNum(ops[2]) : 0;
            uint w = (uint)((31 << 26) | (rt << 21) | (ra << 16) | ((sh & 0x1F) << 11) | (0x1F6 << 1));
            return Word(w);
        };

        // No-register instructions
        Opcodes["NOP"] = new byte[] { 0x60, 0x00, 0x00, 0x00 };
        Opcodes["BCTR"] = new byte[] { 0x4E, 0x80, 0x00, 0x20 };
        Opcodes["BCTRL"] = new byte[] { 0x4E, 0x80, 0x00, 0x21 };
        Opcodes["SC"] = new byte[] { 0x44, 0x00, 0x00, 0x02 };
    }
}
