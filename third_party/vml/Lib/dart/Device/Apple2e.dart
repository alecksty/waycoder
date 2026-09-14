// Apple-IIe 设备定义 - Dart 库
// 生成自: Apple Computer/Apple II/Apple-IIe
// 版本: 1.0
// 日期: 2026-04-17
// 作者: VML Team
// 描述: Apple II Enhanced - 8-bit personal computer with MOS 6502 CPU
// CPU架构: MOS-6502
// 位宽: 8位
// 时钟频率: 1021800 Hz

class Apple_IIeDevice {
  static const String deviceName = "Apple-IIe";
  static const String manufacturer = "Apple Computer";
  static const String family = "Apple II";
  static const String version = "1.0";
  static const String architecture = "MOS-6502";
  static const int bits = 8;
  static const int clockFrequency = 1021800;

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
  static const int P_B_BIT = 4;  // Break Command
  static const int P_U_BIT = 5;  // Unused
  static const int P_V_BIT = 6;  // Overflow Flag
  static const int P_N_BIT = 7;  // Negative Flag

  // 内存段定义
  static const int MAIN_RAM_START = 0x0000;
  static const int MAIN_RAM_END = 0xBFFF;
  static const int MAIN_RAM_SIZE = 49152;  // Main RAM (48KB base, up to 64KB with slot RAM)
  static const int TEXT_RAM_START = 0x0400;
  static const int TEXT_RAM_END = 0x07FF;
  static const int TEXT_RAM_SIZE = 1024;  // Text screen buffer (40x24)
  static const int HIRES_RAM_START = 0x2000;
  static const int HIRES_RAM_END = 0x5FFF;
  static const int HIRES_RAM_SIZE = 16384;  // High-resolution graphics buffer
  static const int AUX_RAM_START = 0x0400;
  static const int AUX_RAM_END = 0x09FF;
  static const int AUX_RAM_SIZE = 1536;  // 80-column text auxiliary RAM
  static const int MONITOR_ROM_START = 0xC100;
  static const int MONITOR_ROM_END = 0xCFFF;
  static const int MONITOR_ROM_SIZE = 3840;  // Monitor ROM (applesoft/Integer)
  static const int BASIC_ROM_START = 0xD000;
  static const int BASIC_ROM_END = 0xFFFF;
  static const int BASIC_ROM_SIZE = 12288;  // Applesoft BASIC ROM
  static const int SLOT_ROM_START = 0xC100;
  static const int SLOT_ROM_END = 0xC7FF;
  static const int SLOT_ROM_SIZE = 768;  // Expansion Slot ROM
  static const int MMIO_START = 0xC080;
  static const int MMIO_END = 0xC0FF;
  static const int MMIO_SIZE = 128;  // I/O Select (slot space)

  // 外设定义
  // Versatile Interface Adapter (6522)
  static const int VIA_BASE = 0xC000;
  static const int VIA_ORB_ADDR = 0xC000;
  static const int VIA_ORA_ADDR = 0xC001;
  static const int VIA_DDRB_ADDR = 0xC002;
  static const int VIA_DDRA_ADDR = 0xC003;
  static const int VIA_T1C_ADDR = 0xC004;
  static const int VIA_T1L_ADDR = 0xC006;
  static const int VIA_T2C_ADDR = 0xC008;
  static const int VIA_SR_ADDR = 0xC00A;
  static const int VIA_ACR_ADDR = 0xC00B;
  static const int VIA_PCR_ADDR = 0xC00C;
  static const int VIA_IFG_ADDR = 0xC00D;
  static const int VIA_IER_ADDR = 0xC00E;
  static const int VIA_ORA_NH_ADDR = 0xC00F;
  // Peripheral Interface Adapter (6520)
  static const int PIA_BASE = 0xC010;
  static const int PIA_PA_ADDR = 0xC010;
  static const int PIA_PB_ADDR = 0xC011;
  static const int PIA_DDRA_ADDR = 0xC012;
  static const int PIA_DDRB_ADDR = 0xC013;
  static const int PIA_CA1_ADDR = 0xC014;
  static const int PIA_CA2_ADDR = 0xC015;
  static const int PIA_CB1_ADDR = 0xC016;
  static const int PIA_CB2_ADDR = 0xC017;
  // Keyboard (via PIA)
  static const int KBD_BASE = 0xC000;
  static const int KBD_KEYDATA_ADDR = 0xC000;
  static const int KBD_KEYSTROBE_ADDR = 0xC010;
  static const int KBD_KBDCTRL_ADDR = 0xC025;
  static const int KBD_KBDERR_ADDR = 0xC026;
  // Speaker
  static const int SPEAKER_BASE = 0xC030;
  static const int SPEAKER_SPKR_ADDR = 0xC030;
  // Game I/O Port
  static const int GAME_PORT_BASE = 0xC050;
  static const int GAME_PORT_GAME_SW0_ADDR = 0xC061;
  static const int GAME_PORT_GAME_SW1_ADDR = 0xC062;
  static const int GAME_PORT_GAME_AN0_ADDR = 0xC064;
  static const int GAME_PORT_GAME_AN1_ADDR = 0xC065;
  static const int GAME_PORT_GAME_AN2_ADDR = 0xC066;
  static const int GAME_PORT_GAME_AN3_ADDR = 0xC067;
  static const int GAME_PORT_GAME_TRIG_ADDR = 0xC070;
  // Disk II Controller
  static const int DISKII_BASE = 0xC0E0;
  static const int DISKII_PHASE0_ADDR = 0xC0E0;
  static const int DISKII_PHASE1_ADDR = 0xC0E1;
  static const int DISKII_PHASE2_ADDR = 0xC0E2;
  static const int DISKII_PHASE3_ADDR = 0xC0E3;
  static const int DISKII_Q6L_ADDR = 0xC0EC;
  static const int DISKII_Q7L_ADDR = 0xC0ED;
  static const int DISKII_Q6R_ADDR = 0xC0EE;
  static const int DISKII_Q7R_ADDR = 0xC0EF;
  // Video Display Generator
  static const int VIDEO_BASE = 0xC050;
  static const int VIDEO_TXTCLR_ADDR = 0xC050;
  static const int VIDEO_MIXCLR_ADDR = 0xC051;
  static const int VIDEO_TXTPAGE2_ADDR = 0xC054;
  static const int VIDEO_TXTPAGE1_ADDR = 0xC055;
  static const int VIDEO_LORES_ADDR = 0xC056;
  static const int VIDEO_HIRES_ADDR = 0xC057;
  static const int VIDEO_DHIRESON_ADDR = 0xC05E;
  static const int VIDEO_AN0_ADDR = 0xC058;
  static const int VIDEO_AN1_ADDR = 0xC059;
  static const int VIDEO_AN2_ADDR = 0xC05A;
  static const int VIDEO_AN3_ADDR = 0xC05B;
  static const int VIDEO__80STORE_ADDR = 0xC000;
  // RAM Read/Write Control
  static const int RAMRD_BASE = 0xC080;
  static const int RAMRD_INTCXROM_ADDR = 0xCFFF;

  // 中断向量定义
  static const int INT_RESET = 0;  // Power-on Reset
  static const int INT_NMI = 1;  // Non-Maskable Interrupt (from VIA)
  static const int INT_IRQ = 2;  // IRQ from VIA/timer/slot
  static const int INT_BRK = 3;  // BRK Instruction

  // 引脚定义
  static const int PIN_VCC = 1;  // +5V Power
  static const int PIN_GND = 2;  // Ground
  static const int PIN_RESET = 3;  // System Reset
  static const int PIN_CLK = 4;  // System Clock (1.023MHz NTSC)
  static const int PIN_RDY = 5;  // CPU Ready
  static const int PIN_NMI = 6;  // Non-Maskable Interrupt
  static const int PIN_IRQ = 7;  // Interrupt Request
  static const int PIN_SO = 8;  // Set Overflow
  static const int PIN_RWB = 9;  // Read/Write Bar
  static const int PIN_SYNC = 10;  // Instruction Sync
  static const int PIN_A0_A15 = 11;  // Address Bus (16-bit)
  static const int PIN_D0_D7 = 12;  // Data Bus (8-bit)
  static const int PIN_PHASE0 = 13;  // Phase 0 (4MHz system)
  static const int PIN_PHASE1 = 14;  // Phase 1
  static const int PIN_PHASE2 = 15;  // Phase 2
  static const int PIN_PHASE3 = 16;  // Phase 3

}
