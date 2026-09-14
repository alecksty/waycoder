/**
 * WS2812B 寄存器定义
 * 生成自: Worldsemi/LED/WS2812B
 * 版本: 1.0
 */
export const ws2812b = {
  // CPU: LED, 24位, 800000 Hz

  // 内存段
  // Frame buffer (up to 256 LEDs × 3 bytes)
  LED_FB_START: 0x00,
  LED_FB_END: 0xFF,
  LED_FB_SIZE: 256,

  // 外设定义
  // WS2812B RGB LED Strip (5V, 60mA/led)
  WS2812B_BASE: 0x00,
  WS2812B_LED_COUNT: 0x00000000,
  WS2812B_LED_DATA: 0x00000002,
  WS2812B_BRIGHTNESS: 0x00000005,
  WS2812B_SHOW: 0x00000006,
  WS2812B_CLEAR: 0x00000007,

  init: function() {
    // 硬件初始化
  }
};
