#ifndef PCF8574_HPP
#define PCF8574_HPP

// PCF8574寄存器定义
// 生成自: NXP/TI/GPIO/PCF8574
// 版本: 1.0
// 日期: 2026-05-06


#ifdef __cplusplus
extern "C" {
#endif

// CPU架构: GPIO
// 位宽: 8位
// 时钟频率: 100000 Hz

// 外设定义
// PCF8574 8-bit GPIO (0x20-0x27, 2.5V-6V)
#define PCF8574_BASE 0x20
#define PCF8574_INPUT (*(volatile uint8_t*)0x00000020)
#define PCF8574_INPUT_P0 0  // Pin P0
#define PCF8574_INPUT_P1 1  // Pin P1
#define PCF8574_INPUT_P2 2  // Pin P2
#define PCF8574_INPUT_P3 3  // Pin P3
#define PCF8574_INPUT_P4 4  // Pin P4
#define PCF8574_INPUT_P5 5  // Pin P5
#define PCF8574_INPUT_P6 6  // Pin P6
#define PCF8574_INPUT_P7 7  // Pin P7
#define PCF8574_OUTPUT (*(volatile uint8_t*)0x00000021)
#define PCF8574_POLARITY (*(volatile uint8_t*)0x00000022)

// 中断向量定义
#define INT_VECTOR 0  // Pin change interrupt (open-drain, active low)

void pcf8574_init(void);

#ifdef __cplusplus
}
#endif

#endif // PCF8574_HPP
