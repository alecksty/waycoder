// BH1750 设备定义 - Dart 库
// 生成自: ROHM/Sensor/BH1750
// 版本: 1.0
// 日期: 2026-05-06
// 作者: VML Team
// 描述: BH1750FVI Digital Ambient Light Sensor (I2C, 1-65535 lux, 16-bit)
// CPU架构: Sensor
// 位宽: 16位
// 时钟频率: 400000 Hz

class BH1750Device {
  static const String deviceName = "BH1750";
  static const String manufacturer = "ROHM";
  static const String family = "Sensor";
  static const String version = "1.0";
  static const String architecture = "Sensor";
  static const int bits = 16;
  static const int clockFrequency = 400000;

  // 外设定义
  // BH1750 Light Sensor (0x23/0x5C, 2.4V-3.6V)
  static const int BH1750_BASE = 0x23;
  static const int BH1750_LUX_ADDR = 0x00;
  static const int BH1750_MODE_ADDR = 0x01;
  static const int BH1750_MODE_CONT_H_BIT = 0;  // Continuous High Res (1lx, 120ms)
  static const int BH1750_MODE_CONT_H2_BIT = 1;  // Continuous High Res 2 (0.5lx, 120ms)
  static const int BH1750_MODE_CONT_L_BIT = 2;  // Continuous Low Res (4lx, 16ms)
  static const int BH1750_MODE_ONCE_H_BIT = 3;  // One-time High Res (1lx, 120ms)
  static const int BH1750_MODE_ONCE_H2_BIT = 4;  // One-time High Res 2 (0.5lx, 120ms)
  static const int BH1750_MODE_ONCE_L_BIT = 5;  // One-time Low Res (4lx, 16ms)
  static const int BH1750_CMD_POWER_ON_ADDR = 0x01;
  static const int BH1750_CMD_POWER_OFF_ADDR = 0x00;
  static const int BH1750_CMD_RESET_ADDR = 0x07;

}
