//
// GD32F103 Register Definitions
// Generated from: 32-bit ARM Cortex-M3 MCU, 108MHz, STM32F103 compatible
// Version: 1.0
// Date: 2026-04-28
//

import Foundation

// MARK: - RCC (Reset and Clock Control)
let RCC_CTLR: UInt32 = 0x0x00
let RCC_CFGR0: UInt32 = 0x0x04
let RCC_APB2PCENR: UInt32 = 0x0x18
let RCC_APB1PCENR: UInt32 = 0x0x1C

// MARK: - GPIOA (General Purpose I/O Port A)
let GPIOA_CTL0: UInt32 = 0x0x00
let GPIOA_CTL1: UInt32 = 0x0x04
let GPIOA_ISTAT: UInt32 = 0x0x08
let GPIOA_OCTL: UInt32 = 0x0x0C
let GPIOA_BOP: UInt32 = 0x0x10
let GPIOA_BC: UInt32 = 0x0x14

// MARK: - GPIOB (General Purpose I/O Port B)
let GPIOB_CTL0: UInt32 = 0x0x00
let GPIOB_CTL1: UInt32 = 0x0x04
let GPIOB_ISTAT: UInt32 = 0x0x08
let GPIOB_OCTL: UInt32 = 0x0x0C
let GPIOB_BOP: UInt32 = 0x0x10
let GPIOB_BC: UInt32 = 0x0x14

// MARK: - GPIOC (General Purpose I/O Port C)
let GPIOC_CTL0: UInt32 = 0x0x00
let GPIOC_CTL1: UInt32 = 0x0x04
let GPIOC_ISTAT: UInt32 = 0x0x08
let GPIOC_OCTL: UInt32 = 0x0x0C
let GPIOC_BOP: UInt32 = 0x0x10
let GPIOC_BC: UInt32 = 0x0x14

// MARK: - USART0 (USART0)
let USART0_STATR: UInt32 = 0x0x00
let USART0_DATAR: UInt32 = 0x0x04
let USART0_BRR: UInt32 = 0x0x08
let USART0_CTLR1: UInt32 = 0x0x0C

// MARK: - USART1 (USART1)
let USART1_STATR: UInt32 = 0x0x00
let USART1_DATAR: UInt32 = 0x0x04
let USART1_BRR: UInt32 = 0x0x08
let USART1_CTLR1: UInt32 = 0x0x0C

// MARK: - Interrupt Vectors
let IRQ_Reset: Int = 0
let IRQ_SVCall: Int = 11
let IRQ_USART0: Int = 25
let IRQ_USART1: Int = 37

// MARK: - Memory Segments
let MEM_flash: (start: UInt32, size: UInt32) = (0x0x08000000, 131072)
let MEM_sram: (start: UInt32, size: UInt32) = (0x0x20000000, 20480)
let MEM_peripheral: (start: UInt32, size: UInt32) = (0x0x40000000, 262144)

// MARK: - Device Functions
func gd32f103_init() {
}

func read_register(_ addr: UInt32) -> UInt32 {
    return 0
}

func write_register(_ addr: UInt32, _ value: UInt32) {
}
