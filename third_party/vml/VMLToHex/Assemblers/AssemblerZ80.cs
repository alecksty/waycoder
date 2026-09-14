using System;
using System.Collections.Generic;
using System.Linq;

namespace VMLToHex.Assemblers;

public class AssemblerZ80 : BaseAssembler
{
    public override string Name => "Z80";
    public override string Description => "Zilog Z80 8-bit CPU";

    private static readonly Dictionary<string, int> RegMap = new(StringComparer.OrdinalIgnoreCase)
    {
        ["b"] = 0, ["c"] = 1, ["d"] = 2, ["e"] = 3, ["h"] = 4, ["l"] = 5, ["a"] = 7
    };

    private static readonly Dictionary<string, int> PairMap = new(StringComparer.OrdinalIgnoreCase)
    {
        ["bc"] = 0, ["de"] = 1, ["hl"] = 2, ["sp"] = 2, ["af"] = 3
    };

    private static readonly Dictionary<string, int> RotRegMap = new(StringComparer.OrdinalIgnoreCase)
    {
        ["b"] = 0, ["c"] = 1, ["d"] = 2, ["e"] = 3, ["h"] = 4, ["l"] = 5, ["(hl)"] = 6, ["a"] = 7
    };

    protected override int RegNum(string reg) => RegMap.TryGetValue(reg, out var r) ? r : 0;

    private static int ResolveOpZ80(string op, Dictionary<string, int>? labels, int addr)
    {
        if (string.IsNullOrEmpty(op)) return 0;
        if (op.StartsWith("(") && op.EndsWith(")")) op = op.Substring(1, op.Length - 2);
        if (op.StartsWith("#") || op.StartsWith("$")) op = op.Substring(1);
        if (op.StartsWith("0x")) op = op.Substring(2);
        if (labels != null && labels.TryGetValue(op, out var la)) return la;
        // Hex
        if (op.IndexOfAny(new[] { 'a', 'b', 'c', 'd', 'e', 'f', 'A', 'B', 'C', 'D', 'E', 'F' }) >= 0)
        {
            if (int.TryParse(op, System.Globalization.NumberStyles.HexNumber, null, out var hx)) return hx;
        }
        if (int.TryParse(op, out var dec)) return dec;
        return 0;
    }

    public AssemblerZ80()
    {
        // LD r1, r2 — 0x40 | (dst << 3) | src
        CustomHandlers["LD"] = (ops, labels, addr) =>
        {
            if (ops.Length < 2) return new byte[] { 0 };
            var dst = ops[0].Trim();
            var src = ops[1].Trim();
            // LD A, (nn) — 0x3A lo hi
            if (dst.Equals("a", StringComparison.OrdinalIgnoreCase) && src.StartsWith("("))
            {
                int val = ResolveOpZ80(src.TrimStart('(').TrimEnd(')'), labels, addr);
                return new byte[] { 0x3A, (byte)(val & 0xFF), (byte)((val >> 8) & 0xFF) };
            }
            // LD (nn), A — 0x32 lo hi
            if (src.Equals("a", StringComparison.OrdinalIgnoreCase) && dst.StartsWith("(") && !dst.Contains("bc", StringComparison.OrdinalIgnoreCase) && !dst.Contains("de", StringComparison.OrdinalIgnoreCase))
            {
                int val = ResolveOpZ80(dst.TrimStart('(').TrimEnd(')'), labels, addr);
                return new byte[] { 0x32, (byte)(val & 0xFF), (byte)((val >> 8) & 0xFF) };
            }
            // LD r, n — 0x06 | (r << 3), n
            if (!src.StartsWith("(") && !dst.StartsWith("("))
            {
                bool isImm = src.StartsWith("#") || src[0] == '$' || char.IsDigit(src[0]) || src[0] == '-' || (labels != null && labels.ContainsKey(src));
                if (isImm && !RegMap.ContainsKey(src.ToLowerInvariant()))
                {
                    int val = ResolveOpZ80(src, labels, addr);
                    if (RegMap.TryGetValue(dst.ToLowerInvariant(), out var r))
                        return new byte[] { (byte)(0x06 | (r << 3)), (byte)(val & 0xFF) };
                    return new byte[] { 0, 0 };
                }
                // LD r1, r2
                if (RegMap.TryGetValue(dst.ToLowerInvariant(), out var rd) && RegMap.TryGetValue(src.ToLowerInvariant(), out var rs))
                    return new byte[] { (byte)(0x40 | (rd << 3) | rs) };
                // LD HL, (nn) — 0x2A
                if (dst.Equals("hl", StringComparison.OrdinalIgnoreCase) && src.StartsWith("("))
                {
                    int val = ResolveOpZ80(src.TrimStart('(').TrimEnd(')'), labels, addr);
                    return new byte[] { 0x2A, (byte)(val & 0xFF), (byte)((val >> 8) & 0xFF) };
                }
                // LD (nn), HL — 0x22
                if (src.Equals("hl", StringComparison.OrdinalIgnoreCase) && dst.StartsWith("("))
                {
                    int val = ResolveOpZ80(dst.TrimStart('(').TrimEnd(')'), labels, addr);
                    return new byte[] { 0x22, (byte)(val & 0xFF), (byte)((val >> 8) & 0xFF) };
                }
                // LD (BC), A — 0x02
                if (dst.Equals("(bc)", StringComparison.OrdinalIgnoreCase) && src.Equals("a", StringComparison.OrdinalIgnoreCase)) return new byte[] { 0x02 };
                if (dst.Equals("(de)", StringComparison.OrdinalIgnoreCase) && src.Equals("a", StringComparison.OrdinalIgnoreCase)) return new byte[] { 0x12 };
                // LD A, (BC) — 0x0A
                if (dst.Equals("a", StringComparison.OrdinalIgnoreCase) && src.Equals("(bc)", StringComparison.OrdinalIgnoreCase)) return new byte[] { 0x0A };
                if (dst.Equals("a", StringComparison.OrdinalIgnoreCase) && src.Equals("(de)", StringComparison.OrdinalIgnoreCase)) return new byte[] { 0x1A };
            }
            return new byte[] { 0 };
        };

        // INC r — 0x04 | (r << 3)
        CustomHandlers["INC"] = (ops, labels, addr) =>
        {
            var r = ops[0].Trim().ToLowerInvariant();
            // INC (HL) — 0x34
            if (r == "(hl)") return new byte[] { 0x34 };
            // INC BC — 0x03, INC DE — 0x13, INC HL — 0x23, INC SP — 0x33
            if (r == "bc") return new byte[] { 0x03 };
            if (r == "de") return new byte[] { 0x13 };
            if (r == "hl") return new byte[] { 0x23 };
            if (r == "sp") return new byte[] { 0x33 };
            if (RegMap.TryGetValue(r, out var reg))
                return new byte[] { (byte)(0x04 | (reg << 3)) };
            return new byte[] { 0 };
        };

        // DEC r — 0x05 | (r << 3)
        CustomHandlers["DEC"] = (ops, labels, addr) =>
        {
            var r = ops[0].Trim().ToLowerInvariant();
            if (r == "(hl)") return new byte[] { 0x35 };
            if (r == "bc") return new byte[] { 0x0B };
            if (r == "de") return new byte[] { 0x1B };
            if (r == "hl") return new byte[] { 0x2B };
            if (r == "sp") return new byte[] { 0x3B };
            if (RegMap.TryGetValue(r, out var reg))
                return new byte[] { (byte)(0x05 | (reg << 3)) };
            return new byte[] { 0 };
        };

        // ADD A, r — 0x80 | r
        // SUB r — 0x90 | r
        // AND r — 0xA0 | r
        // OR r — 0xB0 | r
        // XOR r — 0xA8 | r
        // CP r — 0xB8 | r
        void Alu(string name, byte opBase)
        {
            CustomHandlers[name] = (ops, labels, addr) =>
            {
                var r = ops.Last().Trim().ToLowerInvariant();
                if (r == "(hl)") return new byte[] { (byte)(opBase | 6) };
                if (RegMap.TryGetValue(r, out var reg))
                    return new byte[] { (byte)(opBase | reg) };
                // ADD A, n — 0xC6 n
                if (name.Equals("ADD", StringComparison.OrdinalIgnoreCase) || name.Equals("ADC", StringComparison.OrdinalIgnoreCase))
                {
                    int val = ResolveOpZ80(r, labels, addr);
                    return new byte[] { (byte)(name.Equals("ADC") ? 0xCE : 0xC6), (byte)(val & 0xFF) };
                }
                if (name.Equals("SUB", StringComparison.OrdinalIgnoreCase))
                {
                    int val = ResolveOpZ80(r, labels, addr);
                    return new byte[] { 0xD6, (byte)(val & 0xFF) };
                }
                return new byte[] { (byte)(opBase | 6) };
            };
        }

        Alu("ADD", 0x80); Alu("ADC", 0x88);
        Alu("SUB", 0x90); Alu("SBC", 0x98);
        Alu("AND", 0xA0); Alu("OR", 0xB0);
        Alu("XOR", 0xA8); Alu("CP", 0xB8);

        // PUSH rr — 0xC5 | (pair << 4)
        CustomHandlers["PUSH"] = (ops, labels, addr) =>
        {
            var pair = ops[0].Trim().ToLowerInvariant();
            if (pair == "ix") return new byte[] { 0xDD, 0xE5 };
            if (pair == "iy") return new byte[] { 0xFD, 0xE5 };
            if (PairMap.TryGetValue(pair, out var p))
                return new byte[] { (byte)(0xC5 | (p << 4)) };
            return new byte[] { 0 };
        };
        CustomHandlers["POP"] = (ops, labels, addr) =>
        {
            var pair = ops[0].Trim().ToLowerInvariant();
            if (pair == "ix") return new byte[] { 0xDD, 0xE1 };
            if (pair == "iy") return new byte[] { 0xFD, 0xE1 };
            if (PairMap.TryGetValue(pair, out var p))
                return new byte[] { (byte)(0xC1 | (p << 4)) };
            return new byte[] { 0 };
        };

        // JP addr — 0xC3, JP cc, addr — condition flag
        CustomHandlers["JP"] = (ops, labels, addr) =>
        {
            int val;
            if (ops.Length >= 2)
            {
                // JP cc, addr
                var cc = ops[0].Trim().ToLowerInvariant();
                val = ResolveOpZ80(ops[1], labels, addr);
                var cond = cc switch
                {
                    "nz" => 0, "z" => 1, "nc" => 2, "c" => 3,
                    "po" => 4, "pe" => 5, "p" => 6, "m" => 7,
                    _ => -1
                };
                if (cond >= 0)
                    return new byte[] { (byte)(0xC2 | (cond << 3)), (byte)(val & 0xFF), (byte)((val >> 8) & 0xFF) };
            }
            val = ResolveOpZ80(ops[0], labels, addr);
            return new byte[] { 0xC3, (byte)(val & 0xFF), (byte)((val >> 8) & 0xFF) };
        };
        CustomHandlers["JP_"] = (ops, labels, addr) =>
        {
            // JP (HL)
            if (ops.Length >= 1 && ops[0].Trim().ToLowerInvariant() == "(hl)")
                return new byte[] { 0xE9 };
            return new byte[] { 0 };
        };

        // JR offset — 0x18, JR cc, offset
        CustomHandlers["JR"] = (ops, labels, addr) =>
        {
            int val;
            if (ops.Length >= 2)
            {
                var cc = ops[0].Trim().ToLowerInvariant();
                val = ResolveOpZ80(ops[1], labels, addr);
                int offset = (sbyte)(val - (addr + 2));
                return cc switch
                {
                    "nz" => new byte[] { 0x20, (byte)(offset & 0xFF) },
                    "z" => new byte[] { 0x28, (byte)(offset & 0xFF) },
                    "nc" => new byte[] { 0x30, (byte)(offset & 0xFF) },
                    "c" => new byte[] { 0x38, (byte)(offset & 0xFF) },
                    _ => new byte[] { 0x18, (byte)(offset & 0xFF) }
                };
            }
            val = ResolveOpZ80(ops[0], labels, addr);
            int off = (sbyte)(val - (addr + 2));
            return new byte[] { 0x18, (byte)(off & 0xFF) };
        };

        // CALL addr — 0xCD, CALL cc, addr
        CustomHandlers["CALL"] = (ops, labels, addr) =>
        {
            int val;
            if (ops.Length >= 2)
            {
                var cc = ops[0].Trim().ToLowerInvariant();
                val = ResolveOpZ80(ops[1], labels, addr);
                var cond = cc switch
                {
                    "nz" => 0, "z" => 1, "nc" => 2, "c" => 3,
                    "po" => 4, "pe" => 5, "p" => 6, "m" => 7,
                    _ => -1
                };
                if (cond >= 0)
                    return new byte[] { (byte)(0xC4 | (cond << 3)), (byte)(val & 0xFF), (byte)((val >> 8) & 0xFF) };
            }
            val = ResolveOpZ80(ops[0], labels, addr);
            return new byte[] { 0xCD, (byte)(val & 0xFF), (byte)((val >> 8) & 0xFF) };
        };

        // RET / RET cc
        CustomHandlers["RET"] = (ops, labels, addr) =>
        {
            if (ops.Length == 0 || string.IsNullOrEmpty(ops[0])) return new byte[] { 0xC9 };
            var cc = ops[0].Trim().ToLowerInvariant();
            return cc switch
            {
                "nz" => new byte[] { 0xC0 }, "z" => new byte[] { 0xC8 },
                "nc" => new byte[] { 0xD0 }, "c" => new byte[] { 0xD8 },
                "po" => new byte[] { 0xE0 }, "pe" => new byte[] { 0xE8 },
                "p" => new byte[] { 0xF0 }, "m" => new byte[] { 0xF8 },
                _ => new byte[] { 0xC9 }
            };
        };

        // RST n — 0xC7 | (n * 8)
        CustomHandlers["RST"] = (ops, labels, addr) =>
        {
            if (!int.TryParse(ops[0].TrimStart('$').TrimStart('#'), out var n))
                n = ResolveOpZ80(ops[0], labels, addr);
            return new byte[] { (byte)(0xC7 | (n & 0x38)) };
        };

        // BIT b, r — 0xCB prefix: 0x40 | (b << 3) | r
        // SET b, r — 0xCB prefix: 0xC0 | (b << 3) | r
        // RES b, r — 0xCB prefix: 0x80 | (b << 3) | r
        void BitOp(string name, byte baseOp)
        {
            CustomHandlers[name] = (ops, labels, addr) =>
            {
                int bit = int.TryParse(ops[0], out var b) ? b : 0;
                var r = ops[1].Trim().ToLowerInvariant();
                int regIdx = (r == "(hl)") ? 6 : (RotRegMap.TryGetValue(r, out var ri) ? ri : 0);
                return new byte[] { 0xCB, (byte)(baseOp | (bit << 3) | regIdx) };
            };
        }

        BitOp("BIT", 0x40); BitOp("SET", 0xC0); BitOp("RES", 0x80);

        // RLC / RRC / RL / RR / SLA / SRA / SRL r — 0xCB prefix
        void RotOp(string name, byte baseOp)
        {
            CustomHandlers[name] = (ops, labels, addr) =>
            {
                var r = ops[0].Trim().ToLowerInvariant();
                int regIdx = (r == "(hl)") ? 6 : (RotRegMap.TryGetValue(r, out var ri) ? ri : 0);
                return new byte[] { 0xCB, (byte)(baseOp | regIdx) };
            };
        }

        RotOp("RLC", 0x00); RotOp("RRC", 0x08);
        RotOp("RL", 0x10); RotOp("RR", 0x18);
        RotOp("SLA", 0x20); RotOp("SRA", 0x28);
        RotOp("SRL", 0x38);

        // IN r, (C) — 0xED 0x40 | (r << 3)
        CustomHandlers["IN"] = (ops, labels, addr) =>
        {
            var r = ops[0].Trim().ToLowerInvariant();
            if (RegMap.TryGetValue(r, out var reg))
                return new byte[] { 0xED, (byte)(0x40 | (reg << 3) | 1) }; // IN r,(C)
            return new byte[] { 0xDB, (byte)(ResolveOpZ80(ops.Last().TrimStart('(').TrimEnd(')'), labels, addr) & 0xFF) };
        };

        // OUT (C), r — 0xED 0x41 | (r << 3)
        CustomHandlers["OUT"] = (ops, labels, addr) =>
        {
            var r = ops.Last().Trim().ToLowerInvariant();
            if (RegMap.TryGetValue(r, out var reg))
                return new byte[] { 0xED, (byte)(0x41 | (reg << 3) | 1) }; // OUT (C),r
            return new byte[] { 0xD3, (byte)(ResolveOpZ80(ops[0].TrimStart('(').TrimEnd(')'), labels, addr) & 0xFF) };
        };

        // Single-byte instructions
        Opcodes["NOP"] = new byte[] { 0x00 }; Opcodes["HALT"] = new byte[] { 0x76 };
        Opcodes["EI"] = new byte[] { 0xFB }; Opcodes["DI"] = new byte[] { 0xF3 };
        Opcodes["RLCA"] = new byte[] { 0x07 }; Opcodes["RRCA"] = new byte[] { 0x0F };
        Opcodes["RLA"] = new byte[] { 0x17 }; Opcodes["RRA"] = new byte[] { 0x1F };
        Opcodes["DAA"] = new byte[] { 0x27 }; Opcodes["CPL"] = new byte[] { 0x2F };
        Opcodes["SCF"] = new byte[] { 0x37 }; Opcodes["CCF"] = new byte[] { 0x3F };
        Opcodes["EX_DE_HL"] = new byte[] { 0xEB };
        Opcodes["EX_AF_AF_"] = new byte[] { 0x08 };
        Opcodes["EXX"] = new byte[] { 0xD9 };
        Opcodes["LDI"] = new byte[] { 0xED, 0xA0 };
        Opcodes["LDIR"] = new byte[] { 0xED, 0xB0 };
        Opcodes["LDD"] = new byte[] { 0xED, 0xA8 };
        Opcodes["LDDR"] = new byte[] { 0xED, 0xB8 };
        Opcodes["CPI"] = new byte[] { 0xED, 0xA1 };
        Opcodes["CPIR"] = new byte[] { 0xED, 0xB1 };
        Opcodes["CPD"] = new byte[] { 0xED, 0xA9 };
        Opcodes["CPDR"] = new byte[] { 0xED, 0xB9 };
        Opcodes["NEG"] = new byte[] { 0xED, 0x44 };
        Opcodes["IM_0"] = new byte[] { 0xED, 0x46 };
        Opcodes["IM_1"] = new byte[] { 0xED, 0x56 };
        Opcodes["IM_2"] = new byte[] { 0xED, 0x5E };
    }
}
