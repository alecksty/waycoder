#ifndef PIC32MX170F256B_HPP
#define PIC32MX170F256B_HPP

// PIC32MX170F256B寄存器定义
// 生成自: Microchip/PIC32/PIC32MX170F256B
// 版本: 1.0
// 日期: 2026-04-28


#ifdef __cplusplus
extern "C" {
#endif

// CPU架构: MIPS32-M4K
// 位宽: 32位
// 时钟频率: 50000000 Hz

// 寄存器定义
// Hard-wired zero
#define _0 (*(volatile uint32_t*)0x00)

// AT
#define _1 (*(volatile uint32_t*)0x04)

// V0
#define _2 (*(volatile uint32_t*)0x08)

// V1
#define _3 (*(volatile uint32_t*)0x0C)

// A0
#define _4 (*(volatile uint32_t*)0x10)

// A1
#define _5 (*(volatile uint32_t*)0x14)

// Stack Pointer (SP)
#define _29 (*(volatile uint32_t*)0x74)

// Return Address (RA)
#define _31 (*(volatile uint32_t*)0x7C)

// Program Counter
#define PC (*(volatile uint32_t*)0x80)

// 内存段定义
// Program Flash
#define FLASH_START 0x9D000000
#define FLASH_END 0x9D03FFFF
#define FLASH_SIZE 262144

#define SRAM_START 0xA0000000
#define SRAM_END 0xA000FFFF
#define SRAM_SIZE 65536

#define PERIPHERAL_START 0xBF800000
#define PERIPHERAL_END 0xBF8FFFFF
#define PERIPHERAL_SIZE 1048576

// Boot Flash
#define BOOTFLASH_START 0xBFC00000
#define BOOTFLASH_END 0xBFC02FFF
#define BOOTFLASH_SIZE 12288

// 外设定义
// General Purpose I/O Port A
#define PORTA_BASE 0xBF886000
#define PORTA_TRISA (*(volatile uint32_t*)0xBF886000)
#define PORTA_PORTA (*(volatile uint32_t*)0xBF886010)
#define PORTA_LATA (*(volatile uint32_t*)0xBF886020)
#define PORTA_ODCA (*(volatile uint32_t*)0xBF886030)

// General Purpose I/O Port B
#define PORTB_BASE 0xBF886100
#define PORTB_TRISB (*(volatile uint32_t*)0xBF886100)
#define PORTB_PORTB (*(volatile uint32_t*)0xBF886110)
#define PORTB_LATB (*(volatile uint32_t*)0xBF886120)
#define PORTB_ODCB (*(volatile uint32_t*)0xBF886130)

// UART1
#define UART1_BASE 0xBF822000
#define UART1_UXMODE (*(volatile uint32_t*)0xBF822000)
#define UART1_UXSTA (*(volatile uint32_t*)0xBF822004)
#define UART1_UXTXREG (*(volatile uint32_t*)0xBF822008)
#define UART1_UXRXREG (*(volatile uint32_t*)0xBF82200C)
#define UART1_UXBRG (*(volatile uint32_t*)0xBF822010)

// 中断向量定义
#define RESET_VECTOR 0  // 
#define UART1_VECTOR 8  // UART1 Interrupt

void pic32mx170f256b_init(void);

#ifdef __cplusplus
}
#endif

#endif // PIC32MX170F256B_HPP
