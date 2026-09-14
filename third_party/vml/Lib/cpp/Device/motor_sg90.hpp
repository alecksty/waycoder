#ifndef SG90_HPP
#define SG90_HPP

// SG90寄存器定义
// 生成自: Tower Pro/Motor/SG90
// 版本: 1.0
// 日期: 2026-05-06


#ifdef __cplusplus
extern "C" {
#endif

// CPU架构: Motor
// 位宽: 8位
// 时钟频率: 0 Hz

// 外设定义
// SG90 Micro Servo (500-2500us pulse, 50Hz)
#define SG90_BASE 0x00
#define SG90_ANGLE (*(volatile uint8_t*)0x00000000)
#define SG90_PULSE_MIN (*(volatile uint16_t*)0x00000001)
#define SG90_PULSE_MAX (*(volatile uint16_t*)0x00000003)
#define SG90_CURRENT_ANGLE (*(volatile uint8_t*)0x00000005)
#define SG90_SPEED (*(volatile uint8_t*)0x00000006)

void sg90_init(void);

#ifdef __cplusplus
}
#endif

#endif // SG90_HPP
