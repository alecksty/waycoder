! Sega-Genesis 设备定义 - Fortran 模块
! 生成自: Sega/Genesis/Mega Drive/Sega-Genesis
! 版本: 1.0
! 日期: 2026-04-17
! 作者: VML Team
! 描述: Sega Genesis/Mega Drive 16-bit video game console with Motorola 68000 CPU
! CPU架构: Motorola 68000
! 位宽: 32位
! 时钟频率: 7670000 Hz

module sega_genesis_device
  implicit none

  ! 寄存器地址定义
  integer, parameter :: D0_ADDR = 0  ! Data Register 0
  integer, parameter :: D1_ADDR = 0  ! Data Register 1
  integer, parameter :: D2_ADDR = 0  ! Data Register 2
  integer, parameter :: D3_ADDR = 0  ! Data Register 3
  integer, parameter :: D4_ADDR = 0  ! Data Register 4
  integer, parameter :: D5_ADDR = 0  ! Data Register 5
  integer, parameter :: D6_ADDR = 0  ! Data Register 6
  integer, parameter :: D7_ADDR = 0  ! Data Register 7
  integer, parameter :: A0_ADDR = 0  ! Address Register 0
  integer, parameter :: A1_ADDR = 0  ! Address Register 1
  integer, parameter :: A2_ADDR = 0  ! Address Register 2
  integer, parameter :: A3_ADDR = 0  ! Address Register 3
  integer, parameter :: A4_ADDR = 0  ! Address Register 4
  integer, parameter :: A5_ADDR = 0  ! Address Register 5
  integer, parameter :: A6_ADDR = 0  ! Address Register 6
  integer, parameter :: A7_ADDR = 0  ! Address Register 7 (SP)
  integer, parameter :: PC_ADDR = 0  ! Program Counter
  integer, parameter :: SR_ADDR = 0  ! Status Register

  ! 外设定义
  ! Video Display Processor (315-5313)
  integer, parameter :: VDP_BASE = 
  integer, parameter :: VDP_VDP_DATA_ADDR = 0xC00000
  integer, parameter :: VDP_VDP_CONTROL_ADDR = 0xC00004
  integer, parameter :: VDP_VDP_HVCOUNTER_ADDR = 0xC00008
  integer, parameter :: VDP_VDP_PSG_ADDR = 0xC00011
  ! FM synthesis sound chip
  integer, parameter :: YM2612_BASE = 
  integer, parameter :: YM2612_YM2612_ADDR0_ADDR = 0xA04000
  integer, parameter :: YM2612_YM2612_DATA0_ADDR = 0xA04001
  integer, parameter :: YM2612_YM2612_ADDR1_ADDR = 0xA04002
  integer, parameter :: YM2612_YM2612_DATA1_ADDR = 0xA04003
  ! I/O ports
  integer, parameter :: IOPORTS_BASE = 
  integer, parameter :: IOPORTS_IO_DATA1_ADDR = 0xA10002
  integer, parameter :: IOPORTS_IO_DATA2_ADDR = 0xA10004
  integer, parameter :: IOPORTS_IO_DATA3_ADDR = 0xA10006
  integer, parameter :: IOPORTS_IO_CTRL1_ADDR = 0xA10008
  integer, parameter :: IOPORTS_IO_CTRL2_ADDR = 0xA1000A
  integer, parameter :: IOPORTS_IO_CTRL3_ADDR = 0xA1000C
  ! TradeMark Security System
  integer, parameter :: TMSS_BASE = 
  integer, parameter :: TMSS_TMSS_ADDR = 0xA14000
  ! Z80 bus control
  integer, parameter :: Z80BUS_BASE = 
  integer, parameter :: Z80BUS_Z80_BUSREQ_ADDR = 0xA11100
  integer, parameter :: Z80BUS_Z80_RESET_ADDR = 0xA11200
  integer, parameter :: Z80BUS_Z80_YM2612_ADDR = 0xA04000

  ! 中断向量定义
  integer, parameter :: INT_RESET_SP = 0  ! Reset (Initial SP)
  integer, parameter :: INT_RESET_PC = 4  ! Reset (Initial PC)
  integer, parameter :: INT_HBLANK = 24  ! Horizontal blank interrupt
  integer, parameter :: INT_VBLANK = 28  ! Vertical blank interrupt
  integer, parameter :: INT_EXTINT1 = 32  ! External interrupt 1
  integer, parameter :: INT_EXTINT2 = 36  ! External interrupt 2
  integer, parameter :: INT_EXTINT3 = 40  ! External interrupt 3
  integer, parameter :: INT_EXTINT4 = 44  ! External interrupt 4
  integer, parameter :: INT_EXTINT5 = 48  ! External interrupt 5
  integer, parameter :: INT_EXTINT6 = 52  ! External interrupt 6
  integer, parameter :: INT_EXTINT7 = 56  ! External interrupt 7

end module sega_genesis_device
