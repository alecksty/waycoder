// MSP432P401R 设备定义 - Dart 库
// 生成自: Texas Instruments/MSP432/MSP432P401R
// 版本: 1.0
// 日期: 2026-04-29
// 作者: VML Team
// 描述: 32-bit ARM Cortex-M4F MCU with 256KB Flash, 64KB SRAM, 48MHz, FPU
// CPU架构: ARM-Cortex-M4F
// 位宽: 32位
// 时钟频率: 48000000 Hz

class MSP432P401RDevice {
  static const String deviceName = "MSP432P401R";
  static const String manufacturer = "Texas Instruments";
  static const String family = "MSP432";
  static const String version = "1.0";
  static const String architecture = "ARM-Cortex-M4F";
  static const int bits = 32;
  static const int clockFrequency = 48000000;

  // 外设定义
  // eUSCI_A0 UART
  static const int UART0_BASE = 0x40001000;
  static const int UART0_CTLW0_ADDR = 0x00;
  static const int UART0_BRW_ADDR = 0x06;
  static const int UART0_UCA0TXBUF_ADDR = 0x08;
  static const int UART0_UCA0RXBUF_ADDR = 0x0A;
  static const int UART0_IFG_ADDR = 0x0C;
  static const int UART0_IE_ADDR = 0x0E;
  // eUSCI_A1 UART
  static const int UART1_BASE = 0x40002000;
  static const int UART1_CTLW0_ADDR = 0x00;
  static const int UART1_BRW_ADDR = 0x06;
  static const int UART1_TXBUF_ADDR = 0x08;
  static const int UART1_RXBUF_ADDR = 0x0A;
  static const int UART1_IFG_ADDR = 0x0C;
  static const int UART1_IE_ADDR = 0x0E;
  // Timer_A0 16bit
  static const int TIMER0_BASE = 0x40003000;
  static const int TIMER0_CTL_ADDR = 0x00;
  static const int TIMER0_R_ADDR = 0x10;
  static const int TIMER0_CCR0_ADDR = 0x12;
  static const int TIMER0_CCR1_ADDR = 0x14;
  static const int TIMER0_CCR2_ADDR = 0x16;
  static const int TIMER0_EX0_ADDR = 0x20;
  // ADC14 14-bit
  static const int ADC14_BASE = 0x40006000;
  static const int ADC14_CTL0_ADDR = 0x00;
  static const int ADC14_CTL1_ADDR = 0x02;
  static const int ADC14_LO_ADDR = 0x04;
  static const int ADC14_HI_ADDR = 0x06;
  static const int ADC14_MCTL0_ADDR = 0x08;
  static const int ADC14_MEM0_ADDR = 0x20;

}
