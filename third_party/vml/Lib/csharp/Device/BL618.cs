using System;

namespace VML.Device.BouffaloLab.BL618
{
    /// <summary>
    /// BL618 寄存器定义
    /// 生成自: Bouffalo Lab/BL6/BL618
    /// 版本: 1.0
    /// </summary>
    public static class BL618
    {
        // CPU架构: RISC-V, 32位, 320000000 Hz

        // 寄存器定义
        // Return Address
        public const int X1_ADDR = 0x04;
        public static unsafe uint* x1 => (uint*)0x04;

        // Stack Pointer (SP)
        public const int X2_ADDR = 0x08;
        public static unsafe uint* x2 => (uint*)0x08;

        // Global Pointer (GP)
        public const int X3_ADDR = 0x0C;
        public static unsafe uint* x3 => (uint*)0x0C;

        // Frame Pointer (FP)
        public const int X8_ADDR = 0x20;
        public static unsafe uint* x8 => (uint*)0x20;

        // Function Argument (A0)
        public const int X10_ADDR = 0x28;
        public static unsafe uint* x10 => (uint*)0x28;

        // Function Argument (A1)
        public const int X11_ADDR = 0x2C;
        public static unsafe uint* x11 => (uint*)0x2C;

        // Program Counter
        public const int PC_ADDR = 0x3C;
        public static unsafe uint* pc => (uint*)0x3C;

        // 内存段定义
        public const int FLASH_START = 0x20000000;
        public const int FLASH_END = 0x203FFFFF;
        public const int FLASH_SIZE = 4194304;

        public const int SRAM_HPSYS_START = 0x22000000;
        public const int SRAM_HPSYS_END = 0x22003FFF;
        public const int SRAM_HPSYS_SIZE = 16384;

        // DTCM
        public const int SRAM_DTCM_START = 0x22010000;
        public const int SRAM_DTCM_END = 0x22017FFF;
        public const int SRAM_DTCM_SIZE = 32768;

        public const int SRAM_SYS_START = 0x22020000;
        public const int SRAM_SYS_END = 0x2208FFFF;
        public const int SRAM_SYS_SIZE = 458752;

        public const int PERIPHERAL_START = 0x30000000;
        public const int PERIPHERAL_END = 0x300FFFFF;
        public const int PERIPHERAL_SIZE = 1048576;

        // 外设定义
        // Global Control (Clock and Reset)
        public const int GLB_BASE = 0x30000000;
        public static unsafe uint* GLB_GLB_CLK_EN => (uint*)0x30000010;
        public const int GLB_GLB_CLK_EN_GPIO_CLK_EN = 6;  // GPIO clock enable
        public const int GLB_GLB_CLK_EN_UART0_CLK_EN = 12;  // UART0 clock enable
        public static unsafe uint* GLB_GLB_SYS_CLK_CTRL => (uint*)0x30000014;
        public static unsafe uint* GLB_GLB_PLL_CTRL => (uint*)0x3000001C;

        // GPIO Port A
        public const int GPIO_P0_BASE = 0x30007000;
        public static unsafe uint* GPIO_P0_GPIO_CFG0 => (uint*)0x30007000;
        public static unsafe uint* GPIO_P0_GPIO_CFG1 => (uint*)0x30007004;
        public static unsafe uint* GPIO_P0_GPIO_OE => (uint*)0x30007008;
        public static unsafe uint* GPIO_P0_GPIO_OUT => (uint*)0x3000700C;
        public static unsafe uint* GPIO_P0_GPIO_IN => (uint*)0x30007010;
        public static unsafe uint* GPIO_P0_GPIO_SET => (uint*)0x30007014;
        public static unsafe uint* GPIO_P0_GPIO_CLR => (uint*)0x30007018;
        public static unsafe uint* GPIO_P0_GPIO_TOG => (uint*)0x3000701C;

        // GPIO Port B
        public const int GPIO_P1_BASE = 0x30007200;
        public static unsafe uint* GPIO_P1_GPIO_CFG0 => (uint*)0x30007200;
        public static unsafe uint* GPIO_P1_GPIO_CFG1 => (uint*)0x30007204;
        public static unsafe uint* GPIO_P1_GPIO_OE => (uint*)0x30007208;
        public static unsafe uint* GPIO_P1_GPIO_OUT => (uint*)0x3000720C;
        public static unsafe uint* GPIO_P1_GPIO_IN => (uint*)0x30007210;
        public static unsafe uint* GPIO_P1_GPIO_SET => (uint*)0x30007214;
        public static unsafe uint* GPIO_P1_GPIO_CLR => (uint*)0x30007218;
        public static unsafe uint* GPIO_P1_GPIO_TOG => (uint*)0x3000721C;

        // UART 0
        public const int UART0_BASE = 0x30002000;
        public static unsafe uint* UART0_UART_CR => (uint*)0x30002000;
        public static unsafe uint* UART0_UART_BRR => (uint*)0x30002004;
        public static unsafe uint* UART0_UART_TDR => (uint*)0x30002008;
        public static unsafe uint* UART0_UART_RDR => (uint*)0x3000200C;
        public static unsafe uint* UART0_UART_SR => (uint*)0x30002010;

        // 中断向量定义
        public const int IRQ_RESET = 1;  // 
        public const int IRQ_MACHINESOFTWARE = 3;  // 
        public const int IRQ_MACHINETIMER = 7;  // 
        public const int IRQ_MACHINEEXTERNAL = 11;  // 
        public const int IRQ_UART0 = 20;  // UART0 Interrupt

        public static void bl618_init()
        {
            // 硬件初始化代码
        }
    }
}
