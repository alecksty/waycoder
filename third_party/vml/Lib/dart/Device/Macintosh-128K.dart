// Macintosh-128K 设备定义 - Dart 库
// 生成自: Apple Computer/Macintosh/Macintosh-128K
// 版本: 1.0
// 日期: 2026-04-17
// 作者: VML Team
// 描述: Original Macintosh 128K with Motorola 68000 CPU, 128KB RAM, and 9-inch monochrome display
// CPU架构: Motorola 68000
// 位宽: 32位
// 时钟频率: 7998000 Hz

class Macintosh_128KDevice {
  static const String deviceName = "Macintosh-128K";
  static const String manufacturer = "Apple Computer";
  static const String family = "Macintosh";
  static const String version = "1.0";
  static const String architecture = "Motorola 68000";
  static const int bits = 32;
  static const int clockFrequency = 7998000;

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
  // Versatile Interface Adapter (6522)
  static const int VIA_BASE = ;
  static const int VIA_VIA_ORB_ADDR = 0xE80000;
  static const int VIA_VIA_ORA_ADDR = 0xE80001;
  static const int VIA_VIA_DDRB_ADDR = 0xE80002;
  static const int VIA_VIA_DDRA_ADDR = 0xE80003;
  static const int VIA_VIA_T1CL_ADDR = 0xE80004;
  static const int VIA_VIA_T1CH_ADDR = 0xE80005;
  static const int VIA_VIA_T1LL_ADDR = 0xE80006;
  static const int VIA_VIA_T1LH_ADDR = 0xE80007;
  static const int VIA_VIA_T2CL_ADDR = 0xE80008;
  static const int VIA_VIA_T2CH_ADDR = 0xE80009;
  static const int VIA_VIA_SR_ADDR = 0xE8000A;
  static const int VIA_VIA_ACR_ADDR = 0xE8000B;
  static const int VIA_VIA_PCR_ADDR = 0xE8000C;
  static const int VIA_VIA_IFR_ADDR = 0xE8000D;
  static const int VIA_VIA_IER_ADDR = 0xE8000E;
  static const int VIA_VIA_ORA2_ADDR = 0xE8000F;
  // Integrated Woz Machine (floppy controller)
  static const int IWM_BASE = ;
  static const int IWM_IWM_Q6_ADDR = 0xD00000;
  static const int IWM_IWM_Q7_ADDR = 0xD00002;
  static const int IWM_IWM_PH0_ADDR = 0xD00004;
  static const int IWM_IWM_PH1_ADDR = 0xD00006;
  static const int IWM_IWM_PH2_ADDR = 0xD00008;
  static const int IWM_IWM_PH3_ADDR = 0xD0000A;
  // Zilog 8530 Serial Communications Controller
  static const int SCC_BASE = ;
  static const int SCC_SCC_CA_ADDR = 0x500000;
  static const int SCC_SCC_DA_ADDR = 0x500002;
  static const int SCC_SCC_CB_ADDR = 0x500004;
  static const int SCC_SCC_DB_ADDR = 0x500006;
  // Built-in speaker
  static const int SOUND_BASE = ;
  static const int SOUND_SOUND_VOL_ADDR = 0xE80100;
  static const int SOUND_SOUND_FREQ_ADDR = 0xE80102;

  // 中断向量定义
  static const int INT_RESET_SP = 0;  // Reset (Initial SP)
  static const int INT_RESET_PC = 4;  // Reset (Initial PC)
  static const int INT_AUTOVECTOR1 = 24;  // Auto vector 1
  static const int INT_AUTOVECTOR2 = 25;  // Auto vector 2
  static const int INT_AUTOVECTOR3 = 26;  // Auto vector 3
  static const int INT_AUTOVECTOR4 = 27;  // Auto vector 4
  static const int INT_AUTOVECTOR5 = 28;  // Auto vector 5
  static const int INT_AUTOVECTOR6 = 29;  // Auto vector 6
  static const int INT_AUTOVECTOR7 = 30;  // Auto vector 7
  static const int INT_SPURIOUS = 31;  // Spurious interrupt

}
