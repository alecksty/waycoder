// IBM-PC-5150 设备定义 - Objective-C 头文件
// 生成自: IBM/Personal Computer/IBM-PC-5150
// 版本: 1.0
// 日期: 2026-04-16
// 作者: VML Team
// 描述: Original IBM Personal Computer Model 5150
// CPU架构: x86
// 位宽: 16位
// 时钟频率: 4772727 Hz

#ifndef IBM-PC-5150_DEVICE_H
#define IBM-PC-5150_DEVICE_H

#import <Foundation/Foundation.h>

// 寄存器地址定义
#define AX_ADDR 0x0  // Accumulator Register
#define BX_ADDR 0x1  // Base Register
#define CX_ADDR 0x2  // Count Register
#define DX_ADDR 0x3  // Data Register
#define SI_ADDR 0x4  // Source Index
#define DI_ADDR 0x5  // Destination Index
#define BP_ADDR 0x6  // Base Pointer
#define SP_ADDR 0x7  // Stack Pointer
#define CS_ADDR 0x8  // Code Segment
#define DS_ADDR 0x9  // Data Segment
#define ES_ADDR 0xA  // Extra Segment
#define SS_ADDR 0xB  // Stack Segment
#define IP_ADDR 0xC  // Instruction Pointer
#define FLAGS_ADDR 0xD  // Flags Register
#define FLAGS_CF_BIT 0  // Carry Flag
#define FLAGS_PF_BIT 2  // Parity Flag
#define FLAGS_AF_BIT 4  // Auxiliary Carry Flag
#define FLAGS_ZF_BIT 6  // Zero Flag
#define FLAGS_SF_BIT 7  // Sign Flag
#define FLAGS_TF_BIT 8  // Trap Flag
#define FLAGS_IF_BIT 9  // Interrupt Enable Flag
#define FLAGS_DF_BIT 10  // Direction Flag
#define FLAGS_OF_BIT 11  // Overflow Flag

// 内存段定义
#define BIOS_START 0xF0000
#define BIOS_END 0xFFFFF
#define BIOS_SIZE 65536  // BIOS ROM
#define VIDEO_START 0xB8000
#define VIDEO_END 0xBFFFF
#define VIDEO_SIZE 32768  // Video Memory
#define CONVENTIONAL_START 0x00000
#define CONVENTIONAL_END 0x9FFFF
#define CONVENTIONAL_SIZE 640  // Conventional Memory (640KB)
#define EXTENDED_START 0x100000
#define EXTENDED_END 0x10FFFF
#define EXTENDED_SIZE 64  // Extended Memory (64KB)

// 外设定义
// Programmable Interrupt Controller
#define PIC_BASE 0x20
#define PIC_PIC1_CMD_ADDR 0x20
#define PIC_PIC1_DATA_ADDR 0x21
#define PIC_PIC2_CMD_ADDR 0xA0
#define PIC_PIC2_DATA_ADDR 0xA1
// Programmable Interval Timer
#define PIT_BASE 0x40
#define PIT_PIT_CH0_ADDR 0x40
#define PIT_PIT_CH1_ADDR 0x41
#define PIT_PIT_CH2_ADDR 0x42
#define PIT_PIT_CTRL_ADDR 0x43
// Programmable Peripheral Interface
#define PPI_BASE 0x60
#define PPI_PPI_PA_ADDR 0x60
#define PPI_PPI_PB_ADDR 0x61
#define PPI_PPI_PC_ADDR 0x62
#define PPI_PPI_CTRL_ADDR 0x63
// Direct Memory Access Controller
#define DMA_BASE 0x00
#define DMA_DMA_CH0_ADDR_ADDR 0x00
#define DMA_DMA_CH0_COUNT_ADDR 0x01
#define DMA_DMA_CMD_ADDR 0x08
#define DMA_DMA_MASK_ADDR 0x0A
#define DMA_DMA_MODE_ADDR 0x0B
// Color Graphics Adapter
#define CGA_BASE 0x3D4
#define CGA_CGA_INDEX_ADDR 0x3D4
#define CGA_CGA_DATA_ADDR 0x3D5
#define CGA_CGA_MODE_ADDR 0x3D8
#define CGA_CGA_COLOR_ADDR 0x3D9

// 中断向量定义
#define INT_DIVIDE_ERROR 0  // Divide Error
#define INT_SINGLE_STEP 1  // Single Step
#define INT_NMI 2  // Non-Maskable Interrupt
#define INT_BREAKPOINT 3  // Breakpoint
#define INT_OVERFLOW 4  // Overflow
#define INT_PRINT_SCREEN 5  // Print Screen
#define INT_IRQ0 8  // Timer Interrupt
#define INT_IRQ1 9  // Keyboard Interrupt
#define INT_IRQ2 10  // Cascade (8259A)
#define INT_IRQ3 11  // COM2
#define INT_IRQ4 12  // COM1
#define INT_IRQ5 13  // LPT2
#define INT_IRQ6 14  // Floppy Disk
#define INT_IRQ7 15  // LPT1
#define INT_IRQ8 16  // Real Time Clock
#define INT_IRQ11 19  // Reserved
#define INT_IRQ13 21  // Coprocessor
#define INT_IRQ15 31  // Reserved

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

#endif /* IBM-PC-5150_DEVICE_H */
