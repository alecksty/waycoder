//
// IBM-PC-5150 Register Definitions
// Generated from: Original IBM Personal Computer Model 5150
// Version: 1.0
// Date: 2026-04-16
//

import Foundation

// MARK: - PIC (Programmable Interrupt Controller)
let PIC_PIC1_CMD: UInt8 = 0x0x20
let PIC_PIC1_DATA: UInt8 = 0x0x21
let PIC_PIC2_CMD: UInt8 = 0x0xA0
let PIC_PIC2_DATA: UInt8 = 0x0xA1

// MARK: - PIT (Programmable Interval Timer)
let PIT_PIT_CH0: UInt8 = 0x0x40
let PIT_PIT_CH1: UInt8 = 0x0x41
let PIT_PIT_CH2: UInt8 = 0x0x42
let PIT_PIT_CTRL: UInt8 = 0x0x43

// MARK: - PPI (Programmable Peripheral Interface)
let PPI_PPI_PA: UInt8 = 0x0x60
let PPI_PPI_PB: UInt8 = 0x0x61
let PPI_PPI_PC: UInt8 = 0x0x62
let PPI_PPI_CTRL: UInt8 = 0x0x63

// MARK: - DMA (Direct Memory Access Controller)
let DMA_DMA_CH0_ADDR: UInt16 = 0x0x00
let DMA_DMA_CH0_COUNT: UInt16 = 0x0x01
let DMA_DMA_CMD: UInt8 = 0x0x08
let DMA_DMA_MASK: UInt8 = 0x0x0A
let DMA_DMA_MODE: UInt8 = 0x0x0B

// MARK: - CGA (Color Graphics Adapter)
let CGA_CGA_INDEX: UInt8 = 0x0x3D4
let CGA_CGA_DATA: UInt8 = 0x0x3D5
let CGA_CGA_MODE: UInt8 = 0x0x3D8
let CGA_CGA_COLOR: UInt8 = 0x0x3D9

// MARK: - Interrupt Vectors
let IRQ_DIVIDE_ERROR: Int = 0
let IRQ_SINGLE_STEP: Int = 1
let IRQ_NMI: Int = 2
let IRQ_BREAKPOINT: Int = 3
let IRQ_OVERFLOW: Int = 4
let IRQ_PRINT_SCREEN: Int = 5
let IRQ_IRQ0: Int = 8
let IRQ_IRQ1: Int = 9
let IRQ_IRQ2: Int = 10
let IRQ_IRQ3: Int = 11
let IRQ_IRQ4: Int = 12
let IRQ_IRQ5: Int = 13
let IRQ_IRQ6: Int = 14
let IRQ_IRQ7: Int = 15
let IRQ_IRQ8: Int = 16
let IRQ_IRQ11: Int = 19
let IRQ_IRQ13: Int = 21
let IRQ_IRQ15: Int = 31

// MARK: - Memory Segments
let MEM_BIOS: (start: UInt32, size: UInt32) = (0x0xF0000, 65536)
let MEM_VIDEO: (start: UInt32, size: UInt32) = (0x0xB8000, 32768)
let MEM_CONVENTIONAL: (start: UInt32, size: UInt32) = (0x0x00000, 640)
let MEM_EXTENDED: (start: UInt32, size: UInt32) = (0x0x100000, 64)

// MARK: - Device Functions
func ibm_pc_5150_init() {
}

func read_register(_ addr: UInt32) -> UInt32 {
    return 0
}

func write_register(_ addr: UInt32, _ value: UInt32) {
}
