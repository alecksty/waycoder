// STM32L432 设备定义 - Objective-C 头文件
// 生成自: STMicroelectronics/STM32/STM32L432
// 版本: 1.0
// 日期: 2026-04-28
// 作者: VML Team
// 描述: 32-bit ARM Cortex-M4 MCU ultra-low-power with 256KB Flash, 64KB RAM, 80MHz
// CPU架构: ARM-Cortex-M4
// 位宽: 32位
// 时钟频率: 80000000 Hz

#ifndef STM32L432_DEVICE_H
#define STM32L432_DEVICE_H

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
#define FLASH_END 0x0803FFFF
#define FLASH_SIZE 262144  // 
#define SRAM_START 0x20000000
#define SRAM_END 0x2000FFFF
#define SRAM_SIZE 65536  // 
#define PERIPHERAL_START 0x40000000
#define PERIPHERAL_END 0x4007FFFF
#define PERIPHERAL_SIZE 524288  // 

// 外设定义
// Reset and Clock Control
#define RCC_BASE 0x40021000
#define RCC_CR_ADDR 0x00
#define RCC_CFGR_ADDR 0x08
#define RCC_PLLCFGR_ADDR 0x0C
#define RCC_AHB1ENR_ADDR 0x38
#define RCC_AHB1ENR_GPIOAEN_BIT 0  // GPIOA clock enable
#define RCC_AHB1ENR_GPIOBEN_BIT 1  // GPIOB clock enable
#define RCC_APB1ENR1_ADDR 0x58
#define RCC_APB2ENR_ADDR 0x60
// General Purpose I/O Port A
#define GPIOA_BASE 0x48000000
#define GPIOA_MODER_ADDR 0x00
#define GPIOA_OTYPER_ADDR 0x04
#define GPIOA_OSPEEDR_ADDR 0x08
#define GPIOA_PUPDR_ADDR 0x0C
#define GPIOA_IDR_ADDR 0x10
#define GPIOA_ODR_ADDR 0x14
#define GPIOA_BSRR_ADDR 0x18
#define GPIOA_BRR_ADDR 0x28
// General Purpose I/O Port B
#define GPIOB_BASE 0x48000400
#define GPIOB_MODER_ADDR 0x00
#define GPIOB_OTYPER_ADDR 0x04
#define GPIOB_OSPEEDR_ADDR 0x08
#define GPIOB_PUPDR_ADDR 0x0C
#define GPIOB_IDR_ADDR 0x10
#define GPIOB_ODR_ADDR 0x14
#define GPIOB_BSRR_ADDR 0x18
#define GPIOB_BRR_ADDR 0x28
// Low-power UART 1
#define LPUART1_BASE 0x40008000
#define LPUART1_CR1_ADDR 0x00
#define LPUART1_BRR_ADDR 0x0C
#define LPUART1_RDR_ADDR 0x24
#define LPUART1_TDR_ADDR 0x28

// 中断向量定义
#define INT_RESET 0  // 
#define INT_SVCALL 11  // 
#define INT_LPUART1 53  // LPUART1 Global Interrupt

#endif /* STM32L432_DEVICE_H */
