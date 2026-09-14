//
// ATtiny13 Register Definitions
// Generated from: 8-bit AVR MCU with 1KB Flash, 64B RAM, 64B EEPROM, 20MHz, tiny
// Version: 1.0
// Date: 2026-04-28
//

import Foundation

// MARK: - PORTB (Port B (only port))
let PORTB_DDRB: UInt8 = 0x0x17
let PORTB_PORTB: UInt8 = 0x0x18
let PORTB_PINB: UInt8 = 0x0x19

// MARK: - TIMER0 (8-bit Timer/Counter0)
let TIMER0_TCCR0A: UInt8 = 0x0x33
let TIMER0_TCCR0B: UInt8 = 0x0x33
let TIMER0_TCNT0: UInt8 = 0x0x32
let TIMER0_OCR0A: UInt8 = 0x0x36
let TIMER0_OCR0B: UInt8 = 0x0x35
let TIMER0_TIMSK0: UInt8 = 0x0x39
let TIMER0_TIFR0: UInt8 = 0x0x38

// MARK: - ADC (Analog-to-Digital)
let ADC_ADMUX: UInt8 = 0x0x07
let ADC_ADCSRA: UInt8 = 0x0x06
let ADC_ADCL: UInt8 = 0x0x04
let ADC_ADCH: UInt8 = 0x0x05

// MARK: - Interrupt Vectors
let IRQ_Reset: Int = 1
let IRQ_INT0: Int = 2
let IRQ_PCINT0: Int = 3
let IRQ_TIM0_OVF: Int = 4
let IRQ_TIM0_COMPA: Int = 5
let IRQ_WDT: Int = 6
let IRQ_ADC: Int = 7

// MARK: - Memory Segments
let MEM_FLASH: (start: UInt32, size: UInt32) = (0x0x0000, 1024)
let MEM_SRAM: (start: UInt32, size: UInt32) = (0x0x0060, 64)
let MEM_EEPROM: (start: UInt32, size: UInt32) = (0x0x0000, 64)
let MEM_IO: (start: UInt32, size: UInt32) = (0x0x00, 32)
let MEM_EXTIO: (start: UInt32, size: UInt32) = (0x0x20, 64)

// MARK: - Device Functions
func attiny13_init() {
}

func read_register(_ addr: UInt32) -> UInt32 {
    return 0
}

func write_register(_ addr: UInt32, _ value: UInt32) {
}
