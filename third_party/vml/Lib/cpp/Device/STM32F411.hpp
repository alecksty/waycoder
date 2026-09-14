#ifndef STM32F411_HPP
#define STM32F411_HPP

// STM32F411寄存器定义
// 生成自: STMicroelectronics/STM32/STM32F411
// 版本: 1.0
// 日期: 2026-04-28


#ifdef __cplusplus
extern "C" {
#endif

// CPU架构: ARM-Cortex-M4
// 位宽: 32位
// 时钟频率: 100000000 Hz

// 寄存器定义
#define R0 (*(volatile uint32_t*)0x00)

#define R1 (*(volatile uint32_t*)0x04)

#define R2 (*(volatile uint32_t*)0x08)

#define R3 (*(volatile uint32_t*)0x0C)

#define R4 (*(volatile uint32_t*)0x10)

#define R5 (*(volatile uint32_t*)0x14)

#define SP (*(volatile uint32_t*)0x34)

#define LR (*(volatile uint32_t*)0x38)

#define PC (*(volatile uint32_t*)0x3C)

// 内存段定义
#define FLASH_START 0x08000000
#define FLASH_END 0x0807FFFF
#define FLASH_SIZE 524288

#define SRAM_START 0x20000000
#define SRAM_END 0x2001FFFF
#define SRAM_SIZE 131072

#define PERIPHERAL_START 0x40000000
#define PERIPHERAL_END 0x400FFFFF
#define PERIPHERAL_SIZE 1048576

// 外设定义
// Reset and Clock Control
#define RCC_BASE 0x40023800
#define RCC_CR (*(volatile uint32_t*)0x40023800)
#define RCC_PLLCFGR (*(volatile uint32_t*)0x40023804)
#define RCC_CFGR (*(volatile uint32_t*)0x40023808)
#define RCC_AHB1ENR (*(volatile uint32_t*)0x40023830)
#define RCC_AHB1ENR_GPIOAEN 0  // GPIOA clock enable
#define RCC_AHB1ENR_GPIOBEN 1  // GPIOB clock enable
#define RCC_AHB1ENR_GPIOCEN 2  // GPIOC clock enable
#define RCC_APB1ENR (*(volatile uint32_t*)0x40023840)
#define RCC_APB2ENR (*(volatile uint32_t*)0x40023844)

// General Purpose I/O Port A
#define GPIOA_BASE 0x40020000
#define GPIOA_MODER (*(volatile uint32_t*)0x40020000)
#define GPIOA_OTYPER (*(volatile uint32_t*)0x40020004)
#define GPIOA_OSPEEDR (*(volatile uint32_t*)0x40020008)
#define GPIOA_PUPDR (*(volatile uint32_t*)0x4002000C)
#define GPIOA_IDR (*(volatile uint32_t*)0x40020010)
#define GPIOA_ODR (*(volatile uint32_t*)0x40020014)
#define GPIOA_BSRR (*(volatile uint32_t*)0x40020018)
#define GPIOA_BRR (*(volatile uint32_t*)0x40020028)

// General Purpose I/O Port B
#define GPIOB_BASE 0x40020400
#define GPIOB_MODER (*(volatile uint32_t*)0x40020400)
#define GPIOB_OTYPER (*(volatile uint32_t*)0x40020404)
#define GPIOB_OSPEEDR (*(volatile uint32_t*)0x40020408)
#define GPIOB_PUPDR (*(volatile uint32_t*)0x4002040C)
#define GPIOB_IDR (*(volatile uint32_t*)0x40020410)
#define GPIOB_ODR (*(volatile uint32_t*)0x40020414)
#define GPIOB_BSRR (*(volatile uint32_t*)0x40020418)
#define GPIOB_BRR (*(volatile uint32_t*)0x40020428)

// General Purpose I/O Port C
#define GPIOC_BASE 0x40020800
#define GPIOC_MODER (*(volatile uint32_t*)0x40020800)
#define GPIOC_OTYPER (*(volatile uint32_t*)0x40020804)
#define GPIOC_IDR (*(volatile uint32_t*)0x40020810)
#define GPIOC_ODR (*(volatile uint32_t*)0x40020814)
#define GPIOC_BSRR (*(volatile uint32_t*)0x40020818)

// Universal Synchronous/Asynchronous Receiver/Transmitter 1
#define USART1_BASE 0x40011000
#define USART1_SR (*(volatile uint32_t*)0x40011000)
#define USART1_DR (*(volatile uint32_t*)0x40011004)
#define USART1_BRR (*(volatile uint32_t*)0x40011008)
#define USART1_CR1 (*(volatile uint32_t*)0x4001100C)

// 中断向量定义
#define RESET_VECTOR 0  // 
#define SVCALL_VECTOR 11  // 
#define USART1_VECTOR 37  // USART1 Global Interrupt

void stm32f411_init(void);

#ifdef __cplusplus
}
#endif

#endif // STM32F411_HPP
