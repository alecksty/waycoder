// GD32F103 设备定义 - Objective-C 头文件
// 生成自: GigaDevice/GD32/GD32F103
// 版本: 1.0
// 日期: 2026-04-28
// 作者: VML Team
// 描述: 32-bit ARM Cortex-M3 MCU, 108MHz, STM32F103 compatible
// CPU架构: ARM-Cortex-M3
// 位宽: 32位
// 时钟频率: 108000000 Hz

#ifndef GD32F103_DEVICE_H
#define GD32F103_DEVICE_H

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
#define FLASH_START 0x08000000
#define FLASH_END 0x0801FFFF
#define FLASH_SIZE 131072  // 
#define SRAM_START 0x20000000
#define SRAM_END 0x20004FFF
#define SRAM_SIZE 20480  // 
#define PERIPHERAL_START 0x40000000
#define PERIPHERAL_END 0x4003FFFF
#define PERIPHERAL_SIZE 262144  // 

// 外设定义
// Reset and Clock Control
#define RCC_BASE 0x40021000
#define RCC_CTLR_ADDR 0x00
#define RCC_CFGR0_ADDR 0x04
#define RCC_APB2PCENR_ADDR 0x18
#define RCC_APB2PCENR_IOPAEN_BIT 2  // GPIOA clock enable
#define RCC_APB2PCENR_IOPBEN_BIT 3  // GPIOB clock enable
#define RCC_APB2PCENR_IOPCEN_BIT 4  // GPIOC clock enable
#define RCC_APB2PCENR_USART0EN_BIT 14  // USART0 clock enable
#define RCC_APB1PCENR_ADDR 0x1C
#define RCC_APB1PCENR_USART1EN_BIT 17  // USART1 clock enable
// General Purpose I/O Port A
#define GPIOA_BASE 0x40010800
#define GPIOA_CTL0_ADDR 0x00
#define GPIOA_CTL1_ADDR 0x04
#define GPIOA_ISTAT_ADDR 0x08
#define GPIOA_OCTL_ADDR 0x0C
#define GPIOA_BOP_ADDR 0x10
#define GPIOA_BC_ADDR 0x14
// General Purpose I/O Port B
#define GPIOB_BASE 0x40010C00
#define GPIOB_CTL0_ADDR 0x00
#define GPIOB_CTL1_ADDR 0x04
#define GPIOB_ISTAT_ADDR 0x08
#define GPIOB_OCTL_ADDR 0x0C
#define GPIOB_BOP_ADDR 0x10
#define GPIOB_BC_ADDR 0x14
// General Purpose I/O Port C
#define GPIOC_BASE 0x40011000
#define GPIOC_CTL0_ADDR 0x00
#define GPIOC_CTL1_ADDR 0x04
#define GPIOC_ISTAT_ADDR 0x08
#define GPIOC_OCTL_ADDR 0x0C
#define GPIOC_BOP_ADDR 0x10
#define GPIOC_BC_ADDR 0x14
// USART0
#define USART0_BASE 0x40013800
#define USART0_STATR_ADDR 0x00
#define USART0_DATAR_ADDR 0x04
#define USART0_BRR_ADDR 0x08
#define USART0_CTLR1_ADDR 0x0C
// USART1
#define USART1_BASE 0x40004400
#define USART1_STATR_ADDR 0x00
#define USART1_DATAR_ADDR 0x04
#define USART1_BRR_ADDR 0x08
#define USART1_CTLR1_ADDR 0x0C

// 中断向量定义
#define INT_RESET 0  // 
#define INT_SVCALL 11  // 
#define INT_USART0 25  // USART0 Global Interrupt
#define INT_USART1 37  // USART1 Global Interrupt

#endif /* GD32F103_DEVICE_H */
