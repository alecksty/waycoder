! ZX-Spectrum 设备定义 - Fortran 模块
! 生成自: Sinclair Research/ZX Spectrum/ZX-Spectrum
! 版本: 1.0
! 日期: 2026-04-17
! 作者: VML Team
! 描述: ZX Spectrum 48K home computer with Z80 CPU, 48KB RAM, and color graphics
! CPU架构: Zilog Z80
! 位宽: 8位
! 时钟频率: 3500000 Hz

module zx_spectrum_device
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
  integer, parameter :: AF_ADDR = 0  ! Alternate AF
  integer, parameter :: BC_ADDR = 0  ! Alternate BC
  integer, parameter :: DE_ADDR = 0  ! Alternate DE
  integer, parameter :: HL_ADDR = 0  ! Alternate HL

  ! 外设定义
  ! Uncommitted Logic Array (video and I/O)
  integer, parameter :: ULA_BASE = 
  integer, parameter :: ULA_ULA_PORT_FE_ADDR = 0xFE
  integer, parameter :: ULA_ULA_BORDER_ADDR = 0xFE
  integer, parameter :: ULA_ULA_BEEPER_ADDR = 0xFE
  integer, parameter :: ULA_ULA_MIC_ADDR = 0xFE
  ! General Instruments AY-3-8912 sound chip
  integer, parameter :: AY_3_8912_BASE = 
  integer, parameter :: AY_3_8912_AY_REG_SEL_ADDR = 0xFFFD
  integer, parameter :: AY_3_8912_AY_DATA_ADDR = 0xBFFD
  integer, parameter :: AY_3_8912_AY_READ_ADDR = 0xFFFD
  ! 40-key rubber keyboard
  integer, parameter :: KEYBOARD_BASE = 
  integer, parameter :: KEYBOARD_KEY_ROW0_ADDR = 0xFEFE
  integer, parameter :: KEYBOARD_KEY_ROW1_ADDR = 0xFDFE
  integer, parameter :: KEYBOARD_KEY_ROW2_ADDR = 0xFBFE
  integer, parameter :: KEYBOARD_KEY_ROW3_ADDR = 0xF7FE
  integer, parameter :: KEYBOARD_KEY_ROW4_ADDR = 0xEFFE
  integer, parameter :: KEYBOARD_KEY_ROW5_ADDR = 0xDFFE
  integer, parameter :: KEYBOARD_KEY_ROW6_ADDR = 0xBFFE
  integer, parameter :: KEYBOARD_KEY_ROW7_ADDR = 0x7FFE
  ! Kempston joystick interface
  integer, parameter :: KEMPSTON_BASE = 
  integer, parameter :: KEMPSTON_KEMPSTON_JOY_ADDR = 0x1F
  ! ZX Interface 1 (RS-232 and Microdrive)
  integer, parameter :: INTERFACE1_BASE = 
  integer, parameter :: INTERFACE1_IF1_STATUS_ADDR = 0x1FFD
  integer, parameter :: INTERFACE1_IF1_DATA_ADDR = 0x3FFD
  ! ZX Interface 2 (joystick and ROM cartridge)
  integer, parameter :: INTERFACE2_BASE = 
  integer, parameter :: INTERFACE2_IF2_JOY1_ADDR = 0x1F
  integer, parameter :: INTERFACE2_IF2_JOY2_ADDR = 0x37

  ! 中断向量定义
  integer, parameter :: INT_IM1 = 56  ! Interrupt Mode 1
  integer, parameter :: INT_RST_00 = 0  ! Restart 00h
  integer, parameter :: INT_RST_08 = 8  ! Restart 08h
  integer, parameter :: INT_RST_10 = 16  ! Restart 10h
  integer, parameter :: INT_RST_18 = 24  ! Restart 18h
  integer, parameter :: INT_RST_20 = 32  ! Restart 20h
  integer, parameter :: INT_RST_28 = 40  ! Restart 28h
  integer, parameter :: INT_RST_30 = 48  ! Restart 30h
  integer, parameter :: INT_RST_38 = 56  ! Restart 38h

end module zx_spectrum_device
