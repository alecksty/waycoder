#ifndef _8086_HPP
#define _8086_HPP

// 8086寄存器定义
// 生成自: Intel/x86/8086
// 版本: 1.0
// 日期: 2026-04-17


#ifdef __cplusplus
extern "C" {
#endif

// CPU架构: x86
// 位宽: 16位
// 时钟频率: 5000000 Hz

// 寄存器定义
// Accumulator
#define AX (*(volatile uint16_t*)0)
#define AX_AH 8  // High byte of AX
#define AX_AL 0  // Low byte of AX

// Base
#define BX (*(volatile uint16_t*)1)
#define BX_BH 8  // High byte of BX
#define BX_BL 0  // Low byte of BX

// Counter
#define CX (*(volatile uint16_t*)2)
#define CX_CH 8  // High byte of CX
#define CX_CL 0  // Low byte of CX

// Data
#define DX (*(volatile uint16_t*)3)
#define DX_DH 8  // High byte of DX
#define DX_DL 0  // Low byte of DX

// Source Index
#define SI (*(volatile uint16_t*)4)

// Destination Index
#define DI (*(volatile uint16_t*)5)

// Base Pointer
#define BP (*(volatile uint16_t*)6)

// Stack Pointer
#define SP (*(volatile uint16_t*)7)

// Instruction Pointer
#define IP (*(volatile uint16_t*)8)

// Code Segment
#define CS (*(volatile uint16_t*)9)

// Data Segment
#define DS (*(volatile uint16_t*)10)

// Extra Segment
#define ES (*(volatile uint16_t*)11)

// Stack Segment
#define SS (*(volatile uint16_t*)12)

// Flags Register
#define FLAGS (*(volatile uint16_t*)13)
#define FLAGS_CF 0  // Carry Flag
#define FLAGS_PF 2  // Parity Flag
#define FLAGS_AF 4  // Auxiliary Flag
#define FLAGS_ZF 6  // Zero Flag
#define FLAGS_SF 7  // Sign Flag
#define FLAGS_TF 8  // Trap Flag
#define FLAGS_IF 9  // Interrupt Enable Flag
#define FLAGS_DF 10  // Direction Flag
#define FLAGS_OF 11  // Overflow Flag

// 内存段定义
// 1MB address space
#define CODE_START 0x00000
#define CODE_END 0xFFFFF
#define CODE_SIZE 1048576

// Data memory
#define DATA_START 0x00000
#define DATA_END 0xFFFFF
#define DATA_SIZE 1048576

// Stack memory
#define STACK_START 0xF0000
#define STACK_END 0xFFFFF
#define STACK_SIZE 65536

// BIOS ROM
#define BIOS_START 0xF0000
#define BIOS_END 0xFFFFF
#define BIOS_SIZE 65536

// 外设定义
// Programmable Interrupt Controller
#define PIC_BASE 0x0020
#define PIC_PIC1_CMD (*(volatile uint8_t*)0x00000040)
#define PIC_PIC1_DATA (*(volatile uint8_t*)0x00000041)
#define PIC_PIC2_CMD (*(volatile uint8_t*)0x000000C0)
#define PIC_PIC2_DATA (*(volatile uint8_t*)0x000000C1)

// Programmable Interval Timer
#define PIT_BASE 0x0040
#define PIT_PIT_CH0 (*(volatile uint8_t*)0x00000080)
#define PIT_PIT_CH1 (*(volatile uint8_t*)0x00000081)
#define PIT_PIT_CH2 (*(volatile uint8_t*)0x00000082)
#define PIT_PIT_CMD (*(volatile uint8_t*)0x00000083)

// Programmable Peripheral Interface
#define PPI_BASE 0x0060
#define PPI_PPI_PA (*(volatile uint8_t*)0x000000C0)
#define PPI_PPI_PB (*(volatile uint8_t*)0x000000C1)
#define PPI_PPI_PC (*(volatile uint8_t*)0x000000C2)
#define PPI_PPI_CMD (*(volatile uint8_t*)0x000000C3)

// 中断向量定义
#define DIVIDE_ERROR_VECTOR 0  // Divide by zero
#define DEBUG_VECTOR 1  // Single step
#define NMI_VECTOR 2  // Non-maskable interrupt
#define BREAKPOINT_VECTOR 3  // Breakpoint
#define OVERFLOW_VECTOR 4  // INTO detected overflow
#define IRQ0_VECTOR 8  // Timer interrupt
#define IRQ1_VECTOR 9  // Keyboard interrupt
#define IRQ2_VECTOR 10  // Cascade
#define IRQ3_VECTOR 11  // COM2
#define IRQ4_VECTOR 12  // COM1
#define IRQ5_VECTOR 13  // LPT2
#define IRQ6_VECTOR 14  // Floppy disk
#define IRQ7_VECTOR 15  // LPT1

void _8086_init(void);

#ifdef __cplusplus
}
#endif

#endif // _8086_HPP
