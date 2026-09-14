#ifndef STM32H743_HPP
#define STM32H743_HPP

// STM32H743寄存器定义
// 生成自: STMicroelectronics/STM32/STM32H743
// 版本: 1.0
// 日期: 2026-04-28


#ifdef __cplusplus
extern "C" {
#endif

// CPU架构: ARM-Cortex-M7
// 位宽: 32位
// 时钟频率: 400000000 Hz

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
#define FLASH_END 0x081FFFFF
#define FLASH_SIZE 2097152

// DTCM RAM
#define DTCM_START 0x20000000
#define DTCM_END 0x2001FFFF
#define DTCM_SIZE 131072

// ITCM RAM
#define ITCM_START 0x00000000
#define ITCM_END 0x0000FFFF
#define ITCM_SIZE 65536

// AXI SRAM
#define SRAM_AXI_START 0x24000000
#define SRAM_AXI_END 0x2407FFFF
#define SRAM_AXI_SIZE 524288

// SRAM1-3
#define SRAM_SRAM_START 0x30000000
#define SRAM_SRAM_END 0x3003FFFF
#define SRAM_SRAM_SIZE 262144

#define PERIPHERAL_START 0x40000000
#define PERIPHERAL_END 0x4FFFFFFF
#define PERIPHERAL_SIZE 268435456

// 外设定义
// Reset and Clock Control
#define RCC_BASE 0x58024400
#define RCC_CR (*(volatile uint32_t*)0x58024400)
#define RCC_CFGR (*(volatile uint32_t*)0x58024404)
#define RCC_PLL1CFGR (*(volatile uint32_t*)0x5802440C)
#define RCC_AHB1ENR (*(volatile uint32_t*)0x58024430)
#define RCC_AHB1ENR_GPIOAEN 0  // GPIOA clock enable
#define RCC_AHB1ENR_GPIOBEN 1  // GPIOB clock enable
#define RCC_AHB1ENR_GPIOCEN 2  // GPIOC clock enable
#define RCC_AHB1ENR_GPIODEN 3  // GPIOD clock enable
#define RCC_AHB1ENR_GPIOEEN 4  // GPIOE clock enable
#define RCC_AHB1ENR_DMA1EN 21  // DMA1 clock enable
#define RCC_AHB1ENR_DMA2EN 22  // DMA2 clock enable
#define RCC_AHB2ENR (*(volatile uint32_t*)0x58024434)
#define RCC_AHB4ENR (*(volatile uint32_t*)0x5802443C)
#define RCC_APB1LENR (*(volatile uint32_t*)0x58024450)
#define RCC_APB2ENR (*(volatile uint32_t*)0x58024458)

// General Purpose I/O Port A
#define GPIOA_BASE 0x58020000
#define GPIOA_MODER (*(volatile uint32_t*)0x58020000)
#define GPIOA_OTYPER (*(volatile uint32_t*)0x58020004)
#define GPIOA_OSPEEDR (*(volatile uint32_t*)0x58020008)
#define GPIOA_PUPDR (*(volatile uint32_t*)0x5802000C)
#define GPIOA_IDR (*(volatile uint32_t*)0x58020010)
#define GPIOA_ODR (*(volatile uint32_t*)0x58020014)
#define GPIOA_BSRR (*(volatile uint32_t*)0x58020018)
#define GPIOA_BRR (*(volatile uint32_t*)0x58020028)

// General Purpose I/O Port B
#define GPIOB_BASE 0x58020400
#define GPIOB_MODER (*(volatile uint32_t*)0x58020400)
#define GPIOB_OTYPER (*(volatile uint32_t*)0x58020404)
#define GPIOB_OSPEEDR (*(volatile uint32_t*)0x58020408)
#define GPIOB_PUPDR (*(volatile uint32_t*)0x5802040C)
#define GPIOB_IDR (*(volatile uint32_t*)0x58020410)
#define GPIOB_ODR (*(volatile uint32_t*)0x58020414)
#define GPIOB_BSRR (*(volatile uint32_t*)0x58020418)
#define GPIOB_BRR (*(volatile uint32_t*)0x58020428)

// General Purpose I/O Port C
#define GPIOC_BASE 0x58020800
#define GPIOC_MODER (*(volatile uint32_t*)0x58020800)
#define GPIOC_OTYPER (*(volatile uint32_t*)0x58020804)
#define GPIOC_IDR (*(volatile uint32_t*)0x58020810)
#define GPIOC_ODR (*(volatile uint32_t*)0x58020814)
#define GPIOC_BSRR (*(volatile uint32_t*)0x58020818)

// General Purpose I/O Port D
#define GPIOD_BASE 0x58020C00
#define GPIOD_MODER (*(volatile uint32_t*)0x58020C00)
#define GPIOD_OTYPER (*(volatile uint32_t*)0x58020C04)
#define GPIOD_IDR (*(volatile uint32_t*)0x58020C10)
#define GPIOD_ODR (*(volatile uint32_t*)0x58020C14)
#define GPIOD_BSRR (*(volatile uint32_t*)0x58020C18)

// General Purpose I/O Port E
#define GPIOE_BASE 0x58021000
#define GPIOE_MODER (*(volatile uint32_t*)0x58021000)
#define GPIOE_OTYPER (*(volatile uint32_t*)0x58021004)
#define GPIOE_IDR (*(volatile uint32_t*)0x58021010)
#define GPIOE_ODR (*(volatile uint32_t*)0x58021014)
#define GPIOE_BSRR (*(volatile uint32_t*)0x58021018)

// USART1
#define USART1_BASE 0x40011000
#define USART1_CR1 (*(volatile uint32_t*)0x40011000)
#define USART1_BRR (*(volatile uint32_t*)0x4001100C)
#define USART1_RDR (*(volatile uint32_t*)0x40011024)
#define USART1_TDR (*(volatile uint32_t*)0x40011028)

// 中断向量定义
#define RESET_VECTOR 0  // 
#define SVCALL_VECTOR 11  // 
#define SYSTICK_VECTOR 15  // 
#define USART1_VECTOR 56  // USART1 Global Interrupt

void stm32h743_init(void);

#ifdef __cplusplus
}
#endif

#endif // STM32H743_HPP
