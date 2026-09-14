#ifndef NINTENDO_ENTERTAINMENT_SYSTEM_HPP
#define NINTENDO_ENTERTAINMENT_SYSTEM_HPP

// Nintendo Entertainment System寄存器定义
// 生成自: Nintendo/NES/Nintendo Entertainment System
// 版本: 
// 日期: 


#ifdef __cplusplus
extern "C" {
#endif

// CPU架构: 6502
// 位宽: 0位
// 时钟频率: 0 Hz

// 外设定义
// Picture Processing Unit (Ricoh 2C02)
#define PPU_BASE 
#define PPU_PPUCTRL (*(volatile uint64_t*)0x00002000)
#define PPU_PPUCTRL_NMI 7  // VBlank NMI enable
#define PPU_PPUCTRL_MASTERSLAVE 6  // Master/slave select
#define PPU_PPUCTRL_SPRITESIZE 5  // Sprite size (0=8x8, 1=8x16)
#define PPU_PPUCTRL_BGPATTERN 4  // Background pattern table address
#define PPU_PPUCTRL_SPRITEPATTERN 3  // Sprite pattern table address
#define PPU_PPUCTRL_VRAMINCREMENT 2  // VRAM address increment (0=1, 1=32)
#define PPU_PPUCTRL_NAMETABLE 0  // Nametable address
#define PPU_PPUMASK (*(volatile uint64_t*)0x00002001)
#define PPU_PPUMASK_EMPHASIZEBLUE 7  // Emphasize blue
#define PPU_PPUMASK_EMPHASIZEGREEN 6  // Emphasize green
#define PPU_PPUMASK_EMPHASIZERED 5  // Emphasize red
#define PPU_PPUMASK_SHOWSPRITES 4  // Show sprites
#define PPU_PPUMASK_SHOWBACKGROUND 3  // Show background
#define PPU_PPUMASK_SHOWLEFTSPRITES 2  // Show sprites in left 8 pixels
#define PPU_PPUMASK_SHOWLEFTBACKGROUND 1  // Show background in left 8 pixels
#define PPU_PPUMASK_GRAYSCALE 0  // Grayscale mode
#define PPU_PPUSTATUS (*(volatile uint64_t*)0x00002002)
#define PPU_PPUSTATUS_VBLANK 7  // VBlank started
#define PPU_PPUSTATUS_SPRITE0HIT 6  // Sprite 0 hit
#define PPU_PPUSTATUS_SPRITEOVERFLOW 5  // Sprite overflow
#define PPU_OAMADDR (*(volatile uint64_t*)0x00002003)
#define PPU_OAMDATA (*(volatile uint64_t*)0x00002004)
#define PPU_PPUSCROLL (*(volatile uint64_t*)0x00002005)
#define PPU_PPUADDR (*(volatile uint64_t*)0x00002006)
#define PPU_PPUDATA (*(volatile uint64_t*)0x00002007)
#define PPU_OAMDMA (*(volatile uint64_t*)0x00004014)

// Audio Processing Unit (Ricoh 2A03)
#define APU_BASE 
#define APU_SQ1_VOL (*(volatile uint64_t*)0x00004000)
#define APU_SQ1_VOL_DUTY 6  // Duty cycle
#define APU_SQ1_VOL_LENGTHCOUNTERHALT 5  // Length counter halt/envelope loop
#define APU_SQ1_VOL_CONSTANTVOLUME 4  // Constant volume
#define APU_SQ1_VOL_VOLUME 0  // Volume/envelope period
#define APU_SQ1_SWEEP (*(volatile uint64_t*)0x00004001)
#define APU_SQ1_SWEEP_ENABLED 7  // Sweep enabled
#define APU_SQ1_SWEEP_PERIOD 4  // Sweep period
#define APU_SQ1_SWEEP_NEGATE 3  // Sweep negate
#define APU_SQ1_SWEEP_SHIFT 0  // Sweep shift amount
#define APU_SQ1_LO (*(volatile uint64_t*)0x00004002)
#define APU_SQ1_HI (*(volatile uint64_t*)0x00004003)
#define APU_SQ1_HI_LENGTHCOUNTER 3  // Length counter load
#define APU_SQ1_HI_TIMERHIGH 0  // Timer high bits
#define APU_SQ2_VOL (*(volatile uint64_t*)0x00004004)
#define APU_SQ2_SWEEP (*(volatile uint64_t*)0x00004005)
#define APU_SQ2_LO (*(volatile uint64_t*)0x00004006)
#define APU_SQ2_HI (*(volatile uint64_t*)0x00004007)
#define APU_TRI_LINEAR (*(volatile uint64_t*)0x00004008)
#define APU_TRI_LINEAR_CONTROL 7  // Length counter halt/linear counter control
#define APU_TRI_LINEAR_PERIOD 0  // Linear counter load
#define APU_TRI_LO (*(volatile uint64_t*)0x0000400A)
#define APU_TRI_HI (*(volatile uint64_t*)0x0000400B)
#define APU_NOISE_VOL (*(volatile uint64_t*)0x0000400C)
#define APU_NOISE_LO (*(volatile uint64_t*)0x0000400E)
#define APU_NOISE_LO_MODE 7  // Noise mode
#define APU_NOISE_LO_PERIOD 0  // Noise period
#define APU_NOISE_HI (*(volatile uint64_t*)0x0000400F)
#define APU_DMC_FREQ (*(volatile uint64_t*)0x00004010)
#define APU_DMC_FREQ_IRQ 7  // IRQ enable
#define APU_DMC_FREQ_LOOP 6  // Loop flag
#define APU_DMC_FREQ_FREQUENCY 0  // Frequency index
#define APU_DMC_RAW (*(volatile uint64_t*)0x00004011)
#define APU_DMC_START (*(volatile uint64_t*)0x00004012)
#define APU_DMC_LEN (*(volatile uint64_t*)0x00004013)
#define APU_OAMDMA (*(volatile uint64_t*)0x00004014)
#define APU_APUSTATUS (*(volatile uint64_t*)0x00004015)
#define APU_APUSTATUS_DMCINTERRUPT 7  // DMC interrupt flag
#define APU_APUSTATUS_FRAMEINTERRUPT 6  // Frame interrupt flag
#define APU_APUSTATUS_DMCENABLED 4  // DMC enabled
#define APU_APUSTATUS_NOISEENABLED 3  // Noise enabled
#define APU_APUSTATUS_TRIANGLEENABLED 2  // Triangle enabled
#define APU_APUSTATUS_SQUARE2ENABLED 1  // Square 2 enabled
#define APU_APUSTATUS_SQUARE1ENABLED 0  // Square 1 enabled
#define APU_APUFRAME (*(volatile uint64_t*)0x00004017)
#define APU_APUFRAME_MODE 7  // Frame counter mode
#define APU_APUFRAME_IRQINHIBIT 6  // IRQ inhibit

// Controller Interface
#define CONTROLLER_BASE 
#define CONTROLLER_JOY1 (*(volatile uint64_t*)0x00004016)
#define CONTROLLER_JOY1_A 7  // A button
#define CONTROLLER_JOY1_B 6  // B button
#define CONTROLLER_JOY1_SELECT 5  // Select button
#define CONTROLLER_JOY1_START 4  // Start button
#define CONTROLLER_JOY1_UP 3  // Up direction
#define CONTROLLER_JOY1_DOWN 2  // Down direction
#define CONTROLLER_JOY1_LEFT 1  // Left direction
#define CONTROLLER_JOY1_RIGHT 0  // Right direction
#define CONTROLLER_JOY2 (*(volatile uint64_t*)0x00004017)

// Memory Mapper (Cartridge)
#define MAPPER_BASE 
#define MAPPER_PRGROM (*(volatile uint0_t*)0x00000000)
#define MAPPER_CHRROM (*(volatile uint0_t*)0x00000000)
#define MAPPER_PRGRAM (*(volatile uint0_t*)0x00000000)
#define MAPPER_CHRRAM (*(volatile uint0_t*)0x00000000)

// 中断向量定义
#define NMI_VECTOR 65530  // Non-maskable interrupt (VBlank)
#define RESET_VECTOR 65532  // Reset vector
#define IRQ_VECTOR 65534  // Interrupt request

void nintendo_entertainment_system_init(void);

#ifdef __cplusplus
}
#endif

#endif // NINTENDO_ENTERTAINMENT_SYSTEM_HPP
