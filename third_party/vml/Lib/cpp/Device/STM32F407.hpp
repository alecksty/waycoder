#ifndef STM32F407VGT6_HPP
#define STM32F407VGT6_HPP

// STM32F407VGT6寄存器定义
// 生成自: STMicroelectronics/STM32F4/STM32F407VGT6
// 版本: 1.0
// 日期: 2026-04-16


#ifdef __cplusplus
extern "C" {
#endif

// CPU架构: ARM-Cortex-M4
// 位宽: 32位
// 时钟频率: 16000000 Hz

// 寄存器定义
// General Purpose Register 0
#define R0 (*(volatile uint32_t*)0x00000000)

// General Purpose Register 1
#define R1 (*(volatile uint32_t*)0x00000004)

// General Purpose Register 2
#define R2 (*(volatile uint32_t*)0x00000008)

// General Purpose Register 3
#define R3 (*(volatile uint32_t*)0x0000000C)

// General Purpose Register 4
#define R4 (*(volatile uint32_t*)0x00000010)

// General Purpose Register 5
#define R5 (*(volatile uint32_t*)0x00000014)

// General Purpose Register 6
#define R6 (*(volatile uint32_t*)0x00000018)

// General Purpose Register 7
#define R7 (*(volatile uint32_t*)0x0000001C)

// General Purpose Register 8
#define R8 (*(volatile uint32_t*)0x00000020)

// General Purpose Register 9
#define R9 (*(volatile uint32_t*)0x00000024)

// General Purpose Register 10
#define R10 (*(volatile uint32_t*)0x00000028)

// General Purpose Register 11
#define R11 (*(volatile uint32_t*)0x0000002C)

// General Purpose Register 12
#define R12 (*(volatile uint32_t*)0x00000030)

// Stack Pointer
#define SP (*(volatile uint32_t*)0x00000034)

// Link Register
#define LR (*(volatile uint32_t*)0x00000038)

// Program Counter
#define PC (*(volatile uint32_t*)0x0000003C)

// Program Status Register
#define PSR (*(volatile uint32_t*)0x00000040)
#define PSR_N 31  // Negative Flag
#define PSR_Z 30  // Zero Flag
#define PSR_C 29  // Carry Flag
#define PSR_V 28  // Overflow Flag
#define PSR_Q 27  // SAT Flag
#define PSR_ICI 0  // ICI/IT
#define PSR_GE 0  // Greater than or Equal
#define PSR_IT 0  // IT status
#define PSR_Q 9  // APSR
#define PSR_IPSR 0  // IPSR

// Priority Mask Register
#define PRIMASK (*(volatile uint32_t*)0xE0000E20)

// Base Priority Register
#define BASEPRI (*(volatile uint32_t*)0xE0000E24)

// Fault Mask Register
#define FAULTMASK (*(volatile uint32_t*)0xE0000E28)

// Control Register
#define CONTROL (*(volatile uint32_t*)0xE0000E2C)

// FPU Status Control
#define FPSCR (*(volatile uint32_t*)E0000EF34)

// FP Register 0
#define S0 (*(volatile uint32_t*)0xE0000EF00)

// FP Register 1
#define S1 (*(volatile uint32_t*)0xE0000EF04)

// FP Register 2
#define S2 (*(volatile uint32_t*)0xE0000EF08)

// FP Register 3
#define S3 (*(volatile uint32_t*)0xE0000EF0C)

// FP Register 4
#define S4 (*(volatile uint32_t*)0xE0000EF10)

// FP Register 5
#define S5 (*(volatile uint32_t*)0xE0000EF14)

// FP Register 6
#define S6 (*(volatile uint32_t*)0xE0000EF18)

// FP Register 7
#define S7 (*(volatile uint32_t*)0xE0000EF1C)

// FP Register 8
#define S8 (*(volatile uint32_t*)0xE0000EF20)

// FP Register 9
#define S9 (*(volatile uint32_t*)0xE0000EF24)

// FP Register 10
#define S10 (*(volatile uint32_t*)0xE0000EF28)

// FP Register 11
#define S11 (*(volatile uint32_t*)0xE0000EF2C)

// FP Register 12
#define S12 (*(volatile uint32_t*)0xE0000EF30)

// FP Register 13
#define S13 (*(volatile uint32_t*)0xE0000EF34)

// FP Register 14
#define S14 (*(volatile uint32_t*)0xE0000EF38)

// FP Register 15
#define S15 (*(volatile uint32_t*)0xE0000EF3C)

// FP Register 16
#define S16 (*(volatile uint32_t*)0xE0000EF40)

// FP Register 17
#define S17 (*(volatile uint32_t*)0xE0000EF44)

// FP Register 18
#define S18 (*(volatile uint32_t*)0xE0000EF48)

// FP Register 19
#define S19 (*(volatile uint32_t*)0xE0000EF4C)

// FP Register 20
#define S20 (*(volatile uint32_t*)0xE0000EF50)

// FP Register 21
#define S21 (*(volatile uint32_t*)0xE0000EF54)

// FP Register 22
#define S22 (*(volatile uint32_t*)0xE0000EF58)

// FP Register 23
#define S23 (*(volatile uint32_t*)0xE0000EF5C)

// FP Register 24
#define S24 (*(volatile uint32_t*)0xE0000EF60)

// FP Register 25
#define S25 (*(volatile uint32_t*)0xE0000EF64)

// FP Register 26
#define S26 (*(volatile uint32_t*)0xE0000EF68)

// FP Register 27
#define S27 (*(volatile uint32_t*)0xE0000EF6C)

// FP Register 28
#define S28 (*(volatile uint32_t*)0xE0000EF70)

// FP Register 29
#define S29 (*(volatile uint32_t*)0xE0000EF74)

// FP Register 30
#define S30 (*(volatile uint32_t*)0xE0000EF78)

// FP Register 31
#define S31 (*(volatile uint32_t*)0xE0000EF7C)

// 内存段定义
// Main Flash (1MB)
#define FLASH_START 0x08000000
#define FLASH_END 0x080FFFFF
#define FLASH_SIZE 1048576

// System Flash (32KB)
#define SYSTEM_START 0x1FFF0000
#define SYSTEM_END 0x1FFF7FFF
#define SYSTEM_SIZE 32768

// Option Bytes (16KB)
#define OPTION_START 0x1FFF8000
#define OPTION_END 0x1FFFC000
#define OPTION_SIZE 16384

// SRAM1 (128KB)
#define SRAM1_START 0x20000000
#define SRAM1_END 0x2001FFFF
#define SRAM1_SIZE 131072

// SRAM2 (64KB)
#define SRAM2_START 0x20020000
#define SRAM2_END 0x2002FFFF
#define SRAM2_SIZE 65536

// CCM RAM (64KB)
#define CCM_START 0x20030000
#define CCM_END 0x2003FFFF
#define CCM_SIZE 65536

// Peripheral Registers
#define PERIPHERAL_START 0x40000000
#define PERIPHERAL_END 0x40023FFF
#define PERIPHERAL_SIZE 147456

// FMC Registers
#define FMC_START 0x40020000
#define FMC_END 0x40023FFF
#define FMC_SIZE 16384

// FSMC Registers
#define FSMC_START 0x40000000
#define FSMC_END 0x400003FF
#define FSMC_SIZE 1024

// GPIOA Registers
#define GPIOA_START 0x40020000
#define GPIOA_END 0x400203FF
#define GPIOA_SIZE 1024

// 外设定义
// Reset and Clock Control
#define RCC_BASE 0x40023800
#define RCC_CR (*(volatile uint32_t*)0x40023800)
#define RCC_PLLCFGR (*(volatile uint32_t*)0x40023804)
#define RCC_CFGR (*(volatile uint32_t*)0x40023808)
#define RCC_CIR (*(volatile uint32_t*)0x4002380C)
#define RCC_APB2RSTR (*(volatile uint32_t*)0x40023810)
#define RCC_APB1RSTR (*(volatile uint32_t*)0x40023814)
#define RCC_AHB1ENR (*(volatile uint32_t*)0x40023830)
#define RCC_AHB2ENR (*(volatile uint32_t*)0x40023834)
#define RCC_AHB3ENR (*(volatile uint32_t*)0x40023838)
#define RCC_APB2ENR (*(volatile uint32_t*)0x40023840)
#define RCC_APB1ENR (*(volatile uint32_t*)0x40023844)
#define RCC_BDCR (*(volatile uint32_t*)0x40023850)
#define RCC_CSR (*(volatile uint32_t*)0x40023854)
#define RCC_AHB1RSTR (*(volatile uint32_t*)0x40023820)
#define RCC_AHB2RSTR (*(volatile uint32_t*)0x40023824)
#define RCC_AHB3RSTR (*(volatile uint32_t*)0x40023828)
#define RCC_CFGR2 (*(volatile uint32_t*)0x40023818)
#define RCC_CFGR3 (*(volatile uint32_t*)0x4002381C)
#define RCC_PLL2CFGR (*(volatile uint32_t*)0x40023860)
#define RCC_PLL2DIV (*(volatile uint32_t*)0x40023864)
#define RCC_PLL3CFGR (*(volatile uint32_t*)0x40023868)
#define RCC_PLL3DIV (*(volatile uint32_t*)0x4002386C)

// Flash Interface
#define FLASH_BASE 0x40023C00
#define FLASH_ACR (*(volatile uint32_t*)0x40023C00)
#define FLASH_KEYR (*(volatile uint32_t*)0x40023C04)
#define FLASH_OPTKEYR (*(volatile uint32_t*)0x40023C08)
#define FLASH_SR (*(volatile uint32_t*)0x40023C0C)
#define FLASH_CR (*(volatile uint32_t*)0x40023C10)
#define FLASH_OPTCR (*(volatile uint32_t*)0x40023C14)
#define FLASH_OPTCR1 (*(volatile uint32_t*)0x40023C18)

// Power Control
#define PWR_BASE 0x40007000
#define PWR_CR (*(volatile uint32_t*)0x40007000)
#define PWR_CSR (*(volatile uint32_t*)0x40007004)

// DMA1 Controller
#define DMA1_BASE 0x40026000
#define DMA1_LIFCR (*(volatile uint32_t*)0x40026000)
#define DMA1_HIFCR (*(volatile uint32_t*)0x40026004)
#define DMA1_LISR (*(volatile uint32_t*)0x40026008)
#define DMA1_HISR (*(volatile uint32_t*)0x4002600C)
#define DMA1_S0CR (*(volatile uint32_t*)0x40026010)
#define DMA1_S0NDTR (*(volatile uint32_t*)0x40026014)
#define DMA1_S0PAR (*(volatile uint32_t*)0x40026018)
#define DMA1_S0M0AR (*(volatile uint32_t*)0x4002601C)
#define DMA1_S0M1AR (*(volatile uint32_t*)0x40026020)
#define DMA1_S0FCR (*(volatile uint32_t*)0x40026024)
#define DMA1_S1CR (*(volatile uint32_t*)0x40026028)
#define DMA1_S1NDTR (*(volatile uint32_t*)0x4002602C)
#define DMA1_S2CR (*(volatile uint32_t*)0x40026040)
#define DMA1_S3CR (*(volatile uint32_t*)0x40026058)
#define DMA1_S4CR (*(volatile uint32_t*)0x40026070)
#define DMA1_S5CR (*(volatile uint32_t*)0x40026088)
#define DMA1_S6CR (*(volatile uint32_t*)0x400260A0)
#define DMA1_S7CR (*(volatile uint32_t*)0x400260B8)

// DMA2 Controller
#define DMA2_BASE 0x40026400
#define DMA2_LIFCR (*(volatile uint32_t*)0x40026400)
#define DMA2_HIFCR (*(volatile uint32_t*)0x40026404)
#define DMA2_LISR (*(volatile uint32_t*)0x40026408)
#define DMA2_HISR (*(volatile uint32_t*)0x4002640C)
#define DMA2_S0CR (*(volatile uint32_t*)0x40026410)
#define DMA2_S1CR (*(volatile uint32_t*)0x40026428)
#define DMA2_S2CR (*(volatile uint32_t*)0x40026440)
#define DMA2_S3CR (*(volatile uint32_t*)0x40026458)
#define DMA2_S4CR (*(volatile uint32_t*)0x40026470)
#define DMA2_S5CR (*(volatile uint32_t*)0x40026488)
#define DMA2_S6CR (*(volatile uint32_t*)0x400264A0)
#define DMA2_S7CR (*(volatile uint32_t*)0x400264B8)

// USART1
#define USART1_BASE 0x40011000
#define USART1_SR (*(volatile uint32_t*)0x40011000)
#define USART1_DR (*(volatile uint32_t*)0x40011004)
#define USART1_BRR (*(volatile uint32_t*)0x40011008)
#define USART1_CR1 (*(volatile uint32_t*)0x4001100C)
#define USART1_CR2 (*(volatile uint32_t*)0x40011010)
#define USART1_CR3 (*(volatile uint32_t*)0x40011014)
#define USART1_GTPR (*(volatile uint32_t*)0x40011018)

// USART2
#define USART2_BASE 0x40004400
#define USART2_SR (*(volatile uint32_t*)0x40004400)
#define USART2_DR (*(volatile uint32_t*)0x40004404)
#define USART2_BRR (*(volatile uint32_t*)0x40004408)
#define USART2_CR1 (*(volatile uint32_t*)0x4000440C)
#define USART2_CR2 (*(volatile uint32_t*)0x40004410)
#define USART2_CR3 (*(volatile uint32_t*)0x40004414)

// USART3
#define USART3_BASE 0x40004800
#define USART3_SR (*(volatile uint32_t*)0x40004800)
#define USART3_DR (*(volatile uint32_t*)0x40004804)
#define USART3_BRR (*(volatile uint32_t*)0x40004808)
#define USART3_CR1 (*(volatile uint32_t*)0x4000480C)
#define USART3_CR2 (*(volatile uint32_t*)0x40004810)
#define USART3_CR3 (*(volatile uint32_t*)0x40004814)

// SPI1
#define SPI1_BASE 0x40013000
#define SPI1_CR1 (*(volatile uint32_t*)0x40013000)
#define SPI1_CR2 (*(volatile uint32_t*)0x40013004)
#define SPI1_SR (*(volatile uint32_t*)0x40013008)
#define SPI1_DR (*(volatile uint32_t*)0x4001300C)
#define SPI1_CRCPR (*(volatile uint32_t*)0x40013010)
#define SPI1_RXCRCR (*(volatile uint32_t*)0x40013014)
#define SPI1_TXCRCR (*(volatile uint32_t*)0x40013018)
#define SPI1_I2SCFGR (*(volatile uint32_t*)0x4001301C)

// SPI2
#define SPI2_BASE 0x40003800
#define SPI2_CR1 (*(volatile uint32_t*)0x40003800)
#define SPI2_CR2 (*(volatile uint32_t*)0x40003804)
#define SPI2_SR (*(volatile uint32_t*)0x40003808)
#define SPI2_DR (*(volatile uint32_t*)0x4000380C)
#define SPI2_CRCPR (*(volatile uint32_t*)0x40003810)
#define SPI2_RXCRCR (*(volatile uint32_t*)0x40003814)

// SPI3
#define SPI3_BASE 0x40003C00
#define SPI3_CR1 (*(volatile uint32_t*)0x40003C00)
#define SPI3_CR2 (*(volatile uint32_t*)0x40003C04)
#define SPI3_SR (*(volatile uint32_t*)0x40003C08)
#define SPI3_DR (*(volatile uint32_t*)0x40003C0C)

// I2C1
#define I2C1_BASE 0x40005400
#define I2C1_CR1 (*(volatile uint32_t*)0x40005400)
#define I2C1_CR2 (*(volatile uint32_t*)0x40005404)
#define I2C1_OAR1 (*(volatile uint32_t*)0x40005408)
#define I2C1_OAR2 (*(volatile uint32_t*)0x4000540C)
#define I2C1_DR (*(volatile uint32_t*)0x40005410)
#define I2C1_SR1 (*(volatile uint32_t*)0x40005414)
#define I2C1_SR2 (*(volatile uint32_t*)0x40005418)
#define I2C1_CCR (*(volatile uint32_t*)0x4000541C)
#define I2C1_TRISE (*(volatile uint32_t*)0x40005420)
#define I2C1_FLTR (*(volatile uint32_t*)0x40005424)

// I2C2
#define I2C2_BASE 0x40005800
#define I2C2_CR1 (*(volatile uint32_t*)0x40005800)
#define I2C2_CR2 (*(volatile uint32_t*)0x40005804)
#define I2C2_OAR1 (*(volatile uint32_t*)0x40005808)
#define I2C2_OAR2 (*(volatile uint32_t*)0x4000580C)
#define I2C2_DR (*(volatile uint32_t*)0x40005810)
#define I2C2_SR1 (*(volatile uint32_t*)0x40005814)
#define I2C2_SR2 (*(volatile uint32_t*)0x40005818)
#define I2C2_CCR (*(volatile uint32_t*)0x4000581C)
#define I2C2_TRISE (*(volatile uint32_t*)0x40005820)

// I2C3
#define I2C3_BASE 0x40005C00
#define I2C3_CR1 (*(volatile uint32_t*)0x40005C00)
#define I2C3_CR2 (*(volatile uint32_t*)0x40005C04)
#define I2C3_OAR1 (*(volatile uint32_t*)0x40005C08)
#define I2C3_DR (*(volatile uint32_t*)0x40005C10)
#define I2C3_SR1 (*(volatile uint32_t*)0x40005C14)
#define I2C3_SR2 (*(volatile uint32_t*)0x40005C18)
#define I2C3_CCR (*(volatile uint32_t*)0x40005C1C)

// Advanced Timer 1
#define TIM1_BASE 0x40012C00
#define TIM1_CR1 (*(volatile uint32_t*)0x40012C00)
#define TIM1_CR2 (*(volatile uint32_t*)0x40012C04)
#define TIM1_SMCR (*(volatile uint32_t*)0x40012C08)
#define TIM1_DIER (*(volatile uint32_t*)0x40012C0C)
#define TIM1_SR (*(volatile uint32_t*)0x40012C10)
#define TIM1_EGR (*(volatile uint32_t*)0x40012C14)
#define TIM1_CCMR1 (*(volatile uint32_t*)0x40012C18)
#define TIM1_CCMR2 (*(volatile uint32_t*)0x40012C1C)
#define TIM1_CCER (*(volatile uint32_t*)0x40012C20)
#define TIM1_CNT (*(volatile uint32_t*)0x40012C24)
#define TIM1_PSC (*(volatile uint32_t*)0x40012C28)
#define TIM1_ARR (*(volatile uint32_t*)0x40012C2C)
#define TIM1_RCR (*(volatile uint32_t*)0x40012C30)
#define TIM1_CCR1 (*(volatile uint32_t*)0x40012C34)
#define TIM1_CCR2 (*(volatile uint32_t*)0x40012C38)
#define TIM1_CCR3 (*(volatile uint32_t*)0x40012C3C)
#define TIM1_CCR4 (*(volatile uint32_t*)0x40012C40)
#define TIM1_BDTR (*(volatile uint32_t*)0x40012C44)
#define TIM1_DCR (*(volatile uint32_t*)0x40012C48)
#define TIM1_DMAR (*(volatile uint32_t*)0x40012C4C)

// General Purpose Timer 2
#define TIM2_BASE 0x40000000
#define TIM2_CR1 (*(volatile uint32_t*)0x40000000)
#define TIM2_CR2 (*(volatile uint32_t*)0x40000004)
#define TIM2_SMCR (*(volatile uint32_t*)0x40000008)
#define TIM2_DIER (*(volatile uint32_t*)0x4000000C)
#define TIM2_SR (*(volatile uint32_t*)0x40000010)
#define TIM2_EGR (*(volatile uint32_t*)0x40000014)
#define TIM2_CCMR1 (*(volatile uint32_t*)0x40000018)
#define TIM2_CCMR2 (*(volatile uint32_t*)0x4000001C)
#define TIM2_CCER (*(volatile uint32_t*)0x40000020)
#define TIM2_CNT (*(volatile uint32_t*)0x40000024)
#define TIM2_PSC (*(volatile uint32_t*)0x40000028)
#define TIM2_ARR (*(volatile uint32_t*)0x4000002C)
#define TIM2_CCR1 (*(volatile uint32_t*)0x40000034)
#define TIM2_CCR2 (*(volatile uint32_t*)0x40000038)
#define TIM2_CCR3 (*(volatile uint32_t*)0x4000003C)
#define TIM2_CCR4 (*(volatile uint32_t*)0x40000040)

// General Purpose Timer 3
#define TIM3_BASE 0x40000400
#define TIM3_CR1 (*(volatile uint32_t*)0x40000400)
#define TIM3_SR (*(volatile uint32_t*)0x40000410)
#define TIM3_CCMR1 (*(volatile uint32_t*)0x40000418)
#define TIM3_CCER (*(volatile uint32_t*)0x40000420)
#define TIM3_CNT (*(volatile uint32_t*)0x40000424)
#define TIM3_PSC (*(volatile uint32_t*)0x40000428)
#define TIM3_ARR (*(volatile uint32_t*)0x4000042C)
#define TIM3_CCR1 (*(volatile uint32_t*)0x40000434)
#define TIM3_CCR2 (*(volatile uint32_t*)0x40000438)
#define TIM3_CCR3 (*(volatile uint32_t*)0x4000043C)
#define TIM3_CCR4 (*(volatile uint32_t*)0x40000440)

// General Purpose Timer 4
#define TIM4_BASE 0x40000800
#define TIM4_CR1 (*(volatile uint32_t*)0x40000800)
#define TIM4_SR (*(volatile uint32_t*)0x40000810)
#define TIM4_CCMR1 (*(volatile uint32_t*)0x40000818)
#define TIM4_CCER (*(volatile uint32_t*)0x40000820)
#define TIM4_CNT (*(volatile uint32_t*)0x40000824)
#define TIM4_PSC (*(volatile uint32_t*)0x40000828)
#define TIM4_ARR (*(volatile uint32_t*)0x4000082C)
#define TIM4_CCR1 (*(volatile uint32_t*)0x40000834)
#define TIM4_CCR2 (*(volatile uint32_t*)0x40000838)
#define TIM4_CCR3 (*(volatile uint32_t*)0x4000083C)
#define TIM4_CCR4 (*(volatile uint32_t*)0x40000840)

// General Purpose Timer 5
#define TIM5_BASE 0x40000C00
#define TIM5_CR1 (*(volatile uint32_t*)0x40000C00)
#define TIM5_SR (*(volatile uint32_t*)0x40000C10)
#define TIM5_CNT (*(volatile uint32_t*)0x40000C24)
#define TIM5_PSC (*(volatile uint32_t*)0x40000C28)
#define TIM5_ARR (*(volatile uint32_t*)0x40000C2C)
#define TIM5_CCR1 (*(volatile uint32_t*)0x40000C34)
#define TIM5_CCR2 (*(volatile uint32_t*)0x40000C38)
#define TIM5_CCR3 (*(volatile uint32_t*)0x40000C3C)
#define TIM5_CCR4 (*(volatile uint32_t*)0x40000C40)

// General Purpose Timer 9
#define TIM9_BASE 0x40014C00
#define TIM9_CR1 (*(volatile uint32_t*)0x40014C00)
#define TIM9_SMCR (*(volatile uint32_t*)0x40014C08)
#define TIM9_DIER (*(volatile uint32_t*)0x40014C0C)
#define TIM9_SR (*(volatile uint32_t*)0x40014C10)
#define TIM9_EGR (*(volatile uint32_t*)0x40014C14)
#define TIM9_CCMR1 (*(volatile uint32_t*)0x40014C18)
#define TIM9_CCER (*(volatile uint32_t*)0x40014C20)
#define TIM9_CNT (*(volatile uint32_t*)0x40014C24)
#define TIM9_PSC (*(volatile uint32_t*)0x40014C28)
#define TIM9_ARR (*(volatile uint32_t*)0x40014C2C)
#define TIM9_CCR1 (*(volatile uint32_t*)0x40014C34)
#define TIM9_CCR2 (*(volatile uint32_t*)0x40014C38)

// General Purpose Timer 10
#define TIM10_BASE 0x40015000
#define TIM10_CR1 (*(volatile uint32_t*)0x40015000)
#define TIM10_DIER (*(volatile uint32_t*)0x4001500C)
#define TIM10_SR (*(volatile uint32_t*)0x40015010)
#define TIM10_EGR (*(volatile uint32_t*)0x40015014)
#define TIM10_CCMR1 (*(volatile uint32_t*)0x40015018)
#define TIM10_CCER (*(volatile uint32_t*)0x40015020)
#define TIM10_CNT (*(volatile uint32_t*)0x40015024)
#define TIM10_PSC (*(volatile uint32_t*)0x40015028)
#define TIM10_ARR (*(volatile uint32_t*)0x4001502C)
#define TIM10_CCR1 (*(volatile uint32_t*)0x40015034)

// ADC1
#define ADC1_BASE 0x40012000
#define ADC1_SR (*(volatile uint32_t*)0x40012000)
#define ADC1_CR1 (*(volatile uint32_t*)0x40012004)
#define ADC1_CR2 (*(volatile uint32_t*)0x40012008)
#define ADC1_SMPR1 (*(volatile uint32_t*)0x4001200C)
#define ADC1_SMPR2 (*(volatile uint32_t*)0x40012010)
#define ADC1_JOFR1 (*(volatile uint32_t*)0x40012014)
#define ADC1_JOFR2 (*(volatile uint32_t*)0x40012018)
#define ADC1_JOFR3 (*(volatile uint32_t*)0x4001201C)
#define ADC1_JOFR4 (*(volatile uint32_t*)0x40012020)
#define ADC1_HTR (*(volatile uint32_t*)0x40012024)
#define ADC1_LTR (*(volatile uint32_t*)0x40012028)
#define ADC1_SQR1 (*(volatile uint32_t*)0x4001202C)
#define ADC1_SQR2 (*(volatile uint32_t*)0x40012030)
#define ADC1_SQR3 (*(volatile uint32_t*)0x40012034)
#define ADC1_JSQR (*(volatile uint32_t*)0x4001203C)
#define ADC1_JDR1 (*(volatile uint32_t*)0x40012040)
#define ADC1_JDR2 (*(volatile uint32_t*)0x40012044)
#define ADC1_JDR3 (*(volatile uint32_t*)0x40012048)
#define ADC1_JDR4 (*(volatile uint32_t*)0x4001204C)
#define ADC1_DR (*(volatile uint32_t*)0x40012050)

// ADC2
#define ADC2_BASE 0x40012100
#define ADC2_SR (*(volatile uint32_t*)0x40012100)
#define ADC2_CR1 (*(volatile uint32_t*)0x40012104)
#define ADC2_CR2 (*(volatile uint32_t*)0x40012108)
#define ADC2_SMPR1 (*(volatile uint32_t*)0x4001210C)
#define ADC2_SMPR2 (*(volatile uint32_t*)0x40012110)
#define ADC2_SQR1 (*(volatile uint32_t*)0x4001212C)
#define ADC2_DR (*(volatile uint32_t*)0x40012150)

// ADC3
#define ADC3_BASE 0x40012200
#define ADC3_SR (*(volatile uint32_t*)0x40012200)
#define ADC3_CR1 (*(volatile uint32_t*)0x40012204)
#define ADC3_CR2 (*(volatile uint32_t*)0x40012208)
#define ADC3_SMPR1 (*(volatile uint32_t*)0x4001220C)
#define ADC3_SMPR2 (*(volatile uint32_t*)0x40012210)
#define ADC3_SQR1 (*(volatile uint32_t*)0x4001222C)
#define ADC3_DR (*(volatile uint32_t*)0x40012250)

// System Configuration Controller
#define SYSCFG_BASE 0x40013800
#define SYSCFG_CFGR (*(volatile uint32_t*)0x40013800)
#define SYSCFG_EXTICR1 (*(volatile uint32_t*)0x40013808)
#define SYSCFG_EXTICR2 (*(volatile uint32_t*)0x4001380C)
#define SYSCFG_EXTICR3 (*(volatile uint32_t*)0x40013810)
#define SYSCFG_EXTICR4 (*(volatile uint32_t*)0x40013814)
#define SYSCFG_CBR (*(volatile uint32_t*)0x4001381C)

// External Interrupt/Event Controller
#define EXTI_BASE 0x40013C00
#define EXTI_IMR (*(volatile uint32_t*)0x40013C00)
#define EXTI_EMR (*(volatile uint32_t*)0x40013C04)
#define EXTI_RTSR (*(volatile uint32_t*)0x40013C08)
#define EXTI_FTSR (*(volatile uint32_t*)0x40013C0C)
#define EXTI_SWIER (*(volatile uint32_t*)0x40013C10)
#define EXTI_PR (*(volatile uint32_t*)0x40013C14)

// Random Number Generator
#define RNG_BASE 0x50060800
#define RNG_CR (*(volatile uint32_t*)0x50060800)
#define RNG_SR (*(volatile uint32_t*)0x50060804)
#define RNG_DR (*(volatile uint32_t*)0x50060808)

// CRYP Accelerator
#define CRYP_BASE 0x50060000
#define CRYP_CR (*(volatile uint32_t*)0x50060000)
#define CRYP_SR (*(volatile uint32_t*)0x50060004)
#define CRYP_DIN (*(volatile uint32_t*)0x50060008)
#define CRYP_DOUT (*(volatile uint32_t*)0x5006000C)
#define CRYP_DMACR (*(volatile uint32_t*)0x50060010)
#define CRYP_IMSCR (*(volatile uint32_t*)0x50060014)
#define CRYP_RISR (*(volatile uint32_t*)0x50060018)
#define CRYP_MISR (*(volatile uint32_t*)0x5006001C)
#define CRYP_K0LR (*(volatile uint32_t*)0x50060020)
#define CRYP_K0RR (*(volatile uint32_t*)0x50060024)
#define CRYP_K1LR (*(volatile uint32_t*)0x50060028)
#define CRYP_K1RR (*(volatile uint32_t*)0x5006002C)
#define CRYP_K2LR (*(volatile uint32_t*)0x50060030)
#define CRYP_K2RR (*(volatile uint32_t*)0x50060034)
#define CRYP_K3LR (*(volatile uint32_t*)0x50060038)
#define CRYP_K3RR (*(volatile uint32_t*)0x5006003C)
#define CRYP_IV0LR (*(volatile uint32_t*)0x50060040)
#define CRYP_IV0RR (*(volatile uint32_t*)0x50060044)
#define CRYP_IV1LR (*(volatile uint32_t*)0x50060048)
#define CRYP_IV1RR (*(volatile uint32_t*)0x5006004C)

// HASH Accelerator
#define HASH_BASE 0x50060400
#define HASH_CR (*(volatile uint32_t*)0x50060400)
#define HASH_DIN (*(volatile uint32_t*)0x50060404)
#define HASH_DINSTAT (*(volatile uint32_t*)0x50060408)
#define HASH_HR (*(volatile uint32_t*)0x5006040C)
#define HASH_IMR (*(volatile uint32_t*)0x50060420)
#define HASH_SR (*(volatile uint32_t*)0x50060424)

// Digital Camera Interface
#define DCMI_BASE 0x50050000
#define DCMI_CR (*(volatile uint32_t*)0x50050000)
#define DCMI_SR (*(volatile uint32_t*)0x50050004)
#define DCMI_RISR (*(volatile uint32_t*)0x50050008)
#define DCMI_IER (*(volatile uint32_t*)0x5005000C)
#define DCMI_MISR (*(volatile uint32_t*)0x50050010)
#define DCMI_ICR (*(volatile uint32_t*)0x50050014)
#define DCMI_MFISH (*(volatile uint32_t*)0x5005001C)
#define DCMI_CWSTRT (*(volatile uint32_t*)0x50050020)
#define DCMI_CWSIZE (*(volatile uint32_t*)0x50050024)
#define DCMI_DR (*(volatile uint32_t*)0x50050028)
#define DCMI_OR (*(volatile uint32_t*)0x5005002C)

// USB OTG High Speed
#define USB_OTG_HS_BASE 0x40040000
#define USB_OTG_HS_GOTGCTL (*(volatile uint32_t*)0x40040000)
#define USB_OTG_HS_GOTGINT (*(volatile uint32_t*)0x40040004)
#define USB_OTG_HS_GINTMSK (*(volatile uint32_t*)0x40040008)
#define USB_OTG_HS_GRSTCTL (*(volatile uint32_t*)0x4004000C)
#define USB_OTG_HS_GINTSTS (*(volatile uint32_t*)0x40040010)
#define USB_OTG_HS_GRXSTSR (*(volatile uint32_t*)0x40040014)
#define USB_OTG_HS_GRXFSIZ (*(volatile uint32_t*)0x40040024)
#define USB_OTG_HS_HNPTXFSIZ (*(volatile uint32_t*)0x40040028)
#define USB_OTG_HS_HNPTXSTS (*(volatile uint32_t*)0x4004002C)
#define USB_OTG_HS_GCCFG (*(volatile uint32_t*)0x40040038)
#define USB_OTG_HS_CID (*(volatile uint32_t*)0x4004003C)
#define USB_OTG_HS_HPTXFSIZ (*(volatile uint32_t*)0x40040100)
#define USB_OTG_HS_DIEPTXF (*(volatile uint32_t*)0x40040200)

// Ethernet
#define ETH_BASE 0x40028000
#define ETH_MACCR (*(volatile uint32_t*)0x40028000)
#define ETH_MACFFR (*(volatile uint32_t*)0x40028004)
#define ETH_MACHTHR (*(volatile uint32_t*)0x40028008)
#define ETH_MACHTLR (*(volatile uint32_t*)0x4002800C)
#define ETH_MACMIIAR (*(volatile uint32_t*)0x40028010)
#define ETH_MACMIIDR (*(volatile uint32_t*)0x40028014)
#define ETH_MACCR (*(volatile uint32_t*)0x40028018)
#define ETH_MACVLANTR (*(volatile uint32_t*)0x4002801C)
#define ETH_MACRWUFFR (*(volatile uint32_t*)0x40028028)
#define ETH_MACPMTCSR (*(volatile uint32_t*)0x4002802C)
#define ETH_MACSR (*(volatile uint32_t*)0x40028030)
#define ETH_MACIMR (*(volatile uint32_t*)0x40028034)
#define ETH_MACA0HR (*(volatile uint32_t*)0x40028040)
#define ETH_MACA0LR (*(volatile uint32_t*)0x40028044)
#define ETH_MACA1HR (*(volatile uint32_t*)0x40028048)
#define ETH_MACA1LR (*(volatile uint32_t*)0x4002804C)
#define ETH_MMCCR (*(volatile uint32_t*)0x40028100)
#define ETH_MMCRIR (*(volatile uint32_t*)0x40028104)
#define ETH_MMCTIR (*(volatile uint32_t*)0x40028108)
#define ETH_MMCRIMR (*(volatile uint32_t*)0x4002810C)
#define ETH_MMCTIMR (*(volatile uint32_t*)0x40028110)
#define ETH_MMCTGBSCCR (*(volatile uint32_t*)0x40028114)
#define ETH_MMCRGUFCCR (*(volatile uint32_t*)0x40028118)
#define ETH_PTPTSCR (*(volatile uint32_t*)0x40028700)
#define ETH_PTPSSIR (*(volatile uint32_t*)0x40028704)
#define ETH_PTPTSHR (*(volatile uint32_t*)0x40028708)
#define ETH_PTPTSLR (*(volatile uint32_t*)0x4002870C)
#define ETH_PTPTSHUR (*(volatile uint32_t*)0x40028710)
#define ETH_PTPTSLUR (*(volatile uint32_t*)0x40028714)
#define ETH_PTPTSAR (*(volatile uint32_t*)0x40028718)
#define ETH_PTPTTHR (*(volatile uint32_t*)0x4002871C)
#define ETH_PTPTTLR (*(volatile uint32_t*)0x40028720)
#define ETH_PTPTSR (*(volatile uint32_t*)0x40028728)
#define ETH_DMABMR (*(volatile uint32_t*)0x40029000)
#define ETH_DMASR (*(volatile uint32_t*)0x40029004)
#define ETH_DMAOMR (*(volatile uint32_t*)0x40029008)
#define ETH_DMAIER (*(volatile uint32_t*)0x4002900C)
#define ETH_DMAMFBOCR (*(volatile uint32_t*)0x40029010)
#define ETH_DMACHTDR (*(volatile uint32_t*)0x40029014)
#define ETH_DMACHRDR (*(volatile uint32_t*)0x40029018)
#define ETH_DMACHTBAR (*(volatile uint32_t*)0x4002901C)
#define ETH_DMACHRBAR (*(volatile uint32_t*)0x40029020)

// 中断向量定义
#define WWDG_VECTOR 0  // Window WatchDog interrupt
#define PVD_VECTOR 1  // PVD through EXTI line detection interrupt
#define TAMPER_VECTOR 2  // Tamper interrupt
#define RTC_WKUP_VECTOR 3  // RTC Wakeup interrupt
#define FLASH_VECTOR 4  // FLASH global interrupt
#define RCC_VECTOR 5  // RCC global interrupt
#define EXTI0_VECTOR 6  // EXTI Line0 interrupt
#define EXTI1_VECTOR 7  // EXTI Line1 interrupt
#define EXTI2_VECTOR 8  // EXTI Line2 interrupt
#define EXTI3_VECTOR 9  // EXTI Line3 interrupt
#define EXTI4_VECTOR 10  // EXTI Line4 interrupt
#define DMA1_STREAM0_VECTOR 11  // DMA1 Stream0 global interrupt
#define DMA1_STREAM1_VECTOR 12  // DMA1 Stream1 global interrupt
#define DMA1_STREAM2_VECTOR 13  // DMA1 Stream2 global interrupt
#define DMA1_STREAM3_VECTOR 14  // DMA1 Stream3 global interrupt
#define DMA1_STREAM4_VECTOR 15  // DMA1 Stream4 global interrupt
#define DMA1_STREAM5_VECTOR 16  // DMA1 Stream5 global interrupt
#define DMA1_STREAM6_VECTOR 17  // DMA1 Stream6 global interrupt
#define ADC_VECTOR 18  // ADC1 global interrupt
#define CAN1_TX_VECTOR 19  // CAN1 TX interrupts
#define CAN1_RX0_VECTOR 20  // CAN1 RX0 interrupts
#define CAN1_RX1_VECTOR 21  // CAN1 RX1 interrupt
#define CAN1_SCE_VECTOR 22  // CAN1 SCE interrupt
#define EXTI9_5_VECTOR 23  // EXTI Line[9:5] interrupt
#define TIM1_BRK_TIM9_VECTOR 24  // TIM1 Break interrupt and TIM9 global interrupt
#define TIM1_UP_TIM10_VECTOR 25  // TIM1 Update interrupt and TIM10 global interrupt
#define TIM1_TRG_COM_TIM11_VECTOR 26  // TIM1 Trigger and commutation interrupt and TIM11 global interrupt
#define TIM1_CC_VECTOR 27  // TIM1 Capture Compare interrupt
#define TIM2_VECTOR 28  // TIM2 global interrupt
#define TIM3_VECTOR 29  // TIM3 global interrupt
#define TIM4_VECTOR 30  // TIM4 global interrupt
#define I2C1_EV_VECTOR 31  // I2C1 Event interrupt
#define I2C1_ER_VECTOR 32  // I2C1 Error interrupt
#define I2C2_EV_VECTOR 33  // I2C2 Event interrupt
#define I2C2_ER_VECTOR 34  // I2C2 Error interrupt
#define SPI1_VECTOR 35  // SPI1 global interrupt
#define SPI2_VECTOR 36  // SPI2 global interrupt
#define USART1_VECTOR 37  // USART1 global interrupt
#define USART2_VECTOR 38  // USART2 global interrupt
#define USART3_VECTOR 39  // USART3 global interrupt
#define EXTI15_10_VECTOR 40  // EXTI Line[15:10] interrupts
#define RTC_ALARM_VECTOR 41  // RTC Alarm (A and B) through EXTI Line interrupt
#define OTG_FS_WKUP_VECTOR 42  // USB OTG FS Wakeup through EXTI interrupt
#define TIM8_BRK_TIM12_VECTOR 43  // TIM8 Break interrupt and TIM12 global interrupt
#define TIM8_UP_TIM13_VECTOR 44  // TIM8 Update interrupt and TIM13 global interrupt
#define TIM8_TRG_COM_TIM14_VECTOR 45  // TIM8 Trigger and commutation interrupt and TIM14 global interrupt
#define TIM8_CC_VECTOR 46  // TIM8 Capture Compare interrupt
#define SPI3_VECTOR 47  // SPI3 global interrupt
#define UART4_VECTOR 48  // UART4 global interrupt
#define UART5_VECTOR 49  // UART5 global interrupt
#define TIM6_VECTOR 50  // TIM6 global interrupt
#define TIM7_VECTOR 51  // TIM7 global interrupt
#define DMA2_STREAM0_VECTOR 52  // DMA2 Stream0 global interrupt
#define DMA2_STREAM1_VECTOR 53  // DMA2 Stream1 global interrupt
#define DMA2_STREAM2_VECTOR 54  // DMA2 Stream2 global interrupt
#define DMA2_STREAM3_VECTOR 55  // DMA2 Stream3 global interrupt
#define DMA2_STREAM4_VECTOR 56  // DMA2 Stream4 global interrupt
#define ETH_VECTOR 57  // Ethernet global interrupt
#define ETH_WKUP_VECTOR 58  // Ethernet Wakeup through EXTI interrupt
#define CAN2_TX_VECTOR 59  // CAN2 TX interrupts
#define CAN2_RX0_VECTOR 60  // CAN2 RX0 interrupts
#define CAN2_RX1_VECTOR 61  // CAN2 RX1 interrupt
#define CAN2_SCE_VECTOR 62  // CAN2 SCE interrupt
#define NA_VECTOR 63  // Not Available
#define OTG_FS_VECTOR 64  // USB OTG FS global interrupt
#define DCMI_VECTOR 65  // DCMI global interrupt
#define CRYP_VECTOR 66  // CRYP global interrupt
#define HASH_RNG_VECTOR 67  // HASH and RNG global interrupt
#define FPU_VECTOR 68  // FPU global interrupt

// 引脚定义
#define PIN_PE2 1  // Tristate - any function
#define PIN_PE3 2  // Tristate - any function
#define PIN_PE4 3  // Tristate - any function
#define PIN_PE5 4  // Tristate - any function
#define PIN_PE6 5  // Tristate - any function
#define PIN_VCAP1 6  // 1.2V voltage supply
#define PIN_VBAT 7  // Battery voltage supply
#define PIN_PC13 8  // TAMPER-RTC
#define PIN_PC14 9  // OSC32_IN
#define PIN_PC15 10  // OSC32_OUT
#define PIN_PH0 11  // OSC_IN
#define PIN_PH1 12  // OSC_OUT
#define PIN_NRST 13  // External reset
#define PIN_VSSA 14  // Analog ground
#define PIN_VDDA 15  // Analog supply
#define PIN_PA0 16  // WKUP/USART_CTS
#define PIN_PA1 17  // USART_RTS
#define PIN_PA2 18  // USART_TX
#define PIN_PA3 19  // USART_RX
#define PIN_PA4 20  // DAC1_OUT
#define PIN_PA5 21  // DAC2_OUT
#define PIN_PA6 22  // SPI1_MISO
#define PIN_PA7 23  // SPI1_MOSI
#define PIN_PA8 24  // MCO1
#define PIN_PA9 25  // USART1_TX
#define PIN_PA10 26  // USART1_RX
#define PIN_PA11 27  // USB_DM
#define PIN_PA12 28  // USB_DP
#define PIN_PA13 29  // JTMS/SWDIO
#define PIN_VCAP2 30  // 1.2V voltage supply
#define PIN_PA14 31  // JTCK/SWCLK
#define PIN_PA15 32  // JTDI
#define PIN_PB0 33  // SPI1_CS/TIM3_CH3
#define PIN_PB1 34  // TIM3_CH4
#define PIN_PB2 35  // BOOT1
#define PIN_PB3 36  // JTDO/TRACESWO
#define PIN_PB4 37  // NJTRST
#define PIN_PB5 38  // CAN2_RX/TIM3_CH2
#define PIN_PB6 39  // USART1_TX
#define PIN_PB7 40  // USART1_RX
#define PIN_PB8 41  // I2C1_SCL/TIM4_CH1
#define PIN_PB9 42  // I2C1_SDA/TIM4_CH2
#define PIN_PB10 43  // I2C2_SCL/USART3_TX
#define PIN_PB11 44  // I2C2_SDA/USART3_RX
#define PIN_PB12 45  // SPI2_CS/I2C2_SMBA
#define PIN_PB13 46  // SPI2_SCK
#define PIN_PB14 47  // SPI2_MISO
#define PIN_PB15 48  // SPI2_MOSI
#define PIN_PD0 49  // FSMC_D2
#define PIN_PD1 50  // FSMC_D3
#define PIN_PD2 51  // TIM3_ETR/UART4_RX
#define PIN_PD3 52  // FSMC_CLK/UART4_CTS
#define PIN_PD4 53  // FSMC_NOE
#define PIN_PD5 54  // FSMC_NWE
#define PIN_PD6 55  // FSMC_NWAIT
#define PIN_PD7 56  // FSMC_NE1/FSMC_NCE2
#define PIN_PD8 57  // FSMC_D13/USART3_TX
#define PIN_PD9 58  // FSMC_D14/USART3_RX
#define PIN_PD10 59  // FSMC_D15/USART3_CK
#define PIN_PD11 60  // FSMC_A16/USART3_CTS
#define PIN_PD12 61  // FSMC_A17/TIM4_CH1
#define PIN_PD13 62  // FSMC_A18/TIM4_CH2
#define PIN_PD14 63  // FSMC_D0/TIM4_CH3
#define PIN_PD15 64  // FSMC_D1/TIM4_CH4
#define PIN_PG0 65  // FSMC_A10/TIM4_ETR
#define PIN_PG1 66  // FSMC_A11
#define PIN_PE0 67  // FSMC_NBL0/TIM4_CH3
#define PIN_PE1 68  // FSMC_NBL1/TIM4_CH4
#define PIN_PE3 69  // FSMC_A19
#define PIN_PE4 70  // FSMC_A20
#define PIN_PE5 71  // FSMC_A21
#define PIN_PE6 72  // FSMC_A22/TIM4_CH1
#define PIN_PE7 73  // FSMC_D4/USART6_RX
#define PIN_PE8 74  // FSMC_D5/USART6_TX
#define PIN_PE9 75  // FSMC_D6/TIM4_CH1
#define PIN_PE10 76  // FSMC_D7/USART6_RTS
#define PIN_PE11 77  // FSMC_D8
#define PIN_PE12 78  // FSMC_D9
#define PIN_PE13 79  // FSMC_D10
#define PIN_PE14 80  // FSMC_D11
#define PIN_PE15 81  // FSMC_D12
#define PIN_PB10 82  // I2C2_SCL/USART3_TX
#define PIN_PB11 83  // I2C2_SDA/USART3_RX
#define PIN_PB12 84  // SPI2_CS/I2C2_SMBA
#define PIN_PB13 85  // SPI2_SCK
#define PIN_PB14 86  // SPI2_MISO
#define PIN_PB15 87  // SPI2_MOSI
#define PIN_PD8 88  // FSMC_D13/USART3_TX
#define PIN_PD9 89  // FSMC_D14/USART3_RX
#define PIN_PD10 90  // FSMC_D15/USART3_CK
#define PIN_PD11 91  // FSMC_A16/USART3_CTS
#define PIN_PD12 92  // FSMC_A17/TIM4_CH1
#define PIN_PD13 93  // FSMC_A18/TIM4_CH2
#define PIN_PD14 94  // FSMC_D0/TIM4_CH3
#define PIN_PD15 95  // FSMC_D1/TIM4_CH4
#define PIN_PG2 96  // FSMC_A12
#define PIN_PG3 97  // FSMC_A13
#define PIN_PG4 98  // FSMC_A14
#define PIN_PG5 99  // FSMC_A15
#define PIN_PG6 100  // FSMC_INT2
#define PIN_PG7 101  // FSMC_INT3
#define PIN_PG8 102  // FSMC_INT4
#define PIN_PG9 103  // FSMC_NE2/FSMC_NCE3
#define PIN_PG10 104  // FSMC_NCE4_1
#define PIN_PG11 105  // FSMC_NCE4_2
#define PIN_PG12 106  // FSMC_NCE4_3
#define PIN_PG13 107  // FSMC_A24
#define PIN_PG14 108  // FSMC_A25
#define PIN_PG15 109  // FSMC_INT2
#define PIN_VSS 110  // Ground
#define PIN_VDD 111  // 3.3V Supply

void stm32f407vgt6_init(void);

#ifdef __cplusplus
}
#endif

#endif // STM32F407VGT6_HPP
