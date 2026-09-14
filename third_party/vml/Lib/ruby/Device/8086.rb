# 8086 设备定义 - Ruby 模块
# 生成自: Intel/x86/8086
# 版本: 1.0
# 日期: 2026-04-17
# 作者: VML Team
# 描述: 16-bit microprocessor, first x86 processor
# CPU架构: x86
# 位宽: 16位
# 时钟频率: 5000000 Hz

module 8086

  # 寄存器地址定义
  AX_ADDR = 0  # Accumulator
  AX_AH_BIT = 8  # High byte of AX
  AX_AL_BIT = 0  # Low byte of AX
  BX_ADDR = 1  # Base
  BX_BH_BIT = 8  # High byte of BX
  BX_BL_BIT = 0  # Low byte of BX
  CX_ADDR = 2  # Counter
  CX_CH_BIT = 8  # High byte of CX
  CX_CL_BIT = 0  # Low byte of CX
  DX_ADDR = 3  # Data
  DX_DH_BIT = 8  # High byte of DX
  DX_DL_BIT = 0  # Low byte of DX
  SI_ADDR = 4  # Source Index
  DI_ADDR = 5  # Destination Index
  BP_ADDR = 6  # Base Pointer
  SP_ADDR = 7  # Stack Pointer
  IP_ADDR = 8  # Instruction Pointer
  CS_ADDR = 9  # Code Segment
  DS_ADDR = 10  # Data Segment
  ES_ADDR = 11  # Extra Segment
  SS_ADDR = 12  # Stack Segment
  FLAGS_ADDR = 13  # Flags Register
  FLAGS_CF_BIT = 0  # Carry Flag
  FLAGS_PF_BIT = 2  # Parity Flag
  FLAGS_AF_BIT = 4  # Auxiliary Flag
  FLAGS_ZF_BIT = 6  # Zero Flag
  FLAGS_SF_BIT = 7  # Sign Flag
  FLAGS_TF_BIT = 8  # Trap Flag
  FLAGS_IF_BIT = 9  # Interrupt Enable Flag
  FLAGS_DF_BIT = 10  # Direction Flag
  FLAGS_OF_BIT = 11  # Overflow Flag

  # 内存段定义
  CODE_START = 0x00000
  CODE_END = 0xFFFFF
  CODE_SIZE = 1048576  # 1MB address space
  DATA_START = 0x00000
  DATA_END = 0xFFFFF
  DATA_SIZE = 1048576  # Data memory
  STACK_START = 0xF0000
  STACK_END = 0xFFFFF
  STACK_SIZE = 65536  # Stack memory
  BIOS_START = 0xF0000
  BIOS_END = 0xFFFFF
  BIOS_SIZE = 65536  # BIOS ROM

  # 外设定义
  # Programmable Interrupt Controller
  PIC_BASE = 0x0020
  PIC_PIC1_CMD_ADDR = 0x0020
  PIC_PIC1_DATA_ADDR = 0x0021
  PIC_PIC2_CMD_ADDR = 0x00A0
  PIC_PIC2_DATA_ADDR = 0x00A1
  # Programmable Interval Timer
  PIT_BASE = 0x0040
  PIT_PIT_CH0_ADDR = 0x0040
  PIT_PIT_CH1_ADDR = 0x0041
  PIT_PIT_CH2_ADDR = 0x0042
  PIT_PIT_CMD_ADDR = 0x0043
  # Programmable Peripheral Interface
  PPI_BASE = 0x0060
  PPI_PPI_PA_ADDR = 0x0060
  PPI_PPI_PB_ADDR = 0x0061
  PPI_PPI_PC_ADDR = 0x0062
  PPI_PPI_CMD_ADDR = 0x0063

  # 中断向量定义
  INT_DIVIDE_ERROR = 0  # Divide by zero
  INT_DEBUG = 1  # Single step
  INT_NMI = 2  # Non-maskable interrupt
  INT_BREAKPOINT = 3  # Breakpoint
  INT_OVERFLOW = 4  # INTO detected overflow
  INT_IRQ0 = 8  # Timer interrupt
  INT_IRQ1 = 9  # Keyboard interrupt
  INT_IRQ2 = 10  # Cascade
  INT_IRQ3 = 11  # COM2
  INT_IRQ4 = 12  # COM1
  INT_IRQ5 = 13  # LPT2
  INT_IRQ6 = 14  # Floppy disk
  INT_IRQ7 = 15  # LPT1

end
