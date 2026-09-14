using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace VMLToHex.Assemblers;

public class AssemblerPic : BaseAssembler
{
    public override string Name => "PIC";
    public override string Description => "Microchip PIC16 8-bit MCU (14-bit instruction)";

    protected override int RegNum(string reg) => 0;

    private static int ResolveOpPIC(string op, Dictionary<string, int> labels, int addr)
    {
        if (string.IsNullOrEmpty(op)) return 0;
        if (op.StartsWith("#") || op.StartsWith(".0x")) op = op.StartsWith("#") ? op.Substring(1) : op.Substring(1);
        if (op.StartsWith("0x", StringComparison.OrdinalIgnoreCase))
            return int.TryParse(op.Substring(2), NumberStyles.HexNumber, null, out var h) ? h : 0;
        if (op.EndsWith("h", StringComparison.OrdinalIgnoreCase) && op.Length > 1)
            return int.TryParse(op.TrimEnd('h', 'H'), NumberStyles.HexNumber, null, out var he) ? he : 0;
        if (op.StartsWith("."))
            return int.TryParse(op.Substring(1), out var d) ? d : 0;
        if (labels != null && labels.TryGetValue(op, out var la)) return la;
        if (int.TryParse(op, out var dec)) return dec;
        return 0;
    }

    private static byte[] EncodePIC(int op14)
    {
        // 14-bit instruction stored as 2 bytes little-endian
        return new byte[]
        {
            (byte)(op14 & 0xFF),
            (byte)((op14 >> 8) & 0xFF)
        };
    }

    public AssemblerPic()
    {
        // Byte-oriented operations — F = file register address (7-bit)
        // MOVF f, d — 0x0180 | (d << 7) | f
        // ADDWF f, d — 0x0700 | (d << 7) | f
        // SUBWF f, d — 0x0800 | (d << 7) | f
        // ANDWF f, d — 0x0500 | (d << 7) | f
        // IORWF f, d — 0x0400 | (d << 7) | f
        // XORWF f, d — 0x0600 | (d << 7) | f
        // COMF f, d — 0x0900 | (d << 7) | f
        // INCF f, d — 0x0A00 | (d << 7) | f
        // DECF f, d — 0x0B00 | (d << 7) | f
        // RLF f, d — 0x0D00 | (d << 7) | f
        // RRF f, d — 0x0C00 | (d << 7) | f
        // SWAPF f, d — 0x0E00 | (d << 7) | f
        void ByteOp(string name, int opBase)
        {
            CustomHandlers[name] = (ops, labels, addr) =>
            {
                string fOp;
                int d;
                if (ops.Length >= 2 && RegNum(ops[1].Trim()) == 0)
                {
                    // op f, d  — d=0 (W) or d=1 (F)
                    fOp = ops[0].Trim();
                    d = RegNum(ops[1].Trim());
                }
                else
                {
                    fOp = ops[0].Trim();
                    d = 1; // default to F
                }
                int f = ResolveOpPIC(fOp, labels, addr);
                return EncodePIC((opBase & 0xFC00) | ((d & 1) << 7) | (f & 0x7F));
            };
        }

        ByteOp("ADDWF", 0x0700); ByteOp("SUBWF", 0x0800);
        ByteOp("ANDWF", 0x0500); ByteOp("IORWF", 0x0400);
        ByteOp("XORWF", 0x0600); ByteOp("MOVF", 0x0180);
        ByteOp("COMF", 0x0900); ByteOp("INCF", 0x0A00);
        ByteOp("DECF", 0x0B00); ByteOp("RLF", 0x0D00);
        ByteOp("RRF", 0x0C00); ByteOp("SWAPF", 0x0E00);

        // CLRF f — 0x0180 | (f & 0x7F)  (same as MOVF f, 0 but CLRW if f=0)
        // CLRW — 0x0100
        CustomHandlers["CLRF"] = (ops, labels, addr) =>
        {
            int f = ResolveOpPIC(ops[0], labels, addr);
            return EncodePIC(0x0180 | (f & 0x7F));
        };

        Opcodes["CLRW"] = EncodePIC(0x0100);

        // MOVWF f — 0x0085 | (f & 0x7F)
        CustomHandlers["MOVWF"] = (ops, labels, addr) =>
        {
            int f = ResolveOpPIC(ops[0], labels, addr);
            return EncodePIC(0x0085 | (f & 0x7F));
        };

        // Bit-oriented: BSF f, b — 0x0C00 | ((b & 7) << 7) | (f & 0x7F)
        //               BCF f, b — 0x1000 | ((b & 7) << 7) | (f & 0x7F)
        //               BTFSS f, b — 0x1C00 | ((b & 7) << 7) | (f & 0x7F)
        //               BTFSC f, b — 0x1800 | ((b & 7) << 7) | (f & 0x7F)
        void BitOp(string name, int opBase)
        {
            CustomHandlers[name] = (ops, labels, addr) =>
            {
                int f = ResolveOpPIC(ops[0], labels, addr);
                int b = int.TryParse(ops.Length > 1 ? ops[1].Trim() : "0", out var bi) ? bi : 0;
                return EncodePIC(opBase | ((b & 7) << 7) | (f & 0x7F));
            };
        }

        BitOp("BSF", 0x0C00); BitOp("BCF", 0x1000);
        BitOp("BTFSS", 0x1C00); BitOp("BTFSC", 0x1800);

        // Literal/control operations
        // GOTO target — 0x2800 | (addr & 0x7FF)
        CustomHandlers["GOTO"] = (ops, labels, addr) =>
        {
            int target = ResolveOpPIC(ops[0], labels, addr);
            return EncodePIC(0x2800 | (target & 0x7FF));
        };

        // CALL target — 0x2000 | (addr & 0x7FF)
        CustomHandlers["CALL"] = (ops, labels, addr) =>
        {
            int target = ResolveOpPIC(ops[0], labels, addr);
            return EncodePIC(0x2000 | (target & 0x7FF));
        };

        // MOVLW k — 0x3000 | (k & 0xFF)
        CustomHandlers["MOVLW"] = (ops, labels, addr) => { int k = ResolveOpPIC(ops[0], labels, addr); return EncodePIC(0x3000 | (k & 0xFF)); };
        // ADDLW k — 0x3E00 | (k & 0xFF)
        CustomHandlers["ADDLW"] = (ops, labels, addr) => { int k = ResolveOpPIC(ops[0], labels, addr); return EncodePIC(0x3E00 | (k & 0xFF)); };
        // SUBLW k — 0x3C00 | (k & 0xFF)
        CustomHandlers["SUBLW"] = (ops, labels, addr) => { int k = ResolveOpPIC(ops[0], labels, addr); return EncodePIC(0x3C00 | (k & 0xFF)); };
        // ANDLW k — 0x3900 | (k & 0xFF)
        CustomHandlers["ANDLW"] = (ops, labels, addr) => { int k = ResolveOpPIC(ops[0], labels, addr); return EncodePIC(0x3900 | (k & 0xFF)); };
        // IORLW k — 0x3800 | (k & 0xFF)
        CustomHandlers["IORLW"] = (ops, labels, addr) => { int k = ResolveOpPIC(ops[0], labels, addr); return EncodePIC(0x3800 | (k & 0xFF)); };
        // XORLW k — 0x3A00 | (k & 0xFF)
        CustomHandlers["XORLW"] = (ops, labels, addr) => { int k = ResolveOpPIC(ops[0], labels, addr); return EncodePIC(0x3A00 | (k & 0xFF)); };
        // RETLW k — 0x3400 | (k & 0xFF)
        CustomHandlers["RETLW"] = (ops, labels, addr) => { int k = ResolveOpPIC(ops[0], labels, addr); return EncodePIC(0x3400 | (k & 0xFF)); };

        // BTFSS/BSF/BCF already done as bit ops above; skip-if variants

        // Single-word instructions
        Opcodes["NOP"] = EncodePIC(0x0000);
        Opcodes["RETURN"] = EncodePIC(0x0008);
        Opcodes["RETFIE"] = EncodePIC(0x0009);
        Opcodes["SLEEP"] = EncodePIC(0x0063);
        Opcodes["CLRWDT"] = EncodePIC(0x0064);
    }
}
