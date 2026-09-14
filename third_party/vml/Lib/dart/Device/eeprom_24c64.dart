// 24C64 设备定义 - Dart 库
// 生成自: Generic/Memory/24C64
// 版本: 1.0
// 日期: 2026-05-06
// 作者: VML Team
// 描述: 24C64 64Kbit I2C Serial EEPROM (8K×8, 32-byte page write)
// CPU架构: Memory
// 位宽: 8位
// 时钟频率: 400000 Hz

class 24C64Device {
  static const String deviceName = "24C64";
  static const String manufacturer = "Generic";
  static const String family = "Memory";
  static const String version = "1.0";
  static const String architecture = "Memory";
  static const int bits = 8;
  static const int clockFrequency = 400000;

  // 内存段定义
  static const int EEPROM_START = 0x00;
  static const int EEPROM_END = 0x1FFF;
  static const int EEPROM_SIZE = 8192;  // EEPROM main memory array (8KB, 32-byte page write)

  // 外设定义
  // 24C64 I2C EEPROM (0x50-0x57, 1.7V-5.5V)
  static const int _24C64_BASE = 0x50;
  static const int _24C64_ADDR_H_ADDR = 0x00;
  static const int _24C64_ADDR_L_ADDR = 0x01;
  static const int _24C64_DATA_ADDR = 0x02;
  static const int _24C64_PAGE_SIZE_ADDR = 0xFE;
  static const int _24C64_SIZE_ADDR = 0xFD;

}
