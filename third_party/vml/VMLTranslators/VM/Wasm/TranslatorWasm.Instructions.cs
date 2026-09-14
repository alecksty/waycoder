using VMLAssembler;
using System.Text;

namespace VMLTranslators
{
    public partial class TranslatorWasm
    {
        protected override void TranslateInstruction(Instruction instr)
        {
            switch (instr.Opcode)
            {
                // ======== Load / Move ========
                                                                case OpCode.MOVE:
                    TranslateMOVE(instr);
                    break;
                                case OpCode.ENTER:
                    TranslateENTER(instr);
                    break;
                case OpCode.LEAVE:
                    TranslateLEAVE(instr);
                    break;

                // ======== Store ========
                                                                // ======== Stack ========
                case OpCode.PUSH:
                    Indent($"  ;; PUSH {GetRegName(instr.Operands[0])}");
                    Indent($"  local.get $sp");
                    Indent($"  {GetOperandExpr(instr, 0)}");
                    Indent("  i32.store");
                    Indent("  local.get $sp");
                    Indent("  i32.const 4");
                    Indent("  i32.sub");
                    Indent("  local.set $sp");
                    break;
                case OpCode.POP:
                    Indent($"  ;; POP {GetRegName(instr.Operands[0])}");
                    Indent("  local.get $sp");
                    Indent("  i32.const 4");
                    Indent("  i32.add");
                    Indent("  local.set $sp");
                    Indent("  local.get $sp");
                    Indent("  i32.load");
                    Indent($"  local.set {GetRegName(instr.Operands[0])}");
                    break;

                // ======== Arith ========
                case OpCode.ADD: TranslateBinaryOp(instr, "i32.add"); break;
                case OpCode.SUB: TranslateBinaryOp(instr, "i32.sub"); break;
                case OpCode.MUL: TranslateBinaryOp(instr, "i32.mul"); break;
                case OpCode.DIV: TranslateBinaryOp(instr, "i32.div_s"); break;
                case OpCode.MOD: TranslateBinaryOp(instr, "i32.rem_s"); break;
                case OpCode.INC: EmitUnary("i32.const 1", "i32.add", instr); break;
                case OpCode.DEC: EmitUnary("i32.const 1", "i32.sub", instr); break;
                case OpCode.NEG: EmitUnary("i32.const 0", "i32.sub", instr, swap: true); break;
                case OpCode.TEST:
                    // TEST Rx, Ry — saves register + 0 to CMP memory
                    // so conditional branch can compare Rx == 0
                    Indent($"  ;; TEST {GetOperandExpr(instr, 0)} — save to CMP memory");
                    Indent($"  i32.const {CMP_LEFT_ADDR}");
                    Indent($"  {GetOperandExpr(instr, 0)}");
                    Indent("  i32.store");
                    Indent($"  i32.const {CMP_RIGHT_ADDR}");
                    Indent("  i32.const 0");
                    Indent("  i32.store");
                    break;

                // ======== Logic ========
                case OpCode.AND: TranslateBinaryOp(instr, "i32.and"); break;
                case OpCode.OR:  TranslateBinaryOp(instr, "i32.or"); break;
                case OpCode.XOR: TranslateBinaryOp(instr, "i32.xor"); break;
                case OpCode.NOT:
                    Indent($"  ;; NOT {GetRegName(instr.Operands[0])}");
                    Indent($"  {GetOperandExpr(instr, 0)}");
                    Indent("  i32.const -1");
                    Indent("  i32.xor");
                    Indent($"  local.set {GetRegName(instr.Operands[0])}");
                    break;

                // ======== Shift ========
                case OpCode.SHL: TranslateBinaryOp(instr, "i32.shl"); break;
                case OpCode.SHR: TranslateBinaryOp(instr, "i32.shr_s"); break;
                case OpCode.SHLV: TranslateShiftVar(instr, "i32.shl"); break;
                case OpCode.SHRV: TranslateShiftVar(instr, "i32.shr_s"); break;

                // ======== CMP ========
                case OpCode.CMP:
                    TranslateCMP(instr);
                    break;

                // ======== Control Flow ========
                case OpCode.JMP: TranslateJMP(instr); break;
                case OpCode.JZ:  TranslateBranch(instr, "i32.eq"); break;   // jump if equal (Z flag from CMP)
                case OpCode.JNZ: TranslateBranch(instr, "i32.ne"); break;  // jump if not equal
                case OpCode.JE:  TranslateBranch(instr, "i32.eq"); break;
                case OpCode.JNE: TranslateBranch(instr, "i32.ne"); break;
                case OpCode.JG:  TranslateBranch(instr, "i32.gt_s"); break;
                case OpCode.JL:  TranslateBranch(instr, "i32.lt_s"); break;
                case OpCode.JGE: TranslateBranch(instr, "i32.ge_s"); break;
                case OpCode.JLE: TranslateBranch(instr, "i32.le_s"); break;

                case OpCode.CALL: TranslateCALL(instr); break;
                case OpCode.RET:  TranslateRET(instr); break;

                // ======== Float ========
                case OpCode.MOVEF:
                                // MOVEF store path: dest != REGISTER → FSTORE (handled via goto from MOVEF above)
                                case OpCode.FADD: TranslateBinaryOp(instr, "f32.add"); break;
                case OpCode.FSUB: TranslateBinaryOp(instr, "f32.sub"); break;
                case OpCode.FMUL: TranslateBinaryOp(instr, "f32.mul"); break;
                case OpCode.FDIV: TranslateBinaryOp(instr, "f32.div"); break;
                case OpCode.FNEG:
                    Indent($"  ;; FNEG {GetFRegName(instr.Operands[0])}");
                    Indent($"  {GetOperandExpr(instr, 0)}");
                    Indent("  f32.neg");
                    Indent($"  local.set {GetFRegName(instr.Operands[0])}");
                    break;
                case OpCode.FCMP:
                    // FCMP Fs, Ft — compare two f32 and save to CMP memory for branches
                    {
                        var left = instr.Operands[0];
                        var right = instr.Operands[1];
                        Indent($"  ;; FCMP {GetFRegName(left)}, {GetFRegName(right)} — save f32 to CMP memory");
                        Indent($"  i32.const {CMP_LEFT_ADDR}");
                        Indent($"  {GetOperandExpr(instr, 0)}");
                        Indent("  i32.store");
                        Indent($"  i32.const {CMP_RIGHT_ADDR}");
                        Indent($"  {GetOperandExpr(instr, 1)}");
                        Indent("  i32.store");
                    }
                    break;
                case OpCode.I2F:
                    Indent($"  ;; I2F {GetRegName(instr.Operands[0])}");
                    Indent($"  {GetOperandExpr(instr, 0)}");
                    Indent("  f32.convert_i32_s");
                    Indent($"  local.set {GetFRegName(instr.Operands[0])}");
                    break;
                case OpCode.F2I:
                    Indent($"  ;; F2I {GetFRegName(instr.Operands[0])}");
                    Indent($"  {GetOperandExpr(instr, 0)}");
                    Indent("  i32.trunc_f32_s");
                    Indent($"  local.set {GetRegName(instr.Operands[0])}");
                    break;
                case OpCode.FPUSH:
                    // FPUSH Fs — push f32 onto stack: *(SP-4) = Fs; SP -= 4
                    {
                        int f = SafeToInt(instr.Operands[0]);
                        Indent($"  ;; FPUSH F{f}");
                        Indent("  local.get $sp");
                        Indent("  i32.const 4");
                        Indent("  i32.sub");
                        Indent("  local.tee $sp");
                        Indent($"  local.get $f{f}");
                        Indent("  f32.store");
                    }
                    break;
                case OpCode.FPOP:
                    // FPOP Fd — pop f32 from stack: Fd = *SP; SP += 4
                    {
                        int f = SafeToInt(instr.Operands[0]);
                        Indent($"  ;; FPOP F{f}");
                        Indent("  local.get $sp");
                        Indent("  f32.load");
                        Indent($"  local.set $f{f}");
                        Indent("  local.get $sp");
                        Indent("  i32.const 4");
                        Indent("  i32.add");
                        Indent("  local.set $sp");
                    }
                    break;

                // ======== Double ========
                case OpCode.MOVED:
                                // MOVED store path: dest != REGISTER → DSTORE (handled via goto from MOVED above)
                                case OpCode.DADD: TranslateBinaryOp(instr, "f64.add"); break;
                case OpCode.DSUB: TranslateBinaryOp(instr, "f64.sub"); break;
                case OpCode.DMUL: TranslateBinaryOp(instr, "f64.mul"); break;
                case OpCode.DDIV: TranslateBinaryOp(instr, "f64.div"); break;
                case OpCode.DNEG:
                    Indent($"  ;; DNEG {GetDRegName(instr.Operands[0])}");
                    Indent($"  {GetOperandExpr(instr, 0)}");
                    Indent("  f64.neg");
                    Indent($"  local.set {GetDRegName(instr.Operands[0])}");
                    break;
                case OpCode.DCMP:
                    // DCMP Ds, Dt — compare two f64 and save to CMP memory for branches
                    {
                        var left = instr.Operands[0];
                        var right = instr.Operands[1];
                        Indent($"  ;; DCMP {GetDRegName(left)}, {GetDRegName(right)} — save f64 to CMP memory");
                        Indent($"  i32.const {CMP_LEFT_ADDR}");
                        Indent($"  {GetOperandExpr(instr, 0)}");
                        Indent("  i32.store");
                        Indent($"  i32.const {CMP_RIGHT_ADDR}");
                        Indent($"  {GetOperandExpr(instr, 1)}");
                        Indent("  i32.store");
                    }
                    break;
                case OpCode.I2D:
                    Indent($"  ;; I2D {GetRegName(instr.Operands[0])}");
                    Indent($"  {GetOperandExpr(instr, 0)}");
                    Indent("  f64.convert_i32_s");
                    Indent($"  local.set {GetDRegName(instr.Operands[0])}");
                    break;
                case OpCode.D2I:
                    Indent($"  ;; D2I {GetDRegName(instr.Operands[0])}");
                    Indent($"  {GetOperandExpr(instr, 0)}");
                    Indent("  i32.trunc_f64_s");
                    Indent($"  local.set {GetRegName(instr.Operands[0])}");
                    break;
                case OpCode.F2D:
                    // F2D Rd, Rs — convert f32 to f64: Dd = (f64)Fs
                    {
                        int dd = SafeToInt(instr.Operands[0]);
                        int fs = instr.Operands.Count >= 2 ? SafeToInt(instr.Operands[1]) : dd;
                        Indent($"  ;; F2D D{dd} = (f64)F{fs}");
                        Indent($"  local.get $f{fs}");
                        Indent("  f64.promote_f32");
                        Indent($"  local.set $d{dd}");
                    }
                    break;
                case OpCode.D2F:
                    // D2F Rd, Rs — convert f64 to f32: Fd = (f32)Ds
                    {
                        int fd = SafeToInt(instr.Operands[0]);
                        int ds = instr.Operands.Count >= 2 ? SafeToInt(instr.Operands[1]) : fd;
                        Indent($"  ;; D2F F{fd} = (f32)D{ds}");
                        Indent($"  local.get $d{ds}");
                        Indent("  f32.demote_f64");
                        Indent($"  local.set $f{fd}");
                    }
                    break;
                case OpCode.DPUSH:
                    // DPUSH Rd — push f64 onto stack: *(SP-8) = Dd; SP -= 8
                    {
                        int d = SafeToInt(instr.Operands[0]);
                        Indent($"  ;; DPUSH D{d}");
                        Indent("  local.get $sp");
                        Indent("  i32.const 8");
                        Indent("  i32.sub");
                        Indent("  local.tee $sp");
                        Indent($"  local.get $d{d}");
                        Indent("  f64.store");
                    }
                    break;
                case OpCode.DPOP:
                    // DPOP Rd — pop f64 from stack: Dd = *SP; SP += 8
                    {
                        int d = SafeToInt(instr.Operands[0]);
                        Indent($"  ;; DPOP D{d}");
                        Indent("  local.get $sp");
                        Indent("  f64.load");
                        Indent($"  local.set $d{d}");
                        Indent("  local.get $sp");
                        Indent("  i32.const 8");
                        Indent("  i32.add");
                        Indent("  local.set $sp");
                    }
                    break;

                // ======== Syscall ========
                case OpCode.SYSCALL:
                    TranslateSYSCALL(instr);
                    break;

                // ======== Other ========
                case OpCode.NOP:
                    Indent("  ;; nop");
                    break;
                case OpCode.HALT:
                    Indent("  ;; HALT — return 0");
                    Indent("  i32.const 0");
                    Indent("  return");
                    break;
                case OpCode.LABEL:
                    Indent($"  ;; LABEL {instr.Operands[0].Value}");
                    break;

                // 符号扩展+循环移位 (Wasm 原生) / 条件移动 (软件库) (v1.66.28+)
                case OpCode.SEXTB: Indent("  i32.extend8_s"); break;
                case OpCode.SEXTH: Indent("  i32.extend16_s"); break;
                case OpCode.ROL: Indent("  i32.rotl"); break;
                case OpCode.ROR: Indent("  i32.rotr"); break;
                case OpCode.CMOVZ: case OpCode.CMOVNZ:
                    Indent($"  call $__{instr.Opcode.ToString().ToLower()}");
                    break;

                // 字节/半字移动 (Wasm 原生符号扩展) (v1.66.31)
                case OpCode.MOVEB: Indent("  i32.extend8_s"); break;
                case OpCode.MOVEH: Indent("  i32.extend16_s"); break;

                // 字节/半字栈操作 (v1.66.31)
                case OpCode.PUSHB:
                case OpCode.PUSHH:
                    // PUSHB/PUSHH — push value to stack (Wasm memory is byte-addressable, same as PUSH)
                    Indent($"  ;; {instr.Opcode} {GetRegName(instr.Operands[0])}");
                    Indent($"  local.get $sp");
                    Indent($"  {GetOperandExpr(instr, 0)}");
                    Indent("  i32.store");
                    Indent("  local.get $sp");
                    Indent("  i32.const 4");
                    Indent("  i32.sub");
                    Indent("  local.set $sp");
                    break;
                case OpCode.POPB:
                    // POPB Rd — pop byte from stack, sign-extend to 32-bit
                    Indent($"  ;; POPB {GetRegName(instr.Operands[0])}");
                    Indent("  local.get $sp");
                    Indent("  i32.const 4");
                    Indent("  i32.add");
                    Indent("  local.set $sp");
                    Indent("  local.get $sp");
                    Indent("  i32.load8_s");
                    Indent($"  local.set {GetRegName(instr.Operands[0])}");
                    break;
                case OpCode.POPH:
                    // POPH Rd — pop halfword from stack, sign-extend to 32-bit
                    Indent($"  ;; POPH {GetRegName(instr.Operands[0])}");
                    Indent("  local.get $sp");
                    Indent("  i32.const 4");
                    Indent("  i32.add");
                    Indent("  local.set $sp");
                    Indent("  local.get $sp");
                    Indent("  i32.load16_s");
                    Indent($"  local.set {GetRegName(instr.Operands[0])}");
                    break;

                // 清零寄存器 (v1.66.31)
                case OpCode.ZERO:
                    Indent($"  ;; ZERO {GetRegName(instr.Operands[0])}");
                    Indent("  i32.const 0");
                    Indent($"  local.set {GetRegName(instr.Operands[0])}");
                    break;

                default:
                    Indent($"  ;; UNSUPPORTED: {instr.Opcode}");
                    break;
            }
        }

        // ======== Translation helpers ========

        private void TranslateLOAD(Instruction instr)
        {
            if (instr.Operands.Count != 2)
            {
                Indent($"  ;; LOAD error: expected 2 operands");
                return;
            }
            var dst = instr.Operands[0];
            var src = instr.Operands[1];

            if (src.Type == OperandType.IMMEDIATE)
            {
                Indent($"  ;; LOAD {GetRegName(dst)}, #{src.Value}");
                Indent($"  i32.const {SafeToInt(src)}");
                Indent($"  local.set {GetRegName(dst)}");
            }
            else if (src.Type == OperandType.REGISTER)
            {
                Indent($"  ;; LOAD {GetRegName(dst)}, {GetRegName(src)}");
                Indent($"  local.get {GetRegName(src)}");
                Indent($"  local.set {GetRegName(dst)}");
            }
            else if (src.Type == OperandType.LABEL)
            {
                Indent($"  ;; LOAD {GetRegName(dst)}, {src.Value}");
                Indent($"  i32.const {GetLabelAddr(src.Value?.ToString() ?? "0")}");
                Indent($"  local.set {GetRegName(dst)}");
            }
            else if (src.Type == OperandType.MEMORY)
            {
                var exprStr = src.Value?.ToString() ?? "0";
                Indent($"  ;; LOAD {GetRegName(dst)}, [{src.Value}]");
                if (exprStr.StartsWith("R"))
                {
                    var memExpr = ParseMemExpr(exprStr);
                    Indent($"  i32.const {memExpr.offset}");
                    Indent($"  local.get {memExpr.reg}");
                    Indent("  i32.add");
                }
                else if (LabelMap.TryGetValue(exprStr, out var resolved))
                {
                    Indent($"  i32.const {resolved}");
                }
                else
                {
                    Indent($"  i32.const {SafeToInt(exprStr)}");
                }
                Indent("  i32.load");
                Indent($"  local.set {GetRegName(dst)}");
            }
        }

        private void TranslateMOVE(Instruction instr)
        {
            var dst = instr.Operands[0];
            if (instr.Operands.Count == 1)
            {
                Indent($"  ;; MOVE {GetRegName(dst)} (no-op)");
            }
            else
            {
                var src = instr.Operands[1];
                Indent($"  ;; MOVE {GetRegName(dst)}, {GetOperandExpr(instr, 1)}");
                Indent($"  {GetOperandExpr(instr, 1)}");
                Indent($"  local.set {GetRegName(dst)}");
            }
        }

        private void TranslateLEA(Instruction instr)
        {
            // LEA Rd, offset(Rs) — compute effective address
            var dst = instr.Operands[0];
            var memExpr = ParseMemExpr(instr.Operands[1].Value?.ToString() ?? "0");
            Indent($"  ;; LEA {GetRegName(dst)}, {instr.Operands[1].Value}");
            Indent($"  local.get {memExpr.reg}");
            Indent($"  i32.const {memExpr.offset}");
            Indent("  i32.add");
            Indent($"  local.set {GetRegName(dst)}");
        }

        private void TranslateENTER(Instruction instr)
        {
            Indent("  ;; ENTER — push bp, set bp=sp, allocate locals");
            Indent("  local.get $sp");
            Indent("  local.get $bp");
            Indent("  i32.store");
            Indent("  local.get $sp");
            Indent("  i32.const 4");
            Indent("  i32.sub");
            Indent("  local.set $sp");
            Indent("  local.get $sp");
            Indent("  local.set $bp");
        }

        private void TranslateLEAVE(Instruction instr)
        {
            Indent("  ;; LEAVE — restore sp from bp, pop bp");
            Indent("  local.get $bp");
            Indent("  local.set $sp");
            Indent("  local.get $sp");
            Indent("  i32.load");
            Indent("  local.set $bp");
            Indent("  local.get $sp");
            Indent("  i32.const 4");
            Indent("  i32.add");
            Indent("  local.set $sp");
        }

        private void TranslateLoadMem(Instruction instr, string wasmLoadOp, int opIdx)
        {
            var dst = instr.Operands[0];
            var memSrc = instr.Operands[opIdx + 1];
            string memStr = memSrc.Value?.ToString() ?? "0";
            string regName;

            if (memStr.StartsWith("(") && memStr.EndsWith(")"))
            {
                // Memory indirect: (R0) or (R1) etc.
                var innerReg = memStr.Trim('(', ')');
                regName = MapRegisterExpr(innerReg);
                Indent($"  ;; {instr.Opcode} {GetRegName(dst)}, ({innerReg})");
                Indent($"  local.get {regName}");
            }
            else if (memStr.Contains("+") || memStr.Contains("-"))
            {
                // Register+offset: R12+4 or R12-8
                var (reg, offset, neg) = ParseRegOffset(memStr);
                var mappedReg = MapRegisterExpr($"R{reg}");
                Indent($"  ;; {instr.Opcode} {GetRegName(dst)}, [{memStr}]");
                Indent($"  local.get {mappedReg}");
                Indent($"  i32.const {offset}");
                Indent(neg ? "  i32.sub" : "  i32.add");
            }
            else if (memStr.StartsWith("R") && memStr.Length > 1 && int.TryParse(memStr.Substring(1), out var regNum3))
            {
                // [R0] — indirect addressing through register
                var mappedReg = MapRegisterExpr(memStr);
                Indent($"  ;; {instr.Opcode} {GetRegName(dst)}, [{memStr}] (indirect)");
                Indent($"  local.get {mappedReg}");
            }
            else
            {
                // Direct address: 0x4000 or label
                var addrStr = memStr;
                if (LabelMap.TryGetValue(memStr, out var resolved))
                    addrStr = resolved;
                Indent($"  ;; {instr.Opcode} {GetRegName(dst)}, [{memStr}]");
                Indent($"  i32.const {addrStr}");
            }

            Indent($"  {wasmLoadOp}");
            // 根据目标寄存器类型 set
            if (instr.Opcode == OpCode.MOVEF)
                Indent($"  local.set {GetFRegName(dst)}");
            else if (instr.Opcode == OpCode.MOVED)
                Indent($"  local.set $d{SafeToInt(dst)}");
            else
                Indent($"  local.set {GetRegName(dst)}");
        }

        private void TranslateStoreMem(Instruction instr, string wasmStoreOp)
        {
            // Determine which operand is value and which is memory address
            // VML FSTORE can be "FSTORE R0 [addr]" or "FSTORE [addr] R0"
            var v0 = instr.Operands[0];
            var v1 = instr.Operands[1];

            Operand valOp, memOp;
            int valIdx;
            if (v0.Type == OperandType.MEMORY || (v0.Value?.ToString()?.StartsWith("[") == true))
            {
                memOp = v0; valOp = v1; valIdx = 1;
            }
            else
            {
                valOp = v0; memOp = v1; valIdx = 0;
            }

            string memStr = memOp.Value?.ToString() ?? "0";

            if (memStr.StartsWith("(") && memStr.EndsWith(")"))
            {
                var innerReg = memStr.Trim('(', ')');
                Indent($"  ;; {instr.Opcode} {GetOperandValue(valOp)}, ({innerReg})");
                Indent($"  local.get {MapRegisterExpr(innerReg)}");
                Indent($"  {GetOperandExpr(instr, valIdx)}");
            }
            else if (memStr.Contains("+") || memStr.Contains("-"))
            {
                var (reg, offset, neg) = ParseRegOffset(memStr);
                var mappedReg = MapRegisterExpr($"R{reg}");
                Indent($"  ;; {instr.Opcode} {GetOperandValue(valOp)}, [{memStr}]");
                Indent($"  local.get {mappedReg}");
                Indent($"  i32.const {offset}");
                Indent(neg ? "  i32.sub" : "  i32.add");
                Indent($"  {GetOperandExpr(instr, valIdx)}");
            }
            else if (memStr.StartsWith("R") && memStr.Length > 1 && int.TryParse(memStr.Substring(1), out var regNum4))
            {
                // [R0] — indirect addressing through register
                var mappedReg = MapRegisterExpr(memStr);
                Indent($"  ;; {instr.Opcode} {GetOperandValue(valOp)}, [{memStr}] (indirect)");
                Indent($"  local.get {mappedReg}");
                Indent($"  {GetOperandExpr(instr, valIdx)}");
            }
            else
            {
                var addrStr = memStr;
                if (LabelMap.TryGetValue(memStr, out var resolved))
                    addrStr = resolved;
                Indent($"  ;; {instr.Opcode} {GetOperandValue(valOp)}, [{memStr}]");
                Indent($"  i32.const {addrStr}");
                Indent($"  {GetOperandExpr(instr, valIdx)}");
            }
            Indent($"  {wasmStoreOp}");
        }

        private void TranslateBinaryOp(Instruction instr, string wasmOp)
        {
            // NormalizeBinaryOp handled by BaseTranslator.NormalizeAndTranslate
            var dst = instr.Operands[0];
            var src1 = instr.Operands[1];
            var src2 = instr.Operands[2];

            string dstName = IsFloatOp(instr.Opcode) ? GetFRegName(dst)
                           : IsDoubleOp(instr.Opcode) ? $"$d{SafeToInt(dst)}"
                           : GetRegName(dst);

            string loadOp = IsFloatOp(instr.Opcode) ? "f32.load"
                          : IsDoubleOp(instr.Opcode) ? "f64.load"
                          : "i32.load";

            Indent($"  ;; {instr.Opcode} {dstName}={GetOperandValue(src1)} OP {GetOperandValue(src2)}");
            Indent($"  {GetOperandExpr(instr, 1)}");
            EmitOperandWithLoad(instr, 2, loadOp);
            Indent($"  {wasmOp}");
            Indent($"  local.set {dstName}");
        }

        /// <summary>Emit an operand — if it's MEMORY, emit address + load; otherwise just emit the operand expression.</summary>
        private void EmitOperandWithLoad(Instruction instr, int idx, string loadOp)
        {
            if (idx < instr.Operands.Count && instr.Operands[idx].Type == OperandType.MEMORY)
            {
                string addrStr = GetMemAddr(instr.Operands[idx]);
                Indent($"  i32.const {addrStr}");
                Indent($"  {loadOp}");
            }
            else
            {
                Indent($"  {GetOperandExpr(instr, idx)}");
            }
        }

        private string GetMemAddr(Operand op)
        {
            var val = op.Value;
            if (val is int i) return i.ToString();
            var s = val?.ToString() ?? "0";
            if (LabelMap.TryGetValue(s, out var resolved))
                return resolved;
            if (int.TryParse(s, out int r)) return r.ToString();
            return s;
        }

        private void EmitUnary(string constOp, string wasmOp, Instruction instr, bool swap = false)
        {
            var dst = instr.Operands[0];
            Indent($"  ;; {instr.Opcode} {GetRegName(dst)}");
            if (swap)
            {
                Indent($"  {constOp}");
                Indent($"  {GetOperandExpr(instr, 0)}");
            }
            else
            {
                Indent($"  {GetOperandExpr(instr, 0)}");
                Indent($"  {constOp}");
            }
            Indent($"  {wasmOp}");
            Indent($"  local.set {GetRegName(dst)}");
        }

        private void TranslateShiftVar(Instruction instr, string wasmOp)
        {
            // SHLV Rd, Rs, Rt — shift Rs by value in Rt
            var dst = instr.Operands[0];
            var src = instr.Operands[1];
            var shift = instr.Operands[2];
            Indent($"  ;; {instr.Opcode} {GetRegName(dst)}, {GetOperandValue(src)}, {GetOperandValue(shift)}");
            Indent($"  {GetOperandExpr(instr, 1)}");
            Indent($"  {GetOperandExpr(instr, 2)}");
            Indent($"  {wasmOp}");
            Indent($"  local.set {GetRegName(dst)}");
        }

        private void TranslateCMP(Instruction instr)
        {
            // CMP sets flags for subsequent branches.
            // Save operands to shared memory so branches (JZ/JL/JE/etc.) can load them.
            var left = instr.Operands[0];
            var right = instr.Operands[1];
            Indent($"  ;; CMP {GetOperandValue(left)}, {GetOperandValue(right)} — save operands");
            Indent($"  i32.const {CMP_LEFT_ADDR}");
            Indent($"  {GetOperandExpr(instr, 0)}");
            Indent("  i32.store");
            Indent($"  i32.const {CMP_RIGHT_ADDR}");
            Indent($"  {GetOperandExpr(instr, 1)}");
            Indent("  i32.store");
        }

        private void TranslateJMP(Instruction instr)
        {
            var label = instr.Operands[0].Value?.ToString() ?? "unknown";
            if (_returnLabels.Contains(label))
            {
                Indent($"  ;; JMP {label} (epilogue) → return");
                Indent("  local.get $r0");
                Indent("  return");
            }
            else
            {
                Indent($"  ;; JMP {label}");
                Indent($"  br ${SanitizeLabel(label)}");
            }
        }

        private void TranslateBranch(Instruction instr, string wasmCond, string? wasmCond2 = null)
        {
            var label = instr.Operands[0].Value?.ToString() ?? "unknown";
            var cleanLabel = SanitizeLabel(label);

            // All conditional branches load CMP operands from shared memory.
            // CMP saved left operand (R1) at CMP_LEFT_ADDR and right operand (R0) at CMP_RIGHT_ADDR.
            // JZ/JNZ compare left == right; JL/JG/etc. compare left < right, etc.

            if (_returnLabels.Contains(label))
            {
                // Conditional jump to epilogue → conditional return
                Indent($"  ;; {instr.Opcode} {label} (epilogue) → conditional return");
                Indent($"  i32.const {CMP_LEFT_ADDR}");
                Indent("  i32.load");
                Indent($"  i32.const {CMP_RIGHT_ADDR}");
                Indent("  i32.load");
                Indent($"  {wasmCond}");
                if (wasmCond2 != null)
                    Indent($"  {wasmCond2}");
                Indent("  if");
                Indent("    local.get $r0");
                Indent("    return");
                Indent("  end");
                return;
            }

            Indent($"  ;; {instr.Opcode} {label}");
            Indent($"  i32.const {CMP_LEFT_ADDR}");
            Indent("  i32.load");
            Indent($"  i32.const {CMP_RIGHT_ADDR}");
            Indent("  i32.load");
            Indent($"  {wasmCond}");
            if (wasmCond2 != null)
                Indent($"  {wasmCond2}");
            Indent($"  br_if ${cleanLabel}");
        }

        private void TranslateCALL(Instruction instr)
        {
            var target = instr.Operands[0].Value?.ToString() ?? "unknown";
            Indent($"  ;; CALL {target}");

            // Inline known builtins instead of generating WASM call
            switch (target)
            {
                case "vml_poke":
                    Indent("  local.get $r1");
                    Indent("  local.get $r0");
                    Indent("  i32.store");
                    break;
                case "vml_pokeb":
                    Indent("  local.get $r1");
                    Indent("  local.get $r0");
                    Indent("  i32.store8");
                    break;
                case "vml_pokeh":
                    Indent("  local.get $r1");
                    Indent("  local.get $r0");
                    Indent("  i32.store16");
                    break;
                case "shared_poke":
                    Indent("  local.get $bp");
                    Indent("  i32.const 12");
                    Indent("  i32.add");
                    Indent("  i32.load");
                    Indent("  local.get $bp");
                    Indent("  i32.const 8");
                    Indent("  i32.add");
                    Indent("  i32.load");
                    Indent("  i32.store");
                    break;
                case "shared_pokeb":
                    Indent("  local.get $bp");
                    Indent("  i32.const 12");
                    Indent("  i32.add");
                    Indent("  i32.load");
                    Indent("  local.get $bp");
                    Indent("  i32.const 8");
                    Indent("  i32.add");
                    Indent("  i32.load");
                    Indent("  i32.store8");
                    break;
                case "shared_pokeh":
                    Indent("  local.get $bp");
                    Indent("  i32.const 12");
                    Indent("  i32.add");
                    Indent("  i32.load");
                    Indent("  local.get $bp");
                    Indent("  i32.const 8");
                    Indent("  i32.add");
                    Indent("  i32.load");
                    Indent("  i32.store16");
                    break;
                default:
                    if (_functionEntries.Contains(target))
                    {
                        // Cross-function call: save BP/SP and param regs, call, restore return value
                        Indent($"  ;; save BP, SP for callee");
                        Indent($"  i32.const {BP_MEM_ADDR}");
                        Indent("  local.get $bp");
                        Indent("  i32.store");
                        Indent($"  i32.const {SP_MEM_ADDR}");
                        Indent("  local.get $sp");
                        Indent("  i32.store");
                        Indent($"  ;; save param regs R0, R1, R2");
                        Indent($"  i32.const {PARAM0_MEM_ADDR}");
                        Indent("  local.get $r0");
                        Indent("  i32.store");
                        Indent($"  i32.const {PARAM1_MEM_ADDR}");
                        Indent("  local.get $r1");
                        Indent("  i32.store");
                        Indent($"  i32.const {PARAM2_MEM_ADDR}");
                        Indent("  local.get $r2");
                        Indent("  i32.store");
                        Indent($"  ;; save float param regs F0, F1");
                        Indent($"  i32.const {PARAMF0_MEM_ADDR}");
                        Indent("  local.get $f0");
                        Indent("  f32.store");
                        Indent($"  i32.const {PARAMF1_MEM_ADDR}");
                        Indent("  local.get $f1");
                        Indent("  f32.store");
                        Indent($"  call ${SanitizeLabel(target)}");
                        Indent("  local.set $r0  ;; return value");
                    }
                    else
                    {
                        // Intra-function call (e.g. library stub) — use br
                        Indent($"  br ${SanitizeLabel(target)}");
                    }
                    break;
            }
        }

        private void TranslateRET(Instruction instr)
        {
            Indent("  ;; RET");
            Indent("  local.get $r0");
            Indent("  return");
        }

        private void TranslateSYSCALL(Instruction instr)
        {
            var num = SafeToInt(instr.Operands[0]);
            Indent($"  ;; SYSCALL #{num}");

            switch (num)
            {
                case 0: // getconfig
                    Indent("  i32.const 0  ;; getconfig placeholder");
                    break;
                case 1: // print_string at R0
                    EmitPrintString();
                    break;
                case 3: // exit
                    Indent("  local.get $r0");
                    Indent("  call $proc_exit");
                    break;
                case 4: // print_char R0
                    EmitPrintChar();
                    break;
                case 5: // getchar
                    Indent("  i32.const 0  ;; getchar placeholder");
                    break;
                case 6: // print_int R0
                    EmitPrintInt();
                    break;
                case 40: // malloc
                    Indent("  ;; malloc(R0) -> R0");
                    Indent("  local.get $r0");
                    Indent("  memory.grow");
                    Indent("  i32.const 16");
                    Indent("  i32.shl");
                    Indent("  local.set $r0");
                    break;
                case 41: // free
                    Indent("  ;; free — no-op in Wasm");
                    break;
                case 50: // random
                    Indent("  ;; random — placeholder");
                    Indent("  i32.const 42");
                    Indent("  local.set $r0");
                    break;
                case 60: // getconfig
                    Indent("  ;; getconfig — return device base address");
                    Indent("  i32.const 0");
                    Indent("  local.set $r0");
                    break;
                default:
                    Indent($"  ;; SYSCALL #{num} — not implemented");
                    Indent("  i32.const 0 ;; default return");
                    break;
            }
        }

        private void EmitPrintString()
        {
            // Build CIOVE on stack (at SP area)
            Indent("  ;; fd_write(1, str_ptr, strlen(str_ptr), &written)");
            Indent("  ;; Build iovec: [ptr, len] at sp");
            Indent("  local.get $sp  ;; iovec base");
            Indent("  local.get $r0  ;; ptr");
            Indent("  i32.store");
            Indent("  local.get $sp");
            Indent("  i32.const 4");
            Indent("  i32.add");
            Indent("  local.get $r0  ;; compute strlen");
            Indent("  call $strlen");
            Indent("  i32.store");
            Indent("  ;; fd_write(1, $sp, 1, $sp+8)");
            Indent("  i32.const 1");
            Indent("  local.get $sp");
            Indent("  i32.const 1");
            Indent("  local.get $sp");
            Indent("  i32.const 8");
            Indent("  i32.add");
            Indent("  call $fd_write");
            Indent("  drop");
        }

        private void EmitPrintChar()
        {
            Indent("  ;; fd_write(1, &char_buf, 1, &written)");
            Indent("  local.get $sp");
            Indent("  local.get $r0");
            Indent("  i32.store8");
            Indent("  i32.const 1");
            Indent("  local.get $sp");
            Indent("  i32.const 1");
            Indent("  local.get $sp");
            Indent("  i32.const 1");
            Indent("  i32.add");
            Indent("  call $fd_write");
            Indent("  drop");
        }

        private void EmitPrintInt()
        {
            // itoa then fd_write
            Indent("  local.get $r0");
            Indent("  call $itoa");
            Indent("  local.set $r0");
            Indent("  ;; fallthrough to print string");
            Indent("  local.get $r0  ;; ptr");
            Indent("  call $strlen");
            Indent("  local.set $r1  ;; len");
            Indent("  i32.const 1    ;; fd=1");
            Indent("  local.get $r0  ;; iov_base");
            Indent("  local.get $r1  ;; iov_len");
            Indent("  local.get $sp  ;; &written");
            Indent("  call $fd_write");
            Indent("  drop");
        }

        // ======== Wasm helper functions ========

        private void EmitHelperFunctions()
        {
            // strlen helper
            Indent(";; --- strlen($ptr) -> length ---");
            Indent("(func $strlen (param $ptr i32) (result i32)");
            Indent("  (local $len i32)");
            Indent("  i32.const 0");
            Indent("  local.set $len");
            Indent("  block $done");
            Indent("    loop $scan");
            Indent("      local.get $ptr");
            Indent("      local.get $len");
            Indent("      i32.add");
            Indent("      i32.load8_u");
            Indent("      i32.eqz");
            Indent("      br_if $done");
            Indent("      local.get $len");
            Indent("      i32.const 1");
            Indent("      i32.add");
            Indent("      local.set $len");
            Indent("      br $scan");
            Indent("    end");
            Indent("  end");
            Indent("  local.get $len");
            Indent(")");
            Indent("");

            // itoa helper (simple, uses fixed buffer at 0x20000)
            Indent(";; --- itoa($n) -> ptr (writes to fixed buffer) ---");
            Indent("(func $itoa (param $n i32) (result i32)");
            Indent("  (local $buf i32) (local $neg i32)");
            Indent("  i32.const 0x20000");
            Indent("  local.set $buf");
            Indent("  i32.const 0");
            Indent("  local.set $neg");
            Indent("  local.get $n");
            Indent("  i32.const 0");
            Indent("  i32.lt_s");
            Indent("  if");
            Indent("    i32.const 1");
            Indent("    local.set $neg");
            Indent("    i32.const 0");
            Indent("    local.get $n");
            Indent("    i32.sub");
            Indent("    local.set $n");
            Indent("  end");
            Indent("  ;; digit loop — simplified: write at buf+31 and scan backward");
            Indent("  local.get $buf");
            Indent("  i32.const 31");
            Indent("  i32.add");
            Indent("  local.set $buf");
            Indent("  i32.const 0");
            Indent("  local.get $buf");
            Indent("  i32.store8  ;; NUL terminator");
            Indent("  block $done");
            Indent("    loop $digit");
            Indent("      local.get $buf");
            Indent("      i32.const 1");
            Indent("      i32.sub");
            Indent("      local.set $buf");
            Indent("      local.get $n");
            Indent("      i32.const 10");
            Indent("      i32.rem_u");
            Indent("      i32.const 48  ;; '0'");
            Indent("      i32.add");
            Indent("      local.get $buf");
            Indent("      i32.store8");
            Indent("      local.get $n");
            Indent("      i32.const 10");
            Indent("      i32.div_u");
            Indent("      local.tee $n");
            Indent("      i32.eqz");
            Indent("      br_if $done");
            Indent("      br $digit");
            Indent("    end");
            Indent("  end");
            Indent("  local.get $neg");
            Indent("  if");
            Indent("    local.get $buf");
            Indent("    i32.const 1");
            Indent("    i32.sub");
            Indent("    local.set $buf");
            Indent("    i32.const 45  ;; '-'");
            Indent("    local.get $buf");
            Indent("    i32.store8");
            Indent("  end");
            Indent("  local.get $buf");
            Indent(")");
            Indent("");
        }

        // ======== Operand utilities ========

        private string GetOperandExpr(Instruction instr, int idx)
        {
            if (idx >= instr.Operands.Count) return "i32.const 0";
            var op = instr.Operands[idx];
            return op.Type switch
            {
                OperandType.IMMEDIATE => $"i32.const {SafeToInt(op)}",
                OperandType.REGISTER =>
                    IsFloatOp(instr.Opcode)
                        ? $"local.get {GetFRegName(op)}"
                        : IsDoubleOp(instr.Opcode)
                            ? $"local.get $d{SafeToInt(op)}"
                            : $"local.get {GetRegName(op)}",
                OperandType.LABEL => $"i32.const {GetLabelAddr(op.Value?.ToString() ?? "0")}",
                OperandType.MEMORY => $"i32.const {SafeToInt(op)}",
                _ => $"i32.const 0 ;; unknown operand type {op.Type}"
            };
        }

        private string GetRegName(Operand op) => MapRegisterExpr($"R{SafeToInt(op)}");
        private string MapRegisterExpr(string regName)
        {
            if (regName.StartsWith("R") && int.TryParse(regName.Substring(1), out int rn))
            {
                // Map VML convention R12=BP, R13=SP to $bp/$sp locals
                if (rn == 12) return "$bp";
                if (rn == 13) return "$sp";
                return $"$r{rn}";
            }
            if (regName.StartsWith("%R") && int.TryParse(regName.Substring(2), out int rn2))
            {
                if (rn2 == 12) return "$bp";
                if (rn2 == 13) return "$sp";
                return $"$r{rn2}";
            }
            return $"${regName.ToLower()}";
        }

        private string GetFRegName(Operand op) => $"$f{SafeToInt(op)}";
        private string GetDRegName(Operand op) => $"$d{SafeToInt(op)}";

        private string GetLabelAddr(string label)
        {
            return LabelMap.TryGetValue(label, out var addr) ? addr : label;
        }

        private new string SanitizeLabel(string label)
        {
            return label.Replace(".", "_").Replace(":", "").Replace("-", "_");
        }

        private (int reg, int offset, bool neg) ParseRegOffset(string expr)
        {
            int reg = 0, offset = 0;
            bool neg = false;
            // Parse R12+4 or R12-8 or R14-12 etc.
            var parts = expr.Split('+', '-');
            if (parts.Length >= 1)
            {
                var regPart = parts[0].Trim();
                if (regPart.StartsWith("R") && int.TryParse(regPart.Substring(1), out int r))
                    reg = r;
            }
            if (expr.Contains('-'))
            {
                neg = true;
                var idx = expr.IndexOf('-');
                if (idx >= 0 && int.TryParse(expr.Substring(idx + 1).Trim(), out int off))
                    offset = off;
            }
            else if (expr.Contains('+'))
            {
                var idx = expr.IndexOf('+');
                if (idx >= 0 && int.TryParse(expr.Substring(idx + 1).Trim(), out int off))
                    offset = off;
            }
            return (reg, offset, neg);
        }

        private (string reg, int offset) ParseMemExpr(string expr)
        {
            if (expr.StartsWith("R"))
            {
                if (expr.Contains("+"))
                {
                    var parts = expr.Split('+');
                    return (MapRegisterExpr(parts[0].Trim()), int.TryParse(parts[1].Trim(), out int o) ? o : 0);
                }
                if (expr.Contains("-"))
                {
                    var idx = expr.IndexOf('-');
                    return (MapRegisterExpr(expr.Substring(0, idx).Trim()), -int.Parse(expr.Substring(idx + 1).Trim()));
                }
                return (MapRegisterExpr(expr.Trim()), 0);
            }
            // Check if it's a data label
            if (LabelMap.TryGetValue(expr, out var resolved))
                return ("$r0", int.TryParse(resolved, out int a) ? a : 0);
            return ("$r0", SafeToInt(expr));
        }

        private bool IsFloatOp(OpCode op) => op is
            OpCode.MOVEF or OpCode.FADD or OpCode.FSUB
            or OpCode.FMUL or OpCode.FDIV or OpCode.FNEG
            or OpCode.FCMP or OpCode.FPUSH or OpCode.FPOP
            or OpCode.I2F or OpCode.F2I or OpCode.F2D;

        private bool IsDoubleOp(OpCode op) => op is
            OpCode.MOVED or OpCode.DADD or OpCode.DSUB
            or OpCode.DMUL or OpCode.DDIV or OpCode.DNEG
            or OpCode.DCMP or OpCode.DPUSH or OpCode.DPOP
            or OpCode.I2D or OpCode.D2I or OpCode.D2F;

        private void Indent(string line)
        {
            Emit(new string(' ', _indent) + line);
        }
    }
}
