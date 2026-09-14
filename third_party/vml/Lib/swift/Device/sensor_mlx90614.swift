//
// MLX90614 Register Definitions
// Generated from: MLX90614 Infrared Thermometer (I2C, non-contact, -70 to +380°C, 17-bit)
// Version: 1.0
// Date: 2026-05-06
//

import Foundation

// MARK: - MLX90614 (MLX90614 IR Thermometer (0x5A, 3V-5V, TO-39))
let MLX90614_T_AMBIENT: UInt16 = 0x0x06
let MLX90614_T_OBJECT1: UInt16 = 0x0x07
let MLX90614_T_OBJECT2: UInt16 = 0x0x08
let MLX90614_RAW_IR1: UInt16 = 0x0x04
let MLX90614_RAW_IR2: UInt16 = 0x0x05
let MLX90614_EMISSIVITY: UInt16 = 0x0x04

// MARK: - Interrupt Vectors

// MARK: - Memory Segments
let MEM_EEPROM: (start: UInt32, size: UInt32) = (0x0x00, 32)

// MARK: - Device Functions
func mlx90614_init() {
}

func read_register(_ addr: UInt32) -> UInt32 {
    return 0
}

func write_register(_ addr: UInt32, _ value: UInt32) {
}
