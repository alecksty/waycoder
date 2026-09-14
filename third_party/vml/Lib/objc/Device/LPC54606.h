// LPC54606 设备定义 - Objective-C 头文件
// 生成自: NXP/LPC/LPC54606
// 版本: 1.0
// 日期: 2026-04-28
// 作者: VML Team
// 描述: 32-bit ARM Cortex-M4 MCU with 256KB Flash, 136KB SRAM, 180MHz
// CPU架构: ARM-Cortex-M4
// 位宽: 32位
// 时钟频率: 180000000 Hz

#ifndef LPC54606_DEVICE_H
#define LPC54606_DEVICE_H

#import <Foundation/Foundation.h>

// 寄存器地址定义
#define R0_ADDR 0x00  // 
#define R1_ADDR 0x04  // 
#define R2_ADDR 0x08  // 
#define R3_ADDR 0x0C  // 
#define R4_ADDR 0x10  // 
#define R5_ADDR 0x14  // 
#define SP_ADDR 0x34  // 
#define LR_ADDR 0x38  // 
#define PC_ADDR 0x3C  // 

// 内存段定义
#define FLASH_START 0x00000000
#define FLASH_END 0x0003FFFF
#define FLASH_SIZE 262144  // 
#define SRAM_START 0x20000000
#define SRAM_END 0x20021FFF
#define SRAM_SIZE 139264  // 
#define PERIPHERAL_START 0x40000000
#define PERIPHERAL_END 0x401FFFFF
#define PERIPHERAL_SIZE 2097152  // 

// 外设定义
// System Control
#define SYSCON_BASE 0x40000000
#define SYSCON_SYSAHBCLKCTRL_ADDR 0x80
#define SYSCON_MAINCLKSEL_ADDR 0x04
#define SYSCON_MAINCLKUEN_ADDR 0x08
#define SYSCON_SYSPLLCTRL_ADDR 0x0C
// General Purpose I/O
#define GPIO_BASE 0x400F4000
#define GPIO_DIR0_ADDR 0x0000
#define GPIO_PIN0_ADDR 0x1000
#define GPIO_SET0_ADDR 0x2000
#define GPIO_CLR0_ADDR 0x3000
#define GPIO_NOT0_ADDR 0x4000
#define GPIO_DIR1_ADDR 0x0004
#define GPIO_PIN1_ADDR 0x1004
#define GPIO_SET1_ADDR 0x2004
#define GPIO_CLR1_ADDR 0x3004
#define GPIO_NOT1_ADDR 0x4004
// USART0
#define USART0_BASE 0x40086000
#define USART0_CFG_ADDR 0x00
#define USART0_CTRL_ADDR 0x04
#define USART0_STAT_ADDR 0x08
#define USART0_TXDAT_ADDR 0x10
#define USART0_RXDAT_ADDR 0x14
#define USART0_BRG_ADDR 0x20

// 中断向量定义
#define INT_RESET 0  // 
#define INT_SVCALL 11  // 
#define INT_USART0 24  // USART0 Interrupt

#endif /* LPC54606_DEVICE_H */
