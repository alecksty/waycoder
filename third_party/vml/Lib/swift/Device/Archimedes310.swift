//
// Acorn-Archimedes-A310 Register Definitions
// Generated from: Acorn Archimedes A310 - First ARM-based home computer with RISC OS, ARM250 @ 26MHz
// Version: 1.0
// Date: 2026-04-17
//

import Foundation

// MARK: - IOC (I/O Controller (IOC) - Interrupt/Keyboard/RTC)
let IOC_IOC_TIMER1: UInt32 = 0x0x03000000
let IOC_IOC_TIMER2: UInt32 = 0x0x03000004
let IOC_IOC_IOSEL: UInt32 = 0x0x03000008
let IOC_IOC_IRQST: UInt32 = 0x0x0300000C
let IOC_IOC_IRQLATCH: UInt32 = 0x0x03000010
let IOC_IOC_FIQST: UInt32 = 0x0x03000014
let IOC_IOC_FIQEN: UInt32 = 0x0x03000018
let IOC_IOC_IRQEN: UInt32 = 0x0x0300001C
let IOC_IOC_KBDDATA: UInt32 = 0x0x03000020
let IOC_IOC_KBDCR: UInt32 = 0x0x03000024
let IOC_IOC_RTCDR: UInt32 = 0x0x03000028
let IOC_IOC_RTCCR: UInt32 = 0x0x0300002C
let IOC_IOC_PRST: UInt32 = 0x0x03000030
let IOC_IOC_PORTA: UInt32 = 0x0x03000034
let IOC_IOC_PORTB: UInt32 = 0x0x03000038
let IOC_IOC_PORTC: UInt32 = 0x0x0300003C

// MARK: - MEMC (Memory Controller (MEMC1))
let MEMC_MEMC_PT: UInt32 = 0x0x03200000
let MEMC_MEMC_CTRL: UInt32 = 0x0x03200004
let MEMC_MEMC_DRAM: UInt32 = 0x0x03200008
let MEMC_MEMC_ERR: UInt32 = 0x0x0320000C

// MARK: - VIDC (Video Controller - VIDC1)
let VIDC_VIDC_PALETTE: UInt32 = 0x0x03400000
let VIDC_VIDC_STARTL: UInt32 = 0x0x03400004
let VIDC_VIDC_STARTH: UInt32 = 0x0x03400008
let VIDC_VIDC_CONFIG: UInt32 = 0x0x0340000C
let VIDC_VIDC_HDISP: UInt32 = 0x0x03400010
let VIDC_VIDC_VDISP: UInt32 = 0x0x03400014
let VIDC_VIDC_HSYNC: UInt32 = 0x0x03400018
let VIDC_VIDC_VSYNC: UInt32 = 0x0x0340001C
let VIDC_VIDC_BORDER: UInt32 = 0x0x03400020
let VIDC_VIDC_CURSOR: UInt32 = 0x0x03400024
let VIDC_VIDC_SOUND: UInt32 = 0x0x03400028

// MARK: - FDC (Intel 82710 Floppy Disk Controller)
let FDC_FDC_STATUS: UInt8 = 0x0x03010000
let FDC_FDC_COMMAND: UInt8 = 0x0x03010000
let FDC_FDC_TRACK: UInt8 = 0x0x03010004
let FDC_FDC_SECTOR: UInt8 = 0x0x03010008
let FDC_FDC_DATA: UInt8 = 0x0x0301000C

// MARK: - SERIAL (Serial Port (via IOC))
let SERIAL_SERIAL_TX: UInt8 = 0x0x03010010
let SERIAL_SERIAL_RX: UInt8 = 0x0x03010014
let SERIAL_SERIAL_CTRL: UInt8 = 0x0x03010018

// MARK: - Interrupt Vectors
let IRQ_RESET: Int = 0
let IRQ_UND: Int = 1
let IRQ_SWI: Int = 2
let IRQ_PABORT: Int = 3
let IRQ_DABORT: Int = 4
let IRQ_ADDRESS: Int = 5
let IRQ_IRQ: Int = 6
let IRQ_FIQ: Int = 7

// MARK: - Memory Segments
let MEM_rom: (start: UInt32, size: UInt32) = (0x0x00000000, 524288)
let MEM_ram: (start: UInt32, size: UInt32) = (0x0x00080000, 3932160)
let MEM_vram: (start: UInt32, size: UInt32) = (0x0x00400000, 4194304)
let MEM_io: (start: UInt32, size: UInt32) = (0x0x03000000, 131072)
let MEM_memc: (start: UInt32, size: UInt32) = (0x0x03200000, 4096)
let MEM_vidc: (start: UInt32, size: UInt32) = (0x0x03400000, 4096)
let MEM_iomd: (start: UInt32, size: UInt32) = (0x0x03300000, 4096)

// MARK: - Device Functions
func acorn_archimedes_a310_init() {
}

func read_register(_ addr: UInt32) -> UInt32 {
    return 0
}

func write_register(_ addr: UInt32, _ value: UInt32) {
}
