using System;

namespace VML.Device.GigaDevice.GD32F103
{
    /// <summary>
    /// GD32F103 寄存器定义
    /// 生成自: GigaDevice/GD32/GD32F103
    /// 版本: 1.0
    /// </summary>
    public static class GD32F103
    {
        // CPU架构: ARM-Cortex-M3, 32位, 108000000 Hz

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
        public const int FLASH_END = 0x0801FFFF;
        public const int FLASH_SIZE = 131072;

        public const int SRAM_START = 0x20000000;
        public const int SRAM_END = 0x20004FFF;
        public const int SRAM_SIZE = 20480;

        public const int PERIPHERAL_START = 0x40000000;
        public const int PERIPHERAL_END = 0x4003FFFF;
        public const int PERIPHERAL_SIZE = 262144;

        // 外设定义
        // Reset and Clock Control
        public const int RCC_BASE = 0x40021000;
        public static unsafe uint* RCC_CTLR => (uint*)0x40021000;
        public static unsafe uint* RCC_CFGR0 => (uint*)0x40021004;
        public static unsafe uint* RCC_APB2PCENR => (uint*)0x40021018;
        public const int RCC_APB2PCENR_IOPAEN = 2;  // GPIOA clock enable
        public const int RCC_APB2PCENR_IOPBEN = 3;  // GPIOB clock enable
        public const int RCC_APB2PCENR_IOPCEN = 4;  // GPIOC clock enable
        public const int RCC_APB2PCENR_USART0EN = 14;  // USART0 clock enable
        public static unsafe uint* RCC_APB1PCENR => (uint*)0x4002101C;
        public const int RCC_APB1PCENR_USART1EN = 17;  // USART1 clock enable

        // General Purpose I/O Port A
        public const int GPIOA_BASE = 0x40010800;
        public static unsafe uint* GPIOA_CTL0 => (uint*)0x40010800;
        public static unsafe uint* GPIOA_CTL1 => (uint*)0x40010804;
        public static unsafe uint* GPIOA_ISTAT => (uint*)0x40010808;
        public static unsafe uint* GPIOA_OCTL => (uint*)0x4001080C;
        public static unsafe uint* GPIOA_BOP => (uint*)0x40010810;
        public static unsafe uint* GPIOA_BC => (uint*)0x40010814;

        // General Purpose I/O Port B
        public const int GPIOB_BASE = 0x40010C00;
        public static unsafe uint* GPIOB_CTL0 => (uint*)0x40010C00;
        public static unsafe uint* GPIOB_CTL1 => (uint*)0x40010C04;
        public static unsafe uint* GPIOB_ISTAT => (uint*)0x40010C08;
        public static unsafe uint* GPIOB_OCTL => (uint*)0x40010C0C;
        public static unsafe uint* GPIOB_BOP => (uint*)0x40010C10;
        public static unsafe uint* GPIOB_BC => (uint*)0x40010C14;

        // General Purpose I/O Port C
        public const int GPIOC_BASE = 0x40011000;
        public static unsafe uint* GPIOC_CTL0 => (uint*)0x40011000;
        public static unsafe uint* GPIOC_CTL1 => (uint*)0x40011004;
        public static unsafe uint* GPIOC_ISTAT => (uint*)0x40011008;
        public static unsafe uint* GPIOC_OCTL => (uint*)0x4001100C;
        public static unsafe uint* GPIOC_BOP => (uint*)0x40011010;
        public static unsafe uint* GPIOC_BC => (uint*)0x40011014;

        // USART0
        public const int USART0_BASE = 0x40013800;
        public static unsafe uint* USART0_STATR => (uint*)0x40013800;
        public static unsafe uint* USART0_DATAR => (uint*)0x40013804;
        public static unsafe uint* USART0_BRR => (uint*)0x40013808;
        public static unsafe uint* USART0_CTLR1 => (uint*)0x4001380C;

        // USART1
        public const int USART1_BASE = 0x40004400;
        public static unsafe uint* USART1_STATR => (uint*)0x40004400;
        public static unsafe uint* USART1_DATAR => (uint*)0x40004404;
        public static unsafe uint* USART1_BRR => (uint*)0x40004408;
        public static unsafe uint* USART1_CTLR1 => (uint*)0x4000440C;

        // 中断向量定义
        public const int IRQ_RESET = 0;  // 
        public const int IRQ_SVCALL = 11;  // 
        public const int IRQ_USART0 = 25;  // USART0 Global Interrupt
        public const int IRQ_USART1 = 37;  // USART1 Global Interrupt

        public static void gd32f103_init()
        {
            // 硬件初始化代码
        }
    }
}
