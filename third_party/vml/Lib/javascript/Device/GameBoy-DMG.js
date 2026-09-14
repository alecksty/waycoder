/**
 * Sharp-LR35902 寄存器定义
 * 生成自: Sharp/Z80/Sharp-LR35902
 * 版本: 1.0
 */
export const sharp_lr35902 = {
  // CPU: LR35902, 8位, 4194304 Hz

  // 寄存器定义
  // Accumulator
  A: 0x00,
  // B Register
  B: 0x01,
  // C Register
  C: 0x02,
  // D Register
  D: 0x03,
  // E Register
  E: 0x04,
  // Flags Register
  F: 0x05,
  F_C: 4,  // Carry
  F_H: 5,  // Half Carry
  F_N: 6,  // Subtract
  F_Z: 7,  // Zero
  // H Register
  H: 0x06,
  // L Register
  L: 0x07,
  // AF Register Pair (Accumulator + Flags)
  AF: 0x08,
  // BC Register Pair
  BC: 0x0A,
  // DE Register Pair
  DE: 0x0C,
  // HL Register Pair
  HL: 0x0E,
  // Stack Pointer
  SP: 0x10,
  // Program Counter
  PC: 0x12,

  // 内存段
  // Work RAM (4KB)
  wram_START: 0xC000,
  wram_END: 0xCFFF,
  wram_SIZE: 4096,
  // Work RAM Shadow (Echo RAM)
  wram_shadow_START: 0xE000,
  wram_shadow_END: 0xEFFF,
  wram_shadow_SIZE: 4096,
  // High RAM (127 bytes)
  hram_START: 0xFF80,
  hram_END: 0xFFFE,
  hram_SIZE: 127,
  // I/O Registers
  io_registers_START: 0xFF00,
  io_registers_END: 0xFF7F,
  io_registers_SIZE: 128,
  // Sprite Attribute Table (OAM)
  oam_START: 0xFE00,
  oam_END: 0xFE9F,
  oam_SIZE: 160,
  // Video RAM (8KB)
  vram_START: 0x8000,
  vram_END: 0x9FFF,
  vram_SIZE: 8192,
  // Background Map 1
  bg_map_1_START: 0x9800,
  bg_map_1_END: 0x9BFF,
  bg_map_1_SIZE: 1024,
  // Background Map 2
  bg_map_2_START: 0x9C00,
  bg_map_2_END: 0x9FFF,
  bg_map_2_SIZE: 1024,
  // ROM Bank 0 (Cartridge Header)
  rom_bank0_START: 0x0000,
  rom_bank0_END: 0x3FFF,
  rom_bank0_SIZE: 16384,
  // ROM Bank 1 (Switchable)
  rom_bank1_START: 0x4000,
  rom_bank1_END: 0x7FFF,
  rom_bank1_SIZE: 16384,
  // Cartridge RAM / MBC
  cart_ram_START: 0xA000,
  cart_ram_END: 0xBFFF,
  cart_ram_SIZE: 8192,

  // 外设定义
  // LCD Controller / Picture Processing Unit
  PPU_BASE: 0xFF40,
  PPU_LCDC: 0x0001FE80,
  PPU_LCDC_BG_ENABLE: 0,  // Background Display Enable
  PPU_LCDC_SPRITE_ENABLE: 1,  // Sprite Display Enable
  PPU_LCDC_SPRITE_SIZE: 2,  // Sprite Size (0=8x8, 1=8x16)
  PPU_LCDC_BG_TILE_MAP: 3,  // BG Tile Map Area (0=9800, 1=9C00)
  PPU_LCDC_TILE_DATA: 4,  // Tile Data Area (0=8800, 1=8000)
  PPU_LCDC_WINDOW_ENABLE: 5,  // Window Display Enable
  PPU_LCDC_WINDOW_MAP: 6,  // Window Tile Map Area (0=9800, 1=9C00)
  PPU_LCDC_LCD_ENABLE: 7,  // LCD Display Enable
  PPU_STAT: 0x0001FE81,
  PPU_STAT_MODE: 0,  // LCD Mode (0=H-Blank, 1=V-Blank, 2=OAM, 3=VRAM)
  PPU_STAT_LYC_FLAG: 2,  // LY=LYC Compare Flag
  PPU_STAT_HBLANK_IRQ: 3,  // H-Blank Interrupt Enable
  PPU_STAT_VBLANK_IRQ: 4,  // V-Blank Interrupt Enable
  PPU_STAT_OAM_IRQ: 5,  // OAM Interrupt Enable
  PPU_STAT_LYC_IRQ: 6,  // LYC Interrupt Enable
  PPU_SCY: 0x0001FE82,
  PPU_SCX: 0x0001FE83,
  PPU_LY: 0x0001FE84,
  PPU_LYC: 0x0001FE85,
  PPU_DMA: 0x0001FE86,
  PPU_BGP: 0x0001FE87,
  PPU_OBP0: 0x0001FE88,
  PPU_OBP1: 0x0001FE89,
  PPU_WY: 0x0001FE8A,
  PPU_WX: 0x0001FE8B,
  // Audio Processing Unit
  apu_BASE: 0xFF10,
  apu_NR10: 0x0001FE20,
  apu_NR10_SWEEP_TIME: 0,  // Sweep Time
  apu_NR10_SWEEP_INCREASE: 3,  // Sweep Increase/Decrease
  apu_NR10_SWEEP_SHIFTS: 0,  // Sweep Number of Shifts
  apu_NR11: 0x0001FE21,
  apu_NR12: 0x0001FE22,
  apu_NR13: 0x0001FE23,
  apu_NR14: 0x0001FE24,
  apu_NR21: 0x0001FE26,
  apu_NR22: 0x0001FE27,
  apu_NR23: 0x0001FE28,
  apu_NR24: 0x0001FE29,
  apu_NR30: 0x0001FE2A,
  apu_NR31: 0x0001FE2B,
  apu_NR32: 0x0001FE2C,
  apu_NR33: 0x0001FE2D,
  apu_NR34: 0x0001FE2E,
  apu_NR41: 0x0001FE30,
  apu_NR42: 0x0001FE31,
  apu_NR43: 0x0001FE32,
  apu_NR44: 0x0001FE33,
  apu_NR50: 0x0001FE34,
  apu_NR51: 0x0001FE35,
  apu_NR52: 0x0001FE36,
  apu_NR52_CH1_ON: 0,  // Channel 1 ON
  apu_NR52_CH2_ON: 1,  // Channel 2 ON
  apu_NR52_CH3_ON: 2,  // Channel 3 ON
  apu_NR52_CH4_ON: 3,  // Channel 4 ON
  apu_NR52_ALL_ON: 7,  // All Sound ON
  // Timer Unit
  TIMER_BASE: 0xFF04,
  TIMER_DIV: 0x0001FE08,
  TIMER_TIMA: 0x0001FE09,
  TIMER_TMA: 0x0001FE0A,
  TIMER_TAC: 0x0001FE0B,
  TIMER_TAC_TIMER_ENABLE: 2,  // Timer Enable
  TIMER_TAC_CLOCK_SEL: 0,  // Clock Select (00=4kHz, 01=262kHz, 10=65kHz, 11=16kHz)
  // Joypad Controller
  JOYPAD_BASE: 0xFF00,
  JOYPAD_P1: 0x0001FE00,
  JOYPAD_P1_A_BTN: 0,  // A Button (1=Pressed when selected)
  JOYPAD_P1_B_BTN: 1,  // B Button (1=Pressed when selected)
  JOYPAD_P1_SELECT: 2,  // Select Button (1=Pressed)
  JOYPAD_P1_START: 3,  // Start Button (1=Pressed)
  JOYPAD_P1_DIR_DOWN: 4,  // Direction Down (1=Pressed when selected)
  JOYPAD_P1_DIR_UP: 5,  // Direction Up (1=Pressed when selected)
  JOYPAD_P1_DIR_LEFT: 6,  // Direction Left (1=Pressed when selected)
  JOYPAD_P1_DIR_RIGHT: 7,  // Direction Right (1=Pressed when selected)
  // Serial I/O (Link Cable)
  SERIAL_BASE: 0xFF01,
  SERIAL_SB: 0x0001FE02,
  SERIAL_SC: 0x0001FE03,
  SERIAL_SC_TRANSFER_START: 7,  // Transfer Start
  SERIAL_SC_CLOCK_SPEED: 1,  // Clock Select (0=External, 1=Internal 8192Hz)
  // Interrupt Flag Register
  INTERRUPT_BASE: 0xFF0F,
  INTERRUPT_IF: 0x0001FE1E,
  INTERRUPT_IF_VBLANK: 0,  // V-Blank Interrupt Request
  INTERRUPT_IF_LCDC: 1,  // LCDC Status Interrupt Request
  INTERRUPT_IF_TIMER: 2,  // Timer Overflow Interrupt Request
  INTERRUPT_IF_SERIAL: 3,  // Serial Transfer Complete Interrupt Request
  INTERRUPT_IF_JOYPAD: 4,  // Joypad Interrupt Request
  // Interrupt Enable Register
  IE_BASE: 0xFFFF,
  IE_IE: 0x0001FFFE,
  IE_IE_VBLANK_IE: 0,  // V-Blank Interrupt Enable
  IE_IE_LCDC_IE: 1,  // LCDC Status Interrupt Enable
  IE_IE_TIMER_IE: 2,  // Timer Interrupt Enable
  IE_IE_SERIAL_IE: 3,  // Serial Interrupt Enable
  IE_IE_JOYPAD_IE: 4,  // Joypad Interrupt Enable

  // 中断向量
  IRQ_VBLANK: 0,  // V-Blank Interrupt (LY=144, during vertical blanking)
  IRQ_LCDC_STATUS: 1,  // LCDC Status Interrupt (H-Blank/OAM/V-Count match)
  IRQ_TIMER_OVERFLOW: 2,  // Timer Overflow Interrupt (TIMA overflow)
  IRQ_SERIAL_COMPLETE: 3,  // Serial Transfer Complete Interrupt
  IRQ_JOYPAD: 4,  // Joypad Interrupt (button press/release)

  // 引脚定义
  PIN_VSS: 1,  // Ground
  PIN_VDD: 2,  // Power Supply
  PIN_PHI: 3,  // System Clock Output (4.19MHz / 2 = 2.1MHz CPU)
  PIN_RESET: 4,  // Reset Signal (active low)
  PIN_INT: 5,  // Interrupt Request
  PIN_BUSREQ: 6,  // Bus Request (external DMA access)
  PIN_A0: 7,  // Address Bus Bit 0
  PIN_A1: 8,  // Address Bus Bit 1
  PIN_A2: 9,  // Address Bus Bit 2
  PIN_A3: 10,  // Address Bus Bit 3
  PIN_A4: 11,  // Address Bus Bit 4
  PIN_A5: 12,  // Address Bus Bit 5
  PIN_A6: 13,  // Address Bus Bit 6
  PIN_A7: 14,  // Address Bus Bit 7
  PIN_A8: 15,  // Address Bus Bit 8
  PIN_A9: 16,  // Address Bus Bit 9
  PIN_A10: 17,  // Address Bus Bit 10
  PIN_A11: 18,  // Address Bus Bit 11
  PIN_A12: 19,  // Address Bus Bit 12
  PIN_A13: 20,  // Address Bus Bit 13
  PIN_A14: 21,  // Address Bus Bit 14
  PIN_A15: 22,  // Address Bus Bit 15
  PIN_D0: 23,  // Data Bus Bit 0
  PIN_D1: 24,  // Data Bus Bit 1
  PIN_D2: 25,  // Data Bus Bit 2
  PIN_D3: 26,  // Data Bus Bit 3
  PIN_D4: 27,  // Data Bus Bit 4
  PIN_D5: 28,  // Data Bus Bit 5
  PIN_D6: 29,  // Data Bus Bit 6
  PIN_D7: 30,  // Data Bus Bit 7
  PIN_RD: 31,  // Read Strobe (active low)
  PIN_WR: 32,  // Write Strobe (active low)
  PIN_CS: 33,  // Chip Select (active low)
  PIN_SOUND_OUT: 34,  // Audio Output
  PIN_LCD_DATA0: 35,  // LCD Data Bus Bit 0
  PIN_LCD_DATA1: 36,  // LCD Data Bus Bit 1
  PIN_LCD_DATA2: 37,  // LCD Data Bus Bit 2
  PIN_LCD_DATA3: 38,  // LCD Data Bus Bit 3
  PIN_LCD_DATA4: 39,  // LCD Data Bus Bit 4
  PIN_LCD_DATA5: 40,  // LCD Data Bus Bit 5
  PIN_LCD_DATA6: 41,  // LCD Data Bus Bit 6
  PIN_LCD_DATA7: 42,  // LCD Data Bus Bit 7
  PIN_IR: 43,  // Infrared Port (DMG-CGB-01)

  init: function() {
    // 硬件初始化
  }
};
