/**
 * Sega-Genesis 寄存器定义
 * 生成自: Sega/Genesis/Mega Drive/Sega-Genesis
 * 版本: 1.0
 */
export const sega_genesis = {
  // CPU: Motorola 68000, 32位, 7670000 Hz

  // 寄存器定义
  // Data Register 0
  D0: 0,
  // Data Register 1
  D1: 0,
  // Data Register 2
  D2: 0,
  // Data Register 3
  D3: 0,
  // Data Register 4
  D4: 0,
  // Data Register 5
  D5: 0,
  // Data Register 6
  D6: 0,
  // Data Register 7
  D7: 0,
  // Address Register 0
  A0: 0,
  // Address Register 1
  A1: 0,
  // Address Register 2
  A2: 0,
  // Address Register 3
  A3: 0,
  // Address Register 4
  A4: 0,
  // Address Register 5
  A5: 0,
  // Address Register 6
  A6: 0,
  // Address Register 7 (SP)
  A7: 0,
  // Program Counter
  PC: 0,
  // Status Register
  SR: 0,

  // 外设定义
  // Video Display Processor (315-5313)
  VDP_BASE: ,
  VDP_VDP_DATA: 0x00C00000,
  VDP_VDP_CONTROL: 0x00C00004,
  VDP_VDP_HVCOUNTER: 0x00C00008,
  VDP_VDP_PSG: 0x00C00011,
  // FM synthesis sound chip
  YM2612_BASE: ,
  YM2612_YM2612_ADDR0: 0x00A04000,
  YM2612_YM2612_DATA0: 0x00A04001,
  YM2612_YM2612_ADDR1: 0x00A04002,
  YM2612_YM2612_DATA1: 0x00A04003,
  // I/O ports
  IOPorts_BASE: ,
  IOPorts_IO_DATA1: 0x00A10002,
  IOPorts_IO_DATA2: 0x00A10004,
  IOPorts_IO_DATA3: 0x00A10006,
  IOPorts_IO_CTRL1: 0x00A10008,
  IOPorts_IO_CTRL2: 0x00A1000A,
  IOPorts_IO_CTRL3: 0x00A1000C,
  // TradeMark Security System
  TMSS_BASE: ,
  TMSS_TMSS: 0x00A14000,
  // Z80 bus control
  Z80Bus_BASE: ,
  Z80Bus_Z80_BUSREQ: 0x00A11100,
  Z80Bus_Z80_RESET: 0x00A11200,
  Z80Bus_Z80_YM2612: 0x00A04000,

  // 中断向量
  IRQ_RESET_SP: 0,  // Reset (Initial SP)
  IRQ_RESET_PC: 4,  // Reset (Initial PC)
  IRQ_HBLANK: 24,  // Horizontal blank interrupt
  IRQ_VBLANK: 28,  // Vertical blank interrupt
  IRQ_EXTINT1: 32,  // External interrupt 1
  IRQ_EXTINT2: 36,  // External interrupt 2
  IRQ_EXTINT3: 40,  // External interrupt 3
  IRQ_EXTINT4: 44,  // External interrupt 4
  IRQ_EXTINT5: 48,  // External interrupt 5
  IRQ_EXTINT6: 52,  // External interrupt 6
  IRQ_EXTINT7: 56,  // External interrupt 7

  init: function() {
    // 硬件初始化
  }
};
