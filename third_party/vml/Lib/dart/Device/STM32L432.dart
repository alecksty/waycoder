// STM32L432 设备定义 - Dart 库
// 生成自: STMicroelectronics/STM32/STM32L432
// 版本: 1.0
// 日期: 2026-04-28
// 作者: VML Team
// 描述: 32-bit ARM Cortex-M4 MCU ultra-low-power with 256KB Flash, 64KB RAM, 80MHz
// CPU架构: ARM-Cortex-M4
// 位宽: 32位
// 时钟频率: 80000000 Hz

class STM32L432Device {
  static const String deviceName = "STM32L432";
  static const String manufacturer = "STMicroelectronics";
  static const String family = "STM32";
  static const String version = "1.0";
  static const String architecture = "ARM-Cortex-M4";
  static const int bits = 32;
  static const int clockFrequency = 80000000;

  // 寄存器地址定义
  static const int R0_ADDR = 0x00;  // 
  static const int R1_ADDR = 0x04;  // 
  static const int R2_ADDR = 0x08;  // 
  static const int R3_ADDR = 0x0C;  // 
  static const int R4_ADDR = 0x10;  // 
  static const int R5_ADDR = 0x14;  // 
  static const int SP_ADDR = 0x34;  // 
  static const int LR_ADDR = 0x38;  // 
  static const int PC_ADDR = 0x3C;  // 

  // 内存段定义
  static const int FLASH_START = 0x08000000;
  static const int FLASH_END = 0x0803FFFF;
  static const int FLASH_SIZE = 262144;  // 
  static const int SRAM_START = 0x20000000;
  static const int SRAM_END = 0x2000FFFF;
  static const int SRAM_SIZE = 65536;  // 
  static const int PERIPHERAL_START = 0x40000000;
  static const int PERIPHERAL_END = 0x4007FFFF;
  static const int PERIPHERAL_SIZE = 524288;  // 

  // 外设定义
  // Reset and Clock Control
  static const int RCC_BASE = 0x40021000;
  static const int RCC_CR_ADDR = 0x00;
  static const int RCC_CFGR_ADDR = 0x08;
  static const int RCC_PLLCFGR_ADDR = 0x0C;
  static const int RCC_AHB1ENR_ADDR = 0x38;
  static const int RCC_AHB1ENR_GPIOAEN_BIT = 0;  // GPIOA clock enable
  static const int RCC_AHB1ENR_GPIOBEN_BIT = 1;  // GPIOB clock enable
  static const int RCC_APB1ENR1_ADDR = 0x58;
  static const int RCC_APB2ENR_ADDR = 0x60;
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
  // Low-power UART 1
  static const int LPUART1_BASE = 0x40008000;
  static const int LPUART1_CR1_ADDR = 0x00;
  static const int LPUART1_BRR_ADDR = 0x0C;
  static const int LPUART1_RDR_ADDR = 0x24;
  static const int LPUART1_TDR_ADDR = 0x28;

  // 中断向量定义
  static const int INT_RESET = 0;  // 
  static const int INT_SVCALL = 11;  // 
  static const int INT_LPUART1 = 53;  // LPUART1 Global Interrupt

}
