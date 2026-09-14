// RP2350 设备定义 - Objective-C 头文件
// 生成自: Raspberry/RP2/RP2350
// 版本: 1.0
// 日期: 2026-04-28
// 作者: VML Team
// 描述: Dual Cortex-M33 + RISC-V Hazard3 MCU with 520KB SRAM, 150MHz
// CPU架构: ARM-Cortex-M33
// 位宽: 32位
// 时钟频率: 150000000 Hz

#ifndef RP2350_DEVICE_H
#define RP2350_DEVICE_H

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
#define FLASH_START 0x10000000
#define FLASH_END 0x107FFFFF
#define FLASH_SIZE 8388608  // XIP Flash
#define SRAM_START 0x20000000
#define SRAM_END 0x20081FFF
#define SRAM_SIZE 532480  // Total SRAM
#define PERIPHERAL_START 0x40000000
#define PERIPHERAL_END 0x5000FFFF
#define PERIPHERAL_SIZE 16777216  // 

// 外设定义
// Single-Cycle I/O (GPIO)
#define SIO_BASE 0xD0000000
#define SIO_GPIO_IN_ADDR 0x004
#define SIO_GPIO_OUT_ADDR 0x010
#define SIO_GPIO_OUT_SET_ADDR 0x014
#define SIO_GPIO_OUT_CLR_ADDR 0x018
#define SIO_GPIO_OUT_XOR_ADDR 0x01C
#define SIO_GPIO_OE_ADDR 0x020
#define SIO_GPIO_OE_SET_ADDR 0x024
#define SIO_GPIO_OE_CLR_ADDR 0x028
// IO Bank 0 (GPIO control)
#define IO_BANK0_BASE 0x40028000
#define IO_BANK0_GPIO0_STATUS_ADDR 0x000
#define IO_BANK0_GPIO0_CTRL_ADDR 0x004
#define IO_BANK0_GPIO1_STATUS_ADDR 0x008
#define IO_BANK0_GPIO1_CTRL_ADDR 0x00C
// Pad controls for GPIO 0-29
#define PADS_BANK0_BASE 0x4002C000
#define PADS_BANK0_GPIO0_ADDR 0x000
#define PADS_BANK0_GPIO1_ADDR 0x004
// Reset Controller
#define RESETS_BASE 0x4000C000
#define RESETS_RESET_ADDR 0x000
#define RESETS_RESET_DONE_ADDR 0x008

// 中断向量定义
#define INT_RESET 0  // 
#define INT_SVCALL 11  // 

#endif /* RP2350_DEVICE_H */
