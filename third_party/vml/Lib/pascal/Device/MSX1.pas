unit msx1;

interface

// MSX1寄存器定义
// 生成自: Various (ASCII/Awanaga/MSX Association)/MSX/MSX1
// 版本: 1.0
// 日期: 2026-04-17
// 作者: VML Team
// 描述: MSX - Standardized 8-bit home computer with Z80A CPU, TMS9918A graphics, and AY-3-8910 audio

// CPU架构: Z80A
// 位宽: 8位
// 时钟频率: 3579545 Hz

const

  // 寄存器定义
  // Accumulator
  A = 0x00;

  // Flags
  F = 0x01;
  F_C = 0;  // Carry
  F_N = 1;  // Subtract
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

  // Interrupt Vector
  I = 0x10;

  // Refresh
  R = 0x11;

  // Index X
  IX = 0x12;

  // Index Y (usually = 0xF38F)
  IY = 0x14;

  // Stack Pointer
  SP = 0x16;

  // Program Counter
  PC = 0x18;

  // 内存段定义
  // Cartridge/SUB-ROM / Main-ROM
  SLOT0_ROM_START = 0x0000;
  SLOT0_ROM_END = 0x7FFF;
  SLOT0_ROM_SIZE = 32768;

  // MSX-BIOS ROM
  SYSROM_START = 0x0000;
  SYSROM_END = 0x3FFF;
  SYSROM_SIZE = 16384;

  // Extension ROM (cartridge)
  EXTROM_START = 0x4000;
  EXTROM_END = 0x7FFF;
  EXTROM_SIZE = 16384;

  // Main RAM (32KB working area)
  MAIN_RAM_START = 0x4000;
  MAIN_RAM_END = 0xC000;
  MAIN_RAM_SIZE = 32768;

  // Work RAM (16KB)
  WORK_RAM_START = 0xC000;
  WORK_RAM_END = 0xFFFF;
  WORK_RAM_SIZE = 16384;

  // System variables area
  SYSVAR_START = 0xF000;
  SYSVAR_END = 0xFCA0;
  SYSVAR_SIZE = 3232;

  // Slot-mapped memory
  SLOTS_START = 0x8000;
  SLOTS_END = 0xFFFF;
  SLOTS_SIZE = 32768;

  // 外设定义
  // TMS9918A Video Display Processor
  VDP_BASE = 0x98;
  VDP_VDP_REG0 = 0x99;
  VDP_VDP_REG1 = 0x99;
  VDP_VDP_REG2 = 0x99;
  VDP_VDP_REG3 = 0x99;
  VDP_VDP_REG4 = 0x99;
  VDP_VDP_REG5 = 0x99;
  VDP_VDP_REG6 = 0x99;
  VDP_VDP_REG7 = 0x99;
  VDP_VDP_STATUS = 0x99;
  VDP_VDP_DATA = 0x98;
  VDP_VDP_POT = 0x98;

  // AY-3-8910 Programmable Sound Generator
  PSG_BASE = 0xA0;
  PSG_PSG_REG = 0xA1;
  PSG_PSG_DATA = 0xA3;
  PSG_FREQ_A_LO = 0xA0;
  PSG_FREQ_A_HI = 0xA1;
  PSG_FREQ_B_LO = 0xA2;
  PSG_FREQ_B_HI = 0xA3;
  PSG_FREQ_C_LO = 0xA4;
  PSG_FREQ_C_HI = 0xA5;
  PSG_NOISE_FREQ = 0xA6;
  PSG_ENABLE = 0xA7;
  PSG_VOL_A = 0xA8;
  PSG_VOL_B = 0xA9;
  PSG_VOL_C = 0xAA;
  PSG_ENV_FREQ_LO = 0xAB;
  PSG_ENV_FREQ_HI = 0xAC;
  PSG_ENV_SHAPE = 0xAD;
  PSG_PORT_A = 0xAE;
  PSG_PORT_B = 0xAF;

  // PPI 8255 Programmable Peripheral Interface
  PPI_BASE = 0xA8;
  PPI_PPI_PA = 0xA8;
  PPI_PPI_PB = 0xA9;
  PPI_PPI_PC = 0xAA;
  PPI_PPI_CTRL = 0xAB;

  // MSX Slot Expansion System
  SLOTEXP_BASE = 0x0000;
  SLOTEXP_SLOT0 = 0xFCC0;
  SLOTEXP_SLOT1 = 0xFCC1;
  SLOTEXP_SLOT2 = 0xFCC2;
  SLOTEXP_SLOT3 = 0xFCC3;
  SLOTEXP_EXPTBL0 = 0xFCC4;
  SLOTEXP_EXPTBL1 = 0xFCC5;
  SLOTEXP_EXPTBL2 = 0xFCC6;
  SLOTEXP_EXPTBL3 = 0xFCC7;

  // 中断向量定义
  RESET_VECTOR = 0;  // Power-on / Reset
  NMI_VECTOR = 1;  // Non-Maskable Interrupt
  INT_VECTOR = 2;  // VDP Vertical Interrupt (frame)

  // 引脚定义
  PIN_VCC = 1;  // +5V Power
  PIN_GND = 2;  // Ground
  PIN_CLK = 3;  // Z80 Clock (3.58MHz)
  PIN_A0_A15 = 4;  // Address Bus
  PIN_D0_D7 = 5;  // Data Bus
  PIN_MREQ = 6;  // Memory Request
  PIN_IORQ = 7;  // I/O Request
  PIN_RD = 8;  // Read
  PIN_WR = 9;  // Write
  PIN_INT = 10;  // Interrupt Request
  PIN_NMI = 11;  // Non-Maskable Interrupt
  PIN_RESET = 12;  // Reset
  PIN_SLTSL = 13;  // Slot select (for memory mapping)
  PIN_WAIT = 14;  // Wait (for slow I/O)

type
  TMSX1 = record
    // 设备状态记录
  end;

// 设备初始化函数
procedure msx1_init;

// 常用函数
function read_register(addr: Word): Byte;
procedure write_register(addr: Word; value: Byte);

implementation

procedure msx1_init;
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
