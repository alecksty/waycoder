// HD44780 设备定义 - Dart 库
// 生成自: Hitachi/Display/HD44780
// 版本: 1.0
// 日期: 2026-05-06
// 作者: VML Team
// 描述: HD44780 16x2 Character LCD Controller (4-bit/8-bit parallel or I2C via PCF8574)
// CPU架构: Display
// 位宽: 8位
// 时钟频率: 0 Hz

class HD44780Device {
  static const String deviceName = "HD44780";
  static const String manufacturer = "Hitachi";
  static const String family = "Display";
  static const String version = "1.0";
  static const String architecture = "Display";
  static const int bits = 8;
  static const int clockFrequency = 0;

  // 内存段定义
  static const int DDRAM_START = 0x00;
  static const int DDRAM_END = 0x4F;
  static const int DDRAM_SIZE = 80;  // Display Data RAM (80 bytes, 2 lines)
  static const int CGRAM_START = 0x00;
  static const int CGRAM_END = 0x3F;
  static const int CGRAM_SIZE = 64;  // Character Generator RAM (8 custom chars x 8 bytes)

  // 外设定义
  // HD44780 16x2 LCD (0x27/0x3F I2C, 5V)
  static const int HD44780_BASE = 0x27;
  static const int HD44780_CMD_ADDR = 0x00;
  static const int HD44780_DATA_ADDR = 0x01;
  static const int HD44780_CTRL_RS_ADDR = 0x00;
  static const int HD44780_CTRL_RW_ADDR = 0x01;
  static const int HD44780_CTRL_EN_ADDR = 0x02;
  static const int HD44780_CTRL_BL_ADDR = 0x03;
  static const int HD44780_ADDR_DDRAM_ADDR = 0x80;
  static const int HD44780_ADDR_CGRAM_ADDR = 0x40;

}
