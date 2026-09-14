using System;

namespace VML.Device.STMicroelectronics.STM32H743
{
    /// <summary>
    /// STM32H743 寄存器定义
    /// 生成自: STMicroelectronics/STM32/STM32H743
    /// 版本: 1.0
    /// </summary>
    public static class STM32H743
    {
        // CPU架构: ARM-Cortex-M7, 32位, 400000000 Hz

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
        public const int FLASH_END = 0x081FFFFF;
        public const int FLASH_SIZE = 2097152;

        // DTCM RAM
        public const int DTCM_START = 0x20000000;
        public const int DTCM_END = 0x2001FFFF;
        public const int DTCM_SIZE = 131072;

        // ITCM RAM
        public const int ITCM_START = 0x00000000;
        public const int ITCM_END = 0x0000FFFF;
        public const int ITCM_SIZE = 65536;

        // AXI SRAM
        public const int SRAM_AXI_START = 0x24000000;
        public const int SRAM_AXI_END = 0x2407FFFF;
        public const int SRAM_AXI_SIZE = 524288;

        // SRAM1-3
        public const int SRAM_SRAM_START = 0x30000000;
        public const int SRAM_SRAM_END = 0x3003FFFF;
        public const int SRAM_SRAM_SIZE = 262144;

        public const int PERIPHERAL_START = 0x40000000;
        public const int PERIPHERAL_END = 0x4FFFFFFF;
        public const int PERIPHERAL_SIZE = 268435456;

        // 外设定义
        // Reset and Clock Control
        public const int RCC_BASE = 0x58024400;
        public static unsafe uint* RCC_CR => (uint*)0x58024400;
        public static unsafe uint* RCC_CFGR => (uint*)0x58024404;
        public static unsafe uint* RCC_PLL1CFGR => (uint*)0x5802440C;
        public static unsafe uint* RCC_AHB1ENR => (uint*)0x58024430;
        public const int RCC_AHB1ENR_GPIOAEN = 0;  // GPIOA clock enable
        public const int RCC_AHB1ENR_GPIOBEN = 1;  // GPIOB clock enable
        public const int RCC_AHB1ENR_GPIOCEN = 2;  // GPIOC clock enable
        public const int RCC_AHB1ENR_GPIODEN = 3;  // GPIOD clock enable
        public const int RCC_AHB1ENR_GPIOEEN = 4;  // GPIOE clock enable
        public const int RCC_AHB1ENR_DMA1EN = 21;  // DMA1 clock enable
        public const int RCC_AHB1ENR_DMA2EN = 22;  // DMA2 clock enable
        public static unsafe uint* RCC_AHB2ENR => (uint*)0x58024434;
        public static unsafe uint* RCC_AHB4ENR => (uint*)0x5802443C;
        public static unsafe uint* RCC_APB1LENR => (uint*)0x58024450;
        public static unsafe uint* RCC_APB2ENR => (uint*)0x58024458;

        // General Purpose I/O Port A
        public const int GPIOA_BASE = 0x58020000;
        public static unsafe uint* GPIOA_MODER => (uint*)0x58020000;
        public static unsafe uint* GPIOA_OTYPER => (uint*)0x58020004;
        public static unsafe uint* GPIOA_OSPEEDR => (uint*)0x58020008;
        public static unsafe uint* GPIOA_PUPDR => (uint*)0x5802000C;
        public static unsafe uint* GPIOA_IDR => (uint*)0x58020010;
        public static unsafe uint* GPIOA_ODR => (uint*)0x58020014;
        public static unsafe uint* GPIOA_BSRR => (uint*)0x58020018;
        public static unsafe uint* GPIOA_BRR => (uint*)0x58020028;

        // General Purpose I/O Port B
        public const int GPIOB_BASE = 0x58020400;
        public static unsafe uint* GPIOB_MODER => (uint*)0x58020400;
        public static unsafe uint* GPIOB_OTYPER => (uint*)0x58020404;
        public static unsafe uint* GPIOB_OSPEEDR => (uint*)0x58020408;
        public static unsafe uint* GPIOB_PUPDR => (uint*)0x5802040C;
        public static unsafe uint* GPIOB_IDR => (uint*)0x58020410;
        public static unsafe uint* GPIOB_ODR => (uint*)0x58020414;
        public static unsafe uint* GPIOB_BSRR => (uint*)0x58020418;
        public static unsafe uint* GPIOB_BRR => (uint*)0x58020428;

        // General Purpose I/O Port C
        public const int GPIOC_BASE = 0x58020800;
        public static unsafe uint* GPIOC_MODER => (uint*)0x58020800;
        public static unsafe uint* GPIOC_OTYPER => (uint*)0x58020804;
        public static unsafe uint* GPIOC_IDR => (uint*)0x58020810;
        public static unsafe uint* GPIOC_ODR => (uint*)0x58020814;
        public static unsafe uint* GPIOC_BSRR => (uint*)0x58020818;

        // General Purpose I/O Port D
        public const int GPIOD_BASE = 0x58020C00;
        public static unsafe uint* GPIOD_MODER => (uint*)0x58020C00;
        public static unsafe uint* GPIOD_OTYPER => (uint*)0x58020C04;
        public static unsafe uint* GPIOD_IDR => (uint*)0x58020C10;
        public static unsafe uint* GPIOD_ODR => (uint*)0x58020C14;
        public static unsafe uint* GPIOD_BSRR => (uint*)0x58020C18;

        // General Purpose I/O Port E
        public const int GPIOE_BASE = 0x58021000;
        public static unsafe uint* GPIOE_MODER => (uint*)0x58021000;
        public static unsafe uint* GPIOE_OTYPER => (uint*)0x58021004;
        public static unsafe uint* GPIOE_IDR => (uint*)0x58021010;
        public static unsafe uint* GPIOE_ODR => (uint*)0x58021014;
        public static unsafe uint* GPIOE_BSRR => (uint*)0x58021018;

        // USART1
        public const int USART1_BASE = 0x40011000;
        public static unsafe uint* USART1_CR1 => (uint*)0x40011000;
        public static unsafe uint* USART1_BRR => (uint*)0x4001100C;
        public static unsafe uint* USART1_RDR => (uint*)0x40011024;
        public static unsafe uint* USART1_TDR => (uint*)0x40011028;

        // 中断向量定义
        public const int IRQ_RESET = 0;  // 
        public const int IRQ_SVCALL = 11;  // 
        public const int IRQ_SYSTICK = 15;  // 
        public const int IRQ_USART1 = 56;  // USART1 Global Interrupt

        public static void stm32h743_init()
        {
            // 硬件初始化代码
        }
    }
}
