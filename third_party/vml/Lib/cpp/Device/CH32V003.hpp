#ifndef CH32V003_HPP
#define CH32V003_HPP

// CH32V003寄存器定义
// 生成自: WCH/CH32V0/CH32V003
// 版本: 1.0
// 日期: 2026-04-28


#ifdef __cplusplus
extern "C" {
#endif

// CPU架构: RISC-V
// 位宽: 32位
// 时钟频率: 48000000 Hz

// 寄存器定义
// Return Address
#define X1 (*(volatile uint32_t*)0x04)

// Stack Pointer (SP)
#define X2 (*(volatile uint32_t*)0x08)

// Global Pointer (GP)
#define X3 (*(volatile uint32_t*)0x0C)

// Program Counter
#define PC (*(volatile uint32_t*)0x3C)

// 内存段定义
#define FLASH_START 0x08000000
#define FLASH_END 0x08003FFF
#define FLASH_SIZE 16384

#define SRAM_START 0x20000000
#define SRAM_END 0x200007FF
#define SRAM_SIZE 2048

#define PERIPHERAL_START 0x40000000
#define PERIPHERAL_END 0x40003FFF
#define PERIPHERAL_SIZE 16384

// 外设定义
// Reset and Clock Control
#define RCC_BASE 0x40021000
#define RCC_CTLR (*(volatile uint32_t*)0x40021000)
#define RCC_CFGR0 (*(volatile uint32_t*)0x40021004)
#define RCC_APB2PCENR (*(volatile uint32_t*)0x40021018)
#define RCC_APB2PCENR_IOPAEN 2  // GPIOA clock enable
#define RCC_APB2PCENR_IOPCEN 4  // GPIOC clock enable
#define RCC_APB2PCENR_IOPDEN 5  // GPIOD clock enable

// General Purpose I/O Port A
#define GPIOA_BASE 0x40010800
#define GPIOA_CFGLR (*(volatile uint32_t*)0x40010800)
#define GPIOA_CFGHR (*(volatile uint32_t*)0x40010804)
#define GPIOA_INDR (*(volatile uint32_t*)0x40010808)
#define GPIOA_OUTDR (*(volatile uint32_t*)0x4001080C)
#define GPIOA_BSHR (*(volatile uint32_t*)0x40010810)
#define GPIOA_BCR (*(volatile uint32_t*)0x40010814)

// General Purpose I/O Port C
#define GPIOC_BASE 0x40011000
#define GPIOC_CFGLR (*(volatile uint32_t*)0x40011000)
#define GPIOC_CFGHR (*(volatile uint32_t*)0x40011004)
#define GPIOC_INDR (*(volatile uint32_t*)0x40011008)
#define GPIOC_OUTDR (*(volatile uint32_t*)0x4001100C)
#define GPIOC_BSHR (*(volatile uint32_t*)0x40011010)
#define GPIOC_BCR (*(volatile uint32_t*)0x40011014)

// General Purpose I/O Port D
#define GPIOD_BASE 0x40011400
#define GPIOD_CFGLR (*(volatile uint32_t*)0x40011400)
#define GPIOD_CFGHR (*(volatile uint32_t*)0x40011404)
#define GPIOD_INDR (*(volatile uint32_t*)0x40011408)
#define GPIOD_OUTDR (*(volatile uint32_t*)0x4001140C)
#define GPIOD_BSHR (*(volatile uint32_t*)0x40011410)
#define GPIOD_BCR (*(volatile uint32_t*)0x40011414)

// USART1
#define USART1_BASE 0x40013800
#define USART1_STATR (*(volatile uint32_t*)0x40013800)
#define USART1_DATAR (*(volatile uint32_t*)0x40013804)
#define USART1_BRR (*(volatile uint32_t*)0x40013808)
#define USART1_CTLR1 (*(volatile uint32_t*)0x4001380C)

// 中断向量定义
#define RESET_VECTOR 1  // 
#define MACHINESOFTWARE_VECTOR 3  // 
#define MACHINETIMER_VECTOR 7  // 
#define MACHINEEXTERNAL_VECTOR 11  // 
#define USART1_VECTOR 25  // USART1 Global Interrupt

void ch32v003_init(void);

#ifdef __cplusplus
}
#endif

#endif // CH32V003_HPP
