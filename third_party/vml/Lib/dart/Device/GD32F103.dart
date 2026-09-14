// GD32F103 设备定义 - Dart 库
// 生成自: GigaDevice/GD32/GD32F103
// 版本: 1.0
// 日期: 2026-04-28
// 作者: VML Team
// 描述: 32-bit ARM Cortex-M3 MCU, 108MHz, STM32F103 compatible
// CPU架构: ARM-Cortex-M3
// 位宽: 32位
// 时钟频率: 108000000 Hz

class GD32F103Device {
  static const String deviceName = "GD32F103";
  static const String manufacturer = "GigaDevice";
  static const String family = "GD32";
  static const String version = "1.0";
  static const String architecture = "ARM-Cortex-M3";
  static const int bits = 32;
  static const int clockFrequency = 108000000;

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
  static const int FLASH_END = 0x0801FFFF;
  static const int FLASH_SIZE = 131072;  // 
  static const int SRAM_START = 0x20000000;
  static const int SRAM_END = 0x20004FFF;
  static const int SRAM_SIZE = 20480;  // 
  static const int PERIPHERAL_START = 0x40000000;
  static const int PERIPHERAL_END = 0x4003FFFF;
  static const int PERIPHERAL_SIZE = 262144;  // 

  // 外设定义
  // Reset and Clock Control
  static const int RCC_BASE = 0x40021000;
  static const int RCC_CTLR_ADDR = 0x00;
  static const int RCC_CFGR0_ADDR = 0x04;
  static const int RCC_APB2PCENR_ADDR = 0x18;
  static const int RCC_APB2PCENR_IOPAEN_BIT = 2;  // GPIOA clock enable
  static const int RCC_APB2PCENR_IOPBEN_BIT = 3;  // GPIOB clock enable
  static const int RCC_APB2PCENR_IOPCEN_BIT = 4;  // GPIOC clock enable
  static const int RCC_APB2PCENR_USART0EN_BIT = 14;  // USART0 clock enable
  static const int RCC_APB1PCENR_ADDR = 0x1C;
  static const int RCC_APB1PCENR_USART1EN_BIT = 17;  // USART1 clock enable
  // General Purpose I/O Port A
  static const int GPIOA_BASE = 0x40010800;
  static const int GPIOA_CTL0_ADDR = 0x00;
  static const int GPIOA_CTL1_ADDR = 0x04;
  static const int GPIOA_ISTAT_ADDR = 0x08;
  static const int GPIOA_OCTL_ADDR = 0x0C;
  static const int GPIOA_BOP_ADDR = 0x10;
  static const int GPIOA_BC_ADDR = 0x14;
  // General Purpose I/O Port B
  static const int GPIOB_BASE = 0x40010C00;
  static const int GPIOB_CTL0_ADDR = 0x00;
  static const int GPIOB_CTL1_ADDR = 0x04;
  static const int GPIOB_ISTAT_ADDR = 0x08;
  static const int GPIOB_OCTL_ADDR = 0x0C;
  static const int GPIOB_BOP_ADDR = 0x10;
  static const int GPIOB_BC_ADDR = 0x14;
  // General Purpose I/O Port C
  static const int GPIOC_BASE = 0x40011000;
  static const int GPIOC_CTL0_ADDR = 0x00;
  static const int GPIOC_CTL1_ADDR = 0x04;
  static const int GPIOC_ISTAT_ADDR = 0x08;
  static const int GPIOC_OCTL_ADDR = 0x0C;
  static const int GPIOC_BOP_ADDR = 0x10;
  static const int GPIOC_BC_ADDR = 0x14;
  // USART0
  static const int USART0_BASE = 0x40013800;
  static const int USART0_STATR_ADDR = 0x00;
  static const int USART0_DATAR_ADDR = 0x04;
  static const int USART0_BRR_ADDR = 0x08;
  static const int USART0_CTLR1_ADDR = 0x0C;
  // USART1
  static const int USART1_BASE = 0x40004400;
  static const int USART1_STATR_ADDR = 0x00;
  static const int USART1_DATAR_ADDR = 0x04;
  static const int USART1_BRR_ADDR = 0x08;
  static const int USART1_CTLR1_ADDR = 0x0C;

  // 中断向量定义
  static const int INT_RESET = 0;  // 
  static const int INT_SVCALL = 11;  // 
  static const int INT_USART0 = 25;  // USART0 Global Interrupt
  static const int INT_USART1 = 37;  // USART1 Global Interrupt

}
