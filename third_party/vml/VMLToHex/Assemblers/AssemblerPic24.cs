using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace VMLToHex.Assemblers;

public class AssemblerPic24 : BaseAssembler
{
    public override string Name => "PIC24";
    public override string Description => "Microchip PIC24 16-bit MCU";

    private static readonly Dictionary<string, int> RegMap = new(StringComparer.OrdinalIgnoreCase)
    {
        ["w0"] = 0, ["w1"] = 1, ["w2"] = 2, ["w3"] = 3,
        ["w4"] = 4, ["w5"] = 5, ["w6"] = 6, ["w7"] = 7,
        ["w8"] = 8, ["w9"] = 9, ["w10"] = 10, ["w11"] = 11,
        ["w12"] = 12, ["w13"] = 13, ["w14"] = 14, ["w15"] = 15,
    };

    protected override int RegNum(string reg) => RegMap.TryGetValue(reg, out var r) ? r : 0;

    private static int ResolveOpPIC24(string op, Dictionary<string, int> labels, int addr)
    {
        if (string.IsNullOrEmpty(op)) return 0;
        if (op.StartsWith("#")) op = op.Substring(1);
        if (labels != null && labels.TryGetValue(op, out var la)) return la;
        if (op.StartsWith("0x", StringComparison.OrdinalIgnoreCase))
            return int.TryParse(op.Substring(2), NumberStyles.HexNumber, null, out var h) ? h : 0;
        if (int.TryParse(op, out var d)) return d;
        return 0;
    }

    private static byte[] Encode24(int opcode)
    {
        // 24-bit instruction, little-endian word order (16-bit word, then low byte)
        return new byte[]
        {
            (byte)(opcode & 0xFF),
            (byte)((opcode >> 8) & 0xFF),
            (byte)((opcode >> 16) & 0xFF)
        };
    }

    public AssemblerPic24()
    {
        // MOV Ws, Wd — 0x400000 | (Wd << 8) | (Ws << 0)
        CustomHandlers["MOV"] = (ops, labels, addr) =>
        {
            if (ops.Length < 2) return new byte[] { 0, 0, 0 };
            var src = ops[0].Trim().ToLowerInvariant().TrimStart('[').TrimEnd(']');
            var dst = ops[1].Trim().ToLowerInvariant().TrimStart('[').TrimEnd(']');
            int ws, wd;

            // MOV Ws, Wd
            if (RegMap.TryGetValue(src, out ws) && RegMap.TryGetValue(dst, out wd))
            {
                return Encode24(0x400000 | (wd << 8) | ws);
            }
            // MOV #imm, Wd
            if (src.StartsWith("#"))
            {
                int val = ResolveOpPIC24(src, labels, addr);
                wd = RegMap.TryGetValue(dst, out var r) ? r : 0;
                return Encode24(0x200000 | (wd << 8) | (val & 0xFFFF));
            }
            // MOV [Ws], Wd — indirect, no offset
            if (ops[0].Trim().StartsWith("["))
            {
                var wsStr = ops[0].Trim().TrimStart('[').TrimEnd(']');
                if (RegMap.TryGetValue(wsStr, out ws) && RegMap.TryGetValue(dst, out wd))
                    return Encode24(0x780000 | (wd << 8) | (ws << 0) | (1 << 13)); // INDIRECT mode
            }
            // MOV Ws, [Wd]
            if (ops[1].Trim().StartsWith("["))
            {
                var wdStr = ops[1].Trim().TrimStart('[').TrimEnd(']');
                if (RegMap.TryGetValue(src, out ws) && RegMap.TryGetValue(wdStr, out wd))
                    return Encode24(0x780000 | (wd << 8) | (ws << 0) | (1 << 13) | (1 << 10));
            }
            return new byte[] { 0, 0, 0 };
        };

        // ADD Wb, Ws, Wd — 0x400000 | (Wd << 16) | (Wb << 8) | Ws, with op=0001
        CustomHandlers["ADD"] = (ops, labels, addr) =>
        {
            // ADD Wb, Ws, Wd
            int wb, ws, wd;
            if (ops.Length == 3 &&
                RegMap.TryGetValue(ops[0].ToLowerInvariant(), out wb) &&
                RegMap.TryGetValue(ops[1].ToLowerInvariant(), out ws) &&
                RegMap.TryGetValue(ops[2].ToLowerInvariant(), out wd))
            {
                return Encode24((1 << 24) | (wb << 16) | (wd << 8) | ws);
            }
            // ADD #imm, Wd
            if (ops.Length == 2 && ops[0].StartsWith("#"))
            {
                int val = ResolveOpPIC24(ops[0], labels, addr);
                int wd2 = RegMap.TryGetValue(ops[1].ToLowerInvariant(), out var r) ? r : 0;
                return Encode24(0x400000 | (wd2 << 8) | (val & 0x7FFF) | (1 << 15));
            }
            return new byte[] { 0, 0, 0 };
        };

        // SUB Wb, Ws, Wd — similar to ADD with different op field
        CustomHandlers["SUB"] = (ops, labels, addr) =>
        {
            int wb, ws, wd;
            if (ops.Length == 3 &&
                RegMap.TryGetValue(ops[0].ToLowerInvariant(), out wb) &&
                RegMap.TryGetValue(ops[1].ToLowerInvariant(), out ws) &&
                RegMap.TryGetValue(ops[2].ToLowerInvariant(), out wd))
            {
                return Encode24((2 << 24) | (wb << 16) | (wd << 8) | ws);
            }
            return new byte[] { 0, 0, 0 };
        };

        // MUL Wb, Ws, Wd — 0x200000 | (Wb << 16) | (Wd << 8) | Ws (op=0010)
        CustomHandlers["MUL"] = (ops, labels, addr) =>
        {
            int wb, ws, wd;
            if (ops.Length == 3 &&
                RegMap.TryGetValue(ops[0].ToLowerInvariant(), out wb) &&
                RegMap.TryGetValue(ops[1].ToLowerInvariant(), out ws) &&
                RegMap.TryGetValue(ops[2].ToLowerInvariant(), out wd))
            {
                return Encode24(0x200000 | (wb << 16) | (wd << 8) | ws);
            }
            return new byte[] { 0, 0, 0 };
        };

        // AND Wb, Ws, Wd — op=1010
        CustomHandlers["AND"] = (ops, labels, addr) =>
        {
            int wb, ws, wd;
            if (ops.Length == 3 &&
                RegMap.TryGetValue(ops[0].ToLowerInvariant(), out wb) &&
                RegMap.TryGetValue(ops[1].ToLowerInvariant(), out ws) &&
                RegMap.TryGetValue(ops[2].ToLowerInvariant(), out wd))
            {
                return Encode24(0xA00000 | (wb << 16) | (wd << 8) | ws);
            }
            return new byte[] { 0, 0, 0 };
        };

        // IOR Wb, Ws, Wd — op=1000
        CustomHandlers["IOR"] = (ops, labels, addr) =>
        {
            int wb, ws, wd;
            if (ops.Length == 3 &&
                RegMap.TryGetValue(ops[0].ToLowerInvariant(), out wb) &&
                RegMap.TryGetValue(ops[1].ToLowerInvariant(), out ws) &&
                RegMap.TryGetValue(ops[2].ToLowerInvariant(), out wd))
            {
                return Encode24(0x800000 | (wb << 16) | (wd << 8) | ws);
            }
            return new byte[] { 0, 0, 0 };
        };

        // XOR Wb, Ws, Wd — op=1001
        CustomHandlers["XOR"] = (ops, labels, addr) =>
        {
            int wb, ws, wd;
            if (ops.Length == 3 &&
                RegMap.TryGetValue(ops[0].ToLowerInvariant(), out wb) &&
                RegMap.TryGetValue(ops[1].ToLowerInvariant(), out ws) &&
                RegMap.TryGetValue(ops[2].ToLowerInvariant(), out wd))
            {
                return Encode24(0x900000 | (wb << 16) | (wd << 8) | ws);
            }
            return new byte[] { 0, 0, 0 };
        };

        // CLR Wd — 0x602800 | (Wd << 8)
        CustomHandlers["CLR"] = (ops, labels, addr) =>
        {
            if (RegMap.TryGetValue(ops[0].ToLowerInvariant(), out var wd))
                return Encode24(0x602800 | (wd << 8));
            return new byte[] { 0, 0, 0 };
        };

        // CP0 Ws — 0x642800 | (Ws << 0)
        CustomHandlers["CP0"] = (ops, labels, addr) =>
        {
            if (RegMap.TryGetValue(ops[0].ToLowerInvariant(), out var ws))
                return Encode24(0x642800 | ws);
            return new byte[] { 0, 0, 0 };
        };

        // CP Wb, Ws — compare
        CustomHandlers["CP"] = (ops, labels, addr) =>
        {
            if (ops.Length == 2 &&
                RegMap.TryGetValue(ops[0].ToLowerInvariant(), out var wb) &&
                RegMap.TryGetValue(ops[1].ToLowerInvariant(), out var ws))
            {
                return Encode24(0x440000 | (wb << 16) | ws);
            }
            return new byte[] { 0, 0, 0 };
        };

        // INC Wd, Ws — 0x480000 | (Wd << 8) | Ws
        CustomHandlers["INC"] = (ops, labels, addr) =>
        {
            int wd, ws;
            if (ops.Length == 2 &&
                RegMap.TryGetValue(ops[0].ToLowerInvariant(), out wd) &&
                RegMap.TryGetValue(ops[1].ToLowerInvariant(), out ws))
            {
                return Encode24(0x480000 | (wd << 8) | ws);
            }
            if (ops.Length == 1 && RegMap.TryGetValue(ops[0].ToLowerInvariant(), out var w))
                return Encode24(0x480000 | (w << 8) | w);
            return new byte[] { 0, 0, 0 };
        };

        // DEC Wd, Ws — 0x480000 | (1 << 15) | (Wd << 8) | Ws
        CustomHandlers["DEC"] = (ops, labels, addr) =>
        {
            int wd, ws;
            if (ops.Length == 2 &&
                RegMap.TryGetValue(ops[0].ToLowerInvariant(), out wd) &&
                RegMap.TryGetValue(ops[1].ToLowerInvariant(), out ws))
            {
                return Encode24(0x488000 | (wd << 8) | ws);
            }
            if (ops.Length == 1 && RegMap.TryGetValue(ops[0].ToLowerInvariant(), out var w))
                return Encode24(0x488000 | (w << 8) | w);
            return new byte[] { 0, 0, 0 };
        };

        // BRA target — branch (13-bit signed relative, PC relative)
        CustomHandlers["BRA"] = (ops, labels, addr) =>
        {
            int target = ResolveOpPIC24(ops[0], labels, addr);
            int offset = target - (addr + 4); // PIC24 PC increments by 4 (2 words)
            int disp12 = (offset / 2) & 0xFFF; // word-aligned
            return Encode24(0x300000 | disp12);
        };

        // GOTO target — 0x000000 | target (23-bit address)
        CustomHandlers["GOTO"] = (ops, labels, addr) =>
        {
            int target = ResolveOpPIC24(ops[0], labels, addr);
            return Encode24(target & 0x7FFFFF | (1 << 23));
        };

        // CALL target — 0x000000 | target (23-bit address)
        CustomHandlers["CALL"] = (ops, labels, addr) =>
        {
            int target = ResolveOpPIC24(ops[0], labels, addr);
            return Encode24(target & 0x7FFFFF);
        };

        // RETURN — 0x060000
        Opcodes["RETURN"] = new byte[] { 0x00, 0x00, 0x06 };
        Opcodes["NOP"] = new byte[] { 0x00, 0x00, 0x00 };
    }
}
