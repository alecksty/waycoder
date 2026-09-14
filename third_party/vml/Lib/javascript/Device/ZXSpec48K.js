/**
 * ZX-Spectrum-48K 寄存器定义
 * 生成自: Sinclair Research/ZX Spectrum/ZX-Spectrum-48K
 * 版本: 1.0
 */
export const zx_spectrum_48k = {
  // CPU: Z80A, 8位, 3500000 Hz

  // 寄存器定义
  // Accumulator
  A: 0x00,
  // Flags Register
  F: 0x01,
  F_C: 0,  // Carry
  F_N: 1,  // Add/Subtract
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
  // Interrupt Vector Register
  I: 0x10,
  // Refresh Counter
  R: 0x11,
  // Index X
  IX: 0x12,
  // Index Y
  IY: 0x14,
  // Stack Pointer
  SP: 0x16,
  // Program Counter
  PC: 0x18,

  // 内存段
  // 48KB ZX Spectrum ROM (BASIC + monitor)
  rom_START: 0x0000,
  rom_END: 0x3FFF,
  rom_SIZE: 16384,
  // Display file (256x192 bitmap)
  video_ram_START: 0x4000,
  video_ram_END: 0x57FF,
  video_ram_SIZE: 6144,
  // Attribute file (32x24 color cells)
  attr_ram_START: 0x5800,
  attr_ram_END: 0x5AFF,
  attr_ram_SIZE: 768,
  // User RAM (40KB)
  user_ram_START: 0x5B00,
  user_ram_END: 0xFFFF,
  user_ram_SIZE: 40960,

  // 外设定义
  // Uncommitted Logic Array - Sinclair custom IC
  ULA_BASE: 0xFE,
  ULA_BORDER: 0x000001FC,
  ULA_KBD_ROW0: 0x000001FC,
  ULA_KBD_ROW1: 0x000001FC,
  ULA_KBD_ROW2: 0x000001FC,
  ULA_KBD_ROW3: 0x000001FC,
  ULA_KBD_ROW4: 0x000001FC,
  ULA_KBD_ROW5: 0x000001FC,
  ULA_KBD_ROW6: 0x000001FC,
  ULA_KBD_ROW7: 0x000001FC,
  ULA_KBD_ROW8: 0x000001FC,
  // Keyboard Matrix (40 keys, 8 rows x 5 cols)
  KEYBOARD_BASE: 0xFE,
  KEYBOARD_KBD_IN: 0x000001FC,
  // Internal Beeper
  BEEPER_BASE: 0xFE,
  BEEPER_BEEP: 0x000001FC,
  // Tape Interface
  TAPE_BASE: 0xFE,
  TAPE_EAR_IN: 0x000001FC,
  TAPE_MIC_OUT: 0x000001FC,
  // Kempston Joystick Interface
  JOYSTICK_BASE: 0xF7FE,
  JOYSTICK_KEMPSTON: 0x0001EFFC,

  // 中断向量
  IRQ_RESET: 0,  // Power-on / Reset
  IRQ_NMI: 1,  // Non-Maskable Interrupt (BREAK key)
  IRQ_INT: 2,  // Maskable Interrupt (ULA vertical blank, 50Hz)

  // 引脚定义
  PIN_VCC: 1,  // +5V Power
  PIN_GND: 2,  // Ground
  PIN_CLK: 3,  // Z80 Clock (3.5MHz)
  PIN_M1: 4,  // Machine Cycle 1
  PIN_MREQ: 5,  // Memory Request
  PIN_IORQ: 6,  // I/O Request
  PIN_RD: 7,  // Read
  PIN_WR: 8,  // Write
  PIN_HALT: 9,  // Halt State
  PIN_BUSAK: 10,  // Bus Acknowledge
  PIN_WAIT: 11,  // Wait State (ULA inserts)
  PIN_INT: 12,  // Interrupt Request
  PIN_NMI: 13,  // Non-Maskable Interrupt
  PIN_RESET: 14,  // Reset
  PIN_A0-A15: 15,  // Address Bus (16-bit)
  PIN_D0-D7: 16,  // Data Bus (8-bit)

  init: function() {
    // 硬件初始化
  }
};
