using System;

namespace VML.Device.STMicroelectronics.STM32L432
{
    /// <summary>
    /// STM32L432 寄存器定义
    /// 生成自: STMicroelectronics/STM32/STM32L432
    /// 版本: 1.0
    /// </summary>
    public static class STM32L432
    {
        // CPU架构: ARM-Cortex-M4, 32位, 80000000 Hz

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
        public const int FLASH_END = 0x0803FFFF;
        public const int FLASH_SIZE = 262144;

        public const int SRAM_START = 0x20000000;
        public const int SRAM_END = 0x2000FFFF;
        public const int SRAM_SIZE = 65536;

        public const int PERIPHERAL_START = 0x40000000;
        public const int PERIPHERAL_END = 0x4007FFFF;
        public const int PERIPHERAL_SIZE = 524288;

        // 外设定义
        // Reset and Clock Control
        public const int RCC_BASE = 0x40021000;
        public static unsafe uint* RCC_CR => (uint*)0x40021000;
        public static unsafe uint* RCC_CFGR => (uint*)0x40021008;
        public static unsafe uint* RCC_PLLCFGR => (uint*)0x4002100C;
        public static unsafe uint* RCC_AHB1ENR => (uint*)0x40021038;
        public const int RCC_AHB1ENR_GPIOAEN = 0;  // GPIOA clock enable
        public const int RCC_AHB1ENR_GPIOBEN = 1;  // GPIOB clock enable
        public static unsafe uint* RCC_APB1ENR1 => (uint*)0x40021058;
        public static unsafe uint* RCC_APB2ENR => (uint*)0x40021060;

        // General Purpose I/O Port A
        public const int GPIOA_BASE = 0x48000000;
        public static unsafe uint* GPIOA_MODER => (uint*)0x48000000;
        public static unsafe uint* GPIOA_OTYPER => (uint*)0x48000004;
        public static unsafe uint* GPIOA_OSPEEDR => (uint*)0x48000008;
        public static unsafe uint* GPIOA_PUPDR => (uint*)0x4800000C;
        public static unsafe uint* GPIOA_IDR => (uint*)0x48000010;
        public static unsafe uint* GPIOA_ODR => (uint*)0x48000014;
        public static unsafe uint* GPIOA_BSRR => (uint*)0x48000018;
        public static unsafe uint* GPIOA_BRR => (uint*)0x48000028;

        // General Purpose I/O Port B
        public const int GPIOB_BASE = 0x48000400;
        public static unsafe uint* GPIOB_MODER => (uint*)0x48000400;
        public static unsafe uint* GPIOB_OTYPER => (uint*)0x48000404;
        public static unsafe uint* GPIOB_OSPEEDR => (uint*)0x48000408;
        public static unsafe uint* GPIOB_PUPDR => (uint*)0x4800040C;
        public static unsafe uint* GPIOB_IDR => (uint*)0x48000410;
        public static unsafe uint* GPIOB_ODR => (uint*)0x48000414;
        public static unsafe uint* GPIOB_BSRR => (uint*)0x48000418;
        public static unsafe uint* GPIOB_BRR => (uint*)0x48000428;

        // Low-power UART 1
        public const int LPUART1_BASE = 0x40008000;
        public static unsafe uint* LPUART1_CR1 => (uint*)0x40008000;
        public static unsafe uint* LPUART1_BRR => (uint*)0x4000800C;
        public static unsafe uint* LPUART1_RDR => (uint*)0x40008024;
        public static unsafe uint* LPUART1_TDR => (uint*)0x40008028;

        // 中断向量定义
        public const int IRQ_RESET = 0;  // 
        public const int IRQ_SVCALL = 11;  // 
        public const int IRQ_LPUART1 = 53;  // LPUART1 Global Interrupt

        public static void stm32l432_init()
        {
            // 硬件初始化代码
        }
    }
}
