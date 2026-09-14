//
// ATmega2560 Register Definitions
// Generated from: 8-bit AVR MCU with 256KB Flash, 8KB RAM, 4KB EEPROM, 16MHz, Arduino Mega
// Version: 1.0
// Date: 2026-04-28
//

import Foundation

// MARK: - PORTA (Port A)
let PORTA_DDRA: UInt8 = 0x0x21
let PORTA_PORTA: UInt8 = 0x0x22
let PORTA_PINA: UInt8 = 0x0x20

// MARK: - PORTB (Port B)
let PORTB_DDRB: UInt8 = 0x0x24
let PORTB_PORTB: UInt8 = 0x0x25
let PORTB_PINB: UInt8 = 0x0x23

// MARK: - PORTC (Port C)
let PORTC_DDRC: UInt8 = 0x0x27
let PORTC_PORTC: UInt8 = 0x0x28
let PORTC_PINC: UInt8 = 0x0x26

// MARK: - PORTD (Port D)
let PORTD_DDRD: UInt8 = 0x0x2A
let PORTD_PORTD: UInt8 = 0x0x2B
let PORTD_PIND: UInt8 = 0x0x29

// MARK: - PORTE (Port E)
let PORTE_DDRE: UInt8 = 0x0x2D
let PORTE_PORTE: UInt8 = 0x0x2E
let PORTE_PINE: UInt8 = 0x0x2C

// MARK: - PORTF (Port F)
let PORTF_DDRF: UInt8 = 0x0x30
let PORTF_PORTF: UInt8 = 0x0x31
let PORTF_PINF: UInt8 = 0x0x2F

// MARK: - PORTG (Port G)
let PORTG_DDRG: UInt8 = 0x0x33
let PORTG_PORTG: UInt8 = 0x0x34
let PORTG_PING: UInt8 = 0x0x32

// MARK: - USART0 (USART 0)
let USART0_UDR0: UInt8 = 0x0xC6
let USART0_UCSR0A: UInt8 = 0x0xC0
let USART0_UCSR0B: UInt8 = 0x0xC1
let USART0_UCSR0C: UInt8 = 0x0xC2
let USART0_UBRR0L: UInt8 = 0x0xC4
let USART0_UBRR0H: UInt8 = 0x0xC5

// MARK: - Interrupt Vectors
let IRQ_Reset: Int = 1
let IRQ_INT0: Int = 2
let IRQ_INT1: Int = 3
let IRQ_INT2: Int = 4
let IRQ_INT3: Int = 5
let IRQ_INT4: Int = 6
let IRQ_INT5: Int = 7
let IRQ_INT6: Int = 8
let IRQ_INT7: Int = 9
let IRQ_PCINT0: Int = 10
let IRQ_PCINT1: Int = 11
let IRQ_PCINT2: Int = 12
let IRQ_WDT: Int = 13
let IRQ_TIM2_COMPA: Int = 14
let IRQ_TIM2_COMPB: Int = 15
let IRQ_TIM2_OVF: Int = 16
let IRQ_TIM1_CAPT: Int = 17
let IRQ_TIM1_COMPA: Int = 18
let IRQ_TIM1_COMPB: Int = 19
let IRQ_TIM1_OVF: Int = 20
let IRQ_TIM0_COMPA: Int = 21
let IRQ_TIM0_COMPB: Int = 22
let IRQ_TIM0_OVF: Int = 23
let IRQ_SPI_STC: Int = 24
let IRQ_USART0_RX: Int = 25
let IRQ_USART0_UDRE: Int = 26
let IRQ_USART0_TX: Int = 27

// MARK: - Memory Segments
let MEM_FLASH: (start: UInt32, size: UInt32) = (0x0x0000, 262144)
let MEM_SRAM: (start: UInt32, size: UInt32) = (0x0x0200, 8192)
let MEM_EEPROM: (start: UInt32, size: UInt32) = (0x0x0000, 4096)
let MEM_IO: (start: UInt32, size: UInt32) = (0x0x00, 64)
let MEM_EXTIO: (start: UInt32, size: UInt32) = (0x0x40, 192)

// MARK: - Device Functions
func atmega2560_init() {
}

func read_register(_ addr: UInt32) -> UInt32 {
    return 0
}

func write_register(_ addr: UInt32, _ value: UInt32) {
}
