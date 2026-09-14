/**
 * Ricoh-2A03 寄存器定义
 * 生成自: Ricoh/MOS-6502/Ricoh-2A03
 * 版本: 1.0
 */
export const ricoh_2a03 = {
  // CPU: MOS-6502, 8位, 10765930 Hz

  // 寄存器定义
  // Accumulator
  A: 0x00,
  // X Index
  X: 0x01,
  // Y Index
  Y: 0x02,
  // Stack Pointer
  SP: 0x03,
  // Program Counter (16-bit)
  PC: 0x04,
  // Processor Status
  P: 0x06,
  P_C: 0,  // Carry
  P_Z: 1,  // Zero
  P_I: 2,  // Interrupt Disable
  P_D: 3,  // Decimal Mode
  P_B: 4,  // Break
  P_U: 5,  // Unused
  P_V: 6,  // Overflow
  P_N: 7,  // Negative

  // 内存段
  // CPU 2KB RAM (mirrored)
  cpu_ram_START: 0x0000,
  cpu_ram_END: 0x07FF,
  cpu_ram_SIZE: 2048,
  // PPU Registers (mirrored every 8 bytes)
  ppu_registers_START: 0x2000,
  ppu_registers_END: 0x3FFF,
  ppu_registers_SIZE: 8192,
  // APU and I/O Registers
  apu_registers_START: 0x4000,
  apu_registers_END: 0x401F,
  apu_registers_SIZE: 32,
  // Expansion ROM
  expansion_START: 0x4020,
  expansion_END: 0x5FFF,
  expansion_SIZE: 8160,
  // Save RAM
  sram_START: 0x6000,
  sram_END: 0x7FFF,
  sram_SIZE: 8192,
  // PRG ROM Lower Bank (16KB)
  prg_rom_low_START: 0x8000,
  prg_rom_low_END: 0xBFFF,
  prg_rom_low_SIZE: 16384,
  // PRG ROM Higher Bank (16KB)
  prg_rom_high_START: 0xC000,
  prg_rom_high_END: 0xFFFF,
  prg_rom_high_SIZE: 16384,

  // 外设定义
  // Picture Processing Unit
  PPU_BASE: 0x2000,
  PPU_PPUCTRL: 0x00004000,
  PPU_PPUMASK: 0x00004001,
  PPU_PPUSTATUS: 0x00004002,
  PPU_OAMADDR: 0x00004003,
  PPU_OAMDATA: 0x00004004,
  PPU_PPUSCROLL: 0x00004005,
  PPU_PPUADDR: 0x00004006,
  PPU_PPUDATA: 0x00004007,
  // Audio Processing Unit
  APU_BASE: 0x4000,
  APU_PULSE1_VOL: 0x00008000,
  APU_PULSE1_SWEEP: 0x00008001,
  APU_PULSE1_LO: 0x00008002,
  APU_PULSE1_HI: 0x00008003,
  APU_PULSE2_VOL: 0x00008004,
  APU_PULSE2_SWEEP: 0x00008005,
  APU_PULSE2_LO: 0x00008006,
  APU_PULSE2_HI: 0x00008007,
  APU_TRIANGLE: 0x00008008,
  APU_TRIANGLE_HI: 0x0000800B,
  APU_NOISE_VOL: 0x0000800C,
  APU_NOISE_HI: 0x0000800E,
  APU_NOISE_LENGTH: 0x0000800F,
  APU_DMC_RATE: 0x00008010,
  APU_DMC_RAW: 0x00008011,
  APU_DMC_START: 0x00008012,
  APU_DMC_LENGTH: 0x00008013,
  APU_OAMDMA: 0x00008014,
  APU_SNDCHN: 0x00008015,
  APU_JOY1: 0x00008016,
  APU_JOY2: 0x00008017,
  // Controller Port 1
  INPUT1_BASE: 0x4016,
  INPUT1_JOYPAD1: 0x0000802C,
  // Controller Port 2
  INPUT2_BASE: 0x4017,
  INPUT2_JOYPAD2: 0x0000802E,

  // 中断向量
  IRQ_RESET: 0,  // Reset
  IRQ_NMI: 1,  // Non-Maskable Interrupt (VBlank)
  IRQ_IRQ: 2,  // IRQ / BRK

  init: function() {
    // 硬件初始化
  }
};
