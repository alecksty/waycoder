// 24C02 设备定义 - Objective-C 头文件
// 生成自: Generic/Memory/24C02
// 版本: 1.0
// 日期: 2026-05-06
// 作者: VML Team
// 描述: 2Kbit I2C Serial EEPROM (256 x 8 bits)
// CPU架构: Memory
// 位宽: 8位
// 时钟频率: 400000 Hz

#ifndef 24C02_DEVICE_H
#define 24C02_DEVICE_H

#import <Foundation/Foundation.h>

// 内存段定义
#define EEPROM_START 0x00
#define EEPROM_END 0xFF
#define EEPROM_SIZE 256  // EEPROM main memory array (256 bytes, 8-byte page write)

// 外设定义
// 24C02 I2C EEPROM (0x50-0x57, 1.8V-5.5V, DIP-8)
#define _24C02_BASE 0x50
#define _24C02_STATUS_ADDR 0xFF
#define _24C02_STATUS_BUSY_BIT 0  // 1=Write in progress
#define _24C02_PAGE_SIZE_ADDR 0xFE
#define _24C02_SIZE_ADDR 0xFD

#endif /* 24C02_DEVICE_H */
