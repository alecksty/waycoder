//
// ATmega328P Register Definitions
// Generated from: 8-bit AVR microcontroller with 32KB Flash, 2KB SRAM, 1KB EEPROM
// Version: 1.0
// Date: 2026-04-16
//

import Foundation

// MARK: - PORTB (Port B Data Register)
let PORTB_PORTB: UInt8 = 0x0x25
let PORTB_DDRB: UInt8 = 0x0x24
let PORTB_PINB: UInt8 = 0x0x23

// MARK: - PORTC (Port C Data Register)
let PORTC_PORTC: UInt8 = 0x0x28
let PORTC_DDRC: UInt8 = 0x0x27
let PORTC_PINC: UInt8 = 0x0x26

// MARK: - PORTD (Port D Data Register)
let PORTD_PORTD: UInt8 = 0x0x2B
let PORTD_DDRD: UInt8 = 0x0x2A
let PORTD_PIND: UInt8 = 0x0x29

// MARK: - TIMER0 (8-bit Timer/Counter0)
let TIMER0_TCCR0A: UInt8 = 0x0x44
let TIMER0_TCCR0B: UInt8 = 0x0x45
let TIMER0_TCNT0: UInt8 = 0x0x46
let TIMER0_OCR0A: UInt8 = 0x0x47
let TIMER0_OCR0B: UInt8 = 0x0x48
let TIMER0_TIMSK0: UInt8 = 0x0x6E
let TIMER0_TIFR0: UInt8 = 0x0x35

// MARK: - USART0 (Universal Synchronous/Asynchronous Receiver/Transmitter)
let USART0_UDR0: UInt8 = 0x0xC6
let USART0_UCSR0A: UInt8 = 0x0xC0
let USART0_UCSR0B: UInt8 = 0x0xC1
let USART0_UCSR0C: UInt8 = 0x0xC2
let USART0_UBRR0: UInt16 = 0x0xC4

// MARK: - ADC (Analog-to-Digital Converter)
let ADC_ADMUX: UInt8 = 0x0x7C
let ADC_ADCSRA: UInt8 = 0x0x7A
let ADC_ADCH: UInt8 = 0x0x79
let ADC_ADCL: UInt8 = 0x0x78

// MARK: - Interrupt Vectors
let IRQ_INT0: Int = 1
let IRQ_INT1: Int = 2
let IRQ_PCINT0: Int = 3
let IRQ_PCINT1: Int = 4
let IRQ_PCINT2: Int = 5
let IRQ_WDT: Int = 6
let IRQ_TIMER2_COMPA: Int = 7
let IRQ_TIMER2_COMPB: Int = 8
let IRQ_TIMER2_OVF: Int = 9
let IRQ_TIMER1_CAPT: Int = 10
let IRQ_TIMER1_COMPA: Int = 11
let IRQ_TIMER1_COMPB: Int = 12
let IRQ_TIMER1_OVF: Int = 13
let IRQ_TIMER0_COMPA: Int = 14
let IRQ_TIMER0_COMPB: Int = 15
let IRQ_TIMER0_OVF: Int = 16
let IRQ_SPI_STC: Int = 17
let IRQ_USART_RX: Int = 18
let IRQ_USART_UDRE: Int = 19
let IRQ_USART_TX: Int = 20
let IRQ_ADC: Int = 21
let IRQ_EE_READY: Int = 22
let IRQ_ANALOG_COMP: Int = 23
let IRQ_TWI: Int = 24
let IRQ_SPM_READY: Int = 25

// MARK: - Memory Segments
let MEM_FLASH: (start: UInt32, size: UInt32) = (0x0x0000, 32768)
let MEM_SRAM: (start: UInt32, size: UInt32) = (0x0x0100, 2048)
let MEM_EEPROM: (start: UInt32, size: UInt32) = (0x0x0000, 1024)
let MEM_IO: (start: UInt32, size: UInt32) = (0x0x00, 64)
let MEM_EXTIO: (start: UInt32, size: UInt32) = (0x0x40, 192)

// MARK: - Device Functions
func atmega328p_init() {
}

func read_register(_ addr: UInt32) -> UInt32 {
    return 0
}

func write_register(_ addr: UInt32, _ value: UInt32) {
}
