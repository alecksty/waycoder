/**
 * Sega-Master-System 寄存器定义
 * 生成自: Sega/Master System/Sega-Master-System
 * 版本: 1.0
 */
export const sega_master_system = {
  // CPU: Zilog Z80, 8位, 3579545 Hz

  // 寄存器定义
  // Accumulator
  A: 0,
  // Flags
  F: 0,
  // B
  B: 0,
  // C
  C: 0,
  // D
  D: 0,
  // E
  E: 0,
  // H
  H: 0,
  // L
  L: 0,
  // Index Register X
  IX: 0,
  // Index Register Y
  IY: 0,
  // Stack Pointer
  SP: 0,
  // Program Counter
  PC: 0,
  // Interrupt Vector
  I: 0,
  // Memory Refresh
  R: 0,

  // 外设定义
  // Video Display Processor (TMS9918A)
  VDP_BASE: ,
  VDP_VDP_DATA: 0x000000BE,
  VDP_VDP_ADDR: 0x000000BF,
  VDP_VDP_STATUS: 0x000000BF,
  // Programmable Sound Generator (SN76489)
  PSG_BASE: ,
  PSG_PSG_DATA: 0x0000007F,
  // I/O ports
  IO_BASE: ,
  IO_IO_PORT_A: 0x000000DC,
  IO_IO_PORT_B: 0x000000DD,
  IO_IO_PORT_MISC: 0x000000DE,
  IO_IO_PORT_VDP: 0x000000DF,
  // Memory mapper
  MemoryMapper_BASE: ,
  MemoryMapper_MAPPER_0: 0x0000FFFC,
  MemoryMapper_MAPPER_1: 0x0000FFFD,
  MemoryMapper_MAPPER_2: 0x0000FFFE,
  MemoryMapper_MAPPER_3: 0x0000FFFF,
  // FM Sound Unit (optional)
  FMUnit_BASE: ,
  FMUnit_FM_ADDR: 0x000000F0,
  FMUnit_FM_DATA: 0x000000F1,
  FMUnit_FM_DETECT: 0x000000F2,

  // 中断向量
  IRQ_RST_00: 0,  // Restart 00h
  IRQ_IM1: 56,  // Interrupt Mode 1
  IRQ_VBLANK: 56,  // Vertical blank interrupt
  IRQ_LINE: 100,  // Line interrupt

  init: function() {
    // 硬件初始化
  }
};
