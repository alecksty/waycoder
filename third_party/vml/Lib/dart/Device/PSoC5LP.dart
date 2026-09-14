// CY8C5888LTI-LP097 设备定义 - Dart 库
// 生成自: Cypress (Infineon)/PSoC/CY8C5888LTI-LP097
// 版本: 1.0
// 日期: 2026-04-29
// 作者: VML Team
// 描述: 32-bit ARM Cortex-M3 PSoC 5LP with 256KB Flash, 64KB SRAM, 80MHz, UDB
// CPU架构: ARM-Cortex-M3
// 位宽: 32位
// 时钟频率: 80000000 Hz

class CY8C5888LTI_LP097Device {
  static const String deviceName = "CY8C5888LTI-LP097";
  static const String manufacturer = "Cypress (Infineon)";
  static const String family = "PSoC";
  static const String version = "1.0";
  static const String architecture = "ARM-Cortex-M3";
  static const int bits = 32;
  static const int clockFrequency = 80000000;

  // 外设定义
  // SCB UART (可编程)
  static const int UART_BASE = 0x40050000;
  static const int UART_CTRL_ADDR = 0x00;
  static const int UART_STATUS_ADDR = 0x04;
  static const int UART_TX_DATA_ADDR = 0x08;
  static const int UART_RX_DATA_ADDR = 0x0C;
  // SCB I2C
  static const int I2C_BASE = 0x40051000;
  static const int I2C_CTRL_ADDR = 0x00;
  static const int I2C_STATUS_ADDR = 0x04;
  static const int I2C_TX_DATA_ADDR = 0x08;
  static const int I2C_RX_DATA_ADDR = 0x0C;
  // TCPWM 定时器
  static const int TIMER_BASE = 0x40060000;
  static const int TIMER_CTRL_ADDR = 0x00;
  static const int TIMER_STATUS_ADDR = 0x04;
  static const int TIMER_CNT_ADDR = 0x08;
  static const int TIMER_PERIOD_ADDR = 0x0C;
  static const int TIMER_CC_ADDR = 0x10;
  // DelSig ADC 20-bit
  static const int ADC_BASE = 0x40100000;
  static const int ADC_CTRL_ADDR = 0x00;
  static const int ADC_STATUS_ADDR = 0x04;
  static const int ADC_DATA_ADDR = 0x08;
  static const int ADC_CLOCK_ADDR = 0x10;
  // GPIO 端口
  static const int GPIO_BASE = 0x40040000;
  static const int GPIO_DR_ADDR = 0x00;
  static const int GPIO_PS_ADDR = 0x04;
  static const int GPIO_IE_ADDR = 0x08;
  static const int GPIO_DM_ADDR = 0x0C;
  // USB 控制器
  static const int USB_BASE = 0x40080000;
  static const int USB_CR0_ADDR = 0x00;
  static const int USB_CR1_ADDR = 0x04;
  static const int USB_STAT_ADDR = 0x08;

}
