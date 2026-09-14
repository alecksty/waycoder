//
// 8086 Register Definitions
// Generated from: 16-bit microprocessor, first x86 processor
// Version: 1.0
// Date: 2026-04-17
//

import Foundation

// MARK: - PIC (Programmable Interrupt Controller)
let PIC_PIC1_CMD: UInt8 = 0x0x0020
let PIC_PIC1_DATA: UInt8 = 0x0x0021
let PIC_PIC2_CMD: UInt8 = 0x0x00A0
let PIC_PIC2_DATA: UInt8 = 0x0x00A1

// MARK: - PIT (Programmable Interval Timer)
let PIT_PIT_CH0: UInt8 = 0x0x0040
let PIT_PIT_CH1: UInt8 = 0x0x0041
let PIT_PIT_CH2: UInt8 = 0x0x0042
let PIT_PIT_CMD: UInt8 = 0x0x0043

// MARK: - PPI (Programmable Peripheral Interface)
let PPI_PPI_PA: UInt8 = 0x0x0060
let PPI_PPI_PB: UInt8 = 0x0x0061
let PPI_PPI_PC: UInt8 = 0x0x0062
let PPI_PPI_CMD: UInt8 = 0x0x0063

// MARK: - Interrupt Vectors
let IRQ_DIVIDE_ERROR: Int = 0
let IRQ_DEBUG: Int = 1
let IRQ_NMI: Int = 2
let IRQ_BREAKPOINT: Int = 3
let IRQ_OVERFLOW: Int = 4
let IRQ_IRQ0: Int = 8
let IRQ_IRQ1: Int = 9
let IRQ_IRQ2: Int = 10
let IRQ_IRQ3: Int = 11
let IRQ_IRQ4: Int = 12
let IRQ_IRQ5: Int = 13
let IRQ_IRQ6: Int = 14
let IRQ_IRQ7: Int = 15

// MARK: - Memory Segments
let MEM_CODE: (start: UInt32, size: UInt32) = (0x0x00000, 1048576)
let MEM_DATA: (start: UInt32, size: UInt32) = (0x0x00000, 1048576)
let MEM_STACK: (start: UInt32, size: UInt32) = (0x0xF0000, 65536)
let MEM_BIOS: (start: UInt32, size: UInt32) = (0x0xF0000, 65536)

// MARK: - Device Functions
func _8086_init() {
}

func read_register(_ addr: UInt32) -> UInt32 {
    return 0
}

func write_register(_ addr: UInt32, _ value: UInt32) {
}
