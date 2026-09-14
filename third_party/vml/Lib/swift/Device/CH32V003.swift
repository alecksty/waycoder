//
// CH32V003 Register Definitions
// Generated from: 32-bit RISC-V RV32EC MCU with 16KB Flash, 2KB RAM, 48MHz, ultra-low-cost
// Version: 1.0
// Date: 2026-04-28
//

import Foundation

// MARK: - RCC (Reset and Clock Control)
let RCC_CTLR: UInt32 = 0x0x00
let RCC_CFGR0: UInt32 = 0x0x04
let RCC_APB2PCENR: UInt32 = 0x0x18

// MARK: - GPIOA (General Purpose I/O Port A)
let GPIOA_CFGLR: UInt32 = 0x0x00
let GPIOA_CFGHR: UInt32 = 0x0x04
let GPIOA_INDR: UInt32 = 0x0x08
let GPIOA_OUTDR: UInt32 = 0x0x0C
let GPIOA_BSHR: UInt32 = 0x0x10
let GPIOA_BCR: UInt32 = 0x0x14

// MARK: - GPIOC (General Purpose I/O Port C)
let GPIOC_CFGLR: UInt32 = 0x0x00
let GPIOC_CFGHR: UInt32 = 0x0x04
let GPIOC_INDR: UInt32 = 0x0x08
let GPIOC_OUTDR: UInt32 = 0x0x0C
let GPIOC_BSHR: UInt32 = 0x0x10
let GPIOC_BCR: UInt32 = 0x0x14

// MARK: - GPIOD (General Purpose I/O Port D)
let GPIOD_CFGLR: UInt32 = 0x0x00
let GPIOD_CFGHR: UInt32 = 0x0x04
let GPIOD_INDR: UInt32 = 0x0x08
let GPIOD_OUTDR: UInt32 = 0x0x0C
let GPIOD_BSHR: UInt32 = 0x0x10
let GPIOD_BCR: UInt32 = 0x0x14

// MARK: - USART1 (USART1)
let USART1_STATR: UInt32 = 0x0x00
let USART1_DATAR: UInt32 = 0x0x04
let USART1_BRR: UInt32 = 0x0x08
let USART1_CTLR1: UInt32 = 0x0x0C

// MARK: - Interrupt Vectors
let IRQ_Reset: Int = 1
let IRQ_MachineSoftware: Int = 3
let IRQ_MachineTimer: Int = 7
let IRQ_MachineExternal: Int = 11
let IRQ_USART1: Int = 25

// MARK: - Memory Segments
let MEM_flash: (start: UInt32, size: UInt32) = (0x0x08000000, 16384)
let MEM_sram: (start: UInt32, size: UInt32) = (0x0x20000000, 2048)
let MEM_peripheral: (start: UInt32, size: UInt32) = (0x0x40000000, 16384)

// MARK: - Device Functions
func ch32v003_init() {
}

func read_register(_ addr: UInt32) -> UInt32 {
    return 0
}

func write_register(_ addr: UInt32, _ value: UInt32) {
}
