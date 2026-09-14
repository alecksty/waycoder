// ZX-Spectrum-48K 设备定义 - Objective-C 头文件
// 生成自: Sinclair Research/ZX Spectrum/ZX-Spectrum-48K
// 版本: 1.0
// 日期: 2026-04-17
// 作者: VML Team
// 描述: Sinclair ZX Spectrum 48K - Iconic British 8-bit home computer with Z80A CPU and ULA graphics
// CPU架构: Z80A
// 位宽: 8位
// 时钟频率: 3500000 Hz

#ifndef ZX-SPECTRUM-48K_DEVICE_H
#define ZX-SPECTRUM-48K_DEVICE_H

#import <Foundation/Foundation.h>

// 寄存器地址定义
#define A_ADDR 0x00  // Accumulator
#define F_ADDR 0x01  // Flags Register
#define F_C_BIT 0  // Carry
#define F_N_BIT 1  // Add/Subtract
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
#define I_ADDR 0x10  // Interrupt Vector Register
#define R_ADDR 0x11  // Refresh Counter
#define IX_ADDR 0x12  // Index X
#define IY_ADDR 0x14  // Index Y
#define SP_ADDR 0x16  // Stack Pointer
#define PC_ADDR 0x18  // Program Counter

// 内存段定义
#define ROM_START 0x0000
#define ROM_END 0x3FFF
#define ROM_SIZE 16384  // 48KB ZX Spectrum ROM (BASIC + monitor)
#define VIDEO_RAM_START 0x4000
#define VIDEO_RAM_END 0x57FF
#define VIDEO_RAM_SIZE 6144  // Display file (256x192 bitmap)
#define ATTR_RAM_START 0x5800
#define ATTR_RAM_END 0x5AFF
#define ATTR_RAM_SIZE 768  // Attribute file (32x24 color cells)
#define USER_RAM_START 0x5B00
#define USER_RAM_END 0xFFFF
#define USER_RAM_SIZE 40960  // User RAM (40KB)

// 外设定义
// Uncommitted Logic Array - Sinclair custom IC
#define ULA_BASE 0xFE
#define ULA_BORDER_ADDR 0xFE
#define ULA_KBD_ROW0_ADDR 0xFE
#define ULA_KBD_ROW1_ADDR 0xFE
#define ULA_KBD_ROW2_ADDR 0xFE
#define ULA_KBD_ROW3_ADDR 0xFE
#define ULA_KBD_ROW4_ADDR 0xFE
#define ULA_KBD_ROW5_ADDR 0xFE
#define ULA_KBD_ROW6_ADDR 0xFE
#define ULA_KBD_ROW7_ADDR 0xFE
#define ULA_KBD_ROW8_ADDR 0xFE
// Keyboard Matrix (40 keys, 8 rows x 5 cols)
#define KEYBOARD_BASE 0xFE
#define KEYBOARD_KBD_IN_ADDR 0xFE
// Internal Beeper
#define BEEPER_BASE 0xFE
#define BEEPER_BEEP_ADDR 0xFE
// Tape Interface
#define TAPE_BASE 0xFE
#define TAPE_EAR_IN_ADDR 0xFE
#define TAPE_MIC_OUT_ADDR 0xFE
// Kempston Joystick Interface
#define JOYSTICK_BASE 0xF7FE
#define JOYSTICK_KEMPSTON_ADDR 0xF7FE

// 中断向量定义
#define INT_RESET 0  // Power-on / Reset
#define INT_NMI 1  // Non-Maskable Interrupt (BREAK key)
#define INT_INT 2  // Maskable Interrupt (ULA vertical blank, 50Hz)

// 引脚定义
#define PIN_VCC 1  // +5V Power
#define PIN_GND 2  // Ground
#define PIN_CLK 3  // Z80 Clock (3.5MHz)
#define PIN_M1 4  // Machine Cycle 1
#define PIN_MREQ 5  // Memory Request
#define PIN_IORQ 6  // I/O Request
#define PIN_RD 7  // Read
#define PIN_WR 8  // Write
#define PIN_HALT 9  // Halt State
#define PIN_BUSAK 10  // Bus Acknowledge
#define PIN_WAIT 11  // Wait State (ULA inserts)
#define PIN_INT 12  // Interrupt Request
#define PIN_NMI 13  // Non-Maskable Interrupt
#define PIN_RESET 14  // Reset
#define PIN_A0_A15 15  // Address Bus (16-bit)
#define PIN_D0_D7 16  // Data Bus (8-bit)

#endif /* ZX-SPECTRUM-48K_DEVICE_H */
