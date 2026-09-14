/**
 * MOS-6507 寄存器定义
 * 生成自: MOS Technology/MOS-6502/MOS-6507
 * 版本: 1.0
 */
export const mos_6507 = {
  // CPU: MOS-6507, 8位, 1190000 Hz

  // 寄存器定义
  // Accumulator
  A: 0x00,
  // X Index
  X: 0x01,
  // Y Index
  Y: 0x02,
  // Stack Pointer (6-bit, 128-byte stack)
  SP: 0x03,
  // Program Counter (16-bit)
  PC: 0x04,
  // Processor Status
  P: 0x06,
  P_N: 7,  // Negative
  P_V: 6,  // Overflow
  P_B: 4,  // Break
  P_D: 3,  // Decimal Mode (N/A on 6507)
  P_I: 2,  // Interrupt Disable
  P_Z: 1,  // Zero
  P_C: 0,  // Carry

  // 内存段
  // TIA Registers
  tia_regs_START: 0x0000,
  tia_regs_END: 0x007F,
  tia_regs_SIZE: 128,
  // RIOT 128byte RAM mirrored
  riot_ram_START: 0x0080,
  riot_ram_END: 0x00FF,
  riot_ram_SIZE: 128,
  // RIOT I/O Registers (SWCHA/SWACNT/SWCHB/SWBCNT/INTIM)
  riot_io_START: 0x0280,
  riot_io_END: 0x029F,
  riot_io_SIZE: 32,
  // Cartridge ROM (4KB, bank-switched)
  cart_rom_START: 0x1000,
  cart_rom_END: 0x1FFF,
  cart_rom_SIZE: 4096,

  // 外设定义
  // Television Interface Adaptor (Video + Audio + I/O)
  TIA_BASE: 0x0000,
  TIA_VSYNC: 0x00000000,
  TIA_VBLANK: 0x00000001,
  TIA_VBLANK_D7: 7,  // Inhibit D7 (1=disable D7 output to PB7)
  TIA_VBLANK_D6: 6,  // Inhibit D6 (1=disable D6 output to PB6)
  TIA_VBLANK_D5: 5,  // Inhibit D5 (1=disable D5 output to PB5)
  TIA_VBLANK_D4: 4,  // Inhibit D4 (1=disable D4 output to PB4)
  TIA_VBLANK_D3: 3,  // Inhibit D3 (1=disable D3 output to PB3)
  TIA_VBLANK_D2: 2,  // Inhibit D2 (1=disable D2 output to PB2)
  TIA_VBLANK_D1: 1,  // Inhibit D1 (1=disable D1 output to PB1)
  TIA_VBLANK_D0: 0,  // Inhibit D0 (1=disable D0 output to PB0)
  TIA_VBLANK_VBW: 5,  // Vertical Blank Enable (1=set VBLANK)
  TIA_VBLANK_VBL: 1,  // Vertical Blank Set (1=V-Blank active)
  TIA_VBLANK_RESBL: 0,  // Reset Blank (1=allow VSYNC/VBLANK reset on clock)
  TIA_WSYNC: 0x00000002,
  TIA_RSYNC: 0x00000003,
  TIA_NUSIZ0: 0x00000004,
  TIA_NUSIZ0_NUSIZ: 0,  // Number/Size Code (0-7)
  TIA_NUSIZ0_MISSILE_SIZE: 0,  // Missile Size
  TIA_NUSIZ0_RESM0: 6,  // Reset M0
  TIA_NUSIZ0_RESM1: 7,  // Reset M1
  TIA_NUSIZ1: 0x00000005,
  TIA_COLUP0: 0x00000006,
  TIA_COLUP1: 0x00000007,
  TIA_COLUPF: 0x00000008,
  TIA_COLUBK: 0x00000009,
  TIA_CTRLPF: 0x0000000A,
  TIA_CTRLPF_DELL: 0,  // Delay Playfield L (Reflected/Left score)
  TIA_CTRLPF_BALL_SIZE: 0,  // Ball Size (0=1, 1=2, 2=3, 3=4, 4=5, 5=6, 6=7, 7=8 clocks)
  TIA_CTRLPF_REF: 5,  // Reflect (1=mirror playfield)
  TIA_CTRLPF_SCORE: 6,  // Score Mode (1=use player colors for L/R halves)
  TIA_CTRLPF_DELBL: 7,  // Delay Ball (1=delay ball 1 clock)
  TIA_REFPL: 0x0000000B,
  TIA_PF0: 0x0000000D,
  TIA_PF1: 0x0000000E,
  TIA_PF2: 0x0000000F,
  TIA_RESP0: 0x00000010,
  TIA_RESP1: 0x00000011,
  TIA_RESM0: 0x00000012,
  TIA_RESM1: 0x00000013,
  TIA_RESBL: 0x00000014,
  TIA_AUDC0: 0x00000015,
  TIA_AUDC0_VOL: 0,  // Volume (0-15)
  TIA_AUDC0_TONE: 0,  // Tone Divisor (5-bit counter)
  TIA_AUDC1: 0x00000016,
  TIA_AUDF0: 0x00000017,
  TIA_AUDF1: 0x00000018,
  TIA_AUDV0: 0x00000019,
  TIA_AUDV1: 0x0000001A,
  TIA_GRP0: 0x0000001B,
  TIA_GRP1: 0x0000001C,
  TIA_DGRP0: 0x0000001D,
  TIA_DGRP1: 0x0000001E,
  TIA_ENAM0: 0x0000001F,
  TIA_ENAM1: 0x00000020,
  TIA_ENABL: 0x00000021,
  TIA_HMP0: 0x00000022,
  TIA_HMP1: 0x00000023,
  TIA_HMM0: 0x00000024,
  TIA_HMM1: 0x00000025,
  TIA_HMBL: 0x00000026,
  TIA_VDEL0: 0x00000027,
  TIA_VDEL1: 0x00000028,
  TIA_VDELBL: 0x00000029,
  TIA_RESBB: 0x0000002A,
  TIA_HMOVE: 0x0000002A,
  TIA_HMCLR: 0x0000002B,
  TIA_CXM0P: 0x00000030,
  TIA_CXM1P: 0x00000031,
  TIA_CXP0FB: 0x00000032,
  TIA_CXP1FB: 0x00000033,
  TIA_CXM0FB: 0x00000034,
  TIA_CXM1FB: 0x00000035,
  TIA_CXBLPF: 0x00000036,
  TIA_CXPPMM: 0x00000037,
  TIA_INPT0: 0x00000038,
  TIA_INPT1: 0x00000039,
  TIA_INPT2: 0x0000003A,
  TIA_INPT3: 0x0000003B,
  TIA_INPT4: 0x0000003C,
  TIA_INPT5: 0x0000003D,
  // RAM, I/O, Timer (6532 RIOT)
  RIOT_BASE: 0x0080,
  RIOT_SWCHA: 0x00000300,
  RIOT_SWACNT: 0x00000301,
  RIOT_SWCHB: 0x00000302,
  RIOT_SWCHB_RESET: 1,  // Game Reset Switch (0=pressed)
  RIOT_SWCHB_SELECT: 2,  // Game Select Switch (0=pressed)
  RIOT_SWCHB_DIFFB: 3,  // Difficulty B (0=hard, 1=easy)
  RIOT_SWCHB_DIFFA: 4,  // Difficulty A (0=hard, 1=easy)
  RIOT_SWBCNT: 0x00000303,
  RIOT_INTIM: 0x00000304,
  RIOT_TIMINT: 0x00000305,
  RIOT_TIM1T: 0x00000314,
  RIOT_TIM8T: 0x00000315,
  RIOT_TIM64T: 0x00000316,
  RIOT_TIM1024T: 0x00000317,
  // Controller Port 1 (Joystick)
  CONTROLLER1_BASE: 0x280,
  CONTROLLER1_SWCHA: 0x00000500,
  // Controller Port 2 (Joystick)
  CONTROLLER2_BASE: 0x281,
  CONTROLLER2_SWCHA: 0x00000501,

  // 中断向量
  IRQ_RESET: 0,  // Power-On Reset

  // 引脚定义
  PIN_VSS: 1,  // Ground
  PIN_VCC: 2,  // Power Supply
  PIN_PHI0: 3,  // Clock Input (1.19MHz NTSC / 1.18MHz PAL)
  PIN_RESET: 4,  // Reset (active low)
  PIN_A0: 5,  // Address Bus Bit 0
  PIN_A1: 6,  // Address Bus Bit 1
  PIN_A2: 7,  // Address Bus Bit 2
  PIN_A3: 8,  // Address Bus Bit 3
  PIN_A4: 9,  // Address Bus Bit 4
  PIN_A5: 10,  // Address Bus Bit 5
  PIN_A6: 11,  // Address Bus Bit 6
  PIN_A7: 12,  // Address Bus Bit 7
  PIN_A8: 13,  // Address Bus Bit 8
  PIN_A9: 14,  // Address Bus Bit 9
  PIN_A10: 15,  // Address Bus Bit 10
  PIN_A11: 16,  // Address Bus Bit 11
  PIN_A12: 17,  // Address Bus Bit 12
  PIN_D0: 18,  // Data Bus Bit 0
  PIN_D1: 19,  // Data Bus Bit 1
  PIN_D2: 20,  // Data Bus Bit 2
  PIN_D3: 21,  // Data Bus Bit 3
  PIN_D4: 22,  // Data Bus Bit 4
  PIN_D5: 23,  // Data Bus Bit 5
  PIN_D6: 24,  // Data Bus Bit 6
  PIN_D7: 25,  // Data Bus Bit 7
  PIN_RDY: 26,  // Ready (stops CPU on read)
  PIN_R_W: 27,  // Read/Write (1=Read, 0=Write)
  PIN_NC: 28,  // Not Connected

  init: function() {
    // 硬件初始化
  }
};
