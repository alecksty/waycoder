namespace VMLAssembler
{
    /// <summary>
    /// VML 指令操作码，这里是 VML 指令集的完整列表
    /// 数值与 C 运行时 vml_opcodes.h 严格对齐，禁止调整已有枚举成员的顺序或值
    /// </summary>
    public enum OpCode
    {
        // ======== 基础指令 ========
        NOP = 0,
        HALT = 1,

        // ======== v1.65.166+ 已删除: LOAD/STORE → MOVE/MOVEH/MOVEB ========


        // ======== 压栈指令 ========
        PUSH = 8,
        PUSHH = 9,
        PUSHB = 10,

        // ======== 弹栈指令 ========
        POP = 11,
        POPH = 12,
        POPB = 13,

        // ======== 移动指令 ========
        MOVE = 14,
        MOVEH = 15,
        MOVEB = 16,



        // ======== 算术指令 ========
        ADD = 18,
        SUB = 19,
        MUL = 20,
        DIV = 21,
        MOD = 22,

        // ======== 自增指令 ========
        INC = 23,
        DEC = 24,
        NEG = 25,
        TEST = 26,

        // ======== 逻辑指令 ========
        AND = 27,
        OR = 28,
        XOR = 29,
        NOT = 30,

        // ======== 移位指令 ========
        SHL = 31,
        SHR = 32,

        // ======== 可变移位 + 清零 (v1.62) ========
        SHLV = 33,  // Rd = Rs << Rt 可变左移 (Rt为移位量)
        SHRV = 34,  // Rd = Rs >> Rt 可变右移 (Rt为移位量)
        ZERO = 35,  // Rd = 0        清零

        // ======== 跳转指令 ========
        CMP = 36,
        JMP = 37,
        JZ = 38,
        JNZ = 39,
        JE = 40,
        JNE = 41,
        JG = 42,
        JL = 43,
        JGE = 44,
        JLE = 45,

        // ======== 调用指令 ========
        CALL = 46,
        RET = 47,

        ENTER = 48,
        LEAVE = 49,

        // ======== 中断异常处理指令 ========
        CLI = 50,
        STI = 51,
        INT = 52,
        IRET = 53,

        // ======== 清除指令 ========
        CLC = 54,
        STC = 55,

        // ======== 系统调用 ========
        SYSCALL = 56,

        // ======== 浮点运算指令 ========
        FADD = 57,
        FSUB = 58,
        FMUL = 59,
        FDIV = 60,
        FCMP = 61,
        FNEG = 62,

        // v1.65.166+ 已删除: FLOAD/FSTORE → MOVEF
        FPUSH = 65,
        FPOP = 66,

        I2F = 67,
        F2I = 68,

        // ======== 双精度浮点转换指令 ========
        F2D = 69,
        D2F = 70,
        I2D = 71,
        D2I = 72,

        // v1.65.166+ 已删除: DLOAD/DSTORE → MOVED
        DPUSH = 75,
        DPOP = 76,

        // ======== 双精度浮点运算指令 ========
        DADD = 77,
        DSUB = 78,
        DMUL = 79,
        DDIV = 80,
        DCMP = 81,
        DNEG = 82,

        // ======== 统一移动指令 (浮点/64位) ========
        MOVEF = 83,     // 浮点移动 (统一 FLOAD/FSTORE)
        MOVED = 84,     // 双精度移动 (统一 DLOAD/DSTORE)
        MOVEL = 85,     // 64位长整数移动

        // ======== 64位长整数栈操作 ========
        PUSHL = 86,     // 64位长整数压栈
        POPL = 87,      // 64位长整数弹栈

        // ======== 64位长整数算术 ========
        ADDL = 88,      // 64位加法
        SUBL = 89,      // 64位减法
        MULL = 90,      // 64位乘法
        DIVL = 91,      // 64位除法
        MODL = 92,      // 64位取模
        NEGL = 93,      // 64位取负
        CMPL = 94,      // 64位比较

        // ======== 64位长整数类型转换 ========
        I2L = 95,       // int32 → long64
        L2I = 96,       // long64 → int32
        F2L = 97,       // float → long64
        L2F = 98,       // long64 → float
        D2L = 99,       // double → long64
        L2D = 100,      // long64 → double

        // ======== 64位整数位运算 ========
        ANDL = 101,     // 64位按位与
        ORL  = 102,     // 64位按位或
        XORL = 103,     // 64位按位异或
        NOTL = 104,     // 64位按位取反
        SHLL = 105,     // 64位左移
        SHRL = 106,     // 64位右移

        // ======== 符号扩展指令 ========
        SEXTB = 107,    // 符号扩展: Rd = sign-extend(Rs低8位)
        SEXTH = 108,    // 符号扩展: Rd = sign-extend(Rs低16位)

        // ======== 条件移动指令 ========
        CMOVZ = 109,    // 条件移动: if (ZF) Rd = Rs
        CMOVNZ = 110,   // 条件移动: if (!ZF) Rd = Rs

        // ======== 旋转指令 ========
        ROL = 111,      // 循环左移: Rd = Rd <<< Rs
        ROR = 112,      // 循环右移: Rd = Rd >>> Rs

        // ======== 伪指令 (不写入VMB, >= 128) ========
        LABEL = 128,    // 标签,VMB中不需要
        BREAK = 129,    // 调试断点
        DUMP = 130,     // 调试转储
        TRACE = 131,    // 调试跟踪

        // ======== 内联汇编指令 ========
        ASM = 132,      // 内联汇编 .asm "code",不在VMB中
        CHIPASM = 133,  // 架构专属内联汇编 .chipasm "arch","code",不在VMB中

        // ======== 中断/异常处理指令 ========
        THROW = 134,    // 抛出异常
        CATCH = 135,    // 捕获异常
        ENDCATCH = 136, // 结束异常捕获
    }
}
