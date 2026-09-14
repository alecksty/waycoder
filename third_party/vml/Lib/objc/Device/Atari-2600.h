// MOS-6507 设备定义 - Objective-C 头文件
// 生成自: MOS Technology/MOS-6502/MOS-6507
// 版本: 1.0
// 日期: 2026-04-16
// 作者: VML Team
// 描述: Atari 2600 VCS main processor - MOS 6507 (simplified 6502) @ 1.19MHz with TIA and RIOT
// CPU架构: MOS-6507
// 位宽: 8位
// 时钟频率: 1190000 Hz

#ifndef MOS-6507_DEVICE_H
#define MOS-6507_DEVICE_H

#import <Foundation/Foundation.h>

// 寄存器地址定义
#define A_ADDR 0x00  // Accumulator
#define X_ADDR 0x01  // X Index
#define Y_ADDR 0x02  // Y Index
#define SP_ADDR 0x03  // Stack Pointer (6-bit, 128-byte stack)
#define PC_ADDR 0x04  // Program Counter (16-bit)
#define P_ADDR 0x06  // Processor Status
#define P_N_BIT 7  // Negative
#define P_V_BIT 6  // Overflow
#define P_B_BIT 4  // Break
#define P_D_BIT 3  // Decimal Mode (N/A on 6507)
#define P_I_BIT 2  // Interrupt Disable
#define P_Z_BIT 1  // Zero
#define P_C_BIT 0  // Carry

// 内存段定义
#define TIA_REGS_START 0x0000
#define TIA_REGS_END 0x007F
#define TIA_REGS_SIZE 128  // TIA Registers
#define RIOT_RAM_START 0x0080
#define RIOT_RAM_END 0x00FF
#define RIOT_RAM_SIZE 128  // RIOT 128byte RAM mirrored
#define RIOT_IO_START 0x0280
#define RIOT_IO_END 0x029F
#define RIOT_IO_SIZE 32  // RIOT I/O Registers (SWCHA/SWACNT/SWCHB/SWBCNT/INTIM)
#define CART_ROM_START 0x1000
#define CART_ROM_END 0x1FFF
#define CART_ROM_SIZE 4096  // Cartridge ROM (4KB, bank-switched)

// 外设定义
// Television Interface Adaptor (Video + Audio + I/O)
#define TIA_BASE 0x0000
#define TIA_VSYNC_ADDR 0x00
#define TIA_VBLANK_ADDR 0x01
#define TIA_VBLANK_D7_BIT 7  // Inhibit D7 (1=disable D7 output to PB7)
#define TIA_VBLANK_D6_BIT 6  // Inhibit D6 (1=disable D6 output to PB6)
#define TIA_VBLANK_D5_BIT 5  // Inhibit D5 (1=disable D5 output to PB5)
#define TIA_VBLANK_D4_BIT 4  // Inhibit D4 (1=disable D4 output to PB4)
#define TIA_VBLANK_D3_BIT 3  // Inhibit D3 (1=disable D3 output to PB3)
#define TIA_VBLANK_D2_BIT 2  // Inhibit D2 (1=disable D2 output to PB2)
#define TIA_VBLANK_D1_BIT 1  // Inhibit D1 (1=disable D1 output to PB1)
#define TIA_VBLANK_D0_BIT 0  // Inhibit D0 (1=disable D0 output to PB0)
#define TIA_VBLANK_VBW_BIT 5  // Vertical Blank Enable (1=set VBLANK)
#define TIA_VBLANK_VBL_BIT 1  // Vertical Blank Set (1=V-Blank active)
#define TIA_VBLANK_RESBL_BIT 0  // Reset Blank (1=allow VSYNC/VBLANK reset on clock)
#define TIA_WSYNC_ADDR 0x02
#define TIA_RSYNC_ADDR 0x03
#define TIA_NUSIZ0_ADDR 0x04
#define TIA_NUSIZ0_NUSIZ_BIT 0  // Number/Size Code (0-7)
#define TIA_NUSIZ0_MISSILE_SIZE_BIT 0  // Missile Size
#define TIA_NUSIZ0_RESM0_BIT 6  // Reset M0
#define TIA_NUSIZ0_RESM1_BIT 7  // Reset M1
#define TIA_NUSIZ1_ADDR 0x05
#define TIA_COLUP0_ADDR 0x06
#define TIA_COLUP1_ADDR 0x07
#define TIA_COLUPF_ADDR 0x08
#define TIA_COLUBK_ADDR 0x09
#define TIA_CTRLPF_ADDR 0x0A
#define TIA_CTRLPF_DELL_BIT 0  // Delay Playfield L (Reflected/Left score)
#define TIA_CTRLPF_BALL_SIZE_BIT 0  // Ball Size (0=1, 1=2, 2=3, 3=4, 4=5, 5=6, 6=7, 7=8 clocks)
#define TIA_CTRLPF_REF_BIT 5  // Reflect (1=mirror playfield)
#define TIA_CTRLPF_SCORE_BIT 6  // Score Mode (1=use player colors for L/R halves)
#define TIA_CTRLPF_DELBL_BIT 7  // Delay Ball (1=delay ball 1 clock)
#define TIA_REFPL_ADDR 0x0B
#define TIA_PF0_ADDR 0x0D
#define TIA_PF1_ADDR 0x0E
#define TIA_PF2_ADDR 0x0F
#define TIA_RESP0_ADDR 0x10
#define TIA_RESP1_ADDR 0x11
#define TIA_RESM0_ADDR 0x12
#define TIA_RESM1_ADDR 0x13
#define TIA_RESBL_ADDR 0x14
#define TIA_AUDC0_ADDR 0x15
#define TIA_AUDC0_VOL_BIT 0  // Volume (0-15)
#define TIA_AUDC0_TONE_BIT 0  // Tone Divisor (5-bit counter)
#define TIA_AUDC1_ADDR 0x16
#define TIA_AUDF0_ADDR 0x17
#define TIA_AUDF1_ADDR 0x18
#define TIA_AUDV0_ADDR 0x19
#define TIA_AUDV1_ADDR 0x1A
#define TIA_GRP0_ADDR 0x1B
#define TIA_GRP1_ADDR 0x1C
#define TIA_DGRP0_ADDR 0x1D
#define TIA_DGRP1_ADDR 0x1E
#define TIA_ENAM0_ADDR 0x1F
#define TIA_ENAM1_ADDR 0x20
#define TIA_ENABL_ADDR 0x21
#define TIA_HMP0_ADDR 0x22
#define TIA_HMP1_ADDR 0x23
#define TIA_HMM0_ADDR 0x24
#define TIA_HMM1_ADDR 0x25
#define TIA_HMBL_ADDR 0x26
#define TIA_VDEL0_ADDR 0x27
#define TIA_VDEL1_ADDR 0x28
#define TIA_VDELBL_ADDR 0x29
#define TIA_RESBB_ADDR 0x2A
#define TIA_HMOVE_ADDR 0x2A
#define TIA_HMCLR_ADDR 0x2B
#define TIA_CXM0P_ADDR 0x30
#define TIA_CXM1P_ADDR 0x31
#define TIA_CXP0FB_ADDR 0x32
#define TIA_CXP1FB_ADDR 0x33
#define TIA_CXM0FB_ADDR 0x34
#define TIA_CXM1FB_ADDR 0x35
#define TIA_CXBLPF_ADDR 0x36
#define TIA_CXPPMM_ADDR 0x37
#define TIA_INPT0_ADDR 0x38
#define TIA_INPT1_ADDR 0x39
#define TIA_INPT2_ADDR 0x3A
#define TIA_INPT3_ADDR 0x3B
#define TIA_INPT4_ADDR 0x3C
#define TIA_INPT5_ADDR 0x3D
// RAM, I/O, Timer (6532 RIOT)
#define RIOT_BASE 0x0080
#define RIOT_SWCHA_ADDR 0x280
#define RIOT_SWACNT_ADDR 0x281
#define RIOT_SWCHB_ADDR 0x282
#define RIOT_SWCHB_RESET_BIT 1  // Game Reset Switch (0=pressed)
#define RIOT_SWCHB_SELECT_BIT 2  // Game Select Switch (0=pressed)
#define RIOT_SWCHB_DIFFB_BIT 3  // Difficulty B (0=hard, 1=easy)
#define RIOT_SWCHB_DIFFA_BIT 4  // Difficulty A (0=hard, 1=easy)
#define RIOT_SWBCNT_ADDR 0x283
#define RIOT_INTIM_ADDR 0x284
#define RIOT_TIMINT_ADDR 0x285
#define RIOT_TIM1T_ADDR 0x294
#define RIOT_TIM8T_ADDR 0x295
#define RIOT_TIM64T_ADDR 0x296
#define RIOT_TIM1024T_ADDR 0x297
// Controller Port 1 (Joystick)
#define CONTROLLER1_BASE 0x280
#define CONTROLLER1_SWCHA_ADDR 0x280
// Controller Port 2 (Joystick)
#define CONTROLLER2_BASE 0x281
#define CONTROLLER2_SWCHA_ADDR 0x280

// 中断向量定义
#define INT_RESET 0  // Power-On Reset

// 引脚定义
#define PIN_VSS 1  // Ground
#define PIN_VCC 2  // Power Supply
#define PIN_PHI0 3  // Clock Input (1.19MHz NTSC / 1.18MHz PAL)
#define PIN_RESET 4  // Reset (active low)
#define PIN_A0 5  // Address Bus Bit 0
#define PIN_A1 6  // Address Bus Bit 1
#define PIN_A2 7  // Address Bus Bit 2
#define PIN_A3 8  // Address Bus Bit 3
#define PIN_A4 9  // Address Bus Bit 4
#define PIN_A5 10  // Address Bus Bit 5
#define PIN_A6 11  // Address Bus Bit 6
#define PIN_A7 12  // Address Bus Bit 7
#define PIN_A8 13  // Address Bus Bit 8
#define PIN_A9 14  // Address Bus Bit 9
#define PIN_A10 15  // Address Bus Bit 10
#define PIN_A11 16  // Address Bus Bit 11
#define PIN_A12 17  // Address Bus Bit 12
#define PIN_D0 18  // Data Bus Bit 0
#define PIN_D1 19  // Data Bus Bit 1
#define PIN_D2 20  // Data Bus Bit 2
#define PIN_D3 21  // Data Bus Bit 3
#define PIN_D4 22  // Data Bus Bit 4
#define PIN_D5 23  // Data Bus Bit 5
#define PIN_D6 24  // Data Bus Bit 6
#define PIN_D7 25  // Data Bus Bit 7
#define PIN_RDY 26  // Ready (stops CPU on read)
#define PIN_R_W 27  // Read/Write (1=Read, 0=Write)
#define PIN_NC 28  // Not Connected

#endif /* MOS-6507_DEVICE_H */
