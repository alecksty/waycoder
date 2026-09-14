// STM32F407VGT6 设备定义 - Objective-C 头文件
// 生成自: STMicroelectronics/STM32F4/STM32F407VGT6
// 版本: 1.0
// 日期: 2026-04-16
// 作者: VML Team
// 描述: High-performance ARM Cortex-M4 with FPU, 168MHz, 1MB Flash, 192KB SRAM
// CPU架构: ARM-Cortex-M4
// 位宽: 32位
// 时钟频率: 16000000 Hz

#ifndef STM32F407VGT6_DEVICE_H
#define STM32F407VGT6_DEVICE_H

#import <Foundation/Foundation.h>

// 寄存器地址定义
#define R0_ADDR 0x00000000  // General Purpose Register 0
#define R1_ADDR 0x00000004  // General Purpose Register 1
#define R2_ADDR 0x00000008  // General Purpose Register 2
#define R3_ADDR 0x0000000C  // General Purpose Register 3
#define R4_ADDR 0x00000010  // General Purpose Register 4
#define R5_ADDR 0x00000014  // General Purpose Register 5
#define R6_ADDR 0x00000018  // General Purpose Register 6
#define R7_ADDR 0x0000001C  // General Purpose Register 7
#define R8_ADDR 0x00000020  // General Purpose Register 8
#define R9_ADDR 0x00000024  // General Purpose Register 9
#define R10_ADDR 0x00000028  // General Purpose Register 10
#define R11_ADDR 0x0000002C  // General Purpose Register 11
#define R12_ADDR 0x00000030  // General Purpose Register 12
#define SP_ADDR 0x00000034  // Stack Pointer
#define LR_ADDR 0x00000038  // Link Register
#define PC_ADDR 0x0000003C  // Program Counter
#define PSR_ADDR 0x00000040  // Program Status Register
#define PSR_N_BIT 31  // Negative Flag
#define PSR_Z_BIT 30  // Zero Flag
#define PSR_C_BIT 29  // Carry Flag
#define PSR_V_BIT 28  // Overflow Flag
#define PSR_Q_BIT 27  // SAT Flag
#define PSR_ICI_BIT 0  // ICI/IT
#define PSR_GE_BIT 0  // Greater than or Equal
#define PSR_IT_BIT 0  // IT status
#define PSR_Q_BIT 9  // APSR
#define PSR_IPSR_BIT 0  // IPSR
#define PRIMASK_ADDR 0xE0000E20  // Priority Mask Register
#define BASEPRI_ADDR 0xE0000E24  // Base Priority Register
#define FAULTMASK_ADDR 0xE0000E28  // Fault Mask Register
#define CONTROL_ADDR 0xE0000E2C  // Control Register
#define FPSCR_ADDR E0000EF34  // FPU Status Control
#define S0_ADDR 0xE0000EF00  // FP Register 0
#define S1_ADDR 0xE0000EF04  // FP Register 1
#define S2_ADDR 0xE0000EF08  // FP Register 2
#define S3_ADDR 0xE0000EF0C  // FP Register 3
#define S4_ADDR 0xE0000EF10  // FP Register 4
#define S5_ADDR 0xE0000EF14  // FP Register 5
#define S6_ADDR 0xE0000EF18  // FP Register 6
#define S7_ADDR 0xE0000EF1C  // FP Register 7
#define S8_ADDR 0xE0000EF20  // FP Register 8
#define S9_ADDR 0xE0000EF24  // FP Register 9
#define S10_ADDR 0xE0000EF28  // FP Register 10
#define S11_ADDR 0xE0000EF2C  // FP Register 11
#define S12_ADDR 0xE0000EF30  // FP Register 12
#define S13_ADDR 0xE0000EF34  // FP Register 13
#define S14_ADDR 0xE0000EF38  // FP Register 14
#define S15_ADDR 0xE0000EF3C  // FP Register 15
#define S16_ADDR 0xE0000EF40  // FP Register 16
#define S17_ADDR 0xE0000EF44  // FP Register 17
#define S18_ADDR 0xE0000EF48  // FP Register 18
#define S19_ADDR 0xE0000EF4C  // FP Register 19
#define S20_ADDR 0xE0000EF50  // FP Register 20
#define S21_ADDR 0xE0000EF54  // FP Register 21
#define S22_ADDR 0xE0000EF58  // FP Register 22
#define S23_ADDR 0xE0000EF5C  // FP Register 23
#define S24_ADDR 0xE0000EF60  // FP Register 24
#define S25_ADDR 0xE0000EF64  // FP Register 25
#define S26_ADDR 0xE0000EF68  // FP Register 26
#define S27_ADDR 0xE0000EF6C  // FP Register 27
#define S28_ADDR 0xE0000EF70  // FP Register 28
#define S29_ADDR 0xE0000EF74  // FP Register 29
#define S30_ADDR 0xE0000EF78  // FP Register 30
#define S31_ADDR 0xE0000EF7C  // FP Register 31

// 内存段定义
#define FLASH_START 0x08000000
#define FLASH_END 0x080FFFFF
#define FLASH_SIZE 1048576  // Main Flash (1MB)
#define SYSTEM_START 0x1FFF0000
#define SYSTEM_END 0x1FFF7FFF
#define SYSTEM_SIZE 32768  // System Flash (32KB)
#define OPTION_START 0x1FFF8000
#define OPTION_END 0x1FFFC000
#define OPTION_SIZE 16384  // Option Bytes (16KB)
#define SRAM1_START 0x20000000
#define SRAM1_END 0x2001FFFF
#define SRAM1_SIZE 131072  // SRAM1 (128KB)
#define SRAM2_START 0x20020000
#define SRAM2_END 0x2002FFFF
#define SRAM2_SIZE 65536  // SRAM2 (64KB)
#define CCM_START 0x20030000
#define CCM_END 0x2003FFFF
#define CCM_SIZE 65536  // CCM RAM (64KB)
#define PERIPHERAL_START 0x40000000
#define PERIPHERAL_END 0x40023FFF
#define PERIPHERAL_SIZE 147456  // Peripheral Registers
#define FMC_START 0x40020000
#define FMC_END 0x40023FFF
#define FMC_SIZE 16384  // FMC Registers
#define FSMC_START 0x40000000
#define FSMC_END 0x400003FF
#define FSMC_SIZE 1024  // FSMC Registers
#define GPIOA_START 0x40020000
#define GPIOA_END 0x400203FF
#define GPIOA_SIZE 1024  // GPIOA Registers

// 外设定义
// Reset and Clock Control
#define RCC_BASE 0x40023800
#define RCC_CR_ADDR 0x0000
#define RCC_PLLCFGR_ADDR 0x0004
#define RCC_CFGR_ADDR 0x0008
#define RCC_CIR_ADDR 0x000C
#define RCC_APB2RSTR_ADDR 0x0010
#define RCC_APB1RSTR_ADDR 0x0014
#define RCC_AHB1ENR_ADDR 0x0030
#define RCC_AHB2ENR_ADDR 0x0034
#define RCC_AHB3ENR_ADDR 0x0038
#define RCC_APB2ENR_ADDR 0x0040
#define RCC_APB1ENR_ADDR 0x0044
#define RCC_BDCR_ADDR 0x0050
#define RCC_CSR_ADDR 0x0054
#define RCC_AHB1RSTR_ADDR 0x0020
#define RCC_AHB2RSTR_ADDR 0x0024
#define RCC_AHB3RSTR_ADDR 0x0028
#define RCC_CFGR2_ADDR 0x0018
#define RCC_CFGR3_ADDR 0x001C
#define RCC_PLL2CFGR_ADDR 0x0060
#define RCC_PLL2DIV_ADDR 0x0064
#define RCC_PLL3CFGR_ADDR 0x0068
#define RCC_PLL3DIV_ADDR 0x006C
// Flash Interface
#define FLASH_BASE 0x40023C00
#define FLASH_ACR_ADDR 0x0000
#define FLASH_KEYR_ADDR 0x0004
#define FLASH_OPTKEYR_ADDR 0x0008
#define FLASH_SR_ADDR 0x000C
#define FLASH_CR_ADDR 0x0010
#define FLASH_OPTCR_ADDR 0x0014
#define FLASH_OPTCR1_ADDR 0x0018
// Power Control
#define PWR_BASE 0x40007000
#define PWR_CR_ADDR 0x0000
#define PWR_CSR_ADDR 0x0004
// DMA1 Controller
#define DMA1_BASE 0x40026000
#define DMA1_LIFCR_ADDR 0x0000
#define DMA1_HIFCR_ADDR 0x0004
#define DMA1_LISR_ADDR 0x0008
#define DMA1_HISR_ADDR 0x000C
#define DMA1_S0CR_ADDR 0x0010
#define DMA1_S0NDTR_ADDR 0x0014
#define DMA1_S0PAR_ADDR 0x0018
#define DMA1_S0M0AR_ADDR 0x001C
#define DMA1_S0M1AR_ADDR 0x0020
#define DMA1_S0FCR_ADDR 0x0024
#define DMA1_S1CR_ADDR 0x0028
#define DMA1_S1NDTR_ADDR 0x002C
#define DMA1_S2CR_ADDR 0x0040
#define DMA1_S3CR_ADDR 0x0058
#define DMA1_S4CR_ADDR 0x0070
#define DMA1_S5CR_ADDR 0x0088
#define DMA1_S6CR_ADDR 0x00A0
#define DMA1_S7CR_ADDR 0x00B8
// DMA2 Controller
#define DMA2_BASE 0x40026400
#define DMA2_LIFCR_ADDR 0x0000
#define DMA2_HIFCR_ADDR 0x0004
#define DMA2_LISR_ADDR 0x0008
#define DMA2_HISR_ADDR 0x000C
#define DMA2_S0CR_ADDR 0x0010
#define DMA2_S1CR_ADDR 0x0028
#define DMA2_S2CR_ADDR 0x0040
#define DMA2_S3CR_ADDR 0x0058
#define DMA2_S4CR_ADDR 0x0070
#define DMA2_S5CR_ADDR 0x0088
#define DMA2_S6CR_ADDR 0x00A0
#define DMA2_S7CR_ADDR 0x00B8
// USART1
#define USART1_BASE 0x40011000
#define USART1_SR_ADDR 0x0000
#define USART1_DR_ADDR 0x0004
#define USART1_BRR_ADDR 0x0008
#define USART1_CR1_ADDR 0x000C
#define USART1_CR2_ADDR 0x0010
#define USART1_CR3_ADDR 0x0014
#define USART1_GTPR_ADDR 0x0018
// USART2
#define USART2_BASE 0x40004400
#define USART2_SR_ADDR 0x0000
#define USART2_DR_ADDR 0x0004
#define USART2_BRR_ADDR 0x0008
#define USART2_CR1_ADDR 0x000C
#define USART2_CR2_ADDR 0x0010
#define USART2_CR3_ADDR 0x0014
// USART3
#define USART3_BASE 0x40004800
#define USART3_SR_ADDR 0x0000
#define USART3_DR_ADDR 0x0004
#define USART3_BRR_ADDR 0x0008
#define USART3_CR1_ADDR 0x000C
#define USART3_CR2_ADDR 0x0010
#define USART3_CR3_ADDR 0x0014
// SPI1
#define SPI1_BASE 0x40013000
#define SPI1_CR1_ADDR 0x0000
#define SPI1_CR2_ADDR 0x0004
#define SPI1_SR_ADDR 0x0008
#define SPI1_DR_ADDR 0x000C
#define SPI1_CRCPR_ADDR 0x0010
#define SPI1_RXCRCR_ADDR 0x0014
#define SPI1_TXCRCR_ADDR 0x0018
#define SPI1_I2SCFGR_ADDR 0x001C
// SPI2
#define SPI2_BASE 0x40003800
#define SPI2_CR1_ADDR 0x0000
#define SPI2_CR2_ADDR 0x0004
#define SPI2_SR_ADDR 0x0008
#define SPI2_DR_ADDR 0x000C
#define SPI2_CRCPR_ADDR 0x0010
#define SPI2_RXCRCR_ADDR 0x0014
// SPI3
#define SPI3_BASE 0x40003C00
#define SPI3_CR1_ADDR 0x0000
#define SPI3_CR2_ADDR 0x0004
#define SPI3_SR_ADDR 0x0008
#define SPI3_DR_ADDR 0x000C
// I2C1
#define I2C1_BASE 0x40005400
#define I2C1_CR1_ADDR 0x0000
#define I2C1_CR2_ADDR 0x0004
#define I2C1_OAR1_ADDR 0x0008
#define I2C1_OAR2_ADDR 0x000C
#define I2C1_DR_ADDR 0x0010
#define I2C1_SR1_ADDR 0x0014
#define I2C1_SR2_ADDR 0x0018
#define I2C1_CCR_ADDR 0x001C
#define I2C1_TRISE_ADDR 0x0020
#define I2C1_FLTR_ADDR 0x0024
// I2C2
#define I2C2_BASE 0x40005800
#define I2C2_CR1_ADDR 0x0000
#define I2C2_CR2_ADDR 0x0004
#define I2C2_OAR1_ADDR 0x0008
#define I2C2_OAR2_ADDR 0x000C
#define I2C2_DR_ADDR 0x0010
#define I2C2_SR1_ADDR 0x0014
#define I2C2_SR2_ADDR 0x0018
#define I2C2_CCR_ADDR 0x001C
#define I2C2_TRISE_ADDR 0x0020
// I2C3
#define I2C3_BASE 0x40005C00
#define I2C3_CR1_ADDR 0x0000
#define I2C3_CR2_ADDR 0x0004
#define I2C3_OAR1_ADDR 0x0008
#define I2C3_DR_ADDR 0x0010
#define I2C3_SR1_ADDR 0x0014
#define I2C3_SR2_ADDR 0x0018
#define I2C3_CCR_ADDR 0x001C
// Advanced Timer 1
#define TIM1_BASE 0x40012C00
#define TIM1_CR1_ADDR 0x0000
#define TIM1_CR2_ADDR 0x0004
#define TIM1_SMCR_ADDR 0x0008
#define TIM1_DIER_ADDR 0x000C
#define TIM1_SR_ADDR 0x0010
#define TIM1_EGR_ADDR 0x0014
#define TIM1_CCMR1_ADDR 0x0018
#define TIM1_CCMR2_ADDR 0x001C
#define TIM1_CCER_ADDR 0x0020
#define TIM1_CNT_ADDR 0x0024
#define TIM1_PSC_ADDR 0x0028
#define TIM1_ARR_ADDR 0x002C
#define TIM1_RCR_ADDR 0x0030
#define TIM1_CCR1_ADDR 0x0034
#define TIM1_CCR2_ADDR 0x0038
#define TIM1_CCR3_ADDR 0x003C
#define TIM1_CCR4_ADDR 0x0040
#define TIM1_BDTR_ADDR 0x0044
#define TIM1_DCR_ADDR 0x0048
#define TIM1_DMAR_ADDR 0x004C
// General Purpose Timer 2
#define TIM2_BASE 0x40000000
#define TIM2_CR1_ADDR 0x0000
#define TIM2_CR2_ADDR 0x0004
#define TIM2_SMCR_ADDR 0x0008
#define TIM2_DIER_ADDR 0x000C
#define TIM2_SR_ADDR 0x0010
#define TIM2_EGR_ADDR 0x0014
#define TIM2_CCMR1_ADDR 0x0018
#define TIM2_CCMR2_ADDR 0x001C
#define TIM2_CCER_ADDR 0x0020
#define TIM2_CNT_ADDR 0x0024
#define TIM2_PSC_ADDR 0x0028
#define TIM2_ARR_ADDR 0x002C
#define TIM2_CCR1_ADDR 0x0034
#define TIM2_CCR2_ADDR 0x0038
#define TIM2_CCR3_ADDR 0x003C
#define TIM2_CCR4_ADDR 0x0040
// General Purpose Timer 3
#define TIM3_BASE 0x40000400
#define TIM3_CR1_ADDR 0x0000
#define TIM3_SR_ADDR 0x0010
#define TIM3_CCMR1_ADDR 0x0018
#define TIM3_CCER_ADDR 0x0020
#define TIM3_CNT_ADDR 0x0024
#define TIM3_PSC_ADDR 0x0028
#define TIM3_ARR_ADDR 0x002C
#define TIM3_CCR1_ADDR 0x0034
#define TIM3_CCR2_ADDR 0x0038
#define TIM3_CCR3_ADDR 0x003C
#define TIM3_CCR4_ADDR 0x0040
// General Purpose Timer 4
#define TIM4_BASE 0x40000800
#define TIM4_CR1_ADDR 0x0000
#define TIM4_SR_ADDR 0x0010
#define TIM4_CCMR1_ADDR 0x0018
#define TIM4_CCER_ADDR 0x0020
#define TIM4_CNT_ADDR 0x0024
#define TIM4_PSC_ADDR 0x0028
#define TIM4_ARR_ADDR 0x002C
#define TIM4_CCR1_ADDR 0x0034
#define TIM4_CCR2_ADDR 0x0038
#define TIM4_CCR3_ADDR 0x003C
#define TIM4_CCR4_ADDR 0x0040
// General Purpose Timer 5
#define TIM5_BASE 0x40000C00
#define TIM5_CR1_ADDR 0x0000
#define TIM5_SR_ADDR 0x0010
#define TIM5_CNT_ADDR 0x0024
#define TIM5_PSC_ADDR 0x0028
#define TIM5_ARR_ADDR 0x002C
#define TIM5_CCR1_ADDR 0x0034
#define TIM5_CCR2_ADDR 0x0038
#define TIM5_CCR3_ADDR 0x003C
#define TIM5_CCR4_ADDR 0x0040
// General Purpose Timer 9
#define TIM9_BASE 0x40014C00
#define TIM9_CR1_ADDR 0x0000
#define TIM9_SMCR_ADDR 0x0008
#define TIM9_DIER_ADDR 0x000C
#define TIM9_SR_ADDR 0x0010
#define TIM9_EGR_ADDR 0x0014
#define TIM9_CCMR1_ADDR 0x0018
#define TIM9_CCER_ADDR 0x0020
#define TIM9_CNT_ADDR 0x0024
#define TIM9_PSC_ADDR 0x0028
#define TIM9_ARR_ADDR 0x002C
#define TIM9_CCR1_ADDR 0x0034
#define TIM9_CCR2_ADDR 0x0038
// General Purpose Timer 10
#define TIM10_BASE 0x40015000
#define TIM10_CR1_ADDR 0x0000
#define TIM10_DIER_ADDR 0x000C
#define TIM10_SR_ADDR 0x0010
#define TIM10_EGR_ADDR 0x0014
#define TIM10_CCMR1_ADDR 0x0018
#define TIM10_CCER_ADDR 0x0020
#define TIM10_CNT_ADDR 0x0024
#define TIM10_PSC_ADDR 0x0028
#define TIM10_ARR_ADDR 0x002C
#define TIM10_CCR1_ADDR 0x0034
// ADC1
#define ADC1_BASE 0x40012000
#define ADC1_SR_ADDR 0x0000
#define ADC1_CR1_ADDR 0x0004
#define ADC1_CR2_ADDR 0x0008
#define ADC1_SMPR1_ADDR 0x000C
#define ADC1_SMPR2_ADDR 0x0010
#define ADC1_JOFR1_ADDR 0x0014
#define ADC1_JOFR2_ADDR 0x0018
#define ADC1_JOFR3_ADDR 0x001C
#define ADC1_JOFR4_ADDR 0x0020
#define ADC1_HTR_ADDR 0x0024
#define ADC1_LTR_ADDR 0x0028
#define ADC1_SQR1_ADDR 0x002C
#define ADC1_SQR2_ADDR 0x0030
#define ADC1_SQR3_ADDR 0x0034
#define ADC1_JSQR_ADDR 0x003C
#define ADC1_JDR1_ADDR 0x0040
#define ADC1_JDR2_ADDR 0x0044
#define ADC1_JDR3_ADDR 0x0048
#define ADC1_JDR4_ADDR 0x004C
#define ADC1_DR_ADDR 0x0050
// ADC2
#define ADC2_BASE 0x40012100
#define ADC2_SR_ADDR 0x0000
#define ADC2_CR1_ADDR 0x0004
#define ADC2_CR2_ADDR 0x0008
#define ADC2_SMPR1_ADDR 0x000C
#define ADC2_SMPR2_ADDR 0x0010
#define ADC2_SQR1_ADDR 0x002C
#define ADC2_DR_ADDR 0x0050
// ADC3
#define ADC3_BASE 0x40012200
#define ADC3_SR_ADDR 0x0000
#define ADC3_CR1_ADDR 0x0004
#define ADC3_CR2_ADDR 0x0008
#define ADC3_SMPR1_ADDR 0x000C
#define ADC3_SMPR2_ADDR 0x0010
#define ADC3_SQR1_ADDR 0x002C
#define ADC3_DR_ADDR 0x0050
// System Configuration Controller
#define SYSCFG_BASE 0x40013800
#define SYSCFG_CFGR_ADDR 0x0000
#define SYSCFG_EXTICR1_ADDR 0x0008
#define SYSCFG_EXTICR2_ADDR 0x000C
#define SYSCFG_EXTICR3_ADDR 0x0010
#define SYSCFG_EXTICR4_ADDR 0x0014
#define SYSCFG_CBR_ADDR 0x001C
// External Interrupt/Event Controller
#define EXTI_BASE 0x40013C00
#define EXTI_IMR_ADDR 0x0000
#define EXTI_EMR_ADDR 0x0004
#define EXTI_RTSR_ADDR 0x0008
#define EXTI_FTSR_ADDR 0x000C
#define EXTI_SWIER_ADDR 0x0010
#define EXTI_PR_ADDR 0x0014
// Random Number Generator
#define RNG_BASE 0x50060800
#define RNG_CR_ADDR 0x0000
#define RNG_SR_ADDR 0x0004
#define RNG_DR_ADDR 0x0008
// CRYP Accelerator
#define CRYP_BASE 0x50060000
#define CRYP_CR_ADDR 0x0000
#define CRYP_SR_ADDR 0x0004
#define CRYP_DIN_ADDR 0x0008
#define CRYP_DOUT_ADDR 0x000C
#define CRYP_DMACR_ADDR 0x0010
#define CRYP_IMSCR_ADDR 0x0014
#define CRYP_RISR_ADDR 0x0018
#define CRYP_MISR_ADDR 0x001C
#define CRYP_K0LR_ADDR 0x0020
#define CRYP_K0RR_ADDR 0x0024
#define CRYP_K1LR_ADDR 0x0028
#define CRYP_K1RR_ADDR 0x002C
#define CRYP_K2LR_ADDR 0x0030
#define CRYP_K2RR_ADDR 0x0034
#define CRYP_K3LR_ADDR 0x0038
#define CRYP_K3RR_ADDR 0x003C
#define CRYP_IV0LR_ADDR 0x0040
#define CRYP_IV0RR_ADDR 0x0044
#define CRYP_IV1LR_ADDR 0x0048
#define CRYP_IV1RR_ADDR 0x004C
// HASH Accelerator
#define HASH_BASE 0x50060400
#define HASH_CR_ADDR 0x0000
#define HASH_DIN_ADDR 0x0004
#define HASH_DINSTAT_ADDR 0x0008
#define HASH_HR_ADDR 0x000C
#define HASH_IMR_ADDR 0x0020
#define HASH_SR_ADDR 0x0024
// Digital Camera Interface
#define DCMI_BASE 0x50050000
#define DCMI_CR_ADDR 0x0000
#define DCMI_SR_ADDR 0x0004
#define DCMI_RISR_ADDR 0x0008
#define DCMI_IER_ADDR 0x000C
#define DCMI_MISR_ADDR 0x0010
#define DCMI_ICR_ADDR 0x0014
#define DCMI_MFISH_ADDR 0x001C
#define DCMI_CWSTRT_ADDR 0x0020
#define DCMI_CWSIZE_ADDR 0x0024
#define DCMI_DR_ADDR 0x0028
#define DCMI_OR_ADDR 0x002C
// USB OTG High Speed
#define USB_OTG_HS_BASE 0x40040000
#define USB_OTG_HS_GOTGCTL_ADDR 0x0000
#define USB_OTG_HS_GOTGINT_ADDR 0x0004
#define USB_OTG_HS_GINTMSK_ADDR 0x0008
#define USB_OTG_HS_GRSTCTL_ADDR 0x000C
#define USB_OTG_HS_GINTSTS_ADDR 0x0010
#define USB_OTG_HS_GRXSTSR_ADDR 0x0014
#define USB_OTG_HS_GRXFSIZ_ADDR 0x0024
#define USB_OTG_HS_HNPTXFSIZ_ADDR 0x0028
#define USB_OTG_HS_HNPTXSTS_ADDR 0x002C
#define USB_OTG_HS_GCCFG_ADDR 0x0038
#define USB_OTG_HS_CID_ADDR 0x003C
#define USB_OTG_HS_HPTXFSIZ_ADDR 0x0100
#define USB_OTG_HS_DIEPTXF_ADDR 0x0200
// Ethernet
#define ETH_BASE 0x40028000
#define ETH_MACCR_ADDR 0x0000
#define ETH_MACFFR_ADDR 0x0004
#define ETH_MACHTHR_ADDR 0x0008
#define ETH_MACHTLR_ADDR 0x000C
#define ETH_MACMIIAR_ADDR 0x0010
#define ETH_MACMIIDR_ADDR 0x0014
#define ETH_MACCR_ADDR 0x0018
#define ETH_MACVLANTR_ADDR 0x001C
#define ETH_MACRWUFFR_ADDR 0x0028
#define ETH_MACPMTCSR_ADDR 0x002C
#define ETH_MACSR_ADDR 0x0030
#define ETH_MACIMR_ADDR 0x0034
#define ETH_MACA0HR_ADDR 0x0040
#define ETH_MACA0LR_ADDR 0x0044
#define ETH_MACA1HR_ADDR 0x0048
#define ETH_MACA1LR_ADDR 0x004C
#define ETH_MMCCR_ADDR 0x0100
#define ETH_MMCRIR_ADDR 0x0104
#define ETH_MMCTIR_ADDR 0x0108
#define ETH_MMCRIMR_ADDR 0x010C
#define ETH_MMCTIMR_ADDR 0x0110
#define ETH_MMCTGBSCCR_ADDR 0x0114
#define ETH_MMCRGUFCCR_ADDR 0x0118
#define ETH_PTPTSCR_ADDR 0x0700
#define ETH_PTPSSIR_ADDR 0x0704
#define ETH_PTPTSHR_ADDR 0x0708
#define ETH_PTPTSLR_ADDR 0x070C
#define ETH_PTPTSHUR_ADDR 0x0710
#define ETH_PTPTSLUR_ADDR 0x0714
#define ETH_PTPTSAR_ADDR 0x0718
#define ETH_PTPTTHR_ADDR 0x071C
#define ETH_PTPTTLR_ADDR 0x0720
#define ETH_PTPTSR_ADDR 0x0728
#define ETH_DMABMR_ADDR 0x1000
#define ETH_DMASR_ADDR 0x1004
#define ETH_DMAOMR_ADDR 0x1008
#define ETH_DMAIER_ADDR 0x100C
#define ETH_DMAMFBOCR_ADDR 0x1010
#define ETH_DMACHTDR_ADDR 0x1014
#define ETH_DMACHRDR_ADDR 0x1018
#define ETH_DMACHTBAR_ADDR 0x101C
#define ETH_DMACHRBAR_ADDR 0x1020

// 中断向量定义
#define INT_WWDG 0  // Window WatchDog interrupt
#define INT_PVD 1  // PVD through EXTI line detection interrupt
#define INT_TAMPER 2  // Tamper interrupt
#define INT_RTC_WKUP 3  // RTC Wakeup interrupt
#define INT_FLASH 4  // FLASH global interrupt
#define INT_RCC 5  // RCC global interrupt
#define INT_EXTI0 6  // EXTI Line0 interrupt
#define INT_EXTI1 7  // EXTI Line1 interrupt
#define INT_EXTI2 8  // EXTI Line2 interrupt
#define INT_EXTI3 9  // EXTI Line3 interrupt
#define INT_EXTI4 10  // EXTI Line4 interrupt
#define INT_DMA1_STREAM0 11  // DMA1 Stream0 global interrupt
#define INT_DMA1_STREAM1 12  // DMA1 Stream1 global interrupt
#define INT_DMA1_STREAM2 13  // DMA1 Stream2 global interrupt
#define INT_DMA1_STREAM3 14  // DMA1 Stream3 global interrupt
#define INT_DMA1_STREAM4 15  // DMA1 Stream4 global interrupt
#define INT_DMA1_STREAM5 16  // DMA1 Stream5 global interrupt
#define INT_DMA1_STREAM6 17  // DMA1 Stream6 global interrupt
#define INT_ADC 18  // ADC1 global interrupt
#define INT_CAN1_TX 19  // CAN1 TX interrupts
#define INT_CAN1_RX0 20  // CAN1 RX0 interrupts
#define INT_CAN1_RX1 21  // CAN1 RX1 interrupt
#define INT_CAN1_SCE 22  // CAN1 SCE interrupt
#define INT_EXTI9_5 23  // EXTI Line[9:5] interrupt
#define INT_TIM1_BRK_TIM9 24  // TIM1 Break interrupt and TIM9 global interrupt
#define INT_TIM1_UP_TIM10 25  // TIM1 Update interrupt and TIM10 global interrupt
#define INT_TIM1_TRG_COM_TIM11 26  // TIM1 Trigger and commutation interrupt and TIM11 global interrupt
#define INT_TIM1_CC 27  // TIM1 Capture Compare interrupt
#define INT_TIM2 28  // TIM2 global interrupt
#define INT_TIM3 29  // TIM3 global interrupt
#define INT_TIM4 30  // TIM4 global interrupt
#define INT_I2C1_EV 31  // I2C1 Event interrupt
#define INT_I2C1_ER 32  // I2C1 Error interrupt
#define INT_I2C2_EV 33  // I2C2 Event interrupt
#define INT_I2C2_ER 34  // I2C2 Error interrupt
#define INT_SPI1 35  // SPI1 global interrupt
#define INT_SPI2 36  // SPI2 global interrupt
#define INT_USART1 37  // USART1 global interrupt
#define INT_USART2 38  // USART2 global interrupt
#define INT_USART3 39  // USART3 global interrupt
#define INT_EXTI15_10 40  // EXTI Line[15:10] interrupts
#define INT_RTC_ALARM 41  // RTC Alarm (A and B) through EXTI Line interrupt
#define INT_OTG_FS_WKUP 42  // USB OTG FS Wakeup through EXTI interrupt
#define INT_TIM8_BRK_TIM12 43  // TIM8 Break interrupt and TIM12 global interrupt
#define INT_TIM8_UP_TIM13 44  // TIM8 Update interrupt and TIM13 global interrupt
#define INT_TIM8_TRG_COM_TIM14 45  // TIM8 Trigger and commutation interrupt and TIM14 global interrupt
#define INT_TIM8_CC 46  // TIM8 Capture Compare interrupt
#define INT_SPI3 47  // SPI3 global interrupt
#define INT_UART4 48  // UART4 global interrupt
#define INT_UART5 49  // UART5 global interrupt
#define INT_TIM6 50  // TIM6 global interrupt
#define INT_TIM7 51  // TIM7 global interrupt
#define INT_DMA2_STREAM0 52  // DMA2 Stream0 global interrupt
#define INT_DMA2_STREAM1 53  // DMA2 Stream1 global interrupt
#define INT_DMA2_STREAM2 54  // DMA2 Stream2 global interrupt
#define INT_DMA2_STREAM3 55  // DMA2 Stream3 global interrupt
#define INT_DMA2_STREAM4 56  // DMA2 Stream4 global interrupt
#define INT_ETH 57  // Ethernet global interrupt
#define INT_ETH_WKUP 58  // Ethernet Wakeup through EXTI interrupt
#define INT_CAN2_TX 59  // CAN2 TX interrupts
#define INT_CAN2_RX0 60  // CAN2 RX0 interrupts
#define INT_CAN2_RX1 61  // CAN2 RX1 interrupt
#define INT_CAN2_SCE 62  // CAN2 SCE interrupt
#define INT_NA 63  // Not Available
#define INT_OTG_FS 64  // USB OTG FS global interrupt
#define INT_DCMI 65  // DCMI global interrupt
#define INT_CRYP 66  // CRYP global interrupt
#define INT_HASH_RNG 67  // HASH and RNG global interrupt
#define INT_FPU 68  // FPU global interrupt

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

#endif /* STM32F407VGT6_DEVICE_H */
