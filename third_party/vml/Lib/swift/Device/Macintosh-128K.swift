//
// Macintosh-128K Register Definitions
// Generated from: Original Macintosh 128K with Motorola 68000 CPU, 128KB RAM, and 9-inch monochrome display
// Version: 1.0
// Date: 2026-04-17
//

import Foundation

// MARK: - VIA (Versatile Interface Adapter (6522))
let VIA_VIA_ORB: UInt8 = 0x0xE80000
let VIA_VIA_ORA: UInt8 = 0x0xE80001
let VIA_VIA_DDRB: UInt8 = 0x0xE80002
let VIA_VIA_DDRA: UInt8 = 0x0xE80003
let VIA_VIA_T1CL: UInt8 = 0x0xE80004
let VIA_VIA_T1CH: UInt8 = 0x0xE80005
let VIA_VIA_T1LL: UInt8 = 0x0xE80006
let VIA_VIA_T1LH: UInt8 = 0x0xE80007
let VIA_VIA_T2CL: UInt8 = 0x0xE80008
let VIA_VIA_T2CH: UInt8 = 0x0xE80009
let VIA_VIA_SR: UInt8 = 0x0xE8000A
let VIA_VIA_ACR: UInt8 = 0x0xE8000B
let VIA_VIA_PCR: UInt8 = 0x0xE8000C
let VIA_VIA_IFR: UInt8 = 0x0xE8000D
let VIA_VIA_IER: UInt8 = 0x0xE8000E
let VIA_VIA_ORA2: UInt8 = 0x0xE8000F

// MARK: - IWM (Integrated Woz Machine (floppy controller))
let IWM_IWM_Q6: UInt8 = 0x0xD00000
let IWM_IWM_Q7: UInt8 = 0x0xD00002
let IWM_IWM_PH0: UInt8 = 0x0xD00004
let IWM_IWM_PH1: UInt8 = 0x0xD00006
let IWM_IWM_PH2: UInt8 = 0x0xD00008
let IWM_IWM_PH3: UInt8 = 0x0xD0000A

// MARK: - SCC (Zilog 8530 Serial Communications Controller)
let SCC_SCC_CA: UInt8 = 0x0x500000
let SCC_SCC_DA: UInt8 = 0x0x500002
let SCC_SCC_CB: UInt8 = 0x0x500004
let SCC_SCC_DB: UInt8 = 0x0x500006

// MARK: - Sound (Built-in speaker)
let Sound_SOUND_VOL: UInt8 = 0x0xE80100
let Sound_SOUND_FREQ: UInt8 = 0x0xE80102

// MARK: - Interrupt Vectors
let IRQ_RESET_SP: Int = 0
let IRQ_RESET_PC: Int = 4
let IRQ_AUTOVECTOR1: Int = 24
let IRQ_AUTOVECTOR2: Int = 25
let IRQ_AUTOVECTOR3: Int = 26
let IRQ_AUTOVECTOR4: Int = 27
let IRQ_AUTOVECTOR5: Int = 28
let IRQ_AUTOVECTOR6: Int = 29
let IRQ_AUTOVECTOR7: Int = 30
let IRQ_SPURIOUS: Int = 31

// MARK: - Memory Segments

// MARK: - Device Functions
func macintosh_128k_init() {
}

func read_register(_ addr: UInt32) -> UInt32 {
    return 0
}

func write_register(_ addr: UInt32, _ value: UInt32) {
}
