#ifndef MSX1_HPP
#define MSX1_HPP

// MSX1寄存器定义
// 生成自: Various (ASCII/Awanaga/MSX Association)/MSX/MSX1
// 版本: 1.0
// 日期: 2026-04-17


#ifdef __cplusplus
extern "C" {
#endif

// CPU架构: Z80A
// 位宽: 8位
// 时钟频率: 3579545 Hz

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

// Index Y (usually = 0xF38F)
#define IY (*(volatile uint16_t*)0x14)

// Stack Pointer
#define SP (*(volatile uint16_t*)0x16)

// Program Counter
#define PC (*(volatile uint16_t*)0x18)

// 内存段定义
// Cartridge/SUB-ROM / Main-ROM
#define SLOT0_ROM_START 0x0000
#define SLOT0_ROM_END 0x7FFF
#define SLOT0_ROM_SIZE 32768

// MSX-BIOS ROM
#define SYSROM_START 0x0000
#define SYSROM_END 0x3FFF
#define SYSROM_SIZE 16384

// Extension ROM (cartridge)
#define EXTROM_START 0x4000
#define EXTROM_END 0x7FFF
#define EXTROM_SIZE 16384

// Main RAM (32KB working area)
#define MAIN_RAM_START 0x4000
#define MAIN_RAM_END 0xC000
#define MAIN_RAM_SIZE 32768

// Work RAM (16KB)
#define WORK_RAM_START 0xC000
#define WORK_RAM_END 0xFFFF
#define WORK_RAM_SIZE 16384

// System variables area
#define SYSVAR_START 0xF000
#define SYSVAR_END 0xFCA0
#define SYSVAR_SIZE 3232

// Slot-mapped memory
#define SLOTS_START 0x8000
#define SLOTS_END 0xFFFF
#define SLOTS_SIZE 32768

// 外设定义
// TMS9918A Video Display Processor
#define VDP_BASE 0x98
#define VDP_VDP_REG0 (*(volatile uint8_t*)0x00000131)
#define VDP_VDP_REG1 (*(volatile uint8_t*)0x00000131)
#define VDP_VDP_REG2 (*(volatile uint8_t*)0x00000131)
#define VDP_VDP_REG3 (*(volatile uint8_t*)0x00000131)
#define VDP_VDP_REG4 (*(volatile uint8_t*)0x00000131)
#define VDP_VDP_REG5 (*(volatile uint8_t*)0x00000131)
#define VDP_VDP_REG6 (*(volatile uint8_t*)0x00000131)
#define VDP_VDP_REG7 (*(volatile uint8_t*)0x00000131)
#define VDP_VDP_STATUS (*(volatile uint8_t*)0x00000131)
#define VDP_VDP_DATA (*(volatile uint8_t*)0x00000130)
#define VDP_VDP_POT (*(volatile uint8_t*)0x00000130)

// AY-3-8910 Programmable Sound Generator
#define PSG_BASE 0xA0
#define PSG_PSG_REG (*(volatile uint8_t*)0x00000141)
#define PSG_PSG_DATA (*(volatile uint8_t*)0x00000143)
#define PSG_FREQ_A_LO (*(volatile uint8_t*)0x00000140)
#define PSG_FREQ_A_HI (*(volatile uint8_t*)0x00000141)
#define PSG_FREQ_B_LO (*(volatile uint8_t*)0x00000142)
#define PSG_FREQ_B_HI (*(volatile uint8_t*)0x00000143)
#define PSG_FREQ_C_LO (*(volatile uint8_t*)0x00000144)
#define PSG_FREQ_C_HI (*(volatile uint8_t*)0x00000145)
#define PSG_NOISE_FREQ (*(volatile uint8_t*)0x00000146)
#define PSG_ENABLE (*(volatile uint8_t*)0x00000147)
#define PSG_VOL_A (*(volatile uint8_t*)0x00000148)
#define PSG_VOL_B (*(volatile uint8_t*)0x00000149)
#define PSG_VOL_C (*(volatile uint8_t*)0x0000014A)
#define PSG_ENV_FREQ_LO (*(volatile uint8_t*)0x0000014B)
#define PSG_ENV_FREQ_HI (*(volatile uint8_t*)0x0000014C)
#define PSG_ENV_SHAPE (*(volatile uint8_t*)0x0000014D)
#define PSG_PORT_A (*(volatile uint8_t*)0x0000014E)
#define PSG_PORT_B (*(volatile uint8_t*)0x0000014F)

// PPI 8255 Programmable Peripheral Interface
#define PPI_BASE 0xA8
#define PPI_PPI_PA (*(volatile uint8_t*)0x00000150)
#define PPI_PPI_PB (*(volatile uint8_t*)0x00000151)
#define PPI_PPI_PC (*(volatile uint8_t*)0x00000152)
#define PPI_PPI_CTRL (*(volatile uint8_t*)0x00000153)

// MSX Slot Expansion System
#define SLOTEXP_BASE 0x0000
#define SLOTEXP_SLOT0 (*(volatile uint8_t*)0x0000FCC0)
#define SLOTEXP_SLOT1 (*(volatile uint8_t*)0x0000FCC1)
#define SLOTEXP_SLOT2 (*(volatile uint8_t*)0x0000FCC2)
#define SLOTEXP_SLOT3 (*(volatile uint8_t*)0x0000FCC3)
#define SLOTEXP_EXPTBL0 (*(volatile uint8_t*)0x0000FCC4)
#define SLOTEXP_EXPTBL1 (*(volatile uint8_t*)0x0000FCC5)
#define SLOTEXP_EXPTBL2 (*(volatile uint8_t*)0x0000FCC6)
#define SLOTEXP_EXPTBL3 (*(volatile uint8_t*)0x0000FCC7)

// 中断向量定义
#define RESET_VECTOR 0  // Power-on / Reset
#define NMI_VECTOR 1  // Non-Maskable Interrupt
#define INT_VECTOR 2  // VDP Vertical Interrupt (frame)

// 引脚定义
#define PIN_VCC 1  // +5V Power
#define PIN_GND 2  // Ground
#define PIN_CLK 3  // Z80 Clock (3.58MHz)
#define PIN_A0_A15 4  // Address Bus
#define PIN_D0_D7 5  // Data Bus
#define PIN_MREQ 6  // Memory Request
#define PIN_IORQ 7  // I/O Request
#define PIN_RD 8  // Read
#define PIN_WR 9  // Write
#define PIN_INT 10  // Interrupt Request
#define PIN_NMI 11  // Non-Maskable Interrupt
#define PIN_RESET 12  // Reset
#define PIN_SLTSL 13  // Slot select (for memory mapping)
#define PIN_WAIT 14  // Wait (for slow I/O)

void msx1_init(void);

#ifdef __cplusplus
}
#endif

#endif // MSX1_HPP
