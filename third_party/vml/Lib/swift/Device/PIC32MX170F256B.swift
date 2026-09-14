//
// PIC32MX170F256B Register Definitions
// Generated from: 32-bit MIPS32 M4K MCU with 256KB Flash, 64KB RAM, 50MHz
// Version: 1.0
// Date: 2026-04-28
//

import Foundation

// MARK: - PORTA (General Purpose I/O Port A)
let PORTA_TRISA: UInt32 = 0x0x00
let PORTA_PORTA: UInt32 = 0x0x10
let PORTA_LATA: UInt32 = 0x0x20
let PORTA_ODCA: UInt32 = 0x0x30

// MARK: - PORTB (General Purpose I/O Port B)
let PORTB_TRISB: UInt32 = 0x0x00
let PORTB_PORTB: UInt32 = 0x0x10
let PORTB_LATB: UInt32 = 0x0x20
let PORTB_ODCB: UInt32 = 0x0x30

// MARK: - UART1 (UART1)
let UART1_UXMODE: UInt32 = 0x0x00
let UART1_UXSTA: UInt32 = 0x0x04
let UART1_UXTXREG: UInt32 = 0x0x08
let UART1_UXRXREG: UInt32 = 0x0x0C
let UART1_UXBRG: UInt32 = 0x0x10

// MARK: - Interrupt Vectors
let IRQ_Reset: Int = 0
let IRQ_UART1: Int = 8

// MARK: - Memory Segments
let MEM_flash: (start: UInt32, size: UInt32) = (0x0x9D000000, 262144)
let MEM_sram: (start: UInt32, size: UInt32) = (0x0xA0000000, 65536)
let MEM_peripheral: (start: UInt32, size: UInt32) = (0x0xBF800000, 1048576)
let MEM_bootflash: (start: UInt32, size: UInt32) = (0x0xBFC00000, 12288)

// MARK: - Device Functions
func pic32mx170f256b_init() {
}

func read_register(_ addr: UInt32) -> UInt32 {
    return 0
}

func write_register(_ addr: UInt32, _ value: UInt32) {
}
