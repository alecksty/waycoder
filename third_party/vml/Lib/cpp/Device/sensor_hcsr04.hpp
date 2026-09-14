#ifndef HC_SR04_HPP
#define HC_SR04_HPP

// HC_SR04寄存器定义
// 生成自: Generic/Sensor/HC_SR04
// 版本: 1.0
// 日期: 2026-05-06


#ifdef __cplusplus
extern "C" {
#endif

// CPU架构: Sensor
// 位宽: 8位
// 时钟频率: 0 Hz

// 内存段定义
// PCB Module (45x20x15mm)
#define PACKAGE_START 0x00
#define PACKAGE_END 0x00
#define PACKAGE_SIZE 0

// 外设定义
// HC-SR04 Ultrasonic Sensor (4.5V-5.5V)
#define HC_SR04_BASE 0x00
#define HC_SR04_TRIG (*(volatile uint8_t*)0x00000000)
#define HC_SR04_DISTANCE_H (*(volatile uint8_t*)0x00000001)
#define HC_SR04_DISTANCE_L (*(volatile uint8_t*)0x00000002)
#define HC_SR04_STATUS (*(volatile uint8_t*)0x00000003)
#define HC_SR04_STATUS_BUSY 0  // 1=Measurement in progress
#define HC_SR04_STATUS_VALID 1  // 1=Valid measurement available
#define HC_SR04_STATUS_TIMEOUT 2  // 1=No echo received (out of range)

void hc_sr04_init(void);

#ifdef __cplusplus
}
#endif

#endif // HC_SR04_HPP
