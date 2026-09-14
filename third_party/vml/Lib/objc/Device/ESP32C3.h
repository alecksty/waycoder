// ESP32-C3 设备定义 - Objective-C 头文件
// 生成自: Espressif/ESP32-C/ESP32-C3
// 版本: 1.0
// 日期: 2026-04-28
// 作者: VML Team
// 描述: 32-bit RISC-V single-core WiFi + BLE SoC, 160MHz, 400KB SRAM
// CPU架构: RISC-V
// 位宽: 32位
// 时钟频率: 160000000 Hz

#ifndef ESP32-C3_DEVICE_H
#define ESP32-C3_DEVICE_H

#import <Foundation/Foundation.h>

// 寄存器地址定义
#define X1_ADDR 0x04  // Return Address
#define X2_ADDR 0x08  // Stack Pointer (SP)
#define X3_ADDR 0x0C  // Global Pointer (GP)
#define X8_ADDR 0x20  // Frame Pointer (FP)
#define X10_ADDR 0x28  // Function Argument (A0)
#define X11_ADDR 0x2C  // Function Argument (A1)
#define PC_ADDR 0x3C  // Program Counter

// 内存段定义
#define FLASH_START 0x42000000
#define FLASH_END 0x427FFFFF
#define FLASH_SIZE 8388608  // Flash via Cache
#define SRAM_START 0x3FC80000
#define SRAM_END 0x3FCE3FFF
#define SRAM_SIZE 409600  // Internal SRAM
#define PERIPHERAL_START 0x60000000
#define PERIPHERAL_END 0x600FFFFF
#define PERIPHERAL_SIZE 1048576  // 

// 外设定义
// General Purpose I/O
#define GPIO_BASE 0x60004000
#define GPIO_OUT_ADDR 0x04
#define GPIO_OUT_W1TS_ADDR 0x08
#define GPIO_OUT_W1TC_ADDR 0x0C
#define GPIO_IN_ADDR 0x10
#define GPIO_ENABLE_ADDR 0x20
#define GPIO_ENABLE_W1TS_ADDR 0x24
#define GPIO_ENABLE_W1TC_ADDR 0x28
// I/O MUX
#define IO_MUX_BASE 0x60009000
#define IO_MUX_GPIO0_ADDR 0x00
#define IO_MUX_GPIO1_ADDR 0x04
#define IO_MUX_GPIO2_ADDR 0x08
#define IO_MUX_GPIO3_ADDR 0x0C
// RTC Control
#define RTC_CNTL_BASE 0x60008000
#define RTC_CNTL_OPTIONS0_ADDR 0x00
#define RTC_CNTL_CLK_CONF_ADDR 0x30

// 中断向量定义
#define INT_RESET 1  // 
#define INT_MACHINESOFTWARE 3  // 
#define INT_MACHINETIMER 7  // 
#define INT_MACHINEEXTERNAL 11  // 

#endif /* ESP32-C3_DEVICE_H */
