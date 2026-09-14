using System;

namespace VML.Device.TexasInstruments.TMS320F280049
{
    /// <summary>
    /// TMS320F280049 寄存器定义
    /// 生成自: Texas Instruments/C2000/TMS320F280049
    /// 版本: 1.0
    /// </summary>
    public static class TMS320F280049
    {
        // CPU架构: C28x-DSP, 32位, 100000000 Hz

        // 寄存器定义
        // Accumulator Low
        public const int AL_ADDR = 0x00;
        public static unsafe ushort* AL => (ushort*)0x00;

        // Accumulator High
        public const int AH_ADDR = 0x02;
        public static unsafe ushort* AH => (ushort*)0x02;

        // Product High
        public const int PH_ADDR = 0x04;
        public static unsafe ushort* PH => (ushort*)0x04;

        // Product Low
        public const int PL_ADDR = 0x06;
        public static unsafe ushort* PL => (ushort*)0x06;

        // Temporary Register
        public const int TREG_ADDR = 0x08;
        public static unsafe ushort* TREG => (ushort*)0x08;

        public const int AR0_ADDR = 0x0A;
        public static unsafe ushort* AR0 => (ushort*)0x0A;

        public const int AR1_ADDR = 0x0C;
        public static unsafe ushort* AR1 => (ushort*)0x0C;

        // Status 0
        public const int ST0_ADDR = 0x20;
        public static unsafe ushort* ST0 => (ushort*)0x20;

        // Status 1
        public const int ST1_ADDR = 0x22;
        public static unsafe ushort* ST1 => (ushort*)0x22;

        // Program Counter
        public const int PC_ADDR = 0x24;
        public static unsafe ushort* PC => (ushort*)0x24;

        // Stack Pointer
        public const int SP_ADDR = 0x26;
        public static unsafe ushort* SP => (ushort*)0x26;

        // 内存段定义
        public const int FLASH_START = 0x080000;
        public const int FLASH_END = 0x0BFFFF;
        public const int FLASH_SIZE = 262144;

        // Local Shared RAM
        public const int SRAM_LS_START = 0x008000;
        public const int SRAM_LS_END = 0x00BFFF;
        public const int SRAM_LS_SIZE = 16384;

        // Global Shared RAM
        public const int SRAM_GS_START = 0x00C000;
        public const int SRAM_GS_END = 0x01FFFF;
        public const int SRAM_GS_SIZE = 81920;

        public const int PERIPHERAL_START = 0x400000;
        public const int PERIPHERAL_END = 0x40FFFF;
        public const int PERIPHERAL_SIZE = 65536;

        // 外设定义
        // PLL Clock Control
        public const int PLL_BASE = 0x5C10;
        public static unsafe ushort* PLL_SYSPLLCTL1 => (ushort*)0x00005C10;
        public static unsafe ushort* PLL_SYSPLLCTL2 => (ushort*)0x00005C12;
        public static unsafe ushort* PLL_CLKSRCCTL1 => (ushort*)0x00005C14;
        public static unsafe ushort* PLL_CLKSRCCTL2 => (ushort*)0x00005C16;

        // GPIO Control Registers
        public const int GPIO_CTRL_BASE = 0x7C00;
        public static unsafe ushort* GPIO_CTRL_GPACTRL => (ushort*)0x00007C00;
        public static unsafe ushort* GPIO_CTRL_GPAQSEL1 => (ushort*)0x00007C02;
        public static unsafe ushort* GPIO_CTRL_GPAQSEL2 => (ushort*)0x00007C04;
        public static unsafe ushort* GPIO_CTRL_GPAMUX1 => (ushort*)0x00007C06;
        public static unsafe ushort* GPIO_CTRL_GPAMUX2 => (ushort*)0x00007C08;
        public static unsafe ushort* GPIO_CTRL_GPADIR => (ushort*)0x00007C0A;
        public static unsafe ushort* GPIO_CTRL_GPAPUD => (ushort*)0x00007C0C;

        // GPIO Data Registers
        public const int GPIO_DATA_BASE = 0x7F00;
        public static unsafe ushort* GPIO_DATA_GPADAT => (ushort*)0x00007F00;
        public static unsafe ushort* GPIO_DATA_GPASET => (ushort*)0x00007F02;
        public static unsafe ushort* GPIO_DATA_GPACLEAR => (ushort*)0x00007F04;
        public static unsafe ushort* GPIO_DATA_GPATOGGLE => (ushort*)0x00007F06;
        public static unsafe ushort* GPIO_DATA_GPBDAT => (ushort*)0x00007F08;
        public static unsafe ushort* GPIO_DATA_GPBSET => (ushort*)0x00007F0A;
        public static unsafe ushort* GPIO_DATA_GPBCLEAR => (ushort*)0x00007F0C;
        public static unsafe ushort* GPIO_DATA_GPBTOGGLE => (ushort*)0x00007F0E;

        // GPIO B Control
        public const int GPIO_B_CTRL_BASE = 0x7C20;
        public static unsafe ushort* GPIO_B_CTRL_GPBMUX1 => (ushort*)0x00007C20;
        public static unsafe ushort* GPIO_B_CTRL_GPBMUX2 => (ushort*)0x00007C22;
        public static unsafe ushort* GPIO_B_CTRL_GPBDIR => (ushort*)0x00007C24;
        public static unsafe ushort* GPIO_B_CTRL_GPBPUD => (ushort*)0x00007C26;

        // SCI-A UART
        public const int SCI_A_BASE = 0x7320;
        public static unsafe ushort* SCI_A_SCICCR => (ushort*)0x00007320;
        public static unsafe ushort* SCI_A_SCICTL1 => (ushort*)0x00007322;
        public static unsafe ushort* SCI_A_SCIBAUD => (ushort*)0x00007324;
        public static unsafe ushort* SCI_A_SCIRXBUF => (ushort*)0x0000732A;
        public static unsafe ushort* SCI_A_SCITXBUF => (ushort*)0x0000732C;

        // 中断向量定义
        public const int IRQ_RESET = 1;  // 
        public const int IRQ_SCIA_RX = 8;  // SCI-A Receive Interrupt
        public const int IRQ_SCIA_TX = 9;  // SCI-A Transmit Interrupt

        public static void tms320f280049_init()
        {
            // 硬件初始化代码
        }
    }
}
