#ifndef RICOH_2A03_HPP
#define RICOH_2A03_HPP

// Ricoh-2A03寄存器定义
// 生成自: Ricoh/MOS-6502/Ricoh-2A03
// 版本: 1.0
// 日期: 2026-04-16


#ifdef __cplusplus
extern "C" {
#endif

// CPU架构: MOS-6502
// 位宽: 8位
// 时钟频率: 10765930 Hz

// 寄存器定义
// Accumulator
#define A (*(volatile uint8_t*)0x00)

// X Index
#define X (*(volatile uint8_t*)0x01)

// Y Index
#define Y (*(volatile uint8_t*)0x02)

// Stack Pointer
#define SP (*(volatile uint8_t*)0x03)

// Program Counter (16-bit)
#define PC (*(volatile uint16_t*)0x04)

// Processor Status
#define P (*(volatile uint8_t*)0x06)
#define P_C 0  // Carry
#define P_Z 1  // Zero
#define P_I 2  // Interrupt Disable
#define P_D 3  // Decimal Mode
#define P_B 4  // Break
#define P_U 5  // Unused
#define P_V 6  // Overflow
#define P_N 7  // Negative

// 内存段定义
// CPU 2KB RAM (mirrored)
#define CPU_RAM_START 0x0000
#define CPU_RAM_END 0x07FF
#define CPU_RAM_SIZE 2048

// PPU Registers (mirrored every 8 bytes)
#define PPU_REGISTERS_START 0x2000
#define PPU_REGISTERS_END 0x3FFF
#define PPU_REGISTERS_SIZE 8192

// APU and I/O Registers
#define APU_REGISTERS_START 0x4000
#define APU_REGISTERS_END 0x401F
#define APU_REGISTERS_SIZE 32

// Expansion ROM
#define EXPANSION_START 0x4020
#define EXPANSION_END 0x5FFF
#define EXPANSION_SIZE 8160

// Save RAM
#define SRAM_START 0x6000
#define SRAM_END 0x7FFF
#define SRAM_SIZE 8192

// PRG ROM Lower Bank (16KB)
#define PRG_ROM_LOW_START 0x8000
#define PRG_ROM_LOW_END 0xBFFF
#define PRG_ROM_LOW_SIZE 16384

// PRG ROM Higher Bank (16KB)
#define PRG_ROM_HIGH_START 0xC000
#define PRG_ROM_HIGH_END 0xFFFF
#define PRG_ROM_HIGH_SIZE 16384

// 外设定义
// Picture Processing Unit
#define PPU_BASE 0x2000
#define PPU_PPUCTRL (*(volatile uint8_t*)0x00004000)
#define PPU_PPUMASK (*(volatile uint8_t*)0x00004001)
#define PPU_PPUSTATUS (*(volatile uint8_t*)0x00004002)
#define PPU_OAMADDR (*(volatile uint8_t*)0x00004003)
#define PPU_OAMDATA (*(volatile uint8_t*)0x00004004)
#define PPU_PPUSCROLL (*(volatile uint8_t*)0x00004005)
#define PPU_PPUADDR (*(volatile uint8_t*)0x00004006)
#define PPU_PPUDATA (*(volatile uint8_t*)0x00004007)

// Audio Processing Unit
#define APU_BASE 0x4000
#define APU_PULSE1_VOL (*(volatile uint8_t*)0x00008000)
#define APU_PULSE1_SWEEP (*(volatile uint8_t*)0x00008001)
#define APU_PULSE1_LO (*(volatile uint8_t*)0x00008002)
#define APU_PULSE1_HI (*(volatile uint8_t*)0x00008003)
#define APU_PULSE2_VOL (*(volatile uint8_t*)0x00008004)
#define APU_PULSE2_SWEEP (*(volatile uint8_t*)0x00008005)
#define APU_PULSE2_LO (*(volatile uint8_t*)0x00008006)
#define APU_PULSE2_HI (*(volatile uint8_t*)0x00008007)
#define APU_TRIANGLE (*(volatile uint8_t*)0x00008008)
#define APU_TRIANGLE_HI (*(volatile uint8_t*)0x0000800B)
#define APU_NOISE_VOL (*(volatile uint8_t*)0x0000800C)
#define APU_NOISE_HI (*(volatile uint8_t*)0x0000800E)
#define APU_NOISE_LENGTH (*(volatile uint8_t*)0x0000800F)
#define APU_DMC_RATE (*(volatile uint8_t*)0x00008010)
#define APU_DMC_RAW (*(volatile uint8_t*)0x00008011)
#define APU_DMC_START (*(volatile uint8_t*)0x00008012)
#define APU_DMC_LENGTH (*(volatile uint8_t*)0x00008013)
#define APU_OAMDMA (*(volatile uint8_t*)0x00008014)
#define APU_SNDCHN (*(volatile uint8_t*)0x00008015)
#define APU_JOY1 (*(volatile uint8_t*)0x00008016)
#define APU_JOY2 (*(volatile uint8_t*)0x00008017)

// Controller Port 1
#define INPUT1_BASE 0x4016
#define INPUT1_JOYPAD1 (*(volatile uint8_t*)0x0000802C)

// Controller Port 2
#define INPUT2_BASE 0x4017
#define INPUT2_JOYPAD2 (*(volatile uint8_t*)0x0000802E)

// 中断向量定义
#define RESET_VECTOR 0  // Reset
#define NMI_VECTOR 1  // Non-Maskable Interrupt (VBlank)
#define IRQ_VECTOR 2  // IRQ / BRK

void ricoh_2a03_init(void);

#ifdef __cplusplus
}
#endif

#endif // RICOH_2A03_HPP
