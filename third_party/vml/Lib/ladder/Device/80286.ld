(*
 * Intel 80286梯形图定义
 * 生成自: Intel/x86/Intel 80286
 * 版本: 
 * 日期: 
 * 作者: 
 * 描述: Intel 80286 16-bit microprocessor with memory management and protection
 * CPU架构: x86-16
 * 位宽: 0位
 * 时钟频率: 0 Hz
 *)

PROGRAM Intel 80286
VAR

    (* 输入变量 *)

    (* 输出变量 *)

    (* 内存变量 *)

    (* 外设变量 *)
    (* Programmable Interrupt Controller *)
    _8259A_BASE AT %MW : WORD;
    _8259A_ICW1 AT %MW0x20 : LWORD;
    _8259A_ICW2 AT %MW0x21 : LWORD;
    _8259A_ICW3 AT %MW0x21 : LWORD;
    _8259A_ICW4 AT %MW0x21 : LWORD;
    _8259A_OCW1 AT %MW0x21 : LWORD;
    _8259A_OCW2 AT %MW0x20 : LWORD;
    _8259A_OCW3 AT %MW0x20 : LWORD;
    (* Programmable Interval Timer *)
    _8253_BASE AT %MW : WORD;
    _8253_COUNTER0 AT %MW0x40 : LWORD;
    _8253_COUNTER1 AT %MW0x41 : LWORD;
    _8253_COUNTER2 AT %MW0x42 : LWORD;
    _8253_CONTROL AT %MW0x43 : LWORD;
    (* Programmable Peripheral Interface *)
    _8255_BASE AT %MW : WORD;
    _8255_PORTA AT %MW0x60 : LWORD;
    _8255_PORTB AT %MW0x61 : LWORD;
    _8255_PORTC AT %MW0x62 : LWORD;
    _8255_CONTROL AT %MW0x63 : LWORD;
    (* Direct Memory Access Controller *)
    _8237_BASE AT %MW : WORD;
    _8237_CHANNEL0 AT %MW0x00 : ARRAY[0..15] OF BOOL;
    _8237_CHANNEL1 AT %MW0x02 : ARRAY[0..15] OF BOOL;
    _8237_CHANNEL2 AT %MW0x04 : ARRAY[0..15] OF BOOL;
    _8237_CHANNEL3 AT %MW0x06 : ARRAY[0..15] OF BOOL;
    _8237_STATUS AT %MW0x08 : LWORD;
    _8237_COMMAND AT %MW0x08 : LWORD;
    _8237_REQUEST AT %MW0x09 : LWORD;
    _8237_MASK AT %MW0x0A : LWORD;
    _8237_MODE AT %MW0x0B : LWORD;
    _8237_FLIPFLOP AT %MW0x0C : LWORD;
    _8237_TEMP AT %MW0x0D : LWORD;
    _8237_MASTERCLEAR AT %MW0x0D : LWORD;
    _8237_MASKALL AT %MW0x0F : LWORD;
    (* Keyboard Controller *)
    _8042_BASE AT %MW : WORD;
    _8042_DATA AT %MW0x60 : LWORD;
    _8042_STATUS AT %MW0x64 : LWORD;

    (* 中断变量 *)
    INT_DIVIDE_ERROR : BOOL; (* Division by zero or overflow *)
    INT_DEBUG_EXCEPTION : BOOL; (* Single-step or debug register access *)
    INT_NMI : BOOL; (* Non-maskable interrupt *)
    INT_BREAKPOINT : BOOL; (* INT 3 instruction *)
    INT_OVERFLOW : BOOL; (* INTO instruction with OF=1 *)
    INT_BOUNDS_CHECK : BOOL; (* BOUND instruction *)
    INT_INVALID_OPCODE : BOOL; (* Undefined opcode *)
    INT_COPROCESSOR_NOT_AVAILABLE : BOOL; (* No math coprocessor *)
    INT_DOUBLE_FAULT : BOOL; (* Two exceptions in handler *)
    INT_COPROCESSOR_SEGMENT_OVERRUN : BOOL; (* Coprocessor operand beyond segment *)
    INT_INVALID_TSS : BOOL; (* Invalid Task State Segment *)
    INT_SEGMENT_NOT_PRESENT : BOOL; (* Segment not present *)
    INT_STACK_FAULT : BOOL; (* Stack segment limit violation *)
    INT_GENERAL_PROTECTION : BOOL; (* Memory access violation *)
    INT_PAGE_FAULT : BOOL; (* Page not present (386+) *)
    INT_COPROCESSOR_ERROR : BOOL; (* Math coprocessor error *)
    INT_IRQ0 : BOOL; (* Timer interrupt *)
    INT_IRQ1 : BOOL; (* Keyboard interrupt *)
    INT_IRQ2 : BOOL; (* Cascade to IRQ8-15 *)
    INT_IRQ3 : BOOL; (* COM2 interrupt *)
    INT_IRQ4 : BOOL; (* COM1 interrupt *)
    INT_IRQ5 : BOOL; (* LPT2 interrupt *)
    INT_IRQ6 : BOOL; (* Floppy disk interrupt *)
    INT_IRQ7 : BOOL; (* LPT1 interrupt *)
    INT_IRQ8 : BOOL; (* Real-time clock interrupt *)
    INT_IRQ9 : BOOL; (* Redirected IRQ2 *)
    INT_IRQ10 : BOOL; (* Reserved *)
    INT_IRQ11 : BOOL; (* Reserved *)
    INT_IRQ12 : BOOL; (* PS/2 mouse interrupt *)
    INT_IRQ13 : BOOL; (* Coprocessor interrupt *)
    INT_IRQ14 : BOOL; (* Primary IDE interrupt *)
    INT_IRQ15 : BOOL; (* Secondary IDE interrupt *)

END_VAR

BEGIN
    (* Intel 80286初始化梯形图 *)
    (* 网络 1: 设备初始化 *)
    [初始化完成]

    (* 网络 2: 输入监控示例 *)
    (* 网络 4: 中断处理 *)
    --|INT_DIVIDE_ERROR|--[中断处理]--
    --|INT_DEBUG_EXCEPTION|--[中断处理]--
    --|INT_NMI|--[中断处理]--
    --|INT_BREAKPOINT|--[中断处理]--
    --|INT_OVERFLOW|--[中断处理]--
    --|INT_BOUNDS_CHECK|--[中断处理]--
    --|INT_INVALID_OPCODE|--[中断处理]--
    --|INT_COPROCESSOR_NOT_AVAILABLE|--[中断处理]--
    --|INT_DOUBLE_FAULT|--[中断处理]--
    --|INT_COPROCESSOR_SEGMENT_OVERRUN|--[中断处理]--
    --|INT_INVALID_TSS|--[中断处理]--
    --|INT_SEGMENT_NOT_PRESENT|--[中断处理]--
    --|INT_STACK_FAULT|--[中断处理]--
    --|INT_GENERAL_PROTECTION|--[中断处理]--
    --|INT_PAGE_FAULT|--[中断处理]--
    --|INT_COPROCESSOR_ERROR|--[中断处理]--
    --|INT_IRQ0|--[中断处理]--
    --|INT_IRQ1|--[中断处理]--
    --|INT_IRQ2|--[中断处理]--
    --|INT_IRQ3|--[中断处理]--
    --|INT_IRQ4|--[中断处理]--
    --|INT_IRQ5|--[中断处理]--
    --|INT_IRQ6|--[中断处理]--
    --|INT_IRQ7|--[中断处理]--
    --|INT_IRQ8|--[中断处理]--
    --|INT_IRQ9|--[中断处理]--
    --|INT_IRQ10|--[中断处理]--
    --|INT_IRQ11|--[中断处理]--
    --|INT_IRQ12|--[中断处理]--
    --|INT_IRQ13|--[中断处理]--
    --|INT_IRQ14|--[中断处理]--
    --|INT_IRQ15|--[中断处理]--

END_PROGRAM
