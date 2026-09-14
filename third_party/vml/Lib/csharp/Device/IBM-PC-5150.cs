using System;

namespace VML.Device.IBM.IBM_PC_5150
{
    /// <summary>
    /// IBM-PC-5150 寄存器定义
    /// 生成自: IBM/Personal Computer/IBM-PC-5150
    /// 版本: 1.0
    /// </summary>
    public static class IBM_PC_5150
    {
        // CPU架构: x86, 16位, 4772727 Hz

        // 寄存器定义
        // Accumulator Register
        public const int AX_ADDR = 0x0;
        public static unsafe ushort* AX => (ushort*)0x0;

        // Base Register
        public const int BX_ADDR = 0x1;
        public static unsafe ushort* BX => (ushort*)0x1;

        // Count Register
        public const int CX_ADDR = 0x2;
        public static unsafe ushort* CX => (ushort*)0x2;

        // Data Register
        public const int DX_ADDR = 0x3;
        public static unsafe ushort* DX => (ushort*)0x3;

        // Source Index
        public const int SI_ADDR = 0x4;
        public static unsafe ushort* SI => (ushort*)0x4;

        // Destination Index
        public const int DI_ADDR = 0x5;
        public static unsafe ushort* DI => (ushort*)0x5;

        // Base Pointer
        public const int BP_ADDR = 0x6;
        public static unsafe ushort* BP => (ushort*)0x6;

        // Stack Pointer
        public const int SP_ADDR = 0x7;
        public static unsafe ushort* SP => (ushort*)0x7;

        // Code Segment
        public const int CS_ADDR = 0x8;
        public static unsafe ushort* CS => (ushort*)0x8;

        // Data Segment
        public const int DS_ADDR = 0x9;
        public static unsafe ushort* DS => (ushort*)0x9;

        // Extra Segment
        public const int ES_ADDR = 0xA;
        public static unsafe ushort* ES => (ushort*)0xA;

        // Stack Segment
        public const int SS_ADDR = 0xB;
        public static unsafe ushort* SS => (ushort*)0xB;

        // Instruction Pointer
        public const int IP_ADDR = 0xC;
        public static unsafe ushort* IP => (ushort*)0xC;

        // Flags Register
        public const int FLAGS_ADDR = 0xD;
        public static unsafe ushort* FLAGS => (ushort*)0xD;
        public const int FLAGS_CF = 0;  // Carry Flag
        public const int FLAGS_PF = 2;  // Parity Flag
        public const int FLAGS_AF = 4;  // Auxiliary Carry Flag
        public const int FLAGS_ZF = 6;  // Zero Flag
        public const int FLAGS_SF = 7;  // Sign Flag
        public const int FLAGS_TF = 8;  // Trap Flag
        public const int FLAGS_IF = 9;  // Interrupt Enable Flag
        public const int FLAGS_DF = 10;  // Direction Flag
        public const int FLAGS_OF = 11;  // Overflow Flag

        // 内存段定义
        // BIOS ROM
        public const int BIOS_START = 0xF0000;
        public const int BIOS_END = 0xFFFFF;
        public const int BIOS_SIZE = 65536;

        // Video Memory
        public const int VIDEO_START = 0xB8000;
        public const int VIDEO_END = 0xBFFFF;
        public const int VIDEO_SIZE = 32768;

        // Conventional Memory (640KB)
        public const int CONVENTIONAL_START = 0x00000;
        public const int CONVENTIONAL_END = 0x9FFFF;
        public const int CONVENTIONAL_SIZE = 640;

        // Extended Memory (64KB)
        public const int EXTENDED_START = 0x100000;
        public const int EXTENDED_END = 0x10FFFF;
        public const int EXTENDED_SIZE = 64;

        // 外设定义
        // Programmable Interrupt Controller
        public const int PIC_BASE = 0x20;
        public static unsafe byte* PIC_PIC1_CMD => (byte*)0x00000040;
        public static unsafe byte* PIC_PIC1_DATA => (byte*)0x00000041;
        public static unsafe byte* PIC_PIC2_CMD => (byte*)0x000000C0;
        public static unsafe byte* PIC_PIC2_DATA => (byte*)0x000000C1;

        // Programmable Interval Timer
        public const int PIT_BASE = 0x40;
        public static unsafe byte* PIT_PIT_CH0 => (byte*)0x00000080;
        public static unsafe byte* PIT_PIT_CH1 => (byte*)0x00000081;
        public static unsafe byte* PIT_PIT_CH2 => (byte*)0x00000082;
        public static unsafe byte* PIT_PIT_CTRL => (byte*)0x00000083;

        // Programmable Peripheral Interface
        public const int PPI_BASE = 0x60;
        public static unsafe byte* PPI_PPI_PA => (byte*)0x000000C0;
        public static unsafe byte* PPI_PPI_PB => (byte*)0x000000C1;
        public static unsafe byte* PPI_PPI_PC => (byte*)0x000000C2;
        public static unsafe byte* PPI_PPI_CTRL => (byte*)0x000000C3;

        // Direct Memory Access Controller
        public const int DMA_BASE = 0x00;
        public static unsafe ushort* DMA_DMA_CH0_ADDR => (ushort*)0x00000000;
        public static unsafe ushort* DMA_DMA_CH0_COUNT => (ushort*)0x00000001;
        public static unsafe byte* DMA_DMA_CMD => (byte*)0x00000008;
        public static unsafe byte* DMA_DMA_MASK => (byte*)0x0000000A;
        public static unsafe byte* DMA_DMA_MODE => (byte*)0x0000000B;

        // Color Graphics Adapter
        public const int CGA_BASE = 0x3D4;
        public static unsafe byte* CGA_CGA_INDEX => (byte*)0x000007A8;
        public static unsafe byte* CGA_CGA_DATA => (byte*)0x000007A9;
        public static unsafe byte* CGA_CGA_MODE => (byte*)0x000007AC;
        public static unsafe byte* CGA_CGA_COLOR => (byte*)0x000007AD;

        // 中断向量定义
        public const int IRQ_DIVIDE_ERROR = 0;  // Divide Error
        public const int IRQ_SINGLE_STEP = 1;  // Single Step
        public const int IRQ_NMI = 2;  // Non-Maskable Interrupt
        public const int IRQ_BREAKPOINT = 3;  // Breakpoint
        public const int IRQ_OVERFLOW = 4;  // Overflow
        public const int IRQ_PRINT_SCREEN = 5;  // Print Screen
        public const int IRQ_IRQ0 = 8;  // Timer Interrupt
        public const int IRQ_IRQ1 = 9;  // Keyboard Interrupt
        public const int IRQ_IRQ2 = 10;  // Cascade (8259A)
        public const int IRQ_IRQ3 = 11;  // COM2
        public const int IRQ_IRQ4 = 12;  // COM1
        public const int IRQ_IRQ5 = 13;  // LPT2
        public const int IRQ_IRQ6 = 14;  // Floppy Disk
        public const int IRQ_IRQ7 = 15;  // LPT1
        public const int IRQ_IRQ8 = 16;  // Real Time Clock
        public const int IRQ_IRQ11 = 19;  // Reserved
        public const int IRQ_IRQ13 = 21;  // Coprocessor
        public const int IRQ_IRQ15 = 31;  // Reserved

        // 引脚定义
        public const int PIN_VCC = 1;  // +5V Power Supply
        public const int PIN_GND = 2;  // Ground
        public const int PIN_RESET = 3;  // System Reset
        public const int PIN_CLK = 4;  // System Clock (4.77MHz)
        public const int PIN_READY = 5;  // CPU Ready Signal
        public const int PIN_NMI = 6;  // Non-Maskable Interrupt
        public const int PIN_INTR = 7;  // Interrupt Request
        public const int PIN_HLDA = 8;  // Hold Acknowledge
        public const int PIN_HOLD = 9;  // Hold Request
        public const int PIN_MEMR = 10;  // Memory Read
        public const int PIN_MEMW = 11;  // Memory Write
        public const int PIN_IOR = 12;  // I/O Read
        public const int PIN_IOW = 13;  // I/O Write
        public const int PIN_ALE = 14;  // Address Latch Enable
        public const int PIN_DTR = 15;  // Data Terminal Ready (Serial)
        public const int PIN_RTS = 16;  // Request To Send (Serial)
        public const int PIN_CTS = 17;  // Clear To Send (Serial)
        public const int PIN_DSR = 18;  // Data Set Ready (Serial)
        public const int PIN_RI = 19;  // Ring Indicator (Serial)
        public const int PIN_DCD = 20;  // Data Carrier Detect (Serial)

        public static void ibm_pc_5150_init()
        {
            // 硬件初始化代码
        }
    }
}
