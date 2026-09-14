#ifndef NRF52832_HPP
#define NRF52832_HPP

// nRF52832寄存器定义
// 生成自: Nordic/nRF52/nRF52832
// 版本: 1.0
// 日期: 2026-04-28


#ifdef __cplusplus
extern "C" {
#endif

// CPU架构: ARM-Cortex-M4F
// 位宽: 32位
// 时钟频率: 64000000 Hz

// 寄存器定义
#define R0 (*(volatile uint32_t*)0x00)

#define R1 (*(volatile uint32_t*)0x04)

#define R2 (*(volatile uint32_t*)0x08)

#define R3 (*(volatile uint32_t*)0x0C)

#define SP (*(volatile uint32_t*)0x34)

#define LR (*(volatile uint32_t*)0x38)

#define PC (*(volatile uint32_t*)0x3C)

// 内存段定义
#define FLASH_START 0x00000000
#define FLASH_END 0x0007FFFF
#define FLASH_SIZE 524288

#define SRAM_START 0x20000000
#define SRAM_END 0x2000FFFF
#define SRAM_SIZE 65536

#define PERIPHERAL_START 0x40000000
#define PERIPHERAL_END 0x400FFFFF
#define PERIPHERAL_SIZE 1048576

// Factory Information Configuration Registers
#define FICR_START 0x10000000
#define FICR_END 0x10000FFF
#define FICR_SIZE 4096

// 外设定义
// General Purpose I/O Port 0
#define GPIO_P0_BASE 0x50000000
#define GPIO_P0_OUT (*(volatile uint32_t*)0x50000504)
#define GPIO_P0_OUTSET (*(volatile uint32_t*)0x50000508)
#define GPIO_P0_OUTCLR (*(volatile uint32_t*)0x5000050C)
#define GPIO_P0_IN (*(volatile uint32_t*)0x50000510)
#define GPIO_P0_DIR (*(volatile uint32_t*)0x50000514)
#define GPIO_P0_DIRSET (*(volatile uint32_t*)0x50000518)
#define GPIO_P0_DIRCLR (*(volatile uint32_t*)0x5000051C)

// Power Control
#define POWER_BASE 0x40000000
#define POWER_DCDCEN (*(volatile uint32_t*)0x400001C4)
#define POWER_RAMSTATUS (*(volatile uint32_t*)0x40000268)

// Clock Control
#define CLOCK_BASE 0x40000000
#define CLOCK_HFCLKSTART (*(volatile uint32_t*)0x40000108)
#define CLOCK_HFCLKSTARTED (*(volatile uint32_t*)0x40000208)

// 中断向量定义
#define RESET_VECTOR 0  // 
#define SVCALL_VECTOR 11  // 

void nrf52832_init(void);

#ifdef __cplusplus
}
#endif

#endif // NRF52832_HPP
