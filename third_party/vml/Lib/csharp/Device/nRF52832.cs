using System;

namespace VML.Device.Nordic.nRF52832
{
    /// <summary>
    /// nRF52832 寄存器定义
    /// 生成自: Nordic/nRF52/nRF52832
    /// 版本: 1.0
    /// </summary>
    public static class nRF52832
    {
        // CPU架构: ARM-Cortex-M4F, 32位, 64000000 Hz

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
        public const int FLASH_START = 0x00000000;
        public const int FLASH_END = 0x0007FFFF;
        public const int FLASH_SIZE = 524288;

        public const int SRAM_START = 0x20000000;
        public const int SRAM_END = 0x2000FFFF;
        public const int SRAM_SIZE = 65536;

        public const int PERIPHERAL_START = 0x40000000;
        public const int PERIPHERAL_END = 0x400FFFFF;
        public const int PERIPHERAL_SIZE = 1048576;

        // Factory Information Configuration Registers
        public const int FICR_START = 0x10000000;
        public const int FICR_END = 0x10000FFF;
        public const int FICR_SIZE = 4096;

        // 外设定义
        // General Purpose I/O Port 0
        public const int GPIO_P0_BASE = 0x50000000;
        public static unsafe uint* GPIO_P0_OUT => (uint*)0x50000504;
        public static unsafe uint* GPIO_P0_OUTSET => (uint*)0x50000508;
        public static unsafe uint* GPIO_P0_OUTCLR => (uint*)0x5000050C;
        public static unsafe uint* GPIO_P0_IN => (uint*)0x50000510;
        public static unsafe uint* GPIO_P0_DIR => (uint*)0x50000514;
        public static unsafe uint* GPIO_P0_DIRSET => (uint*)0x50000518;
        public static unsafe uint* GPIO_P0_DIRCLR => (uint*)0x5000051C;

        // Power Control
        public const int POWER_BASE = 0x40000000;
        public static unsafe uint* POWER_DCDCEN => (uint*)0x400001C4;
        public static unsafe uint* POWER_RAMSTATUS => (uint*)0x40000268;

        // Clock Control
        public const int CLOCK_BASE = 0x40000000;
        public static unsafe uint* CLOCK_HFCLKSTART => (uint*)0x40000108;
        public static unsafe uint* CLOCK_HFCLKSTARTED => (uint*)0x40000208;

        // 中断向量定义
        public const int IRQ_RESET = 0;  // 
        public const int IRQ_SVCALL = 11;  // 

        public static void nrf52832_init()
        {
            // 硬件初始化代码
        }
    }
}
