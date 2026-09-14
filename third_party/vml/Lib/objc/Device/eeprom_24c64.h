// 24C64 设备定义 - Objective-C 头文件
// 生成自: Generic/Memory/24C64
// 版本: 1.0
// 日期: 2026-05-06
// 作者: VML Team
// 描述: 24C64 64Kbit I2C Serial EEPROM (8K×8, 32-byte page write)
// CPU架构: Memory
// 位宽: 8位
// 时钟频率: 400000 Hz

#ifndef 24C64_DEVICE_H
#define 24C64_DEVICE_H

#import <Foundation/Foundation.h>

// 内存段定义
#define EEPROM_START 0x00
#define EEPROM_END 0x1FFF
#define EEPROM_SIZE 8192  // EEPROM main memory array (8KB, 32-byte page write)

// 外设定义
// 24C64 I2C EEPROM (0x50-0x57, 1.7V-5.5V)
#define _24C64_BASE 0x50
#define _24C64_ADDR_H_ADDR 0x00
#define _24C64_ADDR_L_ADDR 0x01
#define _24C64_DATA_ADDR 0x02
#define _24C64_PAGE_SIZE_ADDR 0xFE
#define _24C64_SIZE_ADDR 0xFD

#endif /* 24C64_DEVICE_H */
