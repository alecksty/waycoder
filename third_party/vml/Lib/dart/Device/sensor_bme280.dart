// BME280 设备定义 - Dart 库
// 生成自: Bosch/Sensor/BME280
// 版本: 1.0
// 日期: 2026-05-06
// 作者: VML Team
// 描述: BME280 Combined Humidity, Pressure, and Temperature Sensor (I2C/SPI)
// CPU架构: Sensor
// 位宽: 8位
// 时钟频率: 400000 Hz

class BME280Device {
  static const String deviceName = "BME280";
  static const String manufacturer = "Bosch";
  static const String family = "Sensor";
  static const String version = "1.0";
  static const String architecture = "Sensor";
  static const int bits = 8;
  static const int clockFrequency = 400000;

  // 外设定义
  // BME280 Environmental Sensor (0x76/0x77, 1.71V-3.6V)
  static const int BME280_BASE = 0x76;
  static const int BME280_CHIP_ID_ADDR = 0xD0;
  static const int BME280_RESET_ADDR = 0xE0;
  static const int BME280_CTRL_HUM_ADDR = 0xF2;
  static const int BME280_STATUS_ADDR = 0xF3;
  static const int BME280_CTRL_MEAS_ADDR = 0xF4;
  static const int BME280_CONFIG_ADDR = 0xF5;
  static const int BME280_PRESS_ADDR = 0xF7;
  static const int BME280_TEMP_ADDR = 0xFA;
  static const int BME280_HUM_ADDR = 0xFD;

}
