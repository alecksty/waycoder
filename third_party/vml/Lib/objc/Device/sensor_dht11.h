// DHT11 设备定义 - Objective-C 头文件
// 生成自: Aosong/Sensor/DHT11
// 版本: 1.0
// 日期: 2026-05-06
// 作者: VML Team
// 描述: Digital Temperature and Humidity Sensor (1-Wire)
// CPU架构: Sensor
// 位宽: 8位
// 时钟频率: 500000 Hz

#ifndef DHT11_DEVICE_H
#define DHT11_DEVICE_H

#import <Foundation/Foundation.h>

// 内存段定义
#define PACKAGE_START 0x00
#define PACKAGE_END 0x00
#define PACKAGE_SIZE 4  // DIP-4/SMD-4

// 外设定义
// DHT11 1-Wire Sensor (3.0V-5.5V)
#define DHT11_BASE 0x00
#define DHT11_HUMIDITY_INT_ADDR 0x00
#define DHT11_HUMIDITY_DEC_ADDR 0x01
#define DHT11_TEMP_INT_ADDR 0x02
#define DHT11_TEMP_DEC_ADDR 0x03
#define DHT11_CHECKSUM_ADDR 0x04

#endif /* DHT11_DEVICE_H */
