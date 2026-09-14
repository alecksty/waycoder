using VMLAssembler;

namespace VMLTranslators
{
    /// <summary>
    /// 8-bit 架构转译器中间层 (6502, Z80, 8051, AVR, PIC).
    /// 共用特性: 4字节虚拟寄存器存储, 软浮点库调用, $ 十六进制前缀.
    /// </summary>
    public abstract class Translator8bit : BaseTranslator
    {
        protected Translator8bit(VmlProgram vmlProgram) : base(vmlProgram) { }

        /// <summary>子程序调用指令 (JSR / CALL / RCALL / LCALL)</summary>
        protected abstract string CallMnemonic { get; }

        protected override string HexPrefix => "$";

        /// <summary>浮点库标签命名格式: "FP_{OP}" (可覆写, 如 AVR 用 "__{opcode}")</summary>
        protected virtual string FloatLibLabel(OpCode op) => op switch
        {
            OpCode.FADD => "FP_ADD", OpCode.FSUB => "FP_SUB",
            OpCode.FMUL => "FP_MUL", OpCode.FDIV => "FP_DIV",
            OpCode.FNEG => "FP_NEG", OpCode.FCMP => "FP_CMP",
            OpCode.I2F  => "FP_I2F",  OpCode.F2I  => "FP_F2I",
            OpCode.F2D  => "FP_F2D",  OpCode.D2F  => "FP_D2F",
            OpCode.MOVEF => "FP_MOVE",
            OpCode.FPUSH => "FP_PUSH", OpCode.FPOP => "FP_POP",
            _ => $"FP_{op}"
        };

        /// <summary>浮点指令统一翻译: 使用软件浮点库</summary>
        protected override bool TranslateFloatInstruction(Instruction instr)
        {
            if (FloatLibLabel(instr.Opcode) != null)
            {
                EmitCall(FloatLibLabel(instr.Opcode));
                return true;
            }
            return false;
        }

        protected void EmitCall(string label)
        {
            Emit($"        {CallMnemonic} {label}");
        }
    }
}
