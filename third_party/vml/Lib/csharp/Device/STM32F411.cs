using System;

namespace VML.Device.STMicroelectronics.STM32F411
{
    /// <summary>
    /// STM32F411 寄存器定义
    /// 生成自: STMicroelectronics/STM32/STM32F411
    /// 版本: 1.0
    /// </summary>
    public static class STM32F411
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
        public const int FLASH_START = 0x08000000;
        public const int FLASH_END = 0x0807FFFF;
        public const int FLASH_SIZE = 524288;

        public const int SRAM_START = 0x20000000;
        public const int SRAM_END = 0x2001FFFF;
        public const int SRAM_SIZE = 131072;

        public const int PERIPHERAL_START = 0x40000000;
        public const int PERIPHERAL_END = 0x400FFFFF;
        public const int PERIPHERAL_SIZE = 1048576;

        // 外设定义
        // Reset and Clock Control
        public const int RCC_BASE = 0x40023800;
        public static unsafe uint* RCC_CR => (uint*)0x40023800;
        public static unsafe uint* RCC_PLLCFGR => (uint*)0x40023804;
        public static unsafe uint* RCC_CFGR => (uint*)0x40023808;
        public static unsafe uint* RCC_AHB1ENR => (uint*)0x40023830;
        public const int RCC_AHB1ENR_GPIOAEN = 0;  // GPIOA clock enable
        public const int RCC_AHB1ENR_GPIOBEN = 1;  // GPIOB clock enable
        public const int RCC_AHB1ENR_GPIOCEN = 2;  // GPIOC clock enable
        public static unsafe uint* RCC_APB1ENR => (uint*)0x40023840;
        public static unsafe uint* RCC_APB2ENR => (uint*)0x40023844;

        // General Purpose I/O Port A
        public const int GPIOA_BASE = 0x40020000;
        public static unsafe uint* GPIOA_MODER => (uint*)0x40020000;
        public static unsafe uint* GPIOA_OTYPER => (uint*)0x40020004;
        public static unsafe uint* GPIOA_OSPEEDR => (uint*)0x40020008;
        public static unsafe uint* GPIOA_PUPDR => (uint*)0x4002000C;
        public static unsafe uint* GPIOA_IDR => (uint*)0x40020010;
        public static unsafe uint* GPIOA_ODR => (uint*)0x40020014;
        public static unsafe uint* GPIOA_BSRR => (uint*)0x40020018;
        public static unsafe uint* GPIOA_BRR => (uint*)0x40020028;

        // General Purpose I/O Port B
        public const int GPIOB_BASE = 0x40020400;
        public static unsafe uint* GPIOB_MODER => (uint*)0x40020400;
        public static unsafe uint* GPIOB_OTYPER => (uint*)0x40020404;
        public static unsafe uint* GPIOB_OSPEEDR => (uint*)0x40020408;
        public static unsafe uint* GPIOB_PUPDR => (uint*)0x4002040C;
        public static unsafe uint* GPIOB_IDR => (uint*)0x40020410;
        public static unsafe uint* GPIOB_ODR => (uint*)0x40020414;
        public static unsafe uint* GPIOB_BSRR => (uint*)0x40020418;
        public static unsafe uint* GPIOB_BRR => (uint*)0x40020428;

        // General Purpose I/O Port C
        public const int GPIOC_BASE = 0x40020800;
        public static unsafe uint* GPIOC_MODER => (uint*)0x40020800;
        public static unsafe uint* GPIOC_OTYPER => (uint*)0x40020804;
        public static unsafe uint* GPIOC_IDR => (uint*)0x40020810;
        public static unsafe uint* GPIOC_ODR => (uint*)0x40020814;
        public static unsafe uint* GPIOC_BSRR => (uint*)0x40020818;

        // Universal Synchronous/Asynchronous Receiver/Transmitter 1
        public const int USART1_BASE = 0x40011000;
        public static unsafe uint* USART1_SR => (uint*)0x40011000;
        public static unsafe uint* USART1_DR => (uint*)0x40011004;
        public static unsafe uint* USART1_BRR => (uint*)0x40011008;
        public static unsafe uint* USART1_CR1 => (uint*)0x4001100C;

        // 中断向量定义
        public const int IRQ_RESET = 0;  // 
        public const int IRQ_SVCALL = 11;  // 
        public const int IRQ_USART1 = 37;  // USART1 Global Interrupt

        public static void stm32f411_init()
        {
            // 硬件初始化代码
        }
    }
}
