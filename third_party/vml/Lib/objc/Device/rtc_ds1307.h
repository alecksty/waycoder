// DS1307 设备定义 - Objective-C 头文件
// 生成自: Maxim/Dallas/RTC/DS1307
// 版本: 1.0
// 日期: 2026-05-06
// 作者: VML Team
// 描述: DS1307 I2C Real-Time Clock (56-byte NVRAM, battery backup)
// CPU架构: RTC
// 位宽: 8位
// 时钟频率: 100000 Hz

#ifndef DS1307_DEVICE_H
#define DS1307_DEVICE_H

#import <Foundation/Foundation.h>

// 内存段定义
#define NVRAM_START 0x08
#define NVRAM_END 0x3F
#define NVRAM_SIZE 56  // Non-volatile RAM (56 bytes)

// 外设定义
// DS1307 RTC (0x68, 5V, DIP-8)
#define DS1307_BASE 0x68
#define DS1307_SEC_ADDR 0x00
#define DS1307_MIN_ADDR 0x01
#define DS1307_HOUR_ADDR 0x02
#define DS1307_DAY_ADDR 0x03
#define DS1307_DATE_ADDR 0x04
#define DS1307_MONTH_ADDR 0x05
#define DS1307_YEAR_ADDR 0x06
#define DS1307_CTRL_ADDR 0x07

#endif /* DS1307_DEVICE_H */
