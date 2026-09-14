using System;

namespace VML.Device.GigaDevice.GD32VF103
{
    /// <summary>
    /// GD32VF103 寄存器定义
    /// 生成自: GigaDevice/GD32/GD32VF103
    /// 版本: 1.0
    /// </summary>
    public static class GD32VF103
    {
        // CPU架构: RISC-V, 32位, 108000000 Hz

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
        public const int FLASH_END = 0x0801FFFF;
        public const int FLASH_SIZE = 131072;

        public const int SRAM_START = 0x20000000;
        public const int SRAM_END = 0x20007FFF;
        public const int SRAM_SIZE = 32768;

        public const int PERIPHERAL_START = 0x40000000;
        public const int PERIPHERAL_END = 0x4003FFFF;
        public const int PERIPHERAL_SIZE = 262144;

        // 外设定义
        // Reset and Clock Control
        public const int RCU_BASE = 0x40021000;
        public static unsafe uint* RCU_CTL => (uint*)0x40021000;
        public static unsafe uint* RCU_CFG0 => (uint*)0x40021004;
        public static unsafe uint* RCU_CFG1 => (uint*)0x40021008;
        public static unsafe uint* RCU_APB2EN => (uint*)0x40021018;
        public const int RCU_APB2EN_PAEN = 2;  // GPIOA enable
        public const int RCU_APB2EN_PBEN = 3;  // GPIOB enable
        public const int RCU_APB2EN_PCEN = 4;  // GPIOC enable
        public const int RCU_APB2EN_USART0EN = 14;  // USART0 enable
        public static unsafe uint* RCU_APB1EN => (uint*)0x4002101C;

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

        // 中断向量定义
        public const int IRQ_RESET = 1;  // 
        public const int IRQ_MACHINESOFTWARE = 3;  // 
        public const int IRQ_MACHINETIMER = 7;  // 
        public const int IRQ_MACHINEEXTERNAL = 11;  // 
        public const int IRQ_USART0 = 25;  // USART0 Global Interrupt

        public static void gd32vf103_init()
        {
            // 硬件初始化代码
        }
    }
}
