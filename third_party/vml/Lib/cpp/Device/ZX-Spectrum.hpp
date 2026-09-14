#ifndef ZX_SPECTRUM_HPP
#define ZX_SPECTRUM_HPP

// ZX-Spectrum寄存器定义
// 生成自: Sinclair Research/ZX Spectrum/ZX-Spectrum
// 版本: 1.0
// 日期: 2026-04-17


#ifdef __cplusplus
extern "C" {
#endif

// CPU架构: Zilog Z80
// 位宽: 8位
// 时钟频率: 3500000 Hz

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

// Alternate AF
#define AF (*(volatile uint16_t*)0)

// Alternate BC
#define BC (*(volatile uint16_t*)0)

// Alternate DE
#define DE (*(volatile uint16_t*)0)

// Alternate HL
#define HL (*(volatile uint16_t*)0)

// 外设定义
// Uncommitted Logic Array (video and I/O)
#define ULA_BASE 
#define ULA_ULA_PORT_FE (*(volatile uint8_t*)0x000000FE)
#define ULA_ULA_BORDER (*(volatile uint8_t*)0x000000FE)
#define ULA_ULA_BEEPER (*(volatile uint8_t*)0x000000FE)
#define ULA_ULA_MIC (*(volatile uint8_t*)0x000000FE)

// General Instruments AY-3-8912 sound chip
#define AY_3_8912_BASE 
#define AY_3_8912_AY_REG_SEL (*(volatile uint8_t*)0x0000FFFD)
#define AY_3_8912_AY_DATA (*(volatile uint8_t*)0x0000BFFD)
#define AY_3_8912_AY_READ (*(volatile uint8_t*)0x0000FFFD)

// 40-key rubber keyboard
#define KEYBOARD_BASE 
#define KEYBOARD_KEY_ROW0 (*(volatile uint8_t*)0x0000FEFE)
#define KEYBOARD_KEY_ROW1 (*(volatile uint8_t*)0x0000FDFE)
#define KEYBOARD_KEY_ROW2 (*(volatile uint8_t*)0x0000FBFE)
#define KEYBOARD_KEY_ROW3 (*(volatile uint8_t*)0x0000F7FE)
#define KEYBOARD_KEY_ROW4 (*(volatile uint8_t*)0x0000EFFE)
#define KEYBOARD_KEY_ROW5 (*(volatile uint8_t*)0x0000DFFE)
#define KEYBOARD_KEY_ROW6 (*(volatile uint8_t*)0x0000BFFE)
#define KEYBOARD_KEY_ROW7 (*(volatile uint8_t*)0x00007FFE)

// Kempston joystick interface
#define KEMPSTON_BASE 
#define KEMPSTON_KEMPSTON_JOY (*(volatile uint8_t*)0x0000001F)

// ZX Interface 1 (RS-232 and Microdrive)
#define INTERFACE1_BASE 
#define INTERFACE1_IF1_STATUS (*(volatile uint8_t*)0x00001FFD)
#define INTERFACE1_IF1_DATA (*(volatile uint8_t*)0x00003FFD)

// ZX Interface 2 (joystick and ROM cartridge)
#define INTERFACE2_BASE 
#define INTERFACE2_IF2_JOY1 (*(volatile uint8_t*)0x0000001F)
#define INTERFACE2_IF2_JOY2 (*(volatile uint8_t*)0x00000037)

// 中断向量定义
#define IM1_VECTOR 56  // Interrupt Mode 1
#define RST_00_VECTOR 0  // Restart 00h
#define RST_08_VECTOR 8  // Restart 08h
#define RST_10_VECTOR 16  // Restart 10h
#define RST_18_VECTOR 24  // Restart 18h
#define RST_20_VECTOR 32  // Restart 20h
#define RST_28_VECTOR 40  // Restart 28h
#define RST_30_VECTOR 48  // Restart 30h
#define RST_38_VECTOR 56  // Restart 38h

void zx_spectrum_init(void);

#ifdef __cplusplus
}
#endif

#endif // ZX_SPECTRUM_HPP
