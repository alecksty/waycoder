// STM32G070 设备定义 - Objective-C 头文件
// 生成自: STMicroelectronics/STM32/STM32G070
// 版本: 1.0
// 日期: 2026-04-28
// 作者: VML Team
// 描述: 32-bit ARM Cortex-M0+ MCU with 128KB Flash, 36KB RAM, 64MHz
// CPU架构: ARM-Cortex-M0+
// 位宽: 32位
// 时钟频率: 64000000 Hz

#ifndef STM32G070_DEVICE_H
#define STM32G070_DEVICE_H

#import <Foundation/Foundation.h>

// 寄存器地址定义
#define R0_ADDR 0x00  // 
#define R1_ADDR 0x04  // 
#define R2_ADDR 0x08  // 
#define R3_ADDR 0x0C  // 
#define SP_ADDR 0x34  // 
#define LR_ADDR 0x38  // 
#define PC_ADDR 0x3C  // 

// 内存段定义
#define FLASH_START 0x08000000
#define FLASH_END 0x0801FFFF
#define FLASH_SIZE 131072  // 
#define SRAM_START 0x20000000
#define SRAM_END 0x20008FFF
#define SRAM_SIZE 36864  // 
#define PERIPHERAL_START 0x40000000
#define PERIPHERAL_END 0x4002FFFF
#define PERIPHERAL_SIZE 196608  // 

// 外设定义
// Reset and Clock Control
#define RCC_BASE 0x40021000
#define RCC_CR_ADDR 0x00
#define RCC_CFGR_ADDR 0x04
#define RCC_AHBRSTR_ADDR 0x18
#define RCC_APBRSTR_ADDR 0x1C
// General Purpose I/O Port A
#define GPIOA_BASE 0x50000000
#define GPIOA_MODER_ADDR 0x00
#define GPIOA_OTYPER_ADDR 0x04
#define GPIOA_OSPEEDR_ADDR 0x08
#define GPIOA_PUPDR_ADDR 0x0C
#define GPIOA_IDR_ADDR 0x10
#define GPIOA_ODR_ADDR 0x14
#define GPIOA_BSRR_ADDR 0x18
#define GPIOA_BRR_ADDR 0x28
// General Purpose I/O Port B
#define GPIOB_BASE 0x50000400
#define GPIOB_MODER_ADDR 0x00
#define GPIOB_OTYPER_ADDR 0x04
#define GPIOB_OSPEEDR_ADDR 0x08
#define GPIOB_PUPDR_ADDR 0x0C
#define GPIOB_IDR_ADDR 0x10
#define GPIOB_ODR_ADDR 0x14
#define GPIOB_BSRR_ADDR 0x18
#define GPIOB_BRR_ADDR 0x28
// General Purpose I/O Port C
#define GPIOC_BASE 0x50000800
#define GPIOC_MODER_ADDR 0x00
#define GPIOC_OTYPER_ADDR 0x04
#define GPIOC_IDR_ADDR 0x10
#define GPIOC_ODR_ADDR 0x14
#define GPIOC_BSRR_ADDR 0x18

// 中断向量定义
#define INT_RESET 0  // 
#define INT_SVCALL 11  // 

#endif /* STM32G070_DEVICE_H */
