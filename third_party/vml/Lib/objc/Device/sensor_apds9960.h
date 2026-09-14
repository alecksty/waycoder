// APDS9960 设备定义 - Objective-C 头文件
// 生成自: Broadcom/Avago/Sensor/APDS9960
// 版本: 1.0
// 日期: 2026-05-06
// 作者: VML Team
// 描述: APDS9960 Gesture/Proximity/Ambient Light/RGB Sensor (I2C)
// CPU架构: Sensor
// 位宽: 8位
// 时钟频率: 400000 Hz

#ifndef APDS9960_DEVICE_H
#define APDS9960_DEVICE_H

#import <Foundation/Foundation.h>

// 外设定义
// APDS9960 Gesture/RGB Sensor (0x39, 3.3V)
#define APDS9960_BASE 0x39
#define APDS9960_ENABLE_ADDR 0x80
#define APDS9960_GESTURE_ADDR 0xFC
#define APDS9960_PROXIMITY_ADDR 0x9C
#define APDS9960_AMBIENT_ADDR 0x96
#define APDS9960_RED_ADDR 0x98
#define APDS9960_GREEN_ADDR 0x9A
#define APDS9960_BLUE_ADDR 0x9C
#define APDS9960_GESTURE_FIFO_ADDR 0xFC
#define APDS9960_GESTURE_COUNT_ADDR 0xFD

// 中断向量定义
#define INT_INT 0  // Gesture/Proximity/Light interrupt

#endif /* APDS9960_DEVICE_H */
