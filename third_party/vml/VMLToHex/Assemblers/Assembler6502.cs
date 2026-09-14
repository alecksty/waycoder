using System;
using System.Collections.Generic;
using System.Linq;

namespace VMLToHex.Assemblers
{
    public class Assembler6502 : BaseAssembler
    {
        public override string Name => "6502";
        public override string Description => "MOS Technology 6502";

        private static int RegNum6502(string reg)
        {
            if (string.IsNullOrEmpty(reg)) return 0;
            return reg.ToLowerInvariant() switch { "a" => 0, "x" => 1, "y" => 2, "sp" => 3, _ => 0 };
        }

        protected override int RegNum(string reg) => RegNum6502(reg);

        private static byte[] Byte1(byte op) => new byte[] { op };
        private static byte[] Byte2(byte op, int lo) => new byte[] { op, (byte)(lo & 0xFF) };
        private static byte[] Byte3(byte op, int addr) => new byte[] { op, (byte)(addr & 0xFF), (byte)((addr >> 8) & 0xFF) };

        private static byte[] LoadStore(byte opImm, byte opZp, byte opAbs, byte opZpX, byte opAbsX, byte opAbsY, byte opIndX, byte opIndY, string[] ops, int mode)
        {
            int val = mode >= 0 ? ops[mode] is string s && int.TryParse(s.TrimStart('#'), out var n) ? n : 0 : 0;
            switch (ops[0][0])
            {
                case '#': return Byte2(opImm, val);
                default:
                    if (ops[0].Contains(",x")) return Byte3(opAbsX, val);
                    if (ops[0].Contains(",y")) return Byte3(opAbsY, val);
                    if (ops[0].StartsWith("(") && ops[0].Contains(",x)")) return Byte2(opIndX, val);
                    if (ops[0].StartsWith("(") && ops[0].EndsWith("),y")) return Byte2(opIndY, val);
                    if (ops[0].StartsWith("(")) return Byte3(opAbs, val);
                    if (val < 256) return Byte2(opZp, val);
                    return Byte3(opAbs, val);
            }
        }

        private static int ResolveOp(string op, Dictionary<string, int>? labels, int addr)
        {
            op = op.Trim();
            if (op.StartsWith("#")) op = op.Substring(1);
            // Check for label
            if (!string.IsNullOrEmpty(op) && !char.IsDigit(op[0]) && op[0] != '$' && op[0] != '0')
            {
                // Strip addressing mode suffix
                var baseLabel = op.Split(',', '(', ')')[0].Trim();
                if (labels != null && labels.TryGetValue(baseLabel, out var la)) return la;
            }
            return int.TryParse(op.Replace("$", ""), System.Globalization.NumberStyles.HexNumber, null, out var v) ? v : 0;
        }

        public Assembler6502()
        {
            void LdSt(string name, byte imm, byte zp, byte abs, byte zpX, byte absX, byte absY, byte indX, byte indY)
            {
                CustomHandlers[name] = (ops, labels, addr) =>
                {
                    var op = ops[0];
                    int paren = op.IndexOf('(');
                    int commaX = op.IndexOf(",x", StringComparison.OrdinalIgnoreCase);
                    int commaY = op.IndexOf(",y", StringComparison.OrdinalIgnoreCase);
                    bool isImm = op.StartsWith("#");
                    int val = 0;
                    var cleanOp = isImm ? op.Substring(1) : op;
                    // Handle label
                    if (!isImm && labels != null)
                    {
                        var label = cleanOp.Split(',', '(', ')')[0].Trim();
                        if (labels.TryGetValue(label, out var la)) val = la;
                    }
                    if (val == 0) val = ResolveOp(op, labels, addr);

                    if (isImm) return Byte2(imm, val & 0xFF);
                    if (commaX >= 0) { var abs = val > 255; return abs ? Byte3(absX, val) : Byte2(zpX, val & 0xFF); }
                    if (commaY >= 0) { var abs = val > 255; return abs ? Byte3(absY, val) : Byte2(0, val & 0xFF); }
                    if (paren >= 0 && op.EndsWith(",x)")) return Byte2(indX, val & 0xFF);
                    if (paren >= 0 && op.EndsWith("),y")) return Byte2(indY, val & 0xFF);
                    if (val > 255) return Byte3(abs, val);
                    return Byte2(zp, val & 0xFF);
                };
            }

            LdSt("LDA", 0xA9, 0xA5, 0xAD, 0xB5, 0xBD, 0xB9, 0xA1, 0xB1);
            LdSt("LDX", 0xA2, 0xA6, 0xAE, 0x00, 0x00, 0xBE, 0x00, 0x00);
            LdSt("LDY", 0xA0, 0xA4, 0xAC, 0xB4, 0xBC, 0x00, 0x00, 0x00);
            LdSt("STA", 0x00, 0x85, 0x8D, 0x95, 0x9D, 0x99, 0x81, 0x91);
            LdSt("STX", 0x00, 0x86, 0x8E, 0x00, 0x00, 0x00, 0x00, 0x00);
            LdSt("STY", 0x00, 0x84, 0x8C, 0x94, 0x00, 0x00, 0x00, 0x00);

            void Alu(string name, byte imm, byte zp, byte abs, byte zpX, byte absX, byte absY, byte indX, byte indY)
            {
                CustomHandlers[name] = (ops, labels, addr) =>
                {
                    var op = ops[0];
                    bool isImm = op.StartsWith("#");
                    int val = ResolveOp(op, labels, addr);
                    if (isImm) return Byte2(imm, val & 0xFF);
                    int commaX = op.IndexOf(",x", StringComparison.OrdinalIgnoreCase);
                    int commaY = op.IndexOf(",y", StringComparison.OrdinalIgnoreCase);
                    int paren = op.IndexOf('(');
                    if (commaX >= 0) return val > 255 ? Byte3(absX, val) : Byte2(zpX, val & 0xFF);
                    if (commaY >= 0) return val > 255 ? Byte3(absY, val) : Byte2(0, val & 0xFF);
                    if (paren >= 0 && op.EndsWith(",x)")) return Byte2(indX, val & 0xFF);
                    if (paren >= 0 && op.EndsWith("),y")) return Byte2(indY, val & 0xFF);
                    if (val > 255) return Byte3(abs, val);
                    return Byte2(zp, val & 0xFF);
                };
            }

            Alu("ADC", 0x69, 0x65, 0x6D, 0x75, 0x7D, 0x79, 0x61, 0x71);
            Alu("SBC", 0xE9, 0xE5, 0xED, 0xF5, 0xFD, 0xF9, 0xE1, 0xF1);
            Alu("AND", 0x29, 0x25, 0x2D, 0x35, 0x3D, 0x39, 0x21, 0x31);
            Alu("ORA", 0x09, 0x05, 0x0D, 0x15, 0x1D, 0x19, 0x01, 0x11);
            Alu("EOR", 0x49, 0x45, 0x4D, 0x55, 0x5D, 0x59, 0x41, 0x51);
            Alu("CMP", 0xC9, 0xC5, 0xCD, 0xD5, 0xDD, 0xD9, 0xC1, 0xD1);

            CustomHandlers["CPX"] = (ops, labels, addr) => { var op = ops[0]; int val = ResolveOp(op, labels, addr); return op.StartsWith("#") ? Byte2(0xE0, val & 0xFF) : val > 255 ? Byte3(0xEC, val) : Byte2(0xE4, val & 0xFF); };
            CustomHandlers["CPY"] = (ops, labels, addr) => { var op = ops[0]; int val = ResolveOp(op, labels, addr); return op.StartsWith("#") ? Byte2(0xC0, val & 0xFF) : val > 255 ? Byte3(0xCC, val) : Byte2(0xC4, val & 0xFF); };

            void IncDec(string name, byte zp, byte abs, byte zpX, byte absX)
            {
                CustomHandlers[name] = (ops, labels, addr) =>
                {
                    int val = ResolveOp(ops[0], labels, addr);
                    int commaX = ops[0].IndexOf(",x", StringComparison.OrdinalIgnoreCase);
                    if (commaX >= 0) return val > 255 ? Byte3(absX, val) : Byte2(zpX, val & 0xFF);
                    if (val > 255) return Byte3(abs, val);
                    return Byte2(zp, val & 0xFF);
                };
            }

            IncDec("INC", 0xE6, 0xEE, 0xF6, 0xFE);
            IncDec("DEC", 0xC6, 0xCE, 0xD6, 0xDE);

            void Shift(string name, byte acc, byte zp, byte abs, byte zpX, byte absX)
            {
                CustomHandlers[name] = (ops, labels, addr) =>
                {
                    var op = ops[0];
                    bool isAcc = op.Equals("a", StringComparison.OrdinalIgnoreCase);
                    if (isAcc) return Byte1(acc);
                    int val = ResolveOp(op, labels, addr);
                    int commaX = op.IndexOf(",x", StringComparison.OrdinalIgnoreCase);
                    if (commaX >= 0) return val > 255 ? Byte3(absX, val) : Byte2(zpX, val & 0xFF);
                    if (val > 255) return Byte3(abs, val);
                    return Byte2(zp, val & 0xFF);
                };
            }

            Shift("ASL", 0x0A, 0x06, 0x0E, 0x16, 0x1E);
            Shift("LSR", 0x4A, 0x46, 0x4E, 0x56, 0x5E);
            Shift("ROL", 0x2A, 0x26, 0x2E, 0x36, 0x3E);
            Shift("ROR", 0x6A, 0x66, 0x6E, 0x76, 0x7E);

            CustomHandlers["BIT"] = (ops, labels, addr) => { int val = ResolveOp(ops[0], labels, addr); return val > 255 ? Byte3(0x2C, val) : Byte2(0x24, val & 0xFF); };
            CustomHandlers["STZ"] = (ops, labels, addr) => { int val = ResolveOp(ops[0], labels, addr); return val > 255 ? Byte3(0x9C, val) : Byte2(0x64, val & 0xFF); };
            CustomHandlers["TRB"] = (ops, labels, addr) => { int val = ResolveOp(ops[0], labels, addr); return val > 255 ? Byte3(0x1C, val) : Byte2(0x14, val & 0xFF); };
            CustomHandlers["TSB"] = (ops, labels, addr) => { int val = ResolveOp(ops[0], labels, addr); return val > 255 ? Byte3(0x0C, val) : Byte2(0x04, val & 0xFF); };

            // JMP: absolute or indirect
            CustomHandlers["JMP"] = (ops, labels, addr) =>
            {
                var op = ops[0];
                if (op.StartsWith("("))
                {
                    int val = ResolveOp(op.Trim('(', ')'), labels, addr);
                    return Byte3(0x6C, val);
                }
                int target = ResolveOp(op, labels, addr);
                return Byte3(0x4C, target);
            };

            CustomHandlers["JSR"] = (ops, labels, addr) => Byte3(0x20, ResolveOp(ops[0], labels, addr));

            // Branches
            void Branch(string name, byte opcode) => CustomHandlers[name] = (ops, labels, addr) =>
            {
                int target = ResolveOp(ops[0], labels, addr);
                int offset = (sbyte)(target - (addr + 2));
                return Byte2(opcode, (byte)(offset & 0xFF));
            };

            Branch("BEQ", 0xF0); Branch("BNE", 0xD0);
            Branch("BCC", 0x90); Branch("BCS", 0xB0);
            Branch("BMI", 0x30); Branch("BPL", 0x10);
            Branch("BVC", 0x50); Branch("BVS", 0x70);
            Branch("BRA", 0x80);

            // No-register instructions
            Opcodes["TAX"] = new byte[] { 0xAA }; Opcodes["TAY"] = new byte[] { 0xA8 };
            Opcodes["TXA"] = new byte[] { 0x8A }; Opcodes["TYA"] = new byte[] { 0x98 };
            Opcodes["TSX"] = new byte[] { 0xBA }; Opcodes["TXS"] = new byte[] { 0x9A };
            Opcodes["PHA"] = new byte[] { 0x48 }; Opcodes["PLA"] = new byte[] { 0x68 };
            Opcodes["PHP"] = new byte[] { 0x08 }; Opcodes["PLP"] = new byte[] { 0x28 };
            Opcodes["INX"] = new byte[] { 0xE8 }; Opcodes["INY"] = new byte[] { 0xC8 };
            Opcodes["DEX"] = new byte[] { 0xCA }; Opcodes["DEY"] = new byte[] { 0x88 };
            Opcodes["RTS"] = new byte[] { 0x60 }; Opcodes["RTI"] = new byte[] { 0x40 };
            Opcodes["BRK"] = new byte[] { 0x00 }; Opcodes["NOP"] = new byte[] { 0xEA };
            Opcodes["CLD"] = new byte[] { 0xD8 }; Opcodes["SED"] = new byte[] { 0xF8 };
            Opcodes["CLI"] = new byte[] { 0x58 }; Opcodes["SEI"] = new byte[] { 0x78 };
            Opcodes["CLC"] = new byte[] { 0x18 }; Opcodes["SEC"] = new byte[] { 0x38 };
            Opcodes["CLV"] = new byte[] { 0xB8 };
            Opcodes["PHX"] = new byte[] { 0xDA }; Opcodes["PHY"] = new byte[] { 0x5A };
            Opcodes["PLX"] = new byte[] { 0xFA }; Opcodes["PLY"] = new byte[] { 0x7A };
        }
    }
}
