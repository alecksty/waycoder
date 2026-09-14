using System;

namespace VML.Device.Intel.8086
{
    /// <summary>
    /// 8086 寄存器定义
    /// 生成自: Intel/x86/8086
    /// 版本: 1.0
    /// </summary>
    public static class 8086
    {
        // CPU架构: x86, 16位, 5000000 Hz

        // 寄存器定义
        // Accumulator
        public const int AX_ADDR = 0;
        public static unsafe ushort* AX => (ushort*)0;
        public const int AX_AH = 8;  // High byte of AX
        public const int AX_AL = 0;  // Low byte of AX

        // Base
        public const int BX_ADDR = 1;
        public static unsafe ushort* BX => (ushort*)1;
        public const int BX_BH = 8;  // High byte of BX
        public const int BX_BL = 0;  // Low byte of BX

        // Counter
        public const int CX_ADDR = 2;
        public static unsafe ushort* CX => (ushort*)2;
        public const int CX_CH = 8;  // High byte of CX
        public const int CX_CL = 0;  // Low byte of CX

        // Data
        public const int DX_ADDR = 3;
        public static unsafe ushort* DX => (ushort*)3;
        public const int DX_DH = 8;  // High byte of DX
        public const int DX_DL = 0;  // Low byte of DX

        // Source Index
        public const int SI_ADDR = 4;
        public static unsafe ushort* SI => (ushort*)4;

        // Destination Index
        public const int DI_ADDR = 5;
        public static unsafe ushort* DI => (ushort*)5;

        // Base Pointer
        public const int BP_ADDR = 6;
        public static unsafe ushort* BP => (ushort*)6;

        // Stack Pointer
        public const int SP_ADDR = 7;
        public static unsafe ushort* SP => (ushort*)7;

        // Instruction Pointer
        public const int IP_ADDR = 8;
        public static unsafe ushort* IP => (ushort*)8;

        // Code Segment
        public const int CS_ADDR = 9;
        public static unsafe ushort* CS => (ushort*)9;

        // Data Segment
        public const int DS_ADDR = 10;
        public static unsafe ushort* DS => (ushort*)10;

        // Extra Segment
        public const int ES_ADDR = 11;
        public static unsafe ushort* ES => (ushort*)11;

        // Stack Segment
        public const int SS_ADDR = 12;
        public static unsafe ushort* SS => (ushort*)12;

        // Flags Register
        public const int FLAGS_ADDR = 13;
        public static unsafe ushort* FLAGS => (ushort*)13;
        public const int FLAGS_CF = 0;  // Carry Flag
        public const int FLAGS_PF = 2;  // Parity Flag
        public const int FLAGS_AF = 4;  // Auxiliary Flag
        public const int FLAGS_ZF = 6;  // Zero Flag
        public const int FLAGS_SF = 7;  // Sign Flag
        public const int FLAGS_TF = 8;  // Trap Flag
        public const int FLAGS_IF = 9;  // Interrupt Enable Flag
        public const int FLAGS_DF = 10;  // Direction Flag
        public const int FLAGS_OF = 11;  // Overflow Flag

        // 内存段定义
        // 1MB address space
        public const int CODE_START = 0x00000;
        public const int CODE_END = 0xFFFFF;
        public const int CODE_SIZE = 1048576;

        // Data memory
        public const int DATA_START = 0x00000;
        public const int DATA_END = 0xFFFFF;
        public const int DATA_SIZE = 1048576;

        // Stack memory
        public const int STACK_START = 0xF0000;
        public const int STACK_END = 0xFFFFF;
        public const int STACK_SIZE = 65536;

        // BIOS ROM
        public const int BIOS_START = 0xF0000;
        public const int BIOS_END = 0xFFFFF;
        public const int BIOS_SIZE = 65536;

        // 外设定义
        // Programmable Interrupt Controller
        public const int PIC_BASE = 0x0020;
        public static unsafe byte* PIC_PIC1_CMD => (byte*)0x00000040;
        public static unsafe byte* PIC_PIC1_DATA => (byte*)0x00000041;
        public static unsafe byte* PIC_PIC2_CMD => (byte*)0x000000C0;
        public static unsafe byte* PIC_PIC2_DATA => (byte*)0x000000C1;

        // Programmable Interval Timer
        public const int PIT_BASE = 0x0040;
        public static unsafe byte* PIT_PIT_CH0 => (byte*)0x00000080;
        public static unsafe byte* PIT_PIT_CH1 => (byte*)0x00000081;
        public static unsafe byte* PIT_PIT_CH2 => (byte*)0x00000082;
        public static unsafe byte* PIT_PIT_CMD => (byte*)0x00000083;

        // Programmable Peripheral Interface
        public const int PPI_BASE = 0x0060;
        public static unsafe byte* PPI_PPI_PA => (byte*)0x000000C0;
        public static unsafe byte* PPI_PPI_PB => (byte*)0x000000C1;
        public static unsafe byte* PPI_PPI_PC => (byte*)0x000000C2;
        public static unsafe byte* PPI_PPI_CMD => (byte*)0x000000C3;

        // 中断向量定义
        public const int IRQ_DIVIDE_ERROR = 0;  // Divide by zero
        public const int IRQ_DEBUG = 1;  // Single step
        public const int IRQ_NMI = 2;  // Non-maskable interrupt
        public const int IRQ_BREAKPOINT = 3;  // Breakpoint
        public const int IRQ_OVERFLOW = 4;  // INTO detected overflow
        public const int IRQ_IRQ0 = 8;  // Timer interrupt
        public const int IRQ_IRQ1 = 9;  // Keyboard interrupt
        public const int IRQ_IRQ2 = 10;  // Cascade
        public const int IRQ_IRQ3 = 11;  // COM2
        public const int IRQ_IRQ4 = 12;  // COM1
        public const int IRQ_IRQ5 = 13;  // LPT2
        public const int IRQ_IRQ6 = 14;  // Floppy disk
        public const int IRQ_IRQ7 = 15;  // LPT1

        public static void _8086_init()
        {
            // 硬件初始化代码
        }
    }
}
