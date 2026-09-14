// CH32V003 设备定义 - Dart 库
// 生成自: WCH/CH32V0/CH32V003
// 版本: 1.0
// 日期: 2026-04-28
// 作者: VML Team
// 描述: 32-bit RISC-V RV32EC MCU with 16KB Flash, 2KB RAM, 48MHz, ultra-low-cost
// CPU架构: RISC-V
// 位宽: 32位
// 时钟频率: 48000000 Hz

class CH32V003Device {
  static const String deviceName = "CH32V003";
  static const String manufacturer = "WCH";
  static const String family = "CH32V0";
  static const String version = "1.0";
  static const String architecture = "RISC-V";
  static const int bits = 32;
  static const int clockFrequency = 48000000;

  // 寄存器地址定义
  static const int X1_ADDR = 0x04;  // Return Address
  static const int X2_ADDR = 0x08;  // Stack Pointer (SP)
  static const int X3_ADDR = 0x0C;  // Global Pointer (GP)
  static const int PC_ADDR = 0x3C;  // Program Counter

  // 内存段定义
  static const int FLASH_START = 0x08000000;
  static const int FLASH_END = 0x08003FFF;
  static const int FLASH_SIZE = 16384;  // 
  static const int SRAM_START = 0x20000000;
  static const int SRAM_END = 0x200007FF;
  static const int SRAM_SIZE = 2048;  // 
  static const int PERIPHERAL_START = 0x40000000;
  static const int PERIPHERAL_END = 0x40003FFF;
  static const int PERIPHERAL_SIZE = 16384;  // 

  // 外设定义
  // Reset and Clock Control
  static const int RCC_BASE = 0x40021000;
  static const int RCC_CTLR_ADDR = 0x00;
  static const int RCC_CFGR0_ADDR = 0x04;
  static const int RCC_APB2PCENR_ADDR = 0x18;
  static const int RCC_APB2PCENR_IOPAEN_BIT = 2;  // GPIOA clock enable
  static const int RCC_APB2PCENR_IOPCEN_BIT = 4;  // GPIOC clock enable
  static const int RCC_APB2PCENR_IOPDEN_BIT = 5;  // GPIOD clock enable
  // General Purpose I/O Port A
  static const int GPIOA_BASE = 0x40010800;
  static const int GPIOA_CFGLR_ADDR = 0x00;
  static const int GPIOA_CFGHR_ADDR = 0x04;
  static const int GPIOA_INDR_ADDR = 0x08;
  static const int GPIOA_OUTDR_ADDR = 0x0C;
  static const int GPIOA_BSHR_ADDR = 0x10;
  static const int GPIOA_BCR_ADDR = 0x14;
  // General Purpose I/O Port C
  static const int GPIOC_BASE = 0x40011000;
  static const int GPIOC_CFGLR_ADDR = 0x00;
  static const int GPIOC_CFGHR_ADDR = 0x04;
  static const int GPIOC_INDR_ADDR = 0x08;
  static const int GPIOC_OUTDR_ADDR = 0x0C;
  static const int GPIOC_BSHR_ADDR = 0x10;
  static const int GPIOC_BCR_ADDR = 0x14;
  // General Purpose I/O Port D
  static const int GPIOD_BASE = 0x40011400;
  static const int GPIOD_CFGLR_ADDR = 0x00;
  static const int GPIOD_CFGHR_ADDR = 0x04;
  static const int GPIOD_INDR_ADDR = 0x08;
  static const int GPIOD_OUTDR_ADDR = 0x0C;
  static const int GPIOD_BSHR_ADDR = 0x10;
  static const int GPIOD_BCR_ADDR = 0x14;
  // USART1
  static const int USART1_BASE = 0x40013800;
  static const int USART1_STATR_ADDR = 0x00;
  static const int USART1_DATAR_ADDR = 0x04;
  static const int USART1_BRR_ADDR = 0x08;
  static const int USART1_CTLR1_ADDR = 0x0C;

  // 中断向量定义
  static const int INT_RESET = 1;  // 
  static const int INT_MACHINESOFTWARE = 3;  // 
  static const int INT_MACHINETIMER = 7;  // 
  static const int INT_MACHINEEXTERNAL = 11;  // 
  static const int INT_USART1 = 25;  // USART1 Global Interrupt

}
