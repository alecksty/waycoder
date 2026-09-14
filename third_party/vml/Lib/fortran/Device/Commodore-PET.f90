! Commodore-PET 设备定义 - Fortran 模块
! 生成自: Commodore International/PET/Commodore-PET
! 版本: 1.0
! 日期: 2026-04-17
! 作者: VML Team
! 描述: Commodore PET 2001 personal computer with MOS 6502 CPU and built-in monitor
! CPU架构: MOS 6502
! 位宽: 8位
! 时钟频率: 1000000 Hz

module commodore_pet_device
  implicit none

  ! 寄存器地址定义
  integer, parameter :: A_ADDR = 0  ! Accumulator
  integer, parameter :: X_ADDR = 0  ! Index Register X
  integer, parameter :: Y_ADDR = 0  ! Index Register Y
  integer, parameter :: SP_ADDR = 0  ! Stack Pointer
  integer, parameter :: PC_ADDR = 0  ! Program Counter
  integer, parameter :: P_ADDR = 0  ! Status Register

  ! 外设定义
  ! Peripheral Interface Adapter 1 (6520)
  integer, parameter :: PIA1_BASE = 
  integer, parameter :: PIA1_PIA1_DDRA_ADDR = 0xE810
  integer, parameter :: PIA1_PIA1_ORA_ADDR = 0xE811
  integer, parameter :: PIA1_PIA1_DDRB_ADDR = 0xE812
  integer, parameter :: PIA1_PIA1_ORB_ADDR = 0xE813
  integer, parameter :: PIA1_PIA1_CRA_ADDR = 0xE814
  integer, parameter :: PIA1_PIA1_CRB_ADDR = 0xE815
  ! Peripheral Interface Adapter 2 (6520)
  integer, parameter :: PIA2_BASE = 
  integer, parameter :: PIA2_PIA2_DDRA_ADDR = 0xE820
  integer, parameter :: PIA2_PIA2_ORA_ADDR = 0xE821
  integer, parameter :: PIA2_PIA2_DDRB_ADDR = 0xE822
  integer, parameter :: PIA2_PIA2_ORB_ADDR = 0xE823
  integer, parameter :: PIA2_PIA2_CRA_ADDR = 0xE824
  integer, parameter :: PIA2_PIA2_CRB_ADDR = 0xE825
  ! Versatile Interface Adapter (6522)
  integer, parameter :: VIA_BASE = 
  integer, parameter :: VIA_VIA_ORB_ADDR = 0xE840
  integer, parameter :: VIA_VIA_ORA_ADDR = 0xE841
  integer, parameter :: VIA_VIA_DDRB_ADDR = 0xE842
  integer, parameter :: VIA_VIA_DDRA_ADDR = 0xE843
  integer, parameter :: VIA_VIA_T1CL_ADDR = 0xE844
  integer, parameter :: VIA_VIA_T1CH_ADDR = 0xE845
  integer, parameter :: VIA_VIA_T1LL_ADDR = 0xE846
  integer, parameter :: VIA_VIA_T1LH_ADDR = 0xE847
  integer, parameter :: VIA_VIA_T2CL_ADDR = 0xE848
  integer, parameter :: VIA_VIA_T2CH_ADDR = 0xE849
  integer, parameter :: VIA_VIA_SR_ADDR = 0xE84A
  integer, parameter :: VIA_VIA_ACR_ADDR = 0xE84B
  integer, parameter :: VIA_VIA_PCR_ADDR = 0xE84C
  integer, parameter :: VIA_VIA_IFR_ADDR = 0xE84D
  integer, parameter :: VIA_VIA_IER_ADDR = 0xE84E
  ! CRT Controller (6545)
  integer, parameter :: CRTC_BASE = 
  integer, parameter :: CRTC_CRTC_ADDR_ADDR = 0xE880
  integer, parameter :: CRTC_CRTC_DATA_ADDR = 0xE881
  ! Cassette tape interface
  integer, parameter :: CASSETTE_BASE = 
  integer, parameter :: CASSETTE_CASS_MOTOR_ADDR = 0xE840
  integer, parameter :: CASSETTE_CASS_WRITE_ADDR = 0xE842
  integer, parameter :: CASSETTE_CASS_READ_ADDR = 0xE812
  ! IEEE-488 bus interface
  integer, parameter :: IEEE488_BASE = 
  integer, parameter :: IEEE488_IEEE_DATA_ADDR = 0xE801
  integer, parameter :: IEEE488_IEEE_STATUS_ADDR = 0xE802
  integer, parameter :: IEEE488_IEEE_CONTROL_ADDR = 0xE803

  ! 中断向量定义
  integer, parameter :: INT_NMI = 65526  ! Non-maskable interrupt
  integer, parameter :: INT_RESET = 65528  ! Reset vector
  integer, parameter :: INT_IRQ = 65530  ! Interrupt request
  integer, parameter :: INT_BRK = 65532  ! Break instruction

end module commodore_pet_device
