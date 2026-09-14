//
// ADS1115 Register Definitions
// Generated from: ADS1115 16-bit I2C ADC (4-channel, PGA, 860SPS)
// Version: 1.0
// Date: 2026-05-06
//

import Foundation

// MARK: - ADS1115 (ADS1115 16-bit ADC (0x48-0x4B, 2.0V-5.5V))
let ADS1115_CONV_RESULT: UInt16 = 0x0x00
let ADS1115_CONFIG: UInt16 = 0x0x01
let ADS1115_LO_THRESH: UInt16 = 0x0x02
let ADS1115_HI_THRESH: UInt16 = 0x0x03

// MARK: - Interrupt Vectors

// MARK: - Memory Segments

// MARK: - Device Functions
func ads1115_init() {
}

func read_register(_ addr: UInt32) -> UInt32 {
    return 0
}

func write_register(_ addr: UInt32, _ value: UInt32) {
}
