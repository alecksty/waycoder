// SG90 设备定义 - Objective-C 头文件
// 生成自: Tower Pro/Motor/SG90
// 版本: 1.0
// 日期: 2026-05-06
// 作者: VML Team
// 描述: SG90 Micro Servo Motor (0-180°, 4.8V-6V)
// CPU架构: Motor
// 位宽: 8位
// 时钟频率: 0 Hz

#ifndef SG90_DEVICE_H
#define SG90_DEVICE_H

#import <Foundation/Foundation.h>

// 外设定义
// SG90 Micro Servo (500-2500us pulse, 50Hz)
#define SG90_BASE 0x00
#define SG90_ANGLE_ADDR 0x00
#define SG90_PULSE_MIN_ADDR 0x01
#define SG90_PULSE_MAX_ADDR 0x03
#define SG90_CURRENT_ANGLE_ADDR 0x05
#define SG90_SPEED_ADDR 0x06

#endif /* SG90_DEVICE_H */
