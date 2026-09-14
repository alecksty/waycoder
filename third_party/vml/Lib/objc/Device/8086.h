// 8086 设备定义 - Objective-C 头文件
// 生成自: Intel/x86/8086
// 版本: 1.0
// 日期: 2026-04-17
// 作者: VML Team
// 描述: 16-bit microprocessor, first x86 processor
// CPU架构: x86
// 位宽: 16位
// 时钟频率: 5000000 Hz

#ifndef 8086_DEVICE_H
#define 8086_DEVICE_H

#import <Foundation/Foundation.h>

// 寄存器地址定义
#define AX_ADDR 0  // Accumulator
#define AX_AH_BIT 8  // High byte of AX
#define AX_AL_BIT 0  // Low byte of AX
#define BX_ADDR 1  // Base
#define BX_BH_BIT 8  // High byte of BX
#define BX_BL_BIT 0  // Low byte of BX
#define CX_ADDR 2  // Counter
#define CX_CH_BIT 8  // High byte of CX
#define CX_CL_BIT 0  // Low byte of CX
#define DX_ADDR 3  // Data
#define DX_DH_BIT 8  // High byte of DX
#define DX_DL_BIT 0  // Low byte of DX
#define SI_ADDR 4  // Source Index
#define DI_ADDR 5  // Destination Index
#define BP_ADDR 6  // Base Pointer
#define SP_ADDR 7  // Stack Pointer
#define IP_ADDR 8  // Instruction Pointer
#define CS_ADDR 9  // Code Segment
#define DS_ADDR 10  // Data Segment
#define ES_ADDR 11  // Extra Segment
#define SS_ADDR 12  // Stack Segment
#define FLAGS_ADDR 13  // Flags Register
#define FLAGS_CF_BIT 0  // Carry Flag
#define FLAGS_PF_BIT 2  // Parity Flag
#define FLAGS_AF_BIT 4  // Auxiliary Flag
#define FLAGS_ZF_BIT 6  // Zero Flag
#define FLAGS_SF_BIT 7  // Sign Flag
#define FLAGS_TF_BIT 8  // Trap Flag
#define FLAGS_IF_BIT 9  // Interrupt Enable Flag
#define FLAGS_DF_BIT 10  // Direction Flag
#define FLAGS_OF_BIT 11  // Overflow Flag

// 内存段定义
#define CODE_START 0x00000
#define CODE_END 0xFFFFF
#define CODE_SIZE 1048576  // 1MB address space
#define DATA_START 0x00000
#define DATA_END 0xFFFFF
#define DATA_SIZE 1048576  // Data memory
#define STACK_START 0xF0000
#define STACK_END 0xFFFFF
#define STACK_SIZE 65536  // Stack memory
#define BIOS_START 0xF0000
#define BIOS_END 0xFFFFF
#define BIOS_SIZE 65536  // BIOS ROM

// 外设定义
// Programmable Interrupt Controller
#define PIC_BASE 0x0020
#define PIC_PIC1_CMD_ADDR 0x0020
#define PIC_PIC1_DATA_ADDR 0x0021
#define PIC_PIC2_CMD_ADDR 0x00A0
#define PIC_PIC2_DATA_ADDR 0x00A1
// Programmable Interval Timer
#define PIT_BASE 0x0040
#define PIT_PIT_CH0_ADDR 0x0040
#define PIT_PIT_CH1_ADDR 0x0041
#define PIT_PIT_CH2_ADDR 0x0042
#define PIT_PIT_CMD_ADDR 0x0043
// Programmable Peripheral Interface
#define PPI_BASE 0x0060
#define PPI_PPI_PA_ADDR 0x0060
#define PPI_PPI_PB_ADDR 0x0061
#define PPI_PPI_PC_ADDR 0x0062
#define PPI_PPI_CMD_ADDR 0x0063

// 中断向量定义
#define INT_DIVIDE_ERROR 0  // Divide by zero
#define INT_DEBUG 1  // Single step
#define INT_NMI 2  // Non-maskable interrupt
#define INT_BREAKPOINT 3  // Breakpoint
#define INT_OVERFLOW 4  // INTO detected overflow
#define INT_IRQ0 8  // Timer interrupt
#define INT_IRQ1 9  // Keyboard interrupt
#define INT_IRQ2 10  // Cascade
#define INT_IRQ3 11  // COM2
#define INT_IRQ4 12  // COM1
#define INT_IRQ5 13  // LPT2
#define INT_IRQ6 14  // Floppy disk
#define INT_IRQ7 15  // LPT1

#endif /* 8086_DEVICE_H */
