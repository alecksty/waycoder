// Sega-Genesis 设备定义 - Objective-C 头文件
// 生成自: Sega/Genesis/Mega Drive/Sega-Genesis
// 版本: 1.0
// 日期: 2026-04-17
// 作者: VML Team
// 描述: Sega Genesis/Mega Drive 16-bit video game console with Motorola 68000 CPU
// CPU架构: Motorola 68000
// 位宽: 32位
// 时钟频率: 7670000 Hz

#ifndef SEGA-GENESIS_DEVICE_H
#define SEGA-GENESIS_DEVICE_H

#import <Foundation/Foundation.h>

// 寄存器地址定义
#define D0_ADDR 0  // Data Register 0
#define D1_ADDR 0  // Data Register 1
#define D2_ADDR 0  // Data Register 2
#define D3_ADDR 0  // Data Register 3
#define D4_ADDR 0  // Data Register 4
#define D5_ADDR 0  // Data Register 5
#define D6_ADDR 0  // Data Register 6
#define D7_ADDR 0  // Data Register 7
#define A0_ADDR 0  // Address Register 0
#define A1_ADDR 0  // Address Register 1
#define A2_ADDR 0  // Address Register 2
#define A3_ADDR 0  // Address Register 3
#define A4_ADDR 0  // Address Register 4
#define A5_ADDR 0  // Address Register 5
#define A6_ADDR 0  // Address Register 6
#define A7_ADDR 0  // Address Register 7 (SP)
#define PC_ADDR 0  // Program Counter
#define SR_ADDR 0  // Status Register

// 外设定义
// Video Display Processor (315-5313)
#define VDP_BASE 
#define VDP_VDP_DATA_ADDR 0xC00000
#define VDP_VDP_CONTROL_ADDR 0xC00004
#define VDP_VDP_HVCOUNTER_ADDR 0xC00008
#define VDP_VDP_PSG_ADDR 0xC00011
// FM synthesis sound chip
#define YM2612_BASE 
#define YM2612_YM2612_ADDR0_ADDR 0xA04000
#define YM2612_YM2612_DATA0_ADDR 0xA04001
#define YM2612_YM2612_ADDR1_ADDR 0xA04002
#define YM2612_YM2612_DATA1_ADDR 0xA04003
// I/O ports
#define IOPORTS_BASE 
#define IOPORTS_IO_DATA1_ADDR 0xA10002
#define IOPORTS_IO_DATA2_ADDR 0xA10004
#define IOPORTS_IO_DATA3_ADDR 0xA10006
#define IOPORTS_IO_CTRL1_ADDR 0xA10008
#define IOPORTS_IO_CTRL2_ADDR 0xA1000A
#define IOPORTS_IO_CTRL3_ADDR 0xA1000C
// TradeMark Security System
#define TMSS_BASE 
#define TMSS_TMSS_ADDR 0xA14000
// Z80 bus control
#define Z80BUS_BASE 
#define Z80BUS_Z80_BUSREQ_ADDR 0xA11100
#define Z80BUS_Z80_RESET_ADDR 0xA11200
#define Z80BUS_Z80_YM2612_ADDR 0xA04000

// 中断向量定义
#define INT_RESET_SP 0  // Reset (Initial SP)
#define INT_RESET_PC 4  // Reset (Initial PC)
#define INT_HBLANK 24  // Horizontal blank interrupt
#define INT_VBLANK 28  // Vertical blank interrupt
#define INT_EXTINT1 32  // External interrupt 1
#define INT_EXTINT2 36  // External interrupt 2
#define INT_EXTINT3 40  // External interrupt 3
#define INT_EXTINT4 44  // External interrupt 4
#define INT_EXTINT5 48  // External interrupt 5
#define INT_EXTINT6 52  // External interrupt 6
#define INT_EXTINT7 56  // External interrupt 7

#endif /* SEGA-GENESIS_DEVICE_H */
