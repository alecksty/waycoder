// Commodore-PET 设备定义 - Dart 库
// 生成自: Commodore International/PET/Commodore-PET
// 版本: 1.0
// 日期: 2026-04-17
// 作者: VML Team
// 描述: Commodore PET 2001 personal computer with MOS 6502 CPU and built-in monitor
// CPU架构: MOS 6502
// 位宽: 8位
// 时钟频率: 1000000 Hz

class Commodore_PETDevice {
  static const String deviceName = "Commodore-PET";
  static const String manufacturer = "Commodore International";
  static const String family = "PET";
  static const String version = "1.0";
  static const String architecture = "MOS 6502";
  static const int bits = 8;
  static const int clockFrequency = 1000000;

  // 寄存器地址定义
  static const int A_ADDR = 0;  // Accumulator
  static const int X_ADDR = 0;  // Index Register X
  static const int Y_ADDR = 0;  // Index Register Y
  static const int SP_ADDR = 0;  // Stack Pointer
  static const int PC_ADDR = 0;  // Program Counter
  static const int P_ADDR = 0;  // Status Register

  // 外设定义
  // Peripheral Interface Adapter 1 (6520)
  static const int PIA1_BASE = ;
  static const int PIA1_PIA1_DDRA_ADDR = 0xE810;
  static const int PIA1_PIA1_ORA_ADDR = 0xE811;
  static const int PIA1_PIA1_DDRB_ADDR = 0xE812;
  static const int PIA1_PIA1_ORB_ADDR = 0xE813;
  static const int PIA1_PIA1_CRA_ADDR = 0xE814;
  static const int PIA1_PIA1_CRB_ADDR = 0xE815;
  // Peripheral Interface Adapter 2 (6520)
  static const int PIA2_BASE = ;
  static const int PIA2_PIA2_DDRA_ADDR = 0xE820;
  static const int PIA2_PIA2_ORA_ADDR = 0xE821;
  static const int PIA2_PIA2_DDRB_ADDR = 0xE822;
  static const int PIA2_PIA2_ORB_ADDR = 0xE823;
  static const int PIA2_PIA2_CRA_ADDR = 0xE824;
  static const int PIA2_PIA2_CRB_ADDR = 0xE825;
  // Versatile Interface Adapter (6522)
  static const int VIA_BASE = ;
  static const int VIA_VIA_ORB_ADDR = 0xE840;
  static const int VIA_VIA_ORA_ADDR = 0xE841;
  static const int VIA_VIA_DDRB_ADDR = 0xE842;
  static const int VIA_VIA_DDRA_ADDR = 0xE843;
  static const int VIA_VIA_T1CL_ADDR = 0xE844;
  static const int VIA_VIA_T1CH_ADDR = 0xE845;
  static const int VIA_VIA_T1LL_ADDR = 0xE846;
  static const int VIA_VIA_T1LH_ADDR = 0xE847;
  static const int VIA_VIA_T2CL_ADDR = 0xE848;
  static const int VIA_VIA_T2CH_ADDR = 0xE849;
  static const int VIA_VIA_SR_ADDR = 0xE84A;
  static const int VIA_VIA_ACR_ADDR = 0xE84B;
  static const int VIA_VIA_PCR_ADDR = 0xE84C;
  static const int VIA_VIA_IFR_ADDR = 0xE84D;
  static const int VIA_VIA_IER_ADDR = 0xE84E;
  // CRT Controller (6545)
  static const int CRTC_BASE = ;
  static const int CRTC_CRTC_ADDR_ADDR = 0xE880;
  static const int CRTC_CRTC_DATA_ADDR = 0xE881;
  // Cassette tape interface
  static const int CASSETTE_BASE = ;
  static const int CASSETTE_CASS_MOTOR_ADDR = 0xE840;
  static const int CASSETTE_CASS_WRITE_ADDR = 0xE842;
  static const int CASSETTE_CASS_READ_ADDR = 0xE812;
  // IEEE-488 bus interface
  static const int IEEE488_BASE = ;
  static const int IEEE488_IEEE_DATA_ADDR = 0xE801;
  static const int IEEE488_IEEE_STATUS_ADDR = 0xE802;
  static const int IEEE488_IEEE_CONTROL_ADDR = 0xE803;

  // 中断向量定义
  static const int INT_NMI = 65526;  // Non-maskable interrupt
  static const int INT_RESET = 65528;  // Reset vector
  static const int INT_IRQ = 65530;  // Interrupt request
  static const int INT_BRK = 65532;  // Break instruction

}
