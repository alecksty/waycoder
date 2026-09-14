/**
 * 8086 寄存器定义
 * 生成自: Intel/x86/8086
 * 版本: 1.0
 */
export const _8086 = {
  // CPU: x86, 16位, 5000000 Hz

  // 寄存器定义
  // Accumulator
  AX: 0,
  AX_AH: 8,  // High byte of AX
  AX_AL: 0,  // Low byte of AX
  // Base
  BX: 1,
  BX_BH: 8,  // High byte of BX
  BX_BL: 0,  // Low byte of BX
  // Counter
  CX: 2,
  CX_CH: 8,  // High byte of CX
  CX_CL: 0,  // Low byte of CX
  // Data
  DX: 3,
  DX_DH: 8,  // High byte of DX
  DX_DL: 0,  // Low byte of DX
  // Source Index
  SI: 4,
  // Destination Index
  DI: 5,
  // Base Pointer
  BP: 6,
  // Stack Pointer
  SP: 7,
  // Instruction Pointer
  IP: 8,
  // Code Segment
  CS: 9,
  // Data Segment
  DS: 10,
  // Extra Segment
  ES: 11,
  // Stack Segment
  SS: 12,
  // Flags Register
  FLAGS: 13,
  FLAGS_CF: 0,  // Carry Flag
  FLAGS_PF: 2,  // Parity Flag
  FLAGS_AF: 4,  // Auxiliary Flag
  FLAGS_ZF: 6,  // Zero Flag
  FLAGS_SF: 7,  // Sign Flag
  FLAGS_TF: 8,  // Trap Flag
  FLAGS_IF: 9,  // Interrupt Enable Flag
  FLAGS_DF: 10,  // Direction Flag
  FLAGS_OF: 11,  // Overflow Flag

  // 内存段
  // 1MB address space
  CODE_START: 0x00000,
  CODE_END: 0xFFFFF,
  CODE_SIZE: 1048576,
  // Data memory
  DATA_START: 0x00000,
  DATA_END: 0xFFFFF,
  DATA_SIZE: 1048576,
  // Stack memory
  STACK_START: 0xF0000,
  STACK_END: 0xFFFFF,
  STACK_SIZE: 65536,
  // BIOS ROM
  BIOS_START: 0xF0000,
  BIOS_END: 0xFFFFF,
  BIOS_SIZE: 65536,

  // 外设定义
  // Programmable Interrupt Controller
  PIC_BASE: 0x0020,
  PIC_PIC1_CMD: 0x00000040,
  PIC_PIC1_DATA: 0x00000041,
  PIC_PIC2_CMD: 0x000000C0,
  PIC_PIC2_DATA: 0x000000C1,
  // Programmable Interval Timer
  PIT_BASE: 0x0040,
  PIT_PIT_CH0: 0x00000080,
  PIT_PIT_CH1: 0x00000081,
  PIT_PIT_CH2: 0x00000082,
  PIT_PIT_CMD: 0x00000083,
  // Programmable Peripheral Interface
  PPI_BASE: 0x0060,
  PPI_PPI_PA: 0x000000C0,
  PPI_PPI_PB: 0x000000C1,
  PPI_PPI_PC: 0x000000C2,
  PPI_PPI_CMD: 0x000000C3,

  // 中断向量
  IRQ_DIVIDE_ERROR: 0,  // Divide by zero
  IRQ_DEBUG: 1,  // Single step
  IRQ_NMI: 2,  // Non-maskable interrupt
  IRQ_BREAKPOINT: 3,  // Breakpoint
  IRQ_OVERFLOW: 4,  // INTO detected overflow
  IRQ_IRQ0: 8,  // Timer interrupt
  IRQ_IRQ1: 9,  // Keyboard interrupt
  IRQ_IRQ2: 10,  // Cascade
  IRQ_IRQ3: 11,  // COM2
  IRQ_IRQ4: 12,  // COM1
  IRQ_IRQ5: 13,  // LPT2
  IRQ_IRQ6: 14,  // Floppy disk
  IRQ_IRQ7: 15,  // LPT1

  init: function() {
    // 硬件初始化
  }
};
