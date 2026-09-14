// DS3231 设备定义 - Dart 库
// 生成自: Maxim/Dallas/RTC/DS3231
// 版本: 1.0
// 日期: 2026-05-06
// 作者: VML Team
// 描述: DS3231 I2C High-Precision RTC (±2ppm, temperature compensated, 32K EEPROM)
// CPU架构: RTC
// 位宽: 8位
// 时钟频率: 400000 Hz

class DS3231Device {
  static const String deviceName = "DS3231";
  static const String manufacturer = "Maxim/Dallas";
  static const String family = "RTC";
  static const String version = "1.0";
  static const String architecture = "RTC";
  static const int bits = 8;
  static const int clockFrequency = 400000;

  // 内存段定义
  static const int EEPROM_START = 0x14;
  static const int EEPROM_END = 0xFF;
  static const int EEPROM_SIZE = 236;  // AT24C32 EEPROM (32Kbit)

  // 外设定义
  // DS3231 Precision RTC (0x68, 3.3V-5.5V)
  static const int DS3231_BASE = 0x68;
  static const int DS3231_SEC_ADDR = 0x00;
  static const int DS3231_MIN_ADDR = 0x01;
  static const int DS3231_HOUR_ADDR = 0x02;
  static const int DS3231_DAY_ADDR = 0x03;
  static const int DS3231_DATE_ADDR = 0x04;
  static const int DS3231_MONTH_CENT_ADDR = 0x05;
  static const int DS3231_YEAR_ADDR = 0x06;
  static const int DS3231_ALARM1_SEC_ADDR = 0x07;
  static const int DS3231_ALARM1_MIN_ADDR = 0x08;
  static const int DS3231_ALARM1_HOUR_ADDR = 0x09;
  static const int DS3231_ALARM2_MIN_ADDR = 0x0B;
  static const int DS3231_ALARM2_HOUR_ADDR = 0x0C;
  static const int DS3231_CTRL_ADDR = 0x0E;
  static const int DS3231_CTRL_STATUS_ADDR = 0x0F;
  static const int DS3231_TEMP_MSB_ADDR = 0x11;
  static const int DS3231_TEMP_LSB_ADDR = 0x12;

}
