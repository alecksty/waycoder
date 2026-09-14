using System;

namespace VML.Device.AcornComputers.Acorn_Archimedes_A310
{
    /// <summary>
    /// Acorn-Archimedes-A310 寄存器定义
    /// 生成自: Acorn Computers/Archimedes/Acorn-Archimedes-A310
    /// 版本: 1.0
    /// </summary>
    public static class Acorn_Archimedes_A310
    {
        // CPU架构: ARM250, 32位, 26000000 Hz

        // 寄存器定义
        // General Purpose Register 0
        public const int R0_ADDR = 0x00;
        public static unsafe uint* R0 => (uint*)0x00;

        // General Purpose Register 1
        public const int R1_ADDR = 0x04;
        public static unsafe uint* R1 => (uint*)0x04;

        // General Purpose Register 2
        public const int R2_ADDR = 0x08;
        public static unsafe uint* R2 => (uint*)0x08;

        // General Purpose Register 3
        public const int R3_ADDR = 0x0C;
        public static unsafe uint* R3 => (uint*)0x0C;

        // General Purpose Register 4
        public const int R4_ADDR = 0x10;
        public static unsafe uint* R4 => (uint*)0x10;

        // General Purpose Register 5
        public const int R5_ADDR = 0x14;
        public static unsafe uint* R5 => (uint*)0x14;

        // General Purpose Register 6
        public const int R6_ADDR = 0x18;
        public static unsafe uint* R6 => (uint*)0x18;

        // General Purpose Register 7
        public const int R7_ADDR = 0x1C;
        public static unsafe uint* R7 => (uint*)0x1C;

        // General Purpose Register 8
        public const int R8_ADDR = 0x20;
        public static unsafe uint* R8 => (uint*)0x20;

        // General Purpose Register 9
        public const int R9_ADDR = 0x24;
        public static unsafe uint* R9 => (uint*)0x24;

        // General Purpose Register 10
        public const int R10_ADDR = 0x28;
        public static unsafe uint* R10 => (uint*)0x28;

        // General Purpose Register 11 (fp)
        public const int R11_ADDR = 0x2C;
        public static unsafe uint* R11 => (uint*)0x2C;

        // General Purpose Register 12
        public const int R12_ADDR = 0x30;
        public static unsafe uint* R12 => (uint*)0x30;

        // Stack Pointer (R13)
        public const int SP_ADDR = 0x34;
        public static unsafe uint* SP => (uint*)0x34;

        // Link Register (R14)
        public const int LR_ADDR = 0x38;
        public static unsafe uint* LR => (uint*)0x38;

        // Program Counter (R15)
        public const int PC_ADDR = 0x3C;
        public static unsafe uint* PC => (uint*)0x3C;

        // Processor Status Register
        public const int PSR_ADDR = 0x40;
        public static unsafe uint* PSR => (uint*)0x40;
        public const int PSR_MODE = 0;  // Mode bits (0-4)
        public const int PSR_T = 5;  // Thumb state
        public const int PSR_F = 6;  // FIQ disable
        public const int PSR_I = 7;  // IRQ disable
        public const int PSR_V = 28;  // Overflow
        public const int PSR_C = 29;  // Carry
        public const int PSR_Z = 30;  // Zero
        public const int PSR_N = 31;  // Negative

        // 内存段定义
        // RISC OS ROM (512KB)
        public const int ROM_START = 0x00000000;
        public const int ROM_END = 0x0007FFFF;
        public const int ROM_SIZE = 524288;

        // Main RAM (up to 4MB)
        public const int RAM_START = 0x00080000;
        public const int RAM_END = 0x003FFFFF;
        public const int RAM_SIZE = 3932160;

        // Video RAM (4MB, VIDC)
        public const int VRAM_START = 0x00400000;
        public const int VRAM_END = 0x007FFFFF;
        public const int VRAM_SIZE = 4194304;

        // I/O controller (IOC)
        public const int IO_START = 0x03000000;
        public const int IO_END = 0x0301FFFF;
        public const int IO_SIZE = 131072;

        // Memory Controller (MEMC)
        public const int MEMC_START = 0x03200000;
        public const int MEMC_END = 0x0320FFFF;
        public const int MEMC_SIZE = 4096;

        // Video Controller (VIDC)
        public const int VIDC_START = 0x03400000;
        public const int VIDC_END = 0x0340FFFF;
        public const int VIDC_SIZE = 4096;

        // I/O and Memory DMA
        public const int IOMD_START = 0x03300000;
        public const int IOMD_END = 0x0330FFFF;
        public const int IOMD_SIZE = 4096;

        // 外设定义
        // I/O Controller (IOC) - Interrupt/Keyboard/RTC
        public const int IOC_BASE = 0x03000000;
        public static unsafe uint* IOC_IOC_TIMER1 => (uint*)0x06000000;
        public static unsafe uint* IOC_IOC_TIMER2 => (uint*)0x06000004;
        public static unsafe uint* IOC_IOC_IOSEL => (uint*)0x06000008;
        public static unsafe uint* IOC_IOC_IRQST => (uint*)0x0600000C;
        public static unsafe uint* IOC_IOC_IRQLATCH => (uint*)0x06000010;
        public static unsafe uint* IOC_IOC_FIQST => (uint*)0x06000014;
        public static unsafe uint* IOC_IOC_FIQEN => (uint*)0x06000018;
        public static unsafe uint* IOC_IOC_IRQEN => (uint*)0x0600001C;
        public static unsafe uint* IOC_IOC_KBDDATA => (uint*)0x06000020;
        public static unsafe uint* IOC_IOC_KBDCR => (uint*)0x06000024;
        public static unsafe uint* IOC_IOC_RTCDR => (uint*)0x06000028;
        public static unsafe uint* IOC_IOC_RTCCR => (uint*)0x0600002C;
        public static unsafe uint* IOC_IOC_PRST => (uint*)0x06000030;
        public static unsafe uint* IOC_IOC_PORTA => (uint*)0x06000034;
        public static unsafe uint* IOC_IOC_PORTB => (uint*)0x06000038;
        public static unsafe uint* IOC_IOC_PORTC => (uint*)0x0600003C;

        // Memory Controller (MEMC1)
        public const int MEMC_BASE = 0x03200000;
        public static unsafe uint* MEMC_MEMC_PT => (uint*)0x06400000;
        public static unsafe uint* MEMC_MEMC_CTRL => (uint*)0x06400004;
        public static unsafe uint* MEMC_MEMC_DRAM => (uint*)0x06400008;
        public static unsafe uint* MEMC_MEMC_ERR => (uint*)0x0640000C;

        // Video Controller - VIDC1
        public const int VIDC_BASE = 0x03400000;
        public static unsafe uint* VIDC_VIDC_PALETTE => (uint*)0x06800000;
        public static unsafe uint* VIDC_VIDC_STARTL => (uint*)0x06800004;
        public static unsafe uint* VIDC_VIDC_STARTH => (uint*)0x06800008;
        public static unsafe uint* VIDC_VIDC_CONFIG => (uint*)0x0680000C;
        public static unsafe uint* VIDC_VIDC_HDISP => (uint*)0x06800010;
        public static unsafe uint* VIDC_VIDC_VDISP => (uint*)0x06800014;
        public static unsafe uint* VIDC_VIDC_HSYNC => (uint*)0x06800018;
        public static unsafe uint* VIDC_VIDC_VSYNC => (uint*)0x0680001C;
        public static unsafe uint* VIDC_VIDC_BORDER => (uint*)0x06800020;
        public static unsafe uint* VIDC_VIDC_CURSOR => (uint*)0x06800024;
        public static unsafe uint* VIDC_VIDC_SOUND => (uint*)0x06800028;

        // Intel 82710 Floppy Disk Controller
        public const int FDC_BASE = 0x03010000;
        public static unsafe byte* FDC_FDC_STATUS => (byte*)0x06020000;
        public static unsafe byte* FDC_FDC_COMMAND => (byte*)0x06020000;
        public static unsafe byte* FDC_FDC_TRACK => (byte*)0x06020004;
        public static unsafe byte* FDC_FDC_SECTOR => (byte*)0x06020008;
        public static unsafe byte* FDC_FDC_DATA => (byte*)0x0602000C;

        // Serial Port (via IOC)
        public const int SERIAL_BASE = 0x03010010;
        public static unsafe byte* SERIAL_SERIAL_TX => (byte*)0x06020020;
        public static unsafe byte* SERIAL_SERIAL_RX => (byte*)0x06020024;
        public static unsafe byte* SERIAL_SERIAL_CTRL => (byte*)0x06020028;

        // 中断向量定义
        public const int IRQ_RESET = 0;  // Reset
        public const int IRQ_UND = 1;  // Undefined instruction
        public const int IRQ_SWI = 2;  // Software Interrupt (SWI/SVC)
        public const int IRQ_PABORT = 3;  // Prefetch Abort
        public const int IRQ_DABORT = 4;  // Data Abort
        public const int IRQ_ADDRESS = 5;  // Address Exception
        public const int IRQ_IRQ = 6;  // IRQ interrupt (IOC)
        public const int IRQ_FIQ = 7;  // FIQ interrupt (VIDC)

        // 引脚定义
        public const int PIN_VCC = 1;  // +5V Power
        public const int PIN_GND = 2;  // Ground
        public const int PIN_CLK = 3;  // ARM clock (26MHz)
        public const int PIN_NRESET = 4;  // Reset (active low)
        public const int PIN_NMREQ = 5;  // Memory Request (active low)
        public const int PIN_NIORQ = 6;  // I/O Request (active low)
        public const int PIN_NRW = 7;  // Read/Write (0=write, 1=read)
        public const int PIN_MAS0 = 8;  // Master address bit 0
        public const int PIN_MAS1 = 9;  // Master address bit 1
        public const int PIN_MAS2 = 10;  // Master address bit 2
        public const int PIN_LOCK = 11;  // Bus lock
        public const int PIN_NMREQ = 12;  // Memory request (active low)
        public const int PIN_NWAIT = 13;  // Wait state (active low)
        public const int PIN_NIRQLINE = 14;  // IRQ line (active low)
        public const int PIN_NFIRQLINE = 15;  // FIQ line (active low)
        public const int PIN_A1_A25 = 16;  // Address Bus (26-bit)
        public const int PIN_D0_D31 = 17;  // Data Bus (32-bit)

        public static void acorn_archimedes_a310_init()
        {
            // 硬件初始化代码
        }
    }
}
