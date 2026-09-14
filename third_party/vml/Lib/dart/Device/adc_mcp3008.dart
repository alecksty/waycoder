// MCP3008 设备定义 - Dart 库
// 生成自: Microchip/ADC/MCP3008
// 版本: 1.0
// 日期: 2026-05-06
// 作者: VML Team
// 描述: MCP3008 10-bit SPI ADC (8-channel, 200ksps)
// CPU架构: ADC
// 位宽: 10位
// 时钟频率: 1350000 Hz

class MCP3008Device {
  static const String deviceName = "MCP3008";
  static const String manufacturer = "Microchip";
  static const String family = "ADC";
  static const String version = "1.0";
  static const String architecture = "ADC";
  static const int bits = 10;
  static const int clockFrequency = 1350000;

  // 外设定义
  // MCP3008 10-bit 8-ch ADC (SPI, 2.7V-5.5V, DIP-16)
  static const int MCP3008_BASE = 0x00;
  static const int MCP3008_CH0_ADDR = 0x00;
  static const int MCP3008_CH1_ADDR = 0x01;
  static const int MCP3008_CH2_ADDR = 0x02;
  static const int MCP3008_CH3_ADDR = 0x03;
  static const int MCP3008_CH4_ADDR = 0x04;
  static const int MCP3008_CH5_ADDR = 0x05;
  static const int MCP3008_CH6_ADDR = 0x06;
  static const int MCP3008_CH7_ADDR = 0x07;
  static const int MCP3008_DIFF_01_ADDR = 0x08;
  static const int MCP3008_DIFF_23_ADDR = 0x09;

}
