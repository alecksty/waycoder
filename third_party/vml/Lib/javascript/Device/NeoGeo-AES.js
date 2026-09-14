/**
 * NeoGeo-68000 寄存器定义
 * 生成自: SNK/M68K/NeoGeo-68000
 * 版本: 1.0
 */
export const neogeo_68000 = {
  // CPU: MC68000, 32位, 12000000 Hz

  // 寄存器定义
  // Data Register 0
  D0: 0x00,
  // Data Register 1
  D1: 0x04,
  // Data Register 2
  D2: 0x08,
  // Data Register 3
  D3: 0x0C,
  // Data Register 4
  D4: 0x10,
  // Data Register 5
  D5: 0x14,
  // Data Register 6
  D6: 0x18,
  // Data Register 7
  D7: 0x1C,
  // Address Register 0
  A0: 0x20,
  // Address Register 1
  A1: 0x24,
  // Address Register 2
  A2: 0x28,
  // Address Register 3
  A3: 0x2C,
  // Address Register 4
  A4: 0x30,
  // Address Register 5
  A5: 0x34,
  // Address Register 6
  A6: 0x38,
  // User Stack Pointer (USP)
  A7: 0x3C,
  // Supervisor Stack Pointer (SSP)
  SP: 0x3C,
  // Program Counter
  PC: 0x40,
  // Status Register
  SR: 0x44,
  SR_C: 0,  // Carry
  SR_V: 1,  // Overflow
  SR_Z: 2,  // Zero
  SR_N: 3,  // Negative
  SR_X: 4,  // Extend
  SR_I0: 8,  // Interrupt Mask 0
  SR_I1: 9,  // Interrupt Mask 1
  SR_I2: 10,  // Interrupt Mask 2
  SR_M: 11,  // Master/Interrupt
  SR_S: 13,  // Supervisor/User
  SR_T0: 14,  // Trace Mode 0
  SR_T1: 15,  // Trace Mode 1

  // 内存段
  // Work RAM (64KB)
  work_ram_START: 0x100000,
  work_ram_END: 0x10FFFF,
  work_ram_SIZE: 65536,
  // Backup SRAM (battery-backed, 64KB)
  backup_ram_START: 0x200000,
  backup_ram_END: 0x20FFFF,
  backup_ram_SIZE: 65536,
  // Fix Layer ROM (512KB)
  fix_rom_START: 0x000000,
  fix_rom_END: 0x07FFFF,
  fix_rom_SIZE: 524288,
  // Sprite ROM (up to 1MB)
  spr_rom_START: 0x400000,
  spr_rom_END: 0x4FFFFF,
  spr_rom_SIZE: 1048576,
  // Audio ROM (up to 64KB)
  audio_rom_START: 0x800000,
  audio_rom_END: 0x80FFFF,
  audio_rom_SIZE: 65536,
  // Cartridge ROM (up to 512KB, expandable)
  cart_rom_START: 0xC00000,
  cart_rom_END: 0xC7FFFF,
  cart_rom_SIZE: 524288,
  // I/O Area (VDP, YM2610, Z80 port, etc.)
  io_area_START: 0x300000,
  io_area_END: 0x3FFFFF,
  io_area_SIZE: 1048576,
  // Z80 Work RAM (2KB)
  z80_ram_START: 0x10000,
  z80_ram_END: 0x107FF,
  z80_ram_SIZE: 2048,

  // 外设定义
  // Z80 Audio Coprocessor @ 4MHz
  Z80_BASE: 0x300000,
  Z80_Z80_A: 0x00300000,
  Z80_Z80_F: 0x00300001,
  Z80_Z80_B: 0x00300002,
  Z80_Z80_C: 0x00300003,
  Z80_Z80_D: 0x00300004,
  Z80_Z80_E: 0x00300005,
  Z80_Z80_H: 0x00300006,
  Z80_Z80_L: 0x00300007,
  Z80_Z80_AF_: 0x00300008,
  Z80_Z80_BC_: 0x0030000A,
  Z80_Z80_DE_: 0x0030000C,
  Z80_Z80_HL_: 0x0030000E,
  Z80_Z80_IX: 0x00300010,
  Z80_Z80_IY: 0x00300012,
  Z80_Z80_SP: 0x00300014,
  Z80_Z80_PC: 0x00300016,
  Z80_Z80_I: 0x00300018,
  Z80_Z80_R: 0x00300019,
  Z80_Z80_IM: 0x0030001A,
  Z80_Z80_BUSREQ: 0x0030001E,
  Z80_Z80_RESET: 0x0030001F,
  // Yamaha YM2610 FM + ADPCM Audio Generator
  YM2610_BASE: 0x300000,
  YM2610_YM_ADDR_A0: 0x00300000,
  YM2610_YM_DATA_A0: 0x00300001,
  YM2610_YM_ADDR_A1: 0x00300002,
  YM2610_YM_DATA_A1: 0x00300003,
  YM2610_YM_ADDR_B0: 0x00300004,
  YM2610_YM_DATA_B0: 0x00300005,
  YM2610_YM_TEST: 0x00300008,
  YM2610_YM_FM_CH0_FREQ_L: 0x003000A0,
  YM2610_YM_FM_CH0_FREQ_H: 0x003000A4,
  YM2610_YM_FM_CH1_FREQ_L: 0x003000A1,
  YM2610_YM_FM_CH1_FREQ_H: 0x003000A5,
  YM2610_YM_FM_CH2_FREQ_L: 0x003000A2,
  YM2610_YM_FM_CH2_FREQ_H: 0x003000A6,
  YM2610_YM_FM_CH3_FREQ_L: 0x003000A3,
  YM2610_YM_FM_CH3_FREQ_H: 0x003000A7,
  YM2610_YM_FM_KEY_ON: 0x00300028,
  YM2610_YM_FM_CH0_ALG: 0x003000B0,
  YM2610_YM_FM_CH1_ALG: 0x003000B1,
  YM2610_YM_FM_CH2_ALG: 0x003000B2,
  YM2610_YM_FM_CH3_ALG: 0x003000B3,
  YM2610_YM_FM_TIMER_H: 0x00300024,
  YM2610_YM_FM_TIMER_L: 0x00300025,
  YM2610_YM_FM_TIMER_CTRL: 0x00300027,
  YM2610_YM_FM_TIMER_CTRL_TIMER_A_START: 0,  // Timer A Start
  YM2610_YM_FM_TIMER_CTRL_TIMER_B_START: 1,  // Timer B Start
  YM2610_YM_FM_TIMER_CTRL_LOAD_A: 2,  // Load Timer A
  YM2610_YM_FM_TIMER_CTRL_LOAD_B: 3,  // Load Timer B
  YM2610_YM_FM_TIMER_CTRL_IRQ_EN_A: 4,  // Timer A IRQ Enable
  YM2610_YM_FM_TIMER_CTRL_IRQ_EN_B: 5,  // Timer B IRQ Enable
  YM2610_YM_FM_TIMER_CTRL_CSM_MODE: 7,  // CSM Mode (auto Key-On after timer A)
  YM2610_YM_FM_CH0_DETune: 0x00300030,
  YM2610_YM_FM_CH0_MUL: 0x00300030,
  YM2610_YM_FM_CH0_TL: 0x00300040,
  YM2610_YM_FM_CH0_KS_AR: 0x00300050,
  YM2610_YM_FM_CH0_AM_DR: 0x00300060,
  YM2610_YM_FM_CH0_SR: 0x00300070,
  YM2610_YM_FM_CH0_RR_SL: 0x00300080,
  YM2610_YM_FM_CH0_SSG: 0x00300090,
  YM2610_YM_SSG_CHA_FREQ_L: 0x00300000,
  YM2610_YM_SSG_CHA_FREQ_H: 0x00300001,
  YM2610_YM_SSG_CHB_FREQ_L: 0x00300002,
  YM2610_YM_SSG_CHB_FREQ_H: 0x00300003,
  YM2610_YM_SSG_CHC_FREQ_L: 0x00300004,
  YM2610_YM_SSG_CHC_FREQ_H: 0x00300005,
  YM2610_YM_SSG_CHA_VOL: 0x00300008,
  YM2610_YM_SSG_CHB_VOL: 0x00300009,
  YM2610_YM_SSG_CHC_VOL: 0x0030000A,
  YM2610_YM_SSG_MIXER: 0x00300007,
  YM2610_YM_SSG_ENV_FREQ_L: 0x0030000B,
  YM2610_YM_SSG_ENV_FREQ_H: 0x0030000C,
  YM2610_YM_SSG_ENV_SHAPE: 0x0030000D,
  YM2610_YM_SSG_IO_A: 0x0030000E,
  YM2610_YM_SSG_IO_B: 0x0030000F,
  YM2610_YM_ADPCM_STATUS: 0x00300010,
  YM2610_YM_ADPCM_START: 0x00300011,
  YM2610_YM_ADPCM_END: 0x00300012,
  YM2610_YM_ADPCM_VOL_L: 0x00300013,
  YM2610_YM_ADPCM_VOL_R: 0x00300014,
  YM2610_YM_DELTA_N_L: 0x00300015,
  YM2610_YM_DELTA_N_H: 0x00300016,
  YM2610_YM_ADPCM_B_START: 0x00300018,
  YM2610_YM_ADPCM_B_END: 0x00300019,
  YM2610_YM_ADPCM_B_VOL: 0x0030001A,
  YM2610_YM_ADPCM_B_CTRL: 0x0030001B,
  // Neo Geo VDP (Video Display Processor)
  YGV628_BASE: 0x3C0000,
  YGV628_VRAM_ADDR_L: 0x003C0000,
  YGV628_VRAM_ADDR_H: 0x003C0001,
  YGV628_VRAM_DATA: 0x003C0002,
  YGV628_VRAM_READ: 0x003C0003,
  YGV628_CRAM_ADDR: 0x003C0004,
  YGV628_CRAM_DATA: 0x003C0005,
  YGV628_VDP_STATUS: 0x003C0006,
  YGV628_VDP_STATUS_VBLANK: 0,  // V-Blank Flag
  YGV628_VDP_STATUS_FIELD: 1,  // Field (0=even, 1=odd for interlace)
  YGV628_VDP_STATUS_ODD_FIELD: 1,  // Odd Field Flag
  YGV628_VDP_STATUS_DMA_BUSY: 2,  // DMA Busy
  YGV628_VDP_STATUS_SPRITE_OVERFLOW: 3,  // Sprite Overflow (more than 16 per line)
  YGV628_VDP_STATUS_SPRITE_COLLISION: 4,  // Sprite Collision
  YGV628_VDP_CTRL: 0x003C0007,
  YGV628_VDP_CTRL_VRAM_INC: 0,  // VRAM Auto-Increment (0=+1, 1=+2)
  YGV628_VDP_CTRL_ROW_SCROLL: 1,  // Row Scroll Mode
  YGV628_VDP_CTRL_COL_SCROLL: 2,  // Column Scroll Mode
  YGV628_VDP_CTRL_FIX_DISP: 3,  // Fix Layer Display
  YGV628_VDP_CTRL_SPR_DISP: 4,  // Sprite Layer Display
  YGV628_VDP_CTRL_SCROLL2_DISP: 5,  // Scroll Layer 2 Display
  YGV628_VDP_CTRL_SCROLL1_DISP: 6,  // Scroll Layer 1 Display
  YGV628_VDP_CTRL_DMA_ENABLE: 7,  // DMA Enable
  YGV628_SCROLL1_BASE: 0x003C0008,
  YGV628_SCROLL2_BASE: 0x003C000A,
  YGV628_SPR_BASE: 0x003C000C,
  YGV628_SPR_COUNT: 0x003C000E,
  YGV628_WINDOW_X: 0x003C0010,
  YGV628_WINDOW_Y: 0x003C0011,
  YGV628_WINDOW_W: 0x003C0012,
  YGV628_WINDOW_H: 0x003C0013,
  YGV628_LINE_SCROLL_L: 0x003C0014,
  YGV628_LINE_SCROLL_H: 0x003C0015,
  YGV628_RASTER_COMP: 0x003C0016,
  YGV628_H_TIMING: 0x003C0018,
  YGV628_V_TIMING: 0x003C0019,
  YGV628_DMA_SRC_L: 0x003C001A,
  YGV628_DMA_SRC_H: 0x003C001B,
  YGV628_DMA_SRC_B: 0x003C001C,
  YGV628_DMA_DEST_L: 0x003C001D,
  YGV628_DMA_DEST_H: 0x003C001E,
  YGV628_DMA_COUNT: 0x003C001F,
  // Neo Geo System Driver / Controller
  NEODRIVER_BASE: 0x310000,
  NEODRIVER_PDI0: 0x00310000,
  NEODRIVER_PDI0_UP: 0,  // Up (0=pressed)
  NEODRIVER_PDI0_DOWN: 1,  // Down (0=pressed)
  NEODRIVER_PDI0_LEFT: 2,  // Left (0=pressed)
  NEODRIVER_PDI0_RIGHT: 3,  // Right (0=pressed)
  NEODRIVER_PDI0_A: 4,  // A Button (0=pressed)
  NEODRIVER_PDI0_B: 5,  // B Button (0=pressed)
  NEODRIVER_PDI0_C: 6,  // C Button (0=pressed)
  NEODRIVER_PDI0_D: 7,  // D Button (0=pressed)
  NEODRIVER_PDI1: 0x00310001,
  NEODRIVER_PDI2: 0x00310002,
  NEODRIVER_PDI3: 0x00310003,
  NEODRIVER_PDO0: 0x00310004,
  NEODRIVER_PDO1: 0x00310005,
  NEODRIVER_PDO2: 0x00310006,
  NEODRIVER_PDO3: 0x00310007,
  NEODRIVER_DIPSEL1: 0x00310008,
  NEODRIVER_DIPSEL1_COIN_SELECT: 0,  // Coin Select (0=common, 1=1 coin 1 credit)
  NEODRIVER_DIPSEL1_FREE_PLAY: 1,  // Free Play
  NEODRIVER_DIPSEL1_DEMO_SOUND: 2,  // Demo Sound
  NEODRIVER_DIPSEL1_CHIP_MODE: 3,  // Chip Mode (0=AES, 1=MVS)
  NEODRIVER_DIPSEL1_CONTROLLER_TYPE: 4,  // Controller Type (0=standard, 1=keyboard)
  NEODRIVER_DIPSEL2: 0x00310009,
  NEODRIVER_DIPSEL3: 0x0031000A,
  NEODRIVER_DIPSEL4: 0x0031000B,
  NEODRIVER_SYSCTRL: 0x0031000C,
  NEODRIVER_SYSCTRL_RTSEL: 0,  // Real Time Switch Select
  NEODRIVER_SYSCTRL_RESERVED0: 1,  // Reserved
  NEODRIVER_SYSCTRL_SCC: 2,  // System Clock Control
  NEODRIVER_SYSCTRL_PHEN: 3,  // PHEN (bus timing)
  NEODRIVER_SYSCTRL_PCK2: 4,  // PCK2 (bus timing)
  NEODRIVER_SYSCTRL_PCK1: 5,  // PCK1 (bus timing)
  NEODRIVER_SYSCTRL_CKDIV2: 6,  // Clock Divide by 2
  NEODRIVER_SYSCTRL_FEFIX: 7,  // FE Fix
  NEODRIVER_IRQMASK: 0x0031000D,
  NEODRIVER_IRQMASK_VBLANK_MASK: 0,  // V-Blank Interrupt Mask
  NEODRIVER_IRQMASK_HBLANK_MASK: 1,  // H-Blank Interrupt Mask
  NEODRIVER_IRQMASK_VECTOR_IN_MASK: 2,  // Vector In (from Z80) Mask
  NEODRIVER_IRQMASK_SYSTEM_IN_MASK: 3,  // System Input (JAMMA) Mask
  NEODRIVER_IRQFLAG: 0x0031000E,
  NEODRIVER_SECAM_MODE: 0x0031000F,
  // Controller Port 1
  CONTROLLER1_BASE: 0x310000,
  CONTROLLER1_PDI0: 0x00310000,
  // Controller Port 2
  CONTROLLER2_BASE: 0x310001,
  CONTROLLER2_PDI1: 0x00310001,
  // Memory Card Interface
  MEMORY_CARD_BASE: 0x320000,
  MEMORY_CARD_CARD_DATA: 0x00320000,
  MEMORY_CARD_CARD_STATUS: 0x00320001,
  MEMORY_CARD_CARD_STATUS_INSERTED: 0,  // Card Inserted (0=yes)
  MEMORY_CARD_CARD_STATUS_WRITE_PROTECT: 1,  // Write Protected (0=yes)
  MEMORY_CARD_CARD_STATUS_READY: 2,  // Ready for I/O
  MEMORY_CARD_CARD_CTRL: 0x00320002,
  // Cartridge Bank Switching
  CART_BANK_BASE: 0x2FFFF0,
  CART_BANK_BANK_REG: 0x002FFFF0,

  // 中断向量
  IRQ_RESET_SP: 1,  // Reset Initial Stack Pointer
  IRQ_RESET_PC: 2,  // Reset Initial PC
  IRQ_BUS_ERROR: 3,  // Bus Error
  IRQ_ADDRESS_ERROR: 4,  // Address Error
  IRQ_ILLEGAL_INSTR: 5,  // Illegal Instruction
  IRQ_ZERO_DIVIDE: 6,  // Zero Divide
  IRQ_CHK_EXCEPTION: 7,  // CHK Exception
  IRQ_TRAPV: 8,  // TRAPV Exception
  IRQ_PRIVILEGE: 9,  // Privilege Violation
  IRQ_TRACE: 10,  // Trace
  IRQ_LINE_A: 11,  // Line 1010 Emulator
  IRQ_LINE_F: 12,  // Line 1111 Emulator
  IRQ_IRQ1: 24,  // H-Blank / VDP Interrupt (raster)
  IRQ_IRQ2: 25,  // V-Blank / Frame End Interrupt
  IRQ_IRQ3: 26,  // System Controller / Z80 Vector In
  IRQ_IRQ4: 27,  // JAMMA / System Input
  IRQ_IRQ5: 28,  // Z80 Interrupt Request
  IRQ_TRAP0: 32,  // TRAP #0 (system call)
  IRQ_TRAP1: 33,  // TRAP #1

  // 引脚定义
  PIN_VCC: 1,  // Power Supply (5V)
  PIN_GND: 2,  // Ground
  PIN_CLK: 3,  // System Clock (12MHz for 68K)
  PIN_RESET: 4,  // Reset (active low)
  PIN_HALT: 5,  // Halt (stops CPU)
  PIN_NMI: 6,  // Non-Maskable Interrupt
  PIN_IPL0: 7,  // Interrupt Priority Level 0
  PIN_IPL1: 8,  // Interrupt Priority Level 1
  PIN_IPL2: 9,  // Interrupt Priority Level 2
  PIN_DTACK: 10,  // Data Acknowledge (active low)
  PIN_BERR: 11,  // Bus Error (active low)
  PIN_BR: 12,  // Bus Request (active low)
  PIN_BG: 13,  // Bus Grant (active low)
  PIN_A0: 14,  // Address Bus Bit 0
  PIN_A1: 15,  // Address Bus Bit 1
  PIN_A2: 16,  // Address Bus Bit 2
  PIN_A3: 17,  // Address Bus Bit 3
  PIN_A4: 18,  // Address Bus Bit 4
  PIN_A5: 19,  // Address Bus Bit 5
  PIN_A6: 20,  // Address Bus Bit 6
  PIN_A7: 21,  // Address Bus Bit 7
  PIN_A8: 22,  // Address Bus Bit 8
  PIN_A9: 23,  // Address Bus Bit 9
  PIN_A10: 24,  // Address Bus Bit 10
  PIN_A11: 25,  // Address Bus Bit 11
  PIN_A12: 26,  // Address Bus Bit 12
  PIN_A13: 27,  // Address Bus Bit 13
  PIN_A14: 28,  // Address Bus Bit 14
  PIN_A15: 29,  // Address Bus Bit 15
  PIN_A16: 30,  // Address Bus Bit 16
  PIN_A17: 31,  // Address Bus Bit 17
  PIN_A18: 32,  // Address Bus Bit 18
  PIN_A19: 33,  // Address Bus Bit 19
  PIN_A20: 34,  // Address Bus Bit 20
  PIN_A21: 35,  // Address Bus Bit 21
  PIN_A22: 36,  // Address Bus Bit 22
  PIN_A23: 37,  // Address Bus Bit 23
  PIN_D0: 38,  // Data Bus Bit 0
  PIN_D1: 39,  // Data Bus Bit 1
  PIN_D2: 40,  // Data Bus Bit 2
  PIN_D3: 41,  // Data Bus Bit 3
  PIN_D4: 42,  // Data Bus Bit 4
  PIN_D5: 43,  // Data Bus Bit 5
  PIN_D6: 44,  // Data Bus Bit 6
  PIN_D7: 45,  // Data Bus Bit 7
  PIN_D8: 46,  // Data Bus Bit 8
  PIN_D9: 47,  // Data Bus Bit 9
  PIN_D10: 48,  // Data Bus Bit 10
  PIN_D11: 49,  // Data Bus Bit 11
  PIN_D12: 50,  // Data Bus Bit 12
  PIN_D13: 51,  // Data Bus Bit 13
  PIN_D14: 52,  // Data Bus Bit 14
  PIN_D15: 53,  // Data Bus Bit 15
  PIN_AS: 54,  // Address Strobe (active low)
  PIN_UDS: 55,  // Upper Data Strobe (active low)
  PIN_LDS: 56,  // Lower Data Strobe (active low)
  PIN_R_W: 57,  // Read/Write (1=Read, 0=Write)
  PIN_FC0: 58,  // Function Code 0
  PIN_FC1: 59,  // Function Code 1
  PIN_FC2: 60,  // Function Code 2
  PIN_E: 61,  // E Clock (Enable, for Z80 sync)
  PIN_VPA: 62,  // Valid Peripheral Address (for Z80 I/O)
  PIN_VM: 63,  // Valid Memory (for Z80 memory access)
  PIN_BKGR: 64,  // Background Audio Mix (analog output)
  PIN_AUDIO_OUT: 65,  // Main Audio Output (Left)
  PIN_AUDIO_R: 66,  // Audio Right Channel
  PIN_VIDEO_R: 67,  // Video Output Red
  PIN_VIDEO_G: 68,  // Video Output Green
  PIN_VIDEO_B: 69,  // Video Output Blue
  PIN_SYNC: 70,  // Video Sync

  init: function() {
    // 硬件初始化
  }
};
