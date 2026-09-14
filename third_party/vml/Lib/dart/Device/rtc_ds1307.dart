// DS1307 设备定义 - Dart 库
// 生成自: Maxim/Dallas/RTC/DS1307
// 版本: 1.0
// 日期: 2026-05-06
// 作者: VML Team
// 描述: DS1307 I2C Real-Time Clock (56-byte NVRAM, battery backup)
// CPU架构: RTC
// 位宽: 8位
// 时钟频率: 100000 Hz

class DS1307Device {
  static const String deviceName = "DS1307";
  static const String manufacturer = "Maxim/Dallas";
  static const String family = "RTC";
  static const String version = "1.0";
  static const String architecture = "RTC";
  static const int bits = 8;
  static const int clockFrequency = 100000;

  // 内存段定义
  static const int NVRAM_START = 0x08;
  static const int NVRAM_END = 0x3F;
  static const int NVRAM_SIZE = 56;  // Non-volatile RAM (56 bytes)

  // 外设定义
  // DS1307 RTC (0x68, 5V, DIP-8)
  static const int DS1307_BASE = 0x68;
  static const int DS1307_SEC_ADDR = 0x00;
  static const int DS1307_MIN_ADDR = 0x01;
  static const int DS1307_HOUR_ADDR = 0x02;
  static const int DS1307_DAY_ADDR = 0x03;
  static const int DS1307_DATE_ADDR = 0x04;
  static const int DS1307_MONTH_ADDR = 0x05;
  static const int DS1307_YEAR_ADDR = 0x06;
  static const int DS1307_CTRL_ADDR = 0x07;

}
