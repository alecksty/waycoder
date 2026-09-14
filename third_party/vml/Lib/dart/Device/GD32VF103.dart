// GD32VF103 设备定义 - Dart 库
// 生成自: GigaDevice/GD32/GD32VF103
// 版本: 1.0
// 日期: 2026-04-28
// 作者: VML Team
// 描述: 32-bit RISC-V RV32IMAC MCU with 128KB Flash, 32KB RAM, 108MHz, STM32F103 compatible
// CPU架构: RISC-V
// 位宽: 32位
// 时钟频率: 108000000 Hz

class GD32VF103Device {
  static const String deviceName = "GD32VF103";
  static const String manufacturer = "GigaDevice";
  static const String family = "GD32";
  static const String version = "1.0";
  static const String architecture = "RISC-V";
  static const int bits = 32;
  static const int clockFrequency = 108000000;

  // 寄存器地址定义
  static const int X1_ADDR = 0x04;  // Return Address
  static const int X2_ADDR = 0x08;  // Stack Pointer (SP)
  static const int X3_ADDR = 0x0C;  // Global Pointer (GP)
  static const int X8_ADDR = 0x20;  // Frame Pointer (FP)
  static const int X10_ADDR = 0x28;  // Function Argument (A0)
  static const int X11_ADDR = 0x2C;  // Function Argument (A1)
  static const int PC_ADDR = 0x3C;  // Program Counter

  // 内存段定义
  static const int FLASH_START = 0x08000000;
  static const int FLASH_END = 0x0801FFFF;
  static const int FLASH_SIZE = 131072;  // 
  static const int SRAM_START = 0x20000000;
  static const int SRAM_END = 0x20007FFF;
  static const int SRAM_SIZE = 32768;  // 
  static const int PERIPHERAL_START = 0x40000000;
  static const int PERIPHERAL_END = 0x4003FFFF;
  static const int PERIPHERAL_SIZE = 262144;  // 

  // 外设定义
  // Reset and Clock Control
  static const int RCU_BASE = 0x40021000;
  static const int RCU_CTL_ADDR = 0x00;
  static const int RCU_CFG0_ADDR = 0x04;
  static const int RCU_CFG1_ADDR = 0x08;
  static const int RCU_APB2EN_ADDR = 0x18;
  static const int RCU_APB2EN_PAEN_BIT = 2;  // GPIOA enable
  static const int RCU_APB2EN_PBEN_BIT = 3;  // GPIOB enable
  static const int RCU_APB2EN_PCEN_BIT = 4;  // GPIOC enable
  static const int RCU_APB2EN_USART0EN_BIT = 14;  // USART0 enable
  static const int RCU_APB1EN_ADDR = 0x1C;
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

  // 中断向量定义
  static const int INT_RESET = 1;  // 
  static const int INT_MACHINESOFTWARE = 3;  // 
  static const int INT_MACHINETIMER = 7;  // 
  static const int INT_MACHINEEXTERNAL = 11;  // 
  static const int INT_USART0 = 25;  // USART0 Global Interrupt

}
