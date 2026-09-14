/**
 * NEC-VR4300 寄存器定义
 * 生成自: NEC/MIPS-R4000/NEC-VR4300
 * 版本: 1.0
 */
export const nec_vr4300 = {
  // CPU: MIPS-R4300i, 64位, 93750000 Hz

  // 寄存器定义
  // Hard-wired Zero
  R0: 0x00,
  // Assembler Temporary
  R1: 0x08,
  // Value Return
  R2: 0x10,
  // Expression Evaluation
  R3: 0x18,
  // Expression Evaluation
  R4: 0x20,
  // Expression Evaluation
  R5: 0x28,
  // Expression Evaluation
  R6: 0x30,
  // Expression Evaluation
  R7: 0x38,
  // Expression Evaluation
  R8: 0x40,
  // Expression Evaluation
  R9: 0x48,
  // Expression Evaluation
  R10: 0x50,
  // Expression Evaluation
  R11: 0x58,
  // Expression Evaluation
  R12: 0x60,
  // Expression Evaluation
  R13: 0x68,
  // Expression Evaluation
  R14: 0x70,
  // Expression Evaluation
  R15: 0x78,
  // Saved Value
  R16: 0x80,
  // Saved Value
  R17: 0x88,
  // Saved Value
  R18: 0x90,
  // Saved Value
  R19: 0x98,
  // Saved Value
  R20: 0xA0,
  // Saved Value
  R21: 0xA8,
  // Saved Value
  R22: 0xB0,
  // Saved Value
  R23: 0xB8,
  // Temporary
  R24: 0xC0,
  // Temporary
  R25: 0xC8,
  // Kernel Reserved
  R26: 0xD0,
  // Kernel Reserved
  R27: 0xD8,
  // Global Pointer
  R28: 0xE0,
  // Stack Pointer
  R29: 0xE8,
  // Frame Pointer
  R30: 0xF0,
  // Return Address
  R31: 0xF8,
  // Multiply/Divide High (64-bit)
  HI: 0x100,
  // Multiply/Divide Low (64-bit)
  LO: 0x108,
  // Program Counter
  PC: 0x110,
  // LLAddr / LLBit (for LL/SC)
  LLB: 0x118,
  // TLB Index
  CP0_INDEX: 0x200,
  // TLB Random
  CP0_RANDOM: 0x208,
  // TLB EntryLo 0 (even page)
  CP0_ENTRYLO0: 0x210,
  // TLB EntryLo 1 (odd page)
  CP0_ENTRYLO1: 0x218,
  // Context Register (PTE base)
  CP0_CONTEXT: 0x220,
  // Page Mask (variable page size)
  CP0_PAGEMASK: 0x228,
  // TLB Wired
  CP0_WIRED: 0x230,
  // Bad Virtual Address
  CP0_BADVADDR: 0x238,
  // Count (incrementing timer)
  CP0_COUNT: 0x240,
  // TLB EntryHi (VPN2 + ASID)
  CP0_ENTRYHI: 0x250,
  // Compare (timer interrupt)
  CP0_COMPARE: 0x258,
  // Status Register
  CP0_STATUS: 0x260,
  CP0_STATUS_IE: 0,  // Interrupt Enable
  CP0_STATUS_EXL: 1,  // Exception Level
  CP0_STATUS_ERL: 2,  // Error Level
  CP0_STATUS_KSU: 0,  // Kernel/User Mode
  CP0_STATUS_UX: 5,  // User Mode 64-bit (1=64-bit user)
  CP0_STATUS_SX: 6,  // Supervisor Mode 64-bit
  CP0_STATUS_KX: 7,  // Kernel Mode 64-bit
  CP0_STATUS_IM0-7: 0,  // Interrupt Mask
  CP0_STATUS_CU0: 28,  // Coprocessor 0 Usable
  CP0_STATUS_BEV: 22,  // Bootstrap Exception Vector
  CP0_STATUS_TS: 21,  // TLB Shutdown
  CP0_STATUS_FR: 26,  // Floating-Point Register Mode (32 double)
  // Cause Register
  CP0_CAUSE: 0x268,
  CP0_CAUSE_EXCCODE: 0,  // Exception Code
  CP0_CAUSE_IP0-7: 0,  // Interrupt Pending
  CP0_CAUSE_BD: 31,  // Branch Delay Slot
  CP0_CAUSE_CE: 0,  // Coprocessor Error
  // Exception PC
  CP0_EPC: 0x270,
  // Config Register
  CP0_CONFIG: 0x280,
  // Load Linked Address
  CP0_LLADDR: 0x288,
  // WatchLo (data/instruction break)
  CP0_WATCHLO: 0x290,
  // WatchHi
  CP0_WATCHHI: 0x298,
  // Extended Context
  CP0_XCONTEXT: 0x2A0,
  // Tag/Process ID
  CP0_PID: 0x2B0,
  // Debug Register
  CP0_DEBUG: 0x2D8,
  // Performance Counter
  CP0_PERF: 0x2F0,

  // 内存段
  // RDRAM (4MB base, up to 8MB)
  rdram_START: 0x00000000,
  rdram_END: 0x003FFFFF,
  rdram_SIZE: 4194304,
  // RDRAM Registers
  rdram_reg_START: 0x18000000,
  rdram_reg_END: 0x18000FFF,
  rdram_reg_SIZE: 4096,
  // RCP SP Memory / DMEM (2KB)
  sp_mem_START: 0x1FC00000,
  sp_mem_END: 0x1FC007FF,
  sp_mem_SIZE: 2048,
  // RCP SP Instruction Memory / IMEM (2KB)
  sp_imem_START: 0x1FC00800,
  sp_imem_END: 0x1FC00FFF,
  sp_imem_SIZE: 2048,
  // RCP Register Area
  rcp_regs_START: 0x1FC00000,
  rcp_regs_END: 0x1FC3FFFF,
  rcp_regs_SIZE: 262144,
  // PI (Peripheral Interface) Registers
  pi_regs_START: 0x1FC00000,
  pi_regs_END: 0x1FC007FF,
  pi_regs_SIZE: 2048,
  // VI (Video Interface) Registers
  vi_regs_START: 0x1FC002C0,
  vi_regs_END: 0x1FC002FF,
  vi_regs_SIZE: 64,
  // AI (Audio Interface) Registers
  ai_regs_START: 0x1FC00500,
  ai_regs_END: 0x1FC0053F,
  ai_regs_SIZE: 64,
  // SI (Serial Interface) Registers
  si_regs_START: 0x1FC004C0,
  si_regs_END: 0x1FC004FF,
  si_regs_SIZE: 64,
  // PI Bus DRAM (cartridge)
  pi_dram_START: 0xA0000000,
  pi_dram_END: 0xA4000000,
  pi_dram_SIZE: 67108864,
  // Cartridge ROM (up to 256MB)
  cart_rom_START: 0xB0000000,
  cart_rom_END: 0xBFFFFFFF,
  cart_rom_SIZE: 268435456,
  // PIF-NUS ROM/RAM (CIC)
  pif_ram_START: 0x1FC007C0,
  pif_ram_END: 0x1FC007FF,
  pif_ram_SIZE: 64,

  // 外设定义
  // Reality Signal Processor (Audio/Video microcode engine)
  RSP_BASE: 0x04040000,
  RSP_SP_MEM_ADDR: 0x04040000,
  RSP_SP_DRAM_ADDR: 0x04040004,
  RSP_SP_RD_LEN: 0x04040008,
  RSP_SP_WR_LEN: 0x0404000C,
  RSP_SP_STATUS: 0x04040010,
  RSP_SP_STATUS_BROKE: 0,  // Command Queue Broke
  RSP_SP_STATUS_SLEEP: 2,  // SP Sleep
  RSP_SP_STATUS_GOODMATCH: 3,  // DMEM/IMEM Goodmatch
  RSP_SP_STATUS_SSTEP: 4,  // Single Step
  RSP_SP_STATUS_INTSIG: 5,  // Interrupt Signal
  RSP_SP_STATUS_HALT: 6,  // Halt
  RSP_SP_STATUS_CLEAR: 7,  // Clear SP Status
  RSP_SP_STATUS_INTR_BRK: 8,  // IntrOnBreak
  RSP_SP_STATUS_SIGNAL0: 12,  // Software Signal 0
  RSP_SP_STATUS_SIGNAL1: 13,  // Software Signal 1
  RSP_SP_STATUS_SIGNAL2: 14,  // Software Signal 2
  RSP_SP_STATUS_SIGNAL3: 15,  // Software Signal 3
  RSP_SP_STATUS_SIGNAL4: 16,  // Software Signal 4
  RSP_SP_STATUS_SIGNAL5: 17,  // Software Signal 5
  RSP_SP_STATUS_SIGNAL6: 18,  // Software Signal 6
  RSP_SP_STATUS_SIGNAL7: 19,  // Software Signal 7
  RSP_SP_DMA_FULL: 0x04040014,
  RSP_SP_DMA_BUSY: 0x04040018,
  RSP_SP_SEMAPHORE: 0x0404001C,
  RSP_SP_PC: 0x04040020,
  RSP_SP_IBIST: 0x04040024,
  // Reality Drawing Processor (Triangle/Quad rasterizer)
  RDP_BASE: 0x04100000,
  RDP_DP_START: 0x04100000,
  RDP_DP_END: 0x04100004,
  RDP_DP_CURRENT: 0x04100008,
  RDP_DP_STATUS: 0x0410000C,
  RDP_DP_STATUS_TERMINATE: 0,  // Terminator
  RDP_DP_STATUS_PIPE_BUSY: 1,  // Pipeline Busy
  RDP_DP_STATUS_TOMINO_BUSY: 2,  // ToMini Busy
  RDP_DP_STATUS_PIPE_FLUSH: 3,  // Pipeline Flush
  RDP_DP_STATUS_TOMINO_FLUSH: 4,  // ToMini Flush
  RDP_DP_STATUS_FREEZE: 5,  // Freeze
  RDP_DP_STATUS_START_GCLK: 24,  // Start GCLK
  RDP_DP_CLOCK: 0x04100010,
  RDP_DP_BUFBUSY: 0x04100014,
  RDP_DP_PIPEBUSY: 0x04100018,
  RDP_DP_TMEM: 0x0410001C,
  // Video Interface (scanout engine)
  VI_BASE: 0x04400000,
  VI_VI_STATUS: 0x04400000,
  VI_VI_STATUS_TYPE: 0,  // Display Type (0=blank, 1=reserved, 2=480i, 3=240p, 4=1080i, 5=576i)
  VI_VI_STATUS_DITHER_FILTER: 6,  // Dither Filter Enable
  VI_VI_STATUS_GAMMA: 7,  // Gamma Correction Enable
  VI_VI_STATUS_GAMMA_DITHER: 8,  // Gamma Dither Enable
  VI_VI_STATUS_DIVOT: 9,  // Divot Control
  VI_VI_STATUS_SERRATION: 10,  //  Serration Enable (for interlaced)
  VI_VI_ORIGIN: 0x04400004,
  VI_VI_WIDTH: 0x04400008,
  VI_VI_V_INTR: 0x0440000C,
  VI_VI_V_CURRENT: 0x04400010,
  VI_VI_BURST: 0x04400014,
  VI_VI_H_SYNC: 0x04400018,
  VI_VI_H_SYNC_LEAP: 0x0440001C,
  VI_VI_H_VIDEO: 0x04400020,
  VI_VI_V_VIDEO: 0x04400024,
  VI_VI_V_BURST: 0x04400028,
  VI_VI_X_SCALE: 0x0440002C,
  VI_VI_Y_SCALE: 0x04400030,
  // Audio Interface (DAC)
  AI_BASE: 0x04500000,
  AI_AI_DRAM_ADDR: 0x04500000,
  AI_AI_LEN: 0x04500004,
  AI_AI_CONTROL: 0x04500008,
  AI_AI_CONTROL_DMA_ENABLE: 0,  // DMA Enable
  AI_AI_CONTROL_DMA_FIFO_FULL: 1,  // DMA FIFO Full
  AI_AI_STATUS: 0x0450000C,
  AI_AI_DACRATE: 0x04500010,
  AI_AI_BITRATE: 0x04500014,
  // Peripheral Interface (cartridge bus)
  PI_BASE: 0x04600000,
  PI_PI_DRAM_ADDR: 0x04600000,
  PI_PI_CART_ADDR: 0x04600004,
  PI_PI_RD_LEN: 0x04600008,
  PI_PI_WR_LEN: 0x0460000C,
  PI_PI_STATUS: 0x04600010,
  PI_PI_STATUS_DMA_BUSY: 0,  // DMA Busy
  PI_PI_STATUS_IO_BUSY: 1,  // I/O Busy
  PI_PI_STATUS_ERROR: 2,  // Bus Error
  PI_PI_BSD_DOM1_LAT: 0x04600014,
  PI_PI_BSD_DOM1_PWD: 0x04600018,
  PI_PI_BSD_DOM1_PGS: 0x0460001C,
  PI_PI_BSD_DOM1_RLS: 0x04600020,
  PI_PI_BSD_DOM2_LAT: 0x04600024,
  PI_PI_BSD_DOM2_PWD: 0x04600028,
  PI_PI_BSD_DOM2_PGS: 0x0460002C,
  PI_PI_BSD_DOM2_RLS: 0x04600030,
  // Serial Interface (Controller Pak / 64DD)
  SI_BASE: 0x04800000,
  SI_SI_DRAM_ADDR: 0x04800000,
  SI_SI_PIF_ADDR_RD64B: 0x04800004,
  SI_SI_PIF_ADDR_WR64B: 0x04800008,
  SI_SI_STATUS: 0x04800010,
  SI_SI_STATUS_DMA_BUSY: 0,  // DMA Busy
  SI_SI_STATUS_IO_BUSY: 1,  // I/O Busy
  SI_SI_STATUS_INTERRUPT: 12,  // SI Interrupt
  // PIF (CIC / NUSYC - anti-piracy/copy protection)
  PIF_BASE: 0x1FC007C0,
  PIF_PIF_CMD0: 0x1FC007C0,
  PIF_PIF_CMD1: 0x1FC007C1,
  PIF_PIF_CMD2: 0x1FC007C2,
  PIF_PIF_CMD3: 0x1FC007C3,
  PIF_PIF_CMD4: 0x1FC007C4,
  PIF_PIF_CMD5: 0x1FC007C5,
  PIF_PIF_CMD6: 0x1FC007C6,
  PIF_PIF_CMD7: 0x1FC007C7,
  PIF_PIF_STATUS: 0x1FC007FF,
  // Interrupt Control
  INTERRUPT_BASE: 0x1FC00200,
  INTERRUPT_MI_MODE: 0x1FC00200,
  INTERRUPT_MI_MODE_INIT_MODE: 0,  // Initialize Mode
  INTERRUPT_MI_MODE_EBUS_TEST: 1,  // EBUS Test Mode
  INTERRUPT_MI_VERSION: 0x1FC00204,
  INTERRUPT_MI_INTR: 0x1FC00208,
  INTERRUPT_MI_INTR_SP: 0,  // SP Interrupt Pending
  INTERRUPT_MI_INTR_SI: 1,  // SI Interrupt Pending
  INTERRUPT_MI_INTR_AI: 2,  // AI Interrupt Pending
  INTERRUPT_MI_INTR_VI: 3,  // VI Interrupt Pending
  INTERRUPT_MI_INTR_PI: 4,  // PI Interrupt Pending
  INTERRUPT_MI_INTR_DP: 5,  // DP Interrupt Pending
  INTERRUPT_MI_INTR_MASK: 0x1FC0020C,
  INTERRUPT_MI_INTR_MASK_SP_MASK: 0,  // SP Interrupt Mask
  INTERRUPT_MI_INTR_MASK_SI_MASK: 1,  // SI Interrupt Mask
  INTERRUPT_MI_INTR_MASK_AI_MASK: 2,  // AI Interrupt Mask
  INTERRUPT_MI_INTR_MASK_VI_MASK: 3,  // VI Interrupt Mask
  INTERRUPT_MI_INTR_MASK_PI_MASK: 4,  // PI Interrupt Mask
  INTERRUPT_MI_INTR_MASK_DP_MASK: 5,  // DP Interrupt Mask
  // Controller Interface (SI channel 0-3)
  CONTROLLER_BASE: 0x1FC00600,
  CONTROLLER_SI_CH0_DATA: 0x1FC00600,
  CONTROLLER_SI_CH1_DATA: 0x1FC00608,
  CONTROLLER_SI_CH2_DATA: 0x1FC00610,
  CONTROLLER_SI_CH3_DATA: 0x1FC00618,

  // 中断向量
  IRQ_RESET: 0,  // Soft Reset / NMI
  IRQ_TLB_REFILL: 1,  // TLB Refill (I) / TLB Refill (D)
  IRQ_CACHE_ERROR: 2,  // Cache Error
  IRQ_GENERAL_EXCEPTION: 3,  // General Exception
  IRQ_RSP: 4,  // RSP Interrupt (microcode signal)
  IRQ_RDP: 5,  // RDP Interrupt (display list complete)
  IRQ_VI: 6,  // VI Interrupt (V-Blank / scanline)
  IRQ_AI: 7,  // AI Interrupt (audio DMA complete)
  IRQ_PI: 8,  // PI Interrupt (cartridge DMA)
  IRQ_SI: 9,  // SI Interrupt (serial interface)
  IRQ_TIMER_COMPARE: 10,  // Timer Compare (CP0 Count == Compare)

  // 引脚定义
  PIN_VCC: 1,  // Power Supply (3.3V)
  PIN_VSS: 2,  // Ground
  PIN_CLK: 3,  // System Clock (93.75MHz from CIC/PLL)
  PIN_RESET: 4,  // Reset (active low)
  PIN_NMI: 5,  // Non-Maskable Interrupt
  PIN_INT0: 6,  // Interrupt 0 (RCP)
  PIN_INT1: 7,  // Interrupt 1 (cartridge)
  PIN_INT2: 8,  // Interrupt 2 (SI)
  PIN_INT3: 9,  // Interrupt 3 (PIF)
  PIN_AB0: 10,  // Address Bus Bit 0
  PIN_AB1: 11,  // Address Bus Bit 1
  PIN_AB2: 12,  // Address Bus Bit 2
  PIN_AB3: 13,  // Address Bus Bit 3
  PIN_AB4: 14,  // Address Bus Bit 4
  PIN_AB5: 15,  // Address Bus Bit 5
  PIN_AB6: 16,  // Address Bus Bit 6
  PIN_AB7: 17,  // Address Bus Bit 7
  PIN_AB8: 18,  // Address Bus Bit 8
  PIN_AB9: 19,  // Address Bus Bit 9
  PIN_AB10: 20,  // Address Bus Bit 10
  PIN_AB11: 21,  // Address Bus Bit 11
  PIN_AB12: 22,  // Address Bus Bit 12
  PIN_AB13: 23,  // Address Bus Bit 13
  PIN_AB14: 24,  // Address Bus Bit 14
  PIN_AB15: 25,  // Address Bus Bit 15
  PIN_AB16: 26,  // Address Bus Bit 16
  PIN_AB17: 27,  // Address Bus Bit 17
  PIN_AB18: 28,  // Address Bus Bit 18
  PIN_AB19: 29,  // Address Bus Bit 19
  PIN_AB20: 30,  // Address Bus Bit 20
  PIN_AB21: 31,  // Address Bus Bit 21
  PIN_AB22: 32,  // Address Bus Bit 22
  PIN_AB23: 33,  // Address Bus Bit 23
  PIN_AB24: 34,  // Address Bus Bit 24
  PIN_AB25: 35,  // Address Bus Bit 25
  PIN_AB26: 36,  // Address Bus Bit 26
  PIN_AB27: 37,  // Address Bus Bit 27
  PIN_AB28: 38,  // Address Bus Bit 28
  PIN_AB29: 39,  // Address Bus Bit 29
  PIN_AB30: 40,  // Address Bus Bit 30
  PIN_AB31: 41,  // Address Bus Bit 31
  PIN_AB32: 42,  // Address Bus Bit 32
  PIN_AB33: 43,  // Address Bus Bit 33
  PIN_AB34: 44,  // Address Bus Bit 34
  PIN_AB35: 45,  // Address Bus Bit 35
  PIN_DB0: 46,  // Data Bus Bit 0
  PIN_DB1: 47,  // Data Bus Bit 1
  PIN_DB2: 48,  // Data Bus Bit 2
  PIN_DB3: 49,  // Data Bus Bit 3
  PIN_DB4: 50,  // Data Bus Bit 4
  PIN_DB5: 51,  // Data Bus Bit 5
  PIN_DB6: 52,  // Data Bus Bit 6
  PIN_DB7: 53,  // Data Bus Bit 7
  PIN_DB8: 54,  // Data Bus Bit 8
  PIN_DB9: 55,  // Data Bus Bit 9
  PIN_DB10: 56,  // Data Bus Bit 10
  PIN_DB11: 57,  // Data Bus Bit 11
  PIN_DB12: 58,  // Data Bus Bit 12
  PIN_DB13: 59,  // Data Bus Bit 13
  PIN_DB14: 60,  // Data Bus Bit 14
  PIN_DB15: 61,  // Data Bus Bit 15
  PIN_DB16: 62,  // Data Bus Bit 16
  PIN_DB17: 63,  // Data Bus Bit 17
  PIN_DB18: 64,  // Data Bus Bit 18
  PIN_DB19: 65,  // Data Bus Bit 19
  PIN_DB20: 66,  // Data Bus Bit 20
  PIN_DB21: 67,  // Data Bus Bit 21
  PIN_DB22: 68,  // Data Bus Bit 22
  PIN_DB23: 69,  // Data Bus Bit 23
  PIN_DB24: 70,  // Data Bus Bit 24
  PIN_DB25: 71,  // Data Bus Bit 25
  PIN_DB26: 72,  // Data Bus Bit 26
  PIN_DB27: 73,  // Data Bus Bit 27
  PIN_DB28: 74,  // Data Bus Bit 28
  PIN_DB29: 75,  // Data Bus Bit 29
  PIN_DB30: 76,  // Data Bus Bit 30
  PIN_DB31: 77,  // Data Bus Bit 31
  PIN_BE0: 78,  // Byte Enable 0
  PIN_BE1: 79,  // Byte Enable 1
  PIN_BE2: 80,  // Byte Enable 2
  PIN_BE3: 81,  // Byte Enable 3
  PIN_NCS0: 82,  // Chip Select 0 (RDRAM)
  PIN_NCS1: 83,  // Chip Select 1 (RCP)
  PIN_NCS2: 84,  // Chip Select 2 (PIF ROM)
  PIN_NCS3: 85,  // Chip Select 3 (Cartridge)
  PIN_NWR: 86,  // Write Enable
  PIN_NRD: 87,  // Read Enable
  PIN_EKN: 88,  // Audio DAC Data (I2S/EKN format)
  PIN_AUDIO_L: 89,  // Audio Left Output
  PIN_AUDIO_R: 90,  // Audio Right Output
  PIN_VIDEO_R: 91,  // Video Output Red
  PIN_VIDEO_G: 92,  // Video Output Green
  PIN_VIDEO_B: 93,  // Video Output Blue
  PIN_SYNC: 94,  // Video Sync

  init: function() {
    // 硬件初始化
  }
};
