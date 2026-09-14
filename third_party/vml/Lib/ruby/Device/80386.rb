# Intel 80386 设备定义 - Ruby 模块
# 生成自: Intel/x86/Intel 80386
# 版本: 
# 日期: 
# 作者: 
# 描述: Intel 80386 32-bit microprocessor with virtual 8086 mode and paging
# CPU架构: x86-32
# 位宽: 0位
# 时钟频率: 0 Hz

module Intel 80386

  # 外设定义
  # Programmable Interrupt Controller
  _8259A_BASE = 
  _8259A_ICW1_ADDR = 0x20
  _8259A_ICW2_ADDR = 0x21
  _8259A_ICW3_ADDR = 0x21
  _8259A_ICW4_ADDR = 0x21
  _8259A_OCW1_ADDR = 0x21
  _8259A_OCW2_ADDR = 0x20
  _8259A_OCW3_ADDR = 0x20
  # Programmable Interval Timer
  _8253_BASE = 
  _8253_COUNTER0_ADDR = 0x40
  _8253_COUNTER1_ADDR = 0x41
  _8253_COUNTER2_ADDR = 0x42
  _8253_CONTROL_ADDR = 0x43
  # Direct Memory Access Controller
  _8237_BASE = 
  _8237_CHANNEL0_ADDR = 0x00
  _8237_CHANNEL1_ADDR = 0x02
  _8237_CHANNEL2_ADDR = 0x04
  _8237_CHANNEL3_ADDR = 0x06
  _8237_STATUS_ADDR = 0x08
  _8237_COMMAND_ADDR = 0x08
  _8237_REQUEST_ADDR = 0x09
  _8237_MASK_ADDR = 0x0A
  _8237_MODE_ADDR = 0x0B
  _8237_FLIPFLOP_ADDR = 0x0C
  _8237_TEMP_ADDR = 0x0D
  _8237_MASTERCLEAR_ADDR = 0x0D
  _8237_MASKALL_ADDR = 0x0F
  # Keyboard Controller
  _8042_BASE = 
  _8042_DATA_ADDR = 0x60
  _8042_STATUS_ADDR = 0x64
  # Integrated System Peripheral
  _82380_BASE = 
  _82380_DMA_ADDR = 0x0000
  _82380_INTERRUPT_ADDR = 0x0200
  _82380_TIMER_ADDR = 0x0400
  _82380_DRAM_ADDR = 0x0600
  _82380_WAITSTATE_ADDR = 0x0800

  # 中断向量定义
  INT_DIVIDE_ERROR = 0  # Division by zero or overflow
  INT_DEBUG_EXCEPTION = 1  # Single-step or debug register access
  INT_NMI = 2  # Non-maskable interrupt
  INT_BREAKPOINT = 3  # INT 3 instruction
  INT_OVERFLOW = 4  # INTO instruction with OF=1
  INT_BOUNDS_CHECK = 5  # BOUND instruction
  INT_INVALID_OPCODE = 6  # Undefined opcode
  INT_COPROCESSOR_NOT_AVAILABLE = 7  # No math coprocessor
  INT_DOUBLE_FAULT = 8  # Two exceptions in handler
  INT_COPROCESSOR_SEGMENT_OVERRUN = 9  # Coprocessor operand beyond segment
  INT_INVALID_TSS = 10  # Invalid Task State Segment
  INT_SEGMENT_NOT_PRESENT = 11  # Segment not present
  INT_STACK_FAULT = 12  # Stack segment limit violation
  INT_GENERAL_PROTECTION = 13  # Memory access violation
  INT_PAGE_FAULT = 14  # Page not present
  INT_COPROCESSOR_ERROR = 16  # Math coprocessor error
  INT_ALIGNMENT_CHECK = 17  # Unaligned memory access
  INT_IRQ0 = 32  # Timer interrupt
  INT_IRQ1 = 33  # Keyboard interrupt
  INT_IRQ2 = 34  # Cascade to IRQ8-15
  INT_IRQ3 = 35  # COM2 interrupt
  INT_IRQ4 = 36  # COM1 interrupt
  INT_IRQ5 = 37  # LPT2 interrupt
  INT_IRQ6 = 38  # Floppy disk interrupt
  INT_IRQ7 = 39  # LPT1 interrupt
  INT_IRQ8 = 40  # Real-time clock interrupt
  INT_IRQ9 = 41  # Redirected IRQ2
  INT_IRQ10 = 42  # Reserved
  INT_IRQ11 = 43  # Reserved
  INT_IRQ12 = 44  # PS/2 mouse interrupt
  INT_IRQ13 = 45  # Coprocessor interrupt
  INT_IRQ14 = 46  # Primary IDE interrupt
  INT_IRQ15 = 47  # Secondary IDE interrupt

end
