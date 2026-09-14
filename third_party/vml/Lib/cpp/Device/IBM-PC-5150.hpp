#ifndef IBM_PC_5150_HPP
#define IBM_PC_5150_HPP

// IBM-PC-5150寄存器定义
// 生成自: IBM/Personal Computer/IBM-PC-5150
// 版本: 1.0
// 日期: 2026-04-16


#ifdef __cplusplus
extern "C" {
#endif

// CPU架构: x86
// 位宽: 16位
// 时钟频率: 4772727 Hz

// 寄存器定义
// Accumulator Register
#define AX (*(volatile uint16_t*)0x0)

// Base Register
#define BX (*(volatile uint16_t*)0x1)

// Count Register
#define CX (*(volatile uint16_t*)0x2)

// Data Register
#define DX (*(volatile uint16_t*)0x3)

// Source Index
#define SI (*(volatile uint16_t*)0x4)

// Destination Index
#define DI (*(volatile uint16_t*)0x5)

// Base Pointer
#define BP (*(volatile uint16_t*)0x6)

// Stack Pointer
#define SP (*(volatile uint16_t*)0x7)

// Code Segment
#define CS (*(volatile uint16_t*)0x8)

// Data Segment
#define DS (*(volatile uint16_t*)0x9)

// Extra Segment
#define ES (*(volatile uint16_t*)0xA)

// Stack Segment
#define SS (*(volatile uint16_t*)0xB)

// Instruction Pointer
#define IP (*(volatile uint16_t*)0xC)

// Flags Register
#define FLAGS (*(volatile uint16_t*)0xD)
#define FLAGS_CF 0  // Carry Flag
#define FLAGS_PF 2  // Parity Flag
#define FLAGS_AF 4  // Auxiliary Carry Flag
#define FLAGS_ZF 6  // Zero Flag
#define FLAGS_SF 7  // Sign Flag
#define FLAGS_TF 8  // Trap Flag
#define FLAGS_IF 9  // Interrupt Enable Flag
#define FLAGS_DF 10  // Direction Flag
#define FLAGS_OF 11  // Overflow Flag

// 内存段定义
// BIOS ROM
#define BIOS_START 0xF0000
#define BIOS_END 0xFFFFF
#define BIOS_SIZE 65536

// Video Memory
#define VIDEO_START 0xB8000
#define VIDEO_END 0xBFFFF
#define VIDEO_SIZE 32768

// Conventional Memory (640KB)
#define CONVENTIONAL_START 0x00000
#define CONVENTIONAL_END 0x9FFFF
#define CONVENTIONAL_SIZE 640

// Extended Memory (64KB)
#define EXTENDED_START 0x100000
#define EXTENDED_END 0x10FFFF
#define EXTENDED_SIZE 64

// 外设定义
// Programmable Interrupt Controller
#define PIC_BASE 0x20
#define PIC_PIC1_CMD (*(volatile uint8_t*)0x00000040)
#define PIC_PIC1_DATA (*(volatile uint8_t*)0x00000041)
#define PIC_PIC2_CMD (*(volatile uint8_t*)0x000000C0)
#define PIC_PIC2_DATA (*(volatile uint8_t*)0x000000C1)

// Programmable Interval Timer
#define PIT_BASE 0x40
#define PIT_PIT_CH0 (*(volatile uint8_t*)0x00000080)
#define PIT_PIT_CH1 (*(volatile uint8_t*)0x00000081)
#define PIT_PIT_CH2 (*(volatile uint8_t*)0x00000082)
#define PIT_PIT_CTRL (*(volatile uint8_t*)0x00000083)

// Programmable Peripheral Interface
#define PPI_BASE 0x60
#define PPI_PPI_PA (*(volatile uint8_t*)0x000000C0)
#define PPI_PPI_PB (*(volatile uint8_t*)0x000000C1)
#define PPI_PPI_PC (*(volatile uint8_t*)0x000000C2)
#define PPI_PPI_CTRL (*(volatile uint8_t*)0x000000C3)

// Direct Memory Access Controller
#define DMA_BASE 0x00
#define DMA_DMA_CH0_ADDR (*(volatile uint16_t*)0x00000000)
#define DMA_DMA_CH0_COUNT (*(volatile uint16_t*)0x00000001)
#define DMA_DMA_CMD (*(volatile uint8_t*)0x00000008)
#define DMA_DMA_MASK (*(volatile uint8_t*)0x0000000A)
#define DMA_DMA_MODE (*(volatile uint8_t*)0x0000000B)

// Color Graphics Adapter
#define CGA_BASE 0x3D4
#define CGA_CGA_INDEX (*(volatile uint8_t*)0x000007A8)
#define CGA_CGA_DATA (*(volatile uint8_t*)0x000007A9)
#define CGA_CGA_MODE (*(volatile uint8_t*)0x000007AC)
#define CGA_CGA_COLOR (*(volatile uint8_t*)0x000007AD)

// 中断向量定义
#define DIVIDE_ERROR_VECTOR 0  // Divide Error
#define SINGLE_STEP_VECTOR 1  // Single Step
#define NMI_VECTOR 2  // Non-Maskable Interrupt
#define BREAKPOINT_VECTOR 3  // Breakpoint
#define OVERFLOW_VECTOR 4  // Overflow
#define PRINT_SCREEN_VECTOR 5  // Print Screen
#define IRQ0_VECTOR 8  // Timer Interrupt
#define IRQ1_VECTOR 9  // Keyboard Interrupt
#define IRQ2_VECTOR 10  // Cascade (8259A)
#define IRQ3_VECTOR 11  // COM2
#define IRQ4_VECTOR 12  // COM1
#define IRQ5_VECTOR 13  // LPT2
#define IRQ6_VECTOR 14  // Floppy Disk
#define IRQ7_VECTOR 15  // LPT1
#define IRQ8_VECTOR 16  // Real Time Clock
#define IRQ11_VECTOR 19  // Reserved
#define IRQ13_VECTOR 21  // Coprocessor
#define IRQ15_VECTOR 31  // Reserved

// 引脚定义
#define PIN_VCC 1  // +5V Power Supply
#define PIN_GND 2  // Ground
#define PIN_RESET 3  // System Reset
#define PIN_CLK 4  // System Clock (4.77MHz)
#define PIN_READY 5  // CPU Ready Signal
#define PIN_NMI 6  // Non-Maskable Interrupt
#define PIN_INTR 7  // Interrupt Request
#define PIN_HLDA 8  // Hold Acknowledge
#define PIN_HOLD 9  // Hold Request
#define PIN_MEMR 10  // Memory Read
#define PIN_MEMW 11  // Memory Write
#define PIN_IOR 12  // I/O Read
#define PIN_IOW 13  // I/O Write
#define PIN_ALE 14  // Address Latch Enable
#define PIN_DTR 15  // Data Terminal Ready (Serial)
#define PIN_RTS 16  // Request To Send (Serial)
#define PIN_CTS 17  // Clear To Send (Serial)
#define PIN_DSR 18  // Data Set Ready (Serial)
#define PIN_RI 19  // Ring Indicator (Serial)
#define PIN_DCD 20  // Data Carrier Detect (Serial)

void ibm_pc_5150_init(void);

#ifdef __cplusplus
}
#endif

#endif // IBM_PC_5150_HPP
