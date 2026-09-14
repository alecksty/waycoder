#ifndef TM4C123GH6PM_HPP
#define TM4C123GH6PM_HPP

// TM4C123GH6PM寄存器定义
// 生成自: Texas Instruments/Tiva C/TM4C123GH6PM
// 版本: 1.0
// 日期: 2026-04-29


#ifdef __cplusplus
extern "C" {
#endif

// CPU架构: ARM-Cortex-M4F
// 位宽: 32位
// 时钟频率: 80000000 Hz

// 外设定义
// UART 0
#define UART0_BASE 0x4000C000
#define UART0_DR (*(volatile uint32_t*)0x4000C000)
#define UART0_FR (*(volatile uint32_t*)0x4000C018)
#define UART0_IBRD (*(volatile uint32_t*)0x4000C024)
#define UART0_FBRD (*(volatile uint32_t*)0x4000C028)
#define UART0_LCRH (*(volatile uint32_t*)0x4000C02C)
#define UART0_CTL (*(volatile uint32_t*)0x4000C030)
#define UART0_IM (*(volatile uint32_t*)0x4000C038)
#define UART0_RIS (*(volatile uint32_t*)0x4000C03C)
#define UART0_ICR (*(volatile uint32_t*)0x4000C044)

// UART 1
#define UART1_BASE 0x4000D000
#define UART1_DR (*(volatile uint32_t*)0x4000D000)
#define UART1_FR (*(volatile uint32_t*)0x4000D018)
#define UART1_IBRD (*(volatile uint32_t*)0x4000D024)
#define UART1_FBRD (*(volatile uint32_t*)0x4000D028)
#define UART1_LCRH (*(volatile uint32_t*)0x4000D02C)
#define UART1_CTL (*(volatile uint32_t*)0x4000D030)

// GPIO Port A
#define GPIOA_BASE 0x40004000
#define GPIOA_DATA (*(volatile uint32_t*)0x400043FC)
#define GPIOA_DIR (*(volatile uint32_t*)0x40004400)
#define GPIOA_IS (*(volatile uint32_t*)0x40004404)
#define GPIOA_IBE (*(volatile uint32_t*)0x40004408)
#define GPIOA_IEV (*(volatile uint32_t*)0x4000440C)
#define GPIOA_IM (*(volatile uint32_t*)0x40004410)
#define GPIOA_RIS (*(volatile uint32_t*)0x40004414)
#define GPIOA_MIS (*(volatile uint32_t*)0x40004418)
#define GPIOA_ICR (*(volatile uint32_t*)0x4000441C)
#define GPIOA_AFSEL (*(volatile uint32_t*)0x40004420)
#define GPIOA_DEN (*(volatile uint32_t*)0x4000451C)

// 16/32-bit Timer 0
#define TIMER0_BASE 0x40030000
#define TIMER0_CFG (*(volatile uint32_t*)0x40030000)
#define TIMER0_TAMR (*(volatile uint32_t*)0x40030004)
#define TIMER0_CTL (*(volatile uint32_t*)0x4003000C)
#define TIMER0_ILR (*(volatile uint32_t*)0x40030028)
#define TIMER0_V (*(volatile uint32_t*)0x40030038)
#define TIMER0_ICR (*(volatile uint32_t*)0x40030024)

// ADC 0
#define ADC0_BASE 0x40038000
#define ADC0_ACTSS (*(volatile uint32_t*)0x40038000)
#define ADC0_EMUX (*(volatile uint32_t*)0x40038014)
#define ADC0_SSMUX0 (*(volatile uint32_t*)0x40038040)
#define ADC0_SSFIFO0 (*(volatile uint32_t*)0x40038048)
#define ADC0_PROC (*(volatile uint32_t*)0x40038030)

void tm4c123gh6pm_init(void);

#ifdef __cplusplus
}
#endif

#endif // TM4C123GH6PM_HPP
