using System;

namespace VML.Device.Espressif.ESP32_C3
{
    /// <summary>
    /// ESP32-C3 寄存器定义
    /// 生成自: Espressif/ESP32-C/ESP32-C3
    /// 版本: 1.0
    /// </summary>
    public static class ESP32_C3
    {
        // CPU架构: RISC-V, 32位, 160000000 Hz

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
        // Flash via Cache
        public const int FLASH_START = 0x42000000;
        public const int FLASH_END = 0x427FFFFF;
        public const int FLASH_SIZE = 8388608;

        // Internal SRAM
        public const int SRAM_START = 0x3FC80000;
        public const int SRAM_END = 0x3FCE3FFF;
        public const int SRAM_SIZE = 409600;

        public const int PERIPHERAL_START = 0x60000000;
        public const int PERIPHERAL_END = 0x600FFFFF;
        public const int PERIPHERAL_SIZE = 1048576;

        // 外设定义
        // General Purpose I/O
        public const int GPIO_BASE = 0x60004000;
        public static unsafe uint* GPIO_OUT => (uint*)0x60004004;
        public static unsafe uint* GPIO_OUT_W1TS => (uint*)0x60004008;
        public static unsafe uint* GPIO_OUT_W1TC => (uint*)0x6000400C;
        public static unsafe uint* GPIO_IN => (uint*)0x60004010;
        public static unsafe uint* GPIO_ENABLE => (uint*)0x60004020;
        public static unsafe uint* GPIO_ENABLE_W1TS => (uint*)0x60004024;
        public static unsafe uint* GPIO_ENABLE_W1TC => (uint*)0x60004028;

        // I/O MUX
        public const int IO_MUX_BASE = 0x60009000;
        public static unsafe uint* IO_MUX_GPIO0 => (uint*)0x60009000;
        public static unsafe uint* IO_MUX_GPIO1 => (uint*)0x60009004;
        public static unsafe uint* IO_MUX_GPIO2 => (uint*)0x60009008;
        public static unsafe uint* IO_MUX_GPIO3 => (uint*)0x6000900C;

        // RTC Control
        public const int RTC_CNTL_BASE = 0x60008000;
        public static unsafe uint* RTC_CNTL_OPTIONS0 => (uint*)0x60008000;
        public static unsafe uint* RTC_CNTL_CLK_CONF => (uint*)0x60008030;

        // 中断向量定义
        public const int IRQ_RESET = 1;  // 
        public const int IRQ_MACHINESOFTWARE = 3;  // 
        public const int IRQ_MACHINETIMER = 7;  // 
        public const int IRQ_MACHINEEXTERNAL = 11;  // 

        public static void esp32_c3_init()
        {
            // 硬件初始化代码
        }
    }
}
