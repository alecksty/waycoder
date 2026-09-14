//
// 8051 Register Definitions
// Generated from: 8-bit microcontroller with 4KB ROM, 128B RAM, 32 I/O lines
// Version: 1.0
// Date: 2026-04-16
//

import Foundation

// MARK: - PORT0 (Port 0)
let PORT0_P0: UInt8 = 0x0x80

// MARK: - PORT1 (Port 1)
let PORT1_P1: UInt8 = 0x0x90

// MARK: - PORT2 (Port 2)
let PORT2_P2: UInt8 = 0x0xA0

// MARK: - PORT3 (Port 3)
let PORT3_P3: UInt8 = 0x0xB0

// MARK: - TIMER0 (Timer/Counter 0)
let TIMER0_TH0: UInt8 = 0x0x8C
let TIMER0_TL0: UInt8 = 0x0x8A
let TIMER0_TMOD: UInt8 = 0x0x89
let TIMER0_TCON: UInt8 = 0x0x88

// MARK: - UART (Serial Port)
let UART_SBUF: UInt8 = 0x0x99
let UART_SCON: UInt8 = 0x0x98

// MARK: - Interrupt Vectors
let IRQ_RESET: Int = 0
let IRQ_INT0: Int = 1
let IRQ_TIMER0: Int = 2
let IRQ_INT1: Int = 3
let IRQ_TIMER1: Int = 4
let IRQ_UART: Int = 5

// MARK: - Memory Segments
let MEM_CODE: (start: UInt32, size: UInt32) = (0x0x0000, 4096)
let MEM_IDATA: (start: UInt32, size: UInt32) = (0x0x00, 128)
let MEM_SFR: (start: UInt32, size: UInt32) = (0x0x80, 128)
let MEM_XDATA: (start: UInt32, size: UInt32) = (0x0x0000, 65536)

// MARK: - Device Functions
func _8051_init() {
}

func read_register(_ addr: UInt32) -> UInt32 {
    return 0
}

func write_register(_ addr: UInt32, _ value: UInt32) {
}
