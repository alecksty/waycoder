using System.Linq;
using VMLAssembler;

namespace VMLTranslators
{
    /// <summary>
    /// 转译器基类
    /// </summary>
    public abstract class BaseTranslator
    {
        public virtual string ARCH_NAME => "base";
        protected VmlProgram VmlProgram { get; set; }
        protected List<string> Output { get; set; }
        protected Dictionary<string, string> LabelMap { get; set; }

        /// <summary>ROM 基地址 (默认 0x08000000)</summary>
        public uint RomBase { get; set; } = 0x08000000;

        /// <summary>RAM 基地址 (默认 0x20000000)</summary>
        public uint RamBase { get; set; } = 0x20000000;

        /// <summary>RAM 大小 (默认 64KB)</summary>
        public uint RamSize { get; set; } = 0x10000;

        /// <summary>跳过头部 (BIOS 已提供向量表+Reset_Handler 时)</summary>
        public bool SkipHeader { get; set; } = false;

        /// <summary>栈顶 = RamBase + RamSize - 4</summary>
        public uint StackTop => RamBase + RamSize - 4;

        public BaseTranslator(VmlProgram vmlProgram)
        {
            VmlProgram = vmlProgram;
            Output = new List<string>();
            LabelMap = new Dictionary<string, string>();
        }

        public TranslatedProgram Translate()
        {
            Output.Clear();
            if (!SkipHeader) EmitHeader();
            EmitDataSection();
            EmitCode();
            if (!SkipHeader) EmitFooter();

            return new TranslatedProgram(
                string.Join("\n", Output),
                ARCH_NAME,
                new Dictionary<string, object> { { "instruction_count", VmlProgram.Instructions.Count } }
            );
        }

        protected void Emit(string line = "")
        {
            Output.Add(line);
        }

        protected virtual void EmitHeader() { }
        protected virtual void EmitFooter() { }

        /// <summary>字节数据指令 (如 .byte, .db, .dc.b)</summary>
        protected virtual string ByteDirective => ".byte";
        /// <summary>字数据指令 (如 .word, .dw, .dc.w)</summary>
        protected virtual string WordDirective => ".word";
        /// <summary>十六进制前缀 (如 0x, $)</summary>
        protected virtual string HexPrefix => "0x";
        /// <summary>字节值格式化</summary>
        protected virtual string FmtByte(int v) => $"{HexPrefix}{v:X2}";
        /// <summary>字值格式化</summary>
        protected virtual string FmtWord(int v) => $"{HexPrefix}{v:X4}";

        protected virtual void EmitDataSection()
        {
            if (VmlProgram.DataSection.Count > 0)
            {
                Emit("");
                Emit("        ; Data Section");
                foreach (var kvp in VmlProgram.DataSection)
                {
                    string name = kvp.Key;
                    object value = kvp.Value;
                    if (value is DataString ds)
                    {
                        Emit($"{name}:");
                        foreach (char c in ds.Value)
                            Emit($"        {ByteDirective} {FmtByte((int)c)}");
                    }
                    else if (value is string strValue)
                    {
                        Emit($"{name}:");
                        foreach (char c in strValue)
                            Emit($"        {ByteDirective} {FmtByte((int)c)}");
                    }
                    else if (value is System.Collections.IList listValue)
                    {
                        Emit($"{name}:");
                        foreach (var item in listValue)
                            if (int.TryParse(item.ToString(), out int iv))
                                Emit($"        {ByteDirective} {FmtByte(iv)}");
                    }
                    else if (int.TryParse(value.ToString(), out int intValue))
                        Emit($"{name}: {WordDirective} {FmtWord(intValue)}");
                }
                Emit("");
            }
        }

        /// <summary>规范化并翻译指令：2-operand→3-operand + TryTranslateCommonOpcode + TranslateInstruction</summary>
        protected void NormalizeAndTranslate(Instruction instr)
        {
            var normalized = NormalizeBinaryOp(instr);
            try
            {
                if (!TryTranslateCommonOpcode(normalized))
                    TranslateInstruction(normalized);
            }
            catch (Exception ex) { Emit($"; ERROR: {ex.Message}"); }
        }

        /// <summary>
        /// 翻译每个指令前先调用此方法，处理所有转译器共通的 opcode。
        /// 返回 true 表示已处理，不再调用 TranslateInstruction。
        /// </summary>
        protected virtual bool TryTranslateCommonOpcode(Instruction instr)
        {
            switch (instr.Opcode)
            {
                case OpCode.ASM:
                    Emit($"        {instr.Operands[0].Value}");
                    return true;
                case OpCode.CHIPASM:
                    if (instr.Operands.Count >= 2 && instr.Operands[0].Value is string arch
                        && instr.Operands[1].Value is string code)
                    {
                        if (string.Equals(arch, ARCH_NAME, StringComparison.OrdinalIgnoreCase))
                            Emit($"        {code}");
                        else
                            Emit($"        ; chipasm({arch}): {code}");
                    }
                    return true;
                case OpCode.CLC: EmitCLC(); return true;
                case OpCode.STC: EmitSTC(); return true;
                case OpCode.CLI:
                case OpCode.STI:
                case OpCode.INT:
                case OpCode.IRET:
                    return TranslateInterrupt(instr);

                // 64-bit 全部操作 — 统一软件库调用 (v1.66.28+, 扩展 v1.66.31+)
                case OpCode.ANDL:
                case OpCode.ORL:
                case OpCode.XORL:
                case OpCode.NOTL:
                case OpCode.SHLL:
                case OpCode.SHRL:
                case OpCode.ADDL:
                case OpCode.SUBL:
                case OpCode.MULL:
                case OpCode.DIVL:
                case OpCode.MODL:
                case OpCode.NEGL:
                case OpCode.CMPL:
                case OpCode.I2L:
                case OpCode.L2I:
                case OpCode.F2L:
                case OpCode.L2F:
                case OpCode.D2L:
                case OpCode.L2D:
                case OpCode.MOVEL:
                case OpCode.PUSHL:
                case OpCode.POPL:
                    EmitCall(instr);
                    return true;
            }
            return false;
        }

        /// <summary>生成软件库调用: call/bl/jsr __opcode</summary>
        protected virtual void EmitCall(Instruction instr)
        {
            Emit($"        call __{instr.Opcode.ToString().ToLower()}");
        }

        /// <summary>CLC — 清除进位标志。</summary>
        protected virtual void EmitCLC() => Emit($"        ; CLC — no flags on {ARCH_NAME}");
        /// <summary>STC — 设置进位标志。</summary>
        protected virtual void EmitSTC() => Emit($"        ; STC — no flags on {ARCH_NAME}");

        /// <summary>转义字符串用于 .asciz/.asciiz 数据指令（MIPS/RISC-V/ARM-CM 共用）</summary>
        protected static string EscapeString(string s) => s
            .Replace("\\", "\\\\").Replace("\"", "\\\"")
            .Replace("\n", "\\n").Replace("\r", "\\r").Replace("\t", "\\t");

        protected virtual void EmitCode()
        {
            // Find entry point address (skip library init code)
            int entryAddr = -1;
            string entryName = VmlProgram.EntryPoint ?? "main";

            // 检查地址系统是否可靠：如果大部分指令的 Address 为 0，
            // 说明编译器没有正确设置地址，此时应处理全部指令。
            int zeroAddrCount = VmlProgram.Instructions?.Count(i => i.Address == 0) ?? 0;
            int totalCount = VmlProgram.Instructions?.Count ?? 0;
            bool useAddrSkip = zeroAddrCount < totalCount / 2; // 少于一半是零地址才使用地址跳过

            if (useAddrSkip && VmlProgram.Labels != null && VmlProgram.Labels.TryGetValue(entryName, out entryAddr))
            {
                var labelAtAddr = new Dictionary<int, string>();
                foreach (var kv in VmlProgram.Labels)
                    if (kv.Value >= entryAddr)
                        labelAtAddr[kv.Value] = kv.Key;

                foreach (var instr in VmlProgram.Instructions)
                {
                    if (instr.Address < entryAddr) continue;
                    if (labelAtAddr.TryGetValue(instr.Address, out var lbl))
                        Emit($"{lbl}:");
                    if (!string.IsNullOrEmpty(instr.Label))
                        Emit($"{instr.Label}:");
                    NormalizeAndTranslate(instr);
                }
            }
            else
            {
                var labelAtAddr = new Dictionary<int, string>();
                if (VmlProgram.Labels != null)
                    foreach (var kv in VmlProgram.Labels)
                        labelAtAddr[kv.Value] = kv.Key;

                foreach (var instr in VmlProgram.Instructions)
                {
                    if (labelAtAddr.TryGetValue(instr.Address, out var lbl))
                        Emit($"{lbl}:");
                    if (!string.IsNullOrEmpty(instr.Label))
                        Emit($"{instr.Label}:");
                    NormalizeAndTranslate(instr);
                }
            }
        }

        protected abstract void TranslateInstruction(Instruction instr);

        /// <summary>发出未实现指令的注释（子类 TranslateInstruction 的 default 分支可调用此方法）</summary>
        protected virtual void EmitUnimplemented(Instruction instr)
            => Emit($"        ; UNIMPLEMENTED: {instr.Opcode} {string.Join(", ", instr.Operands.Select(o => o.Value))}");

        /// <summary>浮点指令统一翻译入口。子类必须覆写以支持浮点，基类默认抛出异常。</summary>
        protected virtual bool TranslateFloatInstruction(Instruction instr)
        {
            switch (instr.Opcode)
            {
                case OpCode.FADD:
                case OpCode.FSUB:
                case OpCode.FMUL:
                case OpCode.FDIV:
                case OpCode.FNEG:
                case OpCode.FCMP:
                case OpCode.I2F:
                case OpCode.F2I:
                case OpCode.F2D:
                case OpCode.D2F:
                case OpCode.MOVEF:
                case OpCode.MOVED:
                case OpCode.MOVEL:
                case OpCode.FPUSH:
                case OpCode.FPOP:
                    throw new NotSupportedException(
                        $"浮点指令 {instr.Opcode} 在当前翻译器中未实现。请在子类中覆写 TranslateFloatInstruction 方法。");
                default: return false;
            }
        }

        /// <summary>Safely convert operand value to int. Returns 0 for non-numeric values like GCC %0 placeholders.</summary>
        protected static int SafeToInt(Operand op) => SafeToInt(op.Value);
        protected static int SafeToInt(object val)
        {
            if (val is int i) return i;
            var s = val?.ToString() ?? "0";
            if (s.StartsWith("%")) return 0;
            if (int.TryParse(s, out int r)) return r;
            return 0;
        }

        protected static int EvaluateExpression(string expr)
        {
            if (int.TryParse(expr, out int result))
                return result;

            // 处理简单表达式 "12+12", "14-4"
            int opIndex = -1;
            char op = '+';
            for (int i = 1; i < expr.Length; i++)
            {
                if (expr[i] == '+' || expr[i] == '-')
                {
                    opIndex = i;
                    op = expr[i];
                    break;
                }
            }

            if (opIndex > 0)
            {
                string left = expr.Substring(0, opIndex);
                string right = expr.Substring(opIndex + 1);
                if (int.TryParse(left, out int leftVal) && int.TryParse(right, out int rightVal))
                    return op == '+' ? leftVal + rightVal : leftVal - rightVal;
            }

            return 0;
        }

        /// <summary>立即数是否需要 # 前缀 (x86/MIPS/RISC-V 覆写为 false)</summary>
        protected virtual bool UseImmediateHash => true;

        protected virtual string GetOperandValue(Operand operand)
        {
            switch (operand.Type)
            {
                case OperandType.IMMEDIATE:
                {
                    var val = EvaluateExpression(operand.Value?.ToString() ?? "0");
                    return UseImmediateHash ? $"#{val}" : val.ToString();
                }
                case OperandType.REGISTER:
                    return MapRegister((int)operand.Value);
                case OperandType.MEMORY:
                    return $"[{operand.Value}]";
                default:
                    return operand.Value.ToString();
            }
        }

        protected virtual string MapRegister(int regNum)
        {
            return $"R{regNum}";
        }

        /// <summary>
        /// 规范化二元运算指令的操作数。
        /// CCompiler 生成 2-operand 格式 (dst, src)，手动 VML 使用 3-operand 格式 (dst, src1, src2)。
        /// 此方法统一为 3-operand：2-operand 时 src1 = dst。
        /// </summary>
        protected static readonly HashSet<OpCode> BinaryOps = new HashSet<OpCode>
        {
            OpCode.ADD, OpCode.SUB, OpCode.MUL, OpCode.DIV, OpCode.MOD,
            OpCode.AND, OpCode.OR, OpCode.XOR,
            OpCode.SHL, OpCode.SHR,
            OpCode.FADD, OpCode.FSUB, OpCode.FMUL, OpCode.FDIV,
            OpCode.I2F, OpCode.F2I, OpCode.F2D, OpCode.D2F
        };

        protected Instruction NormalizeBinaryOp(Instruction instr)
        {
            if (instr.Operands.Count == 2 && BinaryOps.Contains(instr.Opcode))
            {
                return new Instruction(instr.Opcode, new List<Operand>
                {
                    instr.Operands[0],  // dst
                    instr.Operands[0],  // src1 = dst (2-operand 模式)
                    instr.Operands[1]   // src2 = src
                });
            }
            return instr;
        }

        // ═══════════════════════════════════════════════════
        // 中断系统 — 架构相关虚方法（子类覆写）
        // ═══════════════════════════════════════════════════

        /// <summary>原生软件中断指令，如 x86: int N, ARM: svc #N</summary>
        protected virtual string NativeInt(int vector) => $"int {vector}";

        /// <summary>原生中断返回指令</summary>
        protected virtual string NativeIret() => "iret";

        /// <summary>原生关中断指令</summary>
        protected virtual string NativeCli() => "cli";

        /// <summary>原生开中断指令</summary>
        protected virtual string NativeSti() => "sti";

        /// <summary>
        /// 中断时需要保存的原生寄存器名列表（压栈顺序）。
        /// 子类必须覆写 — 基类默认抛出以强制子类显式定义架构特定寄存器。
        /// </summary>
        protected virtual string[] ContextRegNames() =>
            throw new NotSupportedException(
                $"当前翻译器未覆写 ContextRegNames()。请返回架构特定的寄存器名列表，例如：new[] {{\"eax\", \"ebx\", \"ecx\", \"edx\", \"esi\", \"edi\", \"ebp\"}}。");

        /// <summary>保存中断上下文 — 压栈通用寄存器 + 标志</summary>
        protected virtual void EmitSaveContext()
        {
            foreach (var r in ContextRegNames())
                Emit($"    push {r}");
            Emit("    pushf");
        }

        /// <summary>恢复中断上下文 — 弹栈标志 + 通用寄存器（逆序）</summary>
        protected virtual void EmitRestoreContext()
        {
            Emit("    popf");
            var regs = ContextRegNames();
            for (int i = regs.Length - 1; i >= 0; i--)
                Emit($"    pop {regs[i]}");
        }

        /// <summary>统一的中断指令翻译入口。</summary>
        protected bool TranslateInterrupt(Instruction instr)
        {
            switch (instr.Opcode)
            {
                case OpCode.INT:
                    EmitSaveContext();
                    EmitNativeInt(SafeToInt(instr.Operands[0]));
                    return true;
                case OpCode.IRET:
                    if (!string.IsNullOrEmpty(NativeIret()))
                        Emit($"    {NativeIret()}");
                    EmitRestoreContext();
                    return true;
                case OpCode.CLI:
                    EmitNativeCli();
                    return true;
                case OpCode.STI:
                    EmitNativeSti();
                    return true;
            }
            return false;
        }

        /// <summary>发出原生 INT 指令（可覆写以支持多行前缀）。</summary>
        protected virtual void EmitNativeInt(int vector)
            => Emit($"    {NativeInt(vector)}");

        /// <summary>发出原生 CLI 指令（可覆写以支持多行）。</summary>
        protected virtual void EmitNativeCli()
        { if (!string.IsNullOrEmpty(NativeCli())) Emit($"    {NativeCli()}"); }

        /// <summary>发出原生 STI 指令（可覆写以支持多行）。</summary>
        protected virtual void EmitNativeSti()
        { if (!string.IsNullOrEmpty(NativeSti())) Emit($"    {NativeSti()}"); }
    }
}
