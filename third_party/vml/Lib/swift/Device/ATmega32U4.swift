//
// ATmega32U4 Register Definitions
// Generated from: 8-bit AVR microcontroller with USB, 32KB Flash, 2.5KB SRAM, 16MHz
// Version: 1.0
// Date: 2026-04-28
//

import Foundation

// MARK: - PORTB (Port B)
let PORTB_PORTB: UInt8 = 0x0x25
let PORTB_DDRB: UInt8 = 0x0x24
let PORTB_PINB: UInt8 = 0x0x23

// MARK: - PORTC (Port C)
let PORTC_PORTC: UInt8 = 0x0x28
let PORTC_DDRC: UInt8 = 0x0x27
let PORTC_PINC: UInt8 = 0x0x26

// MARK: - PORTD (Port D)
let PORTD_PORTD: UInt8 = 0x0x2B
let PORTD_DDRD: UInt8 = 0x0x2A
let PORTD_PIND: UInt8 = 0x0x29

// MARK: - PORTE (Port E)
let PORTE_PORTE: UInt8 = 0x0x2E
let PORTE_DDRE: UInt8 = 0x0x2D
let PORTE_PINE: UInt8 = 0x0x2C

// MARK: - UART1 (USART1)
let UART1_UDR1: UInt8 = 0x0xCE
let UART1_UCSR1A: UInt8 = 0x0xC8
let UART1_UCSR1B: UInt8 = 0x0xC9
let UART1_UCSR1C: UInt8 = 0x0xCA
let UART1_UBRR1: UInt16 = 0x0xCC

// MARK: - USB (USB Controller)
let USB_UDCON: UInt8 = 0x0xD0
let USB_UDIEN: UInt8 = 0x0xD1
let USB_UDINT: UInt8 = 0x0xD2

// MARK: - Interrupt Vectors
let IRQ_INT0: Int = 1
let IRQ_INT1: Int = 2
let IRQ_INT2: Int = 3
let IRQ_INT3: Int = 4
let IRQ_INT4: Int = 5
let IRQ_INT5: Int = 6
let IRQ_INT6: Int = 7
let IRQ_PCINT0: Int = 8
let IRQ_USB_General: Int = 9
let IRQ_USB_Endpoint: Int = 10
let IRQ_WDT: Int = 11
let IRQ_TIMER1_CAPT: Int = 12
let IRQ_TIMER1_COMPA: Int = 13
let IRQ_TIMER1_COMPB: Int = 14
let IRQ_TIMER1_OVF: Int = 15
let IRQ_TIMER0_COMPA: Int = 16
let IRQ_TIMER0_COMPB: Int = 17
let IRQ_TIMER0_OVF: Int = 18
let IRQ_SPI_STC: Int = 19
let IRQ_UART1_RX: Int = 20
let IRQ_UART1_UDRE: Int = 21
let IRQ_UART1_TX: Int = 22
let IRQ_ADC: Int = 23

// MARK: - Memory Segments
let MEM_FLASH: (start: UInt32, size: UInt32) = (0x0x0000, 32768)
let MEM_SRAM: (start: UInt32, size: UInt32) = (0x0x0100, 2560)
let MEM_EEPROM: (start: UInt32, size: UInt32) = (0x0x0000, 1024)
let MEM_IO: (start: UInt32, size: UInt32) = (0x0x00, 64)
let MEM_EXTIO: (start: UInt32, size: UInt32) = (0x0x40, 192)

// MARK: - Device Functions
func atmega32u4_init() {
}

func read_register(_ addr: UInt32) -> UInt32 {
    return 0
}

func write_register(_ addr: UInt32, _ value: UInt32) {
}
