//
// Apple-IIe Register Definitions
// Generated from: Apple II Enhanced - 8-bit personal computer with MOS 6502 CPU
// Version: 1.0
// Date: 2026-04-17
//

import Foundation

// MARK: - VIA (Versatile Interface Adapter (6522))
let VIA_ORB: UInt8 = 0x0xC000
let VIA_ORA: UInt8 = 0x0xC001
let VIA_DDRB: UInt8 = 0x0xC002
let VIA_DDRA: UInt8 = 0x0xC003
let VIA_T1C: UInt16 = 0x0xC004
let VIA_T1L: UInt16 = 0x0xC006
let VIA_T2C: UInt16 = 0x0xC008
let VIA_SR: UInt8 = 0x0xC00A
let VIA_ACR: UInt8 = 0x0xC00B
let VIA_PCR: UInt8 = 0x0xC00C
let VIA_IFG: UInt8 = 0x0xC00D
let VIA_IER: UInt8 = 0x0xC00E
let VIA_ORA_NH: UInt8 = 0x0xC00F

// MARK: - PIA (Peripheral Interface Adapter (6520))
let PIA_PA: UInt8 = 0x0xC010
let PIA_PB: UInt8 = 0x0xC011
let PIA_DDRA: UInt8 = 0x0xC012
let PIA_DDRB: UInt8 = 0x0xC013
let PIA_CA1: UInt8 = 0x0xC014
let PIA_CA2: UInt8 = 0x0xC015
let PIA_CB1: UInt8 = 0x0xC016
let PIA_CB2: UInt8 = 0x0xC017

// MARK: - KBD (Keyboard (via PIA))
let KBD_KEYDATA: UInt8 = 0x0xC000
let KBD_KEYSTROBE: UInt8 = 0x0xC010
let KBD_KBDCTRL: UInt8 = 0x0xC025
let KBD_KBDERR: UInt8 = 0x0xC026

// MARK: - SPEAKER (Speaker)
let SPEAKER_SPKR: UInt8 = 0x0xC030

// MARK: - GAME_PORT (Game I/O Port)
let GAME_PORT_GAME_SW0: UInt8 = 0x0xC061
let GAME_PORT_GAME_SW1: UInt8 = 0x0xC062
let GAME_PORT_GAME_AN0: UInt8 = 0x0xC064
let GAME_PORT_GAME_AN1: UInt8 = 0x0xC065
let GAME_PORT_GAME_AN2: UInt8 = 0x0xC066
let GAME_PORT_GAME_AN3: UInt8 = 0x0xC067
let GAME_PORT_GAME_TRIG: UInt8 = 0x0xC070

// MARK: - DISKII (Disk II Controller)
let DISKII_PHASE0: UInt8 = 0x0xC0E0
let DISKII_PHASE1: UInt8 = 0x0xC0E1
let DISKII_PHASE2: UInt8 = 0x0xC0E2
let DISKII_PHASE3: UInt8 = 0x0xC0E3
let DISKII_Q6L: UInt8 = 0x0xC0EC
let DISKII_Q7L: UInt8 = 0x0xC0ED
let DISKII_Q6R: UInt8 = 0x0xC0EE
let DISKII_Q7R: UInt8 = 0x0xC0EF

// MARK: - VIDEO (Video Display Generator)
let VIDEO_TXTCLR: UInt8 = 0x0xC050
let VIDEO_MIXCLR: UInt8 = 0x0xC051
let VIDEO_TXTPAGE2: UInt8 = 0x0xC054
let VIDEO_TXTPAGE1: UInt8 = 0x0xC055
let VIDEO_LORES: UInt8 = 0x0xC056
let VIDEO_HIRES: UInt8 = 0x0xC057
let VIDEO_DHIRESON: UInt8 = 0x0xC05E
let VIDEO_AN0: UInt8 = 0x0xC058
let VIDEO_AN1: UInt8 = 0x0xC059
let VIDEO_AN2: UInt8 = 0x0xC05A
let VIDEO_AN3: UInt8 = 0x0xC05B
let VIDEO_80STORE: UInt8 = 0x0xC000

// MARK: - RAMRD (RAM Read/Write Control)
let RAMRD_INTCXROM: UInt8 = 0x0xCFFF

// MARK: - Interrupt Vectors
let IRQ_RESET: Int = 0
let IRQ_NMI: Int = 1
let IRQ_IRQ: Int = 2
let IRQ_BRK: Int = 3

// MARK: - Memory Segments
let MEM_main_ram: (start: UInt32, size: UInt32) = (0x0x0000, 49152)
let MEM_text_ram: (start: UInt32, size: UInt32) = (0x0x0400, 1024)
let MEM_hires_ram: (start: UInt32, size: UInt32) = (0x0x2000, 16384)
let MEM_aux_ram: (start: UInt32, size: UInt32) = (0x0x0400, 1536)
let MEM_monitor_rom: (start: UInt32, size: UInt32) = (0x0xC100, 3840)
let MEM_basic_rom: (start: UInt32, size: UInt32) = (0x0xD000, 12288)
let MEM_slot_rom: (start: UInt32, size: UInt32) = (0x0xC100, 768)
let MEM_mmio: (start: UInt32, size: UInt32) = (0x0xC080, 128)

// MARK: - Device Functions
func apple_iie_init() {
}

func read_register(_ addr: UInt32) -> UInt32 {
    return 0
}

func write_register(_ addr: UInt32, _ value: UInt32) {
}
