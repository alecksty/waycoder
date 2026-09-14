#ifndef ZX_SPECTRUM_48K_HPP
#define ZX_SPECTRUM_48K_HPP

// ZX-Spectrum-48K寄存器定义
// 生成自: Sinclair Research/ZX Spectrum/ZX-Spectrum-48K
// 版本: 1.0
// 日期: 2026-04-17


#ifdef __cplusplus
extern "C" {
#endif

// CPU架构: Z80A
// 位宽: 8位
// 时钟频率: 3500000 Hz

// 寄存器定义
// Accumulator
#define A (*(volatile uint8_t*)0x00)

// Flags Register
#define F (*(volatile uint8_t*)0x01)
#define F_C 0  // Carry
#define F_N 1  // Add/Subtract
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

// Interrupt Vector Register
#define I (*(volatile uint8_t*)0x10)

// Refresh Counter
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
// 48KB ZX Spectrum ROM (BASIC + monitor)
#define ROM_START 0x0000
#define ROM_END 0x3FFF
#define ROM_SIZE 16384

// Display file (256x192 bitmap)
#define VIDEO_RAM_START 0x4000
#define VIDEO_RAM_END 0x57FF
#define VIDEO_RAM_SIZE 6144

// Attribute file (32x24 color cells)
#define ATTR_RAM_START 0x5800
#define ATTR_RAM_END 0x5AFF
#define ATTR_RAM_SIZE 768

// User RAM (40KB)
#define USER_RAM_START 0x5B00
#define USER_RAM_END 0xFFFF
#define USER_RAM_SIZE 40960

// 外设定义
// Uncommitted Logic Array - Sinclair custom IC
#define ULA_BASE 0xFE
#define ULA_BORDER (*(volatile uint8_t*)0x000001FC)
#define ULA_KBD_ROW0 (*(volatile uint8_t*)0x000001FC)
#define ULA_KBD_ROW1 (*(volatile uint8_t*)0x000001FC)
#define ULA_KBD_ROW2 (*(volatile uint8_t*)0x000001FC)
#define ULA_KBD_ROW3 (*(volatile uint8_t*)0x000001FC)
#define ULA_KBD_ROW4 (*(volatile uint8_t*)0x000001FC)
#define ULA_KBD_ROW5 (*(volatile uint8_t*)0x000001FC)
#define ULA_KBD_ROW6 (*(volatile uint8_t*)0x000001FC)
#define ULA_KBD_ROW7 (*(volatile uint8_t*)0x000001FC)
#define ULA_KBD_ROW8 (*(volatile uint8_t*)0x000001FC)

// Keyboard Matrix (40 keys, 8 rows x 5 cols)
#define KEYBOARD_BASE 0xFE
#define KEYBOARD_KBD_IN (*(volatile uint8_t*)0x000001FC)

// Internal Beeper
#define BEEPER_BASE 0xFE
#define BEEPER_BEEP (*(volatile uint8_t*)0x000001FC)

// Tape Interface
#define TAPE_BASE 0xFE
#define TAPE_EAR_IN (*(volatile uint8_t*)0x000001FC)
#define TAPE_MIC_OUT (*(volatile uint8_t*)0x000001FC)

// Kempston Joystick Interface
#define JOYSTICK_BASE 0xF7FE
#define JOYSTICK_KEMPSTON (*(volatile uint8_t*)0x0001EFFC)

// 中断向量定义
#define RESET_VECTOR 0  // Power-on / Reset
#define NMI_VECTOR 1  // Non-Maskable Interrupt (BREAK key)
#define INT_VECTOR 2  // Maskable Interrupt (ULA vertical blank, 50Hz)

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

void zx_spectrum_48k_init(void);

#ifdef __cplusplus
}
#endif

#endif // ZX_SPECTRUM_48K_HPP
