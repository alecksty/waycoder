#ifndef SHARP_LR35902_HPP
#define SHARP_LR35902_HPP

// Sharp-LR35902寄存器定义
// 生成自: Sharp/Z80/Sharp-LR35902
// 版本: 1.0
// 日期: 2026-04-16


#ifdef __cplusplus
extern "C" {
#endif

// CPU架构: LR35902
// 位宽: 8位
// 时钟频率: 4194304 Hz

// 寄存器定义
// Accumulator
#define A (*(volatile uint8_t*)0x00)

// B Register
#define B (*(volatile uint8_t*)0x01)

// C Register
#define C (*(volatile uint8_t*)0x02)

// D Register
#define D (*(volatile uint8_t*)0x03)

// E Register
#define E (*(volatile uint8_t*)0x04)

// Flags Register
#define F (*(volatile uint8_t*)0x05)
#define F_C 4  // Carry
#define F_H 5  // Half Carry
#define F_N 6  // Subtract
#define F_Z 7  // Zero

// H Register
#define H (*(volatile uint8_t*)0x06)

// L Register
#define L (*(volatile uint8_t*)0x07)

// AF Register Pair (Accumulator + Flags)
#define AF (*(volatile uint16_t*)0x08)

// BC Register Pair
#define BC (*(volatile uint16_t*)0x0A)

// DE Register Pair
#define DE (*(volatile uint16_t*)0x0C)

// HL Register Pair
#define HL (*(volatile uint16_t*)0x0E)

// Stack Pointer
#define SP (*(volatile uint16_t*)0x10)

// Program Counter
#define PC (*(volatile uint16_t*)0x12)

// 内存段定义
// Work RAM (4KB)
#define WRAM_START 0xC000
#define WRAM_END 0xCFFF
#define WRAM_SIZE 4096

// Work RAM Shadow (Echo RAM)
#define WRAM_SHADOW_START 0xE000
#define WRAM_SHADOW_END 0xEFFF
#define WRAM_SHADOW_SIZE 4096

// High RAM (127 bytes)
#define HRAM_START 0xFF80
#define HRAM_END 0xFFFE
#define HRAM_SIZE 127

// I/O Registers
#define IO_REGISTERS_START 0xFF00
#define IO_REGISTERS_END 0xFF7F
#define IO_REGISTERS_SIZE 128

// Sprite Attribute Table (OAM)
#define OAM_START 0xFE00
#define OAM_END 0xFE9F
#define OAM_SIZE 160

// Video RAM (8KB)
#define VRAM_START 0x8000
#define VRAM_END 0x9FFF
#define VRAM_SIZE 8192

// Background Map 1
#define BG_MAP_1_START 0x9800
#define BG_MAP_1_END 0x9BFF
#define BG_MAP_1_SIZE 1024

// Background Map 2
#define BG_MAP_2_START 0x9C00
#define BG_MAP_2_END 0x9FFF
#define BG_MAP_2_SIZE 1024

// ROM Bank 0 (Cartridge Header)
#define ROM_BANK0_START 0x0000
#define ROM_BANK0_END 0x3FFF
#define ROM_BANK0_SIZE 16384

// ROM Bank 1 (Switchable)
#define ROM_BANK1_START 0x4000
#define ROM_BANK1_END 0x7FFF
#define ROM_BANK1_SIZE 16384

// Cartridge RAM / MBC
#define CART_RAM_START 0xA000
#define CART_RAM_END 0xBFFF
#define CART_RAM_SIZE 8192

// 外设定义
// LCD Controller / Picture Processing Unit
#define PPU_BASE 0xFF40
#define PPU_LCDC (*(volatile uint8_t*)0x0001FE80)
#define PPU_LCDC_BG_ENABLE 0  // Background Display Enable
#define PPU_LCDC_SPRITE_ENABLE 1  // Sprite Display Enable
#define PPU_LCDC_SPRITE_SIZE 2  // Sprite Size (0=8x8, 1=8x16)
#define PPU_LCDC_BG_TILE_MAP 3  // BG Tile Map Area (0=9800, 1=9C00)
#define PPU_LCDC_TILE_DATA 4  // Tile Data Area (0=8800, 1=8000)
#define PPU_LCDC_WINDOW_ENABLE 5  // Window Display Enable
#define PPU_LCDC_WINDOW_MAP 6  // Window Tile Map Area (0=9800, 1=9C00)
#define PPU_LCDC_LCD_ENABLE 7  // LCD Display Enable
#define PPU_STAT (*(volatile uint8_t*)0x0001FE81)
#define PPU_STAT_MODE 0  // LCD Mode (0=H-Blank, 1=V-Blank, 2=OAM, 3=VRAM)
#define PPU_STAT_LYC_FLAG 2  // LY=LYC Compare Flag
#define PPU_STAT_HBLANK_IRQ 3  // H-Blank Interrupt Enable
#define PPU_STAT_VBLANK_IRQ 4  // V-Blank Interrupt Enable
#define PPU_STAT_OAM_IRQ 5  // OAM Interrupt Enable
#define PPU_STAT_LYC_IRQ 6  // LYC Interrupt Enable
#define PPU_SCY (*(volatile uint8_t*)0x0001FE82)
#define PPU_SCX (*(volatile uint8_t*)0x0001FE83)
#define PPU_LY (*(volatile uint8_t*)0x0001FE84)
#define PPU_LYC (*(volatile uint8_t*)0x0001FE85)
#define PPU_DMA (*(volatile uint8_t*)0x0001FE86)
#define PPU_BGP (*(volatile uint8_t*)0x0001FE87)
#define PPU_OBP0 (*(volatile uint8_t*)0x0001FE88)
#define PPU_OBP1 (*(volatile uint8_t*)0x0001FE89)
#define PPU_WY (*(volatile uint8_t*)0x0001FE8A)
#define PPU_WX (*(volatile uint8_t*)0x0001FE8B)

// Audio Processing Unit
#define APU_BASE 0xFF10
#define APU_NR10 (*(volatile uint8_t*)0x0001FE20)
#define APU_NR10_SWEEP_TIME 0  // Sweep Time
#define APU_NR10_SWEEP_INCREASE 3  // Sweep Increase/Decrease
#define APU_NR10_SWEEP_SHIFTS 0  // Sweep Number of Shifts
#define APU_NR11 (*(volatile uint8_t*)0x0001FE21)
#define APU_NR12 (*(volatile uint8_t*)0x0001FE22)
#define APU_NR13 (*(volatile uint8_t*)0x0001FE23)
#define APU_NR14 (*(volatile uint8_t*)0x0001FE24)
#define APU_NR21 (*(volatile uint8_t*)0x0001FE26)
#define APU_NR22 (*(volatile uint8_t*)0x0001FE27)
#define APU_NR23 (*(volatile uint8_t*)0x0001FE28)
#define APU_NR24 (*(volatile uint8_t*)0x0001FE29)
#define APU_NR30 (*(volatile uint8_t*)0x0001FE2A)
#define APU_NR31 (*(volatile uint8_t*)0x0001FE2B)
#define APU_NR32 (*(volatile uint8_t*)0x0001FE2C)
#define APU_NR33 (*(volatile uint8_t*)0x0001FE2D)
#define APU_NR34 (*(volatile uint8_t*)0x0001FE2E)
#define APU_NR41 (*(volatile uint8_t*)0x0001FE30)
#define APU_NR42 (*(volatile uint8_t*)0x0001FE31)
#define APU_NR43 (*(volatile uint8_t*)0x0001FE32)
#define APU_NR44 (*(volatile uint8_t*)0x0001FE33)
#define APU_NR50 (*(volatile uint8_t*)0x0001FE34)
#define APU_NR51 (*(volatile uint8_t*)0x0001FE35)
#define APU_NR52 (*(volatile uint8_t*)0x0001FE36)
#define APU_NR52_CH1_ON 0  // Channel 1 ON
#define APU_NR52_CH2_ON 1  // Channel 2 ON
#define APU_NR52_CH3_ON 2  // Channel 3 ON
#define APU_NR52_CH4_ON 3  // Channel 4 ON
#define APU_NR52_ALL_ON 7  // All Sound ON

// Timer Unit
#define TIMER_BASE 0xFF04
#define TIMER_DIV (*(volatile uint8_t*)0x0001FE08)
#define TIMER_TIMA (*(volatile uint8_t*)0x0001FE09)
#define TIMER_TMA (*(volatile uint8_t*)0x0001FE0A)
#define TIMER_TAC (*(volatile uint8_t*)0x0001FE0B)
#define TIMER_TAC_TIMER_ENABLE 2  // Timer Enable
#define TIMER_TAC_CLOCK_SEL 0  // Clock Select (00=4kHz, 01=262kHz, 10=65kHz, 11=16kHz)

// Joypad Controller
#define JOYPAD_BASE 0xFF00
#define JOYPAD_P1 (*(volatile uint8_t*)0x0001FE00)
#define JOYPAD_P1_A_BTN 0  // A Button (1=Pressed when selected)
#define JOYPAD_P1_B_BTN 1  // B Button (1=Pressed when selected)
#define JOYPAD_P1_SELECT 2  // Select Button (1=Pressed)
#define JOYPAD_P1_START 3  // Start Button (1=Pressed)
#define JOYPAD_P1_DIR_DOWN 4  // Direction Down (1=Pressed when selected)
#define JOYPAD_P1_DIR_UP 5  // Direction Up (1=Pressed when selected)
#define JOYPAD_P1_DIR_LEFT 6  // Direction Left (1=Pressed when selected)
#define JOYPAD_P1_DIR_RIGHT 7  // Direction Right (1=Pressed when selected)

// Serial I/O (Link Cable)
#define SERIAL_BASE 0xFF01
#define SERIAL_SB (*(volatile uint8_t*)0x0001FE02)
#define SERIAL_SC (*(volatile uint8_t*)0x0001FE03)
#define SERIAL_SC_TRANSFER_START 7  // Transfer Start
#define SERIAL_SC_CLOCK_SPEED 1  // Clock Select (0=External, 1=Internal 8192Hz)

// Interrupt Flag Register
#define INTERRUPT_BASE 0xFF0F
#define INTERRUPT_IF (*(volatile uint8_t*)0x0001FE1E)
#define INTERRUPT_IF_VBLANK 0  // V-Blank Interrupt Request
#define INTERRUPT_IF_LCDC 1  // LCDC Status Interrupt Request
#define INTERRUPT_IF_TIMER 2  // Timer Overflow Interrupt Request
#define INTERRUPT_IF_SERIAL 3  // Serial Transfer Complete Interrupt Request
#define INTERRUPT_IF_JOYPAD 4  // Joypad Interrupt Request

// Interrupt Enable Register
#define IE_BASE 0xFFFF
#define IE_IE (*(volatile uint8_t*)0x0001FFFE)
#define IE_IE_VBLANK_IE 0  // V-Blank Interrupt Enable
#define IE_IE_LCDC_IE 1  // LCDC Status Interrupt Enable
#define IE_IE_TIMER_IE 2  // Timer Interrupt Enable
#define IE_IE_SERIAL_IE 3  // Serial Interrupt Enable
#define IE_IE_JOYPAD_IE 4  // Joypad Interrupt Enable

// 中断向量定义
#define VBLANK_VECTOR 0  // V-Blank Interrupt (LY=144, during vertical blanking)
#define LCDC_STATUS_VECTOR 1  // LCDC Status Interrupt (H-Blank/OAM/V-Count match)
#define TIMER_OVERFLOW_VECTOR 2  // Timer Overflow Interrupt (TIMA overflow)
#define SERIAL_COMPLETE_VECTOR 3  // Serial Transfer Complete Interrupt
#define JOYPAD_VECTOR 4  // Joypad Interrupt (button press/release)

// 引脚定义
#define PIN_VSS 1  // Ground
#define PIN_VDD 2  // Power Supply
#define PIN_PHI 3  // System Clock Output (4.19MHz / 2 = 2.1MHz CPU)
#define PIN_RESET 4  // Reset Signal (active low)
#define PIN_INT 5  // Interrupt Request
#define PIN_BUSREQ 6  // Bus Request (external DMA access)
#define PIN_A0 7  // Address Bus Bit 0
#define PIN_A1 8  // Address Bus Bit 1
#define PIN_A2 9  // Address Bus Bit 2
#define PIN_A3 10  // Address Bus Bit 3
#define PIN_A4 11  // Address Bus Bit 4
#define PIN_A5 12  // Address Bus Bit 5
#define PIN_A6 13  // Address Bus Bit 6
#define PIN_A7 14  // Address Bus Bit 7
#define PIN_A8 15  // Address Bus Bit 8
#define PIN_A9 16  // Address Bus Bit 9
#define PIN_A10 17  // Address Bus Bit 10
#define PIN_A11 18  // Address Bus Bit 11
#define PIN_A12 19  // Address Bus Bit 12
#define PIN_A13 20  // Address Bus Bit 13
#define PIN_A14 21  // Address Bus Bit 14
#define PIN_A15 22  // Address Bus Bit 15
#define PIN_D0 23  // Data Bus Bit 0
#define PIN_D1 24  // Data Bus Bit 1
#define PIN_D2 25  // Data Bus Bit 2
#define PIN_D3 26  // Data Bus Bit 3
#define PIN_D4 27  // Data Bus Bit 4
#define PIN_D5 28  // Data Bus Bit 5
#define PIN_D6 29  // Data Bus Bit 6
#define PIN_D7 30  // Data Bus Bit 7
#define PIN_RD 31  // Read Strobe (active low)
#define PIN_WR 32  // Write Strobe (active low)
#define PIN_CS 33  // Chip Select (active low)
#define PIN_SOUND_OUT 34  // Audio Output
#define PIN_LCD_DATA0 35  // LCD Data Bus Bit 0
#define PIN_LCD_DATA1 36  // LCD Data Bus Bit 1
#define PIN_LCD_DATA2 37  // LCD Data Bus Bit 2
#define PIN_LCD_DATA3 38  // LCD Data Bus Bit 3
#define PIN_LCD_DATA4 39  // LCD Data Bus Bit 4
#define PIN_LCD_DATA5 40  // LCD Data Bus Bit 5
#define PIN_LCD_DATA6 41  // LCD Data Bus Bit 6
#define PIN_LCD_DATA7 42  // LCD Data Bus Bit 7
#define PIN_IR 43  // Infrared Port (DMG-CGB-01)

void sharp_lr35902_init(void);

#ifdef __cplusplus
}
#endif

#endif // SHARP_LR35902_HPP
