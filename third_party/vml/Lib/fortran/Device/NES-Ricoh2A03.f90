! Ricoh-2A03 设备定义 - Fortran 模块
! 生成自: Ricoh/MOS-6502/Ricoh-2A03
! 版本: 1.0
! 日期: 2026-04-16
! 作者: VML Team
! 描述: NES (Famicom) main processor - 8-bit MOS 6502 variant with audio/video support
! CPU架构: MOS-6502
! 位宽: 8位
! 时钟频率: 10765930 Hz

module ricoh_2a03_device
  implicit none

  ! 寄存器地址定义
  integer, parameter :: A_ADDR = 0x00  ! Accumulator
  integer, parameter :: X_ADDR = 0x01  ! X Index
  integer, parameter :: Y_ADDR = 0x02  ! Y Index
  integer, parameter :: SP_ADDR = 0x03  ! Stack Pointer
  integer, parameter :: PC_ADDR = 0x04  ! Program Counter (16-bit)
  integer, parameter :: P_ADDR = 0x06  ! Processor Status
  integer, parameter :: P_C_BIT = 0  ! Carry
  integer, parameter :: P_Z_BIT = 1  ! Zero
  integer, parameter :: P_I_BIT = 2  ! Interrupt Disable
  integer, parameter :: P_D_BIT = 3  ! Decimal Mode
  integer, parameter :: P_B_BIT = 4  ! Break
  integer, parameter :: P_U_BIT = 5  ! Unused
  integer, parameter :: P_V_BIT = 6  ! Overflow
  integer, parameter :: P_N_BIT = 7  ! Negative

  ! 内存段定义
  integer, parameter :: CPU_RAM_START = 0x0000
  integer, parameter :: CPU_RAM_END = 0x07FF
  integer, parameter :: CPU_RAM_SIZE = 2048  ! CPU 2KB RAM (mirrored)
  integer, parameter :: PPU_REGISTERS_START = 0x2000
  integer, parameter :: PPU_REGISTERS_END = 0x3FFF
  integer, parameter :: PPU_REGISTERS_SIZE = 8192  ! PPU Registers (mirrored every 8 bytes)
  integer, parameter :: APU_REGISTERS_START = 0x4000
  integer, parameter :: APU_REGISTERS_END = 0x401F
  integer, parameter :: APU_REGISTERS_SIZE = 32  ! APU and I/O Registers
  integer, parameter :: EXPANSION_START = 0x4020
  integer, parameter :: EXPANSION_END = 0x5FFF
  integer, parameter :: EXPANSION_SIZE = 8160  ! Expansion ROM
  integer, parameter :: SRAM_START = 0x6000
  integer, parameter :: SRAM_END = 0x7FFF
  integer, parameter :: SRAM_SIZE = 8192  ! Save RAM
  integer, parameter :: PRG_ROM_LOW_START = 0x8000
  integer, parameter :: PRG_ROM_LOW_END = 0xBFFF
  integer, parameter :: PRG_ROM_LOW_SIZE = 16384  ! PRG ROM Lower Bank (16KB)
  integer, parameter :: PRG_ROM_HIGH_START = 0xC000
  integer, parameter :: PRG_ROM_HIGH_END = 0xFFFF
  integer, parameter :: PRG_ROM_HIGH_SIZE = 16384  ! PRG ROM Higher Bank (16KB)

  ! 外设定义
  ! Picture Processing Unit
  integer, parameter :: PPU_BASE = 0x2000
  integer, parameter :: PPU_PPUCTRL_ADDR = 0x2000
  integer, parameter :: PPU_PPUMASK_ADDR = 0x2001
  integer, parameter :: PPU_PPUSTATUS_ADDR = 0x2002
  integer, parameter :: PPU_OAMADDR_ADDR = 0x2003
  integer, parameter :: PPU_OAMDATA_ADDR = 0x2004
  integer, parameter :: PPU_PPUSCROLL_ADDR = 0x2005
  integer, parameter :: PPU_PPUADDR_ADDR = 0x2006
  integer, parameter :: PPU_PPUDATA_ADDR = 0x2007
  ! Audio Processing Unit
  integer, parameter :: APU_BASE = 0x4000
  integer, parameter :: APU_PULSE1_VOL_ADDR = 0x4000
  integer, parameter :: APU_PULSE1_SWEEP_ADDR = 0x4001
  integer, parameter :: APU_PULSE1_LO_ADDR = 0x4002
  integer, parameter :: APU_PULSE1_HI_ADDR = 0x4003
  integer, parameter :: APU_PULSE2_VOL_ADDR = 0x4004
  integer, parameter :: APU_PULSE2_SWEEP_ADDR = 0x4005
  integer, parameter :: APU_PULSE2_LO_ADDR = 0x4006
  integer, parameter :: APU_PULSE2_HI_ADDR = 0x4007
  integer, parameter :: APU_TRIANGLE_ADDR = 0x4008
  integer, parameter :: APU_TRIANGLE_HI_ADDR = 0x400B
  integer, parameter :: APU_NOISE_VOL_ADDR = 0x400C
  integer, parameter :: APU_NOISE_HI_ADDR = 0x400E
  integer, parameter :: APU_NOISE_LENGTH_ADDR = 0x400F
  integer, parameter :: APU_DMC_RATE_ADDR = 0x4010
  integer, parameter :: APU_DMC_RAW_ADDR = 0x4011
  integer, parameter :: APU_DMC_START_ADDR = 0x4012
  integer, parameter :: APU_DMC_LENGTH_ADDR = 0x4013
  integer, parameter :: APU_OAMDMA_ADDR = 0x4014
  integer, parameter :: APU_SNDCHN_ADDR = 0x4015
  integer, parameter :: APU_JOY1_ADDR = 0x4016
  integer, parameter :: APU_JOY2_ADDR = 0x4017
  ! Controller Port 1
  integer, parameter :: INPUT1_BASE = 0x4016
  integer, parameter :: INPUT1_JOYPAD1_ADDR = 0x4016
  ! Controller Port 2
  integer, parameter :: INPUT2_BASE = 0x4017
  integer, parameter :: INPUT2_JOYPAD2_ADDR = 0x4017

  ! 中断向量定义
  integer, parameter :: INT_RESET = 0  ! Reset
  integer, parameter :: INT_NMI = 1  ! Non-Maskable Interrupt (VBlank)
  integer, parameter :: INT_IRQ = 2  ! IRQ / BRK

end module ricoh_2a03_device
