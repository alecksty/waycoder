// Intel 80286 设备定义 - Objective-C 头文件
// 生成自: Intel/x86/Intel 80286
// 版本: 
// 日期: 
// 作者: 
// 描述: Intel 80286 16-bit microprocessor with memory management and protection
// CPU架构: x86-16
// 位宽: 0位
// 时钟频率: 0 Hz

#ifndef INTEL 80286_DEVICE_H
#define INTEL 80286_DEVICE_H

#import <Foundation/Foundation.h>

// 外设定义
// Programmable Interrupt Controller
#define _8259A_BASE 
#define _8259A_ICW1_ADDR 0x20
#define _8259A_ICW2_ADDR 0x21
#define _8259A_ICW3_ADDR 0x21
#define _8259A_ICW4_ADDR 0x21
#define _8259A_OCW1_ADDR 0x21
#define _8259A_OCW2_ADDR 0x20
#define _8259A_OCW3_ADDR 0x20
// Programmable Interval Timer
#define _8253_BASE 
#define _8253_COUNTER0_ADDR 0x40
#define _8253_COUNTER1_ADDR 0x41
#define _8253_COUNTER2_ADDR 0x42
#define _8253_CONTROL_ADDR 0x43
// Programmable Peripheral Interface
#define _8255_BASE 
#define _8255_PORTA_ADDR 0x60
#define _8255_PORTB_ADDR 0x61
#define _8255_PORTC_ADDR 0x62
#define _8255_CONTROL_ADDR 0x63
// Direct Memory Access Controller
#define _8237_BASE 
#define _8237_CHANNEL0_ADDR 0x00
#define _8237_CHANNEL1_ADDR 0x02
#define _8237_CHANNEL2_ADDR 0x04
#define _8237_CHANNEL3_ADDR 0x06
#define _8237_STATUS_ADDR 0x08
#define _8237_COMMAND_ADDR 0x08
#define _8237_REQUEST_ADDR 0x09
#define _8237_MASK_ADDR 0x0A
#define _8237_MODE_ADDR 0x0B
#define _8237_FLIPFLOP_ADDR 0x0C
#define _8237_TEMP_ADDR 0x0D
#define _8237_MASTERCLEAR_ADDR 0x0D
#define _8237_MASKALL_ADDR 0x0F
// Keyboard Controller
#define _8042_BASE 
#define _8042_DATA_ADDR 0x60
#define _8042_STATUS_ADDR 0x64

// 中断向量定义
#define INT_DIVIDE_ERROR 0  // Division by zero or overflow
#define INT_DEBUG_EXCEPTION 1  // Single-step or debug register access
#define INT_NMI 2  // Non-maskable interrupt
#define INT_BREAKPOINT 3  // INT 3 instruction
#define INT_OVERFLOW 4  // INTO instruction with OF=1
#define INT_BOUNDS_CHECK 5  // BOUND instruction
#define INT_INVALID_OPCODE 6  // Undefined opcode
#define INT_COPROCESSOR_NOT_AVAILABLE 7  // No math coprocessor
#define INT_DOUBLE_FAULT 8  // Two exceptions in handler
#define INT_COPROCESSOR_SEGMENT_OVERRUN 9  // Coprocessor operand beyond segment
#define INT_INVALID_TSS 10  // Invalid Task State Segment
#define INT_SEGMENT_NOT_PRESENT 11  // Segment not present
#define INT_STACK_FAULT 12  // Stack segment limit violation
#define INT_GENERAL_PROTECTION 13  // Memory access violation
#define INT_PAGE_FAULT 14  // Page not present (386+)
#define INT_COPROCESSOR_ERROR 16  // Math coprocessor error
#define INT_IRQ0 32  // Timer interrupt
#define INT_IRQ1 33  // Keyboard interrupt
#define INT_IRQ2 34  // Cascade to IRQ8-15
#define INT_IRQ3 35  // COM2 interrupt
#define INT_IRQ4 36  // COM1 interrupt
#define INT_IRQ5 37  // LPT2 interrupt
#define INT_IRQ6 38  // Floppy disk interrupt
#define INT_IRQ7 39  // LPT1 interrupt
#define INT_IRQ8 40  // Real-time clock interrupt
#define INT_IRQ9 41  // Redirected IRQ2
#define INT_IRQ10 42  // Reserved
#define INT_IRQ11 43  // Reserved
#define INT_IRQ12 44  // PS/2 mouse interrupt
#define INT_IRQ13 45  // Coprocessor interrupt
#define INT_IRQ14 46  // Primary IDE interrupt
#define INT_IRQ15 47  // Secondary IDE interrupt

#endif /* INTEL 80286_DEVICE_H */
