using System;

namespace VML.Device.Infineon.XMC4500
{
    /// <summary>
    /// XMC4500 寄存器定义
    /// 生成自: Infineon/XMC4000/XMC4500
    /// 版本: 1.0
    /// </summary>
    public static class XMC4500
    {
        // CPU架构: ARM-Cortex-M4, 32位, 120000000 Hz

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
        public const int FLASH_END = 0x080FFFFF;
        public const int FLASH_SIZE = 1048576;

        public const int SRAM_START = 0x1FF00000;
        public const int SRAM_END = 0x1FF0FFFF;
        public const int SRAM_SIZE = 65536;

        // Communication Memory
        public const int SRAM_COM_START = 0x20000000;
        public const int SRAM_COM_END = 0x20007FFF;
        public const int SRAM_COM_SIZE = 32768;

        // CPU SRAM
        public const int SRAM_CPU_START = 0x20010000;
        public const int SRAM_CPU_END = 0x2001FFFF;
        public const int SRAM_CPU_SIZE = 65536;

        public const int PERIPHERAL_START = 0x40000000;
        public const int PERIPHERAL_END = 0x4FFFFFFF;
        public const int PERIPHERAL_SIZE = 268435456;

        // 外设定义
        // System Control Unit
        public const int SCU_BASE = 0x40020000;
        public static unsafe uint* SCU_CLKCR => (uint*)0x40020000;
        public const int SCU_CLKCR_PCLK_SEL = 0;  // CPU clock selection
        public const int SCU_CLKCR_FBKDIV = 16;  // Feedback divider
        public static unsafe uint* SCU_PLLCONFIG => (uint*)0x40020004;
        public static unsafe uint* SCU_OSCHPCTRL => (uint*)0x40020008;
        public static unsafe uint* SCU_CGATSET0 => (uint*)0x40020020;
        public const int SCU_CGATSET0_CG_GATE_GPIO = 4;  // GPIO gate enable
        public static unsafe uint* SCU_CGATCLR0 => (uint*)0x40020024;

        // Port 0
        public const int PORT0_BASE = 0x48000000;
        public static unsafe uint* PORT0_OUT => (uint*)0x48000000;
        public static unsafe uint* PORT0_OMR => (uint*)0x48000004;
        public static unsafe uint* PORT0_IOCR0 => (uint*)0x48000010;
        public static unsafe uint* PORT0_IOCR4 => (uint*)0x48000014;
        public static unsafe uint* PORT0_IOCR8 => (uint*)0x48000018;
        public static unsafe uint* PORT0_IOCR12 => (uint*)0x4800001C;
        public static unsafe uint* PORT0_IN => (uint*)0x48000024;

        // Port 1
        public const int PORT1_BASE = 0x48010000;
        public static unsafe uint* PORT1_OUT => (uint*)0x48010000;
        public static unsafe uint* PORT1_OMR => (uint*)0x48010004;
        public static unsafe uint* PORT1_IOCR0 => (uint*)0x48010010;
        public static unsafe uint* PORT1_IOCR4 => (uint*)0x48010014;
        public static unsafe uint* PORT1_IOCR8 => (uint*)0x48010018;
        public static unsafe uint* PORT1_IOCR12 => (uint*)0x4801001C;
        public static unsafe uint* PORT1_IN => (uint*)0x48010024;

        // Port 2
        public const int PORT2_BASE = 0x48020000;
        public static unsafe uint* PORT2_OUT => (uint*)0x48020000;
        public static unsafe uint* PORT2_OMR => (uint*)0x48020004;
        public static unsafe uint* PORT2_IOCR0 => (uint*)0x48020010;
        public static unsafe uint* PORT2_IOCR4 => (uint*)0x48020014;
        public static unsafe uint* PORT2_IN => (uint*)0x48020024;

        // Universal Serial Interface 0 (UART)
        public const int USIC0_BASE = 0x48030000;
        public static unsafe uint* USIC0_CCR => (uint*)0x48030000;
        public static unsafe uint* USIC0_PCR => (uint*)0x48030004;
        public static unsafe uint* USIC0_RBUF => (uint*)0x48030008;
        public static unsafe uint* USIC0_TBUF => (uint*)0x4803000C;
        public static unsafe uint* USIC0_BRG => (uint*)0x48030010;

        // 中断向量定义
        public const int IRQ_RESET = 0;  // 
        public const int IRQ_SVCALL = 11;  // 
        public const int IRQ_USIC0_SR0 = 12;  // USIC0 Service Request 0

        public static void xmc4500_init()
        {
            // 硬件初始化代码
        }
    }
}
