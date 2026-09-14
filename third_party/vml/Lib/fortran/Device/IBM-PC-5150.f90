! IBM-PC-5150 设备定义 - Fortran 模块
! 生成自: IBM/Personal Computer/IBM-PC-5150
! 版本: 1.0
! 日期: 2026-04-16
! 作者: VML Team
! 描述: Original IBM Personal Computer Model 5150
! CPU架构: x86
! 位宽: 16位
! 时钟频率: 4772727 Hz

module ibm_pc_5150_device
  implicit none

  ! 寄存器地址定义
  integer, parameter :: AX_ADDR = 0x0  ! Accumulator Register
  integer, parameter :: BX_ADDR = 0x1  ! Base Register
  integer, parameter :: CX_ADDR = 0x2  ! Count Register
  integer, parameter :: DX_ADDR = 0x3  ! Data Register
  integer, parameter :: SI_ADDR = 0x4  ! Source Index
  integer, parameter :: DI_ADDR = 0x5  ! Destination Index
  integer, parameter :: BP_ADDR = 0x6  ! Base Pointer
  integer, parameter :: SP_ADDR = 0x7  ! Stack Pointer
  integer, parameter :: CS_ADDR = 0x8  ! Code Segment
  integer, parameter :: DS_ADDR = 0x9  ! Data Segment
  integer, parameter :: ES_ADDR = 0xA  ! Extra Segment
  integer, parameter :: SS_ADDR = 0xB  ! Stack Segment
  integer, parameter :: IP_ADDR = 0xC  ! Instruction Pointer
  integer, parameter :: FLAGS_ADDR = 0xD  ! Flags Register
  integer, parameter :: FLAGS_CF_BIT = 0  ! Carry Flag
  integer, parameter :: FLAGS_PF_BIT = 2  ! Parity Flag
  integer, parameter :: FLAGS_AF_BIT = 4  ! Auxiliary Carry Flag
  integer, parameter :: FLAGS_ZF_BIT = 6  ! Zero Flag
  integer, parameter :: FLAGS_SF_BIT = 7  ! Sign Flag
  integer, parameter :: FLAGS_TF_BIT = 8  ! Trap Flag
  integer, parameter :: FLAGS_IF_BIT = 9  ! Interrupt Enable Flag
  integer, parameter :: FLAGS_DF_BIT = 10  ! Direction Flag
  integer, parameter :: FLAGS_OF_BIT = 11  ! Overflow Flag

  ! 内存段定义
  integer, parameter :: BIOS_START = 0xF0000
  integer, parameter :: BIOS_END = 0xFFFFF
  integer, parameter :: BIOS_SIZE = 65536  ! BIOS ROM
  integer, parameter :: VIDEO_START = 0xB8000
  integer, parameter :: VIDEO_END = 0xBFFFF
  integer, parameter :: VIDEO_SIZE = 32768  ! Video Memory
  integer, parameter :: CONVENTIONAL_START = 0x00000
  integer, parameter :: CONVENTIONAL_END = 0x9FFFF
  integer, parameter :: CONVENTIONAL_SIZE = 640  ! Conventional Memory (640KB)
  integer, parameter :: EXTENDED_START = 0x100000
  integer, parameter :: EXTENDED_END = 0x10FFFF
  integer, parameter :: EXTENDED_SIZE = 64  ! Extended Memory (64KB)

  ! 外设定义
  ! Programmable Interrupt Controller
  integer, parameter :: PIC_BASE = 0x20
  integer, parameter :: PIC_PIC1_CMD_ADDR = 0x20
  integer, parameter :: PIC_PIC1_DATA_ADDR = 0x21
  integer, parameter :: PIC_PIC2_CMD_ADDR = 0xA0
  integer, parameter :: PIC_PIC2_DATA_ADDR = 0xA1
  ! Programmable Interval Timer
  integer, parameter :: PIT_BASE = 0x40
  integer, parameter :: PIT_PIT_CH0_ADDR = 0x40
  integer, parameter :: PIT_PIT_CH1_ADDR = 0x41
  integer, parameter :: PIT_PIT_CH2_ADDR = 0x42
  integer, parameter :: PIT_PIT_CTRL_ADDR = 0x43
  ! Programmable Peripheral Interface
  integer, parameter :: PPI_BASE = 0x60
  integer, parameter :: PPI_PPI_PA_ADDR = 0x60
  integer, parameter :: PPI_PPI_PB_ADDR = 0x61
  integer, parameter :: PPI_PPI_PC_ADDR = 0x62
  integer, parameter :: PPI_PPI_CTRL_ADDR = 0x63
  ! Direct Memory Access Controller
  integer, parameter :: DMA_BASE = 0x00
  integer, parameter :: DMA_DMA_CH0_ADDR_ADDR = 0x00
  integer, parameter :: DMA_DMA_CH0_COUNT_ADDR = 0x01
  integer, parameter :: DMA_DMA_CMD_ADDR = 0x08
  integer, parameter :: DMA_DMA_MASK_ADDR = 0x0A
  integer, parameter :: DMA_DMA_MODE_ADDR = 0x0B
  ! Color Graphics Adapter
  integer, parameter :: CGA_BASE = 0x3D4
  integer, parameter :: CGA_CGA_INDEX_ADDR = 0x3D4
  integer, parameter :: CGA_CGA_DATA_ADDR = 0x3D5
  integer, parameter :: CGA_CGA_MODE_ADDR = 0x3D8
  integer, parameter :: CGA_CGA_COLOR_ADDR = 0x3D9

  ! 中断向量定义
  integer, parameter :: INT_DIVIDE_ERROR = 0  ! Divide Error
  integer, parameter :: INT_SINGLE_STEP = 1  ! Single Step
  integer, parameter :: INT_NMI = 2  ! Non-Maskable Interrupt
  integer, parameter :: INT_BREAKPOINT = 3  ! Breakpoint
  integer, parameter :: INT_OVERFLOW = 4  ! Overflow
  integer, parameter :: INT_PRINT_SCREEN = 5  ! Print Screen
  integer, parameter :: INT_IRQ0 = 8  ! Timer Interrupt
  integer, parameter :: INT_IRQ1 = 9  ! Keyboard Interrupt
  integer, parameter :: INT_IRQ2 = 10  ! Cascade (8259A)
  integer, parameter :: INT_IRQ3 = 11  ! COM2
  integer, parameter :: INT_IRQ4 = 12  ! COM1
  integer, parameter :: INT_IRQ5 = 13  ! LPT2
  integer, parameter :: INT_IRQ6 = 14  ! Floppy Disk
  integer, parameter :: INT_IRQ7 = 15  ! LPT1
  integer, parameter :: INT_IRQ8 = 16  ! Real Time Clock
  integer, parameter :: INT_IRQ11 = 19  ! Reserved
  integer, parameter :: INT_IRQ13 = 21  ! Coprocessor
  integer, parameter :: INT_IRQ15 = 31  ! Reserved

  ! 引脚定义
  integer, parameter :: PIN_VCC = 1  ! +5V Power Supply
  integer, parameter :: PIN_GND = 2  ! Ground
  integer, parameter :: PIN_RESET = 3  ! System Reset
  integer, parameter :: PIN_CLK = 4  ! System Clock (4.77MHz)
  integer, parameter :: PIN_READY = 5  ! CPU Ready Signal
  integer, parameter :: PIN_NMI = 6  ! Non-Maskable Interrupt
  integer, parameter :: PIN_INTR = 7  ! Interrupt Request
  integer, parameter :: PIN_HLDA = 8  ! Hold Acknowledge
  integer, parameter :: PIN_HOLD = 9  ! Hold Request
  integer, parameter :: PIN_MEMR = 10  ! Memory Read
  integer, parameter :: PIN_MEMW = 11  ! Memory Write
  integer, parameter :: PIN_IOR = 12  ! I/O Read
  integer, parameter :: PIN_IOW = 13  ! I/O Write
  integer, parameter :: PIN_ALE = 14  ! Address Latch Enable
  integer, parameter :: PIN_DTR = 15  ! Data Terminal Ready (Serial)
  integer, parameter :: PIN_RTS = 16  ! Request To Send (Serial)
  integer, parameter :: PIN_CTS = 17  ! Clear To Send (Serial)
  integer, parameter :: PIN_DSR = 18  ! Data Set Ready (Serial)
  integer, parameter :: PIN_RI = 19  ! Ring Indicator (Serial)
  integer, parameter :: PIN_DCD = 20  ! Data Carrier Detect (Serial)

end module ibm_pc_5150_device
