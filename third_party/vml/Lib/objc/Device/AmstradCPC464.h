// Amstrad-CPC-464 设备定义 - Objective-C 头文件
// 生成自: Amstrad/CPC/Amstrad-CPC-464
// 版本: 1.0
// 日期: 2026-04-17
// 作者: VML Team
// 描述: Amstrad CPC 464 - British 8-bit home computer with Z80 CPU and built-in cassette recorder
// CPU架构: Z80A
// 位宽: 8位
// 时钟频率: 4000000 Hz

#ifndef AMSTRAD-CPC-464_DEVICE_H
#define AMSTRAD-CPC-464_DEVICE_H

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
#define IY_ADDR 0x14  // Index Y
#define SP_ADDR 0x16  // Stack Pointer
#define PC_ADDR 0x18  // Program Counter

// 内存段定义
#define LOWER_ROM_START 0x0000
#define LOWER_ROM_END 0x3FFF
#define LOWER_ROM_SIZE 16384  // Lower ROM (AMSDOS / CP/M)
#define RAM_BANK0_START 0x0000
#define RAM_BANK0_END 0x3FFF
#define RAM_BANK0_SIZE 16384  // Lower RAM bank (switchable)
#define RAM_MAIN_START 0x4000
#define RAM_MAIN_END 0xBFFF
#define RAM_MAIN_SIZE 32768  // Main RAM (32KB)
#define UPPER_ROM_START 0xC000
#define UPPER_ROM_END 0xFFFF
#define UPPER_ROM_SIZE 16384  // Upper ROM (BASIC)

// 外设定义
// Gate Array - Custom ASIC (video/sound/RAM control)
#define GA_BASE 0x7F00
#define GA_GA_MR_ADDR 0x7F00
#define GA_GA_IR_ADDR 0x7F01
#define GA_GA_R1_ADDR 0x7F02
#define GA_GA_R2_ADDR 0x7F03
#define GA_GA_R3_ADDR 0x7F04
#define GA_GA_R4_ADDR 0x7F05
#define GA_GA_R5_ADDR 0x7F06
#define GA_GA_R6_ADDR 0x7F07
#define GA_GA_R7_ADDR 0x7F08
// CRT Controller 6845 - Video timing
#define CRTC_BASE 0xBC00
#define CRTC_CRTC_REG_ADDR 0xBC00
#define CRTC_CRTC_DATA_ADDR 0xBD00
#define CRTC_CRTC_H_TOTAL_ADDR 0xBC01
#define CRTC_CRTC_H_DISP_ADDR 0xBC02
#define CRTC_CRTC_HSYNC_POS_ADDR 0xBC03
#define CRTC_CRTC_HSYNC_WIDTH_ADDR 0xBC04
#define CRTC_CRTC_V_TOTAL_ADDR 0xBC05
#define CRTC_CRTC_V_TOTAL_ADJ_ADDR 0xBC06
#define CRTC_CRTC_V_DISP_ADDR 0xBC07
#define CRTC_CRTC_VSYNC_POS_ADDR 0xBC08
#define CRTC_CRTC_INTERLACE_ADDR 0xBC09
#define CRTC_CRTC_CURSOR_START_ADDR 0xBC0A
#define CRTC_CRTC_CURSOR_END_ADDR 0xBC0B
#define CRTC_CRTC_SA_HI_ADDR 0xBC0C
#define CRTC_CRTC_SA_LO_ADDR 0xBC0D
#define CRTC_CRTC_CURSOR_HI_ADDR 0xBC0E
#define CRTC_CRTC_CURSOR_LO_ADDR 0xBC0F
// AY-3-8912 Programmable Sound Generator
#define PSG_BASE 0xF400
#define PSG_PSG_REG_ADDR 0xF400
#define PSG_PSG_DATA_ADDR 0xF600
#define PSG_FREQ_A_LO_ADDR 0xF400
#define PSG_FREQ_A_HI_ADDR 0xF401
#define PSG_FREQ_B_LO_ADDR 0xF402
#define PSG_FREQ_B_HI_ADDR 0xF403
#define PSG_FREQ_C_LO_ADDR 0xF404
#define PSG_FREQ_C_HI_ADDR 0xF405
#define PSG_NOISE_FREQ_ADDR 0xF406
#define PSG_ENABLE_ADDR 0xF407
#define PSG_VOL_A_ADDR 0xF408
#define PSG_VOL_B_ADDR 0xF409
#define PSG_VOL_C_ADDR 0xF40A
#define PSG_ENV_FREQ_LO_ADDR 0xF40B
#define PSG_ENV_FREQ_HI_ADDR 0xF40C
#define PSG_ENV_SHAPE_ADDR 0xF40D
#define PSG_PORT_A_ADDR 0xF40E
#define PSG_PORT_B_ADDR 0xF40F
// WD1772 Floppy Disk Controller (via expansion)
#define FDC_BASE 0xF800
#define FDC_FDC_STATUS_ADDR 0xF8E0
#define FDC_FDC_COMMAND_ADDR 0xF8E0
#define FDC_FDC_TRACK_ADDR 0xF8E1
#define FDC_FDC_SECTOR_ADDR 0xF8E2
#define FDC_FDC_DATA_ADDR 0xF8E3
// Centronics Parallel Printer Port
#define PRINTER_BASE 0xEE
#define PRINTER_PRN_DATA_ADDR 0xEE
#define PRINTER_PRN_STROBE_ADDR 0xEF

// 中断向量定义
#define INT_RESET 0  // Power-on / Reset
#define INT_NMI 1  // Non-Maskable Interrupt
#define INT_INT 2  // Gate Array interrupt (50Hz vertical blank)

// 引脚定义
#define PIN_VCC 1  // +5V Power
#define PIN_GND 2  // Ground
#define PIN_CLK 3  // Z80 Clock (4MHz)
#define PIN_A0_A15 4  // Address Bus
#define PIN_D0_D7 5  // Data Bus
#define PIN_MREQ 6  // Memory Request
#define PIN_IORQ 7  // I/O Request
#define PIN_RD 8  // Read
#define PIN_WR 9  // Write
#define PIN_INT 10  // Interrupt Request
#define PIN_NMI 11  // Non-Maskable Interrupt
#define PIN_RESET 12  // Reset

#endif /* AMSTRAD-CPC-464_DEVICE_H */
