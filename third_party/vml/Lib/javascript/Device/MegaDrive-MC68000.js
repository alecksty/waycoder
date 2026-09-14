/**
 * Motorola-68000 寄存器定义
 * 生成自: Motorola/68000/Motorola-68000
 * 版本: 1.0
 */
export const motorola_68000 = {
  // CPU: MC68000, 32位, 7670452 Hz

  // 寄存器定义
  // Data Register 0
  D0: 0x00,
  // Data Register 1
  D1: 0x04,
  // Data Register 2
  D2: 0x08,
  // Data Register 3
  D3: 0x0C,
  // Data Register 4
  D4: 0x10,
  // Data Register 5
  D5: 0x14,
  // Data Register 6
  D6: 0x18,
  // Data Register 7
  D7: 0x1C,
  // Address Register 0
  A0: 0x20,
  // Address Register 1
  A1: 0x24,
  // Address Register 2
  A2: 0x28,
  // Address Register 3
  A3: 0x2C,
  // Address Register 4
  A4: 0x30,
  // Address Register 5
  A5: 0x34,
  // Address Register 6
  A6: 0x38,
  // Stack Pointer (USP)
  A7: 0x3C,
  // Program Counter
  PC: 0x40,
  // Status Register
  SR: 0x44,
  SR_C: 0,  // Carry
  SR_V: 1,  // Overflow
  SR_Z: 2,  // Zero
  SR_N: 3,  // Negative
  SR_X: 4,  // Extend
  SR_I0: 8,  // Interrupt Mask 0
  SR_I1: 9,  // Interrupt Mask 1
  SR_I2: 10,  // Interrupt Mask 2
  SR_M: 11,  // Master/Interrupt
  SR_S: 13,  // Supervisor/User
  SR_T0: 14,  // Trace Mode 0
  SR_T1: 15,  // Trace Mode 1

  // 内存段
  // System RAM (4MB)
  ram_START: 0x000000,
  ram_END: 0x3FFFFF,
  ram_SIZE: 4194304,
  // Cartridge ROM
  rom_START: 0x000000,
  rom_END: 0x3FFFFF,
  rom_SIZE: 4194304,
  // I/O Register Area
  io_START: 0xA00000,
  io_END: 0xA1FFFF,
  io_SIZE: 131072,
  // VDP Registers
  vdp_START: 0xC00000,
  vdp_END: 0xC0001F,
  vdp_SIZE: 32,
  // Video RAM (256KB)
  vram_START: 0xE00000,
  vram_END: 0xE3FFFF,
  vram_SIZE: 262144,

  // 外设定义
  // Video Display Processor (TMS9918A variant)
  VDP_BASE: 0xC00000,
  VDP_DATA: 0x00C00000,
  VDP_CTRL: 0x00C00004,
  VDP_HVCOUNT: 0x00C00008,
  VDP_HVB_STATUS: 0x00C0000A,
  // Programmable Sound Generator (AY-3-8910)
  PSG_BASE: 0xC00011,
  PSG_CH_A_FREQ: 0x00C00011,
  PSG_CH_A_VOL: 0x00C00019,
  PSG_CH_B_FREQ: 0x00C00013,
  PSG_CH_B_VOL: 0x00C0001A,
  PSG_CH_C_FREQ: 0x00C00015,
  PSG_CH_C_VOL: 0x00C0001B,
  PSG_NOISE_FREQ: 0x00C00017,
  PSG_MIXER: 0x00C00018,
  PSG_ENV_FREQ: 0x00C0001E,
  PSG_ENV_SHAPE: 0x00C0001C,
  // Z80 Secondary CPU (Sound)
  Z80_BASE: 0xA00000,
  Z80_Z80_RESET: 0x00A00000,
  Z80_Z80_BUSREQ: 0x00A00004,
  Z80_Z80_STATUS: 0x00A00008,
  // Bank Register
  BANK_REG_BASE: 0xA12000,
  BANK_REG_ROM_BANK: 0x00A12000,
  BANK_REG_RAM_BANK: 0x00A12004,
  // Hardware Version
  HW_VERSION_BASE: 0xA10001,
  HW_VERSION_VERSION: 0x00A10001,
  // Controller Port 1
  CONTROLLER1_BASE: 0xA10003,
  CONTROLLER1_DATA: 0x00A10003,
  CONTROLLER1_CTRL: 0x00A10007,
  // Controller Port 2
  CONTROLLER2_BASE: 0xA10005,
  CONTROLLER2_DATA: 0x00A10005,
  CONTROLLER2_CTRL: 0x00A10009,
  // External Port
  EXT_PORT_BASE: 0xA10007,
  EXT_PORT_DATA: 0x00A10007,
  // DMA Controller
  DMA_BASE: 0xA10008,
  DMA_SOURCE: 0x00A10008,
  DMA_DEST: 0x00A1000C,
  DMA_COUNT: 0x00A10010,
  DMA_CTRL: 0x00A10012,
  // Hardware Timer
  TIMER_BASE: 0xA1000E,
  TIMER_H_COUNTER: 0x00A1000E,
  TIMER_V_COUNTER: 0x00A10012,

  // 中断向量
  IRQ_RESET_SP: 1,  // Reset Initial Stack Pointer
  IRQ_RESET_PC: 2,  // Reset Initial PC
  IRQ_BUS_ERROR: 3,  // Bus Error
  IRQ_ADDRESS_ERROR: 4,  // Address Error
  IRQ_ILLEGAL_INSTR: 5,  // Illegal Instruction
  IRQ_ZERO_DIVIDE: 6,  // Zero Divide
  IRQ_CHK_EXCEPTION: 7,  // CHK Exception
  IRQ_TRAPV: 8,  // TRAPV Exception
  IRQ_PRIVILEGE: 9,  // Privilege Violation
  IRQ_TRACE: 10,  // Trace
  IRQ_LINE_A: 11,  // Line 1010 Emulator
  IRQ_LINE_F: 12,  // Line 1111 Emulator
  IRQ_IRQ1: 24,  // External Interrupt 1 (H-Blank)
  IRQ_IRQ2: 25,  // External Interrupt 2 (V-Blank)
  IRQ_IRQ3: 26,  // External Interrupt 3
  IRQ_IRQ4: 27,  // External Interrupt 4 (D-Req)
  IRQ_IRQ5: 28,  // External Interrupt 5
  IRQ_IRQ6: 29,  // External Interrupt 6
  IRQ_IRQ7: 30,  // External Interrupt 7
  IRQ_TRAP0: 32,  // TRAP #0
  IRQ_TRAP1: 33,  // TRAP #1
  IRQ_TRAP15: 47,  // TRAP #15

  init: function() {
    // 硬件初始化
  }
};
