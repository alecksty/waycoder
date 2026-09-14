// Nintendo Entertainment System 设备定义 - Dart 库
// 生成自: Nintendo/NES/Nintendo Entertainment System
// 版本: 
// 日期: 
// 作者: 
// 描述: Nintendo Entertainment System (NES/Famicom) 8-bit video game console
// CPU架构: 6502
// 位宽: 0位
// 时钟频率: 0 Hz

class Nintendo Entertainment SystemDevice {
  static const String deviceName = "Nintendo Entertainment System";
  static const String manufacturer = "Nintendo";
  static const String family = "NES";
  static const String version = "";
  static const String architecture = "6502";
  static const int bits = 0;
  static const int clockFrequency = 0;

  // 外设定义
  // Picture Processing Unit (Ricoh 2C02)
  static const int PPU_BASE = ;
  static const int PPU_PPUCTRL_ADDR = 0x2000;
  static const int PPU_PPUCTRL_NMI_BIT = 7;  // VBlank NMI enable
  static const int PPU_PPUCTRL_MASTERSLAVE_BIT = 6;  // Master/slave select
  static const int PPU_PPUCTRL_SPRITESIZE_BIT = 5;  // Sprite size (0=8x8, 1=8x16)
  static const int PPU_PPUCTRL_BGPATTERN_BIT = 4;  // Background pattern table address
  static const int PPU_PPUCTRL_SPRITEPATTERN_BIT = 3;  // Sprite pattern table address
  static const int PPU_PPUCTRL_VRAMINCREMENT_BIT = 2;  // VRAM address increment (0=1, 1=32)
  static const int PPU_PPUCTRL_NAMETABLE_BIT = 0;  // Nametable address
  static const int PPU_PPUMASK_ADDR = 0x2001;
  static const int PPU_PPUMASK_EMPHASIZEBLUE_BIT = 7;  // Emphasize blue
  static const int PPU_PPUMASK_EMPHASIZEGREEN_BIT = 6;  // Emphasize green
  static const int PPU_PPUMASK_EMPHASIZERED_BIT = 5;  // Emphasize red
  static const int PPU_PPUMASK_SHOWSPRITES_BIT = 4;  // Show sprites
  static const int PPU_PPUMASK_SHOWBACKGROUND_BIT = 3;  // Show background
  static const int PPU_PPUMASK_SHOWLEFTSPRITES_BIT = 2;  // Show sprites in left 8 pixels
  static const int PPU_PPUMASK_SHOWLEFTBACKGROUND_BIT = 1;  // Show background in left 8 pixels
  static const int PPU_PPUMASK_GRAYSCALE_BIT = 0;  // Grayscale mode
  static const int PPU_PPUSTATUS_ADDR = 0x2002;
  static const int PPU_PPUSTATUS_VBLANK_BIT = 7;  // VBlank started
  static const int PPU_PPUSTATUS_SPRITE0HIT_BIT = 6;  // Sprite 0 hit
  static const int PPU_PPUSTATUS_SPRITEOVERFLOW_BIT = 5;  // Sprite overflow
  static const int PPU_OAMADDR_ADDR = 0x2003;
  static const int PPU_OAMDATA_ADDR = 0x2004;
  static const int PPU_PPUSCROLL_ADDR = 0x2005;
  static const int PPU_PPUADDR_ADDR = 0x2006;
  static const int PPU_PPUDATA_ADDR = 0x2007;
  static const int PPU_OAMDMA_ADDR = 0x4014;
  // Audio Processing Unit (Ricoh 2A03)
  static const int APU_BASE = ;
  static const int APU_SQ1_VOL_ADDR = 0x4000;
  static const int APU_SQ1_VOL_DUTY_BIT = 6;  // Duty cycle
  static const int APU_SQ1_VOL_LENGTHCOUNTERHALT_BIT = 5;  // Length counter halt/envelope loop
  static const int APU_SQ1_VOL_CONSTANTVOLUME_BIT = 4;  // Constant volume
  static const int APU_SQ1_VOL_VOLUME_BIT = 0;  // Volume/envelope period
  static const int APU_SQ1_SWEEP_ADDR = 0x4001;
  static const int APU_SQ1_SWEEP_ENABLED_BIT = 7;  // Sweep enabled
  static const int APU_SQ1_SWEEP_PERIOD_BIT = 4;  // Sweep period
  static const int APU_SQ1_SWEEP_NEGATE_BIT = 3;  // Sweep negate
  static const int APU_SQ1_SWEEP_SHIFT_BIT = 0;  // Sweep shift amount
  static const int APU_SQ1_LO_ADDR = 0x4002;
  static const int APU_SQ1_HI_ADDR = 0x4003;
  static const int APU_SQ1_HI_LENGTHCOUNTER_BIT = 3;  // Length counter load
  static const int APU_SQ1_HI_TIMERHIGH_BIT = 0;  // Timer high bits
  static const int APU_SQ2_VOL_ADDR = 0x4004;
  static const int APU_SQ2_SWEEP_ADDR = 0x4005;
  static const int APU_SQ2_LO_ADDR = 0x4006;
  static const int APU_SQ2_HI_ADDR = 0x4007;
  static const int APU_TRI_LINEAR_ADDR = 0x4008;
  static const int APU_TRI_LINEAR_CONTROL_BIT = 7;  // Length counter halt/linear counter control
  static const int APU_TRI_LINEAR_PERIOD_BIT = 0;  // Linear counter load
  static const int APU_TRI_LO_ADDR = 0x400A;
  static const int APU_TRI_HI_ADDR = 0x400B;
  static const int APU_NOISE_VOL_ADDR = 0x400C;
  static const int APU_NOISE_LO_ADDR = 0x400E;
  static const int APU_NOISE_LO_MODE_BIT = 7;  // Noise mode
  static const int APU_NOISE_LO_PERIOD_BIT = 0;  // Noise period
  static const int APU_NOISE_HI_ADDR = 0x400F;
  static const int APU_DMC_FREQ_ADDR = 0x4010;
  static const int APU_DMC_FREQ_IRQ_BIT = 7;  // IRQ enable
  static const int APU_DMC_FREQ_LOOP_BIT = 6;  // Loop flag
  static const int APU_DMC_FREQ_FREQUENCY_BIT = 0;  // Frequency index
  static const int APU_DMC_RAW_ADDR = 0x4011;
  static const int APU_DMC_START_ADDR = 0x4012;
  static const int APU_DMC_LEN_ADDR = 0x4013;
  static const int APU_OAMDMA_ADDR = 0x4014;
  static const int APU_APUSTATUS_ADDR = 0x4015;
  static const int APU_APUSTATUS_DMCINTERRUPT_BIT = 7;  // DMC interrupt flag
  static const int APU_APUSTATUS_FRAMEINTERRUPT_BIT = 6;  // Frame interrupt flag
  static const int APU_APUSTATUS_DMCENABLED_BIT = 4;  // DMC enabled
  static const int APU_APUSTATUS_NOISEENABLED_BIT = 3;  // Noise enabled
  static const int APU_APUSTATUS_TRIANGLEENABLED_BIT = 2;  // Triangle enabled
  static const int APU_APUSTATUS_SQUARE2ENABLED_BIT = 1;  // Square 2 enabled
  static const int APU_APUSTATUS_SQUARE1ENABLED_BIT = 0;  // Square 1 enabled
  static const int APU_APUFRAME_ADDR = 0x4017;
  static const int APU_APUFRAME_MODE_BIT = 7;  // Frame counter mode
  static const int APU_APUFRAME_IRQINHIBIT_BIT = 6;  // IRQ inhibit
  // Controller Interface
  static const int CONTROLLER_BASE = ;
  static const int CONTROLLER_JOY1_ADDR = 0x4016;
  static const int CONTROLLER_JOY1_A_BIT = 7;  // A button
  static const int CONTROLLER_JOY1_B_BIT = 6;  // B button
  static const int CONTROLLER_JOY1_SELECT_BIT = 5;  // Select button
  static const int CONTROLLER_JOY1_START_BIT = 4;  // Start button
  static const int CONTROLLER_JOY1_UP_BIT = 3;  // Up direction
  static const int CONTROLLER_JOY1_DOWN_BIT = 2;  // Down direction
  static const int CONTROLLER_JOY1_LEFT_BIT = 1;  // Left direction
  static const int CONTROLLER_JOY1_RIGHT_BIT = 0;  // Right direction
  static const int CONTROLLER_JOY2_ADDR = 0x4017;
  // Memory Mapper (Cartridge)
  static const int MAPPER_BASE = ;
  static const int MAPPER_PRGROM_ADDR = 0;
  static const int MAPPER_CHRROM_ADDR = 0;
  static const int MAPPER_PRGRAM_ADDR = 0;
  static const int MAPPER_CHRRAM_ADDR = 0;

  // 中断向量定义
  static const int INT_NMI = 65530;  // Non-maskable interrupt (VBlank)
  static const int INT_RESET = 65532;  // Reset vector
  static const int INT_IRQ = 65534;  // Interrupt request

}
