// MCP23017 设备定义 - Dart 库
// 生成自: Microchip/GPIO/MCP23017
// 版本: 1.0
// 日期: 2026-05-06
// 作者: VML Team
// 描述: MCP23017 16-bit I2C GPIO Expander (2 banks, interrupt, 25mA per pin)
// CPU架构: GPIO
// 位宽: 16位
// 时钟频率: 400000 Hz

class MCP23017Device {
  static const String deviceName = "MCP23017";
  static const String manufacturer = "Microchip";
  static const String family = "GPIO";
  static const String version = "1.0";
  static const String architecture = "GPIO";
  static const int bits = 16;
  static const int clockFrequency = 400000;

  // 外设定义
  // MCP23017 16-bit GPIO (0x20-0x27, 1.8V-5.5V)
  static const int MCP23017_BASE = 0x20;
  static const int MCP23017_IODIRA_ADDR = 0x00;
  static const int MCP23017_IODIRB_ADDR = 0x01;
  static const int MCP23017_GPIOA_ADDR = 0x12;
  static const int MCP23017_GPIOB_ADDR = 0x13;
  static const int MCP23017_GPINTENA_ADDR = 0x04;
  static const int MCP23017_GPINTENB_ADDR = 0x05;
  static const int MCP23017_INTCONA_ADDR = 0x08;
  static const int MCP23017_IOCON_ADDR = 0x0A;
  static const int MCP23017_GPPUA_ADDR = 0x0C;
  static const int MCP23017_GPPUB_ADDR = 0x0D;

  // 中断向量定义
  static const int INT_INTA = 0;  // Port A interrupt
  static const int INT_INTB = 1;  // Port B interrupt

}
