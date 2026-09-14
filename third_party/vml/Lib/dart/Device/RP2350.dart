// RP2350 设备定义 - Dart 库
// 生成自: Raspberry/RP2/RP2350
// 版本: 1.0
// 日期: 2026-04-28
// 作者: VML Team
// 描述: Dual Cortex-M33 + RISC-V Hazard3 MCU with 520KB SRAM, 150MHz
// CPU架构: ARM-Cortex-M33
// 位宽: 32位
// 时钟频率: 150000000 Hz

class RP2350Device {
  static const String deviceName = "RP2350";
  static const String manufacturer = "Raspberry";
  static const String family = "RP2";
  static const String version = "1.0";
  static const String architecture = "ARM-Cortex-M33";
  static const int bits = 32;
  static const int clockFrequency = 150000000;

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
  static const int FLASH_START = 0x10000000;
  static const int FLASH_END = 0x107FFFFF;
  static const int FLASH_SIZE = 8388608;  // XIP Flash
  static const int SRAM_START = 0x20000000;
  static const int SRAM_END = 0x20081FFF;
  static const int SRAM_SIZE = 532480;  // Total SRAM
  static const int PERIPHERAL_START = 0x40000000;
  static const int PERIPHERAL_END = 0x5000FFFF;
  static const int PERIPHERAL_SIZE = 16777216;  // 

  // 外设定义
  // Single-Cycle I/O (GPIO)
  static const int SIO_BASE = 0xD0000000;
  static const int SIO_GPIO_IN_ADDR = 0x004;
  static const int SIO_GPIO_OUT_ADDR = 0x010;
  static const int SIO_GPIO_OUT_SET_ADDR = 0x014;
  static const int SIO_GPIO_OUT_CLR_ADDR = 0x018;
  static const int SIO_GPIO_OUT_XOR_ADDR = 0x01C;
  static const int SIO_GPIO_OE_ADDR = 0x020;
  static const int SIO_GPIO_OE_SET_ADDR = 0x024;
  static const int SIO_GPIO_OE_CLR_ADDR = 0x028;
  // IO Bank 0 (GPIO control)
  static const int IO_BANK0_BASE = 0x40028000;
  static const int IO_BANK0_GPIO0_STATUS_ADDR = 0x000;
  static const int IO_BANK0_GPIO0_CTRL_ADDR = 0x004;
  static const int IO_BANK0_GPIO1_STATUS_ADDR = 0x008;
  static const int IO_BANK0_GPIO1_CTRL_ADDR = 0x00C;
  // Pad controls for GPIO 0-29
  static const int PADS_BANK0_BASE = 0x4002C000;
  static const int PADS_BANK0_GPIO0_ADDR = 0x000;
  static const int PADS_BANK0_GPIO1_ADDR = 0x004;
  // Reset Controller
  static const int RESETS_BASE = 0x4000C000;
  static const int RESETS_RESET_ADDR = 0x000;
  static const int RESETS_RESET_DONE_ADDR = 0x008;

  // 中断向量定义
  static const int INT_RESET = 0;  // 
  static const int INT_SVCALL = 11;  // 

}
