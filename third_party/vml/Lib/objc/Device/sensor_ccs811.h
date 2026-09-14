// CCS811 设备定义 - Objective-C 头文件
// 生成自: AMS/ScioSense/Sensor/CCS811
// 版本: 1.0
// 日期: 2026-05-06
// 作者: VML Team
// 描述: CCS811 VOC/eCO2 Air Quality Sensor (I2C, 400-8192ppm CO2, 0-1187ppb TVOC)
// CPU架构: Sensor
// 位宽: 16位
// 时钟频率: 400000 Hz

#ifndef CCS811_DEVICE_H
#define CCS811_DEVICE_H

#import <Foundation/Foundation.h>

// 外设定义
// CCS811 Air Quality Sensor (0x5A/0x5B, 1.8V-3.6V)
#define CCS811_BASE 0x5A
#define CCS811_STATUS_ADDR 0x00
#define CCS811_MEAS_MODE_ADDR 0x01
#define CCS811_ALG_RESULT_ADDR 0x02
#define CCS811_ECO2_ADDR 0x02
#define CCS811_TVOC_ADDR 0x04
#define CCS811_RAW_DATA_ADDR 0x06
#define CCS811_BASELINE_ADDR 0x0B
#define CCS811_HW_ID_ADDR 0x20
#define CCS811_ERROR_ID_ADDR 0xE0
#define CCS811_APP_START_ADDR 0xF4
#define CCS811_SW_RESET_ADDR 0xFF

// 中断向量定义
#define INT_INT 0  // Data ready / interrupt pin

// 引脚定义
#define PIN_WAKE 1  // Wake pin (active low)

#endif /* CCS811_DEVICE_H */
