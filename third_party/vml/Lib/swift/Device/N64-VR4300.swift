//
// NEC-VR4300 Register Definitions
// Generated from: Nintendo 64 main processor - NEC VR4300 (MIPS R4300i-compatible) @ 93.75MHz, 64-bit R4000-like
// Version: 1.0
// Date: 2026-04-16
//

import Foundation

// MARK: - RSP (Reality Signal Processor (Audio/Video microcode engine))
let RSP_SP_MEM_ADDR: UInt32 = 0x0x00
let RSP_SP_DRAM_ADDR: UInt32 = 0x0x04
let RSP_SP_RD_LEN: UInt32 = 0x0x08
let RSP_SP_WR_LEN: UInt32 = 0x0x0C
let RSP_SP_STATUS: UInt32 = 0x0x10
let RSP_SP_DMA_FULL: UInt32 = 0x0x14
let RSP_SP_DMA_BUSY: UInt32 = 0x0x18
let RSP_SP_SEMAPHORE: UInt32 = 0x0x1C
let RSP_SP_PC: UInt32 = 0x0x20
let RSP_SP_IBIST: UInt32 = 0x0x24

// MARK: - RDP (Reality Drawing Processor (Triangle/Quad rasterizer))
let RDP_DP_START: UInt32 = 0x0x00
let RDP_DP_END: UInt32 = 0x0x04
let RDP_DP_CURRENT: UInt32 = 0x0x08
let RDP_DP_STATUS: UInt32 = 0x0x0C
let RDP_DP_CLOCK: UInt32 = 0x0x10
let RDP_DP_BUFBUSY: UInt32 = 0x0x14
let RDP_DP_PIPEBUSY: UInt32 = 0x0x18
let RDP_DP_TMEM: UInt32 = 0x0x1C

// MARK: - VI (Video Interface (scanout engine))
let VI_VI_STATUS: UInt32 = 0x0x00
let VI_VI_ORIGIN: UInt32 = 0x0x04
let VI_VI_WIDTH: UInt32 = 0x0x08
let VI_VI_V_INTR: UInt32 = 0x0x0C
let VI_VI_V_CURRENT: UInt32 = 0x0x10
let VI_VI_BURST: UInt32 = 0x0x14
let VI_VI_H_SYNC: UInt32 = 0x0x18
let VI_VI_H_SYNC_LEAP: UInt32 = 0x0x1C
let VI_VI_H_VIDEO: UInt32 = 0x0x20
let VI_VI_V_VIDEO: UInt32 = 0x0x24
let VI_VI_V_BURST: UInt32 = 0x0x28
let VI_VI_X_SCALE: UInt32 = 0x0x2C
let VI_VI_Y_SCALE: UInt32 = 0x0x30

// MARK: - AI (Audio Interface (DAC))
let AI_AI_DRAM_ADDR: UInt32 = 0x0x00
let AI_AI_LEN: UInt32 = 0x0x04
let AI_AI_CONTROL: UInt32 = 0x0x08
let AI_AI_STATUS: UInt32 = 0x0x0C
let AI_AI_DACRATE: UInt32 = 0x0x10
let AI_AI_BITRATE: UInt32 = 0x0x14

// MARK: - PI (Peripheral Interface (cartridge bus))
let PI_PI_DRAM_ADDR: UInt32 = 0x0x00
let PI_PI_CART_ADDR: UInt32 = 0x0x04
let PI_PI_RD_LEN: UInt32 = 0x0x08
let PI_PI_WR_LEN: UInt32 = 0x0x0C
let PI_PI_STATUS: UInt32 = 0x0x10
let PI_PI_BSD_DOM1_LAT: UInt32 = 0x0x14
let PI_PI_BSD_DOM1_PWD: UInt32 = 0x0x18
let PI_PI_BSD_DOM1_PGS: UInt32 = 0x0x1C
let PI_PI_BSD_DOM1_RLS: UInt32 = 0x0x20
let PI_PI_BSD_DOM2_LAT: UInt32 = 0x0x24
let PI_PI_BSD_DOM2_PWD: UInt32 = 0x0x28
let PI_PI_BSD_DOM2_PGS: UInt32 = 0x0x2C
let PI_PI_BSD_DOM2_RLS: UInt32 = 0x0x30

// MARK: - SI (Serial Interface (Controller Pak / 64DD))
let SI_SI_DRAM_ADDR: UInt32 = 0x0x00
let SI_SI_PIF_ADDR_RD64B: UInt32 = 0x0x04
let SI_SI_PIF_ADDR_WR64B: UInt32 = 0x0x08
let SI_SI_STATUS: UInt32 = 0x0x10

// MARK: - PIF (PIF (CIC / NUSYC - anti-piracy/copy protection))
let PIF_PIF_CMD0: UInt8 = 0x0x00
let PIF_PIF_CMD1: UInt8 = 0x0x01
let PIF_PIF_CMD2: UInt8 = 0x0x02
let PIF_PIF_CMD3: UInt8 = 0x0x03
let PIF_PIF_CMD4: UInt8 = 0x0x04
let PIF_PIF_CMD5: UInt8 = 0x0x05
let PIF_PIF_CMD6: UInt8 = 0x0x06
let PIF_PIF_CMD7: UInt8 = 0x0x07
let PIF_PIF_STATUS: UInt8 = 0x0x3F

// MARK: - INTERRUPT (Interrupt Control)
let INTERRUPT_MI_MODE: UInt32 = 0x0x00
let INTERRUPT_MI_VERSION: UInt32 = 0x0x04
let INTERRUPT_MI_INTR: UInt32 = 0x0x08
let INTERRUPT_MI_INTR_MASK: UInt32 = 0x0x0C

// MARK: - CONTROLLER (Controller Interface (SI channel 0-3))
let CONTROLLER_SI_CH0_DATA: UInt64 = 0x0x00
let CONTROLLER_SI_CH1_DATA: UInt64 = 0x0x08
let CONTROLLER_SI_CH2_DATA: UInt64 = 0x0x10
let CONTROLLER_SI_CH3_DATA: UInt64 = 0x0x18

// MARK: - Interrupt Vectors
let IRQ_RESET: Int = 0
let IRQ_TLB_REFILL: Int = 1
let IRQ_CACHE_ERROR: Int = 2
let IRQ_GENERAL_EXCEPTION: Int = 3
let IRQ_RSP: Int = 4
let IRQ_RDP: Int = 5
let IRQ_VI: Int = 6
let IRQ_AI: Int = 7
let IRQ_PI: Int = 8
let IRQ_SI: Int = 9
let IRQ_TIMER_COMPARE: Int = 10

// MARK: - Memory Segments
let MEM_rdram: (start: UInt32, size: UInt32) = (0x0x00000000, 4194304)
let MEM_rdram_reg: (start: UInt32, size: UInt32) = (0x0x18000000, 4096)
let MEM_sp_mem: (start: UInt32, size: UInt32) = (0x0x1FC00000, 2048)
let MEM_sp_imem: (start: UInt32, size: UInt32) = (0x0x1FC00800, 2048)
let MEM_rcp_regs: (start: UInt32, size: UInt32) = (0x0x1FC00000, 262144)
let MEM_pi_regs: (start: UInt32, size: UInt32) = (0x0x1FC00000, 2048)
let MEM_vi_regs: (start: UInt32, size: UInt32) = (0x0x1FC002C0, 64)
let MEM_ai_regs: (start: UInt32, size: UInt32) = (0x0x1FC00500, 64)
let MEM_si_regs: (start: UInt32, size: UInt32) = (0x0x1FC004C0, 64)
let MEM_pi_dram: (start: UInt32, size: UInt32) = (0x0xA0000000, 67108864)
let MEM_cart_rom: (start: UInt32, size: UInt32) = (0x0xB0000000, 268435456)
let MEM_pif_ram: (start: UInt32, size: UInt32) = (0x0x1FC007C0, 64)

// MARK: - Device Functions
func nec_vr4300_init() {
}

func read_register(_ addr: UInt32) -> UInt32 {
    return 0
}

func write_register(_ addr: UInt32, _ value: UInt32) {
}
