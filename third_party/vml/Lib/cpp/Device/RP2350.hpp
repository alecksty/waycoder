#ifndef RP2350_HPP
#define RP2350_HPP

// RP2350寄存器定义
// 生成自: Raspberry/RP2/RP2350
// 版本: 1.0
// 日期: 2026-04-28


#ifdef __cplusplus
extern "C" {
#endif

// CPU架构: ARM-Cortex-M33
// 位宽: 32位
// 时钟频率: 150000000 Hz

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
// XIP Flash
#define FLASH_START 0x10000000
#define FLASH_END 0x107FFFFF
#define FLASH_SIZE 8388608

// Total SRAM
#define SRAM_START 0x20000000
#define SRAM_END 0x20081FFF
#define SRAM_SIZE 532480

#define PERIPHERAL_START 0x40000000
#define PERIPHERAL_END 0x5000FFFF
#define PERIPHERAL_SIZE 16777216

// 外设定义
// Single-Cycle I/O (GPIO)
#define SIO_BASE 0xD0000000
#define SIO_GPIO_IN (*(volatile uint32_t*)0xD0000004)
#define SIO_GPIO_OUT (*(volatile uint32_t*)0xD0000010)
#define SIO_GPIO_OUT_SET (*(volatile uint32_t*)0xD0000014)
#define SIO_GPIO_OUT_CLR (*(volatile uint32_t*)0xD0000018)
#define SIO_GPIO_OUT_XOR (*(volatile uint32_t*)0xD000001C)
#define SIO_GPIO_OE (*(volatile uint32_t*)0xD0000020)
#define SIO_GPIO_OE_SET (*(volatile uint32_t*)0xD0000024)
#define SIO_GPIO_OE_CLR (*(volatile uint32_t*)0xD0000028)

// IO Bank 0 (GPIO control)
#define IO_BANK0_BASE 0x40028000
#define IO_BANK0_GPIO0_STATUS (*(volatile uint32_t*)0x40028000)
#define IO_BANK0_GPIO0_CTRL (*(volatile uint32_t*)0x40028004)
#define IO_BANK0_GPIO1_STATUS (*(volatile uint32_t*)0x40028008)
#define IO_BANK0_GPIO1_CTRL (*(volatile uint32_t*)0x4002800C)

// Pad controls for GPIO 0-29
#define PADS_BANK0_BASE 0x4002C000
#define PADS_BANK0_GPIO0 (*(volatile uint32_t*)0x4002C000)
#define PADS_BANK0_GPIO1 (*(volatile uint32_t*)0x4002C004)

// Reset Controller
#define RESETS_BASE 0x4000C000
#define RESETS_RESET (*(volatile uint32_t*)0x4000C000)
#define RESETS_RESET_DONE (*(volatile uint32_t*)0x4000C008)

// 中断向量定义
#define RESET_VECTOR 0  // 
#define SVCALL_VECTOR 11  // 

void rp2350_init(void);

#ifdef __cplusplus
}
#endif

#endif // RP2350_HPP
