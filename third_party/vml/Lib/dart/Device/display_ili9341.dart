// ILI9341 设备定义 - Dart 库
// 生成自: Ilitek/Display/ILI9341
// 版本: 1.0
// 日期: 2026-05-06
// 作者: VML Team
// 描述: ILI9341 2.8" 240x320 TFT LCD Display (SPI, 18-bit color, touch)
// CPU架构: Display
// 位宽: 18位
// 时钟频率: 20000000 Hz

class ILI9341Device {
  static const String deviceName = "ILI9341";
  static const String manufacturer = "Ilitek";
  static const String family = "Display";
  static const String version = "1.0";
  static const String architecture = "Display";
  static const int bits = 18;
  static const int clockFrequency = 20000000;

  // 内存段定义
  static const int GRAM_START = 0x00;
  static const int GRAM_END = 0xBCFF;
  static const int GRAM_SIZE = 156672;  // Graphics RAM (240x320x18bit)

  // 外设定义
  // ILI9341 240x320 TFT (SPI, 3.3V, 2.8inch)
  static const int ILI9341_BASE = 0x00;
  static const int ILI9341_CMD_ADDR = 0x00;
  static const int ILI9341_DATA_ADDR = 0x01;
  static const int ILI9341_COL_START_ADDR = 0x2A;
  static const int ILI9341_PAGE_START_ADDR = 0x2B;
  static const int ILI9341_WRITE_RAM_ADDR = 0x2C;
  static const int ILI9341_MADCTL_ADDR = 0x36;
  static const int ILI9341_PIXFMT_ADDR = 0x3A;
  static const int ILI9341_FRMCTL_ADDR = 0xB1;
  static const int ILI9341_GAMMA_ADDR = 0x26;
  static const int ILI9341_SLEEP_OUT_ADDR = 0x11;
  static const int ILI9341_DISP_ON_ADDR = 0x29;

}
