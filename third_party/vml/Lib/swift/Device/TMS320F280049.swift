//
// TMS320F280049 Register Definitions
// Generated from: 32-bit C28x DSP + CLA MCU with 256KB Flash, 100KB RAM, 100MHz
// Version: 1.0
// Date: 2026-04-28
//

import Foundation

// MARK: - PLL (PLL Clock Control)
let PLL_SYSPLLCTL1: UInt16 = 0x0x00
let PLL_SYSPLLCTL2: UInt16 = 0x0x02
let PLL_CLKSRCCTL1: UInt16 = 0x0x04
let PLL_CLKSRCCTL2: UInt16 = 0x0x06

// MARK: - GPIO_CTRL (GPIO Control Registers)
let GPIO_CTRL_GPACTRL: UInt16 = 0x0x00
let GPIO_CTRL_GPAQSEL1: UInt16 = 0x0x02
let GPIO_CTRL_GPAQSEL2: UInt16 = 0x0x04
let GPIO_CTRL_GPAMUX1: UInt16 = 0x0x06
let GPIO_CTRL_GPAMUX2: UInt16 = 0x0x08
let GPIO_CTRL_GPADIR: UInt16 = 0x0x0A
let GPIO_CTRL_GPAPUD: UInt16 = 0x0x0C

// MARK: - GPIO_DATA (GPIO Data Registers)
let GPIO_DATA_GPADAT: UInt16 = 0x0x00
let GPIO_DATA_GPASET: UInt16 = 0x0x02
let GPIO_DATA_GPACLEAR: UInt16 = 0x0x04
let GPIO_DATA_GPATOGGLE: UInt16 = 0x0x06
let GPIO_DATA_GPBDAT: UInt16 = 0x0x08
let GPIO_DATA_GPBSET: UInt16 = 0x0x0A
let GPIO_DATA_GPBCLEAR: UInt16 = 0x0x0C
let GPIO_DATA_GPBTOGGLE: UInt16 = 0x0x0E

// MARK: - GPIO_B_CTRL (GPIO B Control)
let GPIO_B_CTRL_GPBMUX1: UInt16 = 0x0x00
let GPIO_B_CTRL_GPBMUX2: UInt16 = 0x0x02
let GPIO_B_CTRL_GPBDIR: UInt16 = 0x0x04
let GPIO_B_CTRL_GPBPUD: UInt16 = 0x0x06

// MARK: - SCI_A (SCI-A UART)
let SCI_A_SCICCR: UInt16 = 0x0x00
let SCI_A_SCICTL1: UInt16 = 0x0x02
let SCI_A_SCIBAUD: UInt16 = 0x0x04
let SCI_A_SCIRXBUF: UInt16 = 0x0x0A
let SCI_A_SCITXBUF: UInt16 = 0x0x0C

// MARK: - Interrupt Vectors
let IRQ_Reset: Int = 1
let IRQ_SCIA_RX: Int = 8
let IRQ_SCIA_TX: Int = 9

// MARK: - Memory Segments
let MEM_flash: (start: UInt32, size: UInt32) = (0x0x080000, 262144)
let MEM_sram_ls: (start: UInt32, size: UInt32) = (0x0x008000, 16384)
let MEM_sram_gs: (start: UInt32, size: UInt32) = (0x0x00C000, 81920)
let MEM_peripheral: (start: UInt32, size: UInt32) = (0x0x400000, 65536)

// MARK: - Device Functions
func tms320f280049_init() {
}

func read_register(_ addr: UInt32) -> UInt32 {
    return 0
}

func write_register(_ addr: UInt32, _ value: UInt32) {
}
