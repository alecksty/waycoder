using System;

namespace VML.Device.CommodoreInternational.Commodore_64
{
    /// <summary>
    /// Commodore-64 寄存器定义
    /// 生成自: Commodore International/Commodore 64/Commodore-64
    /// 版本: 1.0
    /// </summary>
    public static class Commodore_64
    {
        // CPU架构: MOS 6510, 8位, 985248 Hz

        // 寄存器定义
        // Accumulator
        public const int A_ADDR = 0;
        public static unsafe byte* A => (byte*)0;

        // Index Register X
        public const int X_ADDR = 0;
        public static unsafe byte* X => (byte*)0;

        // Index Register Y
        public const int Y_ADDR = 0;
        public static unsafe byte* Y => (byte*)0;

        // Stack Pointer
        public const int SP_ADDR = 0;
        public static unsafe byte* SP => (byte*)0;

        // Program Counter
        public const int PC_ADDR = 0;
        public static unsafe ushort* PC => (ushort*)0;

        // Status Register
        public const int P_ADDR = 0;
        public static unsafe byte* P => (byte*)0;

        // I/O Port (6510 specific)
        public const int PORT_ADDR = 1;
        public static unsafe byte* PORT => (byte*)1;

        // 外设定义
        // Video Interface Chip II
        public const int VIC_II_BASE = ;
        public static unsafe byte* VIC_II_VIC_CTRL1 => (byte*)0x0000D011;
        public static unsafe byte* VIC_II_VIC_CTRL2 => (byte*)0x0000D016;
        public static unsafe byte* VIC_II_VIC_RASTER => (byte*)0x0000D012;
        public static unsafe byte* VIC_II_VIC_MEMPTR => (byte*)0x0000D018;
        public static unsafe byte* VIC_II_VIC_IRQ => (byte*)0x0000D019;
        public static unsafe byte* VIC_II_VIC_IRQMASK => (byte*)0x0000D01A;
        public static unsafe byte* VIC_II_VIC_BORDER => (byte*)0x0000D020;
        public static unsafe byte* VIC_II_VIC_BG0 => (byte*)0x0000D021;
        public static unsafe byte* VIC_II_VIC_BG1 => (byte*)0x0000D022;
        public static unsafe byte* VIC_II_VIC_BG2 => (byte*)0x0000D023;
        public static unsafe byte* VIC_II_VIC_BG3 => (byte*)0x0000D024;
        public static unsafe byte* VIC_II_VIC_SPRITE0_X => (byte*)0x0000D000;
        public static unsafe byte* VIC_II_VIC_SPRITE0_Y => (byte*)0x0000D001;
        public static unsafe byte* VIC_II_VIC_SPRITE1_X => (byte*)0x0000D002;
        public static unsafe byte* VIC_II_VIC_SPRITE1_Y => (byte*)0x0000D003;

        // Sound Interface Device (6581)
        public const int SID_BASE = ;
        public static unsafe byte* SID_SID_VOICE1_FREQ_LO => (byte*)0x0000D400;
        public static unsafe byte* SID_SID_VOICE1_FREQ_HI => (byte*)0x0000D401;
        public static unsafe byte* SID_SID_VOICE1_PW_LO => (byte*)0x0000D402;
        public static unsafe byte* SID_SID_VOICE1_PW_HI => (byte*)0x0000D403;
        public static unsafe byte* SID_SID_VOICE1_CTRL => (byte*)0x0000D404;
        public static unsafe byte* SID_SID_VOICE1_AD => (byte*)0x0000D405;
        public static unsafe byte* SID_SID_VOICE1_SR => (byte*)0x0000D406;
        public static unsafe byte* SID_SID_VOICE2_FREQ_LO => (byte*)0x0000D407;
        public static unsafe byte* SID_SID_VOICE2_FREQ_HI => (byte*)0x0000D408;
        public static unsafe byte* SID_SID_VOICE2_PW_LO => (byte*)0x0000D409;
        public static unsafe byte* SID_SID_VOICE2_PW_HI => (byte*)0x0000D40A;
        public static unsafe byte* SID_SID_VOICE2_CTRL => (byte*)0x0000D40B;
        public static unsafe byte* SID_SID_VOICE2_AD => (byte*)0x0000D40C;
        public static unsafe byte* SID_SID_VOICE2_SR => (byte*)0x0000D40D;
        public static unsafe byte* SID_SID_VOICE3_FREQ_LO => (byte*)0x0000D40E;
        public static unsafe byte* SID_SID_VOICE3_FREQ_HI => (byte*)0x0000D40F;
        public static unsafe byte* SID_SID_VOICE3_PW_LO => (byte*)0x0000D410;
        public static unsafe byte* SID_SID_VOICE3_PW_HI => (byte*)0x0000D411;
        public static unsafe byte* SID_SID_VOICE3_CTRL => (byte*)0x0000D412;
        public static unsafe byte* SID_SID_VOICE3_AD => (byte*)0x0000D413;
        public static unsafe byte* SID_SID_VOICE3_SR => (byte*)0x0000D414;
        public static unsafe byte* SID_SID_FILTER_CUTOFF_LO => (byte*)0x0000D415;
        public static unsafe byte* SID_SID_FILTER_CUTOFF_HI => (byte*)0x0000D416;
        public static unsafe byte* SID_SID_FILTER_CTRL => (byte*)0x0000D417;
        public static unsafe byte* SID_SID_VOLUME => (byte*)0x0000D418;
        public static unsafe byte* SID_SID_POTX => (byte*)0x0000D419;
        public static unsafe byte* SID_SID_POTY => (byte*)0x0000D41A;
        public static unsafe byte* SID_SID_OSC3 => (byte*)0x0000D41B;
        public static unsafe byte* SID_SID_ENV3 => (byte*)0x0000D41C;

        // Complex Interface Adapter 1 (6526)
        public const int CIA1_BASE = ;
        public static unsafe byte* CIA1_CIA1_PRA => (byte*)0x0000DC00;
        public static unsafe byte* CIA1_CIA1_PRB => (byte*)0x0000DC01;
        public static unsafe byte* CIA1_CIA1_DDRA => (byte*)0x0000DC02;
        public static unsafe byte* CIA1_CIA1_DDRB => (byte*)0x0000DC03;
        public static unsafe byte* CIA1_CIA1_TALO => (byte*)0x0000DC04;
        public static unsafe byte* CIA1_CIA1_TAHI => (byte*)0x0000DC05;
        public static unsafe byte* CIA1_CIA1_TBLO => (byte*)0x0000DC06;
        public static unsafe byte* CIA1_CIA1_TBHI => (byte*)0x0000DC07;
        public static unsafe byte* CIA1_CIA1_TODTEN => (byte*)0x0000DC08;
        public static unsafe byte* CIA1_CIA1_TODSEC => (byte*)0x0000DC09;
        public static unsafe byte* CIA1_CIA1_TODMIN => (byte*)0x0000DC0A;
        public static unsafe byte* CIA1_CIA1_TODHR => (byte*)0x0000DC0B;
        public static unsafe byte* CIA1_CIA1_SDR => (byte*)0x0000DC0C;
        public static unsafe byte* CIA1_CIA1_ICR => (byte*)0x0000DC0D;
        public static unsafe byte* CIA1_CIA1_CRA => (byte*)0x0000DC0E;
        public static unsafe byte* CIA1_CIA1_CRB => (byte*)0x0000DC0F;

        // Complex Interface Adapter 2 (6526)
        public const int CIA2_BASE = ;
        public static unsafe byte* CIA2_CIA2_PRA => (byte*)0x0000DD00;
        public static unsafe byte* CIA2_CIA2_PRB => (byte*)0x0000DD01;
        public static unsafe byte* CIA2_CIA2_DDRA => (byte*)0x0000DD02;
        public static unsafe byte* CIA2_CIA2_DDRB => (byte*)0x0000DD03;
        public static unsafe byte* CIA2_CIA2_TALO => (byte*)0x0000DD04;
        public static unsafe byte* CIA2_CIA2_TAHI => (byte*)0x0000DD05;
        public static unsafe byte* CIA2_CIA2_TBLO => (byte*)0x0000DD06;
        public static unsafe byte* CIA2_CIA2_TBHI => (byte*)0x0000DD07;
        public static unsafe byte* CIA2_CIA2_TODTEN => (byte*)0x0000DD08;
        public static unsafe byte* CIA2_CIA2_TODSEC => (byte*)0x0000DD09;
        public static unsafe byte* CIA2_CIA2_TODMIN => (byte*)0x0000DD0A;
        public static unsafe byte* CIA2_CIA2_TODHR => (byte*)0x0000DD0B;
        public static unsafe byte* CIA2_CIA2_SDR => (byte*)0x0000DD0C;
        public static unsafe byte* CIA2_CIA2_ICR => (byte*)0x0000DD0D;
        public static unsafe byte* CIA2_CIA2_CRA => (byte*)0x0000DD0E;
        public static unsafe byte* CIA2_CIA2_CRB => (byte*)0x0000DD0F;

        // 中断向量定义
        public const int IRQ_IRQ = 65532;  // Maskable Interrupt
        public const int IRQ_NMI = 65534;  // Non-Maskable Interrupt
        public const int IRQ_RESET = 65526;  // Reset Vector

        public static void commodore_64_init()
        {
            // 硬件初始化代码
        }
    }
}
