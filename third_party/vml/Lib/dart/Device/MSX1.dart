// MSX1 设备定义 - Dart 库
// 生成自: Various (ASCII/Awanaga/MSX Association)/MSX/MSX1
// 版本: 1.0
// 日期: 2026-04-17
// 作者: VML Team
// 描述: MSX - Standardized 8-bit home computer with Z80A CPU, TMS9918A graphics, and AY-3-8910 audio
// CPU架构: Z80A
// 位宽: 8位
// 时钟频率: 3579545 Hz

class MSX1Device {
  static const String deviceName = "MSX1";
  static const String manufacturer = "Various (ASCII/Awanaga/MSX Association)";
  static const String family = "MSX";
  static const String version = "1.0";
  static const String architecture = "Z80A";
  static const int bits = 8;
  static const int clockFrequency = 3579545;

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
  static const int IY_ADDR = 0x14;  // Index Y (usually = 0xF38F)
  static const int SP_ADDR = 0x16;  // Stack Pointer
  static const int PC_ADDR = 0x18;  // Program Counter

  // 内存段定义
  static const int SLOT0_ROM_START = 0x0000;
  static const int SLOT0_ROM_END = 0x7FFF;
  static const int SLOT0_ROM_SIZE = 32768;  // Cartridge/SUB-ROM / Main-ROM
  static const int SYSROM_START = 0x0000;
  static const int SYSROM_END = 0x3FFF;
  static const int SYSROM_SIZE = 16384;  // MSX-BIOS ROM
  static const int EXTROM_START = 0x4000;
  static const int EXTROM_END = 0x7FFF;
  static const int EXTROM_SIZE = 16384;  // Extension ROM (cartridge)
  static const int MAIN_RAM_START = 0x4000;
  static const int MAIN_RAM_END = 0xC000;
  static const int MAIN_RAM_SIZE = 32768;  // Main RAM (32KB working area)
  static const int WORK_RAM_START = 0xC000;
  static const int WORK_RAM_END = 0xFFFF;
  static const int WORK_RAM_SIZE = 16384;  // Work RAM (16KB)
  static const int SYSVAR_START = 0xF000;
  static const int SYSVAR_END = 0xFCA0;
  static const int SYSVAR_SIZE = 3232;  // System variables area
  static const int SLOTS_START = 0x8000;
  static const int SLOTS_END = 0xFFFF;
  static const int SLOTS_SIZE = 32768;  // Slot-mapped memory

  // 外设定义
  // TMS9918A Video Display Processor
  static const int VDP_BASE = 0x98;
  static const int VDP_VDP_REG0_ADDR = 0x99;
  static const int VDP_VDP_REG1_ADDR = 0x99;
  static const int VDP_VDP_REG2_ADDR = 0x99;
  static const int VDP_VDP_REG3_ADDR = 0x99;
  static const int VDP_VDP_REG4_ADDR = 0x99;
  static const int VDP_VDP_REG5_ADDR = 0x99;
  static const int VDP_VDP_REG6_ADDR = 0x99;
  static const int VDP_VDP_REG7_ADDR = 0x99;
  static const int VDP_VDP_STATUS_ADDR = 0x99;
  static const int VDP_VDP_DATA_ADDR = 0x98;
  static const int VDP_VDP_POT_ADDR = 0x98;
  // AY-3-8910 Programmable Sound Generator
  static const int PSG_BASE = 0xA0;
  static const int PSG_PSG_REG_ADDR = 0xA1;
  static const int PSG_PSG_DATA_ADDR = 0xA3;
  static const int PSG_FREQ_A_LO_ADDR = 0xA0;
  static const int PSG_FREQ_A_HI_ADDR = 0xA1;
  static const int PSG_FREQ_B_LO_ADDR = 0xA2;
  static const int PSG_FREQ_B_HI_ADDR = 0xA3;
  static const int PSG_FREQ_C_LO_ADDR = 0xA4;
  static const int PSG_FREQ_C_HI_ADDR = 0xA5;
  static const int PSG_NOISE_FREQ_ADDR = 0xA6;
  static const int PSG_ENABLE_ADDR = 0xA7;
  static const int PSG_VOL_A_ADDR = 0xA8;
  static const int PSG_VOL_B_ADDR = 0xA9;
  static const int PSG_VOL_C_ADDR = 0xAA;
  static const int PSG_ENV_FREQ_LO_ADDR = 0xAB;
  static const int PSG_ENV_FREQ_HI_ADDR = 0xAC;
  static const int PSG_ENV_SHAPE_ADDR = 0xAD;
  static const int PSG_PORT_A_ADDR = 0xAE;
  static const int PSG_PORT_B_ADDR = 0xAF;
  // PPI 8255 Programmable Peripheral Interface
  static const int PPI_BASE = 0xA8;
  static const int PPI_PPI_PA_ADDR = 0xA8;
  static const int PPI_PPI_PB_ADDR = 0xA9;
  static const int PPI_PPI_PC_ADDR = 0xAA;
  static const int PPI_PPI_CTRL_ADDR = 0xAB;
  // MSX Slot Expansion System
  static const int SLOTEXP_BASE = 0x0000;
  static const int SLOTEXP_SLOT0_ADDR = 0xFCC0;
  static const int SLOTEXP_SLOT1_ADDR = 0xFCC1;
  static const int SLOTEXP_SLOT2_ADDR = 0xFCC2;
  static const int SLOTEXP_SLOT3_ADDR = 0xFCC3;
  static const int SLOTEXP_EXPTBL0_ADDR = 0xFCC4;
  static const int SLOTEXP_EXPTBL1_ADDR = 0xFCC5;
  static const int SLOTEXP_EXPTBL2_ADDR = 0xFCC6;
  static const int SLOTEXP_EXPTBL3_ADDR = 0xFCC7;

  // 中断向量定义
  static const int INT_RESET = 0;  // Power-on / Reset
  static const int INT_NMI = 1;  // Non-Maskable Interrupt
  static const int INT_INT = 2;  // VDP Vertical Interrupt (frame)

  // 引脚定义
  static const int PIN_VCC = 1;  // +5V Power
  static const int PIN_GND = 2;  // Ground
  static const int PIN_CLK = 3;  // Z80 Clock (3.58MHz)
  static const int PIN_A0_A15 = 4;  // Address Bus
  static const int PIN_D0_D7 = 5;  // Data Bus
  static const int PIN_MREQ = 6;  // Memory Request
  static const int PIN_IORQ = 7;  // I/O Request
  static const int PIN_RD = 8;  // Read
  static const int PIN_WR = 9;  // Write
  static const int PIN_INT = 10;  // Interrupt Request
  static const int PIN_NMI = 11;  // Non-Maskable Interrupt
  static const int PIN_RESET = 12;  // Reset
  static const int PIN_SLTSL = 13;  // Slot select (for memory mapping)
  static const int PIN_WAIT = 14;  // Wait (for slow I/O)

}
