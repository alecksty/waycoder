#ifndef APPLE_II_HPP
#define APPLE_II_HPP

// Apple-II寄存器定义
// 生成自: Apple Computer/Apple II/Apple-II
// 版本: 1.0
// 日期: 2026-04-17


#ifdef __cplusplus
extern "C" {
#endif

// CPU架构: MOS 6502
// 位宽: 8位
// 时钟频率: 1023000 Hz

// 寄存器定义
// Accumulator
#define A (*(volatile uint8_t*)0)

// Index Register X
#define X (*(volatile uint8_t*)0)

// Index Register Y
#define Y (*(volatile uint8_t*)0)

// Stack Pointer
#define SP (*(volatile uint8_t*)0)

// Program Counter
#define PC (*(volatile uint16_t*)0)

// Status Register
#define P (*(volatile uint8_t*)0)

// 外设定义
// Apple II keyboard
#define KEYBOARD_BASE 
#define KEYBOARD_KBD (*(volatile uint8_t*)0x0000C000)
#define KEYBOARD_KBDSTRB (*(volatile uint8_t*)0x0000C010)

// Built-in speaker
#define SPEAKER_BASE 
#define SPEAKER_SPKR (*(volatile uint8_t*)0x0000C030)

// Cassette tape interface
#define CASSETTE_BASE 
#define CASSETTE_TAPEIN (*(volatile uint8_t*)0x0000C060)
#define CASSETTE_TAPEOUT (*(volatile uint8_t*)0x0000C020)

// Game controller port
#define GAMEPORT_BASE 
#define GAMEPORT_PADDLE0 (*(volatile uint8_t*)0x0000C064)
#define GAMEPORT_PADDLE1 (*(volatile uint8_t*)0x0000C065)
#define GAMEPORT_PADDLE2 (*(volatile uint8_t*)0x0000C066)
#define GAMEPORT_PADDLE3 (*(volatile uint8_t*)0x0000C067)
#define GAMEPORT_BUTTON0 (*(volatile uint8_t*)0x0000C061)
#define GAMEPORT_BUTTON1 (*(volatile uint8_t*)0x0000C062)

// Disk II controller
#define DISKCONTROLLER_BASE 
#define DISKCONTROLLER_DISKUNIT (*(volatile uint8_t*)0x0000C0E0)
#define DISKCONTROLLER_DISKCMD (*(volatile uint8_t*)0x0000C0E8)
#define DISKCONTROLLER_DISKSTAT (*(volatile uint8_t*)0x0000C0E9)
#define DISKCONTROLLER_DISKDATA (*(volatile uint8_t*)0x0000C0EA)

// 中断向量定义
#define NMI_VECTOR 65526  // Non-maskable interrupt
#define RESET_VECTOR 65528  // Reset vector
#define IRQ_VECTOR 65530  // Interrupt request
#define BRK_VECTOR 65532  // Break instruction

void apple_ii_init(void);

#ifdef __cplusplus
}
#endif

#endif // APPLE_II_HPP
