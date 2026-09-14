//
// Macintosh-128K Register Definitions
// Generated from: Apple Macintosh 128K - First Macintosh - Motorola 68000, 128KB RAM, 512x342 display
// Version: 1.0
// Date: 2026-04-17
//

import Foundation

// MARK: - VIA (Versatile Interface Adapter 6522)
let VIA_ORB: UInt8 = 0x0xE00000
let VIA_ORA: UInt8 = 0x0xE00002
let VIA_DDRB: UInt8 = 0x0xE00004
let VIA_DDRA: UInt8 = 0x0xE00006
let VIA_T1C_L: UInt16 = 0x0xE00008
let VIA_T1C_H: UInt16 = 0x0xE0000A
let VIA_T1L_L: UInt16 = 0x0xE0000C
let VIA_T1L_H: UInt16 = 0x0xE0000E
let VIA_T2C_L: UInt16 = 0x0xE00010
let VIA_T2C_H: UInt16 = 0x0xE00012
let VIA_SR: UInt8 = 0x0xE00014
let VIA_ACR: UInt8 = 0x0xE00016
let VIA_PCR: UInt8 = 0x0xE00018
let VIA_IFR: UInt8 = 0x0xE0001E
let VIA_IER: UInt8 = 0x0xE0001E

// MARK: - SCC (SCC 8530 Serial Communications Controller)
let SCC_SCC_CHA_B: UInt8 = 0x0xF00000
let SCC_SCC_CHA_C: UInt8 = 0x0xF00002
let SCC_SCC_CHB_D: UInt8 = 0x0xF00004
let SCC_SCC_CHB_CT: UInt8 = 0x0xF00006

// MARK: - IWM (Integrated Woz Machine - Floppy Disk Controller)
let IWM_IWM_DATA: UInt8 = 0x0x1E00000
let IWM_IWM_MODE: UInt8 = 0x0x1E00008
let IWM_IWM_Q6L: UInt8 = 0x0x1E00020
let IWM_IWM_Q7L: UInt8 = 0x0x1E00022
let IWM_IWM_Q6R: UInt8 = 0x0x1E00024
let IWM_IWM_Q7R: UInt8 = 0x0x1E00026

// MARK: - VGC (Video Graphics Controller (custom Apple chip))
let VGC_VGC_MODE: UInt8 = 0x0x00F20000
let VGC_VGC_START_HI: UInt8 = 0x0x00F20002
let VGC_VGC_START_LO: UInt8 = 0x0x00F20004

// MARK: - ADB (Apple Desktop Bus)
let ADB_ADB_DATA: UInt8 = 0x0x01600000
let ADB_ADB_STATUS: UInt8 = 0x0x01600004
let ADB_ADB_CMD: UInt8 = 0x0x01600008

// MARK: - Interrupt Vectors
let IRQ_RESET: Int = 1
let IRQ_RESET_PC: Int = 2
let IRQ_IRQ1: Int = 24
let IRQ_IRQ2: Int = 25
let IRQ_IRQ3: Int = 26
let IRQ_IRQ4: Int = 27

// MARK: - Memory Segments
let MEM_ram: (start: UInt32, size: UInt32) = (0x0x000000, 131072)
let MEM_rom: (start: UInt32, size: UInt32) = (0x0x40000000, 131072)
let MEM_framebuffer: (start: UInt32, size: UInt32) = (0x0x00400000, 1366)
let MEM_framebuffer2: (start: UInt32, size: UInt32) = (0x0x00410000, 1366)
let MEM_VIA: (start: UInt32, size: UInt32) = (0x0x00E00000, 4096)
let MEM_SCC: (start: UInt32, size: UInt32) = (0x0x00F00000, 4096)
let MEM_ADB: (start: UInt32, size: UInt32) = (0x0x01600000, 4096)
let MEM_IWM: (start: UInt32, size: UInt32) = (0x0x01E00000, 4096)

// MARK: - Device Functions
func macintosh_128k_init() {
}

func read_register(_ addr: UInt32) -> UInt32 {
    return 0
}

func write_register(_ addr: UInt32, _ value: UInt32) {
}
