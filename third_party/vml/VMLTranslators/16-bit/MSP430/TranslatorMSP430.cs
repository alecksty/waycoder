using VMLAssembler;

namespace VMLTranslators
{
    /// <summary>
    /// MSP430 (Texas Instruments) 16-bit MCU 转译器
    /// </summary>
    public class TranslatorMSP430 : Translator16bit
    {
        public override string ARCH_NAME => "MSP430";
        protected override string AddMnemonic => "add.w";
        protected override string SubMnemonic => "sub.w";
        protected override string MovMnemonic => "mov.w";

        // MSP430 中断上下文
        protected override string[] ContextRegNames() => new[] { "r4","r5","r6","r7","r8","r9","r10","r11","r12","r13","r14","r15" };
        protected override string NativeIret() => "reti";
        protected override string NativeCli() => "dint";
        protected override string NativeSti() => "eint";
        protected override void EmitNativeInt(int vector)
        {
            Emit($"        ; INT #{vector} - MSP430 software interrupt");
            Emit("        call #__sw_int");
        }
        protected override void EmitSaveContext()
        {
            foreach (var r in ContextRegNames())
                Emit($"        push {r}");
        }
        protected override void EmitRestoreContext()
        {
            var regs = ContextRegNames();
            for (int i = regs.Length - 1; i >= 0; i--)
                Emit($"        pop {regs[i]}");
        }

        // MSP430 registers: R0=PC, R1=SP, R2=SR/CG1, R3=CG2, R4-R15=General
        private readonly Dictionary<int, string> REGISTER_MAP = new()
        {
            { 0, "R4" },  { 1, "R5" },  { 2, "R6" },  { 3, "R7" },
            { 4, "R8" },  { 5, "R9" },  { 6, "R10" }, { 7, "R11" },
            { 8, "R12" }, { 9, "R13" }, { 10, "R14" },{ 11, "R15" },
            { 12, "R4" }, { 13, "R1" }, { 14, "R4" }, { 15, "R4" },
        };

        public TranslatorMSP430(VmlProgram prog) : base(prog) { }

        protected override void EmitHeader()
        {
            Emit("; MSP430 Assembly - Translated from VML");
            Emit("; Target: MSP430F5438A / MSP430G2553");
            Emit("; Assembler: msp430-elf-gcc / naken_asm");
            Emit("");
            Emit("        .cdecls C,LIST,\"msp430.h\"");
            Emit("        .text");
            Emit("        .global _start");
            Emit("");
            Emit("_start:");
            Emit("        mov.w   #0x5C00, SP      ; Initialize stack pointer");
            Emit("");
        }

        protected override void EmitDataSection()
        {
            if (VmlProgram.DataSection.Count <= 0) return;
            Emit("");
            Emit("        .data");
            Emit("        .align 2");
            foreach (var kvp in VmlProgram.DataSection)
            {
                string name = kvp.Key;
                if (kvp.Value is string s)
                    Emit($"{name}: .byte {s.Length + 1}");
                else if (int.TryParse(kvp.Value?.ToString(), out int v))
                    Emit($"{name}: .word {v}");
            }
        }

        protected override void EmitCode()
        {
            Emit("");
            Emit("        .text");
            Emit("        .align 2");
            base.EmitCode();
            Emit("        jmp     $               ; Infinite loop");
        }

        protected override void TranslateInstruction(Instruction instr)
        {
            switch (instr.Opcode)
            {
                                                case OpCode.ADD:
                    if (instr.Operands.Count >= 2)
                    {
                        string dst = MapRegister((int)instr.Operands[0].Value);
                        var src = instr.Operands[1];
                        if (src.Type == OperandType.IMMEDIATE)
                            Emit($"        add.w   #{src.Value}, {dst}");
                        else if (src.Type == OperandType.REGISTER)
                            Emit($"        add.w   {MapRegister((int)src.Value)}, {dst}");
                        else
                            Emit($"        add.w   {src.Value}, {dst}");
                    }
                    break;
                case OpCode.SUB:
                    if (instr.Operands.Count >= 2)
                    {
                        string dst = MapRegister((int)instr.Operands[0].Value);
                        var src = instr.Operands[1];
                        if (src.Type == OperandType.IMMEDIATE)
                            Emit($"        sub.w   #{src.Value}, {dst}");
                        else if (src.Type == OperandType.REGISTER)
                            Emit($"        sub.w   {MapRegister((int)src.Value)}, {dst}");
                        else
                            Emit($"        sub.w   {src.Value}, {dst}");
                    }
                    break;
                case OpCode.MUL:
                    if (instr.Operands.Count >= 2)
                        Emit($"        mpy.w   {MapRegister((int)instr.Operands[1].Value)}, {MapRegister((int)instr.Operands[0].Value)}");
                    break;
                case OpCode.MOVE:
                    if (instr.Operands.Count >= 2)
                        Emit($"        mov.w   {GetOp(instr.Operands[1])}, {MapRegister((int)instr.Operands[0].Value)}");
                    break;
                case OpCode.PUSH:
                    Emit($"        push.w  {GetOp(instr.Operands[0])}");
                    break;
                case OpCode.POP:
                    Emit($"        pop.w   {MapRegister((int)instr.Operands[0].Value)}");
                    break;
                case OpCode.CMP:
                    if (instr.Operands.Count >= 2)
                        Emit($"        cmp.w   {GetOp(instr.Operands[1])}, {GetOp(instr.Operands[0])}");
                    break;
                case OpCode.JMP:
                    Emit($"        jmp     {instr.Operands[0].Value}");
                    break;
                case OpCode.JZ:
                case OpCode.JE:
                    Emit($"        jeq     {instr.Operands[0].Value}");
                    break;
                case OpCode.JNZ:
                case OpCode.JNE:
                    Emit($"        jne     {instr.Operands[0].Value}");
                    break;
                case OpCode.JL:
                    Emit($"        jl      {instr.Operands[0].Value}");
                    break;
                case OpCode.JGE:
                    Emit($"        jge     {instr.Operands[0].Value}");
                    break;
                case OpCode.JG:
                    Emit($"        jg      {instr.Operands[0].Value}");
                    break;
                case OpCode.JLE:
                    Emit($"        jle     {instr.Operands[0].Value}");
                    break;
                case OpCode.CALL:
                    Emit($"        call    #{instr.Operands[0].Value}");
                    break;
                case OpCode.RET:
                    Emit($"        ret");
                    break;
                // case OpCode.RET_VAL:  // unimplemented
                case OpCode.AND:
                    Emit($"        and.w   {GetOp(instr.Operands[1])}, {MapRegister((int)instr.Operands[0].Value)}");
                    break;
                case OpCode.OR:
                    Emit($"        or.w    {GetOp(instr.Operands[1])}, {MapRegister((int)instr.Operands[0].Value)}");
                    break;
                case OpCode.XOR:
                    Emit($"        xor.w   {GetOp(instr.Operands[1])}, {MapRegister((int)instr.Operands[0].Value)}");
                    break;
                case OpCode.NOT:
                    Emit($"        inv.w   {MapRegister((int)instr.Operands[0].Value)}");
                    break;
                case OpCode.DIV:
                    Emit($"        call    #__div32     ; DIV {GetOp(instr.Operands[1])}, {MapRegister((int)instr.Operands[0].Value)}");
                    break;
                case OpCode.MOD:
                    Emit($"        call    #__mod32     ; MOD {GetOp(instr.Operands[1])}, {MapRegister((int)instr.Operands[0].Value)}");
                    break;
                case OpCode.SHL:
                    if (instr.Operands.Count >= 3 && instr.Operands[2].Type == OperandType.IMMEDIATE)
                        Emit($"        rla.w   {MapRegister((int)instr.Operands[0].Value)}");
                    break;
                case OpCode.SHR:
                    if (instr.Operands.Count >= 3 && instr.Operands[2].Type == OperandType.IMMEDIATE)
                        Emit($"        rra.w   {MapRegister((int)instr.Operands[0].Value)}");
                    break;
                case OpCode.SHLV:
                    Emit($"        call    #__shl32     ; SHLV {GetOp(instr.Operands[0])}, {GetOp(instr.Operands[1])}, {GetOp(instr.Operands[2])}");
                    break;
                case OpCode.SHRV:
                    Emit($"        call    #__shr32     ; SHRV {GetOp(instr.Operands[0])}, {GetOp(instr.Operands[1])}, {GetOp(instr.Operands[2])}");
                    break;
                case OpCode.TEST:
                    if (instr.Operands.Count >= 2)
                        Emit($"        tst.w   {GetOp(instr.Operands[0])}");
                    break;
                case OpCode.INC:
                    Emit($"        inc.w   {MapRegister((int)instr.Operands[0].Value)}");
                    break;
                case OpCode.DEC:
                    Emit($"        dec.w   {MapRegister((int)instr.Operands[0].Value)}");
                    break;
                case OpCode.NEG:
                    Emit($"        sub.w   #0, {MapRegister((int)instr.Operands[0].Value)}");
                    break;
                case OpCode.SYSCALL:
                    Emit($"        call #__syscall_{instr.Operands[0].Value}");
                    break;
                case OpCode.HALT:
                    Emit($"        jmp     $");
                    break;
                case OpCode.NOP:
                    Emit($"        nop");
                    break;
                case OpCode.STI:
                    // v1.66.31 fix: CLI/STI/INT/IRET handled by BaseTranslator, add explicit break
                    break;
                case OpCode.THROW:
                    Emit("        ; THROW - MSP430 exception");
                    Emit("        jmp     $");
                    break;
                case OpCode.CATCH:
                    break;
                case OpCode.ENDCATCH:
                    break;
                case OpCode.ZERO:
                    Emit($"        mov.w   #0, {MapRegister((int)instr.Operands[0].Value)}");
                    break;
                                                                                case OpCode.MOVEB:
                    Emit($"        mov.b   {GetOp(instr.Operands[1])}, {MapRegister((int)instr.Operands[0].Value)}");
                    break;
                case OpCode.MOVEH:
                    Emit($"        mov.w   {GetOp(instr.Operands[1])}, {MapRegister((int)instr.Operands[0].Value)}");
                    break;
                case OpCode.PUSHB:
                    Emit($"        push.b  {GetOp(instr.Operands[0])}");
                    break;
                case OpCode.PUSHH:
                    Emit($"        push.w  {GetOp(instr.Operands[0])}");
                    break;
                case OpCode.POPB:
                    Emit($"        pop.b   {MapRegister((int)instr.Operands[0].Value)}");
                    break;
                case OpCode.POPH:
                    Emit($"        pop.w   {MapRegister((int)instr.Operands[0].Value)}");
                    break;
                case OpCode.ENTER:
                    Emit($"        ; ENTER frame_size={instr.Operands[1].Value}");
                    Emit($"        push.w  {MapRegister(14)}");
                    Emit($"        mov.w   SP, {MapRegister(14)}");
                    Emit($"        sub.w   #{instr.Operands[0].Value}, SP");
                    break;
                case OpCode.LEAVE:
                    Emit($"        mov.w   {MapRegister(14)}, SP");
                    Emit($"        pop.w   {MapRegister(14)}");
                    break;
                // CLC/STC handled by Translator16bit.EmitCLC/EmitSTC (v1.66.31+)
                case OpCode.BREAK:
                    Emit("        ; BREAK - debug breakpoint");
                    break;
                // OpCode.CHIPASM handled by BaseTranslator.TryTranslateCommonOpcode
                case OpCode.LABEL:
                    break;
                // ── 浮点 / 64-bit / long 指令 → MSP430 软件库调用 (v1.66.31+) ──
                case OpCode.FADD: Emit("        call #__addsf3"); break;
                case OpCode.FSUB: Emit("        call #__subsf3"); break;
                case OpCode.FMUL: Emit("        call #__mulsf3"); break;
                case OpCode.FDIV: Emit("        call #__divsf3"); break;
                case OpCode.FNEG: Emit("        call #__negsf2"); break;
                case OpCode.FCMP: Emit("        call #__lesf2"); break;
                case OpCode.I2F:  Emit("        call #__floatsisf"); break;
                case OpCode.F2I:  Emit("        call #__fixsfsi"); break;
                case OpCode.F2D:  Emit("        call #__extendsfdf2"); break;
                case OpCode.D2F:  Emit("        call #__truncdfsf2"); break;
                case OpCode.I2D:  Emit("        call #__floatsidf"); break;
                case OpCode.D2I:  Emit("        call #__fixdfsi"); break;
                // ── 浮点/64位数据移动 (v1.66.31+: 寄存器对操作) ──
                case OpCode.MOVEF:
                {
                    int fd = SafeToInt(instr.Operands[0]), fs = SafeToInt(instr.Operands[1]);
                    Emit($"        mov.w   {MapRegister(fs)}, {MapRegister(fd)}");
                    Emit($"        mov.w   {MapRegister(fs+1)}, {MapRegister(fd+1)}");
                    break;
                }
                case OpCode.MOVED:
                case OpCode.MOVEL:
                {
                    int dd = SafeToInt(instr.Operands[0]), ds = SafeToInt(instr.Operands[1]);
                    Emit($"        mov.w   {MapRegister(ds)}, {MapRegister(dd)}");
                    Emit($"        mov.w   {MapRegister(ds+1)}, {MapRegister(dd+1)}");
                    Emit($"        mov.w   {MapRegister(ds+2)}, {MapRegister(dd+2)}");
                    Emit($"        mov.w   {MapRegister(ds+3)}, {MapRegister(dd+3)}");
                    break;
                }
                case OpCode.FPUSH:
                {
                    int f = SafeToInt(instr.Operands[0]);
                    Emit($"        push.w  {MapRegister(f+1)}");
                    Emit($"        push.w  {MapRegister(f)}");
                    break;
                }
                case OpCode.FPOP:
                {
                    int f = SafeToInt(instr.Operands[0]);
                    Emit($"        pop.w   {MapRegister(f)}");
                    Emit($"        pop.w   {MapRegister(f+1)}");
                    break;
                }
                case OpCode.DPUSH:
                {
                    int d = SafeToInt(instr.Operands[0]);
                    Emit($"        push.w  {MapRegister(d+3)}");
                    Emit($"        push.w  {MapRegister(d+2)}");
                    Emit($"        push.w  {MapRegister(d+1)}");
                    Emit($"        push.w  {MapRegister(d)}");
                    break;
                }
                case OpCode.DPOP:
                {
                    int d = SafeToInt(instr.Operands[0]);
                    Emit($"        pop.w   {MapRegister(d)}");
                    Emit($"        pop.w   {MapRegister(d+1)}");
                    Emit($"        pop.w   {MapRegister(d+2)}");
                    Emit($"        pop.w   {MapRegister(d+3)}");
                    break;
                }
                case OpCode.DADD: Emit("        call #__adddf3"); break;
                case OpCode.DSUB: Emit("        call #__subdf3"); break;
                case OpCode.DMUL: Emit("        call #__muldf3"); break;
                case OpCode.DDIV: Emit("        call #__divdf3"); break;
                case OpCode.DNEG: Emit("        call #__negdf2"); break;
                case OpCode.DCMP: Emit("        call #__ledf2"); break;
                // 64-bit long — multi-word software sequences
                case OpCode.ADDL: case OpCode.SUBL: case OpCode.MULL:
                case OpCode.DIVL: case OpCode.MODL: case OpCode.NEGL: case OpCode.CMPL:
                case OpCode.I2L: case OpCode.L2I: case OpCode.F2L: case OpCode.L2F:
                case OpCode.D2L: case OpCode.L2D: case OpCode.PUSHL: case OpCode.POPL:
                    Emit($"        call #__{instr.Opcode.ToString().ToLower()}");
                    break;
                case OpCode.CLI:
                    Emit("        ; CLI — disable interrupts");
                    break;
                case OpCode.INT:
                    Emit("        ; INT — software interrupt");
                    break;
                case OpCode.IRET:
                    Emit("        ; IRET — interrupt return");
                    break;
                                // 内联汇编
                // OpCode.ASM handled by BaseTranslator.TryTranslateCommonOpcode

                                // 符号扩展/条件移动/循环移位 (v1.66.28+) — 软件库
                case OpCode.SEXTB: case OpCode.SEXTH:
                case OpCode.CMOVZ: case OpCode.CMOVNZ:
                case OpCode.ROL: case OpCode.ROR:
                    Emit($"        CALL #__{instr.Opcode.ToString().ToLower()}");
                    break;

                default:
                    Emit($"        ; UNIMPLEMENTED: {instr.Opcode}");
                    break;
            }
        }

        // GetOp → uses Translator16bit.GetOp

        protected override string MapRegister(int regNum)
        {
            return REGISTER_MAP.TryGetValue(regNum, out string? r) ? r : $"R{regNum}";
        }
    }
}
