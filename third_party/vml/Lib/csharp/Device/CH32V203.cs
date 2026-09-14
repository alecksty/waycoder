using System;

namespace VML.Device.WCH.CH32V203
{
    /// <summary>
    /// CH32V203 寄存器定义
    /// 生成自: WCH/CH32V2/CH32V203
    /// 版本: 1.0
    /// </summary>
    public static class CH32V203
    {
        // CPU架构: RISC-V, 32位, 144000000 Hz

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
        public const int FLASH_START = 0x08000000;
        public const int FLASH_END = 0x0800FFFF;
        public const int FLASH_SIZE = 65536;

        public const int SRAM_START = 0x20000000;
        public const int SRAM_END = 0x20004FFF;
        public const int SRAM_SIZE = 20480;

        public const int PERIPHERAL_START = 0x40000000;
        public const int PERIPHERAL_END = 0x4003FFFF;
        public const int PERIPHERAL_SIZE = 262144;

        // 外设定义
        // Reset and Clock Control
        public const int RCC_BASE = 0x40021000;
        public static unsafe uint* RCC_RCC_CTLR => (uint*)0x40021000;
        public static unsafe uint* RCC_RCC_CFGR0 => (uint*)0x40021004;
        public static unsafe uint* RCC_RCC_APB2PCENR => (uint*)0x40021018;
        public const int RCC_RCC_APB2PCENR_IOPAEN = 2;  // GPIOA clock enable
        public const int RCC_RCC_APB2PCENR_IOPBEN = 3;  // GPIOB clock enable
        public const int RCC_RCC_APB2PCENR_IOPCEN = 4;  // GPIOC clock enable

        // General Purpose I/O Port A
        public const int GPIOA_BASE = 0x40010800;
        public static unsafe uint* GPIOA_CFGLR => (uint*)0x40010800;
        public static unsafe uint* GPIOA_CFGHR => (uint*)0x40010804;
        public static unsafe uint* GPIOA_INDR => (uint*)0x40010808;
        public static unsafe uint* GPIOA_OUTDR => (uint*)0x4001080C;
        public static unsafe uint* GPIOA_BSHR => (uint*)0x40010810;
        public static unsafe uint* GPIOA_BCR => (uint*)0x40010814;

        // General Purpose I/O Port B
        public const int GPIOB_BASE = 0x40010C00;
        public static unsafe uint* GPIOB_CFGLR => (uint*)0x40010C00;
        public static unsafe uint* GPIOB_CFGHR => (uint*)0x40010C04;
        public static unsafe uint* GPIOB_INDR => (uint*)0x40010C08;
        public static unsafe uint* GPIOB_OUTDR => (uint*)0x40010C0C;
        public static unsafe uint* GPIOB_BSHR => (uint*)0x40010C10;
        public static unsafe uint* GPIOB_BCR => (uint*)0x40010C14;

        // General Purpose I/O Port C
        public const int GPIOC_BASE = 0x40011000;
        public static unsafe uint* GPIOC_CFGLR => (uint*)0x40011000;
        public static unsafe uint* GPIOC_CFGHR => (uint*)0x40011004;
        public static unsafe uint* GPIOC_INDR => (uint*)0x40011008;
        public static unsafe uint* GPIOC_OUTDR => (uint*)0x4001100C;
        public static unsafe uint* GPIOC_BSHR => (uint*)0x40011010;
        public static unsafe uint* GPIOC_BCR => (uint*)0x40011014;

        // USART1
        public const int USART1_BASE = 0x40013800;
        public static unsafe uint* USART1_USART_STATR => (uint*)0x40013800;
        public static unsafe uint* USART1_USART_DATAR => (uint*)0x40013804;
        public static unsafe uint* USART1_USART_BRR => (uint*)0x40013808;
        public static unsafe uint* USART1_USART_CTLR1 => (uint*)0x4001380C;

        // 中断向量定义
        public const int IRQ_RESET = 1;  // 
        public const int IRQ_MACHINESOFTWARE = 3;  // 
        public const int IRQ_MACHINETIMER = 7;  // 
        public const int IRQ_MACHINEEXTERNAL = 11;  // 
        public const int IRQ_USART1 = 25;  // USART1 Global Interrupt

        public static void ch32v203_init()
        {
            // 硬件初始化代码
        }
    }
}
