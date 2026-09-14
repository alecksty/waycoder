package vml.device.amstrad.amstrad_cpc_464;

/**
 * Amstrad-CPC-464 寄存器定义
 * 生成自: Amstrad/CPC/Amstrad-CPC-464
 * 版本: 1.0
 */
public final class Amstrad_CPC_464 {
    private Amstrad_CPC_464() {} // 工具类
    // CPU架构: Z80A, 8位, 4000000 Hz

    // 寄存器定义
    // Accumulator
    public static final int A_ADDR = (int)0x00;

    // Flags
    public static final int F_ADDR = (int)0x01;
    public static final int F_C = 0;  // Carry
    public static final int F_N = 1;  // Subtract
    public static final int F_PV = 2;  // Parity/Overflow
    public static final int F_H = 4;  // Half Carry
    public static final int F_Z = 6;  // Zero
    public static final int F_S = 7;  // Sign

    // B Register
    public static final int B_ADDR = (int)0x02;

    // C Register
    public static final int C_ADDR = (int)0x03;

    // D Register
    public static final int D_ADDR = (int)0x04;

    // E Register
    public static final int E_ADDR = (int)0x05;

    // H Register
    public static final int H_ADDR = (int)0x06;

    // L Register
    public static final int L_ADDR = (int)0x07;

    // Alternate AF
    public static final int AF_ADDR = (int)0x08;

    // Alternate BC
    public static final int BC_ADDR = (int)0x0A;

    // Alternate DE
    public static final int DE_ADDR = (int)0x0C;

    // Alternate HL
    public static final int HL_ADDR = (int)0x0E;

    // Interrupt Vector
    public static final int I_ADDR = (int)0x10;

    // Refresh
    public static final int R_ADDR = (int)0x11;

    // Index X
    public static final int IX_ADDR = (int)0x12;

    // Index Y
    public static final int IY_ADDR = (int)0x14;

    // Stack Pointer
    public static final int SP_ADDR = (int)0x16;

    // Program Counter
    public static final int PC_ADDR = (int)0x18;

    // 内存段定义
    // Lower ROM (AMSDOS / CP/M)
    public static final int LOWER_ROM_START = (int)0x0000;
    public static final int LOWER_ROM_END = (int)0x3FFF;
    public static final int LOWER_ROM_SIZE = 16384;

    // Lower RAM bank (switchable)
    public static final int RAM_BANK0_START = (int)0x0000;
    public static final int RAM_BANK0_END = (int)0x3FFF;
    public static final int RAM_BANK0_SIZE = 16384;

    // Main RAM (32KB)
    public static final int RAM_MAIN_START = (int)0x4000;
    public static final int RAM_MAIN_END = (int)0xBFFF;
    public static final int RAM_MAIN_SIZE = 32768;

    // Upper ROM (BASIC)
    public static final int UPPER_ROM_START = (int)0xC000;
    public static final int UPPER_ROM_END = (int)0xFFFF;
    public static final int UPPER_ROM_SIZE = 16384;

    // 外设定义
    // Gate Array - Custom ASIC (video/sound/RAM control)
    public static final int GA_BASE = (int)0x7F00;
    public static final int GA_GA_MR = (int)0x0000FE00;
    public static final int GA_GA_IR = (int)0x0000FE01;
    public static final int GA_GA_R1 = (int)0x0000FE02;
    public static final int GA_GA_R2 = (int)0x0000FE03;
    public static final int GA_GA_R3 = (int)0x0000FE04;
    public static final int GA_GA_R4 = (int)0x0000FE05;
    public static final int GA_GA_R5 = (int)0x0000FE06;
    public static final int GA_GA_R6 = (int)0x0000FE07;
    public static final int GA_GA_R7 = (int)0x0000FE08;

    // CRT Controller 6845 - Video timing
    public static final int CRTC_BASE = (int)0xBC00;
    public static final int CRTC_CRTC_REG = (int)0x00017800;
    public static final int CRTC_CRTC_DATA = (int)0x00017900;
    public static final int CRTC_CRTC_H_TOTAL = (int)0x00017801;
    public static final int CRTC_CRTC_H_DISP = (int)0x00017802;
    public static final int CRTC_CRTC_HSYNC_POS = (int)0x00017803;
    public static final int CRTC_CRTC_HSYNC_WIDTH = (int)0x00017804;
    public static final int CRTC_CRTC_V_TOTAL = (int)0x00017805;
    public static final int CRTC_CRTC_V_TOTAL_ADJ = (int)0x00017806;
    public static final int CRTC_CRTC_V_DISP = (int)0x00017807;
    public static final int CRTC_CRTC_VSYNC_POS = (int)0x00017808;
    public static final int CRTC_CRTC_INTERLACE = (int)0x00017809;
    public static final int CRTC_CRTC_CURSOR_START = (int)0x0001780A;
    public static final int CRTC_CRTC_CURSOR_END = (int)0x0001780B;
    public static final int CRTC_CRTC_SA_HI = (int)0x0001780C;
    public static final int CRTC_CRTC_SA_LO = (int)0x0001780D;
    public static final int CRTC_CRTC_CURSOR_HI = (int)0x0001780E;
    public static final int CRTC_CRTC_CURSOR_LO = (int)0x0001780F;

    // AY-3-8912 Programmable Sound Generator
    public static final int PSG_BASE = (int)0xF400;
    public static final int PSG_PSG_REG = (int)0x0001E800;
    public static final int PSG_PSG_DATA = (int)0x0001EA00;
    public static final int PSG_FREQ_A_LO = (int)0x0001E800;
    public static final int PSG_FREQ_A_HI = (int)0x0001E801;
    public static final int PSG_FREQ_B_LO = (int)0x0001E802;
    public static final int PSG_FREQ_B_HI = (int)0x0001E803;
    public static final int PSG_FREQ_C_LO = (int)0x0001E804;
    public static final int PSG_FREQ_C_HI = (int)0x0001E805;
    public static final int PSG_NOISE_FREQ = (int)0x0001E806;
    public static final int PSG_ENABLE = (int)0x0001E807;
    public static final int PSG_VOL_A = (int)0x0001E808;
    public static final int PSG_VOL_B = (int)0x0001E809;
    public static final int PSG_VOL_C = (int)0x0001E80A;
    public static final int PSG_ENV_FREQ_LO = (int)0x0001E80B;
    public static final int PSG_ENV_FREQ_HI = (int)0x0001E80C;
    public static final int PSG_ENV_SHAPE = (int)0x0001E80D;
    public static final int PSG_PORT_A = (int)0x0001E80E;
    public static final int PSG_PORT_B = (int)0x0001E80F;

    // WD1772 Floppy Disk Controller (via expansion)
    public static final int FDC_BASE = (int)0xF800;
    public static final int FDC_FDC_STATUS = (int)0x0001F0E0;
    public static final int FDC_FDC_COMMAND = (int)0x0001F0E0;
    public static final int FDC_FDC_TRACK = (int)0x0001F0E1;
    public static final int FDC_FDC_SECTOR = (int)0x0001F0E2;
    public static final int FDC_FDC_DATA = (int)0x0001F0E3;

    // Centronics Parallel Printer Port
    public static final int PRINTER_BASE = (int)0xEE;
    public static final int PRINTER_PRN_DATA = (int)0x000001DC;
    public static final int PRINTER_PRN_STROBE = (int)0x000001DD;

    // 中断向量定义
    public static final int IRQ_RESET = 0;  // Power-on / Reset
    public static final int IRQ_NMI = 1;  // Non-Maskable Interrupt
    public static final int IRQ_INT = 2;  // Gate Array interrupt (50Hz vertical blank)

    // 引脚定义
    public static final int PIN_VCC = 1;  // +5V Power
    public static final int PIN_GND = 2;  // Ground
    public static final int PIN_CLK = 3;  // Z80 Clock (4MHz)
    public static final int PIN_A0_A15 = 4;  // Address Bus
    public static final int PIN_D0_D7 = 5;  // Data Bus
    public static final int PIN_MREQ = 6;  // Memory Request
    public static final int PIN_IORQ = 7;  // I/O Request
    public static final int PIN_RD = 8;  // Read
    public static final int PIN_WR = 9;  // Write
    public static final int PIN_INT = 10;  // Interrupt Request
    public static final int PIN_NMI = 11;  // Non-Maskable Interrupt
    public static final int PIN_RESET = 12;  // Reset

    public static native void amstrad_cpc_464_init();
}
