//
// Sharp-LR35902 Register Definitions
// Generated from: Game Boy (DMG-01) main processor - Sharp LR35902 (Z80-like) @ 4.19MHz
// Version: 1.0
// Date: 2026-04-16
//

import Foundation

// MARK: - PPU (LCD Controller / Picture Processing Unit)
let PPU_LCDC: UInt8 = 0x0xFF40
let PPU_STAT: UInt8 = 0x0xFF41
let PPU_SCY: UInt8 = 0x0xFF42
let PPU_SCX: UInt8 = 0x0xFF43
let PPU_LY: UInt8 = 0x0xFF44
let PPU_LYC: UInt8 = 0x0xFF45
let PPU_DMA: UInt8 = 0x0xFF46
let PPU_BGP: UInt8 = 0x0xFF47
let PPU_OBP0: UInt8 = 0x0xFF48
let PPU_OBP1: UInt8 = 0x0xFF49
let PPU_WY: UInt8 = 0x0xFF4A
let PPU_WX: UInt8 = 0x0xFF4B

// MARK: - apu (Audio Processing Unit)
let apu_NR10: UInt8 = 0x0xFF10
let apu_NR11: UInt8 = 0x0xFF11
let apu_NR12: UInt8 = 0x0xFF12
let apu_NR13: UInt8 = 0x0xFF13
let apu_NR14: UInt8 = 0x0xFF14
let apu_NR21: UInt8 = 0x0xFF16
let apu_NR22: UInt8 = 0x0xFF17
let apu_NR23: UInt8 = 0x0xFF18
let apu_NR24: UInt8 = 0x0xFF19
let apu_NR30: UInt8 = 0x0xFF1A
let apu_NR31: UInt8 = 0x0xFF1B
let apu_NR32: UInt8 = 0x0xFF1C
let apu_NR33: UInt8 = 0x0xFF1D
let apu_NR34: UInt8 = 0x0xFF1E
let apu_NR41: UInt8 = 0x0xFF20
let apu_NR42: UInt8 = 0x0xFF21
let apu_NR43: UInt8 = 0x0xFF22
let apu_NR44: UInt8 = 0x0xFF23
let apu_NR50: UInt8 = 0x0xFF24
let apu_NR51: UInt8 = 0x0xFF25
let apu_NR52: UInt8 = 0x0xFF26

// MARK: - TIMER (Timer Unit)
let TIMER_DIV: UInt8 = 0x0xFF04
let TIMER_TIMA: UInt8 = 0x0xFF05
let TIMER_TMA: UInt8 = 0x0xFF06
let TIMER_TAC: UInt8 = 0x0xFF07

// MARK: - JOYPAD (Joypad Controller)
let JOYPAD_P1: UInt8 = 0x0xFF00

// MARK: - SERIAL (Serial I/O (Link Cable))
let SERIAL_SB: UInt8 = 0x0xFF01
let SERIAL_SC: UInt8 = 0x0xFF02

// MARK: - INTERRUPT (Interrupt Flag Register)
let INTERRUPT_IF: UInt8 = 0x0xFF0F

// MARK: - IE (Interrupt Enable Register)
let IE_IE: UInt8 = 0x0xFFFF

// MARK: - Interrupt Vectors
let IRQ_VBLANK: Int = 0
let IRQ_LCDC_STATUS: Int = 1
let IRQ_TIMER_OVERFLOW: Int = 2
let IRQ_SERIAL_COMPLETE: Int = 3
let IRQ_JOYPAD: Int = 4

// MARK: - Memory Segments
let MEM_wram: (start: UInt32, size: UInt32) = (0x0xC000, 4096)
let MEM_wram_shadow: (start: UInt32, size: UInt32) = (0x0xE000, 4096)
let MEM_hram: (start: UInt32, size: UInt32) = (0x0xFF80, 127)
let MEM_io_registers: (start: UInt32, size: UInt32) = (0x0xFF00, 128)
let MEM_oam: (start: UInt32, size: UInt32) = (0x0xFE00, 160)
let MEM_vram: (start: UInt32, size: UInt32) = (0x0x8000, 8192)
let MEM_bg_map_1: (start: UInt32, size: UInt32) = (0x0x9800, 1024)
let MEM_bg_map_2: (start: UInt32, size: UInt32) = (0x0x9C00, 1024)
let MEM_rom_bank0: (start: UInt32, size: UInt32) = (0x0x0000, 16384)
let MEM_rom_bank1: (start: UInt32, size: UInt32) = (0x0x4000, 16384)
let MEM_cart_ram: (start: UInt32, size: UInt32) = (0x0xA000, 8192)

// MARK: - Device Functions
func sharp_lr35902_init() {
}

func read_register(_ addr: UInt32) -> UInt32 {
    return 0
}

func write_register(_ addr: UInt32, _ value: UInt32) {
}
