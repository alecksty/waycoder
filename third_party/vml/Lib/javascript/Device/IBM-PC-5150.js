/**
 * IBM-PC-5150 寄存器定义
 * 生成自: IBM/Personal Computer/IBM-PC-5150
 * 版本: 1.0
 */
export const ibm_pc_5150 = {
  // CPU: x86, 16位, 4772727 Hz

  // 寄存器定义
  // Accumulator Register
  AX: 0x0,
  // Base Register
  BX: 0x1,
  // Count Register
  CX: 0x2,
  // Data Register
  DX: 0x3,
  // Source Index
  SI: 0x4,
  // Destination Index
  DI: 0x5,
  // Base Pointer
  BP: 0x6,
  // Stack Pointer
  SP: 0x7,
  // Code Segment
  CS: 0x8,
  // Data Segment
  DS: 0x9,
  // Extra Segment
  ES: 0xA,
  // Stack Segment
  SS: 0xB,
  // Instruction Pointer
  IP: 0xC,
  // Flags Register
  FLAGS: 0xD,
  FLAGS_CF: 0,  // Carry Flag
  FLAGS_PF: 2,  // Parity Flag
  FLAGS_AF: 4,  // Auxiliary Carry Flag
  FLAGS_ZF: 6,  // Zero Flag
  FLAGS_SF: 7,  // Sign Flag
  FLAGS_TF: 8,  // Trap Flag
  FLAGS_IF: 9,  // Interrupt Enable Flag
  FLAGS_DF: 10,  // Direction Flag
  FLAGS_OF: 11,  // Overflow Flag

  // 内存段
  // BIOS ROM
  BIOS_START: 0xF0000,
  BIOS_END: 0xFFFFF,
  BIOS_SIZE: 65536,
  // Video Memory
  VIDEO_START: 0xB8000,
  VIDEO_END: 0xBFFFF,
  VIDEO_SIZE: 32768,
  // Conventional Memory (640KB)
  CONVENTIONAL_START: 0x00000,
  CONVENTIONAL_END: 0x9FFFF,
  CONVENTIONAL_SIZE: 640,
  // Extended Memory (64KB)
  EXTENDED_START: 0x100000,
  EXTENDED_END: 0x10FFFF,
  EXTENDED_SIZE: 64,

  // 外设定义
  // Programmable Interrupt Controller
  PIC_BASE: 0x20,
  PIC_PIC1_CMD: 0x00000040,
  PIC_PIC1_DATA: 0x00000041,
  PIC_PIC2_CMD: 0x000000C0,
  PIC_PIC2_DATA: 0x000000C1,
  // Programmable Interval Timer
  PIT_BASE: 0x40,
  PIT_PIT_CH0: 0x00000080,
  PIT_PIT_CH1: 0x00000081,
  PIT_PIT_CH2: 0x00000082,
  PIT_PIT_CTRL: 0x00000083,
  // Programmable Peripheral Interface
  PPI_BASE: 0x60,
  PPI_PPI_PA: 0x000000C0,
  PPI_PPI_PB: 0x000000C1,
  PPI_PPI_PC: 0x000000C2,
  PPI_PPI_CTRL: 0x000000C3,
  // Direct Memory Access Controller
  DMA_BASE: 0x00,
  DMA_DMA_CH0_ADDR: 0x00000000,
  DMA_DMA_CH0_COUNT: 0x00000001,
  DMA_DMA_CMD: 0x00000008,
  DMA_DMA_MASK: 0x0000000A,
  DMA_DMA_MODE: 0x0000000B,
  // Color Graphics Adapter
  CGA_BASE: 0x3D4,
  CGA_CGA_INDEX: 0x000007A8,
  CGA_CGA_DATA: 0x000007A9,
  CGA_CGA_MODE: 0x000007AC,
  CGA_CGA_COLOR: 0x000007AD,

  // 中断向量
  IRQ_DIVIDE_ERROR: 0,  // Divide Error
  IRQ_SINGLE_STEP: 1,  // Single Step
  IRQ_NMI: 2,  // Non-Maskable Interrupt
  IRQ_BREAKPOINT: 3,  // Breakpoint
  IRQ_OVERFLOW: 4,  // Overflow
  IRQ_PRINT_SCREEN: 5,  // Print Screen
  IRQ_IRQ0: 8,  // Timer Interrupt
  IRQ_IRQ1: 9,  // Keyboard Interrupt
  IRQ_IRQ2: 10,  // Cascade (8259A)
  IRQ_IRQ3: 11,  // COM2
  IRQ_IRQ4: 12,  // COM1
  IRQ_IRQ5: 13,  // LPT2
  IRQ_IRQ6: 14,  // Floppy Disk
  IRQ_IRQ7: 15,  // LPT1
  IRQ_IRQ8: 16,  // Real Time Clock
  IRQ_IRQ11: 19,  // Reserved
  IRQ_IRQ13: 21,  // Coprocessor
  IRQ_IRQ15: 31,  // Reserved

  // 引脚定义
  PIN_VCC: 1,  // +5V Power Supply
  PIN_GND: 2,  // Ground
  PIN_RESET: 3,  // System Reset
  PIN_CLK: 4,  // System Clock (4.77MHz)
  PIN_READY: 5,  // CPU Ready Signal
  PIN_NMI: 6,  // Non-Maskable Interrupt
  PIN_INTR: 7,  // Interrupt Request
  PIN_HLDA: 8,  // Hold Acknowledge
  PIN_HOLD: 9,  // Hold Request
  PIN_MEMR: 10,  // Memory Read
  PIN_MEMW: 11,  // Memory Write
  PIN_IOR: 12,  // I/O Read
  PIN_IOW: 13,  // I/O Write
  PIN_ALE: 14,  // Address Latch Enable
  PIN_DTR: 15,  // Data Terminal Ready (Serial)
  PIN_RTS: 16,  // Request To Send (Serial)
  PIN_CTS: 17,  // Clear To Send (Serial)
  PIN_DSR: 18,  // Data Set Ready (Serial)
  PIN_RI: 19,  // Ring Indicator (Serial)
  PIN_DCD: 20,  // Data Carrier Detect (Serial)

  init: function() {
    // 硬件初始化
  }
};
