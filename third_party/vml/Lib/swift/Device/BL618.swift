//
// BL618 Register Definitions
// Generated from: 32-bit RISC-V RV32IMAFC WiFi6 + BLE SoC with 4MB Flash, 512KB SRAM, 480MHz
// Version: 1.0
// Date: 2026-04-28
//

import Foundation

// MARK: - GLB (Global Control (Clock and Reset))
let GLB_GLB_CLK_EN: UInt32 = 0x0x10
let GLB_GLB_SYS_CLK_CTRL: UInt32 = 0x0x14
let GLB_GLB_PLL_CTRL: UInt32 = 0x0x1C

// MARK: - GPIO_P0 (GPIO Port A)
let GPIO_P0_GPIO_CFG0: UInt32 = 0x0x00
let GPIO_P0_GPIO_CFG1: UInt32 = 0x0x04
let GPIO_P0_GPIO_OE: UInt32 = 0x0x08
let GPIO_P0_GPIO_OUT: UInt32 = 0x0x0C
let GPIO_P0_GPIO_IN: UInt32 = 0x0x10
let GPIO_P0_GPIO_SET: UInt32 = 0x0x14
let GPIO_P0_GPIO_CLR: UInt32 = 0x0x18
let GPIO_P0_GPIO_TOG: UInt32 = 0x0x1C

// MARK: - GPIO_P1 (GPIO Port B)
let GPIO_P1_GPIO_CFG0: UInt32 = 0x0x00
let GPIO_P1_GPIO_CFG1: UInt32 = 0x0x04
let GPIO_P1_GPIO_OE: UInt32 = 0x0x08
let GPIO_P1_GPIO_OUT: UInt32 = 0x0x0C
let GPIO_P1_GPIO_IN: UInt32 = 0x0x10
let GPIO_P1_GPIO_SET: UInt32 = 0x0x14
let GPIO_P1_GPIO_CLR: UInt32 = 0x0x18
let GPIO_P1_GPIO_TOG: UInt32 = 0x0x1C

// MARK: - UART0 (UART 0)
let UART0_UART_CR: UInt32 = 0x0x00
let UART0_UART_BRR: UInt32 = 0x0x04
let UART0_UART_TDR: UInt32 = 0x0x08
let UART0_UART_RDR: UInt32 = 0x0x0C
let UART0_UART_SR: UInt32 = 0x0x10

// MARK: - Interrupt Vectors
let IRQ_Reset: Int = 1
let IRQ_MachineSoftware: Int = 3
let IRQ_MachineTimer: Int = 7
let IRQ_MachineExternal: Int = 11
let IRQ_UART0: Int = 20

// MARK: - Memory Segments
let MEM_flash: (start: UInt32, size: UInt32) = (0x0x20000000, 4194304)
let MEM_sram_hpsys: (start: UInt32, size: UInt32) = (0x0x22000000, 16384)
let MEM_sram_dtcm: (start: UInt32, size: UInt32) = (0x0x22010000, 32768)
let MEM_sram_sys: (start: UInt32, size: UInt32) = (0x0x22020000, 458752)
let MEM_peripheral: (start: UInt32, size: UInt32) = (0x0x30000000, 1048576)

// MARK: - Device Functions
func bl618_init() {
}

func read_register(_ addr: UInt32) -> UInt32 {
    return 0
}

func write_register(_ addr: UInt32, _ value: UInt32) {
}
