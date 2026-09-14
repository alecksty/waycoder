//
// Amstrad-CPC-464 Register Definitions
// Generated from: Amstrad CPC 464 - British 8-bit home computer with Z80 CPU and built-in cassette recorder
// Version: 1.0
// Date: 2026-04-17
//

import Foundation

// MARK: - GA (Gate Array - Custom ASIC (video/sound/RAM control))
let GA_GA_MR: UInt8 = 0x0x7F00
let GA_GA_IR: UInt8 = 0x0x7F01
let GA_GA_R1: UInt8 = 0x0x7F02
let GA_GA_R2: UInt8 = 0x0x7F03
let GA_GA_R3: UInt8 = 0x0x7F04
let GA_GA_R4: UInt8 = 0x0x7F05
let GA_GA_R5: UInt8 = 0x0x7F06
let GA_GA_R6: UInt8 = 0x0x7F07
let GA_GA_R7: UInt8 = 0x0x7F08

// MARK: - CRTC (CRT Controller 6845 - Video timing)
let CRTC_CRTC_REG: UInt8 = 0x0xBC00
let CRTC_CRTC_DATA: UInt8 = 0x0xBD00
let CRTC_CRTC_H_TOTAL: UInt8 = 0x0xBC01
let CRTC_CRTC_H_DISP: UInt8 = 0x0xBC02
let CRTC_CRTC_HSYNC_POS: UInt8 = 0x0xBC03
let CRTC_CRTC_HSYNC_WIDTH: UInt8 = 0x0xBC04
let CRTC_CRTC_V_TOTAL: UInt8 = 0x0xBC05
let CRTC_CRTC_V_TOTAL_ADJ: UInt8 = 0x0xBC06
let CRTC_CRTC_V_DISP: UInt8 = 0x0xBC07
let CRTC_CRTC_VSYNC_POS: UInt8 = 0x0xBC08
let CRTC_CRTC_INTERLACE: UInt8 = 0x0xBC09
let CRTC_CRTC_CURSOR_START: UInt8 = 0x0xBC0A
let CRTC_CRTC_CURSOR_END: UInt8 = 0x0xBC0B
let CRTC_CRTC_SA_HI: UInt8 = 0x0xBC0C
let CRTC_CRTC_SA_LO: UInt8 = 0x0xBC0D
let CRTC_CRTC_CURSOR_HI: UInt8 = 0x0xBC0E
let CRTC_CRTC_CURSOR_LO: UInt8 = 0x0xBC0F

// MARK: - PSG (AY-3-8912 Programmable Sound Generator)
let PSG_PSG_REG: UInt8 = 0x0xF400
let PSG_PSG_DATA: UInt8 = 0x0xF600
let PSG_FREQ_A_LO: UInt8 = 0x0xF400
let PSG_FREQ_A_HI: UInt8 = 0x0xF401
let PSG_FREQ_B_LO: UInt8 = 0x0xF402
let PSG_FREQ_B_HI: UInt8 = 0x0xF403
let PSG_FREQ_C_LO: UInt8 = 0x0xF404
let PSG_FREQ_C_HI: UInt8 = 0x0xF405
let PSG_NOISE_FREQ: UInt8 = 0x0xF406
let PSG_ENABLE: UInt8 = 0x0xF407
let PSG_VOL_A: UInt8 = 0x0xF408
let PSG_VOL_B: UInt8 = 0x0xF409
let PSG_VOL_C: UInt8 = 0x0xF40A
let PSG_ENV_FREQ_LO: UInt8 = 0x0xF40B
let PSG_ENV_FREQ_HI: UInt8 = 0x0xF40C
let PSG_ENV_SHAPE: UInt8 = 0x0xF40D
let PSG_PORT_A: UInt8 = 0x0xF40E
let PSG_PORT_B: UInt8 = 0x0xF40F

// MARK: - FDC (WD1772 Floppy Disk Controller (via expansion))
let FDC_FDC_STATUS: UInt8 = 0x0xF8E0
let FDC_FDC_COMMAND: UInt8 = 0x0xF8E0
let FDC_FDC_TRACK: UInt8 = 0x0xF8E1
let FDC_FDC_SECTOR: UInt8 = 0x0xF8E2
let FDC_FDC_DATA: UInt8 = 0x0xF8E3

// MARK: - PRINTER (Centronics Parallel Printer Port)
let PRINTER_PRN_DATA: UInt8 = 0x0xEE
let PRINTER_PRN_STROBE: UInt8 = 0x0xEF

// MARK: - Interrupt Vectors
let IRQ_RESET: Int = 0
let IRQ_NMI: Int = 1
let IRQ_INT: Int = 2

// MARK: - Memory Segments
let MEM_lower_rom: (start: UInt32, size: UInt32) = (0x0x0000, 16384)
let MEM_ram_bank0: (start: UInt32, size: UInt32) = (0x0x0000, 16384)
let MEM_ram_main: (start: UInt32, size: UInt32) = (0x0x4000, 32768)
let MEM_upper_rom: (start: UInt32, size: UInt32) = (0x0xC000, 16384)

// MARK: - Device Functions
func amstrad_cpc_464_init() {
}

func read_register(_ addr: UInt32) -> UInt32 {
    return 0
}

func write_register(_ addr: UInt32, _ value: UInt32) {
}
