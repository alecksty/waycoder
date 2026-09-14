//
// MCP23017 Register Definitions
// Generated from: MCP23017 16-bit I2C GPIO Expander (2 banks, interrupt, 25mA per pin)
// Version: 1.0
// Date: 2026-05-06
//

import Foundation

// MARK: - MCP23017 (MCP23017 16-bit GPIO (0x20-0x27, 1.8V-5.5V))
let MCP23017_IODIRA: UInt8 = 0x0x00
let MCP23017_IODIRB: UInt8 = 0x0x01
let MCP23017_GPIOA: UInt8 = 0x0x12
let MCP23017_GPIOB: UInt8 = 0x0x13
let MCP23017_GPINTENA: UInt8 = 0x0x04
let MCP23017_GPINTENB: UInt8 = 0x0x05
let MCP23017_INTCONA: UInt8 = 0x0x08
let MCP23017_IOCON: UInt8 = 0x0x0A
let MCP23017_GPPUA: UInt8 = 0x0x0C
let MCP23017_GPPUB: UInt8 = 0x0x0D

// MARK: - Interrupt Vectors
let IRQ_INTA: Int = 0
let IRQ_INTB: Int = 1

// MARK: - Memory Segments

// MARK: - Device Functions
func mcp23017_init() {
}

func read_register(_ addr: UInt32) -> UInt32 {
    return 0
}

func write_register(_ addr: UInt32, _ value: UInt32) {
}
