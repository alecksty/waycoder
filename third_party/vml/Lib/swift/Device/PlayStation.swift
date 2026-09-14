//
// MIPS-R3000A Register Definitions
// Generated from: Sony PlayStation (PS1) main processor - MIPS R3000A @ 33.87MHz with R4000-like ISA
// Version: 1.0
// Date: 2026-04-16
//

import Foundation

// MARK: - GPU (Graphics Processing Unit)
let GPU_GP0_CMD: UInt32 = 0x0x00
let GPU_GP0_DATA: UInt32 = 0x0x04
let GPU_GP1_CMD: UInt32 = 0x0x08
let GPU_GP1_DATA: UInt32 = 0x0x0C
let GPU_TPAGE: UInt8 = 0x0x00
let GPU_DRAW_MODE: UInt8 = 0x0x01
let GPU_TEXTURE_WIN: UInt8 = 0x0x02
let GPU_DRAW_OFFSET_X: UInt8 = 0x0x03
let GPU_DRAW_OFFSET_Y: UInt8 = 0x0x04
let GPU_DRAW_AREA_X: UInt8 = 0x0x05
let GPU_DRAW_AREA_Y: UInt8 = 0x0x06
let GPU_DITHER: UInt8 = 0x0x07
let GPU_DISPLAY_MODE: UInt8 = 0x0x08
let GPU_DISPLAY_START_X: UInt8 = 0x0x09
let GPU_DISPLAY_START_Y: UInt8 = 0x0x0A
let GPU_DISPLAY_HORZ: UInt8 = 0x0x0B
let GPU_DISPLAY_VERT: UInt8 = 0x0x0C
let GPU_DMA_MODE: UInt8 = 0x0x0D
let GPU_GPU_STAT: UInt8 = 0x0x0C

// MARK: - GTE (Geometry Transformation Engine)
let GTE_GTE_VXY0: UInt32 = 0x0x00
let GTE_GTE_VZ0: UInt32 = 0x0x04
let GTE_GTE_VXY1: UInt32 = 0x0x08
let GTE_GTE_VZ1: UInt32 = 0x0x0C
let GTE_GTE_VXY2: UInt32 = 0x0x10
let GTE_GTE_VZ2: UInt32 = 0x0x14
let GTE_GTE_RGB0: UInt32 = 0x0x18
let GTE_GTE_RGB1: UInt32 = 0x0x1C
let GTE_GTE_RGB2: UInt32 = 0x0x20
let GTE_GTE_RTP: UInt32 = 0x0x30
let GTE_GTE_TRX: UInt32 = 0x0x34
let GTE_GTE_TRY: UInt32 = 0x0x38
let GTE_GTE_TRZ: UInt32 = 0x0x3C
let GTE_GTE_MAC0: UInt32 = 0x0x40
let GTE_GTE_MAC1: UInt32 = 0x0x44
let GTE_GTE_MAC2: UInt32 = 0x0x48
let GTE_GTE_MAC3: UInt32 = 0x0x4C
let GTE_GTE_IR0: UInt32 = 0x0x50
let GTE_GTE_IR1: UInt32 = 0x0x54
let GTE_GTE_IR2: UInt32 = 0x0x58
let GTE_GTE_IR3: UInt32 = 0x0x5C
let GTE_GTE_LZCS: UInt32 = 0x0x60
let GTE_GTE_LZCR: UInt32 = 0x0x64
let GTE_GTE_CTX: UInt32 = 0x0x68
let GTE_GTE_CTY: UInt32 = 0x0x6C
let GTE_GTE_CTZ: UInt32 = 0x0x70
let GTE_GTE_RTX: UInt32 = 0x0x74
let GTE_GTE_RTY: UInt32 = 0x0x78
let GTE_GTE_RTZ: UInt32 = 0x0x7C
let GTE_GTE_SR: UInt32 = 0x0x80
let GTE_GTE_CMD: UInt32 = 0x0x84
let GTE_GTE_H: UInt32 = 0x0x88
let GTE_GTE_DQB: UInt32 = 0x0x8C
let GTE_GTE_DQA: UInt32 = 0x0x90
let GTE_GTE_ZSF3: UInt32 = 0x0x94
let GTE_GTE_ZSF4: UInt32 = 0x0x98
let GTE_GTE_OTZ: UInt32 = 0x0x9C

// MARK: - SPU (Sound Processing Unit (24-channel ADPCM))
let SPU_SPU_CTRL: UInt16 = 0x0x00
let SPU_SPU_STAT: UInt16 = 0x0x04
let SPU_SPU_CDVOL_L: UInt16 = 0x0x08
let SPU_SPU_CDVOL_R: UInt16 = 0x0x0A
let SPU_SPU_MAINVOL_L: UInt16 = 0x0x0C
let SPU_SPU_MAINVOL_R: UInt16 = 0x0x0E
let SPU_SPU_REVERB_L: UInt16 = 0x0x10
let SPU_SPU_REVERB_R: UInt16 = 0x0x12
let SPU_SPU_KEYON: UInt16 = 0x0x80
let SPU_SPU_KEYOFF: UInt16 = 0x0x82
let SPU_SPU_CHANNEL_MUTE: UInt16 = 0x0x84
let SPU_SPU_NOISE_CLK: UInt16 = 0x0x88
let SPU_SPU_REVERB_ADDR: UInt16 = 0x0x8A
let SPU_SPU_IRQ_ADDR: UInt16 = 0x0x8C
let SPU_SPU_REVERB_VOL_L: UInt16 = 0x0x8E
let SPU_SPU_REVERB_VOL_R: UInt16 = 0x0x90
let SPU_SPU_VOICE_VOL_L: UInt8 = 0x0x00
let SPU_SPU_VOICE_VOL_R: UInt8 = 0x0x01
let SPU_SPU_VOICE_FREQ: UInt16 = 0x0x02
let SPU_SPU_VOICE_START: UInt16 = 0x0x04
let SPU_SPU_VOICE_ADSR1: UInt16 = 0x0x06
let SPU_SPU_VOICE_ADSR2: UInt16 = 0x0x08
let SPU_SPU_VOICE_ENV: UInt16 = 0x0x0A
let SPU_SPU_VOICE_REPEAT: UInt16 = 0x0x0C
let SPU_VOICE_BASE_SIZE: UInt8 = 0x0x10

// MARK: - MDEC (Motion Decoder (JPEG Decompression))
let MDEC_MDEC_CTRL: UInt32 = 0x0x00
let MDEC_MDEC_DATA: UInt32 = 0x0x04
let MDEC_MDEC_BKGD: UInt32 = 0x0x08

// MARK: - DMA (DMA Controller (7 channels))
let DMA_DMA_DPCR: UInt8 = 0x0x00
let DMA_DMA_INT: UInt8 = 0x0x04
let DMA_DMA_CH0_BASE: UInt32 = 0x0x10
let DMA_DMA_CH0_COUNT: UInt16 = 0x0x14
let DMA_DMA_CH0_CTRL: UInt8 = 0x0x18
let DMA_DMA_CH1_BASE: UInt32 = 0x0x20
let DMA_DMA_CH1_COUNT: UInt16 = 0x0x24
let DMA_DMA_CH1_CTRL: UInt8 = 0x0x28
let DMA_DMA_CH2_BASE: UInt32 = 0x0x30
let DMA_DMA_CH2_COUNT: UInt16 = 0x0x34
let DMA_DMA_CH2_CTRL: UInt8 = 0x0x38
let DMA_DMA_CH3_BASE: UInt32 = 0x0x40
let DMA_DMA_CH3_COUNT: UInt16 = 0x0x44
let DMA_DMA_CH3_CTRL: UInt8 = 0x0x48
let DMA_DMA_CH4_BASE: UInt32 = 0x0x50
let DMA_DMA_CH4_COUNT: UInt16 = 0x0x54
let DMA_DMA_CH4_CTRL: UInt8 = 0x0x58
let DMA_DMA_CH5_BASE: UInt32 = 0x0x60
let DMA_DMA_CH5_COUNT: UInt16 = 0x0x64
let DMA_DMA_CH5_CTRL: UInt8 = 0x0x68
let DMA_DMA_CH6_BASE: UInt32 = 0x0x70
let DMA_DMA_CH6_COUNT: UInt16 = 0x0x74
let DMA_DMA_CH6_CTRL: UInt8 = 0x0x78

// MARK: - TIMER (Timers (3 timers))
let TIMER_TM0_COUNT: UInt16 = 0x0x00
let TIMER_TM0_MODE: UInt16 = 0x0x04
let TIMER_TM0_TARGET: UInt16 = 0x0x08
let TIMER_TM1_COUNT: UInt16 = 0x0x10
let TIMER_TM1_MODE: UInt16 = 0x0x14
let TIMER_TM1_TARGET: UInt16 = 0x0x18
let TIMER_TM2_COUNT: UInt16 = 0x0x20
let TIMER_TM2_MODE: UInt16 = 0x0x24
let TIMER_TM2_TARGET: UInt16 = 0x0x28

// MARK: - CDROM (CD-ROM Controller)
let CDROM_CD0_DATA: UInt8 = 0x0x00
let CDROM_CD0_STATUS: UInt8 = 0x0x01
let CDROM_CD0_RESPONSE: UInt8 = 0x0x02
let CDROM_CD0_DATA1: UInt8 = 0x0x03
let CDROM_CD0_INT_FLAG: UInt8 = 0x0x04
let CDROM_CD0_VOLUME_L: UInt8 = 0x0x08
let CDROM_CD0_VOLUME_R: UInt8 = 0x0x09

// MARK: - JOY (JOY Interface)
let JOY_JOY_CTRL: UInt8 = 0x0x00
let JOY_JOY_MODE: UInt8 = 0x0x01
let JOY_JOY_BAUD: UInt8 = 0x0x02
let JOY_JOY_TX_DATA: UInt8 = 0x0x04
let JOY_JOY_RX_DATA: UInt8 = 0x0x05
let JOY_JOY_STAT: UInt8 = 0x0x06

// MARK: - SIO (SIO (Serial I/O - Memory Card))
let SIO_SIO_DATA: UInt8 = 0x0x00
let SIO_SIO_STATUS: UInt8 = 0x0x01
let SIO_SIO_MODE: UInt8 = 0x0x02
let SIO_SIO_CTRL: UInt8 = 0x0x03
let SIO_SIO_BAUD: UInt8 = 0x0x04

// MARK: - INTERRUPT (Interrupt Controller)
let INTERRUPT_INT_STAT: UInt16 = 0x0x00
let INTERRUPT_INT_MASK: UInt16 = 0x0x04

// MARK: - Interrupt Vectors
let IRQ_VBLANK: Int = 0
let IRQ_GPU: Int = 1
let IRQ_CDROM: Int = 2
let IRQ_DMA0: Int = 3
let IRQ_DMA1: Int = 4
let IRQ_DMA2: Int = 5
let IRQ_DMA3: Int = 6
let IRQ_DMA4: Int = 7
let IRQ_DMA5: Int = 8
let IRQ_DMA6: Int = 9
let IRQ_TIMER0: Int = 10
let IRQ_TIMER1: Int = 11
let IRQ_TIMER2: Int = 12
let IRQ_SIO: Int = 13
let IRQ_SPU: Int = 14
let IRQ_PIO: Int = 15

// MARK: - Memory Segments
let MEM_kseg0: (start: UInt32, size: UInt32) = (0x0x80000000, 2097152)
let MEM_kseg1: (start: UInt32, size: UInt32) = (0x0xA0000000, 2097152)
let MEM_vram: (start: UInt32, size: UInt32) = (0x0xA0000000, 1048576)
let MEM_expansion: (start: UInt32, size: UInt32) = (0x0xA0000000, 1048576)
let MEM_scratchpad: (start: UInt32, size: UInt32) = (0x0x1F800000, 1024)
let MEM_exp_rom: (start: UInt32, size: UInt32) = (0x0x1FC00000, 524288)
let MEM_user_rom: (start: UInt32, size: UInt32) = (0x0x1F800000, 4194304)
let MEM_mmio: (start: UInt32, size: UInt32) = (0x0x1F801000, 8192)
let MEM_gpu: (start: UInt32, size: UInt32) = (0x0x1F801810, 8)
let MEM_cdrom: (start: UInt32, size: UInt32) = (0x0x1F801800, 16)
let MEM_spu: (start: UInt32, size: UInt32) = (0x0x1F801C00, 512)
let MEM_irq: (start: UInt32, size: UInt32) = (0x0x1F801070, 8)
let MEM_dma: (start: UInt32, size: UInt32) = (0x0x1F801080, 128)
let MEM_timer: (start: UInt32, size: UInt32) = (0x0x1F801100, 48)
let MEM_joy: (start: UInt32, size: UInt32) = (0x0x1F801040, 16)
let MEM_mdec: (start: UInt32, size: UInt32) = (0x0x1F801820, 8)
let MEM_sio: (start: UInt32, size: UInt32) = (0x0x1F801050, 16)
let MEM_gpu_stat: (start: UInt32, size: UInt32) = (0x0x1F801814, 4)
let MEM_cdrom_stat: (start: UInt32, size: UInt32) = (0x0x1F801801, 3)
let MEM_spu_ram: (start: UInt32, size: UInt32) = (0x0x1F800000, 1024)

// MARK: - Device Functions
func mips_r3000a_init() {
}

func read_register(_ addr: UInt32) -> UInt32 {
    return 0
}

func write_register(_ addr: UInt32, _ value: UInt32) {
}
