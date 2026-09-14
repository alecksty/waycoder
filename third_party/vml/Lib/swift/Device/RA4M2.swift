//
// RA4M2 Register Definitions
// Generated from: 32-bit ARM Cortex-M4 MCU with 256KB Flash, 128KB RAM, 100MHz
// Version: 1.0
// Date: 2026-04-28
//

import Foundation

// MARK: - MSTP (Module Stop Control)
let MSTP_MSTPCR_A: UInt32 = 0x0x20
let MSTP_MSTPCR_B: UInt32 = 0x0x24
let MSTP_MSTPCR_C: UInt32 = 0x0x28
let MSTP_MSTPCR_D: UInt32 = 0x0x2C

// MARK: - ICU (Interrupt Controller Unit)
let ICU_IRQCR0: UInt16 = 0x0x600
let ICU_IRQCR1: UInt16 = 0x0x602

// MARK: - GPIOA (General Purpose I/O Port A)
let GPIOA_PDR: UInt16 = 0x0x00
let GPIOA_PODR: UInt16 = 0x0x04
let GPIOA_PIDR: UInt16 = 0x0x08
let GPIOA_PMR: UInt16 = 0x0x10
let GPIOA_PCR: UInt32 = 0x0x18

// MARK: - GPIOB (General Purpose I/O Port B)
let GPIOB_PDR: UInt16 = 0x0x00
let GPIOB_PODR: UInt16 = 0x0x04
let GPIOB_PIDR: UInt16 = 0x0x08
let GPIOB_PMR: UInt16 = 0x0x10

// MARK: - SCIUART0 (SCI UART 0)
let SCIUART0_SCR: UInt8 = 0x0x00
let SCIUART0_BRR: UInt8 = 0x0x04
let SCIUART0_TDR: UInt8 = 0x0x08
let SCIUART0_RDR: UInt8 = 0x0x0C
let SCIUART0_SSR: UInt8 = 0x0x10

// MARK: - Interrupt Vectors
let IRQ_Reset: Int = 0
let IRQ_SVCall: Int = 11
let IRQ_SCIUART0_RXI: Int = 24
let IRQ_SCIUART0_TXI: Int = 25

// MARK: - Memory Segments
let MEM_flash: (start: UInt32, size: UInt32) = (0x0x00000000, 262144)
let MEM_sram: (start: UInt32, size: UInt32) = (0x0x1FFE0000, 32768)
let MEM_sram1: (start: UInt32, size: UInt32) = (0x0x20000000, 98304)
let MEM_peripheral: (start: UInt32, size: UInt32) = (0x0x40000000, 1048576)

// MARK: - Device Functions
func ra4m2_init() {
}

func read_register(_ addr: UInt32) -> UInt32 {
    return 0
}

func write_register(_ addr: UInt32, _ value: UInt32) {
}
