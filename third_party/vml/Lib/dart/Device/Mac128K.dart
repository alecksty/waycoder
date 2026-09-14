// Macintosh-128K 设备定义 - Dart 库
// 生成自: Apple Computer/Macintosh/Macintosh-128K
// 版本: 1.0
// 日期: 2026-04-17
// 作者: VML Team
// 描述: Apple Macintosh 128K - First Macintosh - Motorola 68000, 128KB RAM, 512x342 display
// CPU架构: MC68000
// 位宽: 32位
// 时钟频率: 7833600 Hz

class Macintosh_128KDevice {
  static const String deviceName = "Macintosh-128K";
  static const String manufacturer = "Apple Computer";
  static const String family = "Macintosh";
  static const String version = "1.0";
  static const String architecture = "MC68000";
  static const int bits = 32;
  static const int clockFrequency = 7833600;

  // 寄存器地址定义
  static const int D0_ADDR = 0x00;  // Data Register 0
  static const int D1_ADDR = 0x04;  // Data Register 1
  static const int D2_ADDR = 0x08;  // Data Register 2
  static const int D3_ADDR = 0x0C;  // Data Register 3
  static const int D4_ADDR = 0x10;  // Data Register 4
  static const int D5_ADDR = 0x14;  // Data Register 5
  static const int D6_ADDR = 0x18;  // Data Register 6
  static const int D7_ADDR = 0x1C;  // Data Register 7
  static const int A0_ADDR = 0x20;  // Address Register 0
  static const int A1_ADDR = 0x24;  // Address Register 1
  static const int A2_ADDR = 0x28;  // Address Register 2
  static const int A3_ADDR = 0x2C;  // Address Register 3
  static const int A4_ADDR = 0x30;  // Address Register 4
  static const int A5_ADDR = 0x34;  // Address Register 5
  static const int A6_ADDR = 0x38;  // Address Register 6
  static const int A7_ADDR = 0x3C;  // Stack Pointer (USP)
  static const int PC_ADDR = 0x40;  // Program Counter
  static const int SR_ADDR = 0x44;  // Status Register
  static const int SR_C_BIT = 0;  // Carry
  static const int SR_V_BIT = 1;  // Overflow
  static const int SR_Z_BIT = 2;  // Zero
  static const int SR_N_BIT = 3;  // Negative
  static const int SR_X_BIT = 4;  // Extend
  static const int SR_I0_BIT = 8;  // Interrupt Mask 0
  static const int SR_I1_BIT = 9;  // Interrupt Mask 1
  static const int SR_I2_BIT = 10;  // Interrupt Mask 2
  static const int SR_S_BIT = 13;  // Supervisor/User
  static const int SR_T0_BIT = 14;  // Trace Mode 0
  static const int SR_T1_BIT = 15;  // Trace Mode 1

  // 内存段定义
  static const int RAM_START = 0x000000;
  static const int RAM_END = 0x01FFFF;
  static const int RAM_SIZE = 131072;  // Main RAM (128KB unified)
  static const int ROM_START = 0x40000000;
  static const int ROM_END = 0x4001FFFF;
  static const int ROM_SIZE = 131072;  // Mac ROM (128KB)
  static const int FRAMEBUFFER_START = 0x00400000;
  static const int FRAMEBUFFER_END = 0x00400555;
  static const int FRAMEBUFFER_SIZE = 1366;  // Screen bitmap (512x342x1 = 21792 bytes)
  static const int FRAMEBUFFER2_START = 0x00410000;
  static const int FRAMEBUFFER2_END = 0x00410555;
  static const int FRAMEBUFFER2_SIZE = 1366;  // Shadow screen (double-buffering)
  static const int VIA_START = 0x00E00000;
  static const int VIA_END = 0x00E0FFFF;
  static const int VIA_SIZE = 4096;  // VIA 6522 (I/O)
  static const int SCC_START = 0x00F00000;
  static const int SCC_END = 0x00F0FFFF;
  static const int SCC_SIZE = 4096;  // SCC 8530 (serial)
  static const int ADB_START = 0x01600000;
  static const int ADB_END = 0x0160FFFF;
  static const int ADB_SIZE = 4096;  // ADB bus
  static const int IWM_START = 0x01E00000;
  static const int IWM_END = 0x01E0FFFF;
  static const int IWM_SIZE = 4096;  // IWM floppy controller

  // 外设定义
  // Versatile Interface Adapter 6522
  static const int VIA_BASE = 0xE00000;
  static const int VIA_ORB_ADDR = 0xE00000;
  static const int VIA_ORA_ADDR = 0xE00002;
  static const int VIA_DDRB_ADDR = 0xE00004;
  static const int VIA_DDRA_ADDR = 0xE00006;
  static const int VIA_T1C_L_ADDR = 0xE00008;
  static const int VIA_T1C_H_ADDR = 0xE0000A;
  static const int VIA_T1L_L_ADDR = 0xE0000C;
  static const int VIA_T1L_H_ADDR = 0xE0000E;
  static const int VIA_T2C_L_ADDR = 0xE00010;
  static const int VIA_T2C_H_ADDR = 0xE00012;
  static const int VIA_SR_ADDR = 0xE00014;
  static const int VIA_ACR_ADDR = 0xE00016;
  static const int VIA_PCR_ADDR = 0xE00018;
  static const int VIA_IFR_ADDR = 0xE0001E;
  static const int VIA_IER_ADDR = 0xE0001E;
  // SCC 8530 Serial Communications Controller
  static const int SCC_BASE = 0xF00000;
  static const int SCC_SCC_CHA_B_ADDR = 0xF00000;
  static const int SCC_SCC_CHA_C_ADDR = 0xF00002;
  static const int SCC_SCC_CHB_D_ADDR = 0xF00004;
  static const int SCC_SCC_CHB_CT_ADDR = 0xF00006;
  // Integrated Woz Machine - Floppy Disk Controller
  static const int IWM_BASE = 0x1E00000;
  static const int IWM_IWM_DATA_ADDR = 0x1E00000;
  static const int IWM_IWM_MODE_ADDR = 0x1E00008;
  static const int IWM_IWM_Q6L_ADDR = 0x1E00020;
  static const int IWM_IWM_Q7L_ADDR = 0x1E00022;
  static const int IWM_IWM_Q6R_ADDR = 0x1E00024;
  static const int IWM_IWM_Q7R_ADDR = 0x1E00026;
  // Video Graphics Controller (custom Apple chip)
  static const int VGC_BASE = 0x00F20000;
  static const int VGC_VGC_MODE_ADDR = 0x00F20000;
  static const int VGC_VGC_START_HI_ADDR = 0x00F20002;
  static const int VGC_VGC_START_LO_ADDR = 0x00F20004;
  // Apple Desktop Bus
  static const int ADB_BASE = 0x01600000;
  static const int ADB_ADB_DATA_ADDR = 0x01600000;
  static const int ADB_ADB_STATUS_ADDR = 0x01600004;
  static const int ADB_ADB_CMD_ADDR = 0x01600008;

  // 中断向量定义
  static const int INT_RESET = 1;  // Reset Initial SP
  static const int INT_RESET_PC = 2;  // Reset Initial PC
  static const int INT_IRQ1 = 24;  // VIA interrupt (level 1)
  static const int INT_IRQ2 = 25;  // SCC interrupt (level 2)
  static const int INT_IRQ3 = 26;  // ADB / VIA (level 3)
  static const int INT_IRQ4 = 27;  // ADB / VIA (level 4)

  // 引脚定义
  static const int PIN_VCC = 1;  // +5V Power
  static const int PIN_GND = 2;  // Ground
  static const int PIN_CLK = 3;  // 16MHz master clock / 7.83MHz CPU clock
  static const int PIN_FC0 = 4;  // Function Code 0
  static const int PIN_FC1 = 5;  // Function Code 1
  static const int PIN_FC2 = 6;  // Function Code 2
  static const int PIN_AS = 7;  // Address Strobe
  static const int PIN_UDS = 8;  // Upper Data Strobe
  static const int PIN_LDS = 9;  // Lower Data Strobe
  static const int PIN_RWB = 10;  // Read/Write
  static const int PIN_DTACK = 11;  // Data Acknowledge
  static const int PIN_BERR = 12;  // Bus Error
  static const int PIN_BR = 13;  // Bus Request
  static const int PIN_BG = 14;  // Bus Grant
  static const int PIN_BGACK = 15;  // Bus Grant Acknowledge
  static const int PIN_IPL0 = 16;  // Interrupt Priority 0
  static const int PIN_IPL1 = 17;  // Interrupt Priority 1
  static const int PIN_IPL2 = 18;  // Interrupt Priority 2
  static const int PIN_RESET = 19;  // Reset
  static const int PIN_HALT = 20;  // Halt
  static const int PIN_A1_A23 = 21;  // Address Bus (24-bit)
  static const int PIN_D0_D15 = 22;  // Data Bus (16-bit)

}
