#ifndef ZILOG_Z80_HPP
#define ZILOG_Z80_HPP

// Zilog-Z80寄存器定义
// 生成自: Zilog/Z80/Zilog-Z80
// 版本: 1.0
// 日期: 2026-04-16


#ifdef __cplusplus
extern "C" {
#endif

// CPU架构: Z80
// 位宽: 8位
// 时钟频率: 3580000 Hz

// 寄存器定义
// Accumulator
#define A (*(volatile uint8_t*)0x00)

// Flags Register
#define F (*(volatile uint8_t*)0x01)
#define F_C 0  // Carry
#define F_N 1  // Subtract
#define F_P 2  // Parity/Overflow
#define F_H 4  // Half Carry
#define F_Z 6  // Zero
#define F_S 7  // Sign/Negative

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

// Index Register X
#define IX (*(volatile uint16_t*)0x10)

// Index Register Y
#define IY (*(volatile uint16_t*)0x12)

// Stack Pointer
#define SP (*(volatile uint16_t*)0x14)

// Program Counter
#define PC (*(volatile uint16_t*)0x16)

// Interrupt Vector Register
#define I (*(volatile uint8_t*)0x18)

// Memory Refresh Register
#define R (*(volatile uint8_t*)0x19)

// Interrupt Mode (0/1/2)
#define IM (*(volatile uint8_t*)0x1A)

// 内存段定义
// Work RAM (2KB internal)
#define WRAM_START 0xC000
#define WRAM_END 0xC7FF
#define WRAM_SIZE 2048

// Work RAM Shadow (Echo RAM)
#define WRAM_SHADOW_START 0xE000
#define WRAM_SHADOW_END 0xE7FF
#define WRAM_SHADOW_SIZE 2048

// Video RAM (16KB)
#define VRAM_START 0x4000
#define VRAM_END 0x7FFF
#define VRAM_SIZE 16384

// Cartridge SRAM (if present)
#define SRAM_START 0x8000
#define SRAM_END 0xBFFF
#define SRAM_SIZE 16384

// Cartridge ROM (up to 48KB)
#define CART_ROM_START 0x0000
#define CART_ROM_END 0x7FFF
#define CART_ROM_SIZE 32768

// BIOS ROM (Master System built-in, 8KB)
#define BIOS_START 0x0000
#define BIOS_END 0x1FFF
#define BIOS_SIZE 8192

// I/O Register Area
#define IO_REGS_START 0x3F00
#define IO_REGS_END 0x3FFF
#define IO_REGS_SIZE 256

// 外设定义
// Video Display Processor (TMS9918A variant)
#define VDP_BASE 0xBE
#define VDP_VDP_CTRL (*(volatile uint8_t*)0x0000017D)
#define VDP_VDP_DATA (*(volatile uint8_t*)0x0000017C)
#define VDP_VDP_STATUS (*(volatile uint8_t*)0x0000017D)
#define VDP_VDP_STATUS_FIFO_FULL 0  // VRAM to CPU Transfer Pending
#define VDP_VDP_STATUS_FIFO_EMPTY 1  // VRAM Write FIFO Empty
#define VDP_VDP_STATUS_INT_FLAG 7  // V-Blank / Sprite Collision Flag
#define VDP_R0 (*(volatile uint8_t*)0x000000BE)
#define VDP_R0_M3 0  // Mode 3 Enable
#define VDP_R0_M2 1  // Mode 2 Enable
#define VDP_R0_M1 2  // Mode 1 Enable
#define VDP_R0_DISPLAY_DISABLE 3  // Display Disable (1=blank screen)
#define VDP_R0_VIRQ_EN 4  // Vertical Interrupt Enable
#define VDP_R0_M4 5  // Mode 4 Enable (SMS2 only)
#define VDP_R0_SPRITE_SHIFT 6  // Sprite Double Height
#define VDP_R0_HVC_LATCH 7  // H-Counter Latch Enable
#define VDP_R1 (*(volatile uint8_t*)0x000000BF)
#define VDP_R1_DISPLAY 3  // Display Enable (1=active)
#define VDP_R1_FRAME_INT 4  // Frame Interrupt (V-Blank) Enable
#define VDP_R1_M4 5  // Mode 4 (256-color)
#define VDP_R1_SMS_MODE 6  // SMS Display Mode (vs Coleco)
#define VDP_R1_EXT_VIDEO 7  // External Video Enable
#define VDP_R2 (*(volatile uint8_t*)0x000000C0)
#define VDP_R3 (*(volatile uint8_t*)0x000000C1)
#define VDP_R4 (*(volatile uint8_t*)0x000000C2)
#define VDP_R5 (*(volatile uint8_t*)0x000000C3)
#define VDP_R6 (*(volatile uint8_t*)0x000000C4)
#define VDP_R7 (*(volatile uint8_t*)0x000000C5)
#define VDP_R8 (*(volatile uint8_t*)0x000000C6)
#define VDP_R8_HSCROLL_EN 0  // Horizontal Scroll Enable
#define VDP_R8_VSCROLL_EN 1  // Vertical Scroll Enable
#define VDP_R8_LINE_INT 4  // Line Interrupt Enable
#define VDP_R8_VSCROLL_2X 7  // Vertical Scroll 2x Speed
#define VDP_R9 (*(volatile uint8_t*)0x000000C7)
#define VDP_R10 (*(volatile uint8_t*)0x000000C8)
#define VDP_R11 (*(volatile uint8_t*)0x000000C9)
#define VDP_R12 (*(volatile uint8_t*)0x000000CA)
#define VDP_R13 (*(volatile uint8_t*)0x000000CB)
#define VDP_R14 (*(volatile uint8_t*)0x000000CC)
#define VDP_R15 (*(volatile uint8_t*)0x000000CD)
#define VDP_VCOUNTER (*(volatile uint8_t*)0x0000013C)
#define VDP_HCOUNTER (*(volatile uint8_t*)0x0000013D)

// SN76489 Programmable Sound Generator (3 Square + 1 Noise)
#define PSG_BASE 0x7F
#define PSG_CH0_FREQ (*(volatile uint8_t*)0x0000007F)
#define PSG_CH1_FREQ (*(volatile uint8_t*)0x00000081)
#define PSG_CH2_FREQ (*(volatile uint8_t*)0x00000083)
#define PSG_CH3_CONFIG (*(volatile uint8_t*)0x00000085)
#define PSG_CH3_CONFIG_TYPE 0  // Noise Type (0=White, 1=Periodic, 2-3=Periodic at freq/2^type)
#define PSG_CH3_CONFIG_VOLUME 0  // Volume (0-15)
#define PSG_CH0_VOLUME (*(volatile uint8_t*)0x00000080)
#define PSG_CH1_VOLUME (*(volatile uint8_t*)0x00000082)
#define PSG_CH2_VOLUME (*(volatile uint8_t*)0x00000084)

// I/O Port Registers
#define PORTS_BASE 0x3F
#define PORTS_PORT_A (*(volatile uint8_t*)0x0000007E)
#define PORTS_PORT_A_UP 0  // Up (0=pressed)
#define PORTS_PORT_A_DOWN 1  // Down (0=pressed)
#define PORTS_PORT_A_LEFT 2  // Left (0=pressed)
#define PORTS_PORT_A_RIGHT 3  // Right (0=pressed)
#define PORTS_PORT_A_TR 4  // Button TR (0=pressed)
#define PORTS_PORT_A_TL 5  // Button TL (0=pressed)
#define PORTS_PORT_B (*(volatile uint8_t*)0x0000007E)
#define PORTS_PORT_B_UP 0  // Up (0=pressed)
#define PORTS_PORT_B_DOWN 1  // Down (0=pressed)
#define PORTS_PORT_B_LEFT 2  // Left (0=pressed)
#define PORTS_PORT_B_RIGHT 3  // Right (0=pressed)
#define PORTS_PORT_B_TR 4  // Button TR (0=pressed)
#define PORTS_PORT_B_TL 5  // Button TL (0=pressed)
#define PORTS_PORT_A_DDR (*(volatile uint8_t*)0x0000007E)
#define PORTS_PORT_B_DDR (*(volatile uint8_t*)0x0000007E)

// Sega Mapper (Memory Bank Switching)
#define SEGAMAPPER_BASE 0xFFFD
#define SEGAMAPPER_ROM_BANK0 (*(volatile uint8_t*)0x0001FFFA)
#define SEGAMAPPER_ROM_BANK1 (*(volatile uint8_t*)0x0001FFFB)
#define SEGAMAPPER_ROM_BANK2 (*(volatile uint8_t*)0x0001FFFC)

// Memory Mapper Control
#define MAPPER_BASE 0xFFFF
#define MAPPER_SRAM_BANK (*(volatile uint8_t*)0x0001FFF7)

// 中断向量定义
#define NMI_VECTOR 0  // Non-Maskable Interrupt (Pause button / V-Blank)
#define INT_VBLANK_VECTOR 1  // V-Blank Interrupt (Frame end)
#define INT_LINE_VECTOR 2  // Scanline Interrupt (Line counter match)
#define INT_EXT_VECTOR 3  // External I/O Interrupt

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

void zilog_z80_init(void);

#ifdef __cplusplus
}
#endif

#endif // ZILOG_Z80_HPP
