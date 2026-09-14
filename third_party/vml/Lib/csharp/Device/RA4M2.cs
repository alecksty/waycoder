using System;

namespace VML.Device.Renesas.RA4M2
{
    /// <summary>
    /// RA4M2 寄存器定义
    /// 生成自: Renesas/RA/RA4M2
    /// 版本: 1.0
    /// </summary>
    public static class RA4M2
    {
        // CPU架构: ARM-Cortex-M4, 32位, 100000000 Hz

        // 寄存器定义
        public const int R0_ADDR = 0x00;
        public static unsafe uint* R0 => (uint*)0x00;

        public const int R1_ADDR = 0x04;
        public static unsafe uint* R1 => (uint*)0x04;

        public const int R2_ADDR = 0x08;
        public static unsafe uint* R2 => (uint*)0x08;

        public const int R3_ADDR = 0x0C;
        public static unsafe uint* R3 => (uint*)0x0C;

        public const int R4_ADDR = 0x10;
        public static unsafe uint* R4 => (uint*)0x10;

        public const int R5_ADDR = 0x14;
        public static unsafe uint* R5 => (uint*)0x14;

        public const int SP_ADDR = 0x34;
        public static unsafe uint* SP => (uint*)0x34;

        public const int LR_ADDR = 0x38;
        public static unsafe uint* LR => (uint*)0x38;

        public const int PC_ADDR = 0x3C;
        public static unsafe uint* PC => (uint*)0x3C;

        // 内存段定义
        public const int FLASH_START = 0x00000000;
        public const int FLASH_END = 0x0003FFFF;
        public const int FLASH_SIZE = 262144;

        // SRAM0
        public const int SRAM_START = 0x1FFE0000;
        public const int SRAM_END = 0x1FFE7FFF;
        public const int SRAM_SIZE = 32768;

        // SRAM1
        public const int SRAM1_START = 0x20000000;
        public const int SRAM1_END = 0x20017FFF;
        public const int SRAM1_SIZE = 98304;

        public const int PERIPHERAL_START = 0x40000000;
        public const int PERIPHERAL_END = 0x400FFFFF;
        public const int PERIPHERAL_SIZE = 1048576;

        // 外设定义
        // Module Stop Control
        public const int MSTP_BASE = 0x40020000;
        public static unsafe uint* MSTP_MSTPCR_A => (uint*)0x40020020;
        public const int MSTP_MSTPCR_A_MSTP41 = 9;  // GPIO A stop
        public const int MSTP_MSTPCR_A_MSTP42 = 10;  // GPIO B stop
        public static unsafe uint* MSTP_MSTPCR_B => (uint*)0x40020024;
        public static unsafe uint* MSTP_MSTPCR_C => (uint*)0x40020028;
        public static unsafe uint* MSTP_MSTPCR_D => (uint*)0x4002002C;

        // Interrupt Controller Unit
        public const int ICU_BASE = 0x40030000;
        public static unsafe ushort* ICU_IRQCR0 => (ushort*)0x40030600;
        public static unsafe ushort* ICU_IRQCR1 => (ushort*)0x40030602;

        // General Purpose I/O Port A
        public const int GPIOA_BASE = 0x40040000;
        public static unsafe ushort* GPIOA_PDR => (ushort*)0x40040000;
        public static unsafe ushort* GPIOA_PODR => (ushort*)0x40040004;
        public static unsafe ushort* GPIOA_PIDR => (ushort*)0x40040008;
        public static unsafe ushort* GPIOA_PMR => (ushort*)0x40040010;
        public static unsafe uint* GPIOA_PCR => (uint*)0x40040018;

        // General Purpose I/O Port B
        public const int GPIOB_BASE = 0x40040020;
        public static unsafe ushort* GPIOB_PDR => (ushort*)0x40040020;
        public static unsafe ushort* GPIOB_PODR => (ushort*)0x40040024;
        public static unsafe ushort* GPIOB_PIDR => (ushort*)0x40040028;
        public static unsafe ushort* GPIOB_PMR => (ushort*)0x40040030;

        // SCI UART 0
        public const int SCIUART0_BASE = 0x40070000;
        public static unsafe byte* SCIUART0_SCR => (byte*)0x40070000;
        public static unsafe byte* SCIUART0_BRR => (byte*)0x40070004;
        public static unsafe byte* SCIUART0_TDR => (byte*)0x40070008;
        public static unsafe byte* SCIUART0_RDR => (byte*)0x4007000C;
        public static unsafe byte* SCIUART0_SSR => (byte*)0x40070010;

        // 中断向量定义
        public const int IRQ_RESET = 0;  // 
        public const int IRQ_SVCALL = 11;  // 
        public const int IRQ_SCIUART0_RXI = 24;  // SCI UART0 Receive Interrupt
        public const int IRQ_SCIUART0_TXI = 25;  // SCI UART0 Transmit Interrupt

        public static void ra4m2_init()
        {
            // 硬件初始化代码
        }
    }
}
