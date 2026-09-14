//
// LPC54606 Register Definitions
// Generated from: 32-bit ARM Cortex-M4 MCU with 256KB Flash, 136KB SRAM, 180MHz
// Version: 1.0
// Date: 2026-04-28
//

import Foundation

// MARK: - SYSCON (System Control)
let SYSCON_SYSAHBCLKCTRL: UInt32 = 0x0x80
let SYSCON_MAINCLKSEL: UInt32 = 0x0x04
let SYSCON_MAINCLKUEN: UInt32 = 0x0x08
let SYSCON_SYSPLLCTRL: UInt32 = 0x0x0C

// MARK: - GPIO (General Purpose I/O)
let GPIO_DIR0: UInt32 = 0x0x0000
let GPIO_PIN0: UInt32 = 0x0x1000
let GPIO_SET0: UInt32 = 0x0x2000
let GPIO_CLR0: UInt32 = 0x0x3000
let GPIO_NOT0: UInt32 = 0x0x4000
let GPIO_DIR1: UInt32 = 0x0x0004
let GPIO_PIN1: UInt32 = 0x0x1004
let GPIO_SET1: UInt32 = 0x0x2004
let GPIO_CLR1: UInt32 = 0x0x3004
let GPIO_NOT1: UInt32 = 0x0x4004

// MARK: - USART0 (USART0)
let USART0_CFG: UInt32 = 0x0x00
let USART0_CTRL: UInt32 = 0x0x04
let USART0_STAT: UInt32 = 0x0x08
let USART0_TXDAT: UInt32 = 0x0x10
let USART0_RXDAT: UInt32 = 0x0x14
let USART0_BRG: UInt32 = 0x0x20

// MARK: - Interrupt Vectors
let IRQ_Reset: Int = 0
let IRQ_SVCall: Int = 11
let IRQ_USART0: Int = 24

// MARK: - Memory Segments
let MEM_flash: (start: UInt32, size: UInt32) = (0x0x00000000, 262144)
let MEM_sram: (start: UInt32, size: UInt32) = (0x0x20000000, 139264)
let MEM_peripheral: (start: UInt32, size: UInt32) = (0x0x40000000, 2097152)

// MARK: - Device Functions
func lpc54606_init() {
}

func read_register(_ addr: UInt32) -> UInt32 {
    return 0
}

func write_register(_ addr: UInt32, _ value: UInt32) {
}
