//
// ESP32-C3 Register Definitions
// Generated from: 32-bit RISC-V single-core WiFi + BLE SoC, 160MHz, 400KB SRAM
// Version: 1.0
// Date: 2026-04-28
//

import Foundation

// MARK: - GPIO (General Purpose I/O)
let GPIO_OUT: UInt32 = 0x0x04
let GPIO_OUT_W1TS: UInt32 = 0x0x08
let GPIO_OUT_W1TC: UInt32 = 0x0x0C
let GPIO_IN: UInt32 = 0x0x10
let GPIO_ENABLE: UInt32 = 0x0x20
let GPIO_ENABLE_W1TS: UInt32 = 0x0x24
let GPIO_ENABLE_W1TC: UInt32 = 0x0x28

// MARK: - IO_MUX (I/O MUX)
let IO_MUX_GPIO0: UInt32 = 0x0x00
let IO_MUX_GPIO1: UInt32 = 0x0x04
let IO_MUX_GPIO2: UInt32 = 0x0x08
let IO_MUX_GPIO3: UInt32 = 0x0x0C

// MARK: - RTC_CNTL (RTC Control)
let RTC_CNTL_OPTIONS0: UInt32 = 0x0x00
let RTC_CNTL_CLK_CONF: UInt32 = 0x0x30

// MARK: - Interrupt Vectors
let IRQ_Reset: Int = 1
let IRQ_MachineSoftware: Int = 3
let IRQ_MachineTimer: Int = 7
let IRQ_MachineExternal: Int = 11

// MARK: - Memory Segments
let MEM_flash: (start: UInt32, size: UInt32) = (0x0x42000000, 8388608)
let MEM_sram: (start: UInt32, size: UInt32) = (0x0x3FC80000, 409600)
let MEM_peripheral: (start: UInt32, size: UInt32) = (0x0x60000000, 1048576)

// MARK: - Device Functions
func esp32_c3_init() {
}

func read_register(_ addr: UInt32) -> UInt32 {
    return 0
}

func write_register(_ addr: UInt32, _ value: UInt32) {
}
