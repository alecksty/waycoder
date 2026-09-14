// Sega-Master-System 设备定义 - Objective-C 头文件
// 生成自: Sega/Master System/Sega-Master-System
// 版本: 1.0
// 日期: 2026-04-17
// 作者: VML Team
// 描述: Sega Master System 8-bit video game console with Z80 CPU
// CPU架构: Zilog Z80
// 位宽: 8位
// 时钟频率: 3579545 Hz

#ifndef SEGA-MASTER-SYSTEM_DEVICE_H
#define SEGA-MASTER-SYSTEM_DEVICE_H

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

// 外设定义
// Video Display Processor (TMS9918A)
#define VDP_BASE 
#define VDP_VDP_DATA_ADDR 0xBE
#define VDP_VDP_ADDR_ADDR 0xBF
#define VDP_VDP_STATUS_ADDR 0xBF
// Programmable Sound Generator (SN76489)
#define PSG_BASE 
#define PSG_PSG_DATA_ADDR 0x7F
// I/O ports
#define IO_BASE 
#define IO_IO_PORT_A_ADDR 0xDC
#define IO_IO_PORT_B_ADDR 0xDD
#define IO_IO_PORT_MISC_ADDR 0xDE
#define IO_IO_PORT_VDP_ADDR 0xDF
// Memory mapper
#define MEMORYMAPPER_BASE 
#define MEMORYMAPPER_MAPPER_0_ADDR 0xFFFC
#define MEMORYMAPPER_MAPPER_1_ADDR 0xFFFD
#define MEMORYMAPPER_MAPPER_2_ADDR 0xFFFE
#define MEMORYMAPPER_MAPPER_3_ADDR 0xFFFF
// FM Sound Unit (optional)
#define FMUNIT_BASE 
#define FMUNIT_FM_ADDR_ADDR 0xF0
#define FMUNIT_FM_DATA_ADDR 0xF1
#define FMUNIT_FM_DETECT_ADDR 0xF2

// 中断向量定义
#define INT_RST_00 0  // Restart 00h
#define INT_IM1 56  // Interrupt Mode 1
#define INT_VBLANK 56  // Vertical blank interrupt
#define INT_LINE 100  // Line interrupt

#endif /* SEGA-MASTER-SYSTEM_DEVICE_H */
