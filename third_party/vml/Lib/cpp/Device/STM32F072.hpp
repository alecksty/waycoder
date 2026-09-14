#ifndef STM32F072_HPP
#define STM32F072_HPP

// STM32F072寄存器定义
// 生成自: STMicroelectronics/STM32/STM32F072
// 版本: 1.0
// 日期: 2026-04-28


#ifdef __cplusplus
extern "C" {
#endif

// CPU架构: ARM-Cortex-M0
// 位宽: 32位
// 时钟频率: 48000000 Hz

// 寄存器定义
#define R0 (*(volatile uint32_t*)0x00)

#define R1 (*(volatile uint32_t*)0x04)

#define R2 (*(volatile uint32_t*)0x08)

#define R3 (*(volatile uint32_t*)0x0C)

#define SP (*(volatile uint32_t*)0x34)

#define LR (*(volatile uint32_t*)0x38)

#define PC (*(volatile uint32_t*)0x3C)

// 内存段定义
#define FLASH_START 0x08000000
#define FLASH_END 0x0801FFFF
#define FLASH_SIZE 131072

#define SRAM_START 0x20000000
#define SRAM_END 0x20003FFF
#define SRAM_SIZE 16384

#define PERIPHERAL_START 0x40000000
#define PERIPHERAL_END 0x40027FFF
#define PERIPHERAL_SIZE 163840

// 外设定义
// Reset and Clock Control
#define RCC_BASE 0x40021000
#define RCC_CR (*(volatile uint32_t*)0x40021000)
#define RCC_CFGR (*(volatile uint32_t*)0x40021004)
#define RCC_AHBENR (*(volatile uint32_t*)0x40021014)
#define RCC_AHBENR_GPIOAEN 17  // GPIOA clock enable
#define RCC_AHBENR_GPIOBEN 18  // GPIOB clock enable
#define RCC_AHBENR_GPIOCEN 19  // GPIOC clock enable
#define RCC_APB2ENR (*(volatile uint32_t*)0x40021018)

// General Purpose I/O Port A
#define GPIOA_BASE 0x48000000
#define GPIOA_MODER (*(volatile uint32_t*)0x48000000)
#define GPIOA_OTYPER (*(volatile uint32_t*)0x48000004)
#define GPIOA_OSPEEDR (*(volatile uint32_t*)0x48000008)
#define GPIOA_PUPDR (*(volatile uint32_t*)0x4800000C)
#define GPIOA_IDR (*(volatile uint32_t*)0x48000010)
#define GPIOA_ODR (*(volatile uint32_t*)0x48000014)
#define GPIOA_BSRR (*(volatile uint32_t*)0x48000018)
#define GPIOA_BRR (*(volatile uint32_t*)0x48000028)

// General Purpose I/O Port B
#define GPIOB_BASE 0x48000400
#define GPIOB_MODER (*(volatile uint32_t*)0x48000400)
#define GPIOB_OTYPER (*(volatile uint32_t*)0x48000404)
#define GPIOB_OSPEEDR (*(volatile uint32_t*)0x48000408)
#define GPIOB_PUPDR (*(volatile uint32_t*)0x4800040C)
#define GPIOB_IDR (*(volatile uint32_t*)0x48000410)
#define GPIOB_ODR (*(volatile uint32_t*)0x48000414)
#define GPIOB_BSRR (*(volatile uint32_t*)0x48000418)
#define GPIOB_BRR (*(volatile uint32_t*)0x48000428)

// General Purpose I/O Port C
#define GPIOC_BASE 0x48000800
#define GPIOC_MODER (*(volatile uint32_t*)0x48000800)
#define GPIOC_OTYPER (*(volatile uint32_t*)0x48000804)
#define GPIOC_IDR (*(volatile uint32_t*)0x48000810)
#define GPIOC_ODR (*(volatile uint32_t*)0x48000814)
#define GPIOC_BSRR (*(volatile uint32_t*)0x48000818)

// 中断向量定义
#define RESET_VECTOR 0  // 
#define SVCALL_VECTOR 11  // 

void stm32f072_init(void);

#ifdef __cplusplus
}
#endif

#endif // STM32F072_HPP
