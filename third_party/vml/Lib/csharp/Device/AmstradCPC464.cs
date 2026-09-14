using System;

namespace VML.Device.Amstrad.Amstrad_CPC_464
{
    /// <summary>
    /// Amstrad-CPC-464 寄存器定义
    /// 生成自: Amstrad/CPC/Amstrad-CPC-464
    /// 版本: 1.0
    /// </summary>
    public static class Amstrad_CPC_464
    {
        // CPU架构: Z80A, 8位, 4000000 Hz

        // 寄存器定义
        // Accumulator
        public const int A_ADDR = 0x00;
        public static unsafe byte* A => (byte*)0x00;

        // Flags
        public const int F_ADDR = 0x01;
        public static unsafe byte* F => (byte*)0x01;
        public const int F_C = 0;  // Carry
        public const int F_N = 1;  // Subtract
        public const int F_PV = 2;  // Parity/Overflow
        public const int F_H = 4;  // Half Carry
        public const int F_Z = 6;  // Zero
        public const int F_S = 7;  // Sign

        // B Register
        public const int B_ADDR = 0x02;
        public static unsafe byte* B => (byte*)0x02;

        // C Register
        public const int C_ADDR = 0x03;
        public static unsafe byte* C => (byte*)0x03;

        // D Register
        public const int D_ADDR = 0x04;
        public static unsafe byte* D => (byte*)0x04;

        // E Register
        public const int E_ADDR = 0x05;
        public static unsafe byte* E => (byte*)0x05;

        // H Register
        public const int H_ADDR = 0x06;
        public static unsafe byte* H => (byte*)0x06;

        // L Register
        public const int L_ADDR = 0x07;
        public static unsafe byte* L => (byte*)0x07;

        // Alternate AF
        public const int AF_ADDR = 0x08;
        public static unsafe ushort* AF' => (ushort*)0x08;

        // Alternate BC
        public const int BC_ADDR = 0x0A;
        public static unsafe ushort* BC' => (ushort*)0x0A;

        // Alternate DE
        public const int DE_ADDR = 0x0C;
        public static unsafe ushort* DE' => (ushort*)0x0C;

        // Alternate HL
        public const int HL_ADDR = 0x0E;
        public static unsafe ushort* HL' => (ushort*)0x0E;

        // Interrupt Vector
        public const int I_ADDR = 0x10;
        public static unsafe byte* I => (byte*)0x10;

        // Refresh
        public const int R_ADDR = 0x11;
        public static unsafe byte* R => (byte*)0x11;

        // Index X
        public const int IX_ADDR = 0x12;
        public static unsafe ushort* IX => (ushort*)0x12;

        // Index Y
        public const int IY_ADDR = 0x14;
        public static unsafe ushort* IY => (ushort*)0x14;

        // Stack Pointer
        public const int SP_ADDR = 0x16;
        public static unsafe ushort* SP => (ushort*)0x16;

        // Program Counter
        public const int PC_ADDR = 0x18;
        public static unsafe ushort* PC => (ushort*)0x18;

        // 内存段定义
        // Lower ROM (AMSDOS / CP/M)
        public const int LOWER_ROM_START = 0x0000;
        public const int LOWER_ROM_END = 0x3FFF;
        public const int LOWER_ROM_SIZE = 16384;

        // Lower RAM bank (switchable)
        public const int RAM_BANK0_START = 0x0000;
        public const int RAM_BANK0_END = 0x3FFF;
        public const int RAM_BANK0_SIZE = 16384;

        // Main RAM (32KB)
        public const int RAM_MAIN_START = 0x4000;
        public const int RAM_MAIN_END = 0xBFFF;
        public const int RAM_MAIN_SIZE = 32768;

        // Upper ROM (BASIC)
        public const int UPPER_ROM_START = 0xC000;
        public const int UPPER_ROM_END = 0xFFFF;
        public const int UPPER_ROM_SIZE = 16384;

        // 外设定义
        // Gate Array - Custom ASIC (video/sound/RAM control)
        public const int GA_BASE = 0x7F00;
        public static unsafe byte* GA_GA_MR => (byte*)0x0000FE00;
        public static unsafe byte* GA_GA_IR => (byte*)0x0000FE01;
        public static unsafe byte* GA_GA_R1 => (byte*)0x0000FE02;
        public static unsafe byte* GA_GA_R2 => (byte*)0x0000FE03;
        public static unsafe byte* GA_GA_R3 => (byte*)0x0000FE04;
        public static unsafe byte* GA_GA_R4 => (byte*)0x0000FE05;
        public static unsafe byte* GA_GA_R5 => (byte*)0x0000FE06;
        public static unsafe byte* GA_GA_R6 => (byte*)0x0000FE07;
        public static unsafe byte* GA_GA_R7 => (byte*)0x0000FE08;

        // CRT Controller 6845 - Video timing
        public const int CRTC_BASE = 0xBC00;
        public static unsafe byte* CRTC_CRTC_REG => (byte*)0x00017800;
        public static unsafe byte* CRTC_CRTC_DATA => (byte*)0x00017900;
        public static unsafe byte* CRTC_CRTC_H_TOTAL => (byte*)0x00017801;
        public static unsafe byte* CRTC_CRTC_H_DISP => (byte*)0x00017802;
        public static unsafe byte* CRTC_CRTC_HSYNC_POS => (byte*)0x00017803;
        public static unsafe byte* CRTC_CRTC_HSYNC_WIDTH => (byte*)0x00017804;
        public static unsafe byte* CRTC_CRTC_V_TOTAL => (byte*)0x00017805;
        public static unsafe byte* CRTC_CRTC_V_TOTAL_ADJ => (byte*)0x00017806;
        public static unsafe byte* CRTC_CRTC_V_DISP => (byte*)0x00017807;
        public static unsafe byte* CRTC_CRTC_VSYNC_POS => (byte*)0x00017808;
        public static unsafe byte* CRTC_CRTC_INTERLACE => (byte*)0x00017809;
        public static unsafe byte* CRTC_CRTC_CURSOR_START => (byte*)0x0001780A;
        public static unsafe byte* CRTC_CRTC_CURSOR_END => (byte*)0x0001780B;
        public static unsafe byte* CRTC_CRTC_SA_HI => (byte*)0x0001780C;
        public static unsafe byte* CRTC_CRTC_SA_LO => (byte*)0x0001780D;
        public static unsafe byte* CRTC_CRTC_CURSOR_HI => (byte*)0x0001780E;
        public static unsafe byte* CRTC_CRTC_CURSOR_LO => (byte*)0x0001780F;

        // AY-3-8912 Programmable Sound Generator
        public const int PSG_BASE = 0xF400;
        public static unsafe byte* PSG_PSG_REG => (byte*)0x0001E800;
        public static unsafe byte* PSG_PSG_DATA => (byte*)0x0001EA00;
        public static unsafe byte* PSG_FREQ_A_LO => (byte*)0x0001E800;
        public static unsafe byte* PSG_FREQ_A_HI => (byte*)0x0001E801;
        public static unsafe byte* PSG_FREQ_B_LO => (byte*)0x0001E802;
        public static unsafe byte* PSG_FREQ_B_HI => (byte*)0x0001E803;
        public static unsafe byte* PSG_FREQ_C_LO => (byte*)0x0001E804;
        public static unsafe byte* PSG_FREQ_C_HI => (byte*)0x0001E805;
        public static unsafe byte* PSG_NOISE_FREQ => (byte*)0x0001E806;
        public static unsafe byte* PSG_ENABLE => (byte*)0x0001E807;
        public static unsafe byte* PSG_VOL_A => (byte*)0x0001E808;
        public static unsafe byte* PSG_VOL_B => (byte*)0x0001E809;
        public static unsafe byte* PSG_VOL_C => (byte*)0x0001E80A;
        public static unsafe byte* PSG_ENV_FREQ_LO => (byte*)0x0001E80B;
        public static unsafe byte* PSG_ENV_FREQ_HI => (byte*)0x0001E80C;
        public static unsafe byte* PSG_ENV_SHAPE => (byte*)0x0001E80D;
        public static unsafe byte* PSG_PORT_A => (byte*)0x0001E80E;
        public static unsafe byte* PSG_PORT_B => (byte*)0x0001E80F;

        // WD1772 Floppy Disk Controller (via expansion)
        public const int FDC_BASE = 0xF800;
        public static unsafe byte* FDC_FDC_STATUS => (byte*)0x0001F0E0;
        public static unsafe byte* FDC_FDC_COMMAND => (byte*)0x0001F0E0;
        public static unsafe byte* FDC_FDC_TRACK => (byte*)0x0001F0E1;
        public static unsafe byte* FDC_FDC_SECTOR => (byte*)0x0001F0E2;
        public static unsafe byte* FDC_FDC_DATA => (byte*)0x0001F0E3;

        // Centronics Parallel Printer Port
        public const int PRINTER_BASE = 0xEE;
        public static unsafe byte* PRINTER_PRN_DATA => (byte*)0x000001DC;
        public static unsafe byte* PRINTER_PRN_STROBE => (byte*)0x000001DD;

        // 中断向量定义
        public const int IRQ_RESET = 0;  // Power-on / Reset
        public const int IRQ_NMI = 1;  // Non-Maskable Interrupt
        public const int IRQ_INT = 2;  // Gate Array interrupt (50Hz vertical blank)

        // 引脚定义
        public const int PIN_VCC = 1;  // +5V Power
        public const int PIN_GND = 2;  // Ground
        public const int PIN_CLK = 3;  // Z80 Clock (4MHz)
        public const int PIN_A0_A15 = 4;  // Address Bus
        public const int PIN_D0_D7 = 5;  // Data Bus
        public const int PIN_MREQ = 6;  // Memory Request
        public const int PIN_IORQ = 7;  // I/O Request
        public const int PIN_RD = 8;  // Read
        public const int PIN_WR = 9;  // Write
        public const int PIN_INT = 10;  // Interrupt Request
        public const int PIN_NMI = 11;  // Non-Maskable Interrupt
        public const int PIN_RESET = 12;  // Reset

        public static void amstrad_cpc_464_init()
        {
            // 硬件初始化代码
        }
    }
}
