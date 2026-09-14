// Nintendo Entertainment System 设备定义 - Objective-C 头文件
// 生成自: Nintendo/NES/Nintendo Entertainment System
// 版本: 
// 日期: 
// 作者: 
// 描述: Nintendo Entertainment System (NES/Famicom) 8-bit video game console
// CPU架构: 6502
// 位宽: 0位
// 时钟频率: 0 Hz

#ifndef NINTENDO ENTERTAINMENT SYSTEM_DEVICE_H
#define NINTENDO ENTERTAINMENT SYSTEM_DEVICE_H

#import <Foundation/Foundation.h>

// 外设定义
// Picture Processing Unit (Ricoh 2C02)
#define PPU_BASE 
#define PPU_PPUCTRL_ADDR 0x2000
#define PPU_PPUCTRL_NMI_BIT 7  // VBlank NMI enable
#define PPU_PPUCTRL_MASTERSLAVE_BIT 6  // Master/slave select
#define PPU_PPUCTRL_SPRITESIZE_BIT 5  // Sprite size (0=8x8, 1=8x16)
#define PPU_PPUCTRL_BGPATTERN_BIT 4  // Background pattern table address
#define PPU_PPUCTRL_SPRITEPATTERN_BIT 3  // Sprite pattern table address
#define PPU_PPUCTRL_VRAMINCREMENT_BIT 2  // VRAM address increment (0=1, 1=32)
#define PPU_PPUCTRL_NAMETABLE_BIT 0  // Nametable address
#define PPU_PPUMASK_ADDR 0x2001
#define PPU_PPUMASK_EMPHASIZEBLUE_BIT 7  // Emphasize blue
#define PPU_PPUMASK_EMPHASIZEGREEN_BIT 6  // Emphasize green
#define PPU_PPUMASK_EMPHASIZERED_BIT 5  // Emphasize red
#define PPU_PPUMASK_SHOWSPRITES_BIT 4  // Show sprites
#define PPU_PPUMASK_SHOWBACKGROUND_BIT 3  // Show background
#define PPU_PPUMASK_SHOWLEFTSPRITES_BIT 2  // Show sprites in left 8 pixels
#define PPU_PPUMASK_SHOWLEFTBACKGROUND_BIT 1  // Show background in left 8 pixels
#define PPU_PPUMASK_GRAYSCALE_BIT 0  // Grayscale mode
#define PPU_PPUSTATUS_ADDR 0x2002
#define PPU_PPUSTATUS_VBLANK_BIT 7  // VBlank started
#define PPU_PPUSTATUS_SPRITE0HIT_BIT 6  // Sprite 0 hit
#define PPU_PPUSTATUS_SPRITEOVERFLOW_BIT 5  // Sprite overflow
#define PPU_OAMADDR_ADDR 0x2003
#define PPU_OAMDATA_ADDR 0x2004
#define PPU_PPUSCROLL_ADDR 0x2005
#define PPU_PPUADDR_ADDR 0x2006
#define PPU_PPUDATA_ADDR 0x2007
#define PPU_OAMDMA_ADDR 0x4014
// Audio Processing Unit (Ricoh 2A03)
#define APU_BASE 
#define APU_SQ1_VOL_ADDR 0x4000
#define APU_SQ1_VOL_DUTY_BIT 6  // Duty cycle
#define APU_SQ1_VOL_LENGTHCOUNTERHALT_BIT 5  // Length counter halt/envelope loop
#define APU_SQ1_VOL_CONSTANTVOLUME_BIT 4  // Constant volume
#define APU_SQ1_VOL_VOLUME_BIT 0  // Volume/envelope period
#define APU_SQ1_SWEEP_ADDR 0x4001
#define APU_SQ1_SWEEP_ENABLED_BIT 7  // Sweep enabled
#define APU_SQ1_SWEEP_PERIOD_BIT 4  // Sweep period
#define APU_SQ1_SWEEP_NEGATE_BIT 3  // Sweep negate
#define APU_SQ1_SWEEP_SHIFT_BIT 0  // Sweep shift amount
#define APU_SQ1_LO_ADDR 0x4002
#define APU_SQ1_HI_ADDR 0x4003
#define APU_SQ1_HI_LENGTHCOUNTER_BIT 3  // Length counter load
#define APU_SQ1_HI_TIMERHIGH_BIT 0  // Timer high bits
#define APU_SQ2_VOL_ADDR 0x4004
#define APU_SQ2_SWEEP_ADDR 0x4005
#define APU_SQ2_LO_ADDR 0x4006
#define APU_SQ2_HI_ADDR 0x4007
#define APU_TRI_LINEAR_ADDR 0x4008
#define APU_TRI_LINEAR_CONTROL_BIT 7  // Length counter halt/linear counter control
#define APU_TRI_LINEAR_PERIOD_BIT 0  // Linear counter load
#define APU_TRI_LO_ADDR 0x400A
#define APU_TRI_HI_ADDR 0x400B
#define APU_NOISE_VOL_ADDR 0x400C
#define APU_NOISE_LO_ADDR 0x400E
#define APU_NOISE_LO_MODE_BIT 7  // Noise mode
#define APU_NOISE_LO_PERIOD_BIT 0  // Noise period
#define APU_NOISE_HI_ADDR 0x400F
#define APU_DMC_FREQ_ADDR 0x4010
#define APU_DMC_FREQ_IRQ_BIT 7  // IRQ enable
#define APU_DMC_FREQ_LOOP_BIT 6  // Loop flag
#define APU_DMC_FREQ_FREQUENCY_BIT 0  // Frequency index
#define APU_DMC_RAW_ADDR 0x4011
#define APU_DMC_START_ADDR 0x4012
#define APU_DMC_LEN_ADDR 0x4013
#define APU_OAMDMA_ADDR 0x4014
#define APU_APUSTATUS_ADDR 0x4015
#define APU_APUSTATUS_DMCINTERRUPT_BIT 7  // DMC interrupt flag
#define APU_APUSTATUS_FRAMEINTERRUPT_BIT 6  // Frame interrupt flag
#define APU_APUSTATUS_DMCENABLED_BIT 4  // DMC enabled
#define APU_APUSTATUS_NOISEENABLED_BIT 3  // Noise enabled
#define APU_APUSTATUS_TRIANGLEENABLED_BIT 2  // Triangle enabled
#define APU_APUSTATUS_SQUARE2ENABLED_BIT 1  // Square 2 enabled
#define APU_APUSTATUS_SQUARE1ENABLED_BIT 0  // Square 1 enabled
#define APU_APUFRAME_ADDR 0x4017
#define APU_APUFRAME_MODE_BIT 7  // Frame counter mode
#define APU_APUFRAME_IRQINHIBIT_BIT 6  // IRQ inhibit
// Controller Interface
#define CONTROLLER_BASE 
#define CONTROLLER_JOY1_ADDR 0x4016
#define CONTROLLER_JOY1_A_BIT 7  // A button
#define CONTROLLER_JOY1_B_BIT 6  // B button
#define CONTROLLER_JOY1_SELECT_BIT 5  // Select button
#define CONTROLLER_JOY1_START_BIT 4  // Start button
#define CONTROLLER_JOY1_UP_BIT 3  // Up direction
#define CONTROLLER_JOY1_DOWN_BIT 2  // Down direction
#define CONTROLLER_JOY1_LEFT_BIT 1  // Left direction
#define CONTROLLER_JOY1_RIGHT_BIT 0  // Right direction
#define CONTROLLER_JOY2_ADDR 0x4017
// Memory Mapper (Cartridge)
#define MAPPER_BASE 
#define MAPPER_PRGROM_ADDR 0
#define MAPPER_CHRROM_ADDR 0
#define MAPPER_PRGRAM_ADDR 0
#define MAPPER_CHRRAM_ADDR 0

// 中断向量定义
#define INT_NMI 65530  // Non-maskable interrupt (VBlank)
#define INT_RESET 65532  // Reset vector
#define INT_IRQ 65534  // Interrupt request

#endif /* NINTENDO ENTERTAINMENT SYSTEM_DEVICE_H */
