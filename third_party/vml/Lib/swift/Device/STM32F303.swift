//
// STM32F303CCT6 Register Definitions
// Generated from: 32-bit ARM Cortex-M4F MCU with 256KB Flash, 48KB SRAM, 72MHz, FPU+DSP
// Version: 1.0
// Date: 2026-04-29
//

import Foundation

// MARK: - USART1 (USART 1)
let USART1_SR: UInt32 = 0x0x00
let USART1_DR: UInt32 = 0x0x04
let USART1_BRR: UInt32 = 0x0x08
let USART1_CR1: UInt32 = 0x0x0C
let USART1_CR2: UInt32 = 0x0x10
let USART1_CR3: UInt32 = 0x0x14

// MARK: - USART2 (USART 2)
let USART2_SR: UInt32 = 0x0x00
let USART2_DR: UInt32 = 0x0x04
let USART2_BRR: UInt32 = 0x0x08
let USART2_CR1: UInt32 = 0x0x0C

// MARK: - USART3 (USART 3)
let USART3_SR: UInt32 = 0x0x00
let USART3_DR: UInt32 = 0x0x04
let USART3_BRR: UInt32 = 0x0x08
let USART3_CR1: UInt32 = 0x0x0C

// MARK: - GPIOA (GPIO Port A)
let GPIOA_MODER: UInt32 = 0x0x00
let GPIOA_OTYPER: UInt32 = 0x0x04
let GPIOA_OSPEEDR: UInt32 = 0x0x08
let GPIOA_PUPDR: UInt32 = 0x0x0C
let GPIOA_IDR: UInt32 = 0x0x10
let GPIOA_ODR: UInt32 = 0x0x14
let GPIOA_BSRR: UInt32 = 0x0x18
let GPIOA_AFRL: UInt32 = 0x0x20
let GPIOA_AFRH: UInt32 = 0x0x24

// MARK: - TIM1 (高级定时器 1)
let TIM1_CR1: UInt32 = 0x0x00
let TIM1_CNT: UInt32 = 0x0x24
let TIM1_PSC: UInt32 = 0x0x28
let TIM1_ARR: UInt32 = 0x0x2C
let TIM1_CCR1: UInt32 = 0x0x34

// MARK: - ADC1 (ADC 1)
let ADC1_SR: UInt32 = 0x0x00
let ADC1_CR: UInt32 = 0x0x08
let ADC1_CFGR: UInt32 = 0x0x0C
let ADC1_SMPR1: UInt32 = 0x0x14
let ADC1_DR: UInt32 = 0x0x40

// MARK: - Memory Segments

// MARK: - Device Functions
func stm32f303cct6_init() {
}

func read_register(_ addr: UInt32) -> UInt32 {
    return 0
}

func write_register(_ addr: UInt32, _ value: UInt32) {
}
