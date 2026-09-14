// Zilog-Z80 设备定义 - Objective-C 头文件
// 生成自: Zilog/Z80/Zilog-Z80
// 版本: 1.0
// 日期: 2026-04-16
// 作者: VML Team
// 描述: Sega Master System (Mark III) main processor - Zilog Z80A @ 3.58MHz
// CPU架构: Z80
// 位宽: 8位
// 时钟频率: 3580000 Hz

#ifndef ZILOG-Z80_DEVICE_H
#define ZILOG-Z80_DEVICE_H

#import <Foundation/Foundation.h>

// 寄存器地址定义
#define A_ADDR 0x00  // Accumulator
#define F_ADDR 0x01  // Flags Register
#define F_C_BIT 0  // Carry
#define F_N_BIT 1  // Subtract
#define F_P_BIT 2  // Parity/Overflow
#define F_H_BIT 4  // Half Carry
#define F_Z_BIT 6  // Zero
#define F_S_BIT 7  // Sign/Negative
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
#define IX_ADDR 0x10  // Index Register X
#define IY_ADDR 0x12  // Index Register Y
#define SP_ADDR 0x14  // Stack Pointer
#define PC_ADDR 0x16  // Program Counter
#define I_ADDR 0x18  // Interrupt Vector Register
#define R_ADDR 0x19  // Memory Refresh Register
#define IM_ADDR 0x1A  // Interrupt Mode (0/1/2)

// 内存段定义
#define WRAM_START 0xC000
#define WRAM_END 0xC7FF
#define WRAM_SIZE 2048  // Work RAM (2KB internal)
#define WRAM_SHADOW_START 0xE000
#define WRAM_SHADOW_END 0xE7FF
#define WRAM_SHADOW_SIZE 2048  // Work RAM Shadow (Echo RAM)
#define VRAM_START 0x4000
#define VRAM_END 0x7FFF
#define VRAM_SIZE 16384  // Video RAM (16KB)
#define SRAM_START 0x8000
#define SRAM_END 0xBFFF
#define SRAM_SIZE 16384  // Cartridge SRAM (if present)
#define CART_ROM_START 0x0000
#define CART_ROM_END 0x7FFF
#define CART_ROM_SIZE 32768  // Cartridge ROM (up to 48KB)
#define BIOS_START 0x0000
#define BIOS_END 0x1FFF
#define BIOS_SIZE 8192  // BIOS ROM (Master System built-in, 8KB)
#define IO_REGS_START 0x3F00
#define IO_REGS_END 0x3FFF
#define IO_REGS_SIZE 256  // I/O Register Area

// 外设定义
// Video Display Processor (TMS9918A variant)
#define VDP_BASE 0xBE
#define VDP_VDP_CTRL_ADDR 0xBF
#define VDP_VDP_DATA_ADDR 0xBE
#define VDP_VDP_STATUS_ADDR 0xBF
#define VDP_VDP_STATUS_FIFO_FULL_BIT 0  // VRAM to CPU Transfer Pending
#define VDP_VDP_STATUS_FIFO_EMPTY_BIT 1  // VRAM Write FIFO Empty
#define VDP_VDP_STATUS_INT_FLAG_BIT 7  // V-Blank / Sprite Collision Flag
#define VDP_R0_ADDR 0x00
#define VDP_R0_M3_BIT 0  // Mode 3 Enable
#define VDP_R0_M2_BIT 1  // Mode 2 Enable
#define VDP_R0_M1_BIT 2  // Mode 1 Enable
#define VDP_R0_DISPLAY_DISABLE_BIT 3  // Display Disable (1=blank screen)
#define VDP_R0_VIRQ_EN_BIT 4  // Vertical Interrupt Enable
#define VDP_R0_M4_BIT 5  // Mode 4 Enable (SMS2 only)
#define VDP_R0_SPRITE_SHIFT_BIT 6  // Sprite Double Height
#define VDP_R0_HVC_LATCH_BIT 7  // H-Counter Latch Enable
#define VDP_R1_ADDR 0x01
#define VDP_R1_DISPLAY_BIT 3  // Display Enable (1=active)
#define VDP_R1_FRAME_INT_BIT 4  // Frame Interrupt (V-Blank) Enable
#define VDP_R1_M4_BIT 5  // Mode 4 (256-color)
#define VDP_R1_SMS_MODE_BIT 6  // SMS Display Mode (vs Coleco)
#define VDP_R1_EXT_VIDEO_BIT 7  // External Video Enable
#define VDP_R2_ADDR 0x02
#define VDP_R3_ADDR 0x03
#define VDP_R4_ADDR 0x04
#define VDP_R5_ADDR 0x05
#define VDP_R6_ADDR 0x06
#define VDP_R7_ADDR 0x07
#define VDP_R8_ADDR 0x08
#define VDP_R8_HSCROLL_EN_BIT 0  // Horizontal Scroll Enable
#define VDP_R8_VSCROLL_EN_BIT 1  // Vertical Scroll Enable
#define VDP_R8_LINE_INT_BIT 4  // Line Interrupt Enable
#define VDP_R8_VSCROLL_2X_BIT 7  // Vertical Scroll 2x Speed
#define VDP_R9_ADDR 0x09
#define VDP_R10_ADDR 0x0A
#define VDP_R11_ADDR 0x0B
#define VDP_R12_ADDR 0x0C
#define VDP_R13_ADDR 0x0D
#define VDP_R14_ADDR 0x0E
#define VDP_R15_ADDR 0x0F
#define VDP_VCOUNTER_ADDR 0x7E
#define VDP_HCOUNTER_ADDR 0x7F
// SN76489 Programmable Sound Generator (3 Square + 1 Noise)
#define PSG_BASE 0x7F
#define PSG_CH0_FREQ_ADDR 0x00
#define PSG_CH1_FREQ_ADDR 0x02
#define PSG_CH2_FREQ_ADDR 0x04
#define PSG_CH3_CONFIG_ADDR 0x06
#define PSG_CH3_CONFIG_TYPE_BIT 0  // Noise Type (0=White, 1=Periodic, 2-3=Periodic at freq/2^type)
#define PSG_CH3_CONFIG_VOLUME_BIT 0  // Volume (0-15)
#define PSG_CH0_VOLUME_ADDR 0x01
#define PSG_CH1_VOLUME_ADDR 0x03
#define PSG_CH2_VOLUME_ADDR 0x05
// I/O Port Registers
#define PORTS_BASE 0x3F
#define PORTS_PORT_A_ADDR 0x3F
#define PORTS_PORT_A_UP_BIT 0  // Up (0=pressed)
#define PORTS_PORT_A_DOWN_BIT 1  // Down (0=pressed)
#define PORTS_PORT_A_LEFT_BIT 2  // Left (0=pressed)
#define PORTS_PORT_A_RIGHT_BIT 3  // Right (0=pressed)
#define PORTS_PORT_A_TR_BIT 4  // Button TR (0=pressed)
#define PORTS_PORT_A_TL_BIT 5  // Button TL (0=pressed)
#define PORTS_PORT_B_ADDR 0x3F
#define PORTS_PORT_B_UP_BIT 0  // Up (0=pressed)
#define PORTS_PORT_B_DOWN_BIT 1  // Down (0=pressed)
#define PORTS_PORT_B_LEFT_BIT 2  // Left (0=pressed)
#define PORTS_PORT_B_RIGHT_BIT 3  // Right (0=pressed)
#define PORTS_PORT_B_TR_BIT 4  // Button TR (0=pressed)
#define PORTS_PORT_B_TL_BIT 5  // Button TL (0=pressed)
#define PORTS_PORT_A_DDR_ADDR 0x3F
#define PORTS_PORT_B_DDR_ADDR 0x3F
// Sega Mapper (Memory Bank Switching)
#define SEGAMAPPER_BASE 0xFFFD
#define SEGAMAPPER_ROM_BANK0_ADDR 0xFFFD
#define SEGAMAPPER_ROM_BANK1_ADDR 0xFFFE
#define SEGAMAPPER_ROM_BANK2_ADDR 0xFFFF
// Memory Mapper Control
#define MAPPER_BASE 0xFFFF
#define MAPPER_SRAM_BANK_ADDR 0xFFF8

// 中断向量定义
#define INT_NMI 0  // Non-Maskable Interrupt (Pause button / V-Blank)
#define INT_INT_VBLANK 1  // V-Blank Interrupt (Frame end)
#define INT_INT_LINE 2  // Scanline Interrupt (Line counter match)
#define INT_INT_EXT 3  // External I/O Interrupt

// 引脚定义
#define PIN_A 1  // Power Supply
#define PIN_GND 2  // Ground
#define PIN_PHI 3  // System Clock (3.579545 MHz NTSC / 3.546894 MHz PAL)
#define PIN_RESET 4  // Reset (active low)
#define PIN_M1 5  // Machine Cycle 1 (instruction fetch)
#define PIN_MREQ 6  // Memory Request
#define PIN_IORQ 7  // I/O Request
#define PIN_RD 8  // Read Strobe
#define PIN_WR 9  // Write Strobe
#define PIN_HALT 10  // Halt State
#define PIN_WAIT 11  // Wait State Request
#define PIN_INT 12  // Interrupt Request (active low)
#define PIN_NMI 13  // Non-Maskable Interrupt (active low)
#define PIN_BUSRQ 14  // Bus Request (active low)
#define PIN_BUSAK 15  // Bus Acknowledge (active low)
#define PIN_A0 16  // Address Bus Bit 0
#define PIN_A1 17  // Address Bus Bit 1
#define PIN_A2 18  // Address Bus Bit 2
#define PIN_A3 19  // Address Bus Bit 3
#define PIN_A4 20  // Address Bus Bit 4
#define PIN_A5 21  // Address Bus Bit 5
#define PIN_A6 22  // Address Bus Bit 6
#define PIN_A7 23  // Address Bus Bit 7
#define PIN_A8 24  // Address Bus Bit 8
#define PIN_A9 25  // Address Bus Bit 9
#define PIN_A10 26  // Address Bus Bit 10
#define PIN_A11 27  // Address Bus Bit 11
#define PIN_A12 28  // Address Bus Bit 12
#define PIN_A13 29  // Address Bus Bit 13
#define PIN_A14 30  // Address Bus Bit 14
#define PIN_A15 31  // Address Bus Bit 15
#define PIN_D0 32  // Data Bus Bit 0
#define PIN_D1 33  // Data Bus Bit 1
#define PIN_D2 34  // Data Bus Bit 2
#define PIN_D3 35  // Data Bus Bit 3
#define PIN_D4 36  // Data Bus Bit 4
#define PIN_D5 37  // Data Bus Bit 5
#define PIN_D6 38  // Data Bus Bit 6
#define PIN_D7 39  // Data Bus Bit 7
#define PIN_AUDIO_OUT 40  // Audio Output
#define PIN_VIDEO_SYNC 41  // Composite Video Sync
#define PIN_VIDEO_OUT 42  // Composite Video Output

#endif /* ZILOG-Z80_DEVICE_H */
