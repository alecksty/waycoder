#ifndef STM32L073_HPP
#define STM32L073_HPP

// STM32L073寄存器定义
// 生成自: STMicroelectronics/STM32/STM32L073
// 版本: 1.0
// 日期: 2026-04-28


#ifdef __cplusplus
extern "C" {
#endif

// CPU架构: ARM-Cortex-M0+
// 位宽: 32位
// 时钟频率: 32000000 Hz

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
#define FLASH_END 0x0802FFFF
#define FLASH_SIZE 196608

#define SRAM_START 0x20000000
#define SRAM_END 0x20004FFF
#define SRAM_SIZE 20480

#define PERIPHERAL_START 0x40000000
#define PERIPHERAL_END 0x4002FFFF
#define PERIPHERAL_SIZE 196608

// 外设定义
// Reset and Clock Control
#define RCC_BASE 0x40020000
#define RCC_CR (*(volatile uint32_t*)0x40020000)
#define RCC_CFGR (*(volatile uint32_t*)0x40020004)
#define RCC_AHBENR (*(volatile uint32_t*)0x4002001C)
#define RCC_AHBENR_GPIOAEN 17  // GPIOA clock enable
#define RCC_AHBENR_GPIOBEN 18  // GPIOB clock enable
#define RCC_AHBENR_GPIOCEN 19  // GPIOC clock enable
#define RCC_APB1ENR (*(volatile uint32_t*)0x40020020)

// General Purpose I/O Port A
#define GPIOA_BASE 0x50000000
#define GPIOA_MODER (*(volatile uint32_t*)0x50000000)
#define GPIOA_OTYPER (*(volatile uint32_t*)0x50000004)
#define GPIOA_OSPEEDR (*(volatile uint32_t*)0x50000008)
#define GPIOA_PUPDR (*(volatile uint32_t*)0x5000000C)
#define GPIOA_IDR (*(volatile uint32_t*)0x50000010)
#define GPIOA_ODR (*(volatile uint32_t*)0x50000014)
#define GPIOA_BSRR (*(volatile uint32_t*)0x50000018)
#define GPIOA_BRR (*(volatile uint32_t*)0x50000028)

// General Purpose I/O Port B
#define GPIOB_BASE 0x50000400
#define GPIOB_MODER (*(volatile uint32_t*)0x50000400)
#define GPIOB_OTYPER (*(volatile uint32_t*)0x50000404)
#define GPIOB_OSPEEDR (*(volatile uint32_t*)0x50000408)
#define GPIOB_PUPDR (*(volatile uint32_t*)0x5000040C)
#define GPIOB_IDR (*(volatile uint32_t*)0x50000410)
#define GPIOB_ODR (*(volatile uint32_t*)0x50000414)
#define GPIOB_BSRR (*(volatile uint32_t*)0x50000418)
#define GPIOB_BRR (*(volatile uint32_t*)0x50000428)

// General Purpose I/O Port C
#define GPIOC_BASE 0x50000800
#define GPIOC_MODER (*(volatile uint32_t*)0x50000800)
#define GPIOC_OTYPER (*(volatile uint32_t*)0x50000804)
#define GPIOC_IDR (*(volatile uint32_t*)0x50000810)
#define GPIOC_ODR (*(volatile uint32_t*)0x50000814)
#define GPIOC_BSRR (*(volatile uint32_t*)0x50000818)

// General Purpose I/O Port D
#define GPIOD_BASE 0x50000C00
#define GPIOD_MODER (*(volatile uint32_t*)0x50000C00)
#define GPIOD_IDR (*(volatile uint32_t*)0x50000C10)
#define GPIOD_ODR (*(volatile uint32_t*)0x50000C14)
#define GPIOD_BSRR (*(volatile uint32_t*)0x50000C18)

// General Purpose I/O Port E
#define GPIOE_BASE 0x50001000
#define GPIOE_MODER (*(volatile uint32_t*)0x50001000)
#define GPIOE_IDR (*(volatile uint32_t*)0x50001010)
#define GPIOE_ODR (*(volatile uint32_t*)0x50001014)
#define GPIOE_BSRR (*(volatile uint32_t*)0x50001018)

// 中断向量定义
#define RESET_VECTOR 0  // 
#define SVCALL_VECTOR 11  // 

void stm32l073_init(void);

#ifdef __cplusplus
}
#endif

#endif // STM32L073_HPP
