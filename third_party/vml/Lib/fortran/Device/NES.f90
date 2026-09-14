! Nintendo Entertainment System 设备定义 - Fortran 模块
! 生成自: Nintendo/NES/Nintendo Entertainment System
! 版本: 
! 日期: 
! 作者: 
! 描述: Nintendo Entertainment System (NES/Famicom) 8-bit video game console
! CPU架构: 6502
! 位宽: 0位
! 时钟频率: 0 Hz

module nintendo entertainment system_device
  implicit none

  ! 外设定义
  ! Picture Processing Unit (Ricoh 2C02)
  integer, parameter :: PPU_BASE = 
  integer, parameter :: PPU_PPUCTRL_ADDR = 0x2000
  integer, parameter :: PPU_PPUCTRL_NMI_BIT = 7  ! VBlank NMI enable
  integer, parameter :: PPU_PPUCTRL_MASTERSLAVE_BIT = 6  ! Master/slave select
  integer, parameter :: PPU_PPUCTRL_SPRITESIZE_BIT = 5  ! Sprite size (0=8x8, 1=8x16)
  integer, parameter :: PPU_PPUCTRL_BGPATTERN_BIT = 4  ! Background pattern table address
  integer, parameter :: PPU_PPUCTRL_SPRITEPATTERN_BIT = 3  ! Sprite pattern table address
  integer, parameter :: PPU_PPUCTRL_VRAMINCREMENT_BIT = 2  ! VRAM address increment (0=1, 1=32)
  integer, parameter :: PPU_PPUCTRL_NAMETABLE_BIT = 0  ! Nametable address
  integer, parameter :: PPU_PPUMASK_ADDR = 0x2001
  integer, parameter :: PPU_PPUMASK_EMPHASIZEBLUE_BIT = 7  ! Emphasize blue
  integer, parameter :: PPU_PPUMASK_EMPHASIZEGREEN_BIT = 6  ! Emphasize green
  integer, parameter :: PPU_PPUMASK_EMPHASIZERED_BIT = 5  ! Emphasize red
  integer, parameter :: PPU_PPUMASK_SHOWSPRITES_BIT = 4  ! Show sprites
  integer, parameter :: PPU_PPUMASK_SHOWBACKGROUND_BIT = 3  ! Show background
  integer, parameter :: PPU_PPUMASK_SHOWLEFTSPRITES_BIT = 2  ! Show sprites in left 8 pixels
  integer, parameter :: PPU_PPUMASK_SHOWLEFTBACKGROUND_BIT = 1  ! Show background in left 8 pixels
  integer, parameter :: PPU_PPUMASK_GRAYSCALE_BIT = 0  ! Grayscale mode
  integer, parameter :: PPU_PPUSTATUS_ADDR = 0x2002
  integer, parameter :: PPU_PPUSTATUS_VBLANK_BIT = 7  ! VBlank started
  integer, parameter :: PPU_PPUSTATUS_SPRITE0HIT_BIT = 6  ! Sprite 0 hit
  integer, parameter :: PPU_PPUSTATUS_SPRITEOVERFLOW_BIT = 5  ! Sprite overflow
  integer, parameter :: PPU_OAMADDR_ADDR = 0x2003
  integer, parameter :: PPU_OAMDATA_ADDR = 0x2004
  integer, parameter :: PPU_PPUSCROLL_ADDR = 0x2005
  integer, parameter :: PPU_PPUADDR_ADDR = 0x2006
  integer, parameter :: PPU_PPUDATA_ADDR = 0x2007
  integer, parameter :: PPU_OAMDMA_ADDR = 0x4014
  ! Audio Processing Unit (Ricoh 2A03)
  integer, parameter :: APU_BASE = 
  integer, parameter :: APU_SQ1_VOL_ADDR = 0x4000
  integer, parameter :: APU_SQ1_VOL_DUTY_BIT = 6  ! Duty cycle
  integer, parameter :: APU_SQ1_VOL_LENGTHCOUNTERHALT_BIT = 5  ! Length counter halt/envelope loop
  integer, parameter :: APU_SQ1_VOL_CONSTANTVOLUME_BIT = 4  ! Constant volume
  integer, parameter :: APU_SQ1_VOL_VOLUME_BIT = 0  ! Volume/envelope period
  integer, parameter :: APU_SQ1_SWEEP_ADDR = 0x4001
  integer, parameter :: APU_SQ1_SWEEP_ENABLED_BIT = 7  ! Sweep enabled
  integer, parameter :: APU_SQ1_SWEEP_PERIOD_BIT = 4  ! Sweep period
  integer, parameter :: APU_SQ1_SWEEP_NEGATE_BIT = 3  ! Sweep negate
  integer, parameter :: APU_SQ1_SWEEP_SHIFT_BIT = 0  ! Sweep shift amount
  integer, parameter :: APU_SQ1_LO_ADDR = 0x4002
  integer, parameter :: APU_SQ1_HI_ADDR = 0x4003
  integer, parameter :: APU_SQ1_HI_LENGTHCOUNTER_BIT = 3  ! Length counter load
  integer, parameter :: APU_SQ1_HI_TIMERHIGH_BIT = 0  ! Timer high bits
  integer, parameter :: APU_SQ2_VOL_ADDR = 0x4004
  integer, parameter :: APU_SQ2_SWEEP_ADDR = 0x4005
  integer, parameter :: APU_SQ2_LO_ADDR = 0x4006
  integer, parameter :: APU_SQ2_HI_ADDR = 0x4007
  integer, parameter :: APU_TRI_LINEAR_ADDR = 0x4008
  integer, parameter :: APU_TRI_LINEAR_CONTROL_BIT = 7  ! Length counter halt/linear counter control
  integer, parameter :: APU_TRI_LINEAR_PERIOD_BIT = 0  ! Linear counter load
  integer, parameter :: APU_TRI_LO_ADDR = 0x400A
  integer, parameter :: APU_TRI_HI_ADDR = 0x400B
  integer, parameter :: APU_NOISE_VOL_ADDR = 0x400C
  integer, parameter :: APU_NOISE_LO_ADDR = 0x400E
  integer, parameter :: APU_NOISE_LO_MODE_BIT = 7  ! Noise mode
  integer, parameter :: APU_NOISE_LO_PERIOD_BIT = 0  ! Noise period
  integer, parameter :: APU_NOISE_HI_ADDR = 0x400F
  integer, parameter :: APU_DMC_FREQ_ADDR = 0x4010
  integer, parameter :: APU_DMC_FREQ_IRQ_BIT = 7  ! IRQ enable
  integer, parameter :: APU_DMC_FREQ_LOOP_BIT = 6  ! Loop flag
  integer, parameter :: APU_DMC_FREQ_FREQUENCY_BIT = 0  ! Frequency index
  integer, parameter :: APU_DMC_RAW_ADDR = 0x4011
  integer, parameter :: APU_DMC_START_ADDR = 0x4012
  integer, parameter :: APU_DMC_LEN_ADDR = 0x4013
  integer, parameter :: APU_OAMDMA_ADDR = 0x4014
  integer, parameter :: APU_APUSTATUS_ADDR = 0x4015
  integer, parameter :: APU_APUSTATUS_DMCINTERRUPT_BIT = 7  ! DMC interrupt flag
  integer, parameter :: APU_APUSTATUS_FRAMEINTERRUPT_BIT = 6  ! Frame interrupt flag
  integer, parameter :: APU_APUSTATUS_DMCENABLED_BIT = 4  ! DMC enabled
  integer, parameter :: APU_APUSTATUS_NOISEENABLED_BIT = 3  ! Noise enabled
  integer, parameter :: APU_APUSTATUS_TRIANGLEENABLED_BIT = 2  ! Triangle enabled
  integer, parameter :: APU_APUSTATUS_SQUARE2ENABLED_BIT = 1  ! Square 2 enabled
  integer, parameter :: APU_APUSTATUS_SQUARE1ENABLED_BIT = 0  ! Square 1 enabled
  integer, parameter :: APU_APUFRAME_ADDR = 0x4017
  integer, parameter :: APU_APUFRAME_MODE_BIT = 7  ! Frame counter mode
  integer, parameter :: APU_APUFRAME_IRQINHIBIT_BIT = 6  ! IRQ inhibit
  ! Controller Interface
  integer, parameter :: CONTROLLER_BASE = 
  integer, parameter :: CONTROLLER_JOY1_ADDR = 0x4016
  integer, parameter :: CONTROLLER_JOY1_A_BIT = 7  ! A button
  integer, parameter :: CONTROLLER_JOY1_B_BIT = 6  ! B button
  integer, parameter :: CONTROLLER_JOY1_SELECT_BIT = 5  ! Select button
  integer, parameter :: CONTROLLER_JOY1_START_BIT = 4  ! Start button
  integer, parameter :: CONTROLLER_JOY1_UP_BIT = 3  ! Up direction
  integer, parameter :: CONTROLLER_JOY1_DOWN_BIT = 2  ! Down direction
  integer, parameter :: CONTROLLER_JOY1_LEFT_BIT = 1  ! Left direction
  integer, parameter :: CONTROLLER_JOY1_RIGHT_BIT = 0  ! Right direction
  integer, parameter :: CONTROLLER_JOY2_ADDR = 0x4017
  ! Memory Mapper (Cartridge)
  integer, parameter :: MAPPER_BASE = 
  integer, parameter :: MAPPER_PRGROM_ADDR = 0
  integer, parameter :: MAPPER_CHRROM_ADDR = 0
  integer, parameter :: MAPPER_PRGRAM_ADDR = 0
  integer, parameter :: MAPPER_CHRRAM_ADDR = 0

  ! 中断向量定义
  integer, parameter :: INT_NMI = 65530  ! Non-maskable interrupt (VBlank)
  integer, parameter :: INT_RESET = 65532  ! Reset vector
  integer, parameter :: INT_IRQ = 65534  ! Interrupt request

end module nintendo entertainment system_device
