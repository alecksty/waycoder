! 8086 设备定义 - Fortran 模块
! 生成自: Intel/x86/8086
! 版本: 1.0
! 日期: 2026-04-17
! 作者: VML Team
! 描述: 16-bit microprocessor, first x86 processor
! CPU架构: x86
! 位宽: 16位
! 时钟频率: 5000000 Hz

module 8086_device
  implicit none

  ! 寄存器地址定义
  integer, parameter :: AX_ADDR = 0  ! Accumulator
  integer, parameter :: AX_AH_BIT = 8  ! High byte of AX
  integer, parameter :: AX_AL_BIT = 0  ! Low byte of AX
  integer, parameter :: BX_ADDR = 1  ! Base
  integer, parameter :: BX_BH_BIT = 8  ! High byte of BX
  integer, parameter :: BX_BL_BIT = 0  ! Low byte of BX
  integer, parameter :: CX_ADDR = 2  ! Counter
  integer, parameter :: CX_CH_BIT = 8  ! High byte of CX
  integer, parameter :: CX_CL_BIT = 0  ! Low byte of CX
  integer, parameter :: DX_ADDR = 3  ! Data
  integer, parameter :: DX_DH_BIT = 8  ! High byte of DX
  integer, parameter :: DX_DL_BIT = 0  ! Low byte of DX
  integer, parameter :: SI_ADDR = 4  ! Source Index
  integer, parameter :: DI_ADDR = 5  ! Destination Index
  integer, parameter :: BP_ADDR = 6  ! Base Pointer
  integer, parameter :: SP_ADDR = 7  ! Stack Pointer
  integer, parameter :: IP_ADDR = 8  ! Instruction Pointer
  integer, parameter :: CS_ADDR = 9  ! Code Segment
  integer, parameter :: DS_ADDR = 10  ! Data Segment
  integer, parameter :: ES_ADDR = 11  ! Extra Segment
  integer, parameter :: SS_ADDR = 12  ! Stack Segment
  integer, parameter :: FLAGS_ADDR = 13  ! Flags Register
  integer, parameter :: FLAGS_CF_BIT = 0  ! Carry Flag
  integer, parameter :: FLAGS_PF_BIT = 2  ! Parity Flag
  integer, parameter :: FLAGS_AF_BIT = 4  ! Auxiliary Flag
  integer, parameter :: FLAGS_ZF_BIT = 6  ! Zero Flag
  integer, parameter :: FLAGS_SF_BIT = 7  ! Sign Flag
  integer, parameter :: FLAGS_TF_BIT = 8  ! Trap Flag
  integer, parameter :: FLAGS_IF_BIT = 9  ! Interrupt Enable Flag
  integer, parameter :: FLAGS_DF_BIT = 10  ! Direction Flag
  integer, parameter :: FLAGS_OF_BIT = 11  ! Overflow Flag

  ! 内存段定义
  integer, parameter :: CODE_START = 0x00000
  integer, parameter :: CODE_END = 0xFFFFF
  integer, parameter :: CODE_SIZE = 1048576  ! 1MB address space
  integer, parameter :: DATA_START = 0x00000
  integer, parameter :: DATA_END = 0xFFFFF
  integer, parameter :: DATA_SIZE = 1048576  ! Data memory
  integer, parameter :: STACK_START = 0xF0000
  integer, parameter :: STACK_END = 0xFFFFF
  integer, parameter :: STACK_SIZE = 65536  ! Stack memory
  integer, parameter :: BIOS_START = 0xF0000
  integer, parameter :: BIOS_END = 0xFFFFF
  integer, parameter :: BIOS_SIZE = 65536  ! BIOS ROM

  ! 外设定义
  ! Programmable Interrupt Controller
  integer, parameter :: PIC_BASE = 0x0020
  integer, parameter :: PIC_PIC1_CMD_ADDR = 0x0020
  integer, parameter :: PIC_PIC1_DATA_ADDR = 0x0021
  integer, parameter :: PIC_PIC2_CMD_ADDR = 0x00A0
  integer, parameter :: PIC_PIC2_DATA_ADDR = 0x00A1
  ! Programmable Interval Timer
  integer, parameter :: PIT_BASE = 0x0040
  integer, parameter :: PIT_PIT_CH0_ADDR = 0x0040
  integer, parameter :: PIT_PIT_CH1_ADDR = 0x0041
  integer, parameter :: PIT_PIT_CH2_ADDR = 0x0042
  integer, parameter :: PIT_PIT_CMD_ADDR = 0x0043
  ! Programmable Peripheral Interface
  integer, parameter :: PPI_BASE = 0x0060
  integer, parameter :: PPI_PPI_PA_ADDR = 0x0060
  integer, parameter :: PPI_PPI_PB_ADDR = 0x0061
  integer, parameter :: PPI_PPI_PC_ADDR = 0x0062
  integer, parameter :: PPI_PPI_CMD_ADDR = 0x0063

  ! 中断向量定义
  integer, parameter :: INT_DIVIDE_ERROR = 0  ! Divide by zero
  integer, parameter :: INT_DEBUG = 1  ! Single step
  integer, parameter :: INT_NMI = 2  ! Non-maskable interrupt
  integer, parameter :: INT_BREAKPOINT = 3  ! Breakpoint
  integer, parameter :: INT_OVERFLOW = 4  ! INTO detected overflow
  integer, parameter :: INT_IRQ0 = 8  ! Timer interrupt
  integer, parameter :: INT_IRQ1 = 9  ! Keyboard interrupt
  integer, parameter :: INT_IRQ2 = 10  ! Cascade
  integer, parameter :: INT_IRQ3 = 11  ! COM2
  integer, parameter :: INT_IRQ4 = 12  ! COM1
  integer, parameter :: INT_IRQ5 = 13  ! LPT2
  integer, parameter :: INT_IRQ6 = 14  ! Floppy disk
  integer, parameter :: INT_IRQ7 = 15  ! LPT1

end module 8086_device
