# IBM-PC-5150 设备定义 - Ruby 模块
# 生成自: IBM/Personal Computer/IBM-PC-5150
# 版本: 1.0
# 日期: 2026-04-16
# 作者: VML Team
# 描述: Original IBM Personal Computer Model 5150
# CPU架构: x86
# 位宽: 16位
# 时钟频率: 4772727 Hz

module IBM_PC_5150

  # 寄存器地址定义
  AX_ADDR = 0x0  # Accumulator Register
  BX_ADDR = 0x1  # Base Register
  CX_ADDR = 0x2  # Count Register
  DX_ADDR = 0x3  # Data Register
  SI_ADDR = 0x4  # Source Index
  DI_ADDR = 0x5  # Destination Index
  BP_ADDR = 0x6  # Base Pointer
  SP_ADDR = 0x7  # Stack Pointer
  CS_ADDR = 0x8  # Code Segment
  DS_ADDR = 0x9  # Data Segment
  ES_ADDR = 0xA  # Extra Segment
  SS_ADDR = 0xB  # Stack Segment
  IP_ADDR = 0xC  # Instruction Pointer
  FLAGS_ADDR = 0xD  # Flags Register
  FLAGS_CF_BIT = 0  # Carry Flag
  FLAGS_PF_BIT = 2  # Parity Flag
  FLAGS_AF_BIT = 4  # Auxiliary Carry Flag
  FLAGS_ZF_BIT = 6  # Zero Flag
  FLAGS_SF_BIT = 7  # Sign Flag
  FLAGS_TF_BIT = 8  # Trap Flag
  FLAGS_IF_BIT = 9  # Interrupt Enable Flag
  FLAGS_DF_BIT = 10  # Direction Flag
  FLAGS_OF_BIT = 11  # Overflow Flag

  # 内存段定义
  BIOS_START = 0xF0000
  BIOS_END = 0xFFFFF
  BIOS_SIZE = 65536  # BIOS ROM
  VIDEO_START = 0xB8000
  VIDEO_END = 0xBFFFF
  VIDEO_SIZE = 32768  # Video Memory
  CONVENTIONAL_START = 0x00000
  CONVENTIONAL_END = 0x9FFFF
  CONVENTIONAL_SIZE = 640  # Conventional Memory (640KB)
  EXTENDED_START = 0x100000
  EXTENDED_END = 0x10FFFF
  EXTENDED_SIZE = 64  # Extended Memory (64KB)

  # 外设定义
  # Programmable Interrupt Controller
  PIC_BASE = 0x20
  PIC_PIC1_CMD_ADDR = 0x20
  PIC_PIC1_DATA_ADDR = 0x21
  PIC_PIC2_CMD_ADDR = 0xA0
  PIC_PIC2_DATA_ADDR = 0xA1
  # Programmable Interval Timer
  PIT_BASE = 0x40
  PIT_PIT_CH0_ADDR = 0x40
  PIT_PIT_CH1_ADDR = 0x41
  PIT_PIT_CH2_ADDR = 0x42
  PIT_PIT_CTRL_ADDR = 0x43
  # Programmable Peripheral Interface
  PPI_BASE = 0x60
  PPI_PPI_PA_ADDR = 0x60
  PPI_PPI_PB_ADDR = 0x61
  PPI_PPI_PC_ADDR = 0x62
  PPI_PPI_CTRL_ADDR = 0x63
  # Direct Memory Access Controller
  DMA_BASE = 0x00
  DMA_DMA_CH0_ADDR_ADDR = 0x00
  DMA_DMA_CH0_COUNT_ADDR = 0x01
  DMA_DMA_CMD_ADDR = 0x08
  DMA_DMA_MASK_ADDR = 0x0A
  DMA_DMA_MODE_ADDR = 0x0B
  # Color Graphics Adapter
  CGA_BASE = 0x3D4
  CGA_CGA_INDEX_ADDR = 0x3D4
  CGA_CGA_DATA_ADDR = 0x3D5
  CGA_CGA_MODE_ADDR = 0x3D8
  CGA_CGA_COLOR_ADDR = 0x3D9

  # 中断向量定义
  INT_DIVIDE_ERROR = 0  # Divide Error
  INT_SINGLE_STEP = 1  # Single Step
  INT_NMI = 2  # Non-Maskable Interrupt
  INT_BREAKPOINT = 3  # Breakpoint
  INT_OVERFLOW = 4  # Overflow
  INT_PRINT_SCREEN = 5  # Print Screen
  INT_IRQ0 = 8  # Timer Interrupt
  INT_IRQ1 = 9  # Keyboard Interrupt
  INT_IRQ2 = 10  # Cascade (8259A)
  INT_IRQ3 = 11  # COM2
  INT_IRQ4 = 12  # COM1
  INT_IRQ5 = 13  # LPT2
  INT_IRQ6 = 14  # Floppy Disk
  INT_IRQ7 = 15  # LPT1
  INT_IRQ8 = 16  # Real Time Clock
  INT_IRQ11 = 19  # Reserved
  INT_IRQ13 = 21  # Coprocessor
  INT_IRQ15 = 31  # Reserved

  # 引脚定义
  PIN_VCC = 1  # +5V Power Supply
  PIN_GND = 2  # Ground
  PIN_RESET = 3  # System Reset
  PIN_CLK = 4  # System Clock (4.77MHz)
  PIN_READY = 5  # CPU Ready Signal
  PIN_NMI = 6  # Non-Maskable Interrupt
  PIN_INTR = 7  # Interrupt Request
  PIN_HLDA = 8  # Hold Acknowledge
  PIN_HOLD = 9  # Hold Request
  PIN_MEMR = 10  # Memory Read
  PIN_MEMW = 11  # Memory Write
  PIN_IOR = 12  # I/O Read
  PIN_IOW = 13  # I/O Write
  PIN_ALE = 14  # Address Latch Enable
  PIN_DTR = 15  # Data Terminal Ready (Serial)
  PIN_RTS = 16  # Request To Send (Serial)
  PIN_CTS = 17  # Clear To Send (Serial)
  PIN_DSR = 18  # Data Set Ready (Serial)
  PIN_RI = 19  # Ring Indicator (Serial)
  PIN_DCD = 20  # Data Carrier Detect (Serial)

end
