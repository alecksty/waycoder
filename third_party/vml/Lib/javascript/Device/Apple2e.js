/**
 * Apple-IIe 寄存器定义
 * 生成自: Apple Computer/Apple II/Apple-IIe
 * 版本: 1.0
 */
export const apple_iie = {
  // CPU: MOS-6502, 8位, 1021800 Hz

  // 寄存器定义
  // Accumulator
  A: 0x00,
  // X Index Register
  X: 0x01,
  // Y Index Register
  Y: 0x02,
  // Stack Pointer
  SP: 0x03,
  // Program Counter
  PC: 0x04,
  // Processor Status
  P: 0x06,
  P_C: 0,  // Carry Flag
  P_Z: 1,  // Zero Flag
  P_I: 2,  // Interrupt Disable
  P_D: 3,  // Decimal Mode
  P_B: 4,  // Break Command
  P_U: 5,  // Unused
  P_V: 6,  // Overflow Flag
  P_N: 7,  // Negative Flag

  // 内存段
  // Main RAM (48KB base, up to 64KB with slot RAM)
  main_ram_START: 0x0000,
  main_ram_END: 0xBFFF,
  main_ram_SIZE: 49152,
  // Text screen buffer (40x24)
  text_ram_START: 0x0400,
  text_ram_END: 0x07FF,
  text_ram_SIZE: 1024,
  // High-resolution graphics buffer
  hires_ram_START: 0x2000,
  hires_ram_END: 0x5FFF,
  hires_ram_SIZE: 16384,
  // 80-column text auxiliary RAM
  aux_ram_START: 0x0400,
  aux_ram_END: 0x09FF,
  aux_ram_SIZE: 1536,
  // Monitor ROM (applesoft/Integer)
  monitor_rom_START: 0xC100,
  monitor_rom_END: 0xCFFF,
  monitor_rom_SIZE: 3840,
  // Applesoft BASIC ROM
  basic_rom_START: 0xD000,
  basic_rom_END: 0xFFFF,
  basic_rom_SIZE: 12288,
  // Expansion Slot ROM
  slot_rom_START: 0xC100,
  slot_rom_END: 0xC7FF,
  slot_rom_SIZE: 768,
  // I/O Select (slot space)
  mmio_START: 0xC080,
  mmio_END: 0xC0FF,
  mmio_SIZE: 128,

  // 外设定义
  // Versatile Interface Adapter (6522)
  VIA_BASE: 0xC000,
  VIA_ORB: 0x00018000,
  VIA_ORA: 0x00018001,
  VIA_DDRB: 0x00018002,
  VIA_DDRA: 0x00018003,
  VIA_T1C: 0x00018004,
  VIA_T1L: 0x00018006,
  VIA_T2C: 0x00018008,
  VIA_SR: 0x0001800A,
  VIA_ACR: 0x0001800B,
  VIA_PCR: 0x0001800C,
  VIA_IFG: 0x0001800D,
  VIA_IER: 0x0001800E,
  VIA_ORA_NH: 0x0001800F,
  // Peripheral Interface Adapter (6520)
  PIA_BASE: 0xC010,
  PIA_PA: 0x00018020,
  PIA_PB: 0x00018021,
  PIA_DDRA: 0x00018022,
  PIA_DDRB: 0x00018023,
  PIA_CA1: 0x00018024,
  PIA_CA2: 0x00018025,
  PIA_CB1: 0x00018026,
  PIA_CB2: 0x00018027,
  // Keyboard (via PIA)
  KBD_BASE: 0xC000,
  KBD_KEYDATA: 0x00018000,
  KBD_KEYSTROBE: 0x00018010,
  KBD_KBDCTRL: 0x00018025,
  KBD_KBDERR: 0x00018026,
  // Speaker
  SPEAKER_BASE: 0xC030,
  SPEAKER_SPKR: 0x00018060,
  // Game I/O Port
  GAME_PORT_BASE: 0xC050,
  GAME_PORT_GAME_SW0: 0x000180B1,
  GAME_PORT_GAME_SW1: 0x000180B2,
  GAME_PORT_GAME_AN0: 0x000180B4,
  GAME_PORT_GAME_AN1: 0x000180B5,
  GAME_PORT_GAME_AN2: 0x000180B6,
  GAME_PORT_GAME_AN3: 0x000180B7,
  GAME_PORT_GAME_TRIG: 0x000180C0,
  // Disk II Controller
  DISKII_BASE: 0xC0E0,
  DISKII_PHASE0: 0x000181C0,
  DISKII_PHASE1: 0x000181C1,
  DISKII_PHASE2: 0x000181C2,
  DISKII_PHASE3: 0x000181C3,
  DISKII_Q6L: 0x000181CC,
  DISKII_Q7L: 0x000181CD,
  DISKII_Q6R: 0x000181CE,
  DISKII_Q7R: 0x000181CF,
  // Video Display Generator
  VIDEO_BASE: 0xC050,
  VIDEO_TXTCLR: 0x000180A0,
  VIDEO_MIXCLR: 0x000180A1,
  VIDEO_TXTPAGE2: 0x000180A4,
  VIDEO_TXTPAGE1: 0x000180A5,
  VIDEO_LORES: 0x000180A6,
  VIDEO_HIRES: 0x000180A7,
  VIDEO_DHIRESON: 0x000180AE,
  VIDEO_AN0: 0x000180A8,
  VIDEO_AN1: 0x000180A9,
  VIDEO_AN2: 0x000180AA,
  VIDEO_AN3: 0x000180AB,
  VIDEO_80STORE: 0x00018050,
  // RAM Read/Write Control
  RAMRD_BASE: 0xC080,
  RAMRD_INTCXROM: 0x0001907F,

  // 中断向量
  IRQ_RESET: 0,  // Power-on Reset
  IRQ_NMI: 1,  // Non-Maskable Interrupt (from VIA)
  IRQ_IRQ: 2,  // IRQ from VIA/timer/slot
  IRQ_BRK: 3,  // BRK Instruction

  // 引脚定义
  PIN_VCC: 1,  // +5V Power
  PIN_GND: 2,  // Ground
  PIN_RESET: 3,  // System Reset
  PIN_CLK: 4,  // System Clock (1.023MHz NTSC)
  PIN_RDY: 5,  // CPU Ready
  PIN_NMI: 6,  // Non-Maskable Interrupt
  PIN_IRQ: 7,  // Interrupt Request
  PIN_SO: 8,  // Set Overflow
  PIN_RWB: 9,  // Read/Write Bar
  PIN_SYNC: 10,  // Instruction Sync
  PIN_A0-A15: 11,  // Address Bus (16-bit)
  PIN_D0-D7: 12,  // Data Bus (8-bit)
  PIN_PHASE0: 13,  // Phase 0 (4MHz system)
  PIN_PHASE1: 14,  // Phase 1
  PIN_PHASE2: 15,  // Phase 2
  PIN_PHASE3: 16,  // Phase 3

  init: function() {
    // 硬件初始化
  }
};
