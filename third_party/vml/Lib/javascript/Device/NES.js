/**
 * Nintendo Entertainment System 寄存器定义
 * 生成自: Nintendo/NES/Nintendo Entertainment System
 * 版本: 
 */
export const nintendo_entertainment_system = {
  // CPU: 6502, 0位, 0 Hz

  // 外设定义
  // Picture Processing Unit (Ricoh 2C02)
  PPU_BASE: ,
  PPU_PPUCTRL: 0x00002000,
  PPU_PPUCTRL_NMI: 7,  // VBlank NMI enable
  PPU_PPUCTRL_MasterSlave: 6,  // Master/slave select
  PPU_PPUCTRL_SpriteSize: 5,  // Sprite size (0=8x8, 1=8x16)
  PPU_PPUCTRL_BGPattern: 4,  // Background pattern table address
  PPU_PPUCTRL_SpritePattern: 3,  // Sprite pattern table address
  PPU_PPUCTRL_VRAMIncrement: 2,  // VRAM address increment (0=1, 1=32)
  PPU_PPUCTRL_Nametable: 0,  // Nametable address
  PPU_PPUMASK: 0x00002001,
  PPU_PPUMASK_EmphasizeBlue: 7,  // Emphasize blue
  PPU_PPUMASK_EmphasizeGreen: 6,  // Emphasize green
  PPU_PPUMASK_EmphasizeRed: 5,  // Emphasize red
  PPU_PPUMASK_ShowSprites: 4,  // Show sprites
  PPU_PPUMASK_ShowBackground: 3,  // Show background
  PPU_PPUMASK_ShowLeftSprites: 2,  // Show sprites in left 8 pixels
  PPU_PPUMASK_ShowLeftBackground: 1,  // Show background in left 8 pixels
  PPU_PPUMASK_Grayscale: 0,  // Grayscale mode
  PPU_PPUSTATUS: 0x00002002,
  PPU_PPUSTATUS_VBlank: 7,  // VBlank started
  PPU_PPUSTATUS_Sprite0Hit: 6,  // Sprite 0 hit
  PPU_PPUSTATUS_SpriteOverflow: 5,  // Sprite overflow
  PPU_OAMADDR: 0x00002003,
  PPU_OAMDATA: 0x00002004,
  PPU_PPUSCROLL: 0x00002005,
  PPU_PPUADDR: 0x00002006,
  PPU_PPUDATA: 0x00002007,
  PPU_OAMDMA: 0x00004014,
  // Audio Processing Unit (Ricoh 2A03)
  APU_BASE: ,
  APU_SQ1_VOL: 0x00004000,
  APU_SQ1_VOL_Duty: 6,  // Duty cycle
  APU_SQ1_VOL_LengthCounterHalt: 5,  // Length counter halt/envelope loop
  APU_SQ1_VOL_ConstantVolume: 4,  // Constant volume
  APU_SQ1_VOL_Volume: 0,  // Volume/envelope period
  APU_SQ1_SWEEP: 0x00004001,
  APU_SQ1_SWEEP_Enabled: 7,  // Sweep enabled
  APU_SQ1_SWEEP_Period: 4,  // Sweep period
  APU_SQ1_SWEEP_Negate: 3,  // Sweep negate
  APU_SQ1_SWEEP_Shift: 0,  // Sweep shift amount
  APU_SQ1_LO: 0x00004002,
  APU_SQ1_HI: 0x00004003,
  APU_SQ1_HI_LengthCounter: 3,  // Length counter load
  APU_SQ1_HI_TimerHigh: 0,  // Timer high bits
  APU_SQ2_VOL: 0x00004004,
  APU_SQ2_SWEEP: 0x00004005,
  APU_SQ2_LO: 0x00004006,
  APU_SQ2_HI: 0x00004007,
  APU_TRI_LINEAR: 0x00004008,
  APU_TRI_LINEAR_Control: 7,  // Length counter halt/linear counter control
  APU_TRI_LINEAR_Period: 0,  // Linear counter load
  APU_TRI_LO: 0x0000400A,
  APU_TRI_HI: 0x0000400B,
  APU_NOISE_VOL: 0x0000400C,
  APU_NOISE_LO: 0x0000400E,
  APU_NOISE_LO_Mode: 7,  // Noise mode
  APU_NOISE_LO_Period: 0,  // Noise period
  APU_NOISE_HI: 0x0000400F,
  APU_DMC_FREQ: 0x00004010,
  APU_DMC_FREQ_IRQ: 7,  // IRQ enable
  APU_DMC_FREQ_Loop: 6,  // Loop flag
  APU_DMC_FREQ_Frequency: 0,  // Frequency index
  APU_DMC_RAW: 0x00004011,
  APU_DMC_START: 0x00004012,
  APU_DMC_LEN: 0x00004013,
  APU_OAMDMA: 0x00004014,
  APU_APUSTATUS: 0x00004015,
  APU_APUSTATUS_DMCInterrupt: 7,  // DMC interrupt flag
  APU_APUSTATUS_FrameInterrupt: 6,  // Frame interrupt flag
  APU_APUSTATUS_DMCEnabled: 4,  // DMC enabled
  APU_APUSTATUS_NoiseEnabled: 3,  // Noise enabled
  APU_APUSTATUS_TriangleEnabled: 2,  // Triangle enabled
  APU_APUSTATUS_Square2Enabled: 1,  // Square 2 enabled
  APU_APUSTATUS_Square1Enabled: 0,  // Square 1 enabled
  APU_APUFRAME: 0x00004017,
  APU_APUFRAME_Mode: 7,  // Frame counter mode
  APU_APUFRAME_IRQInhibit: 6,  // IRQ inhibit
  // Controller Interface
  Controller_BASE: ,
  Controller_JOY1: 0x00004016,
  Controller_JOY1_A: 7,  // A button
  Controller_JOY1_B: 6,  // B button
  Controller_JOY1_Select: 5,  // Select button
  Controller_JOY1_Start: 4,  // Start button
  Controller_JOY1_Up: 3,  // Up direction
  Controller_JOY1_Down: 2,  // Down direction
  Controller_JOY1_Left: 1,  // Left direction
  Controller_JOY1_Right: 0,  // Right direction
  Controller_JOY2: 0x00004017,
  // Memory Mapper (Cartridge)
  Mapper_BASE: ,
  Mapper_PRGROM: 0x00000000,
  Mapper_CHRROM: 0x00000000,
  Mapper_PRGRAM: 0x00000000,
  Mapper_CHRRAM: 0x00000000,

  // 中断向量
  IRQ_NMI: 65530,  // Non-maskable interrupt (VBlank)
  IRQ_RESET: 65532,  // Reset vector
  IRQ_IRQ: 65534,  // Interrupt request

  init: function() {
    // 硬件初始化
  }
};
