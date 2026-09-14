using VMLAssembler;

namespace VMLTranslators
{
    /// <summary>
    /// 32-bit 架构转译器中间层 (ARM-CM, x86, MIPS, RISC-V, 68000, PowerPC, SPARC).
    /// 共用特性: 32位原生字长, 硬件浮点, 3-operand 指令格式.
    /// </summary>
    public abstract class Translator32bit : BaseTranslator
    {
        protected Translator32bit(VmlProgram vmlProgram) : base(vmlProgram) { }

        /// <summary>32位加法指令 (默认 x86: ADD)</summary>
        protected virtual string Add32 => "ADD";
        /// <summary>32位减法指令</summary>
        protected virtual string Sub32 => "SUB";
        /// <summary>32位乘法指令</summary>
        protected virtual string Mul32 => "MUL";
        /// <summary>32位 MOV 指令</summary>
        protected virtual string Mov32 => "MOV";
        /// <summary>32位加载立即数指令</summary>
        protected virtual string LdImm32 => "MOV";
        /// <summary>浮点加法 (默认 x86: FADD)</summary>
        protected virtual string FAdd => "FADD";
        /// <summary>浮点减法</summary>
        protected virtual string FSub => "FSUB";
        /// <summary>浮点乘法</summary>
        protected virtual string FMul => "FMUL";
        /// <summary>浮点除法</summary>
        protected virtual string FDiv => "FDIV";
        /// <summary>分支前缀 (默认 x86: J)</summary>
        protected virtual string BranchPrefix => "J";
        /// <summary>比较指令</summary>
        protected virtual string Cmp32 => "CMP";

        /// <summary>浮点指令翻译: 使用硬件浮点指令</summary>
        protected override bool TranslateFloatInstruction(Instruction instr)
        {
            switch (instr.Opcode)
            {
                case OpCode.FADD: EmitBinFloat(FAdd); return true;
                case OpCode.FSUB: EmitBinFloat(FSub); return true;
                case OpCode.FMUL: EmitBinFloat(FMul); return true;
                case OpCode.FDIV: EmitBinFloat(FDiv); return true;
                case OpCode.FNEG: EmitUnaryFloat("NEG.F"); return true;
                case OpCode.FCMP: EmitCmpFloat(); return true;
                default: return false;
            }
        }

        protected void EmitBinFloat(string op)
        {
            var dst = MapRegister(SafeToInt(_currentInstr.Operands[0]));
            var s1  = MapRegister(SafeToInt(_currentInstr.Operands[1]));
            var s2  = GetOperandValue(_currentInstr.Operands[2]);
            Emit($"        {op} {dst}, {s1}, {s2}");
        }

        protected void EmitUnaryFloat(string op)
        {
            var dst = MapRegister(SafeToInt(_currentInstr.Operands[0]));
            var src = MapRegister(SafeToInt(_currentInstr.Operands[1]));
            Emit($"        {op} {dst}, {src}");
        }

        protected void EmitCmpFloat()
        {
            var s1 = MapRegister(SafeToInt(_currentInstr.Operands[0]));
            var s2 = MapRegister(SafeToInt(_currentInstr.Operands[1]));
            Emit($"        {Cmp32} {s1}, {s2}");
        }

        /// <summary>当前正在翻译的指令（由 TranslateInstruction 设置）</summary>
        protected Instruction _currentInstr = null!;

        /// <summary>翻译二元算术运算: dst = src1 op src2</summary>
        protected void EmitBinary32(Instruction instr)
        {
            _currentInstr = instr;
            string op = instr.Opcode switch
            {
                OpCode.ADD => Add32,
                OpCode.SUB => Sub32,
                OpCode.MUL => Mul32,
                _ => Add32
            };
            var dst = MapRegister(SafeToInt(instr.Operands[0]));
            var s1  = MapRegister(SafeToInt(instr.Operands[1]));
            var s2  = GetOperandValue(instr.Operands[2]);
            Emit($"        {op} {dst}, {s1}, {s2}");
        }

        /// <summary>发出条件分支</summary>
        protected void EmitCondBranch(Instruction instr, string cond)
        {
            var target = instr.Operands[0].Value?.ToString() ?? ".";
            Emit($"        {BranchPrefix}{cond} {target}");
        }

        // EmitCLC/EmitSTC → uses BaseTranslator (override in x86 for real clc/stc)
    }
}
