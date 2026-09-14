// ZX-Spectrum-48K 设备定义 - Dart 库
// 生成自: Sinclair Research/ZX Spectrum/ZX-Spectrum-48K
// 版本: 1.0
// 日期: 2026-04-17
// 作者: VML Team
// 描述: Sinclair ZX Spectrum 48K - Iconic British 8-bit home computer with Z80A CPU and ULA graphics
// CPU架构: Z80A
// 位宽: 8位
// 时钟频率: 3500000 Hz

class ZX_Spectrum_48KDevice {
  static const String deviceName = "ZX-Spectrum-48K";
  static const String manufacturer = "Sinclair Research";
  static const String family = "ZX Spectrum";
  static const String version = "1.0";
  static const String architecture = "Z80A";
  static const int bits = 8;
  static const int clockFrequency = 3500000;

  // 寄存器地址定义
  static const int A_ADDR = 0x00;  // Accumulator
  static const int F_ADDR = 0x01;  // Flags Register
  static const int F_C_BIT = 0;  // Carry
  static const int F_N_BIT = 1;  // Add/Subtract
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
  static const int I_ADDR = 0x10;  // Interrupt Vector Register
  static const int R_ADDR = 0x11;  // Refresh Counter
  static const int IX_ADDR = 0x12;  // Index X
  static const int IY_ADDR = 0x14;  // Index Y
  static const int SP_ADDR = 0x16;  // Stack Pointer
  static const int PC_ADDR = 0x18;  // Program Counter

  // 内存段定义
  static const int ROM_START = 0x0000;
  static const int ROM_END = 0x3FFF;
  static const int ROM_SIZE = 16384;  // 48KB ZX Spectrum ROM (BASIC + monitor)
  static const int VIDEO_RAM_START = 0x4000;
  static const int VIDEO_RAM_END = 0x57FF;
  static const int VIDEO_RAM_SIZE = 6144;  // Display file (256x192 bitmap)
  static const int ATTR_RAM_START = 0x5800;
  static const int ATTR_RAM_END = 0x5AFF;
  static const int ATTR_RAM_SIZE = 768;  // Attribute file (32x24 color cells)
  static const int USER_RAM_START = 0x5B00;
  static const int USER_RAM_END = 0xFFFF;
  static const int USER_RAM_SIZE = 40960;  // User RAM (40KB)

  // 外设定义
  // Uncommitted Logic Array - Sinclair custom IC
  static const int ULA_BASE = 0xFE;
  static const int ULA_BORDER_ADDR = 0xFE;
  static const int ULA_KBD_ROW0_ADDR = 0xFE;
  static const int ULA_KBD_ROW1_ADDR = 0xFE;
  static const int ULA_KBD_ROW2_ADDR = 0xFE;
  static const int ULA_KBD_ROW3_ADDR = 0xFE;
  static const int ULA_KBD_ROW4_ADDR = 0xFE;
  static const int ULA_KBD_ROW5_ADDR = 0xFE;
  static const int ULA_KBD_ROW6_ADDR = 0xFE;
  static const int ULA_KBD_ROW7_ADDR = 0xFE;
  static const int ULA_KBD_ROW8_ADDR = 0xFE;
  // Keyboard Matrix (40 keys, 8 rows x 5 cols)
  static const int KEYBOARD_BASE = 0xFE;
  static const int KEYBOARD_KBD_IN_ADDR = 0xFE;
  // Internal Beeper
  static const int BEEPER_BASE = 0xFE;
  static const int BEEPER_BEEP_ADDR = 0xFE;
  // Tape Interface
  static const int TAPE_BASE = 0xFE;
  static const int TAPE_EAR_IN_ADDR = 0xFE;
  static const int TAPE_MIC_OUT_ADDR = 0xFE;
  // Kempston Joystick Interface
  static const int JOYSTICK_BASE = 0xF7FE;
  static const int JOYSTICK_KEMPSTON_ADDR = 0xF7FE;

  // 中断向量定义
  static const int INT_RESET = 0;  // Power-on / Reset
  static const int INT_NMI = 1;  // Non-Maskable Interrupt (BREAK key)
  static const int INT_INT = 2;  // Maskable Interrupt (ULA vertical blank, 50Hz)

  // 引脚定义
  static const int PIN_VCC = 1;  // +5V Power
  static const int PIN_GND = 2;  // Ground
  static const int PIN_CLK = 3;  // Z80 Clock (3.5MHz)
  static const int PIN_M1 = 4;  // Machine Cycle 1
  static const int PIN_MREQ = 5;  // Memory Request
  static const int PIN_IORQ = 6;  // I/O Request
  static const int PIN_RD = 7;  // Read
  static const int PIN_WR = 8;  // Write
  static const int PIN_HALT = 9;  // Halt State
  static const int PIN_BUSAK = 10;  // Bus Acknowledge
  static const int PIN_WAIT = 11;  // Wait State (ULA inserts)
  static const int PIN_INT = 12;  // Interrupt Request
  static const int PIN_NMI = 13;  // Non-Maskable Interrupt
  static const int PIN_RESET = 14;  // Reset
  static const int PIN_A0_A15 = 15;  // Address Bus (16-bit)
  static const int PIN_D0_D7 = 16;  // Data Bus (8-bit)

}
