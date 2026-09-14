//
// PIC16F84 Register Definitions
// Generated from: Microchip PIC16F84 8-bit microcontroller with EEPROM
// Version: 
// Date: 
//

import Foundation

// MARK: - Timer0 (8-bit timer/counter with prescaler)
let Timer0_TMR0: UInt64 = 0x0x01

// MARK: - Timer1 (16-bit timer/counter with prescaler)
let Timer1_TMR1L: UInt64 = 0x0x0E
let Timer1_TMR1H: UInt64 = 0x0x0F
let Timer1_T1CON: UInt64 = 0x0x10

// MARK: - Watchdog (Watchdog Timer)
let Watchdog_WDTCON: UInt64 = 0x0x07

// MARK: - EEPROM (64-byte EEPROM data memory)
let EEPROM_EEDATA: UInt64 = 0x0x08
let EEPROM_EEADR: UInt64 = 0x0x09
let EEPROM_EECON1: UInt64 = 0x0x88
let EEPROM_EECON2: UInt64 = 0x0x89

// MARK: - GPIO (General Purpose I/O)
let GPIO_PORTA: UInt64 = 0x0x05
let GPIO_PORTB: UInt64 = 0x0x06
let GPIO_TRISA: UInt64 = 0x0x85
let GPIO_TRISB: UInt64 = 0x0x86

// MARK: - Interrupt Vectors
let IRQ_INT: Int = 4
let IRQ_TMR0: Int = 4
let IRQ_PORTB: Int = 4
let IRQ_EEPROM: Int = 4

// MARK: - Memory Segments

// MARK: - Device Functions
func pic16f84_init() {
}

func read_register(_ addr: UInt32) -> UInt32 {
    return 0
}

func write_register(_ addr: UInt32, _ value: UInt32) {
}
