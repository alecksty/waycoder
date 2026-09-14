//
// NEO6M Register Definitions
// Generated from: NEO-6M GPS Module (UART, 50-channel, -162dBm tracking)
// Version: 1.0
// Date: 2026-05-06
//

import Foundation

// MARK: - NEO6M (NEO-6M GPS Module (UART 9600bps, 3.3V-5V))
let NEO6M_LATITUDE: UInt32 = 0x0x00
let NEO6M_LONGITUDE: UInt32 = 0x0x04
let NEO6M_ALTITUDE: UInt32 = 0x0x08
let NEO6M_SPEED: UInt16 = 0x0x0C
let NEO6M_HEADING: UInt16 = 0x0x0E
let NEO6M_SATELLITES: UInt8 = 0x0x10
let NEO6M_HDOP: UInt16 = 0x0x11
let NEO6M_FIX_TYPE: UInt8 = 0x0x13
let NEO6M_DATE: UInt32 = 0x0x14
let NEO6M_TIME: UInt32 = 0x0x18
let NEO6M_VALID: UInt8 = 0x0x1C

// MARK: - Interrupt Vectors

// MARK: - Memory Segments

// MARK: - Device Functions
func neo6m_init() {
}

func read_register(_ addr: UInt32) -> UInt32 {
    return 0
}

func write_register(_ addr: UInt32, _ value: UInt32) {
}
