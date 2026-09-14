// DS18B20 设备定义 - Objective-C 头文件
// 生成自: Maxim/Dallas/Sensor/DS18B20
// 版本: 1.0
// 日期: 2026-05-06
// 作者: VML Team
// 描述: Programmable Resolution 1-Wire Digital Thermometer
// CPU架构: Sensor
// 位宽: 8位
// 时钟频率: 100000 Hz

#ifndef DS18B20_DEVICE_H
#define DS18B20_DEVICE_H

#import <Foundation/Foundation.h>

// 内存段定义
#define SCRATCHPAD_START 0x00
#define SCRATCHPAD_END 0x08
#define SCRATCHPAD_SIZE 9  // Scratchpad memory (9 bytes)
#define EEPROM_START 0x00
#define EEPROM_END 0x02
#define EEPROM_SIZE 3  // EEPROM (TH, TL, config bytes)

// 外设定义
// DS18B20 1-Wire Thermometer (3.0V-5.5V, TO-92)
#define DS18B20_BASE 0x00
#define DS18B20_TEMP_LSB_ADDR 0x00
#define DS18B20_TEMP_MSB_ADDR 0x01
#define DS18B20_TH_REG_ADDR 0x02
#define DS18B20_TL_REG_ADDR 0x03
#define DS18B20_CONFIG_ADDR 0x04
#define DS18B20_CONFIG_R0_BIT 5  // Resolution select bit 0
#define DS18B20_CONFIG_R1_BIT 6  // Resolution select bit 1 (00=9bit,10=10bit,01=11bit,11=12bit)
#define DS18B20_COUNT_REMAIN_ADDR 0x06
#define DS18B20_COUNT_PER_C_ADDR 0x07
#define DS18B20_CRC_ADDR 0x08

#endif /* DS18B20_DEVICE_H */
