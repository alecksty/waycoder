// MOS-6507 设备定义 - Dart 库
// 生成自: MOS Technology/MOS-6502/MOS-6507
// 版本: 1.0
// 日期: 2026-04-16
// 作者: VML Team
// 描述: Atari 2600 VCS main processor - MOS 6507 (simplified 6502) @ 1.19MHz with TIA and RIOT
// CPU架构: MOS-6507
// 位宽: 8位
// 时钟频率: 1190000 Hz

class MOS_6507Device {
  static const String deviceName = "MOS-6507";
  static const String manufacturer = "MOS Technology";
  static const String family = "MOS-6502";
  static const String version = "1.0";
  static const String architecture = "MOS-6507";
  static const int bits = 8;
  static const int clockFrequency = 1190000;

  // 寄存器地址定义
  static const int A_ADDR = 0x00;  // Accumulator
  static const int X_ADDR = 0x01;  // X Index
  static const int Y_ADDR = 0x02;  // Y Index
  static const int SP_ADDR = 0x03;  // Stack Pointer (6-bit, 128-byte stack)
  static const int PC_ADDR = 0x04;  // Program Counter (16-bit)
  static const int P_ADDR = 0x06;  // Processor Status
  static const int P_N_BIT = 7;  // Negative
  static const int P_V_BIT = 6;  // Overflow
  static const int P_B_BIT = 4;  // Break
  static const int P_D_BIT = 3;  // Decimal Mode (N/A on 6507)
  static const int P_I_BIT = 2;  // Interrupt Disable
  static const int P_Z_BIT = 1;  // Zero
  static const int P_C_BIT = 0;  // Carry

  // 内存段定义
  static const int TIA_REGS_START = 0x0000;
  static const int TIA_REGS_END = 0x007F;
  static const int TIA_REGS_SIZE = 128;  // TIA Registers
  static const int RIOT_RAM_START = 0x0080;
  static const int RIOT_RAM_END = 0x00FF;
  static const int RIOT_RAM_SIZE = 128;  // RIOT 128byte RAM mirrored
  static const int RIOT_IO_START = 0x0280;
  static const int RIOT_IO_END = 0x029F;
  static const int RIOT_IO_SIZE = 32;  // RIOT I/O Registers (SWCHA/SWACNT/SWCHB/SWBCNT/INTIM)
  static const int CART_ROM_START = 0x1000;
  static const int CART_ROM_END = 0x1FFF;
  static const int CART_ROM_SIZE = 4096;  // Cartridge ROM (4KB, bank-switched)

  // 外设定义
  // Television Interface Adaptor (Video + Audio + I/O)
  static const int TIA_BASE = 0x0000;
  static const int TIA_VSYNC_ADDR = 0x00;
  static const int TIA_VBLANK_ADDR = 0x01;
  static const int TIA_VBLANK_D7_BIT = 7;  // Inhibit D7 (1=disable D7 output to PB7)
  static const int TIA_VBLANK_D6_BIT = 6;  // Inhibit D6 (1=disable D6 output to PB6)
  static const int TIA_VBLANK_D5_BIT = 5;  // Inhibit D5 (1=disable D5 output to PB5)
  static const int TIA_VBLANK_D4_BIT = 4;  // Inhibit D4 (1=disable D4 output to PB4)
  static const int TIA_VBLANK_D3_BIT = 3;  // Inhibit D3 (1=disable D3 output to PB3)
  static const int TIA_VBLANK_D2_BIT = 2;  // Inhibit D2 (1=disable D2 output to PB2)
  static const int TIA_VBLANK_D1_BIT = 1;  // Inhibit D1 (1=disable D1 output to PB1)
  static const int TIA_VBLANK_D0_BIT = 0;  // Inhibit D0 (1=disable D0 output to PB0)
  static const int TIA_VBLANK_VBW_BIT = 5;  // Vertical Blank Enable (1=set VBLANK)
  static const int TIA_VBLANK_VBL_BIT = 1;  // Vertical Blank Set (1=V-Blank active)
  static const int TIA_VBLANK_RESBL_BIT = 0;  // Reset Blank (1=allow VSYNC/VBLANK reset on clock)
  static const int TIA_WSYNC_ADDR = 0x02;
  static const int TIA_RSYNC_ADDR = 0x03;
  static const int TIA_NUSIZ0_ADDR = 0x04;
  static const int TIA_NUSIZ0_NUSIZ_BIT = 0;  // Number/Size Code (0-7)
  static const int TIA_NUSIZ0_MISSILE_SIZE_BIT = 0;  // Missile Size
  static const int TIA_NUSIZ0_RESM0_BIT = 6;  // Reset M0
  static const int TIA_NUSIZ0_RESM1_BIT = 7;  // Reset M1
  static const int TIA_NUSIZ1_ADDR = 0x05;
  static const int TIA_COLUP0_ADDR = 0x06;
  static const int TIA_COLUP1_ADDR = 0x07;
  static const int TIA_COLUPF_ADDR = 0x08;
  static const int TIA_COLUBK_ADDR = 0x09;
  static const int TIA_CTRLPF_ADDR = 0x0A;
  static const int TIA_CTRLPF_DELL_BIT = 0;  // Delay Playfield L (Reflected/Left score)
  static const int TIA_CTRLPF_BALL_SIZE_BIT = 0;  // Ball Size (0=1, 1=2, 2=3, 3=4, 4=5, 5=6, 6=7, 7=8 clocks)
  static const int TIA_CTRLPF_REF_BIT = 5;  // Reflect (1=mirror playfield)
  static const int TIA_CTRLPF_SCORE_BIT = 6;  // Score Mode (1=use player colors for L/R halves)
  static const int TIA_CTRLPF_DELBL_BIT = 7;  // Delay Ball (1=delay ball 1 clock)
  static const int TIA_REFPL_ADDR = 0x0B;
  static const int TIA_PF0_ADDR = 0x0D;
  static const int TIA_PF1_ADDR = 0x0E;
  static const int TIA_PF2_ADDR = 0x0F;
  static const int TIA_RESP0_ADDR = 0x10;
  static const int TIA_RESP1_ADDR = 0x11;
  static const int TIA_RESM0_ADDR = 0x12;
  static const int TIA_RESM1_ADDR = 0x13;
  static const int TIA_RESBL_ADDR = 0x14;
  static const int TIA_AUDC0_ADDR = 0x15;
  static const int TIA_AUDC0_VOL_BIT = 0;  // Volume (0-15)
  static const int TIA_AUDC0_TONE_BIT = 0;  // Tone Divisor (5-bit counter)
  static const int TIA_AUDC1_ADDR = 0x16;
  static const int TIA_AUDF0_ADDR = 0x17;
  static const int TIA_AUDF1_ADDR = 0x18;
  static const int TIA_AUDV0_ADDR = 0x19;
  static const int TIA_AUDV1_ADDR = 0x1A;
  static const int TIA_GRP0_ADDR = 0x1B;
  static const int TIA_GRP1_ADDR = 0x1C;
  static const int TIA_DGRP0_ADDR = 0x1D;
  static const int TIA_DGRP1_ADDR = 0x1E;
  static const int TIA_ENAM0_ADDR = 0x1F;
  static const int TIA_ENAM1_ADDR = 0x20;
  static const int TIA_ENABL_ADDR = 0x21;
  static const int TIA_HMP0_ADDR = 0x22;
  static const int TIA_HMP1_ADDR = 0x23;
  static const int TIA_HMM0_ADDR = 0x24;
  static const int TIA_HMM1_ADDR = 0x25;
  static const int TIA_HMBL_ADDR = 0x26;
  static const int TIA_VDEL0_ADDR = 0x27;
  static const int TIA_VDEL1_ADDR = 0x28;
  static const int TIA_VDELBL_ADDR = 0x29;
  static const int TIA_RESBB_ADDR = 0x2A;
  static const int TIA_HMOVE_ADDR = 0x2A;
  static const int TIA_HMCLR_ADDR = 0x2B;
  static const int TIA_CXM0P_ADDR = 0x30;
  static const int TIA_CXM1P_ADDR = 0x31;
  static const int TIA_CXP0FB_ADDR = 0x32;
  static const int TIA_CXP1FB_ADDR = 0x33;
  static const int TIA_CXM0FB_ADDR = 0x34;
  static const int TIA_CXM1FB_ADDR = 0x35;
  static const int TIA_CXBLPF_ADDR = 0x36;
  static const int TIA_CXPPMM_ADDR = 0x37;
  static const int TIA_INPT0_ADDR = 0x38;
  static const int TIA_INPT1_ADDR = 0x39;
  static const int TIA_INPT2_ADDR = 0x3A;
  static const int TIA_INPT3_ADDR = 0x3B;
  static const int TIA_INPT4_ADDR = 0x3C;
  static const int TIA_INPT5_ADDR = 0x3D;
  // RAM, I/O, Timer (6532 RIOT)
  static const int RIOT_BASE = 0x0080;
  static const int RIOT_SWCHA_ADDR = 0x280;
  static const int RIOT_SWACNT_ADDR = 0x281;
  static const int RIOT_SWCHB_ADDR = 0x282;
  static const int RIOT_SWCHB_RESET_BIT = 1;  // Game Reset Switch (0=pressed)
  static const int RIOT_SWCHB_SELECT_BIT = 2;  // Game Select Switch (0=pressed)
  static const int RIOT_SWCHB_DIFFB_BIT = 3;  // Difficulty B (0=hard, 1=easy)
  static const int RIOT_SWCHB_DIFFA_BIT = 4;  // Difficulty A (0=hard, 1=easy)
  static const int RIOT_SWBCNT_ADDR = 0x283;
  static const int RIOT_INTIM_ADDR = 0x284;
  static const int RIOT_TIMINT_ADDR = 0x285;
  static const int RIOT_TIM1T_ADDR = 0x294;
  static const int RIOT_TIM8T_ADDR = 0x295;
  static const int RIOT_TIM64T_ADDR = 0x296;
  static const int RIOT_TIM1024T_ADDR = 0x297;
  // Controller Port 1 (Joystick)
  static const int CONTROLLER1_BASE = 0x280;
  static const int CONTROLLER1_SWCHA_ADDR = 0x280;
  // Controller Port 2 (Joystick)
  static const int CONTROLLER2_BASE = 0x281;
  static const int CONTROLLER2_SWCHA_ADDR = 0x280;

  // 中断向量定义
  static const int INT_RESET = 0;  // Power-On Reset

  // 引脚定义
  static const int PIN_VSS = 1;  // Ground
  static const int PIN_VCC = 2;  // Power Supply
  static const int PIN_PHI0 = 3;  // Clock Input (1.19MHz NTSC / 1.18MHz PAL)
  static const int PIN_RESET = 4;  // Reset (active low)
  static const int PIN_A0 = 5;  // Address Bus Bit 0
  static const int PIN_A1 = 6;  // Address Bus Bit 1
  static const int PIN_A2 = 7;  // Address Bus Bit 2
  static const int PIN_A3 = 8;  // Address Bus Bit 3
  static const int PIN_A4 = 9;  // Address Bus Bit 4
  static const int PIN_A5 = 10;  // Address Bus Bit 5
  static const int PIN_A6 = 11;  // Address Bus Bit 6
  static const int PIN_A7 = 12;  // Address Bus Bit 7
  static const int PIN_A8 = 13;  // Address Bus Bit 8
  static const int PIN_A9 = 14;  // Address Bus Bit 9
  static const int PIN_A10 = 15;  // Address Bus Bit 10
  static const int PIN_A11 = 16;  // Address Bus Bit 11
  static const int PIN_A12 = 17;  // Address Bus Bit 12
  static const int PIN_D0 = 18;  // Data Bus Bit 0
  static const int PIN_D1 = 19;  // Data Bus Bit 1
  static const int PIN_D2 = 20;  // Data Bus Bit 2
  static const int PIN_D3 = 21;  // Data Bus Bit 3
  static const int PIN_D4 = 22;  // Data Bus Bit 4
  static const int PIN_D5 = 23;  // Data Bus Bit 5
  static const int PIN_D6 = 24;  // Data Bus Bit 6
  static const int PIN_D7 = 25;  // Data Bus Bit 7
  static const int PIN_RDY = 26;  // Ready (stops CPU on read)
  static const int PIN_R_W = 27;  // Read/Write (1=Read, 0=Write)
  static const int PIN_NC = 28;  // Not Connected

}
