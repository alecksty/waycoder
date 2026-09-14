using System;

namespace VML.Device.Intel.Intel 80286
{
    /// <summary>
    /// Intel 80286 寄存器定义
    /// 生成自: Intel/x86/Intel 80286
    /// 版本: 
    /// </summary>
    public static class Intel 80286
    {
        // CPU架构: x86-16, 0位, 0 Hz

        // 外设定义
        // Programmable Interrupt Controller
        public const int _8259A_BASE = ;
        public static unsafe ulong* _8259A_ICW1 => (ulong*)0x00000020;
        public static unsafe ulong* _8259A_ICW2 => (ulong*)0x00000021;
        public static unsafe ulong* _8259A_ICW3 => (ulong*)0x00000021;
        public static unsafe ulong* _8259A_ICW4 => (ulong*)0x00000021;
        public static unsafe ulong* _8259A_OCW1 => (ulong*)0x00000021;
        public static unsafe ulong* _8259A_OCW2 => (ulong*)0x00000020;
        public static unsafe ulong* _8259A_OCW3 => (ulong*)0x00000020;

        // Programmable Interval Timer
        public const int _8253_BASE = ;
        public static unsafe ulong* _8253_COUNTER0 => (ulong*)0x00000040;
        public static unsafe ulong* _8253_COUNTER1 => (ulong*)0x00000041;
        public static unsafe ulong* _8253_COUNTER2 => (ulong*)0x00000042;
        public static unsafe ulong* _8253_CONTROL => (ulong*)0x00000043;

        // Programmable Peripheral Interface
        public const int _8255_BASE = ;
        public static unsafe ulong* _8255_PORTA => (ulong*)0x00000060;
        public static unsafe ulong* _8255_PORTB => (ulong*)0x00000061;
        public static unsafe ulong* _8255_PORTC => (ulong*)0x00000062;
        public static unsafe ulong* _8255_CONTROL => (ulong*)0x00000063;

        // Direct Memory Access Controller
        public const int _8237_BASE = ;
        public static unsafe uint* _8237_CHANNEL0 => (uint*)0x00000000;
        public static unsafe uint* _8237_CHANNEL1 => (uint*)0x00000002;
        public static unsafe uint* _8237_CHANNEL2 => (uint*)0x00000004;
        public static unsafe uint* _8237_CHANNEL3 => (uint*)0x00000006;
        public static unsafe ulong* _8237_STATUS => (ulong*)0x00000008;
        public static unsafe ulong* _8237_COMMAND => (ulong*)0x00000008;
        public static unsafe ulong* _8237_REQUEST => (ulong*)0x00000009;
        public static unsafe ulong* _8237_MASK => (ulong*)0x0000000A;
        public static unsafe ulong* _8237_MODE => (ulong*)0x0000000B;
        public static unsafe ulong* _8237_FLIPFLOP => (ulong*)0x0000000C;
        public static unsafe ulong* _8237_TEMP => (ulong*)0x0000000D;
        public static unsafe ulong* _8237_MASTERCLEAR => (ulong*)0x0000000D;
        public static unsafe ulong* _8237_MASKALL => (ulong*)0x0000000F;

        // Keyboard Controller
        public const int _8042_BASE = ;
        public static unsafe ulong* _8042_DATA => (ulong*)0x00000060;
        public static unsafe ulong* _8042_STATUS => (ulong*)0x00000064;

        // 中断向量定义
        public const int IRQ_DIVIDE_ERROR = 0;  // Division by zero or overflow
        public const int IRQ_DEBUG_EXCEPTION = 1;  // Single-step or debug register access
        public const int IRQ_NMI = 2;  // Non-maskable interrupt
        public const int IRQ_BREAKPOINT = 3;  // INT 3 instruction
        public const int IRQ_OVERFLOW = 4;  // INTO instruction with OF=1
        public const int IRQ_BOUNDS_CHECK = 5;  // BOUND instruction
        public const int IRQ_INVALID_OPCODE = 6;  // Undefined opcode
        public const int IRQ_COPROCESSOR_NOT_AVAILABLE = 7;  // No math coprocessor
        public const int IRQ_DOUBLE_FAULT = 8;  // Two exceptions in handler
        public const int IRQ_COPROCESSOR_SEGMENT_OVERRUN = 9;  // Coprocessor operand beyond segment
        public const int IRQ_INVALID_TSS = 10;  // Invalid Task State Segment
        public const int IRQ_SEGMENT_NOT_PRESENT = 11;  // Segment not present
        public const int IRQ_STACK_FAULT = 12;  // Stack segment limit violation
        public const int IRQ_GENERAL_PROTECTION = 13;  // Memory access violation
        public const int IRQ_PAGE_FAULT = 14;  // Page not present (386+)
        public const int IRQ_COPROCESSOR_ERROR = 16;  // Math coprocessor error
        public const int IRQ_IRQ0 = 32;  // Timer interrupt
        public const int IRQ_IRQ1 = 33;  // Keyboard interrupt
        public const int IRQ_IRQ2 = 34;  // Cascade to IRQ8-15
        public const int IRQ_IRQ3 = 35;  // COM2 interrupt
        public const int IRQ_IRQ4 = 36;  // COM1 interrupt
        public const int IRQ_IRQ5 = 37;  // LPT2 interrupt
        public const int IRQ_IRQ6 = 38;  // Floppy disk interrupt
        public const int IRQ_IRQ7 = 39;  // LPT1 interrupt
        public const int IRQ_IRQ8 = 40;  // Real-time clock interrupt
        public const int IRQ_IRQ9 = 41;  // Redirected IRQ2
        public const int IRQ_IRQ10 = 42;  // Reserved
        public const int IRQ_IRQ11 = 43;  // Reserved
        public const int IRQ_IRQ12 = 44;  // PS/2 mouse interrupt
        public const int IRQ_IRQ13 = 45;  // Coprocessor interrupt
        public const int IRQ_IRQ14 = 46;  // Primary IDE interrupt
        public const int IRQ_IRQ15 = 47;  // Secondary IDE interrupt

        public static void intel_80286_init()
        {
            // 硬件初始化代码
        }
    }
}
