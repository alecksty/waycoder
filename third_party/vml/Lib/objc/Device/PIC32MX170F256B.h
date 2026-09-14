// PIC32MX170F256B 设备定义 - Objective-C 头文件
// 生成自: Microchip/PIC32/PIC32MX170F256B
// 版本: 1.0
// 日期: 2026-04-28
// 作者: VML Team
// 描述: 32-bit MIPS32 M4K MCU with 256KB Flash, 64KB RAM, 50MHz
// CPU架构: MIPS32-M4K
// 位宽: 32位
// 时钟频率: 50000000 Hz

#ifndef PIC32MX170F256B_DEVICE_H
#define PIC32MX170F256B_DEVICE_H

#import <Foundation/Foundation.h>

// 寄存器地址定义
#define _0_ADDR 0x00  // Hard-wired zero
#define _1_ADDR 0x04  // AT
#define _2_ADDR 0x08  // V0
#define _3_ADDR 0x0C  // V1
#define _4_ADDR 0x10  // A0
#define _5_ADDR 0x14  // A1
#define _29_ADDR 0x74  // Stack Pointer (SP)
#define _31_ADDR 0x7C  // Return Address (RA)
#define PC_ADDR 0x80  // Program Counter

// 内存段定义
#define FLASH_START 0x9D000000
#define FLASH_END 0x9D03FFFF
#define FLASH_SIZE 262144  // Program Flash
#define SRAM_START 0xA0000000
#define SRAM_END 0xA000FFFF
#define SRAM_SIZE 65536  // 
#define PERIPHERAL_START 0xBF800000
#define PERIPHERAL_END 0xBF8FFFFF
#define PERIPHERAL_SIZE 1048576  // 
#define BOOTFLASH_START 0xBFC00000
#define BOOTFLASH_END 0xBFC02FFF
#define BOOTFLASH_SIZE 12288  // Boot Flash

// 外设定义
// General Purpose I/O Port A
#define PORTA_BASE 0xBF886000
#define PORTA_TRISA_ADDR 0x00
#define PORTA_PORTA_ADDR 0x10
#define PORTA_LATA_ADDR 0x20
#define PORTA_ODCA_ADDR 0x30
// General Purpose I/O Port B
#define PORTB_BASE 0xBF886100
#define PORTB_TRISB_ADDR 0x00
#define PORTB_PORTB_ADDR 0x10
#define PORTB_LATB_ADDR 0x20
#define PORTB_ODCB_ADDR 0x30
// UART1
#define UART1_BASE 0xBF822000
#define UART1_UXMODE_ADDR 0x00
#define UART1_UXSTA_ADDR 0x04
#define UART1_UXTXREG_ADDR 0x08
#define UART1_UXRXREG_ADDR 0x0C
#define UART1_UXBRG_ADDR 0x10

// 中断向量定义
#define INT_RESET 0  // 
#define INT_UART1 8  // UART1 Interrupt

#endif /* PIC32MX170F256B_DEVICE_H */
