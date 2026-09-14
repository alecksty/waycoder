#ifndef MAX7219_HPP
#define MAX7219_HPP

// MAX7219寄存器定义
// 生成自: Maxim/LED/MAX7219
// 版本: 1.0
// 日期: 2026-05-06


#ifdef __cplusplus
extern "C" {
#endif

// CPU架构: LED
// 位宽: 8位
// 时钟频率: 10000000 Hz

// 外设定义
// MAX7219 8-Digit/8x8 Matrix Driver (4.0V-5.5V, DIP-24)
#define MAX7219_BASE 0x00
#define MAX7219_DIGIT0 (*(volatile uint8_t*)0x00000001)
#define MAX7219_DIGIT1 (*(volatile uint8_t*)0x00000002)
#define MAX7219_DIGIT2 (*(volatile uint8_t*)0x00000003)
#define MAX7219_DIGIT3 (*(volatile uint8_t*)0x00000004)
#define MAX7219_DIGIT4 (*(volatile uint8_t*)0x00000005)
#define MAX7219_DIGIT5 (*(volatile uint8_t*)0x00000006)
#define MAX7219_DIGIT6 (*(volatile uint8_t*)0x00000007)
#define MAX7219_DIGIT7 (*(volatile uint8_t*)0x00000008)
#define MAX7219_DECODE (*(volatile uint8_t*)0x00000009)
#define MAX7219_INTENSITY (*(volatile uint8_t*)0x0000000A)
#define MAX7219_SCAN_LIMIT (*(volatile uint8_t*)0x0000000B)
#define MAX7219_SHUTDOWN (*(volatile uint8_t*)0x0000000C)
#define MAX7219_TEST (*(volatile uint8_t*)0x0000000F)

void max7219_init(void);

#ifdef __cplusplus
}
#endif

#endif // MAX7219_HPP
