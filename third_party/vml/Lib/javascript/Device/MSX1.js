/**
 * MSX1 寄存器定义
 * 生成自: Various (ASCII/Awanaga/MSX Association)/MSX/MSX1
 * 版本: 1.0
 */
export const msx1 = {
  // CPU: Z80A, 8位, 3579545 Hz

  // 寄存器定义
  // Accumulator
  A: 0x00,
  // Flags
  F: 0x01,
  F_C: 0,  // Carry
  F_N: 1,  // Subtract
  F_PV: 2,  // Parity/Overflow
  F_H: 4,  // Half Carry
  F_Z: 6,  // Zero
  F_S: 7,  // Sign
  // B Register
  B: 0x02,
  // C Register
  C: 0x03,
  // D Register
  D: 0x04,
  // E Register
  E: 0x05,
  // H Register
  H: 0x06,
  // L Register
  L: 0x07,
  // Alternate AF
  AF': 0x08,
  // Alternate BC
  BC': 0x0A,
  // Alternate DE
  DE': 0x0C,
  // Alternate HL
  HL': 0x0E,
  // Interrupt Vector
  I: 0x10,
  // Refresh
  R: 0x11,
  // Index X
  IX: 0x12,
  // Index Y (usually = 0xF38F)
  IY: 0x14,
  // Stack Pointer
  SP: 0x16,
  // Program Counter
  PC: 0x18,

  // 内存段
  // Cartridge/SUB-ROM / Main-ROM
  slot0_rom_START: 0x0000,
  slot0_rom_END: 0x7FFF,
  slot0_rom_SIZE: 32768,
  // MSX-BIOS ROM
  sysrom_START: 0x0000,
  sysrom_END: 0x3FFF,
  sysrom_SIZE: 16384,
  // Extension ROM (cartridge)
  extrom_START: 0x4000,
  extrom_END: 0x7FFF,
  extrom_SIZE: 16384,
  // Main RAM (32KB working area)
  main_ram_START: 0x4000,
  main_ram_END: 0xC000,
  main_ram_SIZE: 32768,
  // Work RAM (16KB)
  work_ram_START: 0xC000,
  work_ram_END: 0xFFFF,
  work_ram_SIZE: 16384,
  // System variables area
  sysvar_START: 0xF000,
  sysvar_END: 0xFCA0,
  sysvar_SIZE: 3232,
  // Slot-mapped memory
  slots_START: 0x8000,
  slots_END: 0xFFFF,
  slots_SIZE: 32768,

  // 外设定义
  // TMS9918A Video Display Processor
  VDP_BASE: 0x98,
  VDP_VDP_REG0: 0x00000131,
  VDP_VDP_REG1: 0x00000131,
  VDP_VDP_REG2: 0x00000131,
  VDP_VDP_REG3: 0x00000131,
  VDP_VDP_REG4: 0x00000131,
  VDP_VDP_REG5: 0x00000131,
  VDP_VDP_REG6: 0x00000131,
  VDP_VDP_REG7: 0x00000131,
  VDP_VDP_STATUS: 0x00000131,
  VDP_VDP_DATA: 0x00000130,
  VDP_VDP_POT: 0x00000130,
  // AY-3-8910 Programmable Sound Generator
  PSG_BASE: 0xA0,
  PSG_PSG_REG: 0x00000141,
  PSG_PSG_DATA: 0x00000143,
  PSG_FREQ_A_LO: 0x00000140,
  PSG_FREQ_A_HI: 0x00000141,
  PSG_FREQ_B_LO: 0x00000142,
  PSG_FREQ_B_HI: 0x00000143,
  PSG_FREQ_C_LO: 0x00000144,
  PSG_FREQ_C_HI: 0x00000145,
  PSG_NOISE_FREQ: 0x00000146,
  PSG_ENABLE: 0x00000147,
  PSG_VOL_A: 0x00000148,
  PSG_VOL_B: 0x00000149,
  PSG_VOL_C: 0x0000014A,
  PSG_ENV_FREQ_LO: 0x0000014B,
  PSG_ENV_FREQ_HI: 0x0000014C,
  PSG_ENV_SHAPE: 0x0000014D,
  PSG_PORT_A: 0x0000014E,
  PSG_PORT_B: 0x0000014F,
  // PPI 8255 Programmable Peripheral Interface
  PPI_BASE: 0xA8,
  PPI_PPI_PA: 0x00000150,
  PPI_PPI_PB: 0x00000151,
  PPI_PPI_PC: 0x00000152,
  PPI_PPI_CTRL: 0x00000153,
  // MSX Slot Expansion System
  SLOTEXP_BASE: 0x0000,
  SLOTEXP_SLOT0: 0x0000FCC0,
  SLOTEXP_SLOT1: 0x0000FCC1,
  SLOTEXP_SLOT2: 0x0000FCC2,
  SLOTEXP_SLOT3: 0x0000FCC3,
  SLOTEXP_EXPTBL0: 0x0000FCC4,
  SLOTEXP_EXPTBL1: 0x0000FCC5,
  SLOTEXP_EXPTBL2: 0x0000FCC6,
  SLOTEXP_EXPTBL3: 0x0000FCC7,

  // 中断向量
  IRQ_RESET: 0,  // Power-on / Reset
  IRQ_NMI: 1,  // Non-Maskable Interrupt
  IRQ_INT: 2,  // VDP Vertical Interrupt (frame)

  // 引脚定义
  PIN_VCC: 1,  // +5V Power
  PIN_GND: 2,  // Ground
  PIN_CLK: 3,  // Z80 Clock (3.58MHz)
  PIN_A0-A15: 4,  // Address Bus
  PIN_D0-D7: 5,  // Data Bus
  PIN_MREQ: 6,  // Memory Request
  PIN_IORQ: 7,  // I/O Request
  PIN_RD: 8,  // Read
  PIN_WR: 9,  // Write
  PIN_INT: 10,  // Interrupt Request
  PIN_NMI: 11,  // Non-Maskable Interrupt
  PIN_RESET: 12,  // Reset
  PIN_SLTSL: 13,  // Slot select (for memory mapping)
  PIN_WAIT: 14,  // Wait (for slow I/O)

  init: function() {
    // 硬件初始化
  }
};
