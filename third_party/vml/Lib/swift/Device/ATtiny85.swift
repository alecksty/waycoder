//
// ATtiny85 Register Definitions
// Generated from: 8-bit AVR microcontroller with 8KB Flash, 512B SRAM, 512B EEPROM
// Version: 1.0
// Date: 2026-04-16
//

import Foundation

// MARK: - PORTA (Port A)
let PORTA_PINA: UInt8 = 0x0x20
let PORTA_DDRA: UInt8 = 0x0x21
let PORTA_PORTA: UInt8 = 0x0x22

// MARK: - PORTB (Port B)
let PORTB_PINB: UInt8 = 0x0x16
let PORTB_DDRB: UInt8 = 0x0x17
let PORTB_PORTB: UInt8 = 0x0x18

// MARK: - TIPO (Timer/Counter0)
let TIPO_TCCR0A: UInt8 = 0x0x20
let TIPO_TCCR0B: UInt8 = 0x0x21
let TIPO_TCNT0: UInt8 = 0x0x22
let TIPO_OCR0A: UInt8 = 0x0x23
let TIPO_OCR0B: UInt8 = 0x0x24
let TIPO_TIMSK: UInt8 = 0x0x39
let TIPO_TIFR: UInt8 = 0x0x38

// MARK: - TMR1 (Timer/Counter1)
let TMR1_TCCR1A: UInt8 = 0x0x28
let TMR1_TCCR1B: UInt8 = 0x0x29
let TMR1_TCNT1: UInt16 = 0x0x2A
let TMR1_OCR1A: UInt16 = 0x0x2C
let TMR1_OCR1B: UInt16 = 0x0x2E
let TMR1_OCR1C: UInt16 = 0x0x30
let TMR1_TIMSK1: UInt8 = 0x0x33
let TMR1_TIFR1: UInt8 = 0x0x32

// MARK: - ADMUX (ADC Multiplexer)
let ADMUX_ADMUX: UInt8 = 0x0x12
let ADMUX_ADCSRA: UInt8 = 0x0x13
let ADMUX_ADCH: UInt8 = 0x0x14
let ADMUX_ADCL: UInt8 = 0x0x15

// MARK: - USI (Universal Serial Interface)
let USI_USIDR: UInt8 = 0x0x18
let USI_USISR: UInt8 = 0x0x19
let USI_USICR: UInt8 = 0x0x1A
let USI_USIPORT: UInt8 = 0x0x1B

// MARK: - MCUCR (MCU Control)
let MCUCR_MCUCR: UInt8 = 0x0x35
let MCUCR_MCUCSR: UInt8 = 0x0x36

// MARK: - WDTCR (Watchdog Timer)
let WDTCR_WDTCR: UInt8 = 0x0x21

// MARK: - EEPR (EEPROM)
let EEPR_EEAR: UInt8 = 0x0x1E
let EEPR_EEDR: UInt8 = 0x0x1D
let EEPR_EECR: UInt8 = 0x0x1F

// MARK: - GIMSK (External Interrupt)
let GIMSK_GIMSK: UInt8 = 0x0x3B
let GIMSK_GIFR: UInt8 = 0x0x3C

// MARK: - PCMSK (Pin Change Mask)
let PCMSK_PCMSK: UInt8 = 0x0x15

// MARK: - SPMCSR (Store Program Memory)
let SPMCSR_SPMCSR: UInt8 = 0x0x37

// MARK: - Interrupt Vectors
let IRQ_RESET: Int = 0
let IRQ_INT0: Int = 1
let IRQ_PCINT0: Int = 2
let IRQ_WDT: Int = 3
let IRQ_TIM1_COMPA: Int = 4
let IRQ_TIM1_OVF: Int = 5
let IRQ_TIM0_COMPA: Int = 6
let IRQ_TIM0_OVF: Int = 7
let IRQ_SPI_STC: Int = 8
let IRQ_ADC: Int = 9
let IRQ_USI_START: Int = 10
let IRQ_USI_OVF: Int = 11
let IRQ_EE_READY: Int = 12

// MARK: - Memory Segments
let MEM_flash: (start: UInt32, size: UInt32) = (0x0x0000, 8192)
let MEM_sram: (start: UInt32, size: UInt32) = (0x0x0060, 512)
let MEM_eeprom: (start: UInt32, size: UInt32) = (0x0x0000, 512)
let MEM_io: (start: UInt32, size: UInt32) = (0x0x00, 64)

// MARK: - Device Functions
func attiny85_init() {
}

func read_register(_ addr: UInt32) -> UInt32 {
    return 0
}

func write_register(_ addr: UInt32, _ value: UInt32) {
}
