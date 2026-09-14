// LPC54606 设备定义 - Dart 库
// 生成自: NXP/LPC/LPC54606
// 版本: 1.0
// 日期: 2026-04-28
// 作者: VML Team
// 描述: 32-bit ARM Cortex-M4 MCU with 256KB Flash, 136KB SRAM, 180MHz
// CPU架构: ARM-Cortex-M4
// 位宽: 32位
// 时钟频率: 180000000 Hz

class LPC54606Device {
  static const String deviceName = "LPC54606";
  static const String manufacturer = "NXP";
  static const String family = "LPC";
  static const String version = "1.0";
  static const String architecture = "ARM-Cortex-M4";
  static const int bits = 32;
  static const int clockFrequency = 180000000;

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
  static const int FLASH_START = 0x00000000;
  static const int FLASH_END = 0x0003FFFF;
  static const int FLASH_SIZE = 262144;  // 
  static const int SRAM_START = 0x20000000;
  static const int SRAM_END = 0x20021FFF;
  static const int SRAM_SIZE = 139264;  // 
  static const int PERIPHERAL_START = 0x40000000;
  static const int PERIPHERAL_END = 0x401FFFFF;
  static const int PERIPHERAL_SIZE = 2097152;  // 

  // 外设定义
  // System Control
  static const int SYSCON_BASE = 0x40000000;
  static const int SYSCON_SYSAHBCLKCTRL_ADDR = 0x80;
  static const int SYSCON_MAINCLKSEL_ADDR = 0x04;
  static const int SYSCON_MAINCLKUEN_ADDR = 0x08;
  static const int SYSCON_SYSPLLCTRL_ADDR = 0x0C;
  // General Purpose I/O
  static const int GPIO_BASE = 0x400F4000;
  static const int GPIO_DIR0_ADDR = 0x0000;
  static const int GPIO_PIN0_ADDR = 0x1000;
  static const int GPIO_SET0_ADDR = 0x2000;
  static const int GPIO_CLR0_ADDR = 0x3000;
  static const int GPIO_NOT0_ADDR = 0x4000;
  static const int GPIO_DIR1_ADDR = 0x0004;
  static const int GPIO_PIN1_ADDR = 0x1004;
  static const int GPIO_SET1_ADDR = 0x2004;
  static const int GPIO_CLR1_ADDR = 0x3004;
  static const int GPIO_NOT1_ADDR = 0x4004;
  // USART0
  static const int USART0_BASE = 0x40086000;
  static const int USART0_CFG_ADDR = 0x00;
  static const int USART0_CTRL_ADDR = 0x04;
  static const int USART0_STAT_ADDR = 0x08;
  static const int USART0_TXDAT_ADDR = 0x10;
  static const int USART0_RXDAT_ADDR = 0x14;
  static const int USART0_BRG_ADDR = 0x20;

  // 中断向量定义
  static const int INT_RESET = 0;  // 
  static const int INT_SVCALL = 11;  // 
  static const int INT_USART0 = 24;  // USART0 Interrupt

}
