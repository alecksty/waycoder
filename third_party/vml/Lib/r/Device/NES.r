# Nintendo Entertainment System 设备定义 - R 脚本
# 生成自: Nintendo/NES/Nintendo Entertainment System
# 版本: 
# 日期: 
# 作者: 
# 描述: Nintendo Entertainment System (NES/Famicom) 8-bit video game console
# CPU架构: 6502
# 位宽: 0位
# 时钟频率: 0 Hz

# 外设定义
# Picture Processing Unit (Ricoh 2C02)
PPU_BASE <- 
PPU_PPUCTRL_ADDR <- 0x2000
PPU_PPUCTRL_NMI_BIT <- 7  # VBlank NMI enable
PPU_PPUCTRL_MASTERSLAVE_BIT <- 6  # Master/slave select
PPU_PPUCTRL_SPRITESIZE_BIT <- 5  # Sprite size (0=8x8, 1=8x16)
PPU_PPUCTRL_BGPATTERN_BIT <- 4  # Background pattern table address
PPU_PPUCTRL_SPRITEPATTERN_BIT <- 3  # Sprite pattern table address
PPU_PPUCTRL_VRAMINCREMENT_BIT <- 2  # VRAM address increment (0=1, 1=32)
PPU_PPUCTRL_NAMETABLE_BIT <- 0  # Nametable address
PPU_PPUMASK_ADDR <- 0x2001
PPU_PPUMASK_EMPHASIZEBLUE_BIT <- 7  # Emphasize blue
PPU_PPUMASK_EMPHASIZEGREEN_BIT <- 6  # Emphasize green
PPU_PPUMASK_EMPHASIZERED_BIT <- 5  # Emphasize red
PPU_PPUMASK_SHOWSPRITES_BIT <- 4  # Show sprites
PPU_PPUMASK_SHOWBACKGROUND_BIT <- 3  # Show background
PPU_PPUMASK_SHOWLEFTSPRITES_BIT <- 2  # Show sprites in left 8 pixels
PPU_PPUMASK_SHOWLEFTBACKGROUND_BIT <- 1  # Show background in left 8 pixels
PPU_PPUMASK_GRAYSCALE_BIT <- 0  # Grayscale mode
PPU_PPUSTATUS_ADDR <- 0x2002
PPU_PPUSTATUS_VBLANK_BIT <- 7  # VBlank started
PPU_PPUSTATUS_SPRITE0HIT_BIT <- 6  # Sprite 0 hit
PPU_PPUSTATUS_SPRITEOVERFLOW_BIT <- 5  # Sprite overflow
PPU_OAMADDR_ADDR <- 0x2003
PPU_OAMDATA_ADDR <- 0x2004
PPU_PPUSCROLL_ADDR <- 0x2005
PPU_PPUADDR_ADDR <- 0x2006
PPU_PPUDATA_ADDR <- 0x2007
PPU_OAMDMA_ADDR <- 0x4014
# Audio Processing Unit (Ricoh 2A03)
APU_BASE <- 
APU_SQ1_VOL_ADDR <- 0x4000
APU_SQ1_VOL_DUTY_BIT <- 6  # Duty cycle
APU_SQ1_VOL_LENGTHCOUNTERHALT_BIT <- 5  # Length counter halt/envelope loop
APU_SQ1_VOL_CONSTANTVOLUME_BIT <- 4  # Constant volume
APU_SQ1_VOL_VOLUME_BIT <- 0  # Volume/envelope period
APU_SQ1_SWEEP_ADDR <- 0x4001
APU_SQ1_SWEEP_ENABLED_BIT <- 7  # Sweep enabled
APU_SQ1_SWEEP_PERIOD_BIT <- 4  # Sweep period
APU_SQ1_SWEEP_NEGATE_BIT <- 3  # Sweep negate
APU_SQ1_SWEEP_SHIFT_BIT <- 0  # Sweep shift amount
APU_SQ1_LO_ADDR <- 0x4002
APU_SQ1_HI_ADDR <- 0x4003
APU_SQ1_HI_LENGTHCOUNTER_BIT <- 3  # Length counter load
APU_SQ1_HI_TIMERHIGH_BIT <- 0  # Timer high bits
APU_SQ2_VOL_ADDR <- 0x4004
APU_SQ2_SWEEP_ADDR <- 0x4005
APU_SQ2_LO_ADDR <- 0x4006
APU_SQ2_HI_ADDR <- 0x4007
APU_TRI_LINEAR_ADDR <- 0x4008
APU_TRI_LINEAR_CONTROL_BIT <- 7  # Length counter halt/linear counter control
APU_TRI_LINEAR_PERIOD_BIT <- 0  # Linear counter load
APU_TRI_LO_ADDR <- 0x400A
APU_TRI_HI_ADDR <- 0x400B
APU_NOISE_VOL_ADDR <- 0x400C
APU_NOISE_LO_ADDR <- 0x400E
APU_NOISE_LO_MODE_BIT <- 7  # Noise mode
APU_NOISE_LO_PERIOD_BIT <- 0  # Noise period
APU_NOISE_HI_ADDR <- 0x400F
APU_DMC_FREQ_ADDR <- 0x4010
APU_DMC_FREQ_IRQ_BIT <- 7  # IRQ enable
APU_DMC_FREQ_LOOP_BIT <- 6  # Loop flag
APU_DMC_FREQ_FREQUENCY_BIT <- 0  # Frequency index
APU_DMC_RAW_ADDR <- 0x4011
APU_DMC_START_ADDR <- 0x4012
APU_DMC_LEN_ADDR <- 0x4013
APU_OAMDMA_ADDR <- 0x4014
APU_APUSTATUS_ADDR <- 0x4015
APU_APUSTATUS_DMCINTERRUPT_BIT <- 7  # DMC interrupt flag
APU_APUSTATUS_FRAMEINTERRUPT_BIT <- 6  # Frame interrupt flag
APU_APUSTATUS_DMCENABLED_BIT <- 4  # DMC enabled
APU_APUSTATUS_NOISEENABLED_BIT <- 3  # Noise enabled
APU_APUSTATUS_TRIANGLEENABLED_BIT <- 2  # Triangle enabled
APU_APUSTATUS_SQUARE2ENABLED_BIT <- 1  # Square 2 enabled
APU_APUSTATUS_SQUARE1ENABLED_BIT <- 0  # Square 1 enabled
APU_APUFRAME_ADDR <- 0x4017
APU_APUFRAME_MODE_BIT <- 7  # Frame counter mode
APU_APUFRAME_IRQINHIBIT_BIT <- 6  # IRQ inhibit
# Controller Interface
CONTROLLER_BASE <- 
CONTROLLER_JOY1_ADDR <- 0x4016
CONTROLLER_JOY1_A_BIT <- 7  # A button
CONTROLLER_JOY1_B_BIT <- 6  # B button
CONTROLLER_JOY1_SELECT_BIT <- 5  # Select button
CONTROLLER_JOY1_START_BIT <- 4  # Start button
CONTROLLER_JOY1_UP_BIT <- 3  # Up direction
CONTROLLER_JOY1_DOWN_BIT <- 2  # Down direction
CONTROLLER_JOY1_LEFT_BIT <- 1  # Left direction
CONTROLLER_JOY1_RIGHT_BIT <- 0  # Right direction
CONTROLLER_JOY2_ADDR <- 0x4017
# Memory Mapper (Cartridge)
MAPPER_BASE <- 
MAPPER_PRGROM_ADDR <- 0
MAPPER_CHRROM_ADDR <- 0
MAPPER_PRGRAM_ADDR <- 0
MAPPER_CHRRAM_ADDR <- 0

# 中断向量定义
INT_NMI <- 65530  # Non-maskable interrupt (VBlank)
INT_RESET <- 65532  # Reset vector
INT_IRQ <- 65534  # Interrupt request

