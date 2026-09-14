#ifndef _74HC595_HPP
#define _74HC595_HPP

// 74HC595寄存器定义
// 生成自: TI/NXP/GPIO/74HC595
// 版本: 1.0
// 日期: 2026-05-06


#ifdef __cplusplus
extern "C" {
#endif

// CPU架构: GPIO
// 位宽: 8位
// 时钟频率: 10000000 Hz

// 外设定义
// 74HC595 8-bit Shift Register (2V-6V, DIP-16)
#define _74HC595_BASE 0x00
#define _74HC595_DATA (*(volatile uint8_t*)0x00000000)
#define _74HC595_LATCH (*(volatile uint8_t*)0x00000001)
#define _74HC595_CHAIN_COUNT (*(volatile uint8_t*)0x00000002)
#define _74HC595_OE (*(volatile uint8_t*)0x00000003)
#define _74HC595_CLEAR (*(volatile uint8_t*)0x00000004)

void _74hc595_init(void);

#ifdef __cplusplus
}
#endif

#endif // _74HC595_HPP
