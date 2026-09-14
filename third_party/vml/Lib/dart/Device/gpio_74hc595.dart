// 74HC595 设备定义 - Dart 库
// 生成自: TI/NXP/GPIO/74HC595
// 版本: 1.0
// 日期: 2026-05-06
// 作者: VML Team
// 描述: 74HC595 8-bit Shift Register (SPI-compatible, serial-in parallel-out, daisy-chainable)
// CPU架构: GPIO
// 位宽: 8位
// 时钟频率: 10000000 Hz

class 74HC595Device {
  static const String deviceName = "74HC595";
  static const String manufacturer = "TI/NXP";
  static const String family = "GPIO";
  static const String version = "1.0";
  static const String architecture = "GPIO";
  static const int bits = 8;
  static const int clockFrequency = 10000000;

  // 外设定义
  // 74HC595 8-bit Shift Register (2V-6V, DIP-16)
  static const int _74HC595_BASE = 0x00;
  static const int _74HC595_DATA_ADDR = 0x00;
  static const int _74HC595_LATCH_ADDR = 0x01;
  static const int _74HC595_CHAIN_COUNT_ADDR = 0x02;
  static const int _74HC595_OE_ADDR = 0x03;
  static const int _74HC595_CLEAR_ADDR = 0x04;

}
