#ifndef STM32F303CCT6_HPP
#define STM32F303CCT6_HPP

// STM32F303CCT6寄存器定义
// 生成自: STMicroelectronics/STM32/STM32F303CCT6
// 版本: 1.0
// 日期: 2026-04-29


#ifdef __cplusplus
extern "C" {
#endif

// CPU架构: ARM-Cortex-M4F
// 位宽: 32位
// 时钟频率: 72000000 Hz

// 外设定义
// USART 1
#define USART1_BASE 0x40013800
#define USART1_SR (*(volatile uint32_t*)0x40013800)
#define USART1_DR (*(volatile uint32_t*)0x40013804)
#define USART1_BRR (*(volatile uint32_t*)0x40013808)
#define USART1_CR1 (*(volatile uint32_t*)0x4001380C)
#define USART1_CR2 (*(volatile uint32_t*)0x40013810)
#define USART1_CR3 (*(volatile uint32_t*)0x40013814)

// USART 2
#define USART2_BASE 0x40004400
#define USART2_SR (*(volatile uint32_t*)0x40004400)
#define USART2_DR (*(volatile uint32_t*)0x40004404)
#define USART2_BRR (*(volatile uint32_t*)0x40004408)
#define USART2_CR1 (*(volatile uint32_t*)0x4000440C)

// USART 3
#define USART3_BASE 0x40004800
#define USART3_SR (*(volatile uint32_t*)0x40004800)
#define USART3_DR (*(volatile uint32_t*)0x40004804)
#define USART3_BRR (*(volatile uint32_t*)0x40004808)
#define USART3_CR1 (*(volatile uint32_t*)0x4000480C)

// GPIO Port A
#define GPIOA_BASE 0x48000000
#define GPIOA_MODER (*(volatile uint32_t*)0x48000000)
#define GPIOA_OTYPER (*(volatile uint32_t*)0x48000004)
#define GPIOA_OSPEEDR (*(volatile uint32_t*)0x48000008)
#define GPIOA_PUPDR (*(volatile uint32_t*)0x4800000C)
#define GPIOA_IDR (*(volatile uint32_t*)0x48000010)
#define GPIOA_ODR (*(volatile uint32_t*)0x48000014)
#define GPIOA_BSRR (*(volatile uint32_t*)0x48000018)
#define GPIOA_AFRL (*(volatile uint32_t*)0x48000020)
#define GPIOA_AFRH (*(volatile uint32_t*)0x48000024)

// 高级定时器 1
#define TIM1_BASE 0x40012C00
#define TIM1_CR1 (*(volatile uint32_t*)0x40012C00)
#define TIM1_CNT (*(volatile uint32_t*)0x40012C24)
#define TIM1_PSC (*(volatile uint32_t*)0x40012C28)
#define TIM1_ARR (*(volatile uint32_t*)0x40012C2C)
#define TIM1_CCR1 (*(volatile uint32_t*)0x40012C34)

// ADC 1
#define ADC1_BASE 0x50000000
#define ADC1_SR (*(volatile uint32_t*)0x50000000)
#define ADC1_CR (*(volatile uint32_t*)0x50000008)
#define ADC1_CFGR (*(volatile uint32_t*)0x5000000C)
#define ADC1_SMPR1 (*(volatile uint32_t*)0x50000014)
#define ADC1_DR (*(volatile uint32_t*)0x50000040)

void stm32f303cct6_init(void);

#ifdef __cplusplus
}
#endif

#endif // STM32F303CCT6_HPP
