// nRF52832 设备定义 - Objective-C 头文件
// 生成自: Nordic/nRF52/nRF52832
// 版本: 1.0
// 日期: 2026-04-28
// 作者: VML Team
// 描述: 32-bit ARM Cortex-M4F BLE SoC with 512KB Flash, 64KB RAM, 64MHz
// CPU架构: ARM-Cortex-M4F
// 位宽: 32位
// 时钟频率: 64000000 Hz

#ifndef NRF52832_DEVICE_H
#define NRF52832_DEVICE_H

#import <Foundation/Foundation.h>

// 寄存器地址定义
#define R0_ADDR 0x00  // 
#define R1_ADDR 0x04  // 
#define R2_ADDR 0x08  // 
#define R3_ADDR 0x0C  // 
#define SP_ADDR 0x34  // 
#define LR_ADDR 0x38  // 
#define PC_ADDR 0x3C  // 

// 内存段定义
#define FLASH_START 0x00000000
#define FLASH_END 0x0007FFFF
#define FLASH_SIZE 524288  // 
#define SRAM_START 0x20000000
#define SRAM_END 0x2000FFFF
#define SRAM_SIZE 65536  // 
#define PERIPHERAL_START 0x40000000
#define PERIPHERAL_END 0x400FFFFF
#define PERIPHERAL_SIZE 1048576  // 
#define FICR_START 0x10000000
#define FICR_END 0x10000FFF
#define FICR_SIZE 4096  // Factory Information Configuration Registers

// 外设定义
// General Purpose I/O Port 0
#define GPIO_P0_BASE 0x50000000
#define GPIO_P0_OUT_ADDR 0x504
#define GPIO_P0_OUTSET_ADDR 0x508
#define GPIO_P0_OUTCLR_ADDR 0x50C
#define GPIO_P0_IN_ADDR 0x510
#define GPIO_P0_DIR_ADDR 0x514
#define GPIO_P0_DIRSET_ADDR 0x518
#define GPIO_P0_DIRCLR_ADDR 0x51C
// Power Control
#define POWER_BASE 0x40000000
#define POWER_DCDCEN_ADDR 0x1C4
#define POWER_RAMSTATUS_ADDR 0x268
// Clock Control
#define CLOCK_BASE 0x40000000
#define CLOCK_HFCLKSTART_ADDR 0x108
#define CLOCK_HFCLKSTARTED_ADDR 0x208

// 中断向量定义
#define INT_RESET 0  // 
#define INT_SVCALL 11  // 

#endif /* NRF52832_DEVICE_H */
