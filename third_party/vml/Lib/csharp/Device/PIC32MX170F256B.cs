using System;

namespace VML.Device.Microchip.PIC32MX170F256B
{
    /// <summary>
    /// PIC32MX170F256B 寄存器定义
    /// 生成自: Microchip/PIC32/PIC32MX170F256B
    /// 版本: 1.0
    /// </summary>
    public static class PIC32MX170F256B
    {
        // CPU架构: MIPS32-M4K, 32位, 50000000 Hz

        // 寄存器定义
        // Hard-wired zero
        public const int _0_ADDR = 0x00;
        public static unsafe uint* $0 => (uint*)0x00;

        // AT
        public const int _1_ADDR = 0x04;
        public static unsafe uint* $1 => (uint*)0x04;

        // V0
        public const int _2_ADDR = 0x08;
        public static unsafe uint* $2 => (uint*)0x08;

        // V1
        public const int _3_ADDR = 0x0C;
        public static unsafe uint* $3 => (uint*)0x0C;

        // A0
        public const int _4_ADDR = 0x10;
        public static unsafe uint* $4 => (uint*)0x10;

        // A1
        public const int _5_ADDR = 0x14;
        public static unsafe uint* $5 => (uint*)0x14;

        // Stack Pointer (SP)
        public const int _29_ADDR = 0x74;
        public static unsafe uint* $29 => (uint*)0x74;

        // Return Address (RA)
        public const int _31_ADDR = 0x7C;
        public static unsafe uint* $31 => (uint*)0x7C;

        // Program Counter
        public const int PC_ADDR = 0x80;
        public static unsafe uint* PC => (uint*)0x80;

        // 内存段定义
        // Program Flash
        public const int FLASH_START = 0x9D000000;
        public const int FLASH_END = 0x9D03FFFF;
        public const int FLASH_SIZE = 262144;

        public const int SRAM_START = 0xA0000000;
        public const int SRAM_END = 0xA000FFFF;
        public const int SRAM_SIZE = 65536;

        public const int PERIPHERAL_START = 0xBF800000;
        public const int PERIPHERAL_END = 0xBF8FFFFF;
        public const int PERIPHERAL_SIZE = 1048576;

        // Boot Flash
        public const int BOOTFLASH_START = 0xBFC00000;
        public const int BOOTFLASH_END = 0xBFC02FFF;
        public const int BOOTFLASH_SIZE = 12288;

        // 外设定义
        // General Purpose I/O Port A
        public const int PORTA_BASE = 0xBF886000;
        public static unsafe uint* PORTA_TRISA => (uint*)0xBF886000;
        public static unsafe uint* PORTA_PORTA => (uint*)0xBF886010;
        public static unsafe uint* PORTA_LATA => (uint*)0xBF886020;
        public static unsafe uint* PORTA_ODCA => (uint*)0xBF886030;

        // General Purpose I/O Port B
        public const int PORTB_BASE = 0xBF886100;
        public static unsafe uint* PORTB_TRISB => (uint*)0xBF886100;
        public static unsafe uint* PORTB_PORTB => (uint*)0xBF886110;
        public static unsafe uint* PORTB_LATB => (uint*)0xBF886120;
        public static unsafe uint* PORTB_ODCB => (uint*)0xBF886130;

        // UART1
        public const int UART1_BASE = 0xBF822000;
        public static unsafe uint* UART1_UXMODE => (uint*)0xBF822000;
        public static unsafe uint* UART1_UXSTA => (uint*)0xBF822004;
        public static unsafe uint* UART1_UXTXREG => (uint*)0xBF822008;
        public static unsafe uint* UART1_UXRXREG => (uint*)0xBF82200C;
        public static unsafe uint* UART1_UXBRG => (uint*)0xBF822010;

        // 中断向量定义
        public const int IRQ_RESET = 0;  // 
        public const int IRQ_UART1 = 8;  // UART1 Interrupt

        public static void pic32mx170f256b_init()
        {
            // 硬件初始化代码
        }
    }
}
