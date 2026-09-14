! Acorn-Archimedes-A310 设备定义 - Fortran 模块
! 生成自: Acorn Computers/Archimedes/Acorn-Archimedes-A310
! 版本: 1.0
! 日期: 2026-04-17
! 作者: VML Team
! 描述: Acorn Archimedes A310 - First ARM-based home computer with RISC OS, ARM250 @ 26MHz
! CPU架构: ARM250
! 位宽: 32位
! 时钟频率: 26000000 Hz

module acorn_archimedes_a310_device
  implicit none

  ! 寄存器地址定义
  integer, parameter :: R0_ADDR = 0x00  ! General Purpose Register 0
  integer, parameter :: R1_ADDR = 0x04  ! General Purpose Register 1
  integer, parameter :: R2_ADDR = 0x08  ! General Purpose Register 2
  integer, parameter :: R3_ADDR = 0x0C  ! General Purpose Register 3
  integer, parameter :: R4_ADDR = 0x10  ! General Purpose Register 4
  integer, parameter :: R5_ADDR = 0x14  ! General Purpose Register 5
  integer, parameter :: R6_ADDR = 0x18  ! General Purpose Register 6
  integer, parameter :: R7_ADDR = 0x1C  ! General Purpose Register 7
  integer, parameter :: R8_ADDR = 0x20  ! General Purpose Register 8
  integer, parameter :: R9_ADDR = 0x24  ! General Purpose Register 9
  integer, parameter :: R10_ADDR = 0x28  ! General Purpose Register 10
  integer, parameter :: R11_ADDR = 0x2C  ! General Purpose Register 11 (fp)
  integer, parameter :: R12_ADDR = 0x30  ! General Purpose Register 12
  integer, parameter :: SP_ADDR = 0x34  ! Stack Pointer (R13)
  integer, parameter :: LR_ADDR = 0x38  ! Link Register (R14)
  integer, parameter :: PC_ADDR = 0x3C  ! Program Counter (R15)
  integer, parameter :: PSR_ADDR = 0x40  ! Processor Status Register
  integer, parameter :: PSR_MODE_BIT = 0  ! Mode bits (0-4)
  integer, parameter :: PSR_T_BIT = 5  ! Thumb state
  integer, parameter :: PSR_F_BIT = 6  ! FIQ disable
  integer, parameter :: PSR_I_BIT = 7  ! IRQ disable
  integer, parameter :: PSR_V_BIT = 28  ! Overflow
  integer, parameter :: PSR_C_BIT = 29  ! Carry
  integer, parameter :: PSR_Z_BIT = 30  ! Zero
  integer, parameter :: PSR_N_BIT = 31  ! Negative

  ! 内存段定义
  integer, parameter :: ROM_START = 0x00000000
  integer, parameter :: ROM_END = 0x0007FFFF
  integer, parameter :: ROM_SIZE = 524288  ! RISC OS ROM (512KB)
  integer, parameter :: RAM_START = 0x00080000
  integer, parameter :: RAM_END = 0x003FFFFF
  integer, parameter :: RAM_SIZE = 3932160  ! Main RAM (up to 4MB)
  integer, parameter :: VRAM_START = 0x00400000
  integer, parameter :: VRAM_END = 0x007FFFFF
  integer, parameter :: VRAM_SIZE = 4194304  ! Video RAM (4MB, VIDC)
  integer, parameter :: IO_START = 0x03000000
  integer, parameter :: IO_END = 0x0301FFFF
  integer, parameter :: IO_SIZE = 131072  ! I/O controller (IOC)
  integer, parameter :: MEMC_START = 0x03200000
  integer, parameter :: MEMC_END = 0x0320FFFF
  integer, parameter :: MEMC_SIZE = 4096  ! Memory Controller (MEMC)
  integer, parameter :: VIDC_START = 0x03400000
  integer, parameter :: VIDC_END = 0x0340FFFF
  integer, parameter :: VIDC_SIZE = 4096  ! Video Controller (VIDC)
  integer, parameter :: IOMD_START = 0x03300000
  integer, parameter :: IOMD_END = 0x0330FFFF
  integer, parameter :: IOMD_SIZE = 4096  ! I/O and Memory DMA

  ! 外设定义
  ! I/O Controller (IOC) - Interrupt/Keyboard/RTC
  integer, parameter :: IOC_BASE = 0x03000000
  integer, parameter :: IOC_IOC_TIMER1_ADDR = 0x03000000
  integer, parameter :: IOC_IOC_TIMER2_ADDR = 0x03000004
  integer, parameter :: IOC_IOC_IOSEL_ADDR = 0x03000008
  integer, parameter :: IOC_IOC_IRQST_ADDR = 0x0300000C
  integer, parameter :: IOC_IOC_IRQLATCH_ADDR = 0x03000010
  integer, parameter :: IOC_IOC_FIQST_ADDR = 0x03000014
  integer, parameter :: IOC_IOC_FIQEN_ADDR = 0x03000018
  integer, parameter :: IOC_IOC_IRQEN_ADDR = 0x0300001C
  integer, parameter :: IOC_IOC_KBDDATA_ADDR = 0x03000020
  integer, parameter :: IOC_IOC_KBDCR_ADDR = 0x03000024
  integer, parameter :: IOC_IOC_RTCDR_ADDR = 0x03000028
  integer, parameter :: IOC_IOC_RTCCR_ADDR = 0x0300002C
  integer, parameter :: IOC_IOC_PRST_ADDR = 0x03000030
  integer, parameter :: IOC_IOC_PORTA_ADDR = 0x03000034
  integer, parameter :: IOC_IOC_PORTB_ADDR = 0x03000038
  integer, parameter :: IOC_IOC_PORTC_ADDR = 0x0300003C
  ! Memory Controller (MEMC1)
  integer, parameter :: MEMC_BASE = 0x03200000
  integer, parameter :: MEMC_MEMC_PT_ADDR = 0x03200000
  integer, parameter :: MEMC_MEMC_CTRL_ADDR = 0x03200004
  integer, parameter :: MEMC_MEMC_DRAM_ADDR = 0x03200008
  integer, parameter :: MEMC_MEMC_ERR_ADDR = 0x0320000C
  ! Video Controller - VIDC1
  integer, parameter :: VIDC_BASE = 0x03400000
  integer, parameter :: VIDC_VIDC_PALETTE_ADDR = 0x03400000
  integer, parameter :: VIDC_VIDC_STARTL_ADDR = 0x03400004
  integer, parameter :: VIDC_VIDC_STARTH_ADDR = 0x03400008
  integer, parameter :: VIDC_VIDC_CONFIG_ADDR = 0x0340000C
  integer, parameter :: VIDC_VIDC_HDISP_ADDR = 0x03400010
  integer, parameter :: VIDC_VIDC_VDISP_ADDR = 0x03400014
  integer, parameter :: VIDC_VIDC_HSYNC_ADDR = 0x03400018
  integer, parameter :: VIDC_VIDC_VSYNC_ADDR = 0x0340001C
  integer, parameter :: VIDC_VIDC_BORDER_ADDR = 0x03400020
  integer, parameter :: VIDC_VIDC_CURSOR_ADDR = 0x03400024
  integer, parameter :: VIDC_VIDC_SOUND_ADDR = 0x03400028
  ! Intel 82710 Floppy Disk Controller
  integer, parameter :: FDC_BASE = 0x03010000
  integer, parameter :: FDC_FDC_STATUS_ADDR = 0x03010000
  integer, parameter :: FDC_FDC_COMMAND_ADDR = 0x03010000
  integer, parameter :: FDC_FDC_TRACK_ADDR = 0x03010004
  integer, parameter :: FDC_FDC_SECTOR_ADDR = 0x03010008
  integer, parameter :: FDC_FDC_DATA_ADDR = 0x0301000C
  ! Serial Port (via IOC)
  integer, parameter :: SERIAL_BASE = 0x03010010
  integer, parameter :: SERIAL_SERIAL_TX_ADDR = 0x03010010
  integer, parameter :: SERIAL_SERIAL_RX_ADDR = 0x03010014
  integer, parameter :: SERIAL_SERIAL_CTRL_ADDR = 0x03010018

  ! 中断向量定义
  integer, parameter :: INT_RESET = 0  ! Reset
  integer, parameter :: INT_UND = 1  ! Undefined instruction
  integer, parameter :: INT_SWI = 2  ! Software Interrupt (SWI/SVC)
  integer, parameter :: INT_PABORT = 3  ! Prefetch Abort
  integer, parameter :: INT_DABORT = 4  ! Data Abort
  integer, parameter :: INT_ADDRESS = 5  ! Address Exception
  integer, parameter :: INT_IRQ = 6  ! IRQ interrupt (IOC)
  integer, parameter :: INT_FIQ = 7  ! FIQ interrupt (VIDC)

  ! 引脚定义
  integer, parameter :: PIN_VCC = 1  ! +5V Power
  integer, parameter :: PIN_GND = 2  ! Ground
  integer, parameter :: PIN_CLK = 3  ! ARM clock (26MHz)
  integer, parameter :: PIN_NRESET = 4  ! Reset (active low)
  integer, parameter :: PIN_NMREQ = 5  ! Memory Request (active low)
  integer, parameter :: PIN_NIORQ = 6  ! I/O Request (active low)
  integer, parameter :: PIN_NRW = 7  ! Read/Write (0=write, 1=read)
  integer, parameter :: PIN_MAS0 = 8  ! Master address bit 0
  integer, parameter :: PIN_MAS1 = 9  ! Master address bit 1
  integer, parameter :: PIN_MAS2 = 10  ! Master address bit 2
  integer, parameter :: PIN_LOCK = 11  ! Bus lock
  integer, parameter :: PIN_NMREQ = 12  ! Memory request (active low)
  integer, parameter :: PIN_NWAIT = 13  ! Wait state (active low)
  integer, parameter :: PIN_NIRQLINE = 14  ! IRQ line (active low)
  integer, parameter :: PIN_NFIRQLINE = 15  ! FIQ line (active low)
  integer, parameter :: PIN_A1_A25 = 16  ! Address Bus (26-bit)
  integer, parameter :: PIN_D0_D31 = 17  ! Data Bus (32-bit)

end module acorn_archimedes_a310_device
