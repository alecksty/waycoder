/**
 * Macintosh-128K 寄存器定义
 * 生成自: Apple Computer/Macintosh/Macintosh-128K
 * 版本: 1.0
 */
export const macintosh_128k = {
  // CPU: Motorola 68000, 32位, 7998000 Hz

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
  // Versatile Interface Adapter (6522)
  VIA_BASE: ,
  VIA_VIA_ORB: 0x00E80000,
  VIA_VIA_ORA: 0x00E80001,
  VIA_VIA_DDRB: 0x00E80002,
  VIA_VIA_DDRA: 0x00E80003,
  VIA_VIA_T1CL: 0x00E80004,
  VIA_VIA_T1CH: 0x00E80005,
  VIA_VIA_T1LL: 0x00E80006,
  VIA_VIA_T1LH: 0x00E80007,
  VIA_VIA_T2CL: 0x00E80008,
  VIA_VIA_T2CH: 0x00E80009,
  VIA_VIA_SR: 0x00E8000A,
  VIA_VIA_ACR: 0x00E8000B,
  VIA_VIA_PCR: 0x00E8000C,
  VIA_VIA_IFR: 0x00E8000D,
  VIA_VIA_IER: 0x00E8000E,
  VIA_VIA_ORA2: 0x00E8000F,
  // Integrated Woz Machine (floppy controller)
  IWM_BASE: ,
  IWM_IWM_Q6: 0x00D00000,
  IWM_IWM_Q7: 0x00D00002,
  IWM_IWM_PH0: 0x00D00004,
  IWM_IWM_PH1: 0x00D00006,
  IWM_IWM_PH2: 0x00D00008,
  IWM_IWM_PH3: 0x00D0000A,
  // Zilog 8530 Serial Communications Controller
  SCC_BASE: ,
  SCC_SCC_CA: 0x00500000,
  SCC_SCC_DA: 0x00500002,
  SCC_SCC_CB: 0x00500004,
  SCC_SCC_DB: 0x00500006,
  // Built-in speaker
  Sound_BASE: ,
  Sound_SOUND_VOL: 0x00E80100,
  Sound_SOUND_FREQ: 0x00E80102,

  // 中断向量
  IRQ_RESET_SP: 0,  // Reset (Initial SP)
  IRQ_RESET_PC: 4,  // Reset (Initial PC)
  IRQ_AUTOVECTOR1: 24,  // Auto vector 1
  IRQ_AUTOVECTOR2: 25,  // Auto vector 2
  IRQ_AUTOVECTOR3: 26,  // Auto vector 3
  IRQ_AUTOVECTOR4: 27,  // Auto vector 4
  IRQ_AUTOVECTOR5: 28,  // Auto vector 5
  IRQ_AUTOVECTOR6: 29,  // Auto vector 6
  IRQ_AUTOVECTOR7: 30,  // Auto vector 7
  IRQ_SPURIOUS: 31,  // Spurious interrupt

  init: function() {
    // 硬件初始化
  }
};
