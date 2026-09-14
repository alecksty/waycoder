// Commodore-64 设备定义 - Dart 库
// 生成自: Commodore International/Commodore 64/Commodore-64
// 版本: 1.0
// 日期: 2026-04-17
// 作者: VML Team
// 描述: Commodore 64 home computer with MOS 6510 CPU, 64KB RAM, and SID sound chip
// CPU架构: MOS 6510
// 位宽: 8位
// 时钟频率: 985248 Hz

class Commodore_64Device {
  static const String deviceName = "Commodore-64";
  static const String manufacturer = "Commodore International";
  static const String family = "Commodore 64";
  static const String version = "1.0";
  static const String architecture = "MOS 6510";
  static const int bits = 8;
  static const int clockFrequency = 985248;

  // 寄存器地址定义
  static const int A_ADDR = 0;  // Accumulator
  static const int X_ADDR = 0;  // Index Register X
  static const int Y_ADDR = 0;  // Index Register Y
  static const int SP_ADDR = 0;  // Stack Pointer
  static const int PC_ADDR = 0;  // Program Counter
  static const int P_ADDR = 0;  // Status Register
  static const int PORT_ADDR = 1;  // I/O Port (6510 specific)

  // 外设定义
  // Video Interface Chip II
  static const int VIC_II_BASE = ;
  static const int VIC_II_VIC_CTRL1_ADDR = 0xD011;
  static const int VIC_II_VIC_CTRL2_ADDR = 0xD016;
  static const int VIC_II_VIC_RASTER_ADDR = 0xD012;
  static const int VIC_II_VIC_MEMPTR_ADDR = 0xD018;
  static const int VIC_II_VIC_IRQ_ADDR = 0xD019;
  static const int VIC_II_VIC_IRQMASK_ADDR = 0xD01A;
  static const int VIC_II_VIC_BORDER_ADDR = 0xD020;
  static const int VIC_II_VIC_BG0_ADDR = 0xD021;
  static const int VIC_II_VIC_BG1_ADDR = 0xD022;
  static const int VIC_II_VIC_BG2_ADDR = 0xD023;
  static const int VIC_II_VIC_BG3_ADDR = 0xD024;
  static const int VIC_II_VIC_SPRITE0_X_ADDR = 0xD000;
  static const int VIC_II_VIC_SPRITE0_Y_ADDR = 0xD001;
  static const int VIC_II_VIC_SPRITE1_X_ADDR = 0xD002;
  static const int VIC_II_VIC_SPRITE1_Y_ADDR = 0xD003;
  // Sound Interface Device (6581)
  static const int SID_BASE = ;
  static const int SID_SID_VOICE1_FREQ_LO_ADDR = 0xD400;
  static const int SID_SID_VOICE1_FREQ_HI_ADDR = 0xD401;
  static const int SID_SID_VOICE1_PW_LO_ADDR = 0xD402;
  static const int SID_SID_VOICE1_PW_HI_ADDR = 0xD403;
  static const int SID_SID_VOICE1_CTRL_ADDR = 0xD404;
  static const int SID_SID_VOICE1_AD_ADDR = 0xD405;
  static const int SID_SID_VOICE1_SR_ADDR = 0xD406;
  static const int SID_SID_VOICE2_FREQ_LO_ADDR = 0xD407;
  static const int SID_SID_VOICE2_FREQ_HI_ADDR = 0xD408;
  static const int SID_SID_VOICE2_PW_LO_ADDR = 0xD409;
  static const int SID_SID_VOICE2_PW_HI_ADDR = 0xD40A;
  static const int SID_SID_VOICE2_CTRL_ADDR = 0xD40B;
  static const int SID_SID_VOICE2_AD_ADDR = 0xD40C;
  static const int SID_SID_VOICE2_SR_ADDR = 0xD40D;
  static const int SID_SID_VOICE3_FREQ_LO_ADDR = 0xD40E;
  static const int SID_SID_VOICE3_FREQ_HI_ADDR = 0xD40F;
  static const int SID_SID_VOICE3_PW_LO_ADDR = 0xD410;
  static const int SID_SID_VOICE3_PW_HI_ADDR = 0xD411;
  static const int SID_SID_VOICE3_CTRL_ADDR = 0xD412;
  static const int SID_SID_VOICE3_AD_ADDR = 0xD413;
  static const int SID_SID_VOICE3_SR_ADDR = 0xD414;
  static const int SID_SID_FILTER_CUTOFF_LO_ADDR = 0xD415;
  static const int SID_SID_FILTER_CUTOFF_HI_ADDR = 0xD416;
  static const int SID_SID_FILTER_CTRL_ADDR = 0xD417;
  static const int SID_SID_VOLUME_ADDR = 0xD418;
  static const int SID_SID_POTX_ADDR = 0xD419;
  static const int SID_SID_POTY_ADDR = 0xD41A;
  static const int SID_SID_OSC3_ADDR = 0xD41B;
  static const int SID_SID_ENV3_ADDR = 0xD41C;
  // Complex Interface Adapter 1 (6526)
  static const int CIA1_BASE = ;
  static const int CIA1_CIA1_PRA_ADDR = 0xDC00;
  static const int CIA1_CIA1_PRB_ADDR = 0xDC01;
  static const int CIA1_CIA1_DDRA_ADDR = 0xDC02;
  static const int CIA1_CIA1_DDRB_ADDR = 0xDC03;
  static const int CIA1_CIA1_TALO_ADDR = 0xDC04;
  static const int CIA1_CIA1_TAHI_ADDR = 0xDC05;
  static const int CIA1_CIA1_TBLO_ADDR = 0xDC06;
  static const int CIA1_CIA1_TBHI_ADDR = 0xDC07;
  static const int CIA1_CIA1_TODTEN_ADDR = 0xDC08;
  static const int CIA1_CIA1_TODSEC_ADDR = 0xDC09;
  static const int CIA1_CIA1_TODMIN_ADDR = 0xDC0A;
  static const int CIA1_CIA1_TODHR_ADDR = 0xDC0B;
  static const int CIA1_CIA1_SDR_ADDR = 0xDC0C;
  static const int CIA1_CIA1_ICR_ADDR = 0xDC0D;
  static const int CIA1_CIA1_CRA_ADDR = 0xDC0E;
  static const int CIA1_CIA1_CRB_ADDR = 0xDC0F;
  // Complex Interface Adapter 2 (6526)
  static const int CIA2_BASE = ;
  static const int CIA2_CIA2_PRA_ADDR = 0xDD00;
  static const int CIA2_CIA2_PRB_ADDR = 0xDD01;
  static const int CIA2_CIA2_DDRA_ADDR = 0xDD02;
  static const int CIA2_CIA2_DDRB_ADDR = 0xDD03;
  static const int CIA2_CIA2_TALO_ADDR = 0xDD04;
  static const int CIA2_CIA2_TAHI_ADDR = 0xDD05;
  static const int CIA2_CIA2_TBLO_ADDR = 0xDD06;
  static const int CIA2_CIA2_TBHI_ADDR = 0xDD07;
  static const int CIA2_CIA2_TODTEN_ADDR = 0xDD08;
  static const int CIA2_CIA2_TODSEC_ADDR = 0xDD09;
  static const int CIA2_CIA2_TODMIN_ADDR = 0xDD0A;
  static const int CIA2_CIA2_TODHR_ADDR = 0xDD0B;
  static const int CIA2_CIA2_SDR_ADDR = 0xDD0C;
  static const int CIA2_CIA2_ICR_ADDR = 0xDD0D;
  static const int CIA2_CIA2_CRA_ADDR = 0xDD0E;
  static const int CIA2_CIA2_CRB_ADDR = 0xDD0F;

  // 中断向量定义
  static const int INT_IRQ = 65532;  // Maskable Interrupt
  static const int INT_NMI = 65534;  // Non-Maskable Interrupt
  static const int INT_RESET = 65526;  // Reset Vector

}
