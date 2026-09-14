//
// TM4C123GH6PM Register Definitions
// Generated from: 32-bit ARM Cortex-M4F MCU with 256KB Flash, 32KB SRAM, 80MHz, USB
// Version: 1.0
// Date: 2026-04-29
//

import Foundation

// MARK: - UART0 (UART 0)
let UART0_DR: UInt32 = 0x0x000
let UART0_FR: UInt32 = 0x0x018
let UART0_IBRD: UInt32 = 0x0x024
let UART0_FBRD: UInt32 = 0x0x028
let UART0_LCRH: UInt32 = 0x0x02C
let UART0_CTL: UInt32 = 0x0x030
let UART0_IM: UInt32 = 0x0x038
let UART0_RIS: UInt32 = 0x0x03C
let UART0_ICR: UInt32 = 0x0x044

// MARK: - UART1 (UART 1)
let UART1_DR: UInt32 = 0x0x000
let UART1_FR: UInt32 = 0x0x018
let UART1_IBRD: UInt32 = 0x0x024
let UART1_FBRD: UInt32 = 0x0x028
let UART1_LCRH: UInt32 = 0x0x02C
let UART1_CTL: UInt32 = 0x0x030

// MARK: - GPIOA (GPIO Port A)
let GPIOA_DATA: UInt32 = 0x0x3FC
let GPIOA_DIR: UInt32 = 0x0x400
let GPIOA_IS: UInt32 = 0x0x404
let GPIOA_IBE: UInt32 = 0x0x408
let GPIOA_IEV: UInt32 = 0x0x40C
let GPIOA_IM: UInt32 = 0x0x410
let GPIOA_RIS: UInt32 = 0x0x414
let GPIOA_MIS: UInt32 = 0x0x418
let GPIOA_ICR: UInt32 = 0x0x41C
let GPIOA_AFSEL: UInt32 = 0x0x420
let GPIOA_DEN: UInt32 = 0x0x51C

// MARK: - TIMER0 (16/32-bit Timer 0)
let TIMER0_CFG: UInt32 = 0x0x000
let TIMER0_TAMR: UInt32 = 0x0x004
let TIMER0_CTL: UInt32 = 0x0x00C
let TIMER0_ILR: UInt32 = 0x0x028
let TIMER0_V: UInt32 = 0x0x038
let TIMER0_ICR: UInt32 = 0x0x024

// MARK: - ADC0 (ADC 0)
let ADC0_ACTSS: UInt32 = 0x0x000
let ADC0_EMUX: UInt32 = 0x0x014
let ADC0_SSMUX0: UInt32 = 0x0x040
let ADC0_SSFIFO0: UInt32 = 0x0x048
let ADC0_PROC: UInt32 = 0x0x030

// MARK: - Memory Segments

// MARK: - Device Functions
func tm4c123gh6pm_init() {
}

func read_register(_ addr: UInt32) -> UInt32 {
    return 0
}

func write_register(_ addr: UInt32, _ value: UInt32) {
}
