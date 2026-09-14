// STM32F411 设备定义 - Dart 库
// 生成自: STMicroelectronics/STM32/STM32F411
// 版本: 1.0
// 日期: 2026-04-28
// 作者: VML Team
// 描述: 32-bit ARM Cortex-M4 MCU with 512KB Flash, 128KB RAM, 100MHz
// CPU架构: ARM-Cortex-M4
// 位宽: 32位
// 时钟频率: 100000000 Hz

class STM32F411Device {
  static const String deviceName = "STM32F411";
  static const String manufacturer = "STMicroelectronics";
  static const String family = "STM32";
  static const String version = "1.0";
  static const String architecture = "ARM-Cortex-M4";
  static const int bits = 32;
  static const int clockFrequency = 100000000;

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
  static const int FLASH_END = 0x0807FFFF;
  static const int FLASH_SIZE = 524288;  // 
  static const int SRAM_START = 0x20000000;
  static const int SRAM_END = 0x2001FFFF;
  static const int SRAM_SIZE = 131072;  // 
  static const int PERIPHERAL_START = 0x40000000;
  static const int PERIPHERAL_END = 0x400FFFFF;
  static const int PERIPHERAL_SIZE = 1048576;  // 

  // 外设定义
  // Reset and Clock Control
  static const int RCC_BASE = 0x40023800;
  static const int RCC_CR_ADDR = 0x00;
  static const int RCC_PLLCFGR_ADDR = 0x04;
  static const int RCC_CFGR_ADDR = 0x08;
  static const int RCC_AHB1ENR_ADDR = 0x30;
  static const int RCC_AHB1ENR_GPIOAEN_BIT = 0;  // GPIOA clock enable
  static const int RCC_AHB1ENR_GPIOBEN_BIT = 1;  // GPIOB clock enable
  static const int RCC_AHB1ENR_GPIOCEN_BIT = 2;  // GPIOC clock enable
  static const int RCC_APB1ENR_ADDR = 0x40;
  static const int RCC_APB2ENR_ADDR = 0x44;
  // General Purpose I/O Port A
  static const int GPIOA_BASE = 0x40020000;
  static const int GPIOA_MODER_ADDR = 0x00;
  static const int GPIOA_OTYPER_ADDR = 0x04;
  static const int GPIOA_OSPEEDR_ADDR = 0x08;
  static const int GPIOA_PUPDR_ADDR = 0x0C;
  static const int GPIOA_IDR_ADDR = 0x10;
  static const int GPIOA_ODR_ADDR = 0x14;
  static const int GPIOA_BSRR_ADDR = 0x18;
  static const int GPIOA_BRR_ADDR = 0x28;
  // General Purpose I/O Port B
  static const int GPIOB_BASE = 0x40020400;
  static const int GPIOB_MODER_ADDR = 0x00;
  static const int GPIOB_OTYPER_ADDR = 0x04;
  static const int GPIOB_OSPEEDR_ADDR = 0x08;
  static const int GPIOB_PUPDR_ADDR = 0x0C;
  static const int GPIOB_IDR_ADDR = 0x10;
  static const int GPIOB_ODR_ADDR = 0x14;
  static const int GPIOB_BSRR_ADDR = 0x18;
  static const int GPIOB_BRR_ADDR = 0x28;
  // General Purpose I/O Port C
  static const int GPIOC_BASE = 0x40020800;
  static const int GPIOC_MODER_ADDR = 0x00;
  static const int GPIOC_OTYPER_ADDR = 0x04;
  static const int GPIOC_IDR_ADDR = 0x10;
  static const int GPIOC_ODR_ADDR = 0x14;
  static const int GPIOC_BSRR_ADDR = 0x18;
  // Universal Synchronous/Asynchronous Receiver/Transmitter 1
  static const int USART1_BASE = 0x40011000;
  static const int USART1_SR_ADDR = 0x00;
  static const int USART1_DR_ADDR = 0x04;
  static const int USART1_BRR_ADDR = 0x08;
  static const int USART1_CR1_ADDR = 0x0C;

  // 中断向量定义
  static const int INT_RESET = 0;  // 
  static const int INT_SVCALL = 11;  // 
  static const int INT_USART1 = 37;  // USART1 Global Interrupt

}
