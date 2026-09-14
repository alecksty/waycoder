#ifndef STM32F103C8T6_HPP
#define STM32F103C8T6_HPP

// STM32F103C8T6寄存器定义
// 生成自: STMicroelectronics/STM32/STM32F103C8T6
// 版本: 1.0
// 日期: 2026-04-16


#ifdef __cplusplus
extern "C" {
#endif

// CPU架构: ARM-Cortex-M3
// 位宽: 32位
// 时钟频率: 72000000 Hz

// 寄存器定义
// General Purpose Register 0
#define R0 (*(volatile uint32_t*)0x00)

// General Purpose Register 1
#define R1 (*(volatile uint32_t*)0x04)

// General Purpose Register 2
#define R2 (*(volatile uint32_t*)0x08)

// General Purpose Register 3
#define R3 (*(volatile uint32_t*)0x0C)

// General Purpose Register 4
#define R4 (*(volatile uint32_t*)0x10)

// General Purpose Register 5
#define R5 (*(volatile uint32_t*)0x14)

// General Purpose Register 6
#define R6 (*(volatile uint32_t*)0x18)

// General Purpose Register 7
#define R7 (*(volatile uint32_t*)0x1C)

// General Purpose Register 8
#define R8 (*(volatile uint32_t*)0x20)

// General Purpose Register 9
#define R9 (*(volatile uint32_t*)0x24)

// General Purpose Register 10
#define R10 (*(volatile uint32_t*)0x28)

// General Purpose Register 11
#define R11 (*(volatile uint32_t*)0x2C)

// General Purpose Register 12
#define R12 (*(volatile uint32_t*)0x30)

// Stack Pointer
#define SP (*(volatile uint32_t*)0x34)

// Link Register
#define LR (*(volatile uint32_t*)0x38)

// Program Counter
#define PC (*(volatile uint32_t*)0x3C)

// Program Status Register
#define XPSR (*(volatile uint32_t*)0x40)

// 内存段定义
// Main Flash Memory (64KB)
#define FLASH_START 0x08000000
#define FLASH_END 0x0800FFFF
#define FLASH_SIZE 65536

// System Memory (2KB)
#define SYSTEM_MEMORY_START 0x1FFFF000
#define SYSTEM_MEMORY_END 0x1FFFF7FF
#define SYSTEM_MEMORY_SIZE 2048

// SRAM (20KB)
#define SRAM_START 0x20000000
#define SRAM_END 0x20004FFF
#define SRAM_SIZE 20480

// Peripheral Registers
#define PERIPHERAL_START 0x40000000
#define PERIPHERAL_END 0x40023FFF
#define PERIPHERAL_SIZE 143360

// Core Peripheral Registers
#define CORTEX_M_START 0xE0000000
#define CORTEX_M_END 0xE00FFFFF
#define CORTEX_M_SIZE 1048576

// 外设定义
// Reset and Clock Control
#define RCC_BASE 0x40021000
#define RCC_CR (*(volatile uint32_t*)0x40021000)
#define RCC_CFGR (*(volatile uint32_t*)0x40021004)
#define RCC_APB2ENR (*(volatile uint32_t*)0x40021018)
#define RCC_APB1ENR (*(volatile uint32_t*)0x4002101C)

// GPIO Port A
#define GPIOA_BASE 0x40010800
#define GPIOA_CRL (*(volatile uint32_t*)0x40010800)
#define GPIOA_CRH (*(volatile uint32_t*)0x40010804)
#define GPIOA_IDR (*(volatile uint32_t*)0x40010808)
#define GPIOA_ODR (*(volatile uint32_t*)0x4001080C)
#define GPIOA_BSRR (*(volatile uint32_t*)0x40010810)
#define GPIOA_BRR (*(volatile uint32_t*)0x40010814)
#define GPIOA_LCKR (*(volatile uint32_t*)0x40010818)

// GPIO Port B
#define GPIOB_BASE 0x40010C00
#define GPIOB_CRL (*(volatile uint32_t*)0x40010C00)
#define GPIOB_CRH (*(volatile uint32_t*)0x40010C04)
#define GPIOB_IDR (*(volatile uint32_t*)0x40010C08)
#define GPIOB_ODR (*(volatile uint32_t*)0x40010C0C)
#define GPIOB_BSRR (*(volatile uint32_t*)0x40010C10)
#define GPIOB_BRR (*(volatile uint32_t*)0x40010C14)

// GPIO Port C
#define GPIOC_BASE 0x40011000
#define GPIOC_CRL (*(volatile uint32_t*)0x40011000)
#define GPIOC_CRH (*(volatile uint32_t*)0x40011004)
#define GPIOC_IDR (*(volatile uint32_t*)0x40011008)
#define GPIOC_ODR (*(volatile uint32_t*)0x4001100C)
#define GPIOC_BSRR (*(volatile uint32_t*)0x40011010)

// USART 1
#define USART1_BASE 0x40013800
#define USART1_SR (*(volatile uint32_t*)0x40013800)
#define USART1_DR (*(volatile uint32_t*)0x40013804)
#define USART1_BRR (*(volatile uint32_t*)0x40013808)
#define USART1_CR1 (*(volatile uint32_t*)0x4001380C)
#define USART1_CR2 (*(volatile uint32_t*)0x40013810)
#define USART1_CR3 (*(volatile uint32_t*)0x40013814)
#define USART1_GTPR (*(volatile uint32_t*)0x40013818)

// USART 2
#define USART2_BASE 0x40004400
#define USART2_SR (*(volatile uint32_t*)0x40004400)
#define USART2_DR (*(volatile uint32_t*)0x40004404)
#define USART2_BRR (*(volatile uint32_t*)0x40004408)
#define USART2_CR1 (*(volatile uint32_t*)0x4000440C)

// SPI 1
#define SPI1_BASE 0x40013000
#define SPI1_CR1 (*(volatile uint32_t*)0x40013000)
#define SPI1_CR2 (*(volatile uint32_t*)0x40013004)
#define SPI1_SR (*(volatile uint32_t*)0x40013008)
#define SPI1_DR (*(volatile uint32_t*)0x4001300C)

// SPI 2
#define SPI2_BASE 0x40003800
#define SPI2_CR1 (*(volatile uint32_t*)0x40003800)
#define SPI2_CR2 (*(volatile uint32_t*)0x40003804)
#define SPI2_SR (*(volatile uint32_t*)0x40003808)
#define SPI2_DR (*(volatile uint32_t*)0x4000380C)

// I2C 1
#define I2C1_BASE 0x40005400
#define I2C1_CR1 (*(volatile uint32_t*)0x40005400)
#define I2C1_CR2 (*(volatile uint32_t*)0x40005404)
#define I2C1_SR1 (*(volatile uint32_t*)0x40005408)
#define I2C1_SR2 (*(volatile uint32_t*)0x4000540C)
#define I2C1_DR (*(volatile uint32_t*)0x40005410)
#define I2C1_CCR (*(volatile uint32_t*)0x40005414)
#define I2C1_TRISE (*(volatile uint32_t*)0x40005418)

// I2C 2
#define I2C2_BASE 0x40005800
#define I2C2_CR1 (*(volatile uint32_t*)0x40005800)
#define I2C2_CR2 (*(volatile uint32_t*)0x40005804)
#define I2C2_SR1 (*(volatile uint32_t*)0x40005808)
#define I2C2_SR2 (*(volatile uint32_t*)0x4000580C)
#define I2C2_DR (*(volatile uint32_t*)0x40005810)
#define I2C2_CCR (*(volatile uint32_t*)0x40005814)

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

// General Purpose Timer 2
#define TIM2_BASE 0x40000400
#define TIM2_CR1 (*(volatile uint32_t*)0x40000400)
#define TIM2_CNT (*(volatile uint32_t*)0x40000424)
#define TIM2_PSC (*(volatile uint32_t*)0x40000428)
#define TIM2_ARR (*(volatile uint32_t*)0x4000042C)
#define TIM2_CCR1 (*(volatile uint32_t*)0x40000434)
#define TIM2_CCR2 (*(volatile uint32_t*)0x40000438)
#define TIM2_CCR3 (*(volatile uint32_t*)0x4000043C)
#define TIM2_CCR4 (*(volatile uint32_t*)0x40000440)

// General Purpose Timer 3
#define TIM3_BASE 0x40000400
#define TIM3_CR1 (*(volatile uint32_t*)0x40000400)
#define TIM3_CNT (*(volatile uint32_t*)0x40000424)
#define TIM3_ARR (*(volatile uint32_t*)0x4000042C)
#define TIM3_CCR1 (*(volatile uint32_t*)0x40000434)
#define TIM3_CCR2 (*(volatile uint32_t*)0x40000438)
#define TIM3_CCR3 (*(volatile uint32_t*)0x4000043C)
#define TIM3_CCR4 (*(volatile uint32_t*)0x40000440)

// General Purpose Timer 4
#define TIM4_BASE 0x40000800
#define TIM4_CR1 (*(volatile uint32_t*)0x40000800)
#define TIM4_CNT (*(volatile uint32_t*)0x40000824)
#define TIM4_ARR (*(volatile uint32_t*)0x4000082C)
#define TIM4_CCR1 (*(volatile uint32_t*)0x40000834)
#define TIM4_CCR2 (*(volatile uint32_t*)0x40000838)
#define TIM4_CCR3 (*(volatile uint32_t*)0x4000083C)
#define TIM4_CCR4 (*(volatile uint32_t*)0x40000840)

// ADC 1
#define ADC1_BASE 0x40012400
#define ADC1_SR (*(volatile uint32_t*)0x40012400)
#define ADC1_CR1 (*(volatile uint32_t*)0x40012404)
#define ADC1_CR2 (*(volatile uint32_t*)0x40012408)
#define ADC1_SMPR1 (*(volatile uint32_t*)0x4001240C)
#define ADC1_SMPR2 (*(volatile uint32_t*)0x40012410)
#define ADC1_JOFR1 (*(volatile uint32_t*)0x40012414)
#define ADC1_JOFR2 (*(volatile uint32_t*)0x40012418)
#define ADC1_JOFR3 (*(volatile uint32_t*)0x4001241C)
#define ADC1_JOFR4 (*(volatile uint32_t*)0x40012420)
#define ADC1_HTR (*(volatile uint32_t*)0x40012424)
#define ADC1_LTR (*(volatile uint32_t*)0x40012428)
#define ADC1_SQRT1 (*(volatile uint32_t*)0x4001242C)
#define ADC1_SQRT2 (*(volatile uint32_t*)0x40012430)
#define ADC1_SQRT3 (*(volatile uint32_t*)0x40012434)
#define ADC1_JSQR (*(volatile uint32_t*)0x40012438)
#define ADC1_JDR1 (*(volatile uint32_t*)0x4001243C)
#define ADC1_JDR2 (*(volatile uint32_t*)0x40012440)
#define ADC1_JDR3 (*(volatile uint32_t*)0x40012444)
#define ADC1_JDR4 (*(volatile uint32_t*)0x40012448)
#define ADC1_DR (*(volatile uint32_t*)0x4001244C)

// DMA Controller 1
#define DMA1_BASE 0x40020000
#define DMA1_ISR (*(volatile uint32_t*)0x40020000)
#define DMA1_IFCR (*(volatile uint32_t*)0x40020004)
#define DMA1_CCR1 (*(volatile uint32_t*)0x40020008)
#define DMA1_CNDTR1 (*(volatile uint32_t*)0x4002000C)
#define DMA1_CPAR1 (*(volatile uint32_t*)0x40020010)
#define DMA1_CMAR1 (*(volatile uint32_t*)0x40020014)
#define DMA1_CCR2 (*(volatile uint32_t*)0x4002001C)
#define DMA1_CNDTR2 (*(volatile uint32_t*)0x40020020)
#define DMA1_CPAR2 (*(volatile uint32_t*)0x40020024)
#define DMA1_CMAR2 (*(volatile uint32_t*)0x40020028)

// Power Control
#define PWR_BASE 0x40007000
#define PWR_CR (*(volatile uint32_t*)0x40007000)
#define PWR_CSR (*(volatile uint32_t*)0x40007004)

// Backup Registers
#define BKP_BASE 0x40006C00
#define BKP_DR1 (*(volatile uint32_t*)0x40006C04)
#define BKP_DR2 (*(volatile uint32_t*)0x40006C08)
#define BKP_CSR (*(volatile uint32_t*)0x40006C2C)

// Window Watchdog
#define WWDG_BASE 0x40002C00
#define WWDG_CR (*(volatile uint32_t*)0x40002C00)
#define WWDG_CFR (*(volatile uint32_t*)0x40002C04)
#define WWDG_SR (*(volatile uint32_t*)0x40002C08)

// Independent Watchdog
#define IWDG_BASE 0x40003000
#define IWDG_KR (*(volatile uint32_t*)0x40003000)
#define IWDG_PR (*(volatile uint32_t*)0x40003004)
#define IWDG_RLR (*(volatile uint32_t*)0x40003008)

// External Interrupt/Event Controller
#define EXTI_BASE 0x40010400
#define EXTI_IMR (*(volatile uint32_t*)0x40010400)
#define EXTI_EMR (*(volatile uint32_t*)0x40010404)
#define EXTI_RTSR (*(volatile uint32_t*)0x40010408)
#define EXTI_FTSR (*(volatile uint32_t*)0x4001040C)
#define EXTI_SWIER (*(volatile uint32_t*)0x40010410)
#define EXTI_PR (*(volatile uint32_t*)0x40010414)

// Alternate Function IO
#define AFIO_BASE 0x40010000
#define AFIO_EVCR (*(volatile uint32_t*)0x40010000)
#define AFIO_MAPR (*(volatile uint32_t*)0x40010004)
#define AFIO_EXTICR1 (*(volatile uint32_t*)0x40010008)
#define AFIO_EXTICR2 (*(volatile uint32_t*)0x4001000C)
#define AFIO_EXTICR3 (*(volatile uint32_t*)0x40010010)
#define AFIO_MAPR2 (*(volatile uint32_t*)0x4001001C)

// 中断向量定义
#define WWDG_VECTOR 0  // Window Watchdog Interrupt
#define PVD_VECTOR 1  // PVD through EXTI Line detection
#define TAMPER_VECTOR 2  // Tamper Interrupt
#define RTC_VECTOR 3  // RTC Global Interrupt
#define FLASH_VECTOR 4  // FLASH Global Interrupt
#define RCC_VECTOR 5  // RCC Global Interrupt
#define EXTI0_VECTOR 6  // EXTI Line 0 Interrupt
#define EXTI1_VECTOR 7  // EXTI Line 1 Interrupt
#define EXTI2_VECTOR 8  // EXTI Line 2 Interrupt
#define EXTI3_VECTOR 9  // EXTI Line 3 Interrupt
#define EXTI4_VECTOR 10  // EXTI Line 4 Interrupt
#define DMA1_CHANNEL1_VECTOR 11  // DMA1 Channel 1 Interrupt
#define DMA1_CHANNEL2_VECTOR 12  // DMA1 Channel 2 Interrupt
#define DMA1_CHANNEL3_VECTOR 13  // DMA1 Channel 3 Interrupt
#define DMA1_CHANNEL4_VECTOR 14  // DMA1 Channel 4 Interrupt
#define DMA1_CHANNEL5_VECTOR 15  // DMA1 Channel 5 Interrupt
#define DMA1_CHANNEL6_VECTOR 16  // DMA1 Channel 6 Interrupt
#define DMA1_CHANNEL7_VECTOR 17  // DMA1 Channel 7 Interrupt
#define ADC1_2_VECTOR 18  // ADC1 and ADC2 Global Interrupt
#define USB_HP_CAN_TX_VECTOR 19  // USB HP/CAN TX Interrupts
#define USB_LP_CAN_RX0_VECTOR 20  // USB LP/CAN RX0 Interrupt
#define CAN_RX1_VECTOR 21  // CAN RX1 Interrupt
#define CAN_SCE_VECTOR 22  // CAN SCE Interrupt
#define EXTI9_5_VECTOR 23  // EXTI Line 9..5 Interrupt
#define TIM1_BRK_VECTOR 25  // TIM1 Break Interrupt
#define TIM1_UP_VECTOR 26  // TIM1 Update Interrupt
#define TIM1_TRG_COM_VECTOR 27  // TIM1 Trigger and Commutation
#define TIM1_CC_VECTOR 28  // TIM1 Capture Compare Interrupt
#define TIM2_VECTOR 29  // TIM2 Global Interrupt
#define TIM3_VECTOR 30  // TIM3 Global Interrupt
#define TIM4_VECTOR 31  // TIM4 Global Interrupt
#define I2C1_EV_VECTOR 32  // I2C1 Event Interrupt
#define I2C1_ER_VECTOR 33  // I2C1 Error Interrupt
#define I2C2_EV_VECTOR 34  // I2C2 Event Interrupt
#define I2C2_ER_VECTOR 35  // I2C2 Error Interrupt
#define SPI1_VECTOR 35  // SPI1 Global Interrupt
#define SPI2_VECTOR 36  // SPI2 Global Interrupt
#define USART1_VECTOR 37  // USART1 Global Interrupt
#define USART2_VECTOR 38  // USART2 Global Interrupt
#define USART3_VECTOR 39  // USART3 Global Interrupt
#define EXTI15_10_VECTOR 40  // EXTI Line 15..10 Interrupt
#define RTCALARM_VECTOR 41  // RTC Alarm through EXTI
#define USBWAKEUP_VECTOR 42  // USB Wakeup from suspend

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

void stm32f103c8t6_init(void);

#ifdef __cplusplus
}
#endif

#endif // STM32F103C8T6_HPP
