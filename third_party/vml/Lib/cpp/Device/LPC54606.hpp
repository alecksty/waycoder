#ifndef LPC54606_HPP
#define LPC54606_HPP

// LPC54606寄存器定义
// 生成自: NXP/LPC/LPC54606
// 版本: 1.0
// 日期: 2026-04-28


#ifdef __cplusplus
extern "C" {
#endif

// CPU架构: ARM-Cortex-M4
// 位宽: 32位
// 时钟频率: 180000000 Hz

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
#define FLASH_START 0x00000000
#define FLASH_END 0x0003FFFF
#define FLASH_SIZE 262144

#define SRAM_START 0x20000000
#define SRAM_END 0x20021FFF
#define SRAM_SIZE 139264

#define PERIPHERAL_START 0x40000000
#define PERIPHERAL_END 0x401FFFFF
#define PERIPHERAL_SIZE 2097152

// 外设定义
// System Control
#define SYSCON_BASE 0x40000000
#define SYSCON_SYSAHBCLKCTRL (*(volatile uint32_t*)0x40000080)
#define SYSCON_MAINCLKSEL (*(volatile uint32_t*)0x40000004)
#define SYSCON_MAINCLKUEN (*(volatile uint32_t*)0x40000008)
#define SYSCON_SYSPLLCTRL (*(volatile uint32_t*)0x4000000C)

// General Purpose I/O
#define GPIO_BASE 0x400F4000
#define GPIO_DIR0 (*(volatile uint32_t*)0x400F4000)
#define GPIO_PIN0 (*(volatile uint32_t*)0x400F5000)
#define GPIO_SET0 (*(volatile uint32_t*)0x400F6000)
#define GPIO_CLR0 (*(volatile uint32_t*)0x400F7000)
#define GPIO_NOT0 (*(volatile uint32_t*)0x400F8000)
#define GPIO_DIR1 (*(volatile uint32_t*)0x400F4004)
#define GPIO_PIN1 (*(volatile uint32_t*)0x400F5004)
#define GPIO_SET1 (*(volatile uint32_t*)0x400F6004)
#define GPIO_CLR1 (*(volatile uint32_t*)0x400F7004)
#define GPIO_NOT1 (*(volatile uint32_t*)0x400F8004)

// USART0
#define USART0_BASE 0x40086000
#define USART0_CFG (*(volatile uint32_t*)0x40086000)
#define USART0_CTRL (*(volatile uint32_t*)0x40086004)
#define USART0_STAT (*(volatile uint32_t*)0x40086008)
#define USART0_TXDAT (*(volatile uint32_t*)0x40086010)
#define USART0_RXDAT (*(volatile uint32_t*)0x40086014)
#define USART0_BRG (*(volatile uint32_t*)0x40086020)

// 中断向量定义
#define RESET_VECTOR 0  // 
#define SVCALL_VECTOR 11  // 
#define USART0_VECTOR 24  // USART0 Interrupt

void lpc54606_init(void);

#ifdef __cplusplus
}
#endif

#endif // LPC54606_HPP
