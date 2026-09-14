// BMP280 设备定义 - Objective-C 头文件
// 生成自: Bosch/Sensor/BMP280
// 版本: 1.0
// 日期: 2026-05-06
// 作者: VML Team
// 描述: Digital Barometric Pressure and Temperature Sensor (I2C/SPI)
// CPU架构: Sensor
// 位宽: 8位
// 时钟频率: 3400000 Hz

#ifndef BMP280_DEVICE_H
#define BMP280_DEVICE_H

#import <Foundation/Foundation.h>

// 内存段定义
#define PACKAGE_START 0x00
#define PACKAGE_END 0x00
#define PACKAGE_SIZE 8  // LGA-8 (2.0x2.5x0.95mm)

// 外设定义
// BMP280 I2C Sensor (0x76/0x77, 1.71V-3.6V)
#define BMP280_BASE 0x76
#define BMP280_TEMP_XLSB_ADDR 0xFC
#define BMP280_TEMP_LSB_ADDR 0xFB
#define BMP280_TEMP_MSB_ADDR 0xFA
#define BMP280_PRESS_XLSB_ADDR 0xF9
#define BMP280_PRESS_LSB_ADDR 0xF8
#define BMP280_PRESS_MSB_ADDR 0xF7
#define BMP280_CONFIG_ADDR 0xF5
#define BMP280_CONFIG_T_SB_BIT 5  // Standby time in normal mode
#define BMP280_CONFIG_FILTER_BIT 2  // Filter coefficient
#define BMP280_CONFIG_SPI3W_EN_BIT 0  // Enable 3-wire SPI
#define BMP280_CTRL_MEAS_ADDR 0xF4
#define BMP280_CTRL_MEAS_MODE_BIT 0  // 0=sleep, 1/2=forced, 3=normal
#define BMP280_CTRL_MEAS_OSRS_P_BIT 2  // Pressure oversampling
#define BMP280_CTRL_MEAS_OSRS_T_BIT 5  // Temperature oversampling
#define BMP280_STATUS_ADDR 0xF3
#define BMP280_STATUS_IM_UPDATE_BIT 0  // 1=Image register update in progress
#define BMP280_STATUS_MEASURING_BIT 3  // 1=Conversion is running
#define BMP280_CHIP_ID_ADDR 0xD0
#define BMP280_RESET_ADDR 0xE0

#endif /* BMP280_DEVICE_H */
