//
// Nintendo Entertainment System Register Definitions
// Generated from: Nintendo Entertainment System (NES/Famicom) 8-bit video game console
// Version: 
// Date: 
//

import Foundation

// MARK: - PPU (Picture Processing Unit (Ricoh 2C02))
let PPU_PPUCTRL: UInt64 = 0x0x2000
let PPU_PPUMASK: UInt64 = 0x0x2001
let PPU_PPUSTATUS: UInt64 = 0x0x2002
let PPU_OAMADDR: UInt64 = 0x0x2003
let PPU_OAMDATA: UInt64 = 0x0x2004
let PPU_PPUSCROLL: UInt64 = 0x0x2005
let PPU_PPUADDR: UInt64 = 0x0x2006
let PPU_PPUDATA: UInt64 = 0x0x2007
let PPU_OAMDMA: UInt64 = 0x0x4014

// MARK: - APU (Audio Processing Unit (Ricoh 2A03))
let APU_SQ1_VOL: UInt64 = 0x0x4000
let APU_SQ1_SWEEP: UInt64 = 0x0x4001
let APU_SQ1_LO: UInt64 = 0x0x4002
let APU_SQ1_HI: UInt64 = 0x0x4003
let APU_SQ2_VOL: UInt64 = 0x0x4004
let APU_SQ2_SWEEP: UInt64 = 0x0x4005
let APU_SQ2_LO: UInt64 = 0x0x4006
let APU_SQ2_HI: UInt64 = 0x0x4007
let APU_TRI_LINEAR: UInt64 = 0x0x4008
let APU_TRI_LO: UInt64 = 0x0x400A
let APU_TRI_HI: UInt64 = 0x0x400B
let APU_NOISE_VOL: UInt64 = 0x0x400C
let APU_NOISE_LO: UInt64 = 0x0x400E
let APU_NOISE_HI: UInt64 = 0x0x400F
let APU_DMC_FREQ: UInt64 = 0x0x4010
let APU_DMC_RAW: UInt64 = 0x0x4011
let APU_DMC_START: UInt64 = 0x0x4012
let APU_DMC_LEN: UInt64 = 0x0x4013
let APU_OAMDMA: UInt64 = 0x0x4014
let APU_APUSTATUS: UInt64 = 0x0x4015
let APU_APUFRAME: UInt64 = 0x0x4017

// MARK: - Controller (Controller Interface)
let Controller_JOY1: UInt64 = 0x0x4016
let Controller_JOY2: UInt64 = 0x0x4017

// MARK: - Mapper (Memory Mapper (Cartridge))
let Mapper_PRGROM: UInt32 = 0x0
let Mapper_CHRROM: UInt32 = 0x0
let Mapper_PRGRAM: UInt32 = 0x0
let Mapper_CHRRAM: UInt32 = 0x0

// MARK: - Interrupt Vectors
let IRQ_NMI: Int = 65530
let IRQ_RESET: Int = 65532
let IRQ_IRQ: Int = 65534

// MARK: - Memory Segments

// MARK: - Device Functions
func nintendo_entertainment_system_init() {
}

func read_register(_ addr: UInt32) -> UInt32 {
    return 0
}

func write_register(_ addr: UInt32, _ value: UInt32) {
}
