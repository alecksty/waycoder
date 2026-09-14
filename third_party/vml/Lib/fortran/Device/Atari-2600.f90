! MOS-6507 设备定义 - Fortran 模块
! 生成自: MOS Technology/MOS-6502/MOS-6507
! 版本: 1.0
! 日期: 2026-04-16
! 作者: VML Team
! 描述: Atari 2600 VCS main processor - MOS 6507 (simplified 6502) @ 1.19MHz with TIA and RIOT
! CPU架构: MOS-6507
! 位宽: 8位
! 时钟频率: 1190000 Hz

module mos_6507_device
  implicit none

  ! 寄存器地址定义
  integer, parameter :: A_ADDR = 0x00  ! Accumulator
  integer, parameter :: X_ADDR = 0x01  ! X Index
  integer, parameter :: Y_ADDR = 0x02  ! Y Index
  integer, parameter :: SP_ADDR = 0x03  ! Stack Pointer (6-bit, 128-byte stack)
  integer, parameter :: PC_ADDR = 0x04  ! Program Counter (16-bit)
  integer, parameter :: P_ADDR = 0x06  ! Processor Status
  integer, parameter :: P_N_BIT = 7  ! Negative
  integer, parameter :: P_V_BIT = 6  ! Overflow
  integer, parameter :: P_B_BIT = 4  ! Break
  integer, parameter :: P_D_BIT = 3  ! Decimal Mode (N/A on 6507)
  integer, parameter :: P_I_BIT = 2  ! Interrupt Disable
  integer, parameter :: P_Z_BIT = 1  ! Zero
  integer, parameter :: P_C_BIT = 0  ! Carry

  ! 内存段定义
  integer, parameter :: TIA_REGS_START = 0x0000
  integer, parameter :: TIA_REGS_END = 0x007F
  integer, parameter :: TIA_REGS_SIZE = 128  ! TIA Registers
  integer, parameter :: RIOT_RAM_START = 0x0080
  integer, parameter :: RIOT_RAM_END = 0x00FF
  integer, parameter :: RIOT_RAM_SIZE = 128  ! RIOT 128byte RAM mirrored
  integer, parameter :: RIOT_IO_START = 0x0280
  integer, parameter :: RIOT_IO_END = 0x029F
  integer, parameter :: RIOT_IO_SIZE = 32  ! RIOT I/O Registers (SWCHA/SWACNT/SWCHB/SWBCNT/INTIM)
  integer, parameter :: CART_ROM_START = 0x1000
  integer, parameter :: CART_ROM_END = 0x1FFF
  integer, parameter :: CART_ROM_SIZE = 4096  ! Cartridge ROM (4KB, bank-switched)

  ! 外设定义
  ! Television Interface Adaptor (Video + Audio + I/O)
  integer, parameter :: TIA_BASE = 0x0000
  integer, parameter :: TIA_VSYNC_ADDR = 0x00
  integer, parameter :: TIA_VBLANK_ADDR = 0x01
  integer, parameter :: TIA_VBLANK_D7_BIT = 7  ! Inhibit D7 (1=disable D7 output to PB7)
  integer, parameter :: TIA_VBLANK_D6_BIT = 6  ! Inhibit D6 (1=disable D6 output to PB6)
  integer, parameter :: TIA_VBLANK_D5_BIT = 5  ! Inhibit D5 (1=disable D5 output to PB5)
  integer, parameter :: TIA_VBLANK_D4_BIT = 4  ! Inhibit D4 (1=disable D4 output to PB4)
  integer, parameter :: TIA_VBLANK_D3_BIT = 3  ! Inhibit D3 (1=disable D3 output to PB3)
  integer, parameter :: TIA_VBLANK_D2_BIT = 2  ! Inhibit D2 (1=disable D2 output to PB2)
  integer, parameter :: TIA_VBLANK_D1_BIT = 1  ! Inhibit D1 (1=disable D1 output to PB1)
  integer, parameter :: TIA_VBLANK_D0_BIT = 0  ! Inhibit D0 (1=disable D0 output to PB0)
  integer, parameter :: TIA_VBLANK_VBW_BIT = 5  ! Vertical Blank Enable (1=set VBLANK)
  integer, parameter :: TIA_VBLANK_VBL_BIT = 1  ! Vertical Blank Set (1=V-Blank active)
  integer, parameter :: TIA_VBLANK_RESBL_BIT = 0  ! Reset Blank (1=allow VSYNC/VBLANK reset on clock)
  integer, parameter :: TIA_WSYNC_ADDR = 0x02
  integer, parameter :: TIA_RSYNC_ADDR = 0x03
  integer, parameter :: TIA_NUSIZ0_ADDR = 0x04
  integer, parameter :: TIA_NUSIZ0_NUSIZ_BIT = 0  ! Number/Size Code (0-7)
  integer, parameter :: TIA_NUSIZ0_MISSILE_SIZE_BIT = 0  ! Missile Size
  integer, parameter :: TIA_NUSIZ0_RESM0_BIT = 6  ! Reset M0
  integer, parameter :: TIA_NUSIZ0_RESM1_BIT = 7  ! Reset M1
  integer, parameter :: TIA_NUSIZ1_ADDR = 0x05
  integer, parameter :: TIA_COLUP0_ADDR = 0x06
  integer, parameter :: TIA_COLUP1_ADDR = 0x07
  integer, parameter :: TIA_COLUPF_ADDR = 0x08
  integer, parameter :: TIA_COLUBK_ADDR = 0x09
  integer, parameter :: TIA_CTRLPF_ADDR = 0x0A
  integer, parameter :: TIA_CTRLPF_DELL_BIT = 0  ! Delay Playfield L (Reflected/Left score)
  integer, parameter :: TIA_CTRLPF_BALL_SIZE_BIT = 0  ! Ball Size (0=1, 1=2, 2=3, 3=4, 4=5, 5=6, 6=7, 7=8 clocks)
  integer, parameter :: TIA_CTRLPF_REF_BIT = 5  ! Reflect (1=mirror playfield)
  integer, parameter :: TIA_CTRLPF_SCORE_BIT = 6  ! Score Mode (1=use player colors for L/R halves)
  integer, parameter :: TIA_CTRLPF_DELBL_BIT = 7  ! Delay Ball (1=delay ball 1 clock)
  integer, parameter :: TIA_REFPL_ADDR = 0x0B
  integer, parameter :: TIA_PF0_ADDR = 0x0D
  integer, parameter :: TIA_PF1_ADDR = 0x0E
  integer, parameter :: TIA_PF2_ADDR = 0x0F
  integer, parameter :: TIA_RESP0_ADDR = 0x10
  integer, parameter :: TIA_RESP1_ADDR = 0x11
  integer, parameter :: TIA_RESM0_ADDR = 0x12
  integer, parameter :: TIA_RESM1_ADDR = 0x13
  integer, parameter :: TIA_RESBL_ADDR = 0x14
  integer, parameter :: TIA_AUDC0_ADDR = 0x15
  integer, parameter :: TIA_AUDC0_VOL_BIT = 0  ! Volume (0-15)
  integer, parameter :: TIA_AUDC0_TONE_BIT = 0  ! Tone Divisor (5-bit counter)
  integer, parameter :: TIA_AUDC1_ADDR = 0x16
  integer, parameter :: TIA_AUDF0_ADDR = 0x17
  integer, parameter :: TIA_AUDF1_ADDR = 0x18
  integer, parameter :: TIA_AUDV0_ADDR = 0x19
  integer, parameter :: TIA_AUDV1_ADDR = 0x1A
  integer, parameter :: TIA_GRP0_ADDR = 0x1B
  integer, parameter :: TIA_GRP1_ADDR = 0x1C
  integer, parameter :: TIA_DGRP0_ADDR = 0x1D
  integer, parameter :: TIA_DGRP1_ADDR = 0x1E
  integer, parameter :: TIA_ENAM0_ADDR = 0x1F
  integer, parameter :: TIA_ENAM1_ADDR = 0x20
  integer, parameter :: TIA_ENABL_ADDR = 0x21
  integer, parameter :: TIA_HMP0_ADDR = 0x22
  integer, parameter :: TIA_HMP1_ADDR = 0x23
  integer, parameter :: TIA_HMM0_ADDR = 0x24
  integer, parameter :: TIA_HMM1_ADDR = 0x25
  integer, parameter :: TIA_HMBL_ADDR = 0x26
  integer, parameter :: TIA_VDEL0_ADDR = 0x27
  integer, parameter :: TIA_VDEL1_ADDR = 0x28
  integer, parameter :: TIA_VDELBL_ADDR = 0x29
  integer, parameter :: TIA_RESBB_ADDR = 0x2A
  integer, parameter :: TIA_HMOVE_ADDR = 0x2A
  integer, parameter :: TIA_HMCLR_ADDR = 0x2B
  integer, parameter :: TIA_CXM0P_ADDR = 0x30
  integer, parameter :: TIA_CXM1P_ADDR = 0x31
  integer, parameter :: TIA_CXP0FB_ADDR = 0x32
  integer, parameter :: TIA_CXP1FB_ADDR = 0x33
  integer, parameter :: TIA_CXM0FB_ADDR = 0x34
  integer, parameter :: TIA_CXM1FB_ADDR = 0x35
  integer, parameter :: TIA_CXBLPF_ADDR = 0x36
  integer, parameter :: TIA_CXPPMM_ADDR = 0x37
  integer, parameter :: TIA_INPT0_ADDR = 0x38
  integer, parameter :: TIA_INPT1_ADDR = 0x39
  integer, parameter :: TIA_INPT2_ADDR = 0x3A
  integer, parameter :: TIA_INPT3_ADDR = 0x3B
  integer, parameter :: TIA_INPT4_ADDR = 0x3C
  integer, parameter :: TIA_INPT5_ADDR = 0x3D
  ! RAM, I/O, Timer (6532 RIOT)
  integer, parameter :: RIOT_BASE = 0x0080
  integer, parameter :: RIOT_SWCHA_ADDR = 0x280
  integer, parameter :: RIOT_SWACNT_ADDR = 0x281
  integer, parameter :: RIOT_SWCHB_ADDR = 0x282
  integer, parameter :: RIOT_SWCHB_RESET_BIT = 1  ! Game Reset Switch (0=pressed)
  integer, parameter :: RIOT_SWCHB_SELECT_BIT = 2  ! Game Select Switch (0=pressed)
  integer, parameter :: RIOT_SWCHB_DIFFB_BIT = 3  ! Difficulty B (0=hard, 1=easy)
  integer, parameter :: RIOT_SWCHB_DIFFA_BIT = 4  ! Difficulty A (0=hard, 1=easy)
  integer, parameter :: RIOT_SWBCNT_ADDR = 0x283
  integer, parameter :: RIOT_INTIM_ADDR = 0x284
  integer, parameter :: RIOT_TIMINT_ADDR = 0x285
  integer, parameter :: RIOT_TIM1T_ADDR = 0x294
  integer, parameter :: RIOT_TIM8T_ADDR = 0x295
  integer, parameter :: RIOT_TIM64T_ADDR = 0x296
  integer, parameter :: RIOT_TIM1024T_ADDR = 0x297
  ! Controller Port 1 (Joystick)
  integer, parameter :: CONTROLLER1_BASE = 0x280
  integer, parameter :: CONTROLLER1_SWCHA_ADDR = 0x280
  ! Controller Port 2 (Joystick)
  integer, parameter :: CONTROLLER2_BASE = 0x281
  integer, parameter :: CONTROLLER2_SWCHA_ADDR = 0x280

  ! 中断向量定义
  integer, parameter :: INT_RESET = 0  ! Power-On Reset

  ! 引脚定义
  integer, parameter :: PIN_VSS = 1  ! Ground
  integer, parameter :: PIN_VCC = 2  ! Power Supply
  integer, parameter :: PIN_PHI0 = 3  ! Clock Input (1.19MHz NTSC / 1.18MHz PAL)
  integer, parameter :: PIN_RESET = 4  ! Reset (active low)
  integer, parameter :: PIN_A0 = 5  ! Address Bus Bit 0
  integer, parameter :: PIN_A1 = 6  ! Address Bus Bit 1
  integer, parameter :: PIN_A2 = 7  ! Address Bus Bit 2
  integer, parameter :: PIN_A3 = 8  ! Address Bus Bit 3
  integer, parameter :: PIN_A4 = 9  ! Address Bus Bit 4
  integer, parameter :: PIN_A5 = 10  ! Address Bus Bit 5
  integer, parameter :: PIN_A6 = 11  ! Address Bus Bit 6
  integer, parameter :: PIN_A7 = 12  ! Address Bus Bit 7
  integer, parameter :: PIN_A8 = 13  ! Address Bus Bit 8
  integer, parameter :: PIN_A9 = 14  ! Address Bus Bit 9
  integer, parameter :: PIN_A10 = 15  ! Address Bus Bit 10
  integer, parameter :: PIN_A11 = 16  ! Address Bus Bit 11
  integer, parameter :: PIN_A12 = 17  ! Address Bus Bit 12
  integer, parameter :: PIN_D0 = 18  ! Data Bus Bit 0
  integer, parameter :: PIN_D1 = 19  ! Data Bus Bit 1
  integer, parameter :: PIN_D2 = 20  ! Data Bus Bit 2
  integer, parameter :: PIN_D3 = 21  ! Data Bus Bit 3
  integer, parameter :: PIN_D4 = 22  ! Data Bus Bit 4
  integer, parameter :: PIN_D5 = 23  ! Data Bus Bit 5
  integer, parameter :: PIN_D6 = 24  ! Data Bus Bit 6
  integer, parameter :: PIN_D7 = 25  ! Data Bus Bit 7
  integer, parameter :: PIN_RDY = 26  ! Ready (stops CPU on read)
  integer, parameter :: PIN_R_W = 27  ! Read/Write (1=Read, 0=Write)
  integer, parameter :: PIN_NC = 28  ! Not Connected

end module mos_6507_device
