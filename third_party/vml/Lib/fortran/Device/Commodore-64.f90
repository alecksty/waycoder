! Commodore-64 设备定义 - Fortran 模块
! 生成自: Commodore International/Commodore 64/Commodore-64
! 版本: 1.0
! 日期: 2026-04-17
! 作者: VML Team
! 描述: Commodore 64 home computer with MOS 6510 CPU, 64KB RAM, and SID sound chip
! CPU架构: MOS 6510
! 位宽: 8位
! 时钟频率: 985248 Hz

module commodore_64_device
  implicit none

  ! 寄存器地址定义
  integer, parameter :: A_ADDR = 0  ! Accumulator
  integer, parameter :: X_ADDR = 0  ! Index Register X
  integer, parameter :: Y_ADDR = 0  ! Index Register Y
  integer, parameter :: SP_ADDR = 0  ! Stack Pointer
  integer, parameter :: PC_ADDR = 0  ! Program Counter
  integer, parameter :: P_ADDR = 0  ! Status Register
  integer, parameter :: PORT_ADDR = 1  ! I/O Port (6510 specific)

  ! 外设定义
  ! Video Interface Chip II
  integer, parameter :: VIC_II_BASE = 
  integer, parameter :: VIC_II_VIC_CTRL1_ADDR = 0xD011
  integer, parameter :: VIC_II_VIC_CTRL2_ADDR = 0xD016
  integer, parameter :: VIC_II_VIC_RASTER_ADDR = 0xD012
  integer, parameter :: VIC_II_VIC_MEMPTR_ADDR = 0xD018
  integer, parameter :: VIC_II_VIC_IRQ_ADDR = 0xD019
  integer, parameter :: VIC_II_VIC_IRQMASK_ADDR = 0xD01A
  integer, parameter :: VIC_II_VIC_BORDER_ADDR = 0xD020
  integer, parameter :: VIC_II_VIC_BG0_ADDR = 0xD021
  integer, parameter :: VIC_II_VIC_BG1_ADDR = 0xD022
  integer, parameter :: VIC_II_VIC_BG2_ADDR = 0xD023
  integer, parameter :: VIC_II_VIC_BG3_ADDR = 0xD024
  integer, parameter :: VIC_II_VIC_SPRITE0_X_ADDR = 0xD000
  integer, parameter :: VIC_II_VIC_SPRITE0_Y_ADDR = 0xD001
  integer, parameter :: VIC_II_VIC_SPRITE1_X_ADDR = 0xD002
  integer, parameter :: VIC_II_VIC_SPRITE1_Y_ADDR = 0xD003
  ! Sound Interface Device (6581)
  integer, parameter :: SID_BASE = 
  integer, parameter :: SID_SID_VOICE1_FREQ_LO_ADDR = 0xD400
  integer, parameter :: SID_SID_VOICE1_FREQ_HI_ADDR = 0xD401
  integer, parameter :: SID_SID_VOICE1_PW_LO_ADDR = 0xD402
  integer, parameter :: SID_SID_VOICE1_PW_HI_ADDR = 0xD403
  integer, parameter :: SID_SID_VOICE1_CTRL_ADDR = 0xD404
  integer, parameter :: SID_SID_VOICE1_AD_ADDR = 0xD405
  integer, parameter :: SID_SID_VOICE1_SR_ADDR = 0xD406
  integer, parameter :: SID_SID_VOICE2_FREQ_LO_ADDR = 0xD407
  integer, parameter :: SID_SID_VOICE2_FREQ_HI_ADDR = 0xD408
  integer, parameter :: SID_SID_VOICE2_PW_LO_ADDR = 0xD409
  integer, parameter :: SID_SID_VOICE2_PW_HI_ADDR = 0xD40A
  integer, parameter :: SID_SID_VOICE2_CTRL_ADDR = 0xD40B
  integer, parameter :: SID_SID_VOICE2_AD_ADDR = 0xD40C
  integer, parameter :: SID_SID_VOICE2_SR_ADDR = 0xD40D
  integer, parameter :: SID_SID_VOICE3_FREQ_LO_ADDR = 0xD40E
  integer, parameter :: SID_SID_VOICE3_FREQ_HI_ADDR = 0xD40F
  integer, parameter :: SID_SID_VOICE3_PW_LO_ADDR = 0xD410
  integer, parameter :: SID_SID_VOICE3_PW_HI_ADDR = 0xD411
  integer, parameter :: SID_SID_VOICE3_CTRL_ADDR = 0xD412
  integer, parameter :: SID_SID_VOICE3_AD_ADDR = 0xD413
  integer, parameter :: SID_SID_VOICE3_SR_ADDR = 0xD414
  integer, parameter :: SID_SID_FILTER_CUTOFF_LO_ADDR = 0xD415
  integer, parameter :: SID_SID_FILTER_CUTOFF_HI_ADDR = 0xD416
  integer, parameter :: SID_SID_FILTER_CTRL_ADDR = 0xD417
  integer, parameter :: SID_SID_VOLUME_ADDR = 0xD418
  integer, parameter :: SID_SID_POTX_ADDR = 0xD419
  integer, parameter :: SID_SID_POTY_ADDR = 0xD41A
  integer, parameter :: SID_SID_OSC3_ADDR = 0xD41B
  integer, parameter :: SID_SID_ENV3_ADDR = 0xD41C
  ! Complex Interface Adapter 1 (6526)
  integer, parameter :: CIA1_BASE = 
  integer, parameter :: CIA1_CIA1_PRA_ADDR = 0xDC00
  integer, parameter :: CIA1_CIA1_PRB_ADDR = 0xDC01
  integer, parameter :: CIA1_CIA1_DDRA_ADDR = 0xDC02
  integer, parameter :: CIA1_CIA1_DDRB_ADDR = 0xDC03
  integer, parameter :: CIA1_CIA1_TALO_ADDR = 0xDC04
  integer, parameter :: CIA1_CIA1_TAHI_ADDR = 0xDC05
  integer, parameter :: CIA1_CIA1_TBLO_ADDR = 0xDC06
  integer, parameter :: CIA1_CIA1_TBHI_ADDR = 0xDC07
  integer, parameter :: CIA1_CIA1_TODTEN_ADDR = 0xDC08
  integer, parameter :: CIA1_CIA1_TODSEC_ADDR = 0xDC09
  integer, parameter :: CIA1_CIA1_TODMIN_ADDR = 0xDC0A
  integer, parameter :: CIA1_CIA1_TODHR_ADDR = 0xDC0B
  integer, parameter :: CIA1_CIA1_SDR_ADDR = 0xDC0C
  integer, parameter :: CIA1_CIA1_ICR_ADDR = 0xDC0D
  integer, parameter :: CIA1_CIA1_CRA_ADDR = 0xDC0E
  integer, parameter :: CIA1_CIA1_CRB_ADDR = 0xDC0F
  ! Complex Interface Adapter 2 (6526)
  integer, parameter :: CIA2_BASE = 
  integer, parameter :: CIA2_CIA2_PRA_ADDR = 0xDD00
  integer, parameter :: CIA2_CIA2_PRB_ADDR = 0xDD01
  integer, parameter :: CIA2_CIA2_DDRA_ADDR = 0xDD02
  integer, parameter :: CIA2_CIA2_DDRB_ADDR = 0xDD03
  integer, parameter :: CIA2_CIA2_TALO_ADDR = 0xDD04
  integer, parameter :: CIA2_CIA2_TAHI_ADDR = 0xDD05
  integer, parameter :: CIA2_CIA2_TBLO_ADDR = 0xDD06
  integer, parameter :: CIA2_CIA2_TBHI_ADDR = 0xDD07
  integer, parameter :: CIA2_CIA2_TODTEN_ADDR = 0xDD08
  integer, parameter :: CIA2_CIA2_TODSEC_ADDR = 0xDD09
  integer, parameter :: CIA2_CIA2_TODMIN_ADDR = 0xDD0A
  integer, parameter :: CIA2_CIA2_TODHR_ADDR = 0xDD0B
  integer, parameter :: CIA2_CIA2_SDR_ADDR = 0xDD0C
  integer, parameter :: CIA2_CIA2_ICR_ADDR = 0xDD0D
  integer, parameter :: CIA2_CIA2_CRA_ADDR = 0xDD0E
  integer, parameter :: CIA2_CIA2_CRB_ADDR = 0xDD0F

  ! 中断向量定义
  integer, parameter :: INT_IRQ = 65532  ! Maskable Interrupt
  integer, parameter :: INT_NMI = 65534  ! Non-Maskable Interrupt
  integer, parameter :: INT_RESET = 65526  ! Reset Vector

end module commodore_64_device
