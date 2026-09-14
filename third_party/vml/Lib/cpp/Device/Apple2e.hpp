#ifndef APPLE_IIE_HPP
#define APPLE_IIE_HPP

// Apple-IIe寄存器定义
// 生成自: Apple Computer/Apple II/Apple-IIe
// 版本: 1.0
// 日期: 2026-04-17


#ifdef __cplusplus
extern "C" {
#endif

// CPU架构: MOS-6502
// 位宽: 8位
// 时钟频率: 1021800 Hz

// 寄存器定义
// Accumulator
#define A (*(volatile uint8_t*)0x00)

// X Index Register
#define X (*(volatile uint8_t*)0x01)

// Y Index Register
#define Y (*(volatile uint8_t*)0x02)

// Stack Pointer
#define SP (*(volatile uint8_t*)0x03)

// Program Counter
#define PC (*(volatile uint16_t*)0x04)

// Processor Status
#define P (*(volatile uint8_t*)0x06)
#define P_C 0  // Carry Flag
#define P_Z 1  // Zero Flag
#define P_I 2  // Interrupt Disable
#define P_D 3  // Decimal Mode
#define P_B 4  // Break Command
#define P_U 5  // Unused
#define P_V 6  // Overflow Flag
#define P_N 7  // Negative Flag

// 内存段定义
// Main RAM (48KB base, up to 64KB with slot RAM)
#define MAIN_RAM_START 0x0000
#define MAIN_RAM_END 0xBFFF
#define MAIN_RAM_SIZE 49152

// Text screen buffer (40x24)
#define TEXT_RAM_START 0x0400
#define TEXT_RAM_END 0x07FF
#define TEXT_RAM_SIZE 1024

// High-resolution graphics buffer
#define HIRES_RAM_START 0x2000
#define HIRES_RAM_END 0x5FFF
#define HIRES_RAM_SIZE 16384

// 80-column text auxiliary RAM
#define AUX_RAM_START 0x0400
#define AUX_RAM_END 0x09FF
#define AUX_RAM_SIZE 1536

// Monitor ROM (applesoft/Integer)
#define MONITOR_ROM_START 0xC100
#define MONITOR_ROM_END 0xCFFF
#define MONITOR_ROM_SIZE 3840

// Applesoft BASIC ROM
#define BASIC_ROM_START 0xD000
#define BASIC_ROM_END 0xFFFF
#define BASIC_ROM_SIZE 12288

// Expansion Slot ROM
#define SLOT_ROM_START 0xC100
#define SLOT_ROM_END 0xC7FF
#define SLOT_ROM_SIZE 768

// I/O Select (slot space)
#define MMIO_START 0xC080
#define MMIO_END 0xC0FF
#define MMIO_SIZE 128

// 外设定义
// Versatile Interface Adapter (6522)
#define VIA_BASE 0xC000
#define VIA_ORB (*(volatile uint8_t*)0x00018000)
#define VIA_ORA (*(volatile uint8_t*)0x00018001)
#define VIA_DDRB (*(volatile uint8_t*)0x00018002)
#define VIA_DDRA (*(volatile uint8_t*)0x00018003)
#define VIA_T1C (*(volatile uint16_t*)0x00018004)
#define VIA_T1L (*(volatile uint16_t*)0x00018006)
#define VIA_T2C (*(volatile uint16_t*)0x00018008)
#define VIA_SR (*(volatile uint8_t*)0x0001800A)
#define VIA_ACR (*(volatile uint8_t*)0x0001800B)
#define VIA_PCR (*(volatile uint8_t*)0x0001800C)
#define VIA_IFG (*(volatile uint8_t*)0x0001800D)
#define VIA_IER (*(volatile uint8_t*)0x0001800E)
#define VIA_ORA_NH (*(volatile uint8_t*)0x0001800F)

// Peripheral Interface Adapter (6520)
#define PIA_BASE 0xC010
#define PIA_PA (*(volatile uint8_t*)0x00018020)
#define PIA_PB (*(volatile uint8_t*)0x00018021)
#define PIA_DDRA (*(volatile uint8_t*)0x00018022)
#define PIA_DDRB (*(volatile uint8_t*)0x00018023)
#define PIA_CA1 (*(volatile uint8_t*)0x00018024)
#define PIA_CA2 (*(volatile uint8_t*)0x00018025)
#define PIA_CB1 (*(volatile uint8_t*)0x00018026)
#define PIA_CB2 (*(volatile uint8_t*)0x00018027)

// Keyboard (via PIA)
#define KBD_BASE 0xC000
#define KBD_KEYDATA (*(volatile uint8_t*)0x00018000)
#define KBD_KEYSTROBE (*(volatile uint8_t*)0x00018010)
#define KBD_KBDCTRL (*(volatile uint8_t*)0x00018025)
#define KBD_KBDERR (*(volatile uint8_t*)0x00018026)

// Speaker
#define SPEAKER_BASE 0xC030
#define SPEAKER_SPKR (*(volatile uint8_t*)0x00018060)

// Game I/O Port
#define GAME_PORT_BASE 0xC050
#define GAME_PORT_GAME_SW0 (*(volatile uint8_t*)0x000180B1)
#define GAME_PORT_GAME_SW1 (*(volatile uint8_t*)0x000180B2)
#define GAME_PORT_GAME_AN0 (*(volatile uint8_t*)0x000180B4)
#define GAME_PORT_GAME_AN1 (*(volatile uint8_t*)0x000180B5)
#define GAME_PORT_GAME_AN2 (*(volatile uint8_t*)0x000180B6)
#define GAME_PORT_GAME_AN3 (*(volatile uint8_t*)0x000180B7)
#define GAME_PORT_GAME_TRIG (*(volatile uint8_t*)0x000180C0)

// Disk II Controller
#define DISKII_BASE 0xC0E0
#define DISKII_PHASE0 (*(volatile uint8_t*)0x000181C0)
#define DISKII_PHASE1 (*(volatile uint8_t*)0x000181C1)
#define DISKII_PHASE2 (*(volatile uint8_t*)0x000181C2)
#define DISKII_PHASE3 (*(volatile uint8_t*)0x000181C3)
#define DISKII_Q6L (*(volatile uint8_t*)0x000181CC)
#define DISKII_Q7L (*(volatile uint8_t*)0x000181CD)
#define DISKII_Q6R (*(volatile uint8_t*)0x000181CE)
#define DISKII_Q7R (*(volatile uint8_t*)0x000181CF)

// Video Display Generator
#define VIDEO_BASE 0xC050
#define VIDEO_TXTCLR (*(volatile uint8_t*)0x000180A0)
#define VIDEO_MIXCLR (*(volatile uint8_t*)0x000180A1)
#define VIDEO_TXTPAGE2 (*(volatile uint8_t*)0x000180A4)
#define VIDEO_TXTPAGE1 (*(volatile uint8_t*)0x000180A5)
#define VIDEO_LORES (*(volatile uint8_t*)0x000180A6)
#define VIDEO_HIRES (*(volatile uint8_t*)0x000180A7)
#define VIDEO_DHIRESON (*(volatile uint8_t*)0x000180AE)
#define VIDEO_AN0 (*(volatile uint8_t*)0x000180A8)
#define VIDEO_AN1 (*(volatile uint8_t*)0x000180A9)
#define VIDEO_AN2 (*(volatile uint8_t*)0x000180AA)
#define VIDEO_AN3 (*(volatile uint8_t*)0x000180AB)
#define VIDEO__80STORE (*(volatile uint8_t*)0x00018050)

// RAM Read/Write Control
#define RAMRD_BASE 0xC080
#define RAMRD_INTCXROM (*(volatile uint8_t*)0x0001907F)

// 中断向量定义
#define RESET_VECTOR 0  // Power-on Reset
#define NMI_VECTOR 1  // Non-Maskable Interrupt (from VIA)
#define IRQ_VECTOR 2  // IRQ from VIA/timer/slot
#define BRK_VECTOR 3  // BRK Instruction

// 引脚定义
#define PIN_VCC 1  // +5V Power
#define PIN_GND 2  // Ground
#define PIN_RESET 3  // System Reset
#define PIN_CLK 4  // System Clock (1.023MHz NTSC)
#define PIN_RDY 5  // CPU Ready
#define PIN_NMI 6  // Non-Maskable Interrupt
#define PIN_IRQ 7  // Interrupt Request
#define PIN_SO 8  // Set Overflow
#define PIN_RWB 9  // Read/Write Bar
#define PIN_SYNC 10  // Instruction Sync
#define PIN_A0_A15 11  // Address Bus (16-bit)
#define PIN_D0_D7 12  // Data Bus (8-bit)
#define PIN_PHASE0 13  // Phase 0 (4MHz system)
#define PIN_PHASE1 14  // Phase 1
#define PIN_PHASE2 15  // Phase 2
#define PIN_PHASE3 16  // Phase 3

void apple_iie_init(void);

#ifdef __cplusplus
}
#endif

#endif // APPLE_IIE_HPP
