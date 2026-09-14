using System;

namespace VML.Device.SiliconLabs.EFR32MG24
{
    /// <summary>
    /// EFR32MG24 寄存器定义
    /// 生成自: Silicon Labs/EFR32/EFR32MG24
    /// 版本: 1.0
    /// </summary>
    public static class EFR32MG24
    {
        // CPU架构: ARM-Cortex-M33, 32位, 78000000 Hz

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
        public const int FLASH_START = 0x08000000;
        public const int FLASH_END = 0x0817FFFF;
        public const int FLASH_SIZE = 1572864;

        public const int SRAM_START = 0x20000000;
        public const int SRAM_END = 0x2003FFFF;
        public const int SRAM_SIZE = 262144;

        public const int PERIPHERAL_START = 0x40000000;
        public const int PERIPHERAL_END = 0x4007FFFF;
        public const int PERIPHERAL_SIZE = 524288;

        // 外设定义
        // Clock Management Unit
        public const int CMU_BASE = 0x40080000;
        public static unsafe uint* CMU_CTRL => (uint*)0x40080000;
        public static unsafe uint* CMU_HFCORECLKCFG => (uint*)0x40080008;
        public static unsafe uint* CMU_HFPERCLKEN0 => (uint*)0x40080010;
        public const int CMU_HFPERCLKEN0_GPIOEN = 4;  // GPIO clock enable
        public const int CMU_HFPERCLKEN0_USART0EN = 12;  // USART0 clock enable
        public const int CMU_HFPERCLKEN0_USART1EN = 13;  // USART1 clock enable
        public static unsafe uint* CMU_LFBCLKEN0 => (uint*)0x40080020;

        // GPIO Controller
        public const int GPIO_BASE = 0x40088000;
        public static unsafe uint* GPIO_PORT_A_CTRL => (uint*)0x40088000;
        public static unsafe uint* GPIO_PORT_B_CTRL => (uint*)0x40088004;
        public static unsafe uint* GPIO_PORT_C_CTRL => (uint*)0x40088008;
        public static unsafe uint* GPIO_PORT_D_CTRL => (uint*)0x4008800C;
        public static unsafe uint* GPIO_MODEL => (uint*)0x40088010;
        public static unsafe uint* GPIO_MODEH => (uint*)0x40088014;
        public static unsafe uint* GPIO_DOUT => (uint*)0x4008801C;
        public static unsafe uint* GPIO_DOUTSET => (uint*)0x40088020;
        public static unsafe uint* GPIO_DOUTCLR => (uint*)0x40088024;
        public static unsafe uint* GPIO_DOUTTGL => (uint*)0x40088028;
        public static unsafe uint* GPIO_DIN => (uint*)0x4008802C;

        // GPIO Port A extended
        public const int GPIO_PA_BASE = 0x40088400;
        public static unsafe uint* GPIO_PA_PA_CFG => (uint*)0x40088400;
        public static unsafe uint* GPIO_PA_PA_PINOUT => (uint*)0x40088404;

        // GPIO Port B extended
        public const int GPIO_PB_BASE = 0x40088800;
        public static unsafe uint* GPIO_PB_PB_CFG => (uint*)0x40088800;

        // USART 0
        public const int USART0_BASE = 0x40060000;
        public static unsafe uint* USART0_CTRL => (uint*)0x40060000;
        public static unsafe uint* USART0_CMD => (uint*)0x40060004;
        public static unsafe uint* USART0_STATUS => (uint*)0x40060008;
        public static unsafe uint* USART0_RXDATA => (uint*)0x4006000C;
        public static unsafe uint* USART0_TXDATA => (uint*)0x40060010;
        public static unsafe uint* USART0_CLKDIV => (uint*)0x40060014;

        // 中断向量定义
        public const int IRQ_RESET = 0;  // 
        public const int IRQ_SVCALL = 11;  // 
        public const int IRQ_USART0_RX = 12;  // USART0 Receive Interrupt
        public const int IRQ_USART0_TX = 13;  // USART0 Transmit Interrupt

        public static void efr32mg24_init()
        {
            // 硬件初始化代码
        }
    }
}
