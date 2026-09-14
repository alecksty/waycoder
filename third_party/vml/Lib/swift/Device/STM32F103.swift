//
// STM32F103C8T6 Register Definitions
// Generated from: 32-bit ARM Cortex-M3 MCU with 64KB Flash, 20KB RAM, 72MHz
// Version: 1.0
// Date: 2026-04-16
//

import Foundation

// MARK: - RCC (Reset and Clock Control)
let RCC_CR: UInt32 = 0x0x00
let RCC_CFGR: UInt32 = 0x0x04
let RCC_APB2ENR: UInt32 = 0x0x18
let RCC_APB1ENR: UInt32 = 0x0x1C

// MARK: - GPIOA (GPIO Port A)
let GPIOA_CRL: UInt32 = 0x0x00
let GPIOA_CRH: UInt32 = 0x0x04
let GPIOA_IDR: UInt32 = 0x0x08
let GPIOA_ODR: UInt32 = 0x0x0C
let GPIOA_BSRR: UInt32 = 0x0x10
let GPIOA_BRR: UInt32 = 0x0x14
let GPIOA_LCKR: UInt32 = 0x0x18

// MARK: - GPIOB (GPIO Port B)
let GPIOB_CRL: UInt32 = 0x0x00
let GPIOB_CRH: UInt32 = 0x0x04
let GPIOB_IDR: UInt32 = 0x0x08
let GPIOB_ODR: UInt32 = 0x0x0C
let GPIOB_BSRR: UInt32 = 0x0x10
let GPIOB_BRR: UInt32 = 0x0x14

// MARK: - GPIOC (GPIO Port C)
let GPIOC_CRL: UInt32 = 0x0x00
let GPIOC_CRH: UInt32 = 0x0x04
let GPIOC_IDR: UInt32 = 0x0x08
let GPIOC_ODR: UInt32 = 0x0x0C
let GPIOC_BSRR: UInt32 = 0x0x10

// MARK: - USART1 (USART 1)
let USART1_SR: UInt32 = 0x0x00
let USART1_DR: UInt32 = 0x0x04
let USART1_BRR: UInt32 = 0x0x08
let USART1_CR1: UInt32 = 0x0x0C
let USART1_CR2: UInt32 = 0x0x10
let USART1_CR3: UInt32 = 0x0x14
let USART1_GTPR: UInt32 = 0x0x18

// MARK: - USART2 (USART 2)
let USART2_SR: UInt32 = 0x0x00
let USART2_DR: UInt32 = 0x0x04
let USART2_BRR: UInt32 = 0x0x08
let USART2_CR1: UInt32 = 0x0x0C

// MARK: - SPI1 (SPI 1)
let SPI1_CR1: UInt32 = 0x0x00
let SPI1_CR2: UInt32 = 0x0x04
let SPI1_SR: UInt32 = 0x0x08
let SPI1_DR: UInt32 = 0x0x0C

// MARK: - SPI2 (SPI 2)
let SPI2_CR1: UInt32 = 0x0x00
let SPI2_CR2: UInt32 = 0x0x04
let SPI2_SR: UInt32 = 0x0x08
let SPI2_DR: UInt32 = 0x0x0C

// MARK: - I2C1 (I2C 1)
let I2C1_CR1: UInt32 = 0x0x00
let I2C1_CR2: UInt32 = 0x0x04
let I2C1_SR1: UInt32 = 0x0x08
let I2C1_SR2: UInt32 = 0x0x0C
let I2C1_DR: UInt32 = 0x0x10
let I2C1_CCR: UInt32 = 0x0x14
let I2C1_TRISE: UInt32 = 0x0x18

// MARK: - I2C2 (I2C 2)
let I2C2_CR1: UInt32 = 0x0x00
let I2C2_CR2: UInt32 = 0x0x04
let I2C2_SR1: UInt32 = 0x0x08
let I2C2_SR2: UInt32 = 0x0x0C
let I2C2_DR: UInt32 = 0x0x10
let I2C2_CCR: UInt32 = 0x0x14

// MARK: - TIM1 (Advanced Timer 1)
let TIM1_CR1: UInt32 = 0x0x00
let TIM1_CR2: UInt32 = 0x0x04
let TIM1_SMCR: UInt32 = 0x0x08
let TIM1_DIER: UInt32 = 0x0x0C
let TIM1_SR: UInt32 = 0x0x10
let TIM1_EGR: UInt32 = 0x0x14
let TIM1_CCMR1: UInt32 = 0x0x18
let TIM1_CCMR2: UInt32 = 0x0x1C
let TIM1_CCER: UInt32 = 0x0x20
let TIM1_CNT: UInt32 = 0x0x24
let TIM1_PSC: UInt32 = 0x0x28
let TIM1_ARR: UInt32 = 0x0x2C
let TIM1_RCR: UInt32 = 0x0x30
let TIM1_CCR1: UInt32 = 0x0x34
let TIM1_CCR2: UInt32 = 0x0x38
let TIM1_CCR3: UInt32 = 0x0x3C
let TIM1_CCR4: UInt32 = 0x0x40
let TIM1_BDTR: UInt32 = 0x0x44

// MARK: - TIM2 (General Purpose Timer 2)
let TIM2_CR1: UInt32 = 0x0x00
let TIM2_CNT: UInt32 = 0x0x24
let TIM2_PSC: UInt32 = 0x0x28
let TIM2_ARR: UInt32 = 0x0x2C
let TIM2_CCR1: UInt32 = 0x0x34
let TIM2_CCR2: UInt32 = 0x0x38
let TIM2_CCR3: UInt32 = 0x0x3C
let TIM2_CCR4: UInt32 = 0x0x40

// MARK: - TIM3 (General Purpose Timer 3)
let TIM3_CR1: UInt32 = 0x0x00
let TIM3_CNT: UInt32 = 0x0x24
let TIM3_ARR: UInt32 = 0x0x2C
let TIM3_CCR1: UInt32 = 0x0x34
let TIM3_CCR2: UInt32 = 0x0x38
let TIM3_CCR3: UInt32 = 0x0x3C
let TIM3_CCR4: UInt32 = 0x0x40

// MARK: - TIM4 (General Purpose Timer 4)
let TIM4_CR1: UInt32 = 0x0x00
let TIM4_CNT: UInt32 = 0x0x24
let TIM4_ARR: UInt32 = 0x0x2C
let TIM4_CCR1: UInt32 = 0x0x34
let TIM4_CCR2: UInt32 = 0x0x38
let TIM4_CCR3: UInt32 = 0x0x3C
let TIM4_CCR4: UInt32 = 0x0x40

// MARK: - ADC1 (ADC 1)
let ADC1_SR: UInt32 = 0x0x00
let ADC1_CR1: UInt32 = 0x0x04
let ADC1_CR2: UInt32 = 0x0x08
let ADC1_SMPR1: UInt32 = 0x0x0C
let ADC1_SMPR2: UInt32 = 0x0x10
let ADC1_JOFR1: UInt32 = 0x0x14
let ADC1_JOFR2: UInt32 = 0x0x18
let ADC1_JOFR3: UInt32 = 0x0x1C
let ADC1_JOFR4: UInt32 = 0x0x20
let ADC1_HTR: UInt32 = 0x0x24
let ADC1_LTR: UInt32 = 0x0x28
let ADC1_SQRT1: UInt32 = 0x0x2C
let ADC1_SQRT2: UInt32 = 0x0x30
let ADC1_SQRT3: UInt32 = 0x0x34
let ADC1_JSQR: UInt32 = 0x0x38
let ADC1_JDR1: UInt32 = 0x0x3C
let ADC1_JDR2: UInt32 = 0x0x40
let ADC1_JDR3: UInt32 = 0x0x44
let ADC1_JDR4: UInt32 = 0x0x48
let ADC1_DR: UInt32 = 0x0x4C

// MARK: - DMA1 (DMA Controller 1)
let DMA1_ISR: UInt32 = 0x0x00
let DMA1_IFCR: UInt32 = 0x0x04
let DMA1_CCR1: UInt32 = 0x0x08
let DMA1_CNDTR1: UInt32 = 0x0x0C
let DMA1_CPAR1: UInt32 = 0x0x10
let DMA1_CMAR1: UInt32 = 0x0x14
let DMA1_CCR2: UInt32 = 0x0x1C
let DMA1_CNDTR2: UInt32 = 0x0x20
let DMA1_CPAR2: UInt32 = 0x0x24
let DMA1_CMAR2: UInt32 = 0x0x28

// MARK: - PWR (Power Control)
let PWR_CR: UInt32 = 0x0x00
let PWR_CSR: UInt32 = 0x0x04

// MARK: - BKP (Backup Registers)
let BKP_DR1: UInt32 = 0x0x04
let BKP_DR2: UInt32 = 0x0x08
let BKP_CSR: UInt32 = 0x0x2C

// MARK: - WWDG (Window Watchdog)
let WWDG_CR: UInt32 = 0x0x00
let WWDG_CFR: UInt32 = 0x0x04
let WWDG_SR: UInt32 = 0x0x08

// MARK: - IWDG (Independent Watchdog)
let IWDG_KR: UInt32 = 0x0x00
let IWDG_PR: UInt32 = 0x0x04
let IWDG_RLR: UInt32 = 0x0x08

// MARK: - EXTI (External Interrupt/Event Controller)
let EXTI_IMR: UInt32 = 0x0x00
let EXTI_EMR: UInt32 = 0x0x04
let EXTI_RTSR: UInt32 = 0x0x08
let EXTI_FTSR: UInt32 = 0x0x0C
let EXTI_SWIER: UInt32 = 0x0x10
let EXTI_PR: UInt32 = 0x0x14

// MARK: - AFIO (Alternate Function IO)
let AFIO_EVCR: UInt32 = 0x0x00
let AFIO_MAPR: UInt32 = 0x0x04
let AFIO_EXTICR1: UInt32 = 0x0x08
let AFIO_EXTICR2: UInt32 = 0x0x0C
let AFIO_EXTICR3: UInt32 = 0x0x10
let AFIO_MAPR2: UInt32 = 0x0x1C

// MARK: - Interrupt Vectors
let IRQ_WWDG: Int = 0
let IRQ_PVD: Int = 1
let IRQ_TAMPER: Int = 2
let IRQ_RTC: Int = 3
let IRQ_FLASH: Int = 4
let IRQ_RCC: Int = 5
let IRQ_EXTI0: Int = 6
let IRQ_EXTI1: Int = 7
let IRQ_EXTI2: Int = 8
let IRQ_EXTI3: Int = 9
let IRQ_EXTI4: Int = 10
let IRQ_DMA1_Channel1: Int = 11
let IRQ_DMA1_Channel2: Int = 12
let IRQ_DMA1_Channel3: Int = 13
let IRQ_DMA1_Channel4: Int = 14
let IRQ_DMA1_Channel5: Int = 15
let IRQ_DMA1_Channel6: Int = 16
let IRQ_DMA1_Channel7: Int = 17
let IRQ_ADC1_2: Int = 18
let IRQ_USB_HP_CAN_TX: Int = 19
let IRQ_USB_LP_CAN_RX0: Int = 20
let IRQ_CAN_RX1: Int = 21
let IRQ_CAN_SCE: Int = 22
let IRQ_EXTI9_5: Int = 23
let IRQ_TIM1_BRK: Int = 25
let IRQ_TIM1_UP: Int = 26
let IRQ_TIM1_TRG_COM: Int = 27
let IRQ_TIM1_CC: Int = 28
let IRQ_TIM2: Int = 29
let IRQ_TIM3: Int = 30
let IRQ_TIM4: Int = 31
let IRQ_I2C1_EV: Int = 32
let IRQ_I2C1_ER: Int = 33
let IRQ_I2C2_EV: Int = 34
let IRQ_I2C2_ER: Int = 35
let IRQ_SPI1: Int = 35
let IRQ_SPI2: Int = 36
let IRQ_USART1: Int = 37
let IRQ_USART2: Int = 38
let IRQ_USART3: Int = 39
let IRQ_EXTI15_10: Int = 40
let IRQ_RTCAlarm: Int = 41
let IRQ_USBWakeup: Int = 42

// MARK: - Memory Segments
let MEM_flash: (start: UInt32, size: UInt32) = (0x0x08000000, 65536)
let MEM_system_memory: (start: UInt32, size: UInt32) = (0x0x1FFFF000, 2048)
let MEM_sram: (start: UInt32, size: UInt32) = (0x0x20000000, 20480)
let MEM_peripheral: (start: UInt32, size: UInt32) = (0x0x40000000, 143360)
let MEM_cortex_m: (start: UInt32, size: UInt32) = (0x0xE0000000, 1048576)

// MARK: - Device Functions
func stm32f103c8t6_init() {
}

func read_register(_ addr: UInt32) -> UInt32 {
    return 0
}

func write_register(_ addr: UInt32, _ value: UInt32) {
}
