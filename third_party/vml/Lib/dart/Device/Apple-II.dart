// Apple-II 设备定义 - Dart 库
// 生成自: Apple Computer/Apple II/Apple-II
// 版本: 1.0
// 日期: 2026-04-17
// 作者: VML Team
// 描述: Apple II personal computer with MOS 6502 CPU, 48KB RAM, and color graphics
// CPU架构: MOS 6502
// 位宽: 8位
// 时钟频率: 1023000 Hz

class Apple_IIDevice {
  static const String deviceName = "Apple-II";
  static const String manufacturer = "Apple Computer";
  static const String family = "Apple II";
  static const String version = "1.0";
  static const String architecture = "MOS 6502";
  static const int bits = 8;
  static const int clockFrequency = 1023000;

  // 寄存器地址定义
  static const int A_ADDR = 0;  // Accumulator
  static const int X_ADDR = 0;  // Index Register X
  static const int Y_ADDR = 0;  // Index Register Y
  static const int SP_ADDR = 0;  // Stack Pointer
  static const int PC_ADDR = 0;  // Program Counter
  static const int P_ADDR = 0;  // Status Register

  // 外设定义
  // Apple II keyboard
  static const int KEYBOARD_BASE = ;
  static const int KEYBOARD_KBD_ADDR = 0xC000;
  static const int KEYBOARD_KBDSTRB_ADDR = 0xC010;
  // Built-in speaker
  static const int SPEAKER_BASE = ;
  static const int SPEAKER_SPKR_ADDR = 0xC030;
  // Cassette tape interface
  static const int CASSETTE_BASE = ;
  static const int CASSETTE_TAPEIN_ADDR = 0xC060;
  static const int CASSETTE_TAPEOUT_ADDR = 0xC020;
  // Game controller port
  static const int GAMEPORT_BASE = ;
  static const int GAMEPORT_PADDLE0_ADDR = 0xC064;
  static const int GAMEPORT_PADDLE1_ADDR = 0xC065;
  static const int GAMEPORT_PADDLE2_ADDR = 0xC066;
  static const int GAMEPORT_PADDLE3_ADDR = 0xC067;
  static const int GAMEPORT_BUTTON0_ADDR = 0xC061;
  static const int GAMEPORT_BUTTON1_ADDR = 0xC062;
  // Disk II controller
  static const int DISKCONTROLLER_BASE = ;
  static const int DISKCONTROLLER_DISKUNIT_ADDR = 0xC0E0;
  static const int DISKCONTROLLER_DISKCMD_ADDR = 0xC0E8;
  static const int DISKCONTROLLER_DISKSTAT_ADDR = 0xC0E9;
  static const int DISKCONTROLLER_DISKDATA_ADDR = 0xC0EA;

  // 中断向量定义
  static const int INT_NMI = 65526;  // Non-maskable interrupt
  static const int INT_RESET = 65528;  // Reset vector
  static const int INT_IRQ = 65530;  // Interrupt request
  static const int INT_BRK = 65532;  // Break instruction

}
