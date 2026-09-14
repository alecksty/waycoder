// i.MX RT1062 设备定义 - Dart 库
// 生成自: NXP/i.MX RT/i.MX RT1062
// 版本: 1.0
// 日期: 2026-04-29
// 作者: VML Team
// 描述: 32-bit ARM Cortex-M7 MCU with 1MB SRAM, 600MHz, crossover processor
// CPU架构: ARM-Cortex-M7
// 位宽: 32位
// 时钟频率: 528000000 Hz

class i.MX RT1062Device {
  static const String deviceName = "i.MX RT1062";
  static const String manufacturer = "NXP";
  static const String family = "i.MX RT";
  static const String version = "1.0";
  static const String architecture = "ARM-Cortex-M7";
  static const int bits = 32;
  static const int clockFrequency = 528000000;

  // 外设定义
  // LPUART 1
  static const int UART1_BASE = 0x40184000;
  static const int UART1_VERID_ADDR = 0x000;
  static const int UART1_CTRL_ADDR = 0x010;
  static const int UART1_STAT_ADDR = 0x014;
  static const int UART1_DATA_ADDR = 0x01C;
  static const int UART1_BAUD_ADDR = 0x024;
  // LPUART 2
  static const int UART2_BASE = 0x40188000;
  static const int UART2_CTRL_ADDR = 0x010;
  static const int UART2_STAT_ADDR = 0x014;
  static const int UART2_DATA_ADDR = 0x01C;
  static const int UART2_BAUD_ADDR = 0x024;
  // GPIO 1
  static const int GPIO1_BASE = 0x401B8000;
  static const int GPIO1_DR_ADDR = 0x000;
  static const int GPIO1_GDIR_ADDR = 0x004;
  static const int GPIO1_PSR_ADDR = 0x008;
  static const int GPIO1_ICR1_ADDR = 0x00C;
  static const int GPIO1_ICR2_ADDR = 0x010;
  static const int GPIO1_IMR_ADDR = 0x014;
  static const int GPIO1_ISR_ADDR = 0x018;
  static const int GPIO1_EDGE_SEL_ADDR = 0x01C;
  // GPT 定时器 1
  static const int GPT1_BASE = 0x401EC000;
  static const int GPT1_CR_ADDR = 0x000;
  static const int GPT1_PR_ADDR = 0x004;
  static const int GPT1_SR_ADDR = 0x008;
  static const int GPT1_IR_ADDR = 0x00C;
  static const int GPT1_OCR1_ADDR = 0x010;
  static const int GPT1_CNT_ADDR = 0x024;
  // USB OTG 1
  static const int USB1_BASE = 0x402E0000;
  static const int USB1_ID_ADDR = 0x000;
  static const int USB1_OTGSC_ADDR = 0x00C;
  static const int USB1_USBCMD_ADDR = 0x100;
  static const int USB1_PORTSC1_ADDR = 0x184;

}
