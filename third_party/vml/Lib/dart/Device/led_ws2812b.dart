// WS2812B 设备定义 - Dart 库
// 生成自: Worldsemi/LED/WS2812B
// 版本: 1.0
// 日期: 2026-05-06
// 作者: VML Team
// 描述: WS2812B Intelligent RGB LED (single-wire, 800KHz, daisy-chainable)
// CPU架构: LED
// 位宽: 24位
// 时钟频率: 800000 Hz

class WS2812BDevice {
  static const String deviceName = "WS2812B";
  static const String manufacturer = "Worldsemi";
  static const String family = "LED";
  static const String version = "1.0";
  static const String architecture = "LED";
  static const int bits = 24;
  static const int clockFrequency = 800000;

  // 内存段定义
  static const int LED_FB_START = 0x00;
  static const int LED_FB_END = 0xFF;
  static const int LED_FB_SIZE = 256;  // Frame buffer (up to 256 LEDs × 3 bytes)

  // 外设定义
  // WS2812B RGB LED Strip (5V, 60mA/led)
  static const int WS2812B_BASE = 0x00;
  static const int WS2812B_LED_COUNT_ADDR = 0x00;
  static const int WS2812B_LED_DATA_ADDR = 0x02;
  static const int WS2812B_BRIGHTNESS_ADDR = 0x05;
  static const int WS2812B_SHOW_ADDR = 0x06;
  static const int WS2812B_CLEAR_ADDR = 0x07;

}
