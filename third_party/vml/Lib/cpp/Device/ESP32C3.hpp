#ifndef ESP32_C3_HPP
#define ESP32_C3_HPP

// ESP32-C3寄存器定义
// 生成自: Espressif/ESP32-C/ESP32-C3
// 版本: 1.0
// 日期: 2026-04-28


#ifdef __cplusplus
extern "C" {
#endif

// CPU架构: RISC-V
// 位宽: 32位
// 时钟频率: 160000000 Hz

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
// Flash via Cache
#define FLASH_START 0x42000000
#define FLASH_END 0x427FFFFF
#define FLASH_SIZE 8388608

// Internal SRAM
#define SRAM_START 0x3FC80000
#define SRAM_END 0x3FCE3FFF
#define SRAM_SIZE 409600

#define PERIPHERAL_START 0x60000000
#define PERIPHERAL_END 0x600FFFFF
#define PERIPHERAL_SIZE 1048576

// 外设定义
// General Purpose I/O
#define GPIO_BASE 0x60004000
#define GPIO_OUT (*(volatile uint32_t*)0x60004004)
#define GPIO_OUT_W1TS (*(volatile uint32_t*)0x60004008)
#define GPIO_OUT_W1TC (*(volatile uint32_t*)0x6000400C)
#define GPIO_IN (*(volatile uint32_t*)0x60004010)
#define GPIO_ENABLE (*(volatile uint32_t*)0x60004020)
#define GPIO_ENABLE_W1TS (*(volatile uint32_t*)0x60004024)
#define GPIO_ENABLE_W1TC (*(volatile uint32_t*)0x60004028)

// I/O MUX
#define IO_MUX_BASE 0x60009000
#define IO_MUX_GPIO0 (*(volatile uint32_t*)0x60009000)
#define IO_MUX_GPIO1 (*(volatile uint32_t*)0x60009004)
#define IO_MUX_GPIO2 (*(volatile uint32_t*)0x60009008)
#define IO_MUX_GPIO3 (*(volatile uint32_t*)0x6000900C)

// RTC Control
#define RTC_CNTL_BASE 0x60008000
#define RTC_CNTL_OPTIONS0 (*(volatile uint32_t*)0x60008000)
#define RTC_CNTL_CLK_CONF (*(volatile uint32_t*)0x60008030)

// 中断向量定义
#define RESET_VECTOR 1  // 
#define MACHINESOFTWARE_VECTOR 3  // 
#define MACHINETIMER_VECTOR 7  // 
#define MACHINEEXTERNAL_VECTOR 11  // 

void esp32_c3_init(void);

#ifdef __cplusplus
}
#endif

#endif // ESP32_C3_HPP
