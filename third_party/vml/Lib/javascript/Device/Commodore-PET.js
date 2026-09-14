/**
 * Commodore-PET 寄存器定义
 * 生成自: Commodore International/PET/Commodore-PET
 * 版本: 1.0
 */
export const commodore_pet = {
  // CPU: MOS 6502, 8位, 1000000 Hz

  // 寄存器定义
  // Accumulator
  A: 0,
  // Index Register X
  X: 0,
  // Index Register Y
  Y: 0,
  // Stack Pointer
  SP: 0,
  // Program Counter
  PC: 0,
  // Status Register
  P: 0,

  // 外设定义
  // Peripheral Interface Adapter 1 (6520)
  PIA1_BASE: ,
  PIA1_PIA1_DDRA: 0x0000E810,
  PIA1_PIA1_ORA: 0x0000E811,
  PIA1_PIA1_DDRB: 0x0000E812,
  PIA1_PIA1_ORB: 0x0000E813,
  PIA1_PIA1_CRA: 0x0000E814,
  PIA1_PIA1_CRB: 0x0000E815,
  // Peripheral Interface Adapter 2 (6520)
  PIA2_BASE: ,
  PIA2_PIA2_DDRA: 0x0000E820,
  PIA2_PIA2_ORA: 0x0000E821,
  PIA2_PIA2_DDRB: 0x0000E822,
  PIA2_PIA2_ORB: 0x0000E823,
  PIA2_PIA2_CRA: 0x0000E824,
  PIA2_PIA2_CRB: 0x0000E825,
  // Versatile Interface Adapter (6522)
  VIA_BASE: ,
  VIA_VIA_ORB: 0x0000E840,
  VIA_VIA_ORA: 0x0000E841,
  VIA_VIA_DDRB: 0x0000E842,
  VIA_VIA_DDRA: 0x0000E843,
  VIA_VIA_T1CL: 0x0000E844,
  VIA_VIA_T1CH: 0x0000E845,
  VIA_VIA_T1LL: 0x0000E846,
  VIA_VIA_T1LH: 0x0000E847,
  VIA_VIA_T2CL: 0x0000E848,
  VIA_VIA_T2CH: 0x0000E849,
  VIA_VIA_SR: 0x0000E84A,
  VIA_VIA_ACR: 0x0000E84B,
  VIA_VIA_PCR: 0x0000E84C,
  VIA_VIA_IFR: 0x0000E84D,
  VIA_VIA_IER: 0x0000E84E,
  // CRT Controller (6545)
  CRTC_BASE: ,
  CRTC_CRTC_ADDR: 0x0000E880,
  CRTC_CRTC_DATA: 0x0000E881,
  // Cassette tape interface
  Cassette_BASE: ,
  Cassette_CASS_MOTOR: 0x0000E840,
  Cassette_CASS_WRITE: 0x0000E842,
  Cassette_CASS_READ: 0x0000E812,
  // IEEE-488 bus interface
  IEEE488_BASE: ,
  IEEE488_IEEE_DATA: 0x0000E801,
  IEEE488_IEEE_STATUS: 0x0000E802,
  IEEE488_IEEE_CONTROL: 0x0000E803,

  // 中断向量
  IRQ_NMI: 65526,  // Non-maskable interrupt
  IRQ_RESET: 65528,  // Reset vector
  IRQ_IRQ: 65530,  // Interrupt request
  IRQ_BRK: 65532,  // Break instruction

  init: function() {
    // 硬件初始化
  }
};
