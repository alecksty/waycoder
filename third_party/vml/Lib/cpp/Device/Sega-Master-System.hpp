#ifndef SEGA_MASTER_SYSTEM_HPP
#define SEGA_MASTER_SYSTEM_HPP

// Sega-Master-System寄存器定义
// 生成自: Sega/Master System/Sega-Master-System
// 版本: 1.0
// 日期: 2026-04-17


#ifdef __cplusplus
extern "C" {
#endif

// CPU架构: Zilog Z80
// 位宽: 8位
// 时钟频率: 3579545 Hz

// 寄存器定义
// Accumulator
#define A (*(volatile uint8_t*)0)

// Flags
#define F (*(volatile uint8_t*)0)

// B
#define B (*(volatile uint8_t*)0)

// C
#define C (*(volatile uint8_t*)0)

// D
#define D (*(volatile uint8_t*)0)

// E
#define E (*(volatile uint8_t*)0)

// H
#define H (*(volatile uint8_t*)0)

// L
#define L (*(volatile uint8_t*)0)

// Index Register X
#define IX (*(volatile uint16_t*)0)

// Index Register Y
#define IY (*(volatile uint16_t*)0)

// Stack Pointer
#define SP (*(volatile uint16_t*)0)

// Program Counter
#define PC (*(volatile uint16_t*)0)

// Interrupt Vector
#define I (*(volatile uint8_t*)0)

// Memory Refresh
#define R (*(volatile uint8_t*)0)

// 外设定义
// Video Display Processor (TMS9918A)
#define VDP_BASE 
#define VDP_VDP_DATA (*(volatile uint8_t*)0x000000BE)
#define VDP_VDP_ADDR (*(volatile uint8_t*)0x000000BF)
#define VDP_VDP_STATUS (*(volatile uint8_t*)0x000000BF)

// Programmable Sound Generator (SN76489)
#define PSG_BASE 
#define PSG_PSG_DATA (*(volatile uint8_t*)0x0000007F)

// I/O ports
#define IO_BASE 
#define IO_IO_PORT_A (*(volatile uint8_t*)0x000000DC)
#define IO_IO_PORT_B (*(volatile uint8_t*)0x000000DD)
#define IO_IO_PORT_MISC (*(volatile uint8_t*)0x000000DE)
#define IO_IO_PORT_VDP (*(volatile uint8_t*)0x000000DF)

// Memory mapper
#define MEMORYMAPPER_BASE 
#define MEMORYMAPPER_MAPPER_0 (*(volatile uint8_t*)0x0000FFFC)
#define MEMORYMAPPER_MAPPER_1 (*(volatile uint8_t*)0x0000FFFD)
#define MEMORYMAPPER_MAPPER_2 (*(volatile uint8_t*)0x0000FFFE)
#define MEMORYMAPPER_MAPPER_3 (*(volatile uint8_t*)0x0000FFFF)

// FM Sound Unit (optional)
#define FMUNIT_BASE 
#define FMUNIT_FM_ADDR (*(volatile uint8_t*)0x000000F0)
#define FMUNIT_FM_DATA (*(volatile uint8_t*)0x000000F1)
#define FMUNIT_FM_DETECT (*(volatile uint8_t*)0x000000F2)

// 中断向量定义
#define RST_00_VECTOR 0  // Restart 00h
#define IM1_VECTOR 56  // Interrupt Mode 1
#define VBLANK_VECTOR 56  // Vertical blank interrupt
#define LINE_VECTOR 100  // Line interrupt

void sega_master_system_init(void);

#ifdef __cplusplus
}
#endif

#endif // SEGA_MASTER_SYSTEM_HPP
