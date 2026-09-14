// 8086 设备定义 - Dart 库
// 生成自: Intel/x86/8086
// 版本: 1.0
// 日期: 2026-04-17
// 作者: VML Team
// 描述: 16-bit microprocessor, first x86 processor
// CPU架构: x86
// 位宽: 16位
// 时钟频率: 5000000 Hz

class 8086Device {
  static const String deviceName = "8086";
  static const String manufacturer = "Intel";
  static const String family = "x86";
  static const String version = "1.0";
  static const String architecture = "x86";
  static const int bits = 16;
  static const int clockFrequency = 5000000;

  // 寄存器地址定义
  static const int AX_ADDR = 0;  // Accumulator
  static const int AX_AH_BIT = 8;  // High byte of AX
  static const int AX_AL_BIT = 0;  // Low byte of AX
  static const int BX_ADDR = 1;  // Base
  static const int BX_BH_BIT = 8;  // High byte of BX
  static const int BX_BL_BIT = 0;  // Low byte of BX
  static const int CX_ADDR = 2;  // Counter
  static const int CX_CH_BIT = 8;  // High byte of CX
  static const int CX_CL_BIT = 0;  // Low byte of CX
  static const int DX_ADDR = 3;  // Data
  static const int DX_DH_BIT = 8;  // High byte of DX
  static const int DX_DL_BIT = 0;  // Low byte of DX
  static const int SI_ADDR = 4;  // Source Index
  static const int DI_ADDR = 5;  // Destination Index
  static const int BP_ADDR = 6;  // Base Pointer
  static const int SP_ADDR = 7;  // Stack Pointer
  static const int IP_ADDR = 8;  // Instruction Pointer
  static const int CS_ADDR = 9;  // Code Segment
  static const int DS_ADDR = 10;  // Data Segment
  static const int ES_ADDR = 11;  // Extra Segment
  static const int SS_ADDR = 12;  // Stack Segment
  static const int FLAGS_ADDR = 13;  // Flags Register
  static const int FLAGS_CF_BIT = 0;  // Carry Flag
  static const int FLAGS_PF_BIT = 2;  // Parity Flag
  static const int FLAGS_AF_BIT = 4;  // Auxiliary Flag
  static const int FLAGS_ZF_BIT = 6;  // Zero Flag
  static const int FLAGS_SF_BIT = 7;  // Sign Flag
  static const int FLAGS_TF_BIT = 8;  // Trap Flag
  static const int FLAGS_IF_BIT = 9;  // Interrupt Enable Flag
  static const int FLAGS_DF_BIT = 10;  // Direction Flag
  static const int FLAGS_OF_BIT = 11;  // Overflow Flag

  // 内存段定义
  static const int CODE_START = 0x00000;
  static const int CODE_END = 0xFFFFF;
  static const int CODE_SIZE = 1048576;  // 1MB address space
  static const int DATA_START = 0x00000;
  static const int DATA_END = 0xFFFFF;
  static const int DATA_SIZE = 1048576;  // Data memory
  static const int STACK_START = 0xF0000;
  static const int STACK_END = 0xFFFFF;
  static const int STACK_SIZE = 65536;  // Stack memory
  static const int BIOS_START = 0xF0000;
  static const int BIOS_END = 0xFFFFF;
  static const int BIOS_SIZE = 65536;  // BIOS ROM

  // 外设定义
  // Programmable Interrupt Controller
  static const int PIC_BASE = 0x0020;
  static const int PIC_PIC1_CMD_ADDR = 0x0020;
  static const int PIC_PIC1_DATA_ADDR = 0x0021;
  static const int PIC_PIC2_CMD_ADDR = 0x00A0;
  static const int PIC_PIC2_DATA_ADDR = 0x00A1;
  // Programmable Interval Timer
  static const int PIT_BASE = 0x0040;
  static const int PIT_PIT_CH0_ADDR = 0x0040;
  static const int PIT_PIT_CH1_ADDR = 0x0041;
  static const int PIT_PIT_CH2_ADDR = 0x0042;
  static const int PIT_PIT_CMD_ADDR = 0x0043;
  // Programmable Peripheral Interface
  static const int PPI_BASE = 0x0060;
  static const int PPI_PPI_PA_ADDR = 0x0060;
  static const int PPI_PPI_PB_ADDR = 0x0061;
  static const int PPI_PPI_PC_ADDR = 0x0062;
  static const int PPI_PPI_CMD_ADDR = 0x0063;

  // 中断向量定义
  static const int INT_DIVIDE_ERROR = 0;  // Divide by zero
  static const int INT_DEBUG = 1;  // Single step
  static const int INT_NMI = 2;  // Non-maskable interrupt
  static const int INT_BREAKPOINT = 3;  // Breakpoint
  static const int INT_OVERFLOW = 4;  // INTO detected overflow
  static const int INT_IRQ0 = 8;  // Timer interrupt
  static const int INT_IRQ1 = 9;  // Keyboard interrupt
  static const int INT_IRQ2 = 10;  // Cascade
  static const int INT_IRQ3 = 11;  // COM2
  static const int INT_IRQ4 = 12;  // COM1
  static const int INT_IRQ5 = 13;  // LPT2
  static const int INT_IRQ6 = 14;  // Floppy disk
  static const int INT_IRQ7 = 15;  // LPT1

}
