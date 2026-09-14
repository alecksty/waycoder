// Ricoh-2A03 设备定义 - Objective-C 头文件
// 生成自: Ricoh/MOS-6502/Ricoh-2A03
// 版本: 1.0
// 日期: 2026-04-16
// 作者: VML Team
// 描述: NES (Famicom) main processor - 8-bit MOS 6502 variant with audio/video support
// CPU架构: MOS-6502
// 位宽: 8位
// 时钟频率: 10765930 Hz

#ifndef RICOH-2A03_DEVICE_H
#define RICOH-2A03_DEVICE_H

#import <Foundation/Foundation.h>

// 寄存器地址定义
#define A_ADDR 0x00  // Accumulator
#define X_ADDR 0x01  // X Index
#define Y_ADDR 0x02  // Y Index
#define SP_ADDR 0x03  // Stack Pointer
#define PC_ADDR 0x04  // Program Counter (16-bit)
#define P_ADDR 0x06  // Processor Status
#define P_C_BIT 0  // Carry
#define P_Z_BIT 1  // Zero
#define P_I_BIT 2  // Interrupt Disable
#define P_D_BIT 3  // Decimal Mode
#define P_B_BIT 4  // Break
#define P_U_BIT 5  // Unused
#define P_V_BIT 6  // Overflow
#define P_N_BIT 7  // Negative

// 内存段定义
#define CPU_RAM_START 0x0000
#define CPU_RAM_END 0x07FF
#define CPU_RAM_SIZE 2048  // CPU 2KB RAM (mirrored)
#define PPU_REGISTERS_START 0x2000
#define PPU_REGISTERS_END 0x3FFF
#define PPU_REGISTERS_SIZE 8192  // PPU Registers (mirrored every 8 bytes)
#define APU_REGISTERS_START 0x4000
#define APU_REGISTERS_END 0x401F
#define APU_REGISTERS_SIZE 32  // APU and I/O Registers
#define EXPANSION_START 0x4020
#define EXPANSION_END 0x5FFF
#define EXPANSION_SIZE 8160  // Expansion ROM
#define SRAM_START 0x6000
#define SRAM_END 0x7FFF
#define SRAM_SIZE 8192  // Save RAM
#define PRG_ROM_LOW_START 0x8000
#define PRG_ROM_LOW_END 0xBFFF
#define PRG_ROM_LOW_SIZE 16384  // PRG ROM Lower Bank (16KB)
#define PRG_ROM_HIGH_START 0xC000
#define PRG_ROM_HIGH_END 0xFFFF
#define PRG_ROM_HIGH_SIZE 16384  // PRG ROM Higher Bank (16KB)

// 外设定义
// Picture Processing Unit
#define PPU_BASE 0x2000
#define PPU_PPUCTRL_ADDR 0x2000
#define PPU_PPUMASK_ADDR 0x2001
#define PPU_PPUSTATUS_ADDR 0x2002
#define PPU_OAMADDR_ADDR 0x2003
#define PPU_OAMDATA_ADDR 0x2004
#define PPU_PPUSCROLL_ADDR 0x2005
#define PPU_PPUADDR_ADDR 0x2006
#define PPU_PPUDATA_ADDR 0x2007
// Audio Processing Unit
#define APU_BASE 0x4000
#define APU_PULSE1_VOL_ADDR 0x4000
#define APU_PULSE1_SWEEP_ADDR 0x4001
#define APU_PULSE1_LO_ADDR 0x4002
#define APU_PULSE1_HI_ADDR 0x4003
#define APU_PULSE2_VOL_ADDR 0x4004
#define APU_PULSE2_SWEEP_ADDR 0x4005
#define APU_PULSE2_LO_ADDR 0x4006
#define APU_PULSE2_HI_ADDR 0x4007
#define APU_TRIANGLE_ADDR 0x4008
#define APU_TRIANGLE_HI_ADDR 0x400B
#define APU_NOISE_VOL_ADDR 0x400C
#define APU_NOISE_HI_ADDR 0x400E
#define APU_NOISE_LENGTH_ADDR 0x400F
#define APU_DMC_RATE_ADDR 0x4010
#define APU_DMC_RAW_ADDR 0x4011
#define APU_DMC_START_ADDR 0x4012
#define APU_DMC_LENGTH_ADDR 0x4013
#define APU_OAMDMA_ADDR 0x4014
#define APU_SNDCHN_ADDR 0x4015
#define APU_JOY1_ADDR 0x4016
#define APU_JOY2_ADDR 0x4017
// Controller Port 1
#define INPUT1_BASE 0x4016
#define INPUT1_JOYPAD1_ADDR 0x4016
// Controller Port 2
#define INPUT2_BASE 0x4017
#define INPUT2_JOYPAD2_ADDR 0x4017

// 中断向量定义
#define INT_RESET 0  // Reset
#define INT_NMI 1  // Non-Maskable Interrupt (VBlank)
#define INT_IRQ 2  // IRQ / BRK

#endif /* RICOH-2A03_DEVICE_H */
