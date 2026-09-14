package vml.device.ibm.ibm_pc_5150;

/**
 * IBM-PC-5150 寄存器定义
 * 生成自: IBM/Personal Computer/IBM-PC-5150
 * 版本: 1.0
 */
public final class IBM_PC_5150 {
    private IBM_PC_5150() {} // 工具类
    // CPU架构: x86, 16位, 4772727 Hz

    // 寄存器定义
    // Accumulator Register
    public static final int AX_ADDR = (int)0x0;

    // Base Register
    public static final int BX_ADDR = (int)0x1;

    // Count Register
    public static final int CX_ADDR = (int)0x2;

    // Data Register
    public static final int DX_ADDR = (int)0x3;

    // Source Index
    public static final int SI_ADDR = (int)0x4;

    // Destination Index
    public static final int DI_ADDR = (int)0x5;

    // Base Pointer
    public static final int BP_ADDR = (int)0x6;

    // Stack Pointer
    public static final int SP_ADDR = (int)0x7;

    // Code Segment
    public static final int CS_ADDR = (int)0x8;

    // Data Segment
    public static final int DS_ADDR = (int)0x9;

    // Extra Segment
    public static final int ES_ADDR = (int)0xA;

    // Stack Segment
    public static final int SS_ADDR = (int)0xB;

    // Instruction Pointer
    public static final int IP_ADDR = (int)0xC;

    // Flags Register
    public static final int FLAGS_ADDR = (int)0xD;
    public static final int FLAGS_CF = 0;  // Carry Flag
    public static final int FLAGS_PF = 2;  // Parity Flag
    public static final int FLAGS_AF = 4;  // Auxiliary Carry Flag
    public static final int FLAGS_ZF = 6;  // Zero Flag
    public static final int FLAGS_SF = 7;  // Sign Flag
    public static final int FLAGS_TF = 8;  // Trap Flag
    public static final int FLAGS_IF = 9;  // Interrupt Enable Flag
    public static final int FLAGS_DF = 10;  // Direction Flag
    public static final int FLAGS_OF = 11;  // Overflow Flag

    // 内存段定义
    // BIOS ROM
    public static final int BIOS_START = (int)0xF0000;
    public static final int BIOS_END = (int)0xFFFFF;
    public static final int BIOS_SIZE = 65536;

    // Video Memory
    public static final int VIDEO_START = (int)0xB8000;
    public static final int VIDEO_END = (int)0xBFFFF;
    public static final int VIDEO_SIZE = 32768;

    // Conventional Memory (640KB)
    public static final int CONVENTIONAL_START = (int)0x00000;
    public static final int CONVENTIONAL_END = (int)0x9FFFF;
    public static final int CONVENTIONAL_SIZE = 640;

    // Extended Memory (64KB)
    public static final int EXTENDED_START = (int)0x100000;
    public static final int EXTENDED_END = (int)0x10FFFF;
    public static final int EXTENDED_SIZE = 64;

    // 外设定义
    // Programmable Interrupt Controller
    public static final int PIC_BASE = (int)0x20;
    public static final int PIC_PIC1_CMD = (int)0x00000040;
    public static final int PIC_PIC1_DATA = (int)0x00000041;
    public static final int PIC_PIC2_CMD = (int)0x000000C0;
    public static final int PIC_PIC2_DATA = (int)0x000000C1;

    // Programmable Interval Timer
    public static final int PIT_BASE = (int)0x40;
    public static final int PIT_PIT_CH0 = (int)0x00000080;
    public static final int PIT_PIT_CH1 = (int)0x00000081;
    public static final int PIT_PIT_CH2 = (int)0x00000082;
    public static final int PIT_PIT_CTRL = (int)0x00000083;

    // Programmable Peripheral Interface
    public static final int PPI_BASE = (int)0x60;
    public static final int PPI_PPI_PA = (int)0x000000C0;
    public static final int PPI_PPI_PB = (int)0x000000C1;
    public static final int PPI_PPI_PC = (int)0x000000C2;
    public static final int PPI_PPI_CTRL = (int)0x000000C3;

    // Direct Memory Access Controller
    public static final int DMA_BASE = (int)0x00;
    public static final int DMA_DMA_CH0_ADDR = (int)0x00000000;
    public static final int DMA_DMA_CH0_COUNT = (int)0x00000001;
    public static final int DMA_DMA_CMD = (int)0x00000008;
    public static final int DMA_DMA_MASK = (int)0x0000000A;
    public static final int DMA_DMA_MODE = (int)0x0000000B;

    // Color Graphics Adapter
    public static final int CGA_BASE = (int)0x3D4;
    public static final int CGA_CGA_INDEX = (int)0x000007A8;
    public static final int CGA_CGA_DATA = (int)0x000007A9;
    public static final int CGA_CGA_MODE = (int)0x000007AC;
    public static final int CGA_CGA_COLOR = (int)0x000007AD;

    // 中断向量定义
    public static final int IRQ_DIVIDE_ERROR = 0;  // Divide Error
    public static final int IRQ_SINGLE_STEP = 1;  // Single Step
    public static final int IRQ_NMI = 2;  // Non-Maskable Interrupt
    public static final int IRQ_BREAKPOINT = 3;  // Breakpoint
    public static final int IRQ_OVERFLOW = 4;  // Overflow
    public static final int IRQ_PRINT_SCREEN = 5;  // Print Screen
    public static final int IRQ_IRQ0 = 8;  // Timer Interrupt
    public static final int IRQ_IRQ1 = 9;  // Keyboard Interrupt
    public static final int IRQ_IRQ2 = 10;  // Cascade (8259A)
    public static final int IRQ_IRQ3 = 11;  // COM2
    public static final int IRQ_IRQ4 = 12;  // COM1
    public static final int IRQ_IRQ5 = 13;  // LPT2
    public static final int IRQ_IRQ6 = 14;  // Floppy Disk
    public static final int IRQ_IRQ7 = 15;  // LPT1
    public static final int IRQ_IRQ8 = 16;  // Real Time Clock
    public static final int IRQ_IRQ11 = 19;  // Reserved
    public static final int IRQ_IRQ13 = 21;  // Coprocessor
    public static final int IRQ_IRQ15 = 31;  // Reserved

    // 引脚定义
    public static final int PIN_VCC = 1;  // +5V Power Supply
    public static final int PIN_GND = 2;  // Ground
    public static final int PIN_RESET = 3;  // System Reset
    public static final int PIN_CLK = 4;  // System Clock (4.77MHz)
    public static final int PIN_READY = 5;  // CPU Ready Signal
    public static final int PIN_NMI = 6;  // Non-Maskable Interrupt
    public static final int PIN_INTR = 7;  // Interrupt Request
    public static final int PIN_HLDA = 8;  // Hold Acknowledge
    public static final int PIN_HOLD = 9;  // Hold Request
    public static final int PIN_MEMR = 10;  // Memory Read
    public static final int PIN_MEMW = 11;  // Memory Write
    public static final int PIN_IOR = 12;  // I/O Read
    public static final int PIN_IOW = 13;  // I/O Write
    public static final int PIN_ALE = 14;  // Address Latch Enable
    public static final int PIN_DTR = 15;  // Data Terminal Ready (Serial)
    public static final int PIN_RTS = 16;  // Request To Send (Serial)
    public static final int PIN_CTS = 17;  // Clear To Send (Serial)
    public static final int PIN_DSR = 18;  // Data Set Ready (Serial)
    public static final int PIN_RI = 19;  // Ring Indicator (Serial)
    public static final int PIN_DCD = 20;  // Data Carrier Detect (Serial)

    public static native void ibm_pc_5150_init();
}
