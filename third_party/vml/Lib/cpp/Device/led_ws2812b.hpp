#ifndef WS2812B_HPP
#define WS2812B_HPP

// WS2812B寄存器定义
// 生成自: Worldsemi/LED/WS2812B
// 版本: 1.0
// 日期: 2026-05-06


#ifdef __cplusplus
extern "C" {
#endif

// CPU架构: LED
// 位宽: 24位
// 时钟频率: 800000 Hz

// 内存段定义
// Frame buffer (up to 256 LEDs × 3 bytes)
#define LED_FB_START 0x00
#define LED_FB_END 0xFF
#define LED_FB_SIZE 256

// 外设定义
// WS2812B RGB LED Strip (5V, 60mA/led)
#define WS2812B_BASE 0x00
#define WS2812B_LED_COUNT (*(volatile uint16_t*)0x00000000)
#define WS2812B_LED_DATA (*(volatile uint32_t*)0x00000002)
#define WS2812B_BRIGHTNESS (*(volatile uint8_t*)0x00000005)
#define WS2812B_SHOW (*(volatile uint8_t*)0x00000006)
#define WS2812B_CLEAR (*(volatile uint8_t*)0x00000007)

void ws2812b_init(void);

#ifdef __cplusplus
}
#endif

#endif // WS2812B_HPP
