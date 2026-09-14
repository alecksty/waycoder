using System;

namespace VML.Device.Commodore.Commodore_64
{
    /// <summary>
    /// Commodore-64 寄存器定义
    /// 生成自: Commodore/C64/Commodore-64
    /// 版本: 1.0
    /// </summary>
    public static class Commodore_64
    {
        // CPU架构: MOS-6510, 8位, 1022727 Hz

        // 寄存器定义
        // Accumulator
        public const int A_ADDR = 0x00;
        public static unsafe byte* A => (byte*)0x00;

        // X Index Register
        public const int X_ADDR = 0x01;
        public static unsafe byte* X => (byte*)0x01;

        // Y Index Register
        public const int Y_ADDR = 0x02;
        public static unsafe byte* Y => (byte*)0x02;

        // Stack Pointer
        public const int SP_ADDR = 0x03;
        public static unsafe byte* SP => (byte*)0x03;

        // Program Counter
        public const int PC_ADDR = 0x04;
        public static unsafe ushort* PC => (ushort*)0x04;

        // Processor Status
        public const int P_ADDR = 0x06;
        public static unsafe byte* P => (byte*)0x06;
        public const int P_C = 0;  // Carry Flag
        public const int P_Z = 1;  // Zero Flag
        public const int P_I = 2;  // Interrupt Disable
        public const int P_D = 3;  // Decimal Mode
        public const int P_B = 4;  // Break Flag
        public const int P_U = 5;  // Unused
        public const int P_V = 6;  // Overflow Flag
        public const int P_N = 7;  // Negative Flag

        // I/O Port (6510 only: DDR + data)
        public const int PORT_ADDR = 0x00;
        public static unsafe byte* PORT => (byte*)0x00;

        // 内存段定义
        // 64KB main RAM
        public const int RAM_START = 0x0000;
        public const int RAM_END = 0xFFFF;
        public const int RAM_SIZE = 65536;

        // BASIC interpreter ROM
        public const int BASIC_ROM_START = 0xA000;
        public const int BASIC_ROM_END = 0xBFFF;
        public const int BASIC_ROM_SIZE = 8192;

        // KERNAL operating system ROM
        public const int KERNAL_ROM_START = 0xE000;
        public const int KERNAL_ROM_END = 0xFFFF;
        public const int KERNAL_ROM_SIZE = 8192;

        // Character generator ROM
        public const int CHAR_ROM_START = 0xD000;
        public const int CHAR_ROM_END = 0xDFFF;
        public const int CHAR_ROM_SIZE = 4096;

        // I/O + RAM window (switchable)
        public const int IO_RAM_START = 0xD000;
        public const int IO_RAM_END = 0xDFFF;
        public const int IO_RAM_SIZE = 4096;

        // 外设定义
        // Video Interface Chip II - 6567/6569
        public const int VICII_BASE = 0xD000;
        public static unsafe byte* VICII_SP0X => (byte*)0x0001A000;
        public static unsafe byte* VICII_SP0Y => (byte*)0x0001A001;
        public static unsafe byte* VICII_SP1X => (byte*)0x0001A002;
        public static unsafe byte* VICII_SP1Y => (byte*)0x0001A003;
        public static unsafe byte* VICII_SP2X => (byte*)0x0001A004;
        public static unsafe byte* VICII_SP2Y => (byte*)0x0001A005;
        public static unsafe byte* VICII_SP3X => (byte*)0x0001A006;
        public static unsafe byte* VICII_SP3Y => (byte*)0x0001A007;
        public static unsafe byte* VICII_SP4X => (byte*)0x0001A008;
        public static unsafe byte* VICII_SP4Y => (byte*)0x0001A009;
        public static unsafe byte* VICII_SP5X => (byte*)0x0001A00A;
        public static unsafe byte* VICII_SP5Y => (byte*)0x0001A00B;
        public static unsafe byte* VICII_SP6X => (byte*)0x0001A00C;
        public static unsafe byte* VICII_SP6Y => (byte*)0x0001A00D;
        public static unsafe byte* VICII_SP7X => (byte*)0x0001A00E;
        public static unsafe byte* VICII_SP7Y => (byte*)0x0001A00F;
        public static unsafe byte* VICII_MSIGX => (byte*)0x0001A010;
        public static unsafe byte* VICII_SCROLY => (byte*)0x0001A011;
        public static unsafe byte* VICII_SCROLX => (byte*)0x0001A016;
        public static unsafe byte* VICII_YPSTOP => (byte*)0x0001A012;
        public static unsafe byte* VICII_LPX => (byte*)0x0001A013;
        public static unsafe byte* VICII_LPY => (byte*)0x0001A014;
        public static unsafe byte* VICII_SPENA => (byte*)0x0001A015;
        public static unsafe byte* VICII_CSPMC => (byte*)0x0001A017;
        public static unsafe byte* VICII_MM0 => (byte*)0x0001A018;
        public static unsafe byte* VICII_VM01 => (byte*)0x0001A016;
        public static unsafe byte* VICII_VICBAS => (byte*)0x0001A018;
        public static unsafe byte* VICII_IRQMASK => (byte*)0x0001A019;
        public static unsafe byte* VICII_IRQST => (byte*)0x0001A01A;
        public static unsafe byte* VICII_SPBGPR => (byte*)0x0001A01B;
        public static unsafe byte* VICII_SPMC => (byte*)0x0001A01C;
        public static unsafe byte* VICII_SP1C => (byte*)0x0001A025;
        public static unsafe byte* VICII_SP2C => (byte*)0x0001A026;
        public static unsafe byte* VICII_SPBC => (byte*)0x0001A027;
        public static unsafe byte* VICII_SP1C0 => (byte*)0x0001A028;
        public static unsafe byte* VICII_SP2C0 => (byte*)0x0001A029;
        public static unsafe byte* VICII_SP3C0 => (byte*)0x0001A02A;
        public static unsafe byte* VICII_SP4C0 => (byte*)0x0001A02B;
        public static unsafe byte* VICII_SP5C0 => (byte*)0x0001A02C;
        public static unsafe byte* VICII_SP6C0 => (byte*)0x0001A02D;
        public static unsafe byte* VICII_SP7C0 => (byte*)0x0001A02E;
        public static unsafe byte* VICII_REG_FD => (byte*)0x0001A01D;
        public static unsafe byte* VICII_BGCOL0 => (byte*)0x0001A021;
        public static unsafe byte* VICII_BGCOL1 => (byte*)0x0001A022;
        public static unsafe byte* VICII_BGCOL2 => (byte*)0x0001A023;
        public static unsafe byte* VICII_BGCOL3 => (byte*)0x0001A024;

        // Sound Interface Device 6581/8580
        public const int SID_BASE = 0xD400;
        public static unsafe byte* SID_FREQ1LO => (byte*)0x0001A800;
        public static unsafe byte* SID_FREQ1HI => (byte*)0x0001A801;
        public static unsafe byte* SID_PW1LO => (byte*)0x0001A802;
        public static unsafe byte* SID_PW1HI => (byte*)0x0001A803;
        public static unsafe byte* SID_CR1 => (byte*)0x0001A804;
        public static unsafe byte* SID_AD1 => (byte*)0x0001A805;
        public static unsafe byte* SID_SR1 => (byte*)0x0001A806;
        public static unsafe byte* SID_FREQ2LO => (byte*)0x0001A807;
        public static unsafe byte* SID_FREQ2HI => (byte*)0x0001A808;
        public static unsafe byte* SID_PW2LO => (byte*)0x0001A809;
        public static unsafe byte* SID_PW2HI => (byte*)0x0001A80A;
        public static unsafe byte* SID_CR2 => (byte*)0x0001A80B;
        public static unsafe byte* SID_AD2 => (byte*)0x0001A80C;
        public static unsafe byte* SID_SR2 => (byte*)0x0001A80D;
        public static unsafe byte* SID_FREQ3LO => (byte*)0x0001A80E;
        public static unsafe byte* SID_FREQ3HI => (byte*)0x0001A80F;
        public static unsafe byte* SID_PW3LO => (byte*)0x0001A810;
        public static unsafe byte* SID_PW3HI => (byte*)0x0001A811;
        public static unsafe byte* SID_CR3 => (byte*)0x0001A812;
        public static unsafe byte* SID_AD3 => (byte*)0x0001A813;
        public static unsafe byte* SID_SR3 => (byte*)0x0001A814;
        public static unsafe byte* SID_FCH => (byte*)0x0001A815;
        public static unsafe byte* SID_FCL => (byte*)0x0001A816;
        public static unsafe byte* SID_RES_FLT => (byte*)0x0001A817;
        public static unsafe byte* SID_VOLUME => (byte*)0x0001A818;
        public static unsafe byte* SID_POTX => (byte*)0x0001A819;
        public static unsafe byte* SID_POTY => (byte*)0x0001A81A;
        public static unsafe byte* SID_OSC3 => (byte*)0x0001A81B;
        public static unsafe byte* SID_ENV3 => (byte*)0x0001A81C;

        // Complex Interface Adapter 1 - Keyboard/Serial
        public const int CIA1_BASE = 0xDC00;
        public static unsafe byte* CIA1_PRA => (byte*)0x0001B800;
        public static unsafe byte* CIA1_PRB => (byte*)0x0001B801;
        public static unsafe byte* CIA1_DDRA => (byte*)0x0001B802;
        public static unsafe byte* CIA1_DDRB => (byte*)0x0001B803;
        public static unsafe byte* CIA1_TA_LO => (byte*)0x0001B804;
        public static unsafe byte* CIA1_TA_HI => (byte*)0x0001B805;
        public static unsafe byte* CIA1_TB_LO => (byte*)0x0001B806;
        public static unsafe byte* CIA1_TB_HI => (byte*)0x0001B807;
        public static unsafe byte* CIA1_TOD_TENTH => (byte*)0x0001B808;
        public static unsafe byte* CIA1_TOD_SEC => (byte*)0x0001B809;
        public static unsafe byte* CIA1_TOD_MIN => (byte*)0x0001B80A;
        public static unsafe byte* CIA1_TOD_HR => (byte*)0x0001B80B;
        public static unsafe byte* CIA1_SDR => (byte*)0x0001B80C;
        public static unsafe byte* CIA1_ICR => (byte*)0x0001B80D;
        public static unsafe byte* CIA1_CRA => (byte*)0x0001B80E;
        public static unsafe byte* CIA1_CRB => (byte*)0x0001B80F;

        // Complex Interface Adapter 2 - Serial/Bus
        public const int CIA2_BASE = 0xDD00;
        public static unsafe byte* CIA2_PRA => (byte*)0x0001BA00;
        public static unsafe byte* CIA2_PRB => (byte*)0x0001BA01;
        public static unsafe byte* CIA2_DDRA => (byte*)0x0001BA02;
        public static unsafe byte* CIA2_DDRB => (byte*)0x0001BA03;
        public static unsafe byte* CIA2_TA_LO => (byte*)0x0001BA04;
        public static unsafe byte* CIA2_TA_HI => (byte*)0x0001BA05;
        public static unsafe byte* CIA2_TB_LO => (byte*)0x0001BA06;
        public static unsafe byte* CIA2_TB_HI => (byte*)0x0001BA07;
        public static unsafe byte* CIA2_TOD_TENTH => (byte*)0x0001BA08;
        public static unsafe byte* CIA2_TOD_SEC => (byte*)0x0001BA09;
        public static unsafe byte* CIA2_TOD_MIN => (byte*)0x0001BA0A;
        public static unsafe byte* CIA2_TOD_HR => (byte*)0x0001BA0B;
        public static unsafe byte* CIA2_SDR => (byte*)0x0001BA0C;
        public static unsafe byte* CIA2_ICR => (byte*)0x0001BA0D;
        public static unsafe byte* CIA2_CRA => (byte*)0x0001BA0E;
        public static unsafe byte* CIA2_CRB => (byte*)0x0001BA0F;

        // Color RAM (4-bit per char cell)
        public const int COLORRAM_BASE = 0xD800;
        public static unsafe byte* COLORRAM_COLOR => (byte*)0x0001B000;

        // IEC Serial Bus (via CIA1)
        public const int IEC_BASE = 0xDC00;
        public static unsafe byte* IEC_IEC_DATA => (byte*)0x0001B800;
        public static unsafe byte* IEC_IEC_CLOCK => (byte*)0x0001B801;

        // 中断向量定义
        public const int IRQ_RESET = 0;  // Power-on / Reset
        public const int IRQ_NMI = 1;  // Non-Maskable Interrupt
        public const int IRQ_IRQ = 2;  // IRQ (VIC raster / CIA timer)

        // 引脚定义
        public const int PIN_VCC = 1;  // +5V Power
        public const int PIN_GND = 2;  // Ground
        public const int PIN_RESET = 3;  // System Reset
        public const int PIN_CLK = 4;  // System Clock (~1MHz)
        public const int PIN_DOTCLK = 5;  // VIC Dot Clock (8MHz NTSC / 7.8MHz PAL)
        public const int PIN_AEC = 6;  // Address Enable Control (VIC steals cycles)
        public const int PIN_BA = 7;  // Bus Available (from VIC)
        public const int PIN_IRQ = 8;  // Interrupt Request
        public const int PIN_NMI = 9;  // Non-Maskable Interrupt
        public const int PIN_RWB = 10;  // Read/Write
        public const int PIN_A0_A15 = 11;  // Address Bus
        public const int PIN_D0_D7 = 12;  // Data Bus

        public static void commodore_64_init()
        {
            // 硬件初始化代码
        }
    }
}
