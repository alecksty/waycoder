// HC_SR04 设备定义 - Objective-C 头文件
// 生成自: Generic/Sensor/HC_SR04
// 版本: 1.0
// 日期: 2026-05-06
// 作者: VML Team
// 描述: Ultrasonic Distance Sensor (2cm-400cm)
// CPU架构: Sensor
// 位宽: 8位
// 时钟频率: 0 Hz

#ifndef HC_SR04_DEVICE_H
#define HC_SR04_DEVICE_H

#import <Foundation/Foundation.h>

// 内存段定义
#define PACKAGE_START 0x00
#define PACKAGE_END 0x00
#define PACKAGE_SIZE 0  // PCB Module (45x20x15mm)

// 外设定义
// HC-SR04 Ultrasonic Sensor (4.5V-5.5V)
#define HC_SR04_BASE 0x00
#define HC_SR04_TRIG_ADDR 0x00
#define HC_SR04_DISTANCE_H_ADDR 0x01
#define HC_SR04_DISTANCE_L_ADDR 0x02
#define HC_SR04_STATUS_ADDR 0x03
#define HC_SR04_STATUS_BUSY_BIT 0  // 1=Measurement in progress
#define HC_SR04_STATUS_VALID_BIT 1  // 1=Valid measurement available
#define HC_SR04_STATUS_TIMEOUT_BIT 2  // 1=No echo received (out of range)

#endif /* HC_SR04_DEVICE_H */
