// STM32F103C8T6 设备定义 - Objective-C 头文件
// 生成自: STMicroelectronics/STM32/STM32F103C8T6
// 版本: 1.0
// 日期: 2026-04-16
// 作者: VML Team
// 描述: 32-bit ARM Cortex-M3 MCU with 64KB Flash, 20KB RAM, 72MHz
// CPU架构: ARM-Cortex-M3
// 位宽: 32位
// 时钟频率: 72000000 Hz

#ifndef STM32F103C8T6_DEVICE_H
#define STM32F103C8T6_DEVICE_H

#import <Foundation/Foundation.h>

// 寄存器地址定义
#define R0_ADDR 0x00  // General Purpose Register 0
#define R1_ADDR 0x04  // General Purpose Register 1
#define R2_ADDR 0x08  // General Purpose Register 2
#define R3_ADDR 0x0C  // General Purpose Register 3
#define R4_ADDR 0x10  // General Purpose Register 4
#define R5_ADDR 0x14  // General Purpose Register 5
#define R6_ADDR 0x18  // General Purpose Register 6
#define R7_ADDR 0x1C  // General Purpose Register 7
#define R8_ADDR 0x20  // General Purpose Register 8
#define R9_ADDR 0x24  // General Purpose Register 9
#define R10_ADDR 0x28  // General Purpose Register 10
#define R11_ADDR 0x2C  // General Purpose Register 11
#define R12_ADDR 0x30  // General Purpose Register 12
#define SP_ADDR 0x34  // Stack Pointer
#define LR_ADDR 0x38  // Link Register
#define PC_ADDR 0x3C  // Program Counter
#define XPSR_ADDR 0x40  // Program Status Register

// 内存段定义
#define FLASH_START 0x08000000
#define FLASH_END 0x0800FFFF
#define FLASH_SIZE 65536  // Main Flash Memory (64KB)
#define SYSTEM_MEMORY_START 0x1FFFF000
#define SYSTEM_MEMORY_END 0x1FFFF7FF
#define SYSTEM_MEMORY_SIZE 2048  // System Memory (2KB)
#define SRAM_START 0x20000000
#define SRAM_END 0x20004FFF
#define SRAM_SIZE 20480  // SRAM (20KB)
#define PERIPHERAL_START 0x40000000
#define PERIPHERAL_END 0x40023FFF
#define PERIPHERAL_SIZE 143360  // Peripheral Registers
#define CORTEX_M_START 0xE0000000
#define CORTEX_M_END 0xE00FFFFF
#define CORTEX_M_SIZE 1048576  // Core Peripheral Registers

// 外设定义
// Reset and Clock Control
#define RCC_BASE 0x40021000
#define RCC_CR_ADDR 0x00
#define RCC_CFGR_ADDR 0x04
#define RCC_APB2ENR_ADDR 0x18
#define RCC_APB1ENR_ADDR 0x1C
// GPIO Port A
#define GPIOA_BASE 0x40010800
#define GPIOA_CRL_ADDR 0x00
#define GPIOA_CRH_ADDR 0x04
#define GPIOA_IDR_ADDR 0x08
#define GPIOA_ODR_ADDR 0x0C
#define GPIOA_BSRR_ADDR 0x10
#define GPIOA_BRR_ADDR 0x14
#define GPIOA_LCKR_ADDR 0x18
// GPIO Port B
#define GPIOB_BASE 0x40010C00
#define GPIOB_CRL_ADDR 0x00
#define GPIOB_CRH_ADDR 0x04
#define GPIOB_IDR_ADDR 0x08
#define GPIOB_ODR_ADDR 0x0C
#define GPIOB_BSRR_ADDR 0x10
#define GPIOB_BRR_ADDR 0x14
// GPIO Port C
#define GPIOC_BASE 0x40011000
#define GPIOC_CRL_ADDR 0x00
#define GPIOC_CRH_ADDR 0x04
#define GPIOC_IDR_ADDR 0x08
#define GPIOC_ODR_ADDR 0x0C
#define GPIOC_BSRR_ADDR 0x10
// USART 1
#define USART1_BASE 0x40013800
#define USART1_SR_ADDR 0x00
#define USART1_DR_ADDR 0x04
#define USART1_BRR_ADDR 0x08
#define USART1_CR1_ADDR 0x0C
#define USART1_CR2_ADDR 0x10
#define USART1_CR3_ADDR 0x14
#define USART1_GTPR_ADDR 0x18
// USART 2
#define USART2_BASE 0x40004400
#define USART2_SR_ADDR 0x00
#define USART2_DR_ADDR 0x04
#define USART2_BRR_ADDR 0x08
#define USART2_CR1_ADDR 0x0C
// SPI 1
#define SPI1_BASE 0x40013000
#define SPI1_CR1_ADDR 0x00
#define SPI1_CR2_ADDR 0x04
#define SPI1_SR_ADDR 0x08
#define SPI1_DR_ADDR 0x0C
// SPI 2
#define SPI2_BASE 0x40003800
#define SPI2_CR1_ADDR 0x00
#define SPI2_CR2_ADDR 0x04
#define SPI2_SR_ADDR 0x08
#define SPI2_DR_ADDR 0x0C
// I2C 1
#define I2C1_BASE 0x40005400
#define I2C1_CR1_ADDR 0x00
#define I2C1_CR2_ADDR 0x04
#define I2C1_SR1_ADDR 0x08
#define I2C1_SR2_ADDR 0x0C
#define I2C1_DR_ADDR 0x10
#define I2C1_CCR_ADDR 0x14
#define I2C1_TRISE_ADDR 0x18
// I2C 2
#define I2C2_BASE 0x40005800
#define I2C2_CR1_ADDR 0x00
#define I2C2_CR2_ADDR 0x04
#define I2C2_SR1_ADDR 0x08
#define I2C2_SR2_ADDR 0x0C
#define I2C2_DR_ADDR 0x10
#define I2C2_CCR_ADDR 0x14
// Advanced Timer 1
#define TIM1_BASE 0x40012C00
#define TIM1_CR1_ADDR 0x00
#define TIM1_CR2_ADDR 0x04
#define TIM1_SMCR_ADDR 0x08
#define TIM1_DIER_ADDR 0x0C
#define TIM1_SR_ADDR 0x10
#define TIM1_EGR_ADDR 0x14
#define TIM1_CCMR1_ADDR 0x18
#define TIM1_CCMR2_ADDR 0x1C
#define TIM1_CCER_ADDR 0x20
#define TIM1_CNT_ADDR 0x24
#define TIM1_PSC_ADDR 0x28
#define TIM1_ARR_ADDR 0x2C
#define TIM1_RCR_ADDR 0x30
#define TIM1_CCR1_ADDR 0x34
#define TIM1_CCR2_ADDR 0x38
#define TIM1_CCR3_ADDR 0x3C
#define TIM1_CCR4_ADDR 0x40
#define TIM1_BDTR_ADDR 0x44
// General Purpose Timer 2
#define TIM2_BASE 0x40000400
#define TIM2_CR1_ADDR 0x00
#define TIM2_CNT_ADDR 0x24
#define TIM2_PSC_ADDR 0x28
#define TIM2_ARR_ADDR 0x2C
#define TIM2_CCR1_ADDR 0x34
#define TIM2_CCR2_ADDR 0x38
#define TIM2_CCR3_ADDR 0x3C
#define TIM2_CCR4_ADDR 0x40
// General Purpose Timer 3
#define TIM3_BASE 0x40000400
#define TIM3_CR1_ADDR 0x00
#define TIM3_CNT_ADDR 0x24
#define TIM3_ARR_ADDR 0x2C
#define TIM3_CCR1_ADDR 0x34
#define TIM3_CCR2_ADDR 0x38
#define TIM3_CCR3_ADDR 0x3C
#define TIM3_CCR4_ADDR 0x40
// General Purpose Timer 4
#define TIM4_BASE 0x40000800
#define TIM4_CR1_ADDR 0x00
#define TIM4_CNT_ADDR 0x24
#define TIM4_ARR_ADDR 0x2C
#define TIM4_CCR1_ADDR 0x34
#define TIM4_CCR2_ADDR 0x38
#define TIM4_CCR3_ADDR 0x3C
#define TIM4_CCR4_ADDR 0x40
// ADC 1
#define ADC1_BASE 0x40012400
#define ADC1_SR_ADDR 0x00
#define ADC1_CR1_ADDR 0x04
#define ADC1_CR2_ADDR 0x08
#define ADC1_SMPR1_ADDR 0x0C
#define ADC1_SMPR2_ADDR 0x10
#define ADC1_JOFR1_ADDR 0x14
#define ADC1_JOFR2_ADDR 0x18
#define ADC1_JOFR3_ADDR 0x1C
#define ADC1_JOFR4_ADDR 0x20
#define ADC1_HTR_ADDR 0x24
#define ADC1_LTR_ADDR 0x28
#define ADC1_SQRT1_ADDR 0x2C
#define ADC1_SQRT2_ADDR 0x30
#define ADC1_SQRT3_ADDR 0x34
#define ADC1_JSQR_ADDR 0x38
#define ADC1_JDR1_ADDR 0x3C
#define ADC1_JDR2_ADDR 0x40
#define ADC1_JDR3_ADDR 0x44
#define ADC1_JDR4_ADDR 0x48
#define ADC1_DR_ADDR 0x4C
// DMA Controller 1
#define DMA1_BASE 0x40020000
#define DMA1_ISR_ADDR 0x00
#define DMA1_IFCR_ADDR 0x04
#define DMA1_CCR1_ADDR 0x08
#define DMA1_CNDTR1_ADDR 0x0C
#define DMA1_CPAR1_ADDR 0x10
#define DMA1_CMAR1_ADDR 0x14
#define DMA1_CCR2_ADDR 0x1C
#define DMA1_CNDTR2_ADDR 0x20
#define DMA1_CPAR2_ADDR 0x24
#define DMA1_CMAR2_ADDR 0x28
// Power Control
#define PWR_BASE 0x40007000
#define PWR_CR_ADDR 0x00
#define PWR_CSR_ADDR 0x04
// Backup Registers
#define BKP_BASE 0x40006C00
#define BKP_DR1_ADDR 0x04
#define BKP_DR2_ADDR 0x08
#define BKP_CSR_ADDR 0x2C
// Window Watchdog
#define WWDG_BASE 0x40002C00
#define WWDG_CR_ADDR 0x00
#define WWDG_CFR_ADDR 0x04
#define WWDG_SR_ADDR 0x08
// Independent Watchdog
#define IWDG_BASE 0x40003000
#define IWDG_KR_ADDR 0x00
#define IWDG_PR_ADDR 0x04
#define IWDG_RLR_ADDR 0x08
// External Interrupt/Event Controller
#define EXTI_BASE 0x40010400
#define EXTI_IMR_ADDR 0x00
#define EXTI_EMR_ADDR 0x04
#define EXTI_RTSR_ADDR 0x08
#define EXTI_FTSR_ADDR 0x0C
#define EXTI_SWIER_ADDR 0x10
#define EXTI_PR_ADDR 0x14
// Alternate Function IO
#define AFIO_BASE 0x40010000
#define AFIO_EVCR_ADDR 0x00
#define AFIO_MAPR_ADDR 0x04
#define AFIO_EXTICR1_ADDR 0x08
#define AFIO_EXTICR2_ADDR 0x0C
#define AFIO_EXTICR3_ADDR 0x10
#define AFIO_MAPR2_ADDR 0x1C

// 中断向量定义
#define INT_WWDG 0  // Window Watchdog Interrupt
#define INT_PVD 1  // PVD through EXTI Line detection
#define INT_TAMPER 2  // Tamper Interrupt
#define INT_RTC 3  // RTC Global Interrupt
#define INT_FLASH 4  // FLASH Global Interrupt
#define INT_RCC 5  // RCC Global Interrupt
#define INT_EXTI0 6  // EXTI Line 0 Interrupt
#define INT_EXTI1 7  // EXTI Line 1 Interrupt
#define INT_EXTI2 8  // EXTI Line 2 Interrupt
#define INT_EXTI3 9  // EXTI Line 3 Interrupt
#define INT_EXTI4 10  // EXTI Line 4 Interrupt
#define INT_DMA1_CHANNEL1 11  // DMA1 Channel 1 Interrupt
#define INT_DMA1_CHANNEL2 12  // DMA1 Channel 2 Interrupt
#define INT_DMA1_CHANNEL3 13  // DMA1 Channel 3 Interrupt
#define INT_DMA1_CHANNEL4 14  // DMA1 Channel 4 Interrupt
#define INT_DMA1_CHANNEL5 15  // DMA1 Channel 5 Interrupt
#define INT_DMA1_CHANNEL6 16  // DMA1 Channel 6 Interrupt
#define INT_DMA1_CHANNEL7 17  // DMA1 Channel 7 Interrupt
#define INT_ADC1_2 18  // ADC1 and ADC2 Global Interrupt
#define INT_USB_HP_CAN_TX 19  // USB HP/CAN TX Interrupts
#define INT_USB_LP_CAN_RX0 20  // USB LP/CAN RX0 Interrupt
#define INT_CAN_RX1 21  // CAN RX1 Interrupt
#define INT_CAN_SCE 22  // CAN SCE Interrupt
#define INT_EXTI9_5 23  // EXTI Line 9..5 Interrupt
#define INT_TIM1_BRK 25  // TIM1 Break Interrupt
#define INT_TIM1_UP 26  // TIM1 Update Interrupt
#define INT_TIM1_TRG_COM 27  // TIM1 Trigger and Commutation
#define INT_TIM1_CC 28  // TIM1 Capture Compare Interrupt
#define INT_TIM2 29  // TIM2 Global Interrupt
#define INT_TIM3 30  // TIM3 Global Interrupt
#define INT_TIM4 31  // TIM4 Global Interrupt
#define INT_I2C1_EV 32  // I2C1 Event Interrupt
#define INT_I2C1_ER 33  // I2C1 Error Interrupt
#define INT_I2C2_EV 34  // I2C2 Event Interrupt
#define INT_I2C2_ER 35  // I2C2 Error Interrupt
#define INT_SPI1 35  // SPI1 Global Interrupt
#define INT_SPI2 36  // SPI2 Global Interrupt
#define INT_USART1 37  // USART1 Global Interrupt
#define INT_USART2 38  // USART2 Global Interrupt
#define INT_USART3 39  // USART3 Global Interrupt
#define INT_EXTI15_10 40  // EXTI Line 15..10 Interrupt
#define INT_RTCALARM 41  // RTC Alarm through EXTI
#define INT_USBWAKEUP 42  // USB Wakeup from suspend

// 引脚定义
#define PIN_VBAT 1  // Battery Supply
#define PIN_PC13 2  // GPIO Port C Pin 13
#define PIN_PC14 3  // GPIO Port C Pin 14
#define PIN_PC15 4  // GPIO Port C Pin 15
#define PIN_PD0 5  // GPIO Port D Pin 0
#define PIN_PD1 6  // GPIO Port D Pin 1
#define PIN_NRST 7  // Reset
#define PIN_VSSA 8  // Analog Ground
#define PIN_VDDA 9  // Analog Supply
#define PIN_PA0 10  // GPIO Port A Pin 0 / ADC1_IN0
#define PIN_PA1 11  // GPIO Port A Pin 1 / ADC1_IN1
#define PIN_PA2 12  // GPIO Port A Pin 2 / ADC1_IN2 / USART2_TX
#define PIN_PA3 13  // GPIO Port A Pin 3 / ADC1_IN3 / USART2_RX
#define PIN_PA4 14  // GPIO Port A Pin 4 / DAC_OUT1 / SPI1_NSS
#define PIN_PA5 15  // GPIO Port A Pin 5 / DAC_OUT2 / SPI1_SCK
#define PIN_PA6 16  // GPIO Port A Pin 6 / ADC1_IN6 / SPI1_MISO / TIM3_CH1
#define PIN_PA7 17  // GPIO Port A Pin 7 / ADC1_IN7 / SPI1_MOSI / TIM3_CH2
#define PIN_PB0 18  // GPIO Port B Pin 0 / ADC1_IN8 / TIM3_CH3
#define PIN_PB1 19  // GPIO Port B Pin 1 / ADC1_IN9 / TIM3_CH4
#define PIN_PB2 20  // GPIO Port B Pin 2
#define PIN_PB10 21  // GPIO Port B Pin 10 / I2C2_SCL / USART3_TX
#define PIN_PB11 22  // GPIO Port B Pin 11 / I2C2_SDA / USART3_RX
#define PIN_VSS 23  // Ground
#define PIN_VDD 24  // Digital Supply
#define PIN_PB12 25  // GPIO Port B Pin 12 / SPI2_NSS / I2C2_SMBA
#define PIN_PB13 26  // GPIO Port B Pin 13 / SPI2_SCK / USART3_CK
#define PIN_PB14 27  // GPIO Port B Pin 14 / SPI2_MISO / USART3_RTS
#define PIN_PB15 28  // GPIO Port B Pin 15 / SPI2_MOSI / USART3_CTS
#define PIN_PA8 29  // GPIO Port A Pin 8 / USART1_CK / TIM1_CH1 / MCO
#define PIN_PA9 30  // GPIO Port A Pin 9 / USART1_TX / TIM1_CH2
#define PIN_PA10 31  // GPIO Port A Pin 10 / USART1_RX / TIM1_CH3
#define PIN_PA11 32  // GPIO Port A Pin 11 / USART1_CT / TIM1_CH4 / CAN_RX
#define PIN_PA12 33  // GPIO Port A Pin 12 / USART1_RT / TIM1_ETR / CAN_TX
#define PIN_PA13 34  // JTMS/SWDIO
#define PIN_PA14 37  // JTCK/SWCLK
#define PIN_PA15 38  // GPIO Port A Pin 15 / JTDI / TIM2_CH1_ETR / SPI1_NSS
#define PIN_PB3 39  // GPIO Port B Pin 3 / JTDO / TIM2_CH2 / SPI1_SCK
#define PIN_PB4 40  // GPIO Port B Pin 4 / JNTRST / TIM3_CH1 / SPI1_MISO
#define PIN_PB5 41  // GPIO Port B Pin 5 / TIM3_CH2 / SPI1_MOSI / I2C1_SMBA
#define PIN_PB6 42  // GPIO Port B Pin 6 / TIM4_CH1 / I2C1_SCL / USART1_TX
#define PIN_PB7 43  // GPIO Port B Pin 7 / TIM4_CH2 / I2C1_SDA / USART1_RX
#define PIN_BOOT0 44  // Boot Selection
#define PIN_PB8 45  // GPIO Port B Pin 8 / TIM4_CH3 / I2C1_SCL / CAN_RX
#define PIN_PB9 46  // GPIO Port B Pin 9 / TIM4_CH4 / I2C1_SDA / CAN_TX
#define PIN_VSS 47  // Ground
#define PIN_VDD 48  // Digital Supply

#endif /* STM32F103C8T6_DEVICE_H */
