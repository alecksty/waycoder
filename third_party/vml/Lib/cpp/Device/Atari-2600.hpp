#ifndef MOS_6507_HPP
#define MOS_6507_HPP

// MOS-6507寄存器定义
// 生成自: MOS Technology/MOS-6502/MOS-6507
// 版本: 1.0
// 日期: 2026-04-16


#ifdef __cplusplus
extern "C" {
#endif

// CPU架构: MOS-6507
// 位宽: 8位
// 时钟频率: 1190000 Hz

// 寄存器定义
// Accumulator
#define A (*(volatile uint8_t*)0x00)

// X Index
#define X (*(volatile uint8_t*)0x01)

// Y Index
#define Y (*(volatile uint8_t*)0x02)

// Stack Pointer (6-bit, 128-byte stack)
#define SP (*(volatile uint8_t*)0x03)

// Program Counter (16-bit)
#define PC (*(volatile uint16_t*)0x04)

// Processor Status
#define P (*(volatile uint8_t*)0x06)
#define P_N 7  // Negative
#define P_V 6  // Overflow
#define P_B 4  // Break
#define P_D 3  // Decimal Mode (N/A on 6507)
#define P_I 2  // Interrupt Disable
#define P_Z 1  // Zero
#define P_C 0  // Carry

// 内存段定义
// TIA Registers
#define TIA_REGS_START 0x0000
#define TIA_REGS_END 0x007F
#define TIA_REGS_SIZE 128

// RIOT 128byte RAM mirrored
#define RIOT_RAM_START 0x0080
#define RIOT_RAM_END 0x00FF
#define RIOT_RAM_SIZE 128

// RIOT I/O Registers (SWCHA/SWACNT/SWCHB/SWBCNT/INTIM)
#define RIOT_IO_START 0x0280
#define RIOT_IO_END 0x029F
#define RIOT_IO_SIZE 32

// Cartridge ROM (4KB, bank-switched)
#define CART_ROM_START 0x1000
#define CART_ROM_END 0x1FFF
#define CART_ROM_SIZE 4096

// 外设定义
// Television Interface Adaptor (Video + Audio + I/O)
#define TIA_BASE 0x0000
#define TIA_VSYNC (*(volatile uint8_t*)0x00000000)
#define TIA_VBLANK (*(volatile uint8_t*)0x00000001)
#define TIA_VBLANK_D7 7  // Inhibit D7 (1=disable D7 output to PB7)
#define TIA_VBLANK_D6 6  // Inhibit D6 (1=disable D6 output to PB6)
#define TIA_VBLANK_D5 5  // Inhibit D5 (1=disable D5 output to PB5)
#define TIA_VBLANK_D4 4  // Inhibit D4 (1=disable D4 output to PB4)
#define TIA_VBLANK_D3 3  // Inhibit D3 (1=disable D3 output to PB3)
#define TIA_VBLANK_D2 2  // Inhibit D2 (1=disable D2 output to PB2)
#define TIA_VBLANK_D1 1  // Inhibit D1 (1=disable D1 output to PB1)
#define TIA_VBLANK_D0 0  // Inhibit D0 (1=disable D0 output to PB0)
#define TIA_VBLANK_VBW 5  // Vertical Blank Enable (1=set VBLANK)
#define TIA_VBLANK_VBL 1  // Vertical Blank Set (1=V-Blank active)
#define TIA_VBLANK_RESBL 0  // Reset Blank (1=allow VSYNC/VBLANK reset on clock)
#define TIA_WSYNC (*(volatile uint8_t*)0x00000002)
#define TIA_RSYNC (*(volatile uint8_t*)0x00000003)
#define TIA_NUSIZ0 (*(volatile uint8_t*)0x00000004)
#define TIA_NUSIZ0_NUSIZ 0  // Number/Size Code (0-7)
#define TIA_NUSIZ0_MISSILE_SIZE 0  // Missile Size
#define TIA_NUSIZ0_RESM0 6  // Reset M0
#define TIA_NUSIZ0_RESM1 7  // Reset M1
#define TIA_NUSIZ1 (*(volatile uint8_t*)0x00000005)
#define TIA_COLUP0 (*(volatile uint8_t*)0x00000006)
#define TIA_COLUP1 (*(volatile uint8_t*)0x00000007)
#define TIA_COLUPF (*(volatile uint8_t*)0x00000008)
#define TIA_COLUBK (*(volatile uint8_t*)0x00000009)
#define TIA_CTRLPF (*(volatile uint8_t*)0x0000000A)
#define TIA_CTRLPF_DELL 0  // Delay Playfield L (Reflected/Left score)
#define TIA_CTRLPF_BALL_SIZE 0  // Ball Size (0=1, 1=2, 2=3, 3=4, 4=5, 5=6, 6=7, 7=8 clocks)
#define TIA_CTRLPF_REF 5  // Reflect (1=mirror playfield)
#define TIA_CTRLPF_SCORE 6  // Score Mode (1=use player colors for L/R halves)
#define TIA_CTRLPF_DELBL 7  // Delay Ball (1=delay ball 1 clock)
#define TIA_REFPL (*(volatile uint8_t*)0x0000000B)
#define TIA_PF0 (*(volatile uint8_t*)0x0000000D)
#define TIA_PF1 (*(volatile uint8_t*)0x0000000E)
#define TIA_PF2 (*(volatile uint8_t*)0x0000000F)
#define TIA_RESP0 (*(volatile uint8_t*)0x00000010)
#define TIA_RESP1 (*(volatile uint8_t*)0x00000011)
#define TIA_RESM0 (*(volatile uint8_t*)0x00000012)
#define TIA_RESM1 (*(volatile uint8_t*)0x00000013)
#define TIA_RESBL (*(volatile uint8_t*)0x00000014)
#define TIA_AUDC0 (*(volatile uint8_t*)0x00000015)
#define TIA_AUDC0_VOL 0  // Volume (0-15)
#define TIA_AUDC0_TONE 0  // Tone Divisor (5-bit counter)
#define TIA_AUDC1 (*(volatile uint8_t*)0x00000016)
#define TIA_AUDF0 (*(volatile uint8_t*)0x00000017)
#define TIA_AUDF1 (*(volatile uint8_t*)0x00000018)
#define TIA_AUDV0 (*(volatile uint8_t*)0x00000019)
#define TIA_AUDV1 (*(volatile uint8_t*)0x0000001A)
#define TIA_GRP0 (*(volatile uint8_t*)0x0000001B)
#define TIA_GRP1 (*(volatile uint8_t*)0x0000001C)
#define TIA_DGRP0 (*(volatile uint8_t*)0x0000001D)
#define TIA_DGRP1 (*(volatile uint8_t*)0x0000001E)
#define TIA_ENAM0 (*(volatile uint8_t*)0x0000001F)
#define TIA_ENAM1 (*(volatile uint8_t*)0x00000020)
#define TIA_ENABL (*(volatile uint8_t*)0x00000021)
#define TIA_HMP0 (*(volatile uint8_t*)0x00000022)
#define TIA_HMP1 (*(volatile uint8_t*)0x00000023)
#define TIA_HMM0 (*(volatile uint8_t*)0x00000024)
#define TIA_HMM1 (*(volatile uint8_t*)0x00000025)
#define TIA_HMBL (*(volatile uint8_t*)0x00000026)
#define TIA_VDEL0 (*(volatile uint8_t*)0x00000027)
#define TIA_VDEL1 (*(volatile uint8_t*)0x00000028)
#define TIA_VDELBL (*(volatile uint8_t*)0x00000029)
#define TIA_RESBB (*(volatile uint8_t*)0x0000002A)
#define TIA_HMOVE (*(volatile uint8_t*)0x0000002A)
#define TIA_HMCLR (*(volatile uint8_t*)0x0000002B)
#define TIA_CXM0P (*(volatile uint8_t*)0x00000030)
#define TIA_CXM1P (*(volatile uint8_t*)0x00000031)
#define TIA_CXP0FB (*(volatile uint8_t*)0x00000032)
#define TIA_CXP1FB (*(volatile uint8_t*)0x00000033)
#define TIA_CXM0FB (*(volatile uint8_t*)0x00000034)
#define TIA_CXM1FB (*(volatile uint8_t*)0x00000035)
#define TIA_CXBLPF (*(volatile uint8_t*)0x00000036)
#define TIA_CXPPMM (*(volatile uint8_t*)0x00000037)
#define TIA_INPT0 (*(volatile uint8_t*)0x00000038)
#define TIA_INPT1 (*(volatile uint8_t*)0x00000039)
#define TIA_INPT2 (*(volatile uint8_t*)0x0000003A)
#define TIA_INPT3 (*(volatile uint8_t*)0x0000003B)
#define TIA_INPT4 (*(volatile uint8_t*)0x0000003C)
#define TIA_INPT5 (*(volatile uint8_t*)0x0000003D)

// RAM, I/O, Timer (6532 RIOT)
#define RIOT_BASE 0x0080
#define RIOT_SWCHA (*(volatile uint8_t*)0x00000300)
#define RIOT_SWACNT (*(volatile uint8_t*)0x00000301)
#define RIOT_SWCHB (*(volatile uint8_t*)0x00000302)
#define RIOT_SWCHB_RESET 1  // Game Reset Switch (0=pressed)
#define RIOT_SWCHB_SELECT 2  // Game Select Switch (0=pressed)
#define RIOT_SWCHB_DIFFB 3  // Difficulty B (0=hard, 1=easy)
#define RIOT_SWCHB_DIFFA 4  // Difficulty A (0=hard, 1=easy)
#define RIOT_SWBCNT (*(volatile uint8_t*)0x00000303)
#define RIOT_INTIM (*(volatile uint8_t*)0x00000304)
#define RIOT_TIMINT (*(volatile uint8_t*)0x00000305)
#define RIOT_TIM1T (*(volatile uint8_t*)0x00000314)
#define RIOT_TIM8T (*(volatile uint8_t*)0x00000315)
#define RIOT_TIM64T (*(volatile uint8_t*)0x00000316)
#define RIOT_TIM1024T (*(volatile uint8_t*)0x00000317)

// Controller Port 1 (Joystick)
#define CONTROLLER1_BASE 0x280
#define CONTROLLER1_SWCHA (*(volatile uint8_t*)0x00000500)

// Controller Port 2 (Joystick)
#define CONTROLLER2_BASE 0x281
#define CONTROLLER2_SWCHA (*(volatile uint8_t*)0x00000501)

// 中断向量定义
#define RESET_VECTOR 0  // Power-On Reset

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

void mos_6507_init(void);

#ifdef __cplusplus
}
#endif

#endif // MOS_6507_HPP
