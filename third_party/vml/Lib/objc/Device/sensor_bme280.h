// BME280 设备定义 - Objective-C 头文件
// 生成自: Bosch/Sensor/BME280
// 版本: 1.0
// 日期: 2026-05-06
// 作者: VML Team
// 描述: BME280 Combined Humidity, Pressure, and Temperature Sensor (I2C/SPI)
// CPU架构: Sensor
// 位宽: 8位
// 时钟频率: 400000 Hz

#ifndef BME280_DEVICE_H
#define BME280_DEVICE_H

#import <Foundation/Foundation.h>

// 外设定义
// BME280 Environmental Sensor (0x76/0x77, 1.71V-3.6V)
#define BME280_BASE 0x76
#define BME280_CHIP_ID_ADDR 0xD0
#define BME280_RESET_ADDR 0xE0
#define BME280_CTRL_HUM_ADDR 0xF2
#define BME280_STATUS_ADDR 0xF3
#define BME280_CTRL_MEAS_ADDR 0xF4
#define BME280_CONFIG_ADDR 0xF5
#define BME280_PRESS_ADDR 0xF7
#define BME280_TEMP_ADDR 0xFA
#define BME280_HUM_ADDR 0xFD

#endif /* BME280_DEVICE_H */
