// ZX-Spectrum 设备定义 - Dart 库
// 生成自: Sinclair Research/ZX Spectrum/ZX-Spectrum
// 版本: 1.0
// 日期: 2026-04-17
// 作者: VML Team
// 描述: ZX Spectrum 48K home computer with Z80 CPU, 48KB RAM, and color graphics
// CPU架构: Zilog Z80
// 位宽: 8位
// 时钟频率: 3500000 Hz

class ZX_SpectrumDevice {
  static const String deviceName = "ZX-Spectrum";
  static const String manufacturer = "Sinclair Research";
  static const String family = "ZX Spectrum";
  static const String version = "1.0";
  static const String architecture = "Zilog Z80";
  static const int bits = 8;
  static const int clockFrequency = 3500000;

  // 寄存器地址定义
  static const int A_ADDR = 0;  // Accumulator
  static const int F_ADDR = 0;  // Flags
  static const int B_ADDR = 0;  // B
  static const int C_ADDR = 0;  // C
  static const int D_ADDR = 0;  // D
  static const int E_ADDR = 0;  // E
  static const int H_ADDR = 0;  // H
  static const int L_ADDR = 0;  // L
  static const int IX_ADDR = 0;  // Index Register X
  static const int IY_ADDR = 0;  // Index Register Y
  static const int SP_ADDR = 0;  // Stack Pointer
  static const int PC_ADDR = 0;  // Program Counter
  static const int I_ADDR = 0;  // Interrupt Vector
  static const int R_ADDR = 0;  // Memory Refresh
  static const int AF_ADDR = 0;  // Alternate AF
  static const int BC_ADDR = 0;  // Alternate BC
  static const int DE_ADDR = 0;  // Alternate DE
  static const int HL_ADDR = 0;  // Alternate HL

  // 外设定义
  // Uncommitted Logic Array (video and I/O)
  static const int ULA_BASE = ;
  static const int ULA_ULA_PORT_FE_ADDR = 0xFE;
  static const int ULA_ULA_BORDER_ADDR = 0xFE;
  static const int ULA_ULA_BEEPER_ADDR = 0xFE;
  static const int ULA_ULA_MIC_ADDR = 0xFE;
  // General Instruments AY-3-8912 sound chip
  static const int AY_3_8912_BASE = ;
  static const int AY_3_8912_AY_REG_SEL_ADDR = 0xFFFD;
  static const int AY_3_8912_AY_DATA_ADDR = 0xBFFD;
  static const int AY_3_8912_AY_READ_ADDR = 0xFFFD;
  // 40-key rubber keyboard
  static const int KEYBOARD_BASE = ;
  static const int KEYBOARD_KEY_ROW0_ADDR = 0xFEFE;
  static const int KEYBOARD_KEY_ROW1_ADDR = 0xFDFE;
  static const int KEYBOARD_KEY_ROW2_ADDR = 0xFBFE;
  static const int KEYBOARD_KEY_ROW3_ADDR = 0xF7FE;
  static const int KEYBOARD_KEY_ROW4_ADDR = 0xEFFE;
  static const int KEYBOARD_KEY_ROW5_ADDR = 0xDFFE;
  static const int KEYBOARD_KEY_ROW6_ADDR = 0xBFFE;
  static const int KEYBOARD_KEY_ROW7_ADDR = 0x7FFE;
  // Kempston joystick interface
  static const int KEMPSTON_BASE = ;
  static const int KEMPSTON_KEMPSTON_JOY_ADDR = 0x1F;
  // ZX Interface 1 (RS-232 and Microdrive)
  static const int INTERFACE1_BASE = ;
  static const int INTERFACE1_IF1_STATUS_ADDR = 0x1FFD;
  static const int INTERFACE1_IF1_DATA_ADDR = 0x3FFD;
  // ZX Interface 2 (joystick and ROM cartridge)
  static const int INTERFACE2_BASE = ;
  static const int INTERFACE2_IF2_JOY1_ADDR = 0x1F;
  static const int INTERFACE2_IF2_JOY2_ADDR = 0x37;

  // 中断向量定义
  static const int INT_IM1 = 56;  // Interrupt Mode 1
  static const int INT_RST_00 = 0;  // Restart 00h
  static const int INT_RST_08 = 8;  // Restart 08h
  static const int INT_RST_10 = 16;  // Restart 10h
  static const int INT_RST_18 = 24;  // Restart 18h
  static const int INT_RST_20 = 32;  // Restart 20h
  static const int INT_RST_28 = 40;  // Restart 28h
  static const int INT_RST_30 = 48;  // Restart 30h
  static const int INT_RST_38 = 56;  // Restart 38h

}
