// Amstrad-CPC-464 设备定义 - Dart 库
// 生成自: Amstrad/CPC/Amstrad-CPC-464
// 版本: 1.0
// 日期: 2026-04-17
// 作者: VML Team
// 描述: Amstrad CPC 464 - British 8-bit home computer with Z80 CPU and built-in cassette recorder
// CPU架构: Z80A
// 位宽: 8位
// 时钟频率: 4000000 Hz

class Amstrad_CPC_464Device {
  static const String deviceName = "Amstrad-CPC-464";
  static const String manufacturer = "Amstrad";
  static const String family = "CPC";
  static const String version = "1.0";
  static const String architecture = "Z80A";
  static const int bits = 8;
  static const int clockFrequency = 4000000;

  // 寄存器地址定义
  static const int A_ADDR = 0x00;  // Accumulator
  static const int F_ADDR = 0x01;  // Flags
  static const int F_C_BIT = 0;  // Carry
  static const int F_N_BIT = 1;  // Subtract
  static const int F_PV_BIT = 2;  // Parity/Overflow
  static const int F_H_BIT = 4;  // Half Carry
  static const int F_Z_BIT = 6;  // Zero
  static const int F_S_BIT = 7;  // Sign
  static const int B_ADDR = 0x02;  // B Register
  static const int C_ADDR = 0x03;  // C Register
  static const int D_ADDR = 0x04;  // D Register
  static const int E_ADDR = 0x05;  // E Register
  static const int H_ADDR = 0x06;  // H Register
  static const int L_ADDR = 0x07;  // L Register
  static const int AF_ADDR = 0x08;  // Alternate AF
  static const int BC_ADDR = 0x0A;  // Alternate BC
  static const int DE_ADDR = 0x0C;  // Alternate DE
  static const int HL_ADDR = 0x0E;  // Alternate HL
  static const int I_ADDR = 0x10;  // Interrupt Vector
  static const int R_ADDR = 0x11;  // Refresh
  static const int IX_ADDR = 0x12;  // Index X
  static const int IY_ADDR = 0x14;  // Index Y
  static const int SP_ADDR = 0x16;  // Stack Pointer
  static const int PC_ADDR = 0x18;  // Program Counter

  // 内存段定义
  static const int LOWER_ROM_START = 0x0000;
  static const int LOWER_ROM_END = 0x3FFF;
  static const int LOWER_ROM_SIZE = 16384;  // Lower ROM (AMSDOS / CP/M)
  static const int RAM_BANK0_START = 0x0000;
  static const int RAM_BANK0_END = 0x3FFF;
  static const int RAM_BANK0_SIZE = 16384;  // Lower RAM bank (switchable)
  static const int RAM_MAIN_START = 0x4000;
  static const int RAM_MAIN_END = 0xBFFF;
  static const int RAM_MAIN_SIZE = 32768;  // Main RAM (32KB)
  static const int UPPER_ROM_START = 0xC000;
  static const int UPPER_ROM_END = 0xFFFF;
  static const int UPPER_ROM_SIZE = 16384;  // Upper ROM (BASIC)

  // 外设定义
  // Gate Array - Custom ASIC (video/sound/RAM control)
  static const int GA_BASE = 0x7F00;
  static const int GA_GA_MR_ADDR = 0x7F00;
  static const int GA_GA_IR_ADDR = 0x7F01;
  static const int GA_GA_R1_ADDR = 0x7F02;
  static const int GA_GA_R2_ADDR = 0x7F03;
  static const int GA_GA_R3_ADDR = 0x7F04;
  static const int GA_GA_R4_ADDR = 0x7F05;
  static const int GA_GA_R5_ADDR = 0x7F06;
  static const int GA_GA_R6_ADDR = 0x7F07;
  static const int GA_GA_R7_ADDR = 0x7F08;
  // CRT Controller 6845 - Video timing
  static const int CRTC_BASE = 0xBC00;
  static const int CRTC_CRTC_REG_ADDR = 0xBC00;
  static const int CRTC_CRTC_DATA_ADDR = 0xBD00;
  static const int CRTC_CRTC_H_TOTAL_ADDR = 0xBC01;
  static const int CRTC_CRTC_H_DISP_ADDR = 0xBC02;
  static const int CRTC_CRTC_HSYNC_POS_ADDR = 0xBC03;
  static const int CRTC_CRTC_HSYNC_WIDTH_ADDR = 0xBC04;
  static const int CRTC_CRTC_V_TOTAL_ADDR = 0xBC05;
  static const int CRTC_CRTC_V_TOTAL_ADJ_ADDR = 0xBC06;
  static const int CRTC_CRTC_V_DISP_ADDR = 0xBC07;
  static const int CRTC_CRTC_VSYNC_POS_ADDR = 0xBC08;
  static const int CRTC_CRTC_INTERLACE_ADDR = 0xBC09;
  static const int CRTC_CRTC_CURSOR_START_ADDR = 0xBC0A;
  static const int CRTC_CRTC_CURSOR_END_ADDR = 0xBC0B;
  static const int CRTC_CRTC_SA_HI_ADDR = 0xBC0C;
  static const int CRTC_CRTC_SA_LO_ADDR = 0xBC0D;
  static const int CRTC_CRTC_CURSOR_HI_ADDR = 0xBC0E;
  static const int CRTC_CRTC_CURSOR_LO_ADDR = 0xBC0F;
  // AY-3-8912 Programmable Sound Generator
  static const int PSG_BASE = 0xF400;
  static const int PSG_PSG_REG_ADDR = 0xF400;
  static const int PSG_PSG_DATA_ADDR = 0xF600;
  static const int PSG_FREQ_A_LO_ADDR = 0xF400;
  static const int PSG_FREQ_A_HI_ADDR = 0xF401;
  static const int PSG_FREQ_B_LO_ADDR = 0xF402;
  static const int PSG_FREQ_B_HI_ADDR = 0xF403;
  static const int PSG_FREQ_C_LO_ADDR = 0xF404;
  static const int PSG_FREQ_C_HI_ADDR = 0xF405;
  static const int PSG_NOISE_FREQ_ADDR = 0xF406;
  static const int PSG_ENABLE_ADDR = 0xF407;
  static const int PSG_VOL_A_ADDR = 0xF408;
  static const int PSG_VOL_B_ADDR = 0xF409;
  static const int PSG_VOL_C_ADDR = 0xF40A;
  static const int PSG_ENV_FREQ_LO_ADDR = 0xF40B;
  static const int PSG_ENV_FREQ_HI_ADDR = 0xF40C;
  static const int PSG_ENV_SHAPE_ADDR = 0xF40D;
  static const int PSG_PORT_A_ADDR = 0xF40E;
  static const int PSG_PORT_B_ADDR = 0xF40F;
  // WD1772 Floppy Disk Controller (via expansion)
  static const int FDC_BASE = 0xF800;
  static const int FDC_FDC_STATUS_ADDR = 0xF8E0;
  static const int FDC_FDC_COMMAND_ADDR = 0xF8E0;
  static const int FDC_FDC_TRACK_ADDR = 0xF8E1;
  static const int FDC_FDC_SECTOR_ADDR = 0xF8E2;
  static const int FDC_FDC_DATA_ADDR = 0xF8E3;
  // Centronics Parallel Printer Port
  static const int PRINTER_BASE = 0xEE;
  static const int PRINTER_PRN_DATA_ADDR = 0xEE;
  static const int PRINTER_PRN_STROBE_ADDR = 0xEF;

  // 中断向量定义
  static const int INT_RESET = 0;  // Power-on / Reset
  static const int INT_NMI = 1;  // Non-Maskable Interrupt
  static const int INT_INT = 2;  // Gate Array interrupt (50Hz vertical blank)

  // 引脚定义
  static const int PIN_VCC = 1;  // +5V Power
  static const int PIN_GND = 2;  // Ground
  static const int PIN_CLK = 3;  // Z80 Clock (4MHz)
  static const int PIN_A0_A15 = 4;  // Address Bus
  static const int PIN_D0_D7 = 5;  // Data Bus
  static const int PIN_MREQ = 6;  // Memory Request
  static const int PIN_IORQ = 7;  // I/O Request
  static const int PIN_RD = 8;  // Read
  static const int PIN_WR = 9;  // Write
  static const int PIN_INT = 10;  // Interrupt Request
  static const int PIN_NMI = 11;  // Non-Maskable Interrupt
  static const int PIN_RESET = 12;  // Reset

}
