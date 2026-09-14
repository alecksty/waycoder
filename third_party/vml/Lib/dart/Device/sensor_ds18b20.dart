// DS18B20 设备定义 - Dart 库
// 生成自: Maxim/Dallas/Sensor/DS18B20
// 版本: 1.0
// 日期: 2026-05-06
// 作者: VML Team
// 描述: Programmable Resolution 1-Wire Digital Thermometer
// CPU架构: Sensor
// 位宽: 8位
// 时钟频率: 100000 Hz

class DS18B20Device {
  static const String deviceName = "DS18B20";
  static const String manufacturer = "Maxim/Dallas";
  static const String family = "Sensor";
  static const String version = "1.0";
  static const String architecture = "Sensor";
  static const int bits = 8;
  static const int clockFrequency = 100000;

  // 内存段定义
  static const int SCRATCHPAD_START = 0x00;
  static const int SCRATCHPAD_END = 0x08;
  static const int SCRATCHPAD_SIZE = 9;  // Scratchpad memory (9 bytes)
  static const int EEPROM_START = 0x00;
  static const int EEPROM_END = 0x02;
  static const int EEPROM_SIZE = 3;  // EEPROM (TH, TL, config bytes)

  // 外设定义
  // DS18B20 1-Wire Thermometer (3.0V-5.5V, TO-92)
  static const int DS18B20_BASE = 0x00;
  static const int DS18B20_TEMP_LSB_ADDR = 0x00;
  static const int DS18B20_TEMP_MSB_ADDR = 0x01;
  static const int DS18B20_TH_REG_ADDR = 0x02;
  static const int DS18B20_TL_REG_ADDR = 0x03;
  static const int DS18B20_CONFIG_ADDR = 0x04;
  static const int DS18B20_CONFIG_R0_BIT = 5;  // Resolution select bit 0
  static const int DS18B20_CONFIG_R1_BIT = 6;  // Resolution select bit 1 (00=9bit,10=10bit,01=11bit,11=12bit)
  static const int DS18B20_COUNT_REMAIN_ADDR = 0x06;
  static const int DS18B20_COUNT_PER_C_ADDR = 0x07;
  static const int DS18B20_CRC_ADDR = 0x08;

}
