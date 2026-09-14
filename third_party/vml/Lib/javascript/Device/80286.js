/**
 * Intel 80286 寄存器定义
 * 生成自: Intel/x86/Intel 80286
 * 版本: 
 */
export const intel_80286 = {
  // CPU: x86-16, 0位, 0 Hz

  // 外设定义
  // Programmable Interrupt Controller
  8259A_BASE: ,
  8259A_ICW1: 0x00000020,
  8259A_ICW2: 0x00000021,
  8259A_ICW3: 0x00000021,
  8259A_ICW4: 0x00000021,
  8259A_OCW1: 0x00000021,
  8259A_OCW2: 0x00000020,
  8259A_OCW3: 0x00000020,
  // Programmable Interval Timer
  8253_BASE: ,
  8253_Counter0: 0x00000040,
  8253_Counter1: 0x00000041,
  8253_Counter2: 0x00000042,
  8253_Control: 0x00000043,
  // Programmable Peripheral Interface
  8255_BASE: ,
  8255_PortA: 0x00000060,
  8255_PortB: 0x00000061,
  8255_PortC: 0x00000062,
  8255_Control: 0x00000063,
  // Direct Memory Access Controller
  8237_BASE: ,
  8237_Channel0: 0x00000000,
  8237_Channel1: 0x00000002,
  8237_Channel2: 0x00000004,
  8237_Channel3: 0x00000006,
  8237_Status: 0x00000008,
  8237_Command: 0x00000008,
  8237_Request: 0x00000009,
  8237_Mask: 0x0000000A,
  8237_Mode: 0x0000000B,
  8237_FlipFlop: 0x0000000C,
  8237_Temp: 0x0000000D,
  8237_MasterClear: 0x0000000D,
  8237_MaskAll: 0x0000000F,
  // Keyboard Controller
  8042_BASE: ,
  8042_Data: 0x00000060,
  8042_Status: 0x00000064,

  // 中断向量
  IRQ_Divide Error: 0,  // Division by zero or overflow
  IRQ_Debug Exception: 1,  // Single-step or debug register access
  IRQ_NMI: 2,  // Non-maskable interrupt
  IRQ_Breakpoint: 3,  // INT 3 instruction
  IRQ_Overflow: 4,  // INTO instruction with OF=1
  IRQ_Bounds Check: 5,  // BOUND instruction
  IRQ_Invalid Opcode: 6,  // Undefined opcode
  IRQ_Coprocessor Not Available: 7,  // No math coprocessor
  IRQ_Double Fault: 8,  // Two exceptions in handler
  IRQ_Coprocessor Segment Overrun: 9,  // Coprocessor operand beyond segment
  IRQ_Invalid TSS: 10,  // Invalid Task State Segment
  IRQ_Segment Not Present: 11,  // Segment not present
  IRQ_Stack Fault: 12,  // Stack segment limit violation
  IRQ_General Protection: 13,  // Memory access violation
  IRQ_Page Fault: 14,  // Page not present (386+)
  IRQ_Coprocessor Error: 16,  // Math coprocessor error
  IRQ_IRQ0: 32,  // Timer interrupt
  IRQ_IRQ1: 33,  // Keyboard interrupt
  IRQ_IRQ2: 34,  // Cascade to IRQ8-15
  IRQ_IRQ3: 35,  // COM2 interrupt
  IRQ_IRQ4: 36,  // COM1 interrupt
  IRQ_IRQ5: 37,  // LPT2 interrupt
  IRQ_IRQ6: 38,  // Floppy disk interrupt
  IRQ_IRQ7: 39,  // LPT1 interrupt
  IRQ_IRQ8: 40,  // Real-time clock interrupt
  IRQ_IRQ9: 41,  // Redirected IRQ2
  IRQ_IRQ10: 42,  // Reserved
  IRQ_IRQ11: 43,  // Reserved
  IRQ_IRQ12: 44,  // PS/2 mouse interrupt
  IRQ_IRQ13: 45,  // Coprocessor interrupt
  IRQ_IRQ14: 46,  // Primary IDE interrupt
  IRQ_IRQ15: 47,  // Secondary IDE interrupt

  init: function() {
    // 硬件初始化
  }
};
