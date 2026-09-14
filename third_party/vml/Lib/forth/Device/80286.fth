\ Intel 80286设备定义 - Forth文件
\ 生成自: Intel/x86/Intel 80286
\ 版本: 
\ 日期: 
\ 作者: 
\ 描述: Intel 80286 16-bit microprocessor with memory management and protection
\ CPU架构: x86-16
\ 位宽: 0位
\ 时钟频率: 0 Hz

\ =========================================
\ Intel 80286设备定义
\ =========================================

\ 设备信息
: DEVICE-NAME   S" Intel 80286" ;
: MANUFACTURER  S" Intel" ;
: FAMILY        S" x86" ;
: VERSION       S" " ;
: ARCHITECTURE  S" x86-16" ;
0 CONSTANT BITS
0 CONSTANT CLOCK-FREQ

\ 外设定义
\ Programmable Interrupt Controller
 CONSTANT _8259A-BASE
0x20 CONSTANT _8259A-ICW1
0x21 CONSTANT _8259A-ICW2
0x21 CONSTANT _8259A-ICW3
0x21 CONSTANT _8259A-ICW4
0x21 CONSTANT _8259A-OCW1
0x20 CONSTANT _8259A-OCW2
0x20 CONSTANT _8259A-OCW3
\ Programmable Interval Timer
 CONSTANT _8253-BASE
0x40 CONSTANT _8253-COUNTER0
0x41 CONSTANT _8253-COUNTER1
0x42 CONSTANT _8253-COUNTER2
0x43 CONSTANT _8253-CONTROL
\ Programmable Peripheral Interface
 CONSTANT _8255-BASE
0x60 CONSTANT _8255-PORTA
0x61 CONSTANT _8255-PORTB
0x62 CONSTANT _8255-PORTC
0x63 CONSTANT _8255-CONTROL
\ Direct Memory Access Controller
 CONSTANT _8237-BASE
0x00 CONSTANT _8237-CHANNEL0
0x02 CONSTANT _8237-CHANNEL1
0x04 CONSTANT _8237-CHANNEL2
0x06 CONSTANT _8237-CHANNEL3
0x08 CONSTANT _8237-STATUS
0x08 CONSTANT _8237-COMMAND
0x09 CONSTANT _8237-REQUEST
0x0A CONSTANT _8237-MASK
0x0B CONSTANT _8237-MODE
0x0C CONSTANT _8237-FLIPFLOP
0x0D CONSTANT _8237-TEMP
0x0D CONSTANT _8237-MASTERCLEAR
0x0F CONSTANT _8237-MASKALL
\ Keyboard Controller
 CONSTANT _8042-BASE
0x60 CONSTANT _8042-DATA
0x64 CONSTANT _8042-STATUS

\ 中断向量定义
0 CONSTANT INT-DIVIDE_ERROR  \ Division by zero or overflow
1 CONSTANT INT-DEBUG_EXCEPTION  \ Single-step or debug register access
2 CONSTANT INT-NMI  \ Non-maskable interrupt
3 CONSTANT INT-BREAKPOINT  \ INT 3 instruction
4 CONSTANT INT-OVERFLOW  \ INTO instruction with OF=1
5 CONSTANT INT-BOUNDS_CHECK  \ BOUND instruction
6 CONSTANT INT-INVALID_OPCODE  \ Undefined opcode
7 CONSTANT INT-COPROCESSOR_NOT_AVAILABLE  \ No math coprocessor
8 CONSTANT INT-DOUBLE_FAULT  \ Two exceptions in handler
9 CONSTANT INT-COPROCESSOR_SEGMENT_OVERRUN  \ Coprocessor operand beyond segment
10 CONSTANT INT-INVALID_TSS  \ Invalid Task State Segment
11 CONSTANT INT-SEGMENT_NOT_PRESENT  \ Segment not present
12 CONSTANT INT-STACK_FAULT  \ Stack segment limit violation
13 CONSTANT INT-GENERAL_PROTECTION  \ Memory access violation
14 CONSTANT INT-PAGE_FAULT  \ Page not present (386+)
16 CONSTANT INT-COPROCESSOR_ERROR  \ Math coprocessor error
32 CONSTANT INT-IRQ0  \ Timer interrupt
33 CONSTANT INT-IRQ1  \ Keyboard interrupt
34 CONSTANT INT-IRQ2  \ Cascade to IRQ8-15
35 CONSTANT INT-IRQ3  \ COM2 interrupt
36 CONSTANT INT-IRQ4  \ COM1 interrupt
37 CONSTANT INT-IRQ5  \ LPT2 interrupt
38 CONSTANT INT-IRQ6  \ Floppy disk interrupt
39 CONSTANT INT-IRQ7  \ LPT1 interrupt
40 CONSTANT INT-IRQ8  \ Real-time clock interrupt
41 CONSTANT INT-IRQ9  \ Redirected IRQ2
42 CONSTANT INT-IRQ10  \ Reserved
43 CONSTANT INT-IRQ11  \ Reserved
44 CONSTANT INT-IRQ12  \ PS/2 mouse interrupt
45 CONSTANT INT-IRQ13  \ Coprocessor interrupt
46 CONSTANT INT-IRQ14  \ Primary IDE interrupt
47 CONSTANT INT-IRQ15  \ Secondary IDE interrupt

\ =========================================
\ 寄存器访问字
\ =========================================

\ 外设访问
\ 8259A外设
: _8259A-ICW1@ ( -- n ) _8259A-ICW1 XL@ ;
: _8259A-ICW1! ( n -- ) _8259A-ICW1 XL! ;
: _8259A-ICW2@ ( -- n ) _8259A-ICW2 XL@ ;
: _8259A-ICW2! ( n -- ) _8259A-ICW2 XL! ;
: _8259A-ICW3@ ( -- n ) _8259A-ICW3 XL@ ;
: _8259A-ICW3! ( n -- ) _8259A-ICW3 XL! ;
: _8259A-ICW4@ ( -- n ) _8259A-ICW4 XL@ ;
: _8259A-ICW4! ( n -- ) _8259A-ICW4 XL! ;
: _8259A-OCW1@ ( -- n ) _8259A-OCW1 XL@ ;
: _8259A-OCW1! ( n -- ) _8259A-OCW1 XL! ;
: _8259A-OCW2@ ( -- n ) _8259A-OCW2 XL@ ;
: _8259A-OCW2! ( n -- ) _8259A-OCW2 XL! ;
: _8259A-OCW3@ ( -- n ) _8259A-OCW3 XL@ ;
: _8259A-OCW3! ( n -- ) _8259A-OCW3 XL! ;

\ 8253外设
: _8253-COUNTER0@ ( -- n ) _8253-COUNTER0 XL@ ;
: _8253-COUNTER0! ( n -- ) _8253-COUNTER0 XL! ;
: _8253-COUNTER1@ ( -- n ) _8253-COUNTER1 XL@ ;
: _8253-COUNTER1! ( n -- ) _8253-COUNTER1 XL! ;
: _8253-COUNTER2@ ( -- n ) _8253-COUNTER2 XL@ ;
: _8253-COUNTER2! ( n -- ) _8253-COUNTER2 XL! ;
: _8253-CONTROL@ ( -- n ) _8253-CONTROL XL@ ;
: _8253-CONTROL! ( n -- ) _8253-CONTROL XL! ;

\ 8255外设
: _8255-PORTA@ ( -- n ) _8255-PORTA XL@ ;
: _8255-PORTA! ( n -- ) _8255-PORTA XL! ;
: _8255-PORTB@ ( -- n ) _8255-PORTB XL@ ;
: _8255-PORTB! ( n -- ) _8255-PORTB XL! ;
: _8255-PORTC@ ( -- n ) _8255-PORTC XL@ ;
: _8255-PORTC! ( n -- ) _8255-PORTC XL! ;
: _8255-CONTROL@ ( -- n ) _8255-CONTROL XL@ ;
: _8255-CONTROL! ( n -- ) _8255-CONTROL XL! ;

\ 8237外设
: _8237-CHANNEL0@ ( -- n ) _8237-CHANNEL0  16 CHARS@ ;
: _8237-CHANNEL0! ( n -- ) _8237-CHANNEL0  16 CHARS! ;
: _8237-CHANNEL1@ ( -- n ) _8237-CHANNEL1  16 CHARS@ ;
: _8237-CHANNEL1! ( n -- ) _8237-CHANNEL1  16 CHARS! ;
: _8237-CHANNEL2@ ( -- n ) _8237-CHANNEL2  16 CHARS@ ;
: _8237-CHANNEL2! ( n -- ) _8237-CHANNEL2  16 CHARS! ;
: _8237-CHANNEL3@ ( -- n ) _8237-CHANNEL3  16 CHARS@ ;
: _8237-CHANNEL3! ( n -- ) _8237-CHANNEL3  16 CHARS! ;
: _8237-STATUS@ ( -- n ) _8237-STATUS XL@ ;
: _8237-STATUS! ( n -- ) _8237-STATUS XL! ;
: _8237-COMMAND@ ( -- n ) _8237-COMMAND XL@ ;
: _8237-COMMAND! ( n -- ) _8237-COMMAND XL! ;
: _8237-REQUEST@ ( -- n ) _8237-REQUEST XL@ ;
: _8237-REQUEST! ( n -- ) _8237-REQUEST XL! ;
: _8237-MASK@ ( -- n ) _8237-MASK XL@ ;
: _8237-MASK! ( n -- ) _8237-MASK XL! ;
: _8237-MODE@ ( -- n ) _8237-MODE XL@ ;
: _8237-MODE! ( n -- ) _8237-MODE XL! ;
: _8237-FLIPFLOP@ ( -- n ) _8237-FLIPFLOP XL@ ;
: _8237-FLIPFLOP! ( n -- ) _8237-FLIPFLOP XL! ;
: _8237-TEMP@ ( -- n ) _8237-TEMP XL@ ;
: _8237-TEMP! ( n -- ) _8237-TEMP XL! ;
: _8237-MASTERCLEAR@ ( -- n ) _8237-MASTERCLEAR XL@ ;
: _8237-MASTERCLEAR! ( n -- ) _8237-MASTERCLEAR XL! ;
: _8237-MASKALL@ ( -- n ) _8237-MASKALL XL@ ;
: _8237-MASKALL! ( n -- ) _8237-MASKALL XL! ;

\ 8042外设
: _8042-DATA@ ( -- n ) _8042-DATA XL@ ;
: _8042-DATA! ( n -- ) _8042-DATA XL! ;
: _8042-STATUS@ ( -- n ) _8042-STATUS XL@ ;
: _8042-STATUS! ( n -- ) _8042-STATUS XL! ;

\ =========================================
\ 设备初始化
\ =========================================

: INTEL_80286-INIT ( -- )
  \ 初始化Intel 80286设备
  ." 初始化Intel 80286..." CR


  \ 初始化外设
  \ 初始化8259A
  0 _8259A-ICW1!  \ ICW1寄存器
  0 _8259A-ICW2!  \ ICW2寄存器
  0 _8259A-ICW3!  \ ICW3寄存器
  0 _8259A-ICW4!  \ ICW4寄存器
  0 _8259A-OCW1!  \ OCW1寄存器
  0 _8259A-OCW2!  \ OCW2寄存器
  0 _8259A-OCW3!  \ OCW3寄存器
  \ 初始化8253
  0 _8253-COUNTER0!  \ Counter0寄存器
  0 _8253-COUNTER1!  \ Counter1寄存器
  0 _8253-COUNTER2!  \ Counter2寄存器
  0 _8253-CONTROL!  \ Control寄存器
  \ 初始化8255
  0 _8255-PORTA!  \ PortA寄存器
  0 _8255-PORTB!  \ PortB寄存器
  0 _8255-PORTC!  \ PortC寄存器
  0 _8255-CONTROL!  \ Control寄存器
  \ 初始化8237
  0 _8237-CHANNEL0!  \ Channel0寄存器
  0 _8237-CHANNEL1!  \ Channel1寄存器
  0 _8237-CHANNEL2!  \ Channel2寄存器
  0 _8237-CHANNEL3!  \ Channel3寄存器
  0 _8237-STATUS!  \ Status寄存器
  0 _8237-COMMAND!  \ Command寄存器
  0 _8237-REQUEST!  \ Request寄存器
  0 _8237-MASK!  \ Mask寄存器
  0 _8237-MODE!  \ Mode寄存器
  0 _8237-FLIPFLOP!  \ FlipFlop寄存器
  0 _8237-TEMP!  \ Temp寄存器
  0 _8237-MASTERCLEAR!  \ MasterClear寄存器
  0 _8237-MASKALL!  \ MaskAll寄存器
  \ 初始化8042
  0 _8042-DATA!  \ Data寄存器
  0 _8042-STATUS!  \ Status寄存器

  ." Intel 80286初始化完成" CR
;

\ =========================================
\ 设备信息显示
\ =========================================

: .DEVICE-INFO ( -- )
  CR
  ." 设备: " DEVICE-NAME TYPE CR
  ." 厂商: " MANUFACTURER TYPE CR
  ." 系列: " FAMILY TYPE CR
  ." 版本: " VERSION TYPE CR
  ." 架构: " ARCHITECTURE TYPE CR
  ." 位宽: " BITS . CR
  ." 时钟: " CLOCK-FREQ . ." Hz" CR
;

\ =========================================
\ 中断处理
\ =========================================

\ Division by zero or overflow
: INT-DIVIDE_ERROR-HANDLER ( -- )
  ." Divide Error中断处理" CR
  \ 添加具体的中断处理代码
;

: INT-DIVIDE_ERROR-ENABLE ( -- )
  INT-DIVIDE_ERROR INT-ENABLE
;

: INT-DIVIDE_ERROR-DISABLE ( -- )
  INT-DIVIDE_ERROR INT-DISABLE
;

\ Single-step or debug register access
: INT-DEBUG_EXCEPTION-HANDLER ( -- )
  ." Debug Exception中断处理" CR
  \ 添加具体的中断处理代码
;

: INT-DEBUG_EXCEPTION-ENABLE ( -- )
  INT-DEBUG_EXCEPTION INT-ENABLE
;

: INT-DEBUG_EXCEPTION-DISABLE ( -- )
  INT-DEBUG_EXCEPTION INT-DISABLE
;

\ Non-maskable interrupt
: INT-NMI-HANDLER ( -- )
  ." NMI中断处理" CR
  \ 添加具体的中断处理代码
;

: INT-NMI-ENABLE ( -- )
  INT-NMI INT-ENABLE
;

: INT-NMI-DISABLE ( -- )
  INT-NMI INT-DISABLE
;

\ INT 3 instruction
: INT-BREAKPOINT-HANDLER ( -- )
  ." Breakpoint中断处理" CR
  \ 添加具体的中断处理代码
;

: INT-BREAKPOINT-ENABLE ( -- )
  INT-BREAKPOINT INT-ENABLE
;

: INT-BREAKPOINT-DISABLE ( -- )
  INT-BREAKPOINT INT-DISABLE
;

\ INTO instruction with OF=1
: INT-OVERFLOW-HANDLER ( -- )
  ." Overflow中断处理" CR
  \ 添加具体的中断处理代码
;

: INT-OVERFLOW-ENABLE ( -- )
  INT-OVERFLOW INT-ENABLE
;

: INT-OVERFLOW-DISABLE ( -- )
  INT-OVERFLOW INT-DISABLE
;

\ BOUND instruction
: INT-BOUNDS_CHECK-HANDLER ( -- )
  ." Bounds Check中断处理" CR
  \ 添加具体的中断处理代码
;

: INT-BOUNDS_CHECK-ENABLE ( -- )
  INT-BOUNDS_CHECK INT-ENABLE
;

: INT-BOUNDS_CHECK-DISABLE ( -- )
  INT-BOUNDS_CHECK INT-DISABLE
;

\ Undefined opcode
: INT-INVALID_OPCODE-HANDLER ( -- )
  ." Invalid Opcode中断处理" CR
  \ 添加具体的中断处理代码
;

: INT-INVALID_OPCODE-ENABLE ( -- )
  INT-INVALID_OPCODE INT-ENABLE
;

: INT-INVALID_OPCODE-DISABLE ( -- )
  INT-INVALID_OPCODE INT-DISABLE
;

\ No math coprocessor
: INT-COPROCESSOR_NOT_AVAILABLE-HANDLER ( -- )
  ." Coprocessor Not Available中断处理" CR
  \ 添加具体的中断处理代码
;

: INT-COPROCESSOR_NOT_AVAILABLE-ENABLE ( -- )
  INT-COPROCESSOR_NOT_AVAILABLE INT-ENABLE
;

: INT-COPROCESSOR_NOT_AVAILABLE-DISABLE ( -- )
  INT-COPROCESSOR_NOT_AVAILABLE INT-DISABLE
;

\ Two exceptions in handler
: INT-DOUBLE_FAULT-HANDLER ( -- )
  ." Double Fault中断处理" CR
  \ 添加具体的中断处理代码
;

: INT-DOUBLE_FAULT-ENABLE ( -- )
  INT-DOUBLE_FAULT INT-ENABLE
;

: INT-DOUBLE_FAULT-DISABLE ( -- )
  INT-DOUBLE_FAULT INT-DISABLE
;

\ Coprocessor operand beyond segment
: INT-COPROCESSOR_SEGMENT_OVERRUN-HANDLER ( -- )
  ." Coprocessor Segment Overrun中断处理" CR
  \ 添加具体的中断处理代码
;

: INT-COPROCESSOR_SEGMENT_OVERRUN-ENABLE ( -- )
  INT-COPROCESSOR_SEGMENT_OVERRUN INT-ENABLE
;

: INT-COPROCESSOR_SEGMENT_OVERRUN-DISABLE ( -- )
  INT-COPROCESSOR_SEGMENT_OVERRUN INT-DISABLE
;

\ Invalid Task State Segment
: INT-INVALID_TSS-HANDLER ( -- )
  ." Invalid TSS中断处理" CR
  \ 添加具体的中断处理代码
;

: INT-INVALID_TSS-ENABLE ( -- )
  INT-INVALID_TSS INT-ENABLE
;

: INT-INVALID_TSS-DISABLE ( -- )
  INT-INVALID_TSS INT-DISABLE
;

\ Segment not present
: INT-SEGMENT_NOT_PRESENT-HANDLER ( -- )
  ." Segment Not Present中断处理" CR
  \ 添加具体的中断处理代码
;

: INT-SEGMENT_NOT_PRESENT-ENABLE ( -- )
  INT-SEGMENT_NOT_PRESENT INT-ENABLE
;

: INT-SEGMENT_NOT_PRESENT-DISABLE ( -- )
  INT-SEGMENT_NOT_PRESENT INT-DISABLE
;

\ Stack segment limit violation
: INT-STACK_FAULT-HANDLER ( -- )
  ." Stack Fault中断处理" CR
  \ 添加具体的中断处理代码
;

: INT-STACK_FAULT-ENABLE ( -- )
  INT-STACK_FAULT INT-ENABLE
;

: INT-STACK_FAULT-DISABLE ( -- )
  INT-STACK_FAULT INT-DISABLE
;

\ Memory access violation
: INT-GENERAL_PROTECTION-HANDLER ( -- )
  ." General Protection中断处理" CR
  \ 添加具体的中断处理代码
;

: INT-GENERAL_PROTECTION-ENABLE ( -- )
  INT-GENERAL_PROTECTION INT-ENABLE
;

: INT-GENERAL_PROTECTION-DISABLE ( -- )
  INT-GENERAL_PROTECTION INT-DISABLE
;

\ Page not present (386+)
: INT-PAGE_FAULT-HANDLER ( -- )
  ." Page Fault中断处理" CR
  \ 添加具体的中断处理代码
;

: INT-PAGE_FAULT-ENABLE ( -- )
  INT-PAGE_FAULT INT-ENABLE
;

: INT-PAGE_FAULT-DISABLE ( -- )
  INT-PAGE_FAULT INT-DISABLE
;

\ Math coprocessor error
: INT-COPROCESSOR_ERROR-HANDLER ( -- )
  ." Coprocessor Error中断处理" CR
  \ 添加具体的中断处理代码
;

: INT-COPROCESSOR_ERROR-ENABLE ( -- )
  INT-COPROCESSOR_ERROR INT-ENABLE
;

: INT-COPROCESSOR_ERROR-DISABLE ( -- )
  INT-COPROCESSOR_ERROR INT-DISABLE
;

\ Timer interrupt
: INT-IRQ0-HANDLER ( -- )
  ." IRQ0中断处理" CR
  \ 添加具体的中断处理代码
;

: INT-IRQ0-ENABLE ( -- )
  INT-IRQ0 INT-ENABLE
;

: INT-IRQ0-DISABLE ( -- )
  INT-IRQ0 INT-DISABLE
;

\ Keyboard interrupt
: INT-IRQ1-HANDLER ( -- )
  ." IRQ1中断处理" CR
  \ 添加具体的中断处理代码
;

: INT-IRQ1-ENABLE ( -- )
  INT-IRQ1 INT-ENABLE
;

: INT-IRQ1-DISABLE ( -- )
  INT-IRQ1 INT-DISABLE
;

\ Cascade to IRQ8-15
: INT-IRQ2-HANDLER ( -- )
  ." IRQ2中断处理" CR
  \ 添加具体的中断处理代码
;

: INT-IRQ2-ENABLE ( -- )
  INT-IRQ2 INT-ENABLE
;

: INT-IRQ2-DISABLE ( -- )
  INT-IRQ2 INT-DISABLE
;

\ COM2 interrupt
: INT-IRQ3-HANDLER ( -- )
  ." IRQ3中断处理" CR
  \ 添加具体的中断处理代码
;

: INT-IRQ3-ENABLE ( -- )
  INT-IRQ3 INT-ENABLE
;

: INT-IRQ3-DISABLE ( -- )
  INT-IRQ3 INT-DISABLE
;

\ COM1 interrupt
: INT-IRQ4-HANDLER ( -- )
  ." IRQ4中断处理" CR
  \ 添加具体的中断处理代码
;

: INT-IRQ4-ENABLE ( -- )
  INT-IRQ4 INT-ENABLE
;

: INT-IRQ4-DISABLE ( -- )
  INT-IRQ4 INT-DISABLE
;

\ LPT2 interrupt
: INT-IRQ5-HANDLER ( -- )
  ." IRQ5中断处理" CR
  \ 添加具体的中断处理代码
;

: INT-IRQ5-ENABLE ( -- )
  INT-IRQ5 INT-ENABLE
;

: INT-IRQ5-DISABLE ( -- )
  INT-IRQ5 INT-DISABLE
;

\ Floppy disk interrupt
: INT-IRQ6-HANDLER ( -- )
  ." IRQ6中断处理" CR
  \ 添加具体的中断处理代码
;

: INT-IRQ6-ENABLE ( -- )
  INT-IRQ6 INT-ENABLE
;

: INT-IRQ6-DISABLE ( -- )
  INT-IRQ6 INT-DISABLE
;

\ LPT1 interrupt
: INT-IRQ7-HANDLER ( -- )
  ." IRQ7中断处理" CR
  \ 添加具体的中断处理代码
;

: INT-IRQ7-ENABLE ( -- )
  INT-IRQ7 INT-ENABLE
;

: INT-IRQ7-DISABLE ( -- )
  INT-IRQ7 INT-DISABLE
;

\ Real-time clock interrupt
: INT-IRQ8-HANDLER ( -- )
  ." IRQ8中断处理" CR
  \ 添加具体的中断处理代码
;

: INT-IRQ8-ENABLE ( -- )
  INT-IRQ8 INT-ENABLE
;

: INT-IRQ8-DISABLE ( -- )
  INT-IRQ8 INT-DISABLE
;

\ Redirected IRQ2
: INT-IRQ9-HANDLER ( -- )
  ." IRQ9中断处理" CR
  \ 添加具体的中断处理代码
;

: INT-IRQ9-ENABLE ( -- )
  INT-IRQ9 INT-ENABLE
;

: INT-IRQ9-DISABLE ( -- )
  INT-IRQ9 INT-DISABLE
;

\ Reserved
: INT-IRQ10-HANDLER ( -- )
  ." IRQ10中断处理" CR
  \ 添加具体的中断处理代码
;

: INT-IRQ10-ENABLE ( -- )
  INT-IRQ10 INT-ENABLE
;

: INT-IRQ10-DISABLE ( -- )
  INT-IRQ10 INT-DISABLE
;

\ Reserved
: INT-IRQ11-HANDLER ( -- )
  ." IRQ11中断处理" CR
  \ 添加具体的中断处理代码
;

: INT-IRQ11-ENABLE ( -- )
  INT-IRQ11 INT-ENABLE
;

: INT-IRQ11-DISABLE ( -- )
  INT-IRQ11 INT-DISABLE
;

\ PS/2 mouse interrupt
: INT-IRQ12-HANDLER ( -- )
  ." IRQ12中断处理" CR
  \ 添加具体的中断处理代码
;

: INT-IRQ12-ENABLE ( -- )
  INT-IRQ12 INT-ENABLE
;

: INT-IRQ12-DISABLE ( -- )
  INT-IRQ12 INT-DISABLE
;

\ Coprocessor interrupt
: INT-IRQ13-HANDLER ( -- )
  ." IRQ13中断处理" CR
  \ 添加具体的中断处理代码
;

: INT-IRQ13-ENABLE ( -- )
  INT-IRQ13 INT-ENABLE
;

: INT-IRQ13-DISABLE ( -- )
  INT-IRQ13 INT-DISABLE
;

\ Primary IDE interrupt
: INT-IRQ14-HANDLER ( -- )
  ." IRQ14中断处理" CR
  \ 添加具体的中断处理代码
;

: INT-IRQ14-ENABLE ( -- )
  INT-IRQ14 INT-ENABLE
;

: INT-IRQ14-DISABLE ( -- )
  INT-IRQ14 INT-DISABLE
;

\ Secondary IDE interrupt
: INT-IRQ15-HANDLER ( -- )
  ." IRQ15中断处理" CR
  \ 添加具体的中断处理代码
;

: INT-IRQ15-ENABLE ( -- )
  INT-IRQ15 INT-ENABLE
;

: INT-IRQ15-DISABLE ( -- )
  INT-IRQ15 INT-DISABLE
;

\ =========================================
\ 示例程序
\ =========================================

: EXAMPLE ( -- )
  INTEL_80286-INIT
  .DEVICE-INFO
  .REGISTERS
  CR ." 示例程序运行完成" CR
;

\ 自动运行示例
( EXAMPLE )
