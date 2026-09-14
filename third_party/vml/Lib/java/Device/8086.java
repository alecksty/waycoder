package vml.device.intel._8086;

/**
 * 8086 寄存器定义
 * 生成自: Intel/x86/8086
 * 版本: 1.0
 */
public final class 8086 {
    private 8086() {} // 工具类
    // CPU架构: x86, 16位, 5000000 Hz

    // 寄存器定义
    // Accumulator
    public static final int AX_ADDR = (int)0;
    public static final int AX_AH = 8;  // High byte of AX
    public static final int AX_AL = 0;  // Low byte of AX

    // Base
    public static final int BX_ADDR = (int)1;
    public static final int BX_BH = 8;  // High byte of BX
    public static final int BX_BL = 0;  // Low byte of BX

    // Counter
    public static final int CX_ADDR = (int)2;
    public static final int CX_CH = 8;  // High byte of CX
    public static final int CX_CL = 0;  // Low byte of CX

    // Data
    public static final int DX_ADDR = (int)3;
    public static final int DX_DH = 8;  // High byte of DX
    public static final int DX_DL = 0;  // Low byte of DX

    // Source Index
    public static final int SI_ADDR = (int)4;

    // Destination Index
    public static final int DI_ADDR = (int)5;

    // Base Pointer
    public static final int BP_ADDR = (int)6;

    // Stack Pointer
    public static final int SP_ADDR = (int)7;

    // Instruction Pointer
    public static final int IP_ADDR = (int)8;

    // Code Segment
    public static final int CS_ADDR = (int)9;

    // Data Segment
    public static final int DS_ADDR = (int)10;

    // Extra Segment
    public static final int ES_ADDR = (int)11;

    // Stack Segment
    public static final int SS_ADDR = (int)12;

    // Flags Register
    public static final int FLAGS_ADDR = (int)13;
    public static final int FLAGS_CF = 0;  // Carry Flag
    public static final int FLAGS_PF = 2;  // Parity Flag
    public static final int FLAGS_AF = 4;  // Auxiliary Flag
    public static final int FLAGS_ZF = 6;  // Zero Flag
    public static final int FLAGS_SF = 7;  // Sign Flag
    public static final int FLAGS_TF = 8;  // Trap Flag
    public static final int FLAGS_IF = 9;  // Interrupt Enable Flag
    public static final int FLAGS_DF = 10;  // Direction Flag
    public static final int FLAGS_OF = 11;  // Overflow Flag

    // 内存段定义
    // 1MB address space
    public static final int CODE_START = (int)0x00000;
    public static final int CODE_END = (int)0xFFFFF;
    public static final int CODE_SIZE = 1048576;

    // Data memory
    public static final int DATA_START = (int)0x00000;
    public static final int DATA_END = (int)0xFFFFF;
    public static final int DATA_SIZE = 1048576;

    // Stack memory
    public static final int STACK_START = (int)0xF0000;
    public static final int STACK_END = (int)0xFFFFF;
    public static final int STACK_SIZE = 65536;

    // BIOS ROM
    public static final int BIOS_START = (int)0xF0000;
    public static final int BIOS_END = (int)0xFFFFF;
    public static final int BIOS_SIZE = 65536;

    // 外设定义
    // Programmable Interrupt Controller
    public static final int PIC_BASE = (int)0x0020;
    public static final int PIC_PIC1_CMD = (int)0x00000040;
    public static final int PIC_PIC1_DATA = (int)0x00000041;
    public static final int PIC_PIC2_CMD = (int)0x000000C0;
    public static final int PIC_PIC2_DATA = (int)0x000000C1;

    // Programmable Interval Timer
    public static final int PIT_BASE = (int)0x0040;
    public static final int PIT_PIT_CH0 = (int)0x00000080;
    public static final int PIT_PIT_CH1 = (int)0x00000081;
    public static final int PIT_PIT_CH2 = (int)0x00000082;
    public static final int PIT_PIT_CMD = (int)0x00000083;

    // Programmable Peripheral Interface
    public static final int PPI_BASE = (int)0x0060;
    public static final int PPI_PPI_PA = (int)0x000000C0;
    public static final int PPI_PPI_PB = (int)0x000000C1;
    public static final int PPI_PPI_PC = (int)0x000000C2;
    public static final int PPI_PPI_CMD = (int)0x000000C3;

    // 中断向量定义
    public static final int IRQ_DIVIDE_ERROR = 0;  // Divide by zero
    public static final int IRQ_DEBUG = 1;  // Single step
    public static final int IRQ_NMI = 2;  // Non-maskable interrupt
    public static final int IRQ_BREAKPOINT = 3;  // Breakpoint
    public static final int IRQ_OVERFLOW = 4;  // INTO detected overflow
    public static final int IRQ_IRQ0 = 8;  // Timer interrupt
    public static final int IRQ_IRQ1 = 9;  // Keyboard interrupt
    public static final int IRQ_IRQ2 = 10;  // Cascade
    public static final int IRQ_IRQ3 = 11;  // COM2
    public static final int IRQ_IRQ4 = 12;  // COM1
    public static final int IRQ_IRQ5 = 13;  // LPT2
    public static final int IRQ_IRQ6 = 14;  // Floppy disk
    public static final int IRQ_IRQ7 = 15;  // LPT1

    public static native void _8086_init();
}
