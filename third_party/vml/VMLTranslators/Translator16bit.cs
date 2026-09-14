using VMLAssembler;

namespace VMLTranslators
{
    /// <summary>
    /// 16-bit 架构转译器中间层 (MSP430, PIC24).
    /// 共用特性: 16位原生字长, 16位寄存器映射, 硬件乘法器.
    /// </summary>
    public abstract class Translator16bit : BaseTranslator
    {
        protected Translator16bit(VmlProgram vmlProgram) : base(vmlProgram) { }

        /// <summary>16位加法指令</summary>
        protected virtual string AddMnemonic => "add";
        /// <summary>16位减法指令</summary>
        protected virtual string SubMnemonic => "sub";
        /// <summary>16位 MOV 指令</summary>
        protected virtual string MovMnemonic => "mov";
        /// <summary>条件分支后缀 (EQ/NE/LT/GT/LE/GE) — 子类覆写</summary>
        protected virtual string JmpCondSuffix(OpCode op) => op switch
        {
            OpCode.JE => "eq", OpCode.JNE => "ne",
            OpCode.JL => "l", OpCode.JLE => "le",
            OpCode.JG => "g", OpCode.JGE => "ge",
            _ => ""
        };

        protected override string ByteDirective => ".word";
        protected override string WordDirective => ".long";

        /// <summary>16-bit 架构浮点指令（无硬件浮点，标记为未实现）</summary>
        protected override bool TranslateFloatInstruction(Instruction instr)
        {
            Emit($"        ; {instr.Opcode} — no HW float on 16-bit");
            return true;
        }

        // v1.66.31+: 16-bit 架构有真实进位标志, 覆写基类空注释
        protected override void EmitCLC() => Emit("        clrc");
        protected override void EmitSTC() => Emit("        setc");

        /// <summary>统一操作数格式化 (REGISTER→MapRegister, IMMEDIATE→#val, LABEL→val)</summary>
        protected string GetOp(Operand op)
        {
            if (op.Type == OperandType.REGISTER) return MapRegister((int)op.Value);
            if (op.Type == OperandType.IMMEDIATE) return $"#{op.Value}";
            if (op.Type == OperandType.LABEL) return op.Value?.ToString() ?? "0";
            return $"{op.Value}";
        }

        protected void EmitBinary16(Instruction instr)
        {
            string op = instr.Opcode switch
            {
                OpCode.ADD => AddMnemonic,
                OpCode.SUB => SubMnemonic,
                _ => AddMnemonic
            };
            var dst = MapRegister(SafeToInt(instr.Operands[0]));
            var src = GetOperandValue(instr.Operands[2]);
            Emit($"        {op} {dst}, {src}");
        }
    }
}
