! Macintosh-128K 设备定义 - Fortran 模块
! 生成自: Apple Computer/Macintosh/Macintosh-128K
! 版本: 1.0
! 日期: 2026-04-17
! 作者: VML Team
! 描述: Original Macintosh 128K with Motorola 68000 CPU, 128KB RAM, and 9-inch monochrome display
! CPU架构: Motorola 68000
! 位宽: 32位
! 时钟频率: 7998000 Hz

module macintosh_128k_device
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
  ! Versatile Interface Adapter (6522)
  integer, parameter :: VIA_BASE = 
  integer, parameter :: VIA_VIA_ORB_ADDR = 0xE80000
  integer, parameter :: VIA_VIA_ORA_ADDR = 0xE80001
  integer, parameter :: VIA_VIA_DDRB_ADDR = 0xE80002
  integer, parameter :: VIA_VIA_DDRA_ADDR = 0xE80003
  integer, parameter :: VIA_VIA_T1CL_ADDR = 0xE80004
  integer, parameter :: VIA_VIA_T1CH_ADDR = 0xE80005
  integer, parameter :: VIA_VIA_T1LL_ADDR = 0xE80006
  integer, parameter :: VIA_VIA_T1LH_ADDR = 0xE80007
  integer, parameter :: VIA_VIA_T2CL_ADDR = 0xE80008
  integer, parameter :: VIA_VIA_T2CH_ADDR = 0xE80009
  integer, parameter :: VIA_VIA_SR_ADDR = 0xE8000A
  integer, parameter :: VIA_VIA_ACR_ADDR = 0xE8000B
  integer, parameter :: VIA_VIA_PCR_ADDR = 0xE8000C
  integer, parameter :: VIA_VIA_IFR_ADDR = 0xE8000D
  integer, parameter :: VIA_VIA_IER_ADDR = 0xE8000E
  integer, parameter :: VIA_VIA_ORA2_ADDR = 0xE8000F
  ! Integrated Woz Machine (floppy controller)
  integer, parameter :: IWM_BASE = 
  integer, parameter :: IWM_IWM_Q6_ADDR = 0xD00000
  integer, parameter :: IWM_IWM_Q7_ADDR = 0xD00002
  integer, parameter :: IWM_IWM_PH0_ADDR = 0xD00004
  integer, parameter :: IWM_IWM_PH1_ADDR = 0xD00006
  integer, parameter :: IWM_IWM_PH2_ADDR = 0xD00008
  integer, parameter :: IWM_IWM_PH3_ADDR = 0xD0000A
  ! Zilog 8530 Serial Communications Controller
  integer, parameter :: SCC_BASE = 
  integer, parameter :: SCC_SCC_CA_ADDR = 0x500000
  integer, parameter :: SCC_SCC_DA_ADDR = 0x500002
  integer, parameter :: SCC_SCC_CB_ADDR = 0x500004
  integer, parameter :: SCC_SCC_DB_ADDR = 0x500006
  ! Built-in speaker
  integer, parameter :: SOUND_BASE = 
  integer, parameter :: SOUND_SOUND_VOL_ADDR = 0xE80100
  integer, parameter :: SOUND_SOUND_FREQ_ADDR = 0xE80102

  ! 中断向量定义
  integer, parameter :: INT_RESET_SP = 0  ! Reset (Initial SP)
  integer, parameter :: INT_RESET_PC = 4  ! Reset (Initial PC)
  integer, parameter :: INT_AUTOVECTOR1 = 24  ! Auto vector 1
  integer, parameter :: INT_AUTOVECTOR2 = 25  ! Auto vector 2
  integer, parameter :: INT_AUTOVECTOR3 = 26  ! Auto vector 3
  integer, parameter :: INT_AUTOVECTOR4 = 27  ! Auto vector 4
  integer, parameter :: INT_AUTOVECTOR5 = 28  ! Auto vector 5
  integer, parameter :: INT_AUTOVECTOR6 = 29  ! Auto vector 6
  integer, parameter :: INT_AUTOVECTOR7 = 30  ! Auto vector 7
  integer, parameter :: INT_SPURIOUS = 31  ! Spurious interrupt

end module macintosh_128k_device
