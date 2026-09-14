// Commodore-64 设备定义 - Dart 库
// 生成自: Commodore/C64/Commodore-64
// 版本: 1.0
// 日期: 2026-04-17
// 作者: VML Team
// 描述: Commodore 64 - Best-selling 8-bit home computer with MOS 6510 CPU, VIC-II graphics, and SID audio
// CPU架构: MOS-6510
// 位宽: 8位
// 时钟频率: 1022727 Hz

class Commodore_64Device {
  static const String deviceName = "Commodore-64";
  static const String manufacturer = "Commodore";
  static const String family = "C64";
  static const String version = "1.0";
  static const String architecture = "MOS-6510";
  static const int bits = 8;
  static const int clockFrequency = 1022727;

  // 寄存器地址定义
  static const int A_ADDR = 0x00;  // Accumulator
  static const int X_ADDR = 0x01;  // X Index Register
  static const int Y_ADDR = 0x02;  // Y Index Register
  static const int SP_ADDR = 0x03;  // Stack Pointer
  static const int PC_ADDR = 0x04;  // Program Counter
  static const int P_ADDR = 0x06;  // Processor Status
  static const int P_C_BIT = 0;  // Carry Flag
  static const int P_Z_BIT = 1;  // Zero Flag
  static const int P_I_BIT = 2;  // Interrupt Disable
  static const int P_D_BIT = 3;  // Decimal Mode
  static const int P_B_BIT = 4;  // Break Flag
  static const int P_U_BIT = 5;  // Unused
  static const int P_V_BIT = 6;  // Overflow Flag
  static const int P_N_BIT = 7;  // Negative Flag
  static const int PORT_ADDR = 0x00;  // I/O Port (6510 only: DDR + data)

  // 内存段定义
  static const int RAM_START = 0x0000;
  static const int RAM_END = 0xFFFF;
  static const int RAM_SIZE = 65536;  // 64KB main RAM
  static const int BASIC_ROM_START = 0xA000;
  static const int BASIC_ROM_END = 0xBFFF;
  static const int BASIC_ROM_SIZE = 8192;  // BASIC interpreter ROM
  static const int KERNAL_ROM_START = 0xE000;
  static const int KERNAL_ROM_END = 0xFFFF;
  static const int KERNAL_ROM_SIZE = 8192;  // KERNAL operating system ROM
  static const int CHAR_ROM_START = 0xD000;
  static const int CHAR_ROM_END = 0xDFFF;
  static const int CHAR_ROM_SIZE = 4096;  // Character generator ROM
  static const int IO_RAM_START = 0xD000;
  static const int IO_RAM_END = 0xDFFF;
  static const int IO_RAM_SIZE = 4096;  // I/O + RAM window (switchable)

  // 外设定义
  // Video Interface Chip II - 6567/6569
  static const int VICII_BASE = 0xD000;
  static const int VICII_SP0X_ADDR = 0xD000;
  static const int VICII_SP0Y_ADDR = 0xD001;
  static const int VICII_SP1X_ADDR = 0xD002;
  static const int VICII_SP1Y_ADDR = 0xD003;
  static const int VICII_SP2X_ADDR = 0xD004;
  static const int VICII_SP2Y_ADDR = 0xD005;
  static const int VICII_SP3X_ADDR = 0xD006;
  static const int VICII_SP3Y_ADDR = 0xD007;
  static const int VICII_SP4X_ADDR = 0xD008;
  static const int VICII_SP4Y_ADDR = 0xD009;
  static const int VICII_SP5X_ADDR = 0xD00A;
  static const int VICII_SP5Y_ADDR = 0xD00B;
  static const int VICII_SP6X_ADDR = 0xD00C;
  static const int VICII_SP6Y_ADDR = 0xD00D;
  static const int VICII_SP7X_ADDR = 0xD00E;
  static const int VICII_SP7Y_ADDR = 0xD00F;
  static const int VICII_MSIGX_ADDR = 0xD010;
  static const int VICII_SCROLY_ADDR = 0xD011;
  static const int VICII_SCROLX_ADDR = 0xD016;
  static const int VICII_YPSTOP_ADDR = 0xD012;
  static const int VICII_LPX_ADDR = 0xD013;
  static const int VICII_LPY_ADDR = 0xD014;
  static const int VICII_SPENA_ADDR = 0xD015;
  static const int VICII_CSPMC_ADDR = 0xD017;
  static const int VICII_MM0_ADDR = 0xD018;
  static const int VICII_VM01_ADDR = 0xD016;
  static const int VICII_VICBAS_ADDR = 0xD018;
  static const int VICII_IRQMASK_ADDR = 0xD019;
  static const int VICII_IRQST_ADDR = 0xD01A;
  static const int VICII_SPBGPR_ADDR = 0xD01B;
  static const int VICII_SPMC_ADDR = 0xD01C;
  static const int VICII_SP1C_ADDR = 0xD025;
  static const int VICII_SP2C_ADDR = 0xD026;
  static const int VICII_SPBC_ADDR = 0xD027;
  static const int VICII_SP1C0_ADDR = 0xD028;
  static const int VICII_SP2C0_ADDR = 0xD029;
  static const int VICII_SP3C0_ADDR = 0xD02A;
  static const int VICII_SP4C0_ADDR = 0xD02B;
  static const int VICII_SP5C0_ADDR = 0xD02C;
  static const int VICII_SP6C0_ADDR = 0xD02D;
  static const int VICII_SP7C0_ADDR = 0xD02E;
  static const int VICII_REG_FD_ADDR = 0xD01D;
  static const int VICII_BGCOL0_ADDR = 0xD021;
  static const int VICII_BGCOL1_ADDR = 0xD022;
  static const int VICII_BGCOL2_ADDR = 0xD023;
  static const int VICII_BGCOL3_ADDR = 0xD024;
  // Sound Interface Device 6581/8580
  static const int SID_BASE = 0xD400;
  static const int SID_FREQ1LO_ADDR = 0xD400;
  static const int SID_FREQ1HI_ADDR = 0xD401;
  static const int SID_PW1LO_ADDR = 0xD402;
  static const int SID_PW1HI_ADDR = 0xD403;
  static const int SID_CR1_ADDR = 0xD404;
  static const int SID_AD1_ADDR = 0xD405;
  static const int SID_SR1_ADDR = 0xD406;
  static const int SID_FREQ2LO_ADDR = 0xD407;
  static const int SID_FREQ2HI_ADDR = 0xD408;
  static const int SID_PW2LO_ADDR = 0xD409;
  static const int SID_PW2HI_ADDR = 0xD40A;
  static const int SID_CR2_ADDR = 0xD40B;
  static const int SID_AD2_ADDR = 0xD40C;
  static const int SID_SR2_ADDR = 0xD40D;
  static const int SID_FREQ3LO_ADDR = 0xD40E;
  static const int SID_FREQ3HI_ADDR = 0xD40F;
  static const int SID_PW3LO_ADDR = 0xD410;
  static const int SID_PW3HI_ADDR = 0xD411;
  static const int SID_CR3_ADDR = 0xD412;
  static const int SID_AD3_ADDR = 0xD413;
  static const int SID_SR3_ADDR = 0xD414;
  static const int SID_FCH_ADDR = 0xD415;
  static const int SID_FCL_ADDR = 0xD416;
  static const int SID_RES_FLT_ADDR = 0xD417;
  static const int SID_VOLUME_ADDR = 0xD418;
  static const int SID_POTX_ADDR = 0xD419;
  static const int SID_POTY_ADDR = 0xD41A;
  static const int SID_OSC3_ADDR = 0xD41B;
  static const int SID_ENV3_ADDR = 0xD41C;
  // Complex Interface Adapter 1 - Keyboard/Serial
  static const int CIA1_BASE = 0xDC00;
  static const int CIA1_PRA_ADDR = 0xDC00;
  static const int CIA1_PRB_ADDR = 0xDC01;
  static const int CIA1_DDRA_ADDR = 0xDC02;
  static const int CIA1_DDRB_ADDR = 0xDC03;
  static const int CIA1_TA_LO_ADDR = 0xDC04;
  static const int CIA1_TA_HI_ADDR = 0xDC05;
  static const int CIA1_TB_LO_ADDR = 0xDC06;
  static const int CIA1_TB_HI_ADDR = 0xDC07;
  static const int CIA1_TOD_TENTH_ADDR = 0xDC08;
  static const int CIA1_TOD_SEC_ADDR = 0xDC09;
  static const int CIA1_TOD_MIN_ADDR = 0xDC0A;
  static const int CIA1_TOD_HR_ADDR = 0xDC0B;
  static const int CIA1_SDR_ADDR = 0xDC0C;
  static const int CIA1_ICR_ADDR = 0xDC0D;
  static const int CIA1_CRA_ADDR = 0xDC0E;
  static const int CIA1_CRB_ADDR = 0xDC0F;
  // Complex Interface Adapter 2 - Serial/Bus
  static const int CIA2_BASE = 0xDD00;
  static const int CIA2_PRA_ADDR = 0xDD00;
  static const int CIA2_PRB_ADDR = 0xDD01;
  static const int CIA2_DDRA_ADDR = 0xDD02;
  static const int CIA2_DDRB_ADDR = 0xDD03;
  static const int CIA2_TA_LO_ADDR = 0xDD04;
  static const int CIA2_TA_HI_ADDR = 0xDD05;
  static const int CIA2_TB_LO_ADDR = 0xDD06;
  static const int CIA2_TB_HI_ADDR = 0xDD07;
  static const int CIA2_TOD_TENTH_ADDR = 0xDD08;
  static const int CIA2_TOD_SEC_ADDR = 0xDD09;
  static const int CIA2_TOD_MIN_ADDR = 0xDD0A;
  static const int CIA2_TOD_HR_ADDR = 0xDD0B;
  static const int CIA2_SDR_ADDR = 0xDD0C;
  static const int CIA2_ICR_ADDR = 0xDD0D;
  static const int CIA2_CRA_ADDR = 0xDD0E;
  static const int CIA2_CRB_ADDR = 0xDD0F;
  // Color RAM (4-bit per char cell)
  static const int COLORRAM_BASE = 0xD800;
  static const int COLORRAM_COLOR_ADDR = 0xD800;
  // IEC Serial Bus (via CIA1)
  static const int IEC_BASE = 0xDC00;
  static const int IEC_IEC_DATA_ADDR = 0xDC00;
  static const int IEC_IEC_CLOCK_ADDR = 0xDC01;

  // 中断向量定义
  static const int INT_RESET = 0;  // Power-on / Reset
  static const int INT_NMI = 1;  // Non-Maskable Interrupt
  static const int INT_IRQ = 2;  // IRQ (VIC raster / CIA timer)

  // 引脚定义
  static const int PIN_VCC = 1;  // +5V Power
  static const int PIN_GND = 2;  // Ground
  static const int PIN_RESET = 3;  // System Reset
  static const int PIN_CLK = 4;  // System Clock (~1MHz)
  static const int PIN_DOTCLK = 5;  // VIC Dot Clock (8MHz NTSC / 7.8MHz PAL)
  static const int PIN_AEC = 6;  // Address Enable Control (VIC steals cycles)
  static const int PIN_BA = 7;  // Bus Available (from VIC)
  static const int PIN_IRQ = 8;  // Interrupt Request
  static const int PIN_NMI = 9;  // Non-Maskable Interrupt
  static const int PIN_RWB = 10;  // Read/Write
  static const int PIN_A0_A15 = 11;  // Address Bus
  static const int PIN_D0_D7 = 12;  // Data Bus

}
