using System;

namespace VML.Device.STMicroelectronics.STM32F072
{
    /// <summary>
    /// STM32F072 寄存器定义
    /// 生成自: STMicroelectronics/STM32/STM32F072
    /// 版本: 1.0
    /// </summary>
    public static class STM32F072
    {
        // CPU架构: ARM-Cortex-M0, 32位, 48000000 Hz

        // 寄存器定义
        public const int R0_ADDR = 0x00;
        public static unsafe uint* R0 => (uint*)0x00;

        public const int R1_ADDR = 0x04;
        public static unsafe uint* R1 => (uint*)0x04;

        public const int R2_ADDR = 0x08;
        public static unsafe uint* R2 => (uint*)0x08;

        public const int R3_ADDR = 0x0C;
        public static unsafe uint* R3 => (uint*)0x0C;

        public const int SP_ADDR = 0x34;
        public static unsafe uint* SP => (uint*)0x34;

        public const int LR_ADDR = 0x38;
        public static unsafe uint* LR => (uint*)0x38;

        public const int PC_ADDR = 0x3C;
        public static unsafe uint* PC => (uint*)0x3C;

        // 内存段定义
        public const int FLASH_START = 0x08000000;
        public const int FLASH_END = 0x0801FFFF;
        public const int FLASH_SIZE = 131072;

        public const int SRAM_START = 0x20000000;
        public const int SRAM_END = 0x20003FFF;
        public const int SRAM_SIZE = 16384;

        public const int PERIPHERAL_START = 0x40000000;
        public const int PERIPHERAL_END = 0x40027FFF;
        public const int PERIPHERAL_SIZE = 163840;

        // 外设定义
        // Reset and Clock Control
        public const int RCC_BASE = 0x40021000;
        public static unsafe uint* RCC_CR => (uint*)0x40021000;
        public static unsafe uint* RCC_CFGR => (uint*)0x40021004;
        public static unsafe uint* RCC_AHBENR => (uint*)0x40021014;
        public const int RCC_AHBENR_GPIOAEN = 17;  // GPIOA clock enable
        public const int RCC_AHBENR_GPIOBEN = 18;  // GPIOB clock enable
        public const int RCC_AHBENR_GPIOCEN = 19;  // GPIOC clock enable
        public static unsafe uint* RCC_APB2ENR => (uint*)0x40021018;

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

        // General Purpose I/O Port C
        public const int GPIOC_BASE = 0x48000800;
        public static unsafe uint* GPIOC_MODER => (uint*)0x48000800;
        public static unsafe uint* GPIOC_OTYPER => (uint*)0x48000804;
        public static unsafe uint* GPIOC_IDR => (uint*)0x48000810;
        public static unsafe uint* GPIOC_ODR => (uint*)0x48000814;
        public static unsafe uint* GPIOC_BSRR => (uint*)0x48000818;

        // 中断向量定义
        public const int IRQ_RESET = 0;  // 
        public const int IRQ_SVCALL = 11;  // 

        public static void stm32f072_init()
        {
            // 硬件初始化代码
        }
    }
}
