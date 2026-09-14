#ifndef AMSTRAD_CPC_464_HPP
#define AMSTRAD_CPC_464_HPP

// Amstrad-CPC-464寄存器定义
// 生成自: Amstrad/CPC/Amstrad-CPC-464
// 版本: 1.0
// 日期: 2026-04-17


#ifdef __cplusplus
extern "C" {
#endif

// CPU架构: Z80A
// 位宽: 8位
// 时钟频率: 4000000 Hz

// 寄存器定义
// Accumulator
#define A (*(volatile uint8_t*)0x00)

// Flags
#define F (*(volatile uint8_t*)0x01)
#define F_C 0  // Carry
#define F_N 1  // Subtract
#define F_PV 2  // Parity/Overflow
#define F_H 4  // Half Carry
#define F_Z 6  // Zero
#define F_S 7  // Sign

// B Register
#define B (*(volatile uint8_t*)0x02)

// C Register
#define C (*(volatile uint8_t*)0x03)

// D Register
#define D (*(volatile uint8_t*)0x04)

// E Register
#define E (*(volatile uint8_t*)0x05)

// H Register
#define H (*(volatile uint8_t*)0x06)

// L Register
#define L (*(volatile uint8_t*)0x07)

// Alternate AF
#define AF (*(volatile uint16_t*)0x08)

// Alternate BC
#define BC (*(volatile uint16_t*)0x0A)

// Alternate DE
#define DE (*(volatile uint16_t*)0x0C)

// Alternate HL
#define HL (*(volatile uint16_t*)0x0E)

// Interrupt Vector
#define I (*(volatile uint8_t*)0x10)

// Refresh
#define R (*(volatile uint8_t*)0x11)

// Index X
#define IX (*(volatile uint16_t*)0x12)

// Index Y
#define IY (*(volatile uint16_t*)0x14)

// Stack Pointer
#define SP (*(volatile uint16_t*)0x16)

// Program Counter
#define PC (*(volatile uint16_t*)0x18)

// 内存段定义
// Lower ROM (AMSDOS / CP/M)
#define LOWER_ROM_START 0x0000
#define LOWER_ROM_END 0x3FFF
#define LOWER_ROM_SIZE 16384

// Lower RAM bank (switchable)
#define RAM_BANK0_START 0x0000
#define RAM_BANK0_END 0x3FFF
#define RAM_BANK0_SIZE 16384

// Main RAM (32KB)
#define RAM_MAIN_START 0x4000
#define RAM_MAIN_END 0xBFFF
#define RAM_MAIN_SIZE 32768

// Upper ROM (BASIC)
#define UPPER_ROM_START 0xC000
#define UPPER_ROM_END 0xFFFF
#define UPPER_ROM_SIZE 16384

// 外设定义
// Gate Array - Custom ASIC (video/sound/RAM control)
#define GA_BASE 0x7F00
#define GA_GA_MR (*(volatile uint8_t*)0x0000FE00)
#define GA_GA_IR (*(volatile uint8_t*)0x0000FE01)
#define GA_GA_R1 (*(volatile uint8_t*)0x0000FE02)
#define GA_GA_R2 (*(volatile uint8_t*)0x0000FE03)
#define GA_GA_R3 (*(volatile uint8_t*)0x0000FE04)
#define GA_GA_R4 (*(volatile uint8_t*)0x0000FE05)
#define GA_GA_R5 (*(volatile uint8_t*)0x0000FE06)
#define GA_GA_R6 (*(volatile uint8_t*)0x0000FE07)
#define GA_GA_R7 (*(volatile uint8_t*)0x0000FE08)

// CRT Controller 6845 - Video timing
#define CRTC_BASE 0xBC00
#define CRTC_CRTC_REG (*(volatile uint8_t*)0x00017800)
#define CRTC_CRTC_DATA (*(volatile uint8_t*)0x00017900)
#define CRTC_CRTC_H_TOTAL (*(volatile uint8_t*)0x00017801)
#define CRTC_CRTC_H_DISP (*(volatile uint8_t*)0x00017802)
#define CRTC_CRTC_HSYNC_POS (*(volatile uint8_t*)0x00017803)
#define CRTC_CRTC_HSYNC_WIDTH (*(volatile uint8_t*)0x00017804)
#define CRTC_CRTC_V_TOTAL (*(volatile uint8_t*)0x00017805)
#define CRTC_CRTC_V_TOTAL_ADJ (*(volatile uint8_t*)0x00017806)
#define CRTC_CRTC_V_DISP (*(volatile uint8_t*)0x00017807)
#define CRTC_CRTC_VSYNC_POS (*(volatile uint8_t*)0x00017808)
#define CRTC_CRTC_INTERLACE (*(volatile uint8_t*)0x00017809)
#define CRTC_CRTC_CURSOR_START (*(volatile uint8_t*)0x0001780A)
#define CRTC_CRTC_CURSOR_END (*(volatile uint8_t*)0x0001780B)
#define CRTC_CRTC_SA_HI (*(volatile uint8_t*)0x0001780C)
#define CRTC_CRTC_SA_LO (*(volatile uint8_t*)0x0001780D)
#define CRTC_CRTC_CURSOR_HI (*(volatile uint8_t*)0x0001780E)
#define CRTC_CRTC_CURSOR_LO (*(volatile uint8_t*)0x0001780F)

// AY-3-8912 Programmable Sound Generator
#define PSG_BASE 0xF400
#define PSG_PSG_REG (*(volatile uint8_t*)0x0001E800)
#define PSG_PSG_DATA (*(volatile uint8_t*)0x0001EA00)
#define PSG_FREQ_A_LO (*(volatile uint8_t*)0x0001E800)
#define PSG_FREQ_A_HI (*(volatile uint8_t*)0x0001E801)
#define PSG_FREQ_B_LO (*(volatile uint8_t*)0x0001E802)
#define PSG_FREQ_B_HI (*(volatile uint8_t*)0x0001E803)
#define PSG_FREQ_C_LO (*(volatile uint8_t*)0x0001E804)
#define PSG_FREQ_C_HI (*(volatile uint8_t*)0x0001E805)
#define PSG_NOISE_FREQ (*(volatile uint8_t*)0x0001E806)
#define PSG_ENABLE (*(volatile uint8_t*)0x0001E807)
#define PSG_VOL_A (*(volatile uint8_t*)0x0001E808)
#define PSG_VOL_B (*(volatile uint8_t*)0x0001E809)
#define PSG_VOL_C (*(volatile uint8_t*)0x0001E80A)
#define PSG_ENV_FREQ_LO (*(volatile uint8_t*)0x0001E80B)
#define PSG_ENV_FREQ_HI (*(volatile uint8_t*)0x0001E80C)
#define PSG_ENV_SHAPE (*(volatile uint8_t*)0x0001E80D)
#define PSG_PORT_A (*(volatile uint8_t*)0x0001E80E)
#define PSG_PORT_B (*(volatile uint8_t*)0x0001E80F)

// WD1772 Floppy Disk Controller (via expansion)
#define FDC_BASE 0xF800
#define FDC_FDC_STATUS (*(volatile uint8_t*)0x0001F0E0)
#define FDC_FDC_COMMAND (*(volatile uint8_t*)0x0001F0E0)
#define FDC_FDC_TRACK (*(volatile uint8_t*)0x0001F0E1)
#define FDC_FDC_SECTOR (*(volatile uint8_t*)0x0001F0E2)
#define FDC_FDC_DATA (*(volatile uint8_t*)0x0001F0E3)

// Centronics Parallel Printer Port
#define PRINTER_BASE 0xEE
#define PRINTER_PRN_DATA (*(volatile uint8_t*)0x000001DC)
#define PRINTER_PRN_STROBE (*(volatile uint8_t*)0x000001DD)

// 中断向量定义
#define RESET_VECTOR 0  // Power-on / Reset
#define NMI_VECTOR 1  // Non-Maskable Interrupt
#define INT_VECTOR 2  // Gate Array interrupt (50Hz vertical blank)

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

void amstrad_cpc_464_init(void);

#ifdef __cplusplus
}
#endif

#endif // AMSTRAD_CPC_464_HPP
