// Intel 80386 设备定义 - Dart 库
// 生成自: Intel/x86/Intel 80386
// 版本: 
// 日期: 
// 作者: 
// 描述: Intel 80386 32-bit microprocessor with virtual 8086 mode and paging
// CPU架构: x86-32
// 位宽: 0位
// 时钟频率: 0 Hz

class Intel 80386Device {
  static const String deviceName = "Intel 80386";
  static const String manufacturer = "Intel";
  static const String family = "x86";
  static const String version = "";
  static const String architecture = "x86-32";
  static const int bits = 0;
  static const int clockFrequency = 0;

  // 外设定义
  // Programmable Interrupt Controller
  static const int _8259A_BASE = ;
  static const int _8259A_ICW1_ADDR = 0x20;
  static const int _8259A_ICW2_ADDR = 0x21;
  static const int _8259A_ICW3_ADDR = 0x21;
  static const int _8259A_ICW4_ADDR = 0x21;
  static const int _8259A_OCW1_ADDR = 0x21;
  static const int _8259A_OCW2_ADDR = 0x20;
  static const int _8259A_OCW3_ADDR = 0x20;
  // Programmable Interval Timer
  static const int _8253_BASE = ;
  static const int _8253_COUNTER0_ADDR = 0x40;
  static const int _8253_COUNTER1_ADDR = 0x41;
  static const int _8253_COUNTER2_ADDR = 0x42;
  static const int _8253_CONTROL_ADDR = 0x43;
  // Direct Memory Access Controller
  static const int _8237_BASE = ;
  static const int _8237_CHANNEL0_ADDR = 0x00;
  static const int _8237_CHANNEL1_ADDR = 0x02;
  static const int _8237_CHANNEL2_ADDR = 0x04;
  static const int _8237_CHANNEL3_ADDR = 0x06;
  static const int _8237_STATUS_ADDR = 0x08;
  static const int _8237_COMMAND_ADDR = 0x08;
  static const int _8237_REQUEST_ADDR = 0x09;
  static const int _8237_MASK_ADDR = 0x0A;
  static const int _8237_MODE_ADDR = 0x0B;
  static const int _8237_FLIPFLOP_ADDR = 0x0C;
  static const int _8237_TEMP_ADDR = 0x0D;
  static const int _8237_MASTERCLEAR_ADDR = 0x0D;
  static const int _8237_MASKALL_ADDR = 0x0F;
  // Keyboard Controller
  static const int _8042_BASE = ;
  static const int _8042_DATA_ADDR = 0x60;
  static const int _8042_STATUS_ADDR = 0x64;
  // Integrated System Peripheral
  static const int _82380_BASE = ;
  static const int _82380_DMA_ADDR = 0x0000;
  static const int _82380_INTERRUPT_ADDR = 0x0200;
  static const int _82380_TIMER_ADDR = 0x0400;
  static const int _82380_DRAM_ADDR = 0x0600;
  static const int _82380_WAITSTATE_ADDR = 0x0800;

  // 中断向量定义
  static const int INT_DIVIDE_ERROR = 0;  // Division by zero or overflow
  static const int INT_DEBUG_EXCEPTION = 1;  // Single-step or debug register access
  static const int INT_NMI = 2;  // Non-maskable interrupt
  static const int INT_BREAKPOINT = 3;  // INT 3 instruction
  static const int INT_OVERFLOW = 4;  // INTO instruction with OF=1
  static const int INT_BOUNDS_CHECK = 5;  // BOUND instruction
  static const int INT_INVALID_OPCODE = 6;  // Undefined opcode
  static const int INT_COPROCESSOR_NOT_AVAILABLE = 7;  // No math coprocessor
  static const int INT_DOUBLE_FAULT = 8;  // Two exceptions in handler
  static const int INT_COPROCESSOR_SEGMENT_OVERRUN = 9;  // Coprocessor operand beyond segment
  static const int INT_INVALID_TSS = 10;  // Invalid Task State Segment
  static const int INT_SEGMENT_NOT_PRESENT = 11;  // Segment not present
  static const int INT_STACK_FAULT = 12;  // Stack segment limit violation
  static const int INT_GENERAL_PROTECTION = 13;  // Memory access violation
  static const int INT_PAGE_FAULT = 14;  // Page not present
  static const int INT_COPROCESSOR_ERROR = 16;  // Math coprocessor error
  static const int INT_ALIGNMENT_CHECK = 17;  // Unaligned memory access
  static const int INT_IRQ0 = 32;  // Timer interrupt
  static const int INT_IRQ1 = 33;  // Keyboard interrupt
  static const int INT_IRQ2 = 34;  // Cascade to IRQ8-15
  static const int INT_IRQ3 = 35;  // COM2 interrupt
  static const int INT_IRQ4 = 36;  // COM1 interrupt
  static const int INT_IRQ5 = 37;  // LPT2 interrupt
  static const int INT_IRQ6 = 38;  // Floppy disk interrupt
  static const int INT_IRQ7 = 39;  // LPT1 interrupt
  static const int INT_IRQ8 = 40;  // Real-time clock interrupt
  static const int INT_IRQ9 = 41;  // Redirected IRQ2
  static const int INT_IRQ10 = 42;  // Reserved
  static const int INT_IRQ11 = 43;  // Reserved
  static const int INT_IRQ12 = 44;  // PS/2 mouse interrupt
  static const int INT_IRQ13 = 45;  // Coprocessor interrupt
  static const int INT_IRQ14 = 46;  // Primary IDE interrupt
  static const int INT_IRQ15 = 47;  // Secondary IDE interrupt

}
