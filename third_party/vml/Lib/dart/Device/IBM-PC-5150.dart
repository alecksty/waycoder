// IBM-PC-5150 设备定义 - Dart 库
// 生成自: IBM/Personal Computer/IBM-PC-5150
// 版本: 1.0
// 日期: 2026-04-16
// 作者: VML Team
// 描述: Original IBM Personal Computer Model 5150
// CPU架构: x86
// 位宽: 16位
// 时钟频率: 4772727 Hz

class IBM_PC_5150Device {
  static const String deviceName = "IBM-PC-5150";
  static const String manufacturer = "IBM";
  static const String family = "Personal Computer";
  static const String version = "1.0";
  static const String architecture = "x86";
  static const int bits = 16;
  static const int clockFrequency = 4772727;

  // 寄存器地址定义
  static const int AX_ADDR = 0x0;  // Accumulator Register
  static const int BX_ADDR = 0x1;  // Base Register
  static const int CX_ADDR = 0x2;  // Count Register
  static const int DX_ADDR = 0x3;  // Data Register
  static const int SI_ADDR = 0x4;  // Source Index
  static const int DI_ADDR = 0x5;  // Destination Index
  static const int BP_ADDR = 0x6;  // Base Pointer
  static const int SP_ADDR = 0x7;  // Stack Pointer
  static const int CS_ADDR = 0x8;  // Code Segment
  static const int DS_ADDR = 0x9;  // Data Segment
  static const int ES_ADDR = 0xA;  // Extra Segment
  static const int SS_ADDR = 0xB;  // Stack Segment
  static const int IP_ADDR = 0xC;  // Instruction Pointer
  static const int FLAGS_ADDR = 0xD;  // Flags Register
  static const int FLAGS_CF_BIT = 0;  // Carry Flag
  static const int FLAGS_PF_BIT = 2;  // Parity Flag
  static const int FLAGS_AF_BIT = 4;  // Auxiliary Carry Flag
  static const int FLAGS_ZF_BIT = 6;  // Zero Flag
  static const int FLAGS_SF_BIT = 7;  // Sign Flag
  static const int FLAGS_TF_BIT = 8;  // Trap Flag
  static const int FLAGS_IF_BIT = 9;  // Interrupt Enable Flag
  static const int FLAGS_DF_BIT = 10;  // Direction Flag
  static const int FLAGS_OF_BIT = 11;  // Overflow Flag

  // 内存段定义
  static const int BIOS_START = 0xF0000;
  static const int BIOS_END = 0xFFFFF;
  static const int BIOS_SIZE = 65536;  // BIOS ROM
  static const int VIDEO_START = 0xB8000;
  static const int VIDEO_END = 0xBFFFF;
  static const int VIDEO_SIZE = 32768;  // Video Memory
  static const int CONVENTIONAL_START = 0x00000;
  static const int CONVENTIONAL_END = 0x9FFFF;
  static const int CONVENTIONAL_SIZE = 640;  // Conventional Memory (640KB)
  static const int EXTENDED_START = 0x100000;
  static const int EXTENDED_END = 0x10FFFF;
  static const int EXTENDED_SIZE = 64;  // Extended Memory (64KB)

  // 外设定义
  // Programmable Interrupt Controller
  static const int PIC_BASE = 0x20;
  static const int PIC_PIC1_CMD_ADDR = 0x20;
  static const int PIC_PIC1_DATA_ADDR = 0x21;
  static const int PIC_PIC2_CMD_ADDR = 0xA0;
  static const int PIC_PIC2_DATA_ADDR = 0xA1;
  // Programmable Interval Timer
  static const int PIT_BASE = 0x40;
  static const int PIT_PIT_CH0_ADDR = 0x40;
  static const int PIT_PIT_CH1_ADDR = 0x41;
  static const int PIT_PIT_CH2_ADDR = 0x42;
  static const int PIT_PIT_CTRL_ADDR = 0x43;
  // Programmable Peripheral Interface
  static const int PPI_BASE = 0x60;
  static const int PPI_PPI_PA_ADDR = 0x60;
  static const int PPI_PPI_PB_ADDR = 0x61;
  static const int PPI_PPI_PC_ADDR = 0x62;
  static const int PPI_PPI_CTRL_ADDR = 0x63;
  // Direct Memory Access Controller
  static const int DMA_BASE = 0x00;
  static const int DMA_DMA_CH0_ADDR_ADDR = 0x00;
  static const int DMA_DMA_CH0_COUNT_ADDR = 0x01;
  static const int DMA_DMA_CMD_ADDR = 0x08;
  static const int DMA_DMA_MASK_ADDR = 0x0A;
  static const int DMA_DMA_MODE_ADDR = 0x0B;
  // Color Graphics Adapter
  static const int CGA_BASE = 0x3D4;
  static const int CGA_CGA_INDEX_ADDR = 0x3D4;
  static const int CGA_CGA_DATA_ADDR = 0x3D5;
  static const int CGA_CGA_MODE_ADDR = 0x3D8;
  static const int CGA_CGA_COLOR_ADDR = 0x3D9;

  // 中断向量定义
  static const int INT_DIVIDE_ERROR = 0;  // Divide Error
  static const int INT_SINGLE_STEP = 1;  // Single Step
  static const int INT_NMI = 2;  // Non-Maskable Interrupt
  static const int INT_BREAKPOINT = 3;  // Breakpoint
  static const int INT_OVERFLOW = 4;  // Overflow
  static const int INT_PRINT_SCREEN = 5;  // Print Screen
  static const int INT_IRQ0 = 8;  // Timer Interrupt
  static const int INT_IRQ1 = 9;  // Keyboard Interrupt
  static const int INT_IRQ2 = 10;  // Cascade (8259A)
  static const int INT_IRQ3 = 11;  // COM2
  static const int INT_IRQ4 = 12;  // COM1
  static const int INT_IRQ5 = 13;  // LPT2
  static const int INT_IRQ6 = 14;  // Floppy Disk
  static const int INT_IRQ7 = 15;  // LPT1
  static const int INT_IRQ8 = 16;  // Real Time Clock
  static const int INT_IRQ11 = 19;  // Reserved
  static const int INT_IRQ13 = 21;  // Coprocessor
  static const int INT_IRQ15 = 31;  // Reserved

  // 引脚定义
  static const int PIN_VCC = 1;  // +5V Power Supply
  static const int PIN_GND = 2;  // Ground
  static const int PIN_RESET = 3;  // System Reset
  static const int PIN_CLK = 4;  // System Clock (4.77MHz)
  static const int PIN_READY = 5;  // CPU Ready Signal
  static const int PIN_NMI = 6;  // Non-Maskable Interrupt
  static const int PIN_INTR = 7;  // Interrupt Request
  static const int PIN_HLDA = 8;  // Hold Acknowledge
  static const int PIN_HOLD = 9;  // Hold Request
  static const int PIN_MEMR = 10;  // Memory Read
  static const int PIN_MEMW = 11;  // Memory Write
  static const int PIN_IOR = 12;  // I/O Read
  static const int PIN_IOW = 13;  // I/O Write
  static const int PIN_ALE = 14;  // Address Latch Enable
  static const int PIN_DTR = 15;  // Data Terminal Ready (Serial)
  static const int PIN_RTS = 16;  // Request To Send (Serial)
  static const int PIN_CTS = 17;  // Clear To Send (Serial)
  static const int PIN_DSR = 18;  // Data Set Ready (Serial)
  static const int PIN_RI = 19;  // Ring Indicator (Serial)
  static const int PIN_DCD = 20;  // Data Carrier Detect (Serial)

}
