#ifndef INTEL_80286_HPP
#define INTEL_80286_HPP

// Intel 80286寄存器定义
// 生成自: Intel/x86/Intel 80286
// 版本: 
// 日期: 


#ifdef __cplusplus
extern "C" {
#endif

// CPU架构: x86-16
// 位宽: 0位
// 时钟频率: 0 Hz

// 外设定义
// Programmable Interrupt Controller
#define _8259A_BASE 
#define _8259A_ICW1 (*(volatile uint64_t*)0x00000020)
#define _8259A_ICW2 (*(volatile uint64_t*)0x00000021)
#define _8259A_ICW3 (*(volatile uint64_t*)0x00000021)
#define _8259A_ICW4 (*(volatile uint64_t*)0x00000021)
#define _8259A_OCW1 (*(volatile uint64_t*)0x00000021)
#define _8259A_OCW2 (*(volatile uint64_t*)0x00000020)
#define _8259A_OCW3 (*(volatile uint64_t*)0x00000020)

// Programmable Interval Timer
#define _8253_BASE 
#define _8253_COUNTER0 (*(volatile uint64_t*)0x00000040)
#define _8253_COUNTER1 (*(volatile uint64_t*)0x00000041)
#define _8253_COUNTER2 (*(volatile uint64_t*)0x00000042)
#define _8253_CONTROL (*(volatile uint64_t*)0x00000043)

// Programmable Peripheral Interface
#define _8255_BASE 
#define _8255_PORTA (*(volatile uint64_t*)0x00000060)
#define _8255_PORTB (*(volatile uint64_t*)0x00000061)
#define _8255_PORTC (*(volatile uint64_t*)0x00000062)
#define _8255_CONTROL (*(volatile uint64_t*)0x00000063)

// Direct Memory Access Controller
#define _8237_BASE 
#define _8237_CHANNEL0 (*(volatile uint128_t*)0x00000000)
#define _8237_CHANNEL1 (*(volatile uint128_t*)0x00000002)
#define _8237_CHANNEL2 (*(volatile uint128_t*)0x00000004)
#define _8237_CHANNEL3 (*(volatile uint128_t*)0x00000006)
#define _8237_STATUS (*(volatile uint64_t*)0x00000008)
#define _8237_COMMAND (*(volatile uint64_t*)0x00000008)
#define _8237_REQUEST (*(volatile uint64_t*)0x00000009)
#define _8237_MASK (*(volatile uint64_t*)0x0000000A)
#define _8237_MODE (*(volatile uint64_t*)0x0000000B)
#define _8237_FLIPFLOP (*(volatile uint64_t*)0x0000000C)
#define _8237_TEMP (*(volatile uint64_t*)0x0000000D)
#define _8237_MASTERCLEAR (*(volatile uint64_t*)0x0000000D)
#define _8237_MASKALL (*(volatile uint64_t*)0x0000000F)

// Keyboard Controller
#define _8042_BASE 
#define _8042_DATA (*(volatile uint64_t*)0x00000060)
#define _8042_STATUS (*(volatile uint64_t*)0x00000064)

// 中断向量定义
#define DIVIDE_ERROR_VECTOR 0  // Division by zero or overflow
#define DEBUG_EXCEPTION_VECTOR 1  // Single-step or debug register access
#define NMI_VECTOR 2  // Non-maskable interrupt
#define BREAKPOINT_VECTOR 3  // INT 3 instruction
#define OVERFLOW_VECTOR 4  // INTO instruction with OF=1
#define BOUNDS_CHECK_VECTOR 5  // BOUND instruction
#define INVALID_OPCODE_VECTOR 6  // Undefined opcode
#define COPROCESSOR_NOT_AVAILABLE_VECTOR 7  // No math coprocessor
#define DOUBLE_FAULT_VECTOR 8  // Two exceptions in handler
#define COPROCESSOR_SEGMENT_OVERRUN_VECTOR 9  // Coprocessor operand beyond segment
#define INVALID_TSS_VECTOR 10  // Invalid Task State Segment
#define SEGMENT_NOT_PRESENT_VECTOR 11  // Segment not present
#define STACK_FAULT_VECTOR 12  // Stack segment limit violation
#define GENERAL_PROTECTION_VECTOR 13  // Memory access violation
#define PAGE_FAULT_VECTOR 14  // Page not present (386+)
#define COPROCESSOR_ERROR_VECTOR 16  // Math coprocessor error
#define IRQ0_VECTOR 32  // Timer interrupt
#define IRQ1_VECTOR 33  // Keyboard interrupt
#define IRQ2_VECTOR 34  // Cascade to IRQ8-15
#define IRQ3_VECTOR 35  // COM2 interrupt
#define IRQ4_VECTOR 36  // COM1 interrupt
#define IRQ5_VECTOR 37  // LPT2 interrupt
#define IRQ6_VECTOR 38  // Floppy disk interrupt
#define IRQ7_VECTOR 39  // LPT1 interrupt
#define IRQ8_VECTOR 40  // Real-time clock interrupt
#define IRQ9_VECTOR 41  // Redirected IRQ2
#define IRQ10_VECTOR 42  // Reserved
#define IRQ11_VECTOR 43  // Reserved
#define IRQ12_VECTOR 44  // PS/2 mouse interrupt
#define IRQ13_VECTOR 45  // Coprocessor interrupt
#define IRQ14_VECTOR 46  // Primary IDE interrupt
#define IRQ15_VECTOR 47  // Secondary IDE interrupt

void intel_80286_init(void);

#ifdef __cplusplus
}
#endif

#endif // INTEL_80286_HPP
