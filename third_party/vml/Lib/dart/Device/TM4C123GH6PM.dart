// TM4C123GH6PM 设备定义 - Dart 库
// 生成自: Texas Instruments/Tiva C/TM4C123GH6PM
// 版本: 1.0
// 日期: 2026-04-29
// 作者: VML Team
// 描述: 32-bit ARM Cortex-M4F MCU with 256KB Flash, 32KB SRAM, 80MHz, USB
// CPU架构: ARM-Cortex-M4F
// 位宽: 32位
// 时钟频率: 80000000 Hz

class TM4C123GH6PMDevice {
  static const String deviceName = "TM4C123GH6PM";
  static const String manufacturer = "Texas Instruments";
  static const String family = "Tiva C";
  static const String version = "1.0";
  static const String architecture = "ARM-Cortex-M4F";
  static const int bits = 32;
  static const int clockFrequency = 80000000;

  // 外设定义
  // UART 0
  static const int UART0_BASE = 0x4000C000;
  static const int UART0_DR_ADDR = 0x000;
  static const int UART0_FR_ADDR = 0x018;
  static const int UART0_IBRD_ADDR = 0x024;
  static const int UART0_FBRD_ADDR = 0x028;
  static const int UART0_LCRH_ADDR = 0x02C;
  static const int UART0_CTL_ADDR = 0x030;
  static const int UART0_IM_ADDR = 0x038;
  static const int UART0_RIS_ADDR = 0x03C;
  static const int UART0_ICR_ADDR = 0x044;
  // UART 1
  static const int UART1_BASE = 0x4000D000;
  static const int UART1_DR_ADDR = 0x000;
  static const int UART1_FR_ADDR = 0x018;
  static const int UART1_IBRD_ADDR = 0x024;
  static const int UART1_FBRD_ADDR = 0x028;
  static const int UART1_LCRH_ADDR = 0x02C;
  static const int UART1_CTL_ADDR = 0x030;
  // GPIO Port A
  static const int GPIOA_BASE = 0x40004000;
  static const int GPIOA_DATA_ADDR = 0x3FC;
  static const int GPIOA_DIR_ADDR = 0x400;
  static const int GPIOA_IS_ADDR = 0x404;
  static const int GPIOA_IBE_ADDR = 0x408;
  static const int GPIOA_IEV_ADDR = 0x40C;
  static const int GPIOA_IM_ADDR = 0x410;
  static const int GPIOA_RIS_ADDR = 0x414;
  static const int GPIOA_MIS_ADDR = 0x418;
  static const int GPIOA_ICR_ADDR = 0x41C;
  static const int GPIOA_AFSEL_ADDR = 0x420;
  static const int GPIOA_DEN_ADDR = 0x51C;
  // 16/32-bit Timer 0
  static const int TIMER0_BASE = 0x40030000;
  static const int TIMER0_CFG_ADDR = 0x000;
  static const int TIMER0_TAMR_ADDR = 0x004;
  static const int TIMER0_CTL_ADDR = 0x00C;
  static const int TIMER0_ILR_ADDR = 0x028;
  static const int TIMER0_V_ADDR = 0x038;
  static const int TIMER0_ICR_ADDR = 0x024;
  // ADC 0
  static const int ADC0_BASE = 0x40038000;
  static const int ADC0_ACTSS_ADDR = 0x000;
  static const int ADC0_EMUX_ADDR = 0x014;
  static const int ADC0_SSMUX0_ADDR = 0x040;
  static const int ADC0_SSFIFO0_ADDR = 0x048;
  static const int ADC0_PROC_ADDR = 0x030;

}
