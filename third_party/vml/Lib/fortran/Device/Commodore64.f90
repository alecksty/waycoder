! Commodore-64 设备定义 - Fortran 模块
! 生成自: Commodore/C64/Commodore-64
! 版本: 1.0
! 日期: 2026-04-17
! 作者: VML Team
! 描述: Commodore 64 - Best-selling 8-bit home computer with MOS 6510 CPU, VIC-II graphics, and SID audio
! CPU架构: MOS-6510
! 位宽: 8位
! 时钟频率: 1022727 Hz

module commodore_64_device
  implicit none

  ! 寄存器地址定义
  integer, parameter :: A_ADDR = 0x00  ! Accumulator
  integer, parameter :: X_ADDR = 0x01  ! X Index Register
  integer, parameter :: Y_ADDR = 0x02  ! Y Index Register
  integer, parameter :: SP_ADDR = 0x03  ! Stack Pointer
  integer, parameter :: PC_ADDR = 0x04  ! Program Counter
  integer, parameter :: P_ADDR = 0x06  ! Processor Status
  integer, parameter :: P_C_BIT = 0  ! Carry Flag
  integer, parameter :: P_Z_BIT = 1  ! Zero Flag
  integer, parameter :: P_I_BIT = 2  ! Interrupt Disable
  integer, parameter :: P_D_BIT = 3  ! Decimal Mode
  integer, parameter :: P_B_BIT = 4  ! Break Flag
  integer, parameter :: P_U_BIT = 5  ! Unused
  integer, parameter :: P_V_BIT = 6  ! Overflow Flag
  integer, parameter :: P_N_BIT = 7  ! Negative Flag
  integer, parameter :: PORT_ADDR = 0x00  ! I/O Port (6510 only: DDR + data)

  ! 内存段定义
  integer, parameter :: RAM_START = 0x0000
  integer, parameter :: RAM_END = 0xFFFF
  integer, parameter :: RAM_SIZE = 65536  ! 64KB main RAM
  integer, parameter :: BASIC_ROM_START = 0xA000
  integer, parameter :: BASIC_ROM_END = 0xBFFF
  integer, parameter :: BASIC_ROM_SIZE = 8192  ! BASIC interpreter ROM
  integer, parameter :: KERNAL_ROM_START = 0xE000
  integer, parameter :: KERNAL_ROM_END = 0xFFFF
  integer, parameter :: KERNAL_ROM_SIZE = 8192  ! KERNAL operating system ROM
  integer, parameter :: CHAR_ROM_START = 0xD000
  integer, parameter :: CHAR_ROM_END = 0xDFFF
  integer, parameter :: CHAR_ROM_SIZE = 4096  ! Character generator ROM
  integer, parameter :: IO_RAM_START = 0xD000
  integer, parameter :: IO_RAM_END = 0xDFFF
  integer, parameter :: IO_RAM_SIZE = 4096  ! I/O + RAM window (switchable)

  ! 外设定义
  ! Video Interface Chip II - 6567/6569
  integer, parameter :: VICII_BASE = 0xD000
  integer, parameter :: VICII_SP0X_ADDR = 0xD000
  integer, parameter :: VICII_SP0Y_ADDR = 0xD001
  integer, parameter :: VICII_SP1X_ADDR = 0xD002
  integer, parameter :: VICII_SP1Y_ADDR = 0xD003
  integer, parameter :: VICII_SP2X_ADDR = 0xD004
  integer, parameter :: VICII_SP2Y_ADDR = 0xD005
  integer, parameter :: VICII_SP3X_ADDR = 0xD006
  integer, parameter :: VICII_SP3Y_ADDR = 0xD007
  integer, parameter :: VICII_SP4X_ADDR = 0xD008
  integer, parameter :: VICII_SP4Y_ADDR = 0xD009
  integer, parameter :: VICII_SP5X_ADDR = 0xD00A
  integer, parameter :: VICII_SP5Y_ADDR = 0xD00B
  integer, parameter :: VICII_SP6X_ADDR = 0xD00C
  integer, parameter :: VICII_SP6Y_ADDR = 0xD00D
  integer, parameter :: VICII_SP7X_ADDR = 0xD00E
  integer, parameter :: VICII_SP7Y_ADDR = 0xD00F
  integer, parameter :: VICII_MSIGX_ADDR = 0xD010
  integer, parameter :: VICII_SCROLY_ADDR = 0xD011
  integer, parameter :: VICII_SCROLX_ADDR = 0xD016
  integer, parameter :: VICII_YPSTOP_ADDR = 0xD012
  integer, parameter :: VICII_LPX_ADDR = 0xD013
  integer, parameter :: VICII_LPY_ADDR = 0xD014
  integer, parameter :: VICII_SPENA_ADDR = 0xD015
  integer, parameter :: VICII_CSPMC_ADDR = 0xD017
  integer, parameter :: VICII_MM0_ADDR = 0xD018
  integer, parameter :: VICII_VM01_ADDR = 0xD016
  integer, parameter :: VICII_VICBAS_ADDR = 0xD018
  integer, parameter :: VICII_IRQMASK_ADDR = 0xD019
  integer, parameter :: VICII_IRQST_ADDR = 0xD01A
  integer, parameter :: VICII_SPBGPR_ADDR = 0xD01B
  integer, parameter :: VICII_SPMC_ADDR = 0xD01C
  integer, parameter :: VICII_SP1C_ADDR = 0xD025
  integer, parameter :: VICII_SP2C_ADDR = 0xD026
  integer, parameter :: VICII_SPBC_ADDR = 0xD027
  integer, parameter :: VICII_SP1C0_ADDR = 0xD028
  integer, parameter :: VICII_SP2C0_ADDR = 0xD029
  integer, parameter :: VICII_SP3C0_ADDR = 0xD02A
  integer, parameter :: VICII_SP4C0_ADDR = 0xD02B
  integer, parameter :: VICII_SP5C0_ADDR = 0xD02C
  integer, parameter :: VICII_SP6C0_ADDR = 0xD02D
  integer, parameter :: VICII_SP7C0_ADDR = 0xD02E
  integer, parameter :: VICII_REG_FD_ADDR = 0xD01D
  integer, parameter :: VICII_BGCOL0_ADDR = 0xD021
  integer, parameter :: VICII_BGCOL1_ADDR = 0xD022
  integer, parameter :: VICII_BGCOL2_ADDR = 0xD023
  integer, parameter :: VICII_BGCOL3_ADDR = 0xD024
  ! Sound Interface Device 6581/8580
  integer, parameter :: SID_BASE = 0xD400
  integer, parameter :: SID_FREQ1LO_ADDR = 0xD400
  integer, parameter :: SID_FREQ1HI_ADDR = 0xD401
  integer, parameter :: SID_PW1LO_ADDR = 0xD402
  integer, parameter :: SID_PW1HI_ADDR = 0xD403
  integer, parameter :: SID_CR1_ADDR = 0xD404
  integer, parameter :: SID_AD1_ADDR = 0xD405
  integer, parameter :: SID_SR1_ADDR = 0xD406
  integer, parameter :: SID_FREQ2LO_ADDR = 0xD407
  integer, parameter :: SID_FREQ2HI_ADDR = 0xD408
  integer, parameter :: SID_PW2LO_ADDR = 0xD409
  integer, parameter :: SID_PW2HI_ADDR = 0xD40A
  integer, parameter :: SID_CR2_ADDR = 0xD40B
  integer, parameter :: SID_AD2_ADDR = 0xD40C
  integer, parameter :: SID_SR2_ADDR = 0xD40D
  integer, parameter :: SID_FREQ3LO_ADDR = 0xD40E
  integer, parameter :: SID_FREQ3HI_ADDR = 0xD40F
  integer, parameter :: SID_PW3LO_ADDR = 0xD410
  integer, parameter :: SID_PW3HI_ADDR = 0xD411
  integer, parameter :: SID_CR3_ADDR = 0xD412
  integer, parameter :: SID_AD3_ADDR = 0xD413
  integer, parameter :: SID_SR3_ADDR = 0xD414
  integer, parameter :: SID_FCH_ADDR = 0xD415
  integer, parameter :: SID_FCL_ADDR = 0xD416
  integer, parameter :: SID_RES_FLT_ADDR = 0xD417
  integer, parameter :: SID_VOLUME_ADDR = 0xD418
  integer, parameter :: SID_POTX_ADDR = 0xD419
  integer, parameter :: SID_POTY_ADDR = 0xD41A
  integer, parameter :: SID_OSC3_ADDR = 0xD41B
  integer, parameter :: SID_ENV3_ADDR = 0xD41C
  ! Complex Interface Adapter 1 - Keyboard/Serial
  integer, parameter :: CIA1_BASE = 0xDC00
  integer, parameter :: CIA1_PRA_ADDR = 0xDC00
  integer, parameter :: CIA1_PRB_ADDR = 0xDC01
  integer, parameter :: CIA1_DDRA_ADDR = 0xDC02
  integer, parameter :: CIA1_DDRB_ADDR = 0xDC03
  integer, parameter :: CIA1_TA_LO_ADDR = 0xDC04
  integer, parameter :: CIA1_TA_HI_ADDR = 0xDC05
  integer, parameter :: CIA1_TB_LO_ADDR = 0xDC06
  integer, parameter :: CIA1_TB_HI_ADDR = 0xDC07
  integer, parameter :: CIA1_TOD_TENTH_ADDR = 0xDC08
  integer, parameter :: CIA1_TOD_SEC_ADDR = 0xDC09
  integer, parameter :: CIA1_TOD_MIN_ADDR = 0xDC0A
  integer, parameter :: CIA1_TOD_HR_ADDR = 0xDC0B
  integer, parameter :: CIA1_SDR_ADDR = 0xDC0C
  integer, parameter :: CIA1_ICR_ADDR = 0xDC0D
  integer, parameter :: CIA1_CRA_ADDR = 0xDC0E
  integer, parameter :: CIA1_CRB_ADDR = 0xDC0F
  ! Complex Interface Adapter 2 - Serial/Bus
  integer, parameter :: CIA2_BASE = 0xDD00
  integer, parameter :: CIA2_PRA_ADDR = 0xDD00
  integer, parameter :: CIA2_PRB_ADDR = 0xDD01
  integer, parameter :: CIA2_DDRA_ADDR = 0xDD02
  integer, parameter :: CIA2_DDRB_ADDR = 0xDD03
  integer, parameter :: CIA2_TA_LO_ADDR = 0xDD04
  integer, parameter :: CIA2_TA_HI_ADDR = 0xDD05
  integer, parameter :: CIA2_TB_LO_ADDR = 0xDD06
  integer, parameter :: CIA2_TB_HI_ADDR = 0xDD07
  integer, parameter :: CIA2_TOD_TENTH_ADDR = 0xDD08
  integer, parameter :: CIA2_TOD_SEC_ADDR = 0xDD09
  integer, parameter :: CIA2_TOD_MIN_ADDR = 0xDD0A
  integer, parameter :: CIA2_TOD_HR_ADDR = 0xDD0B
  integer, parameter :: CIA2_SDR_ADDR = 0xDD0C
  integer, parameter :: CIA2_ICR_ADDR = 0xDD0D
  integer, parameter :: CIA2_CRA_ADDR = 0xDD0E
  integer, parameter :: CIA2_CRB_ADDR = 0xDD0F
  ! Color RAM (4-bit per char cell)
  integer, parameter :: COLORRAM_BASE = 0xD800
  integer, parameter :: COLORRAM_COLOR_ADDR = 0xD800
  ! IEC Serial Bus (via CIA1)
  integer, parameter :: IEC_BASE = 0xDC00
  integer, parameter :: IEC_IEC_DATA_ADDR = 0xDC00
  integer, parameter :: IEC_IEC_CLOCK_ADDR = 0xDC01

  ! 中断向量定义
  integer, parameter :: INT_RESET = 0  ! Power-on / Reset
  integer, parameter :: INT_NMI = 1  ! Non-Maskable Interrupt
  integer, parameter :: INT_IRQ = 2  ! IRQ (VIC raster / CIA timer)

  ! 引脚定义
  integer, parameter :: PIN_VCC = 1  ! +5V Power
  integer, parameter :: PIN_GND = 2  ! Ground
  integer, parameter :: PIN_RESET = 3  ! System Reset
  integer, parameter :: PIN_CLK = 4  ! System Clock (~1MHz)
  integer, parameter :: PIN_DOTCLK = 5  ! VIC Dot Clock (8MHz NTSC / 7.8MHz PAL)
  integer, parameter :: PIN_AEC = 6  ! Address Enable Control (VIC steals cycles)
  integer, parameter :: PIN_BA = 7  ! Bus Available (from VIC)
  integer, parameter :: PIN_IRQ = 8  ! Interrupt Request
  integer, parameter :: PIN_NMI = 9  ! Non-Maskable Interrupt
  integer, parameter :: PIN_RWB = 10  ! Read/Write
  integer, parameter :: PIN_A0_A15 = 11  ! Address Bus
  integer, parameter :: PIN_D0_D7 = 12  ! Data Bus

end module commodore_64_device
