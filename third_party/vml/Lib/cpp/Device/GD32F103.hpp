#ifndef GD32F103_HPP
#define GD32F103_HPP

// GD32F103寄存器定义
// 生成自: GigaDevice/GD32/GD32F103
// 版本: 1.0
// 日期: 2026-04-28


#ifdef __cplusplus
extern "C" {
#endif

// CPU架构: ARM-Cortex-M3
// 位宽: 32位
// 时钟频率: 108000000 Hz

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
#define FLASH_END 0x0801FFFF
#define FLASH_SIZE 131072

#define SRAM_START 0x20000000
#define SRAM_END 0x20004FFF
#define SRAM_SIZE 20480

#define PERIPHERAL_START 0x40000000
#define PERIPHERAL_END 0x4003FFFF
#define PERIPHERAL_SIZE 262144

// 外设定义
// Reset and Clock Control
#define RCC_BASE 0x40021000
#define RCC_CTLR (*(volatile uint32_t*)0x40021000)
#define RCC_CFGR0 (*(volatile uint32_t*)0x40021004)
#define RCC_APB2PCENR (*(volatile uint32_t*)0x40021018)
#define RCC_APB2PCENR_IOPAEN 2  // GPIOA clock enable
#define RCC_APB2PCENR_IOPBEN 3  // GPIOB clock enable
#define RCC_APB2PCENR_IOPCEN 4  // GPIOC clock enable
#define RCC_APB2PCENR_USART0EN 14  // USART0 clock enable
#define RCC_APB1PCENR (*(volatile uint32_t*)0x4002101C)
#define RCC_APB1PCENR_USART1EN 17  // USART1 clock enable

// General Purpose I/O Port A
#define GPIOA_BASE 0x40010800
#define GPIOA_CTL0 (*(volatile uint32_t*)0x40010800)
#define GPIOA_CTL1 (*(volatile uint32_t*)0x40010804)
#define GPIOA_ISTAT (*(volatile uint32_t*)0x40010808)
#define GPIOA_OCTL (*(volatile uint32_t*)0x4001080C)
#define GPIOA_BOP (*(volatile uint32_t*)0x40010810)
#define GPIOA_BC (*(volatile uint32_t*)0x40010814)

// General Purpose I/O Port B
#define GPIOB_BASE 0x40010C00
#define GPIOB_CTL0 (*(volatile uint32_t*)0x40010C00)
#define GPIOB_CTL1 (*(volatile uint32_t*)0x40010C04)
#define GPIOB_ISTAT (*(volatile uint32_t*)0x40010C08)
#define GPIOB_OCTL (*(volatile uint32_t*)0x40010C0C)
#define GPIOB_BOP (*(volatile uint32_t*)0x40010C10)
#define GPIOB_BC (*(volatile uint32_t*)0x40010C14)

// General Purpose I/O Port C
#define GPIOC_BASE 0x40011000
#define GPIOC_CTL0 (*(volatile uint32_t*)0x40011000)
#define GPIOC_CTL1 (*(volatile uint32_t*)0x40011004)
#define GPIOC_ISTAT (*(volatile uint32_t*)0x40011008)
#define GPIOC_OCTL (*(volatile uint32_t*)0x4001100C)
#define GPIOC_BOP (*(volatile uint32_t*)0x40011010)
#define GPIOC_BC (*(volatile uint32_t*)0x40011014)

// USART0
#define USART0_BASE 0x40013800
#define USART0_STATR (*(volatile uint32_t*)0x40013800)
#define USART0_DATAR (*(volatile uint32_t*)0x40013804)
#define USART0_BRR (*(volatile uint32_t*)0x40013808)
#define USART0_CTLR1 (*(volatile uint32_t*)0x4001380C)

// USART1
#define USART1_BASE 0x40004400
#define USART1_STATR (*(volatile uint32_t*)0x40004400)
#define USART1_DATAR (*(volatile uint32_t*)0x40004404)
#define USART1_BRR (*(volatile uint32_t*)0x40004408)
#define USART1_CTLR1 (*(volatile uint32_t*)0x4000440C)

// 中断向量定义
#define RESET_VECTOR 0  // 
#define SVCALL_VECTOR 11  // 
#define USART0_VECTOR 25  // USART0 Global Interrupt
#define USART1_VECTOR 37  // USART1 Global Interrupt

void gd32f103_init(void);

#ifdef __cplusplus
}
#endif

#endif // GD32F103_HPP
