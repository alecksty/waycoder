// VL53L0X 设备定义 - Objective-C 头文件
// 生成自: STMicroelectronics/Sensor/VL53L0X
// 版本: 1.0
// 日期: 2026-05-06
// 作者: VML Team
// 描述: VL53L0X ToF Laser Distance Sensor (I2C, 2cm-200cm, 940nm VCSEL)
// CPU架构: Sensor
// 位宽: 16位
// 时钟频率: 400000 Hz

#ifndef VL53L0X_DEVICE_H
#define VL53L0X_DEVICE_H

#import <Foundation/Foundation.h>

// 外设定义
// VL53L0X ToF Distance Sensor (0x29, 2.6V-3.5V)
#define VL53L0X_BASE 0x29
#define VL53L0X_DISTANCE_ADDR 0x00
#define VL53L0X_SIGNAL_RATE_ADDR 0x02
#define VL53L0X_AMBIENT_RATE_ADDR 0x04
#define VL53L0X_SPAD_COUNT_ADDR 0x06
#define VL53L0X_RANGE_STATUS_ADDR 0x08
#define VL53L0X_TIMING_BUDGET_ADDR 0x09
#define VL53L0X_INTER_MEAS_ADDR 0x0D
#define VL53L0X_MODE_ADDR 0x0E

// 引脚定义
#define PIN_XSHUT 1  // Shutdown pin (active low)
#define PIN_INT 2  // Interrupt (open-drain)

#endif /* VL53L0X_DEVICE_H */
