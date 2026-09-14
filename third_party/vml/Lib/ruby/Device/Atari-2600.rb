# MOS-6507 设备定义 - Ruby 模块
# 生成自: MOS Technology/MOS-6502/MOS-6507
# 版本: 1.0
# 日期: 2026-04-16
# 作者: VML Team
# 描述: Atari 2600 VCS main processor - MOS 6507 (simplified 6502) @ 1.19MHz with TIA and RIOT
# CPU架构: MOS-6507
# 位宽: 8位
# 时钟频率: 1190000 Hz

module MOS_6507

  # 寄存器地址定义
  A_ADDR = 0x00  # Accumulator
  X_ADDR = 0x01  # X Index
  Y_ADDR = 0x02  # Y Index
  SP_ADDR = 0x03  # Stack Pointer (6-bit, 128-byte stack)
  PC_ADDR = 0x04  # Program Counter (16-bit)
  P_ADDR = 0x06  # Processor Status
  P_N_BIT = 7  # Negative
  P_V_BIT = 6  # Overflow
  P_B_BIT = 4  # Break
  P_D_BIT = 3  # Decimal Mode (N/A on 6507)
  P_I_BIT = 2  # Interrupt Disable
  P_Z_BIT = 1  # Zero
  P_C_BIT = 0  # Carry

  # 内存段定义
  TIA_REGS_START = 0x0000
  TIA_REGS_END = 0x007F
  TIA_REGS_SIZE = 128  # TIA Registers
  RIOT_RAM_START = 0x0080
  RIOT_RAM_END = 0x00FF
  RIOT_RAM_SIZE = 128  # RIOT 128byte RAM mirrored
  RIOT_IO_START = 0x0280
  RIOT_IO_END = 0x029F
  RIOT_IO_SIZE = 32  # RIOT I/O Registers (SWCHA/SWACNT/SWCHB/SWBCNT/INTIM)
  CART_ROM_START = 0x1000
  CART_ROM_END = 0x1FFF
  CART_ROM_SIZE = 4096  # Cartridge ROM (4KB, bank-switched)

  # 外设定义
  # Television Interface Adaptor (Video + Audio + I/O)
  TIA_BASE = 0x0000
  TIA_VSYNC_ADDR = 0x00
  TIA_VBLANK_ADDR = 0x01
  TIA_VBLANK_D7_BIT = 7  # Inhibit D7 (1=disable D7 output to PB7)
  TIA_VBLANK_D6_BIT = 6  # Inhibit D6 (1=disable D6 output to PB6)
  TIA_VBLANK_D5_BIT = 5  # Inhibit D5 (1=disable D5 output to PB5)
  TIA_VBLANK_D4_BIT = 4  # Inhibit D4 (1=disable D4 output to PB4)
  TIA_VBLANK_D3_BIT = 3  # Inhibit D3 (1=disable D3 output to PB3)
  TIA_VBLANK_D2_BIT = 2  # Inhibit D2 (1=disable D2 output to PB2)
  TIA_VBLANK_D1_BIT = 1  # Inhibit D1 (1=disable D1 output to PB1)
  TIA_VBLANK_D0_BIT = 0  # Inhibit D0 (1=disable D0 output to PB0)
  TIA_VBLANK_VBW_BIT = 5  # Vertical Blank Enable (1=set VBLANK)
  TIA_VBLANK_VBL_BIT = 1  # Vertical Blank Set (1=V-Blank active)
  TIA_VBLANK_RESBL_BIT = 0  # Reset Blank (1=allow VSYNC/VBLANK reset on clock)
  TIA_WSYNC_ADDR = 0x02
  TIA_RSYNC_ADDR = 0x03
  TIA_NUSIZ0_ADDR = 0x04
  TIA_NUSIZ0_NUSIZ_BIT = 0  # Number/Size Code (0-7)
  TIA_NUSIZ0_MISSILE_SIZE_BIT = 0  # Missile Size
  TIA_NUSIZ0_RESM0_BIT = 6  # Reset M0
  TIA_NUSIZ0_RESM1_BIT = 7  # Reset M1
  TIA_NUSIZ1_ADDR = 0x05
  TIA_COLUP0_ADDR = 0x06
  TIA_COLUP1_ADDR = 0x07
  TIA_COLUPF_ADDR = 0x08
  TIA_COLUBK_ADDR = 0x09
  TIA_CTRLPF_ADDR = 0x0A
  TIA_CTRLPF_DELL_BIT = 0  # Delay Playfield L (Reflected/Left score)
  TIA_CTRLPF_BALL_SIZE_BIT = 0  # Ball Size (0=1, 1=2, 2=3, 3=4, 4=5, 5=6, 6=7, 7=8 clocks)
  TIA_CTRLPF_REF_BIT = 5  # Reflect (1=mirror playfield)
  TIA_CTRLPF_SCORE_BIT = 6  # Score Mode (1=use player colors for L/R halves)
  TIA_CTRLPF_DELBL_BIT = 7  # Delay Ball (1=delay ball 1 clock)
  TIA_REFPL_ADDR = 0x0B
  TIA_PF0_ADDR = 0x0D
  TIA_PF1_ADDR = 0x0E
  TIA_PF2_ADDR = 0x0F
  TIA_RESP0_ADDR = 0x10
  TIA_RESP1_ADDR = 0x11
  TIA_RESM0_ADDR = 0x12
  TIA_RESM1_ADDR = 0x13
  TIA_RESBL_ADDR = 0x14
  TIA_AUDC0_ADDR = 0x15
  TIA_AUDC0_VOL_BIT = 0  # Volume (0-15)
  TIA_AUDC0_TONE_BIT = 0  # Tone Divisor (5-bit counter)
  TIA_AUDC1_ADDR = 0x16
  TIA_AUDF0_ADDR = 0x17
  TIA_AUDF1_ADDR = 0x18
  TIA_AUDV0_ADDR = 0x19
  TIA_AUDV1_ADDR = 0x1A
  TIA_GRP0_ADDR = 0x1B
  TIA_GRP1_ADDR = 0x1C
  TIA_DGRP0_ADDR = 0x1D
  TIA_DGRP1_ADDR = 0x1E
  TIA_ENAM0_ADDR = 0x1F
  TIA_ENAM1_ADDR = 0x20
  TIA_ENABL_ADDR = 0x21
  TIA_HMP0_ADDR = 0x22
  TIA_HMP1_ADDR = 0x23
  TIA_HMM0_ADDR = 0x24
  TIA_HMM1_ADDR = 0x25
  TIA_HMBL_ADDR = 0x26
  TIA_VDEL0_ADDR = 0x27
  TIA_VDEL1_ADDR = 0x28
  TIA_VDELBL_ADDR = 0x29
  TIA_RESBB_ADDR = 0x2A
  TIA_HMOVE_ADDR = 0x2A
  TIA_HMCLR_ADDR = 0x2B
  TIA_CXM0P_ADDR = 0x30
  TIA_CXM1P_ADDR = 0x31
  TIA_CXP0FB_ADDR = 0x32
  TIA_CXP1FB_ADDR = 0x33
  TIA_CXM0FB_ADDR = 0x34
  TIA_CXM1FB_ADDR = 0x35
  TIA_CXBLPF_ADDR = 0x36
  TIA_CXPPMM_ADDR = 0x37
  TIA_INPT0_ADDR = 0x38
  TIA_INPT1_ADDR = 0x39
  TIA_INPT2_ADDR = 0x3A
  TIA_INPT3_ADDR = 0x3B
  TIA_INPT4_ADDR = 0x3C
  TIA_INPT5_ADDR = 0x3D
  # RAM, I/O, Timer (6532 RIOT)
  RIOT_BASE = 0x0080
  RIOT_SWCHA_ADDR = 0x280
  RIOT_SWACNT_ADDR = 0x281
  RIOT_SWCHB_ADDR = 0x282
  RIOT_SWCHB_RESET_BIT = 1  # Game Reset Switch (0=pressed)
  RIOT_SWCHB_SELECT_BIT = 2  # Game Select Switch (0=pressed)
  RIOT_SWCHB_DIFFB_BIT = 3  # Difficulty B (0=hard, 1=easy)
  RIOT_SWCHB_DIFFA_BIT = 4  # Difficulty A (0=hard, 1=easy)
  RIOT_SWBCNT_ADDR = 0x283
  RIOT_INTIM_ADDR = 0x284
  RIOT_TIMINT_ADDR = 0x285
  RIOT_TIM1T_ADDR = 0x294
  RIOT_TIM8T_ADDR = 0x295
  RIOT_TIM64T_ADDR = 0x296
  RIOT_TIM1024T_ADDR = 0x297
  # Controller Port 1 (Joystick)
  CONTROLLER1_BASE = 0x280
  CONTROLLER1_SWCHA_ADDR = 0x280
  # Controller Port 2 (Joystick)
  CONTROLLER2_BASE = 0x281
  CONTROLLER2_SWCHA_ADDR = 0x280

  # 中断向量定义
  INT_RESET = 0  # Power-On Reset

  # 引脚定义
  PIN_VSS = 1  # Ground
  PIN_VCC = 2  # Power Supply
  PIN_PHI0 = 3  # Clock Input (1.19MHz NTSC / 1.18MHz PAL)
  PIN_RESET = 4  # Reset (active low)
  PIN_A0 = 5  # Address Bus Bit 0
  PIN_A1 = 6  # Address Bus Bit 1
  PIN_A2 = 7  # Address Bus Bit 2
  PIN_A3 = 8  # Address Bus Bit 3
  PIN_A4 = 9  # Address Bus Bit 4
  PIN_A5 = 10  # Address Bus Bit 5
  PIN_A6 = 11  # Address Bus Bit 6
  PIN_A7 = 12  # Address Bus Bit 7
  PIN_A8 = 13  # Address Bus Bit 8
  PIN_A9 = 14  # Address Bus Bit 9
  PIN_A10 = 15  # Address Bus Bit 10
  PIN_A11 = 16  # Address Bus Bit 11
  PIN_A12 = 17  # Address Bus Bit 12
  PIN_D0 = 18  # Data Bus Bit 0
  PIN_D1 = 19  # Data Bus Bit 1
  PIN_D2 = 20  # Data Bus Bit 2
  PIN_D3 = 21  # Data Bus Bit 3
  PIN_D4 = 22  # Data Bus Bit 4
  PIN_D5 = 23  # Data Bus Bit 5
  PIN_D6 = 24  # Data Bus Bit 6
  PIN_D7 = 25  # Data Bus Bit 7
  PIN_RDY = 26  # Ready (stops CPU on read)
  PIN_R_W = 27  # Read/Write (1=Read, 0=Write)
  PIN_NC = 28  # Not Connected

end
