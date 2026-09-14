// Motorola-68000 设备定义 - Objective-C 头文件
// 生成自: Motorola/68000/Motorola-68000
// 版本: 1.0
// 日期: 2026-04-16
// 作者: VML Team
// 描述: 16/32-bit microprocessor used in Sega Genesis, Amiga, Atari ST, Macintosh
// CPU架构: MC68000
// 位宽: 32位
// 时钟频率: 7670452 Hz

#ifndef MOTOROLA-68000_DEVICE_H
#define MOTOROLA-68000_DEVICE_H

#import <Foundation/Foundation.h>

// 寄存器地址定义
#define D0_ADDR 0x00  // Data Register 0
#define D1_ADDR 0x04  // Data Register 1
#define D2_ADDR 0x08  // Data Register 2
#define D3_ADDR 0x0C  // Data Register 3
#define D4_ADDR 0x10  // Data Register 4
#define D5_ADDR 0x14  // Data Register 5
#define D6_ADDR 0x18  // Data Register 6
#define D7_ADDR 0x1C  // Data Register 7
#define A0_ADDR 0x20  // Address Register 0
#define A1_ADDR 0x24  // Address Register 1
#define A2_ADDR 0x28  // Address Register 2
#define A3_ADDR 0x2C  // Address Register 3
#define A4_ADDR 0x30  // Address Register 4
#define A5_ADDR 0x34  // Address Register 5
#define A6_ADDR 0x38  // Address Register 6
#define A7_ADDR 0x3C  // Stack Pointer (USP)
#define PC_ADDR 0x40  // Program Counter
#define SR_ADDR 0x44  // Status Register
#define SR_C_BIT 0  // Carry
#define SR_V_BIT 1  // Overflow
#define SR_Z_BIT 2  // Zero
#define SR_N_BIT 3  // Negative
#define SR_X_BIT 4  // Extend
#define SR_I0_BIT 8  // Interrupt Mask 0
#define SR_I1_BIT 9  // Interrupt Mask 1
#define SR_I2_BIT 10  // Interrupt Mask 2
#define SR_M_BIT 11  // Master/Interrupt
#define SR_S_BIT 13  // Supervisor/User
#define SR_T0_BIT 14  // Trace Mode 0
#define SR_T1_BIT 15  // Trace Mode 1

// 内存段定义
#define RAM_START 0x000000
#define RAM_END 0x3FFFFF
#define RAM_SIZE 4194304  // System RAM (4MB)
#define ROM_START 0x000000
#define ROM_END 0x3FFFFF
#define ROM_SIZE 4194304  // Cartridge ROM
#define IO_START 0xA00000
#define IO_END 0xA1FFFF
#define IO_SIZE 131072  // I/O Register Area
#define VDP_START 0xC00000
#define VDP_END 0xC0001F
#define VDP_SIZE 32  // VDP Registers
#define VRAM_START 0xE00000
#define VRAM_END 0xE3FFFF
#define VRAM_SIZE 262144  // Video RAM (256KB)

// 外设定义
// Video Display Processor (TMS9918A variant)
#define VDP_BASE 0xC00000
#define VDP_DATA_ADDR 0x00
#define VDP_CTRL_ADDR 0x04
#define VDP_HVCOUNT_ADDR 0x08
#define VDP_HVB_STATUS_ADDR 0x0A
// Programmable Sound Generator (AY-3-8910)
#define PSG_BASE 0xC00011
#define PSG_CH_A_FREQ_ADDR 0x00
#define PSG_CH_A_VOL_ADDR 0x08
#define PSG_CH_B_FREQ_ADDR 0x02
#define PSG_CH_B_VOL_ADDR 0x09
#define PSG_CH_C_FREQ_ADDR 0x04
#define PSG_CH_C_VOL_ADDR 0x0A
#define PSG_NOISE_FREQ_ADDR 0x06
#define PSG_MIXER_ADDR 0x07
#define PSG_ENV_FREQ_ADDR 0x0D
#define PSG_ENV_SHAPE_ADDR 0x0B
// Z80 Secondary CPU (Sound)
#define Z80_BASE 0xA00000
#define Z80_Z80_RESET_ADDR 0x00
#define Z80_Z80_BUSREQ_ADDR 0x04
#define Z80_Z80_STATUS_ADDR 0x08
// Bank Register
#define BANK_REG_BASE 0xA12000
#define BANK_REG_ROM_BANK_ADDR 0x00
#define BANK_REG_RAM_BANK_ADDR 0x04
// Hardware Version
#define HW_VERSION_BASE 0xA10001
#define HW_VERSION_VERSION_ADDR 0x00
// Controller Port 1
#define CONTROLLER1_BASE 0xA10003
#define CONTROLLER1_DATA_ADDR 0x00
#define CONTROLLER1_CTRL_ADDR 0x04
// Controller Port 2
#define CONTROLLER2_BASE 0xA10005
#define CONTROLLER2_DATA_ADDR 0x00
#define CONTROLLER2_CTRL_ADDR 0x04
// External Port
#define EXT_PORT_BASE 0xA10007
#define EXT_PORT_DATA_ADDR 0x00
// DMA Controller
#define DMA_BASE 0xA10008
#define DMA_SOURCE_ADDR 0x00
#define DMA_DEST_ADDR 0x04
#define DMA_COUNT_ADDR 0x08
#define DMA_CTRL_ADDR 0x0A
// Hardware Timer
#define TIMER_BASE 0xA1000E
#define TIMER_H_COUNTER_ADDR 0x00
#define TIMER_V_COUNTER_ADDR 0x04

// 中断向量定义
#define INT_RESET_SP 1  // Reset Initial Stack Pointer
#define INT_RESET_PC 2  // Reset Initial PC
#define INT_BUS_ERROR 3  // Bus Error
#define INT_ADDRESS_ERROR 4  // Address Error
#define INT_ILLEGAL_INSTR 5  // Illegal Instruction
#define INT_ZERO_DIVIDE 6  // Zero Divide
#define INT_CHK_EXCEPTION 7  // CHK Exception
#define INT_TRAPV 8  // TRAPV Exception
#define INT_PRIVILEGE 9  // Privilege Violation
#define INT_TRACE 10  // Trace
#define INT_LINE_A 11  // Line 1010 Emulator
#define INT_LINE_F 12  // Line 1111 Emulator
#define INT_IRQ1 24  // External Interrupt 1 (H-Blank)
#define INT_IRQ2 25  // External Interrupt 2 (V-Blank)
#define INT_IRQ3 26  // External Interrupt 3
#define INT_IRQ4 27  // External Interrupt 4 (D-Req)
#define INT_IRQ5 28  // External Interrupt 5
#define INT_IRQ6 29  // External Interrupt 6
#define INT_IRQ7 30  // External Interrupt 7
#define INT_TRAP0 32  // TRAP #0
#define INT_TRAP1 33  // TRAP #1
#define INT_TRAP15 47  // TRAP #15

#endif /* MOTOROLA-68000_DEVICE_H */
