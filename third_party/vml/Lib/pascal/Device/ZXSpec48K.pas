unit zx_spectrum_48k;

interface

// ZX-Spectrum-48K寄存器定义
// 生成自: Sinclair Research/ZX Spectrum/ZX-Spectrum-48K
// 版本: 1.0
// 日期: 2026-04-17
// 作者: VML Team
// 描述: Sinclair ZX Spectrum 48K - Iconic British 8-bit home computer with Z80A CPU and ULA graphics

// CPU架构: Z80A
// 位宽: 8位
// 时钟频率: 3500000 Hz

const

  // 寄存器定义
  // Accumulator
  A = 0x00;

  // Flags Register
  F = 0x01;
  F_C = 0;  // Carry
  F_N = 1;  // Add/Subtract
  F_PV = 2;  // Parity/Overflow
  F_H = 4;  // Half Carry
  F_Z = 6;  // Zero
  F_S = 7;  // Sign

  // B Register
  B = 0x02;

  // C Register
  C = 0x03;

  // D Register
  D = 0x04;

  // E Register
  E = 0x05;

  // H Register
  H = 0x06;

  // L Register
  L = 0x07;

  // Alternate AF
  AF = 0x08;

  // Alternate BC
  BC = 0x0A;

  // Alternate DE
  DE = 0x0C;

  // Alternate HL
  HL = 0x0E;

  // Interrupt Vector Register
  I = 0x10;

  // Refresh Counter
  R = 0x11;

  // Index X
  IX = 0x12;

  // Index Y
  IY = 0x14;

  // Stack Pointer
  SP = 0x16;

  // Program Counter
  PC = 0x18;

  // 内存段定义
  // 48KB ZX Spectrum ROM (BASIC + monitor)
  ROM_START = 0x0000;
  ROM_END = 0x3FFF;
  ROM_SIZE = 16384;

  // Display file (256x192 bitmap)
  VIDEO_RAM_START = 0x4000;
  VIDEO_RAM_END = 0x57FF;
  VIDEO_RAM_SIZE = 6144;

  // Attribute file (32x24 color cells)
  ATTR_RAM_START = 0x5800;
  ATTR_RAM_END = 0x5AFF;
  ATTR_RAM_SIZE = 768;

  // User RAM (40KB)
  USER_RAM_START = 0x5B00;
  USER_RAM_END = 0xFFFF;
  USER_RAM_SIZE = 40960;

  // 外设定义
  // Uncommitted Logic Array - Sinclair custom IC
  ULA_BASE = 0xFE;
  ULA_BORDER = 0xFE;
  ULA_KBD_ROW0 = 0xFE;
  ULA_KBD_ROW1 = 0xFE;
  ULA_KBD_ROW2 = 0xFE;
  ULA_KBD_ROW3 = 0xFE;
  ULA_KBD_ROW4 = 0xFE;
  ULA_KBD_ROW5 = 0xFE;
  ULA_KBD_ROW6 = 0xFE;
  ULA_KBD_ROW7 = 0xFE;
  ULA_KBD_ROW8 = 0xFE;

  // Keyboard Matrix (40 keys, 8 rows x 5 cols)
  KEYBOARD_BASE = 0xFE;
  KEYBOARD_KBD_IN = 0xFE;

  // Internal Beeper
  BEEPER_BASE = 0xFE;
  BEEPER_BEEP = 0xFE;

  // Tape Interface
  TAPE_BASE = 0xFE;
  TAPE_EAR_IN = 0xFE;
  TAPE_MIC_OUT = 0xFE;

  // Kempston Joystick Interface
  JOYSTICK_BASE = 0xF7FE;
  JOYSTICK_KEMPSTON = 0xF7FE;

  // 中断向量定义
  RESET_VECTOR = 0;  // Power-on / Reset
  NMI_VECTOR = 1;  // Non-Maskable Interrupt (BREAK key)
  INT_VECTOR = 2;  // Maskable Interrupt (ULA vertical blank, 50Hz)

  // 引脚定义
  PIN_VCC = 1;  // +5V Power
  PIN_GND = 2;  // Ground
  PIN_CLK = 3;  // Z80 Clock (3.5MHz)
  PIN_M1 = 4;  // Machine Cycle 1
  PIN_MREQ = 5;  // Memory Request
  PIN_IORQ = 6;  // I/O Request
  PIN_RD = 7;  // Read
  PIN_WR = 8;  // Write
  PIN_HALT = 9;  // Halt State
  PIN_BUSAK = 10;  // Bus Acknowledge
  PIN_WAIT = 11;  // Wait State (ULA inserts)
  PIN_INT = 12;  // Interrupt Request
  PIN_NMI = 13;  // Non-Maskable Interrupt
  PIN_RESET = 14;  // Reset
  PIN_A0_A15 = 15;  // Address Bus (16-bit)
  PIN_D0_D7 = 16;  // Data Bus (8-bit)

type
  TZX-Spectrum-48K = record
    // 设备状态记录
  end;

// 设备初始化函数
procedure zx_spectrum_48k_init;

// 常用函数
function read_register(addr: Word): Byte;
procedure write_register(addr: Word; value: Byte);

implementation

procedure zx_spectrum_48k_init;
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
