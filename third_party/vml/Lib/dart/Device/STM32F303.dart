// STM32F303CCT6 设备定义 - Dart 库
// 生成自: STMicroelectronics/STM32/STM32F303CCT6
// 版本: 1.0
// 日期: 2026-04-29
// 作者: VML Team
// 描述: 32-bit ARM Cortex-M4F MCU with 256KB Flash, 48KB SRAM, 72MHz, FPU+DSP
// CPU架构: ARM-Cortex-M4F
// 位宽: 32位
// 时钟频率: 72000000 Hz

class STM32F303CCT6Device {
  static const String deviceName = "STM32F303CCT6";
  static const String manufacturer = "STMicroelectronics";
  static const String family = "STM32";
  static const String version = "1.0";
  static const String architecture = "ARM-Cortex-M4F";
  static const int bits = 32;
  static const int clockFrequency = 72000000;

  // 外设定义
  // USART 1
  static const int USART1_BASE = 0x40013800;
  static const int USART1_SR_ADDR = 0x00;
  static const int USART1_DR_ADDR = 0x04;
  static const int USART1_BRR_ADDR = 0x08;
  static const int USART1_CR1_ADDR = 0x0C;
  static const int USART1_CR2_ADDR = 0x10;
  static const int USART1_CR3_ADDR = 0x14;
  // USART 2
  static const int USART2_BASE = 0x40004400;
  static const int USART2_SR_ADDR = 0x00;
  static const int USART2_DR_ADDR = 0x04;
  static const int USART2_BRR_ADDR = 0x08;
  static const int USART2_CR1_ADDR = 0x0C;
  // USART 3
  static const int USART3_BASE = 0x40004800;
  static const int USART3_SR_ADDR = 0x00;
  static const int USART3_DR_ADDR = 0x04;
  static const int USART3_BRR_ADDR = 0x08;
  static const int USART3_CR1_ADDR = 0x0C;
  // GPIO Port A
  static const int GPIOA_BASE = 0x48000000;
  static const int GPIOA_MODER_ADDR = 0x00;
  static const int GPIOA_OTYPER_ADDR = 0x04;
  static const int GPIOA_OSPEEDR_ADDR = 0x08;
  static const int GPIOA_PUPDR_ADDR = 0x0C;
  static const int GPIOA_IDR_ADDR = 0x10;
  static const int GPIOA_ODR_ADDR = 0x14;
  static const int GPIOA_BSRR_ADDR = 0x18;
  static const int GPIOA_AFRL_ADDR = 0x20;
  static const int GPIOA_AFRH_ADDR = 0x24;
  // 高级定时器 1
  static const int TIM1_BASE = 0x40012C00;
  static const int TIM1_CR1_ADDR = 0x00;
  static const int TIM1_CNT_ADDR = 0x24;
  static const int TIM1_PSC_ADDR = 0x28;
  static const int TIM1_ARR_ADDR = 0x2C;
  static const int TIM1_CCR1_ADDR = 0x34;
  // ADC 1
  static const int ADC1_BASE = 0x50000000;
  static const int ADC1_SR_ADDR = 0x00;
  static const int ADC1_CR_ADDR = 0x08;
  static const int ADC1_CFGR_ADDR = 0x0C;
  static const int ADC1_SMPR1_ADDR = 0x14;
  static const int ADC1_DR_ADDR = 0x40;

}
