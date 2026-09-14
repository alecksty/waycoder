package vml.device.intel.intel_80286;

/**
 * Intel 80286 寄存器定义
 * 生成自: Intel/x86/Intel 80286
 * 版本: 
 */
public final class Intel 80286 {
    private Intel 80286() {} // 工具类
    // CPU架构: x86-16, 0位, 0 Hz

    // 外设定义
    // Programmable Interrupt Controller
    public static final int _8259A_BASE = (int);
    public static final int _8259A_ICW1 = (int)0x00000020;
    public static final int _8259A_ICW2 = (int)0x00000021;
    public static final int _8259A_ICW3 = (int)0x00000021;
    public static final int _8259A_ICW4 = (int)0x00000021;
    public static final int _8259A_OCW1 = (int)0x00000021;
    public static final int _8259A_OCW2 = (int)0x00000020;
    public static final int _8259A_OCW3 = (int)0x00000020;

    // Programmable Interval Timer
    public static final int _8253_BASE = (int);
    public static final int _8253_COUNTER0 = (int)0x00000040;
    public static final int _8253_COUNTER1 = (int)0x00000041;
    public static final int _8253_COUNTER2 = (int)0x00000042;
    public static final int _8253_CONTROL = (int)0x00000043;

    // Programmable Peripheral Interface
    public static final int _8255_BASE = (int);
    public static final int _8255_PORTA = (int)0x00000060;
    public static final int _8255_PORTB = (int)0x00000061;
    public static final int _8255_PORTC = (int)0x00000062;
    public static final int _8255_CONTROL = (int)0x00000063;

    // Direct Memory Access Controller
    public static final int _8237_BASE = (int);
    public static final int _8237_CHANNEL0 = (int)0x00000000;
    public static final int _8237_CHANNEL1 = (int)0x00000002;
    public static final int _8237_CHANNEL2 = (int)0x00000004;
    public static final int _8237_CHANNEL3 = (int)0x00000006;
    public static final int _8237_STATUS = (int)0x00000008;
    public static final int _8237_COMMAND = (int)0x00000008;
    public static final int _8237_REQUEST = (int)0x00000009;
    public static final int _8237_MASK = (int)0x0000000A;
    public static final int _8237_MODE = (int)0x0000000B;
    public static final int _8237_FLIPFLOP = (int)0x0000000C;
    public static final int _8237_TEMP = (int)0x0000000D;
    public static final int _8237_MASTERCLEAR = (int)0x0000000D;
    public static final int _8237_MASKALL = (int)0x0000000F;

    // Keyboard Controller
    public static final int _8042_BASE = (int);
    public static final int _8042_DATA = (int)0x00000060;
    public static final int _8042_STATUS = (int)0x00000064;

    // 中断向量定义
    public static final int IRQ_DIVIDE_ERROR = 0;  // Division by zero or overflow
    public static final int IRQ_DEBUG_EXCEPTION = 1;  // Single-step or debug register access
    public static final int IRQ_NMI = 2;  // Non-maskable interrupt
    public static final int IRQ_BREAKPOINT = 3;  // INT 3 instruction
    public static final int IRQ_OVERFLOW = 4;  // INTO instruction with OF=1
    public static final int IRQ_BOUNDS_CHECK = 5;  // BOUND instruction
    public static final int IRQ_INVALID_OPCODE = 6;  // Undefined opcode
    public static final int IRQ_COPROCESSOR_NOT_AVAILABLE = 7;  // No math coprocessor
    public static final int IRQ_DOUBLE_FAULT = 8;  // Two exceptions in handler
    public static final int IRQ_COPROCESSOR_SEGMENT_OVERRUN = 9;  // Coprocessor operand beyond segment
    public static final int IRQ_INVALID_TSS = 10;  // Invalid Task State Segment
    public static final int IRQ_SEGMENT_NOT_PRESENT = 11;  // Segment not present
    public static final int IRQ_STACK_FAULT = 12;  // Stack segment limit violation
    public static final int IRQ_GENERAL_PROTECTION = 13;  // Memory access violation
    public static final int IRQ_PAGE_FAULT = 14;  // Page not present (386+)
    public static final int IRQ_COPROCESSOR_ERROR = 16;  // Math coprocessor error
    public static final int IRQ_IRQ0 = 32;  // Timer interrupt
    public static final int IRQ_IRQ1 = 33;  // Keyboard interrupt
    public static final int IRQ_IRQ2 = 34;  // Cascade to IRQ8-15
    public static final int IRQ_IRQ3 = 35;  // COM2 interrupt
    public static final int IRQ_IRQ4 = 36;  // COM1 interrupt
    public static final int IRQ_IRQ5 = 37;  // LPT2 interrupt
    public static final int IRQ_IRQ6 = 38;  // Floppy disk interrupt
    public static final int IRQ_IRQ7 = 39;  // LPT1 interrupt
    public static final int IRQ_IRQ8 = 40;  // Real-time clock interrupt
    public static final int IRQ_IRQ9 = 41;  // Redirected IRQ2
    public static final int IRQ_IRQ10 = 42;  // Reserved
    public static final int IRQ_IRQ11 = 43;  // Reserved
    public static final int IRQ_IRQ12 = 44;  // PS/2 mouse interrupt
    public static final int IRQ_IRQ13 = 45;  // Coprocessor interrupt
    public static final int IRQ_IRQ14 = 46;  // Primary IDE interrupt
    public static final int IRQ_IRQ15 = 47;  // Secondary IDE interrupt

    public static native void intel_80286_init();
}
