// Sega-Genesis 设备定义 - Dart 库
// 生成自: Sega/Genesis/Mega Drive/Sega-Genesis
// 版本: 1.0
// 日期: 2026-04-17
// 作者: VML Team
// 描述: Sega Genesis/Mega Drive 16-bit video game console with Motorola 68000 CPU
// CPU架构: Motorola 68000
// 位宽: 32位
// 时钟频率: 7670000 Hz

class Sega_GenesisDevice {
  static const String deviceName = "Sega-Genesis";
  static const String manufacturer = "Sega";
  static const String family = "Genesis/Mega Drive";
  static const String version = "1.0";
  static const String architecture = "Motorola 68000";
  static const int bits = 32;
  static const int clockFrequency = 7670000;

  // 寄存器地址定义
  static const int D0_ADDR = 0;  // Data Register 0
  static const int D1_ADDR = 0;  // Data Register 1
  static const int D2_ADDR = 0;  // Data Register 2
  static const int D3_ADDR = 0;  // Data Register 3
  static const int D4_ADDR = 0;  // Data Register 4
  static const int D5_ADDR = 0;  // Data Register 5
  static const int D6_ADDR = 0;  // Data Register 6
  static const int D7_ADDR = 0;  // Data Register 7
  static const int A0_ADDR = 0;  // Address Register 0
  static const int A1_ADDR = 0;  // Address Register 1
  static const int A2_ADDR = 0;  // Address Register 2
  static const int A3_ADDR = 0;  // Address Register 3
  static const int A4_ADDR = 0;  // Address Register 4
  static const int A5_ADDR = 0;  // Address Register 5
  static const int A6_ADDR = 0;  // Address Register 6
  static const int A7_ADDR = 0;  // Address Register 7 (SP)
  static const int PC_ADDR = 0;  // Program Counter
  static const int SR_ADDR = 0;  // Status Register

  // 外设定义
  // Video Display Processor (315-5313)
  static const int VDP_BASE = ;
  static const int VDP_VDP_DATA_ADDR = 0xC00000;
  static const int VDP_VDP_CONTROL_ADDR = 0xC00004;
  static const int VDP_VDP_HVCOUNTER_ADDR = 0xC00008;
  static const int VDP_VDP_PSG_ADDR = 0xC00011;
  // FM synthesis sound chip
  static const int YM2612_BASE = ;
  static const int YM2612_YM2612_ADDR0_ADDR = 0xA04000;
  static const int YM2612_YM2612_DATA0_ADDR = 0xA04001;
  static const int YM2612_YM2612_ADDR1_ADDR = 0xA04002;
  static const int YM2612_YM2612_DATA1_ADDR = 0xA04003;
  // I/O ports
  static const int IOPORTS_BASE = ;
  static const int IOPORTS_IO_DATA1_ADDR = 0xA10002;
  static const int IOPORTS_IO_DATA2_ADDR = 0xA10004;
  static const int IOPORTS_IO_DATA3_ADDR = 0xA10006;
  static const int IOPORTS_IO_CTRL1_ADDR = 0xA10008;
  static const int IOPORTS_IO_CTRL2_ADDR = 0xA1000A;
  static const int IOPORTS_IO_CTRL3_ADDR = 0xA1000C;
  // TradeMark Security System
  static const int TMSS_BASE = ;
  static const int TMSS_TMSS_ADDR = 0xA14000;
  // Z80 bus control
  static const int Z80BUS_BASE = ;
  static const int Z80BUS_Z80_BUSREQ_ADDR = 0xA11100;
  static const int Z80BUS_Z80_RESET_ADDR = 0xA11200;
  static const int Z80BUS_Z80_YM2612_ADDR = 0xA04000;

  // 中断向量定义
  static const int INT_RESET_SP = 0;  // Reset (Initial SP)
  static const int INT_RESET_PC = 4;  // Reset (Initial PC)
  static const int INT_HBLANK = 24;  // Horizontal blank interrupt
  static const int INT_VBLANK = 28;  // Vertical blank interrupt
  static const int INT_EXTINT1 = 32;  // External interrupt 1
  static const int INT_EXTINT2 = 36;  // External interrupt 2
  static const int INT_EXTINT3 = 40;  // External interrupt 3
  static const int INT_EXTINT4 = 44;  // External interrupt 4
  static const int INT_EXTINT5 = 48;  // External interrupt 5
  static const int INT_EXTINT6 = 52;  // External interrupt 6
  static const int INT_EXTINT7 = 56;  // External interrupt 7

}
