// STM32H743 设备定义 - Dart 库
// 生成自: STMicroelectronics/STM32/STM32H743
// 版本: 1.0
// 日期: 2026-04-28
// 作者: VML Team
// 描述: 32-bit ARM Cortex-M7 MCU with 2MB Flash, 1MB RAM, 400MHz
// CPU架构: ARM-Cortex-M7
// 位宽: 32位
// 时钟频率: 400000000 Hz

class STM32H743Device {
  static const String deviceName = "STM32H743";
  static const String manufacturer = "STMicroelectronics";
  static const String family = "STM32";
  static const String version = "1.0";
  static const String architecture = "ARM-Cortex-M7";
  static const int bits = 32;
  static const int clockFrequency = 400000000;

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
  static const int FLASH_END = 0x081FFFFF;
  static const int FLASH_SIZE = 2097152;  // 
  static const int DTCM_START = 0x20000000;
  static const int DTCM_END = 0x2001FFFF;
  static const int DTCM_SIZE = 131072;  // DTCM RAM
  static const int ITCM_START = 0x00000000;
  static const int ITCM_END = 0x0000FFFF;
  static const int ITCM_SIZE = 65536;  // ITCM RAM
  static const int SRAM_AXI_START = 0x24000000;
  static const int SRAM_AXI_END = 0x2407FFFF;
  static const int SRAM_AXI_SIZE = 524288;  // AXI SRAM
  static const int SRAM_SRAM_START = 0x30000000;
  static const int SRAM_SRAM_END = 0x3003FFFF;
  static const int SRAM_SRAM_SIZE = 262144;  // SRAM1-3
  static const int PERIPHERAL_START = 0x40000000;
  static const int PERIPHERAL_END = 0x4FFFFFFF;
  static const int PERIPHERAL_SIZE = 268435456;  // 

  // 外设定义
  // Reset and Clock Control
  static const int RCC_BASE = 0x58024400;
  static const int RCC_CR_ADDR = 0x00;
  static const int RCC_CFGR_ADDR = 0x04;
  static const int RCC_PLL1CFGR_ADDR = 0x0C;
  static const int RCC_AHB1ENR_ADDR = 0x30;
  static const int RCC_AHB1ENR_GPIOAEN_BIT = 0;  // GPIOA clock enable
  static const int RCC_AHB1ENR_GPIOBEN_BIT = 1;  // GPIOB clock enable
  static const int RCC_AHB1ENR_GPIOCEN_BIT = 2;  // GPIOC clock enable
  static const int RCC_AHB1ENR_GPIODEN_BIT = 3;  // GPIOD clock enable
  static const int RCC_AHB1ENR_GPIOEEN_BIT = 4;  // GPIOE clock enable
  static const int RCC_AHB1ENR_DMA1EN_BIT = 21;  // DMA1 clock enable
  static const int RCC_AHB1ENR_DMA2EN_BIT = 22;  // DMA2 clock enable
  static const int RCC_AHB2ENR_ADDR = 0x34;
  static const int RCC_AHB4ENR_ADDR = 0x3C;
  static const int RCC_APB1LENR_ADDR = 0x50;
  static const int RCC_APB2ENR_ADDR = 0x58;
  // General Purpose I/O Port A
  static const int GPIOA_BASE = 0x58020000;
  static const int GPIOA_MODER_ADDR = 0x00;
  static const int GPIOA_OTYPER_ADDR = 0x04;
  static const int GPIOA_OSPEEDR_ADDR = 0x08;
  static const int GPIOA_PUPDR_ADDR = 0x0C;
  static const int GPIOA_IDR_ADDR = 0x10;
  static const int GPIOA_ODR_ADDR = 0x14;
  static const int GPIOA_BSRR_ADDR = 0x18;
  static const int GPIOA_BRR_ADDR = 0x28;
  // General Purpose I/O Port B
  static const int GPIOB_BASE = 0x58020400;
  static const int GPIOB_MODER_ADDR = 0x00;
  static const int GPIOB_OTYPER_ADDR = 0x04;
  static const int GPIOB_OSPEEDR_ADDR = 0x08;
  static const int GPIOB_PUPDR_ADDR = 0x0C;
  static const int GPIOB_IDR_ADDR = 0x10;
  static const int GPIOB_ODR_ADDR = 0x14;
  static const int GPIOB_BSRR_ADDR = 0x18;
  static const int GPIOB_BRR_ADDR = 0x28;
  // General Purpose I/O Port C
  static const int GPIOC_BASE = 0x58020800;
  static const int GPIOC_MODER_ADDR = 0x00;
  static const int GPIOC_OTYPER_ADDR = 0x04;
  static const int GPIOC_IDR_ADDR = 0x10;
  static const int GPIOC_ODR_ADDR = 0x14;
  static const int GPIOC_BSRR_ADDR = 0x18;
  // General Purpose I/O Port D
  static const int GPIOD_BASE = 0x58020C00;
  static const int GPIOD_MODER_ADDR = 0x00;
  static const int GPIOD_OTYPER_ADDR = 0x04;
  static const int GPIOD_IDR_ADDR = 0x10;
  static const int GPIOD_ODR_ADDR = 0x14;
  static const int GPIOD_BSRR_ADDR = 0x18;
  // General Purpose I/O Port E
  static const int GPIOE_BASE = 0x58021000;
  static const int GPIOE_MODER_ADDR = 0x00;
  static const int GPIOE_OTYPER_ADDR = 0x04;
  static const int GPIOE_IDR_ADDR = 0x10;
  static const int GPIOE_ODR_ADDR = 0x14;
  static const int GPIOE_BSRR_ADDR = 0x18;
  // USART1
  static const int USART1_BASE = 0x40011000;
  static const int USART1_CR1_ADDR = 0x00;
  static const int USART1_BRR_ADDR = 0x0C;
  static const int USART1_RDR_ADDR = 0x24;
  static const int USART1_TDR_ADDR = 0x28;

  // 中断向量定义
  static const int INT_RESET = 0;  // 
  static const int INT_SVCALL = 11;  // 
  static const int INT_SYSTICK = 15;  // 
  static const int INT_USART1 = 56;  // USART1 Global Interrupt

}
