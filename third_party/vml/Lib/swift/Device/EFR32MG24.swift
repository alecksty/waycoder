//
// EFR32MG24 Register Definitions
// Generated from: 32-bit ARM Cortex-M33 MCU with 1536KB Flash, 256KB RAM, 78MHz, Zigbee/Thread/Matter
// Version: 1.0
// Date: 2026-04-28
//

import Foundation

// MARK: - CMU (Clock Management Unit)
let CMU_CTRL: UInt32 = 0x0x00
let CMU_HFCORECLKCFG: UInt32 = 0x0x08
let CMU_HFPERCLKEN0: UInt32 = 0x0x10
let CMU_LFBCLKEN0: UInt32 = 0x0x20

// MARK: - GPIO (GPIO Controller)
let GPIO_PORT_A_CTRL: UInt32 = 0x0x00
let GPIO_PORT_B_CTRL: UInt32 = 0x0x04
let GPIO_PORT_C_CTRL: UInt32 = 0x0x08
let GPIO_PORT_D_CTRL: UInt32 = 0x0x0C
let GPIO_MODEL: UInt32 = 0x0x10
let GPIO_MODEH: UInt32 = 0x0x14
let GPIO_DOUT: UInt32 = 0x0x1C
let GPIO_DOUTSET: UInt32 = 0x0x20
let GPIO_DOUTCLR: UInt32 = 0x0x24
let GPIO_DOUTTGL: UInt32 = 0x0x28
let GPIO_DIN: UInt32 = 0x0x2C

// MARK: - GPIO_PA (GPIO Port A extended)
let GPIO_PA_PA_CFG: UInt32 = 0x0x00
let GPIO_PA_PA_PINOUT: UInt32 = 0x0x04

// MARK: - GPIO_PB (GPIO Port B extended)
let GPIO_PB_PB_CFG: UInt32 = 0x0x00

// MARK: - USART0 (USART 0)
let USART0_CTRL: UInt32 = 0x0x00
let USART0_CMD: UInt32 = 0x0x04
let USART0_STATUS: UInt32 = 0x0x08
let USART0_RXDATA: UInt32 = 0x0x0C
let USART0_TXDATA: UInt32 = 0x0x10
let USART0_CLKDIV: UInt32 = 0x0x14

// MARK: - Interrupt Vectors
let IRQ_Reset: Int = 0
let IRQ_SVCall: Int = 11
let IRQ_USART0_RX: Int = 12
let IRQ_USART0_TX: Int = 13

// MARK: - Memory Segments
let MEM_flash: (start: UInt32, size: UInt32) = (0x0x08000000, 1572864)
let MEM_sram: (start: UInt32, size: UInt32) = (0x0x20000000, 262144)
let MEM_peripheral: (start: UInt32, size: UInt32) = (0x0x40000000, 524288)

// MARK: - Device Functions
func efr32mg24_init() {
}

func read_register(_ addr: UInt32) -> UInt32 {
    return 0
}

func write_register(_ addr: UInt32, _ value: UInt32) {
}
