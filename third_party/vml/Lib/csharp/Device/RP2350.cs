using System;

namespace VML.Device.Raspberry.RP2350
{
    /// <summary>
    /// RP2350 寄存器定义
    /// 生成自: Raspberry/RP2/RP2350
    /// 版本: 1.0
    /// </summary>
    public static class RP2350
    {
        // CPU架构: ARM-Cortex-M33, 32位, 150000000 Hz

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
        // XIP Flash
        public const int FLASH_START = 0x10000000;
        public const int FLASH_END = 0x107FFFFF;
        public const int FLASH_SIZE = 8388608;

        // Total SRAM
        public const int SRAM_START = 0x20000000;
        public const int SRAM_END = 0x20081FFF;
        public const int SRAM_SIZE = 532480;

        public const int PERIPHERAL_START = 0x40000000;
        public const int PERIPHERAL_END = 0x5000FFFF;
        public const int PERIPHERAL_SIZE = 16777216;

        // 外设定义
        // Single-Cycle I/O (GPIO)
        public const int SIO_BASE = 0xD0000000;
        public static unsafe uint* SIO_GPIO_IN => (uint*)0xD0000004;
        public static unsafe uint* SIO_GPIO_OUT => (uint*)0xD0000010;
        public static unsafe uint* SIO_GPIO_OUT_SET => (uint*)0xD0000014;
        public static unsafe uint* SIO_GPIO_OUT_CLR => (uint*)0xD0000018;
        public static unsafe uint* SIO_GPIO_OUT_XOR => (uint*)0xD000001C;
        public static unsafe uint* SIO_GPIO_OE => (uint*)0xD0000020;
        public static unsafe uint* SIO_GPIO_OE_SET => (uint*)0xD0000024;
        public static unsafe uint* SIO_GPIO_OE_CLR => (uint*)0xD0000028;

        // IO Bank 0 (GPIO control)
        public const int IO_BANK0_BASE = 0x40028000;
        public static unsafe uint* IO_BANK0_GPIO0_STATUS => (uint*)0x40028000;
        public static unsafe uint* IO_BANK0_GPIO0_CTRL => (uint*)0x40028004;
        public static unsafe uint* IO_BANK0_GPIO1_STATUS => (uint*)0x40028008;
        public static unsafe uint* IO_BANK0_GPIO1_CTRL => (uint*)0x4002800C;

        // Pad controls for GPIO 0-29
        public const int PADS_BANK0_BASE = 0x4002C000;
        public static unsafe uint* PADS_BANK0_GPIO0 => (uint*)0x4002C000;
        public static unsafe uint* PADS_BANK0_GPIO1 => (uint*)0x4002C004;

        // Reset Controller
        public const int RESETS_BASE = 0x4000C000;
        public static unsafe uint* RESETS_RESET => (uint*)0x4000C000;
        public static unsafe uint* RESETS_RESET_DONE => (uint*)0x4000C008;

        // 中断向量定义
        public const int IRQ_RESET = 0;  // 
        public const int IRQ_SVCALL = 11;  // 

        public static void rp2350_init()
        {
            // 硬件初始化代码
        }
    }
}
