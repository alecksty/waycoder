// STM32H743 设备定义 - Objective-C 头文件
// 生成自: STMicroelectronics/STM32/STM32H743
// 版本: 1.0
// 日期: 2026-04-28
// 作者: VML Team
// 描述: 32-bit ARM Cortex-M7 MCU with 2MB Flash, 1MB RAM, 400MHz
// CPU架构: ARM-Cortex-M7
// 位宽: 32位
// 时钟频率: 400000000 Hz

#ifndef STM32H743_DEVICE_H
#define STM32H743_DEVICE_H

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
#define FLASH_END 0x081FFFFF
#define FLASH_SIZE 2097152  // 
#define DTCM_START 0x20000000
#define DTCM_END 0x2001FFFF
#define DTCM_SIZE 131072  // DTCM RAM
#define ITCM_START 0x00000000
#define ITCM_END 0x0000FFFF
#define ITCM_SIZE 65536  // ITCM RAM
#define SRAM_AXI_START 0x24000000
#define SRAM_AXI_END 0x2407FFFF
#define SRAM_AXI_SIZE 524288  // AXI SRAM
#define SRAM_SRAM_START 0x30000000
#define SRAM_SRAM_END 0x3003FFFF
#define SRAM_SRAM_SIZE 262144  // SRAM1-3
#define PERIPHERAL_START 0x40000000
#define PERIPHERAL_END 0x4FFFFFFF
#define PERIPHERAL_SIZE 268435456  // 

// 外设定义
// Reset and Clock Control
#define RCC_BASE 0x58024400
#define RCC_CR_ADDR 0x00
#define RCC_CFGR_ADDR 0x04
#define RCC_PLL1CFGR_ADDR 0x0C
#define RCC_AHB1ENR_ADDR 0x30
#define RCC_AHB1ENR_GPIOAEN_BIT 0  // GPIOA clock enable
#define RCC_AHB1ENR_GPIOBEN_BIT 1  // GPIOB clock enable
#define RCC_AHB1ENR_GPIOCEN_BIT 2  // GPIOC clock enable
#define RCC_AHB1ENR_GPIODEN_BIT 3  // GPIOD clock enable
#define RCC_AHB1ENR_GPIOEEN_BIT 4  // GPIOE clock enable
#define RCC_AHB1ENR_DMA1EN_BIT 21  // DMA1 clock enable
#define RCC_AHB1ENR_DMA2EN_BIT 22  // DMA2 clock enable
#define RCC_AHB2ENR_ADDR 0x34
#define RCC_AHB4ENR_ADDR 0x3C
#define RCC_APB1LENR_ADDR 0x50
#define RCC_APB2ENR_ADDR 0x58
// General Purpose I/O Port A
#define GPIOA_BASE 0x58020000
#define GPIOA_MODER_ADDR 0x00
#define GPIOA_OTYPER_ADDR 0x04
#define GPIOA_OSPEEDR_ADDR 0x08
#define GPIOA_PUPDR_ADDR 0x0C
#define GPIOA_IDR_ADDR 0x10
#define GPIOA_ODR_ADDR 0x14
#define GPIOA_BSRR_ADDR 0x18
#define GPIOA_BRR_ADDR 0x28
// General Purpose I/O Port B
#define GPIOB_BASE 0x58020400
#define GPIOB_MODER_ADDR 0x00
#define GPIOB_OTYPER_ADDR 0x04
#define GPIOB_OSPEEDR_ADDR 0x08
#define GPIOB_PUPDR_ADDR 0x0C
#define GPIOB_IDR_ADDR 0x10
#define GPIOB_ODR_ADDR 0x14
#define GPIOB_BSRR_ADDR 0x18
#define GPIOB_BRR_ADDR 0x28
// General Purpose I/O Port C
#define GPIOC_BASE 0x58020800
#define GPIOC_MODER_ADDR 0x00
#define GPIOC_OTYPER_ADDR 0x04
#define GPIOC_IDR_ADDR 0x10
#define GPIOC_ODR_ADDR 0x14
#define GPIOC_BSRR_ADDR 0x18
// General Purpose I/O Port D
#define GPIOD_BASE 0x58020C00
#define GPIOD_MODER_ADDR 0x00
#define GPIOD_OTYPER_ADDR 0x04
#define GPIOD_IDR_ADDR 0x10
#define GPIOD_ODR_ADDR 0x14
#define GPIOD_BSRR_ADDR 0x18
// General Purpose I/O Port E
#define GPIOE_BASE 0x58021000
#define GPIOE_MODER_ADDR 0x00
#define GPIOE_OTYPER_ADDR 0x04
#define GPIOE_IDR_ADDR 0x10
#define GPIOE_ODR_ADDR 0x14
#define GPIOE_BSRR_ADDR 0x18
// USART1
#define USART1_BASE 0x40011000
#define USART1_CR1_ADDR 0x00
#define USART1_BRR_ADDR 0x0C
#define USART1_RDR_ADDR 0x24
#define USART1_TDR_ADDR 0x28

// 中断向量定义
#define INT_RESET 0  // 
#define INT_SVCALL 11  // 
#define INT_SYSTICK 15  // 
#define INT_USART1 56  // USART1 Global Interrupt

#endif /* STM32H743_DEVICE_H */
