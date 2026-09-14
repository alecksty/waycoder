//
// RP2350 Register Definitions
// Generated from: Dual Cortex-M33 + RISC-V Hazard3 MCU with 520KB SRAM, 150MHz
// Version: 1.0
// Date: 2026-04-28
//

import Foundation

// MARK: - SIO (Single-Cycle I/O (GPIO))
let SIO_GPIO_IN: UInt32 = 0x0x004
let SIO_GPIO_OUT: UInt32 = 0x0x010
let SIO_GPIO_OUT_SET: UInt32 = 0x0x014
let SIO_GPIO_OUT_CLR: UInt32 = 0x0x018
let SIO_GPIO_OUT_XOR: UInt32 = 0x0x01C
let SIO_GPIO_OE: UInt32 = 0x0x020
let SIO_GPIO_OE_SET: UInt32 = 0x0x024
let SIO_GPIO_OE_CLR: UInt32 = 0x0x028

// MARK: - IO_BANK0 (IO Bank 0 (GPIO control))
let IO_BANK0_GPIO0_STATUS: UInt32 = 0x0x000
let IO_BANK0_GPIO0_CTRL: UInt32 = 0x0x004
let IO_BANK0_GPIO1_STATUS: UInt32 = 0x0x008
let IO_BANK0_GPIO1_CTRL: UInt32 = 0x0x00C

// MARK: - PADS_BANK0 (Pad controls for GPIO 0-29)
let PADS_BANK0_GPIO0: UInt32 = 0x0x000
let PADS_BANK0_GPIO1: UInt32 = 0x0x004

// MARK: - RESETS (Reset Controller)
let RESETS_RESET: UInt32 = 0x0x000
let RESETS_RESET_DONE: UInt32 = 0x0x008

// MARK: - Interrupt Vectors
let IRQ_Reset: Int = 0
let IRQ_SVCall: Int = 11

// MARK: - Memory Segments
let MEM_flash: (start: UInt32, size: UInt32) = (0x0x10000000, 8388608)
let MEM_sram: (start: UInt32, size: UInt32) = (0x0x20000000, 532480)
let MEM_peripheral: (start: UInt32, size: UInt32) = (0x0x40000000, 16777216)

// MARK: - Device Functions
func rp2350_init() {
}

func read_register(_ addr: UInt32) -> UInt32 {
    return 0
}

func write_register(_ addr: UInt32, _ value: UInt32) {
}
