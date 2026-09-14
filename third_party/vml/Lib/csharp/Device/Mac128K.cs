using System;

namespace VML.Device.AppleComputer.Macintosh_128K
{
    /// <summary>
    /// Macintosh-128K 寄存器定义
    /// 生成自: Apple Computer/Macintosh/Macintosh-128K
    /// 版本: 1.0
    /// </summary>
    public static class Macintosh_128K
    {
        // CPU架构: MC68000, 32位, 7833600 Hz

        // 寄存器定义
        // Data Register 0
        public const int D0_ADDR = 0x00;
        public static unsafe uint* D0 => (uint*)0x00;

        // Data Register 1
        public const int D1_ADDR = 0x04;
        public static unsafe uint* D1 => (uint*)0x04;

        // Data Register 2
        public const int D2_ADDR = 0x08;
        public static unsafe uint* D2 => (uint*)0x08;

        // Data Register 3
        public const int D3_ADDR = 0x0C;
        public static unsafe uint* D3 => (uint*)0x0C;

        // Data Register 4
        public const int D4_ADDR = 0x10;
        public static unsafe uint* D4 => (uint*)0x10;

        // Data Register 5
        public const int D5_ADDR = 0x14;
        public static unsafe uint* D5 => (uint*)0x14;

        // Data Register 6
        public const int D6_ADDR = 0x18;
        public static unsafe uint* D6 => (uint*)0x18;

        // Data Register 7
        public const int D7_ADDR = 0x1C;
        public static unsafe uint* D7 => (uint*)0x1C;

        // Address Register 0
        public const int A0_ADDR = 0x20;
        public static unsafe uint* A0 => (uint*)0x20;

        // Address Register 1
        public const int A1_ADDR = 0x24;
        public static unsafe uint* A1 => (uint*)0x24;

        // Address Register 2
        public const int A2_ADDR = 0x28;
        public static unsafe uint* A2 => (uint*)0x28;

        // Address Register 3
        public const int A3_ADDR = 0x2C;
        public static unsafe uint* A3 => (uint*)0x2C;

        // Address Register 4
        public const int A4_ADDR = 0x30;
        public static unsafe uint* A4 => (uint*)0x30;

        // Address Register 5
        public const int A5_ADDR = 0x34;
        public static unsafe uint* A5 => (uint*)0x34;

        // Address Register 6
        public const int A6_ADDR = 0x38;
        public static unsafe uint* A6 => (uint*)0x38;

        // Stack Pointer (USP)
        public const int A7_ADDR = 0x3C;
        public static unsafe uint* A7 => (uint*)0x3C;

        // Program Counter
        public const int PC_ADDR = 0x40;
        public static unsafe uint* PC => (uint*)0x40;

        // Status Register
        public const int SR_ADDR = 0x44;
        public static unsafe ushort* SR => (ushort*)0x44;
        public const int SR_C = 0;  // Carry
        public const int SR_V = 1;  // Overflow
        public const int SR_Z = 2;  // Zero
        public const int SR_N = 3;  // Negative
        public const int SR_X = 4;  // Extend
        public const int SR_I0 = 8;  // Interrupt Mask 0
        public const int SR_I1 = 9;  // Interrupt Mask 1
        public const int SR_I2 = 10;  // Interrupt Mask 2
        public const int SR_S = 13;  // Supervisor/User
        public const int SR_T0 = 14;  // Trace Mode 0
        public const int SR_T1 = 15;  // Trace Mode 1

        // 内存段定义
        // Main RAM (128KB unified)
        public const int RAM_START = 0x000000;
        public const int RAM_END = 0x01FFFF;
        public const int RAM_SIZE = 131072;

        // Mac ROM (128KB)
        public const int ROM_START = 0x40000000;
        public const int ROM_END = 0x4001FFFF;
        public const int ROM_SIZE = 131072;

        // Screen bitmap (512x342x1 = 21792 bytes)
        public const int FRAMEBUFFER_START = 0x00400000;
        public const int FRAMEBUFFER_END = 0x00400555;
        public const int FRAMEBUFFER_SIZE = 1366;

        // Shadow screen (double-buffering)
        public const int FRAMEBUFFER2_START = 0x00410000;
        public const int FRAMEBUFFER2_END = 0x00410555;
        public const int FRAMEBUFFER2_SIZE = 1366;

        // VIA 6522 (I/O)
        public const int VIA_START = 0x00E00000;
        public const int VIA_END = 0x00E0FFFF;
        public const int VIA_SIZE = 4096;

        // SCC 8530 (serial)
        public const int SCC_START = 0x00F00000;
        public const int SCC_END = 0x00F0FFFF;
        public const int SCC_SIZE = 4096;

        // ADB bus
        public const int ADB_START = 0x01600000;
        public const int ADB_END = 0x0160FFFF;
        public const int ADB_SIZE = 4096;

        // IWM floppy controller
        public const int IWM_START = 0x01E00000;
        public const int IWM_END = 0x01E0FFFF;
        public const int IWM_SIZE = 4096;

        // 外设定义
        // Versatile Interface Adapter 6522
        public const int VIA_BASE = 0xE00000;
        public static unsafe byte* VIA_ORB => (byte*)0x01C00000;
        public static unsafe byte* VIA_ORA => (byte*)0x01C00002;
        public static unsafe byte* VIA_DDRB => (byte*)0x01C00004;
        public static unsafe byte* VIA_DDRA => (byte*)0x01C00006;
        public static unsafe ushort* VIA_T1C_L => (ushort*)0x01C00008;
        public static unsafe ushort* VIA_T1C_H => (ushort*)0x01C0000A;
        public static unsafe ushort* VIA_T1L_L => (ushort*)0x01C0000C;
        public static unsafe ushort* VIA_T1L_H => (ushort*)0x01C0000E;
        public static unsafe ushort* VIA_T2C_L => (ushort*)0x01C00010;
        public static unsafe ushort* VIA_T2C_H => (ushort*)0x01C00012;
        public static unsafe byte* VIA_SR => (byte*)0x01C00014;
        public static unsafe byte* VIA_ACR => (byte*)0x01C00016;
        public static unsafe byte* VIA_PCR => (byte*)0x01C00018;
        public static unsafe byte* VIA_IFR => (byte*)0x01C0001E;
        public static unsafe byte* VIA_IER => (byte*)0x01C0001E;

        // SCC 8530 Serial Communications Controller
        public const int SCC_BASE = 0xF00000;
        public static unsafe byte* SCC_SCC_CHA_B => (byte*)0x01E00000;
        public static unsafe byte* SCC_SCC_CHA_C => (byte*)0x01E00002;
        public static unsafe byte* SCC_SCC_CHB_D => (byte*)0x01E00004;
        public static unsafe byte* SCC_SCC_CHB_CT => (byte*)0x01E00006;

        // Integrated Woz Machine - Floppy Disk Controller
        public const int IWM_BASE = 0x1E00000;
        public static unsafe byte* IWM_IWM_DATA => (byte*)0x03C00000;
        public static unsafe byte* IWM_IWM_MODE => (byte*)0x03C00008;
        public static unsafe byte* IWM_IWM_Q6L => (byte*)0x03C00020;
        public static unsafe byte* IWM_IWM_Q7L => (byte*)0x03C00022;
        public static unsafe byte* IWM_IWM_Q6R => (byte*)0x03C00024;
        public static unsafe byte* IWM_IWM_Q7R => (byte*)0x03C00026;

        // Video Graphics Controller (custom Apple chip)
        public const int VGC_BASE = 0x00F20000;
        public static unsafe byte* VGC_VGC_MODE => (byte*)0x01E40000;
        public static unsafe byte* VGC_VGC_START_HI => (byte*)0x01E40002;
        public static unsafe byte* VGC_VGC_START_LO => (byte*)0x01E40004;

        // Apple Desktop Bus
        public const int ADB_BASE = 0x01600000;
        public static unsafe byte* ADB_ADB_DATA => (byte*)0x02C00000;
        public static unsafe byte* ADB_ADB_STATUS => (byte*)0x02C00004;
        public static unsafe byte* ADB_ADB_CMD => (byte*)0x02C00008;

        // 中断向量定义
        public const int IRQ_RESET = 1;  // Reset Initial SP
        public const int IRQ_RESET_PC = 2;  // Reset Initial PC
        public const int IRQ_IRQ1 = 24;  // VIA interrupt (level 1)
        public const int IRQ_IRQ2 = 25;  // SCC interrupt (level 2)
        public const int IRQ_IRQ3 = 26;  // ADB / VIA (level 3)
        public const int IRQ_IRQ4 = 27;  // ADB / VIA (level 4)

        // 引脚定义
        public const int PIN_VCC = 1;  // +5V Power
        public const int PIN_GND = 2;  // Ground
        public const int PIN_CLK = 3;  // 16MHz master clock / 7.83MHz CPU clock
        public const int PIN_FC0 = 4;  // Function Code 0
        public const int PIN_FC1 = 5;  // Function Code 1
        public const int PIN_FC2 = 6;  // Function Code 2
        public const int PIN_AS = 7;  // Address Strobe
        public const int PIN_UDS = 8;  // Upper Data Strobe
        public const int PIN_LDS = 9;  // Lower Data Strobe
        public const int PIN_RWB = 10;  // Read/Write
        public const int PIN_DTACK = 11;  // Data Acknowledge
        public const int PIN_BERR = 12;  // Bus Error
        public const int PIN_BR = 13;  // Bus Request
        public const int PIN_BG = 14;  // Bus Grant
        public const int PIN_BGACK = 15;  // Bus Grant Acknowledge
        public const int PIN_IPL0 = 16;  // Interrupt Priority 0
        public const int PIN_IPL1 = 17;  // Interrupt Priority 1
        public const int PIN_IPL2 = 18;  // Interrupt Priority 2
        public const int PIN_RESET = 19;  // Reset
        public const int PIN_HALT = 20;  // Halt
        public const int PIN_A1_A23 = 21;  // Address Bus (24-bit)
        public const int PIN_D0_D15 = 22;  // Data Bus (16-bit)

        public static void macintosh_128k_init()
        {
            // 硬件初始化代码
        }
    }
}
