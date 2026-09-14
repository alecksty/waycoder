// STM32F072 设备定义 - Dart 库
// 生成自: STMicroelectronics/STM32/STM32F072
// 版本: 1.0
// 日期: 2026-04-28
// 作者: VML Team
// 描述: 32-bit ARM Cortex-M0 MCU with 128KB Flash, 16KB RAM, 48MHz
// CPU架构: ARM-Cortex-M0
// 位宽: 32位
// 时钟频率: 48000000 Hz

class STM32F072Device {
  static const String deviceName = "STM32F072";
  static const String manufacturer = "STMicroelectronics";
  static const String family = "STM32";
  static const String version = "1.0";
  static const String architecture = "ARM-Cortex-M0";
  static const int bits = 32;
  static const int clockFrequency = 48000000;

  // 寄存器地址定义
  static const int R0_ADDR = 0x00;  // 
  static const int R1_ADDR = 0x04;  // 
  static const int R2_ADDR = 0x08;  // 
  static const int R3_ADDR = 0x0C;  // 
  static const int SP_ADDR = 0x34;  // 
  static const int LR_ADDR = 0x38;  // 
  static const int PC_ADDR = 0x3C;  // 

  // 内存段定义
  static const int FLASH_START = 0x08000000;
  static const int FLASH_END = 0x0801FFFF;
  static const int FLASH_SIZE = 131072;  // 
  static const int SRAM_START = 0x20000000;
  static const int SRAM_END = 0x20003FFF;
  static const int SRAM_SIZE = 16384;  // 
  static const int PERIPHERAL_START = 0x40000000;
  static const int PERIPHERAL_END = 0x40027FFF;
  static const int PERIPHERAL_SIZE = 163840;  // 

  // 外设定义
  // Reset and Clock Control
  static const int RCC_BASE = 0x40021000;
  static const int RCC_CR_ADDR = 0x00;
  static const int RCC_CFGR_ADDR = 0x04;
  static const int RCC_AHBENR_ADDR = 0x14;
  static const int RCC_AHBENR_GPIOAEN_BIT = 17;  // GPIOA clock enable
  static const int RCC_AHBENR_GPIOBEN_BIT = 18;  // GPIOB clock enable
  static const int RCC_AHBENR_GPIOCEN_BIT = 19;  // GPIOC clock enable
  static const int RCC_APB2ENR_ADDR = 0x18;
  // General Purpose I/O Port A
  static const int GPIOA_BASE = 0x48000000;
  static const int GPIOA_MODER_ADDR = 0x00;
  static const int GPIOA_OTYPER_ADDR = 0x04;
  static const int GPIOA_OSPEEDR_ADDR = 0x08;
  static const int GPIOA_PUPDR_ADDR = 0x0C;
  static const int GPIOA_IDR_ADDR = 0x10;
  static const int GPIOA_ODR_ADDR = 0x14;
  static const int GPIOA_BSRR_ADDR = 0x18;
  static const int GPIOA_BRR_ADDR = 0x28;
  // General Purpose I/O Port B
  static const int GPIOB_BASE = 0x48000400;
  static const int GPIOB_MODER_ADDR = 0x00;
  static const int GPIOB_OTYPER_ADDR = 0x04;
  static const int GPIOB_OSPEEDR_ADDR = 0x08;
  static const int GPIOB_PUPDR_ADDR = 0x0C;
  static const int GPIOB_IDR_ADDR = 0x10;
  static const int GPIOB_ODR_ADDR = 0x14;
  static const int GPIOB_BSRR_ADDR = 0x18;
  static const int GPIOB_BRR_ADDR = 0x28;
  // General Purpose I/O Port C
  static const int GPIOC_BASE = 0x48000800;
  static const int GPIOC_MODER_ADDR = 0x00;
  static const int GPIOC_OTYPER_ADDR = 0x04;
  static const int GPIOC_IDR_ADDR = 0x10;
  static const int GPIOC_ODR_ADDR = 0x14;
  static const int GPIOC_BSRR_ADDR = 0x18;

  // 中断向量定义
  static const int INT_RESET = 0;  // 
  static const int INT_SVCALL = 11;  // 

}
