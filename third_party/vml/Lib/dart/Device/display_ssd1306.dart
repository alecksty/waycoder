// SSD1306 设备定义 - Dart 库
// 生成自: Solomon Systech/Display/SSD1306
// 版本: 1.0
// 日期: 2026-05-06
// 作者: VML Team
// 描述: SSD1306 128x64 OLED Display Controller (I2C/SPI)
// CPU架构: Display
// 位宽: 8位
// 时钟频率: 400000 Hz

class SSD1306Device {
  static const String deviceName = "SSD1306";
  static const String manufacturer = "Solomon Systech";
  static const String family = "Display";
  static const String version = "1.0";
  static const String architecture = "Display";
  static const int bits = 8;
  static const int clockFrequency = 400000;

  // 内存段定义
  static const int GDDRAM_START = 0x00;
  static const int GDDRAM_END = 0x3FF;
  static const int GDDRAM_SIZE = 1024;  // Graphic Display Data RAM (128x64 = 1024 bytes)

  // 外设定义
  // SSD1306 128x64 OLED (0x3C/0x3D I2C, 3.3V-5V)
  static const int SSD1306_BASE = 0x3C;
  static const int SSD1306_CMD_ADDR = 0x00;
  static const int SSD1306_DATA_ADDR = 0x40;
  static const int SSD1306_DISPLAY_OFF_ADDR = 0xAE;
  static const int SSD1306_DISPLAY_ON_ADDR = 0xAF;
  static const int SSD1306_CONTRAST_ADDR = 0x81;
  static const int SSD1306_SEG_REMAP_ADDR = 0xA1;
  static const int SSD1306_COM_SCAN_ADDR = 0xC8;
  static const int SSD1306_ADDR_MODE_ADDR = 0x20;
  static const int SSD1306_COL_START_ADDR = 0x21;
  static const int SSD1306_PAGE_START_ADDR = 0x22;

}
