#ifndef CH32V203_HPP
#define CH32V203_HPP

// CH32V203寄存器定义
// 生成自: WCH/CH32V2/CH32V203
// 版本: 1.0
// 日期: 2026-04-28


#ifdef __cplusplus
extern "C" {
#endif

// CPU架构: RISC-V
// 位宽: 32位
// 时钟频率: 144000000 Hz

// 寄存器定义
// Return Address
#define X1 (*(volatile uint32_t*)0x04)

// Stack Pointer (SP)
#define X2 (*(volatile uint32_t*)0x08)

// Global Pointer (GP)
#define X3 (*(volatile uint32_t*)0x0C)

// Frame Pointer (FP)
#define X8 (*(volatile uint32_t*)0x20)

// Function Argument (A0)
#define X10 (*(volatile uint32_t*)0x28)

// Function Argument (A1)
#define X11 (*(volatile uint32_t*)0x2C)

// Program Counter
#define PC (*(volatile uint32_t*)0x3C)

// 内存段定义
#define FLASH_START 0x08000000
#define FLASH_END 0x0800FFFF
#define FLASH_SIZE 65536

#define SRAM_START 0x20000000
#define SRAM_END 0x20004FFF
#define SRAM_SIZE 20480

#define PERIPHERAL_START 0x40000000
#define PERIPHERAL_END 0x4003FFFF
#define PERIPHERAL_SIZE 262144

// 外设定义
// Reset and Clock Control
#define RCC_BASE 0x40021000
#define RCC_RCC_CTLR (*(volatile uint32_t*)0x40021000)
#define RCC_RCC_CFGR0 (*(volatile uint32_t*)0x40021004)
#define RCC_RCC_APB2PCENR (*(volatile uint32_t*)0x40021018)
#define RCC_RCC_APB2PCENR_IOPAEN 2  // GPIOA clock enable
#define RCC_RCC_APB2PCENR_IOPBEN 3  // GPIOB clock enable
#define RCC_RCC_APB2PCENR_IOPCEN 4  // GPIOC clock enable

// General Purpose I/O Port A
#define GPIOA_BASE 0x40010800
#define GPIOA_CFGLR (*(volatile uint32_t*)0x40010800)
#define GPIOA_CFGHR (*(volatile uint32_t*)0x40010804)
#define GPIOA_INDR (*(volatile uint32_t*)0x40010808)
#define GPIOA_OUTDR (*(volatile uint32_t*)0x4001080C)
#define GPIOA_BSHR (*(volatile uint32_t*)0x40010810)
#define GPIOA_BCR (*(volatile uint32_t*)0x40010814)

// General Purpose I/O Port B
#define GPIOB_BASE 0x40010C00
#define GPIOB_CFGLR (*(volatile uint32_t*)0x40010C00)
#define GPIOB_CFGHR (*(volatile uint32_t*)0x40010C04)
#define GPIOB_INDR (*(volatile uint32_t*)0x40010C08)
#define GPIOB_OUTDR (*(volatile uint32_t*)0x40010C0C)
#define GPIOB_BSHR (*(volatile uint32_t*)0x40010C10)
#define GPIOB_BCR (*(volatile uint32_t*)0x40010C14)

// General Purpose I/O Port C
#define GPIOC_BASE 0x40011000
#define GPIOC_CFGLR (*(volatile uint32_t*)0x40011000)
#define GPIOC_CFGHR (*(volatile uint32_t*)0x40011004)
#define GPIOC_INDR (*(volatile uint32_t*)0x40011008)
#define GPIOC_OUTDR (*(volatile uint32_t*)0x4001100C)
#define GPIOC_BSHR (*(volatile uint32_t*)0x40011010)
#define GPIOC_BCR (*(volatile uint32_t*)0x40011014)

// USART1
#define USART1_BASE 0x40013800
#define USART1_USART_STATR (*(volatile uint32_t*)0x40013800)
#define USART1_USART_DATAR (*(volatile uint32_t*)0x40013804)
#define USART1_USART_BRR (*(volatile uint32_t*)0x40013808)
#define USART1_USART_CTLR1 (*(volatile uint32_t*)0x4001380C)

// 中断向量定义
#define RESET_VECTOR 1  // 
#define MACHINESOFTWARE_VECTOR 3  // 
#define MACHINETIMER_VECTOR 7  // 
#define MACHINEEXTERNAL_VECTOR 11  // 
#define USART1_VECTOR 25  // USART1 Global Interrupt

void ch32v203_init(void);

#ifdef __cplusplus
}
#endif

#endif // CH32V203_HPP
