using System;

namespace VML.Device.NXP.LPC54606
{
    /// <summary>
    /// LPC54606 寄存器定义
    /// 生成自: NXP/LPC/LPC54606
    /// 版本: 1.0
    /// </summary>
    public static class LPC54606
    {
        // CPU架构: ARM-Cortex-M4, 32位, 180000000 Hz

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

        public const int SRAM_START = 0x20000000;
        public const int SRAM_END = 0x20021FFF;
        public const int SRAM_SIZE = 139264;

        public const int PERIPHERAL_START = 0x40000000;
        public const int PERIPHERAL_END = 0x401FFFFF;
        public const int PERIPHERAL_SIZE = 2097152;

        // 外设定义
        // System Control
        public const int SYSCON_BASE = 0x40000000;
        public static unsafe uint* SYSCON_SYSAHBCLKCTRL => (uint*)0x40000080;
        public static unsafe uint* SYSCON_MAINCLKSEL => (uint*)0x40000004;
        public static unsafe uint* SYSCON_MAINCLKUEN => (uint*)0x40000008;
        public static unsafe uint* SYSCON_SYSPLLCTRL => (uint*)0x4000000C;

        // General Purpose I/O
        public const int GPIO_BASE = 0x400F4000;
        public static unsafe uint* GPIO_DIR0 => (uint*)0x400F4000;
        public static unsafe uint* GPIO_PIN0 => (uint*)0x400F5000;
        public static unsafe uint* GPIO_SET0 => (uint*)0x400F6000;
        public static unsafe uint* GPIO_CLR0 => (uint*)0x400F7000;
        public static unsafe uint* GPIO_NOT0 => (uint*)0x400F8000;
        public static unsafe uint* GPIO_DIR1 => (uint*)0x400F4004;
        public static unsafe uint* GPIO_PIN1 => (uint*)0x400F5004;
        public static unsafe uint* GPIO_SET1 => (uint*)0x400F6004;
        public static unsafe uint* GPIO_CLR1 => (uint*)0x400F7004;
        public static unsafe uint* GPIO_NOT1 => (uint*)0x400F8004;

        // USART0
        public const int USART0_BASE = 0x40086000;
        public static unsafe uint* USART0_CFG => (uint*)0x40086000;
        public static unsafe uint* USART0_CTRL => (uint*)0x40086004;
        public static unsafe uint* USART0_STAT => (uint*)0x40086008;
        public static unsafe uint* USART0_TXDAT => (uint*)0x40086010;
        public static unsafe uint* USART0_RXDAT => (uint*)0x40086014;
        public static unsafe uint* USART0_BRG => (uint*)0x40086020;

        // 中断向量定义
        public const int IRQ_RESET = 0;  // 
        public const int IRQ_SVCALL = 11;  // 
        public const int IRQ_USART0 = 24;  // USART0 Interrupt

        public static void lpc54606_init()
        {
            // 硬件初始化代码
        }
    }
}
