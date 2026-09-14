' Intel 80286寄存器定义
' 生成自: Intel/x86/Intel 80286
' 版本: 
' 日期: 
' 作者: 
' 描述: Intel 80286 16-bit microprocessor with memory management and protection

' CPU架构: x86-16
' 位宽: 0位
' 时钟频率: 0 Hz

' 外设定义
' Programmable Interrupt Controller
CONST _8259A_BASE = 
CONST _8259A_ICW1 = 0x20
CONST _8259A_ICW2 = 0x21
CONST _8259A_ICW3 = 0x21
CONST _8259A_ICW4 = 0x21
CONST _8259A_OCW1 = 0x21
CONST _8259A_OCW2 = 0x20
CONST _8259A_OCW3 = 0x20

' Programmable Interval Timer
CONST _8253_BASE = 
CONST _8253_COUNTER0 = 0x40
CONST _8253_COUNTER1 = 0x41
CONST _8253_COUNTER2 = 0x42
CONST _8253_CONTROL = 0x43

' Programmable Peripheral Interface
CONST _8255_BASE = 
CONST _8255_PORTA = 0x60
CONST _8255_PORTB = 0x61
CONST _8255_PORTC = 0x62
CONST _8255_CONTROL = 0x63

' Direct Memory Access Controller
CONST _8237_BASE = 
CONST _8237_CHANNEL0 = 0x00
CONST _8237_CHANNEL1 = 0x02
CONST _8237_CHANNEL2 = 0x04
CONST _8237_CHANNEL3 = 0x06
CONST _8237_STATUS = 0x08
CONST _8237_COMMAND = 0x08
CONST _8237_REQUEST = 0x09
CONST _8237_MASK = 0x0A
CONST _8237_MODE = 0x0B
CONST _8237_FLIPFLOP = 0x0C
CONST _8237_TEMP = 0x0D
CONST _8237_MASTERCLEAR = 0x0D
CONST _8237_MASKALL = 0x0F

' Keyboard Controller
CONST _8042_BASE = 
CONST _8042_DATA = 0x60
CONST _8042_STATUS = 0x64

' 中断向量定义
CONST DIVIDE_ERROR_VECTOR = 0  ' Division by zero or overflow
CONST DEBUG_EXCEPTION_VECTOR = 1  ' Single-step or debug register access
CONST NMI_VECTOR = 2  ' Non-maskable interrupt
CONST BREAKPOINT_VECTOR = 3  ' INT 3 instruction
CONST OVERFLOW_VECTOR = 4  ' INTO instruction with OF=1
CONST BOUNDS_CHECK_VECTOR = 5  ' BOUND instruction
CONST INVALID_OPCODE_VECTOR = 6  ' Undefined opcode
CONST COPROCESSOR_NOT_AVAILABLE_VECTOR = 7  ' No math coprocessor
CONST DOUBLE_FAULT_VECTOR = 8  ' Two exceptions in handler
CONST COPROCESSOR_SEGMENT_OVERRUN_VECTOR = 9  ' Coprocessor operand beyond segment
CONST INVALID_TSS_VECTOR = 10  ' Invalid Task State Segment
CONST SEGMENT_NOT_PRESENT_VECTOR = 11  ' Segment not present
CONST STACK_FAULT_VECTOR = 12  ' Stack segment limit violation
CONST GENERAL_PROTECTION_VECTOR = 13  ' Memory access violation
CONST PAGE_FAULT_VECTOR = 14  ' Page not present (386+)
CONST COPROCESSOR_ERROR_VECTOR = 16  ' Math coprocessor error
CONST IRQ0_VECTOR = 32  ' Timer interrupt
CONST IRQ1_VECTOR = 33  ' Keyboard interrupt
CONST IRQ2_VECTOR = 34  ' Cascade to IRQ8-15
CONST IRQ3_VECTOR = 35  ' COM2 interrupt
CONST IRQ4_VECTOR = 36  ' COM1 interrupt
CONST IRQ5_VECTOR = 37  ' LPT2 interrupt
CONST IRQ6_VECTOR = 38  ' Floppy disk interrupt
CONST IRQ7_VECTOR = 39  ' LPT1 interrupt
CONST IRQ8_VECTOR = 40  ' Real-time clock interrupt
CONST IRQ9_VECTOR = 41  ' Redirected IRQ2
CONST IRQ10_VECTOR = 42  ' Reserved
CONST IRQ11_VECTOR = 43  ' Reserved
CONST IRQ12_VECTOR = 44  ' PS/2 mouse interrupt
CONST IRQ13_VECTOR = 45  ' Coprocessor interrupt
CONST IRQ14_VECTOR = 46  ' Primary IDE interrupt
CONST IRQ15_VECTOR = 47  ' Secondary IDE interrupt

' 设备初始化子程序
SUB intel_80286_init()
    ' 初始化代码
END SUB

' 常用函数
FUNCTION read_register(addr AS INTEGER) AS INTEGER
    ' 读取寄存器值
    RETURN PEEK(addr)
END FUNCTION

SUB write_register(addr AS INTEGER, value AS INTEGER)
    ' 写入寄存器值
    POKE addr, value
END SUB
