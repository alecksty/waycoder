#ifndef EFR32MG24_HPP
#define EFR32MG24_HPP

// EFR32MG24寄存器定义
// 生成自: Silicon Labs/EFR32/EFR32MG24
// 版本: 1.0
// 日期: 2026-04-28


#ifdef __cplusplus
extern "C" {
#endif

// CPU架构: ARM-Cortex-M33
// 位宽: 32位
// 时钟频率: 78000000 Hz

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
#define FLASH_END 0x0817FFFF
#define FLASH_SIZE 1572864

#define SRAM_START 0x20000000
#define SRAM_END 0x2003FFFF
#define SRAM_SIZE 262144

#define PERIPHERAL_START 0x40000000
#define PERIPHERAL_END 0x4007FFFF
#define PERIPHERAL_SIZE 524288

// 外设定义
// Clock Management Unit
#define CMU_BASE 0x40080000
#define CMU_CTRL (*(volatile uint32_t*)0x40080000)
#define CMU_HFCORECLKCFG (*(volatile uint32_t*)0x40080008)
#define CMU_HFPERCLKEN0 (*(volatile uint32_t*)0x40080010)
#define CMU_HFPERCLKEN0_GPIOEN 4  // GPIO clock enable
#define CMU_HFPERCLKEN0_USART0EN 12  // USART0 clock enable
#define CMU_HFPERCLKEN0_USART1EN 13  // USART1 clock enable
#define CMU_LFBCLKEN0 (*(volatile uint32_t*)0x40080020)

// GPIO Controller
#define GPIO_BASE 0x40088000
#define GPIO_PORT_A_CTRL (*(volatile uint32_t*)0x40088000)
#define GPIO_PORT_B_CTRL (*(volatile uint32_t*)0x40088004)
#define GPIO_PORT_C_CTRL (*(volatile uint32_t*)0x40088008)
#define GPIO_PORT_D_CTRL (*(volatile uint32_t*)0x4008800C)
#define GPIO_MODEL (*(volatile uint32_t*)0x40088010)
#define GPIO_MODEH (*(volatile uint32_t*)0x40088014)
#define GPIO_DOUT (*(volatile uint32_t*)0x4008801C)
#define GPIO_DOUTSET (*(volatile uint32_t*)0x40088020)
#define GPIO_DOUTCLR (*(volatile uint32_t*)0x40088024)
#define GPIO_DOUTTGL (*(volatile uint32_t*)0x40088028)
#define GPIO_DIN (*(volatile uint32_t*)0x4008802C)

// GPIO Port A extended
#define GPIO_PA_BASE 0x40088400
#define GPIO_PA_PA_CFG (*(volatile uint32_t*)0x40088400)
#define GPIO_PA_PA_PINOUT (*(volatile uint32_t*)0x40088404)

// GPIO Port B extended
#define GPIO_PB_BASE 0x40088800
#define GPIO_PB_PB_CFG (*(volatile uint32_t*)0x40088800)

// USART 0
#define USART0_BASE 0x40060000
#define USART0_CTRL (*(volatile uint32_t*)0x40060000)
#define USART0_CMD (*(volatile uint32_t*)0x40060004)
#define USART0_STATUS (*(volatile uint32_t*)0x40060008)
#define USART0_RXDATA (*(volatile uint32_t*)0x4006000C)
#define USART0_TXDATA (*(volatile uint32_t*)0x40060010)
#define USART0_CLKDIV (*(volatile uint32_t*)0x40060014)

// 中断向量定义
#define RESET_VECTOR 0  // 
#define SVCALL_VECTOR 11  // 
#define USART0_RX_VECTOR 12  // USART0 Receive Interrupt
#define USART0_TX_VECTOR 13  // USART0 Transmit Interrupt

void efr32mg24_init(void);

#ifdef __cplusplus
}
#endif

#endif // EFR32MG24_HPP
