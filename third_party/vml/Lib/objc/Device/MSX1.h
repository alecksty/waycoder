// MSX1 设备定义 - Objective-C 头文件
// 生成自: Various (ASCII/Awanaga/MSX Association)/MSX/MSX1
// 版本: 1.0
// 日期: 2026-04-17
// 作者: VML Team
// 描述: MSX - Standardized 8-bit home computer with Z80A CPU, TMS9918A graphics, and AY-3-8910 audio
// CPU架构: Z80A
// 位宽: 8位
// 时钟频率: 3579545 Hz

#ifndef MSX1_DEVICE_H
#define MSX1_DEVICE_H

#import <Foundation/Foundation.h>

// 寄存器地址定义
#define A_ADDR 0x00  // Accumulator
#define F_ADDR 0x01  // Flags
#define F_C_BIT 0  // Carry
#define F_N_BIT 1  // Subtract
#define F_PV_BIT 2  // Parity/Overflow
#define F_H_BIT 4  // Half Carry
#define F_Z_BIT 6  // Zero
#define F_S_BIT 7  // Sign
#define B_ADDR 0x02  // B Register
#define C_ADDR 0x03  // C Register
#define D_ADDR 0x04  // D Register
#define E_ADDR 0x05  // E Register
#define H_ADDR 0x06  // H Register
#define L_ADDR 0x07  // L Register
#define AF_ADDR 0x08  // Alternate AF
#define BC_ADDR 0x0A  // Alternate BC
#define DE_ADDR 0x0C  // Alternate DE
#define HL_ADDR 0x0E  // Alternate HL
#define I_ADDR 0x10  // Interrupt Vector
#define R_ADDR 0x11  // Refresh
#define IX_ADDR 0x12  // Index X
#define IY_ADDR 0x14  // Index Y (usually = 0xF38F)
#define SP_ADDR 0x16  // Stack Pointer
#define PC_ADDR 0x18  // Program Counter

// 内存段定义
#define SLOT0_ROM_START 0x0000
#define SLOT0_ROM_END 0x7FFF
#define SLOT0_ROM_SIZE 32768  // Cartridge/SUB-ROM / Main-ROM
#define SYSROM_START 0x0000
#define SYSROM_END 0x3FFF
#define SYSROM_SIZE 16384  // MSX-BIOS ROM
#define EXTROM_START 0x4000
#define EXTROM_END 0x7FFF
#define EXTROM_SIZE 16384  // Extension ROM (cartridge)
#define MAIN_RAM_START 0x4000
#define MAIN_RAM_END 0xC000
#define MAIN_RAM_SIZE 32768  // Main RAM (32KB working area)
#define WORK_RAM_START 0xC000
#define WORK_RAM_END 0xFFFF
#define WORK_RAM_SIZE 16384  // Work RAM (16KB)
#define SYSVAR_START 0xF000
#define SYSVAR_END 0xFCA0
#define SYSVAR_SIZE 3232  // System variables area
#define SLOTS_START 0x8000
#define SLOTS_END 0xFFFF
#define SLOTS_SIZE 32768  // Slot-mapped memory

// 外设定义
// TMS9918A Video Display Processor
#define VDP_BASE 0x98
#define VDP_VDP_REG0_ADDR 0x99
#define VDP_VDP_REG1_ADDR 0x99
#define VDP_VDP_REG2_ADDR 0x99
#define VDP_VDP_REG3_ADDR 0x99
#define VDP_VDP_REG4_ADDR 0x99
#define VDP_VDP_REG5_ADDR 0x99
#define VDP_VDP_REG6_ADDR 0x99
#define VDP_VDP_REG7_ADDR 0x99
#define VDP_VDP_STATUS_ADDR 0x99
#define VDP_VDP_DATA_ADDR 0x98
#define VDP_VDP_POT_ADDR 0x98
// AY-3-8910 Programmable Sound Generator
#define PSG_BASE 0xA0
#define PSG_PSG_REG_ADDR 0xA1
#define PSG_PSG_DATA_ADDR 0xA3
#define PSG_FREQ_A_LO_ADDR 0xA0
#define PSG_FREQ_A_HI_ADDR 0xA1
#define PSG_FREQ_B_LO_ADDR 0xA2
#define PSG_FREQ_B_HI_ADDR 0xA3
#define PSG_FREQ_C_LO_ADDR 0xA4
#define PSG_FREQ_C_HI_ADDR 0xA5
#define PSG_NOISE_FREQ_ADDR 0xA6
#define PSG_ENABLE_ADDR 0xA7
#define PSG_VOL_A_ADDR 0xA8
#define PSG_VOL_B_ADDR 0xA9
#define PSG_VOL_C_ADDR 0xAA
#define PSG_ENV_FREQ_LO_ADDR 0xAB
#define PSG_ENV_FREQ_HI_ADDR 0xAC
#define PSG_ENV_SHAPE_ADDR 0xAD
#define PSG_PORT_A_ADDR 0xAE
#define PSG_PORT_B_ADDR 0xAF
// PPI 8255 Programmable Peripheral Interface
#define PPI_BASE 0xA8
#define PPI_PPI_PA_ADDR 0xA8
#define PPI_PPI_PB_ADDR 0xA9
#define PPI_PPI_PC_ADDR 0xAA
#define PPI_PPI_CTRL_ADDR 0xAB
// MSX Slot Expansion System
#define SLOTEXP_BASE 0x0000
#define SLOTEXP_SLOT0_ADDR 0xFCC0
#define SLOTEXP_SLOT1_ADDR 0xFCC1
#define SLOTEXP_SLOT2_ADDR 0xFCC2
#define SLOTEXP_SLOT3_ADDR 0xFCC3
#define SLOTEXP_EXPTBL0_ADDR 0xFCC4
#define SLOTEXP_EXPTBL1_ADDR 0xFCC5
#define SLOTEXP_EXPTBL2_ADDR 0xFCC6
#define SLOTEXP_EXPTBL3_ADDR 0xFCC7

// 中断向量定义
#define INT_RESET 0  // Power-on / Reset
#define INT_NMI 1  // Non-Maskable Interrupt
#define INT_INT 2  // VDP Vertical Interrupt (frame)

// 引脚定义
#define PIN_VCC 1  // +5V Power
#define PIN_GND 2  // Ground
#define PIN_CLK 3  // Z80 Clock (3.58MHz)
#define PIN_A0_A15 4  // Address Bus
#define PIN_D0_D7 5  // Data Bus
#define PIN_MREQ 6  // Memory Request
#define PIN_IORQ 7  // I/O Request
#define PIN_RD 8  // Read
#define PIN_WR 9  // Write
#define PIN_INT 10  // Interrupt Request
#define PIN_NMI 11  // Non-Maskable Interrupt
#define PIN_RESET 12  // Reset
#define PIN_SLTSL 13  // Slot select (for memory mapping)
#define PIN_WAIT 14  // Wait (for slow I/O)

#endif /* MSX1_DEVICE_H */
