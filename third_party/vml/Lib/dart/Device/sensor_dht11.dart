// DHT11 设备定义 - Dart 库
// 生成自: Aosong/Sensor/DHT11
// 版本: 1.0
// 日期: 2026-05-06
// 作者: VML Team
// 描述: Digital Temperature and Humidity Sensor (1-Wire)
// CPU架构: Sensor
// 位宽: 8位
// 时钟频率: 500000 Hz

class DHT11Device {
  static const String deviceName = "DHT11";
  static const String manufacturer = "Aosong";
  static const String family = "Sensor";
  static const String version = "1.0";
  static const String architecture = "Sensor";
  static const int bits = 8;
  static const int clockFrequency = 500000;

  // 内存段定义
  static const int PACKAGE_START = 0x00;
  static const int PACKAGE_END = 0x00;
  static const int PACKAGE_SIZE = 4;  // DIP-4/SMD-4

  // 外设定义
  // DHT11 1-Wire Sensor (3.0V-5.5V)
  static const int DHT11_BASE = 0x00;
  static const int DHT11_HUMIDITY_INT_ADDR = 0x00;
  static const int DHT11_HUMIDITY_DEC_ADDR = 0x01;
  static const int DHT11_TEMP_INT_ADDR = 0x02;
  static const int DHT11_TEMP_DEC_ADDR = 0x03;
  static const int DHT11_CHECKSUM_ADDR = 0x04;

}
