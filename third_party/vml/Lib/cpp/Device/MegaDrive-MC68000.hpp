#ifndef MOTOROLA_68000_HPP
#define MOTOROLA_68000_HPP

// Motorola-68000寄存器定义
// 生成自: Motorola/68000/Motorola-68000
// 版本: 1.0
// 日期: 2026-04-16


#ifdef __cplusplus
extern "C" {
#endif

// CPU架构: MC68000
// 位宽: 32位
// 时钟频率: 7670452 Hz

// 寄存器定义
// Data Register 0
#define D0 (*(volatile uint32_t*)0x00)

// Data Register 1
#define D1 (*(volatile uint32_t*)0x04)

// Data Register 2
#define D2 (*(volatile uint32_t*)0x08)

// Data Register 3
#define D3 (*(volatile uint32_t*)0x0C)

// Data Register 4
#define D4 (*(volatile uint32_t*)0x10)

// Data Register 5
#define D5 (*(volatile uint32_t*)0x14)

// Data Register 6
#define D6 (*(volatile uint32_t*)0x18)

// Data Register 7
#define D7 (*(volatile uint32_t*)0x1C)

// Address Register 0
#define A0 (*(volatile uint32_t*)0x20)

// Address Register 1
#define A1 (*(volatile uint32_t*)0x24)

// Address Register 2
#define A2 (*(volatile uint32_t*)0x28)

// Address Register 3
#define A3 (*(volatile uint32_t*)0x2C)

// Address Register 4
#define A4 (*(volatile uint32_t*)0x30)

// Address Register 5
#define A5 (*(volatile uint32_t*)0x34)

// Address Register 6
#define A6 (*(volatile uint32_t*)0x38)

// Stack Pointer (USP)
#define A7 (*(volatile uint32_t*)0x3C)

// Program Counter
#define PC (*(volatile uint32_t*)0x40)

// Status Register
#define SR (*(volatile uint16_t*)0x44)
#define SR_C 0  // Carry
#define SR_V 1  // Overflow
#define SR_Z 2  // Zero
#define SR_N 3  // Negative
#define SR_X 4  // Extend
#define SR_I0 8  // Interrupt Mask 0
#define SR_I1 9  // Interrupt Mask 1
#define SR_I2 10  // Interrupt Mask 2
#define SR_M 11  // Master/Interrupt
#define SR_S 13  // Supervisor/User
#define SR_T0 14  // Trace Mode 0
#define SR_T1 15  // Trace Mode 1

// 内存段定义
// System RAM (4MB)
#define RAM_START 0x000000
#define RAM_END 0x3FFFFF
#define RAM_SIZE 4194304

// Cartridge ROM
#define ROM_START 0x000000
#define ROM_END 0x3FFFFF
#define ROM_SIZE 4194304

// I/O Register Area
#define IO_START 0xA00000
#define IO_END 0xA1FFFF
#define IO_SIZE 131072

// VDP Registers
#define VDP_START 0xC00000
#define VDP_END 0xC0001F
#define VDP_SIZE 32

// Video RAM (256KB)
#define VRAM_START 0xE00000
#define VRAM_END 0xE3FFFF
#define VRAM_SIZE 262144

// 外设定义
// Video Display Processor (TMS9918A variant)
#define VDP_BASE 0xC00000
#define VDP_DATA (*(volatile uint16_t*)0x00C00000)
#define VDP_CTRL (*(volatile uint16_t*)0x00C00004)
#define VDP_HVCOUNT (*(volatile uint16_t*)0x00C00008)
#define VDP_HVB_STATUS (*(volatile uint8_t*)0x00C0000A)

// Programmable Sound Generator (AY-3-8910)
#define PSG_BASE 0xC00011
#define PSG_CH_A_FREQ (*(volatile uint8_t*)0x00C00011)
#define PSG_CH_A_VOL (*(volatile uint8_t*)0x00C00019)
#define PSG_CH_B_FREQ (*(volatile uint8_t*)0x00C00013)
#define PSG_CH_B_VOL (*(volatile uint8_t*)0x00C0001A)
#define PSG_CH_C_FREQ (*(volatile uint8_t*)0x00C00015)
#define PSG_CH_C_VOL (*(volatile uint8_t*)0x00C0001B)
#define PSG_NOISE_FREQ (*(volatile uint8_t*)0x00C00017)
#define PSG_MIXER (*(volatile uint8_t*)0x00C00018)
#define PSG_ENV_FREQ (*(volatile uint8_t*)0x00C0001E)
#define PSG_ENV_SHAPE (*(volatile uint8_t*)0x00C0001C)

// Z80 Secondary CPU (Sound)
#define Z80_BASE 0xA00000
#define Z80_Z80_RESET (*(volatile uint8_t*)0x00A00000)
#define Z80_Z80_BUSREQ (*(volatile uint8_t*)0x00A00004)
#define Z80_Z80_STATUS (*(volatile uint8_t*)0x00A00008)

// Bank Register
#define BANK_REG_BASE 0xA12000
#define BANK_REG_ROM_BANK (*(volatile uint8_t*)0x00A12000)
#define BANK_REG_RAM_BANK (*(volatile uint8_t*)0x00A12004)

// Hardware Version
#define HW_VERSION_BASE 0xA10001
#define HW_VERSION_VERSION (*(volatile uint8_t*)0x00A10001)

// Controller Port 1
#define CONTROLLER1_BASE 0xA10003
#define CONTROLLER1_DATA (*(volatile uint8_t*)0x00A10003)
#define CONTROLLER1_CTRL (*(volatile uint8_t*)0x00A10007)

// Controller Port 2
#define CONTROLLER2_BASE 0xA10005
#define CONTROLLER2_DATA (*(volatile uint8_t*)0x00A10005)
#define CONTROLLER2_CTRL (*(volatile uint8_t*)0x00A10009)

// External Port
#define EXT_PORT_BASE 0xA10007
#define EXT_PORT_DATA (*(volatile uint8_t*)0x00A10007)

// DMA Controller
#define DMA_BASE 0xA10008
#define DMA_SOURCE (*(volatile uint32_t*)0x00A10008)
#define DMA_DEST (*(volatile uint32_t*)0x00A1000C)
#define DMA_COUNT (*(volatile uint16_t*)0x00A10010)
#define DMA_CTRL (*(volatile uint8_t*)0x00A10012)

// Hardware Timer
#define TIMER_BASE 0xA1000E
#define TIMER_H_COUNTER (*(volatile uint8_t*)0x00A1000E)
#define TIMER_V_COUNTER (*(volatile uint8_t*)0x00A10012)

// 中断向量定义
#define RESET_SP_VECTOR 1  // Reset Initial Stack Pointer
#define RESET_PC_VECTOR 2  // Reset Initial PC
#define BUS_ERROR_VECTOR 3  // Bus Error
#define ADDRESS_ERROR_VECTOR 4  // Address Error
#define ILLEGAL_INSTR_VECTOR 5  // Illegal Instruction
#define ZERO_DIVIDE_VECTOR 6  // Zero Divide
#define CHK_EXCEPTION_VECTOR 7  // CHK Exception
#define TRAPV_VECTOR 8  // TRAPV Exception
#define PRIVILEGE_VECTOR 9  // Privilege Violation
#define TRACE_VECTOR 10  // Trace
#define LINE_A_VECTOR 11  // Line 1010 Emulator
#define LINE_F_VECTOR 12  // Line 1111 Emulator
#define IRQ1_VECTOR 24  // External Interrupt 1 (H-Blank)
#define IRQ2_VECTOR 25  // External Interrupt 2 (V-Blank)
#define IRQ3_VECTOR 26  // External Interrupt 3
#define IRQ4_VECTOR 27  // External Interrupt 4 (D-Req)
#define IRQ5_VECTOR 28  // External Interrupt 5
#define IRQ6_VECTOR 29  // External Interrupt 6
#define IRQ7_VECTOR 30  // External Interrupt 7
#define TRAP0_VECTOR 32  // TRAP #0
#define TRAP1_VECTOR 33  // TRAP #1
#define TRAP15_VECTOR 47  // TRAP #15

void motorola_68000_init(void);

#ifdef __cplusplus
}
#endif

#endif // MOTOROLA_68000_HPP
