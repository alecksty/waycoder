/**
 * Amstrad-CPC-464 寄存器定义
 * 生成自: Amstrad/CPC/Amstrad-CPC-464
 * 版本: 1.0
 */
export const amstrad_cpc_464 = {
  // CPU: Z80A, 8位, 4000000 Hz

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
  // Index Y
  IY: 0x14,
  // Stack Pointer
  SP: 0x16,
  // Program Counter
  PC: 0x18,

  // 内存段
  // Lower ROM (AMSDOS / CP/M)
  lower_rom_START: 0x0000,
  lower_rom_END: 0x3FFF,
  lower_rom_SIZE: 16384,
  // Lower RAM bank (switchable)
  ram_bank0_START: 0x0000,
  ram_bank0_END: 0x3FFF,
  ram_bank0_SIZE: 16384,
  // Main RAM (32KB)
  ram_main_START: 0x4000,
  ram_main_END: 0xBFFF,
  ram_main_SIZE: 32768,
  // Upper ROM (BASIC)
  upper_rom_START: 0xC000,
  upper_rom_END: 0xFFFF,
  upper_rom_SIZE: 16384,

  // 外设定义
  // Gate Array - Custom ASIC (video/sound/RAM control)
  GA_BASE: 0x7F00,
  GA_GA_MR: 0x0000FE00,
  GA_GA_IR: 0x0000FE01,
  GA_GA_R1: 0x0000FE02,
  GA_GA_R2: 0x0000FE03,
  GA_GA_R3: 0x0000FE04,
  GA_GA_R4: 0x0000FE05,
  GA_GA_R5: 0x0000FE06,
  GA_GA_R6: 0x0000FE07,
  GA_GA_R7: 0x0000FE08,
  // CRT Controller 6845 - Video timing
  CRTC_BASE: 0xBC00,
  CRTC_CRTC_REG: 0x00017800,
  CRTC_CRTC_DATA: 0x00017900,
  CRTC_CRTC_H_TOTAL: 0x00017801,
  CRTC_CRTC_H_DISP: 0x00017802,
  CRTC_CRTC_HSYNC_POS: 0x00017803,
  CRTC_CRTC_HSYNC_WIDTH: 0x00017804,
  CRTC_CRTC_V_TOTAL: 0x00017805,
  CRTC_CRTC_V_TOTAL_ADJ: 0x00017806,
  CRTC_CRTC_V_DISP: 0x00017807,
  CRTC_CRTC_VSYNC_POS: 0x00017808,
  CRTC_CRTC_INTERLACE: 0x00017809,
  CRTC_CRTC_CURSOR_START: 0x0001780A,
  CRTC_CRTC_CURSOR_END: 0x0001780B,
  CRTC_CRTC_SA_HI: 0x0001780C,
  CRTC_CRTC_SA_LO: 0x0001780D,
  CRTC_CRTC_CURSOR_HI: 0x0001780E,
  CRTC_CRTC_CURSOR_LO: 0x0001780F,
  // AY-3-8912 Programmable Sound Generator
  PSG_BASE: 0xF400,
  PSG_PSG_REG: 0x0001E800,
  PSG_PSG_DATA: 0x0001EA00,
  PSG_FREQ_A_LO: 0x0001E800,
  PSG_FREQ_A_HI: 0x0001E801,
  PSG_FREQ_B_LO: 0x0001E802,
  PSG_FREQ_B_HI: 0x0001E803,
  PSG_FREQ_C_LO: 0x0001E804,
  PSG_FREQ_C_HI: 0x0001E805,
  PSG_NOISE_FREQ: 0x0001E806,
  PSG_ENABLE: 0x0001E807,
  PSG_VOL_A: 0x0001E808,
  PSG_VOL_B: 0x0001E809,
  PSG_VOL_C: 0x0001E80A,
  PSG_ENV_FREQ_LO: 0x0001E80B,
  PSG_ENV_FREQ_HI: 0x0001E80C,
  PSG_ENV_SHAPE: 0x0001E80D,
  PSG_PORT_A: 0x0001E80E,
  PSG_PORT_B: 0x0001E80F,
  // WD1772 Floppy Disk Controller (via expansion)
  FDC_BASE: 0xF800,
  FDC_FDC_STATUS: 0x0001F0E0,
  FDC_FDC_COMMAND: 0x0001F0E0,
  FDC_FDC_TRACK: 0x0001F0E1,
  FDC_FDC_SECTOR: 0x0001F0E2,
  FDC_FDC_DATA: 0x0001F0E3,
  // Centronics Parallel Printer Port
  PRINTER_BASE: 0xEE,
  PRINTER_PRN_DATA: 0x000001DC,
  PRINTER_PRN_STROBE: 0x000001DD,

  // 中断向量
  IRQ_RESET: 0,  // Power-on / Reset
  IRQ_NMI: 1,  // Non-Maskable Interrupt
  IRQ_INT: 2,  // Gate Array interrupt (50Hz vertical blank)

  // 引脚定义
  PIN_VCC: 1,  // +5V Power
  PIN_GND: 2,  // Ground
  PIN_CLK: 3,  // Z80 Clock (4MHz)
  PIN_A0-A15: 4,  // Address Bus
  PIN_D0-D7: 5,  // Data Bus
  PIN_MREQ: 6,  // Memory Request
  PIN_IORQ: 7,  // I/O Request
  PIN_RD: 8,  // Read
  PIN_WR: 9,  // Write
  PIN_INT: 10,  // Interrupt Request
  PIN_NMI: 11,  // Non-Maskable Interrupt
  PIN_RESET: 12,  // Reset

  init: function() {
    // 硬件初始化
  }
};
