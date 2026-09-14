// CH32V203 设备定义 - Objective-C 头文件
// 生成自: WCH/CH32V2/CH32V203
// 版本: 1.0
// 日期: 2026-04-28
// 作者: VML Team
// 描述: 32-bit RISC-V MCU with 64KB Flash, 20KB RAM, 144MHz
// CPU架构: RISC-V
// 位宽: 32位
// 时钟频率: 144000000 Hz

#ifndef CH32V203_DEVICE_H
#define CH32V203_DEVICE_H

#import <Foundation/Foundation.h>

// 寄存器地址定义
#define X1_ADDR 0x04  // Return Address
#define X2_ADDR 0x08  // Stack Pointer (SP)
#define X3_ADDR 0x0C  // Global Pointer (GP)
#define X8_ADDR 0x20  // Frame Pointer (FP)
#define X10_ADDR 0x28  // Function Argument (A0)
#define X11_ADDR 0x2C  // Function Argument (A1)
#define PC_ADDR 0x3C  // Program Counter

// 内存段定义
#define FLASH_START 0x08000000
#define FLASH_END 0x0800FFFF
#define FLASH_SIZE 65536  // 
#define SRAM_START 0x20000000
#define SRAM_END 0x20004FFF
#define SRAM_SIZE 20480  // 
#define PERIPHERAL_START 0x40000000
#define PERIPHERAL_END 0x4003FFFF
#define PERIPHERAL_SIZE 262144  // 

// 外设定义
// Reset and Clock Control
#define RCC_BASE 0x40021000
#define RCC_RCC_CTLR_ADDR 0x00
#define RCC_RCC_CFGR0_ADDR 0x04
#define RCC_RCC_APB2PCENR_ADDR 0x18
#define RCC_RCC_APB2PCENR_IOPAEN_BIT 2  // GPIOA clock enable
#define RCC_RCC_APB2PCENR_IOPBEN_BIT 3  // GPIOB clock enable
#define RCC_RCC_APB2PCENR_IOPCEN_BIT 4  // GPIOC clock enable
// General Purpose I/O Port A
#define GPIOA_BASE 0x40010800
#define GPIOA_CFGLR_ADDR 0x00
#define GPIOA_CFGHR_ADDR 0x04
#define GPIOA_INDR_ADDR 0x08
#define GPIOA_OUTDR_ADDR 0x0C
#define GPIOA_BSHR_ADDR 0x10
#define GPIOA_BCR_ADDR 0x14
// General Purpose I/O Port B
#define GPIOB_BASE 0x40010C00
#define GPIOB_CFGLR_ADDR 0x00
#define GPIOB_CFGHR_ADDR 0x04
#define GPIOB_INDR_ADDR 0x08
#define GPIOB_OUTDR_ADDR 0x0C
#define GPIOB_BSHR_ADDR 0x10
#define GPIOB_BCR_ADDR 0x14
// General Purpose I/O Port C
#define GPIOC_BASE 0x40011000
#define GPIOC_CFGLR_ADDR 0x00
#define GPIOC_CFGHR_ADDR 0x04
#define GPIOC_INDR_ADDR 0x08
#define GPIOC_OUTDR_ADDR 0x0C
#define GPIOC_BSHR_ADDR 0x10
#define GPIOC_BCR_ADDR 0x14
// USART1
#define USART1_BASE 0x40013800
#define USART1_USART_STATR_ADDR 0x00
#define USART1_USART_DATAR_ADDR 0x04
#define USART1_USART_BRR_ADDR 0x08
#define USART1_USART_CTLR1_ADDR 0x0C

// 中断向量定义
#define INT_RESET 1  // 
#define INT_MACHINESOFTWARE 3  // 
#define INT_MACHINETIMER 7  // 
#define INT_MACHINEEXTERNAL 11  // 
#define INT_USART1 25  // USART1 Global Interrupt

#endif /* CH32V203_DEVICE_H */
