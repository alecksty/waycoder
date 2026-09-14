// RA4M2 设备定义 - Objective-C 头文件
// 生成自: Renesas/RA/RA4M2
// 版本: 1.0
// 日期: 2026-04-28
// 作者: VML Team
// 描述: 32-bit ARM Cortex-M4 MCU with 256KB Flash, 128KB RAM, 100MHz
// CPU架构: ARM-Cortex-M4
// 位宽: 32位
// 时钟频率: 100000000 Hz

#ifndef RA4M2_DEVICE_H
#define RA4M2_DEVICE_H

#import <Foundation/Foundation.h>

// 寄存器地址定义
#define R0_ADDR 0x00  // 
#define R1_ADDR 0x04  // 
#define R2_ADDR 0x08  // 
#define R3_ADDR 0x0C  // 
#define R4_ADDR 0x10  // 
#define R5_ADDR 0x14  // 
#define SP_ADDR 0x34  // 
#define LR_ADDR 0x38  // 
#define PC_ADDR 0x3C  // 

// 内存段定义
#define FLASH_START 0x00000000
#define FLASH_END 0x0003FFFF
#define FLASH_SIZE 262144  // 
#define SRAM_START 0x1FFE0000
#define SRAM_END 0x1FFE7FFF
#define SRAM_SIZE 32768  // SRAM0
#define SRAM1_START 0x20000000
#define SRAM1_END 0x20017FFF
#define SRAM1_SIZE 98304  // SRAM1
#define PERIPHERAL_START 0x40000000
#define PERIPHERAL_END 0x400FFFFF
#define PERIPHERAL_SIZE 1048576  // 

// 外设定义
// Module Stop Control
#define MSTP_BASE 0x40020000
#define MSTP_MSTPCR_A_ADDR 0x20
#define MSTP_MSTPCR_A_MSTP41_BIT 9  // GPIO A stop
#define MSTP_MSTPCR_A_MSTP42_BIT 10  // GPIO B stop
#define MSTP_MSTPCR_B_ADDR 0x24
#define MSTP_MSTPCR_C_ADDR 0x28
#define MSTP_MSTPCR_D_ADDR 0x2C
// Interrupt Controller Unit
#define ICU_BASE 0x40030000
#define ICU_IRQCR0_ADDR 0x600
#define ICU_IRQCR1_ADDR 0x602
// General Purpose I/O Port A
#define GPIOA_BASE 0x40040000
#define GPIOA_PDR_ADDR 0x00
#define GPIOA_PODR_ADDR 0x04
#define GPIOA_PIDR_ADDR 0x08
#define GPIOA_PMR_ADDR 0x10
#define GPIOA_PCR_ADDR 0x18
// General Purpose I/O Port B
#define GPIOB_BASE 0x40040020
#define GPIOB_PDR_ADDR 0x00
#define GPIOB_PODR_ADDR 0x04
#define GPIOB_PIDR_ADDR 0x08
#define GPIOB_PMR_ADDR 0x10
// SCI UART 0
#define SCIUART0_BASE 0x40070000
#define SCIUART0_SCR_ADDR 0x00
#define SCIUART0_BRR_ADDR 0x04
#define SCIUART0_TDR_ADDR 0x08
#define SCIUART0_RDR_ADDR 0x0C
#define SCIUART0_SSR_ADDR 0x10

// 中断向量定义
#define INT_RESET 0  // 
#define INT_SVCALL 11  // 
#define INT_SCIUART0_RXI 24  // SCI UART0 Receive Interrupt
#define INT_SCIUART0_TXI 25  // SCI UART0 Transmit Interrupt

#endif /* RA4M2_DEVICE_H */
