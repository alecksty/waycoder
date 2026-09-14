#ifndef BL618_HPP
#define BL618_HPP

// BL618寄存器定义
// 生成自: Bouffalo Lab/BL6/BL618
// 版本: 1.0
// 日期: 2026-04-28


#ifdef __cplusplus
extern "C" {
#endif

// CPU架构: RISC-V
// 位宽: 32位
// 时钟频率: 320000000 Hz

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
#define FLASH_START 0x20000000
#define FLASH_END 0x203FFFFF
#define FLASH_SIZE 4194304

#define SRAM_HPSYS_START 0x22000000
#define SRAM_HPSYS_END 0x22003FFF
#define SRAM_HPSYS_SIZE 16384

// DTCM
#define SRAM_DTCM_START 0x22010000
#define SRAM_DTCM_END 0x22017FFF
#define SRAM_DTCM_SIZE 32768

#define SRAM_SYS_START 0x22020000
#define SRAM_SYS_END 0x2208FFFF
#define SRAM_SYS_SIZE 458752

#define PERIPHERAL_START 0x30000000
#define PERIPHERAL_END 0x300FFFFF
#define PERIPHERAL_SIZE 1048576

// 外设定义
// Global Control (Clock and Reset)
#define GLB_BASE 0x30000000
#define GLB_GLB_CLK_EN (*(volatile uint32_t*)0x30000010)
#define GLB_GLB_CLK_EN_GPIO_CLK_EN 6  // GPIO clock enable
#define GLB_GLB_CLK_EN_UART0_CLK_EN 12  // UART0 clock enable
#define GLB_GLB_SYS_CLK_CTRL (*(volatile uint32_t*)0x30000014)
#define GLB_GLB_PLL_CTRL (*(volatile uint32_t*)0x3000001C)

// GPIO Port A
#define GPIO_P0_BASE 0x30007000
#define GPIO_P0_GPIO_CFG0 (*(volatile uint32_t*)0x30007000)
#define GPIO_P0_GPIO_CFG1 (*(volatile uint32_t*)0x30007004)
#define GPIO_P0_GPIO_OE (*(volatile uint32_t*)0x30007008)
#define GPIO_P0_GPIO_OUT (*(volatile uint32_t*)0x3000700C)
#define GPIO_P0_GPIO_IN (*(volatile uint32_t*)0x30007010)
#define GPIO_P0_GPIO_SET (*(volatile uint32_t*)0x30007014)
#define GPIO_P0_GPIO_CLR (*(volatile uint32_t*)0x30007018)
#define GPIO_P0_GPIO_TOG (*(volatile uint32_t*)0x3000701C)

// GPIO Port B
#define GPIO_P1_BASE 0x30007200
#define GPIO_P1_GPIO_CFG0 (*(volatile uint32_t*)0x30007200)
#define GPIO_P1_GPIO_CFG1 (*(volatile uint32_t*)0x30007204)
#define GPIO_P1_GPIO_OE (*(volatile uint32_t*)0x30007208)
#define GPIO_P1_GPIO_OUT (*(volatile uint32_t*)0x3000720C)
#define GPIO_P1_GPIO_IN (*(volatile uint32_t*)0x30007210)
#define GPIO_P1_GPIO_SET (*(volatile uint32_t*)0x30007214)
#define GPIO_P1_GPIO_CLR (*(volatile uint32_t*)0x30007218)
#define GPIO_P1_GPIO_TOG (*(volatile uint32_t*)0x3000721C)

// UART 0
#define UART0_BASE 0x30002000
#define UART0_UART_CR (*(volatile uint32_t*)0x30002000)
#define UART0_UART_BRR (*(volatile uint32_t*)0x30002004)
#define UART0_UART_TDR (*(volatile uint32_t*)0x30002008)
#define UART0_UART_RDR (*(volatile uint32_t*)0x3000200C)
#define UART0_UART_SR (*(volatile uint32_t*)0x30002010)

// 中断向量定义
#define RESET_VECTOR 1  // 
#define MACHINESOFTWARE_VECTOR 3  // 
#define MACHINETIMER_VECTOR 7  // 
#define MACHINEEXTERNAL_VECTOR 11  // 
#define UART0_VECTOR 20  // UART0 Interrupt

void bl618_init(void);

#ifdef __cplusplus
}
#endif

#endif // BL618_HPP
