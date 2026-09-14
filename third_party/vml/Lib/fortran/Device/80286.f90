! Intel 80286 设备定义 - Fortran 模块
! 生成自: Intel/x86/Intel 80286
! 版本: 
! 日期: 
! 作者: 
! 描述: Intel 80286 16-bit microprocessor with memory management and protection
! CPU架构: x86-16
! 位宽: 0位
! 时钟频率: 0 Hz

module intel 80286_device
  implicit none

  ! 外设定义
  ! Programmable Interrupt Controller
  integer, parameter :: _8259A_BASE = 
  integer, parameter :: _8259A_ICW1_ADDR = 0x20
  integer, parameter :: _8259A_ICW2_ADDR = 0x21
  integer, parameter :: _8259A_ICW3_ADDR = 0x21
  integer, parameter :: _8259A_ICW4_ADDR = 0x21
  integer, parameter :: _8259A_OCW1_ADDR = 0x21
  integer, parameter :: _8259A_OCW2_ADDR = 0x20
  integer, parameter :: _8259A_OCW3_ADDR = 0x20
  ! Programmable Interval Timer
  integer, parameter :: _8253_BASE = 
  integer, parameter :: _8253_COUNTER0_ADDR = 0x40
  integer, parameter :: _8253_COUNTER1_ADDR = 0x41
  integer, parameter :: _8253_COUNTER2_ADDR = 0x42
  integer, parameter :: _8253_CONTROL_ADDR = 0x43
  ! Programmable Peripheral Interface
  integer, parameter :: _8255_BASE = 
  integer, parameter :: _8255_PORTA_ADDR = 0x60
  integer, parameter :: _8255_PORTB_ADDR = 0x61
  integer, parameter :: _8255_PORTC_ADDR = 0x62
  integer, parameter :: _8255_CONTROL_ADDR = 0x63
  ! Direct Memory Access Controller
  integer, parameter :: _8237_BASE = 
  integer, parameter :: _8237_CHANNEL0_ADDR = 0x00
  integer, parameter :: _8237_CHANNEL1_ADDR = 0x02
  integer, parameter :: _8237_CHANNEL2_ADDR = 0x04
  integer, parameter :: _8237_CHANNEL3_ADDR = 0x06
  integer, parameter :: _8237_STATUS_ADDR = 0x08
  integer, parameter :: _8237_COMMAND_ADDR = 0x08
  integer, parameter :: _8237_REQUEST_ADDR = 0x09
  integer, parameter :: _8237_MASK_ADDR = 0x0A
  integer, parameter :: _8237_MODE_ADDR = 0x0B
  integer, parameter :: _8237_FLIPFLOP_ADDR = 0x0C
  integer, parameter :: _8237_TEMP_ADDR = 0x0D
  integer, parameter :: _8237_MASTERCLEAR_ADDR = 0x0D
  integer, parameter :: _8237_MASKALL_ADDR = 0x0F
  ! Keyboard Controller
  integer, parameter :: _8042_BASE = 
  integer, parameter :: _8042_DATA_ADDR = 0x60
  integer, parameter :: _8042_STATUS_ADDR = 0x64

  ! 中断向量定义
  integer, parameter :: INT_DIVIDE_ERROR = 0  ! Division by zero or overflow
  integer, parameter :: INT_DEBUG_EXCEPTION = 1  ! Single-step or debug register access
  integer, parameter :: INT_NMI = 2  ! Non-maskable interrupt
  integer, parameter :: INT_BREAKPOINT = 3  ! INT 3 instruction
  integer, parameter :: INT_OVERFLOW = 4  ! INTO instruction with OF=1
  integer, parameter :: INT_BOUNDS_CHECK = 5  ! BOUND instruction
  integer, parameter :: INT_INVALID_OPCODE = 6  ! Undefined opcode
  integer, parameter :: INT_COPROCESSOR_NOT_AVAILABLE = 7  ! No math coprocessor
  integer, parameter :: INT_DOUBLE_FAULT = 8  ! Two exceptions in handler
  integer, parameter :: INT_COPROCESSOR_SEGMENT_OVERRUN = 9  ! Coprocessor operand beyond segment
  integer, parameter :: INT_INVALID_TSS = 10  ! Invalid Task State Segment
  integer, parameter :: INT_SEGMENT_NOT_PRESENT = 11  ! Segment not present
  integer, parameter :: INT_STACK_FAULT = 12  ! Stack segment limit violation
  integer, parameter :: INT_GENERAL_PROTECTION = 13  ! Memory access violation
  integer, parameter :: INT_PAGE_FAULT = 14  ! Page not present (386+)
  integer, parameter :: INT_COPROCESSOR_ERROR = 16  ! Math coprocessor error
  integer, parameter :: INT_IRQ0 = 32  ! Timer interrupt
  integer, parameter :: INT_IRQ1 = 33  ! Keyboard interrupt
  integer, parameter :: INT_IRQ2 = 34  ! Cascade to IRQ8-15
  integer, parameter :: INT_IRQ3 = 35  ! COM2 interrupt
  integer, parameter :: INT_IRQ4 = 36  ! COM1 interrupt
  integer, parameter :: INT_IRQ5 = 37  ! LPT2 interrupt
  integer, parameter :: INT_IRQ6 = 38  ! Floppy disk interrupt
  integer, parameter :: INT_IRQ7 = 39  ! LPT1 interrupt
  integer, parameter :: INT_IRQ8 = 40  ! Real-time clock interrupt
  integer, parameter :: INT_IRQ9 = 41  ! Redirected IRQ2
  integer, parameter :: INT_IRQ10 = 42  ! Reserved
  integer, parameter :: INT_IRQ11 = 43  ! Reserved
  integer, parameter :: INT_IRQ12 = 44  ! PS/2 mouse interrupt
  integer, parameter :: INT_IRQ13 = 45  ! Coprocessor interrupt
  integer, parameter :: INT_IRQ14 = 46  ! Primary IDE interrupt
  integer, parameter :: INT_IRQ15 = 47  ! Secondary IDE interrupt

end module intel 80286_device
