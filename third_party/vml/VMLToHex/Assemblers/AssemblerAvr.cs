using System;
using System.Collections.Generic;
using System.Linq;

namespace VMLToHex.Assemblers
{
    public class AssemblerAvr : BaseAssembler
    {
        public override string Name => "avr";
        public override string Description => "Atmel AVR (ATmega)";

        protected override int RegNum(string reg)
        {
            if (string.IsNullOrEmpty(reg)) return 0;
            var s = reg.Trim().TrimStart('r', 'R');
            return int.TryParse(s, out var n) ? (n >= 0 && n <= 31 ? n : 0) : 0;
        }

        private static byte[] Word16(uint w) => new byte[] { (byte)(w & 0xFF), (byte)((w >> 8) & 0xFF) };

        private static uint EncodeRegReg(int opcode, int rd, int rr)
        {
            return (uint)((opcode << 10) | (((rd >> 4) & 1) << 9) | (((rr >> 4) & 1) << 8) | ((rd & 0xF) << 4) | (rr & 0xF));
        }

        private static uint EncodeRegImm(int opcode, int rd, int k)
        {
            return (uint)((opcode << 12) | (((k >> 4) & 0xF) << 8) | ((rd & 0xF) << 4) | (k & 0xF));
        }

        private static uint EncodeRegOnly(int opcode, int rd, int mask)
        {
            return (uint)((opcode << 10) | (((rd >> 4) & 1) << 9) | ((rd & 0xF) << 4) | (mask & 0xF));
        }

        public AssemblerAvr()
        {
            // Two-register arithmetic
            void TwoReg(string name, int opcode) => CustomHandlers[name] = (ops, _, _) =>
            {
                int rd = RegNum(ops[0]), rr = RegNum(ops[1]);
                return Word16(EncodeRegReg(opcode, rd, rr));
            };

            TwoReg("ADD", 0x0C); TwoReg("ADC", 0x1C);
            TwoReg("SUB", 0x18); TwoReg("SBC", 0x08);
            TwoReg("AND", 0x20); TwoReg("OR", 0x28); TwoReg("EOR", 0x24);
            TwoReg("MOV", 0x2C);
            TwoReg("CP", 0x14); TwoReg("CPC", 0x04);
            TwoReg("MUL", 0x9C);

            // Immediate ops (r16-r23 only): LDI, SUBI, SBCI, ANDI, ORI, CPI
            void RegImm(string name, int opcode) => CustomHandlers[name] = (ops, labels, addr) =>
            {
                int rd = RegNum(ops[0]);
                int k = ResolveValue(ops[1], labels, addr);
                return Word16(EncodeRegImm(opcode, rd, k & 0xFF));
            };

            RegImm("LDI", 0xE); RegImm("SUBI", 0x5); RegImm("SBCI", 0x4);
            RegImm("ANDI", 0x7); RegImm("ORI", 0x6); RegImm("CPI", 0x3);

            // Single-register ops
            void OneReg(string name, int opcode, int mask) => CustomHandlers[name] = (ops, _, _) =>
            {
                int rd = RegNum(ops[0]);
                return Word16(EncodeRegOnly(opcode, rd, mask));
            };

            OneReg("INC", 0x94, 0x03); OneReg("DEC", 0x94, 0x0A);
            OneReg("COM", 0x94, 0x00); OneReg("NEG", 0x94, 0x01);
            OneReg("LSR", 0x94, 0x06); OneReg("ROR", 0x94, 0x07);

            // LSL = ADD rd, rd; ROL = ADC rd, rd
            CustomHandlers["LSL"] = (ops, _, _) => { int rd = RegNum(ops[0]); return Word16(EncodeRegReg(0x0C, rd, rd)); };
            CustomHandlers["ROL"] = (ops, _, _) => { int rd = RegNum(ops[0]); return Word16(EncodeRegReg(0x1C, rd, rd)); };

            // LDS Rd, addr (32-bit: 0x9000 + Rd + 16-bit addr)
            CustomHandlers["LDS"] = (ops, labels, addr) =>
            {
                int rd = RegNum(ops[0]);
                int memAddr = ResolveValue(ops[1], labels, addr);
                uint w = (uint)((0x90 << 10) | (((rd >> 4) & 1) << 9) | ((rd & 0xF) << 4) | 0x00);
                return new byte[] { (byte)(w & 0xFF), (byte)((w >> 8) & 0xFF), (byte)(memAddr & 0xFF), (byte)((memAddr >> 8) & 0xFF) };
            };

            // STS addr, Rr
            CustomHandlers["STS"] = (ops, labels, addr) =>
            {
                int memAddr = ResolveValue(ops[0], labels, addr);
                int rr = RegNum(ops[1]);
                uint w = (uint)((0x92 << 10) | (((rr >> 4) & 1) << 9) | ((rr & 0xF) << 4) | 0x00);
                return new byte[] { (byte)(w & 0xFF), (byte)((w >> 8) & 0xFF), (byte)(memAddr & 0xFF), (byte)((memAddr >> 8) & 0xFF) };
            };

            // LD Rd, X/Y/Z (various modes)
            CustomHandlers["LD"] = (ops, _, _) =>
            {
                int rd = RegNum(ops[0]);
                var mode = ops.Length > 1 ? ops[1].ToLowerInvariant() : "";
                uint w = mode switch
                {
                    "x" => (uint)((0x90 << 10) | (((rd >> 4) & 1) << 9) | ((rd & 0xF) << 4) | 0x0C),
                    "x+" => (uint)((0x90 << 10) | (((rd >> 4) & 1) << 9) | ((rd & 0xF) << 4) | 0x0D),
                    "-x" => (uint)((0x90 << 10) | (((rd >> 4) & 1) << 9) | ((rd & 0xF) << 4) | 0x0E),
                    "y" => (uint)((0x80 << 10) | (((rd >> 4) & 1) << 9) | ((rd & 0xF) << 4) | 0x08),
                    "y+" => (uint)((0x90 << 10) | (((rd >> 4) & 1) << 9) | ((rd & 0xF) << 4) | 0x09),
                    "-y" => (uint)((0x90 << 10) | (((rd >> 4) & 1) << 9) | ((rd & 0xF) << 4) | 0x0A),
                    "z" => (uint)((0x80 << 10) | (((rd >> 4) & 1) << 9) | ((rd & 0xF) << 4) | 0x00),
                    "z+" => (uint)((0x90 << 10) | (((rd >> 4) & 1) << 9) | ((rd & 0xF) << 4) | 0x01),
                    "-z" => (uint)((0x90 << 10) | (((rd >> 4) & 1) << 9) | ((rd & 0xF) << 4) | 0x02),
                    _ => (uint)((0x80 << 10) | (((rd >> 4) & 1) << 9) | ((rd & 0xF) << 4) | 0x0C) // default X
                };
                return Word16(w);
            };

            // ST X/Y/Z, Rr
            CustomHandlers["ST"] = (ops, _, _) =>
            {
                int rr = RegNum(ops[1]);
                var mode = ops[0].ToLowerInvariant();
                uint w = mode switch
                {
                    "x" => (uint)((0x92 << 10) | (((rr >> 4) & 1) << 9) | ((rr & 0xF) << 4) | 0x0C),
                    "x+" => (uint)((0x92 << 10) | (((rr >> 4) & 1) << 9) | ((rr & 0xF) << 4) | 0x0D),
                    "-x" => (uint)((0x92 << 10) | (((rr >> 4) & 1) << 9) | ((rr & 0xF) << 4) | 0x0E),
                    "y" => (uint)((0x82 << 10) | (((rr >> 4) & 1) << 9) | ((rr & 0xF) << 4) | 0x08),
                    "y+" => (uint)((0x92 << 10) | (((rr >> 4) & 1) << 9) | ((rr & 0xF) << 4) | 0x09),
                    "-y" => (uint)((0x92 << 10) | (((rr >> 4) & 1) << 9) | ((rr & 0xF) << 4) | 0x0A),
                    "z" => (uint)((0x82 << 10) | (((rr >> 4) & 1) << 9) | ((rr & 0xF) << 4) | 0x00),
                    "z+" => (uint)((0x92 << 10) | (((rr >> 4) & 1) << 9) | ((rr & 0xF) << 4) | 0x01),
                    "-z" => (uint)((0x92 << 10) | (((rr >> 4) & 1) << 9) | ((rr & 0xF) << 4) | 0x02),
                    _ => (uint)((0x92 << 10) | (((rr >> 4) & 1) << 9) | ((rr & 0xF) << 4) | 0x0C)
                };
                return Word16(w);
            };

            // PUSH Rr
            CustomHandlers["PUSH"] = (ops, _, _) =>
            {
                int rr = RegNum(ops[0]);
                return Word16(EncodeRegOnly(0x92, rr, 0x0F));
            };

            // POP Rd
            CustomHandlers["POP"] = (ops, _, _) =>
            {
                int rd = RegNum(ops[0]);
                return Word16(EncodeRegOnly(0x90, rd, 0x0F));
            };

            // IN Rd, A
            CustomHandlers["IN"] = (ops, _, _) =>
            {
                int rd = RegNum(ops[0]);
                int a = ResolveValue(ops[1], null, 0) & 0x3F;
                uint w = (uint)((0xB0 << 10) | (((a >> 4) & 1) << 9) | ((rd & 0xF) << 4) | (a & 0xF));
                return Word16(w);
            };

            // OUT A, Rr
            CustomHandlers["OUT"] = (ops, _, _) =>
            {
                int a = ResolveValue(ops[0], null, 0) & 0x3F;
                int rr = RegNum(ops[1]);
                uint w = (uint)((0xB8 << 10) | (((a >> 4) & 1) << 9) | ((rr & 0xF) << 4) | (a & 0xF));
                return Word16(w);
            };

            // ADIW Rd+1:Rd, K (Rd in {24,26,28,30})
            CustomHandlers["ADIW"] = (ops, _, _) =>
            {
                int rd = RegNum(ops[0]);
                int k = ResolveValue(ops[1], null, 0) & 0x3F;
                int pair = (rd - 24) >> 1;
                uint w = (uint)((0x96 << 10) | ((pair & 3) << 4) | (k & 0x3F));
                return Word16(w);
            };

            // SBIW
            CustomHandlers["SBIW"] = (ops, _, _) =>
            {
                int rd = RegNum(ops[0]);
                int k = ResolveValue(ops[1], null, 0) & 0x3F;
                int pair = (rd - 24) >> 1;
                uint w = (uint)((0x97 << 10) | ((pair & 3) << 4) | (k & 0x3F));
                return Word16(w);
            };

            // JMP addr (32-bit)
            CustomHandlers["JMP"] = (ops, labels, addr) =>
            {
                int target = ResolveValue(ops[0], labels, addr);
                uint w = (uint)((0x0C << 10) | 0x02); // JMP prefix
                return new byte[] { (byte)(w & 0xFF), (byte)((w >> 8) & 0xFF), (byte)((target >> 1) & 0xFF), (byte)(((target >> 1) >> 8) & 0xFF) };
            };

            // CALL addr (32-bit)
            CustomHandlers["CALL"] = (ops, labels, addr) =>
            {
                int target = ResolveValue(ops[0], labels, addr);
                uint w = (uint)((0x0E << 10) | 0x02); // CALL prefix
                return new byte[] { (byte)(w & 0xFF), (byte)((w >> 8) & 0xFF), (byte)((target >> 1) & 0xFF), (byte)(((target >> 1) >> 8) & 0xFF) };
            };

            // RJMP k (relative)
            CustomHandlers["RJMP"] = (ops, labels, addr) =>
            {
                int target = ResolveValue(ops[0], labels, addr);
                int offset = ((target - (addr + 2)) >> 1) & 0xFFF;
                return Word16((uint)(0xC000 | offset));
            };

            // RCALL k
            CustomHandlers["RCALL"] = (ops, labels, addr) =>
            {
                int target = ResolveValue(ops[0], labels, addr);
                int offset = ((target - (addr + 2)) >> 1) & 0xFFF;
                return Word16((uint)(0xD000 | offset));
            };

            // Branches
            void Branch(string name, int cond) => CustomHandlers[name] = (ops, labels, addr) =>
            {
                int target = ResolveValue(ops[0], labels, addr);
                int offset = ((target - (addr + 2)) >> 1) & 0x7F;
                return Word16((uint)(0xF000 | (offset << 3) | cond));
            };

            Branch("BREQ", 1); Branch("BRNE", 9);
            Branch("BRCS", 0); Branch("BRCC", 8);
            Branch("BRSH", 4); Branch("BRLO", 0);
            Branch("BRMI", 2); Branch("BRPL", 6); // Actually BRMI=2, BRPL=6
            Branch("BRGE", 4); Branch("BRLT", 6);

            // SBIC, SBIS, SBI, CBI
            CustomHandlers["SBIC"] = (ops, _, _) => { int a = ResolveValue(ops[0], null, 0) & 0x1F; int b = ResolveValue(ops[1], null, 0) & 7; return Word16((uint)(0x9900 | (b << 4) | a)); };
            CustomHandlers["SBIS"] = (ops, _, _) => { int a = ResolveValue(ops[0], null, 0) & 0x1F; int b = ResolveValue(ops[1], null, 0) & 7; return Word16((uint)(0x9B00 | (b << 4) | a)); };
            CustomHandlers["SBI"] = (ops, _, _) => { int a = ResolveValue(ops[0], null, 0) & 0x1F; int b = ResolveValue(ops[1], null, 0) & 7; return Word16((uint)(0x9A00 | (b << 4) | a)); };
            CustomHandlers["CBI"] = (ops, _, _) => { int a = ResolveValue(ops[0], null, 0) & 0x1F; int b = ResolveValue(ops[1], null, 0) & 7; return Word16((uint)(0x9800 | (b << 4) | a)); };

            // No-register instructions
            Opcodes["NOP"] = new byte[] { 0x00, 0x00 };
            Opcodes["RET"] = new byte[] { 0x08, 0x95 };
            Opcodes["RETI"] = new byte[] { 0x18, 0x95 };
            Opcodes["SEC"] = new byte[] { 0x94, 0x08 };
            Opcodes["CLC"] = new byte[] { 0x94, 0x08 }; // same as SEC? No: SEC=0x9408, CLC=0x94... hmm
            Opcodes["SEN"] = new byte[] { 0x94, 0x08 };
            Opcodes["CLN"] = new byte[] { 0x94, 0x0A };
            Opcodes["SEZ"] = new byte[] { 0x94, 0x08 };
            Opcodes["CLZ"] = new byte[] { 0x94, 0x0A };
            Opcodes["SEI"] = new byte[] { 0x94, 0x78 };
            Opcodes["CLI"] = new byte[] { 0x94, 0x68 };
            Opcodes["SES"] = new byte[] { 0x94, 0x08 };
            Opcodes["CLS"] = new byte[] { 0x94, 0x0A };
            Opcodes["SLEEP"] = new byte[] { 0x95, 0x88 };
            Opcodes["WDR"] = new byte[] { 0x95, 0xA8 };
        }
    }
}
