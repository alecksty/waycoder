// PCF8574 设备定义 - Dart 库
// 生成自: NXP/TI/GPIO/PCF8574
// 版本: 1.0
// 日期: 2026-05-06
// 作者: VML Team
// 描述: PCF8574 8-bit I2C GPIO Expander (quasi-bidirectional, interrupt)
// CPU架构: GPIO
// 位宽: 8位
// 时钟频率: 100000 Hz

class PCF8574Device {
  static const String deviceName = "PCF8574";
  static const String manufacturer = "NXP/TI";
  static const String family = "GPIO";
  static const String version = "1.0";
  static const String architecture = "GPIO";
  static const int bits = 8;
  static const int clockFrequency = 100000;

  // 外设定义
  // PCF8574 8-bit GPIO (0x20-0x27, 2.5V-6V)
  static const int PCF8574_BASE = 0x20;
  static const int PCF8574_INPUT_ADDR = 0x00;
  static const int PCF8574_INPUT_P0_BIT = 0;  // Pin P0
  static const int PCF8574_INPUT_P1_BIT = 1;  // Pin P1
  static const int PCF8574_INPUT_P2_BIT = 2;  // Pin P2
  static const int PCF8574_INPUT_P3_BIT = 3;  // Pin P3
  static const int PCF8574_INPUT_P4_BIT = 4;  // Pin P4
  static const int PCF8574_INPUT_P5_BIT = 5;  // Pin P5
  static const int PCF8574_INPUT_P6_BIT = 6;  // Pin P6
  static const int PCF8574_INPUT_P7_BIT = 7;  // Pin P7
  static const int PCF8574_OUTPUT_ADDR = 0x01;
  static const int PCF8574_POLARITY_ADDR = 0x02;

  // 中断向量定义
  static const int INT_INT = 0;  // Pin change interrupt (open-drain, active low)

}
