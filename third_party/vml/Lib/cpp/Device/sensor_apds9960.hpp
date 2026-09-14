#ifndef APDS9960_HPP
#define APDS9960_HPP

// APDS9960寄存器定义
// 生成自: Broadcom/Avago/Sensor/APDS9960
// 版本: 1.0
// 日期: 2026-05-06


#ifdef __cplusplus
extern "C" {
#endif

// CPU架构: Sensor
// 位宽: 8位
// 时钟频率: 400000 Hz

// 外设定义
// APDS9960 Gesture/RGB Sensor (0x39, 3.3V)
#define APDS9960_BASE 0x39
#define APDS9960_ENABLE (*(volatile uint8_t*)0x000000B9)
#define APDS9960_GESTURE (*(volatile uint8_t*)0x00000135)
#define APDS9960_PROXIMITY (*(volatile uint8_t*)0x000000D5)
#define APDS9960_AMBIENT (*(volatile uint16_t*)0x000000CF)
#define APDS9960_RED (*(volatile uint16_t*)0x000000D1)
#define APDS9960_GREEN (*(volatile uint16_t*)0x000000D3)
#define APDS9960_BLUE (*(volatile uint16_t*)0x000000D5)
#define APDS9960_GESTURE_FIFO (*(volatile uint32_t*)0x00000135)
#define APDS9960_GESTURE_COUNT (*(volatile uint8_t*)0x00000136)

// 中断向量定义
#define INT_VECTOR 0  // Gesture/Proximity/Light interrupt

void apds9960_init(void);

#ifdef __cplusplus
}
#endif

#endif // APDS9960_HPP
