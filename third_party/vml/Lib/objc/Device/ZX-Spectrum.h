// ZX-Spectrum 设备定义 - Objective-C 头文件
// 生成自: Sinclair Research/ZX Spectrum/ZX-Spectrum
// 版本: 1.0
// 日期: 2026-04-17
// 作者: VML Team
// 描述: ZX Spectrum 48K home computer with Z80 CPU, 48KB RAM, and color graphics
// CPU架构: Zilog Z80
// 位宽: 8位
// 时钟频率: 3500000 Hz

#ifndef ZX-SPECTRUM_DEVICE_H
#define ZX-SPECTRUM_DEVICE_H

#import <Foundation/Foundation.h>

// 寄存器地址定义
#define A_ADDR 0  // Accumulator
#define F_ADDR 0  // Flags
#define B_ADDR 0  // B
#define C_ADDR 0  // C
#define D_ADDR 0  // D
#define E_ADDR 0  // E
#define H_ADDR 0  // H
#define L_ADDR 0  // L
#define IX_ADDR 0  // Index Register X
#define IY_ADDR 0  // Index Register Y
#define SP_ADDR 0  // Stack Pointer
#define PC_ADDR 0  // Program Counter
#define I_ADDR 0  // Interrupt Vector
#define R_ADDR 0  // Memory Refresh
#define AF_ADDR 0  // Alternate AF
#define BC_ADDR 0  // Alternate BC
#define DE_ADDR 0  // Alternate DE
#define HL_ADDR 0  // Alternate HL

// 外设定义
// Uncommitted Logic Array (video and I/O)
#define ULA_BASE 
#define ULA_ULA_PORT_FE_ADDR 0xFE
#define ULA_ULA_BORDER_ADDR 0xFE
#define ULA_ULA_BEEPER_ADDR 0xFE
#define ULA_ULA_MIC_ADDR 0xFE
// General Instruments AY-3-8912 sound chip
#define AY_3_8912_BASE 
#define AY_3_8912_AY_REG_SEL_ADDR 0xFFFD
#define AY_3_8912_AY_DATA_ADDR 0xBFFD
#define AY_3_8912_AY_READ_ADDR 0xFFFD
// 40-key rubber keyboard
#define KEYBOARD_BASE 
#define KEYBOARD_KEY_ROW0_ADDR 0xFEFE
#define KEYBOARD_KEY_ROW1_ADDR 0xFDFE
#define KEYBOARD_KEY_ROW2_ADDR 0xFBFE
#define KEYBOARD_KEY_ROW3_ADDR 0xF7FE
#define KEYBOARD_KEY_ROW4_ADDR 0xEFFE
#define KEYBOARD_KEY_ROW5_ADDR 0xDFFE
#define KEYBOARD_KEY_ROW6_ADDR 0xBFFE
#define KEYBOARD_KEY_ROW7_ADDR 0x7FFE
// Kempston joystick interface
#define KEMPSTON_BASE 
#define KEMPSTON_KEMPSTON_JOY_ADDR 0x1F
// ZX Interface 1 (RS-232 and Microdrive)
#define INTERFACE1_BASE 
#define INTERFACE1_IF1_STATUS_ADDR 0x1FFD
#define INTERFACE1_IF1_DATA_ADDR 0x3FFD
// ZX Interface 2 (joystick and ROM cartridge)
#define INTERFACE2_BASE 
#define INTERFACE2_IF2_JOY1_ADDR 0x1F
#define INTERFACE2_IF2_JOY2_ADDR 0x37

// 中断向量定义
#define INT_IM1 56  // Interrupt Mode 1
#define INT_RST_00 0  // Restart 00h
#define INT_RST_08 8  // Restart 08h
#define INT_RST_10 16  // Restart 10h
#define INT_RST_18 24  // Restart 18h
#define INT_RST_20 32  // Restart 20h
#define INT_RST_28 40  // Restart 28h
#define INT_RST_30 48  // Restart 30h
#define INT_RST_38 56  // Restart 38h

#endif /* ZX-SPECTRUM_DEVICE_H */
