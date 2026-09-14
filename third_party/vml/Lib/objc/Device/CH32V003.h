// CH32V003 设备定义 - Objective-C 头文件
// 生成自: WCH/CH32V0/CH32V003
// 版本: 1.0
// 日期: 2026-04-28
// 作者: VML Team
// 描述: 32-bit RISC-V RV32EC MCU with 16KB Flash, 2KB RAM, 48MHz, ultra-low-cost
// CPU架构: RISC-V
// 位宽: 32位
// 时钟频率: 48000000 Hz

#ifndef CH32V003_DEVICE_H
#define CH32V003_DEVICE_H

#import <Foundation/Foundation.h>

// 寄存器地址定义
#define X1_ADDR 0x04  // Return Address
#define X2_ADDR 0x08  // Stack Pointer (SP)
#define X3_ADDR 0x0C  // Global Pointer (GP)
#define PC_ADDR 0x3C  // Program Counter

// 内存段定义
#define FLASH_START 0x08000000
#define FLASH_END 0x08003FFF
#define FLASH_SIZE 16384  // 
#define SRAM_START 0x20000000
#define SRAM_END 0x200007FF
#define SRAM_SIZE 2048  // 
#define PERIPHERAL_START 0x40000000
#define PERIPHERAL_END 0x40003FFF
#define PERIPHERAL_SIZE 16384  // 

// 外设定义
// Reset and Clock Control
#define RCC_BASE 0x40021000
#define RCC_CTLR_ADDR 0x00
#define RCC_CFGR0_ADDR 0x04
#define RCC_APB2PCENR_ADDR 0x18
#define RCC_APB2PCENR_IOPAEN_BIT 2  // GPIOA clock enable
#define RCC_APB2PCENR_IOPCEN_BIT 4  // GPIOC clock enable
#define RCC_APB2PCENR_IOPDEN_BIT 5  // GPIOD clock enable
// General Purpose I/O Port A
#define GPIOA_BASE 0x40010800
#define GPIOA_CFGLR_ADDR 0x00
#define GPIOA_CFGHR_ADDR 0x04
#define GPIOA_INDR_ADDR 0x08
#define GPIOA_OUTDR_ADDR 0x0C
#define GPIOA_BSHR_ADDR 0x10
#define GPIOA_BCR_ADDR 0x14
// General Purpose I/O Port C
#define GPIOC_BASE 0x40011000
#define GPIOC_CFGLR_ADDR 0x00
#define GPIOC_CFGHR_ADDR 0x04
#define GPIOC_INDR_ADDR 0x08
#define GPIOC_OUTDR_ADDR 0x0C
#define GPIOC_BSHR_ADDR 0x10
#define GPIOC_BCR_ADDR 0x14
// General Purpose I/O Port D
#define GPIOD_BASE 0x40011400
#define GPIOD_CFGLR_ADDR 0x00
#define GPIOD_CFGHR_ADDR 0x04
#define GPIOD_INDR_ADDR 0x08
#define GPIOD_OUTDR_ADDR 0x0C
#define GPIOD_BSHR_ADDR 0x10
#define GPIOD_BCR_ADDR 0x14
// USART1
#define USART1_BASE 0x40013800
#define USART1_STATR_ADDR 0x00
#define USART1_DATAR_ADDR 0x04
#define USART1_BRR_ADDR 0x08
#define USART1_CTLR1_ADDR 0x0C

// 中断向量定义
#define INT_RESET 1  // 
#define INT_MACHINESOFTWARE 3  // 
#define INT_MACHINETIMER 7  // 
#define INT_MACHINEEXTERNAL 11  // 
#define INT_USART1 25  // USART1 Global Interrupt

#endif /* CH32V003_DEVICE_H */
