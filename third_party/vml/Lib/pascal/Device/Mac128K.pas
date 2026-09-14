unit macintosh_128k;

interface

// Macintosh-128K寄存器定义
// 生成自: Apple Computer/Macintosh/Macintosh-128K
// 版本: 1.0
// 日期: 2026-04-17
// 作者: VML Team
// 描述: Apple Macintosh 128K - First Macintosh - Motorola 68000, 128KB RAM, 512x342 display

// CPU架构: MC68000
// 位宽: 32位
// 时钟频率: 7833600 Hz

const

  // 寄存器定义
  // Data Register 0
  D0 = 0x00;

  // Data Register 1
  D1 = 0x04;

  // Data Register 2
  D2 = 0x08;

  // Data Register 3
  D3 = 0x0C;

  // Data Register 4
  D4 = 0x10;

  // Data Register 5
  D5 = 0x14;

  // Data Register 6
  D6 = 0x18;

  // Data Register 7
  D7 = 0x1C;

  // Address Register 0
  A0 = 0x20;

  // Address Register 1
  A1 = 0x24;

  // Address Register 2
  A2 = 0x28;

  // Address Register 3
  A3 = 0x2C;

  // Address Register 4
  A4 = 0x30;

  // Address Register 5
  A5 = 0x34;

  // Address Register 6
  A6 = 0x38;

  // Stack Pointer (USP)
  A7 = 0x3C;

  // Program Counter
  PC = 0x40;

  // Status Register
  SR = 0x44;
  SR_C = 0;  // Carry
  SR_V = 1;  // Overflow
  SR_Z = 2;  // Zero
  SR_N = 3;  // Negative
  SR_X = 4;  // Extend
  SR_I0 = 8;  // Interrupt Mask 0
  SR_I1 = 9;  // Interrupt Mask 1
  SR_I2 = 10;  // Interrupt Mask 2
  SR_S = 13;  // Supervisor/User
  SR_T0 = 14;  // Trace Mode 0
  SR_T1 = 15;  // Trace Mode 1

  // 内存段定义
  // Main RAM (128KB unified)
  RAM_START = 0x000000;
  RAM_END = 0x01FFFF;
  RAM_SIZE = 131072;

  // Mac ROM (128KB)
  ROM_START = 0x40000000;
  ROM_END = 0x4001FFFF;
  ROM_SIZE = 131072;

  // Screen bitmap (512x342x1 = 21792 bytes)
  FRAMEBUFFER_START = 0x00400000;
  FRAMEBUFFER_END = 0x00400555;
  FRAMEBUFFER_SIZE = 1366;

  // Shadow screen (double-buffering)
  FRAMEBUFFER2_START = 0x00410000;
  FRAMEBUFFER2_END = 0x00410555;
  FRAMEBUFFER2_SIZE = 1366;

  // VIA 6522 (I/O)
  VIA_START = 0x00E00000;
  VIA_END = 0x00E0FFFF;
  VIA_SIZE = 4096;

  // SCC 8530 (serial)
  SCC_START = 0x00F00000;
  SCC_END = 0x00F0FFFF;
  SCC_SIZE = 4096;

  // ADB bus
  ADB_START = 0x01600000;
  ADB_END = 0x0160FFFF;
  ADB_SIZE = 4096;

  // IWM floppy controller
  IWM_START = 0x01E00000;
  IWM_END = 0x01E0FFFF;
  IWM_SIZE = 4096;

  // 外设定义
  // Versatile Interface Adapter 6522
  VIA_BASE = 0xE00000;
  VIA_ORB = 0xE00000;
  VIA_ORA = 0xE00002;
  VIA_DDRB = 0xE00004;
  VIA_DDRA = 0xE00006;
  VIA_T1C_L = 0xE00008;
  VIA_T1C_H = 0xE0000A;
  VIA_T1L_L = 0xE0000C;
  VIA_T1L_H = 0xE0000E;
  VIA_T2C_L = 0xE00010;
  VIA_T2C_H = 0xE00012;
  VIA_SR = 0xE00014;
  VIA_ACR = 0xE00016;
  VIA_PCR = 0xE00018;
  VIA_IFR = 0xE0001E;
  VIA_IER = 0xE0001E;

  // SCC 8530 Serial Communications Controller
  SCC_BASE = 0xF00000;
  SCC_SCC_CHA_B = 0xF00000;
  SCC_SCC_CHA_C = 0xF00002;
  SCC_SCC_CHB_D = 0xF00004;
  SCC_SCC_CHB_CT = 0xF00006;

  // Integrated Woz Machine - Floppy Disk Controller
  IWM_BASE = 0x1E00000;
  IWM_IWM_DATA = 0x1E00000;
  IWM_IWM_MODE = 0x1E00008;
  IWM_IWM_Q6L = 0x1E00020;
  IWM_IWM_Q7L = 0x1E00022;
  IWM_IWM_Q6R = 0x1E00024;
  IWM_IWM_Q7R = 0x1E00026;

  // Video Graphics Controller (custom Apple chip)
  VGC_BASE = 0x00F20000;
  VGC_VGC_MODE = 0x00F20000;
  VGC_VGC_START_HI = 0x00F20002;
  VGC_VGC_START_LO = 0x00F20004;

  // Apple Desktop Bus
  ADB_BASE = 0x01600000;
  ADB_ADB_DATA = 0x01600000;
  ADB_ADB_STATUS = 0x01600004;
  ADB_ADB_CMD = 0x01600008;

  // 中断向量定义
  RESET_VECTOR = 1;  // Reset Initial SP
  RESET_PC_VECTOR = 2;  // Reset Initial PC
  IRQ1_VECTOR = 24;  // VIA interrupt (level 1)
  IRQ2_VECTOR = 25;  // SCC interrupt (level 2)
  IRQ3_VECTOR = 26;  // ADB / VIA (level 3)
  IRQ4_VECTOR = 27;  // ADB / VIA (level 4)

  // 引脚定义
  PIN_VCC = 1;  // +5V Power
  PIN_GND = 2;  // Ground
  PIN_CLK = 3;  // 16MHz master clock / 7.83MHz CPU clock
  PIN_FC0 = 4;  // Function Code 0
  PIN_FC1 = 5;  // Function Code 1
  PIN_FC2 = 6;  // Function Code 2
  PIN_AS = 7;  // Address Strobe
  PIN_UDS = 8;  // Upper Data Strobe
  PIN_LDS = 9;  // Lower Data Strobe
  PIN_RWB = 10;  // Read/Write
  PIN_DTACK = 11;  // Data Acknowledge
  PIN_BERR = 12;  // Bus Error
  PIN_BR = 13;  // Bus Request
  PIN_BG = 14;  // Bus Grant
  PIN_BGACK = 15;  // Bus Grant Acknowledge
  PIN_IPL0 = 16;  // Interrupt Priority 0
  PIN_IPL1 = 17;  // Interrupt Priority 1
  PIN_IPL2 = 18;  // Interrupt Priority 2
  PIN_RESET = 19;  // Reset
  PIN_HALT = 20;  // Halt
  PIN_A1_A23 = 21;  // Address Bus (24-bit)
  PIN_D0_D15 = 22;  // Data Bus (16-bit)

type
  TMacintosh-128K = record
    // 设备状态记录
  end;

// 设备初始化函数
procedure macintosh_128k_init;

// 常用函数
function read_register(addr: Word): Byte;
procedure write_register(addr: Word; value: Byte);

implementation

procedure macintosh_128k_init;
begin
  // 初始化代码
end;

function read_register(addr: Word): Byte;
begin
  // 读取寄存器值
  Result := 0;
end;

procedure write_register(addr: Word; value: Byte);
begin
  // 写入寄存器值
end;

end.
