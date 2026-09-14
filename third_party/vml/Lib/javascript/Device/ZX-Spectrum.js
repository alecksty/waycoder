/**
 * ZX-Spectrum 寄存器定义
 * 生成自: Sinclair Research/ZX Spectrum/ZX-Spectrum
 * 版本: 1.0
 */
export const zx_spectrum = {
  // CPU: Zilog Z80, 8位, 3500000 Hz

  // 寄存器定义
  // Accumulator
  A: 0,
  // Flags
  F: 0,
  // B
  B: 0,
  // C
  C: 0,
  // D
  D: 0,
  // E
  E: 0,
  // H
  H: 0,
  // L
  L: 0,
  // Index Register X
  IX: 0,
  // Index Register Y
  IY: 0,
  // Stack Pointer
  SP: 0,
  // Program Counter
  PC: 0,
  // Interrupt Vector
  I: 0,
  // Memory Refresh
  R: 0,
  // Alternate AF
  AF': 0,
  // Alternate BC
  BC': 0,
  // Alternate DE
  DE': 0,
  // Alternate HL
  HL': 0,

  // 外设定义
  // Uncommitted Logic Array (video and I/O)
  ULA_BASE: ,
  ULA_ULA_PORT_FE: 0x000000FE,
  ULA_ULA_BORDER: 0x000000FE,
  ULA_ULA_BEEPER: 0x000000FE,
  ULA_ULA_MIC: 0x000000FE,
  // General Instruments AY-3-8912 sound chip
  AY-3-8912_BASE: ,
  AY-3-8912_AY_REG_SEL: 0x0000FFFD,
  AY-3-8912_AY_DATA: 0x0000BFFD,
  AY-3-8912_AY_READ: 0x0000FFFD,
  // 40-key rubber keyboard
  Keyboard_BASE: ,
  Keyboard_KEY_ROW0: 0x0000FEFE,
  Keyboard_KEY_ROW1: 0x0000FDFE,
  Keyboard_KEY_ROW2: 0x0000FBFE,
  Keyboard_KEY_ROW3: 0x0000F7FE,
  Keyboard_KEY_ROW4: 0x0000EFFE,
  Keyboard_KEY_ROW5: 0x0000DFFE,
  Keyboard_KEY_ROW6: 0x0000BFFE,
  Keyboard_KEY_ROW7: 0x00007FFE,
  // Kempston joystick interface
  Kempston_BASE: ,
  Kempston_KEMPSTON_JOY: 0x0000001F,
  // ZX Interface 1 (RS-232 and Microdrive)
  Interface1_BASE: ,
  Interface1_IF1_STATUS: 0x00001FFD,
  Interface1_IF1_DATA: 0x00003FFD,
  // ZX Interface 2 (joystick and ROM cartridge)
  Interface2_BASE: ,
  Interface2_IF2_JOY1: 0x0000001F,
  Interface2_IF2_JOY2: 0x00000037,

  // 中断向量
  IRQ_IM1: 56,  // Interrupt Mode 1
  IRQ_RST_00: 0,  // Restart 00h
  IRQ_RST_08: 8,  // Restart 08h
  IRQ_RST_10: 16,  // Restart 10h
  IRQ_RST_18: 24,  // Restart 18h
  IRQ_RST_20: 32,  // Restart 20h
  IRQ_RST_28: 40,  // Restart 28h
  IRQ_RST_30: 48,  // Restart 30h
  IRQ_RST_38: 56,  // Restart 38h

  init: function() {
    // 硬件初始化
  }
};
