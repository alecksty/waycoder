using VMLAssembler;

namespace VMLTranslators
{
    /// <summary>
    /// PIC24/dsPIC33 (Microchip) 16-bit MCU 转译器
    /// </summary>
    public class TranslatorPIC24 : Translator16bit
    {
        public override string ARCH_NAME => "PIC24";
        protected override string AddMnemonic => "add";
        protected override string SubMnemonic => "sub";
        protected override string MovMnemonic => "mov";

        // PIC24 中断上下文
        protected override string[] ContextRegNames() => new[] { "w0","w1","w2","w3","w4","w5","w6","w7","w8","w9","w10","w11","w12","w13","w14" };
        protected override string NativeIret() => "retfie";
        protected override string NativeCli() => "disi #0x3FFF";
        protected override string NativeSti() => "disi #0";
        protected override void EmitNativeInt(int vector) => Emit($"        ; INT #{vector} - PIC24 software interrupt");
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

        private readonly Dictionary<int, string> REGISTER_MAP = new()
        {
            { 0, "W0" },  { 1, "W1" },  { 2, "W2" },  { 3, "W3" },
            { 4, "W4" },  { 5, "W5" },  { 6, "W6" },  { 7, "W7" },
            { 8, "W8" },  { 9, "W9" },  { 10, "W10" },{ 11, "W11" },
            { 12, "W12" },{ 13, "W15" },{ 14, "W14" },{ 15, "W15" },
        };

        public TranslatorPIC24(VmlProgram prog) : base(prog) { }

        protected override void EmitHeader()
        {
            Emit("; PIC24/dsPIC33 Assembly - Translated from VML");
            Emit("; Target: PIC24HJ256GP610 / dsPIC33EP512MU810");
            Emit("; Assembler: pic30-as / xc16-gcc");
            Emit("");
            Emit("        .linked  \"p24Fxxxx.inc\"");
            Emit("        .global __reset");
            Emit("        .global main");
            Emit("");
            Emit("__reset:");
            Emit("        mov     #0x1000, W15      ; Initialize SP");
            Emit("        mov     #main, W0");
            Emit("        call    W0");
            Emit("        goto    $");
            Emit("");
            Emit("        .text");
            Emit("        .align 2");
            Emit("main:");
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

        protected override void TranslateInstruction(Instruction instr)
        {
            switch (instr.Opcode)
            {
                                                case OpCode.ADD:
                    {
                        string dst = MapRegister((int)instr.Operands[0].Value);
                        var src = instr.Operands[1];
                        if (src.Type == OperandType.IMMEDIATE)
                            Emit($"        add     #{src.Value}, {dst}");
                        else
                            Emit($"        add     {MapRegister((int)src.Value)}, {dst}");
                    }
                    break;
                case OpCode.SUB:
                    {
                        string dst = MapRegister((int)instr.Operands[0].Value);
                        var src = instr.Operands[1];
                        if (src.Type == OperandType.IMMEDIATE)
                            Emit($"        sub     #{src.Value}, {dst}");
                        else
                            Emit($"        sub     {MapRegister((int)src.Value)}, {dst}");
                    }
                    break;
                case OpCode.MUL:
                    {
                        string r0 = MapRegister((int)instr.Operands[0].Value);
                        string r1 = instr.Operands.Count > 2
                            ? GetOp(instr.Operands[2]) : GetOp(instr.Operands[1]);
                        Emit($"        mul     {r0}, {r1}");
                    }
                    break;
                case OpCode.MOVE:
                    Emit($"        mov     {GetOp(instr.Operands[1])}, {MapRegister((int)instr.Operands[0].Value)}");
                    break;
                case OpCode.PUSH:
                    Emit($"        push    {GetOp(instr.Operands[0])}");
                    break;
                case OpCode.POP:
                    Emit($"        pop     {MapRegister((int)instr.Operands[0].Value)}");
                    break;
                case OpCode.CMP:
                    Emit($"        cp      {GetOp(instr.Operands[1])}, {GetOp(instr.Operands[0])}");
                    break;
                case OpCode.JMP:
                    Emit($"        goto    {instr.Operands[0].Value}");
                    break;
                case OpCode.JZ:
                case OpCode.JE:
                    Emit($"        bra     Z, {instr.Operands[0].Value}");
                    break;
                case OpCode.JNZ:
                case OpCode.JNE:
                    Emit($"        bra     NZ, {instr.Operands[0].Value}");
                    break;
                case OpCode.JL:
                    Emit($"        bra     LT, {instr.Operands[0].Value}");
                    break;
                case OpCode.JGE:
                    Emit($"        bra     GE, {instr.Operands[0].Value}");
                    break;
                case OpCode.CALL:
                    Emit($"        call    {instr.Operands[0].Value}");
                    break;
                case OpCode.RET:
                    Emit($"        return");
                    break;
                case OpCode.AND:
                    Emit($"        and     {GetOp(instr.Operands[1])}, {MapRegister((int)instr.Operands[0].Value)}");
                    break;
                case OpCode.OR:
                    Emit($"        ior     {GetOp(instr.Operands[1])}, {MapRegister((int)instr.Operands[0].Value)}");
                    break;
                case OpCode.XOR:
                    Emit($"        xor     {GetOp(instr.Operands[1])}, {MapRegister((int)instr.Operands[0].Value)}");
                    break;
                case OpCode.NOT:
                    Emit($"        com     {MapRegister((int)instr.Operands[0].Value)}");
                    break;
                case OpCode.INC:
                    Emit($"        inc     {MapRegister((int)instr.Operands[0].Value)}");
                    break;
                case OpCode.DEC:
                    Emit($"        dec     {MapRegister((int)instr.Operands[0].Value)}");
                    break;
                case OpCode.NEG:
                    Emit($"        neg     {MapRegister((int)instr.Operands[0].Value)}");
                    break;
                case OpCode.SHL:
                    if (instr.Operands.Count >= 3 && instr.Operands[2].Type == OperandType.IMMEDIATE)
                        Emit($"        sl      {MapRegister((int)instr.Operands[0].Value)}, #{(int)instr.Operands[2].Value}");
                    break;
                case OpCode.SHR:
                    if (instr.Operands.Count >= 3 && instr.Operands[2].Type == OperandType.IMMEDIATE)
                        Emit($"        sr      {MapRegister((int)instr.Operands[0].Value)}, #{(int)instr.Operands[2].Value}");
                    break;
                case OpCode.SYSCALL:
                    Emit($"        call __syscall_{instr.Operands[0].Value}");
                    break;
                case OpCode.HALT:
                    Emit($"        goto    $");
                    break;
                case OpCode.NOP:
                    Emit($"        nop");
                    break;
                case OpCode.STI:
                    // v1.66.31 fix: CLI/STI/INT/IRET handled by BaseTranslator, add explicit break
                    break;
                case OpCode.THROW:
                    Emit("        ; THROW - PIC24 exception");
                    Emit("        reset");
                    break;
                case OpCode.CATCH:
                    break;
                case OpCode.ENDCATCH:
                    break;
                case OpCode.DIV:
                    Emit($"        ; DIV - call software routine");
                    Emit($"        call    __div32");
                    break;
                case OpCode.MOD:
                    Emit($"        ; MOD - call software routine");
                    Emit($"        call    __mod32");
                    break;
                case OpCode.SHLV:
                    Emit($"        ; SHLV - variable shift");
                    Emit($"        sl      {MapRegister((int)instr.Operands[0].Value)}, {GetOp(instr.Operands[2])}");
                    break;
                case OpCode.SHRV:
                    Emit($"        ; SHRV - variable shift");
                    Emit($"        sr      {MapRegister((int)instr.Operands[0].Value)}, {GetOp(instr.Operands[2])}");
                    break;
                case OpCode.TEST:
                    if (instr.Operands.Count >= 1)
                        Emit($"        cp0     {GetOp(instr.Operands[0])}");
                    break;
                case OpCode.ZERO:
                    Emit($"        clr     {MapRegister((int)instr.Operands[0].Value)}");
                    break;
                                                                                case OpCode.MOVEB:
                    Emit($"        mov.b   {GetOp(instr.Operands[1])}, {MapRegister((int)instr.Operands[0].Value)}");
                    break;
                case OpCode.MOVEH:
                    Emit($"        mov     {GetOp(instr.Operands[1])}, {MapRegister((int)instr.Operands[0].Value)}");
                    break;
                case OpCode.PUSHB:
                    Emit($"        push    {GetOp(instr.Operands[0])}");
                    break;
                case OpCode.PUSHH:
                    Emit($"        push    {GetOp(instr.Operands[0])}");
                    break;
                case OpCode.POPB:
                    Emit($"        pop     {MapRegister((int)instr.Operands[0].Value)}");
                    break;
                case OpCode.POPH:
                    Emit($"        pop     {MapRegister((int)instr.Operands[0].Value)}");
                    break;
                case OpCode.ENTER:
                    Emit($"        push    W14");
                    Emit($"        mov     W15, W14");
                    Emit($"        sub     #{instr.Operands[0].Value}, W15");
                    break;
                case OpCode.LEAVE:
                    Emit($"        mov     W14, W15");
                    Emit($"        pop     W14");
                    break;
                // CLC/STC handled by Translator16bit.EmitCLC/EmitSTC (v1.66.31+)
                case OpCode.BREAK:
                    Emit("        ; BREAK");
                    break;
                // OpCode.CHIPASM handled by BaseTranslator.TryTranslateCommonOpcode
                case OpCode.LABEL:
                    break;
                case OpCode.JG:
                    Emit($"        bra     GT, {instr.Operands[0].Value}");
                    break;
                case OpCode.JLE:
                    Emit($"        bra     LE, {instr.Operands[0].Value}");
                    break;
                // ── 浮点 / 64-bit / long 指令 → PIC24 软件库调用 (v1.66.31+) ──
                case OpCode.FADD: Emit("        call __addsf3"); break;
                case OpCode.FSUB: Emit("        call __subsf3"); break;
                case OpCode.FMUL: Emit("        call __mulsf3"); break;
                case OpCode.FDIV: Emit("        call __divsf3"); break;
                case OpCode.FNEG: Emit("        call __negsf2"); break;
                case OpCode.FCMP: Emit("        call __lesf2"); break;
                case OpCode.I2F:  Emit("        call __floatsisf"); break;
                case OpCode.F2I:  Emit("        call __fixsfsi"); break;
                case OpCode.F2D:  Emit("        call __extendsfdf2"); break;
                case OpCode.D2F:  Emit("        call __truncdfsf2"); break;
                case OpCode.I2D:  Emit("        call __floatsidf"); break;
                case OpCode.D2I:  Emit("        call __fixdfsi"); break;
                // ── 浮点/64位数据移动 (v1.66.31+: 寄存器对操作) ──
                case OpCode.MOVEF:
                {
                    int fd = SafeToInt(instr.Operands[0]), fs = SafeToInt(instr.Operands[1]);
                    Emit($"        mov     {MapRegister(fs)}, {MapRegister(fd)}");
                    Emit($"        mov     {MapRegister(fs+1)}, {MapRegister(fd+1)}");
                    break;
                }
                case OpCode.MOVED:
                case OpCode.MOVEL:
                {
                    int dd = SafeToInt(instr.Operands[0]), ds = SafeToInt(instr.Operands[1]);
                    Emit($"        mov     {MapRegister(ds)}, {MapRegister(dd)}");
                    Emit($"        mov     {MapRegister(ds+1)}, {MapRegister(dd+1)}");
                    Emit($"        mov     {MapRegister(ds+2)}, {MapRegister(dd+2)}");
                    Emit($"        mov     {MapRegister(ds+3)}, {MapRegister(dd+3)}");
                    break;
                }
                case OpCode.FPUSH:
                {
                    int f = SafeToInt(instr.Operands[0]);
                    Emit($"        push    {MapRegister(f+1)}");
                    Emit($"        push    {MapRegister(f)}");
                    break;
                }
                case OpCode.FPOP:
                {
                    int f = SafeToInt(instr.Operands[0]);
                    Emit($"        pop     {MapRegister(f)}");
                    Emit($"        pop     {MapRegister(f+1)}");
                    break;
                }
                case OpCode.DPUSH:
                {
                    int d = SafeToInt(instr.Operands[0]);
                    Emit($"        push    {MapRegister(d+3)}");
                    Emit($"        push    {MapRegister(d+2)}");
                    Emit($"        push    {MapRegister(d+1)}");
                    Emit($"        push    {MapRegister(d)}");
                    break;
                }
                case OpCode.DPOP:
                {
                    int d = SafeToInt(instr.Operands[0]);
                    Emit($"        pop     {MapRegister(d)}");
                    Emit($"        pop     {MapRegister(d+1)}");
                    Emit($"        pop     {MapRegister(d+2)}");
                    Emit($"        pop     {MapRegister(d+3)}");
                    break;
                }
                case OpCode.DADD: Emit("        call __adddf3"); break;
                case OpCode.DSUB: Emit("        call __subdf3"); break;
                case OpCode.DMUL: Emit("        call __muldf3"); break;
                case OpCode.DDIV: Emit("        call __divdf3"); break;
                case OpCode.DNEG: Emit("        call __negdf2"); break;
                case OpCode.DCMP: Emit("        call __ledf2"); break;
                case OpCode.ADDL: case OpCode.SUBL: case OpCode.MULL:
                case OpCode.DIVL: case OpCode.MODL: case OpCode.NEGL: case OpCode.CMPL:
                case OpCode.I2L: case OpCode.L2I: case OpCode.F2L: case OpCode.L2F:
                case OpCode.D2L: case OpCode.L2D: case OpCode.PUSHL: case OpCode.POPL:
                    Emit($"        call __{instr.Opcode.ToString().ToLower()}");
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
                    Emit($"        CALL __{instr.Opcode.ToString().ToLower()}");
                    break;

                default:
                    Emit($"        ; UNIMPLEMENTED: {instr.Opcode}");
                    break;
            }
        }

        // GetOp → uses Translator16bit.GetOp

        protected override string MapRegister(int regNum)
        {
            return REGISTER_MAP.TryGetValue(regNum, out string? r) ? r : $"W{regNum}";
        }
    }
}
