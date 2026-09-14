//
// NeoGeo-68000 Register Definitions
// Generated from: SNK Neo Geo AES main processor - Motorola 68000 @ 12MHz + Z80 @ 4MHz (audio coprocessor)
// Version: 1.0
// Date: 2026-04-16
//

import Foundation

// MARK: - Z80 (Z80 Audio Coprocessor @ 4MHz)
let Z80_Z80_A: UInt8 = 0x0x00
let Z80_Z80_F: UInt8 = 0x0x01
let Z80_Z80_B: UInt8 = 0x0x02
let Z80_Z80_C: UInt8 = 0x0x03
let Z80_Z80_D: UInt8 = 0x0x04
let Z80_Z80_E: UInt8 = 0x0x05
let Z80_Z80_H: UInt8 = 0x0x06
let Z80_Z80_L: UInt8 = 0x0x07
let Z80_Z80_AF_: UInt16 = 0x0x08
let Z80_Z80_BC_: UInt16 = 0x0x0A
let Z80_Z80_DE_: UInt16 = 0x0x0C
let Z80_Z80_HL_: UInt16 = 0x0x0E
let Z80_Z80_IX: UInt16 = 0x0x10
let Z80_Z80_IY: UInt16 = 0x0x12
let Z80_Z80_SP: UInt16 = 0x0x14
let Z80_Z80_PC: UInt16 = 0x0x16
let Z80_Z80_I: UInt8 = 0x0x18
let Z80_Z80_R: UInt8 = 0x0x19
let Z80_Z80_IM: UInt8 = 0x0x1A
let Z80_Z80_BUSREQ: UInt8 = 0x0x1E
let Z80_Z80_RESET: UInt8 = 0x0x1F

// MARK: - YM2610 (Yamaha YM2610 FM + ADPCM Audio Generator)
let YM2610_YM_ADDR_A0: UInt8 = 0x0x00
let YM2610_YM_DATA_A0: UInt8 = 0x0x01
let YM2610_YM_ADDR_A1: UInt8 = 0x0x02
let YM2610_YM_DATA_A1: UInt8 = 0x0x03
let YM2610_YM_ADDR_B0: UInt8 = 0x0x04
let YM2610_YM_DATA_B0: UInt8 = 0x0x05
let YM2610_YM_TEST: UInt8 = 0x0x08
let YM2610_YM_FM_CH0_FREQ_L: UInt8 = 0x0xA0
let YM2610_YM_FM_CH0_FREQ_H: UInt8 = 0x0xA4
let YM2610_YM_FM_CH1_FREQ_L: UInt8 = 0x0xA1
let YM2610_YM_FM_CH1_FREQ_H: UInt8 = 0x0xA5
let YM2610_YM_FM_CH2_FREQ_L: UInt8 = 0x0xA2
let YM2610_YM_FM_CH2_FREQ_H: UInt8 = 0x0xA6
let YM2610_YM_FM_CH3_FREQ_L: UInt8 = 0x0xA3
let YM2610_YM_FM_CH3_FREQ_H: UInt8 = 0x0xA7
let YM2610_YM_FM_KEY_ON: UInt8 = 0x0x28
let YM2610_YM_FM_CH0_ALG: UInt8 = 0x0xB0
let YM2610_YM_FM_CH1_ALG: UInt8 = 0x0xB1
let YM2610_YM_FM_CH2_ALG: UInt8 = 0x0xB2
let YM2610_YM_FM_CH3_ALG: UInt8 = 0x0xB3
let YM2610_YM_FM_TIMER_H: UInt8 = 0x0x24
let YM2610_YM_FM_TIMER_L: UInt8 = 0x0x25
let YM2610_YM_FM_TIMER_CTRL: UInt8 = 0x0x27
let YM2610_YM_FM_CH0_DETune: UInt8 = 0x0x30
let YM2610_YM_FM_CH0_MUL: UInt8 = 0x0x30
let YM2610_YM_FM_CH0_TL: UInt8 = 0x0x40
let YM2610_YM_FM_CH0_KS_AR: UInt8 = 0x0x50
let YM2610_YM_FM_CH0_AM_DR: UInt8 = 0x0x60
let YM2610_YM_FM_CH0_SR: UInt8 = 0x0x70
let YM2610_YM_FM_CH0_RR_SL: UInt8 = 0x0x80
let YM2610_YM_FM_CH0_SSG: UInt8 = 0x0x90
let YM2610_YM_SSG_CHA_FREQ_L: UInt8 = 0x0x00
let YM2610_YM_SSG_CHA_FREQ_H: UInt8 = 0x0x01
let YM2610_YM_SSG_CHB_FREQ_L: UInt8 = 0x0x02
let YM2610_YM_SSG_CHB_FREQ_H: UInt8 = 0x0x03
let YM2610_YM_SSG_CHC_FREQ_L: UInt8 = 0x0x04
let YM2610_YM_SSG_CHC_FREQ_H: UInt8 = 0x0x05
let YM2610_YM_SSG_CHA_VOL: UInt8 = 0x0x08
let YM2610_YM_SSG_CHB_VOL: UInt8 = 0x0x09
let YM2610_YM_SSG_CHC_VOL: UInt8 = 0x0x0A
let YM2610_YM_SSG_MIXER: UInt8 = 0x0x07
let YM2610_YM_SSG_ENV_FREQ_L: UInt8 = 0x0x0B
let YM2610_YM_SSG_ENV_FREQ_H: UInt8 = 0x0x0C
let YM2610_YM_SSG_ENV_SHAPE: UInt8 = 0x0x0D
let YM2610_YM_SSG_IO_A: UInt8 = 0x0x0E
let YM2610_YM_SSG_IO_B: UInt8 = 0x0x0F
let YM2610_YM_ADPCM_STATUS: UInt8 = 0x0x10
let YM2610_YM_ADPCM_START: UInt8 = 0x0x11
let YM2610_YM_ADPCM_END: UInt8 = 0x0x12
let YM2610_YM_ADPCM_VOL_L: UInt8 = 0x0x13
let YM2610_YM_ADPCM_VOL_R: UInt8 = 0x0x14
let YM2610_YM_DELTA_N_L: UInt8 = 0x0x15
let YM2610_YM_DELTA_N_H: UInt8 = 0x0x16
let YM2610_YM_ADPCM_B_START: UInt8 = 0x0x18
let YM2610_YM_ADPCM_B_END: UInt8 = 0x0x19
let YM2610_YM_ADPCM_B_VOL: UInt8 = 0x0x1A
let YM2610_YM_ADPCM_B_CTRL: UInt8 = 0x0x1B

// MARK: - YGV628 (Neo Geo VDP (Video Display Processor))
let YGV628_VRAM_ADDR_L: UInt8 = 0x0x00
let YGV628_VRAM_ADDR_H: UInt8 = 0x0x01
let YGV628_VRAM_DATA: UInt8 = 0x0x02
let YGV628_VRAM_READ: UInt8 = 0x0x03
let YGV628_CRAM_ADDR: UInt8 = 0x0x04
let YGV628_CRAM_DATA: UInt8 = 0x0x05
let YGV628_VDP_STATUS: UInt8 = 0x0x06
let YGV628_VDP_CTRL: UInt8 = 0x0x07
let YGV628_SCROLL1_BASE: UInt16 = 0x0x08
let YGV628_SCROLL2_BASE: UInt16 = 0x0x0A
let YGV628_SPR_BASE: UInt16 = 0x0x0C
let YGV628_SPR_COUNT: UInt8 = 0x0x0E
let YGV628_WINDOW_X: UInt8 = 0x0x10
let YGV628_WINDOW_Y: UInt8 = 0x0x11
let YGV628_WINDOW_W: UInt8 = 0x0x12
let YGV628_WINDOW_H: UInt8 = 0x0x13
let YGV628_LINE_SCROLL_L: UInt8 = 0x0x14
let YGV628_LINE_SCROLL_H: UInt8 = 0x0x15
let YGV628_RASTER_COMP: UInt8 = 0x0x16
let YGV628_H_TIMING: UInt8 = 0x0x18
let YGV628_V_TIMING: UInt8 = 0x0x19
let YGV628_DMA_SRC_L: UInt8 = 0x0x1A
let YGV628_DMA_SRC_H: UInt8 = 0x0x1B
let YGV628_DMA_SRC_B: UInt8 = 0x0x1C
let YGV628_DMA_DEST_L: UInt8 = 0x0x1D
let YGV628_DMA_DEST_H: UInt8 = 0x0x1E
let YGV628_DMA_COUNT: UInt16 = 0x0x1F

// MARK: - NEODRIVER (Neo Geo System Driver / Controller)
let NEODRIVER_PDI0: UInt8 = 0x0x00
let NEODRIVER_PDI1: UInt8 = 0x0x01
let NEODRIVER_PDI2: UInt8 = 0x0x02
let NEODRIVER_PDI3: UInt8 = 0x0x03
let NEODRIVER_PDO0: UInt8 = 0x0x04
let NEODRIVER_PDO1: UInt8 = 0x0x05
let NEODRIVER_PDO2: UInt8 = 0x0x06
let NEODRIVER_PDO3: UInt8 = 0x0x07
let NEODRIVER_DIPSEL1: UInt8 = 0x0x08
let NEODRIVER_DIPSEL2: UInt8 = 0x0x09
let NEODRIVER_DIPSEL3: UInt8 = 0x0x0A
let NEODRIVER_DIPSEL4: UInt8 = 0x0x0B
let NEODRIVER_SYSCTRL: UInt8 = 0x0x0C
let NEODRIVER_IRQMASK: UInt8 = 0x0x0D
let NEODRIVER_IRQFLAG: UInt8 = 0x0x0E
let NEODRIVER_SECAM_MODE: UInt8 = 0x0x0F

// MARK: - CONTROLLER1 (Controller Port 1)
let CONTROLLER1_PDI0: UInt8 = 0x0x00

// MARK: - CONTROLLER2 (Controller Port 2)
let CONTROLLER2_PDI1: UInt8 = 0x0x00

// MARK: - MEMORY_CARD (Memory Card Interface)
let MEMORY_CARD_CARD_DATA: UInt8 = 0x0x00
let MEMORY_CARD_CARD_STATUS: UInt8 = 0x0x01
let MEMORY_CARD_CARD_CTRL: UInt8 = 0x0x02

// MARK: - CART_BANK (Cartridge Bank Switching)
let CART_BANK_BANK_REG: UInt8 = 0x0x00

// MARK: - Interrupt Vectors
let IRQ_RESET_SP: Int = 1
let IRQ_RESET_PC: Int = 2
let IRQ_BUS_ERROR: Int = 3
let IRQ_ADDRESS_ERROR: Int = 4
let IRQ_ILLEGAL_INSTR: Int = 5
let IRQ_ZERO_DIVIDE: Int = 6
let IRQ_CHK_EXCEPTION: Int = 7
let IRQ_TRAPV: Int = 8
let IRQ_PRIVILEGE: Int = 9
let IRQ_TRACE: Int = 10
let IRQ_LINE_A: Int = 11
let IRQ_LINE_F: Int = 12
let IRQ_IRQ1: Int = 24
let IRQ_IRQ2: Int = 25
let IRQ_IRQ3: Int = 26
let IRQ_IRQ4: Int = 27
let IRQ_IRQ5: Int = 28
let IRQ_TRAP0: Int = 32
let IRQ_TRAP1: Int = 33

// MARK: - Memory Segments
let MEM_work_ram: (start: UInt32, size: UInt32) = (0x0x100000, 65536)
let MEM_backup_ram: (start: UInt32, size: UInt32) = (0x0x200000, 65536)
let MEM_fix_rom: (start: UInt32, size: UInt32) = (0x0x000000, 524288)
let MEM_spr_rom: (start: UInt32, size: UInt32) = (0x0x400000, 1048576)
let MEM_audio_rom: (start: UInt32, size: UInt32) = (0x0x800000, 65536)
let MEM_cart_rom: (start: UInt32, size: UInt32) = (0x0xC00000, 524288)
let MEM_io_area: (start: UInt32, size: UInt32) = (0x0x300000, 1048576)
let MEM_z80_ram: (start: UInt32, size: UInt32) = (0x0x10000, 2048)

// MARK: - Device Functions
func neogeo_68000_init() {
}

func read_register(_ addr: UInt32) -> UInt32 {
    return 0
}

func write_register(_ addr: UInt32, _ value: UInt32) {
}
