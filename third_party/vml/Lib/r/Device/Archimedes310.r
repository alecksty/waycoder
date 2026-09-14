# Acorn-Archimedes-A310 设备定义 - R 脚本
# 生成自: Acorn Computers/Archimedes/Acorn-Archimedes-A310
# 版本: 1.0
# 日期: 2026-04-17
# 作者: VML Team
# 描述: Acorn Archimedes A310 - First ARM-based home computer with RISC OS, ARM250 @ 26MHz
# CPU架构: ARM250
# 位宽: 32位
# 时钟频率: 26000000 Hz

# 寄存器地址定义
R0_ADDR <- 0x00  # General Purpose Register 0
R1_ADDR <- 0x04  # General Purpose Register 1
R2_ADDR <- 0x08  # General Purpose Register 2
R3_ADDR <- 0x0C  # General Purpose Register 3
R4_ADDR <- 0x10  # General Purpose Register 4
R5_ADDR <- 0x14  # General Purpose Register 5
R6_ADDR <- 0x18  # General Purpose Register 6
R7_ADDR <- 0x1C  # General Purpose Register 7
R8_ADDR <- 0x20  # General Purpose Register 8
R9_ADDR <- 0x24  # General Purpose Register 9
R10_ADDR <- 0x28  # General Purpose Register 10
R11_ADDR <- 0x2C  # General Purpose Register 11 (fp)
R12_ADDR <- 0x30  # General Purpose Register 12
SP_ADDR <- 0x34  # Stack Pointer (R13)
LR_ADDR <- 0x38  # Link Register (R14)
PC_ADDR <- 0x3C  # Program Counter (R15)
PSR_ADDR <- 0x40  # Processor Status Register
PSR_MODE_BIT <- 0  # Mode bits (0-4)
PSR_T_BIT <- 5  # Thumb state
PSR_F_BIT <- 6  # FIQ disable
PSR_I_BIT <- 7  # IRQ disable
PSR_V_BIT <- 28  # Overflow
PSR_C_BIT <- 29  # Carry
PSR_Z_BIT <- 30  # Zero
PSR_N_BIT <- 31  # Negative

# 内存段定义
ROM_START <- 0x00000000
ROM_END <- 0x0007FFFF
ROM_SIZE <- 524288  # RISC OS ROM (512KB)
RAM_START <- 0x00080000
RAM_END <- 0x003FFFFF
RAM_SIZE <- 3932160  # Main RAM (up to 4MB)
VRAM_START <- 0x00400000
VRAM_END <- 0x007FFFFF
VRAM_SIZE <- 4194304  # Video RAM (4MB, VIDC)
IO_START <- 0x03000000
IO_END <- 0x0301FFFF
IO_SIZE <- 131072  # I/O controller (IOC)
MEMC_START <- 0x03200000
MEMC_END <- 0x0320FFFF
MEMC_SIZE <- 4096  # Memory Controller (MEMC)
VIDC_START <- 0x03400000
VIDC_END <- 0x0340FFFF
VIDC_SIZE <- 4096  # Video Controller (VIDC)
IOMD_START <- 0x03300000
IOMD_END <- 0x0330FFFF
IOMD_SIZE <- 4096  # I/O and Memory DMA

# 外设定义
# I/O Controller (IOC) - Interrupt/Keyboard/RTC
IOC_BASE <- 0x03000000
IOC_IOC_TIMER1_ADDR <- 0x03000000
IOC_IOC_TIMER2_ADDR <- 0x03000004
IOC_IOC_IOSEL_ADDR <- 0x03000008
IOC_IOC_IRQST_ADDR <- 0x0300000C
IOC_IOC_IRQLATCH_ADDR <- 0x03000010
IOC_IOC_FIQST_ADDR <- 0x03000014
IOC_IOC_FIQEN_ADDR <- 0x03000018
IOC_IOC_IRQEN_ADDR <- 0x0300001C
IOC_IOC_KBDDATA_ADDR <- 0x03000020
IOC_IOC_KBDCR_ADDR <- 0x03000024
IOC_IOC_RTCDR_ADDR <- 0x03000028
IOC_IOC_RTCCR_ADDR <- 0x0300002C
IOC_IOC_PRST_ADDR <- 0x03000030
IOC_IOC_PORTA_ADDR <- 0x03000034
IOC_IOC_PORTB_ADDR <- 0x03000038
IOC_IOC_PORTC_ADDR <- 0x0300003C
# Memory Controller (MEMC1)
MEMC_BASE <- 0x03200000
MEMC_MEMC_PT_ADDR <- 0x03200000
MEMC_MEMC_CTRL_ADDR <- 0x03200004
MEMC_MEMC_DRAM_ADDR <- 0x03200008
MEMC_MEMC_ERR_ADDR <- 0x0320000C
# Video Controller - VIDC1
VIDC_BASE <- 0x03400000
VIDC_VIDC_PALETTE_ADDR <- 0x03400000
VIDC_VIDC_STARTL_ADDR <- 0x03400004
VIDC_VIDC_STARTH_ADDR <- 0x03400008
VIDC_VIDC_CONFIG_ADDR <- 0x0340000C
VIDC_VIDC_HDISP_ADDR <- 0x03400010
VIDC_VIDC_VDISP_ADDR <- 0x03400014
VIDC_VIDC_HSYNC_ADDR <- 0x03400018
VIDC_VIDC_VSYNC_ADDR <- 0x0340001C
VIDC_VIDC_BORDER_ADDR <- 0x03400020
VIDC_VIDC_CURSOR_ADDR <- 0x03400024
VIDC_VIDC_SOUND_ADDR <- 0x03400028
# Intel 82710 Floppy Disk Controller
FDC_BASE <- 0x03010000
FDC_FDC_STATUS_ADDR <- 0x03010000
FDC_FDC_COMMAND_ADDR <- 0x03010000
FDC_FDC_TRACK_ADDR <- 0x03010004
FDC_FDC_SECTOR_ADDR <- 0x03010008
FDC_FDC_DATA_ADDR <- 0x0301000C
# Serial Port (via IOC)
SERIAL_BASE <- 0x03010010
SERIAL_SERIAL_TX_ADDR <- 0x03010010
SERIAL_SERIAL_RX_ADDR <- 0x03010014
SERIAL_SERIAL_CTRL_ADDR <- 0x03010018

# 中断向量定义
INT_RESET <- 0  # Reset
INT_UND <- 1  # Undefined instruction
INT_SWI <- 2  # Software Interrupt (SWI/SVC)
INT_PABORT <- 3  # Prefetch Abort
INT_DABORT <- 4  # Data Abort
INT_ADDRESS <- 5  # Address Exception
INT_IRQ <- 6  # IRQ interrupt (IOC)
INT_FIQ <- 7  # FIQ interrupt (VIDC)

# 引脚定义
PIN_VCC <- 1  # +5V Power
PIN_GND <- 2  # Ground
PIN_CLK <- 3  # ARM clock (26MHz)
PIN_NRESET <- 4  # Reset (active low)
PIN_NMREQ <- 5  # Memory Request (active low)
PIN_NIORQ <- 6  # I/O Request (active low)
PIN_NRW <- 7  # Read/Write (0=write, 1=read)
PIN_MAS0 <- 8  # Master address bit 0
PIN_MAS1 <- 9  # Master address bit 1
PIN_MAS2 <- 10  # Master address bit 2
PIN_LOCK <- 11  # Bus lock
PIN_NMREQ <- 12  # Memory request (active low)
PIN_NWAIT <- 13  # Wait state (active low)
PIN_NIRQLINE <- 14  # IRQ line (active low)
PIN_NFIRQLINE <- 15  # FIQ line (active low)
PIN_A1_A25 <- 16  # Address Bus (26-bit)
PIN_D0_D31 <- 17  # Data Bus (32-bit)

