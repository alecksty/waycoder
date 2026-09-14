! Amstrad-CPC-464 设备定义 - Fortran 模块
! 生成自: Amstrad/CPC/Amstrad-CPC-464
! 版本: 1.0
! 日期: 2026-04-17
! 作者: VML Team
! 描述: Amstrad CPC 464 - British 8-bit home computer with Z80 CPU and built-in cassette recorder
! CPU架构: Z80A
! 位宽: 8位
! 时钟频率: 4000000 Hz

module amstrad_cpc_464_device
  implicit none

  ! 寄存器地址定义
  integer, parameter :: A_ADDR = 0x00  ! Accumulator
  integer, parameter :: F_ADDR = 0x01  ! Flags
  integer, parameter :: F_C_BIT = 0  ! Carry
  integer, parameter :: F_N_BIT = 1  ! Subtract
  integer, parameter :: F_PV_BIT = 2  ! Parity/Overflow
  integer, parameter :: F_H_BIT = 4  ! Half Carry
  integer, parameter :: F_Z_BIT = 6  ! Zero
  integer, parameter :: F_S_BIT = 7  ! Sign
  integer, parameter :: B_ADDR = 0x02  ! B Register
  integer, parameter :: C_ADDR = 0x03  ! C Register
  integer, parameter :: D_ADDR = 0x04  ! D Register
  integer, parameter :: E_ADDR = 0x05  ! E Register
  integer, parameter :: H_ADDR = 0x06  ! H Register
  integer, parameter :: L_ADDR = 0x07  ! L Register
  integer, parameter :: AF_ADDR = 0x08  ! Alternate AF
  integer, parameter :: BC_ADDR = 0x0A  ! Alternate BC
  integer, parameter :: DE_ADDR = 0x0C  ! Alternate DE
  integer, parameter :: HL_ADDR = 0x0E  ! Alternate HL
  integer, parameter :: I_ADDR = 0x10  ! Interrupt Vector
  integer, parameter :: R_ADDR = 0x11  ! Refresh
  integer, parameter :: IX_ADDR = 0x12  ! Index X
  integer, parameter :: IY_ADDR = 0x14  ! Index Y
  integer, parameter :: SP_ADDR = 0x16  ! Stack Pointer
  integer, parameter :: PC_ADDR = 0x18  ! Program Counter

  ! 内存段定义
  integer, parameter :: LOWER_ROM_START = 0x0000
  integer, parameter :: LOWER_ROM_END = 0x3FFF
  integer, parameter :: LOWER_ROM_SIZE = 16384  ! Lower ROM (AMSDOS / CP/M)
  integer, parameter :: RAM_BANK0_START = 0x0000
  integer, parameter :: RAM_BANK0_END = 0x3FFF
  integer, parameter :: RAM_BANK0_SIZE = 16384  ! Lower RAM bank (switchable)
  integer, parameter :: RAM_MAIN_START = 0x4000
  integer, parameter :: RAM_MAIN_END = 0xBFFF
  integer, parameter :: RAM_MAIN_SIZE = 32768  ! Main RAM (32KB)
  integer, parameter :: UPPER_ROM_START = 0xC000
  integer, parameter :: UPPER_ROM_END = 0xFFFF
  integer, parameter :: UPPER_ROM_SIZE = 16384  ! Upper ROM (BASIC)

  ! 外设定义
  ! Gate Array - Custom ASIC (video/sound/RAM control)
  integer, parameter :: GA_BASE = 0x7F00
  integer, parameter :: GA_GA_MR_ADDR = 0x7F00
  integer, parameter :: GA_GA_IR_ADDR = 0x7F01
  integer, parameter :: GA_GA_R1_ADDR = 0x7F02
  integer, parameter :: GA_GA_R2_ADDR = 0x7F03
  integer, parameter :: GA_GA_R3_ADDR = 0x7F04
  integer, parameter :: GA_GA_R4_ADDR = 0x7F05
  integer, parameter :: GA_GA_R5_ADDR = 0x7F06
  integer, parameter :: GA_GA_R6_ADDR = 0x7F07
  integer, parameter :: GA_GA_R7_ADDR = 0x7F08
  ! CRT Controller 6845 - Video timing
  integer, parameter :: CRTC_BASE = 0xBC00
  integer, parameter :: CRTC_CRTC_REG_ADDR = 0xBC00
  integer, parameter :: CRTC_CRTC_DATA_ADDR = 0xBD00
  integer, parameter :: CRTC_CRTC_H_TOTAL_ADDR = 0xBC01
  integer, parameter :: CRTC_CRTC_H_DISP_ADDR = 0xBC02
  integer, parameter :: CRTC_CRTC_HSYNC_POS_ADDR = 0xBC03
  integer, parameter :: CRTC_CRTC_HSYNC_WIDTH_ADDR = 0xBC04
  integer, parameter :: CRTC_CRTC_V_TOTAL_ADDR = 0xBC05
  integer, parameter :: CRTC_CRTC_V_TOTAL_ADJ_ADDR = 0xBC06
  integer, parameter :: CRTC_CRTC_V_DISP_ADDR = 0xBC07
  integer, parameter :: CRTC_CRTC_VSYNC_POS_ADDR = 0xBC08
  integer, parameter :: CRTC_CRTC_INTERLACE_ADDR = 0xBC09
  integer, parameter :: CRTC_CRTC_CURSOR_START_ADDR = 0xBC0A
  integer, parameter :: CRTC_CRTC_CURSOR_END_ADDR = 0xBC0B
  integer, parameter :: CRTC_CRTC_SA_HI_ADDR = 0xBC0C
  integer, parameter :: CRTC_CRTC_SA_LO_ADDR = 0xBC0D
  integer, parameter :: CRTC_CRTC_CURSOR_HI_ADDR = 0xBC0E
  integer, parameter :: CRTC_CRTC_CURSOR_LO_ADDR = 0xBC0F
  ! AY-3-8912 Programmable Sound Generator
  integer, parameter :: PSG_BASE = 0xF400
  integer, parameter :: PSG_PSG_REG_ADDR = 0xF400
  integer, parameter :: PSG_PSG_DATA_ADDR = 0xF600
  integer, parameter :: PSG_FREQ_A_LO_ADDR = 0xF400
  integer, parameter :: PSG_FREQ_A_HI_ADDR = 0xF401
  integer, parameter :: PSG_FREQ_B_LO_ADDR = 0xF402
  integer, parameter :: PSG_FREQ_B_HI_ADDR = 0xF403
  integer, parameter :: PSG_FREQ_C_LO_ADDR = 0xF404
  integer, parameter :: PSG_FREQ_C_HI_ADDR = 0xF405
  integer, parameter :: PSG_NOISE_FREQ_ADDR = 0xF406
  integer, parameter :: PSG_ENABLE_ADDR = 0xF407
  integer, parameter :: PSG_VOL_A_ADDR = 0xF408
  integer, parameter :: PSG_VOL_B_ADDR = 0xF409
  integer, parameter :: PSG_VOL_C_ADDR = 0xF40A
  integer, parameter :: PSG_ENV_FREQ_LO_ADDR = 0xF40B
  integer, parameter :: PSG_ENV_FREQ_HI_ADDR = 0xF40C
  integer, parameter :: PSG_ENV_SHAPE_ADDR = 0xF40D
  integer, parameter :: PSG_PORT_A_ADDR = 0xF40E
  integer, parameter :: PSG_PORT_B_ADDR = 0xF40F
  ! WD1772 Floppy Disk Controller (via expansion)
  integer, parameter :: FDC_BASE = 0xF800
  integer, parameter :: FDC_FDC_STATUS_ADDR = 0xF8E0
  integer, parameter :: FDC_FDC_COMMAND_ADDR = 0xF8E0
  integer, parameter :: FDC_FDC_TRACK_ADDR = 0xF8E1
  integer, parameter :: FDC_FDC_SECTOR_ADDR = 0xF8E2
  integer, parameter :: FDC_FDC_DATA_ADDR = 0xF8E3
  ! Centronics Parallel Printer Port
  integer, parameter :: PRINTER_BASE = 0xEE
  integer, parameter :: PRINTER_PRN_DATA_ADDR = 0xEE
  integer, parameter :: PRINTER_PRN_STROBE_ADDR = 0xEF

  ! 中断向量定义
  integer, parameter :: INT_RESET = 0  ! Power-on / Reset
  integer, parameter :: INT_NMI = 1  ! Non-Maskable Interrupt
  integer, parameter :: INT_INT = 2  ! Gate Array interrupt (50Hz vertical blank)

  ! 引脚定义
  integer, parameter :: PIN_VCC = 1  ! +5V Power
  integer, parameter :: PIN_GND = 2  ! Ground
  integer, parameter :: PIN_CLK = 3  ! Z80 Clock (4MHz)
  integer, parameter :: PIN_A0_A15 = 4  ! Address Bus
  integer, parameter :: PIN_D0_D7 = 5  ! Data Bus
  integer, parameter :: PIN_MREQ = 6  ! Memory Request
  integer, parameter :: PIN_IORQ = 7  ! I/O Request
  integer, parameter :: PIN_RD = 8  ! Read
  integer, parameter :: PIN_WR = 9  ! Write
  integer, parameter :: PIN_INT = 10  ! Interrupt Request
  integer, parameter :: PIN_NMI = 11  ! Non-Maskable Interrupt
  integer, parameter :: PIN_RESET = 12  ! Reset

end module amstrad_cpc_464_device
