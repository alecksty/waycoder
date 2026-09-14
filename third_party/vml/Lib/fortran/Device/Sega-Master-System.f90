! Sega-Master-System 设备定义 - Fortran 模块
! 生成自: Sega/Master System/Sega-Master-System
! 版本: 1.0
! 日期: 2026-04-17
! 作者: VML Team
! 描述: Sega Master System 8-bit video game console with Z80 CPU
! CPU架构: Zilog Z80
! 位宽: 8位
! 时钟频率: 3579545 Hz

module sega_master_system_device
  implicit none

  ! 寄存器地址定义
  integer, parameter :: A_ADDR = 0  ! Accumulator
  integer, parameter :: F_ADDR = 0  ! Flags
  integer, parameter :: B_ADDR = 0  ! B
  integer, parameter :: C_ADDR = 0  ! C
  integer, parameter :: D_ADDR = 0  ! D
  integer, parameter :: E_ADDR = 0  ! E
  integer, parameter :: H_ADDR = 0  ! H
  integer, parameter :: L_ADDR = 0  ! L
  integer, parameter :: IX_ADDR = 0  ! Index Register X
  integer, parameter :: IY_ADDR = 0  ! Index Register Y
  integer, parameter :: SP_ADDR = 0  ! Stack Pointer
  integer, parameter :: PC_ADDR = 0  ! Program Counter
  integer, parameter :: I_ADDR = 0  ! Interrupt Vector
  integer, parameter :: R_ADDR = 0  ! Memory Refresh

  ! 外设定义
  ! Video Display Processor (TMS9918A)
  integer, parameter :: VDP_BASE = 
  integer, parameter :: VDP_VDP_DATA_ADDR = 0xBE
  integer, parameter :: VDP_VDP_ADDR_ADDR = 0xBF
  integer, parameter :: VDP_VDP_STATUS_ADDR = 0xBF
  ! Programmable Sound Generator (SN76489)
  integer, parameter :: PSG_BASE = 
  integer, parameter :: PSG_PSG_DATA_ADDR = 0x7F
  ! I/O ports
  integer, parameter :: IO_BASE = 
  integer, parameter :: IO_IO_PORT_A_ADDR = 0xDC
  integer, parameter :: IO_IO_PORT_B_ADDR = 0xDD
  integer, parameter :: IO_IO_PORT_MISC_ADDR = 0xDE
  integer, parameter :: IO_IO_PORT_VDP_ADDR = 0xDF
  ! Memory mapper
  integer, parameter :: MEMORYMAPPER_BASE = 
  integer, parameter :: MEMORYMAPPER_MAPPER_0_ADDR = 0xFFFC
  integer, parameter :: MEMORYMAPPER_MAPPER_1_ADDR = 0xFFFD
  integer, parameter :: MEMORYMAPPER_MAPPER_2_ADDR = 0xFFFE
  integer, parameter :: MEMORYMAPPER_MAPPER_3_ADDR = 0xFFFF
  ! FM Sound Unit (optional)
  integer, parameter :: FMUNIT_BASE = 
  integer, parameter :: FMUNIT_FM_ADDR_ADDR = 0xF0
  integer, parameter :: FMUNIT_FM_DATA_ADDR = 0xF1
  integer, parameter :: FMUNIT_FM_DETECT_ADDR = 0xF2

  ! 中断向量定义
  integer, parameter :: INT_RST_00 = 0  ! Restart 00h
  integer, parameter :: INT_IM1 = 56  ! Interrupt Mode 1
  integer, parameter :: INT_VBLANK = 56  ! Vertical blank interrupt
  integer, parameter :: INT_LINE = 100  ! Line interrupt

end module sega_master_system_device
