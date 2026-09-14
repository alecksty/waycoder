// Sega-Master-System 设备定义 - Dart 库
// 生成自: Sega/Master System/Sega-Master-System
// 版本: 1.0
// 日期: 2026-04-17
// 作者: VML Team
// 描述: Sega Master System 8-bit video game console with Z80 CPU
// CPU架构: Zilog Z80
// 位宽: 8位
// 时钟频率: 3579545 Hz

class Sega_Master_SystemDevice {
  static const String deviceName = "Sega-Master-System";
  static const String manufacturer = "Sega";
  static const String family = "Master System";
  static const String version = "1.0";
  static const String architecture = "Zilog Z80";
  static const int bits = 8;
  static const int clockFrequency = 3579545;

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

  // 外设定义
  // Video Display Processor (TMS9918A)
  static const int VDP_BASE = ;
  static const int VDP_VDP_DATA_ADDR = 0xBE;
  static const int VDP_VDP_ADDR_ADDR = 0xBF;
  static const int VDP_VDP_STATUS_ADDR = 0xBF;
  // Programmable Sound Generator (SN76489)
  static const int PSG_BASE = ;
  static const int PSG_PSG_DATA_ADDR = 0x7F;
  // I/O ports
  static const int IO_BASE = ;
  static const int IO_IO_PORT_A_ADDR = 0xDC;
  static const int IO_IO_PORT_B_ADDR = 0xDD;
  static const int IO_IO_PORT_MISC_ADDR = 0xDE;
  static const int IO_IO_PORT_VDP_ADDR = 0xDF;
  // Memory mapper
  static const int MEMORYMAPPER_BASE = ;
  static const int MEMORYMAPPER_MAPPER_0_ADDR = 0xFFFC;
  static const int MEMORYMAPPER_MAPPER_1_ADDR = 0xFFFD;
  static const int MEMORYMAPPER_MAPPER_2_ADDR = 0xFFFE;
  static const int MEMORYMAPPER_MAPPER_3_ADDR = 0xFFFF;
  // FM Sound Unit (optional)
  static const int FMUNIT_BASE = ;
  static const int FMUNIT_FM_ADDR_ADDR = 0xF0;
  static const int FMUNIT_FM_DATA_ADDR = 0xF1;
  static const int FMUNIT_FM_DETECT_ADDR = 0xF2;

  // 中断向量定义
  static const int INT_RST_00 = 0;  // Restart 00h
  static const int INT_IM1 = 56;  // Interrupt Mode 1
  static const int INT_VBLANK = 56;  // Vertical blank interrupt
  static const int INT_LINE = 100;  // Line interrupt

}
