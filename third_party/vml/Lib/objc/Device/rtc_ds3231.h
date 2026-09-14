// DS3231 设备定义 - Objective-C 头文件
// 生成自: Maxim/Dallas/RTC/DS3231
// 版本: 1.0
// 日期: 2026-05-06
// 作者: VML Team
// 描述: DS3231 I2C High-Precision RTC (±2ppm, temperature compensated, 32K EEPROM)
// CPU架构: RTC
// 位宽: 8位
// 时钟频率: 400000 Hz

#ifndef DS3231_DEVICE_H
#define DS3231_DEVICE_H

#import <Foundation/Foundation.h>

// 内存段定义
#define EEPROM_START 0x14
#define EEPROM_END 0xFF
#define EEPROM_SIZE 236  // AT24C32 EEPROM (32Kbit)

// 外设定义
// DS3231 Precision RTC (0x68, 3.3V-5.5V)
#define DS3231_BASE 0x68
#define DS3231_SEC_ADDR 0x00
#define DS3231_MIN_ADDR 0x01
#define DS3231_HOUR_ADDR 0x02
#define DS3231_DAY_ADDR 0x03
#define DS3231_DATE_ADDR 0x04
#define DS3231_MONTH_CENT_ADDR 0x05
#define DS3231_YEAR_ADDR 0x06
#define DS3231_ALARM1_SEC_ADDR 0x07
#define DS3231_ALARM1_MIN_ADDR 0x08
#define DS3231_ALARM1_HOUR_ADDR 0x09
#define DS3231_ALARM2_MIN_ADDR 0x0B
#define DS3231_ALARM2_HOUR_ADDR 0x0C
#define DS3231_CTRL_ADDR 0x0E
#define DS3231_CTRL_STATUS_ADDR 0x0F
#define DS3231_TEMP_MSB_ADDR 0x11
#define DS3231_TEMP_LSB_ADDR 0x12

#endif /* DS3231_DEVICE_H */
